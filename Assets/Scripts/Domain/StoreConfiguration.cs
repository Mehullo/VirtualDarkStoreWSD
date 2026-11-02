using System;
using UnityEngine;

namespace VirtualDarkStore.Domain
{
    /// <summary>
    /// Configuration settings defining the dimensional and topological parameters of the dark store.
    /// Supports standard 4-aisle configurations and realistic quick-commerce 6-aisle topologies (A01-A06).
    /// </summary>
    [Serializable]
    public class StoreConfiguration
    {
        [Header("Store Identification")]
        [SerializeField] private string storeId = "DS-01";
        [SerializeField] private string storeName = "Virtual Dark Store Alpha";

        [Header("Topology Layout")]
        [Range(1, 10)]
        [SerializeField] private int aisleCount = 4;

        [Range(1, 10)]
        [SerializeField] private int baysPerAisleSide = 3;

        [Range(1, 6)]
        [SerializeField] private int tiersPerBay = 3;

        [Range(1, 8)]
        [SerializeField] private int slotsPerTier = 4;

        [Header("Physical Geometry (Meters)")]
        [SerializeField] private float aisleWidth = 1.8f;
        [SerializeField] private float aisleLength = 12.0f;
        [SerializeField] private float rackDepth = 0.6f;
        [SerializeField] private float rackHeight = 2.4f;

        public string StoreId => storeId;
        public string StoreName => storeName;
        public int AisleCount => aisleCount;
        public int BaysPerAisleSide => baysPerAisleSide;
        public int TiersPerBay => tiersPerBay;
        public int SlotsPerTier => slotsPerTier;
        public float AisleWidth => aisleWidth;
        public float AisleLength => aisleLength;
        public float RackDepth => rackDepth;
        public float RackHeight => rackHeight;

        /// <summary>
        /// Total storage slot capacity: AisleCount * 2 sides * BaysPerSide * TiersPerBay * SlotsPerTier
        /// Default: 4 * 2 * 3 * 3 * 4 = 288 slots.
        /// 6-Aisle: 6 * 2 * 3 * 3 * 4 = 432 slots.
        /// </summary>
        public int TotalSlotCapacity => aisleCount * 2 * baysPerAisleSide * tiersPerBay * slotsPerTier;

        public static StoreConfiguration CreateDefault()
        {
            return new StoreConfiguration
            {
                storeId = "DS-01",
                storeName = "Virtual Dark Store Alpha",
                aisleCount = 4,
                baysPerAisleSide = 3,
                tiersPerBay = 3,
                slotsPerTier = 4,
                aisleWidth = 1.8f,
                aisleLength = 12.0f,
                rackDepth = 0.6f,
                rackHeight = 2.4f
            };
        }

        public static StoreConfiguration CreateQuickCommerce6AisleConfig()
        {
            return new StoreConfiguration
            {
                storeId = "DS-QC-01",
                storeName = "Quick-Commerce Dark Store (A01-A06)",
                aisleCount = 6,
                baysPerAisleSide = 3,
                tiersPerBay = 3,
                slotsPerTier = 4,
                aisleWidth = 1.8f,
                aisleLength = 12.0f,
                rackDepth = 0.6f,
                rackHeight = 2.4f
            };
        }
    }
}
