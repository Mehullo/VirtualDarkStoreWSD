using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Scenarios
{
    /// <summary>
    /// Encapsulates the execution results of an entire multi-order simulation session.
    /// Derives aggregate metrics strictly from individual completed order runs and recorded WorkEvents.
    /// </summary>
    [Serializable]
    public class MultiOrderSimulationResult
    {
        public string simulationId;
        public int totalOrdersRequested;
        public int totalOrdersCompleted;
        public int itemsPerOrder;
        public int totalItemsRequested;
        public int totalItemsPicked;
        public int seed;
        public float totalSimulationDuration;
        public float totalTravelDistance;
        public int totalAisleTransitions;
        public int totalAisleBacktracks;
        public float averageCycleDurationPerOrder;
        public float averageDistancePerOrder;
        public List<OperationalCycleRunResult> orderRuns = new List<OperationalCycleRunResult>();
        public List<WorkEvent> allEvents = new List<WorkEvent>();

        public WorkStudyResults AggregateWorkStudy => WorkStudyCalculator.Calculate(allEvents);
    }
}
