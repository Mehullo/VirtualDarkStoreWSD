using System;
using UnityEngine;

namespace VirtualDarkStore.Interaction
{
    /// <summary>
    /// Component representing a physical visual SKU product resting on a dark store shelf slot.
    /// Supports raycast interaction from the picker controller and provides visual feedback.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PickableItemVisual : MonoBehaviour
    {
        [Header("SKU Identity")]
        [SerializeField] private string skuId;
        [SerializeField] private string skuName;
        [SerializeField] private string slotId;

        [Header("Visual State")]
        [SerializeField] private bool isPicked = false;
        [SerializeField] private Renderer meshRenderer;
        [SerializeField] private Collider itemCollider;

        public string SkuId => skuId;
        public string SkuName => skuName;
        public string SlotId => slotId;
        public bool IsPicked => isPicked;

        private void Awake()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<Renderer>();
            if (itemCollider == null) itemCollider = GetComponent<Collider>();
        }

        public void Initialize(string id, string name, string slot, Material material = null)
        {
            skuId = id;
            skuName = name;
            slotId = slot;
            isPicked = false;

            if (meshRenderer == null) meshRenderer = GetComponent<Renderer>();
            if (itemCollider == null) itemCollider = GetComponent<Collider>();

            if (material != null && meshRenderer != null)
            {
                meshRenderer.sharedMaterial = material;
            }
        }

        /// <summary>
        /// Called when the item is successfully picked during an order.
        /// Visually disables the product from the shelf.
        /// </summary>
        public void OnPicked()
        {
            isPicked = true;
            if (itemCollider != null) itemCollider.enabled = false;
            if (meshRenderer != null) meshRenderer.enabled = false;
        }

        /// <summary>
        /// Resets the visual item back to stocked shelf state.
        /// </summary>
        public void ResetVisual()
        {
            isPicked = false;
            if (itemCollider != null) itemCollider.enabled = true;
            if (meshRenderer != null) meshRenderer.enabled = true;
        }
    }
}
