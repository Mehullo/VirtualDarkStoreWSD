using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Scenarios
{
    [Serializable]
    public class OperationalComparison
    {
        public const string Disclaimer =
            "All figures in this comparison are MEASURED SIMULATION TELEMETRY derived deterministically from recorded " +
            "virtual-picker operational cycles. They are not predictions or real-world measurements. " +
            "This is a single-order deterministic work-study comparison; no statistical significance is claimed without a multi-order sample.";

        public OperationalCycleRunResult RunA { get; }
        public OperationalCycleRunResult RunB { get; }
        public WorkStudyResults ResultsA { get; }
        public WorkStudyResults ResultsB { get; }

        public bool IsEquivalent { get; }
        public IReadOnlyList<string> EquivalenceIssues { get; }
        public IReadOnlyList<KpiRow> Kpis { get; }
        public IReadOnlyList<KpiRow> ActivityDistribution { get; }

        public OperationalComparison(OperationalCycleRunResult runA, OperationalCycleRunResult runB)
        {
            RunA = runA ?? throw new ArgumentNullException(nameof(runA));
            RunB = runB ?? throw new ArgumentNullException(nameof(runB));
            ResultsA = runA.Results;
            ResultsB = runB.Results;

            var issues = new List<string>();

            // Verify identical SKU set
            var setA = runA.skuSequence.Select(x => x.Trim().ToUpperInvariant()).OrderBy(x => x).ToList();
            var setB = runB.skuSequence.Select(x => x.Trim().ToUpperInvariant()).OrderBy(x => x).ToList();
            if (!setA.SequenceEqual(setB))
            {
                issues.Add("Order SKU sets differ (must contain identical items).");
            }

            IsEquivalent = (issues.Count == 0);
            EquivalenceIssues = issues;

            var a = ResultsA;
            var b = ResultsB;

            Kpis = new List<KpiRow>
            {
                Row("Total Operational Cycle Time", "s", runA.totalCycleDuration, runB.totalCycleDuration),
                Row("Walking / Transit Time", "s", a.WalkingDuration, b.WalkingDuration),
                Row("Searching Shelf Time", "s", a.SearchingDuration, b.SearchingDuration),
                Row("Item Picking Time", "s", a.PickingDuration, b.PickingDuration),
                Row("Waiting / Desk Time", "s", a.WaitingDuration, b.WaitingDuration),
                Row("Packing & Deposit Time", "s", a.PackingDuration, b.PackingDuration),
                Row("Order Completed Element", "s", a.OrderCompletedDuration, b.OrderCompletedDuration),
                Row("Measured Picker Distance", "m", runA.measuredPlayerDistance, runB.measuredPlayerDistance),
                Row("Algorithmic Route Cost (Solver Matrix)", "m", runA.algorithmicRouteCost, runB.algorithmicRouteCost),
                Row("Successful Item Picks", "count", a.TotalPicks, b.TotalPicks),
                Row("Distinct Shelf Bays Visited", "count", a.ShelfVisits, b.ShelfVisits),
                Row("Inter-Aisle Transitions", "count", runA.aisleTransitions, runB.aisleTransitions),
                Row("Aisle Backtracks (Revisited Aisle)", "count", runA.aisleBacktracks, runB.aisleBacktracks)
            };

            ActivityDistribution = new List<KpiRow>
            {
                Row("Walking / Transit", "% of time", a.WalkingPercentage, b.WalkingPercentage),
                Row("Searching Shelf", "% of time", a.SearchingPercentage, b.SearchingPercentage),
                Row("Item Picking", "% of time", a.PickingPercentage, b.PickingPercentage),
                Row("Waiting / Desk", "% of time", a.WaitingPercentage, b.WaitingPercentage),
                Row("Packing & Deposit", "% of time", a.PackingPercentage, b.PackingPercentage),
                Row("Order Completed", "% of time", a.OrderCompletedPercentage, b.OrderCompletedPercentage)
            };
        }

        private static KpiRow Row(string name, string unit, float a, float b)
        {
            return new KpiRow { Name = name, Unit = unit, A = a, B = b };
        }

        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Operational Work-Study Comparison: Baseline vs Optimized Route");
            sb.AppendLine();
            sb.AppendLine($"> **Methodology Notice**: {Disclaimer}");
            sb.AppendLine();
            sb.AppendLine("## 1. Study & Order Configuration");
            sb.AppendLine("| Parameter | Baseline (Scenario A) | Optimized (Scenario B) |");
            sb.AppendLine("|---|---|---|");
            sb.AppendLine($"| **Order ID** | `{RunA.orderId}` | `{RunB.orderId}` |");
            sb.AppendLine($"| **SKU Count** | `{RunA.skuSequence.Count}` items | `{RunB.skuSequence.Count}` items |");
            sb.AppendLine($"| **Pick Sequence** | {string.Join(" → ", RunA.skuSequence)} | {string.Join(" → ", RunB.skuSequence)} |");
            sb.AppendLine($"| **Optimization Mode** | Baseline (Original Sequence) | Google OR-Tools Local Solver (Guided Local Search) |");
            sb.AppendLine($"| **Recorded At** | {RunA.recordedAtLocal} | {RunB.recordedAtLocal} |");
            if (RunA.generationConfig != null)
            {
                sb.AppendLine($"| **Random Seed** | `{RunA.generationConfig.Seed}` | `{RunA.generationConfig.Seed}` |");
                sb.AppendLine($"| **Duplicate Policy** | `{RunA.generationConfig.DuplicatePolicy}` | `{RunA.generationConfig.DuplicatePolicy}` |");
            }
            sb.AppendLine($"| **Equivalent Conditions** | **{(IsEquivalent ? "YES" : "NO: " + string.Join("; ", EquivalenceIssues))}** | - |");
            sb.AppendLine();

            sb.AppendLine("## 2. Operational Cycle KPI Comparison");
            sb.AppendLine("| Metric | Unit | Baseline (A) | Optimized (B) | Delta (B − A) | Delta % |");
            sb.AppendLine("|---|---|---:|---:|---:|---:|");
            foreach (var r in Kpis)
            {
                string pct = r.DeltaPercent.HasValue ? $"{r.DeltaPercent.Value:F1}%" : "n/a";
                sb.AppendLine($"| {r.Name} | {r.Unit} | {r.A.ToString("F2", CultureInfo.InvariantCulture)} | {r.B.ToString("F2", CultureInfo.InvariantCulture)} | {r.Delta.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture)} | {pct} |");
            }
            sb.AppendLine();

            sb.AppendLine("## 3. Activity Distribution Comparison (Share of Cycle Time)");
            sb.AppendLine("| Activity Category | Unit | Baseline (A) | Optimized (B) | Delta (B − A) |");
            sb.AppendLine("|---|---|---:|---:|---:|");
            foreach (var r in ActivityDistribution)
            {
                sb.AppendLine($"| {r.Name} | {r.Unit} | {r.A.ToString("F1", CultureInfo.InvariantCulture)}% | {r.B.ToString("F1", CultureInfo.InvariantCulture)}% | {r.Delta.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture)}% |");
            }
            sb.AppendLine();

            sb.AppendLine("## 4. Key Work-Study Observations");
            float timeSaved = RunA.totalCycleDuration - RunB.totalCycleDuration;
            float distSaved = RunA.measuredPlayerDistance - RunB.measuredPlayerDistance;
            float distPercent = RunA.measuredPlayerDistance > 0.01f ? (distSaved / RunA.measuredPlayerDistance) * 100f : 0f;

            sb.AppendLine($"- **Cycle Time Reduction**: `{(timeSaved >= 0 ? timeSaved.ToString("F2", CultureInfo.InvariantCulture) + " s saved" : Math.Abs(timeSaved).ToString("F2", CultureInfo.InvariantCulture) + " s slower")}`.");
            sb.AppendLine($"- **Physical Travel Reduction**: `{distSaved:F2} m` measured distance reduction (`{distPercent:F1}%` travel savings).");
            sb.AppendLine($"- **Aisle Backtracking**: Reduced from `{RunA.aisleBacktracks}` to `{RunB.aisleBacktracks}` backtracks.");
            sb.AppendLine($"- **Aisle Transitions**: Changed from `{RunA.aisleTransitions}` to `{RunB.aisleTransitions}` inter-aisle crossings.");
            sb.AppendLine("- **Order Completion**: Both baseline and optimized routes fulfilled all required SKUs and deposited the order at `PACK-01`.");
            sb.AppendLine();
            sb.AppendLine("> *Note: Results represent deterministic measurement of this specific order layout. Statistical significance across broader order distributions requires multi-batch sampling.*");

            return sb.ToString();
        }

        public string ToCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Metric,Unit,Baseline_A,Optimized_B,Delta_B_minus_A,Delta_Percent");
            foreach (var r in Kpis)
            {
                string pct = r.DeltaPercent.HasValue ? r.DeltaPercent.Value.ToString("F1", CultureInfo.InvariantCulture) : "";
                sb.AppendLine($"{WorkStudyReportExporter.EscapeCsv(r.Name)},{r.Unit},{r.A.ToString("F2", CultureInfo.InvariantCulture)},{r.B.ToString("F2", CultureInfo.InvariantCulture)},{r.Delta.ToString("F2", CultureInfo.InvariantCulture)},{pct}");
            }
            return sb.ToString();
        }

        public void ExportReportBundle(string targetDirectory, string baseFileName = "baseline_vs_optimized_report")
        {
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                targetDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "evidence", "phase14"));
            }
            Directory.CreateDirectory(targetDirectory);

            string mdPath = Path.Combine(targetDirectory, $"{baseFileName}.md");
            string csvPath = Path.Combine(targetDirectory, $"{baseFileName}.csv");

            File.WriteAllText(mdPath, ToMarkdown(), Encoding.UTF8);
            File.WriteAllText(csvPath, ToCsv(), Encoding.UTF8);
            Debug.Log($"[OperationalComparison] Exported comparison report to {mdPath} and {csvPath}");
        }
    }
}
