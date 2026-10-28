using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VirtualDarkStore.Player
{
    /// <summary>
    /// First-person desktop controller for dark-store warehouse picker.
    /// Handles locomotion, mouse look, collision with shelves and walls, and gravity.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopPickerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Walk speed in meters per second (standard industrial walking pace: ~1.2 - 1.4 m/s).")]
        [SerializeField] private float walkSpeed = 2.5f;

        [Tooltip("Sprint speed in meters per second for rapid transit between warehouse zones.")]
        [SerializeField] private float sprintSpeed = 4.5f;

        [Tooltip("Acceleration / deceleration smoothing.")]
        [SerializeField] private float acceleration = 12f;

        [Tooltip("Gravity acceleration in m/s^2.")]
        [SerializeField] private float gravity = -18f;

        [Header("Look Settings")]
        [Tooltip("Mouse look sensitivity.")]
        [SerializeField] private float mouseSensitivity = 1.5f;

        [Tooltip("Minimum vertical look angle (looking down).")]
        [SerializeField] private float minPitchAngle = -85f;

        [Tooltip("Maximum vertical look angle (looking up).")]
        [SerializeField] private float maxPitchAngle = 85f;

        [Header("References")]
        [Tooltip("First-person camera transform for vertical pitch rotation.")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController characterController;
        private Vector3 currentVelocity;
        private Vector3 targetMoveVelocity;
        private float verticalVelocity;
        private float cameraPitch;
        private bool isCursorLocked = true;

        public CharacterController Controller => characterController;
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public Vector3 Velocity => currentVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cameraTransform = cam.transform;
                }
            }
        }

        private void Start()
        {
            SetCursorLock(true);
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleLook();
            HandleMovement();
        }

        private void HandleCursorToggle()
        {
            // Toggle cursor lock with Escape key
            bool escapePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escapePressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                escapePressed = true;
            }
#endif
            if (escapePressed)
            {
                SetCursorLock(!isCursorLocked);
            }

            // Click into game window to lock cursor again
#if ENABLE_INPUT_SYSTEM
            if (!isCursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                SetCursorLock(true);
            }
#else
            if (!isCursorLocked && Input.GetMouseButtonDown(0))
            {
                SetCursorLock(true);
            }
#endif
        }

        public void SetCursorLock(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleLook()
        {
            if (!isCursorLocked) return;

            Vector2 lookInput = GetLookInput();
            float mouseX = lookInput.x * mouseSensitivity * 0.1f;
            float mouseY = lookInput.y * mouseSensitivity * 0.1f;

            // Rotate player body horizontally (yaw)
            transform.Rotate(Vector3.up * mouseX);

            // Rotate camera vertically (pitch)
            if (cameraTransform != null)
            {
                cameraPitch -= mouseY;
                cameraPitch = Mathf.Clamp(cameraPitch, minPitchAngle, maxPitchAngle);
                cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            Vector2 moveInput = GetMoveInput();
            bool isSprinting = GetSprintInput();

            float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

            // Calculate movement direction relative to player body orientation
            Vector3 targetDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            targetMoveVelocity = targetDir * (moveInput.magnitude > 0.05f ? targetSpeed : 0f);

            // Smooth horizontal movement
            currentVelocity = Vector3.MoveTowards(
                new Vector3(currentVelocity.x, 0f, currentVelocity.z),
                targetMoveVelocity,
                acceleration * Time.deltaTime
            );

            // Handle gravity and vertical velocity
            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    // Small downward force to stay glued to uneven floor / ramps
                    verticalVelocity = -2f;
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 finalMove = new Vector3(currentVelocity.x, verticalVelocity, currentVelocity.z);
            characterController.Move(finalMove * Time.deltaTime);
        }

        private Vector2 GetMoveInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
            }
#endif

            // Fallback if Input System returned 0 or wasn't active
            if (input == Vector2.zero)
            {
                try
                {
                    input.x = Input.GetAxisRaw("Horizontal");
                    input.y = Input.GetAxisRaw("Vertical");
                }
                catch
                {
                    // Legacy input axis not configured; ignore
                }
            }

            return input;
        }

        private Vector2 GetLookInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                input = Mouse.current.delta.ReadValue();
            }
#endif

            if (input == Vector2.zero)
            {
                try
                {
                    input.x = Input.GetAxis("Mouse X") * 10f;
                    input.y = Input.GetAxis("Mouse Y") * 10f;
                }
                catch
                {
                    // Legacy input axis not configured; ignore
                }
            }

            return input;
        }

        private bool GetSprintInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            {
                return true;
            }
#endif
            try
            {
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            catch
            {
                return false;
            }
        }
    }
}
