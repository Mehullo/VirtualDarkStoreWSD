using System;
using System.Text.RegularExpressions;
using UnityEngine;
using VirtualDarkStore.Environment;

namespace VirtualDarkStore.Domain
{
    /// <summary>
    /// Machine-readable structured store location representation following the hierarchy:
    /// Store -> Aisle -> Side -> Bay -> Shelf Level (Tier) -> Slot.
    /// Example format: "A01-L-B01-T01-S01" (or with store prefix "DS01-A01-L-B01-T01-S01").
    /// </summary>
    [Serializable]
    public struct StoreLocation : IEquatable<StoreLocation>
    {
        private static readonly Regex LocationRegex = new Regex(
            @"^(?:([A-Za-z0-9_-]+)-)?A(\d{2,3})-([LR])-B(\d{2,3})-T(\d{2,3})-S(\d{2,3})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [SerializeField] private string storeId;
        [SerializeField] private int aisleNumber;
        [SerializeField] private ShelfSide side;
        [SerializeField] private int bayIndex;
        [SerializeField] private int tierLevel;
        [SerializeField] private int slotIndex;
        [SerializeField] private string locationId;

        public string StoreId => storeId ?? "DS-01";
        public int AisleNumber => aisleNumber;
        public ShelfSide Side => side;
        public int BayIndex => bayIndex;
        public int TierLevel => tierLevel;
        public int SlotIndex => slotIndex;
        public string LocationId => locationId ?? Format(aisleNumber, side, bayIndex, tierLevel, slotIndex);

        public StoreLocation(int aisle, ShelfSide shelfSide, int bay, int tier, int slot, string store = "DS-01")
        {
            if (aisle <= 0) throw new ArgumentOutOfRangeException(nameof(aisle), "Aisle must be positive (>= 1).");
            if (bay <= 0) throw new ArgumentOutOfRangeException(nameof(bay), "Bay must be positive (>= 1).");
            if (tier <= 0) throw new ArgumentOutOfRangeException(nameof(tier), "Tier must be positive (>= 1).");
            if (slot <= 0) throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be positive (>= 1).");

            storeId = string.IsNullOrWhiteSpace(store) ? "DS-01" : store.Trim();
            aisleNumber = aisle;
            side = shelfSide;
            bayIndex = bay;
            tierLevel = tier;
            slotIndex = slot;
            locationId = Format(aisle, shelfSide, bay, tier, slot);
        }

        /// <summary>
        /// Formats location components into a standardized machine-readable ID: A{aisle:D2}-{side}-B{bay:D2}-T{tier:D2}-S{slot:D2}.
        /// </summary>
        public static string Format(int aisle, ShelfSide side, int bay, int tier, int slot)
        {
            char sideChar = side == ShelfSide.Left ? 'L' : 'R';
            return $"A{aisle:D2}-{sideChar}-B{bay:D2}-T{tier:D2}-S{slot:D2}";
        }

        /// <summary>
        /// Safely parses a machine-readable location string into a structured StoreLocation.
        /// Returns false with errorReason if malformed or invalid.
        /// </summary>
        public static bool TryParse(string rawLocationId, out StoreLocation location, out string errorReason)
        {
            location = default;
            errorReason = null;

            if (string.IsNullOrWhiteSpace(rawLocationId))
            {
                errorReason = "Location ID cannot be null or empty.";
                return false;
            }

            var match = LocationRegex.Match(rawLocationId.Trim());
            if (!match.Success)
            {
                errorReason = $"Location ID '{rawLocationId}' does not match expected format 'A01-L-B01-T01-S01'.";
                return false;
            }

            string matchedStore = match.Groups[1].Success ? match.Groups[1].Value : "DS-01";
            if (!int.TryParse(match.Groups[2].Value, out int aisle) || aisle <= 0)
            {
                errorReason = $"Invalid aisle number in location '{rawLocationId}'. Must be >= 1.";
                return false;
            }

            char sideChar = match.Groups[3].Value[0];
            ShelfSide side;
            if (sideChar == 'L' || sideChar == 'l') side = ShelfSide.Left;
            else if (sideChar == 'R' || sideChar == 'r') side = ShelfSide.Right;
            else
            {
                errorReason = $"Invalid side character '{sideChar}' in location '{rawLocationId}'. Expected 'L' or 'R'.";
                return false;
            }

            if (!int.TryParse(match.Groups[4].Value, out int bay) || bay <= 0)
            {
                errorReason = $"Invalid bay index in location '{rawLocationId}'. Must be >= 1.";
                return false;
            }

            if (!int.TryParse(match.Groups[5].Value, out int tier) || tier <= 0)
            {
                errorReason = $"Invalid tier level in location '{rawLocationId}'. Must be >= 1.";
                return false;
            }

            if (!int.TryParse(match.Groups[6].Value, out int slot) || slot <= 0)
            {
                errorReason = $"Invalid slot index in location '{rawLocationId}'. Must be >= 1.";
                return false;
            }

            location = new StoreLocation(aisle, side, bay, tier, slot, matchedStore);
            return true;
        }

        /// <summary>
        /// Parses a machine-readable location string, throwing FormatException if malformed.
        /// </summary>
        public static StoreLocation Parse(string rawLocationId)
        {
            if (!TryParse(rawLocationId, out StoreLocation location, out string error))
            {
                throw new FormatException(error);
            }
            return location;
        }

        public bool Equals(StoreLocation other)
        {
            return string.Equals(StoreId, other.StoreId, StringComparison.OrdinalIgnoreCase) &&
                   AisleNumber == other.AisleNumber &&
                   Side == other.Side &&
                   BayIndex == other.BayIndex &&
                   TierLevel == other.TierLevel &&
                   SlotIndex == other.SlotIndex;
        }

        public override bool Equals(object obj) => obj is StoreLocation other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (StoreId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(StoreId) : 0);
                hash = (hash * 397) ^ AisleNumber;
                hash = (hash * 397) ^ (int)Side;
                hash = (hash * 397) ^ BayIndex;
                hash = (hash * 397) ^ TierLevel;
                hash = (hash * 397) ^ SlotIndex;
                return hash;
            }
        }

        public static bool operator ==(StoreLocation left, StoreLocation right) => left.Equals(right);
        public static bool operator !=(StoreLocation left, StoreLocation right) => !left.Equals(right);

        public override string ToString() => LocationId;
    }
}
