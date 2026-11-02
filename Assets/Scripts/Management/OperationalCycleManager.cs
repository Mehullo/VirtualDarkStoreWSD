using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Player;
using VirtualDarkStore.Routing;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Management
{
    public enum OperationalStage
    {
        Entry,
        CollectOrder,
        Picking,
        AwaitingPacking,
        DepositOrder,
        WalkToExit,
        Completed
    }

    [Serializable]
    public class OperationalCycleSummary
    {
        public string OrderId;
        public int ItemCount;
        public bool IsOptimizedRoute;
        public float SessionStartTime;
        public float SessionEndTime;
        public float TotalCycleTime;
        public float TotalDistance;
        public int AisleTransitions;
        public int AisleBacktracks;
        public float AlgorithmicRouteCost;
        public bool IsOrderCompleted;
        public WorkStudyResults WorkStudy;
    }

    /// <summary>
    /// Central manager orchestrating the complete end-to-end quick-commerce operational cycle:
    /// ENTRY -> COLLECT ORDER -> PICKING -> AWAITING PACKING -> DEPOSIT ORDER -> WALK TO EXIT -> COMPLETED.
    /// Integrates the store environment, order system, picker controls, and WorkStudy instrumentation into one continuous loop.
    /// </summary>
    public class OperationalCycleManager : MonoBehaviour
    {
        private static OperationalCycleManager instance;
        public static OperationalCycleManager Instance => instance != null ? instance : (instance = FindAnyObjectByType<OperationalCycleManager>());

        [Header("Operational Waypoints & Zones")]
        [SerializeField] private Vector3 entryPosition = new Vector3(-8.0f, 0.0f, -10.5f);
        [SerializeField] private Vector3 collectionDeskPosition = new Vector3(-4.0f, 0.0f, -8.8f);
        [SerializeField] private Vector3 packingDeskPosition = new Vector3(2.0f, 0.0f, -8.8f);
        [SerializeField] private Vector3 exitPosition = new Vector3(8.0f, 0.0f, -10.5f);
        [SerializeField] private float zoneTriggerRadius = 2.2f;

        [Header("Runtime State")]
        [SerializeField] private OperationalStage currentStage = OperationalStage.Entry;
        [SerializeField] private bool useOptimizedRouting = false;
        [SerializeField] private float sessionStartTime = 0f;
        [SerializeField] private float sessionEndTime = 0f;
        [SerializeField] private Order activeOrder;

        private OrderGenerationConfig activeConfig;
        private RouteOptimizationResult lastRoutingResult;
        private int eventSequence = 0;
        private Transform activePlayerTransform;

        public OperationalStage CurrentStage => currentStage;
        public bool UseOptimizedRouting
        {
            get => useOptimizedRouting;
            set => useOptimizedRouting = value;
        }
        public float SessionStartTime => sessionStartTime;
        public float SessionEndTime => sessionEndTime;
        public float TotalCycleTime => (sessionEndTime > sessionStartTime) ? (sessionEndTime - sessionStartTime) : (Time.time - sessionStartTime);
        public Order ActiveOrder => activeOrder;
        public RouteOptimizationResult LastRoutingResult => lastRoutingResult;
        public Vector3 EntryPosition => entryPosition;
        public Vector3 CollectionDeskPosition => collectionDeskPosition;
        public Vector3 PackingDeskPosition => packingDeskPosition;
        public Vector3 ExitPosition => exitPosition;

        // Events
        public event Action<OperationalStage> OnStageChanged;
        public event Action<OperationalCycleSummary> OnCycleCompleted;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void SubscribeOrderEvents()
        {
            var om = OrderManager.Instance;
            if (om != null)
            {
                om.OnItemPicked -= HandleItemPicked;
                om.OnItemPicked += HandleItemPicked;
                om.OnOrderCompleted -= HandleOrderCompleted;
                om.OnOrderCompleted += HandleOrderCompleted;
            }
        }

        public void UnsubscribeOrderEvents()
        {
            var om = OrderManager.Instance;
            if (om != null)
            {
                om.OnItemPicked -= HandleItemPicked;
                om.OnOrderCompleted -= HandleOrderCompleted;
            }
        }

        private void OnEnable()
        {
            SubscribeOrderEvents();
        }

        private void OnDisable()
        {
            UnsubscribeOrderEvents();
        }

        private void Start()
        {
            FindPlayerReference();
        }

        private void Update()
        {
            FindPlayerReference();
            CheckZoneProximity();
        }

        private void FindPlayerReference()
        {
            if (activePlayerTransform != null) return;
            var player = FindAnyObjectByType<DesktopPickerController>(FindObjectsInactive.Include);
            if (player != null)
            {
                activePlayerTransform = player.transform;
                return;
            }
            var vr = FindAnyObjectByType<VirtualDarkStore.VR.VRPlayerRig>(FindObjectsInactive.Include);
            if (vr != null) activePlayerTransform = vr.transform;
        }

        /// <summary>
        /// Proximity detection to guide interactive desktop and VR players smoothly through the operational workflow.
        /// </summary>
        private void CheckZoneProximity()
        {
            if (activePlayerTransform == null) return;
            Vector3 pos = activePlayerTransform.position;
            pos.y = 0f;

            if (currentStage == OperationalStage.WalkToExit)
            {
                Vector3 exit2D = exitPosition; exit2D.y = 0f;
                if (Vector3.Distance(pos, exit2D) <= zoneTriggerRadius)
                {
                    CompleteCycle();
                }
            }
        }

        /// <summary>
        /// Starts a new end-to-end operational cycle beginning at the worker entry zone.
        /// </summary>
        public void StartCycle(OrderGenerationConfig config = null, bool useOptimized = false, Vector3? startSpawnPos = null)
        {
            activeConfig = config;
            useOptimizedRouting = useOptimized;
            sessionStartTime = Time.time;
            sessionEndTime = 0f;
            lastRoutingResult = null;

            Vector3 spawn = startSpawnPos ?? entryPosition;
            if (activePlayerTransform != null)
            {
                var cc = activePlayerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                activePlayerTransform.position = spawn;
                if (cc != null) cc.enabled = true;
            }

            var logger = WorkEventLogger.Instance;
            if (logger != null)
            {
                if (logger.EventCount > 0)
                {
                    sessionStartTime = Mathf.Max(sessionStartTime, logger.LoggedEvents[logger.EventCount - 1].EndTime);
                }
                eventSequence = logger.EventCount;
                logger.LogEvent(new WorkEvent(
                    $"EVT-{++eventSequence:D4}",
                    ActivityType.Waiting,
                    sessionStartTime,
                    sessionStartTime + 0.05f,
                    spawn,
                    "ENTRY",
                    "",
                    "",
                    0f));
            }

            var classifier = FindAnyObjectByType<ActivityClassifier>();
            if (classifier != null)
            {
                classifier.ResetState(sessionStartTime + 0.05f, spawn);
            }

            SubscribeOrderEvents();
            SetStage(OperationalStage.CollectOrder);
            Debug.Log($"[OperationalCycleManager] Cycle STARTED at ENTRY. Proceed to Order Collection Desk (X={collectionDeskPosition.x:F1}, Z={collectionDeskPosition.z:F1}).");
        }

        /// <summary>
        /// Activates/collects the pick order and cart tote at the Order Collection Desk.
        /// Dispatches either the baseline or solver-optimized pick sequence.
        /// </summary>
        public bool CollectOrder(out Order order, out string error)
        {
            order = null;
            error = null;

            var om = OrderManager.Instance;
            if (om == null)
            {
                error = "OrderManager is not available.";
                return false;
            }

            float collectTime = Time.time;
            Vector3 pos = activePlayerTransform != null ? activePlayerTransform.position : collectionDeskPosition;

            if (activeConfig != null)
            {
                if (!om.TryDispatchGeneratedOrder(activeConfig, out order, out error))
                {
                    return false;
                }
            }
            else
            {
                order = om.StartSampleOrder(3);
                if (order == null)
                {
                    error = "Failed to dispatch sample order.";
                    return false;
                }
            }

            activeOrder = order;

            // Route Sequence Optimization (Phase 12 Solver Integration)
            if (useOptimizedRouting)
            {
                lastRoutingResult = om.OptimizeActiveOrderRoute(applyToActiveOrder: true);
                Debug.Log($"[OperationalCycleManager] Applied route optimization: {lastRoutingResult}");
            }
            else
            {
                // Calculate baseline route cost without reordering
                var baselineOptimizer = new BaselineRouteOptimizer();
                lastRoutingResult = om.OptimizeActiveOrderRoute(optimizer: baselineOptimizer, applyToActiveOrder: false);
                Debug.Log($"[OperationalCycleManager] Preserved baseline pick sequence: {lastRoutingResult}");
            }

            var logger = WorkEventLogger.Instance;
            if (logger != null)
            {
                if (logger.EventCount > 0)
                {
                    collectTime = Mathf.Max(collectTime, logger.LoggedEvents[logger.EventCount - 1].EndTime);
                }
                eventSequence = logger.EventCount;
                logger.LogEvent(new WorkEvent(
                    $"EVT-{++eventSequence:D4}",
                    ActivityType.Waiting,
                    collectTime,
                    collectTime + 0.1f,
                    pos,
                    "ORDER_COLLECTION",
                    "",
                    order.OrderId,
                    0f));
            }

            var classifier = FindAnyObjectByType<ActivityClassifier>();
            if (classifier != null)
            {
                classifier.ResetState(collectTime + 0.1f, pos);
            }

            SetStage(OperationalStage.Picking);
            Debug.Log($"[OperationalCycleManager] Collected order {order.OrderId} ({order.TotalItemCount} items). Proceed to pick items in sequence.");
            return true;
        }

        private void HandleItemPicked(OrderItem item, bool allPicked)
        {
            if ((allPicked || (activeOrder != null && activeOrder.IsAllItemsPicked)) && currentStage == OperationalStage.Picking)
            {
                SetStage(OperationalStage.AwaitingPacking);
                Debug.Log($"[OperationalCycleManager] All items picked! Proceed to Packing Desk (X={packingDeskPosition.x:F1}, Z={packingDeskPosition.z:F1}).");
            }
        }

        /// <summary>
        /// Finalizes order deposit and packing at the dispatch packing bench.
        /// </summary>
        public bool DepositOrder(out string error)
        {
            error = null;
            var om = OrderManager.Instance;
            if (om == null)
            {
                error = "OrderManager is not available.";
                return false;
            }

            if (!om.CompletePacking(out error))
            {
                return false;
            }

            SetStage(OperationalStage.WalkToExit);
            Debug.Log($"[OperationalCycleManager] Order deposited and packed at PACK-01. Walk to Exit (X={exitPosition.x:F1}, Z={exitPosition.z:F1}).");
            return true;
        }

        private void HandleOrderCompleted(Order order)
        {
            if (currentStage == OperationalStage.AwaitingPacking || currentStage == OperationalStage.DepositOrder)
            {
                SetStage(OperationalStage.WalkToExit);
            }
        }

        /// <summary>
        /// Concludes the operational session at the exit zone, recording final event telemetry and summary KPIs.
        /// </summary>
        public void CompleteCycle()
        {
            if (currentStage == OperationalStage.Completed) return;

            sessionEndTime = Time.time;
            Vector3 pos = activePlayerTransform != null ? activePlayerTransform.position : exitPosition;

            var logger = WorkEventLogger.Instance;
            if (logger != null)
            {
                if (logger.EventCount > 0)
                {
                    sessionEndTime = Mathf.Max(sessionEndTime, logger.LoggedEvents[logger.EventCount - 1].EndTime);
                }
                eventSequence = logger.EventCount;
                logger.LogEvent(new WorkEvent(
                    $"EVT-{++eventSequence:D4}",
                    ActivityType.OrderCompleted,
                    sessionEndTime,
                    sessionEndTime + 0.05f,
                    pos,
                    "EXIT",
                    "",
                    activeOrder != null ? activeOrder.OrderId : "",
                    0f));
            }

            SetStage(OperationalStage.Completed);

            var summary = GenerateSummary();
            Debug.Log($"[OperationalCycleManager] OPERATIONAL CYCLE COMPLETED! Total Cycle Time: {summary.TotalCycleTime:F2}s, Distance: {summary.TotalDistance:F2}m.");
            OnCycleCompleted?.Invoke(summary);
        }

        public OperationalCycleSummary GenerateSummary()
        {
            var logger = WorkEventLogger.Instance;
            var events = logger != null ? logger.LoggedEvents : new List<WorkEvent>();
            var results = WorkStudyCalculator.Calculate(events);

            var slotIds = new List<string>();
            if (activeOrder != null)
            {
                foreach (var it in activeOrder.Items)
                {
                    slotIds.Add(it.TargetSlotId);
                }
            }

            CalculateAisleMetrics(slotIds, out int transitions, out int backtracks);

            return new OperationalCycleSummary
            {
                OrderId = activeOrder != null ? activeOrder.OrderId : "NONE",
                ItemCount = activeOrder != null ? activeOrder.TotalItemCount : 0,
                IsOptimizedRoute = useOptimizedRouting,
                SessionStartTime = sessionStartTime,
                SessionEndTime = sessionEndTime,
                TotalCycleTime = sessionEndTime > sessionStartTime ? (sessionEndTime - sessionStartTime) : results.TotalTaskTime,
                TotalDistance = results.TotalDistance,
                AisleTransitions = transitions,
                AisleBacktracks = backtracks,
                AlgorithmicRouteCost = lastRoutingResult != null ? lastRoutingResult.TotalCost : 0f,
                IsOrderCompleted = activeOrder != null && activeOrder.Status == OrderStatus.Completed,
                WorkStudy = results
            };
        }

        /// <summary>
        /// Calculates aisle transitions and warehouse backtracking from an ordered sequence of slot IDs.
        /// Backtracking is defined as entering an aisle that was previously exited earlier in the pick sequence.
        /// </summary>
        public static void CalculateAisleMetrics(IReadOnlyList<string> slotIds, out int aisleTransitions, out int aisleBacktracks)
        {
            aisleTransitions = 0;
            aisleBacktracks = 0;

            if (slotIds == null || slotIds.Count <= 1) return;

            var aisleSequence = new List<int>();
            for (int i = 0; i < slotIds.Count; i++)
            {
                if (StoreLocation.TryParse(slotIds[i], out var loc, out _))
                {
                    aisleSequence.Add(loc.AisleNumber);
                }
            }

            if (aisleSequence.Count <= 1) return;

            // Collapse consecutive picks within the same aisle
            var collapsedAisles = new List<int>();
            collapsedAisles.Add(aisleSequence[0]);
            for (int i = 1; i < aisleSequence.Count; i++)
            {
                if (aisleSequence[i] != aisleSequence[i - 1])
                {
                    collapsedAisles.Add(aisleSequence[i]);
                }
            }

            aisleTransitions = collapsedAisles.Count - 1;

            var visitedAisles = new HashSet<int>();
            visitedAisles.Add(collapsedAisles[0]);

            for (int i = 1; i < collapsedAisles.Count; i++)
            {
                int currentAisle = collapsedAisles[i];
                if (visitedAisles.Contains(currentAisle))
                {
                    // Picker returned to an aisle that was previously exited
                    aisleBacktracks++;
                }
                else
                {
                    visitedAisles.Add(currentAisle);
                }
            }
        }

        private void SetStage(OperationalStage stage)
        {
            if (currentStage != stage)
            {
                currentStage = stage;
                OnStageChanged?.Invoke(currentStage);
            }
        }
    }
}
