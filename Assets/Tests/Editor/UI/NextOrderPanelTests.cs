using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Management;
using VirtualDarkStore.UI;

namespace VirtualDarkStore.Tests.Editor.UI
{
    [TestFixture]
    public class NextOrderPanelTests
    {
        private GameObject testRoot;
        private StoreManager store;
        private OrderManager orderManager;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("[NextOrderPanelTestRoot]");

            var storeGo = new GameObject("StoreManager");
            storeGo.transform.SetParent(testRoot.transform);
            store = storeGo.AddComponent<StoreManager>();
            store.InitializeStore(StoreConfiguration.CreateQuickCommerce6AisleConfig());

            var orderMgrGo = new GameObject("OrderManager");
            orderMgrGo.transform.SetParent(testRoot.transform);
            orderManager = orderMgrGo.AddComponent<OrderManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        [Test]
        public void ResolveNextPendingOrder_NullOrderManager_ReturnsNullAndNoUpcomingOrdersMessage()
        {
            var result = SimulationHUD.ResolveNextPendingOrder(null, out string title, out string subtitle, out bool isQueued);

            Assert.IsNull(result);
            Assert.AreEqual("NEXT ORDER | NO UPCOMING ORDERS", title);
            Assert.AreEqual("OrderManager is not active.", subtitle);
            Assert.IsFalse(isQueued);
        }

        [Test]
        public void ResolveNextPendingOrder_ActivePendingOrder_ReturnsOrderWithIdAndItemCount()
        {
            var config = new OrderGenerationConfig(seed: 1001, itemCount: 8, orderCount: 1, DuplicateSkuPolicy.DisallowDuplicates);
            var orders = OrderGenerator.GenerateBatch(store, config);
            Assert.AreEqual(1, orders.Count);

            orderManager.DispatchOrder(orders[0]);

            var result = SimulationHUD.ResolveNextPendingOrder(orderManager, out string title, out string subtitle, out bool isQueued);

            Assert.IsNotNull(result);
            Assert.AreEqual(orders[0].OrderId, result.OrderId);
            Assert.IsTrue(title.Contains(orders[0].OrderId), $"Title '{title}' should contain OrderId '{orders[0].OrderId}'");
            Assert.IsTrue(subtitle.Contains("TOTAL ITEMS: 8"), $"Subtitle '{subtitle}' should state total items");
            Assert.IsFalse(isQueued);
        }

        [Test]
        public void ResolveNextPendingOrder_ActiveCompletedOrder_WithQueuedSimOrder_AdvancesToNextQueuedOrder()
        {
            var config = new OrderGenerationConfig(seed: 1001, itemCount: 4, orderCount: 3, DuplicateSkuPolicy.DisallowDuplicates);
            bool started = orderManager.TryStartMultiOrderSimulation(config, out MultiOrderSimulation sim, out string error);
            Assert.IsTrue(started, $"Simulation start failed: {error}");
            Assert.IsNotNull(sim);
            Assert.AreEqual(3, sim.TotalOrdersCount);

            // Active order is order 0
            var firstOrder = orderManager.ActiveOrder;
            Assert.IsNotNull(firstOrder);

            // Pick all items of the first order
            while (!firstOrder.IsAllItemsPicked)
            {
                var pickRes = orderManager.AttemptPick(firstOrder.CurrentTargetItem.SkuId);
                Assert.AreEqual(PickResult.Success, pickRes);
            }
            bool packed = orderManager.CompletePacking(out string packErr);
            Assert.IsTrue(packed, $"Packing failed: {packErr}");
            Assert.AreEqual(OrderStatus.Completed, firstOrder.Status);

            // Now ResolveNextPendingOrder should advance to Order 1 (dispatched automatically by multi-order sim)
            var nextResult = SimulationHUD.ResolveNextPendingOrder(orderManager, out string title, out string subtitle, out bool isQueued);

            Assert.IsNotNull(nextResult, "Expected next order to be returned after active order completion.");
            Assert.AreEqual(sim.Orders[1].OrderId, nextResult.OrderId);
            Assert.IsTrue(title.Contains(sim.Orders[1].OrderId), $"Title '{title}' should contain OrderId '{sim.Orders[1].OrderId}'");
        }

        [Test]
        public void ResolveNextPendingOrder_AllOrdersCompleted_ReturnsNullAndNoUpcomingOrdersMessage()
        {
            var config = new OrderGenerationConfig(seed: 1001, itemCount: 2, orderCount: 1, DuplicateSkuPolicy.DisallowDuplicates);
            orderManager.TryStartMultiOrderSimulation(config, out MultiOrderSimulation sim, out _);

            var activeOrder = orderManager.ActiveOrder;
            while (!activeOrder.IsAllItemsPicked)
            {
                orderManager.AttemptPick(activeOrder.CurrentTargetItem.SkuId);
            }
            orderManager.CompletePacking(out _);

            var result = SimulationHUD.ResolveNextPendingOrder(orderManager, out string title, out string subtitle, out bool isQueued);

            Assert.IsNull(result);
            Assert.AreEqual("NEXT ORDER | NO UPCOMING ORDERS", title);
            Assert.IsTrue(subtitle.Contains("Completed") || subtitle.Contains("fulfilled"), $"Subtitle '{subtitle}' should state completion.");
            Assert.IsFalse(isQueued);
        }

        [Test]
        public void ResolveProductLocation_LiveStoreSlot_ExtractsAisleAndSlotId()
        {
            // Pick a known SKU from the store
            string testSkuId = "SKU-0001";
            var sku = store.GetSku(testSkuId);
            Assert.IsNotNull(sku);

            bool hasSlot = store.TryGetSlotForSku(testSkuId, out ShelfSlot slot);
            Assert.IsTrue(hasSlot);
            Assert.IsNotNull(slot);

            var item = new OrderItem(testSkuId, sku.Name, slot.SlotId, slot.WorldPosition, 1);

            SimulationHUD.ResolveProductLocation(item, out string aisleStr, out string rackSlotStr);

            Assert.AreEqual($"A{slot.AisleNumber:D2}", aisleStr);
            char sideChar = slot.Side == ShelfSide.Left ? 'L' : 'R';
            Assert.AreEqual($"{sideChar}-B{slot.BayIndex:D2}-T{slot.TierLevel:D2}-S{slot.SlotIndex:D2}", rackSlotStr);
        }

        [Test]
        public void ResolveProductLocation_FallbackLocationString_ParsesCorrectly()
        {
            // Using an uncatalogued SKU so StoreManager.TryGetSlotForSku returns false and triggers fallback
            string fallbackSlotId = "A04-L-B02-T03-S05";
            var item = new OrderItem("SKU-FALLBACK-999", "Special Fallback Product", fallbackSlotId, Vector3.zero, 2);

            SimulationHUD.ResolveProductLocation(item, out string aisleStr, out string rackSlotStr);

            Assert.AreEqual("A04", aisleStr);
            Assert.AreEqual("L-B02-T03-S05", rackSlotStr);
        }

        [Test]
        public void ResolveProductLocation_NullItem_ReturnsPlaceholdersSafely()
        {
            SimulationHUD.ResolveProductLocation(null, out string aisleStr, out string rackSlotStr);

            Assert.AreEqual("---", aisleStr);
            Assert.AreEqual("UNASSIGNED", rackSlotStr);
        }
    }
}
