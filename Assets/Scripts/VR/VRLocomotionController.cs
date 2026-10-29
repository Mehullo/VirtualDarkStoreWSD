using System;
using UnityEngine;

namespace VirtualDarkStore.VR
{
    public enum VRLocomotionMode
    {
        ContinuousMove,
        Teleportation
    }

    /// <summary>
    /// Handles VR player locomotion via continuous thumbstick movement and discrete teleportation.
    /// Strictly measures horizontal displacement to ensure work-study travel metrics remain faithful.
    /// </summary>
    public class VRLocomotionController : MonoBehaviour
    {
        [Header("Locomotion Configuration")]
        [SerializeField] private VRLocomotionMode mode = VRLocomotionMode.ContinuousMove;
        [SerializeField] private float moveSpeed = 1.4f; // Standard industrial walking speed (m/s)
        [SerializeField] private float maxTeleportDistance = 10.0f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("References")]
        [SerializeField] private VRControllerInput moveController; // Usually left hand for move
        [SerializeField] private Transform headTransform;
        [SerializeField] private CharacterController characterController;

        [Header("Runtime State")]
        [SerializeField] private Vector3 lastRecordedPosition = Vector3.zero;
        [SerializeField] private float lastFrameDisplacement = 0f;
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private float cumulativeTravelDistance = 0f;

        public VRLocomotionMode Mode => mode;
        public float MoveSpeed => moveSpeed;
        public float LastFrameDisplacement => lastFrameDisplacement;
        public float CurrentSpeed => currentSpeed;
        public float CumulativeTravelDistance => cumulativeTravelDistance;

        public event Action<Vector3, Vector3, float> OnTeleported; // from, to, distance
        public event Action<float, float> OnMoved; // displacement, speed

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
            lastRecordedPosition = transform.position;
        }

        private void Update()
        {
            Vector3 previousPos = transform.position;

            if (mode == VRLocomotionMode.ContinuousMove)
            {
                ProcessContinuousMovement();
            }

            Vector3 currentPos = transform.position;
            Vector3 horizontalDelta = new Vector3(currentPos.x - previousPos.x, 0f, currentPos.z - previousPos.z);
            lastFrameDisplacement = horizontalDelta.magnitude;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            currentSpeed = lastFrameDisplacement / dt;

            if (lastFrameDisplacement > 0f)
            {
                cumulativeTravelDistance += lastFrameDisplacement;
                OnMoved?.Invoke(lastFrameDisplacement, currentSpeed);
            }

            lastRecordedPosition = currentPos;
        }

        private void ProcessContinuousMovement()
        {
            Vector2 input = Vector2.zero;
            if (moveController != null)
            {
                input = moveController.Thumbstick;
            }

            if (input.sqrMagnitude < 0.01f) return;

            Vector3 forward = headTransform != null ? headTransform.forward : transform.forward;
            Vector3 right = headTransform != null ? headTransform.right : transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * input.y + right * input.x).normalized;
            Vector3 motion = moveDirection * (moveSpeed * Time.deltaTime);

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(motion);
            }
            else
            {
                transform.position += motion;
            }
        }

        /// <summary>
        /// Executes an instantaneous teleportation to the target world position.
        /// Crucially measures and accumulates the exact travel displacement so work-study metrics are preserved.
        /// </summary>
        public bool TeleportTo(Vector3 targetPosition)
        {
            Vector3 fromPos = transform.position;
            Vector3 targetFloorPos = new Vector3(targetPosition.x, fromPos.y, targetPosition.z);

            Vector3 displacementVector = new Vector3(targetFloorPos.x - fromPos.x, 0f, targetFloorPos.z - fromPos.z);
            float distance = displacementVector.magnitude;

            if (distance > maxTeleportDistance)
            {
                Debug.LogWarning($"[VRLocomotionController] Teleport rejected: Distance {distance:F2}m exceeds max {maxTeleportDistance:F2}m.");
                return false;
            }

            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = targetFloorPos;
                characterController.enabled = true;
            }
            else
            {
                transform.position = targetFloorPos;
            }

            lastFrameDisplacement = distance;
            cumulativeTravelDistance += distance;
            // When teleporting, speed over a standard transition interval (~0.2s) reflects the transit
            currentSpeed = distance / 0.2f;

            OnTeleported?.Invoke(fromPos, targetFloorPos, distance);
            OnMoved?.Invoke(distance, currentSpeed);

            lastRecordedPosition = targetFloorPos;
            return true;
        }

        public void SetMode(VRLocomotionMode newMode)
        {
            mode = newMode;
        }

        public void SetReferences(VRControllerInput controller, Transform head, CharacterController cc = null)
        {
            moveController = controller;
            headTransform = head;
            if (cc != null) characterController = cc;
        }
    }
}
