using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.Domain;

namespace VirtualDarkStore.Management
{
    public enum PickResult
    {
        Success,
        WrongItem,
        NoActiveOrder,
        AlreadyFulfilled,
        InvalidItem
    }

    /// <summary>
    /// Central manager for dark store order dispatch, pick list progression, and fulfillment completion.
    /// Strictly enforces item validation: incorrect or missing SKUs never count as correct picks.
    /// </summary>
    public class OrderManager : MonoBehaviour
    {
        private static OrderManager instance;
        public static OrderManager Instance => instance != null ? instance : (instance = FindAnyObjectByType<OrderManager>());

        [Header("Runtime State")]
        [SerializeField] private Order activeOrder;
        [SerializeField] private int completedOrdersCount = 0;
        [SerializeField] private bool autoStartSampleOrderOnAwake = true;

        public Order ActiveOrder => activeOrder;
        public int CompletedOrdersCount => completedOrdersCount;
        public bool HasActiveOrder => activeOrder != null && activeOrder.Status == OrderStatus.InProgress;
        public bool IsAwaitingPacking => activeOrder != null && activeOrder.Status == OrderStatus.AwaitingPacking;

        // Events
        public event Action<Order> OnOrderStarted;
        public event Action<OrderItem, bool> OnItemPicked;
        public event Action<string, string, string> OnPickFailed; // expectedSku, attemptedSku, reason
        public event Action<Order> OnOrderCompleted;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            if (autoStartSampleOrderOnAwake && activeOrder == null)
            {
                // Start a verified 3-item sample order
                StartSampleOrder(3);
            }
        }

        /// <summary>
        /// Starts a new order with randomly selected distinct SKUs from the store catalog.
        /// </summary>
        public Order StartSampleOrder(int itemCount = 3)
        {
            var store = StoreManager.Instance;
            if (store == null || store.RegisteredSkuCount == 0)
            {
                Debug.LogWarning("[OrderManager] StoreManager is not initialized or catalog is empty.");
                return null;
            }

            var allSkus = store.GetAllSkus();
            int count = Mathf.Clamp(itemCount, 1, allSkus.Count);

            // Select distinct SKUs
            var selectedSkus = new List<Sku>();
            var availableIndices = new List<int>();
            for (int i = 0; i < allSkus.Count; i++) availableIndices.Add(i);

            for (int i = 0; i < count; i++)
            {
                int r = UnityEngine.Random.Range(0, availableIndices.Count);
                int skuIndex = availableIndices[r];
                availableIndices.RemoveAt(r);
                selectedSkus.Add(allSkus[skuIndex]);
            }

            string orderId = $"ORD-{(completedOrdersCount + 1):D4}";
            return CreateAndDispatchOrder(orderId, selectedSkus);
        }

        /// <summary>
        /// Starts a specific order deterministically by explicit SKU IDs (useful for unit testing and repeatability).
        /// </summary>
        public Order StartSpecificOrder(string orderId, IReadOnlyList<string> skuIds)
        {
            var store = StoreManager.Instance;
            if (store == null)
            {
                throw new InvalidOperationException("StoreManager is not initialized.");
            }

            var selectedSkus = new List<Sku>();
            foreach (var id in skuIds)
            {
                var sku = store.GetSku(id);
                if (sku == null)
                {
                    throw new ArgumentException($"SKU '{id}' does not exist in store catalog.", nameof(skuIds));
                }
                selectedSkus.Add(sku);
            }

            return CreateAndDispatchOrder(orderId, selectedSkus);
        }

        private Order CreateAndDispatchOrder(string orderId, List<Sku> skus)
        {
            var store = StoreManager.Instance;
            var orderItems = new List<OrderItem>();

            foreach (var sku in skus)
            {
                string slotId = "UNKNOWN";
                Vector3 slotPos = Vector3.zero;

                if (store.TryGetSlotForSku(sku.SkuId, out ShelfSlot slot))
                {
                    slotId = slot.SlotId;
                    slotPos = slot.WorldPosition;
                }

                orderItems.Add(new OrderItem(sku.SkuId, sku.Name, slotId, slotPos, 1));
            }

            activeOrder = new Order(orderId, orderItems);
            Debug.Log($"[OrderManager] Dispatched new order: {activeOrder}");
            OnOrderStarted?.Invoke(activeOrder);
            return activeOrder;
        }

        /// <summary>
        /// Validates and records a pick attempt against the active order.
        /// Strictly fails if attempted SKU does not match expected SKU.
        /// </summary>
        public PickResult AttemptPick(string attemptedSkuId)
        {
            if (activeOrder == null || activeOrder.Status != OrderStatus.InProgress)
            {
                Debug.LogWarning("[OrderManager] Pick rejected: No active order in progress.");
                return PickResult.NoActiveOrder;
            }

            var target = activeOrder.CurrentTargetItem;
            string expectedSkuId = target != null ? target.SkuId : "NONE";

            if (activeOrder.ValidateAndPick(attemptedSkuId, out string failureReason))
            {
                bool orderFinishedPicking = activeOrder.IsAllItemsPicked;
                Debug.Log($"[OrderManager] Pick SUCCESS! Picked '{target.SkuName}' ({target.SkuId}). Order fulfilled: {orderFinishedPicking}");
                OnItemPicked?.Invoke(target, orderFinishedPicking);
                return PickResult.Success;
            }
            else
            {
                Debug.LogWarning($"[OrderManager] Pick REJECTED! {failureReason}");
                OnPickFailed?.Invoke(expectedSkuId, attemptedSkuId, failureReason);
                return PickResult.WrongItem;
            }
        }

        /// <summary>
        /// Finalizes packing for the active order at the dispatch packing bench.
        /// </summary>
        public bool CompletePacking(out string failureReason)
        {
            failureReason = null;

            if (activeOrder == null)
            {
                failureReason = "No order currently exists to pack.";
                return false;
            }

            if (activeOrder.FinalizePacking(out failureReason))
            {
                completedOrdersCount++;
                Debug.Log($"[OrderManager] Order '{activeOrder.OrderId}' successfully completed and packed!");
                OnOrderCompleted?.Invoke(activeOrder);
                return true;
            }

            Debug.LogWarning($"[OrderManager] Packing failed: {failureReason}");
            return false;
        }
    }
}
