using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Management;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Scenarios
{
    /// <summary>
    /// Drives a scripted virtual picker through a <see cref="ScenarioDefinition"/> using the SAME
    /// OrderManager, ActivityClassifier and WorkEventLogger used by Desktop mode (and, later, VR).
    /// The WorkEvents are produced by the classifier from the simulated motion; the runner never writes
    /// events itself. Walking is straight-line along a fixed routing rule (not NavMesh pathfinding):
    /// within an aisle the picker walks along it; between aisles it walks via the front cross-aisle
    /// (z of the packing-bench stand-off). This is a modelling assumption, not an optimisation.
    /// </summary>
    public static class ScenarioRunner
    {
        private const float Epsilon = 1e-4f;

        public static ScenarioRunResult Run(
            ScenarioDefinition scenario,
            OrderManager orderManager,
            WorkEventLogger logger,
            ActivityClassifier classifier,
            float samplingIntervalSeconds = 1f)
        {
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (!scenario.Validate(out string error)) throw new ArgumentException($"Invalid scenario: {error}", nameof(scenario));
            if (orderManager == null || logger == null || classifier == null)
                throw new ArgumentNullException("orderManager/logger/classifier must be provided.");

            var store = StoreManager.Instance;
            if (store == null || store.Configuration == null) throw new InvalidOperationException("StoreManager is not initialised.");
            var station = UnityEngine.Object.FindAnyObjectByType<PackingStation>();
            if (station == null) throw new InvalidOperationException("A PackingStation is required in the scene/harness.");

            float sideOffset = store.Configuration.AisleWidth * 0.5f + store.Configuration.RackDepth * 0.5f;
            Vector3 packStand = new Vector3(station.transform.position.x, 0f, station.transform.position.z + 1.0f);
            float crossZ = packStand.z;

            var run = new ScenarioRunResult
            {
                scenario = scenario.Clone(),
                recordedAtLocal = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                samplingIntervalSeconds = samplingIntervalSeconds
            };

            logger.Clear();
            Vector3 pos = scenario.startPosition; pos.y = 0f;
            float t = 0f;
            classifier.ResetState(0f, pos);
            run.routePoints.Add(pos);
            run.waypointLabels.Add("START");
            run.waypointPositions.Add(pos);

            // Initial stationary wait (classified by the classifier as Waiting; no order active yet)
            t = Stationary(classifier, t, pos, scenario.initialWaitSeconds, scenario.timeStep, false, false);

            var order = orderManager.StartSpecificOrder(scenario.orderId, scenario.skuSequence);
            if (order == null) throw new InvalidOperationException("Order could not be started.");

            foreach (var item in order.Items)
            {
                string slotId = item.TargetSlotId;
                float dir;
                if (slotId.Contains("-L-")) dir = 1f;       // left-side shelf: aisle lies at +x
                else if (slotId.Contains("-R-")) dir = -1f; // right-side shelf: aisle lies at -x
                else throw new InvalidOperationException($"Cannot determine shelf side from slot id '{slotId}'.");

                Vector3 stand = new Vector3(item.TargetWorldPosition.x + dir * sideOffset, 0f, item.TargetWorldPosition.z);
                t = WalkRoute(classifier, run, t, ref pos, stand, crossZ, scenario);
                run.waypointLabels.Add($"PICK {item.SkuId}");
                run.waypointPositions.Add(pos);

                t = Stationary(classifier, t, pos, scenario.searchSeconds, scenario.timeStep, true, false);

                var result = orderManager.AttemptPick(item.SkuId);
                if (result != PickResult.Success)
                    throw new InvalidOperationException($"Pick of {item.SkuId} was rejected ({result}).");
                t = Mathf.Max(t, classifier.CurrentActivityStartTime); // classifier logs the pick element duration
            }

            t = WalkRoute(classifier, run, t, ref pos, packStand, crossZ, scenario);
            run.waypointLabels.Add("PACK");
            run.waypointPositions.Add(pos);

            t = Stationary(classifier, t, pos, scenario.packingSeconds, scenario.timeStep, false, true);

            if (!orderManager.CompletePacking(out string packFailure))
                throw new InvalidOperationException($"Packing failed: {packFailure}");

            run.events.AddRange(logger.LoggedEvents);
            if (!logger.VerifyIntegrity(out string integrity))
                throw new InvalidOperationException($"Event log integrity failed: {integrity}");
            return run;
        }

        private static float Stationary(ActivityClassifier c, float t, Vector3 pos, float seconds, float dt, bool lookingAtShelf, bool atBench)
        {
            float end = t + seconds;
            while (t < end - Epsilon)
            {
                t = Mathf.Min(t + dt, end);
                c.Step(t, pos, 0f, lookingAtShelf, atBench, 0f);
            }
            if (seconds <= 0f) c.Step(t, pos, 0f, lookingAtShelf, atBench, 0f);
            return t;
        }

        private static float WalkRoute(ActivityClassifier c, ScenarioRunResult run, float t, ref Vector3 pos, Vector3 target, float crossZ, ScenarioDefinition s)
        {
            var legs = new List<Vector3>();
            if (Mathf.Abs(pos.x - target.x) > 0.01f)
            {
                legs.Add(new Vector3(pos.x, 0f, crossZ));
                legs.Add(new Vector3(target.x, 0f, crossZ));
            }
            legs.Add(target);

            foreach (var leg in legs)
            {
                while (true)
                {
                    Vector3 delta = leg - pos;
                    float dist = delta.magnitude;
                    if (dist <= Epsilon) break;
                    float step = Mathf.Min(s.walkSpeed * s.timeStep, dist);
                    pos += delta / dist * step;
                    t += step / s.walkSpeed;
                    c.Step(t, pos, s.walkSpeed, false, false, step);
                    run.routePoints.Add(pos);
                }
            }
            return t;
        }
    }
}
