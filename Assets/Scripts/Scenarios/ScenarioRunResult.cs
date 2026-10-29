using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Scenarios
{
    /// <summary>
    /// A recorded scenario run: the scenario, the actual WorkEvents logged by the shared classifier/logger,
    /// and the sampled route. KPI values are always recomputed from the stored events via
    /// <see cref="WorkStudyCalculator"/>; nothing is stored that was not measured in the run.
    /// </summary>
    [Serializable]
    public class ScenarioRunResult
    {
        public const string MeasuredLabel = "MEASURED SIMULATION RESULT (recorded run of the virtual picker; not a prediction)";

        public string label = MeasuredLabel;
        public string recordedAtLocal = string.Empty;
        public ScenarioDefinition scenario = new ScenarioDefinition();
        public List<WorkEvent> events = new List<WorkEvent>();
        public List<Vector3> routePoints = new List<Vector3>();
        public List<string> waypointLabels = new List<string>();
        public List<Vector3> waypointPositions = new List<Vector3>();
        public float samplingIntervalSeconds = 1f;

        public WorkStudyResults Results => WorkStudyCalculator.Calculate(events, samplingIntervalSeconds);
    }
}
