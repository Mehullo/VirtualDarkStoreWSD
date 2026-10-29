using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Unity.XR.CoreUtils;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Interaction;
using VirtualDarkStore.Management;
using VirtualDarkStore.VR;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Tests.VR
{
    [TestFixture]
    public class VRRigTests
    {
        private GameObject testRoot;
        private StoreManager storeManager;
        private OrderManager orderManager;
        private WorkEventLogger eventLogger;
        private ActivityClassifier classifier;
        private VRPlayerRig vrRig;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("[VRTestRoot]");

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

            // Build VR Player Rig
            var rigObj = new GameObject("VRPlayerRig");
            rigObj.transform.SetParent(testRoot.transform);
            rigObj.transform.position = new Vector3(0f, 0f, -8f);

            var cc = rigObj.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var origin = rigObj.AddComponent<XROrigin>();
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            var offsetObj = new GameObject("CameraOffset");
            offsetObj.transform.SetParent(rigObj.transform, false);
            origin.CameraFloorOffsetObject = offsetObj;

            var camObj = new GameObject("VRMainCamera");
            camObj.transform.SetParent(offsetObj.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 1.70f, 0f);
            var cam = camObj.AddComponent<Camera>();
            var headTracking = camObj.AddComponent<VRHeadTracking>();
            origin.Camera = cam;

            var leftObj = new GameObject("LeftHandController");
            leftObj.transform.SetParent(offsetObj.transform, false);
            leftObj.transform.localPosition = new Vector3(-0.25f, 1.2f, 0.35f);
            var leftInput = leftObj.AddComponent<VRControllerInput>();
            var handField = typeof(VRControllerInput).GetField("hand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handField != null) handField.SetValue(leftInput, VRHand.Left);

            var rightObj = new GameObject("RightHandController");
            rightObj.transform.SetParent(offsetObj.transform, false);
            rightObj.transform.localPosition = new Vector3(0.25f, 1.2f, 0.35f);
            var rightInput = rightObj.AddComponent<VRControllerInput>();
            if (handField != null) handField.SetValue(rightInput, VRHand.Right);

            var loco = rigObj.AddComponent<VRLocomotionController>();
            loco.SetReferences(leftInput, camObj.transform, cc);

            var picker = rigObj.AddComponent<VRPickerInteraction>();
            picker.SetReferences(rightInput, cam);

            vrRig = rigObj.AddComponent<VRPlayerRig>();
            vrRig.InitializeRig();

            classifier.SetVRReferences(eventLogger, orderManager, vrRig);
            classifier.ResetState(0f, vrRig.transform.position);
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
        public void VRPlayerRig_InitializesHierarchy_AndTrackingComponents()
        {
            Assert.IsNotNull(vrRig.XrOrigin, "Expected XROrigin component on VRPlayerRig.");
            Assert.IsNotNull(vrRig.HmdCamera, "Expected HMD Camera on VRPlayerRig.");
            Assert.IsNotNull(vrRig.HeadTracking, "Expected VRHeadTracking on VRPlayerRig.");
            Assert.IsNotNull(vrRig.LeftController, "Expected LeftController on VRPlayerRig.");
            Assert.IsNotNull(vrRig.RightController, "Expected RightController on VRPlayerRig.");
            Assert.IsNotNull(vrRig.PickerInteraction, "Expected VRPickerInteraction on VRPlayerRig.");
            Assert.IsNotNull(vrRig.LocomotionController, "Expected VRLocomotionController on VRPlayerRig.");

            Assert.AreEqual(VRHand.Left, vrRig.LeftController.Hand);
            Assert.AreEqual(VRHand.Right, vrRig.RightController.Hand);
            Assert.AreEqual(new Vector3(0f, 0f, -8f), vrRig.FloorPosition);
        }

        [Test]
        public void VRHeadTracking_ExposesOrientationAndRay_WithoutPhysiologicalClaims()
        {
            var ht = vrRig.HeadTracking;
            Assert.IsNotNull(ht);

            // Test simulated pose setting
            Vector3 testLocalPos = new Vector3(0f, 1.75f, 0f);
            Quaternion testLocalRot = Quaternion.Euler(15f, 45f, 0f);
            ht.SetSimulatedPose(testLocalPos, testLocalRot);

            Assert.IsTrue(ht.IsTracked);
            Assert.AreEqual(testLocalPos, ht.HmdCamera.transform.localPosition);
            Assert.AreEqual(testLocalRot, ht.HmdCamera.transform.localRotation);

            // Verify head ray matches physical camera transform
            Ray ray = ht.HeadRay;
            Assert.AreEqual(ht.HeadPosition, ray.origin);
            Assert.AreEqual(ht.Forward, ray.direction);
        }

        [Test]
        public void VRControllerInput_TracksSpatialPose_AndSimulatesButtons()
        {
            var controller = vrRig.RightController;
            Assert.IsNotNull(controller);

            controller.SetSimulatedPose(new Vector3(0.3f, 1.3f, 0.4f), Quaternion.identity);
            Assert.IsTrue(controller.IsTracked);

            bool actionFired = false;
            controller.OnSelectAction += () => actionFired = true;

            controller.SimulateTriggerPress();
            Assert.IsTrue(controller.IsTriggerPressed);
            Assert.AreEqual(1.0f, controller.TriggerValue);
            Assert.IsTrue(actionFired, "Expected OnSelectAction event on trigger press.");

            controller.SimulateTriggerRelease();
            Assert.IsFalse(controller.IsTriggerPressed);
            Assert.AreEqual(0f, controller.TriggerValue);

            controller.SimulateThumbstick(new Vector2(0.5f, 0.8f));
            Assert.AreEqual(new Vector2(0.5f, 0.8f), controller.Thumbstick);
        }

        [Test]
        public void VRPickerInteraction_ValidatesAndPicksItems_SharingSameOrderSystem()
        {
            var picker = vrRig.PickerInteraction;
            Assert.IsNotNull(picker);

            // Create target pickable shelf product directly in front of the active right-hand controller ray
            var productObj = new GameObject("TestProduct_SKU0001");
            productObj.transform.SetParent(testRoot.transform);
            productObj.transform.position = vrRig.RightController.transform.position + Vector3.forward * 1.5f; // within reach (1.5m < 2.8m)
            var col = productObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.3f, 0.3f, 0.3f);
            var visual = productObj.AddComponent<PickableItemVisual>();
            visual.Initialize("SKU-0001", "Cold Brew Coffee 330ml", "A01-L-B01-T01-S01");

            // Dispatch order
            orderManager.StartSpecificOrder("ORD-VR-001", new List<string> { "SKU-0001" });
            Assert.IsTrue(orderManager.HasActiveOrder);

            // Perform raycast check
            picker.PerformRaycast();
            Assert.AreEqual(visual, picker.HoveredItem);
            StringAssert.Contains("SKU-0001", picker.CurrentPrompt);

            // Trigger pick interaction
            bool pickSuccess = picker.ExecuteInteraction();
            Assert.IsTrue(pickSuccess, "Expected successful pick interaction.");
            Assert.IsTrue(visual.IsPicked, "Expected visual to be marked picked.");
            Assert.IsTrue(orderManager.IsAwaitingPacking, "Order should now await packing.");

            // Create packing station in front of controller
            var stationObj = new GameObject("PackingStation_01");
            stationObj.transform.SetParent(testRoot.transform);
            stationObj.transform.position = vrRig.RightController.transform.position + Vector3.forward * 1.0f;
            var stationCol = stationObj.AddComponent<BoxCollider>();
            stationCol.size = new Vector3(1.5f, 1f, 1f);
            var packingStation = stationObj.AddComponent<PackingStation>();

            picker.PerformRaycast();
            Assert.AreEqual(packingStation, picker.HoveredPackingStation);
            StringAssert.Contains("Deliver to Staging", picker.CurrentPrompt);

            // Finalize packing
            bool packSuccess = picker.ExecuteInteraction();
            Assert.IsTrue(packSuccess, "Expected order to be completed and packed.");
            Assert.AreEqual(OrderStatus.Completed, orderManager.ActiveOrder.Status);
        }

        [Test]
        public void VRLocomotion_ContinuousAndTeleportation_PreservesTravelDistanceMeasurement()
        {
            var loco = vrRig.LocomotionController;
            Assert.IsNotNull(loco);

            Vector3 startPos = vrRig.transform.position;
            Assert.AreEqual(0f, loco.CumulativeTravelDistance);

            // Teleport 5m down aisle
            Vector3 targetPos = startPos + new Vector3(0f, 0f, 5.0f);
            bool teleported = loco.TeleportTo(targetPos);

            Assert.IsTrue(teleported, "Expected teleportation to succeed within max distance.");
            Assert.AreEqual(targetPos, vrRig.transform.position);
            Assert.AreEqual(5.0f, loco.LastFrameDisplacement, 0.001f);
            Assert.AreEqual(5.0f, loco.CumulativeTravelDistance, 0.001f);

            // Reject teleport exceeding max distance (> 10m)
            Vector3 invalidFarPos = vrRig.transform.position + new Vector3(0f, 0f, 25.0f);
            bool rejected = loco.TeleportTo(invalidFarPos);
            Assert.IsFalse(rejected, "Expected teleport exceeding max range to be rejected.");
            Assert.AreEqual(5.0f, loco.CumulativeTravelDistance, 0.001f);
        }

        [Test]
        public void VRFulfillment_IntegrationWithActivityClassifier_AndWorkStudyCalculator()
        {
            eventLogger.Clear();

            // Set up packing station in test hierarchy for distance evaluation
            var stationObj = new GameObject("PackingStation_01");
            stationObj.transform.SetParent(testRoot.transform);
            stationObj.transform.position = new Vector3(0f, 0f, -9.5f);
            stationObj.AddComponent<PackingStation>();

            classifier.ResetState(0f, vrRig.transform.position);

            // 1. Dispatch 2-item deterministic order for VR fulfillment
            var skuIds = new List<string> { "SKU-0001", "SKU-0002" };
            var order = orderManager.StartSpecificOrder("ORD-VR-002", skuIds);
            Assert.IsNotNull(order);

            float t = 1.0f;
            var item1 = order.CurrentTargetItem;
            Vector3 target1Pos = item1.TargetWorldPosition;

            // Step 1: VR Picker teleports towards aisle 1 (MovingBetweenLocations)
            Vector3 pos = new Vector3(0f, 0f, -2f);
            vrRig.transform.position = pos;
            classifier.Step(t, pos, speed: 2.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 6f);
            Assert.AreEqual(ActivityType.MovingBetweenLocations, classifier.CurrentActivity);

            // Step 2: Approach target rack bay
            t += 3.0f;
            pos = target1Pos + new Vector3(0f, 0f, 1.5f);
            vrRig.transform.position = pos;
            classifier.Step(t, pos, speed: 1.2f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 3f);
            Assert.AreEqual(ActivityType.Approaching, classifier.CurrentActivity);

            // Step 3: Searching shelf
            t += 1.5f;
            classifier.Step(t, pos, speed: 0f, isLookingAtShelf: true, isAtPackingBench: false, frameDistance: 0f);
            Assert.AreEqual(ActivityType.Searching, classifier.CurrentActivity);

            // Step 4: Pick SKU-0001
            t += 2.0f;
            var pick1 = orderManager.AttemptPick("SKU-0001");
            Assert.AreEqual(PickResult.Success, pick1);

            // Step 5: Travel to SKU-0002
            var item2 = order.CurrentTargetItem;
            Vector3 target2Pos = item2.TargetWorldPosition;
            t += 1.0f;
            pos = (target1Pos + target2Pos) * 0.5f;
            vrRig.transform.position = pos;
            classifier.Step(t, pos, speed: 2.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 4f);

            t += 2.5f;
            pos = target2Pos + new Vector3(0f, 0f, 1.2f);
            vrRig.transform.position = pos;
            classifier.Step(t, pos, speed: 1.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 2f);

            t += 1.2f;
            classifier.Step(t, pos, speed: 0f, isLookingAtShelf: true, isAtPackingBench: false, frameDistance: 0f);
            Assert.AreEqual(ActivityType.Searching, classifier.CurrentActivity);

            t += 1.8f;
            var pick2 = orderManager.AttemptPick("SKU-0002");
            Assert.AreEqual(PickResult.Success, pick2);
            Assert.IsTrue(order.IsAllItemsPicked);

            // Step 6: Travel back to packing bench
            t += 1.0f;
            pos = new Vector3(0f, 0f, -4f);
            vrRig.transform.position = pos;
            classifier.Step(t, pos, speed: 2.2f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 6f);

            t += 2.5f;
            pos = new Vector3(0f, 0f, -8.5f);
            vrRig.transform.position = pos;
            classifier.Step(t, pos, speed: 1.0f, isLookingAtShelf: false, isAtPackingBench: false, frameDistance: 2f);

            t += 1.2f;
            classifier.Step(t, pos, speed: 0f, isLookingAtShelf: false, isAtPackingBench: true, frameDistance: 0f);
            Assert.AreEqual(ActivityType.Packing, classifier.CurrentActivity);

            t += 2.0f;
            bool packed = orderManager.CompletePacking(out string failureReason);
            Assert.IsTrue(packed, failureReason);
            Assert.AreEqual(OrderStatus.Completed, order.Status);

            // Verify WorkStudy Results computed from VR events
            var events = eventLogger.LoggedEvents;
            Assert.GreaterOrEqual(events.Count, 5);
            Assert.IsTrue(eventLogger.VerifyIntegrity(out string integrityErr), integrityErr);

            var results = WorkStudyCalculator.Calculate(events);
            Assert.IsNotNull(results);
            Assert.IsTrue(results.VerifyReconciliation(out string reconErr), reconErr);
            Assert.AreEqual(2, results.TotalPicks);
            Assert.Greater(results.TotalDistance, 15f);
            Assert.Greater(results.TotalTaskTime, 10f);

            Debug.Log("[VR WorkStudy Summary]\n" + results.GetSummaryReport());
        }

        [Test]
        public void VRHardware_Detection_ReportsActualStatus()
        {
            var report = VRHardwareDetector.CheckHardware();
            Debug.Log($"[VR Hardware Report] Status: {report.Status}, Device: {report.DeviceName}, Msg: {report.StatusMessage}");

            // In headless/test runner without physical headset connected, must be marked NotRun
            // (or Detected if physical HMD is attached). Never invent hardware!
            if (report.DeviceName == "None" || string.IsNullOrEmpty(report.DeviceName))
            {
                Assert.AreEqual(HardwareVerificationStatus.NotRun, report.Status);
                StringAssert.Contains("NOT RUN", report.StatusMessage);
            }
            else
            {
                Assert.AreEqual(HardwareVerificationStatus.Detected, report.Status);
            }

            // Export verification evidence report
            string evidenceDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "evidence", "phase7"));
            Directory.CreateDirectory(evidenceDir);
            File.WriteAllText(Path.Combine(evidenceDir, "vr_hardware_report.md"), VRHardwareDetector.GenerateReportMarkdown());
        }
    }
}
