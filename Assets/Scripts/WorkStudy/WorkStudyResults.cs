using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VirtualDarkStore.WorkStudy
{
    /// <summary>
    /// Data model representing aggregated industrial work-study and work-sampling results.
    /// Strictly computed from verifiable simulation event logs with mathematical reconciliation guarantees.
    /// Excludes uninstrumented biometric or speculative improvement figures.
    /// </summary>
    [Serializable]
    public class WorkStudyResults
    {
        [Header("Overview Metrics")]
        [SerializeField] private float totalTaskTime;
        [SerializeField] private float totalDistance;
        [SerializeField] private int totalPicks;
        [SerializeField] private int shelfVisits;
        [SerializeField] private int totalEventsCount;

        [Header("Category Durations (Seconds)")]
        [SerializeField] private float walkingDuration;
        [SerializeField] private float searchingDuration;
        [SerializeField] private float pickingDuration;
        [SerializeField] private float waitingDuration;
        [SerializeField] private float packingDuration;
        [SerializeField] private float orderCompletedDuration;

        [Header("Granular Travel Breakdown (Seconds)")]
        [SerializeField] private float generalWalkingDuration;
        [SerializeField] private float movingBetweenLocationsDuration;
        [SerializeField] private float approachingDuration;

        [Header("Category Percentages (%)")]
        [SerializeField] private float walkingPercentage;
        [SerializeField] private float searchingPercentage;
        [SerializeField] private float pickingPercentage;
        [SerializeField] private float waitingPercentage;
        [SerializeField] private float packingPercentage;
        [SerializeField] private float orderCompletedPercentage;

        [Header("Interval Work-Sampling Data")]
        [SerializeField] private float samplingIntervalSeconds;
        [SerializeField] private int totalWorkSamplesCount;
        [SerializeField] private List<ActivitySampleCount> sampleCounts = new List<ActivitySampleCount>();

        public float TotalTaskTime => totalTaskTime;
        public float TotalDistance => totalDistance;
        public int TotalPicks => totalPicks;
        public int ShelfVisits => shelfVisits;
        public int TotalEventsCount => totalEventsCount;

        public float WalkingDuration => walkingDuration;
        public float SearchingDuration => searchingDuration;
        public float PickingDuration => pickingDuration;
        public float WaitingDuration => waitingDuration;
        public float PackingDuration => packingDuration;
        public float OrderCompletedDuration => orderCompletedDuration;

        public float GeneralWalkingDuration => generalWalkingDuration;
        public float MovingBetweenLocationsDuration => movingBetweenLocationsDuration;
        public float ApproachingDuration => approachingDuration;

        public float WalkingPercentage => walkingPercentage;
        public float SearchingPercentage => searchingPercentage;
        public float PickingPercentage => pickingPercentage;
        public float WaitingPercentage => waitingPercentage;
        public float PackingPercentage => packingPercentage;
        public float OrderCompletedPercentage => orderCompletedPercentage;

        public float SamplingIntervalSeconds => samplingIntervalSeconds;
        public int TotalWorkSamplesCount => totalWorkSamplesCount;
        public IReadOnlyList<ActivitySampleCount> SampleCounts => sampleCounts;

        [Serializable]
        public struct ActivitySampleCount
        {
            public ActivityType Activity;
            public int Count;
            public float Percentage;

            public ActivitySampleCount(ActivityType activity, int count, float percentage)
            {
                Activity = activity;
                Count = count;
                Percentage = percentage;
            }
        }

        public WorkStudyResults(
            float totalTaskTime,
            float totalDistance,
            int totalPicks,
            int shelfVisits,
            int totalEventsCount,
            float walkingDuration,
            float searchingDuration,
            float pickingDuration,
            float waitingDuration,
            float packingDuration,
            float orderCompletedDuration,
            float generalWalkingDuration,
            float movingBetweenLocationsDuration,
            float approachingDuration,
            float walkingPercentage,
            float searchingPercentage,
            float pickingPercentage,
            float waitingPercentage,
            float packingPercentage,
            float orderCompletedPercentage,
            float samplingIntervalSeconds,
            int totalWorkSamplesCount,
            List<ActivitySampleCount> sampleCounts)
        {
            this.totalTaskTime = totalTaskTime;
            this.totalDistance = totalDistance;
            this.totalPicks = totalPicks;
            this.shelfVisits = shelfVisits;
            this.totalEventsCount = totalEventsCount;

            this.walkingDuration = walkingDuration;
            this.searchingDuration = searchingDuration;
            this.pickingDuration = pickingDuration;
            this.waitingDuration = waitingDuration;
            this.packingDuration = packingDuration;
            this.orderCompletedDuration = orderCompletedDuration;

            this.generalWalkingDuration = generalWalkingDuration;
            this.movingBetweenLocationsDuration = movingBetweenLocationsDuration;
            this.approachingDuration = approachingDuration;

            this.walkingPercentage = walkingPercentage;
            this.searchingPercentage = searchingPercentage;
            this.pickingPercentage = pickingPercentage;
            this.waitingPercentage = waitingPercentage;
            this.packingPercentage = packingPercentage;
            this.orderCompletedPercentage = orderCompletedPercentage;

            this.samplingIntervalSeconds = samplingIntervalSeconds;
            this.totalWorkSamplesCount = totalWorkSamplesCount;
            this.sampleCounts = sampleCounts ?? new List<ActivitySampleCount>();
        }

        /// <summary>
        /// Mathematically verifies that category durations sum exactly to TotalTaskTime
        /// and category percentages sum to 100.0%.
        /// </summary>
        public bool VerifyReconciliation(out string failureReason)
        {
            failureReason = null;

            if (totalTaskTime < 0f)
            {
                failureReason = $"Total task time ({totalTaskTime:F3}s) cannot be negative.";
                return false;
            }

            // Duration sum reconciliation
            float sumDurations = walkingDuration + searchingDuration + pickingDuration +
                                 waitingDuration + packingDuration + orderCompletedDuration;

            if (Mathf.Abs(sumDurations - totalTaskTime) > 0.002f)
            {
                failureReason = $"Category durations sum ({sumDurations:F3}s) does not match total task time ({totalTaskTime:F3}s). Delta: {Mathf.Abs(sumDurations - totalTaskTime):F4}s";
                return false;
            }

            // Travel subcategory duration reconciliation
            float sumTravelSubcategories = generalWalkingDuration + movingBetweenLocationsDuration + approachingDuration;
            if (Mathf.Abs(sumTravelSubcategories - walkingDuration) > 0.002f)
            {
                failureReason = $"Travel subcategories sum ({sumTravelSubcategories:F3}s) does not match total walking duration ({walkingDuration:F3}s). Delta: {Mathf.Abs(sumTravelSubcategories - walkingDuration):F4}s";
                return false;
            }

            // Percentage sum reconciliation (when task time > 0)
            if (totalTaskTime > 0.0001f)
            {
                float sumPercentages = walkingPercentage + searchingPercentage + pickingPercentage +
                                       waitingPercentage + packingPercentage + orderCompletedPercentage;

                if (Mathf.Abs(sumPercentages - 100.0f) > 0.05f)
                {
                    failureReason = $"Category percentages sum ({sumPercentages:F2}%) does not reconcile to 100%. Delta: {Mathf.Abs(sumPercentages - 100f):F3}%";
                    return false;
                }
            }

            return true;
        }

        public string GetSummaryReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== INDUSTRIAL WORK-STUDY SIMULATION RESULTS ===");
            sb.AppendLine($"Total Task Time:   {TotalTaskTime:F2} s");
            sb.AppendLine($"Total Traveled:    {TotalDistance:F2} m");
            sb.AppendLine($"Successful Picks:  {TotalPicks}");
            sb.AppendLine($"Shelf Bay Visits:  {ShelfVisits}");
            sb.AppendLine($"Processed Events:  {TotalEventsCount}");
            sb.AppendLine("------------------------------------------------");
            sb.AppendLine("Activity Category Breakdown (Time-Study):");
            sb.AppendLine($"  - Walking / Travel:    {WalkingDuration,6:F2} s  ({WalkingPercentage,5:F1}%)  [Moving: {MovingBetweenLocationsDuration:F2}s, Approach: {ApproachingDuration:F2}s, General: {GeneralWalkingDuration:F2}s]");
            sb.AppendLine($"  - Searching Shelf:     {SearchingDuration,6:F2} s  ({SearchingPercentage,5:F1}%)");
            sb.AppendLine($"  - Item Picking:        {PickingDuration,6:F2} s  ({PickingPercentage,5:F1}%)");
            sb.AppendLine($"  - Waiting / Idle:      {WaitingDuration,6:F2} s  ({WaitingPercentage,5:F1}%)");
            sb.AppendLine($"  - Order Packing:       {PackingDuration,6:F2} s  ({PackingPercentage,5:F1}%)");
            sb.AppendLine($"  - Order Completed:     {OrderCompletedDuration,6:F2} s  ({OrderCompletedPercentage,5:F1}%)");
            sb.AppendLine("------------------------------------------------");
            if (TotalWorkSamplesCount > 0)
            {
                sb.AppendLine($"Work-Sampling (Interval: {SamplingIntervalSeconds:F2} s, Total Samples: {TotalWorkSamplesCount}):");
                foreach (var sample in SampleCounts)
                {
                    sb.AppendLine($"  - {sample.Activity,-22}: {sample.Count,4} obs ({sample.Percentage,5:F1}%)");
                }
            }
            sb.AppendLine("================================================");
            return sb.ToString();
        }
    }
}
