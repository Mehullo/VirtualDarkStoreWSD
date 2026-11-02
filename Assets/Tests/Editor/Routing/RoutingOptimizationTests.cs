using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Management;
using VirtualDarkStore.Routing;

namespace VirtualDarkStore.Tests.Editor.Routing
{
    [TestFixture]
    public class RoutingOptimizationTests
    {
        private GameObject storeGo;
        private StoreManager store;

        [SetUp]
        public void SetUp()
        {
            storeGo = new GameObject("TestStoreManager");
            store = storeGo.AddComponent<StoreManager>();
            store.InitializeStore(StoreConfiguration.CreateQuickCommerce6AisleConfig());
        }

        [TearDown]
        public void TearDown()
        {
            if (storeGo != null) UnityEngine.Object.DestroyImmediate(storeGo);
        }

        [Test]
        public void CostMatrix_CalculatesDistances_WithZeroDiagonal()
        {
            Vector3 start = new Vector3(0f, 0f, -8.5f);
            var stops = new List<Vector3>
            {
                new Vector3(-4.5f, 1f, -2f),
                new Vector3(1.5f, 1f, 4f),
                new Vector3(4.5f, 1f, -5f)
            };
            Vector3 end = new Vector3(2f, 0f, -10f);

            var matrix = TravelCostMatrix.Build(start, stops, end);

            Assert.IsNotNull(matrix);
            Assert.AreEqual(5, matrix.NodeCount); // start + 3 stops + end
            Assert.IsTrue(matrix.Validate(out string error), $"Matrix validation failed: {error}");

            // Verify diagonal is strictly zero
            for (int i = 0; i < matrix.NodeCount; i++)
            {
                Assert.AreEqual(0f, matrix.GetCost(i, i), 0.0001f, $"Diagonal at {i} must be zero.");
            }

            // Verify non-diagonal distances are positive
            for (int i = 0; i < matrix.NodeCount; i++)
            {
                for (int j = 0; j < matrix.NodeCount; j++)
                {
                    if (i != j)
                    {
                        float cost = matrix.GetCost(i, j);
                        Assert.Greater(cost, 0f, $"Cost from {i} to {j} must be positive.");
                        Assert.IsTrue(matrix.IsReachable(i, j));
                    }
                }
            }
        }

        [Test]
        public void CostMatrix_HandlesUnreachableNodes_Explicitly()
        {
            Vector3 start = Vector3.zero;
            var stops = new List<Vector3> { new Vector3(10f, 0f, 0f) };
            Vector3 end = new Vector3(20f, 0f, 0f);

            // Mock distance calculator with an unreachable node
            var mockCalc = new MockDistanceCalculator((from, to) =>
            {
                if (from.x == 10f || to.x == 10f) return null; // Unreachable node
                return Vector3.Distance(from, to);
            });

            var matrix = TravelCostMatrix.Build(start, stops, end, distanceCalc: mockCalc);

            Assert.AreEqual(3, matrix.NodeCount);
            Assert.IsFalse(matrix.IsReachable(0, 1), "Stop 1 should be explicitly unreachable from start.");
            Assert.IsTrue(float.IsPositiveInfinity(matrix.GetCost(0, 1)));
            Assert.IsTrue(matrix.IsReachable(0, 2), "Start to end should be reachable.");
            Assert.IsTrue(matrix.Validate(out _), "Matrix with positive infinity entries is structurally valid.");
        }

        [Test]
        public void Routing_ValidStartAndEnd()
        {
            Vector3 start = new Vector3(0f, 0f, -8f);
            var stops = new List<Vector3>
            {
                new Vector3(-2f, 0f, 2f),
                new Vector3(2f, 0f, 5f)
            };
            Vector3 end = new Vector3(0f, 0f, -10f);
            var skuIds = new List<string> { "SKU-A", "SKU-B" };

            var matrix = TravelCostMatrix.Build(start, stops, end);
            var optimizer = new OrToolsRouteOptimizer();
            var result = optimizer.OptimizeRoute(matrix, skuIds);

            Assert.IsTrue(result.Success, result.StatusMessage);
            Assert.AreEqual(4, result.VisitOrder.Count);
            Assert.AreEqual(0, result.VisitOrder[0], "Route must start at Node 0.");
            Assert.AreEqual(3, result.VisitOrder[result.VisitOrder.Count - 1], "Route must end at Node K+1 (Packing Bench).");
        }

        [Test]
        public void Routing_EveryRequiredSkuExactlyOnce()
        {
            Vector3 start = new Vector3(0f, 0f, -8f);
            var stops = new List<Vector3>
            {
                new Vector3(-4.5f, 0f, 0f),
                new Vector3(-1.5f, 0f, 2f),
                new Vector3(1.5f, 0f, -2f),
                new Vector3(4.5f, 0f, 5f)
            };
            Vector3 end = new Vector3(2f, 0f, -10f);
            var skuIds = new List<string> { "SKU-1", "SKU-2", "SKU-3", "SKU-4" };

            var matrix = TravelCostMatrix.Build(start, stops, end);
            var optimizer = new OrToolsRouteOptimizer();
            var result = optimizer.OptimizeRoute(matrix, skuIds);

            Assert.IsTrue(result.Success, result.StatusMessage);
            Assert.AreEqual(4, result.SkuSequence.Count);

            var visitedSkus = new HashSet<string>(result.SkuSequence);
            Assert.AreEqual(4, visitedSkus.Count, "Every required SKU must appear exactly once (no duplicates).");

            foreach (var sku in skuIds)
            {
                Assert.IsTrue(visitedSkus.Contains(sku), $"Required SKU '{sku}' must appear in the optimized route.");
            }
        }

        [Test]
        public void Routing_NoUnexpectedSku()
        {
            Vector3 start = Vector3.zero;
            var stops = new List<Vector3>
            {
                new Vector3(0f, 0f, 5f),
                new Vector3(0f, 0f, 10f)
            };
            Vector3 end = new Vector3(0f, 0f, 15f);
            var skuIds = new List<string> { "ALPHA", "BETA" };

            var matrix = TravelCostMatrix.Build(start, stops, end);
            var optimizer = new OrToolsRouteOptimizer();
            var result = optimizer.OptimizeRoute(matrix, skuIds);

            foreach (var node in result.VisitOrder)
            {
                Assert.IsTrue(node >= 0 && node < matrix.NodeCount, $"Unexpected node index {node} in route.");
            }

            foreach (var sku in result.SkuSequence)
            {
                Assert.IsTrue(skuIds.Contains(sku), $"Unexpected SKU '{sku}' in route.");
            }
        }

        [Test]
        public void Routing_RouteEndsAtPacking()
        {
            Vector3 start = new Vector3(0f, 0f, -8.5f);
            var stops = new List<Vector3> { new Vector3(1.5f, 1f, 3f) };
            Vector3 end = new Vector3(2f, 0f, -10f); // PACK-01
            var skuIds = new List<string> { "SKU-0001" };

            var matrix = TravelCostMatrix.Build(start, stops, end);
            var optimizer = new OrToolsRouteOptimizer();
            var result = optimizer.OptimizeRoute(matrix, skuIds);

            Assert.IsTrue(result.Success);
            int lastNode = result.VisitOrder[result.VisitOrder.Count - 1];
            Assert.AreEqual(matrix.NodeCount - 1, lastNode);
            Assert.AreEqual(end, matrix.NodePositions[lastNode], "Last node must match packing station position.");
        }

        [Test]
        public void Routing_BaselineSequenceRemainsAvailable()
        {
            Vector3 start = new Vector3(0f, 0f, -8.5f);
            var stops = new List<Vector3>
            {
                new Vector3(10f, 0f, 0f),
                new Vector3(5f, 0f, 0f)
            };
            Vector3 end = new Vector3(2f, 0f, -10f);
            var skuIds = new List<string> { "FIRST", "SECOND" };

            var matrix = TravelCostMatrix.Build(start, stops, end);
            var baseline = new BaselineRouteOptimizer();
            var result = baseline.OptimizeRoute(matrix, skuIds);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.VisitOrder[0]);
            Assert.AreEqual(1, result.VisitOrder[1]);
            Assert.AreEqual(2, result.VisitOrder[2]);
            Assert.AreEqual(3, result.VisitOrder[3]);
            Assert.AreEqual("FIRST", result.SkuSequence[0]);
            Assert.AreEqual("SECOND", result.SkuSequence[1]);
        }

        [Test]
        public void Routing_SolverFailure_FallsBackSafely()
        {
            Vector3 start = Vector3.zero;
            var stops = new List<Vector3>
            {
                new Vector3(10f, 0f, 0f),
                new Vector3(20f, 0f, 0f)
            };
            Vector3 end = new Vector3(30f, 0f, 0f);
            var skuIds = new List<string> { "SKU-1", "SKU-2" };

            // Mock distance calculator returning unreachable for all edges
            var unreachableCalc = new MockDistanceCalculator((from, to) => null);
            var matrix = TravelCostMatrix.Build(start, stops, end, distanceCalc: unreachableCalc);

            var optimizer = new OrToolsRouteOptimizer();
            var result = optimizer.OptimizeRoute(matrix, skuIds);

            Assert.IsNotNull(result);
            // Should not throw; falls back to baseline sequence
            Assert.AreEqual(4, result.VisitOrder.Count);
            Assert.AreEqual(0, result.VisitOrder[0]);
            Assert.AreEqual(3, result.VisitOrder[3]);
            StringAssert.Contains("baseline", result.StatusMessage.ToLower());
        }

        [Test]
        public void OrTools_OptimizesVisitSequence_WhenOutOfOrder()
        {
            // Linear corridor scenario where original sequence backtracks:
            // Start: 0
            // Stop 1: 30 (far)
            // Stop 2: 20 (middle)
            // Stop 3: 10 (near)
            // End: 35
            // Baseline: 0 -> 30 -> 20 -> 10 -> 35 = 30 + 10 + 10 + 25 = 75m
            // Optimal:  0 -> 10 -> 20 -> 30 -> 35 = 10 + 10 + 10 + 5  = 35m

            Vector3 start = new Vector3(0f, 0f, 0f);
            var stops = new List<Vector3>
            {
                new Vector3(0f, 0f, 30f),
                new Vector3(0f, 0f, 20f),
                new Vector3(0f, 0f, 10f)
            };
            Vector3 end = new Vector3(0f, 0f, 35f);
            var skuIds = new List<string> { "SKU-FAR", "SKU-MID", "SKU-NEAR" };

            var matrix = TravelCostMatrix.Build(start, stops, end);
            var optimizer = new OrToolsRouteOptimizer();
            var result = optimizer.OptimizeRoute(matrix, skuIds);

            Assert.IsTrue(result.Success, result.StatusMessage);
            Assert.AreEqual(75f, result.BaselineCost, 0.1f, "Baseline cost calculation check.");
            Assert.AreEqual(35f, result.TotalCost, 0.1f, "OR-Tools should discover the forward sweep optimal path.");
            Assert.Greater(result.ImprovementPercentage, 50f, "Travel reduction should be > 50%.");

            // Optimal visit order: Node 0 -> Node 3 (10m) -> Node 2 (20m) -> Node 1 (30m) -> Node 4 (35m)
            Assert.AreEqual(0, result.VisitOrder[0]);
            Assert.AreEqual(3, result.VisitOrder[1]);
            Assert.AreEqual(2, result.VisitOrder[2]);
            Assert.AreEqual(1, result.VisitOrder[3]);
            Assert.AreEqual(4, result.VisitOrder[4]);

            // Optimal SKU sequence
            Assert.AreEqual("SKU-NEAR", result.SkuSequence[0]);
            Assert.AreEqual("SKU-MID", result.SkuSequence[1]);
            Assert.AreEqual("SKU-FAR", result.SkuSequence[2]);
        }
    }
}
