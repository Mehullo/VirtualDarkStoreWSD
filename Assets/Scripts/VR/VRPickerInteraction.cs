using System;
using UnityEngine;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Interaction;
using VirtualDarkStore.Management;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VirtualDarkStore.VR
{
    /// <summary>
    /// Manages VR picker interaction with shelf SKU items, packing benches, and order collection stations.
    /// Maps 100% of desktop grabbing/picking interactions to the VR character.
    /// Supports head/camera gaze targeting (looking at grabable objects) and controller pointing rays.
    /// Drives the in-VR 3D Grab Prompt floating UI badge.
    /// </summary>
    public class VRPickerInteraction : MonoBehaviour, IPickerInteraction
    {
        [Header("Raycast Settings")]
        [Tooltip("Maximum picking reach in meters (standard arm reach + lean: 2.8m).")]
        [SerializeField] private float reachDistance = 2.8f;

        [Tooltip("Primary controller used for aiming rays.")]
        [SerializeField] private VRControllerInput primaryController;

        [Tooltip("Secondary controller for dual-hand picking.")]
        [SerializeField] private VRControllerInput secondaryController;

        [Tooltip("HMD camera used for gaze targeting.")]
        [SerializeField] private Camera fallbackCamera;

        [Tooltip("Floating in-VR grab prompt UI.")]
        [SerializeField] private VRGazePromptUI promptUI;

        [Tooltip("Layer mask for interactable items and stations.")]
        [SerializeField] private LayerMask interactionLayers = ~0;

        [Header("Runtime State")]
        [SerializeField] private PickableItemVisual hoveredItem;
        [SerializeField] private PackingStation hoveredPackingStation;
        [SerializeField] private GameObject hoveredCollectionDesk;
        [SerializeField] private string currentPrompt = string.Empty;
        [SerializeField] private Vector3 currentTargetWorldPosition = Vector3.zero;

        public PickableItemVisual HoveredItem => hoveredItem;
        public PackingStation HoveredPackingStation => hoveredPackingStation;
        public GameObject HoveredCollectionDesk => hoveredCollectionDesk;
        public string CurrentPrompt => currentPrompt;
        public float ReachDistance => reachDistance;

        public event Action<PickableItemVisual> OnPickAttempted;
        public event Action<bool> OnPackingAttempted;

        private void Awake()
        {
            if (primaryController == null)
            {
                var controllers = GetComponentsInChildren<VRControllerInput>();
                foreach (var c in controllers)
                {
                    if (c.Hand == VRHand.Right && primaryController == null) primaryController = c;
                    if (c.Hand == VRHand.Left && secondaryController == null) secondaryController = c;
                }
            }
            if (fallbackCamera == null)
            {
                fallbackCamera = GetComponentInChildren<Camera>();
                if (fallbackCamera == null) fallbackCamera = Camera.main;
            }
            if (promptUI == null)
            {
                promptUI = GetComponentInChildren<VRGazePromptUI>();
            }
        }

        private void OnEnable()
        {
            SubscribeController(primaryController);
            SubscribeController(secondaryController);
        }

        private void OnDisable()
        {
            UnsubscribeController(primaryController);
            UnsubscribeController(secondaryController);

            if (promptUI != null)
            {
                promptUI.HidePrompt();
            }
        }

        private void SubscribeController(VRControllerInput controller)
        {
            if (controller != null)
            {
                controller.OnSelectAction -= HandleSelectAction;
                controller.OnSelectAction += HandleSelectAction;
                controller.OnGrabAction -= HandleSelectAction;
                controller.OnGrabAction += HandleSelectAction;
            }
        }

        private void UnsubscribeController(VRControllerInput controller)
        {
            if (controller != null)
            {
                controller.OnSelectAction -= HandleSelectAction;
                controller.OnGrabAction -= HandleSelectAction;
            }
        }

        private void Update()
        {
            PerformRaycast();
            HandleKeyboardMouseInput();
        }

        private void HandleKeyboardMouseInput()
        {
            bool grabPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) grabPressed = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) grabPressed = true;
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) grabPressed = true;
            }
            catch
            {
                // Legacy input fallback
            }

            if (grabPressed)
            {
                ExecuteInteraction();
            }
        }

        public void PerformRaycast()
        {
            hoveredItem = null;
            hoveredPackingStation = null;
            hoveredCollectionDesk = null;
            currentPrompt = string.Empty;
            currentTargetWorldPosition = Vector3.zero;

            if (fallbackCamera == null)
            {
                fallbackCamera = GetComponentInChildren<Camera>();
                if (fallbackCamera == null) fallbackCamera = Camera.main;
            }

            // 1. Controller pointer ray (Primary / Right hand aiming ray)
            bool foundTarget = false;
            if (primaryController != null)
            {
                foundTarget = EvaluateRaycast(primaryController.PointerRay);
            }

            // 2. Head / Gaze ray (Looking directly at a grabable object in VR)
            // Suppress gaze raycast if player is currently looking at their wrist/hand checklist
            bool isLookingAtWrist = false;
            if (secondaryController != null && fallbackCamera != null)
            {
                Vector3 toHand = secondaryController.transform.position - fallbackCamera.transform.position;
                float distToHand = toHand.magnitude;
                if (distToHand < 0.75f && distToHand > 0.15f)
                {
                    float angle = Vector3.Angle(fallbackCamera.transform.forward, toHand);
                    if (angle < 28f)
                    {
                        isLookingAtWrist = true;
                    }
                }
            }

            if (!foundTarget && !isLookingAtWrist && fallbackCamera != null)
            {
                Ray gazeRay = fallbackCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                foundTarget = EvaluateRaycast(gazeRay);
            }

            // 3. Controller pointer ray (Secondary / Left hand aiming ray)
            if (!foundTarget && secondaryController != null)
            {
                foundTarget = EvaluateRaycast(secondaryController.PointerRay);
            }

            // 4. Update the in-VR floating 3D prompt badge
            if (promptUI != null)
            {
                if (foundTarget && !string.IsNullOrEmpty(currentPrompt))
                {
                    promptUI.ShowPrompt(currentPrompt, currentTargetWorldPosition);
                }
                else
                {
                    promptUI.HidePrompt();
                }
            }
        }

        private bool EvaluateRaycast(Ray ray)
        {
            var hits = Physics.RaycastAll(ray, reachDistance, interactionLayers);
            if (hits == null || hits.Length == 0) return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];

                // 1. Check for shelf SKU item
                var item = hit.collider.GetComponent<PickableItemVisual>();
                if (item != null && !item.IsPicked)
                {
                    hoveredItem = item;
                    currentTargetWorldPosition = item.transform.position;
                    currentPrompt = $"[Trigger / Grip] Grab: {item.SkuName} ({item.SkuId}) [{item.SlotId}]";
                    return true;
                }

                // 2. Check for packing station
                var station = hit.collider.GetComponentInParent<PackingStation>();
                if (station != null)
                {
                    hoveredPackingStation = station;
                    currentTargetWorldPosition = station.transform.position;
                    var orderManager = OrderManager.Instance;
                    if (orderManager != null && orderManager.IsAwaitingPacking)
                    {
                        currentPrompt = "[Trigger / Grip] Deliver to Staging & Finalize Order";
                    }
                    else if (orderManager != null && orderManager.HasActiveOrder)
                    {
                        currentPrompt = "Packing Station (Fulfill all pick list items first)";
                    }
                    else
                    {
                        currentPrompt = "Packing Station (No active order awaiting dispatch)";
                    }
                    return true;
                }

                // 3. Check for Order Collection Desk
                if (hit.collider.name.IndexOf("Collection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (hit.collider.transform.parent != null && hit.collider.transform.parent.name.IndexOf("Collection", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    hoveredCollectionDesk = hit.collider.gameObject;
                    currentTargetWorldPosition = hit.collider.transform.position;
                    var op = OperationalCycleManager.Instance;
                    if (op != null && op.CurrentStage == OperationalStage.CollectOrder)
                    {
                        currentPrompt = "[Trigger / Grip] Collect Pick Cart & Activate Order";
                    }
                    else
                    {
                        currentPrompt = "Order Collection Desk";
                    }
                    return true;
                }
            }

            return false;
        }

        private void HandleSelectAction()
        {
            ExecuteInteraction();
        }

        /// <summary>
        /// Executes the grab, pick, or pack interaction against the active order or operational stage.
        /// </summary>
        public bool ExecuteInteraction()
        {
            var orderManager = OrderManager.Instance;

            // Pick/Grab product from shelf
            if (hoveredItem != null && orderManager != null)
            {
                OnPickAttempted?.Invoke(hoveredItem);
                var result = orderManager.AttemptPick(hoveredItem.SkuId);
                if (result == PickResult.Success)
                {
                    hoveredItem.OnPicked();
                    hoveredItem = null;
                    if (promptUI != null) promptUI.HidePrompt();
                    return true;
                }
                return false;
            }

            // Deliver tote / finalize packing at packing bench
            if (hoveredPackingStation != null)
            {
                var op = OperationalCycleManager.Instance;
                if (op != null && (op.CurrentStage == OperationalStage.AwaitingPacking || op.CurrentStage == OperationalStage.DepositOrder))
                {
                    bool deposited = op.DepositOrder(out _);
                    if (deposited && promptUI != null) promptUI.HidePrompt();
                    return deposited;
                }
                else if (orderManager != null && orderManager.IsAwaitingPacking)
                {
                    bool packed = orderManager.CompletePacking(out _);
                    OnPackingAttempted?.Invoke(packed);
                    if (packed && promptUI != null) promptUI.HidePrompt();
                    return packed;
                }
            }

            // Order collection desk (advance to pick stage)
            if (hoveredCollectionDesk != null)
            {
                var op = OperationalCycleManager.Instance;
                if (op != null && op.CurrentStage == OperationalStage.CollectOrder)
                {
                    bool collected = op.CollectOrder(out _, out _);
                    if (collected && promptUI != null) promptUI.HidePrompt();
                    return collected;
                }
            }

            return false;
        }

        public void SetReferences(VRControllerInput controller, Camera cam = null)
        {
            UnsubscribeController(primaryController);
            primaryController = controller;
            if (cam != null) fallbackCamera = cam;
            SubscribeController(primaryController);
        }

        public void SetPromptUI(VRGazePromptUI ui)
        {
            promptUI = ui;
        }
    }
}
