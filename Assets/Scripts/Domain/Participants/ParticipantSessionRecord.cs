using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Domain.Participants
{
    /// <summary>
    /// Comprehensive immutable data model representing a single completed participant session.
    /// Encapsulates participant identity, order parameters, work-study metrics, routing results, and data validation.
    /// Strictly complies with RFC-4180 CSV export and comparative statistical analysis requirements.
    /// </summary>
    [Serializable]
    public class ParticipantSessionRecord
    {
        // 1. Participant and Session Information
        public string participantId = string.Empty;
        public string participantName = string.Empty;
        public string scenarioName = string.Empty;
        public string sessionId = string.Empty;
        public string sessionDate = string.Empty;
        public string sessionStartTime = string.Empty;
        public string sessionEndTime = string.Empty;
        public string interactionMode = "Desktop"; // "Desktop" or "VR"
        public string routingMode = "Baseline"; // "Baseline" or "Optimized"
        public string orderId = string.Empty;
        public string batchId = string.Empty;
        public string randomSeed = string.Empty;

        // 2. Order Information
        public int totalItems;
        public int pickedItems;
        public int remainingItems;
        public int uniqueSkus;
        public int totalShelfVisits;
        public int aisleTransitions;

        // 3. Work-Study Metrics (Durations & Distances in seconds and meters)
        public float totalCycleTimeSeconds;
        public float totalWalkingSeconds;
        public float totalWaitingSeconds;
        public float totalSearchingSeconds;
        public float totalApproachingSeconds;
        public float totalPickingSeconds;
        public float totalPackingSeconds;
        public float totalDistanceMeters;
        public float averagePickTimeSeconds;
        public float averageTimePerItemSeconds;
        public float itemsPerMinute;
        public float walkingPercentage;
        public float waitingPercentage;
        public float searchingPercentage;
        public float pickingPercentage;
        public float packingPercentage;

        // 4. Routing Information
        public string plannedPickSequence = string.Empty; // Semicolon-delimited SKU list
        public string actualPickSequence = string.Empty;  // Semicolon-delimited SKU list
        public string plannedRouteDistanceMeters = string.Empty; // Semicolon/Float or empty if unavailable
        public float actualTravelDistanceMeters;
        public string routeCompletionStatus = "Completed";

        // 5. Data Quality
        public string sessionCompletionStatus = "Completed";
        public int missingEventCount = 0;
        public string dataValidationStatus = "PASS";

        public ParticipantSessionRecord() { }

        public static ParticipantSessionRecord Create(
            ParticipantProfile profile,
            string scenarioName,
            string routingMode,
            string interactionMode,
            Order order,
            WorkStudyResults results,
            IReadOnlyList<WorkEvent> events,
            string sessionId = null,
            string batchId = null,
            int? seed = null,
            float? plannedDistance = null,
            IReadOnlyList<string> plannedSequence = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (results == null) results = WorkStudyCalculator.Calculate(events ?? new List<WorkEvent>());

            var record = new ParticipantSessionRecord();
            record.participantId = profile.SlotId;
            record.participantName = profile.ParticipantName;
            record.scenarioName = string.IsNullOrWhiteSpace(scenarioName) ? "StandardScenario" : scenarioName.Trim();

            DateTime now = DateTime.Now;
            record.sessionId = !string.IsNullOrWhiteSpace(sessionId)
                ? sessionId
                : $"SES-{now:yyyyMMddHHmmss}-{profile.SlotId}";
            record.sessionDate = now.ToString("yyyy-MM-dd");
            record.sessionStartTime = now.AddSeconds(-results.TotalTaskTime).ToString("yyyy-MM-ddTHH:mm:sszzz");
            record.sessionEndTime = now.ToString("yyyy-MM-ddTHH:mm:sszzz");

            record.interactionMode = !string.IsNullOrWhiteSpace(interactionMode) ? interactionMode : "Desktop";
            record.routingMode = !string.IsNullOrWhiteSpace(routingMode) ? routingMode : "Baseline";
            record.orderId = order != null ? order.OrderId : (events != null && events.Count > 0 ? events[0].RelatedOrder : "ORD-UNKNOWN");
            record.batchId = batchId ?? string.Empty;
            record.randomSeed = seed.HasValue ? seed.Value.ToString() : string.Empty;

            // Order metrics
            if (order != null)
            {
                record.totalItems = order.TotalItemCount;
                int picked = 0;
                var uniqueSkuSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var it = order.Items[i];
                    if (it.IsFulfilled) picked++;
                    if (!string.IsNullOrEmpty(it.SkuId)) uniqueSkuSet.Add(it.SkuId);
                }
                record.pickedItems = picked;
                record.remainingItems = Mathf.Max(0, record.totalItems - picked);
                record.uniqueSkus = uniqueSkuSet.Count;
            }
            else
            {
                record.totalItems = results.TotalPicks;
                record.pickedItems = results.TotalPicks;
                record.remainingItems = 0;
                record.uniqueSkus = results.TotalPicks;
            }

            record.totalShelfVisits = results.ShelfVisits;

            // Calculate Aisle Transitions and sequences from events and items
            record.aisleTransitions = CalculateAisleTransitions(events);

            // Work-Study metrics
            record.totalCycleTimeSeconds = results.TotalTaskTime;
            record.totalWalkingSeconds = results.WalkingDuration;
            record.totalWaitingSeconds = results.WaitingDuration;
            record.totalSearchingSeconds = results.SearchingDuration;
            record.totalApproachingSeconds = results.ApproachingDuration;
            record.totalPickingSeconds = results.PickingDuration;
            record.totalPackingSeconds = results.PackingDuration;
            record.totalDistanceMeters = results.TotalDistance;

            record.averagePickTimeSeconds = record.pickedItems > 0 ? results.PickingDuration / record.pickedItems : 0f;
            record.averageTimePerItemSeconds = record.pickedItems > 0 ? results.TotalTaskTime / record.pickedItems : 0f;
            record.itemsPerMinute = results.TotalTaskTime > 0.001f ? (record.pickedItems / (results.TotalTaskTime / 60f)) : 0f;

            record.walkingPercentage = results.WalkingPercentage;
            record.waitingPercentage = results.WaitingPercentage;
            record.searchingPercentage = results.SearchingPercentage;
            record.pickingPercentage = results.PickingPercentage;
            record.packingPercentage = results.PackingPercentage;

            // Routing
            if (plannedSequence != null && plannedSequence.Count > 0)
            {
                record.plannedPickSequence = string.Join(";", plannedSequence);
            }
            else if (order != null)
            {
                var seq = new List<string>();
                foreach (var it in order.Items) seq.Add(it.SkuId);
                record.plannedPickSequence = string.Join(";", seq);
            }

            // Actual pick sequence from events
            var actualSeq = new List<string>();
            if (events != null)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i].Activity == ActivityType.Picking && !string.IsNullOrEmpty(events[i].RelatedSku))
                    {
                        if (!actualSeq.Contains(events[i].RelatedSku))
                        {
                            actualSeq.Add(events[i].RelatedSku);
                        }
                    }
                }
            }
            record.actualPickSequence = actualSeq.Count > 0 ? string.Join(";", actualSeq) : record.plannedPickSequence;

            record.plannedRouteDistanceMeters = plannedDistance.HasValue
                ? plannedDistance.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
                : string.Empty;
            record.actualTravelDistanceMeters = results.TotalDistance;
            record.routeCompletionStatus = (order == null || order.Status == OrderStatus.Completed) ? "Completed" : "Partial";

            // Data Quality
            bool recon = results.VerifyReconciliation(out string reason);
            record.sessionCompletionStatus = (order == null || order.Status == OrderStatus.Completed) ? "Completed" : "Incomplete";
            record.missingEventCount = 0;
            record.dataValidationStatus = recon ? "PASS (100.0% Reconciled)" : $"FAIL ({reason})";

            return record;
        }

        private static int CalculateAisleTransitions(IReadOnlyList<WorkEvent> events)
        {
            if (events == null || events.Count == 0) return 0;
            string lastAisle = null;
            int transitions = 0;

            for (int i = 0; i < events.Count; i++)
            {
                string loc = events[i].LocationName;
                if (!string.IsNullOrEmpty(loc) && loc.StartsWith("A", StringComparison.OrdinalIgnoreCase) && loc.Length >= 3)
                {
                    string currentAisle = loc.Substring(0, 3).ToUpperInvariant();
                    if (lastAisle != null && !string.Equals(lastAisle, currentAisle, StringComparison.OrdinalIgnoreCase))
                    {
                        transitions++;
                    }
                    lastAisle = currentAisle;
                }
            }
            return transitions;
        }
    }
}
