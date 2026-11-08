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
            var rigObj = new GameObject("XR Origin Hands (XR Rig)");
            rigObj.transform.SetParent(testRoot.transform);
            rigObj.transform.position = new Vector3(0f, 0f, -8f);

            var cc = rigObj.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var origin = rigObj.AddComponent<XROrigin>();
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            var offsetObj = new GameObject("Camera Floor Offset");
            offsetObj.transform.SetParent(rigObj.transform, false);
            origin.CameraFloorOffsetObject = offsetObj;

            var camObj = new GameObject("VRMainCamera");
            camObj.transform.SetParent(offsetObj.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 1.70f, 0f);
            var cam = camObj.AddComponent<Camera>();
            var headTracking = camObj.AddComponent<VRHeadTracking>();
            origin.Camera = cam;

            var leftObj = new GameObject("Left Hand");
            leftObj.transform.SetParent(offsetObj.transform, false);
            leftObj.transform.localPosition = new Vector3(-0.25f, 1.2f, 0.35f);
            var leftInput = leftObj.AddComponent<VRControllerInput>();
            var handField = typeof(VRControllerInput).GetField("hand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handField != null) handField.SetValue(leftInput, VRHand.Left);

            var rightObj = new GameObject("Right Hand");
            rightObj.transform.SetParent(offsetObj.transform, false);
            rightObj.transform.localPosition = new Vector3(0.25f, 1.2f, 0.35f);
            var rightInput = rightObj.AddComponent<VRControllerInput>();
            if (handField != null) handField.SetValue(rightInput, VRHand.Right);

            var loco = rigObj.AddComponent<VRLocomotionController>();
            loco.SetReferences(leftInput, camObj.transform, cc, rightInput);

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
            productObj.transform.position = vrRig.RightController.transform.position + vrRig.RightController.PointerForward * 1.5f; // within reach (1.5m < 2.8m)
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

        [Test]
        public void VRPlayerRig_CenterSpawnAutoCorrection_MovesToWarehouseStart()
        {
            var centerRigObj = new GameObject("CenterTestRig");
            centerRigObj.transform.SetParent(testRoot.transform);
            centerRigObj.transform.position = Vector3.zero; // Starts at centre

            var rig = centerRigObj.AddComponent<VRPlayerRig>();
            rig.InitializeRig();

            Assert.AreEqual(VRPlayerRig.DefaultWarehouseStartPoint, centerRigObj.transform.position,
                "Expected rig starting at centre (0,0,0) to be relocated to operational start point (0, 0, -8.5).");
            Assert.AreEqual(Quaternion.identity, centerRigObj.transform.rotation);
        }

        [Test]
        public void VRPickerInteraction_GripGrab_ExecutesPick()
        {
            var picker = vrRig.PickerInteraction;
            var controller = vrRig.RightController;

            var productObj = new GameObject("TestProduct_GripSKU");
            productObj.transform.SetParent(testRoot.transform);
            productObj.transform.position = controller.transform.position + controller.PointerForward * 1.2f;
            var col = productObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.3f, 0.3f, 0.3f);
            var visual = productObj.AddComponent<PickableItemVisual>();
            visual.Initialize("SKU-0005", "Almond Butter 250g", "A01-L-B01-T02-S01");

            orderManager.StartSpecificOrder("ORD-VR-GRIP", new List<string> { "SKU-0005" });
            picker.PerformRaycast();

            Assert.AreEqual(visual, picker.HoveredItem);
            StringAssert.Contains("Grip", picker.CurrentPrompt);

            // Simulate Grip Grab
            controller.SimulateGripPress();
            Assert.IsTrue(controller.IsGripPressed);
            Assert.IsTrue(controller.IsGrabPressed);
            Assert.IsTrue(visual.IsPicked, "Expected item to be picked via controller grip (grab) press.");
            controller.SimulateGripRelease();
        }

        [Test]
        public void VRLocomotion_SprintInteraction_AcceleratesSpeed()
        {
            var loco = vrRig.LocomotionController;
            Assert.IsNotNull(loco);

            Assert.AreEqual(1.4f, loco.WalkSpeed, 0.01f);
            Assert.AreEqual(3.5f, loco.SprintSpeed, 0.01f);
            Assert.IsFalse(loco.IsSprinting);
            Assert.AreEqual(1.4f, loco.MoveSpeed, 0.01f);

            // Engage sprint
            loco.SimulateSprint(true);
            Assert.IsTrue(loco.IsSprinting);
            Assert.AreEqual(3.5f, loco.MoveSpeed, 0.01f);

            // Disengage sprint
            loco.SimulateSprint(false);
            Assert.IsFalse(loco.IsSprinting);
            Assert.AreEqual(1.4f, loco.MoveSpeed, 0.01f);
        }

        [Test]
        public void VRPlayerRig_CameraAngle_IsLevelWithWarehouseFloor()
        {
            var origin = vrRig.XrOrigin;
            Assert.IsNotNull(origin);

            // Camera offset must have zero pitch/roll tilt
            Assert.AreEqual(Quaternion.identity, origin.CameraFloorOffsetObject.transform.localRotation,
                "CameraFloorOffsetObject must have zero rotation to avoid tilted horizon in VR headset.");

            // RequestedTrackingOriginMode must be Floor
            Assert.AreEqual(XROrigin.TrackingOriginMode.Floor, origin.RequestedTrackingOriginMode);
            Assert.AreEqual(1.68f, origin.CameraYOffset, 0.05f);
        }

        [Test]
        public void VRPlayerRig_HandVisuals_AreCreatedAndRenderersExist()
        {
            Assert.IsNotNull(vrRig.LeftHandVisual, "Left hand visual component must exist on LeftHandController.");
            Assert.IsNotNull(vrRig.RightHandVisual, "Right hand visual component must exist on RightHandController.");

            // Left hand has wrist anchor for wrist UI
            Assert.IsNotNull(vrRig.LeftHandVisual.WristAnchor, "Left hand visual must provide a WristAnchor.");

            // Right hand has laser pointer ray for distant targeting
            Assert.IsNotNull(vrRig.RightHandVisual.PointerLine, "Right hand visual must provide a LineRenderer pointer.");

            // Visual meshes must exist on hand children (ControllerBody, Grip, etc.)
            var leftRenderers = vrRig.LeftController.GetComponentsInChildren<MeshRenderer>();
            var rightRenderers = vrRig.RightController.GetComponentsInChildren<MeshRenderer>();
            Assert.GreaterOrEqual(leftRenderers.Length, 3, "Left hand should have visible controller/hand mesh parts.");
            Assert.GreaterOrEqual(rightRenderers.Length, 3, "Right hand should have visible controller/hand mesh parts.");

            // Ensure colliders are stripped so hands do not block physics or raycasts
            var leftColliders = vrRig.LeftController.GetComponentsInChildren<Collider>();
            var rightColliders = vrRig.RightController.GetComponentsInChildren<Collider>();
            Assert.AreEqual(0, leftColliders.Length, "Hand visual objects must not contain colliders to prevent locomotion/picking collisions.");
            Assert.AreEqual(0, rightColliders.Length, "Hand visual objects must not contain colliders to prevent locomotion/picking collisions.");
        }

        [Test]
        public void VRPlayerRig_NextOrderWristUI_RendersStaticOnWristAndMatchesOrder()
        {
            var wristUI = vrRig.WristOrderUI;
            Assert.IsNotNull(wristUI, "VRNextOrderWristUI must be initialized on VR rig.");

            // Must be childed to left wrist anchor by default
            Assert.AreEqual(vrRig.LeftHandVisual.WristAnchor, wristUI.transform.parent, "Wrist UI must be anchored to Left Hand WristAnchor.");

            // Start an order and verify display updates
            orderManager.StartSpecificOrder("ORD-VR-TEST-001", new List<string> { "SKU-0001", "SKU-0002" });
            wristUI.RefreshOrderDisplay();

            StringAssert.Contains("ORD-VR-TEST-001", wristUI.TitleText);
            StringAssert.Contains("TOTAL ITEMS: 2", wristUI.SubtitleText);
            StringAssert.Contains("Cold Brew Coffee", wristUI.ItemRowsText);

            // Toggle static pin mode
            wristUI.TogglePinMode();
            Assert.IsTrue(wristUI.IsPinnedStatic, "Pinning wrist UI should set IsPinnedStatic true.");
            wristUI.TogglePinMode();
            Assert.IsFalse(wristUI.IsPinnedStatic, "Unpinning should return UI to wrist anchor.");
        }

        [Test]
        public void VRGazePromptUI_ShowsGrabPrompt_WhenLookingAtPickableItem()
        {
            var gazePrompt = vrRig.GazePromptUI;
            var picker = vrRig.PickerInteraction;
            var hmdCam = vrRig.HmdCamera;

            Assert.IsNotNull(gazePrompt, "VRGazePromptUI must be present on VR rig.");
            Assert.IsNotNull(picker, "VRPickerInteraction must be present.");
            Assert.IsFalse(gazePrompt.IsHovering, "Prompt should be inactive initially.");

            // Place pickable SKU directly in front of camera gaze
            var productObj = new GameObject("TestProduct_GazeSKU");
            productObj.transform.SetParent(testRoot.transform);
            productObj.transform.position = hmdCam.transform.position + hmdCam.transform.forward * 1.5f;
            var col = productObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.4f, 0.4f, 0.4f);
            var visual = productObj.AddComponent<PickableItemVisual>();
            visual.Initialize("SKU-0003", "Oat Milk Barista 1L", "A01-L-B01-T01-S03");

            orderManager.StartSpecificOrder("ORD-VR-GAZE", new List<string> { "SKU-0003" });

            // Raycast toward object
            picker.PerformRaycast();

            Assert.AreEqual(visual, picker.HoveredItem, "Picker should detect product via gaze ray.");
            Assert.IsTrue(gazePrompt.IsHovering, "VRGazePromptUI should be active when looking at grabable SKU.");
            StringAssert.Contains("Grab: Oat Milk Barista 1L", gazePrompt.CurrentPromptText);
            StringAssert.Contains("Trigger / Grip", gazePrompt.CurrentPromptText);

            // Move camera away so gaze ray hits empty space
            hmdCam.transform.position = new Vector3(100f, 100f, 100f);
            picker.PerformRaycast();

            Assert.IsNull(picker.HoveredItem);
            Assert.IsFalse(gazePrompt.IsHovering, "Gaze prompt should hide when looking away from object.");
        }

        [Test]
        public void VRPlayerRig_ControllersAndPointer_AreTilted50DegreesAwayFromUser()
        {
            var leftVisual = vrRig.LeftHandVisual;
            var rightVisual = vrRig.RightHandVisual;
            var rightController = vrRig.RightController;

            Assert.IsNotNull(leftVisual);
            Assert.IsNotNull(rightVisual);
            Assert.IsNotNull(rightController);

            // Ergonomic tilt angle must be 50 degrees away from user
            Assert.AreEqual(50f, leftVisual.TiltAngleAwayFromUser, 0.01f);
            Assert.AreEqual(50f, rightVisual.TiltAngleAwayFromUser, 0.01f);
            Assert.AreEqual(50f, rightController.ForwardAngleOffset, 0.01f);

            // Visual model roots must have local rotation around X of 50 degrees
            var leftModel = leftVisual.transform.Find("HandModelRoot");
            var rightModel = rightVisual.transform.Find("HandModelRoot");
            Assert.IsNotNull(leftModel, "Left hand model root must exist.");
            Assert.IsNotNull(rightModel, "Right hand model root must exist.");

            Assert.AreEqual(50f, leftModel.localEulerAngles.x, 0.5f, "Left hand model must tilt 50 deg away from user.");
            Assert.AreEqual(50f, rightModel.localEulerAngles.x, 0.5f, "Right hand model must tilt 50 deg away from user.");

            // Pointer tip must also be tilted 50 degrees
            Assert.AreEqual(50f, rightVisual.PointerTip.localEulerAngles.x, 0.5f, "Pointer tip must tilt 50 deg forward.");

            // PointerForward must point forward and downward relative to unrotated controller (pitch down by 50 deg)
            Vector3 forwardRay = rightController.PointerForward;
            Assert.Greater(forwardRay.z, 0.5f, "Pointer must point forward away from user.");
            Assert.Less(forwardRay.y, 0.0f, "Pointer must pitch downward away from user, not up towards eyes.");
        }

        [Test]
        public void VRPlayerRig_WristUI_IsElevatedAboveControllerDisc_WithoutOverlap()
        {
            var leftVisual = vrRig.LeftHandVisual;
            var wristUI = vrRig.WristOrderUI;

            Assert.IsNotNull(leftVisual);
            Assert.IsNotNull(wristUI);

            var anchor = leftVisual.WristAnchor;
            Assert.IsNotNull(anchor);

            // Anchor must be elevated above controller top disc (disc is at Y ~ 0.015m, anchor is at Y = 0.14m)
            Assert.GreaterOrEqual(anchor.localPosition.y, 0.12f, "Wrist anchor must be elevated at least 12cm above controller grip.");

            // Canvas pivot must be bottom edge (pivot.y == 0.0f) so the card projects upwards
            var canvasRect = wristUI.GetComponent<RectTransform>();
            Assert.IsNotNull(canvasRect);
            Assert.AreEqual(0.0f, canvasRect.pivot.y, 0.01f, "Canvas pivot Y must be 0 (bottom edge) so card extends upwards above the hand.");
        }

        [Test]
        public void VRLocomotion_RightThumbstick_RotatesCharacter()
        {
            var loco = vrRig.LocomotionController;
            var rightController = vrRig.RightController;

            Assert.IsNotNull(loco);
            Assert.IsNotNull(rightController);

            // Ensure right controller is connected as turn controller
            loco.SetReferences(vrRig.LeftController, vrRig.HmdCamera.transform, vrRig.CharacterController, rightController);
            Assert.AreEqual(rightController, loco.TurnController);

            float initialYRotation = vrRig.transform.eulerAngles.y;
            float initialDistance = loco.CumulativeTravelDistance;

            // 1. Simulate flicking Right Thumbstick to the Right (snap turn +45 deg)
            rightController.SimulateThumbstick(new Vector2(1.0f, 0f));
            // Run Update via Reflection or SimulateTurn
            loco.UseSnapTurn = true;
            loco.SnapTurnAngle = 45.0f;

            // Trigger turn update
            var updateMethod = typeof(VRLocomotionController).GetMethod("ProcessTurn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(updateMethod);
            updateMethod.Invoke(loco, null);

            float turnedYRotation = vrRig.transform.eulerAngles.y;
            float angleDelta = Mathf.DeltaAngle(initialYRotation, turnedYRotation);
            Assert.AreEqual(45.0f, angleDelta, 0.5f, "Right stick flick right must rotate character +45 degrees.");

            // 2. Return stick to center (debounce reset)
            rightController.SimulateThumbstick(Vector2.zero);
            updateMethod.Invoke(loco, null);

            // 3. Flick stick to the Left (snap turn -45 deg)
            rightController.SimulateThumbstick(new Vector2(-1.0f, 0f));
            updateMethod.Invoke(loco, null);

            float returnYRotation = vrRig.transform.eulerAngles.y;
            float returnDelta = Mathf.DeltaAngle(initialYRotation, returnYRotation);
            Assert.AreEqual(0.0f, returnDelta, 0.5f, "Flicking left must return character to initial angle.");

            // 4. Verify in-place rotation does NOT alter cumulative travel distance
            Assert.AreEqual(initialDistance, loco.CumulativeTravelDistance, 0.001f, "Turning in place must not accumulate travel distance.");
        }

        [Test]
        public void VRPlayerRig_WristUI_HasCleanMultiColumnLayout_AndDorsalWristAnchor()
        {
            var leftVisual = vrRig.LeftHandVisual;
            var wristUI = vrRig.WristOrderUI;

            Assert.IsNotNull(leftVisual);
            Assert.IsNotNull(wristUI);

            var anchor = leftVisual.WristAnchor;
            Assert.IsNotNull(anchor);

            // Anchor must be situated along dorsal wrist/forearm: behind grip (Z <= -0.05m) and elevated (Y >= 0.12m)
            Assert.LessOrEqual(anchor.localPosition.z, -0.05f, "Wrist anchor must sit behind the controller grip along the forearm.");
            Assert.GreaterOrEqual(anchor.localPosition.y, 0.12f, "Wrist anchor must be elevated above controller top face.");

            // Start an order with full-length SKU names
            orderManager.StartSpecificOrder("ORD-SCN-0001", new List<string> { "SKU-0001", "SKU-0002" });
            wristUI.RefreshOrderDisplay();

            // Verify Title and Subtitle match expected desktop format
            StringAssert.Contains("NEXT ORDER | ORD-SCN-0001", wristUI.TitleText);
            StringAssert.Contains("TOTAL ITEMS", wristUI.SubtitleText);

            // Verify separate column text objects exist
            var prodCol = wristUI.transform.Find("ProductColumn");
            var qtyCol = wristUI.transform.Find("QtyColumn");
            var aisleCol = wristUI.transform.Find("AisleColumn");

            Assert.IsNotNull(prodCol, "Structured ProductColumn must exist.");
            Assert.IsNotNull(qtyCol, "Structured QtyColumn must exist.");
            Assert.IsNotNull(aisleCol, "Structured AisleColumn must exist.");

            var prodText = prodCol.GetComponent<UnityEngine.UI.Text>();
            Assert.IsNotNull(prodText);
            StringAssert.Contains("Orange Juice Fresh 1L", prodText.text);
        }
    }
}
