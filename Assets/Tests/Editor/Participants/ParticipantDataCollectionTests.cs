using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Domain.Participants;
using VirtualDarkStore.Management;
using VirtualDarkStore.Scenarios;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Tests.Editor.Participants
{
    [TestFixture]
    public class ParticipantDataCollectionTests
    {
        private string testDataDir;
        private ParticipantRoster roster;

        [SetUp]
        public void SetUp()
        {
            testDataDir = Path.Combine(Path.GetTempPath(), "VirtualDarkStore_Test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDataDir);
            roster = new ParticipantRoster(testDataDir);
            roster.InitializeSlots();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDataDir))
            {
                try { Directory.Delete(testDataDir, true); } catch { }
            }
        }

        [Test]
        public void Test01_RegisterAll10Participants_Succeeds()
        {
            for (int i = 1; i <= 10; i++)
            {
                string name = $"Participant_{i:D2}";
                bool registered = roster.TryRegisterParticipant(name, out var profile, out string err);
                Assert.IsTrue(registered, $"Expected slot {i} to register successfully. Error: {err}");
                Assert.IsNotNull(profile);
                Assert.AreEqual($"P{i:D2}", profile.SlotId);
                Assert.AreEqual(name, profile.ParticipantName);
            }
            Assert.AreEqual(10, roster.RegisteredCount);
        }

        [Test]
        public void Test02_Reject11thParticipant_EnforcesCapacityLimit()
        {
            // Register all 10
            for (int i = 1; i <= 10; i++)
            {
                roster.TryRegisterParticipant($"Participant_{i}", out _, out _);
            }

            // Attempt 11th registration
            bool registered = roster.TryRegisterParticipant("Participant_11", out var profile, out string err);
            Assert.IsFalse(registered, "Expected 11th participant to be rejected.");
            Assert.IsNull(profile);
            StringAssert.Contains("Cannot register more than 10 participants", err);
        }

        [Test]
        public void Test03_RejectDuplicateParticipantNames_CaseInsensitive()
        {
            bool reg1 = roster.TryRegisterParticipant("Rahul Sharma", out _, out _);
            Assert.IsTrue(reg1);

            // Attempt duplicate with different casing
            bool reg2 = roster.TryRegisterParticipant("rahul sharma", out _, out string err);
            Assert.IsFalse(reg2, "Expected duplicate name to be rejected.");
            StringAssert.Contains("already registered", err);
        }

        [Test]
        public void Test04_RejectEmptyParticipantName()
        {
            bool regNull = roster.TryRegisterParticipant(null, out _, out string errNull);
            Assert.IsFalse(regNull);
            StringAssert.Contains("cannot be null, empty", errNull);

            bool regEmpty = roster.TryRegisterParticipant("   ", out _, out string errEmpty);
            Assert.IsFalse(regEmpty);
            StringAssert.Contains("cannot be null, empty", errEmpty);
        }

        [Test]
        public void Test05_CompleteScenarioForP01_ExportsSummaryCsv()
        {
            roster.TryRegisterParticipant("Aman", out var profile, out _);
            Assert.IsNotNull(profile);

            var events = CreateSampleWorkEvents("ORD-001");
            var results = WorkStudyCalculator.Calculate(events);
            var order = CreateTestOrder("ORD-001", new[] { "SKU-001", "SKU-002" });

            var record = ParticipantSessionRecord.Create(
                profile,
                scenarioName: "Scenario A",
                routingMode: "Baseline",
                interactionMode: "Desktop",
                order: order,
                results: results,
                events: events,
                sessionId: "S001");

            ParticipantDataExporter.ExportSessionBundle(record, events, testDataDir, out string sumPath, out string evtPath);

            Assert.IsTrue(File.Exists(sumPath), $"Summary file not found at: {sumPath}");
            Assert.IsTrue(File.Exists(evtPath), $"Events file not found at: {evtPath}");

            string sumText = File.ReadAllText(sumPath);
            StringAssert.Contains("P01", sumText);
            StringAssert.Contains("Aman", sumText);
            StringAssert.Contains("Scenario A", sumText);
            StringAssert.Contains("S001", sumText);
            StringAssert.Contains("Baseline", sumText);
        }

        [Test]
        public void Test06_CompleteDifferentScenarioForP01_WithoutOverwritingFirstReport()
        {
            roster.TryRegisterParticipant("Aman", out var profile, out _);

            var eventsA = CreateSampleWorkEvents("ORD-A");
            var resultsA = WorkStudyCalculator.Calculate(eventsA);
            var orderA = CreateTestOrder("ORD-A", new[] { "SKU-001" });

            var recordA = ParticipantSessionRecord.Create(profile, "Scenario A", "Baseline", "Desktop", orderA, resultsA, eventsA, "S001");
            ParticipantDataExporter.ExportSessionBundle(recordA, eventsA, testDataDir, out string sumA, out _);

            var eventsB = CreateSampleWorkEvents("ORD-B");
            var resultsB = WorkStudyCalculator.Calculate(eventsB);
            var orderB = CreateTestOrder("ORD-B", new[] { "SKU-002" });

            var recordB = ParticipantSessionRecord.Create(profile, "Scenario B", "Baseline", "Desktop", orderB, resultsB, eventsB, "S002");
            ParticipantDataExporter.ExportSessionBundle(recordB, eventsB, testDataDir, out string sumB, out _);

            Assert.AreNotEqual(sumA, sumB, "Filenames should be distinct.");
            Assert.IsTrue(File.Exists(sumA), "First session file should still exist.");
            Assert.IsTrue(File.Exists(sumB), "Second session file should exist.");

            StringAssert.Contains("Scenario A", File.ReadAllText(sumA));
            StringAssert.Contains("Scenario B", File.ReadAllText(sumB));
        }

        [Test]
        public void Test07_CompleteSameScenarioForP02_MaintainsSeparateFoldersAndFiles()
        {
            roster.TryRegisterParticipant("Aman", out var p01, out _);
            roster.TryRegisterParticipant("Vikram", out var p02, out _);

            var events = CreateSampleWorkEvents("ORD-001");
            var results = WorkStudyCalculator.Calculate(events);
            var order = CreateTestOrder("ORD-001", new[] { "SKU-001" });

            var record1 = ParticipantSessionRecord.Create(p01, "StandardScenario", "Baseline", "Desktop", order, results, events, "S001");
            ParticipantDataExporter.ExportSessionBundle(record1, events, testDataDir, out string sum1, out _);

            var record2 = ParticipantSessionRecord.Create(p02, "StandardScenario", "Baseline", "Desktop", order, results, events, "S002");
            ParticipantDataExporter.ExportSessionBundle(record2, events, testDataDir, out string sum2, out _);

            Assert.AreNotEqual(sum1, sum2);
            StringAssert.Contains("P01_Aman", sum1);
            StringAssert.Contains("P02_Vikram", sum2);
            Assert.IsTrue(File.Exists(sum1));
            Assert.IsTrue(File.Exists(sum2));
        }

        [Test]
        public void Test08_RunBaselineAndOptimizedRouting_SameParticipantAndScenario()
        {
            roster.TryRegisterParticipant("Priya", out var profile, out _);

            var events = CreateSampleWorkEvents("ORD-001");
            var results = WorkStudyCalculator.Calculate(events);
            var order = CreateTestOrder("ORD-001", new[] { "SKU-001", "SKU-002" });

            var recordBase = ParticipantSessionRecord.Create(profile, "Scenario A", "Baseline", "Desktop", order, results, events, "S001");
            ParticipantDataExporter.ExportSessionBundle(recordBase, events, testDataDir, out string sumBase, out _);

            var recordOpt = ParticipantSessionRecord.Create(profile, "Scenario A", "Optimized", "Desktop", order, results, events, "S002");
            ParticipantDataExporter.ExportSessionBundle(recordOpt, events, testDataDir, out string sumOpt, out _);

            StringAssert.Contains("Baseline", sumBase);
            StringAssert.Contains("Optimized", sumOpt);
            Assert.IsTrue(File.Exists(sumBase));
            Assert.IsTrue(File.Exists(sumOpt));
        }

        [Test]
        public void Test09_VerifySameOrderDefinition_AcrossComparisonSessions()
        {
            roster.TryRegisterParticipant("Participant 1", out var p1, out _);
            roster.TryRegisterParticipant("Participant 2", out var p2, out _);

            var skus = new[] { "SKU-001", "SKU-002", "SKU-003" };
            var orderA = CreateTestOrder("ORD-COMPARE", skus);
            var orderB = CreateTestOrder("ORD-COMPARE", skus);

            var eventsA = CreateSampleWorkEvents("ORD-COMPARE");
            var eventsB = CreateSampleWorkEvents("ORD-COMPARE");

            var recA = ParticipantSessionRecord.Create(p1, "ScnCompare", "Baseline", "Desktop", orderA, WorkStudyCalculator.Calculate(eventsA), eventsA, "S001");
            var recB = ParticipantSessionRecord.Create(p2, "ScnCompare", "Baseline", "VR", orderB, WorkStudyCalculator.Calculate(eventsB), eventsB, "S002");

            Assert.AreEqual(recA.plannedPickSequence, recB.plannedPickSequence);
            Assert.AreEqual(recA.orderId, recB.orderId);
            Assert.AreEqual(recA.totalItems, recB.totalItems);
        }

        [Test]
        public void Test10_VerifyEventLevelCsv_MatchesWorkEventLogger()
        {
            roster.TryRegisterParticipant("Neha", out var profile, out _);
            var events = CreateSampleWorkEvents("ORD-LOG-CHECK");
            var results = WorkStudyCalculator.Calculate(events);
            var order = CreateTestOrder("ORD-LOG-CHECK", new[] { "SKU-001" });

            var record = ParticipantSessionRecord.Create(profile, "LogCheckScn", "Baseline", "Desktop", order, results, events, "S001");
            ParticipantDataExporter.ExportSessionBundle(record, events, testDataDir, out _, out string evtPath);

            var lines = File.ReadAllLines(evtPath);
            // Header + N events
            Assert.AreEqual(events.Count + 1, lines.Length);
            for (int i = 0; i < events.Count; i++)
            {
                var parts = ParticipantDataExporter.ParseCsvLine(lines[i + 1]);
                Assert.AreEqual(profile.SlotId, parts[0]);
                Assert.AreEqual(profile.ParticipantName, parts[1]);
                Assert.AreEqual("S001", parts[2]);
                Assert.AreEqual(events[i].Id, parts[4]);
                Assert.AreEqual(events[i].Activity.ToString(), parts[5]);
            }
        }

        [Test]
        public void Test11_GenerateCombinedSummaries_RetainsAllRecordsExactlyOnce()
        {
            roster.TryRegisterParticipant("User1", out var p1, out _);
            roster.TryRegisterParticipant("User2", out var p2, out _);

            var order = CreateTestOrder("ORD-001", new[] { "SKU-001" });
            var events = CreateSampleWorkEvents("ORD-001");
            var results = WorkStudyCalculator.Calculate(events);

            var rec1 = ParticipantSessionRecord.Create(p1, "ScnA", "Baseline", "Desktop", order, results, events, "S001");
            var rec2 = ParticipantSessionRecord.Create(p2, "ScnA", "Baseline", "Desktop", order, results, events, "S002");

            // Export rec1 twice (simulating idempotent re-export)
            ParticipantDataExporter.ExportSessionBundle(rec1, events, testDataDir, out _, out _);
            ParticipantDataExporter.ExportSessionBundle(rec1, events, testDataDir, out _, out _);

            // Export rec2
            ParticipantDataExporter.ExportSessionBundle(rec2, events, testDataDir, out _, out _);

            string combinedPath = Path.Combine(testDataDir, "Combined", "All_Session_Summaries.csv");
            Assert.IsTrue(File.Exists(combinedPath));

            var lines = File.ReadAllLines(combinedPath);
            // Header + exactly 2 distinct sessions (S001 and S002)
            Assert.AreEqual(3, lines.Length, "Expected exactly 3 lines: 1 header and 2 records.");
        }

        [Test]
        public void Test12_FilenameAndCsvHandling_NamesWithSpacesAndSpecialCharacters()
        {
            string complexName = "Jean-Luc O'Connor, Jr.";
            bool reg = roster.TryRegisterParticipant(complexName, out var profile, out string err);
            Assert.IsTrue(reg, $"Failed to register complex name: {err}");

            var events = CreateSampleWorkEvents("ORD-001");
            var results = WorkStudyCalculator.Calculate(events);
            var order = CreateTestOrder("ORD-001", new[] { "SKU-001" });

            var record = ParticipantSessionRecord.Create(profile, "Scenario: Test, Alpha", "Baseline", "Desktop", order, results, events, "S-001");
            ParticipantDataExporter.ExportSessionBundle(record, events, testDataDir, out string sumPath, out string evtPath);

            Assert.IsTrue(File.Exists(sumPath));
            Assert.IsTrue(File.Exists(evtPath));

            // Verify RFC-4180 escaping in summary file
            string sumText = File.ReadAllText(sumPath);
            StringAssert.Contains("\"Jean-Luc O'Connor, Jr.\"", sumText);
            StringAssert.Contains("\"Scenario: Test, Alpha\"", sumText);
        }

        [Test]
        public void Test13_DesktopExportDirectory_ResolvesSafePath()
        {
            string dir = ParticipantDataExporter.GetRootExportDirectory(null);
            Assert.IsNotNull(dir);
            StringAssert.Contains("WorkStudyData", dir);
        }

        [Test]
        public void Test14_RosterPersistence_PreservesStateAcrossReload()
        {
            roster.TryRegisterParticipant("Alice", out _, out _);
            roster.TryRegisterParticipant("Bob", out _, out _);
            roster.SelectParticipant(1, out _); // Select Bob

            // Create new roster instance pointing to same test directory
            var reloaded = new ParticipantRoster(testDataDir);
            Assert.AreEqual(2, reloaded.RegisteredCount);
            Assert.AreEqual("Alice", reloaded.Slots[0].ParticipantName);
            Assert.AreEqual("Bob", reloaded.Slots[1].ParticipantName);
            Assert.IsNotNull(reloaded.ActiveParticipant);
            Assert.AreEqual("Bob", reloaded.ActiveParticipant.ParticipantName);
        }

        [Test]
        public void Test15_ExistingSimulationCoreParity_ReconcilesWithoutFabrication()
        {
            roster.TryRegisterParticipant("ParityUser", out var p, out _);
            var events = CreateSampleWorkEvents("ORD-PARITY");
            var results = WorkStudyCalculator.Calculate(events);

            Assert.IsTrue(results.VerifyReconciliation(out string reason), $"Reconciliation failed: {reason}");
            Assert.Greater(results.TotalTaskTime, 0f);
            Assert.Greater(results.TotalDistance, 0f);

            var rec = ParticipantSessionRecord.Create(p, "ParityScn", "Baseline", "Desktop", null, results, events);
            Assert.AreEqual("PASS (100.0% Reconciled)", rec.dataValidationStatus);
            Assert.AreEqual(0, rec.missingEventCount);
        }

        private static List<WorkEvent> CreateSampleWorkEvents(string orderId)
        {
            return new List<WorkEvent>
            {
                new WorkEvent("EVT-001", ActivityType.Waiting, 0.0f, 1.0f, new Vector3(0f, 0f, -8.5f), "OrderDesk", "", orderId, 0f),
                new WorkEvent("EVT-002", ActivityType.MovingBetweenLocations, 1.0f, 4.0f, new Vector3(-4.5f, 0f, -4.0f), "A01-L-B01", "", orderId, 5.0f),
                new WorkEvent("EVT-003", ActivityType.Searching, 4.0f, 6.0f, new Vector3(-4.5f, 0f, -4.0f), "A01-L-B01-T01-S01", "SKU-001", orderId, 0f),
                new WorkEvent("EVT-004", ActivityType.Picking, 6.0f, 7.0f, new Vector3(-4.5f, 0f, -4.0f), "A01-L-B01-T01-S01", "SKU-001", orderId, 0f),
                new WorkEvent("EVT-005", ActivityType.Approaching, 7.0f, 9.0f, new Vector3(2.0f, 0f, -10.0f), "PACK-01", "", orderId, 3.0f),
                new WorkEvent("EVT-006", ActivityType.Packing, 9.0f, 11.0f, new Vector3(2.0f, 0f, -10.0f), "PACK-01", "", orderId, 0f)
            };
        }

        private static Order CreateTestOrder(string orderId, string[] skuIds)
        {
            var items = new List<OrderItem>();
            for (int i = 0; i < skuIds.Length; i++)
            {
                var item = new OrderItem(skuIds[i], $"Product {skuIds[i]}", $"A01-L-B01-T01-S0{i + 1}", new Vector3(-4.5f, 0.8f, -4.0f + i), 1);
                items.Add(item);
            }
            return new Order(orderId, items);
        }
    }
}
