using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VirtualDarkStore.Management;
using VirtualDarkStore.Scenarios;

namespace VirtualDarkStore.UI
{
    /// <summary>
    /// Polished IMGUI overlay (toggleable with F3 or from the Demo/Results UI) for Step 7:
    /// comparing two scenarios on the same order under equivalent starting conditions,
    /// rendering 2D top-down and 3D in-world route maps, and inspecting KPI deltas.
    /// </summary>
    public class ScenarioComparisonUI : MonoBehaviour
    {
        [SerializeField] private string runAPath = "docs/evidence/phase6/run_A.json";
        [SerializeField] private string runBPath = "docs/evidence/phase6/run_B.json";

        private bool visible;
        private ScenarioComparison comparison;
        private Texture2D map;
        private string status = "Select a scenario pair to run & compare, or load saved runs.";
        private Vector2 scroll;
        private Texture2D windowBgTexture;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle rowStyle;
        private bool stylesInit;

        public bool IsVisible => visible;
        public ScenarioComparison CurrentComparison => comparison;

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) visible = !visible;
#else
            if (Input.GetKeyDown(KeyCode.F3)) visible = !visible;
#endif
        }

        private static string Resolve(string p) => Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(Application.dataPath, "..", p));

        public void OpenAndCompareSelected()
        {
            visible = true;
            var demo = DemoFlowController.Instance;
            if (demo != null)
            {
                int altIdx = demo.ComparisonScenarioIndex;
                var cmp = demo.CompareWithScenario(altIdx, out string err);
                if (cmp != null)
                {
                    SetComparison(cmp, $"Compared '{cmp.RunA.scenario.scenarioId}' vs '{cmp.RunB.scenario.scenarioId}'.");
                    return;
                }
                else if (!string.IsNullOrEmpty(err))
                {
                    status = "Compare error: " + err;
                }
            }
            LoadSavedRuns();
        }

        public void SetComparison(ScenarioComparison cmp, string message = "Loaded comparison.")
        {
            comparison = cmp;
            status = message;
            if (map != null) Destroy(map);
            map = null;

            if (comparison != null)
            {
                var store = StoreManager.Instance;
                var slots = store != null ? store.GetAllSlots().Select(s => s.WorldPosition).ToList() : null;
                map = RouteMapRenderer.Render(
                    new[] { comparison.RunA, comparison.RunB },
                    new[] { RouteMapRenderer.ColorA, RouteMapRenderer.ColorB },
                    slots,
                    380,
                    380);
            }
        }

        public void LoadSavedRuns()
        {
            try
            {
                var a = ScenarioStorage.LoadRun(Resolve(runAPath));
                var b = ScenarioStorage.LoadRun(Resolve(runBPath));
                SetComparison(new ScenarioComparison(a, b), "Loaded recorded runs from disk.");
            }
            catch (Exception e)
            {
                comparison = null;
                status = "Load failed: " + e.Message;
                SimulationErrorHandler.ReportWarning("ScenarioComparisonUI", status);
            }
        }

        private void InitStyles()
        {
            if (stylesInit) return;
            windowBgTexture = new Texture2D(1, 1);
            windowBgTexture.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.12f, 0.96f));
            windowBgTexture.Apply();

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.82f, 1.0f) }
            };
            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.85f, 0.3f) }
            };
            rowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };
            stylesInit = true;
        }

        private void OnGUI()
        {
            if (!visible) return;
            InitStyles();

            float w = Mathf.Min(960f, Screen.width - 40f);
            float h = Mathf.Min(600f, Screen.height - 40f);
            var rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.DrawTexture(rect, windowBgTexture);

            GUILayout.BeginArea(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, rect.height - 24f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("SCENARIO A/B WORK-STUDY COMPARISON (STEP 7)", titleStyle);
            if (GUILayout.Button("✕ Close [F3]", GUILayout.Width(100f), GUILayout.Height(22f))) visible = false;
            GUILayout.EndHorizontal();

            GUILayout.Label(ScenarioComparison.Disclaimer, rowStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Compare SCN-A vs SCN-B", GUILayout.Width(175f)))
            {
                var demo = DemoFlowController.Instance;
                if (demo != null)
                {
                    demo.SelectScenario(0);
                    var cmp = demo.CompareWithScenario(1, out string err);
                    if (cmp != null) SetComparison(cmp, "Compared SCN-A vs SCN-B (4-Item Multi-Aisle Order).");
                    else status = err;
                }
            }
            if (GUILayout.Button("Compare Demo A vs Demo B", GUILayout.Width(185f)))
            {
                var demo = DemoFlowController.Instance;
                if (demo != null)
                {
                    demo.SelectScenario(2);
                    var cmp = demo.CompareWithScenario(3, out string err);
                    if (cmp != null) SetComparison(cmp, "Compared SCN-DEMO-A vs SCN-DEMO-B (3-Item Express Order).");
                    else status = err;
                }
            }
            if (GUILayout.Button("Load Saved Disk Runs", GUILayout.Width(155f))) LoadSavedRuns();
            if (GUILayout.Button("Clear 3D Floor Routes", GUILayout.Width(150f)))
            {
                var vis = FindAnyObjectByType<ScenarioRouteVisualizer>();
                if (vis != null) vis.Clear();
                status = "Cleared 3D floor route lines.";
            }
            GUILayout.Label(status, rowStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (comparison != null)
            {
                scroll = GUILayout.BeginScrollView(scroll);
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(520f));
                GUILayout.Label($"Run A: {comparison.RunA.scenario.scenarioName} ({string.Join(" -> ", comparison.RunA.scenario.skuSequence)})", sectionStyle);
                GUILayout.Label($"Run B: {comparison.RunB.scenario.scenarioName} ({string.Join(" -> ", comparison.RunB.scenario.skuSequence)})", sectionStyle);
                GUILayout.Label($"Equivalent starting conditions: {(comparison.IsEquivalent ? "YES (Valid Controlled Comparison)" : "NO – " + string.Join("; ", comparison.EquivalenceIssues))}", rowStyle);
                GUILayout.Label($"Varied factor(s): {(comparison.VariedFactors.Count == 0 ? "none" : string.Join(", ", comparison.VariedFactors))}", rowStyle);
                GUILayout.Label($"Mathematical Reconciliation (Both Runs): {(comparison.BothReconciled ? "PASS (100.0%)" : "FAIL")}", rowStyle);
                GUILayout.Space(4);
                DrawRows("KPI Comparison", comparison.Kpis);
                GUILayout.Space(4);
                DrawRows("Activity Time Share (%)", comparison.ActivityDistribution);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.Label("Top-Down Route Map (Blue = A, Orange = B)", sectionStyle);
                if (map != null) GUILayout.Label(map, GUILayout.Width(380f), GUILayout.Height(380f));
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
            }

            GUILayout.EndArea();
        }

        private void DrawRows(string title, IReadOnlyList<KpiRow> rows)
        {
            GUILayout.Label($"— {title} (A | B | Delta B−A) —", sectionStyle);
            foreach (var r in rows)
            {
                string pct = r.DeltaPercent.HasValue ? $"({r.DeltaPercent.Value:+0.0;-0.0;0.0}%)" : "";
                GUILayout.Label($"{r.Name,-26} {r.A,7:F2} | {r.B,7:F2} | {r.Delta,7:+0.00;-0.00;0.00} {r.Unit} {pct}", rowStyle);
            }
        }

        private void OnDestroy()
        {
            if (windowBgTexture != null) Destroy(windowBgTexture);
            if (map != null) Destroy(map);
        }
    }
}
