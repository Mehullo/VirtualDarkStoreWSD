using System;
using System.Collections.Generic;

namespace VirtualDarkStore.Routing
{
    /// <summary>
    /// Non-optimizing baseline route planner that preserves the original order pick sequence.
    /// Used as the reference baseline for work-study sequence comparison and as an unconditional safe fallback.
    /// Traverses Start (0) -> Stop 1 -> Stop 2 -> ... -> Stop K -> Packing (K+1).
    /// </summary>
    public class BaselineRouteOptimizer : IRouteOptimizer
    {
        public string SolverName => "Baseline (Original Order Sequence)";

        public RouteOptimizationResult OptimizeRoute(TravelCostMatrix matrix, IReadOnlyList<string> skuIds)
        {
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            int totalNodes = matrix.NodeCount;
            int stopCount = totalNodes - 2;

            var visitOrder = new List<int>(totalNodes);
            for (int i = 0; i < totalNodes; i++)
            {
                visitOrder.Add(i);
            }

            var skuSequence = new List<string>(stopCount);
            for (int i = 0; i < stopCount; i++)
            {
                if (skuIds != null && i < skuIds.Count)
                {
                    skuSequence.Add(skuIds[i]);
                }
                else
                {
                    skuSequence.Add($"Item-{i + 1}");
                }
            }

            float cost = CalculateSequenceCost(matrix, visitOrder);
            bool isReachable = !float.IsPositiveInfinity(cost) && !float.IsNaN(cost);

            return new RouteOptimizationResult
            {
                Success = isReachable,
                SolverName = SolverName,
                VisitOrder = visitOrder,
                SkuSequence = skuSequence,
                TotalCost = cost,
                BaselineCost = cost,
                ImprovementPercentage = 0.0f,
                IsOptimalProven = false,
                StatusMessage = isReachable
                    ? "Baseline route valid (original order sequence preserved)."
                    : "Baseline route contains unreachable node pairs."
            };
        }

        public static float CalculateSequenceCost(TravelCostMatrix matrix, IList<int> visitOrder)
        {
            if (matrix == null || visitOrder == null || visitOrder.Count < 2) return float.PositiveInfinity;

            float total = 0f;
            for (int i = 0; i < visitOrder.Count - 1; i++)
            {
                float legCost = matrix.GetCost(visitOrder[i], visitOrder[i + 1]);
                if (float.IsPositiveInfinity(legCost) || float.IsNaN(legCost) || legCost < 0f)
                {
                    return float.PositiveInfinity;
                }
                total += legCost;
            }
            return total;
        }
    }
}
