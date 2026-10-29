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
    /// Reads spatial tracking and input controls for one VR controller hand (OpenXR / Input System).
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
        [SerializeField] private bool isPrimaryButtonPressed = false;
        [SerializeField] private Vector2 thumbstick = Vector2.zero;

        public VRHand Hand => hand;
        public bool IsTracked => isTracked;
        public float TriggerValue => triggerValue;
        public float GripValue => gripValue;
        public bool IsTriggerPressed => isTriggerPressed;
        public bool IsGripPressed => isGripPressed;
        public bool IsPrimaryButtonPressed => isPrimaryButtonPressed;
        public Vector2 Thumbstick => thumbstick;

        public Vector3 ControllerPosition => transform.position;
        public Quaternion ControllerRotation => transform.rotation;
        public Vector3 PointerForward => transform.forward;
        public Ray PointerRay => new Ray(transform.position, transform.forward);

        public event Action OnSelectAction; // Trigger or Grip click

        private void Update()
        {
            PollController();
        }

        public void PollController()
        {
#if ENABLE_INPUT_SYSTEM
            var controller = hand == VRHand.Right
                ? InputSystem.GetDevice<XRController>("{RightHand}")
                : InputSystem.GetDevice<XRController>("{LeftHand}");

            if (controller != null && controller.added)
            {
                isTracked = controller.trackingState.ReadValue() != 0;
                if (isTracked)
                {
                    transform.localPosition = controller.devicePosition.ReadValue();
                    transform.localRotation = controller.deviceRotation.ReadValue();
                }

                var trig = controller.TryGetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("trigger");
                if (trig != null)
                {
                    triggerValue = trig.ReadValue();
                }

                var grip = controller.TryGetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("grip");
                if (grip != null)
                {
                    gripValue = grip.ReadValue();
                }

                var stick = controller.TryGetChildControl<UnityEngine.InputSystem.Controls.Vector2Control>("thumbstick");
                if (stick != null)
                {
                    thumbstick = stick.ReadValue();
                }

                bool prevTrigger = isTriggerPressed;
                isTriggerPressed = triggerValue > 0.5f;
                isGripPressed = gripValue > 0.5f;

                if (!prevTrigger && isTriggerPressed)
                {
                    OnSelectAction?.Invoke();
                }
            }
#endif
        }

        /// <summary>
        /// Programmatically injects a trigger click (useful for EditMode/PlayMode testing).
        /// </summary>
        public void SimulateTriggerPress()
        {
            triggerValue = 1.0f;
            isTriggerPressed = true;
            OnSelectAction?.Invoke();
        }

        public void SimulateTriggerRelease()
        {
            triggerValue = 0f;
            isTriggerPressed = false;
        }

        public void SimulateThumbstick(Vector2 stick)
        {
            thumbstick = stick;
        }

        public void SetSimulatedPose(Vector3 localPos, Quaternion localRot)
        {
            transform.localPosition = localPos;
            transform.localRotation = localRot;
            isTracked = true;
        }
    }
}
