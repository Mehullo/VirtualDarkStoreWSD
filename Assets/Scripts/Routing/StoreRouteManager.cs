using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.Domain;

namespace VirtualDarkStore.Routing
{
    /// <summary>
    /// Service for calculating the physical navigation cost matrix and optimizing order pick sequences.
    /// Bridges the Order domain with the travel-cost matrix and OR-Tools routing optimizer.
    /// </summary>
    public static class StoreRouteManager
    {
        public static readonly Vector3 DefaultStartPos = new Vector3(0f, 0f, -8.5f);
        public static readonly Vector3 DefaultPackingPos = new Vector3(2f, 0f, -10f);

        /// <summary>
        /// Builds the TravelCostMatrix and runs route optimization for a given Order.
        /// Preserves the baseline sequence and provides comparative metrics.
        /// </summary>
        public static RouteOptimizationResult OptimizeOrder(
            Order order,
            Vector3? startPosition = null,
            Vector3? packingPosition = null,
            IRouteOptimizer optimizer = null,
            INavPathDistanceCalculator distanceCalc = null)
        {
            if (order == null || order.Items == null || order.Items.Count == 0)
            {
                return new RouteOptimizationResult
                {
                    Success = false,
                    SolverName = "None",
                    StatusMessage = "No items in order to optimize."
                };
            }

            Vector3 start = startPosition ?? DefaultStartPos;
            Vector3 end = packingPosition ?? DefaultPackingPos;

            var stopPositions = new List<Vector3>(order.Items.Count);
            var skuIds = new List<string>(order.Items.Count);
            var stopLabels = new List<string>(order.Items.Count);

            for (int i = 0; i < order.Items.Count; i++)
            {
                var item = order.Items[i];
                stopPositions.Add(item.TargetWorldPosition);
                skuIds.Add(item.SkuId);
                stopLabels.Add($"{item.SkuId} ({item.TargetSlotId})");
            }

            var matrix = TravelCostMatrix.Build(start, stopPositions, end, stopLabels, distanceCalc);

            if (optimizer == null)
            {
                optimizer = new OrToolsRouteOptimizer();
            }

            return optimizer.OptimizeRoute(matrix, skuIds);
        }
    }
}
