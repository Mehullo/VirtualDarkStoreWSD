using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.Domain
{
    /// <summary>
    /// Pure domain entity representing a dark-store picking aisle.
    /// Manages the collection of shelves positioned along its left and right sides.
    /// </summary>
    [Serializable]
    public class Aisle
    {
        [SerializeField] private string aisleId;
        [SerializeField] private int aisleNumber;
        [SerializeField] private string label;
        [SerializeField] private List<Shelf> leftShelves = new List<Shelf>();
        [SerializeField] private List<Shelf> rightShelves = new List<Shelf>();

        public string AisleId => aisleId;
        public int AisleNumber => aisleNumber;
        public string Label => label;
        public IReadOnlyList<Shelf> LeftShelves => leftShelves;
        public IReadOnlyList<Shelf> RightShelves => rightShelves;

        public Aisle() { }

        public Aisle(string id, int number, string aisleLabel)
        {
            aisleId = id;
            aisleNumber = number;
            label = aisleLabel;
            leftShelves = new List<Shelf>();
            rightShelves = new List<Shelf>();
        }

        public void AddLeftShelf(Shelf shelf)
        {
            if (shelf == null) throw new ArgumentNullException(nameof(shelf));
            leftShelves.Add(shelf);
        }

        public void AddRightShelf(Shelf shelf)
        {
            if (shelf == null) throw new ArgumentNullException(nameof(shelf));
            rightShelves.Add(shelf);
        }

        public IEnumerable<ShelfSlot> GetAllSlots()
        {
            foreach (var shelf in leftShelves)
            {
                foreach (var slot in shelf.Slots) yield return slot;
            }
            foreach (var shelf in rightShelves)
            {
                foreach (var slot in shelf.Slots) yield return slot;
            }
        }

        public static string GenerateAisleId(int number) => $"AISLE-{number:D2}";

        public override string ToString() => $"[Aisle: {aisleId}] {label} (Left: {leftShelves.Count}, Right: {rightShelves.Count})";
    }
}
