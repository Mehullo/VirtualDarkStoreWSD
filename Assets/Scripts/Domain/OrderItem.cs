using System;
using UnityEngine;

namespace VirtualDarkStore.Domain
{
    /// <summary>
    /// Pure domain entity representing a line item within a dark store pick order.
    /// Tracks required quantity, picked quantity, and target physical shelf slot.
    /// </summary>
    [Serializable]
    public class OrderItem
    {
        [SerializeField] private string skuId;
        [SerializeField] private string skuName;
        [SerializeField] private string targetSlotId;
        [SerializeField] private Vector3 targetWorldPosition;
        [SerializeField] private int requiredQuantity;
        [SerializeField] private int pickedQuantity;

        public string SkuId => skuId;
        public string SkuName => skuName;
        public string TargetSlotId => targetSlotId;
        public Vector3 TargetWorldPosition => targetWorldPosition;
        public int RequiredQuantity => requiredQuantity;
        public int PickedQuantity => pickedQuantity;
        public bool IsFulfilled => pickedQuantity >= requiredQuantity;

        public OrderItem() { }

        public OrderItem(string id, string name, string slotId, Vector3 slotPosition, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("SKU ID cannot be null or empty.", nameof(id));
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Required quantity must be positive.");

            skuId = id.Trim();
            skuName = name ?? id;
            targetSlotId = slotId ?? "UNASSIGNED";
            targetWorldPosition = slotPosition;
            requiredQuantity = quantity;
            pickedQuantity = 0;
        }

        public bool RecordPick(int count = 1)
        {
            if (count <= 0) return false;
            if (IsFulfilled) return false;

            pickedQuantity = Mathf.Min(requiredQuantity, pickedQuantity + count);
            return true;
        }

        public override string ToString() => $"[Item: {skuId}] {skuName} ({pickedQuantity}/{requiredQuantity}) @ {targetSlotId}";
    }
}
