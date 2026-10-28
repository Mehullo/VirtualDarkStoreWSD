using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Management;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Tests.WorkStudy
{
    [TestFixture]
    public class WorkStudyCalculatorTests
    {
        private GameObject testRoot;
        private StoreManager storeManager;
        private OrderManager orderManager;
        private WorkEventLogger eventLogger;
        private ActivityClassifier classifier;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("[WorkStudyCalcTestRoot]");

            var storeObj = new GameObject("StoreManager");
            storeObj.transform.SetParent(testRoot.transform);
            storeManager = storeObj.AddComponent<StoreManager>();
            storeManager.InitializeStore();

            var orderObj = new GameObject("OrderManager");
            orderObj.transform.SetParent(testRoot.transform);
            orderManager = orderObj.AddComponent<OrderManager>();

            var loggerObj = new GameObject("WorkEventLogger");
            loggerObj.transform.SetParent(testRoot.transform);
            eventLogger = loggerObj.AddComponent<WorkEventLogger>();

            var classifierObj = new GameObject("ActivityClassifier");
            classifierObj.transform.SetParent(testRoot.transform);
            classifier = classifierObj.AddComponent<ActivityClassifier>();
            classifier.SetReferences(eventLogger, orderManager);
            classifier.ResetState(0f, Vector3.zero);
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        [Test]
        public void EmptyEventsList_ProducesZeroResults_ReconcilesSuccessfully()
        {
            var emptyList = new List<WorkEvent>();
            var results = WorkStudyCalculator.Calculate(emptyList, 1.0f);

            Assert.AreEqual(0f, results.TotalTaskTime);
            Assert.AreEqual(0f, results.TotalDistance);
            Assert.AreEqual(0, results.TotalPicks);
            Assert.AreEqual(0, results.ShelfVisits);
            Assert.AreEqual(0, results.TotalEventsCount);
            Assert.AreEqual(0f, results.WalkingDuration);
            Assert.AreEqual(0f, results.SearchingDuration);
            Assert.AreEqual(0f, results.PickingDuration);
            Assert.AreEqual(0f, results.WaitingDuration);
            Assert.AreEqual(0f, results.PackingDuration);
            Assert.AreEqual(0f, results.OrderCompletedDuration);

            Assert.IsTrue(results.VerifyReconciliation(out string failureReason), failureReason);
        }

        [Test]
        public void DeterministicScenario_CalculatesAccurateMetrics_AndReconciles()
        {
            // Build deterministic scenario with exact known timings and distances
            var events = new List<WorkEvent>
            {
                // 1. Waiting at dispatch (2.0s)
                new WorkEvent("EVT-0001", ActivityType.Waiting, 0.0f, 2.0f, new Vector3(0f, 0f, -8f), "PACK-01", "", "ORD-0001", distance: 0f),

                // 2. Macro travel to Aisle 1 (5.0s, 10.0m)
                new WorkEvent("EVT-0002", ActivityType.MovingBetweenLocations, 2.0f, 7.0f, new Vector3(-6f, 0f, -2f), "GENERAL_FLOOR", "SKU-0001", "ORD-0001", distance: 10.0f),

                // 3. Final approach to shelf bay (2.0s, 2.0m)
                new WorkEvent("EVT-0003", ActivityType.Approaching, 7.0f, 9.0f, new Vector3(-7f, 0f, -1.7f), "A01-L-B01-T01-S01", "SKU-0001", "ORD-0001", distance: 2.0f),

                // 4. Searching shelf structure (3.0s)
                new WorkEvent("EVT-0004", ActivityType.Searching, 9.0f, 12.0f, new Vector3(-7f, 0f, -1.7f), "A01-L-B01-T01-S01", "SKU-0001", "ORD-0001", distance: 0f),

                // 5. Picking SKU-0001 (0.5s)
                new WorkEvent("EVT-0005", ActivityType.Picking, 12.0f, 12.5f, new Vector3(-7f, 0f, -1.7f), "A01-L-B01-T01-S01", "SKU-0001", "ORD-0001", distance: 0f),

                // 6. Macro travel to Bay 2 (4.0s, 8.0m)
                new WorkEvent("EVT-0006", ActivityType.MovingBetweenLocations, 12.5f, 16.5f, new Vector3(-6f, 0f, 2f), "GENERAL_FLOOR", "SKU-0012", "ORD-0001", distance: 8.0f),

                // 7. Approach Bay 2 (1.5s, 1.5m)
                new WorkEvent("EVT-0007", ActivityType.Approaching, 16.5f, 18.0f, new Vector3(-7f, 0f, 2.3f), "A01-L-B02-T02-S03", "SKU-0012", "ORD-0001", distance: 1.5f),

                // 8. Searching Bay 2 (2.0s)
                new WorkEvent("EVT-0008", ActivityType.Searching, 18.0f, 20.0f, new Vector3(-7f, 0f, 2.3f), "A01-L-B02-T02-S03", "SKU-0012", "ORD-0001", distance: 0f),

                // 9. Picking SKU-0012 (0.5s)
                new WorkEvent("EVT-0009", ActivityType.Picking, 20.0f, 20.5f, new Vector3(-7f, 0f, 2.3f), "A01-L-B02-T02-S03", "SKU-0012", "ORD-0001", distance: 0f),

                // 10. Travel to packing bench (6.0s, 12.0m)
                new WorkEvent("EVT-0010", ActivityType.MovingBetweenLocations, 20.5f, 26.5f, new Vector3(0f, 0f, -8f), "GENERAL_FLOOR", "", "ORD-0001", distance: 12.0f),

                // 11. Packing order at bench (3.0s)
                new WorkEvent("EVT-0011", ActivityType.Packing, 26.5f, 29.5f, new Vector3(0f, 0f, -9.5f), "PACK-01", "", "ORD-0001", distance: 0f),

                // 12. Finalize completion (0.5s)
                new WorkEvent("EVT-0012", ActivityType.OrderCompleted, 29.5f, 30.0f, new Vector3(0f, 0f, -9.5f), "PACK-01", "", "ORD-0001", distance: 0f)
            };

            var results = WorkStudyCalculator.Calculate(events, samplingIntervalSeconds: 1.0f);

            // 1. Verify exact counts
            Assert.AreEqual(12, results.TotalEventsCount);
            Assert.AreEqual(2, results.TotalPicks, "Expected exactly 2 picks.");
            Assert.AreEqual(2, results.ShelfVisits, "Expected exactly 2 distinct shelf visits.");

            // 2. Verify total task time and distance
            Assert.AreEqual(30.0f, results.TotalTaskTime, 0.001f);
            Assert.AreEqual(33.5f, results.TotalDistance, 0.001f);

            // 3. Verify category durations
            // Walking: 5.0 (moving) + 2.0 (approach) + 4.0 (moving) + 1.5 (approach) + 6.0 (moving) = 18.5s
            Assert.AreEqual(18.5f, results.WalkingDuration, 0.001f);
            Assert.AreEqual(15.0f, results.MovingBetweenLocationsDuration, 0.001f);
            Assert.AreEqual(3.5f, results.ApproachingDuration, 0.001f);
            Assert.AreEqual(0.0f, results.GeneralWalkingDuration, 0.001f);

            // Searching: 3.0 + 2.0 = 5.0s
            Assert.AreEqual(5.0f, results.SearchingDuration, 0.001f);

            // Picking: 0.5 + 0.5 = 1.0s
            Assert.AreEqual(1.0f, results.PickingDuration, 0.001f);

            // Waiting: 2.0s
            Assert.AreEqual(2.0f, results.WaitingDuration, 0.001f);

            // Packing: 3.0s
            Assert.AreEqual(3.0f, results.PackingDuration, 0.001f);

            // Order Completed: 0.5s
            Assert.AreEqual(0.5f, results.OrderCompletedDuration, 0.001f);

            // 4. Verify category percentages
            Assert.AreEqual((18.5f / 30.0f) * 100f, results.WalkingPercentage, 0.01f);
            Assert.AreEqual((5.0f / 30.0f) * 100f, results.SearchingPercentage, 0.01f);
            Assert.AreEqual((1.0f / 30.0f) * 100f, results.PickingPercentage, 0.01f);
            Assert.AreEqual((2.0f / 30.0f) * 100f, results.WaitingPercentage, 0.01f);
            Assert.AreEqual((3.0f / 30.0f) * 100f, results.PackingPercentage, 0.01f);
            Assert.AreEqual((0.5f / 30.0f) * 100f, results.OrderCompletedPercentage, 0.01f);

            // 5. Verify mathematical reconciliation
            Assert.IsTrue(results.VerifyReconciliation(out string failureReason), failureReason);

            float sumDurations = results.WalkingDuration + results.SearchingDuration + results.PickingDuration +
                                 results.WaitingDuration + results.PackingDuration + results.OrderCompletedDuration;
            Assert.AreEqual(results.TotalTaskTime, sumDurations, 0.001f, "Category durations sum must equal TotalTaskTime.");

            float sumPercentages = results.WalkingPercentage + results.SearchingPercentage + results.PickingPercentage +
                                   results.WaitingPercentage + results.PackingPercentage + results.OrderCompletedPercentage;
            Assert.AreEqual(100.0f, sumPercentages, 0.02f, "Category percentages sum must equal 100.0%.");

            Debug.Log(results.GetSummaryReport());
        }

        [Test]
        public void WorkSampling_ConfigurableInterval_MatchesDiscreteObservationRule()
        {
            // Timeline:
            // 0.0 - 4.0: Walking (4.0s)
            // 4.0 - 6.0: Searching (2.0s)
            // 6.0 - 8.0: Picking (2.0s)
            // 8.0 - 10.0: Waiting (2.0s)
            var events = new List<WorkEvent>
            {
                new WorkEvent("EVT-1", ActivityType.Walking, 0f, 4f, Vector3.zero),
                new WorkEvent("EVT-2", ActivityType.Searching, 4f, 6f, Vector3.zero),
                new WorkEvent("EVT-3", ActivityType.Picking, 6f, 8f, Vector3.zero),
                new WorkEvent("EVT-4", ActivityType.Waiting, 8f, 10f, Vector3.zero)
            };

            // Test 1: Sampling interval = 2.0s -> points at t = 0, 2, 4, 6, 8, 10 (6 samples)
            var results2s = WorkStudyCalculator.Calculate(events, samplingIntervalSeconds: 2.0f);
            Assert.AreEqual(6, results2s.TotalWorkSamplesCount);

            // Test 2: Sampling interval = 1.0s -> points at t = 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 (11 samples)
            var results1s = WorkStudyCalculator.Calculate(events, samplingIntervalSeconds: 1.0f);
            Assert.AreEqual(11, results1s.TotalWorkSamplesCount);

            // Test 3: Verify sample counts sum to total samples
            int sumSamples = 0;
            float sumSamplePct = 0f;
            foreach (var s in results1s.SampleCounts)
            {
                sumSamples += s.Count;
                sumSamplePct += s.Percentage;
            }
            Assert.AreEqual(results1s.TotalWorkSamplesCount, sumSamples);
            Assert.AreEqual(100.0f, sumSamplePct, 0.05f);
        }

        [Test]
        public void SampleOrderExecution_GeneratesFullyReconciledResults()
        {
            eventLogger.Clear();

            var stationObj = new GameObject("PackingStation_01");
            stationObj.transform.SetParent(testRoot.transform);
            stationObj.transform.position = new Vector3(0f, 0f, -9.5f);
            stationObj.AddComponent<PackingStation>();

            classifier.ResetState(0f, new Vector3(0f, 0f, -8f));

            // Start 2-item sample order
            var skuIds = new List<string> { "SKU-0001", "SKU-0002" };
            var order = orderManager.StartSpecificOrder("ORD-0001", skuIds);

            float t = 1.0f;
            Vector3 pos = new Vector3(0f, 0f, -8f);

            // Travel to Item 1
            pos = new Vector3(0f, 0f, -2f);
            classifier.Step(t, pos, speed: 2.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 6f);

            // Approach Item 1
            t += 3.0f;
            pos = order.CurrentTargetItem.TargetWorldPosition + new Vector3(0f, 0f, 1.5f);
            classifier.Step(t, pos, speed: 1.2f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 3f);

            // Search Shelf
            t += 1.5f;
            classifier.Step(t, pos, speed: 0f, isLookingAtShelf: true, isAtPackingBench: false, frameDistance: 0f);

            // Pick Item 1
            t += 2.0f;
            orderManager.AttemptPick("SKU-0001");

            // Travel to Item 2
            t += 1.0f;
            pos = new Vector3(-6f, 0f, 2f);
            classifier.Step(t, pos, speed: 2.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 4f);

            // Approach Item 2
            t += 2.5f;
            pos = order.CurrentTargetItem.TargetWorldPosition + new Vector3(0f, 0f, 1.2f);
            classifier.Step(t, pos, speed: 1.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 2f);

            // Search Shelf
            t += 1.2f;
            classifier.Step(t, pos, speed: 0f, isLookingAtShelf: true, isAtPackingBench: false, frameDistance: 0f);

            // Pick Item 2
            t += 1.8f;
            orderManager.AttemptPick("SKU-0002");

            // Travel to packing station
            t += 1.0f;
            pos = new Vector3(0f, 0f, -5f);
            classifier.Step(t, pos, speed: 2.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 5f);

            // Approach packing bench
            t += 2.0f;
            pos = new Vector3(0f, 0f, -8.5f);
            classifier.Step(t, pos, speed: 1.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 2f);

            // At packing bench
            t += 1.0f;
            classifier.Step(t, pos, speed: 0f, isLookingAtShelf: false, isAtPackingBench: true, frameDistance: 0f);

            // Complete order
            t += 2.0f;
            orderManager.CompletePacking(out string failureReason);

            // Calculate results directly from logger
            var results = WorkStudyCalculator.CalculateFromLogger(eventLogger, samplingIntervalSeconds: 1.0f);

            // Assertions
            Assert.GreaterOrEqual(results.TotalEventsCount, 8, "Expected at least 8 events.");
            Assert.AreEqual(2, results.TotalPicks, "Expected 2 successful picks.");
            Assert.GreaterOrEqual(results.ShelfVisits, 1, "Expected shelf bay visits.");
            Assert.Greater(results.TotalTaskTime, 0f, "Total task time must be positive.");
            Assert.Greater(results.TotalDistance, 0f, "Total distance must be positive.");

            // Strict Mathematical Reconciliation
            Assert.IsTrue(results.VerifyReconciliation(out string reconciliationError), reconciliationError);

            float sumDurations = results.WalkingDuration + results.SearchingDuration + results.PickingDuration +
                                 results.WaitingDuration + results.PackingDuration + results.OrderCompletedDuration;
            Assert.AreEqual(results.TotalTaskTime, sumDurations, 0.001f);

            float sumPercentages = results.WalkingPercentage + results.SearchingPercentage + results.PickingPercentage +
                                   results.WaitingPercentage + results.PackingPercentage + results.OrderCompletedPercentage;
            Assert.AreEqual(100.0f, sumPercentages, 0.05f);

            Debug.Log(results.GetSummaryReport());
        }
    }
}
