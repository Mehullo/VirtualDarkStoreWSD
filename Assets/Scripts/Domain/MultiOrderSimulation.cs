using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.Domain
{
    public enum MultiOrderSimulationStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Pure domain entity coordinating a multi-order simulation session.
    /// Manages an ordered sequence of independent orders, tracking progression from Order 1 to Order N.
    /// Preserves individual order measurability while calculating true aggregate simulation metrics without fabrication.
    /// </summary>
    [Serializable]
    public class MultiOrderSimulation
    {
        [SerializeField] private string simulationId;
        [SerializeField] private OrderGenerationConfig config;
        [SerializeField] private MultiOrderSimulationStatus status = MultiOrderSimulationStatus.Pending;
        [SerializeField] private List<Order> orders = new List<Order>();
        [SerializeField] private List<Order> completedOrders = new List<Order>();
        [SerializeField] private int currentOrderIndex = 0;

        public string SimulationId => simulationId;
        public OrderGenerationConfig Config => config;
        public MultiOrderSimulationStatus Status => status;
        public IReadOnlyList<Order> Orders => orders;
        public IReadOnlyList<Order> CompletedOrders => completedOrders;
        public int CurrentOrderIndex => currentOrderIndex;
        public int TotalOrdersCount => orders.Count;
        public int CompletedOrdersCount => completedOrders.Count;
        public int TotalRequestedItemSelections => orders.Count * (config != null ? config.ItemCount : 0);

        public bool IsActive => status == MultiOrderSimulationStatus.InProgress;
        public bool IsComplete => status == MultiOrderSimulationStatus.Completed;
        public bool HasNextOrder => currentOrderIndex + 1 < orders.Count;

        public Order ActiveOrder => (orders.Count > 0 && currentOrderIndex >= 0 && currentOrderIndex < orders.Count)
            ? orders[currentOrderIndex]
            : null;

        public MultiOrderSimulation() { }

        public MultiOrderSimulation(OrderGenerationConfig config, List<Order> generatedOrders, string id = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (generatedOrders == null || generatedOrders.Count == 0)
                throw new ArgumentException("MultiOrderSimulation requires at least one order.", nameof(generatedOrders));

            this.config = config.Clone();
            this.orders = new List<Order>(generatedOrders);
            this.completedOrders = new List<Order>();
            this.simulationId = string.IsNullOrWhiteSpace(id)
                ? $"SIM-{config.Seed}-{orders.Count}x{config.ItemCount}"
                : id.Trim();
            this.status = MultiOrderSimulationStatus.InProgress;
            this.currentOrderIndex = 0;
        }

        /// <summary>
        /// Records an order as successfully completed within this simulation batch.
        /// </summary>
        public void RecordOrderCompleted(Order order)
        {
            if (order == null) return;
            if (!completedOrders.Contains(order))
            {
                completedOrders.Add(order);
            }
        }

        /// <summary>
        /// Advances the simulation to the next order in sequence.
        /// Returns the newly active Order, or null if all orders are finished.
        /// </summary>
        public Order AdvanceToNextOrder()
        {
            if (HasNextOrder)
            {
                currentOrderIndex++;
                return ActiveOrder;
            }

            status = MultiOrderSimulationStatus.Completed;
            return null;
        }

        /// <summary>
        /// Marks the multi-order simulation as complete.
        /// </summary>
        public void Complete()
        {
            status = MultiOrderSimulationStatus.Completed;
        }

        /// <summary>
        /// Cancels or aborts the simulation.
        /// </summary>
        public void Cancel()
        {
            status = MultiOrderSimulationStatus.Cancelled;
        }
    }
}
