using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.Management;

namespace VirtualDarkStore.Domain
{
    public enum DuplicateSkuPolicy
    {
        DisallowDuplicates,
        AllowDuplicates
    }

    /// <summary>
    /// Configuration for deterministic order generation in work-study experiments.
    /// Supports preset reproduction, random orders, and randomized batches.
    /// </summary>
    [Serializable]
    public class OrderGenerationConfig
    {
        [SerializeField] private int seed = 1001;
        [SerializeField] private int itemCount = 3;
        [SerializeField] private int orderCount = 1;
        [SerializeField] private DuplicateSkuPolicy duplicatePolicy = DuplicateSkuPolicy.DisallowDuplicates;
        [SerializeField] private string orderIdPrefix = "ORD-GEN";

        public int Seed
        {
            get => seed;
            set => seed = value;
        }

        public int ItemCount
        {
            get => itemCount;
            set => itemCount = value;
        }

        public int OrderCount
        {
            get => orderCount;
            set => orderCount = value;
        }

        public DuplicateSkuPolicy DuplicatePolicy
        {
            get => duplicatePolicy;
            set => duplicatePolicy = value;
        }

        public string OrderIdPrefix
        {
            get => orderIdPrefix;
            set => orderIdPrefix = string.IsNullOrWhiteSpace(value) ? "ORD-GEN" : value.Trim();
        }

        public OrderGenerationConfig() { }

        public OrderGenerationConfig(int seed, int itemCount = 3, int orderCount = 1, DuplicateSkuPolicy duplicatePolicy = DuplicateSkuPolicy.DisallowDuplicates, string prefix = "ORD-GEN")
        {
            this.seed = seed;
            this.itemCount = itemCount;
            this.orderCount = orderCount;
            this.duplicatePolicy = duplicatePolicy;
            this.orderIdPrefix = string.IsNullOrWhiteSpace(prefix) ? "ORD-GEN" : prefix.Trim();
        }

        public OrderGenerationConfig Clone()
        {
            return new OrderGenerationConfig(seed, itemCount, orderCount, duplicatePolicy, orderIdPrefix);
        }

        /// <summary>
        /// Validates simulation setup parameters against store catalog inventory and policy rules.
        /// Returns true if valid; otherwise returns false with a descriptive validation error message.
        /// </summary>
        public bool Validate(StoreManager store, out string validationError)
        {
            validationError = null;

            if (orderCount <= 0)
            {
                validationError = $"Number of Orders must be at least 1 (got {orderCount}).";
                return false;
            }

            if (itemCount <= 0)
            {
                validationError = $"Items per Order must be at least 1 (got {itemCount}).";
                return false;
            }

            if (store == null || store.RegisteredSkuCount == 0)
            {
                validationError = "StoreManager is not initialized or has no registered SKUs.";
                return false;
            }

            int availableSkus = store.RegisteredSkuCount;
            if (duplicatePolicy == DuplicateSkuPolicy.DisallowDuplicates && itemCount > availableSkus)
            {
                validationError = $"Items per Order ({itemCount}) exceeds available valid SKU catalog capacity ({availableSkus} unique SKUs) under DisallowDuplicates policy.";
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Pure domain generator for repeatable, controlled orders without external dependencies or AI/ML.
    /// Uses System.Random with an explicit seed for strict cross-run determinism.
    /// Validates all generated SKUs against the store catalog and resolves their physical shelf locations.
    /// </summary>
    public static class OrderGenerator
    {
        /// <summary>
        /// Generates a single deterministic Order using the provided configuration and store catalog.
        /// Guaranteed: Same seed + same configuration + same store catalog produces the identical order.
        /// </summary>
        public static Order GenerateOrder(StoreManager store, OrderGenerationConfig config, int orderIndex = 0)
        {
            if (store == null) throw new ArgumentNullException(nameof(store), "StoreManager is required to resolve SKUs and shelf locations.");
            if (config == null) throw new ArgumentNullException(nameof(config), "OrderGenerationConfig cannot be null.");

            var allSkus = store.GetAllSkus();
            if (allSkus == null || allSkus.Count == 0)
            {
                throw new InvalidOperationException("StoreManager has no registered SKUs in catalog.");
            }

            if (config.DuplicatePolicy == DuplicateSkuPolicy.DisallowDuplicates && config.ItemCount > allSkus.Count)
            {
                throw new ArgumentException(
                    $"Requested items per order ({config.ItemCount}) exceeds available catalog capacity ({allSkus.Count}) under DisallowDuplicates policy.",
                    nameof(config));
            }

            int targetItemCount = config.ItemCount;

            // Deterministic random generator for this specific order index
            int effectiveSeed = unchecked(config.Seed + (orderIndex * 31));
            var rng = new System.Random(effectiveSeed);

            var selectedSkus = new List<Sku>();

            if (config.DuplicatePolicy == DuplicateSkuPolicy.DisallowDuplicates)
            {
                // Sampling without replacement using Fisher-Yates selection
                var available = new List<Sku>(allSkus);
                for (int i = 0; i < targetItemCount && available.Count > 0; i++)
                {
                    int chosenIndex = rng.Next(available.Count);
                    selectedSkus.Add(available[chosenIndex]);
                    available.RemoveAt(chosenIndex);
                }
            }
            else
            {
                // Sampling with replacement
                for (int i = 0; i < targetItemCount; i++)
                {
                    int chosenIndex = rng.Next(allSkus.Count);
                    selectedSkus.Add(allSkus[chosenIndex]);
                }
            }

            // Build OrderItems resolving exact physical locations
            var orderItems = new List<OrderItem>();
            for (int i = 0; i < selectedSkus.Count; i++)
            {
                var sku = selectedSkus[i];
                string slotId = "UNASSIGNED";
                Vector3 slotPos = Vector3.zero;

                if (store.TryResolveSkuLocation(sku.SkuId, out string resolvedSlotId, out Vector3 resolvedPos))
                {
                    slotId = resolvedSlotId;
                    slotPos = resolvedPos;
                }
                else if (store.TryGetSlotForSku(sku.SkuId, out ShelfSlot slot) && slot != null)
                {
                    slotId = slot.SlotId;
                    slotPos = slot.WorldPosition;
                }

                orderItems.Add(new OrderItem(sku.SkuId, sku.Name, slotId, slotPos, 1));
            }

            string orderId = $"{config.OrderIdPrefix}-{config.Seed}-{orderIndex + 1:D3}";
            return new Order(orderId, orderItems);
        }

        /// <summary>
        /// Generates a batch of deterministic orders based on OrderCount.
        /// Validates configuration parameters against store catalog before generation.
        /// </summary>
        public static List<Order> GenerateBatch(StoreManager store, OrderGenerationConfig config)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (config == null) throw new ArgumentNullException(nameof(config));

            if (!config.Validate(store, out string valErr))
            {
                throw new ArgumentException(valErr, nameof(config));
            }

            int count = config.OrderCount;
            var batch = new List<Order>(count);

            for (int i = 0; i < count; i++)
            {
                batch.Add(GenerateOrder(store, config, i));
            }

            return batch;
        }
    }
}
