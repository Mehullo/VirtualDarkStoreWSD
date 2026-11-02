using System;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_ANDROID
using Google.OrTools.ConstraintSolver;
#endif

namespace VirtualDarkStore.Routing
{
    /// <summary>
    /// Pick-sequence route optimizer powered by Google OR-Tools running locally.
    /// Solves the multi-stop warehouse TSP with fixed start (Order Desk) and fixed end (Packing Bench).
    /// Fully isolated behind IRouteOptimizer with safe automatic fallback to baseline sequence on any solver failure.
    /// Does not claim global optimality unless mathematically proven.
    /// </summary>
    public class OrToolsRouteOptimizer : IRouteOptimizer
    {
        public string SolverName => "Google OR-Tools (Local Routing Solver)";

        private readonly BaselineRouteOptimizer fallbackOptimizer = new BaselineRouteOptimizer();

        public RouteOptimizationResult OptimizeRoute(TravelCostMatrix matrix, IReadOnlyList<string> skuIds)
        {
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));

            // Calculate baseline cost first for comparison and fallback
            var baselineResult = fallbackOptimizer.OptimizeRoute(matrix, skuIds);
            float baselineCost = baselineResult.TotalCost;

            int totalNodes = matrix.NodeCount;
            int stopCount = totalNodes - 2;

            // Trivial cases: 0 or 1 stop -> baseline is already optimal
            if (stopCount <= 1)
            {
                var trivialResult = baselineResult;
                trivialResult.SolverName = SolverName;
                trivialResult.StatusMessage = "Trivial route (<= 1 stop); baseline sequence is optimal.";
                trivialResult.IsOptimalProven = true;
                return trivialResult;
            }

#if UNITY_ANDROID
            // Standalone Meta Quest Android does not support Google OR-Tools native x64 binaries.
            // Safely fallback to baseline sequence.
            baselineResult.SolverName = $"{SolverName} [Fallback]";
            baselineResult.StatusMessage = "OR-Tools native binaries unsupported on Android. Safely fell back to baseline sequence.";
            return baselineResult;
#else
            try
            {
                // Nodes: 0 (Start), 1..K (Stops), K+1 (End)
                int startNode = 0;
                int endNode = totalNodes - 1;

                var manager = new RoutingIndexManager(totalNodes, 1, new int[] { startNode }, new int[] { endNode });
                var routing = new RoutingModel(manager);

                int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                {
                    int fromNode = manager.IndexToNode(fromIndex);
                    int toNode = manager.IndexToNode(toIndex);

                    float cost = matrix.GetCost(fromNode, toNode);
                    if (float.IsPositiveInfinity(cost) || float.IsNaN(cost) || cost < 0f)
                    {
                        return 100000000L; // Severe penalty for unreachable edges
                    }

                    // Millimeter precision integer cost
                    return (long)(cost * 1000f);
                });

                routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

                RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
                searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
                searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 1 };

                Assignment solution = routing.SolveWithParameters(searchParameters);

                var status = routing.GetStatus();
                bool isSuccess = status == RoutingSearchStatus.Types.Value.RoutingSuccess || status == RoutingSearchStatus.Types.Value.RoutingOptimal;

                if (solution == null || !isSuccess)
                {
                    Debug.LogWarning($"[OrToolsRouteOptimizer] Solver did not find solution (status={status}). Falling back to baseline.");
                    baselineResult.SolverName = $"{SolverName} [Fallback]";
                    baselineResult.StatusMessage = $"OR-Tools solver could not find valid route (status {status}). Safely fell back to baseline.";
                    return baselineResult;
                }

                // Extract visit sequence
                var visitOrder = new List<int>();
                long index = routing.Start(0);
                while (!routing.IsEnd(index))
                {
                    visitOrder.Add(manager.IndexToNode(index));
                    index = solution.Value(routing.NextVar(index));
                }
                visitOrder.Add(manager.IndexToNode(index)); // Append end node

                // Validate resulting route
                if (!ValidateOptimizedRoute(visitOrder, totalNodes, out string validationError))
                {
                    Debug.LogWarning($"[OrToolsRouteOptimizer] Route validation failed: {validationError}. Falling back to baseline.");
                    baselineResult.SolverName = $"{SolverName} [Fallback]";
                    baselineResult.StatusMessage = $"OR-Tools generated invalid route ({validationError}). Safely fell back to baseline.";
                    return baselineResult;
                }

                // Build ordered SKU sequence
                var orderedSkus = new List<string>(stopCount);
                for (int i = 1; i < visitOrder.Count - 1; i++)
                {
                    int stopIdx = visitOrder[i] - 1; // 1-based stop index to 0-based skuIds index
                    if (skuIds != null && stopIdx >= 0 && stopIdx < skuIds.Count)
                    {
                        orderedSkus.Add(skuIds[stopIdx]);
                    }
                    else
                    {
                        orderedSkus.Add($"Stop-{stopIdx + 1}");
                    }
                }

                float totalCost = BaselineRouteOptimizer.CalculateSequenceCost(matrix, visitOrder);
                float improvement = baselineCost > 0.001f ? ((baselineCost - totalCost) / baselineCost) * 100f : 0f;

                return new RouteOptimizationResult
                {
                    Success = true,
                    SolverName = SolverName,
                    VisitOrder = visitOrder,
                    SkuSequence = orderedSkus,
                    TotalCost = totalCost,
                    BaselineCost = baselineCost,
                    ImprovementPercentage = Mathf.Max(0f, improvement),
                    IsOptimalProven = false, // Heuristic guided local search does not guarantee proven global optimality
                    StatusMessage = $"OR-Tools optimization complete: {totalCost:F2}m vs baseline {baselineCost:F2}m ({improvement:F1}% travel reduction)."
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OrToolsRouteOptimizer] Exception running OR-Tools solver: {ex.Message}. Falling back to baseline sequence.");
                baselineResult.SolverName = $"{SolverName} [Fallback]";
                baselineResult.StatusMessage = $"OR-Tools exception ({ex.GetType().Name}). Safely fell back to baseline.";
                return baselineResult;
            }
#endif
        }

        private static bool ValidateOptimizedRoute(List<int> visitOrder, int totalNodes, out string error)
        {
            error = null;
            if (visitOrder == null || visitOrder.Count != totalNodes)
            {
                error = $"Route length ({visitOrder?.Count ?? 0}) does not match expected node count ({totalNodes}).";
                return false;
            }

            if (visitOrder[0] != 0)
            {
                error = $"Route does not start at node 0 (starts at {visitOrder[0]}).";
                return false;
            }

            if (visitOrder[totalNodes - 1] != totalNodes - 1)
            {
                error = $"Route does not end at packing bench (node {totalNodes - 1}, ends at {visitOrder[totalNodes - 1]}).";
                return false;
            }

            var visitedStops = new HashSet<int>();
            for (int i = 1; i < totalNodes - 1; i++)
            {
                int node = visitOrder[i];
                if (node < 1 || node >= totalNodes - 1)
                {
                    error = $"Unexpected stop node index: {node}.";
                    return false;
                }
                if (!visitedStops.Add(node))
                {
                    error = $"Duplicate stop node in route: {node}.";
                    return false;
                }
            }

            if (visitedStops.Count != totalNodes - 2)
            {
                error = $"Expected {totalNodes - 2} unique stops, but found {visitedStops.Count}.";
                return false;
            }

            return true;
        }
    }
}
