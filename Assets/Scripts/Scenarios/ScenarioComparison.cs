using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Scenarios
{
    [Serializable]
    public class KpiRow
    {
        public string Name;
        public string Unit;
        public float A;
        public float B;
        /// <summary>B minus A.</summary>
        public float Delta => B - A;
        /// <summary>(B-A)/A*100, or null when A is zero.</summary>
        public float? DeltaPercent => Math.Abs(A) > 1e-6f ? (B - A) / A * 100f : (float?)null;
    }

    /// <summary>
    /// Side-by-side comparison of two recorded runs of the same order. Pure computation on recorded
    /// events; no ranking, scoring, prediction or optimisation. 'Delta' is simply B minus A.
    /// </summary>
    public class ScenarioComparison
    {
        public const string Disclaimer =
            "All values are MEASURED SIMULATION RESULTS from recorded virtual-picker runs in this simulation. " +
            "They are not predictions and not real-world measurements.";

        public ScenarioRunResult RunA { get; }
        public ScenarioRunResult RunB { get; }
        public WorkStudyResults ResultsA { get; }
        public WorkStudyResults ResultsB { get; }
        public bool IsEquivalent { get; }
        public IReadOnlyList<string> EquivalenceIssues { get; }
        public IReadOnlyList<string> VariedFactors { get; }
        public bool BothReconciled { get; }
        public IReadOnlyList<KpiRow> Kpis { get; }
        public IReadOnlyList<KpiRow> ActivityDistribution { get; }

        public ScenarioComparison(ScenarioRunResult a, ScenarioRunResult b)
        {
            RunA = a ?? throw new ArgumentNullException(nameof(a));
            RunB = b ?? throw new ArgumentNullException(nameof(b));
            ResultsA = a.Results;
            ResultsB = b.Results;

            var issues = new List<string>();
            var varied = new List<string>();
            var sa = a.scenario; var sb = b.scenario;

            var setA = sa.skuSequence.Select(x => x.Trim().ToUpperInvariant()).OrderBy(x => x).ToList();
            var setB = sb.skuSequence.Select(x => x.Trim().ToUpperInvariant()).OrderBy(x => x).ToList();
            if (!setA.SequenceEqual(setB)) issues.Add("Order SKU sets differ (not the same order).");
            if (!sa.skuSequence.Select(x => x.ToUpperInvariant()).SequenceEqual(sb.skuSequence.Select(x => x.ToUpperInvariant())))
                varied.Add("pick/visit sequence");

            if ((sa.startPosition - sb.startPosition).sqrMagnitude > 1e-6f) issues.Add("Start positions differ.");
            if (Math.Abs(sa.walkSpeed - sb.walkSpeed) > 1e-6f) issues.Add("Walk speeds differ.");
            if (Math.Abs(sa.initialWaitSeconds - sb.initialWaitSeconds) > 1e-6f) issues.Add("Initial wait differs.");
            if (Math.Abs(sa.searchSeconds - sb.searchSeconds) > 1e-6f) issues.Add("Search time per pick differs.");
            if (Math.Abs(sa.packingSeconds - sb.packingSeconds) > 1e-6f) issues.Add("Packing time differs.");
            if (Math.Abs(sa.timeStep - sb.timeStep) > 1e-6f) issues.Add("Time step differs.");
            if (Math.Abs(a.samplingIntervalSeconds - b.samplingIntervalSeconds) > 1e-6f) issues.Add("Work-sampling intervals differ.");

            IsEquivalent = issues.Count == 0;
            EquivalenceIssues = issues;
            VariedFactors = varied;

            BothReconciled = ResultsA.VerifyReconciliation(out _) && ResultsB.VerifyReconciliation(out _);

            var A = ResultsA; var B = ResultsB;
            Kpis = new List<KpiRow>
            {
                Row("Total task time", "s", A.TotalTaskTime, B.TotalTaskTime),
                Row("Walking / travel time", "s", A.WalkingDuration, B.WalkingDuration),
                Row("Searching time", "s", A.SearchingDuration, B.SearchingDuration),
                Row("Picking time", "s", A.PickingDuration, B.PickingDuration),
                Row("Waiting time", "s", A.WaitingDuration, B.WaitingDuration),
                Row("Packing time", "s", A.PackingDuration, B.PackingDuration),
                Row("Order-completed element", "s", A.OrderCompletedDuration, B.OrderCompletedDuration),
                Row("Distance travelled", "m", A.TotalDistance, B.TotalDistance),
                Row("Successful picks", "count", A.TotalPicks, B.TotalPicks),
                Row("Shelf bay visits", "count", A.ShelfVisits, B.ShelfVisits),
            };
            ActivityDistribution = new List<KpiRow>
            {
                Row("Walking / travel", "% of time", A.WalkingPercentage, B.WalkingPercentage),
                Row("Searching", "% of time", A.SearchingPercentage, B.SearchingPercentage),
                Row("Picking", "% of time", A.PickingPercentage, B.PickingPercentage),
                Row("Waiting", "% of time", A.WaitingPercentage, B.WaitingPercentage),
                Row("Packing", "% of time", A.PackingPercentage, B.PackingPercentage),
                Row("Order completed", "% of time", A.OrderCompletedPercentage, B.OrderCompletedPercentage),
            };
        }

        private static KpiRow Row(string name, string unit, float a, float b) => new KpiRow { Name = name, Unit = unit, A = a, B = b };

        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Scenario A/B Comparison: {RunA.scenario.scenarioId} vs {RunB.scenario.scenarioId}");
            sb.AppendLine();
            sb.AppendLine($"> {Disclaimer}");
            sb.AppendLine();
            sb.AppendLine($"- A: **{RunA.scenario.scenarioName}** (`{RunA.scenario.scenarioId}`), recorded {RunA.recordedAtLocal}");
            sb.AppendLine($"  - SKU sequence: {string.Join(" → ", RunA.scenario.skuSequence)}");
            sb.AppendLine($"- B: **{RunB.scenario.scenarioName}** (`{RunB.scenario.scenarioId}`), recorded {RunB.recordedAtLocal}");
            sb.AppendLine($"  - SKU sequence: {string.Join(" → ", RunB.scenario.skuSequence)}");
            sb.AppendLine($"- Equivalent starting conditions: **{(IsEquivalent ? "YES" : "NO")}**" +
                          (IsEquivalent ? "" : " — " + string.Join("; ", EquivalenceIssues)));
            sb.AppendLine($"- Factor varied between A and B: {(VariedFactors.Count == 0 ? "none" : string.Join(", ", VariedFactors))}");
            sb.AppendLine($"- Duration/percentage reconciliation (both runs): {(BothReconciled ? "PASS" : "FAIL")}");
            sb.AppendLine($"- Events recorded: A={ResultsA.TotalEventsCount}, B={ResultsB.TotalEventsCount}");
            sb.AppendLine();
            sb.AppendLine("## KPI comparison (Delta = B − A)");
            sb.AppendLine();
            AppendTable(sb, Kpis);
            sb.AppendLine();
            sb.AppendLine("## Activity distribution (share of total task time)");
            sb.AppendLine();
            AppendTable(sb, ActivityDistribution);
            sb.AppendLine();
            sb.AppendLine("Walking / travel = General walking + Moving between locations + Approaching (as defined by `WorkStudyCalculator`).");
            sb.AppendLine("Distance is the horizontal path length accumulated by the classifier. Routing is a fixed straight-line rule, not NavMesh pathfinding.");
            return sb.ToString();
        }

        private static void AppendTable(StringBuilder sb, IReadOnlyList<KpiRow> rows)
        {
            sb.AppendLine("| Metric | Unit | A | B | Delta (B−A) | Delta % |");
            sb.AppendLine("|---|---|---:|---:|---:|---:|");
            foreach (var r in rows)
            {
                string pct = r.DeltaPercent.HasValue ? $"{r.DeltaPercent.Value:F1}%" : "n/a";
                sb.AppendLine($"| {r.Name} | {r.Unit} | {r.A:F2} | {r.B:F2} | {r.Delta:+0.00;-0.00;0.00} | {pct} |");
            }
        }
    }
}
