using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.Domain
{
    public enum OrderStatus
    {
        Pending,
        InProgress,
        AwaitingPacking,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Pure domain entity representing a dark store fulfillment pick order.
    /// Manages sequential or batch line items, pick validation, and packing state.
    /// </summary>
    [Serializable]
    public class Order
    {
        [SerializeField] private string orderId;
        [SerializeField] private OrderStatus status = OrderStatus.Pending;
        [SerializeField] private List<OrderItem> items = new List<OrderItem>();
        [SerializeField] private int currentItemIndex = 0;

        public string OrderId => orderId;
        public OrderStatus Status => status;
        public IReadOnlyList<OrderItem> Items => items;
        public int CurrentItemIndex => currentItemIndex;
        public int TotalItemCount => items.Count;

        public OrderItem CurrentTargetItem
        {
            get
            {
                if (currentItemIndex >= 0 && currentItemIndex < items.Count)
                {
                    return items[currentItemIndex];
                }
                return null;
            }
        }

        public bool IsAllItemsPicked
        {
            get
            {
                if (items.Count == 0) return false;
                for (int i = 0; i < items.Count; i++)
                {
                    if (!items[i].IsFulfilled) return false;
                }
                return true;
            }
        }

        public Order() { }

        public Order(string id, List<OrderItem> orderItems)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Order ID cannot be null or empty.", nameof(id));
            if (orderItems == null || orderItems.Count == 0) throw new ArgumentException("Order must contain at least one line item.", nameof(orderItems));

            orderId = id.Trim();
            items = new List<OrderItem>(orderItems);
            status = OrderStatus.InProgress;
            currentItemIndex = 0;
        }

        /// <summary>
        /// Validates a pick attempt against the active target line item.
        /// Strictly rejects wrong or missing SKUs; never silently counts incorrect items.
        /// </summary>
        public bool ValidateAndPick(string attemptedSkuId, out string failureReason)
        {
            failureReason = null;

            if (status != OrderStatus.InProgress)
            {
                failureReason = $"Cannot pick items for order in status '{status}'.";
                return false;
            }

            var target = CurrentTargetItem;
            if (target == null)
            {
                failureReason = "No active item remaining to pick in this order.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(attemptedSkuId))
            {
                failureReason = "No SKU was provided for the pick attempt.";
                return false;
            }

            if (!string.Equals(target.SkuId, attemptedSkuId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                failureReason = $"Mismatched SKU! Expected '{target.SkuId}' ('{target.SkuName}'), but attempted '{attemptedSkuId}'.";
                return false;
            }

            if (target.IsFulfilled)
            {
                failureReason = $"Line item for SKU '{target.SkuId}' is already completely fulfilled.";
                return false;
            }

            // Record successful pick
            target.RecordPick(1);

            // Advance to next unfulfilled item if current target is satisfied
            if (target.IsFulfilled)
            {
                AdvanceToNextUnfulfilledItem();
            }

            // If all items are now fulfilled, transition order to AwaitingPacking
            if (IsAllItemsPicked)
            {
                status = OrderStatus.AwaitingPacking;
            }

            return true;
        }

        private void AdvanceToNextUnfulfilledItem()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (!items[i].IsFulfilled)
                {
                    currentItemIndex = i;
                    return;
                }
            }
            currentItemIndex = items.Count; // All items picked
        }

        /// <summary>
        /// Finalizes order packing at the staging station.
        /// </summary>
        public bool FinalizePacking(out string failureReason)
        {
            failureReason = null;

            if (status != OrderStatus.AwaitingPacking)
            {
                if (!IsAllItemsPicked)
                {
                    failureReason = "Cannot complete packing: not all items have been picked.";
                }
                else
                {
                    failureReason = $"Cannot finalize packing for order in status '{status}'.";
                }
                return false;
            }

            status = OrderStatus.Completed;
            return true;
        }

        public override string ToString() => $"[Order {orderId}] Status: {status}, Items: {items.Count}, Current: {currentItemIndex + 1}/{items.Count}";
    }
}
