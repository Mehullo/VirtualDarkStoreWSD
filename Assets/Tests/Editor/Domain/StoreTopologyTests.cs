using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Management;

namespace VirtualDarkStore.Tests.Domain
{
    [TestFixture]
    public class StoreTopologyTests
    {
        private GameObject managerHost;
        private StoreManager storeManager;

        [SetUp]
        public void SetUp()
        {
            managerHost = new GameObject("[Test_StoreManager]");
            storeManager = managerHost.AddComponent<StoreManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (managerHost != null)
            {
                Object.DestroyImmediate(managerHost);
            }
        }

        [Test]
        public void VerifiedSampleCatalog_Count_IsBetween50And100()
        {
            var catalog = VerifiedSampleCatalog.CreateCatalog();
            Assert.IsNotNull(catalog);
            Assert.GreaterOrEqual(catalog.Count, 50, "Catalog must contain at least 50 SKUs.");
            Assert.LessOrEqual(catalog.Count, 100, "Catalog must contain at most 100 SKUs.");
            Assert.AreEqual(72, catalog.Count, "Verified exact count of catalog SKUs.");
        }

        [Test]
        public void VerifiedSampleCatalog_AllSkus_HaveValidUniqueIdentifiersAndAttributes()
        {
            var catalog = VerifiedSampleCatalog.CreateCatalog();
            var seenIds = new HashSet<string>();
            var seenBarcodes = new HashSet<string>();

            foreach (var sku in catalog)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(sku.SkuId), "SKU ID cannot be empty.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(sku.Name), $"SKU {sku.SkuId} name cannot be empty.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(sku.Barcode), $"SKU {sku.SkuId} barcode cannot be empty.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(sku.Category), $"SKU {sku.SkuId} category cannot be empty.");
                Assert.Greater(sku.WeightKg, 0f, $"SKU {sku.SkuId} weight must be positive.");
                Assert.Greater(sku.DimensionsMeters.x, 0f, $"SKU {sku.SkuId} width must be positive.");
                Assert.Greater(sku.DimensionsMeters.y, 0f, $"SKU {sku.SkuId} height must be positive.");
                Assert.Greater(sku.DimensionsMeters.z, 0f, $"SKU {sku.SkuId} depth must be positive.");

                Assert.IsTrue(seenIds.Add(sku.SkuId), $"Duplicate SKU ID found: {sku.SkuId}");
                Assert.IsTrue(seenBarcodes.Add(sku.Barcode), $"Duplicate Barcode found: {sku.Barcode}");
            }
        }

        [Test]
        public void StoreManager_InitializesTopology_WithExpectedSlotCapacity()
        {
            var config = StoreConfiguration.CreateDefault();
            storeManager.InitializeStore(config);

            // Default: 4 aisles * 2 sides * 3 bays * 3 tiers * 4 slots = 288 slots
            Assert.AreEqual(4, storeManager.Aisles.Count, "Expected 4 aisles.");
            Assert.AreEqual(288, storeManager.TotalSlotCount, "Expected 288 total slots in store topology.");
            Assert.AreEqual(72, storeManager.RegisteredSkuCount, "Expected 72 registered SKUs.");
            Assert.AreEqual(72, storeManager.OccupiedSlotCount, "Expected 72 occupied slots.");
        }

        [Test]
        public void StoreManager_MultipleShelfLevels_ExistInEveryBay()
        {
            storeManager.InitializeStore();

            foreach (var aisle in storeManager.Aisles)
            {
                foreach (var shelf in aisle.LeftShelves)
                {
                    Assert.AreEqual(3, shelf.TierCount, $"Shelf {shelf.ShelfId} must have multiple tiers (3).");
                    Assert.AreEqual(12, shelf.Slots.Count, $"Shelf {shelf.ShelfId} must have 12 slots (3 tiers * 4 slots).");
                }
                foreach (var shelf in aisle.RightShelves)
                {
                    Assert.AreEqual(3, shelf.TierCount, $"Shelf {shelf.ShelfId} must have multiple tiers (3).");
                    Assert.AreEqual(12, shelf.Slots.Count, $"Shelf {shelf.ShelfId} must have 12 slots (3 tiers * 4 slots).");
                }
            }
        }

        [Test]
        public void StoreManager_EverySku_HasAValidUniqueLocation()
        {
            storeManager.InitializeStore();

            var assignedSlotIds = new HashSet<string>();
            var allSkus = storeManager.GetAllSkus();

            Assert.AreEqual(72, allSkus.Count);

            foreach (var sku in allSkus)
            {
                bool found = storeManager.TryGetSlotForSku(sku.SkuId, out ShelfSlot slot);
                Assert.IsTrue(found, $"SKU {sku.SkuId} must be mapped to a slot.");
                Assert.IsNotNull(slot, $"Mapped slot for {sku.SkuId} cannot be null.");
                Assert.IsFalse(string.IsNullOrEmpty(slot.SlotId), $"Slot for {sku.SkuId} must have a valid SlotId.");
                Assert.AreEqual(sku.SkuId, slot.AssignedSkuId, $"Slot {slot.SlotId} assignedSkuId must match.");
                Assert.IsTrue(slot.IsOccupied, $"Slot {slot.SlotId} must report IsOccupied == true.");

                // Verify uniqueness of location
                Assert.IsTrue(assignedSlotIds.Add(slot.SlotId), $"Location conflict! Slot {slot.SlotId} assigned to multiple SKUs.");
            }

            Assert.AreEqual(72, assignedSlotIds.Count, "Exactly 72 unique locations must be occupied by 72 SKUs.");
        }

        [Test]
        public void StoreManager_BidirectionalMapping_IsConsistent()
        {
            storeManager.InitializeStore();

            foreach (var sku in storeManager.GetAllSkus())
            {
                Assert.IsTrue(storeManager.TryGetSlotForSku(sku.SkuId, out ShelfSlot slot));
                Assert.IsTrue(storeManager.TryGetSkuAtSlot(slot.SlotId, out Sku mappedSku));
                Assert.AreEqual(sku.SkuId, mappedSku.SkuId, "Bidirectional mapping must resolve to the identical SKU.");
            }
        }

        [Test]
        public void StoreManager_ValidateIntegrity_ReturnsTrueWithZeroErrors()
        {
            storeManager.InitializeStore();

            bool isValid = storeManager.ValidateIntegrity(out List<string> errors);
            Assert.IsTrue(isValid, $"Integrity validation failed with errors: {string.Join("; ", errors)}");
            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void StoreManager_QueryNonExistent_ReturnsFalseGracefully()
        {
            storeManager.InitializeStore();

            Assert.IsFalse(storeManager.TryGetSlotForSku("NON_EXISTENT_SKU", out var slot));
            Assert.IsNull(slot);

            Assert.IsFalse(storeManager.TryGetSkuAtSlot("NON_EXISTENT_SLOT", out var sku));
            Assert.IsNull(sku);

            Assert.IsNull(storeManager.GetSku(""));
            Assert.IsNull(storeManager.GetSlot(""));
        }
    }
}
