using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Management;

namespace VirtualDarkStore.Tests.Domain
{
    [TestFixture]
    public class OrderFulfillmentTests
    {
        private GameObject testHost;
        private StoreManager storeManager;
        private OrderManager orderManager;

        [SetUp]
        public void SetUp()
        {
            testHost = new GameObject("[Test_OrderFulfillment]");
            storeManager = testHost.AddComponent<StoreManager>();
            storeManager.InitializeStore();

            orderManager = testHost.AddComponent<OrderManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testHost != null)
            {
                UnityEngine.Object.DestroyImmediate(testHost);
            }
        }

        [Test]
        public void OrderItem_InitialState_AndPickRecording()
        {
            var item = new OrderItem("DAI-001", "Whole Milk 1L", "A01-L-B01-T01-S01", Vector3.zero, 2);

            Assert.AreEqual("DAI-001", item.SkuId);
            Assert.AreEqual(2, item.RequiredQuantity);
            Assert.AreEqual(0, item.PickedQuantity);
            Assert.IsFalse(item.IsFulfilled);

            item.RecordPick(1);
            Assert.AreEqual(1, item.PickedQuantity);
            Assert.IsFalse(item.IsFulfilled);

            item.RecordPick(1);
            Assert.AreEqual(2, item.PickedQuantity);
            Assert.IsTrue(item.IsFulfilled);

            // Overpick attempt should be rejected
            bool overpick = item.RecordPick(1);
            Assert.IsFalse(overpick);
            Assert.AreEqual(2, item.PickedQuantity);
        }

        [Test]
        public void Order_Validation_RejectsWrongSku_WithoutAdvancingOrCounting()
        {
            var items = new List<OrderItem>
            {
                new OrderItem("DAI-001", "Whole Milk 1L", "A01-L-B01-T01-S01", Vector3.zero, 1),
                new OrderItem("BEV-001", "Mineral Water 1L", "A01-L-B01-T01-S02", Vector3.zero, 1)
            };

            var order = new Order("ORD-TEST-01", items);

            Assert.AreEqual(OrderStatus.InProgress, order.Status);
            Assert.AreEqual("DAI-001", order.CurrentTargetItem.SkuId);

            // Attempt to pick wrong SKU
            bool result = order.ValidateAndPick("SNK-001", out string failureReason);

            Assert.IsFalse(result, "Wrong SKU pick must strictly return false.");
            Assert.IsNotNull(failureReason);
            Assert.IsTrue(failureReason.Contains("Mismatched SKU"), $"Failure reason should identify mismatch: {failureReason}");

            // Verify order state was NOT corrupted
            Assert.AreEqual(0, items[0].PickedQuantity, "Wrong item must never increment picked quantity.");
            Assert.IsFalse(items[0].IsFulfilled, "Target item must not be marked fulfilled.");
            Assert.AreEqual("DAI-001", order.CurrentTargetItem.SkuId, "Target item must remain on the unfulfilled item.");
            Assert.AreEqual(0, order.CurrentItemIndex, "Current item index must not advance.");
            Assert.IsFalse(order.IsAllItemsPicked);
        }

        [Test]
        public void Order_Validation_RejectsNullOrEmptySku()
        {
            var items = new List<OrderItem>
            {
                new OrderItem("DAI-001", "Whole Milk 1L", "A01-L-B01-T01-S01", Vector3.zero, 1)
            };
            var order = new Order("ORD-TEST-EMPTY", items);

            Assert.IsFalse(order.ValidateAndPick(null, out _));
            Assert.IsFalse(order.ValidateAndPick("", out _));
            Assert.IsFalse(order.ValidateAndPick("   ", out _));
            Assert.AreEqual(0, items[0].PickedQuantity);
        }

        [Test]
        public void Order_Validation_AcceptsCorrectSku_AdvancesSequentially()
        {
            var items = new List<OrderItem>
            {
                new OrderItem("DAI-001", "Whole Milk 1L", "A01-L-B01-T01-S01", Vector3.zero, 1),
                new OrderItem("BEV-001", "Mineral Water 1L", "A01-L-B01-T01-S02", Vector3.zero, 1)
            };
            var order = new Order("ORD-TEST-02", items);

            // Pick item 1 correctly
            bool pick1 = order.ValidateAndPick("DAI-001", out string fail1);
            Assert.IsTrue(pick1, $"Pick 1 should succeed: {fail1}");
            Assert.IsTrue(items[0].IsFulfilled);
            Assert.AreEqual(1, order.CurrentItemIndex);
            Assert.AreEqual("BEV-001", order.CurrentTargetItem.SkuId);
            Assert.AreEqual(OrderStatus.InProgress, order.Status);

            // Pick item 2 correctly
            bool pick2 = order.ValidateAndPick("BEV-001", out string fail2);
            Assert.IsTrue(pick2, $"Pick 2 should succeed: {fail2}");
            Assert.IsTrue(items[1].IsFulfilled);
            Assert.IsTrue(order.IsAllItemsPicked);
            Assert.AreEqual(OrderStatus.AwaitingPacking, order.Status, "Order must transition to AwaitingPacking once all items picked.");
        }

        [Test]
        public void Order_FinalizePacking_RejectsPrematurePacking()
        {
            var items = new List<OrderItem>
            {
                new OrderItem("DAI-001", "Whole Milk 1L", "A01-L-B01-T01-S01", Vector3.zero, 1)
            };
            var order = new Order("ORD-TEST-03", items);

            // Attempt to finalize before picking
            bool packResult = order.FinalizePacking(out string failureReason);
            Assert.IsFalse(packResult, "Packing must be rejected when items have not been picked.");
            Assert.IsNotNull(failureReason);
            Assert.AreEqual(OrderStatus.InProgress, order.Status);
        }

        [Test]
        public void Order_FinalizePacking_SucceedsWhenAwaitingPacking()
        {
            var items = new List<OrderItem>
            {
                new OrderItem("DAI-001", "Whole Milk 1L", "A01-L-B01-T01-S01", Vector3.zero, 1)
            };
            var order = new Order("ORD-TEST-04", items);
            order.ValidateAndPick("DAI-001", out _);

            Assert.AreEqual(OrderStatus.AwaitingPacking, order.Status);

            bool packResult = order.FinalizePacking(out string failureReason);
            Assert.IsTrue(packResult, $"Packing should succeed: {failureReason}");
            Assert.AreEqual(OrderStatus.Completed, order.Status);
        }

        [Test]
        public void OrderManager_CompleteFulfillmentCycle_EndToEnd()
        {
            // Pick 3 specific verified SKUs from the store catalog
            var skuIds = new List<string> { "SKU-0001", "SKU-0011", "SKU-0021" };
            var order = orderManager.StartSpecificOrder("ORD-E2E-001", skuIds);

            Assert.IsNotNull(order);
            Assert.AreEqual("ORD-E2E-001", order.OrderId);
            Assert.AreEqual(3, order.TotalItemCount);
            Assert.AreEqual(OrderStatus.InProgress, order.Status);
            Assert.AreEqual(0, orderManager.CompletedOrdersCount);

            // 1. Attempt Wrong SKU Pick
            var wrongResult = orderManager.AttemptPick("WRONG-SKU-999");
            Assert.AreEqual(PickResult.WrongItem, wrongResult);
            Assert.AreEqual(0, order.Items[0].PickedQuantity);
            Assert.AreEqual("SKU-0001", order.CurrentTargetItem.SkuId);

            // 2. Pick Item 1 (SKU-0001)
            var pick1 = orderManager.AttemptPick("SKU-0001");
            Assert.AreEqual(PickResult.Success, pick1);
            Assert.IsTrue(order.Items[0].IsFulfilled);
            Assert.AreEqual("SKU-0011", order.CurrentTargetItem.SkuId);

            // 3. Attempt Premature Packing
            bool prematurePack = orderManager.CompletePacking(out string prematureReason);
            Assert.IsFalse(prematurePack);
            Assert.IsNotNull(prematureReason);

            // 4. Pick Item 2 (SKU-0011)
            var pick2 = orderManager.AttemptPick("SKU-0011");
            Assert.AreEqual(PickResult.Success, pick2);
            Assert.IsTrue(order.Items[1].IsFulfilled);
            Assert.AreEqual("SKU-0021", order.CurrentTargetItem.SkuId);

            // 5. Pick Item 3 (SKU-0021)
            var pick3 = orderManager.AttemptPick("SKU-0021");
            Assert.AreEqual(PickResult.Success, pick3);
            Assert.IsTrue(order.Items[2].IsFulfilled);
            Assert.IsTrue(order.IsAllItemsPicked);
            Assert.IsTrue(orderManager.IsAwaitingPacking);

            // 6. Complete and Finalize Packing at Packing Bench
            bool finalPack = orderManager.CompletePacking(out string packReason);
            Assert.IsTrue(finalPack, $"Final packing should succeed: {packReason}");
            Assert.AreEqual(1, orderManager.CompletedOrdersCount);
            Assert.AreEqual(OrderStatus.Completed, order.Status);
        }
    }
}
