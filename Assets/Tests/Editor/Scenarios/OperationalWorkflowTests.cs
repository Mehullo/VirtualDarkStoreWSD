using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Management;
using VirtualDarkStore.Routing;
using VirtualDarkStore.Scenarios;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Tests.Editor.Scenarios
{
    [TestFixture]
    public class OperationalWorkflowTests
    {
        private GameObject storeGo;
        private StoreManager store;
        private GameObject orderMgrGo;
        private OrderManager orderManager;
        private GameObject workStudyGo;
        private WorkEventLogger logger;
        private ActivityClassifier classifier;
        private OperationalCycleManager opManager;
        private GameObject stationGo;
        private PackingStation packingStation;

        [SetUp]
        public void SetUp()
        {
            storeGo = new GameObject("TestStoreManager");
            store = storeGo.AddComponent<StoreManager>();
            store.InitializeStore(StoreConfiguration.CreateQuickCommerce6AisleConfig());

            orderMgrGo = new GameObject("TestOrderManager");
            orderManager = orderMgrGo.AddComponent<OrderManager>();

            workStudyGo = new GameObject("TestWorkStudyManager");
            logger = workStudyGo.AddComponent<WorkEventLogger>();
            classifier = workStudyGo.AddComponent<ActivityClassifier>();
            opManager = workStudyGo.AddComponent<OperationalCycleManager>();

            stationGo = new GameObject("PackingStation_01");
            stationGo.transform.position = new Vector3(2.0f, 0f, -10.0f);
            var bench = new GameObject("PackingBench").transform;
            bench.SetParent(stationGo.transform);
            var drop = new GameObject("DropPad").transform;
            drop.SetParent(stationGo.transform);
            var spawn = new GameObject("SpawnPoint").transform;
            spawn.SetParent(stationGo.transform);
            packingStation = stationGo.AddComponent<PackingStation>();
            packingStation.Initialize("PACK-01", "Dispatch Packing Bench 1", bench, drop, spawn);

            classifier.SetReferences(logger, orderManager);
            logger.Clear();
            classifier.ResetState(0f, Vector3.zero);
        }

        [TearDown]
        public void TearDown()
        {
            if (opManager != null) opManager.UnsubscribeOrderEvents();
            if (classifier != null) classifier.UnsubscribeOrderEvents();
            if (stationGo != null) UnityEngine.Object.DestroyImmediate(stationGo);
            if (workStudyGo != null) UnityEngine.Object.DestroyImmediate(workStudyGo);
            if (orderMgrGo != null) UnityEngine.Object.DestroyImmediate(orderMgrGo);
            if (storeGo != null) UnityEngine.Object.DestroyImmediate(storeGo);
        }

        [Test]
        public void OperationalStage_FullCycleProgression_ReachesCompleted()
        {
            var config = new OrderGenerationConfig(seed: 42001, itemCount: 3);
            var recordedStages = new List<OperationalStage>();

            opManager.OnStageChanged += stage => recordedStages.Add(stage);

            // 1. Entry
            opManager.StartCycle(config, useOptimized: false);
            Assert.AreEqual(OperationalStage.CollectOrder, opManager.CurrentStage);

            // 2. Collect Order at Desk
            bool collected = opManager.CollectOrder(out Order order, out string collectError);
            Assert.IsTrue(collected, collectError);
            Assert.IsNotNull(order);
            Assert.AreEqual(OperationalStage.Picking, opManager.CurrentStage);

            // 3. Picking (Pick all items in sequence)
            for (int i = 0; i < order.TotalItemCount; i++)
            {
                var target = order.CurrentTargetItem;
                Assert.IsNotNull(target);
                var pickRes = orderManager.AttemptPick(target.SkuId);
                Assert.AreEqual(PickResult.Success, pickRes);
            }

            // Order fulfilled -> stage transitions to AwaitingPacking
            Assert.AreEqual(OperationalStage.AwaitingPacking, opManager.CurrentStage);

            // 4. Deposit Order at Packing Desk
            bool deposited = opManager.DepositOrder(out string depositError);
            Assert.IsTrue(deposited, depositError);
            Assert.AreEqual(OperationalStage.WalkToExit, opManager.CurrentStage);

            // 5. Reach Exit Zone
            opManager.CompleteCycle();
            Assert.AreEqual(OperationalStage.Completed, opManager.CurrentStage);

            // Verify stage progression
            Assert.IsTrue(recordedStages.Contains(OperationalStage.CollectOrder));
            Assert.IsTrue(recordedStages.Contains(OperationalStage.Picking));
            Assert.IsTrue(recordedStages.Contains(OperationalStage.AwaitingPacking));
            Assert.IsTrue(recordedStages.Contains(OperationalStage.WalkToExit));
            Assert.IsTrue(recordedStages.Contains(OperationalStage.Completed));
        }

        [Test]
        public void OperationalEvents_RecordedMonotonically_WithZeroNegativeDurations()
        {
            var config = new OrderGenerationConfig(seed: 55555, itemCount: 2);
            opManager.StartCycle(config);
            opManager.CollectOrder(out var order, out _);

            while (order.Status == OrderStatus.InProgress)
            {
                orderManager.AttemptPick(order.CurrentTargetItem.SkuId);
            }

            opManager.DepositOrder(out _);
            opManager.CompleteCycle();

            var events = logger.LoggedEvents;
            Assert.Greater(events.Count, 0, "Events must be recorded during the cycle.");

            // Verify monotonic ordering and non-negative durations
            for (int i = 0; i < events.Count; i++)
            {
                Assert.GreaterOrEqual(events[i].Duration, 0f, $"Event {events[i].Id} duration cannot be negative.");
                Assert.GreaterOrEqual(events[i].EndTime, events[i].StartTime);

                if (i > 0)
                {
                    Assert.GreaterOrEqual(events[i].StartTime, events[i - 1].StartTime, "Events must have monotonic start timestamps.");
                }
            }

            // Verify entry and exit locations are recorded
            Assert.AreEqual("ENTRY", events[0].LocationName);
            Assert.AreEqual("EXIT", events[events.Count - 1].LocationName);
        }

        [Test]
        public void AisleTransitionsAndBacktracking_CalculatesAccurately()
        {
            // Case 1: Backtracking sequence [Aisle 1 -> Aisle 3 -> Aisle 1]
            var backtrackingSlots = new List<string>
            {
                "A01-L-B01-T01-S01",
                "A03-R-B02-T01-S01",
                "A01-R-B03-T01-S01"
            };

            OperationalCycleManager.CalculateAisleMetrics(backtrackingSlots, out int trans1, out int back1);
            Assert.AreEqual(2, trans1, "Should have 2 aisle transitions (1->3, 3->1).");
            Assert.AreEqual(1, back1, "Should detect 1 aisle backtrack returning to Aisle 1.");

            // Case 2: S-shape / sweep sequence [Aisle 1 -> Aisle 1 -> Aisle 2 -> Aisle 4]
            var sweepSlots = new List<string>
            {
                "A01-L-B01-T01-S01",
                "A01-R-B02-T01-S01",
                "A02-L-B01-T01-S01",
                "A04-R-B03-T01-S01"
            };

            OperationalCycleManager.CalculateAisleMetrics(sweepSlots, out int trans2, out int back2);
            Assert.AreEqual(2, trans2, "Consecutive picks in Aisle 1 collapse; 1->2 and 2->4 = 2 transitions.");
            Assert.AreEqual(0, back2, "Monotonic forward sweep should have zero backtracks.");
        }

        [Test]
        public void OperationalCycleRunner_ExecutesEndToEndWorkflow_Deterministically()
        {
            var skuSequence = new List<string> { "SKU-0001", "SKU-0008", "SKU-0039" };

            var runResult = OperationalCycleRunner.RunCycle(
                skuSequence: skuSequence,
                orderId: "ORD-TEST-001",
                isOptimized: false,
                algorithmicCost: 45.0f,
                orderManager: orderManager,
                logger: logger,
                classifier: classifier,
                walkSpeed: 1.4f,
                searchSeconds: 1.0f,
                packingSeconds: 1.5f);

            Assert.IsNotNull(runResult);
            Assert.IsTrue(runResult.orderCompletedSuccessfully);
            Assert.Greater(runResult.totalCycleDuration, 5.0f);
            Assert.Greater(runResult.measuredPlayerDistance, 10.0f);
            Assert.AreEqual(3, runResult.Results.TotalPicks);
            Assert.IsTrue(runResult.Results.VerifyReconciliation(out string reconError), reconError);

            // Verify waypoint sequence
            Assert.AreEqual("ENTRY", runResult.waypointLabels[0]);
            Assert.AreEqual("COLLECT_ORDER", runResult.waypointLabels[1]);
            Assert.AreEqual("DEPOSIT_PACKING", runResult.waypointLabels[runResult.waypointLabels.Count - 2]);
            Assert.AreEqual("EXIT", runResult.waypointLabels[runResult.waypointLabels.Count - 1]);
        }

        [Test]
        public void BaselineVsOptimized_SameOrder_IdenticalStartingConditions_VariesOnlyPickSequence()
        {
            // Backtracking baseline order: Aisle 1 -> Aisle 3 -> Aisle 1 -> Aisle 4
            // SKU-0001 (Aisle 1), SKU-0039 (Aisle 3), SKU-0008 (Aisle 1), SKU-0066 (Aisle 4)
            var baselineSkus = new List<string> { "SKU-0001", "SKU-0039", "SKU-0008", "SKU-0066" };

            // Run Baseline (Run A)
            var runA = OperationalCycleRunner.RunCycle(
                skuSequence: baselineSkus,
                orderId: "ORD-STUDY-001",
                isOptimized: false,
                algorithmicCost: 65.2f,
                orderManager: orderManager,
                logger: logger,
                classifier: classifier);

            // Optimize via OR-Tools for Run B
            var matrix = TravelCostMatrix.Build(
                new Vector3(-4.0f, 0f, -8.8f),
                new[] {
                    store.GetSlot("A01-L-B01-T01-S01").WorldPosition,
                    store.GetSlot("A03-L-B02-T01-S01").WorldPosition,
                    store.GetSlot("A01-R-B02-T01-S01").WorldPosition,
                    store.GetSlot("A04-L-B03-T01-S01").WorldPosition
                },
                new Vector3(2.0f, 0f, -8.8f));

            var optimizer = new OrToolsRouteOptimizer();
            var optResult = optimizer.OptimizeRoute(matrix, baselineSkus);

            Assert.IsTrue(optResult.Success);
            var optimizedSkus = optResult.SkuSequence;

            // Run Optimized (Run B)
            var runB = OperationalCycleRunner.RunCycle(
                skuSequence: optimizedSkus,
                orderId: "ORD-STUDY-001",
                isOptimized: true,
                algorithmicCost: optResult.TotalCost,
                orderManager: orderManager,
                logger: logger,
                classifier: classifier);

            // Compare
            var comparison = new OperationalComparison(runA, runB);
            Assert.IsTrue(comparison.IsEquivalent, "Both runs must have identical conditions.");

            // Verify metrics
            Assert.AreEqual(runA.Results.TotalPicks, runB.Results.TotalPicks);
            Assert.AreEqual(runA.Results.ShelfVisits, runB.Results.ShelfVisits);

            // Optimized route groups Aisle 1 items together -> reduces distance and eliminates backtrack!
            Assert.Less(runB.measuredPlayerDistance, runA.measuredPlayerDistance, "Optimized route must reduce travel distance.");
            Assert.Less(runB.totalCycleDuration, runA.totalCycleDuration, "Optimized route must reduce total cycle time.");
            Assert.LessOrEqual(runB.aisleBacktracks, runA.aisleBacktracks, "Optimized route must not increase aisle backtracking.");

            // Export evidence bundle
            string evidenceDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "evidence", "phase14"));
            comparison.ExportReportBundle(evidenceDir, "baseline_vs_optimized_report");

            Assert.IsTrue(File.Exists(Path.Combine(evidenceDir, "baseline_vs_optimized_report.md")));
            Assert.IsTrue(File.Exists(Path.Combine(evidenceDir, "baseline_vs_optimized_report.csv")));
        }
    }
}
