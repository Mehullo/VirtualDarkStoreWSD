using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Management;

namespace VirtualDarkStore.Tests.Editor.Domain
{
    [TestFixture]
    public class ControlledOrderGenerationTests
    {
        private GameObject storeGo;
        private StoreManager store;
        private GameObject orderMgrGo;
        private OrderManager orderManager;

        [SetUp]
        public void SetUp()
        {
            storeGo = new GameObject("TestStoreManager");
            store = storeGo.AddComponent<StoreManager>();
            store.InitializeStore(StoreConfiguration.CreateQuickCommerce6AisleConfig());

            orderMgrGo = new GameObject("TestOrderManager");
            orderManager = orderMgrGo.AddComponent<OrderManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (orderMgrGo != null) Object.DestroyImmediate(orderMgrGo);
            if (storeGo != null) Object.DestroyImmediate(storeGo);
        }

        [Test]
        public void SameSeed_ProducesIdenticalOrder()
        {
            var config1 = new OrderGenerationConfig(seed: 42001, itemCount: 4, orderCount: 1, DuplicateSkuPolicy.DisallowDuplicates);
            var config2 = new OrderGenerationConfig(seed: 42001, itemCount: 4, orderCount: 1, DuplicateSkuPolicy.DisallowDuplicates);

            Order order1 = OrderGenerator.GenerateOrder(store, config1);
            Order order2 = OrderGenerator.GenerateOrder(store, config2);

            Assert.IsNotNull(order1);
            Assert.IsNotNull(order2);
            Assert.AreEqual(order1.OrderId, order2.OrderId);
            Assert.AreEqual(order1.TotalItemCount, order2.TotalItemCount);

            for (int i = 0; i < order1.TotalItemCount; i++)
            {
                Assert.AreEqual(order1.Items[i].SkuId, order2.Items[i].SkuId, $"SKU mismatch at index {i}");
                Assert.AreEqual(order1.Items[i].TargetSlotId, order2.Items[i].TargetSlotId, $"Slot mismatch at index {i}");
                Assert.AreEqual(order1.Items[i].TargetWorldPosition, order2.Items[i].TargetWorldPosition, $"Position mismatch at index {i}");
                Assert.AreEqual(order1.Items[i].RequiredQuantity, order2.Items[i].RequiredQuantity);
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentOrders()
        {
            var configA = new OrderGenerationConfig(seed: 11111, itemCount: 4);
            var configB = new OrderGenerationConfig(seed: 99999, itemCount: 4);

            Order orderA = OrderGenerator.GenerateOrder(store, configA);
            Order orderB = OrderGenerator.GenerateOrder(store, configB);

            Assert.IsNotNull(orderA);
            Assert.IsNotNull(orderB);
            Assert.AreNotEqual(orderA.OrderId, orderB.OrderId);

            bool hasDifference = false;
            for (int i = 0; i < orderA.TotalItemCount; i++)
            {
                if (orderA.Items[i].SkuId != orderB.Items[i].SkuId)
                {
                    hasDifference = true;
                    break;
                }
            }

            Assert.IsTrue(hasDifference, "Orders from significantly different seeds should select different SKU sequences.");
        }

        [Test]
        public void AllGeneratedSkus_ExistInStoreCatalog()
        {
            var config = new OrderGenerationConfig(seed: 77777, itemCount: 6);
            Order order = OrderGenerator.GenerateOrder(store, config);

            Assert.IsNotNull(order);
            Assert.AreEqual(6, order.TotalItemCount);

            foreach (var item in order.Items)
            {
                Sku catalogSku = store.GetSku(item.SkuId);
                Assert.IsNotNull(catalogSku, $"Generated SKU '{item.SkuId}' must exist in StoreManager catalog.");
                Assert.AreEqual(catalogSku.Name, item.SkuName);
            }
        }

        [Test]
        public void AllGeneratedLocations_HaveValidWorldPositions()
        {
            var config = new OrderGenerationConfig(seed: 55555, itemCount: 5);
            Order order = OrderGenerator.GenerateOrder(store, config);

            foreach (var item in order.Items)
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.TargetSlotId), $"TargetSlotId cannot be null or empty for SKU {item.SkuId}.");
                Assert.AreNotEqual("UNASSIGNED", item.TargetSlotId);

                Assert.IsTrue(store.TryResolveSkuLocation(item.SkuId, out string resolvedSlot, out Vector3 worldPos));
                Assert.AreEqual(item.TargetSlotId, resolvedSlot);
                Assert.AreEqual(item.TargetWorldPosition, worldPos);

                // Quick-commerce store rack boundaries: Y in [0.2, 3.0], Z in [-10, 10]
                Assert.Greater(item.TargetWorldPosition.y, 0.1f, "Shelf level height must be above floor.");
            }
        }

        [Test]
        public void DuplicatePolicy_Disallow_ProducesDistinctSkus()
        {
            var config = new OrderGenerationConfig(seed: 88888, itemCount: 8, orderCount: 1, DuplicateSkuPolicy.DisallowDuplicates);
            Order order = OrderGenerator.GenerateOrder(store, config);

            var seen = new HashSet<string>();
            foreach (var item in order.Items)
            {
                Assert.IsTrue(seen.Add(item.SkuId), $"Duplicate SKU '{item.SkuId}' found under DisallowDuplicates policy.");
            }
        }

        [Test]
        public void BatchGeneration_ProducesRequestedCountAndReproducibleSequences()
        {
            var config = new OrderGenerationConfig(seed: 12345, itemCount: 3, orderCount: 4);
            List<Order> batch1 = OrderGenerator.GenerateBatch(store, config);
            List<Order> batch2 = OrderGenerator.GenerateBatch(store, config);

            Assert.AreEqual(4, batch1.Count);
            Assert.AreEqual(4, batch2.Count);

            for (int b = 0; b < 4; b++)
            {
                Assert.AreEqual(batch1[b].OrderId, batch2[b].OrderId);
                for (int i = 0; i < batch1[b].TotalItemCount; i++)
                {
                    Assert.AreEqual(batch1[b].Items[i].SkuId, batch2[b].Items[i].SkuId);
                }
            }
        }

        [Test]
        public void PresetOrders_RemainUnchanged()
        {
            // Verify that StartSpecificOrder and sample orders remain functional
            var skuIds = new List<string> { "SKU-0001", "SKU-0008", "SKU-0039" };
            Order presetOrder = orderManager.StartSpecificOrder("ORD-PRESET-TEST", skuIds);

            Assert.IsNotNull(presetOrder);
            Assert.AreEqual("ORD-PRESET-TEST", presetOrder.OrderId);
            Assert.AreEqual(3, presetOrder.TotalItemCount);
            Assert.AreEqual("SKU-0001", presetOrder.Items[0].SkuId);
            Assert.AreEqual("SKU-0008", presetOrder.Items[1].SkuId);
            Assert.AreEqual("SKU-0039", presetOrder.Items[2].SkuId);
        }

        [Test]
        public void GeneratedOrder_DispatchesAndExecutesSuccessfully()
        {
            var config = new OrderGenerationConfig(seed: 33333, itemCount: 2);
            bool dispatched = orderManager.TryDispatchGeneratedOrder(config, out Order order, out string error);

            Assert.IsTrue(dispatched, $"Dispatch failed: {error}");
            Assert.IsNotNull(order);
            Assert.IsTrue(orderManager.HasActiveOrder);
            Assert.AreEqual(order, orderManager.ActiveOrder);

            // Attempt valid pick of current target item
            string targetSku = order.CurrentTargetItem.SkuId;
            PickResult result = orderManager.AttemptPick(targetSku);
            Assert.AreEqual(PickResult.Success, result);
            Assert.AreEqual(1, order.Items[0].PickedQuantity);
        }
    }
}
