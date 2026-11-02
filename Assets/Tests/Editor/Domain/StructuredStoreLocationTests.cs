using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Management;

namespace VirtualDarkStore.Tests.Domain
{
    /// <summary>
    /// Phase 10 verification suite for Structured Store Location & SKU Mapping.
    /// Covers:
    /// 1. Unique location IDs
    /// 2. Valid SKU mappings
    /// 3. World-position resolution
    /// 4. Missing location
    /// 5. Duplicate location
    /// 6. Invalid mapping
    /// </summary>
    [TestFixture]
    public class StructuredStoreLocationTests
    {
        private GameObject testHost;
        private StoreManager storeManager;

        [SetUp]
        public void SetUp()
        {
            testHost = new GameObject("[Test_StructuredStoreLocation]");
            storeManager = testHost.AddComponent<StoreManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testHost != null)
            {
                Object.DestroyImmediate(testHost);
            }
        }

        [Test]
        public void UniqueLocationIds_AllSlotsInStore_HaveDistinctFormattedIds()
        {
            // Verify both 4-aisle baseline and 6-aisle quick-commerce configurations
            var configs = new[]
            {
                StoreConfiguration.CreateDefault(),
                StoreConfiguration.CreateQuickCommerce6AisleConfig()
            };

            foreach (var config in configs)
            {
                storeManager.InitializeStore(config);

                var seenSlotIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                var allSlots = storeManager.GetAllSlots();

                Assert.Greater(allSlots.Count, 0, "Store must have defined slots.");

                foreach (var slot in allSlots)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(slot.SlotId), "SlotId must not be empty.");

                    // Verify format: Store -> Aisle -> Side -> Bay -> Shelf Level -> Slot
                    bool parsed = StoreLocation.TryParse(slot.SlotId, out StoreLocation loc, out string parseErr);
                    Assert.IsTrue(parsed, $"Slot ID '{slot.SlotId}' must parse into StoreLocation: {parseErr}");

                    Assert.AreEqual(slot.AisleNumber, loc.AisleNumber);
                    Assert.AreEqual(slot.Side, loc.Side);
                    Assert.AreEqual(slot.BayIndex, loc.BayIndex);
                    Assert.AreEqual(slot.TierLevel, loc.TierLevel);
                    Assert.AreEqual(slot.SlotIndex, loc.SlotIndex);
                    Assert.AreEqual(slot.SlotId, loc.LocationId);

                    // Verify strict uniqueness
                    Assert.IsTrue(seenSlotIds.Add(slot.SlotId), $"Duplicate location ID detected: {slot.SlotId}");
                }

                Assert.AreEqual(allSlots.Count, seenSlotIds.Count, "All location IDs in the store topology must be strictly unique.");
            }
        }

        [Test]
        public void ValidSkuMappings_AllSkus_MapToOneToOneValidPhysicalLocation()
        {
            storeManager.InitializeStore(StoreConfiguration.CreateQuickCommerce6AisleConfig());

            var allSkus = storeManager.GetAllSkus();
            Assert.AreEqual(144, allSkus.Count, "Expected 144 registered SKUs for quick-commerce 6-aisle configuration.");

            var assignedSlotIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var sku in allSkus)
            {
                // 1. Direct SKU -> Slot mapping
                bool found = storeManager.TryGetSlotForSku(sku.SkuId, out ShelfSlot slot);
                Assert.IsTrue(found, $"SKU {sku.SkuId} must have an assigned shelf slot.");
                Assert.IsNotNull(slot, $"Assigned slot for {sku.SkuId} cannot be null.");
                Assert.AreEqual(sku.SkuId, slot.AssignedSkuId, "Slot assignedSkuId must match SKU ID.");
                Assert.IsTrue(slot.IsOccupied, "Slot must be marked occupied.");

                // 2. Uniqueness of physical assignment
                Assert.IsTrue(assignedSlotIds.Add(slot.SlotId), $"Collision: Slot {slot.SlotId} assigned to multiple SKUs.");

                // 3. Bidirectional mapping (Location -> SKU)
                bool backFound = storeManager.TryGetSkuAtLocation(slot.SlotId, out Sku backSku);
                Assert.IsTrue(backFound, $"Must resolve SKU from Location ID '{slot.SlotId}'.");
                Assert.AreEqual(sku.SkuId, backSku.SkuId, "Resolved SKU at location must match original SKU.");
            }

            // 4. Whole-store validation
            bool valid = storeManager.ValidateSkuLocationMappings(out List<string> errors);
            Assert.IsTrue(valid, $"Validation failed with errors: {string.Join("; ", errors)}");
            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void PopulatedShelves_SpanAcrossMultipleAislesSidesBaysAndTiers()
        {
            storeManager.InitializeStore(StoreConfiguration.CreateQuickCommerce6AisleConfig());

            Assert.AreEqual(6, storeManager.Aisles.Count, "Expected 6 aisles (A01-A06).");
            Assert.AreEqual(144, storeManager.OccupiedSlotCount, "Expected 144 occupied product slots.");

            for (int a = 1; a <= 6; a++)
            {
                var aisle = storeManager.Aisles[a - 1];
                var slotsInAisle = new List<ShelfSlot>(aisle.GetAllSlots());

                var occupiedSlots = slotsInAisle.FindAll(s => s.IsOccupied);
                Assert.GreaterOrEqual(occupiedSlots.Count, 18, $"Expected at least 18 populated products in Aisle {a:D2}.");

                // Both Left and Right sides populated
                var leftOccupied = occupiedSlots.FindAll(s => s.Side == ShelfSide.Left);
                var rightOccupied = occupiedSlots.FindAll(s => s.Side == ShelfSide.Right);
                Assert.GreaterOrEqual(leftOccupied.Count, 9, $"Expected at least 9 populated items on Left side of Aisle {a:D2}.");
                Assert.GreaterOrEqual(rightOccupied.Count, 9, $"Expected at least 9 populated items on Right side of Aisle {a:D2}.");

                // All 3 bays on each side populated
                for (int b = 1; b <= 3; b++)
                {
                    var baySlots = occupiedSlots.FindAll(s => s.BayIndex == b);
                    Assert.GreaterOrEqual(baySlots.Count, 6, $"Expected at least 6 populated items in Bay {b:D2} of Aisle {a:D2}.");

                    // Multiple shelf tiers populated
                    for (int t = 1; t <= 3; t++)
                    {
                        var tierSlots = baySlots.FindAll(s => s.TierLevel == t);
                        Assert.GreaterOrEqual(tierSlots.Count, 1, $"Expected at least 1 populated item in Tier {t:D2} of Bay {b:D2}.");
                    }
                }
            }
        }

        [Test]
        public void WorldPositionResolution_SkuAndLocationId_ResolveToValidWorldCoordinates()
        {
            storeManager.InitializeStore(StoreConfiguration.CreateQuickCommerce6AisleConfig());

            foreach (var sku in storeManager.GetAllSkus())
            {
                // 1. Resolve via SKU ID
                bool skuResolved = storeManager.TryResolveSkuLocation(sku.SkuId, out string locId, out Vector3 skuWorldPos);
                Assert.IsTrue(skuResolved, $"Failed to resolve location for SKU '{sku.SkuId}'.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(locId));

                // 2. Resolve via Location ID
                bool locResolved = storeManager.TryResolveLocationPosition(locId, out Vector3 directWorldPos);
                Assert.IsTrue(locResolved, $"Failed to resolve world position for location '{locId}'.");

                // 3. Exact equality of resolved world position
                Assert.AreEqual(skuWorldPos, directWorldPos, $"World position mismatch for SKU '{sku.SkuId}' at '{locId}'.");

                // 4. Physical coordinate sanity (finite, elevated from floor, within warehouse boundary)
                Assert.IsFalse(float.IsNaN(skuWorldPos.x) || float.IsNaN(skuWorldPos.y) || float.IsNaN(skuWorldPos.z));
                Assert.IsFalse(float.IsInfinity(skuWorldPos.x) || float.IsInfinity(skuWorldPos.y) || float.IsInfinity(skuWorldPos.z));
                Assert.Greater(skuWorldPos.y, 0.2f, "Slot world position must be elevated off the ground.");
                Assert.Less(skuWorldPos.y, 3.0f, "Slot world position must be within reachable industrial rack height.");
                Assert.GreaterOrEqual(skuWorldPos.x, -13.0f, "Slot must be within warehouse X bounds.");
                Assert.LessOrEqual(skuWorldPos.x, 13.0f, "Slot must be within warehouse X bounds.");
                Assert.GreaterOrEqual(skuWorldPos.z, -13.0f, "Slot must be within warehouse Z bounds.");
                Assert.LessOrEqual(skuWorldPos.z, 13.0f, "Slot must be within warehouse Z bounds.");
            }
        }

        [Test]
        public void MissingLocation_NonExistentSkuOrLocationId_ReturnsFalseWithoutThrowing()
        {
            storeManager.InitializeStore();

            // 1. Missing SKU ID
            Assert.IsFalse(storeManager.TryResolveSkuLocation("SKU-NONEXISTENT-9999", out string missingLocId, out Vector3 missingPos));
            Assert.IsNull(missingLocId);
            Assert.AreEqual(Vector3.zero, missingPos);

            // Null and empty SKU
            Assert.IsFalse(storeManager.TryResolveSkuLocation(null, out _, out _));
            Assert.IsFalse(storeManager.TryResolveSkuLocation("", out _, out _));

            // 2. Missing Location ID
            Assert.IsFalse(storeManager.TryResolveLocationPosition("A99-L-B99-T99-S99", out Vector3 missingLocPos));
            Assert.AreEqual(Vector3.zero, missingLocPos);

            Assert.IsFalse(storeManager.TryResolveLocationPosition(null, out _));
            Assert.IsFalse(storeManager.TryResolveLocationPosition("", out _));

            // 3. Missing SKU at location
            Assert.IsFalse(storeManager.TryGetSkuAtLocation("A99-L-B99-T99-S99", out Sku missingSku));
            Assert.IsNull(missingSku);

            // 4. Validation of non-existent location ID
            bool validated = storeManager.ValidateLocationId("A99-L-B99-T99-S99", out string valErr);
            Assert.IsFalse(validated);
            StringAssert.Contains("does not exist", valErr);
        }

        [Test]
        public void DuplicateLocation_DetectsCollision_WhenTwoSkusAssignedSameSlot()
        {
            storeManager.InitializeStore();

            // Use reflection to simulate a corrupted state where two distinct SKUs point to the same ShelfSlot
            var skuToSlotMapField = typeof(StoreManager).GetField("skuToSlotMap", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(skuToSlotMapField);
            var skuToSlotMap = (Dictionary<string, ShelfSlot>)skuToSlotMapField.GetValue(storeManager);

            var allSkus = storeManager.GetAllSkus();
            Assert.GreaterOrEqual(allSkus.Count, 2);

            string skuA = allSkus[0].SkuId;
            string skuB = allSkus[1].SkuId;

            // Force skuB to map to skuA's slot
            var slotA = skuToSlotMap[skuA];
            skuToSlotMap[skuB] = slotA;

            bool valid = storeManager.ValidateSkuLocationMappings(out List<string> errors);
            Assert.IsFalse(valid, "Validation must fail when two SKUs collide on the same slot.");
            Assert.GreaterOrEqual(errors.Count, 1);

            bool foundCollision = errors.Exists(e => e.Contains("assigned to multiple SKUs") || e.Contains("Collision"));
            Assert.IsTrue(foundCollision, $"Expected duplicate location collision error, got: {string.Join("; ", errors)}");
        }

        [Test]
        public void InvalidMapping_MalformedLocationStrings_AreDetectedAndRejected()
        {
            storeManager.InitializeStore();

            string[] malformedLocations = new[]
            {
                null,
                "",
                "   ",
                "INVALID-LOCATION-STRING",
                "A01",
                "A01-L-B01",
                "A01-X-B01-T01-S01",       // Invalid side 'X'
                "A01-M-B01-T01-S01",       // Invalid side 'M'
                "A00-L-B01-T01-S01",       // Aisle 0 (must be >= 1)
                "A01-L-B00-T01-S01",       // Bay 0 (must be >= 1)
                "A01-L-B01-T00-S01",       // Tier 0 (must be >= 1)
                "A01-L-B01-T01-S00",       // Slot 0 (must be >= 1)
                "A01-L-BXX-T01-S01",       // Non-numeric bay
                "A01-L-B01-TZZ-S01",       // Non-numeric tier
                "A01-L-B01-T01-S01-EXTRA", // Extra trailing malformed segment
                "Z99-L-B01-T01-S01"        // Wrong prefix 'Z'
            };

            foreach (var badLoc in malformedLocations)
            {
                // 1. Direct StoreLocation.TryParse
                bool parseOk = StoreLocation.TryParse(badLoc, out StoreLocation loc, out string parseErr);
                Assert.IsFalse(parseOk, $"Malformed location '{badLoc}' should have failed parsing.");
                Assert.IsNotEmpty(parseErr);

                // 2. StoreManager.ValidateLocationId
                bool valOk = storeManager.ValidateLocationId(badLoc, out string valErr);
                Assert.IsFalse(valOk, $"ValidateLocationId should have rejected malformed location '{badLoc}'.");
                Assert.IsNotEmpty(valErr);
            }
        }
    }
}
