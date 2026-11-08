using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.XR.CoreUtils;
using VirtualDarkStore.Interaction;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VirtualDarkStore.VR
{
    /// <summary>
    /// Master VR player rig component managing the XROrigin hierarchy, spatial tracking,
    /// dual controller inputs, pick/grab interactions, locomotion, and level camera alignment.
    /// Provides identical telemetry interface to the shared WorkStudy and ActivityClassifier core.
    /// </summary>
    public class VRPlayerRig : MonoBehaviour
    {
        public static readonly Vector3 DefaultWarehouseStartPoint = new Vector3(0f, 0f, -8.5f);

        [Header("Hierarchy References")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Transform cameraOffset;
        [SerializeField] private Camera hmdCamera;
        [SerializeField] private VRHeadTracking headTracking;
        [SerializeField] private VRControllerInput leftController;
        [SerializeField] private VRControllerInput rightController;
        [SerializeField] private VRHandVisual leftHandVisual;
        [SerializeField] private VRHandVisual rightHandVisual;
        [SerializeField] private VRNextOrderWristUI wristOrderUI;
        [SerializeField] private VRGazePromptUI gazePromptUI;
        [SerializeField] private VRPickerInteraction pickerInteraction;
        [SerializeField] private VRLocomotionController locomotionController;
        [SerializeField] private CharacterController characterController;

        public XROrigin XrOrigin => xrOrigin;
        public Camera HmdCamera => hmdCamera;
        public VRHeadTracking HeadTracking => headTracking;
        public VRControllerInput LeftController => leftController;
        public VRControllerInput RightController => rightController;
        public VRHandVisual LeftHandVisual => leftHandVisual;
        public VRHandVisual RightHandVisual => rightHandVisual;
        public VRNextOrderWristUI WristOrderUI => wristOrderUI;
        public VRGazePromptUI GazePromptUI => gazePromptUI;
        public VRPickerInteraction PickerInteraction => pickerInteraction;
        public VRLocomotionController LocomotionController => locomotionController;
        public CharacterController CharacterController => characterController;

        public Vector3 FloorPosition => new Vector3(transform.position.x, 0f, transform.position.z);
        public Vector3 HeadPosition => headTracking != null ? headTracking.HeadPosition : (hmdCamera != null ? hmdCamera.transform.position : transform.position);
        public Vector3 ForwardDirection
        {
            get
            {
                Vector3 fwd = headTracking != null ? headTracking.Forward : transform.forward;
                fwd.y = 0f;
                return fwd.sqrMagnitude > 0.001f ? fwd.normalized : Vector3.forward;
            }
        }

        private void Awake()
        {
            InitializeRig();
        }

        private void OnEnable()
        {
            ConfigureActiveCamera(true);
        }

        private void OnDisable()
        {
            ConfigureActiveCamera(false);
        }

        private void Update()
        {
            // Optional desktop testing shortcut: press [V] to toggle between Desktop and VR rig on the laptop
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                ToggleDesktopAndVR();
            }
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.V))
                {
                    ToggleDesktopAndVR();
                }
            }
            catch
            {
                // Legacy input axis disabled
            }
        }

        public void InitializeRig()
        {
            // If the rig was placed at world center (0, 0, 0), align it with the warehouse operational starting point
            if (transform.position == Vector3.zero)
            {
                transform.position = DefaultWarehouseStartPoint;
                transform.rotation = Quaternion.identity;
            }

            if (xrOrigin == null) xrOrigin = GetComponent<XROrigin>() ?? gameObject.AddComponent<XROrigin>();
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>() ?? gameObject.AddComponent<CharacterController>();
                characterController.radius = 0.35f;
                characterController.height = 1.8f;
                characterController.center = new Vector3(0f, 0.9f, 0f);
                characterController.slopeLimit = 45f;
                characterController.stepOffset = 0.3f;
                characterController.skinWidth = 0.08f;
                characterController.minMoveDistance = 0.001f;
            }

            if (cameraOffset == null)
            {
                var offset = transform.Find("Camera Floor Offset") ?? transform.Find("CameraOffset") ?? transform.Find("Camera Offset");
                if (offset != null) cameraOffset = offset;
            }

            if (headTracking == null) headTracking = GetComponentInChildren<VRHeadTracking>();
            if (hmdCamera == null && headTracking != null) hmdCamera = headTracking.HmdCamera;
            if (hmdCamera == null) hmdCamera = GetComponentInChildren<Camera>();
            if (hmdCamera != null)
            {
                if (headTracking == null) headTracking = hmdCamera.GetComponent<VRHeadTracking>() ?? hmdCamera.gameObject.AddComponent<VRHeadTracking>();
                var urpCam = hmdCamera.GetComponent<UniversalAdditionalCameraData>() ?? hmdCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                if (urpCam != null) urpCam.renderPostProcessing = false;
            }

            if (pickerInteraction == null) pickerInteraction = GetComponentInChildren<VRPickerInteraction>() ?? gameObject.AddComponent<VRPickerInteraction>();
            if (locomotionController == null) locomotionController = GetComponentInChildren<VRLocomotionController>() ?? gameObject.AddComponent<VRLocomotionController>();

            var controllers = GetComponentsInChildren<VRControllerInput>();
            foreach (var c in controllers)
            {
                if (c.Hand == VRHand.Left && leftController == null) leftController = c;
                if (c.Hand == VRHand.Right && rightController == null) rightController = c;
            }

            // Also support standard XR Origin Hands child names: "Left Hand" and "Right Hand"
            if (leftController == null)
            {
                Transform leftT = transform.Find("Camera Floor Offset/Left Hand")
                    ?? transform.Find("CameraOffset/Left Hand")
                    ?? transform.Find("Camera Floor Offset/LeftHandController")
                    ?? transform.Find("CameraOffset/LeftHandController");
                if (leftT == null)
                {
                    foreach (var t in GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.Equals("Left Hand", StringComparison.OrdinalIgnoreCase) ||
                            t.name.Equals("LeftHandController", StringComparison.OrdinalIgnoreCase))
                        {
                            leftT = t;
                            break;
                        }
                    }
                }
                if (leftT != null)
                {
                    leftController = leftT.GetComponent<VRControllerInput>() ?? leftT.gameObject.AddComponent<VRControllerInput>();
                    var handField = typeof(VRControllerInput).GetField("hand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (handField != null) handField.SetValue(leftController, VRHand.Left);
                }
            }

            if (rightController == null)
            {
                Transform rightT = transform.Find("Camera Floor Offset/Right Hand")
                    ?? transform.Find("CameraOffset/Right Hand")
                    ?? transform.Find("Camera Floor Offset/RightHandController")
                    ?? transform.Find("CameraOffset/RightHandController");
                if (rightT == null)
                {
                    foreach (var t in GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.Equals("Right Hand", StringComparison.OrdinalIgnoreCase) ||
                            t.name.Equals("RightHandController", StringComparison.OrdinalIgnoreCase))
                        {
                            rightT = t;
                            break;
                        }
                    }
                }
                if (rightT != null)
                {
                    rightController = rightT.GetComponent<VRControllerInput>() ?? rightT.gameObject.AddComponent<VRControllerInput>();
                    var handField = typeof(VRControllerInput).GetField("hand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (handField != null) handField.SetValue(rightController, VRHand.Right);
                }
            }

            // Wire XROrigin connections & floor origin height
            if (xrOrigin != null)
            {
                if (hmdCamera != null) xrOrigin.Camera = hmdCamera;
                if (cameraOffset != null) xrOrigin.CameraFloorOffsetObject = cameraOffset.gameObject;
                xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
                xrOrigin.CameraYOffset = 1.68f;
            }

            // Fix camera angle: Ensure zero pitch/roll tilt on camera offset so VR horizon is level
            if (cameraOffset != null)
            {
                cameraOffset.localRotation = Quaternion.identity;
            }

            // 1. Ensure visible hands/controllers in VR environment
            if (leftController != null)
            {
                leftHandVisual = leftController.GetComponent<VRHandVisual>() ?? leftController.gameObject.AddComponent<VRHandVisual>();
                leftHandVisual.Initialize(VRHand.Left);
            }
            if (rightController != null)
            {
                rightHandVisual = rightController.GetComponent<VRHandVisual>() ?? rightController.gameObject.AddComponent<VRHandVisual>();
                rightHandVisual.Initialize(VRHand.Right);
            }

            // 2. Ensure in-VR Gaze Prompt UI is created and wired
            if (gazePromptUI == null)
            {
                gazePromptUI = GetComponentInChildren<VRGazePromptUI>();
            }
            if (gazePromptUI == null && hmdCamera != null)
            {
                var promptObj = new GameObject("[VRGazePromptUI]");
                promptObj.transform.SetParent(transform, false);
                gazePromptUI = promptObj.AddComponent<VRGazePromptUI>();
                gazePromptUI.SetHmdCamera(hmdCamera);
            }

            // 3. Ensure Next Order Checklist is mounted on left hand/wrist
            if (wristOrderUI == null)
            {
                wristOrderUI = GetComponentInChildren<VRNextOrderWristUI>();
            }

            var leftAnchor = leftHandVisual != null && leftHandVisual.WristAnchor != null
                ? leftHandVisual.WristAnchor
                : (leftController != null ? leftController.transform : null);

            if (wristOrderUI != null && leftAnchor != null)
            {
                wristOrderUI.SetWristAnchor(leftAnchor);
            }
            else if (wristOrderUI == null && leftAnchor != null)
            {
                var uiObj = new GameObject("[VRNextOrderWristUI]");
                uiObj.transform.SetParent(leftAnchor, false);
                uiObj.transform.localPosition = Vector3.zero;
                uiObj.transform.localRotation = Quaternion.identity;
                wristOrderUI = uiObj.AddComponent<VRNextOrderWristUI>();
                wristOrderUI.SetWristAnchor(leftAnchor);
            }

            if (locomotionController != null)
            {
                locomotionController.SetReferences(
                    leftController != null ? leftController : rightController,
                    hmdCamera != null ? hmdCamera.transform : transform,
                    characterController,
                    rightController != null ? rightController : null);
            }

            if (pickerInteraction != null)
            {
                pickerInteraction.SetReferences(
                    rightController != null ? rightController : leftController,
                    hmdCamera);

                if (gazePromptUI != null)
                {
                    pickerInteraction.SetPromptUI(gazePromptUI);
                }
            }

            if (gameObject.activeInHierarchy)
            {
                ConfigureActiveCamera(true);
            }
        }

        private void ConfigureActiveCamera(bool isVrActive)
        {
            if (hmdCamera != null)
            {
                if (isVrActive)
                {
                    hmdCamera.tag = "MainCamera";
                    var listener = hmdCamera.GetComponent<AudioListener>();
                    if (listener == null) listener = hmdCamera.gameObject.AddComponent<AudioListener>();
                    listener.enabled = true;

                    // Mute desktop camera listeners to avoid duplicate AudioListener warnings
                    var allListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
                    foreach (var l in allListeners)
                    {
                        if (l != listener && !l.transform.IsChildOf(transform))
                        {
                            l.enabled = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Moves the entire rig to the specified warehouse spawn point and resets rotation to level horizon.
        /// </summary>
        public void TeleportToSpawnPoint(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (characterController != null) characterController.enabled = false;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;

            if (cameraOffset != null)
            {
                cameraOffset.localRotation = Quaternion.identity;
            }

            if (characterController != null) characterController.enabled = true;
        }

        private void ToggleDesktopAndVR()
        {
            var desktop = FindAnyObjectByType<Player.DesktopPickerController>(FindObjectsInactive.Include);
            if (desktop == null) return;

            bool vrNextState = !gameObject.activeSelf;
            gameObject.SetActive(vrNextState);
            desktop.gameObject.SetActive(!vrNextState);

            if (vrNextState)
            {
                TeleportToSpawnPoint(desktop.transform.position, desktop.transform.rotation);
                Debug.Log("[VRPlayerRig] Switched to Meta Quest VR Rig mode.");
            }
            else
            {
                var cc = desktop.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                desktop.transform.position = transform.position;
                desktop.transform.rotation = transform.rotation;
                if (cc != null) cc.enabled = true;
                Debug.Log("[VRPlayerRig] Switched to Desktop Picker mode.");
            }
        }
    }
}
