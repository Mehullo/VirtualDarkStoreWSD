using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;

namespace VirtualDarkStore.Management
{
    /// <summary>
    /// Central manager for the dark store domain topology and SKU inventory.
    /// Maintains the strict decoupling between visual scene objects and domain data.
    /// Manages Aisle -> Shelf -> ShelfSlot hierarchy and bidirectional SKU-to-Location mapping.
    /// </summary>
    public class StoreManager : MonoBehaviour
    {
        private static StoreManager instance;
        public static StoreManager Instance => instance != null ? instance : (instance = FindAnyObjectByType<StoreManager>());

        [Header("Store Configuration")]
        [SerializeField] private StoreConfiguration configuration;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private int registeredSkuCount;
        [SerializeField] private int totalSlotCount;
        [SerializeField] private int occupiedSlotCount;

        // Domain collections
        private readonly List<Aisle> aisles = new List<Aisle>();
        private readonly List<Sku> skus = new List<Sku>();

        // Lookup dictionaries
        private readonly Dictionary<string, Sku> skuLookup = new Dictionary<string, Sku>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ShelfSlot> slotLookup = new Dictionary<string, ShelfSlot>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ShelfSlot> skuToSlotMap = new Dictionary<string, ShelfSlot>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Sku> slotToSkuMap = new Dictionary<string, Sku>(StringComparer.OrdinalIgnoreCase);

        public StoreConfiguration Configuration => configuration;
        public IReadOnlyList<Aisle> Aisles => aisles;
        public IReadOnlyList<Sku> Skus => skus;
        public int RegisteredSkuCount => skus.Count;
        public int TotalSlotCount => slotLookup.Count;
        public int OccupiedSlotCount => skuToSlotMap.Count;

        private void Awake()
        {
            if (instance != null && instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            InitializeStore();
        }

        /// <summary>
        /// Explicit initialization method for both runtime Awake and EditMode unit tests.
        /// </summary>
        public void InitializeStore(StoreConfiguration config = null, List<Sku> catalog = null)
        {
            instance = this;
            configuration = config ?? configuration ?? StoreConfiguration.CreateDefault();

            aisles.Clear();
            skus.Clear();
            skuLookup.Clear();
            slotLookup.Clear();
            skuToSlotMap.Clear();
            slotToSkuMap.Clear();

            // 1. Build domain topology (Aisles -> Shelves -> ShelfSlots)
            BuildTopology();

            // 2. Populate SKU catalog
            var sampleCatalog = catalog ?? VerifiedSampleCatalog.CreateCatalog();
            foreach (var sku in sampleCatalog)
            {
                RegisterSku(sku);
            }

            // 3. Map SKUs to unique physical shelf slots (1:1 deterministic mapping)
            AssignSkusToSlots();

            // 4. Verify integrity
            if (!ValidateIntegrity(out List<string> errors))
            {
                foreach (var err in errors)
                {
                    Debug.LogError($"[StoreManager] Topology Integrity Error: {err}");
                }
            }
            else
            {
                registeredSkuCount = skus.Count;
                totalSlotCount = slotLookup.Count;
                occupiedSlotCount = skuToSlotMap.Count;
                Debug.Log($"[StoreManager] Successfully verified store topology: {registeredSkuCount} SKUs mapped to unique locations across {totalSlotCount} total slots ({occupiedSlotCount} occupied).");
            }
        }

        private void BuildTopology()
        {
            float[] aisleXPositions = { -6.0f, -2.0f, 2.0f, 6.0f };
            float[] tierYHeights = { 0.35f, 1.15f, 1.95f, 2.35f };

            int aisleCount = Mathf.Min(configuration.AisleCount, aisleXPositions.Length);
            int baysPerSide = configuration.BaysPerAisleSide;
            int tiersPerBay = configuration.TiersPerBay;
            int slotsPerTier = configuration.SlotsPerTier;

            float bayLength = configuration.AisleLength / baysPerSide;
            float slotSpacingZ = bayLength / (slotsPerTier + 1);

            for (int a = 1; a <= aisleCount; a++)
            {
                string aisleId = Aisle.GenerateAisleId(a);
                var aisle = new Aisle(aisleId, a, $"Aisle {a:D2}");
                float aisleCenterX = aisleXPositions[a - 1];

                // Build Left and Right Shelves
                BuildShelvesForSide(aisle, a, ShelfSide.Left, aisleCenterX, baysPerSide, tiersPerBay, slotsPerTier, bayLength, slotSpacingZ, tierYHeights);
                BuildShelvesForSide(aisle, a, ShelfSide.Right, aisleCenterX, baysPerSide, tiersPerBay, slotsPerTier, bayLength, slotSpacingZ, tierYHeights);

                aisles.Add(aisle);
            }
        }

        private void BuildShelvesForSide(
            Aisle aisle,
            int aisleNum,
            ShelfSide side,
            float aisleCenterX,
            int baysPerSide,
            int tiersPerBay,
            int slotsPerTier,
            float bayLength,
            float slotSpacingZ,
            float[] tierYHeights)
        {
            float sideOffset = (configuration.AisleWidth * 0.5f + configuration.RackDepth * 0.5f);
            float shelfX = side == ShelfSide.Left ? (aisleCenterX - sideOffset) : (aisleCenterX + sideOffset);

            float startZ = -configuration.AisleLength * 0.5f + 2.0f; // matches scene Z center at +2m

            for (int b = 1; b <= baysPerSide; b++)
            {
                string shelfId = Shelf.GenerateShelfId(aisleNum, side, b);
                var shelf = new Shelf(shelfId, aisleNum, side, b, tiersPerBay);
                float bayCenterZ = startZ + (b - 0.5f) * bayLength;

                for (int t = 1; t <= tiersPerBay; t++)
                {
                    float tierY = (t - 1) < tierYHeights.Length ? tierYHeights[t - 1] : (0.35f + (t - 1) * 0.7f);

                    for (int s = 1; s <= slotsPerTier; s++)
                    {
                        string slotId = ShelfSlot.GenerateSlotId(aisleNum, side, b, t, s);
                        float slotZ = (bayCenterZ - bayLength * 0.5f) + s * slotSpacingZ;
                        Vector3 slotPos = new Vector3(shelfX, tierY, slotZ);

                        var slot = new ShelfSlot(slotId, aisleNum, side, b, t, s, slotPos);
                        shelf.AddSlot(slot);
                        slotLookup[slotId] = slot;
                    }
                }

                if (side == ShelfSide.Left) aisle.AddLeftShelf(shelf);
                else aisle.AddRightShelf(shelf);
            }
        }

        private void RegisterSku(Sku sku)
        {
            if (sku == null) return;
            if (skuLookup.ContainsKey(sku.SkuId))
            {
                Debug.LogWarning($"[StoreManager] Duplicate SKU ID: {sku.SkuId}. Skipping.");
                return;
            }

            skus.Add(sku);
            skuLookup[sku.SkuId] = sku;
        }

        private void AssignSkusToSlots()
        {
            int skuIndex = 0;
            int maxSlotsPerTier = configuration != null ? Mathf.Max(1, configuration.SlotsPerTier) : 4;

            // Distribute SKUs evenly across all aisles, bays, and tiers pass-by-pass by slot number (s = 1..maxSlotsPerTier).
            // For the 72-SKU catalog across 4 aisles (2 sides * 3 bays * 3 tiers = 18 bay-tiers per aisle),
            // Pass s = 1 stocks 18 SKUs in each of the 4 aisles (1 per bay-tier).
            for (int targetSlotNum = 1; targetSlotNum <= maxSlotsPerTier && skuIndex < skus.Count; targetSlotNum++)
            {
                foreach (var aisle in aisles)
                {
                    foreach (var slot in aisle.GetAllSlots())
                    {
                        if (skuIndex >= skus.Count) break;
                        if (slot.SlotIndex != targetSlotNum || slot.IsOccupied) continue;

                        var sku = skus[skuIndex];
                        slot.AssignSku(sku.SkuId);
                        skuToSlotMap[sku.SkuId] = slot;
                        slotToSkuMap[slot.SlotId] = sku;

                        skuIndex++;
                    }
                    if (skuIndex >= skus.Count) break;
                }
            }
        }

        public bool ValidateIntegrity(out List<string> errors)
        {
            errors = new List<string>();

            if (skus.Count == 0)
            {
                errors.Add("No SKUs are registered in the store catalog.");
            }

            if (slotLookup.Count == 0)
            {
                errors.Add("No shelf slots are defined in the store topology.");
            }

            if (slotLookup.Count < skus.Count)
            {
                errors.Add($"Insufficient shelf slots ({slotLookup.Count}) for registered SKUs ({skus.Count}).");
            }

            var usedSlotIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sku in skus)
            {
                if (!skuToSlotMap.TryGetValue(sku.SkuId, out ShelfSlot slot))
                {
                    errors.Add($"SKU {sku.SkuId} ('{sku.Name}') has no assigned shelf slot.");
                    continue;
                }

                if (slot == null)
                {
                    errors.Add($"SKU {sku.SkuId} mapped to null slot.");
                    continue;
                }

                if (usedSlotIds.Contains(slot.SlotId))
                {
                    errors.Add($"Slot {slot.SlotId} is assigned to multiple SKUs (Collision on SKU {sku.SkuId}).");
                }
                else
                {
                    usedSlotIds.Add(slot.SlotId);
                }

                if (slot.AssignedSkuId != sku.SkuId)
                {
                    errors.Add($"Slot {slot.SlotId} backreference mismatch: expected {sku.SkuId}, got {slot.AssignedSkuId ?? "NULL"}.");
                }
            }

            return errors.Count == 0;
        }

        // Query APIs
        public bool TryGetSlotForSku(string skuId, out ShelfSlot slot)
        {
            if (string.IsNullOrEmpty(skuId))
            {
                slot = null;
                return false;
            }
            return skuToSlotMap.TryGetValue(skuId, out slot);
        }

        public bool TryGetSkuAtSlot(string slotId, out Sku sku)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                sku = null;
                return false;
            }
            return slotToSkuMap.TryGetValue(slotId, out sku);
        }

        public Sku GetSku(string skuId)
        {
            if (string.IsNullOrEmpty(skuId)) return null;
            skuLookup.TryGetValue(skuId, out var sku);
            return sku;
        }

        public ShelfSlot GetSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return null;
            slotLookup.TryGetValue(slotId, out var slot);
            return slot;
        }

        public IReadOnlyList<Sku> GetAllSkus() => skus;
        public IReadOnlyList<ShelfSlot> GetAllSlots()
        {
            var list = new List<ShelfSlot>(slotLookup.Values);
            return list;
        }
    }
}
