using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace VirtualDarkStore.WorkStudy
{
    /// <summary>
    /// Formats and exports comprehensive Industrial Work-Study and Work-Sampling reports
    /// in Markdown, RFC-4180 CSV, and JSON formats directly from verified simulation event logs.
    /// Strictly excludes AI/ML predictions and uninstrumented physiological claims.
    /// </summary>
    public static class WorkStudyReportExporter
    {
        public const string MethodologyDisclaimer =
            "All figures in this report are MEASURED SIMULATION TELEMETRY derived deterministically from recorded " +
            "WorkEvent logs. No AI/ML prediction, synthetic dataset generation, or uninstrumented physiological/eye-gaze " +
            "estimation is used.";

        public static string GenerateMarkdownReport(
            WorkStudyResults results,
            IReadOnlyList<WorkEvent> events,
            string orderId = "ORD-0001",
            string scenarioId = "LIVE-SESSION",
            string executionMode = "Desktop")
        {
            if (results == null)
            {
                results = WorkStudyCalculator.Calculate(events ?? new List<WorkEvent>());
            }

            bool isReconciled = results.VerifyReconciliation(out string reconReason);
            float meanPickCycle = results.TotalPicks > 0 ? results.TotalTaskTime / results.TotalPicks : 0f;
            float avgTravelSpeed = results.WalkingDuration > 0.001f ? results.TotalDistance / results.WalkingDuration : 0f;

            var sb = new StringBuilder();
            sb.AppendLine($"# Industrial Work-Study & Work-Sampling Report: {orderId}");
            sb.AppendLine();
            sb.AppendLine($"> **Methodology Notice**: {MethodologyDisclaimer}");
            sb.AppendLine();
            sb.AppendLine("## 1. Session & Study Metadata");
            sb.AppendLine("| Field | Value |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| **Order ID** | `{EscapeMd(orderId)}` |");
            sb.AppendLine($"| **Scenario ID** | `{EscapeMd(scenarioId)}` |");
            sb.AppendLine($"| **Execution Mode** | `{EscapeMd(executionMode)}` |");
            sb.AppendLine($"| **Total Logged Events** | `{results.TotalEventsCount}` |");
            sb.AppendLine($"| **Work-Sampling Interval** | `{results.SamplingIntervalSeconds.ToString("F2", CultureInfo.InvariantCulture)} s` (`{results.TotalWorkSamplesCount}` observations) |");
            sb.AppendLine($"| **Mathematical Reconciliation** | **{(isReconciled ? "PASS (100.0% Reconciled)" : "FAIL: " + reconReason)}** |");
            sb.AppendLine();

            sb.AppendLine("## 2. Executive KPI Summary");
            sb.AppendLine("| KPI Metric | Value | Unit |");
            sb.AppendLine("|---|---:|---|");
            sb.AppendLine($"| **Total Task Time** | `{results.TotalTaskTime.ToString("F2", CultureInfo.InvariantCulture)}` | `s` |");
            sb.AppendLine($"| **Total Distance Traveled** | `{results.TotalDistance.ToString("F2", CultureInfo.InvariantCulture)}` | `m` |");
            sb.AppendLine($"| **Successful Item Picks** | `{results.TotalPicks}` | `items` |");
            sb.AppendLine($"| **Distinct Shelf Bays Visited** | `{results.ShelfVisits}` | `bays` |");
            sb.AppendLine($"| **Mean Cycle Time per Pick** | `{meanPickCycle.ToString("F2", CultureInfo.InvariantCulture)}` | `s/item` |");
            sb.AppendLine($"| **Mean Active Walking Speed** | `{avgTravelSpeed.ToString("F2", CultureInfo.InvariantCulture)}` | `m/s` |");
            sb.AppendLine();

            sb.AppendLine("## 3. Continuous Time-Study Elemental Breakdown");
            sb.AppendLine("| Activity Category | Duration (s) | Share of Task Time (%) |");
            sb.AppendLine("|---|---:|---:|");
            sb.AppendLine($"| **Walking / Travel (Total)** | `{results.WalkingDuration.ToString("F2", CultureInfo.InvariantCulture)}` | `{results.WalkingPercentage.ToString("F1", CultureInfo.InvariantCulture)}%` |");
            sb.AppendLine($"|   ↳ *Moving Between Locations (Macro Transit)* | `{results.MovingBetweenLocationsDuration.ToString("F2", CultureInfo.InvariantCulture)}` | `{PercentOf(results.MovingBetweenLocationsDuration, results.TotalTaskTime).ToString("F1", CultureInfo.InvariantCulture)}%` |");
            sb.AppendLine($"|   ↳ *Approaching Target Zone (<= 3.0m)* | `{results.ApproachingDuration.ToString("F2", CultureInfo.InvariantCulture)}` | `{PercentOf(results.ApproachingDuration, results.TotalTaskTime).ToString("F1", CultureInfo.InvariantCulture)}%` |");
            sb.AppendLine($"|   ↳ *General Unassigned Walking* | `{results.GeneralWalkingDuration.ToString("F2", CultureInfo.InvariantCulture)}` | `{PercentOf(results.GeneralWalkingDuration, results.TotalTaskTime).ToString("F1", CultureInfo.InvariantCulture)}%` |");
            sb.AppendLine($"| **Searching Shelf (Stationary Orientation)** | `{results.SearchingDuration.ToString("F2", CultureInfo.InvariantCulture)}` | `{results.SearchingPercentage.ToString("F1", CultureInfo.InvariantCulture)}%` |");
            sb.AppendLine($"| **Item Picking (Validated SKU Grasp)** | `{results.PickingDuration.ToString("F2", CultureInfo.InvariantCulture)}` | `{results.PickingPercentage.ToString("F1", CultureInfo.InvariantCulture)}%` |");
            sb.AppendLine($"| **Waiting / Idle** | `{results.WaitingDuration.ToString("F2", CultureInfo.InvariantCulture)}` | `{results.WaitingPercentage.ToString("F1", CultureInfo.InvariantCulture)}%` |");
            sb.AppendLine($"| **Order Packing (At Dispatch Bench)** | `{results.PackingDuration.ToString("F2", CultureInfo.InvariantCulture)}` | `{results.PackingPercentage.ToString("F1", CultureInfo.InvariantCulture)}%` |");
            sb.AppendLine($"| **Order Completed (Dispatch Finalization)** | `{results.OrderCompletedDuration.ToString("F2", CultureInfo.InvariantCulture)}` | `{results.OrderCompletedPercentage.ToString("F1", CultureInfo.InvariantCulture)}%` |");
            float sumDur = results.WalkingDuration + results.SearchingDuration + results.PickingDuration +
                           results.WaitingDuration + results.PackingDuration + results.OrderCompletedDuration;
            float sumPct = results.WalkingPercentage + results.SearchingPercentage + results.PickingPercentage +
                           results.WaitingPercentage + results.PackingPercentage + results.OrderCompletedPercentage;
            sb.AppendLine($"| **TOTAL RECONCILED** | **`{sumDur.ToString("F2", CultureInfo.InvariantCulture)}`** | **`{sumPct.ToString("F1", CultureInfo.InvariantCulture)}%`** |");
            sb.AppendLine();

            sb.AppendLine("## 4. Interval Work-Sampling Observations");
            if (results.TotalWorkSamplesCount > 0 && results.SampleCounts != null)
            {
                sb.AppendLine("| Activity State | Sample Observations (n) | Sample Proportion (%) |");
                sb.AppendLine("|---|---:|---:|");
                foreach (var s in results.SampleCounts)
                {
                    sb.AppendLine($"| `{s.Activity}` | `{s.Count}` | `{s.Percentage.ToString("F1", CultureInfo.InvariantCulture)}%` |");
                }
                sb.AppendLine($"| **Total Observations (N)** | **`{results.TotalWorkSamplesCount}`** | **`100.0%`** |");
            }
            else
            {
                sb.AppendLine("_No discrete work-sampling observations recorded._");
            }
            sb.AppendLine();

            sb.AppendLine("## 5. Chronological Elemental Event Log");
            if (events != null && events.Count > 0)
            {
                sb.AppendLine("| Event ID | Activity | Start (s) | End (s) | Duration (s) | Distance (m) | Location | SKU | Order | World Position (x, y, z) |");
                sb.AppendLine("|---|---|---:|---:|---:|---:|---|---|---|---|");
                for (int i = 0; i < events.Count; i++)
                {
                    var e = events[i];
                    string posStr = string.Format(CultureInfo.InvariantCulture, "({0:F1}, {1:F1}, {2:F1})", e.Position.x, e.Position.y, e.Position.z);
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "| `{0}` | `{1}` | {2:F2} | {3:F2} | {4:F2} | {5:F2} | `{6}` | `{7}` | `{8}` | `{9}` |",
                        EscapeMd(e.Id),
                        e.Activity,
                        e.StartTime,
                        e.EndTime,
                        e.Duration,
                        e.Distance,
                        EscapeMd(string.IsNullOrEmpty(e.LocationName) ? "-" : e.LocationName),
                        EscapeMd(string.IsNullOrEmpty(e.RelatedSku) ? "-" : e.RelatedSku),
                        EscapeMd(string.IsNullOrEmpty(e.RelatedOrder) ? "-" : e.RelatedOrder),
                        posStr));
                }
            }
            else
            {
                sb.AppendLine("_No events logged._");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates an RFC-4180 compliant CSV string of all recorded WorkEvent items.
        /// </summary>
        public static string GenerateEventsCsv(IReadOnlyList<WorkEvent> events)
        {
            var sb = new StringBuilder();
            sb.AppendLine("EventId,Activity,StartTimeSeconds,EndTimeSeconds,DurationSeconds,DistanceMeters,LocationName,RelatedSku,RelatedOrder,PosX,PosY,PosZ");

            if (events == null) return sb.ToString();

            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F3},{3:F3},{4:F3},{5:F3},{6},{7},{8},{9:F2},{10:F2},{11:F2}\r\n",
                    EscapeCsv(e.Id),
                    EscapeCsv(e.Activity.ToString()),
                    e.StartTime,
                    e.EndTime,
                    e.Duration,
                    e.Distance,
                    EscapeCsv(e.LocationName),
                    EscapeCsv(e.RelatedSku),
                    EscapeCsv(e.RelatedOrder),
                    e.Position.x,
                    e.Position.y,
                    e.Position.z);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates an RFC-4180 compliant CSV summary of aggregated work-study metrics.
        /// </summary>
        public static string GenerateSummaryCsv(WorkStudyResults results, string orderId = "ORD-0001", string scenarioId = "LIVE-SESSION")
        {
            var sb = new StringBuilder();
            sb.AppendLine("OrderId,ScenarioId,TotalTaskTimeSeconds,TotalDistanceMeters,TotalPicks,ShelfVisits,TotalEvents,WalkingSeconds,SearchingSeconds,PickingSeconds,WaitingSeconds,PackingSeconds,OrderCompletedSeconds,WalkingPct,SearchingPct,PickingPct,WaitingPct,PackingPct,OrderCompletedPct,Reconciled");

            if (results == null) return sb.ToString();

            bool reconciled = results.VerifyReconciliation(out _);
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "{0},{1},{2:F3},{3:F3},{4},{5},{6},{7:F3},{8:F3},{9:F3},{10:F3},{11:F3},{12:F3},{13:F2},{14:F2},{15:F2},{16:F2},{17:F2},{18:F2},{19}\r\n",
                EscapeCsv(orderId),
                EscapeCsv(scenarioId),
                results.TotalTaskTime,
                results.TotalDistance,
                results.TotalPicks,
                results.ShelfVisits,
                results.TotalEventsCount,
                results.WalkingDuration,
                results.SearchingDuration,
                results.PickingDuration,
                results.WaitingDuration,
                results.PackingDuration,
                results.OrderCompletedDuration,
                results.WalkingPercentage,
                results.SearchingPercentage,
                results.PickingPercentage,
                results.WaitingPercentage,
                results.PackingPercentage,
                results.OrderCompletedPercentage,
                reconciled ? "TRUE" : "FALSE");

            return sb.ToString();
        }

        public static void ExportReportBundle(
            string directoryPath,
            string baseName,
            WorkStudyResults results,
            IReadOnlyList<WorkEvent> events,
            string orderId,
            string scenarioId,
            string executionMode,
            out string markdownPath,
            out string eventsCsvPath,
            out string summaryCsvPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                directoryPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Reports"));
            }
            Directory.CreateDirectory(directoryPath);

            string safeBase = SanitizeFileName(string.IsNullOrWhiteSpace(baseName) ? "work_study_report" : baseName);
            markdownPath = Path.Combine(directoryPath, $"{safeBase}.md");
            eventsCsvPath = Path.Combine(directoryPath, $"{safeBase}_events.csv");
            summaryCsvPath = Path.Combine(directoryPath, $"{safeBase}_summary.csv");

            File.WriteAllText(markdownPath, GenerateMarkdownReport(results, events, orderId, scenarioId, executionMode), Encoding.UTF8);
            File.WriteAllText(eventsCsvPath, GenerateEventsCsv(events), Encoding.UTF8);
            File.WriteAllText(summaryCsvPath, GenerateSummaryCsv(results, orderId, scenarioId), Encoding.UTF8);
        }

        private static float PercentOf(float part, float total)
        {
            if (total <= 0.0001f) return 0f;
            return (part / total) * 100f;
        }

        private static string EscapeMd(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("|", "\\|");
        }

        public static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
