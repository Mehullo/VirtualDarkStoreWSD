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
            // Solve using exact/2-opt pure C# warehouse routing solver.
            return SolvePureCsharpTsp(matrix, skuIds, baselineResult);
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

        private RouteOptimizationResult SolvePureCsharpTsp(TravelCostMatrix matrix, IReadOnlyList<string> skuIds, RouteOptimizationResult baselineResult)
        {
            int totalNodes = matrix.NodeCount;
            int stopCount = totalNodes - 2;

            var stops = new List<int>(stopCount);
            for (int i = 1; i <= stopCount; i++) stops.Add(i);

            List<int> bestStops = null;
            float bestCost = float.PositiveInfinity;

            if (stopCount <= 8)
            {
                // Exact permutation search for standard pick orders
                void Permute(int k)
                {
                    if (k == stops.Count)
                    {
                        var candidate = new List<int>(totalNodes) { 0 };
                        candidate.AddRange(stops);
                        candidate.Add(totalNodes - 1);
                        float c = BaselineRouteOptimizer.CalculateSequenceCost(matrix, candidate);
                        if (c < bestCost)
                        {
                            bestCost = c;
                            bestStops = new List<int>(stops);
                        }
                        return;
                    }
                    for (int i = k; i < stops.Count; i++)
                    {
                        int temp = stops[k]; stops[k] = stops[i]; stops[i] = temp;
                        Permute(k + 1);
                        temp = stops[k]; stops[k] = stops[i]; stops[i] = temp;
                    }
                }
                Permute(0);
            }
            else
            {
                // Nearest-neighbor initialization
                var unvisited = new HashSet<int>(stops);
                var currentRoute = new List<int>(stopCount);
                int currentNode = 0;
                while (unvisited.Count > 0)
                {
                    int nearest = -1;
                    float nearestDist = float.PositiveInfinity;
                    foreach (int u in unvisited)
                    {
                        float d = matrix.GetCost(currentNode, u);
                        if (d < nearestDist) { nearestDist = d; nearest = u; }
                    }
                    unvisited.Remove(nearest);
                    currentRoute.Add(nearest);
                    currentNode = nearest;
                }

                // 2-opt local search refinement
                bool improved = true;
                while (improved)
                {
                    improved = false;
                    for (int i = 0; i < currentRoute.Count - 1; i++)
                    {
                        for (int j = i + 1; j < currentRoute.Count; j++)
                        {
                            int nPrev = (i == 0) ? 0 : currentRoute[i - 1];
                            int nI = currentRoute[i];
                            int nJ = currentRoute[j];
                            int nNext = (j == currentRoute.Count - 1) ? totalNodes - 1 : currentRoute[j + 1];

                            float currentDist = matrix.GetCost(nPrev, nI) + matrix.GetCost(nJ, nNext);
                            float newDist = matrix.GetCost(nPrev, nJ) + matrix.GetCost(nI, nNext);
                            if (newDist < currentDist - 0.001f)
                            {
                                currentRoute.Reverse(i, j - i + 1);
                                improved = true;
                            }
                        }
                    }
                }
                bestStops = currentRoute;
                var testOrder = new List<int>(totalNodes) { 0 };
                testOrder.AddRange(bestStops);
                testOrder.Add(totalNodes - 1);
                bestCost = BaselineRouteOptimizer.CalculateSequenceCost(matrix, testOrder);
            }

            if (bestStops == null || float.IsPositiveInfinity(bestCost))
            {
                baselineResult.SolverName = $"{SolverName} [Fallback]";
                baselineResult.StatusMessage = "No reachable path found between stops. Safely fell back to baseline sequence.";
                return baselineResult;
            }

            var visitOrder = new List<int>(totalNodes) { 0 };
            if (bestStops != null) visitOrder.AddRange(bestStops);
            visitOrder.Add(totalNodes - 1);

            float baselineCost = baselineResult.TotalCost;
            float improvement = baselineCost > 0.001f ? ((baselineCost - bestCost) / baselineCost) * 100f : 0f;

            var orderedSkus = new List<string>(stopCount);
            if (bestStops != null)
            {
                for (int i = 0; i < bestStops.Count; i++)
                {
                    int stopIdx = bestStops[i] - 1;
                    if (skuIds != null && stopIdx >= 0 && stopIdx < skuIds.Count)
                        orderedSkus.Add(skuIds[stopIdx]);
                    else
                        orderedSkus.Add($"Stop-{stopIdx + 1}");
                }
            }

            return new RouteOptimizationResult
            {
                Success = true,
                SolverName = $"{SolverName} [C# Standalone Optimizer]",
                VisitOrder = visitOrder,
                SkuSequence = orderedSkus,
                TotalCost = bestCost,
                BaselineCost = baselineCost,
                ImprovementPercentage = Mathf.Max(0f, improvement),
                IsOptimalProven = stopCount <= 8,
                StatusMessage = $"Standalone routing complete: {bestCost:F2}m vs baseline {baselineCost:F2}m ({improvement:F1}% travel reduction)."
            };
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
