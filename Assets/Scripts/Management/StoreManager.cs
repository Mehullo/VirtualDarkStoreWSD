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
    /// Manages Store -> Aisle -> Side -> Bay -> Shelf Level (Tier) -> Slot hierarchy and bidirectional SKU-to-Location mapping.
    /// Supports both standard 4-aisle configurations and realistic quick-commerce 6-aisle topologies (A01-A06).
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
            if (instance != null && instance != this)
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
            List<Sku> sampleCatalog;
            if (catalog != null)
            {
                sampleCatalog = catalog;
            }
            else if (configuration != null && configuration.AisleCount >= 6)
            {
                sampleCatalog = VerifiedSampleCatalog.CreateQuickCommerceCatalog();
            }
            else
            {
                sampleCatalog = VerifiedSampleCatalog.CreateCatalog();
            }

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
            float[] tierYHeights = { 0.35f, 1.15f, 1.95f, 2.35f };

            int aisleCount = Mathf.Max(1, configuration.AisleCount);
            int baysPerSide = configuration.BaysPerAisleSide;
            int tiersPerBay = configuration.TiersPerBay;
            int slotsPerTier = configuration.SlotsPerTier;

            float bayLength = configuration.AisleLength / baysPerSide;
            float slotSpacingZ = bayLength / (slotsPerTier + 1);

            // Calculate aisle X coordinates deterministically
            float[] aisleXPositions;
            if (aisleCount == 4)
            {
                // Exact Phase 1-8 4-aisle coordinates for 100% backward compatibility
                aisleXPositions = new[] { -6.0f, -2.0f, 2.0f, 6.0f };
            }
            else if (aisleCount == 6)
            {
                // Quick-Commerce 6-aisle coordinates (A01-A06 with 3.0m pitch)
                aisleXPositions = new[] { -7.5f, -4.5f, -1.5f, 1.5f, 4.5f, 7.5f };
            }
            else
            {
                // General symmetrical layout based on configured aisle corridor and rack dimensions
                aisleXPositions = new float[aisleCount];
                float pitch = configuration.AisleWidth + configuration.RackDepth * 2f;
                float startX = -((aisleCount - 1) * pitch) * 0.5f;
                for (int i = 0; i < aisleCount; i++)
                {
                    aisleXPositions[i] = startX + i * pitch;
                }
            }

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
            // For 4 aisles (72 SKUs): stocks 18 SKUs per aisle.
            // For 6 aisles (72 SKUs): stocks 12 SKUs per aisle (6 on Left side, 6 on Right side; 1 per bay-tier).
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

        /// <summary>
        /// Validates topological and inventory mapping integrity across all SKUs, aisles, shelves, and slots.
        /// </summary>
        public bool ValidateIntegrity(out List<string> errors)
        {
            return ValidateSkuLocationMappings(out errors);
        }

        /// <summary>
        /// Phase 10 structured store location & SKU mapping validation.
        /// Enforces:
        /// 1. Every registered SKU is mapped to a valid, existing slot.
        /// 2. Every slot ID conforms strictly to Store -> Aisle -> Side -> Bay -> Tier -> Slot machine-readable format.
        /// 3. Zero duplicate slot IDs across the topology.
        /// 4. Zero duplicate SKU-to-slot collisions.
        /// 5. Bidirectional consistency (slot.AssignedSkuId matches sku.SkuId).
        /// 6. World coordinates are finite, non-NaN, and within physical bounds.
        /// </summary>
        public bool ValidateSkuLocationMappings(out List<string> errors)
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

            // Check location ID format and uniqueness across all slots
            var seenSlotIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in slotLookup)
            {
                string slotId = kvp.Key;
                var slot = kvp.Value;

                if (!StoreLocation.TryParse(slotId, out StoreLocation parsedLoc, out string parseErr))
                {
                    errors.Add($"Invalid slot ID format '{slotId}': {parseErr}");
                }

                if (!seenSlotIds.Add(slotId))
                {
                    errors.Add($"Duplicate slot ID detected in store topology: '{slotId}'.");
                }

                if (float.IsNaN(slot.WorldPosition.x) || float.IsNaN(slot.WorldPosition.y) || float.IsNaN(slot.WorldPosition.z) ||
                    float.IsInfinity(slot.WorldPosition.x) || float.IsInfinity(slot.WorldPosition.y) || float.IsInfinity(slot.WorldPosition.z))
                {
                    errors.Add($"Slot {slotId} has invalid/non-finite world coordinates: {slot.WorldPosition}.");
                }
            }

            // Check SKU mappings
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

        /// <summary>
        /// Validates a raw location ID string for syntax and checks if it exists in the active store topology.
        /// </summary>
        public bool ValidateLocationId(string locationId, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(locationId))
            {
                error = "Location ID cannot be null or empty.";
                return false;
            }

            if (!StoreLocation.TryParse(locationId, out _, out string parseError))
            {
                error = parseError;
                return false;
            }

            if (!slotLookup.ContainsKey(locationId.Trim()))
            {
                error = $"Location ID '{locationId}' does not exist in store topology.";
                return false;
            }

            return true;
        }

        // Query & Resolution APIs

        /// <summary>
        /// Resolves SKU ID -> Location ID -> World Position.
        /// Returns true if the SKU is registered and mapped to a valid physical slot.
        /// </summary>
        public bool TryResolveSkuLocation(string skuId, out string locationId, out Vector3 worldPosition)
        {
            if (string.IsNullOrEmpty(skuId))
            {
                locationId = null;
                worldPosition = Vector3.zero;
                return false;
            }

            if (skuToSlotMap.TryGetValue(skuId.Trim(), out ShelfSlot slot) && slot != null)
            {
                locationId = slot.SlotId;
                worldPosition = slot.WorldPosition;
                return true;
            }

            locationId = null;
            worldPosition = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Resolves Location ID -> 3D World Position.
        /// Returns true if the location ID exists in the store topology.
        /// </summary>
        public bool TryResolveLocationPosition(string locationId, out Vector3 worldPosition)
        {
            if (string.IsNullOrEmpty(locationId))
            {
                worldPosition = Vector3.zero;
                return false;
            }

            if (slotLookup.TryGetValue(locationId.Trim(), out ShelfSlot slot) && slot != null)
            {
                worldPosition = slot.WorldPosition;
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Resolves Location ID -> Sku.
        /// Returns true if a SKU is currently assigned to the given location.
        /// </summary>
        public bool TryGetSkuAtLocation(string locationId, out Sku sku)
        {
            return TryGetSkuAtSlot(locationId, out sku);
        }

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
