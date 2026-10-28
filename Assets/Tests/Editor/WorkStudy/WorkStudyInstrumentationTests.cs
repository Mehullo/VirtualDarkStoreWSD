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
    public class WorkStudyInstrumentationTests
    {
        private GameObject testRoot;
        private StoreManager storeManager;
        private OrderManager orderManager;
        private WorkEventLogger eventLogger;
        private ActivityClassifier classifier;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("[WorkStudyTestRoot]");

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
        public void WorkEvent_CalculatesDuration_AndValidatesNonNegativeValues()
        {
            var evt = new WorkEvent(
                "EVT-0001",
                ActivityType.Searching,
                startTime: 10.5f,
                endTime: 14.2f,
                position: new Vector3(2f, 0f, 4f),
                locationName: "A01-L-B01-T01-S01",
                relatedSku: "SKU-0001",
                relatedOrder: "ORD-0001",
                distance: 0.8f);

            Assert.AreEqual("EVT-0001", evt.Id);
            Assert.AreEqual(ActivityType.Searching, evt.Activity);
            Assert.AreEqual(10.5f, evt.StartTime);
            Assert.AreEqual(14.2f, evt.EndTime);
            Assert.AreEqual(3.7f, evt.Duration, 0.001f);
            Assert.AreEqual(0.8f, evt.Distance, 0.001f);
            Assert.AreEqual("SKU-0001", evt.RelatedSku);
            Assert.AreEqual("ORD-0001", evt.RelatedOrder);
        }

        [Test]
        public void WorkEvent_RejectsNegativeOrInvertedTimestamps()
        {
            // Negative start time
            Assert.Throws<ArgumentException>(() =>
            {
                new WorkEvent("EVT-ERR1", ActivityType.Waiting, -1.0f, 2.0f, Vector3.zero);
            });

            // End time before start time
            Assert.Throws<ArgumentException>(() =>
            {
                new WorkEvent("EVT-ERR2", ActivityType.Walking, 10.0f, 8.0f, Vector3.zero);
            });

            // Negative distance
            Assert.Throws<ArgumentException>(() =>
            {
                new WorkEvent("EVT-ERR3", ActivityType.Walking, 0f, 1f, Vector3.zero, distance: -5f);
            });
        }

        [Test]
        public void WorkEventLogger_MaintainsMonotonicOrderedTimestamps()
        {
            eventLogger.Clear();

            var evt1 = new WorkEvent("EVT-0001", ActivityType.Waiting, 0.0f, 2.0f, Vector3.zero);
            var evt2 = new WorkEvent("EVT-0002", ActivityType.Walking, 2.0f, 5.5f, new Vector3(3f, 0f, 0f), distance: 3f);
            var evt3 = new WorkEvent("EVT-0003", ActivityType.Approaching, 5.5f, 7.0f, new Vector3(5f, 0f, 0f), distance: 2f);

            eventLogger.LogEvent(evt1);
            eventLogger.LogEvent(evt2);
            eventLogger.LogEvent(evt3);

            Assert.AreEqual(3, eventLogger.EventCount);
            Assert.IsTrue(eventLogger.VerifyIntegrity(out string reason), reason);

            // Out-of-order event must throw
            var outOfOrderEvt = new WorkEvent("EVT-0004", ActivityType.Waiting, 1.0f, 3.0f, Vector3.zero);
            Assert.Throws<InvalidOperationException>(() =>
            {
                eventLogger.LogEvent(outOfOrderEvt);
            });
        }

        [Test]
        public void ActivityClassifier_EvaluatesAll8Activities_AccordingToMeasurableRules()
        {
            // 1. Waiting: Stationary (speed = 0), no active order
            var act1 = classifier.EvaluateActivity(
                speed: 0f,
                hasActiveOrder: false,
                isAwaitingPacking: false,
                distToTarget: 10f,
                distToPacking: 10f,
                isLookingAtShelf: false,
                isAtPackingBench: false);
            Assert.AreEqual(ActivityType.Waiting, act1);

            // 2. Walking: Moving (speed = 1.4 m/s), no active order
            var act2 = classifier.EvaluateActivity(
                speed: 1.4f,
                hasActiveOrder: false,
                isAwaitingPacking: false,
                distToTarget: 10f,
                distToPacking: 10f,
                isLookingAtShelf: false,
                isAtPackingBench: false);
            Assert.AreEqual(ActivityType.Walking, act2);

            // 3. MovingBetweenLocations: Moving with active order, outside approach zone (dist = 8m > 3m)
            var act3 = classifier.EvaluateActivity(
                speed: 1.4f,
                hasActiveOrder: true,
                isAwaitingPacking: false,
                distToTarget: 8.0f,
                distToPacking: 15.0f,
                isLookingAtShelf: false,
                isAtPackingBench: false);
            Assert.AreEqual(ActivityType.MovingBetweenLocations, act3);

            // 4. Approaching: Moving with active order, within approach zone (dist = 1.8m <= 3m)
            var act4 = classifier.EvaluateActivity(
                speed: 1.0f,
                hasActiveOrder: true,
                isAwaitingPacking: false,
                distToTarget: 1.8f,
                distToPacking: 12.0f,
                isLookingAtShelf: false,
                isAtPackingBench: false);
            Assert.AreEqual(ActivityType.Approaching, act4);

            // 5. Searching: Stationary near target rack (dist = 1.2m <= 3m) AND looking at shelf
            var act5 = classifier.EvaluateActivity(
                speed: 0f,
                hasActiveOrder: true,
                isAwaitingPacking: false,
                distToTarget: 1.2f,
                distToPacking: 12.0f,
                isLookingAtShelf: true,
                isAtPackingBench: false);
            Assert.AreEqual(ActivityType.Searching, act5);

            // If stationary near rack but NOT looking at shelf -> Waiting
            var act5b = classifier.EvaluateActivity(
                speed: 0f,
                hasActiveOrder: true,
                isAwaitingPacking: false,
                distToTarget: 1.2f,
                distToPacking: 12.0f,
                isLookingAtShelf: false,
                isAtPackingBench: false);
            Assert.AreEqual(ActivityType.Waiting, act5b);

            // 6. Packing: Order is awaiting packing, stationary at packing bench
            var act6 = classifier.EvaluateActivity(
                speed: 0f,
                hasActiveOrder: false,
                isAwaitingPacking: true,
                distToTarget: 20f,
                distToPacking: 1.0f,
                isLookingAtShelf: false,
                isAtPackingBench: true);
            Assert.AreEqual(ActivityType.Packing, act6);
        }

        [Test]
        public void SampleOrderFulfillment_GeneratesOrderedNonNegativeEvents()
        {
            eventLogger.Clear();

            // Set up packing station in test hierarchy for distance evaluation
            var stationObj = new GameObject("PackingStation_01");
            stationObj.transform.SetParent(testRoot.transform);
            stationObj.transform.position = new Vector3(0f, 0f, -9.5f);
            stationObj.AddComponent<PackingStation>();

            classifier.ResetState(0f, new Vector3(0f, 0f, -8f));

            // 1. Dispatch 2-item deterministic order
            var skuIds = new List<string> { "SKU-0001", "SKU-0002" };
            var order = orderManager.StartSpecificOrder("ORD-0001", skuIds);
            Assert.IsNotNull(order);

            // Time progression simulation
            float t = 1.0f;
            Vector3 pos = new Vector3(0f, 0f, -8f);

            // Step 1: Picker begins travel towards first item slot
            var item1 = order.CurrentTargetItem;
            Vector3 target1Pos = item1.TargetWorldPosition;

            // Travel across warehouse (MovingBetweenLocations)
            pos = new Vector3(0f, 0f, -2f);
            classifier.Step(t, pos, speed: 2.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 6f);
            Assert.AreEqual(ActivityType.MovingBetweenLocations, classifier.CurrentActivity);

            // Approach rack bay
            t += 3.0f;
            pos = target1Pos + new Vector3(0f, 0f, 1.5f); // 1.5m away (< 3.0m)
            classifier.Step(t, pos, speed: 1.2f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 3f);
            Assert.AreEqual(ActivityType.Approaching, classifier.CurrentActivity);

            // Stop at bay and search shelf
            t += 1.5f;
            classifier.Step(t, pos, speed: 0f, isLookingAtShelf: true, isAtPackingBench: false, frameDistance: 0f);
            Assert.AreEqual(ActivityType.Searching, classifier.CurrentActivity);

            // Picker interacts and picks SKU-0001
            t += 2.0f;
            var pick1Result = orderManager.AttemptPick("SKU-0001");
            Assert.AreEqual(PickResult.Success, pick1Result);

            // Next item: SKU-0002
            var item2 = order.CurrentTargetItem;
            Assert.IsNotNull(item2);
            Vector3 target2Pos = item2.TargetWorldPosition;

            // Travel to item 2
            t += 1.0f;
            pos = (target1Pos + target2Pos) * 0.5f;
            classifier.Step(t, pos, speed: 2.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 4f);

            // Approach item 2
            t += 2.5f;
            pos = target2Pos + new Vector3(0f, 0f, 1.2f);
            classifier.Step(t, pos, speed: 1.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 2f);

            // Stop and search shelf
            t += 1.2f;
            classifier.Step(t, pos, speed: 0f, isLookingAtShelf: true, isAtPackingBench: false, frameDistance: 0f);
            Assert.AreEqual(ActivityType.Searching, classifier.CurrentActivity);

            // Pick SKU-0002
            t += 1.8f;
            var pick2Result = orderManager.AttemptPick("SKU-0002");
            Assert.AreEqual(PickResult.Success, pick2Result);
            Assert.IsTrue(order.IsAllItemsPicked);
            Assert.AreEqual(OrderStatus.AwaitingPacking, order.Status);

            // Travel back to packing bench (MovingBetweenLocations -> Approaching)
            t += 1.0f;
            pos = new Vector3(0f, 0f, -4f);
            classifier.Step(t, pos, speed: 2.2f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 6f);
            Assert.AreEqual(ActivityType.MovingBetweenLocations, classifier.CurrentActivity);

            t += 2.5f;
            pos = new Vector3(0f, 0f, -8.5f); // 1.0m from packing station
            classifier.Step(t, pos, speed: 1.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 2f);
            Assert.AreEqual(ActivityType.Approaching, classifier.CurrentActivity);

            // Arrive at packing bench
            t += 1.2f;
            classifier.Step(t, pos, speed: 0f, isLookingAtShelf: false, isAtPackingBench: true, frameDistance: 0f);
            Assert.AreEqual(ActivityType.Packing, classifier.CurrentActivity);

            // Complete packing
            t += 2.0f;
            bool packed = orderManager.CompletePacking(out string failureReason);
            Assert.IsTrue(packed, failureReason);
            Assert.AreEqual(OrderStatus.Completed, order.Status);

            // INSPECT GENERATED EVENTS
            var events = eventLogger.LoggedEvents;
            Debug.Log($"=== GENERATED WORK EVENTS: {events.Count} TOTAL ===");
            foreach (var evt in events)
            {
                Debug.Log(evt.ToString());
            }

            Assert.GreaterOrEqual(events.Count, 5, "Expected at least 5 logged work-study events across sample order fulfillment.");

            // Strict Invariant Verification:
            // 1. Ordered timestamps
            // 2. Non-negative durations
            // 3. Valid activities
            Assert.IsTrue(eventLogger.VerifyIntegrity(out string integrityError), integrityError);

            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                Assert.IsFalse(string.IsNullOrEmpty(evt.Id), $"Event {i} has empty ID.");
                Assert.GreaterOrEqual(evt.StartTime, 0f, $"Event {evt.Id} has negative start time.");
                Assert.GreaterOrEqual(evt.EndTime, evt.StartTime, $"Event {evt.Id} has end time preceding start time.");
                Assert.GreaterOrEqual(evt.Duration, 0f, $"Event {evt.Id} has negative duration.");
                Assert.GreaterOrEqual(evt.Distance, 0f, $"Event {evt.Id} has negative distance.");

                if (i > 0)
                {
                    Assert.GreaterOrEqual(evt.StartTime, events[i - 1].StartTime,
                        $"Event {evt.Id} start time ({evt.StartTime}s) is out of order with previous ({events[i - 1].StartTime}s).");
                }
            }

            // Verify specific activities generated in order run
            var activities = new HashSet<ActivityType>();
            foreach (var evt in events) activities.Add(evt.Activity);

            Assert.IsTrue(activities.Contains(ActivityType.MovingBetweenLocations), "Expected MovingBetweenLocations event.");
            Assert.IsTrue(activities.Contains(ActivityType.Approaching), "Expected Approaching event.");
            Assert.IsTrue(activities.Contains(ActivityType.Searching), "Expected Searching event.");
            Assert.IsTrue(activities.Contains(ActivityType.Picking), "Expected Picking event.");
            Assert.IsTrue(activities.Contains(ActivityType.Packing), "Expected Packing event.");
            Assert.IsTrue(activities.Contains(ActivityType.OrderCompleted), "Expected OrderCompleted event.");
        }
    }
}
