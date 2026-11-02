using System;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Management;
using VirtualDarkStore.Player;

namespace VirtualDarkStore.WorkStudy
{
    /// <summary>
    /// State machine and activity classifier for dark store work-study and simulation.
    /// Employs explicit, measurable rules to classify operator actions:
    /// Walking, Waiting, Searching, Approaching, Picking, MovingBetweenLocations, Packing, OrderCompleted.
    /// Strictly excludes uninstrumented physiological or cognitive claims.
    /// </summary>
    public class ActivityClassifier : MonoBehaviour
    {
        private static ActivityClassifier instance;
        public static ActivityClassifier Instance => instance != null ? instance : (instance = FindAnyObjectByType<ActivityClassifier>());

        [Header("Classification Thresholds")]
        [Tooltip("Speed threshold (m/s) below which picker is considered stationary.")]
        [SerializeField] private float speedThreshold = 0.15f;

        [Tooltip("Proximity radius (m) around target rack bay or packing station considered the approach/work zone.")]
        [SerializeField] private float approachDistanceThreshold = 3.0f;

        [Tooltip("Reach distance for raycast checking if picker is visually scanning shelving structures.")]
        [SerializeField] private float shelfRaycastDistance = 3.5f;

        [Tooltip("Standard elemental duration for physical item grasp/pick.")]
        [SerializeField] private float pickActionDuration = 0.5f;

        [Tooltip("Standard elemental duration for packing finalization at bench.")]
        [SerializeField] private float packingFinalizeDuration = 0.8f;

        [Header("References")]
        [SerializeField] private Transform pickerTransform;
        [SerializeField] private PickerInteraction pickerInteraction;
        [SerializeField] private VirtualDarkStore.VR.VRPickerInteraction vrPickerInteraction;
        [SerializeField] private WorkEventLogger eventLogger;

        [Header("Runtime State")]
        [SerializeField] private ActivityType currentActivity = ActivityType.Waiting;
        [SerializeField] private float currentActivityStartTime = 0f;
        [SerializeField] private Vector3 currentActivityStartPosition = Vector3.zero;
        [SerializeField] private float currentActivityDistance = 0f;
        [SerializeField] private int eventSequence = 0;
        [SerializeField] private float cumulativeTotalDistance = 0f;

        private float currentSimulationTime = 0f;
        private Vector3 lastRecordedPosition = Vector3.zero;
        private bool isInitialized = false;

        public ActivityType CurrentActivity => currentActivity;
        public float CurrentActivityStartTime => currentActivityStartTime;
        public float CurrentActivityDistance => currentActivityDistance;
        public float CumulativeTotalDistance => cumulativeTotalDistance;
        public int LoggedEventCount => eventSequence;

        public event Action<ActivityType, ActivityType> OnActivityChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
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
            InitializeReferences();
            ResetState(Time.time, pickerTransform != null ? pickerTransform.position : Vector3.zero);
        }

        public void SetReferences(WorkEventLogger logger, OrderManager om, Transform picker = null, PickerInteraction interaction = null)
        {
            eventLogger = logger;
            pickerTransform = picker;
            pickerInteraction = interaction;
            SubscribeOrderEvents();
        }

        public void SetVRReferences(WorkEventLogger logger, OrderManager om, VirtualDarkStore.VR.VRPlayerRig vrRig)
        {
            eventLogger = logger;
            if (vrRig != null)
            {
                pickerTransform = vrRig.transform;
                vrPickerInteraction = vrRig.PickerInteraction;
            }
            SubscribeOrderEvents();
        }

        public void InitializeReferences()
        {
            if (pickerTransform == null)
            {
                var vrRig = FindAnyObjectByType<VirtualDarkStore.VR.VRPlayerRig>();
                if (vrRig != null && vrRig.gameObject.activeInHierarchy)
                {
                    pickerTransform = vrRig.transform;
                    vrPickerInteraction = vrRig.PickerInteraction;
                }
                else
                {
                    var picker = FindAnyObjectByType<DesktopPickerController>();
                    if (picker != null) pickerTransform = picker.transform;
                }
            }

            if (pickerInteraction == null && pickerTransform != null)
            {
                pickerInteraction = pickerTransform.GetComponent<PickerInteraction>();
                if (vrPickerInteraction == null)
                {
                    vrPickerInteraction = pickerTransform.GetComponentInChildren<VirtualDarkStore.VR.VRPickerInteraction>();
                }
            }

            if (eventLogger == null)
            {
                eventLogger = WorkEventLogger.Instance;
                if (eventLogger == null)
                {
                    eventLogger = FindAnyObjectByType<WorkEventLogger>();
                }
            }

            SubscribeOrderEvents();
        }

        public void SubscribeOrderEvents()
        {
            var orderManager = OrderManager.Instance;
            if (orderManager != null)
            {
                orderManager.OnOrderStarted -= HandleOrderStarted;
                orderManager.OnOrderStarted += HandleOrderStarted;

                orderManager.OnItemPicked -= HandleItemPicked;
                orderManager.OnItemPicked += HandleItemPicked;

                orderManager.OnOrderCompleted -= HandleOrderCompleted;
                orderManager.OnOrderCompleted += HandleOrderCompleted;
            }
        }

        public void UnsubscribeOrderEvents()
        {
            var orderManager = OrderManager.Instance;
            if (orderManager != null)
            {
                orderManager.OnOrderStarted -= HandleOrderStarted;
                orderManager.OnItemPicked -= HandleItemPicked;
                orderManager.OnOrderCompleted -= HandleOrderCompleted;
            }
        }

        public void ResetState(float initialTime, Vector3 initialPosition)
        {
            currentActivity = ActivityType.Waiting;
            currentActivityStartTime = initialTime;
            currentSimulationTime = initialTime;
            currentActivityStartPosition = initialPosition;
            lastRecordedPosition = initialPosition;
            currentActivityDistance = 0f;
            cumulativeTotalDistance = 0f;
            eventSequence = 0;
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized)
            {
                InitializeReferences();
                ResetState(Time.time, pickerTransform != null ? pickerTransform.position : Vector3.zero);
            }

            if (pickerTransform == null) return;

            float currentTime = Time.time;
            Vector3 currentPos = pickerTransform.position;

            // Measure horizontal movement
            Vector3 horizontalDelta = new Vector3(currentPos.x - lastRecordedPosition.x, 0f, currentPos.z - lastRecordedPosition.z);
            float frameDistance = horizontalDelta.magnitude;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = frameDistance / dt;

            // Check if camera ray is oriented at shelf
            bool isLookingAtShelf = CheckIsLookingAtShelf();

            // Check if at packing bench
            bool isAtPackingBench = (pickerInteraction != null && pickerInteraction.HoveredPackingStation != null)
                || (vrPickerInteraction != null && vrPickerInteraction.HoveredPackingStation != null);

            Step(currentTime, currentPos, speed, isLookingAtShelf, isAtPackingBench, frameDistance);
        }

        private bool CheckIsLookingAtShelf()
        {
            Camera cam = Camera.main;
            Ray ray = cam != null
                ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(pickerTransform.position + Vector3.up * 1.6f, pickerTransform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, shelfRaycastDistance))
            {
                // Collider hits shelf tier, rack upright, product, or shelf unit
                string hitName = hit.collider.gameObject.name.ToLowerInvariant();
                if (hitName.Contains("shelf") || hitName.Contains("rack") || hitName.Contains("product") || hitName.Contains("upright"))
                {
                    return true;
                }

                if (hit.collider.GetComponentInParent<VirtualDarkStore.Environment.ShelfUnit>() != null ||
                    hit.collider.GetComponent<VirtualDarkStore.Interaction.PickableItemVisual>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Explicit deterministic step method for evaluating activities and transitions.
        /// Can be called both in real-time Update and in automated test harnesses.
        /// </summary>
        public void Step(float currentTime, Vector3 currentPos, float speed, bool isLookingAtShelf, bool isAtPackingBench, float frameDistance = 0f)
        {
            if (currentTime < currentActivityStartTime)
            {
                currentTime = currentActivityStartTime;
            }
            currentSimulationTime = currentTime;

            if (frameDistance > 0f)
            {
                currentActivityDistance += frameDistance;
                cumulativeTotalDistance += frameDistance;
            }
            lastRecordedPosition = currentPos;

            var orderManager = OrderManager.Instance;
            bool hasActiveOrder = orderManager != null && orderManager.HasActiveOrder;
            bool isAwaitingPacking = orderManager != null && orderManager.IsAwaitingPacking;

            // Get target distance
            float distToTarget = float.MaxValue;
            if (hasActiveOrder && orderManager.ActiveOrder != null && orderManager.ActiveOrder.CurrentTargetItem != null)
            {
                distToTarget = Vector3.Distance(currentPos, orderManager.ActiveOrder.CurrentTargetItem.TargetWorldPosition);
            }

            float distToPacking = float.MaxValue;
            var packingStation = FindAnyObjectByType<VirtualDarkStore.Environment.PackingStation>();
            if (packingStation != null)
            {
                distToPacking = Vector3.Distance(currentPos, packingStation.transform.position);
            }

            ActivityType classifiedActivity = EvaluateActivity(
                speed,
                hasActiveOrder,
                isAwaitingPacking,
                distToTarget,
                distToPacking,
                isLookingAtShelf,
                isAtPackingBench);

            if (classifiedActivity != currentActivity)
            {
                TransitionActivity(classifiedActivity, currentTime, currentPos);
            }
        }

        /// <summary>
        /// Pure decision rule matrix for activity classification based strictly on measurable metrics.
        /// </summary>
        public ActivityType EvaluateActivity(
            float speed,
            bool hasActiveOrder,
            bool isAwaitingPacking,
            float distToTarget,
            float distToPacking,
            bool isLookingAtShelf,
            bool isAtPackingBench)
        {
            bool isMoving = speed >= speedThreshold;

            if (isMoving)
            {
                if (hasActiveOrder)
                {
                    return distToTarget <= approachDistanceThreshold
                        ? ActivityType.Approaching
                        : ActivityType.MovingBetweenLocations;
                }
                else if (isAwaitingPacking)
                {
                    return distToPacking <= approachDistanceThreshold
                        ? ActivityType.Approaching
                        : ActivityType.MovingBetweenLocations;
                }
                else
                {
                    return ActivityType.Walking;
                }
            }
            else
            {
                // Stationary (speed < threshold)
                if (hasActiveOrder)
                {
                    // Rule for Searching:
                    // Within target rack bay approach radius AND camera raycast oriented at shelf geometry
                    if (distToTarget <= approachDistanceThreshold && isLookingAtShelf)
                    {
                        return ActivityType.Searching;
                    }
                    return ActivityType.Waiting;
                }
                else if (isAwaitingPacking)
                {
                    if (distToPacking <= approachDistanceThreshold || isAtPackingBench)
                    {
                        return ActivityType.Packing;
                    }
                    return ActivityType.Waiting;
                }
                else
                {
                    return ActivityType.Waiting;
                }
            }
        }

        private void TransitionActivity(ActivityType newActivity, float transitionTime, Vector3 position)
        {
            if (transitionTime < currentActivityStartTime)
            {
                transitionTime = currentActivityStartTime;
            }

            // Close current activity event
            float duration = transitionTime - currentActivityStartTime;
            if (duration >= 0f)
            {
                EmitCurrentEvent(transitionTime, position);
            }

            var old = currentActivity;
            currentActivity = newActivity;
            currentActivityStartTime = transitionTime;
            currentSimulationTime = transitionTime;
            currentActivityStartPosition = position;
            currentActivityDistance = 0f;

            OnActivityChanged?.Invoke(old, newActivity);
        }

        private void CloseCurrentActivity(float endTime, Vector3 position)
        {
            if (endTime < currentActivityStartTime)
            {
                endTime = currentActivityStartTime;
            }

            float duration = endTime - currentActivityStartTime;
            if (duration >= 0f)
            {
                EmitCurrentEvent(endTime, position);
            }
        }

        private void EmitCurrentEvent(float endTime, Vector3 position)
        {
            if (eventLogger == null)
            {
                eventLogger = WorkEventLogger.Instance;
                if (eventLogger == null) return;
            }

            if (eventLogger.EventCount > 0)
            {
                var prev = eventLogger.LoggedEvents[eventLogger.EventCount - 1];
                if (currentActivityStartTime < prev.StartTime)
                {
                    currentActivityStartTime = prev.EndTime;
                }
            }

            if (endTime < currentActivityStartTime)
            {
                endTime = currentActivityStartTime;
            }

            float duration = endTime - currentActivityStartTime;
            if (duration < 0f)
            {
                return;
            }

            string eventId = $"EVT-{(++eventSequence):D4}";
            string locationName = GetCurrentLocationName();
            string relatedSku = GetCurrentSkuId();
            string relatedOrder = GetCurrentOrderId();

            var workEvent = new WorkEvent(
                eventId,
                currentActivity,
                currentActivityStartTime,
                endTime,
                position,
                locationName,
                relatedSku,
                relatedOrder,
                currentActivityDistance);

            eventLogger.LogEvent(workEvent);
        }

        private void HandleOrderStarted(Order order)
        {
            float now = currentSimulationTime > 0f ? currentSimulationTime : Time.time;
            if (eventLogger != null && eventLogger.EventCount > 0)
            {
                now = Mathf.Max(now, eventLogger.LoggedEvents[eventLogger.EventCount - 1].EndTime);
            }
            Vector3 pos = pickerTransform != null ? pickerTransform.position : lastRecordedPosition;
            TransitionActivity(ActivityType.MovingBetweenLocations, now, pos);
        }

        private void HandleItemPicked(OrderItem item, bool orderFinished)
        {
            float rawTime = currentSimulationTime > 0f ? currentSimulationTime : Time.time;
            float now = Mathf.Max(rawTime, currentActivityStartTime);
            if (eventLogger != null && eventLogger.EventCount > 0)
            {
                now = Mathf.Max(now, eventLogger.LoggedEvents[eventLogger.EventCount - 1].EndTime);
            }
            Vector3 pos = pickerTransform != null ? pickerTransform.position : lastRecordedPosition;

            // 1. Close preceding activity (e.g. Searching)
            CloseCurrentActivity(now, pos);

            // 2. Emit discrete Picking event with standard measured duration
            float pickStart = now;
            float pickEnd = now + pickActionDuration;
            string eventId = $"EVT-{(++eventSequence):D4}";

            var pickEvent = new WorkEvent(
                eventId,
                ActivityType.Picking,
                pickStart,
                pickEnd,
                pos,
                item != null ? item.TargetSlotId : "",
                item != null ? item.SkuId : "",
                GetCurrentOrderId(),
                0f);

            if (eventLogger != null)
            {
                eventLogger.LogEvent(pickEvent);
            }

            // 3. Next state: if order has more items -> MovingBetweenLocations, else moving to packing bench
            currentActivity = ActivityType.MovingBetweenLocations;
            currentActivityStartTime = pickEnd;
            currentSimulationTime = pickEnd;
            currentActivityStartPosition = pos;
            currentActivityDistance = 0f;
        }

        private void HandleOrderCompleted(Order order)
        {
            float rawTime = currentSimulationTime > 0f ? currentSimulationTime : Time.time;
            float now = Mathf.Max(rawTime, currentActivityStartTime);
            if (eventLogger != null && eventLogger.EventCount > 0)
            {
                now = Mathf.Max(now, eventLogger.LoggedEvents[eventLogger.EventCount - 1].EndTime);
            }
            Vector3 pos = pickerTransform != null ? pickerTransform.position : lastRecordedPosition;

            // 1. Close current Packing activity
            CloseCurrentActivity(now, pos);

            // 2. Emit terminal OrderCompleted event
            float start = now;
            float end = now + packingFinalizeDuration;
            string eventId = $"EVT-{(++eventSequence):D4}";

            var completedEvent = new WorkEvent(
                eventId,
                ActivityType.OrderCompleted,
                start,
                end,
                pos,
                "PACK-01",
                "",
                order != null ? order.OrderId : "",
                0f);

            if (eventLogger != null)
            {
                eventLogger.LogEvent(completedEvent);
            }

            // 3. Return to Waiting state
            currentActivity = ActivityType.Waiting;
            currentActivityStartTime = end;
            currentSimulationTime = end;
            currentActivityStartPosition = pos;
            currentActivityDistance = 0f;
        }

        private string GetCurrentLocationName()
        {
            var orderManager = OrderManager.Instance;
            if (orderManager != null && orderManager.ActiveOrder != null)
            {
                if (orderManager.IsAwaitingPacking) return "PACK-01";
                if (orderManager.ActiveOrder.CurrentTargetItem != null)
                {
                    return orderManager.ActiveOrder.CurrentTargetItem.TargetSlotId;
                }
            }
            return "GENERAL_FLOOR";
        }

        private string GetCurrentSkuId()
        {
            var orderManager = OrderManager.Instance;
            if (orderManager != null && orderManager.ActiveOrder != null && orderManager.ActiveOrder.CurrentTargetItem != null)
            {
                return orderManager.ActiveOrder.CurrentTargetItem.SkuId;
            }
            return string.Empty;
        }

        private string GetCurrentOrderId()
        {
            var orderManager = OrderManager.Instance;
            if (orderManager != null && orderManager.ActiveOrder != null)
            {
                return orderManager.ActiveOrder.OrderId;
            }
            return string.Empty;
        }
    }
}
