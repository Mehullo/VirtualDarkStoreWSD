using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VirtualDarkStore.Domain;
using VirtualDarkStore.Management;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.UI
{
    /// <summary>
    /// Screen-space IMGUI results interface for displaying comprehensive industrial work-study
    /// and work-sampling metrics.
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

        public bool IsVisible => isVisible;
        public WorkStudyResults CurrentResults => currentResults;

        private GUIStyle windowStyle;
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

        public void RefreshResults()
        {
            var logger = WorkEventLogger.Instance;
            currentResults = WorkStudyCalculator.CalculateFromLogger(logger, defaultSamplingInterval);
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
            cardBgTexture = MakeColorTexture(new Color(0.12f, 0.15f, 0.22f, 0.90f));

            windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(16, 16, 16, 16)
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.8f, 1.0f) },
                alignment = TextAnchor.MiddleCenter
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
                fontSize = 11,
                normal = { textColor = new Color(0.75f, 0.80f, 0.85f) },
                alignment = TextAnchor.UpperCenter
            };

            metricValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
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
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 1.0f, 0.4f) },
                alignment = TextAnchor.MiddleCenter
            };

            footerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.6f, 0.65f, 0.7f) },
                alignment = TextAnchor.MiddleCenter
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isVisible) return;

            InitStyles();

            float windowWidth = 620f;
            float windowHeight = 480f;
            float x = (Screen.width - windowWidth) * 0.5f;
            float y = (Screen.height - windowHeight) * 0.5f;

            Rect windowRect = new Rect(x, y, windowWidth, windowHeight);
            GUI.DrawTexture(windowRect, windowBgTexture);

            GUILayout.BeginArea(new Rect(x + 14f, y + 14f, windowWidth - 28f, windowHeight - 28f));

            // 1. Header & Title
            GUILayout.BeginHorizontal();
            GUILayout.Label("INDUSTRIAL WORK-STUDY & SAMPLING RESULTS", titleStyle);
            if (GUILayout.Button("✕ Close [Tab]", GUILayout.Width(90f), GUILayout.Height(22f)))
            {
                HideResults();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            if (currentResults == null)
            {
                RefreshResults();
            }

            var r = currentResults;

            // 2. Overview Metrics Cards (4 columns)
            GUILayout.BeginHorizontal();
            DrawMetricCard("TOTAL TASK TIME", $"{r.TotalTaskTime:F1} s");
            DrawMetricCard("TOTAL DISTANCE", $"{r.TotalDistance:F1} m");
            DrawMetricCard("SUCCESSFUL PICKS", $"{r.TotalPicks}");
            DrawMetricCard("SHELF VISITS", $"{r.ShelfVisits}");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 3. Activity Category Breakdown Table
            GUILayout.Label("Continuous Time-Study & Interval Work-Sampling Breakdown", sectionHeaderStyle);
            GUILayout.Space(2);

            // Table Header
            GUILayout.BeginHorizontal();
            GUILayout.Label("Activity Category", tableHeaderStyle, GUILayout.Width(170f));
            GUILayout.Label("Duration", tableHeaderStyle, GUILayout.Width(75f));
            GUILayout.Label("Time %", tableHeaderStyle, GUILayout.Width(65f));
            GUILayout.Label("Sampling Obs", tableHeaderStyle, GUILayout.Width(90f));
            GUILayout.Label("Sample %", tableHeaderStyle, GUILayout.Width(75f));
            GUILayout.EndHorizontal();

            DrawTableRow("Walking / Travel", $"{r.WalkingDuration:F2} s", $"{r.WalkingPercentage:F1}%", GetSamplingObs(ActivityType.MovingBetweenLocations) + GetSamplingObs(ActivityType.Approaching) + GetSamplingObs(ActivityType.Walking), $"{GetSamplingPct(ActivityType.MovingBetweenLocations) + GetSamplingPct(ActivityType.Approaching) + GetSamplingPct(ActivityType.Walking):F1}%");
            DrawTableRow("  └ Macro Transit", $"{r.MovingBetweenLocationsDuration:F2} s", $"{PercentOf(r.MovingBetweenLocationsDuration, r.TotalTaskTime):F1}%", GetSamplingObs(ActivityType.MovingBetweenLocations), $"{GetSamplingPct(ActivityType.MovingBetweenLocations):F1}%");
            DrawTableRow("  └ Rack Approach", $"{r.ApproachingDuration:F2} s", $"{PercentOf(r.ApproachingDuration, r.TotalTaskTime):F1}%", GetSamplingObs(ActivityType.Approaching), $"{GetSamplingPct(ActivityType.Approaching):F1}%");
            DrawTableRow("Searching Shelf", $"{r.SearchingDuration:F2} s", $"{r.SearchingPercentage:F1}%", GetSamplingObs(ActivityType.Searching), $"{GetSamplingPct(ActivityType.Searching):F1}%");
            DrawTableRow("Item Picking", $"{r.PickingDuration:F2} s", $"{r.PickingPercentage:F1}%", GetSamplingObs(ActivityType.Picking), $"{GetSamplingPct(ActivityType.Picking):F1}%");
            DrawTableRow("Waiting / Idle", $"{r.WaitingDuration:F2} s", $"{r.WaitingPercentage:F1}%", GetSamplingObs(ActivityType.Waiting), $"{GetSamplingPct(ActivityType.Waiting):F1}%");
            DrawTableRow("Order Packing", $"{r.PackingDuration:F2} s", $"{r.PackingPercentage:F1}%", GetSamplingObs(ActivityType.Packing), $"{GetSamplingPct(ActivityType.Packing):F1}%");
            DrawTableRow("Order Completed", $"{r.OrderCompletedDuration:F2} s", $"{r.OrderCompletedPercentage:F1}%", GetSamplingObs(ActivityType.OrderCompleted), $"{GetSamplingPct(ActivityType.OrderCompleted):F1}%");

            GUILayout.Space(6);

            // 4. Reconciliation Status Banner
            bool isReconciled = r.VerifyReconciliation(out string reconciliationError);
            if (isReconciled)
            {
                reconciledStyle.normal.textColor = new Color(0.3f, 1.0f, 0.4f);
                GUILayout.Label($"✓ MATHEMATICALLY RECONCILED: 100.0% OF TIME ACCOUNTED FOR (Events: {r.TotalEventsCount})", reconciledStyle);
            }
            else
            {
                reconciledStyle.normal.textColor = new Color(1.0f, 0.3f, 0.3f);
                GUILayout.Label($"⚠ RECONCILIATION MISMATCH: {reconciliationError}", reconciledStyle);
            }

            GUILayout.Space(4);
            GUILayout.Label($"Work sampling evaluated at {r.SamplingIntervalSeconds:F1}s intervals ({r.TotalWorkSamplesCount} total observations). Press [Tab] to toggle overlay.", footerStyle);

            GUILayout.EndArea();
        }

        private void DrawMetricCard(string label, string value)
        {
            GUILayout.BeginVertical(GUILayout.Width(136f), GUILayout.Height(46f));
            var cardRect = GUILayoutUtility.GetRect(136f, 46f);
            GUI.DrawTexture(cardRect, cardBgTexture);

            Rect labelRect = new Rect(cardRect.x, cardRect.y + 4f, cardRect.width, 16f);
            Rect valueRect = new Rect(cardRect.x, cardRect.y + 20f, cardRect.width, 22f);

            GUI.Label(labelRect, label, metricLabelStyle);
            GUI.Label(valueRect, value, metricValueStyle);
            GUILayout.EndVertical();
        }

        private void DrawTableRow(string category, string duration, string timePct, string sampleObs, string samplePct)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(category, tableRowStyle, GUILayout.Width(170f));
            GUILayout.Label(duration, tableRowStyle, GUILayout.Width(75f));
            GUILayout.Label(timePct, tableRowStyle, GUILayout.Width(65f));
            GUILayout.Label(sampleObs, tableRowStyle, GUILayout.Width(90f));
            GUILayout.Label(samplePct, tableRowStyle, GUILayout.Width(75f));
            GUILayout.EndHorizontal();
        }

        private string GetSamplingObs(ActivityType activity)
        {
            if (currentResults == null) return "0";
            foreach (var s in currentResults.SampleCounts)
            {
                if (s.Activity == activity) return $"{s.Count}";
            }
            return "0";
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
        }
    }
}
