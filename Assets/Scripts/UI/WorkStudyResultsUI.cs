using System;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VirtualDarkStore.Domain;
using VirtualDarkStore.Management;
using VirtualDarkStore.Scenarios;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.UI
{
    /// <summary>
    /// Polished screen-space IMGUI results interface for displaying comprehensive industrial work-study,
    /// work-sampling metrics, chronological elemental event logs, and one-click report exports (MD/CSV).
    /// Toggleable via [Tab] key or displayed automatically upon order fulfillment.
    /// </summary>
    public class WorkStudyResultsUI : MonoBehaviour
    {
        private static WorkStudyResultsUI instance;
        public static WorkStudyResultsUI Instance => instance != null ? instance : (instance = FindAnyObjectByType<WorkStudyResultsUI>());

        [Header("Settings")]
        [SerializeField] private bool isVisible = false;
        [SerializeField] private float defaultSamplingInterval = 1.0f;
        [SerializeField] private bool autoShowOnOrderCompleted = true;

        [Header("Runtime State")]
        [SerializeField] private WorkStudyResults currentResults;

        private int activeTab = 0; // 0 = Summary & Breakdown, 1 = Elemental Event Log
        private Vector2 eventLogScroll = Vector2.zero;
        private string exportStatusMessage = string.Empty;

        public bool IsVisible => isVisible;
        public WorkStudyResults CurrentResults => currentResults;
        public string LastExportStatus => exportStatusMessage;

        private GUIStyle titleStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle metricLabelStyle;
        private GUIStyle metricValueStyle;
        private GUIStyle tableHeaderStyle;
        private GUIStyle tableRowStyle;
        private GUIStyle reconciledStyle;
        private GUIStyle footerStyle;
        private Texture2D windowBgTexture;
        private Texture2D cardBgTexture;
        private Texture2D barBgTexture;
        private Texture2D barFillTexture;
        private bool stylesInitialized = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void OnEnable()
        {
            var om = OrderManager.Instance;
            if (om != null)
            {
                om.OnOrderCompleted -= HandleOrderCompleted;
                om.OnOrderCompleted += HandleOrderCompleted;
            }
        }

        private void OnDisable()
        {
            var om = OrderManager.Instance;
            if (om != null)
            {
                om.OnOrderCompleted -= HandleOrderCompleted;
            }
        }

        private void Update()
        {
            HandleToggleInput();
        }

        private void HandleToggleInput()
        {
            bool tabPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                tabPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                tabPressed = true;
            }
#endif
            if (tabPressed)
            {
                ToggleVisibility();
            }
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
            if (isVisible)
            {
                RefreshResults();
            }
        }

        public void ShowResults()
        {
            RefreshResults();
            isVisible = true;
        }

        public void HideResults()
        {
            isVisible = false;
        }

        public void SetSamplingInterval(float intervalSeconds)
        {
            defaultSamplingInterval = Mathf.Clamp(intervalSeconds, 0.1f, 30.0f);
            RefreshResults();
        }

        public void RefreshResults()
        {
            var logger = WorkEventLogger.Instance;
            currentResults = WorkStudyCalculator.CalculateFromLogger(logger, defaultSamplingInterval);
        }

        public bool ExportCurrentReport(out string markdownPath, out string eventsCsvPath, out string summaryCsvPath)
        {
            RefreshResults();
            var logger = WorkEventLogger.Instance;
            var events = logger != null ? logger.LoggedEvents : Array.Empty<WorkEvent>();
            var om = OrderManager.Instance;
            var demo = DemoFlowController.Instance;

            string orderId = om != null && om.ActiveOrder != null ? om.ActiveOrder.OrderId : "ORD-SESSION";
            string scenarioId = demo != null && demo.SelectedScenario != null ? demo.SelectedScenario.scenarioId : "LIVE-SESSION";
            string modeLabel = demo != null ? demo.ActiveMode.ToString() : "Desktop";

            string reportDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Reports"));
            WorkStudyReportExporter.ExportReportBundle(
                reportDir,
                $"work_study_{orderId}_{scenarioId}",
                currentResults,
                events,
                orderId,
                scenarioId,
                modeLabel,
                out markdownPath,
                out eventsCsvPath,
                out summaryCsvPath);

            exportStatusMessage = $"Exported MD & CSV to Reports/{Path.GetFileName(markdownPath)}";
            SimulationErrorHandler.ReportInfo("WorkStudyReport", exportStatusMessage);
            return true;
        }

        private void HandleOrderCompleted(Order order)
        {
            if (autoShowOnOrderCompleted)
            {
                ShowResults();
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            windowBgTexture = MakeColorTexture(new Color(0.06f, 0.08f, 0.12f, 0.96f));
            cardBgTexture = MakeColorTexture(new Color(0.12f, 0.16f, 0.23f, 0.92f));
            barBgTexture = MakeColorTexture(new Color(0.18f, 0.22f, 0.28f, 0.90f));
            barFillTexture = MakeColorTexture(new Color(0.25f, 0.72f, 0.98f, 0.95f));

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.82f, 1.0f) },
                alignment = TextAnchor.MiddleLeft
            };

            sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.85f, 0.3f) },
                alignment = TextAnchor.MiddleLeft
            };

            metricLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.75f, 0.80f, 0.85f) },
                alignment = TextAnchor.UpperCenter
            };

            metricValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            tableHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.5f, 0.85f, 1.0f) },
                alignment = TextAnchor.MiddleLeft
            };

            tableRowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft
            };

            reconciledStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 1.0f, 0.4f) },
                alignment = TextAnchor.MiddleCenter
            };

            footerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.65f, 0.70f, 0.75f) },
                alignment = TextAnchor.MiddleCenter
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isVisible) return;

            InitStyles();

            float windowWidth = Mathf.Min(760f, Screen.width - 40f);
            float windowHeight = Mathf.Min(540f, Screen.height - 40f);
            float x = (Screen.width - windowWidth) * 0.5f;
            float y = (Screen.height - windowHeight) * 0.5f;

            Rect windowRect = new Rect(x, y, windowWidth, windowHeight);
            GUI.DrawTexture(windowRect, windowBgTexture);

            GUILayout.BeginArea(new Rect(x + 16f, y + 14f, windowWidth - 32f, windowHeight - 28f));

            // 1. Header & Action Bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("INDUSTRIAL WORK-STUDY & WORK-SAMPLING REPORT", titleStyle);
            if (GUILayout.Button("Summary Breakdown", GUILayout.Width(135f), GUILayout.Height(22f))) activeTab = 0;
            if (GUILayout.Button("Event Log Table", GUILayout.Width(115f), GUILayout.Height(22f))) activeTab = 1;
            if (GUILayout.Button("✕ Close [Tab]", GUILayout.Width(95f), GUILayout.Height(22f))) HideResults();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (currentResults == null) RefreshResults();
            var r = currentResults;
            float meanPickTime = r.TotalPicks > 0 ? r.TotalTaskTime / r.TotalPicks : 0f;

            // 2. Overview Metrics Cards (5 columns)
            GUILayout.BeginHorizontal();
            DrawMetricCard("TOTAL TASK TIME", $"{r.TotalTaskTime:F2} s");
            DrawMetricCard("TRAVEL DISTANCE", $"{r.TotalDistance:F2} m");
            DrawMetricCard("SUCCESSFUL PICKS", $"{r.TotalPicks}");
            DrawMetricCard("SHELF BAY VISITS", $"{r.ShelfVisits}");
            DrawMetricCard("MEAN PICK CYCLE", $"{meanPickTime:F2} s");
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            if (activeTab == 0)
            {
                DrawSummaryBreakdownTab(r);
            }
            else
            {
                DrawEventLogTab();
            }

            GUILayout.Space(6);

            // Reconciliation Status Banner
            bool isReconciled = r.VerifyReconciliation(out string reconciliationError);
            if (isReconciled)
            {
                reconciledStyle.normal.textColor = new Color(0.3f, 1.0f, 0.4f);
                GUILayout.Label($"✓ MATHEMATICALLY RECONCILED: 100.0% OF TIME ACCOUNTED FOR (Events: {r.TotalEventsCount} | Interval: {r.SamplingIntervalSeconds:F1}s)", reconciledStyle);
            }
            else
            {
                reconciledStyle.normal.textColor = new Color(1.0f, 0.3f, 0.3f);
                GUILayout.Label($"⚠ RECONCILIATION MISMATCH: {reconciliationError}", reconciledStyle);
            }

            GUILayout.Space(4);

            // Bottom Controls: Sampling Interval, Export Report, Compare Scenarios
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sampling Interval:", tableRowStyle, GUILayout.Width(105f));
            if (GUILayout.Button("0.5s", GUILayout.Width(42f))) SetSamplingInterval(0.5f);
            if (GUILayout.Button("1.0s", GUILayout.Width(42f))) SetSamplingInterval(1.0f);
            if (GUILayout.Button("2.0s", GUILayout.Width(42f))) SetSamplingInterval(2.0f);
            if (GUILayout.Button("5.0s", GUILayout.Width(42f))) SetSamplingInterval(5.0f);
            GUILayout.Space(12f);

            if (GUILayout.Button("Export Report (MD + CSV)", GUILayout.Width(175f), GUILayout.Height(22f)))
            {
                ExportCurrentReport(out _, out _, out _);
            }

            if (GUILayout.Button("Step 7: Compare Scenarios [F3]", GUILayout.Width(195f), GUILayout.Height(22f)))
            {
                HideResults();
                var cmpUI = FindAnyObjectByType<ScenarioComparisonUI>();
                if (cmpUI != null)
                {
                    cmpUI.OpenAndCompareSelected();
                }
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(exportStatusMessage))
            {
                GUILayout.Label(exportStatusMessage, footerStyle);
            }
            else
            {
                GUILayout.Label("Measured simulation telemetry only (no AI/ML prediction or uninstrumented biometrics).", footerStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawSummaryBreakdownTab(WorkStudyResults r)
        {
            GUILayout.Label("Continuous Time-Study & Interval Work-Sampling Breakdown", sectionHeaderStyle);
            GUILayout.Space(2);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Activity Category", tableHeaderStyle, GUILayout.Width(185f));
            GUILayout.Label("Duration", tableHeaderStyle, GUILayout.Width(75f));
            GUILayout.Label("Time %", tableHeaderStyle, GUILayout.Width(65f));
            GUILayout.Label("Visual Share", tableHeaderStyle, GUILayout.Width(150f));
            GUILayout.Label("Sampling Obs", tableHeaderStyle, GUILayout.Width(95f));
            GUILayout.Label("Sample %", tableHeaderStyle, GUILayout.Width(75f));
            GUILayout.EndHorizontal();

            int walkObs = GetSamplingObsCount(ActivityType.MovingBetweenLocations) + GetSamplingObsCount(ActivityType.Approaching) + GetSamplingObsCount(ActivityType.Walking);
            float walkSamplePct = GetSamplingPct(ActivityType.MovingBetweenLocations) + GetSamplingPct(ActivityType.Approaching) + GetSamplingPct(ActivityType.Walking);

            DrawTableRowWithBar("Walking / Travel (Total)", $"{r.WalkingDuration:F2} s", r.WalkingPercentage, $"{walkObs}", $"{walkSamplePct:F1}%");
            DrawTableRowWithBar("  └ Macro Transit", $"{r.MovingBetweenLocationsDuration:F2} s", PercentOf(r.MovingBetweenLocationsDuration, r.TotalTaskTime), $"{GetSamplingObsCount(ActivityType.MovingBetweenLocations)}", $"{GetSamplingPct(ActivityType.MovingBetweenLocations):F1}%");
            DrawTableRowWithBar("  └ Rack Approach (<=3m)", $"{r.ApproachingDuration:F2} s", PercentOf(r.ApproachingDuration, r.TotalTaskTime), $"{GetSamplingObsCount(ActivityType.Approaching)}", $"{GetSamplingPct(ActivityType.Approaching):F1}%");
            DrawTableRowWithBar("Searching Shelf", $"{r.SearchingDuration:F2} s", r.SearchingPercentage, $"{GetSamplingObsCount(ActivityType.Searching)}", $"{GetSamplingPct(ActivityType.Searching):F1}%");
            DrawTableRowWithBar("Item Picking", $"{r.PickingDuration:F2} s", r.PickingPercentage, $"{GetSamplingObsCount(ActivityType.Picking)}", $"{GetSamplingPct(ActivityType.Picking):F1}%");
            DrawTableRowWithBar("Waiting / Idle", $"{r.WaitingDuration:F2} s", r.WaitingPercentage, $"{GetSamplingObsCount(ActivityType.Waiting)}", $"{GetSamplingPct(ActivityType.Waiting):F1}%");
            DrawTableRowWithBar("Order Packing", $"{r.PackingDuration:F2} s", r.PackingPercentage, $"{GetSamplingObsCount(ActivityType.Packing)}", $"{GetSamplingPct(ActivityType.Packing):F1}%");
            DrawTableRowWithBar("Order Completed", $"{r.OrderCompletedDuration:F2} s", r.OrderCompletedPercentage, $"{GetSamplingObsCount(ActivityType.OrderCompleted)}", $"{GetSamplingPct(ActivityType.OrderCompleted):F1}%");
        }

        private void DrawEventLogTab()
        {
            var logger = WorkEventLogger.Instance;
            var events = logger != null ? logger.LoggedEvents : null;

            GUILayout.Label($"Chronological Elemental Event Log ({(events != null ? events.Count : 0)} Events)", sectionHeaderStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("ID", tableHeaderStyle, GUILayout.Width(65f));
            GUILayout.Label("Activity", tableHeaderStyle, GUILayout.Width(155f));
            GUILayout.Label("Start", tableHeaderStyle, GUILayout.Width(55f));
            GUILayout.Label("End", tableHeaderStyle, GUILayout.Width(55f));
            GUILayout.Label("Dur (s)", tableHeaderStyle, GUILayout.Width(55f));
            GUILayout.Label("Dist (m)", tableHeaderStyle, GUILayout.Width(60f));
            GUILayout.Label("SKU", tableHeaderStyle, GUILayout.Width(75f));
            GUILayout.Label("Location", tableHeaderStyle, GUILayout.Width(140f));
            GUILayout.EndHorizontal();

            eventLogScroll = GUILayout.BeginScrollView(eventLogScroll, GUILayout.Height(230f));
            if (events != null)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    var e = events[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(e.Id, tableRowStyle, GUILayout.Width(65f));
                    GUILayout.Label(e.Activity.ToString(), tableRowStyle, GUILayout.Width(155f));
                    GUILayout.Label($"{e.StartTime:F2}", tableRowStyle, GUILayout.Width(55f));
                    GUILayout.Label($"{e.EndTime:F2}", tableRowStyle, GUILayout.Width(55f));
                    GUILayout.Label($"{e.Duration:F2}", tableRowStyle, GUILayout.Width(55f));
                    GUILayout.Label($"{e.Distance:F2}", tableRowStyle, GUILayout.Width(60f));
                    GUILayout.Label(string.IsNullOrEmpty(e.RelatedSku) ? "-" : e.RelatedSku, tableRowStyle, GUILayout.Width(75f));
                    GUILayout.Label(string.IsNullOrEmpty(e.LocationName) ? "-" : e.LocationName, tableRowStyle, GUILayout.Width(140f));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }

        private void DrawMetricCard(string label, string value)
        {
            GUILayout.BeginVertical(GUILayout.Width(138f), GUILayout.Height(46f));
            var cardRect = GUILayoutUtility.GetRect(138f, 46f);
            GUI.DrawTexture(cardRect, cardBgTexture);

            Rect labelRect = new Rect(cardRect.x, cardRect.y + 4f, cardRect.width, 16f);
            Rect valueRect = new Rect(cardRect.x, cardRect.y + 20f, cardRect.width, 22f);

            GUI.Label(labelRect, label, metricLabelStyle);
            GUI.Label(valueRect, value, metricValueStyle);
            GUILayout.EndVertical();
        }

        private void DrawTableRowWithBar(string category, string duration, float timePct, string sampleObs, string samplePct)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(category, tableRowStyle, GUILayout.Width(185f));
            GUILayout.Label(duration, tableRowStyle, GUILayout.Width(75f));
            GUILayout.Label($"{timePct:F1}%", tableRowStyle, GUILayout.Width(65f));

            Rect barRect = GUILayoutUtility.GetRect(140f, 14f, GUILayout.Width(150f));
            Rect innerBg = new Rect(barRect.x, barRect.y + 3f, 135f, 10f);
            GUI.DrawTexture(innerBg, barBgTexture);
            float fillW = Mathf.Clamp01(timePct / 100f) * 135f;
            if (fillW > 0.5f)
            {
                GUI.DrawTexture(new Rect(innerBg.x, innerBg.y, fillW, innerBg.height), barFillTexture);
            }

            GUILayout.Label(sampleObs, tableRowStyle, GUILayout.Width(95f));
            GUILayout.Label(samplePct, tableRowStyle, GUILayout.Width(75f));
            GUILayout.EndHorizontal();
        }

        private int GetSamplingObsCount(ActivityType activity)
        {
            if (currentResults == null) return 0;
            foreach (var s in currentResults.SampleCounts)
            {
                if (s.Activity == activity) return s.Count;
            }
            return 0;
        }

        private float GetSamplingPct(ActivityType activity)
        {
            if (currentResults == null) return 0f;
            foreach (var s in currentResults.SampleCounts)
            {
                if (s.Activity == activity) return s.Percentage;
            }
            return 0f;
        }

        private float PercentOf(float value, float total)
        {
            if (total <= 0.0001f) return 0f;
            return (value / total) * 100f;
        }

        private Texture2D MakeColorTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void OnDestroy()
        {
            if (windowBgTexture != null) Destroy(windowBgTexture);
            if (cardBgTexture != null) Destroy(cardBgTexture);
            if (barBgTexture != null) Destroy(barBgTexture);
            if (barFillTexture != null) Destroy(barFillTexture);
        }
    }
}
