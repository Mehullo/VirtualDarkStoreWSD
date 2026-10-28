using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.WorkStudy
{
    /// <summary>
    /// Central recorder and in-memory repository for WorkStudy measurement events.
    /// Strictly guarantees ordered timestamps and non-negative event durations.
    /// </summary>
    public class WorkEventLogger : MonoBehaviour
    {
        private static WorkEventLogger instance;
        public static WorkEventLogger Instance => instance != null ? instance : (instance = FindAnyObjectByType<WorkEventLogger>());

        [SerializeField] private List<WorkEvent> loggedEvents = new List<WorkEvent>();

        public IReadOnlyList<WorkEvent> LoggedEvents => loggedEvents;
        public int EventCount => loggedEvents.Count;

        public event Action<WorkEvent> OnEventLogged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        /// <summary>
        /// Logs a validated WorkEvent. Enforces ordered start timestamps and non-negative durations.
        /// </summary>
        public void LogEvent(WorkEvent workEvent)
        {
            if (workEvent == null)
            {
                throw new ArgumentNullException(nameof(workEvent), "Cannot log null WorkEvent.");
            }

            if (workEvent.Duration < 0f)
            {
                throw new InvalidOperationException($"Invalid event duration {workEvent.Duration:F3}s on event {workEvent.Id}. Duration cannot be negative.");
            }

            if (loggedEvents.Count > 0)
            {
                var prev = loggedEvents[loggedEvents.Count - 1];
                if (workEvent.StartTime < prev.StartTime)
                {
                    throw new InvalidOperationException(
                        $"Timestamp ordering violation: Event {workEvent.Id} StartTime ({workEvent.StartTime:F3}s) " +
                        $"precedes previous event {prev.Id} StartTime ({prev.StartTime:F3}s).");
                }
            }

            loggedEvents.Add(workEvent);
            Debug.Log($"[WorkEventLogger] Logged: {workEvent}");
            OnEventLogged?.Invoke(workEvent);
        }

        /// <summary>
        /// Clears all recorded events.
        /// </summary>
        public void Clear()
        {
            loggedEvents.Clear();
        }

        /// <summary>
        /// Verifies that all recorded events maintain monotonic non-decreasing timestamps and non-negative durations.
        /// </summary>
        public bool VerifyIntegrity(out string failureReason)
        {
            failureReason = null;
            for (int i = 0; i < loggedEvents.Count; i++)
            {
                var evt = loggedEvents[i];
                if (evt.Duration < 0f)
                {
                    failureReason = $"Event index {i} ({evt.Id}) has negative duration: {evt.Duration}s";
                    return false;
                }
                if (evt.EndTime < evt.StartTime)
                {
                    failureReason = $"Event index {i} ({evt.Id}) EndTime ({evt.EndTime}s) is less than StartTime ({evt.StartTime}s)";
                    return false;
                }
                if (i > 0)
                {
                    var prev = loggedEvents[i - 1];
                    if (evt.StartTime < prev.StartTime)
                    {
                        failureReason = $"Event index {i} ({evt.Id}) StartTime ({evt.StartTime}s) < previous event ({prev.Id}) StartTime ({prev.StartTime}s)";
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
