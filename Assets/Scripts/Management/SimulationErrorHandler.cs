using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.Management
{
    public enum SimulationAlertSeverity
    {
        Info,
        Warning,
        Error
    }

    [Serializable]
    public struct SimulationAlert
    {
        public string Timestamp;
        public SimulationAlertSeverity Severity;
        public string Source;
        public string Message;

        public SimulationAlert(SimulationAlertSeverity severity, string source, string message)
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss");
            Severity = severity;
            Source = source ?? "System";
            Message = message ?? string.Empty;
        }

        public override string ToString() => $"[{Timestamp}] [{Severity}] {Source}: {Message}";
    }

    /// <summary>
    /// Centralized operational error, warning, and scene-validation handler.
    /// Captures recoverable runtime issues (invalid scenario files, mismatched picks, premature packing,
    /// missing references) without crashing the simulation, and broadcasts alerts to the UI.
    /// </summary>
    public class SimulationErrorHandler : MonoBehaviour
    {
        private static SimulationErrorHandler instance;
        public static SimulationErrorHandler Instance => instance != null ? instance : (instance = FindAnyObjectByType<SimulationErrorHandler>());

        private const int MaxAlertHistory = 50;
        [SerializeField] private List<SimulationAlert> alertHistory = new List<SimulationAlert>();

        public IReadOnlyList<SimulationAlert> AlertHistory => alertHistory;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }

        public event Action<SimulationAlert> OnAlertRaised;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        public static void ReportInfo(string source, string message)
        {
            Record(SimulationAlertSeverity.Info, source, message);
        }

        public static void ReportWarning(string source, string message)
        {
            Record(SimulationAlertSeverity.Warning, source, message);
        }

        public static void ReportError(string source, string message)
        {
            Record(SimulationAlertSeverity.Error, source, message);
        }

        private static void Record(SimulationAlertSeverity severity, string source, string message)
        {
            var alert = new SimulationAlert(severity, source, message);

            if (severity == SimulationAlertSeverity.Error)
            {
                Debug.LogWarning($"[SimulationErrorHandler][ERROR] {source}: {message}");
            }
            else if (severity == SimulationAlertSeverity.Warning)
            {
                Debug.LogWarning($"[SimulationErrorHandler][WARN] {source}: {message}");
            }
            else
            {
                Debug.Log($"[SimulationErrorHandler][INFO] {source}: {message}");
            }

            var handler = Instance;
            if (handler != null)
            {
                handler.AddAlert(alert);
            }
        }

        public void AddAlert(SimulationAlert alert)
        {
            if (alert.Severity == SimulationAlertSeverity.Error) ErrorCount++;
            else if (alert.Severity == SimulationAlertSeverity.Warning) WarningCount++;

            alertHistory.Add(alert);
            if (alertHistory.Count > MaxAlertHistory)
            {
                alertHistory.RemoveAt(0);
            }

            OnAlertRaised?.Invoke(alert);
        }

        public void ClearHistory()
        {
            alertHistory.Clear();
            ErrorCount = 0;
            WarningCount = 0;
        }
    }
}
