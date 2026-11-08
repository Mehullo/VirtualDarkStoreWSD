using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using VirtualDarkStore.Domain.Participants;

namespace VirtualDarkStore.WorkStudy
{
    /// <summary>
    /// Service for generating, saving, and consolidating participant-wise work-study CSV reports.
    /// Strictly enforces RFC-4180 formatting, invariant decimal culture, and idempotent combined exports.
    /// </summary>
    public static class ParticipantDataExporter
    {
        public const string SummaryCsvHeader =
            "ParticipantID,ParticipantName,ScenarioName,SessionID,SessionDate,SessionStartTime,SessionEndTime,InteractionMode,RoutingMode,OrderID,BatchID,RandomSeed,TotalItems,PickedItems,RemainingItems,UniqueSKUs,TotalShelfVisits,AisleTransitions,TotalCycleTimeSeconds,TotalWalkingSeconds,TotalWaitingSeconds,TotalSearchingSeconds,TotalApproachingSeconds,TotalPickingSeconds,TotalPackingSeconds,TotalDistanceMeters,AveragePickTimeSeconds,AverageTimePerItemSeconds,ItemsPerMinute,WalkingPct,WaitingPct,SearchingPct,PickingPct,PackingPct,PlannedPickSequence,ActualPickSequence,PlannedRouteDistanceMeters,ActualTravelDistanceMeters,RouteCompletionStatus,SessionCompletionStatus,MissingEventCount,DataValidationStatus";

        public const string EventsCsvHeader =
            "ParticipantID,ParticipantName,SessionID,OrderID,EventID,EventType,StartTimeSeconds,EndTimeSeconds,DurationSeconds,DistanceMeters,SKU,LocationName,PosX,PosY,PosZ";

        /// <summary>
        /// Resolves the root export directory based on platform.
        /// </summary>
        public static string GetRootExportDirectory(string customBaseDirectory = null)
        {
            if (!string.IsNullOrWhiteSpace(customBaseDirectory))
            {
                return customBaseDirectory;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            return Path.Combine(Application.persistentDataPath, "WorkStudyData");
#else
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "WorkStudyData"));
#endif
        }

        /// <summary>
        /// Generates a standardized, collision-safe filename:
        /// ParticipantName_ScenarioName_RoutingMode_SessionID.csv
        /// </summary>
        public static string GenerateBaseFileName(string participantName, string scenarioName, string routingMode, string sessionId)
        {
            string safeName = SanitizeFileNamePart(participantName);
            string safeScn = SanitizeFileNamePart(scenarioName);
            string safeMode = SanitizeFileNamePart(routingMode);
            string safeSession = SanitizeFileNamePart(sessionId);
            return $"{safeName}_{safeScn}_{safeMode}_{safeSession}";
        }

        /// <summary>
        /// Converts a ParticipantSessionRecord into an RFC-4180 formatted CSV row.
        /// </summary>
        public static string FormatSummaryRecordRow(ParticipantSessionRecord r)
        {
            if (r == null) return string.Empty;
            return string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}," +
                "{18:F3},{19:F3},{20:F3},{21:F3},{22:F3},{23:F3},{24:F3},{25:F3}," +
                "{26:F3},{27:F3},{28:F3},{29:F2},{30:F2},{31:F2},{32:F2},{33:F2}," +
                "{34},{35},{36},{37:F3},{38},{39},{40},{41}",
                EscapeCsv(r.participantId),
                EscapeCsv(r.participantName),
                EscapeCsv(r.scenarioName),
                EscapeCsv(r.sessionId),
                EscapeCsv(r.sessionDate),
                EscapeCsv(r.sessionStartTime),
                EscapeCsv(r.sessionEndTime),
                EscapeCsv(r.interactionMode),
                EscapeCsv(r.routingMode),
                EscapeCsv(r.orderId),
                EscapeCsv(r.batchId),
                EscapeCsv(r.randomSeed),
                r.totalItems,
                r.pickedItems,
                r.remainingItems,
                r.uniqueSkus,
                r.totalShelfVisits,
                r.aisleTransitions,
                r.totalCycleTimeSeconds,
                r.totalWalkingSeconds,
                r.totalWaitingSeconds,
                r.totalSearchingSeconds,
                r.totalApproachingSeconds,
                r.totalPickingSeconds,
                r.totalPackingSeconds,
                r.totalDistanceMeters,
                r.averagePickTimeSeconds,
                r.averageTimePerItemSeconds,
                r.itemsPerMinute,
                r.walkingPercentage,
                r.waitingPercentage,
                r.searchingPercentage,
                r.pickingPercentage,
                r.packingPercentage,
                EscapeCsv(r.plannedPickSequence),
                EscapeCsv(r.actualPickSequence),
                EscapeCsv(r.plannedRouteDistanceMeters),
                r.actualTravelDistanceMeters,
                EscapeCsv(r.routeCompletionStatus),
                EscapeCsv(r.sessionCompletionStatus),
                r.missingEventCount,
                EscapeCsv(r.dataValidationStatus));
        }

        /// <summary>
        /// Converts a single WorkEvent into an RFC-4180 formatted CSV row with participant and session tags.
        /// </summary>
        public static string FormatEventRecordRow(string participantId, string participantName, string sessionId, WorkEvent e)
        {
            if (e == null) return string.Empty;
            return string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6:F3},{7:F3},{8:F3},{9:F3},{10},{11},{12:F2},{13:F2},{14:F2}",
                EscapeCsv(participantId),
                EscapeCsv(participantName),
                EscapeCsv(sessionId),
                EscapeCsv(e.RelatedOrder),
                EscapeCsv(e.Id),
                EscapeCsv(e.Activity.ToString()),
                e.StartTime,
                e.EndTime,
                e.Duration,
                e.Distance,
                EscapeCsv(e.RelatedSku),
                EscapeCsv(e.LocationName),
                e.Position.x,
                e.Position.y,
                e.Position.z);
        }

        /// <summary>
        /// Exports a complete participant work-study session bundle:
        /// 1. Individual Summary CSV under Participants/SlotID_Name/Summary/
        /// 2. Individual Events CSV under Participants/SlotID_Name/Events/
        /// 3. Appends/Merges deduplicated records into Combined/All_Session_Summaries.csv & Combined/All_Event_Records.csv
        /// </summary>
        public static void ExportSessionBundle(
            ParticipantSessionRecord record,
            IReadOnlyList<WorkEvent> events,
            string customBaseDir,
            out string summaryCsvPath,
            out string eventsCsvPath)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            string rootDir = GetRootExportDirectory(customBaseDir);
            string safeFolderName = $"{record.participantId}_{SanitizeFileNamePart(record.participantName)}";

            string participantDir = Path.Combine(rootDir, "Participants", safeFolderName);
            string summaryFolder = Path.Combine(participantDir, "Summary");
            string eventsFolder = Path.Combine(participantDir, "Events");
            string combinedFolder = Path.Combine(rootDir, "Combined");

            Directory.CreateDirectory(summaryFolder);
            Directory.CreateDirectory(eventsFolder);
            Directory.CreateDirectory(combinedFolder);

            string baseName = GenerateBaseFileName(record.participantName, record.scenarioName, record.routingMode, record.sessionId);
            summaryCsvPath = Path.Combine(summaryFolder, $"{baseName}.csv");
            eventsCsvPath = Path.Combine(eventsFolder, $"{baseName}_Events.csv");

            // 1. Write individual summary CSV
            var sbSum = new StringBuilder();
            sbSum.AppendLine(SummaryCsvHeader);
            sbSum.AppendLine(FormatSummaryRecordRow(record));
            File.WriteAllText(summaryCsvPath, sbSum.ToString(), Encoding.UTF8);

            // 2. Write individual events CSV
            var sbEvt = new StringBuilder();
            sbEvt.AppendLine(EventsCsvHeader);
            if (events != null)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    sbEvt.AppendLine(FormatEventRecordRow(record.participantId, record.participantName, record.sessionId, events[i]));
                }
            }
            File.WriteAllText(eventsCsvPath, sbEvt.ToString(), Encoding.UTF8);

            // 3. Update combined files
            UpdateCombinedSummaries(Path.Combine(combinedFolder, "All_Session_Summaries.csv"), record);
            UpdateCombinedEvents(Path.Combine(combinedFolder, "All_Event_Records.csv"), record, events);
        }

        /// <summary>
        /// Appends or updates a summary record in the combined CSV ensuring zero duplicates by SessionID.
        /// </summary>
        public static void UpdateCombinedSummaries(string combinedFilePath, ParticipantSessionRecord newRecord)
        {
            if (newRecord == null) return;

            var existingRows = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(combinedFilePath))
            {
                var lines = File.ReadAllLines(combinedFilePath, Encoding.UTF8);
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string sessId = ExtractSessionIdFromSummaryLine(line);
                    if (!string.IsNullOrEmpty(sessId))
                    {
                        existingRows[sessId] = line;
                    }
                }
            }

            // Upsert new record
            existingRows[newRecord.sessionId] = FormatSummaryRecordRow(newRecord);

            var sb = new StringBuilder();
            sb.AppendLine(SummaryCsvHeader);
            foreach (var kvp in existingRows)
            {
                sb.AppendLine(kvp.Value);
            }
            File.WriteAllText(combinedFilePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Appends or updates event rows in the combined CSV ensuring zero duplicates by (SessionID + EventID).
        /// </summary>
        public static void UpdateCombinedEvents(string combinedEventsFilePath, ParticipantSessionRecord record, IReadOnlyList<WorkEvent> events)
        {
            if (record == null || events == null) return;

            var existingRows = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(combinedEventsFilePath))
            {
                var lines = File.ReadAllLines(combinedEventsFilePath, Encoding.UTF8);
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string key = ExtractEventKey(line);
                    if (!string.IsNullOrEmpty(key))
                    {
                        existingRows[key] = line;
                    }
                }
            }

            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                string key = $"{record.sessionId}_{e.Id}";
                existingRows[key] = FormatEventRecordRow(record.participantId, record.participantName, record.sessionId, e);
            }

            var sb = new StringBuilder();
            sb.AppendLine(EventsCsvHeader);
            foreach (var kvp in existingRows)
            {
                sb.AppendLine(kvp.Value);
            }
            File.WriteAllText(combinedEventsFilePath, sb.ToString(), Encoding.UTF8);
        }

        private static string ExtractSessionIdFromSummaryLine(string line)
        {
            // Fields: 0: ParticipantID, 1: ParticipantName, 2: ScenarioName, 3: SessionID
            var parts = ParseCsvLine(line);
            if (parts.Count >= 4) return parts[3].Trim();
            return null;
        }

        private static string ExtractEventKey(string line)
        {
            // Fields: 0: ParticipantID, 1: ParticipantName, 2: SessionID, 3: OrderID, 4: EventID
            var parts = ParseCsvLine(line);
            if (parts.Count >= 5) return $"{parts[2].Trim()}_{parts[4].Trim()}";
            return null;
        }

        public static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(line)) return result;

            bool inQuotes = false;
            var sb = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        sb.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString());
            return result;
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

        public static string SanitizeFileNamePart(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Unassigned";
            var sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
            string result = sb.ToString().Trim('.', ' ', '_');
            return string.IsNullOrEmpty(result) ? "Unassigned" : result;
        }
    }
}
