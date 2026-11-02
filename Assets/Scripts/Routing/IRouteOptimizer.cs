using System;
using System.Collections.Generic;

namespace VirtualDarkStore.Routing
{
    [Serializable]
    public class RouteOptimizationResult
    {
        public bool Success;
        public string SolverName;
        public List<int> VisitOrder; // 0, stop indices, K+1
        public List<string> SkuSequence; // Ordered SKUs corresponding to visited stops
        public float TotalCost;
        public float BaselineCost;
        public float ImprovementPercentage;
        public bool IsOptimalProven;
        public string StatusMessage;

        public RouteOptimizationResult()
        {
            VisitOrder = new List<int>();
            SkuSequence = new List<string>();
        }

        public override string ToString()
        {
            string orderStr = VisitOrder != null ? string.Join(" -> ", VisitOrder) : "None";
            return $"[{SolverName}] Success={Success}, Cost={TotalCost:F2}m (Baseline={BaselineCost:F2}m, Saved={ImprovementPercentage:F1}%), Route={orderStr}, Proven={IsOptimalProven}";
        }
    }

    /// <summary>
    /// Contract for pick-sequence routing optimizers (Multi-stop TSP).
    /// Dispatches start at Node 0, visit every SKU stop node (1..K) exactly once, and terminate at Node K+1.
    /// </summary>
    public interface IRouteOptimizer
    {
        string SolverName { get; }

        RouteOptimizationResult OptimizeRoute(TravelCostMatrix matrix, IReadOnlyList<string> skuIds);
    }
}
