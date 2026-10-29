using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Management;
using VirtualDarkStore.Scenarios;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Tests.Scenarios
{
    [TestFixture]
    public class ScenarioTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static readonly string ScenarioDir = Path.Combine(ProjectRoot, "Scenarios");
        private static readonly string EvidenceDir = Path.Combine(ProjectRoot, "docs", "evidence", "phase6");

        private GameObject root;
        private StoreManager store;
        private OrderManager orders;
        private WorkEventLogger logger;
        private ActivityClassifier classifier;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("[ScenarioTestRoot]");
            store = Make<StoreManager>("StoreManager");
            store.InitializeStore();
            orders = Make<OrderManager>("OrderManager");
            logger = Make<WorkEventLogger>("WorkEventLogger");
            classifier = Make<ActivityClassifier>("ActivityClassifier");
            classifier.SetReferences(logger, orders);
            var station = new GameObject("PackingStation_01");
            station.transform.SetParent(root.transform);
            station.transform.position = new Vector3(0f, 0f, -9.5f);
            station.AddComponent<PackingStation>();
        }

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform);
            return go.AddComponent<T>();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        // Same order (same 4 SKUs, same start/speed/search/pack parameters); only the visit sequence differs.
        private static ScenarioDefinition MakeScenario(string id, string name, string description, params string[] skus)
        {
            return new ScenarioDefinition
            {
                scenarioId = id,
                scenarioName = name,
                description = description,
                orderId = "ORD-SCN-0001",
                skuSequence = new List<string>(skus)
            };
        }

        private static ScenarioDefinition ScenarioA() => MakeScenario("SCN-A", "Scenario A: catalogue-number sequence",
            "Order lines visited in ascending SKU-number order (user-defined; not optimised).",
            "SKU-0003", "SKU-0030", "SKU-0045", "SKU-0066");

        private static ScenarioDefinition ScenarioB() => MakeScenario("SCN-B", "Scenario B: alternate sequence",
            "Same order lines visited in a different user-defined sequence (not optimised).",
            "SKU-0003", "SKU-0045", "SKU-0030", "SKU-0066");

        [Test]
        public void Definition_Validation_RejectsBadInput()
        {
            var s = ScenarioA();
            Assert.IsTrue(s.Validate(out _));

            var dup = ScenarioA(); dup.skuSequence.Add("SKU-0003");
            Assert.IsFalse(dup.Validate(out string e1)); StringAssert.Contains("duplicate", e1);

            var empty = ScenarioA(); empty.skuSequence.Clear();
            Assert.IsFalse(empty.Validate(out _));

            var slow = ScenarioA(); slow.walkSpeed = 0.1f;
            Assert.IsFalse(slow.Validate(out _));

            var zeroWait = ScenarioA(); zeroWait.initialWaitSeconds = 0f;
            Assert.IsFalse(zeroWait.Validate(out _));
        }

        [Test]
        public void SaveLoad_RoundTripsScenarioExactly()
        {
            var a = ScenarioA();
            string path = Path.Combine(ScenarioDir, "scenario_A.json");
            ScenarioStorage.Save(a, path);
            var loaded = ScenarioStorage.Load(path);

            Assert.AreEqual(a.scenarioId, loaded.scenarioId);
            Assert.AreEqual(a.orderId, loaded.orderId);
            CollectionAssert.AreEqual(a.skuSequence, loaded.skuSequence);
            Assert.AreEqual(a.startPosition, loaded.startPosition);
            Assert.AreEqual(a.walkSpeed, loaded.walkSpeed);
            Assert.AreEqual(a.searchSeconds, loaded.searchSeconds);

            ScenarioStorage.Save(ScenarioB(), Path.Combine(ScenarioDir, "scenario_B.json"));
            Assert.Throws<InvalidDataException>(() => ScenarioStorage.FromJson("{\"scenarioId\":\"X\"}"));
        }

        [Test]
        public void Run_IsDeterministic_AndUsesRealClassifierEvents()
        {
            var r1 = ScenarioRunner.Run(ScenarioA(), orders, logger, classifier);
            var r2 = ScenarioRunner.Run(ScenarioA(), orders, logger, classifier);

            Assert.AreEqual(r1.events.Count, r2.events.Count);
            Assert.AreEqual(r1.Results.TotalTaskTime, r2.Results.TotalTaskTime, 1e-3f);
            Assert.AreEqual(r1.Results.TotalDistance, r2.Results.TotalDistance, 1e-3f);
            Assert.AreEqual(4, r1.Results.TotalPicks);
            Assert.IsTrue(r1.Results.VerifyReconciliation(out string why), why);
            Assert.IsTrue(r1.events.Any(e => e.Activity == ActivityType.Searching));
            Assert.IsTrue(r1.events.Any(e => e.Activity == ActivityType.Packing));
            Assert.AreEqual(ScenarioRunResult.MeasuredLabel, r1.label);
        }

        [Test]
        public void Comparison_FlagsNonEquivalentConditions()
        {
            var a = ScenarioRunner.Run(ScenarioA(), orders, logger, classifier);
            var b = ScenarioB(); b.walkSpeed = 1.8f;
            var rb = ScenarioRunner.Run(b, orders, logger, classifier);
            var cmp = new ScenarioComparison(a, rb);
            Assert.IsFalse(cmp.IsEquivalent);
            Assert.IsTrue(cmp.EquivalenceIssues.Any(i => i.Contains("Walk speeds")));

            var differentOrder = MakeScenario("SCN-C", "C", "", "SKU-0003", "SKU-0030");
            var rc = ScenarioRunner.Run(differentOrder, orders, logger, classifier);
            Assert.IsTrue(new ScenarioComparison(a, rc).EquivalenceIssues.Any(i => i.Contains("not the same order")));
        }

        [Test]
        public void TwoScenarios_RunCompareAndExportEvidence()
        {
            foreach (var skuId in new[] { "SKU-0003", "SKU-0030", "SKU-0045", "SKU-0066" })
            {
                store.TryGetSlotForSku(skuId, out ShelfSlot slot);
                Debug.Log($"[Phase6] {skuId} -> {slot.SlotId} @ {slot.WorldPosition}");
            }

            ScenarioStorage.Save(ScenarioA(), Path.Combine(ScenarioDir, "scenario_A.json"));
            ScenarioStorage.Save(ScenarioB(), Path.Combine(ScenarioDir, "scenario_B.json"));
            var runA = ScenarioRunner.Run(ScenarioStorage.Load(Path.Combine(ScenarioDir, "scenario_A.json")), orders, logger, classifier);
            var runB = ScenarioRunner.Run(ScenarioStorage.Load(Path.Combine(ScenarioDir, "scenario_B.json")), orders, logger, classifier);

            // Each run's events reflect only that run
            Assert.AreEqual(runA.events.Count > 0, true);
            Assert.AreEqual(4, runA.Results.TotalPicks);
            Assert.AreEqual(4, runB.Results.TotalPicks);

            var cmp = new ScenarioComparison(runA, runB);
            Assert.IsTrue(cmp.IsEquivalent, string.Join("; ", cmp.EquivalenceIssues));
            CollectionAssert.AreEqual(new[] { "pick/visit sequence" }, cmp.VariedFactors.ToArray());
            Assert.IsTrue(cmp.BothReconciled);
            Assert.Greater(Mathf.Abs(runA.Results.TotalDistance - runB.Results.TotalDistance), 1f, "Sequences were chosen so measured distance differs.");

            // Persist actual recorded runs, comparison report, and route maps
            ScenarioStorage.SaveRun(runA, Path.Combine(EvidenceDir, "run_A.json"));
            ScenarioStorage.SaveRun(runB, Path.Combine(EvidenceDir, "run_B.json"));
            ScenarioStorage.WriteText(Path.Combine(EvidenceDir, "comparison_A_vs_B.md"), cmp.ToMarkdown());

            var slots = store.GetAllSlots().Select(s => s.WorldPosition).ToList();
            var colors = new[] { RouteMapRenderer.ColorA, RouteMapRenderer.ColorB };
            SaveMap(new[] { runA }, new[] { colors[0] }, slots, "route_A.png");
            SaveMap(new[] { runB }, new[] { colors[1] }, slots, "route_B.png");
            SaveMap(new[] { runA, runB }, colors, slots, "route_A_vs_B_overlay.png");

            // Reload recorded runs from disk: KPIs recomputed from stored events must match
            var loadedA = ScenarioStorage.LoadRun(Path.Combine(EvidenceDir, "run_A.json"));
            Assert.AreEqual(runA.Results.TotalTaskTime, loadedA.Results.TotalTaskTime, 1e-3f);
            Assert.AreEqual(runA.routePoints.Count, loadedA.routePoints.Count);

            Debug.Log("[Phase6] \n" + cmp.ToMarkdown());
        }

        private static void SaveMap(ScenarioRunResult[] runs, Color[] colors, List<Vector3> slots, string file)
        {
            var tex = RouteMapRenderer.Render(runs, colors, slots);
            try { File.WriteAllBytes(Path.Combine(EvidenceDir, file), ImageConversion.EncodeToPNG(tex)); }
            finally { Object.DestroyImmediate(tex); }
        }
    }
}
