using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Management;
using VirtualDarkStore.Scenarios;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Tests.Editor.Domain
{
    [TestFixture]
    public class MultiOrderSimulationTests
    {
        private GameObject storeGo;
        private StoreManager store;
        private GameObject orderMgrGo;
        private OrderManager orderManager;
        private GameObject loggerGo;
        private WorkEventLogger logger;
        private GameObject classifierGo;
        private ActivityClassifier classifier;

        [SetUp]
        public void SetUp()
        {
            storeGo = new GameObject("TestStoreManager");
            store = storeGo.AddComponent<StoreManager>();
            store.InitializeStore(StoreConfiguration.CreateQuickCommerce6AisleConfig());

            orderMgrGo = new GameObject("TestOrderManager");
            orderManager = orderMgrGo.AddComponent<OrderManager>();

            loggerGo = new GameObject("TestWorkEventLogger");
            logger = loggerGo.AddComponent<WorkEventLogger>();

            classifierGo = new GameObject("TestActivityClassifier");
            classifier = classifierGo.AddComponent<ActivityClassifier>();
            classifier.SetReferences(logger, orderManager);
        }

        [TearDown]
        public void TearDown()
        {
            if (classifierGo != null) UnityEngine.Object.DestroyImmediate(classifierGo);
            if (loggerGo != null) UnityEngine.Object.DestroyImmediate(loggerGo);
            if (orderMgrGo != null) UnityEngine.Object.DestroyImmediate(orderMgrGo);
            if (storeGo != null) UnityEngine.Object.DestroyImmediate(storeGo);
        }

        [Test]
        public void Case1_SingleOrderWithThreeItems_GeneratesValidOrder()
        {
            var config = new OrderGenerationConfig(seed: 1001, itemCount: 3, orderCount: 1, DuplicateSkuPolicy.DisallowDuplicates);
            Assert.IsTrue(config.Validate(store, out string error), $"Validation failed: {error}");

            List<Order> orders = OrderGenerator.GenerateBatch(store, config);
            Assert.IsNotNull(orders);
            Assert.AreEqual(1, orders.Count, "Expected exactly 1 order.");

            Order order = orders[0];
            Assert.AreEqual(3, order.TotalItemCount, "Expected exactly 3 items in order.");

            var seenSkus = new HashSet<string>();
            foreach (var item in order.Items)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(item.SkuId));
                Assert.IsTrue(seenSkus.Add(item.SkuId), $"Duplicate SKU '{item.SkuId}' detected.");
                Assert.IsNotNull(store.GetSku(item.SkuId), $"SKU '{item.SkuId}' must exist in store catalog.");
                Assert.AreNotEqual("UNASSIGNED", item.TargetSlotId);
                Assert.AreNotEqual(Vector3.zero, item.TargetWorldPosition);
            }
        }

        [Test]
        public void Case2_FiveOrdersWithEightItems_GeneratesFiveValidOrders()
        {
            var config = new OrderGenerationConfig(seed: 1001, itemCount: 8, orderCount: 5, DuplicateSkuPolicy.DisallowDuplicates);
            Assert.IsTrue(config.Validate(store, out string error), $"Validation failed: {error}");

            List<Order> orders = OrderGenerator.GenerateBatch(store, config);
            Assert.IsNotNull(orders);
            Assert.AreEqual(5, orders.Count, "Expected exactly 5 independent orders.");

            int totalRequestedSelections = 5 * 8;
            int actualSelections = 0;

            for (int o = 0; o < orders.Count; o++)
            {
                var order = orders[o];
                Assert.AreEqual(8, order.TotalItemCount, $"Order {o + 1} must contain exactly 8 items.");
                actualSelections += order.TotalItemCount;

                var seenInOrder = new HashSet<string>();
                foreach (var item in order.Items)
                {
                    Assert.IsTrue(seenInOrder.Add(item.SkuId), $"Duplicate SKU '{item.SkuId}' within Order {order.OrderId}.");
                    Assert.IsNotNull(store.GetSku(item.SkuId), $"SKU '{item.SkuId}' must exist in store catalog.");
                    Assert.IsTrue(store.TryResolveSkuLocation(item.SkuId, out string locId, out Vector3 worldPos));
                    Assert.AreEqual(item.TargetSlotId, locId);
                    Assert.AreEqual(item.TargetWorldPosition, worldPos);
                }
            }

            Assert.AreEqual(totalRequestedSelections, actualSelections, "Expected 40 total requested item selections across 5 orders.");
        }

        [Test]
        public void Case3_RepeatGenerationWithSameSeed_ProducesIdenticalOrders()
        {
            var configA = new OrderGenerationConfig(seed: 1001, itemCount: 8, orderCount: 5, DuplicateSkuPolicy.DisallowDuplicates);
            var configB = new OrderGenerationConfig(seed: 1001, itemCount: 8, orderCount: 5, DuplicateSkuPolicy.DisallowDuplicates);

            List<Order> batchA = OrderGenerator.GenerateBatch(store, configA);
            List<Order> batchB = OrderGenerator.GenerateBatch(store, configB);

            Assert.AreEqual(batchA.Count, batchB.Count);

            for (int o = 0; o < batchA.Count; o++)
            {
                Assert.AreEqual(batchA[o].OrderId, batchB[o].OrderId, $"Order ID mismatch at order {o + 1}.");
                Assert.AreEqual(batchA[o].TotalItemCount, batchB[o].TotalItemCount);

                for (int i = 0; i < batchA[o].TotalItemCount; i++)
                {
                    Assert.AreEqual(batchA[o].Items[i].SkuId, batchB[o].Items[i].SkuId, $"SKU mismatch at order {o + 1}, item {i + 1}.");
                    Assert.AreEqual(batchA[o].Items[i].TargetSlotId, batchB[o].Items[i].TargetSlotId);
                    Assert.AreEqual(batchA[o].Items[i].TargetWorldPosition, batchB[o].Items[i].TargetWorldPosition);
                }
            }
        }

        [Test]
        public void Case4_ChangingSeed_ProducesDifferentRandomization()
        {
            var configA = new OrderGenerationConfig(seed: 1001, itemCount: 8, orderCount: 5, DuplicateSkuPolicy.DisallowDuplicates);
            var configB = new OrderGenerationConfig(seed: 2026, itemCount: 8, orderCount: 5, DuplicateSkuPolicy.DisallowDuplicates);

            List<Order> batchA = OrderGenerator.GenerateBatch(store, configA);
            List<Order> batchB = OrderGenerator.GenerateBatch(store, configB);

            Assert.AreEqual(batchA.Count, batchB.Count);

            bool foundDifference = false;
            for (int o = 0; o < batchA.Count; o++)
            {
                if (batchA[o].OrderId != batchB[o].OrderId) foundDifference = true;
                for (int i = 0; i < batchA[o].TotalItemCount; i++)
                {
                    if (batchA[o].Items[i].SkuId != batchB[o].Items[i].SkuId)
                    {
                        foundDifference = true;
                        break;
                    }
                }
                if (foundDifference) break;
            }

            Assert.IsTrue(foundDifference, "Different seeds must produce different SKU item sequences.");
        }

        [Test]
        public void Case5_InvalidInputs_ProduceClearValidationFailuresWithoutCrashing()
        {
            int storeCapacity = store.RegisteredSkuCount; // 144 SKUs

            // 1. Zero orders
            var zeroOrdersConfig = new OrderGenerationConfig(seed: 1001, itemCount: 5, orderCount: 0);
            Assert.IsFalse(zeroOrdersConfig.Validate(store, out string err1));
            StringAssert.Contains("Number of Orders must be at least 1", err1);

            // 2. Negative orders
            var negOrdersConfig = new OrderGenerationConfig(seed: 1001, itemCount: 5, orderCount: -3);
            Assert.IsFalse(negOrdersConfig.Validate(store, out string err2));
            StringAssert.Contains("Number of Orders must be at least 1", err2);

            // 3. Zero items per order
            var zeroItemsConfig = new OrderGenerationConfig(seed: 1001, itemCount: 0, orderCount: 3);
            Assert.IsFalse(zeroItemsConfig.Validate(store, out string err3));
            StringAssert.Contains("Items per Order must be at least 1", err3);

            // 4. Negative items per order
            var negItemsConfig = new OrderGenerationConfig(seed: 1001, itemCount: -5, orderCount: 3);
            Assert.IsFalse(negItemsConfig.Validate(store, out string err4));
            StringAssert.Contains("Items per Order must be at least 1", err4);

            // 5. Items exceeding unique catalog capacity (e.g. 200 items when catalog has 144)
            var excessItemsConfig = new OrderGenerationConfig(seed: 1001, itemCount: storeCapacity + 50, orderCount: 1, DuplicateSkuPolicy.DisallowDuplicates);
            Assert.IsFalse(excessItemsConfig.Validate(store, out string err5));
            StringAssert.Contains("exceeds available valid SKU catalog capacity", err5);
            StringAssert.Contains(storeCapacity.ToString(), err5);

            // 6. Attempting to start multi-order simulation with invalid config fails safely
            bool started = orderManager.TryStartMultiOrderSimulation(excessItemsConfig, out MultiOrderSimulation sim, out string failReason);
            Assert.IsFalse(started);
            Assert.IsNull(sim);
            StringAssert.Contains("exceeds available valid SKU catalog capacity", failReason);
        }

        [Test]
        public void Case6_MultiOrderSimulation_ExecutesSequentiallyAndPreservesWorkStudyMetrics()
        {
            var config = new OrderGenerationConfig(seed: 1001, itemCount: 3, orderCount: 3, DuplicateSkuPolicy.DisallowDuplicates);

            // Start the multi-order simulation
            bool started = orderManager.TryStartMultiOrderSimulation(config, out MultiOrderSimulation sim, out string error);
            Assert.IsTrue(started, $"Simulation start failed: {error}");
            Assert.IsNotNull(sim);
            Assert.AreEqual(3, sim.TotalOrdersCount);
            Assert.AreEqual(9, sim.TotalRequestedItemSelections);
            Assert.IsTrue(orderManager.HasActiveSimulation);

            // Execute each order in sequence
            for (int o = 0; o < 3; o++)
            {
                Assert.AreEqual(o, sim.CurrentOrderIndex, $"Expected active order index {o}.");
                Order activeOrder = orderManager.ActiveOrder;
                Assert.IsNotNull(activeOrder);
                Assert.AreEqual(OrderStatus.InProgress, activeOrder.Status);
                Assert.AreEqual(3, activeOrder.TotalItemCount);

                // Pick all items in order
                while (!activeOrder.IsAllItemsPicked)
                {
                    string targetSku = activeOrder.CurrentTargetItem.SkuId;
                    PickResult pickRes = orderManager.AttemptPick(targetSku);
                    Assert.AreEqual(PickResult.Success, pickRes);
                }

                Assert.AreEqual(OrderStatus.AwaitingPacking, activeOrder.Status);

                // Complete packing at bench
                bool packed = orderManager.CompletePacking(out string packErr);
                Assert.IsTrue(packed, $"Packing failed: {packErr}");
                Assert.AreEqual(OrderStatus.Completed, activeOrder.Status);
            }

            // After 3rd order completes, simulation should be complete
            Assert.AreEqual(3, sim.CompletedOrdersCount);
            Assert.IsTrue(sim.IsComplete);
        }

        [Test]
        public void MultiOrderSimulationRunner_ExecutesBatchDeterministically_WithAggregateWorkStudy()
        {
            var config = new OrderGenerationConfig(seed: 4242, itemCount: 2, orderCount: 3, DuplicateSkuPolicy.DisallowDuplicates);

            MultiOrderSimulationResult result = OperationalCycleRunner.RunMultiOrderSimulation(
                store: store,
                config: config,
                orderManager: orderManager,
                logger: logger,
                classifier: classifier);

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.totalOrdersRequested);
            Assert.AreEqual(3, result.totalOrdersCompleted);
            Assert.AreEqual(2, result.itemsPerOrder);
            Assert.AreEqual(6, result.totalItemsRequested);
            Assert.AreEqual(6, result.totalItemsPicked);
            Assert.AreEqual(3, result.orderRuns.Count);

            // Verify individual order run integrity
            for (int i = 0; i < result.orderRuns.Count; i++)
            {
                var run = result.orderRuns[i];
                Assert.IsTrue(run.orderCompletedSuccessfully);
                Assert.AreEqual(2, run.skuSequence.Count);
                Assert.Greater(run.totalCycleDuration, 0f);
                Assert.Greater(run.measuredPlayerDistance, 0f);
                Assert.GreaterOrEqual(run.aisleTransitions, 0);
            }

            // Aggregate metrics
            Assert.Greater(result.totalSimulationDuration, 0f);
            Assert.Greater(result.totalTravelDistance, 0f);
            Assert.Greater(result.averageCycleDurationPerOrder, 0f);
            Assert.Greater(result.averageDistancePerOrder, 0f);
            Assert.GreaterOrEqual(result.allEvents.Count, 3 * 5); // At least 5 events per order

            // Aggregate Work-Study reconciliation
            var aggregateWs = result.AggregateWorkStudy;
            Assert.IsNotNull(aggregateWs);
            Assert.IsTrue(aggregateWs.VerifyReconciliation(out string reconErr), $"Aggregate reconciliation failed: {reconErr}");
            Assert.AreEqual(6, aggregateWs.TotalPicks);
        }
    }
}
