using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.WorkStudy
{
    /// <summary>
    /// Pure computational engine for calculating work-study time metrics, activity distributions,
    /// and configurable interval-based work-sampling observations.
    /// Strictly derives all figures from verified event logs without synthetic adjustments.
    /// </summary>
    public static class WorkStudyCalculator
    {
        /// <summary>
        /// Computes comprehensive work-study and work-sampling results from a recorded sequence of WorkEvents.
        /// </summary>
        /// <param name="events">The chronologically ordered work events to aggregate.</param>
        /// <param name="samplingIntervalSeconds">Configurable sampling interval (in seconds) for discrete work sampling.</param>
        public static WorkStudyResults Calculate(IReadOnlyList<WorkEvent> events, float samplingIntervalSeconds = 1.0f)
        {
            if (events == null || events.Count == 0)
            {
                return new WorkStudyResults(
                    totalTaskTime: 0f,
                    totalDistance: 0f,
                    totalPicks: 0,
                    shelfVisits: 0,
                    totalEventsCount: 0,
                    walkingDuration: 0f,
                    searchingDuration: 0f,
                    pickingDuration: 0f,
                    waitingDuration: 0f,
                    packingDuration: 0f,
                    orderCompletedDuration: 0f,
                    generalWalkingDuration: 0f,
                    movingBetweenLocationsDuration: 0f,
                    approachingDuration: 0f,
                    walkingPercentage: 0f,
                    searchingPercentage: 0f,
                    pickingPercentage: 0f,
                    waitingPercentage: 0f,
                    packingPercentage: 0f,
                    orderCompletedPercentage: 0f,
                    samplingIntervalSeconds: samplingIntervalSeconds,
                    totalWorkSamplesCount: 0,
                    sampleCounts: new List<WorkStudyResults.ActivitySampleCount>());
            }

            float totalTaskTime = 0f;
            float totalDistance = 0f;
            int totalPicks = 0;

            float generalWalkingDuration = 0f;
            float movingBetweenLocationsDuration = 0f;
            float approachingDuration = 0f;
            float searchingDuration = 0f;
            float pickingDuration = 0f;
            float waitingDuration = 0f;
            float packingDuration = 0f;
            float orderCompletedDuration = 0f;

            var distinctShelfSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                float duration = Mathf.Max(evt.Duration, 0f);
                totalTaskTime += duration;
                totalDistance += Mathf.Max(evt.Distance, 0f);

                switch (evt.Activity)
                {
                    case ActivityType.Walking:
                        generalWalkingDuration += duration;
                        break;
                    case ActivityType.MovingBetweenLocations:
                        movingBetweenLocationsDuration += duration;
                        break;
                    case ActivityType.Approaching:
                        approachingDuration += duration;
                        break;
                    case ActivityType.Searching:
                        searchingDuration += duration;
                        break;
                    case ActivityType.Picking:
                        pickingDuration += duration;
                        totalPicks++;
                        break;
                    case ActivityType.Waiting:
                        waitingDuration += duration;
                        break;
                    case ActivityType.Packing:
                        packingDuration += duration;
                        break;
                    case ActivityType.OrderCompleted:
                        orderCompletedDuration += duration;
                        break;
                }

                // Track distinct shelf bay visits
                if (!string.IsNullOrEmpty(evt.LocationName) &&
                    !evt.LocationName.Equals("GENERAL_FLOOR", StringComparison.OrdinalIgnoreCase) &&
                    !evt.LocationName.Equals("PACK-01", StringComparison.OrdinalIgnoreCase))
                {
                    distinctShelfSlots.Add(evt.LocationName);
                }
            }

            float walkingDuration = generalWalkingDuration + movingBetweenLocationsDuration + approachingDuration;

            // Compute continuous time-study percentages
            float walkingPercentage = 0f;
            float searchingPercentage = 0f;
            float pickingPercentage = 0f;
            float waitingPercentage = 0f;
            float packingPercentage = 0f;
            float orderCompletedPercentage = 0f;

            if (totalTaskTime > 0.0001f)
            {
                walkingPercentage = (walkingDuration / totalTaskTime) * 100f;
                searchingPercentage = (searchingDuration / totalTaskTime) * 100f;
                pickingPercentage = (pickingDuration / totalTaskTime) * 100f;
                waitingPercentage = (waitingDuration / totalTaskTime) * 100f;
                packingPercentage = (packingDuration / totalTaskTime) * 100f;
                orderCompletedPercentage = (orderCompletedDuration / totalTaskTime) * 100f;
            }

            // Interval-Based Work Sampling
            int totalSamples = 0;
            var sampleCounts = ComputeWorkSampling(events, samplingIntervalSeconds, out totalSamples);

            return new WorkStudyResults(
                totalTaskTime: totalTaskTime,
                totalDistance: totalDistance,
                totalPicks: totalPicks,
                shelfVisits: distinctShelfSlots.Count,
                totalEventsCount: events.Count,
                walkingDuration: walkingDuration,
                searchingDuration: searchingDuration,
                pickingDuration: pickingDuration,
                waitingDuration: waitingDuration,
                packingDuration: packingDuration,
                orderCompletedDuration: orderCompletedDuration,
                generalWalkingDuration: generalWalkingDuration,
                movingBetweenLocationsDuration: movingBetweenLocationsDuration,
                approachingDuration: approachingDuration,
                walkingPercentage: walkingPercentage,
                searchingPercentage: searchingPercentage,
                pickingPercentage: pickingPercentage,
                waitingPercentage: waitingPercentage,
                packingPercentage: packingPercentage,
                orderCompletedPercentage: orderCompletedPercentage,
                samplingIntervalSeconds: samplingIntervalSeconds,
                totalWorkSamplesCount: totalSamples,
                sampleCounts: sampleCounts);
        }

        /// <summary>
        /// Convenience method computing results directly from a WorkEventLogger instance.
        /// </summary>
        public static WorkStudyResults CalculateFromLogger(WorkEventLogger logger, float samplingIntervalSeconds = 1.0f)
        {
            if (logger == null)
            {
                return Calculate(new List<WorkEvent>(), samplingIntervalSeconds);
            }
            return Calculate(logger.LoggedEvents, samplingIntervalSeconds);
        }

        /// <summary>
        /// Formal interval-based work-sampling calculation.
        /// Samples state at discrete points t_k = t_0 + k * delta_t along the timeline of actual events.
        /// Note: This is an automated simulation-based work sampling observer; it does not claim external
        /// ANSI/ISO standards compliance without empirical third-party validation.
        /// </summary>
        private static List<WorkStudyResults.ActivitySampleCount> ComputeWorkSampling(
            IReadOnlyList<WorkEvent> events,
            float intervalSeconds,
            out int totalSamples)
        {
            var result = new List<WorkStudyResults.ActivitySampleCount>();
            totalSamples = 0;

            if (events == null || events.Count == 0 || intervalSeconds <= 0.01f)
            {
                return result;
            }

            float tStart = events[0].StartTime;
            float tEnd = events[events.Count - 1].EndTime;

            if (tEnd <= tStart)
            {
                return result;
            }

            var counts = new Dictionary<ActivityType, int>();
            foreach (ActivityType act in Enum.GetValues(typeof(ActivityType)))
            {
                counts[act] = 0;
            }

            int eventIndex = 0;
            for (float t = tStart; t <= tEnd + 0.0001f; t += intervalSeconds)
            {
                // Find event active at timestamp t
                while (eventIndex < events.Count - 1 && events[eventIndex].EndTime < t)
                {
                    eventIndex++;
                }

                var currentEvent = events[eventIndex];
                counts[currentEvent.Activity]++;
                totalSamples++;
            }

            if (totalSamples > 0)
            {
                foreach (var kvp in counts)
                {
                    if (kvp.Value > 0)
                    {
                        float pct = ((float)kvp.Value / totalSamples) * 100f;
                        result.Add(new WorkStudyResults.ActivitySampleCount(kvp.Key, kvp.Value, pct));
                    }
                }
            }

            return result;
        }
    }
}
