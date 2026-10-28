using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.Environment;

namespace VirtualDarkStore.Domain
{
    /// <summary>
    /// Pure domain entity representing an industrial shelving bay unit.
    /// Contains multiple shelf levels/tiers, each holding slots.
    /// </summary>
    [Serializable]
    public class Shelf
    {
        [SerializeField] private string shelfId;
        [SerializeField] private int aisleNumber;
        [SerializeField] private ShelfSide side;
        [SerializeField] private int bayIndex;
        [SerializeField] private int tierCount;
        [SerializeField] private List<ShelfSlot> slots = new List<ShelfSlot>();

        public string ShelfId => shelfId;
        public int AisleNumber => aisleNumber;
        public ShelfSide Side => side;
        public int BayIndex => bayIndex;
        public int TierCount => tierCount;
        public IReadOnlyList<ShelfSlot> Slots => slots;

        public Shelf() { }

        public Shelf(string id, int aisle, ShelfSide shelfSide, int bay, int tiers)
        {
            shelfId = id;
            aisleNumber = aisle;
            side = shelfSide;
            bayIndex = bay;
            tierCount = tiers;
            slots = new List<ShelfSlot>();
        }

        public void AddSlot(ShelfSlot slot)
        {
            if (slot == null) throw new ArgumentNullException(nameof(slot));
            slots.Add(slot);
        }

        public static string GenerateShelfId(int aisle, ShelfSide side, int bay)
        {
            char sideChar = side == ShelfSide.Left ? 'L' : 'R';
            return $"SHELF-A{aisle:D2}-{sideChar}-B{bay:D2}";
        }

        public override string ToString() => $"[Shelf: {shelfId}] ({slots.Count} slots)";
    }
}
