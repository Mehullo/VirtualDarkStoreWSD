using System;
using UnityEngine;
using VirtualDarkStore.Environment;

namespace VirtualDarkStore.Domain
{
    /// <summary>
    /// Pure domain entity representing a specific physical slot / bin location on a shelf tier.
    /// Has a deterministic unique SlotId (e.g., "A01-L-B01-T01-S01") and references its assigned SKU.
    /// Implements machine-readable structured store location mapping (Store -> Aisle -> Side -> Bay -> Shelf Level -> Slot).
    /// </summary>
    [Serializable]
    public class ShelfSlot
    {
        [SerializeField] private string slotId;
        [SerializeField] private int aisleNumber;
        [SerializeField] private ShelfSide side;
        [SerializeField] private int bayIndex;
        [SerializeField] private int tierLevel;
        [SerializeField] private int slotIndex;
        [SerializeField] private Vector3 worldPosition;
        [SerializeField] private string assignedSkuId;

        public string SlotId => slotId;
        public int AisleNumber => aisleNumber;
        public ShelfSide Side => side;
        public int BayIndex => bayIndex;
        public int TierLevel => tierLevel;
        public int SlotIndex => slotIndex;
        public Vector3 WorldPosition => worldPosition;
        public string AssignedSkuId => assignedSkuId;
        public bool IsOccupied => !string.IsNullOrEmpty(assignedSkuId);

        public StoreLocation Location => new StoreLocation(aisleNumber, side, bayIndex, tierLevel, slotIndex);

        public ShelfSlot() { }

        public ShelfSlot(string id, int aisle, ShelfSide shelfSide, int bay, int tier, int slot, Vector3 position)
        {
            slotId = id;
            aisleNumber = aisle;
            side = shelfSide;
            bayIndex = bay;
            tierLevel = tier;
            slotIndex = slot;
            worldPosition = position;
            assignedSkuId = null;
        }

        public void AssignSku(string skuId)
        {
            assignedSkuId = skuId;
        }

        public void ClearSku()
        {
            assignedSkuId = null;
        }

        public static string GenerateSlotId(int aisle, ShelfSide side, int bay, int tier, int slot)
        {
            return StoreLocation.Format(aisle, side, bay, tier, slot);
        }

        public override string ToString() => $"[Slot: {slotId}] SKU: {assignedSkuId ?? "EMPTY"}";
    }
}
