using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

namespace VirtualDarkStore.VR
{
    public enum VRHand
    {
        Left,
        Right
    }

    /// <summary>
    /// Reads spatial tracking and input controls for one VR controller hand (OpenXR / Input System / InputDevices).
    /// Supports both Trigger Pick and Grip Grab actions, as well as Thumbstick Click for sprint.
    /// </summary>
    public class VRControllerInput : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VRHand hand = VRHand.Right;

        [Header("State")]
        [SerializeField] private bool isTracked = false;
        [SerializeField] private float triggerValue = 0f;
        [SerializeField] private float gripValue = 0f;
        [SerializeField] private bool isTriggerPressed = false;
        [SerializeField] private bool isGripPressed = false;
        [SerializeField] private bool isGrabPressed = false;
        [SerializeField] private bool isThumbstickClicked = false;
        [SerializeField] private bool isPrimaryButtonPressed = false;
        [SerializeField] private bool isSecondaryButtonPressed = false;
        [SerializeField] private Vector2 thumbstick = Vector2.zero;

        public VRHand Hand => hand;
        public bool IsTracked => isTracked;
        public float TriggerValue => triggerValue;
        public float GripValue => gripValue;
        public bool IsTriggerPressed => isTriggerPressed;
        public bool IsGripPressed => isGripPressed;
        public bool IsGrabPressed => isGrabPressed;
        public bool IsThumbstickClicked => isThumbstickClicked;
        public bool IsPrimaryButtonPressed => isPrimaryButtonPressed;
        public bool IsSecondaryButtonPressed => isSecondaryButtonPressed;
        public Vector2 Thumbstick => thumbstick;

        [Header("Ergonomic Pointer & Visual Angle")]
        [Tooltip("Forward pitch offset (in degrees away from user) to align grip pose with ergonomic pointing direction.")]
        [SerializeField] private float forwardAngleOffset = 50f;

        public float ForwardAngleOffset
        {
            get => forwardAngleOffset;
            set => forwardAngleOffset = value;
        }

        public Vector3 ControllerPosition => transform.position;
        public Quaternion ControllerRotation => transform.rotation;
        public Vector3 PointerForward => (transform.rotation * Quaternion.Euler(forwardAngleOffset, 0f, 0f)) * Vector3.forward;
        public Ray PointerRay => new Ray(transform.position, PointerForward);

        public event Action OnSelectAction; // Trigger or Grip click (pick/grab)
        public event Action OnGrabAction;   // Explicit Grab (Grip or Trigger)

        private void Update()
        {
            PollController();
        }

        public void PollController()
        {
            bool wasGrab = isGrabPressed;
            bool deviceFound = false;

#if ENABLE_INPUT_SYSTEM
            var controller = hand == VRHand.Right
                ? InputSystem.GetDevice<XRController>("{RightHand}")
                : InputSystem.GetDevice<XRController>("{LeftHand}");

            if (controller != null && controller.added)
            {
                deviceFound = true;
                isTracked = controller.trackingState.ReadValue() != 0;
                if (isTracked)
                {
                    transform.localPosition = controller.devicePosition.ReadValue();
                    transform.localRotation = controller.deviceRotation.ReadValue();
                }

                var trig = controller.TryGetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("trigger");
                if (trig != null) triggerValue = trig.ReadValue();

                var grip = controller.TryGetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("grip");
                if (grip != null) gripValue = grip.ReadValue();

                var stick = controller.TryGetChildControl<UnityEngine.InputSystem.Controls.Vector2Control>("thumbstick");
                if (stick != null) thumbstick = stick.ReadValue();

                var stickClick = controller.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("thumbstickClicked")
                    ?? controller.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primary2DAxisClick");
                if (stickClick != null) isThumbstickClicked = stickClick.isPressed;

                var primBtn = controller.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton");
                if (primBtn != null) isPrimaryButtonPressed = primBtn.isPressed;

                var secBtn = controller.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("secondaryButton");
                if (secBtn != null) isSecondaryButtonPressed = secBtn.isPressed;
            }
#endif

            // Fallback / Supplementary: UnityEngine.XR.InputDevices (guarantees Oculus Link / OpenXR device polling)
            if (!deviceFound || !isTracked)
            {
                var node = hand == VRHand.Right ? UnityEngine.XR.XRNode.RightHand : UnityEngine.XR.XRNode.LeftHand;
                var xrDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
                if (xrDevice.isValid)
                {
                    isTracked = true;
                    if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 dPos)) transform.localPosition = dPos;
                    if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion dRot)) transform.localRotation = dRot;
                    if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float tVal)) triggerValue = tVal;
                    if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gVal)) gripValue = gVal;
                    if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 sVal)) thumbstick = sVal;
                    if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out bool sClick)) isThumbstickClicked = sClick;
                    if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool pBtn)) isPrimaryButtonPressed = pBtn;
                    if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool secBtn)) isSecondaryButtonPressed = secBtn;
                }
            }

            isTriggerPressed = triggerValue > 0.5f;
            isGripPressed = gripValue > 0.5f;
            isGrabPressed = isTriggerPressed || isGripPressed;

            if (!wasGrab && isGrabPressed)
            {
                OnSelectAction?.Invoke();
                OnGrabAction?.Invoke();
            }
        }

        /// <summary>
        /// Programmatically injects a trigger click (useful for EditMode/PlayMode testing).
        /// </summary>
        public void SimulateTriggerPress()
        {
            triggerValue = 1.0f;
            isTriggerPressed = true;
            isGrabPressed = true;
            OnSelectAction?.Invoke();
            OnGrabAction?.Invoke();
        }

        public void SimulateTriggerRelease()
        {
            triggerValue = 0f;
            isTriggerPressed = false;
            isGrabPressed = isGripPressed;
        }

        public void SimulateGripPress()
        {
            gripValue = 1.0f;
            isGripPressed = true;
            isGrabPressed = true;
            OnSelectAction?.Invoke();
            OnGrabAction?.Invoke();
        }

        public void SimulateGripRelease()
        {
            gripValue = 0f;
            isGripPressed = false;
            isGrabPressed = isTriggerPressed;
        }

        public void SimulateThumbstick(Vector2 stick)
        {
            thumbstick = stick;
        }

        public void SimulateThumbstickClick(bool pressed)
        {
            isThumbstickClicked = pressed;
        }

        public void SetSimulatedPose(Vector3 localPos, Quaternion localRot)
        {
            transform.localPosition = localPos;
            transform.localRotation = localRot;
            isTracked = true;
        }
    }
}
