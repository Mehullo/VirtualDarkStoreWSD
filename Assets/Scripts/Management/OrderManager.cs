using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Interaction;
using VirtualDarkStore.Routing;

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

        [Header("Multi-Order Simulation State")]
        [SerializeField] private MultiOrderSimulation activeSimulation;

        public Order ActiveOrder => activeOrder;
        public int CompletedOrdersCount => completedOrdersCount;
        public bool HasActiveOrder => activeOrder != null && activeOrder.Status == OrderStatus.InProgress;
        public bool IsAwaitingPacking => activeOrder != null && activeOrder.Status == OrderStatus.AwaitingPacking;
        public MultiOrderSimulation ActiveSimulation => activeSimulation;
        public bool HasActiveSimulation => activeSimulation != null && activeSimulation.IsActive;

        // Events
        public event Action<Order> OnOrderStarted;
        public event Action<OrderItem, bool> OnItemPicked;
        public event Action<string, string, string> OnPickFailed; // expectedSku, attemptedSku, reason
        public event Action<Order> OnOrderCompleted;
        public event Action<MultiOrderSimulation> OnSimulationStarted;
        public event Action<Order, int, int> OnSimulationOrderChanged; // order, currentOrderNum, totalOrders
        public event Action<MultiOrderSimulation> OnSimulationCompleted;

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

        /// <summary>
        /// Non-throwing validation and dispatch of a specific order by explicit SKU IDs.
        /// </summary>
        public bool TryStartSpecificOrder(string orderId, IReadOnlyList<string> skuIds, out Order order, out string errorReason)
        {
            order = null;
            errorReason = null;

            var store = StoreManager.Instance;
            if (store == null)
            {
                errorReason = "StoreManager is not initialized.";
                SimulationErrorHandler.ReportError("OrderManager", errorReason);
                return false;
            }

            if (string.IsNullOrWhiteSpace(orderId))
            {
                errorReason = "Order ID cannot be empty.";
                SimulationErrorHandler.ReportError("OrderManager", errorReason);
                return false;
            }

            if (skuIds == null || skuIds.Count == 0)
            {
                errorReason = "Order must contain at least one SKU ID.";
                SimulationErrorHandler.ReportError("OrderManager", errorReason);
                return false;
            }

            var selectedSkus = new List<Sku>();
            foreach (var id in skuIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    errorReason = "Order contains an empty SKU ID.";
                    SimulationErrorHandler.ReportError("OrderManager", errorReason);
                    return false;
                }
                var sku = store.GetSku(id.Trim());
                if (sku == null)
                {
                    errorReason = $"SKU '{id}' does not exist in store catalog.";
                    SimulationErrorHandler.ReportError("OrderManager", errorReason);
                    return false;
                }
                selectedSkus.Add(sku);
            }

            order = CreateAndDispatchOrder(orderId.Trim(), selectedSkus);
            return order != null;
        }

        /// <summary>
        /// Generates and dispatches a controlled deterministic order based on OrderGenerationConfig.
        /// </summary>
        public bool TryDispatchGeneratedOrder(OrderGenerationConfig config, out Order order, out string errorReason)
        {
            order = null;
            errorReason = null;

            var store = StoreManager.Instance;
            if (store == null)
            {
                errorReason = "StoreManager is not initialized.";
                SimulationErrorHandler.ReportError("OrderManager", errorReason);
                return false;
            }

            if (config == null)
            {
                errorReason = "OrderGenerationConfig cannot be null.";
                SimulationErrorHandler.ReportError("OrderManager", errorReason);
                return false;
            }

            try
            {
                order = OrderGenerator.GenerateOrder(store, config, completedOrdersCount);
                DispatchOrder(order);
                return true;
            }
            catch (Exception ex)
            {
                errorReason = ex.Message;
                SimulationErrorHandler.ReportError("OrderManager", $"Failed to generate order: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Directly dispatches an existing Order instance into the simulation runtime.
        /// </summary>
        public Order DispatchOrder(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            ResetShelfVisuals();
            activeOrder = order;
            Debug.Log($"[OrderManager] Dispatched new order: {activeOrder.OrderId} ({activeOrder.TotalItemCount} items)");
            OnOrderStarted?.Invoke(activeOrder);
            return activeOrder;
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

            ResetShelfVisuals();

            activeOrder = new Order(orderId, orderItems);
            Debug.Log($"[OrderManager] Dispatched new order: {activeOrder}");
            OnOrderStarted?.Invoke(activeOrder);
            return activeOrder;
        }

        /// <summary>
        /// Restocks all physical shelf product visuals in the scene so previously picked items are visible again.
        /// </summary>
        public void ResetShelfVisuals()
        {
            var visuals = FindObjectsByType<PickableItemVisual>(FindObjectsInactive.Include);
            if (visuals == null) return;
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i] != null)
                {
                    visuals[i].ResetVisual();
                }
            }
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
                SimulationErrorHandler.ReportWarning("OrderManager", failureReason);
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
                SimulationErrorHandler.ReportWarning("OrderManager", failureReason);
                return false;
            }

            if (activeOrder.FinalizePacking(out failureReason))
            {
                completedOrdersCount++;
                Debug.Log($"[OrderManager] Order '{activeOrder.OrderId}' successfully completed and packed!");
                OnOrderCompleted?.Invoke(activeOrder);

                // Multi-order simulation sequence progression
                if (activeSimulation != null && activeSimulation.IsActive)
                {
                    activeSimulation.RecordOrderCompleted(activeOrder);

                    if (activeSimulation.HasNextOrder)
                    {
                        var nextOrder = activeSimulation.AdvanceToNextOrder();
                        Debug.Log($"[OrderManager] Multi-Order Simulation advancing to Order {activeSimulation.CurrentOrderIndex + 1}/{activeSimulation.TotalOrdersCount}: {nextOrder.OrderId}");
                        DispatchOrder(nextOrder);
                        OnSimulationOrderChanged?.Invoke(nextOrder, activeSimulation.CurrentOrderIndex + 1, activeSimulation.TotalOrdersCount);
                    }
                    else
                    {
                        activeSimulation.Complete();
                        Debug.Log($"[OrderManager] Multi-Order Simulation COMPLETED! All {activeSimulation.TotalOrdersCount} orders fulfilled.");
                        OnSimulationCompleted?.Invoke(activeSimulation);
                    }
                }

                return true;
            }

            Debug.LogWarning($"[OrderManager] Packing failed: {failureReason}");
            SimulationErrorHandler.ReportWarning("OrderManager", failureReason);
            return false;
        }

        /// <summary>
        /// Validates, generates, and begins sequential execution of a configurable multi-order simulation.
        /// </summary>
        public bool TryStartMultiOrderSimulation(OrderGenerationConfig config, out MultiOrderSimulation simulation, out string errorReason)
        {
            simulation = null;
            errorReason = null;

            var store = StoreManager.Instance;
            if (store == null)
            {
                errorReason = "StoreManager is not initialized.";
                SimulationErrorHandler.ReportError("OrderManager", errorReason);
                return false;
            }

            if (config == null)
            {
                errorReason = "OrderGenerationConfig cannot be null.";
                SimulationErrorHandler.ReportError("OrderManager", errorReason);
                return false;
            }

            if (!config.Validate(store, out errorReason))
            {
                SimulationErrorHandler.ReportError("OrderManager", errorReason);
                return false;
            }

            try
            {
                var orders = OrderGenerator.GenerateBatch(store, config);
                simulation = new MultiOrderSimulation(config, orders);
                activeSimulation = simulation;

                Debug.Log($"[OrderManager] Starting Multi-Order Simulation: {simulation.SimulationId} ({simulation.TotalOrdersCount} orders, {config.ItemCount} items/order, Seed {config.Seed})");

                // Dispatch the first order
                DispatchOrder(simulation.ActiveOrder);

                OnSimulationStarted?.Invoke(activeSimulation);
                OnSimulationOrderChanged?.Invoke(simulation.ActiveOrder, 1, simulation.TotalOrdersCount);
                return true;
            }
            catch (Exception ex)
            {
                errorReason = ex.Message;
                SimulationErrorHandler.ReportError("OrderManager", $"Failed to start multi-order simulation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cancels and clears any active multi-order simulation.
        /// </summary>
        public void StopMultiOrderSimulation()
        {
            if (activeSimulation != null)
            {
                activeSimulation.Cancel();
                activeSimulation = null;
            }
        }

        /// <summary>
        /// Optimizes the pick-sequence route of the active order using the specified route optimizer (default: OR-Tools).
        /// If applyToActiveOrder is true, reorders the active order items prior to picking.
        /// </summary>
        public RouteOptimizationResult OptimizeActiveOrderRoute(IRouteOptimizer optimizer = null, bool applyToActiveOrder = true)
        {
            if (activeOrder == null)
            {
                return new RouteOptimizationResult
                {
                    Success = false,
                    SolverName = "None",
                    StatusMessage = "No active order to optimize."
                };
            }

            var result = StoreRouteManager.OptimizeOrder(activeOrder, null, null, optimizer);
            if (result.Success && applyToActiveOrder && result.SkuSequence != null && result.SkuSequence.Count > 0)
            {
                if (activeOrder.TryReorderItems(result.SkuSequence, out string reorderError))
                {
                    Debug.Log($"[OrderManager] Successfully reordered active order to optimized pick sequence: {result}");
                }
                else
                {
                    Debug.LogWarning($"[OrderManager] Route optimization succeeded but could not reorder items: {reorderError}");
                }
            }

            return result;
        }
    }
}
