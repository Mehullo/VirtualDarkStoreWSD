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
        [SerializeField] private float walkSpeed = 1.4f; // Standard industrial walking speed (m/s)
        [SerializeField] private float sprintSpeed = 3.5f; // Rapid transit sprint speed (m/s)
        [SerializeField] private float moveSpeed = 1.4f; // Current active speed
        [SerializeField] private bool isSprinting = false;
        [SerializeField] private float maxTeleportDistance = 10.0f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("References")]
        [SerializeField] private VRControllerInput moveController; // Usually left hand for move
        [SerializeField] private VRControllerInput turnController; // Right hand for rotation (VR best practice)
        [SerializeField] private Transform headTransform;
        [SerializeField] private CharacterController characterController;

        [Header("Turn Configuration (Right Thumbstick)")]
        [SerializeField] private bool enableTurn = true;
        [SerializeField] private bool useSnapTurn = true; // Snap turn (comfort standard) vs smooth turn
        [SerializeField] private float snapTurnAngle = 45.0f; // 45 degrees per snap
        [SerializeField] private float smoothTurnSpeed = 90.0f; // degrees per second
        [SerializeField] private float turnDeadzone = 0.5f;

        private bool snapTurnDebounce = false;

        [Header("Runtime State")]
        [SerializeField] private Vector3 lastRecordedPosition = Vector3.zero;
        [SerializeField] private float lastFrameDisplacement = 0f;
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private float cumulativeTravelDistance = 0f;

        public VRLocomotionMode Mode => mode;
        public float MoveSpeed => moveSpeed;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public bool IsSprinting => isSprinting;
        public VRControllerInput TurnController => turnController;
        public bool EnableTurn { get => enableTurn; set => enableTurn = value; }
        public bool UseSnapTurn { get => useSnapTurn; set => useSnapTurn = value; }
        public float SnapTurnAngle { get => snapTurnAngle; set => snapTurnAngle = value; }
        public float SmoothTurnSpeed { get => smoothTurnSpeed; set => smoothTurnSpeed = value; }
        public float LastFrameDisplacement => lastFrameDisplacement;
        public float CurrentSpeed => currentSpeed;
        public float CumulativeTravelDistance => cumulativeTravelDistance;

        public event Action<Vector3, Vector3, float> OnTeleported; // from, to, distance
        public event Action<float, float> OnMoved; // displacement, speed
        public event Action<float> OnTurned; // turned angle in degrees

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

            ProcessTurn();

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

            if (input.sqrMagnitude < 0.01f)
            {
                isSprinting = false;
                moveSpeed = walkSpeed;
                return;
            }

            // Determine if sprint is engaged (left stick click, left grip hold, or desktop shift key)
            bool sprintEngaged = false;
            if (moveController != null && (moveController.IsThumbstickClicked || moveController.IsGripPressed))
            {
                sprintEngaged = true;
            }
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed))
            {
                sprintEngaged = true;
            }
#endif
            try
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    sprintEngaged = true;
                }
            }
            catch
            {
                // Fallback in case legacy axis is disabled
            }

            isSprinting = sprintEngaged;
            moveSpeed = isSprinting ? sprintSpeed : walkSpeed;

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

        private void ProcessTurn()
        {
            if (!enableTurn) return;

            Vector2 turnInput = Vector2.zero;
            if (turnController != null)
            {
                turnInput = turnController.Thumbstick;
            }

            // Keyboard testing fallback (Left/Right arrow keys)
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed) turnInput.x = -1f;
                if (UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed) turnInput.x = 1f;
            }
#endif
            try
            {
                if (Input.GetKey(KeyCode.LeftArrow)) turnInput.x = -1f;
                if (Input.GetKey(KeyCode.RightArrow)) turnInput.x = 1f;
            }
            catch
            {
                // Fallback in case legacy axis is disabled
            }

            if (useSnapTurn)
            {
                if (Mathf.Abs(turnInput.x) > turnDeadzone)
                {
                    if (!snapTurnDebounce)
                    {
                        float turnDir = Mathf.Sign(turnInput.x);
                        float angle = turnDir * snapTurnAngle;
                        transform.Rotate(0f, angle, 0f, Space.World);
                        snapTurnDebounce = true;
                        OnTurned?.Invoke(angle);
                    }
                }
                else if (Mathf.Abs(turnInput.x) < 0.2f)
                {
                    snapTurnDebounce = false;
                }
            }
            else
            {
                if (Mathf.Abs(turnInput.x) > 0.1f)
                {
                    float angle = turnInput.x * smoothTurnSpeed * Time.deltaTime;
                    transform.Rotate(0f, angle, 0f, Space.World);
                    OnTurned?.Invoke(angle);
                }
            }
        }

        public void SimulateTurn(float turnAngle)
        {
            transform.Rotate(0f, turnAngle, 0f, Space.World);
            OnTurned?.Invoke(turnAngle);
        }

        public void SetSprintSpeed(float speed)
        {
            sprintSpeed = speed;
        }

        public void SetWalkSpeed(float speed)
        {
            walkSpeed = speed;
        }

        public void SimulateSprint(bool sprinting)
        {
            isSprinting = sprinting;
            moveSpeed = isSprinting ? sprintSpeed : walkSpeed;
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

        public void SetReferences(VRControllerInput controller, Transform head, CharacterController cc = null, VRControllerInput turnCtrl = null)
        {
            moveController = controller;
            turnController = turnCtrl;
            headTransform = head;
            if (cc != null) characterController = cc;
        }
    }
}
