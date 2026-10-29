using System;
using UnityEngine;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Interaction;
using VirtualDarkStore.Management;

namespace VirtualDarkStore.VR
{
    /// <summary>
    /// Manages VR picker interaction with shelf SKU items and dispatch packing benches.
    /// Supports controller pointing raycasts and direct touch interaction.
    /// Strictly reuses the central OrderManager and inventory models without duplicating fulfillment logic.
    /// </summary>
    public class VRPickerInteraction : MonoBehaviour, IPickerInteraction
    {
        [Header("Raycast Settings")]
        [Tooltip("Maximum picking reach in meters (standard arm reach + lean: 2.8m).")]
        [SerializeField] private float reachDistance = 2.8f;

        [Tooltip("Primary controller used for aiming rays.")]
        [SerializeField] private VRControllerInput primaryController;

        [Tooltip("Fallback camera ray if controllers are unassigned.")]
        [SerializeField] private Camera fallbackCamera;

        [Tooltip("Layer mask for interactable items and stations.")]
        [SerializeField] private LayerMask interactionLayers = ~0;

        [Header("Runtime State")]
        [SerializeField] private PickableItemVisual hoveredItem;
        [SerializeField] private PackingStation hoveredPackingStation;
        [SerializeField] private string currentPrompt = string.Empty;

        public PickableItemVisual HoveredItem => hoveredItem;
        public PackingStation HoveredPackingStation => hoveredPackingStation;
        public string CurrentPrompt => currentPrompt;
        public float ReachDistance => reachDistance;

        public event Action<PickableItemVisual> OnPickAttempted;
        public event Action<bool> OnPackingAttempted;

        private void Awake()
        {
            if (primaryController == null)
            {
                primaryController = GetComponentInChildren<VRControllerInput>();
            }
            if (fallbackCamera == null)
            {
                fallbackCamera = GetComponentInChildren<Camera>();
                if (fallbackCamera == null) fallbackCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            if (primaryController != null)
            {
                primaryController.OnSelectAction -= HandleSelectAction;
                primaryController.OnSelectAction += HandleSelectAction;
            }
        }

        private void OnDisable()
        {
            if (primaryController != null)
            {
                primaryController.OnSelectAction -= HandleSelectAction;
            }
        }

        private void Update()
        {
            PerformRaycast();
        }

        public void PerformRaycast()
        {
            hoveredItem = null;
            hoveredPackingStation = null;
            currentPrompt = string.Empty;

            Ray ray;
            if (primaryController != null)
            {
                ray = primaryController.PointerRay;
            }
            else if (fallbackCamera != null)
            {
                ray = fallbackCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            }
            else
            {
                ray = new Ray(transform.position + Vector3.up * 1.6f, transform.forward);
            }

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
                        currentPrompt = $"[Trigger] Pick: {item.SkuName} ({item.SkuId})";
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
                            currentPrompt = "[Trigger] Deliver to Staging & Finalize Order";
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
                }
            }
        }

        private void HandleSelectAction()
        {
            ExecuteInteraction();
        }

        /// <summary>
        /// Executes the pick or pack interaction against the active order.
        /// </summary>
        public bool ExecuteInteraction()
        {
            var orderManager = OrderManager.Instance;
            if (orderManager == null) return false;

            // Pick product from shelf
            if (hoveredItem != null)
            {
                OnPickAttempted?.Invoke(hoveredItem);
                var result = orderManager.AttemptPick(hoveredItem.SkuId);
                if (result == PickResult.Success)
                {
                    hoveredItem.OnPicked();
                    hoveredItem = null;
                    return true;
                }
                return false;
            }

            // Deliver tote / finalize packing at packing bench
            if (hoveredPackingStation != null)
            {
                if (orderManager.IsAwaitingPacking)
                {
                    bool packed = orderManager.CompletePacking(out string failureReason);
                    OnPackingAttempted?.Invoke(packed);
                    return packed;
                }
            }

            return false;
        }

        public void SetReferences(VRControllerInput controller, Camera cam = null)
        {
            if (primaryController != null)
            {
                primaryController.OnSelectAction -= HandleSelectAction;
            }

            primaryController = controller;
            if (cam != null) fallbackCamera = cam;

            if (primaryController != null)
            {
                primaryController.OnSelectAction += HandleSelectAction;
            }
        }
    }
}
