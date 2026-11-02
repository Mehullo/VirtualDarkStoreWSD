using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Management;
using VirtualDarkStore.Routing;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Scenarios
{
    [Serializable]
    public class OperationalCycleRunResult
    {
        public string runLabel;
        public string orderId;
        public List<string> skuSequence = new List<string>();
        public bool isOptimizedRoute;
        public float algorithmicRouteCost;
        public float measuredPlayerDistance;
        public float totalCycleDuration;
        public int aisleTransitions;
        public int aisleBacktracks;
        public bool orderCompletedSuccessfully;
        public List<WorkEvent> events = new List<WorkEvent>();
        public List<Vector3> routePoints = new List<Vector3>();
        public List<string> waypointLabels = new List<string>();
        public List<Vector3> waypointPositions = new List<Vector3>();
        public float samplingIntervalSeconds = 1.0f;
        public string recordedAtLocal;
        public OrderGenerationConfig generationConfig;

        public WorkStudyResults Results => WorkStudyCalculator.Calculate(events, samplingIntervalSeconds);
    }

    /// <summary>
    /// Executes the complete 7-stage operational workflow via simulated picker motion:
    /// ENTRY -> COLLECT ORDER -> PICKING -> AWAITING PACKING -> DEPOSIT ORDER -> WALK TO EXIT -> COMPLETED.
    /// Drives the identical OrderManager, ActivityClassifier, and WorkEventLogger used by interactive desktop/VR play.
    /// Evaluates true corridor traversal and records accurate work-study event telemetry.
    /// </summary>
    public static class OperationalCycleRunner
    {
        private const float Epsilon = 1e-4f;

        public static OperationalCycleRunResult RunCycle(
            IReadOnlyList<string> skuSequence,
            string orderId,
            bool isOptimized,
            float algorithmicCost,
            OrderManager orderManager,
            WorkEventLogger logger,
            ActivityClassifier classifier,
            OrderGenerationConfig config = null,
            float walkSpeed = 1.4f,
            float searchSeconds = 1.5f,
            float packingSeconds = 2.0f,
            float timeStep = 0.1f,
            float samplingInterval = 1.0f)
        {
            if (skuSequence == null || skuSequence.Count == 0)
                throw new ArgumentException("SKU sequence must contain at least one item.", nameof(skuSequence));
            if (orderManager == null || logger == null || classifier == null)
                throw new ArgumentNullException("OrderManager, WorkEventLogger, and ActivityClassifier must be provided.");

            var store = StoreManager.Instance;
            if (store == null || store.Configuration == null)
                throw new InvalidOperationException("StoreManager is not initialized.");

            var opMgr = OperationalCycleManager.Instance;
            Vector3 entryPos = opMgr != null ? opMgr.EntryPosition : new Vector3(-8.0f, 0.0f, -10.5f);
            Vector3 collectDeskPos = opMgr != null ? opMgr.CollectionDeskPosition : new Vector3(-4.0f, 0.0f, -8.8f);
            Vector3 packDeskPos = opMgr != null ? opMgr.PackingDeskPosition : new Vector3(2.0f, 0.0f, -8.8f);
            Vector3 exitPos = opMgr != null ? opMgr.ExitPosition : new Vector3(8.0f, 0.0f, -10.5f);

            float sideOffset = store.Configuration.AisleWidth * 0.5f + store.Configuration.RackDepth * 0.5f;
            float crossZ = -8.8f; // Front cross-aisle Z corridor

            var runResult = new OperationalCycleRunResult
            {
                runLabel = isOptimized ? "Optimized Pick Sequence (Solver)" : "Baseline Pick Sequence (Original)",
                orderId = orderId ?? "ORD-0001",
                skuSequence = new List<string>(skuSequence),
                isOptimizedRoute = isOptimized,
                algorithmicRouteCost = algorithmicCost,
                samplingIntervalSeconds = samplingInterval,
                recordedAtLocal = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                generationConfig = config != null ? config.Clone() : null
            };

            logger.Clear();
            float t = 0f;
            Vector3 pos = entryPos; pos.y = 0f;
            classifier.ResetState(0f, pos);

            runResult.routePoints.Add(pos);
            runResult.waypointLabels.Add("ENTRY");
            runResult.waypointPositions.Add(pos);

            // STAGE 1: ENTRY -> Wait briefly at entry gate (0.5s)
            t = StepStationary(classifier, t, pos, 0.5f, timeStep, false, false);

            // STAGE 2: WALK TO ORDER COLLECTION DESK
            t = StepWalkStraight(classifier, runResult, t, ref pos, collectDeskPos, walkSpeed, timeStep);
            runResult.waypointLabels.Add("COLLECT_ORDER");
            runResult.waypointPositions.Add(pos);

            // Collect Order & pick cart (1.0s wait at desk)
            t = StepStationary(classifier, t, pos, 1.0f, timeStep, false, false);

            // Dispatch order via OrderManager
            var order = orderManager.StartSpecificOrder(orderId, skuSequence);
            if (order == null) throw new InvalidOperationException($"Failed to dispatch order '{orderId}'.");

            // STAGE 3: PICKING (Visit each item in sequence)
            var visitedSlotIds = new List<string>();

            for (int i = 0; i < order.Items.Count; i++)
            {
                var item = order.Items[i];
                visitedSlotIds.Add(item.TargetSlotId);

                // Compute pick standoff position in aisle corridor
                float dir = 1f;
                if (item.TargetSlotId.Contains("-L-")) dir = 1f;
                else if (item.TargetSlotId.Contains("-R-")) dir = -1f;

                Vector3 pickStand = new Vector3(item.TargetWorldPosition.x + dir * sideOffset, 0f, item.TargetWorldPosition.z);

                // Walk to pick stand via corridor navigation
                t = StepWalkCorridor(classifier, runResult, t, ref pos, pickStand, crossZ, walkSpeed, timeStep);
                runResult.waypointLabels.Add($"PICK {item.SkuId}");
                runResult.waypointPositions.Add(pos);

                // Search shelf bin
                t = StepStationary(classifier, t, pos, searchSeconds, timeStep, true, false);

                // Acquire/pick SKU
                var pickRes = orderManager.AttemptPick(item.SkuId);
                if (pickRes != PickResult.Success)
                {
                    throw new InvalidOperationException($"Pick rejected for item {item.SkuId}: {pickRes}");
                }
                t = Mathf.Max(t, classifier.CurrentActivityStartTime);
            }

            // STAGE 4: AWAITING PACKING -> Return to Packing/Deposit Desk
            t = StepWalkCorridor(classifier, runResult, t, ref pos, packDeskPos, crossZ, walkSpeed, timeStep);
            runResult.waypointLabels.Add("DEPOSIT_PACKING");
            runResult.waypointPositions.Add(pos);

            // STAGE 5: DEPOSIT ORDER at Packing Desk (packSeconds)
            t = StepStationary(classifier, t, pos, packingSeconds, timeStep, false, true);

            if (!orderManager.CompletePacking(out string packError))
            {
                throw new InvalidOperationException($"Packing completion failed: {packError}");
            }

            // STAGE 6: WALK TO EXIT
            t = StepWalkStraight(classifier, runResult, t, ref pos, exitPos, walkSpeed, timeStep);
            runResult.waypointLabels.Add("EXIT");
            runResult.waypointPositions.Add(pos);

            // STAGE 7: COMPLETED -> Session completion at exit zone
            t = StepStationary(classifier, t, pos, 0.2f, timeStep, false, false);

            runResult.totalCycleDuration = t;
            runResult.measuredPlayerDistance = classifier.CumulativeTotalDistance;
            runResult.orderCompletedSuccessfully = (order.Status == OrderStatus.Completed);
            runResult.events.AddRange(logger.LoggedEvents);

            OperationalCycleManager.CalculateAisleMetrics(visitedSlotIds, out int transitions, out int backtracks);
            runResult.aisleTransitions = transitions;
            runResult.aisleBacktracks = backtracks;

            if (!logger.VerifyIntegrity(out string integrityError))
            {
                throw new InvalidOperationException($"Event log integrity violation: {integrityError}");
            }

            return runResult;
        }

        private static float StepWalkCorridor(
            ActivityClassifier classifier,
            OperationalCycleRunResult run,
            float currentTime,
            ref Vector3 currentPos,
            Vector3 targetPos,
            float crossZ,
            float speed,
            float timeStep)
        {
            // If picker is currently in an aisle, transit through the front cross-aisle to avoid rack collisions
            bool needsCrossAisle = Mathf.Abs(currentPos.x - targetPos.x) > Epsilon;

            if (needsCrossAisle)
            {
                // 1. Move to current aisle's cross-aisle exit
                Vector3 toCross = new Vector3(currentPos.x, 0f, crossZ);
                currentTime = StepWalkStraight(classifier, run, currentTime, ref currentPos, toCross, speed, timeStep);

                // 2. Move along front cross-aisle to target aisle's X coordinate
                Vector3 toTargetAisle = new Vector3(targetPos.x, 0f, crossZ);
                currentTime = StepWalkStraight(classifier, run, currentTime, ref currentPos, toTargetAisle, speed, timeStep);
            }

            // 3. Move into target position along Z corridor
            return StepWalkStraight(classifier, run, currentTime, ref currentPos, targetPos, speed, timeStep);
        }

        private static float StepWalkStraight(
            ActivityClassifier classifier,
            OperationalCycleRunResult run,
            float currentTime,
            ref Vector3 currentPos,
            Vector3 targetPos,
            float speed,
            float timeStep)
        {
            float dist = Vector3.Distance(currentPos, targetPos);
            if (dist < Epsilon) return currentTime;

            float legTime = dist / Mathf.Max(speed, 0.1f);
            int steps = Mathf.Max(1, Mathf.RoundToInt(legTime / timeStep));
            float actualStepDuration = legTime / steps;
            float stepDist = dist / steps;

            for (int s = 1; s <= steps; s++)
            {
                float frac = (float)s / steps;
                Vector3 pos = Vector3.Lerp(currentPos, targetPos, frac);
                currentTime += actualStepDuration;
                classifier.Step(currentTime, pos, speed, false, false, stepDist);
                run.routePoints.Add(pos);
            }

            currentPos = targetPos;
            return currentTime;
        }

        private static float StepStationary(
            ActivityClassifier classifier,
            float currentTime,
            Vector3 pos,
            float duration,
            float timeStep,
            bool lookingAtShelf,
            bool atBench)
        {
            if (duration <= 0f) return currentTime;
            int steps = Mathf.Max(1, Mathf.RoundToInt(duration / timeStep));
            float dt = duration / steps;

            for (int s = 0; s < steps; s++)
            {
                currentTime += dt;
                classifier.Step(currentTime, pos, 0f, lookingAtShelf, atBench, 0f);
            }

            return currentTime;
        }

        /// <summary>
        /// Executes an entire multi-order simulation sequentially via simulated picker motion:
        /// For each order in the generated batch:
        ///   Order 1: Pick items -> Complete / Deposit -> Order 2: Pick items -> Complete / Deposit -> ...
        /// Produces per-order results and mathematically reconciled aggregate simulation metrics.
        /// </summary>
        public static MultiOrderSimulationResult RunMultiOrderSimulation(
            StoreManager store,
            OrderGenerationConfig config,
            OrderManager orderManager,
            WorkEventLogger logger,
            ActivityClassifier classifier,
            bool isOptimized = false,
            float walkSpeed = 1.4f,
            float searchSeconds = 1.5f,
            float packingSeconds = 2.0f,
            float timeStep = 0.1f,
            float samplingInterval = 1.0f)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (orderManager == null || logger == null || classifier == null)
                throw new ArgumentNullException("OrderManager, WorkEventLogger, and ActivityClassifier must be provided.");

            if (!config.Validate(store, out string valErr))
            {
                throw new ArgumentException(valErr, nameof(config));
            }

            var batch = OrderGenerator.GenerateBatch(store, config);
            var simResult = new MultiOrderSimulationResult
            {
                simulationId = $"SIM-{config.Seed}-{batch.Count}x{config.ItemCount}",
                totalOrdersRequested = config.OrderCount,
                itemsPerOrder = config.ItemCount,
                totalItemsRequested = config.OrderCount * config.ItemCount,
                seed = config.Seed
            };

            for (int i = 0; i < batch.Count; i++)
            {
                var order = batch[i];
                var skuIds = new List<string>(order.TotalItemCount);
                for (int j = 0; j < order.Items.Count; j++)
                {
                    skuIds.Add(order.Items[j].SkuId);
                }

                var runResult = RunCycle(
                    skuSequence: skuIds,
                    orderId: order.OrderId,
                    isOptimized: isOptimized,
                    algorithmicCost: 0f,
                    orderManager: orderManager,
                    logger: logger,
                    classifier: classifier,
                    config: config,
                    walkSpeed: walkSpeed,
                    searchSeconds: searchSeconds,
                    packingSeconds: packingSeconds,
                    timeStep: timeStep,
                    samplingInterval: samplingInterval);

                simResult.orderRuns.Add(runResult);
                if (runResult.orderCompletedSuccessfully)
                {
                    simResult.totalOrdersCompleted++;
                    simResult.totalItemsPicked += runResult.skuSequence.Count;
                }

                simResult.totalSimulationDuration += runResult.totalCycleDuration;
                simResult.totalTravelDistance += runResult.measuredPlayerDistance;
                simResult.totalAisleTransitions += runResult.aisleTransitions;
                simResult.totalAisleBacktracks += runResult.aisleBacktracks;

                if (runResult.events != null)
                {
                    simResult.allEvents.AddRange(runResult.events);
                }
            }

            if (simResult.totalOrdersCompleted > 0)
            {
                simResult.averageCycleDurationPerOrder = simResult.totalSimulationDuration / simResult.totalOrdersCompleted;
                simResult.averageDistancePerOrder = simResult.totalTravelDistance / simResult.totalOrdersCompleted;
            }

            return simResult;
        }
    }
}
