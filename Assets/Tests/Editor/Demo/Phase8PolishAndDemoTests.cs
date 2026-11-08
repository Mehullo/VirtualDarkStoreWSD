using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Interaction;
using VirtualDarkStore.Management;
using VirtualDarkStore.Player;
using VirtualDarkStore.Scenarios;
using VirtualDarkStore.UI;
using VirtualDarkStore.VR;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Tests.Demo
{
    [TestFixture]
    public class Phase8PolishAndDemoTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static readonly string ScenarioDir = Path.Combine(ProjectRoot, "Scenarios");
        private static readonly string EvidenceDir = Path.Combine(ProjectRoot, "docs", "evidence", "phase8");

        private GameObject testRoot;
        private StoreManager store;
        private OrderManager orders;
        private WorkEventLogger logger;
        private ActivityClassifier classifier;
        private SimulationErrorHandler errorHandler;
        private PerformanceSettingsManager perfManager;
        private DemoFlowController demoFlow;
        private WorkStudyResultsUI resultsUI;
        private ScenarioComparisonUI comparisonUI;
        private DesktopPickerController desktopPicker;
        private VRPlayerRig vrRig;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("[Phase8TestRoot]");

            store = Make<StoreManager>("StoreManager");
            store.InitializeStore();

            orders = Make<OrderManager>("OrderManager");
            logger = Make<WorkEventLogger>("WorkEventLogger");
            classifier = Make<ActivityClassifier>("ActivityClassifier");
            errorHandler = Make<SimulationErrorHandler>("SimulationErrorHandler");
            perfManager = Make<PerformanceSettingsManager>("PerformanceSettingsManager");
            resultsUI = Make<WorkStudyResultsUI>("WorkStudyResultsUI");
            comparisonUI = Make<ScenarioComparisonUI>("ScenarioComparisonUI");

            var station = new GameObject("PackingStation_01");
            station.transform.SetParent(testRoot.transform);
            station.transform.position = new Vector3(0f, 0f, -9.5f);
            station.AddComponent<PackingStation>();

            // Desktop rig
            var dpObj = new GameObject("DesktopPicker");
            dpObj.transform.SetParent(testRoot.transform);
            dpObj.transform.position = new Vector3(0f, 0f, -8.5f);
            dpObj.AddComponent<CharacterController>();
            var dpCamObj = new GameObject("MainCamera");
            dpCamObj.transform.SetParent(dpObj.transform);
            var dpCam = dpCamObj.AddComponent<Camera>();
            dpCam.farClipPlane = 200f;
            desktopPicker = dpObj.AddComponent<DesktopPickerController>();
            dpObj.AddComponent<PickerInteraction>();

            // VR rig
            vrRig = VirtualDarkStore.Editor.DarkStoreSceneBuilder.BuildVRPlayerRig(testRoot, false).GetComponent<VRPlayerRig>();

            classifier.SetReferences(logger, orders, desktopPicker.transform, desktopPicker.GetComponent<PickerInteraction>());
            classifier.ResetState(0f, desktopPicker.transform.position);

            demoFlow = Make<DemoFlowController>("DemoFlowController");
            demoFlow.InitializeCatalogAndPresets();
            demoFlow.ResolveSceneReferences();
        }

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(testRoot.transform);
            return go.AddComponent<T>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        [Test]
        public void StoreManager_Distributes72Skus_AcrossAll4Aisles()
        {
            Assert.AreEqual(4, store.Aisles.Count);
            Assert.AreEqual(72, store.RegisteredSkuCount);
            Assert.AreEqual(72, store.OccupiedSlotCount);

            foreach (var aisle in store.Aisles)
            {
                int occupiedInAisle = aisle.GetAllSlots().Count(s => s.IsOccupied);
                Assert.AreEqual(18, occupiedInAisle, $"Expected 18 occupied slots in {aisle.AisleId} (9 Left, 9 Right).");
            }
        }

        [Test]
        public void ErrorHandler_And_OrderManager_HandleInvalidInputsGracefully_AndResetVisuals()
        {
            errorHandler.ClearHistory();

            // 1. Reject empty order ID without unhandled exception
            bool ok1 = orders.TryStartSpecificOrder("", new[] { "SKU-0001" }, out var ord1, out string err1);
            Assert.IsFalse(ok1);
            Assert.IsNull(ord1);
            Assert.IsNotEmpty(err1);
            Assert.GreaterOrEqual(errorHandler.ErrorCount, 1);

            // 2. Reject unknown SKU ID without unhandled exception
            bool ok2 = orders.TryStartSpecificOrder("ORD-ERR-02", new[] { "SKU-NONEXISTENT-999" }, out var ord2, out string err2);
            Assert.IsFalse(ok2);
            Assert.IsNull(ord2);
            StringAssert.Contains("does not exist", err2);

            // 3. Verify PickableItemVisual is reset automatically when a new order is dispatched
            var prodObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prodObj.transform.SetParent(testRoot.transform);
            var visual = prodObj.AddComponent<PickableItemVisual>();
            visual.Initialize("SKU-0001", "Cold Brew Coffee 330ml", "A01-L-B01-T01-S01");
            visual.OnPicked();
            Assert.IsTrue(visual.IsPicked);

            bool ok3 = orders.TryStartSpecificOrder("ORD-VALID-01", new[] { "SKU-0001" }, out var ord3, out string err3);
            Assert.IsTrue(ok3, err3);
            Assert.IsNotNull(ord3);
            Assert.IsFalse(visual.IsPicked, "Starting a new order must restock PickableItemVisual back to unpicked state.");
        }

        [Test]
        public void WorkStudyReportExporter_ExportsValidMarkdownAndCsvBundle()
        {
            var events = new List<WorkEvent>
            {
                new WorkEvent("EVT-0001", ActivityType.Waiting, 0f, 1f, new Vector3(0f, 0f, -8.5f), "PACK-01", "", "ORD-RPT-01", 0f),
                new WorkEvent("EVT-0002", ActivityType.MovingBetweenLocations, 1f, 6f, new Vector3(-5.2f, 0f, 0f), "A01-L-B01-T01-S01", "SKU-0001", "ORD-RPT-01", 9.0f),
                new WorkEvent("EVT-0003", ActivityType.Searching, 6f, 8f, new Vector3(-5.2f, 0f, 0f), "A01-L-B01-T01-S01", "SKU-0001", "ORD-RPT-01", 0f),
                new WorkEvent("EVT-0004", ActivityType.Picking, 8f, 8.5f, new Vector3(-5.2f, 0f, 0f), "A01-L-B01-T01-S01", "SKU-0001", "ORD-RPT-01", 0f),
                new WorkEvent("EVT-0005", ActivityType.MovingBetweenLocations, 8.5f, 13.5f, new Vector3(0f, 0f, -8.5f), "PACK-01", "", "ORD-RPT-01", 9.0f),
                new WorkEvent("EVT-0006", ActivityType.Packing, 13.5f, 15.5f, new Vector3(0f, 0f, -8.5f), "PACK-01", "", "ORD-RPT-01", 0f),
                new WorkEvent("EVT-0007", ActivityType.OrderCompleted, 15.5f, 16.3f, new Vector3(0f, 0f, -8.5f), "PACK-01", "", "ORD-RPT-01", 0f)
            };

            var results = WorkStudyCalculator.Calculate(events, 1.0f);
            Assert.IsTrue(results.VerifyReconciliation(out string reconErr), reconErr);

            Directory.CreateDirectory(EvidenceDir);
            WorkStudyReportExporter.ExportReportBundle(
                EvidenceDir,
                "unit_test_report",
                results,
                events,
                "ORD-RPT-01",
                "SCN-TEST",
                "Desktop",
                out string mdPath,
                out string eventsCsvPath,
                out string summaryCsvPath);

            Assert.IsTrue(File.Exists(mdPath));
            Assert.IsTrue(File.Exists(eventsCsvPath));
            Assert.IsTrue(File.Exists(summaryCsvPath));

            string mdText = File.ReadAllText(mdPath);
            StringAssert.Contains("MEASURED SIMULATION TELEMETRY", mdText);
            StringAssert.Contains("PASS (100.0% Reconciled)", mdText);
            StringAssert.Contains("EVT-0004", mdText);

            string csvText = File.ReadAllText(eventsCsvPath);
            StringAssert.Contains("EventId,Activity,StartTimeSeconds", csvText);
            StringAssert.Contains("EVT-0004,Picking,8.000,8.500,0.500", csvText);
        }

        [Test]
        public void PerformanceAudit_EnforcesRtx2050Budget_WithoutFabricatedBenchmarks()
        {
            if (testRoot != null) UnityEngine.Object.DestroyImmediate(testRoot);

            try
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/DarkStore_Phase1.unity");
                var scenePerf = UnityEngine.Object.FindAnyObjectByType<PerformanceSettingsManager>();
                Assert.IsNotNull(scenePerf, "Expected PerformanceSettingsManager in DarkStore_Phase1.unity");

                var report = scenePerf.AuditScenePerformance();
                Assert.IsNotNull(report);
                Assert.IsTrue(report.SrpBatcherEnabled, "SRP Batcher must be enabled.");
                Assert.GreaterOrEqual(report.TotalRenderersCount, 100, "Expected full dark store scene renderers (72 products + racks + envelope).");
                Assert.LessOrEqual(report.TotalRenderersCount, 350, "Renderer count must stay within budget (<= 350 for 6-aisle 144-SKU store).");
                Assert.GreaterOrEqual(report.UniqueSharedMaterialsCount, 8, "Expected shared category and structural materials.");
                Assert.AreEqual(report.UniqueSharedMaterialsCount, report.InstancedMaterialsCount, "100% of shared materials must have GPU Instancing enabled.");
                Assert.LessOrEqual(report.MaxCameraFarClipPlane, 50f, "Camera far clip plane must be clamped to <= 50m.");
                Assert.LessOrEqual(report.ShadowCastingLightsCount, 1, "At most 1 directional light should cast shadows.");
                Assert.IsTrue(report.IsWithinRtx2050Budget, "Full DarkStore_Phase1.unity scene must pass RTX 2050 budget checks.");
                StringAssert.Contains("NOT RUN", report.BenchmarkStatus, "Benchmark status must be honestly marked NOT RUN without fabricated FPS.");

                Directory.CreateDirectory(EvidenceDir);
                File.WriteAllText(Path.Combine(EvidenceDir, "rtx2050_performance_audit.md"), report.ToMarkdown());
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
            }
        }

        [Test]
        public void FinalDemo_7StepWorkflow_EndToEnd_DesktopAndVR_AndScenarioComparison()
        {
            Directory.CreateDirectory(EvidenceDir);
            Directory.CreateDirectory(ScenarioDir);

            // Save Demo Scenario JSON files to Scenarios/
            ScenarioStorage.Save(demoFlow.AvailableScenarios[2], Path.Combine(ScenarioDir, "scenario_demo_A.json"));
            ScenarioStorage.Save(demoFlow.AvailableScenarios[3], Path.Combine(ScenarioDir, "scenario_demo_B.json"));

            // STEP 1: Select Scenario (Demo Scenario A: 3-Item Sequential Sweep across Aisles 1, 2, 3)
            var selectedScenario = demoFlow.SelectScenario(2);
            Assert.AreEqual("SCN-DEMO-A", selectedScenario.scenarioId);
            Assert.AreEqual(DemoStep.Step2_SelectOrder, demoFlow.CurrentStep);

            // STEP 2: Select Order (Scenario Defined Order: ORD-DEMO-0001 -> SKU-0001, SKU-0021, SKU-0039)
            var selectedOrderPreset = demoFlow.SelectOrderPreset(0);
            Assert.AreEqual("PRESET-SCENARIO", selectedOrderPreset.presetId);
            Assert.AreEqual(DemoStep.Step3_StartDesktopOrVR, demoFlow.CurrentStep);

            // STEP 3: Start Desktop or VR (First verify VR mode activation, then run Desktop session)
            Assert.IsTrue(demoFlow.StartSession(DemoExecutionMode.VR, out string vrErr), vrErr);
            Assert.IsTrue(vrRig.gameObject.activeSelf);
            Assert.IsFalse(desktopPicker.gameObject.activeSelf);

            Assert.IsTrue(demoFlow.StartSession(DemoExecutionMode.Desktop, out string deskErr), deskErr);
            Assert.IsTrue(desktopPicker.gameObject.activeSelf);
            Assert.IsFalse(vrRig.gameObject.activeSelf);
            Assert.AreEqual(DemoStep.Step4_PickProducts, demoFlow.CurrentStep);
            Assert.IsTrue(orders.HasActiveOrder);
            Assert.AreEqual("ORD-DEMO-0001", orders.ActiveOrder.OrderId);

            // STEP 4: Pick Products (3 items across Aisles 1, 2, and 3)
            float t = 1.0f;
            Vector3 pos = desktopPicker.transform.position;
            classifier.Step(t, pos, 0f, false, false, 0f); // initial wait

            foreach (var item in orders.ActiveOrder.Items.ToList())
            {
                Vector3 targetStand = item.TargetWorldPosition + new Vector3(0.75f, 0f, 0f);
                targetStand.y = 0f;
                float dist = Vector3.Distance(pos, targetStand);

                // Walk to aisle/bay
                t += dist / 1.4f;
                pos = targetStand;
                desktopPicker.transform.position = pos;
                classifier.Step(t, pos, 1.4f, false, false, dist * 0.8f);

                // Approach shelf bay
                t += 1.0f;
                classifier.Step(t, pos, 1.0f, false, false, dist * 0.2f);

                // Search shelf
                t += 2.0f;
                classifier.Step(t, pos, 0f, true, false, 0f);

                // Pick item
                var pickRes = orders.AttemptPick(item.SkuId);
                Assert.AreEqual(PickResult.Success, pickRes);
                t = Mathf.Max(t, classifier.CurrentActivityStartTime);
            }

            Assert.AreEqual(DemoStep.Step5_CompleteOrder, demoFlow.CurrentStep);
            Assert.IsTrue(orders.IsAwaitingPacking);

            // STEP 5: Complete Order at Packing Station
            Vector3 packPos = new Vector3(0f, 0f, -8.5f);
            float returnDist = Vector3.Distance(pos, packPos);
            t += returnDist / 1.4f;
            pos = packPos;
            desktopPicker.transform.position = pos;
            classifier.Step(t, pos, 1.4f, false, false, returnDist);

            t += 2.0f;
            classifier.Step(t, pos, 0f, false, true, 0f);

            bool packed = orders.CompletePacking(out string packErr);
            Assert.IsTrue(packed, packErr);
            Assert.AreEqual(OrderStatus.Completed, orders.ActiveOrder.Status);
            Assert.AreEqual(DemoStep.Step6_ViewWorkStudyResults, demoFlow.CurrentStep);

            // STEP 6: View Work-Study Results & Export Report Bundle
            var studyResults = demoFlow.ViewWorkStudyResults(1.0f);
            Assert.IsNotNull(studyResults);
            Assert.AreEqual(3, studyResults.TotalPicks);
            Assert.AreEqual(3, studyResults.ShelfVisits);
            Assert.IsTrue(studyResults.VerifyReconciliation(out string reconReason), reconReason);
            Assert.IsTrue(resultsUI.IsVisible);

            WorkStudyReportExporter.ExportReportBundle(
                EvidenceDir,
                "demo_order_work_study_report",
                studyResults,
                logger.LoggedEvents,
                orders.ActiveOrder.OrderId,
                selectedScenario.scenarioId,
                "Desktop",
                out string reportMdPath,
                out string reportEventsCsvPath,
                out string reportSummaryCsvPath);
            Assert.IsTrue(File.Exists(reportMdPath));

            // STEP 7: Compare Another Scenario (Compare SCN-DEMO-A vs SCN-DEMO-B)
            var cmp = demoFlow.CompareWithScenario(3, out string cmpErr);
            Assert.IsNotNull(cmp, cmpErr);
            Assert.AreEqual(DemoStep.Step7_CompareAnotherScenario, demoFlow.CurrentStep);
            Assert.IsTrue(cmp.IsEquivalent, string.Join("; ", cmp.EquivalenceIssues));
            Assert.IsTrue(cmp.BothReconciled);
            Assert.Greater(Mathf.Abs(cmp.ResultsA.TotalDistance - cmp.ResultsB.TotalDistance), 1.0f,
                "Expected measurable travel distance difference between sequential and out-of-order aisle routes.");

            File.WriteAllText(Path.Combine(EvidenceDir, "demo_comparison_A_vs_B.md"), cmp.ToMarkdown());
        }
    }
}
