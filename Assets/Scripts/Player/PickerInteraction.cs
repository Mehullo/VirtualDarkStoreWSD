using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VirtualDarkStore.Environment;
using VirtualDarkStore.Interaction;
using VirtualDarkStore.Management;

namespace VirtualDarkStore.Player
{
    /// <summary>
    /// Component managing picker raycast interaction with physical shelf SKUs and packing benches.
    /// Handles first-person center-screen reticle raycasting and [E] / LMB pick triggers.
    /// </summary>
    public class PickerInteraction : MonoBehaviour, IPickerInteraction
    {
        [Header("Raycast Settings")]
        [Tooltip("Maximum picking reach in meters (standard arm reach + lean: 2.8m).")]
        [SerializeField] private float reachDistance = 2.8f;

        [Tooltip("Camera used for center-screen raycast. If null, will search in children.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Layer mask for interactable items and stations.")]
        [SerializeField] private LayerMask interactionLayers = ~0;

        [Header("State")]
        [SerializeField] private PickableItemVisual hoveredItem;
        [SerializeField] private PackingStation hoveredPackingStation;
        [SerializeField] private GameObject hoveredCollectionDesk;
        [SerializeField] private string currentPrompt = string.Empty;

        public PickableItemVisual HoveredItem => hoveredItem;
        public PackingStation HoveredPackingStation => hoveredPackingStation;
        public GameObject HoveredCollectionDesk => hoveredCollectionDesk;
        public string CurrentPrompt => currentPrompt;
        public float ReachDistance => reachDistance;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null) playerCamera = Camera.main;
            }
        }

        private void Update()
        {
            PerformRaycast();
            HandleInput();
        }

        private void PerformRaycast()
        {
            hoveredItem = null;
            hoveredPackingStation = null;
            hoveredCollectionDesk = null;
            currentPrompt = string.Empty;

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null) playerCamera = Camera.main;
            }

            Ray ray = (playerCamera != null)
                ? new Ray(playerCamera.transform.position, playerCamera.transform.forward)
                : new Ray(transform.position + Vector3.up * 1.6f, transform.forward);

            var hits = Physics.RaycastAll(ray, reachDistance, interactionLayers);
            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                for (int i = 0; i < hits.Length; i++)
                {
                    var hit = hits[i];

                    // 1. Check for shelf SKU item
                    var item = hit.collider.GetComponent<PickableItemVisual>();
                    if (item != null && !item.IsPicked)
                    {
                        hoveredItem = item;
                        currentPrompt = $"[E / LMB] Pick: {item.SkuName} ({item.SkuId}) [{item.SlotId}]";
                        return;
                    }

                    // 2. Check for packing station
                    var station = hit.collider.GetComponentInParent<PackingStation>();
                    if (station != null)
                    {
                        hoveredPackingStation = station;
                        var orderManager = OrderManager.Instance;
                        if (orderManager != null && orderManager.IsAwaitingPacking)
                        {
                            currentPrompt = "[E / LMB] Deliver to Staging & Finalize Order";
                        }
                        else if (orderManager != null && orderManager.HasActiveOrder)
                        {
                            currentPrompt = "Packing Station (Fulfill all pick list items first)";
                        }
                        else
                        {
                            currentPrompt = "Packing Station (No active order awaiting dispatch)";
                        }
                        return;
                    }

                    // 3. Check for Order Collection Desk
                    if (hit.collider.name.IndexOf("Collection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (hit.collider.transform.parent != null && hit.collider.transform.parent.name.IndexOf("Collection", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        hoveredCollectionDesk = hit.collider.gameObject;
                        var op = VirtualDarkStore.Management.OperationalCycleManager.Instance;
                        if (op != null && op.CurrentStage == VirtualDarkStore.Management.OperationalStage.CollectOrder)
                        {
                            currentPrompt = "[E / LMB] Collect Pick Cart & Activate Order";
                        }
                        else
                        {
                            currentPrompt = "Order Collection Desk";
                        }
                        return;
                    }
                }
            }
        }

        private void HandleInput()
        {
            bool interactPressed = false;
            bool newOrderPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
                if (Keyboard.current.nKey.wasPressedThisFrame) newOrderPressed = true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                interactPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) interactPressed = true;
            if (Input.GetKeyDown(KeyCode.N)) newOrderPressed = true;
#endif

            if (interactPressed)
            {
                ExecuteInteraction();
            }

            if (newOrderPressed)
            {
                var orderManager = OrderManager.Instance;
                if (orderManager != null && (!orderManager.HasActiveOrder && !orderManager.IsAwaitingPacking))
                {
                    orderManager.StartSampleOrder(3);
                }
            }
        }

        private void ExecuteInteraction()
        {
            var orderManager = OrderManager.Instance;
            if (orderManager == null) return;

            // Pick product from shelf
            if (hoveredItem != null)
            {
                var result = orderManager.AttemptPick(hoveredItem.SkuId);
                if (result == PickResult.Success)
                {
                    hoveredItem.OnPicked();
                }
                return;
            }

            // Collect pick cart & activate order at Order Collection Desk
            if (hoveredCollectionDesk != null)
            {
                var op = VirtualDarkStore.Management.OperationalCycleManager.Instance;
                if (op != null && op.CurrentStage == VirtualDarkStore.Management.OperationalStage.CollectOrder)
                {
                    op.CollectOrder(out _, out _);
                }
                return;
            }

            // Deliver tote / finalize packing at packing bench
            if (hoveredPackingStation != null)
            {
                var op = VirtualDarkStore.Management.OperationalCycleManager.Instance;
                if (op != null && (op.CurrentStage == VirtualDarkStore.Management.OperationalStage.AwaitingPacking || op.CurrentStage == VirtualDarkStore.Management.OperationalStage.DepositOrder))
                {
                    op.DepositOrder(out _);
                }
                else if (orderManager.IsAwaitingPacking)
                {
                    orderManager.CompletePacking(out string failureReason);
                }
                return;
            }
        }
    }
}
