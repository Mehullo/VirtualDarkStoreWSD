using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Management;
using VirtualDarkStore.Player;
using VirtualDarkStore.VR;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Scenarios
{
    public enum DemoExecutionMode
    {
        Desktop,
        VR,
        ScriptedVirtualPicker
    }

    public enum DemoStep
    {
        Step1_SelectScenario = 1,
        Step2_SelectOrder = 2,
        Step3_StartDesktopOrVR = 3,
        Step4_PickProducts = 4,
        Step5_CompleteOrder = 5,
        Step6_ViewWorkStudyResults = 6,
        Step7_CompareAnotherScenario = 7
    }

    [Serializable]
    public class OrderPreset
    {
        public string presetId;
        public string displayName;
        public string orderId;
        public List<string> skuIds = new List<string>();
        public bool useScenarioSequence;
    }

    /// <summary>
    /// Orchestrates the 7-step Final Demo workflow across Desktop, VR, and Scripted modes:
    /// 1. Select scenario
    /// 2. Select order
    /// 3. Start desktop or VR
    /// 4. Pick products
    /// 5. Complete order
    /// 6. View work-study results
    /// 7. Compare another scenario
    /// All modes drive the identical OrderManager, ActivityClassifier, WorkEventLogger, and WorkStudyCalculator.
    /// </summary>
    public class DemoFlowController : MonoBehaviour
    {
        private static DemoFlowController instance;
        public static DemoFlowController Instance => instance != null ? instance : (instance = FindAnyObjectByType<DemoFlowController>());

        [Header("Workflow State")]
        [SerializeField] private DemoStep currentStep = DemoStep.Step1_SelectScenario;
        [SerializeField] private DemoExecutionMode activeMode = DemoExecutionMode.Desktop;
        [SerializeField] private int selectedScenarioIndex = 0;
        [SerializeField] private int selectedOrderPresetIndex = 0;
        [SerializeField] private int comparisonScenarioIndex = 1;

        [Header("Scene References")]
        [SerializeField] private DesktopPickerController desktopPicker;
        [SerializeField] private VRPlayerRig vrPlayerRig;
        [SerializeField] private ScenarioRouteVisualizer routeVisualizer;
        [SerializeField] private Transform targetSlotBeacon;

        private readonly List<ScenarioDefinition> availableScenarios = new List<ScenarioDefinition>();
        private readonly List<OrderPreset> orderPresets = new List<OrderPreset>();
        private readonly List<Vector3> liveRoutePoints = new List<Vector3>();
        private readonly List<string> liveWaypointLabels = new List<string>();
        private readonly List<Vector3> liveWaypointPositions = new List<Vector3>();

        private ScenarioRunResult lastCompletedRun;
        private ScenarioRunResult comparisonRunA;
        private ScenarioRunResult comparisonRunB;
        private ScenarioComparison activeComparison;
        private Vector3 lastRecordedRoutePos;

        public DemoStep CurrentStep => currentStep;
        public DemoExecutionMode ActiveMode => activeMode;
        public IReadOnlyList<ScenarioDefinition> AvailableScenarios => availableScenarios;
        public IReadOnlyList<OrderPreset> OrderPresets => orderPresets;
        public int SelectedScenarioIndex => selectedScenarioIndex;
        public int SelectedOrderPresetIndex => selectedOrderPresetIndex;
        public int ComparisonScenarioIndex => comparisonScenarioIndex;

        public ScenarioDefinition SelectedScenario =>
            (availableScenarios.Count > 0 && selectedScenarioIndex >= 0 && selectedScenarioIndex < availableScenarios.Count)
                ? availableScenarios[selectedScenarioIndex]
                : null;

        public OrderPreset SelectedOrderPreset =>
            (orderPresets.Count > 0 && selectedOrderPresetIndex >= 0 && selectedOrderPresetIndex < orderPresets.Count)
                ? orderPresets[selectedOrderPresetIndex]
                : null;

        public ScenarioRunResult LastCompletedRun => lastCompletedRun;
        public ScenarioComparison ActiveComparison => activeComparison;

        public event Action<DemoStep> OnStepChanged;
        public event Action<ScenarioComparison> OnComparisonReady;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            InitializeCatalogAndPresets();
        }

        private void OnEnable()
        {
            SubscribeOrderEvents();
        }

        private void OnDisable()
        {
            UnsubscribeOrderEvents();
        }

        private void Start()
        {
            ResolveSceneReferences();
            SubscribeOrderEvents();
            EnsureTargetBeaconExists();
            UpdateTargetBeacon();
        }

        private void Update()
        {
            RecordLiveTrajectorySample();
            UpdateTargetBeacon();
        }

        public void InitializeCatalogAndPresets()
        {
            availableScenarios.Clear();
            orderPresets.Clear();

            // 1. Default Scenario A: 4-Aisle Catalogue-Number Sequence
            var scnA = new ScenarioDefinition
            {
                scenarioId = "SCN-A",
                scenarioName = "Scenario A: Catalogue-Number Sequence",
                description = "Order lines visited in ascending SKU-number order across Aisles 1 -> 2 -> 3 -> 4 (user-defined; not optimised).",
                orderId = "ORD-SCN-0001",
                skuSequence = new List<string> { "SKU-0003", "SKU-0030", "SKU-0045", "SKU-0066" },
                startPosition = new Vector3(0f, 0f, -8.5f),
                walkSpeed = 1.4f,
                initialWaitSeconds = 1.0f,
                searchSeconds = 2.0f,
                packingSeconds = 2.0f,
                timeStep = 0.1f
            };

            // 2. Default Scenario B: Alternate Sequence (same order, different visit order)
            var scnB = new ScenarioDefinition
            {
                scenarioId = "SCN-B",
                scenarioName = "Scenario B: Alternate Cross-Aisle Sequence",
                description = "Same 4-item order visited in alternate sequence (Aisle 1 -> 3 -> 2 -> 4; user-defined; not optimised).",
                orderId = "ORD-SCN-0001",
                skuSequence = new List<string> { "SKU-0003", "SKU-0045", "SKU-0030", "SKU-0066" },
                startPosition = new Vector3(0f, 0f, -8.5f),
                walkSpeed = 1.4f,
                initialWaitSeconds = 1.0f,
                searchSeconds = 2.0f,
                packingSeconds = 2.0f,
                timeStep = 0.1f
            };

            // 3. Demo Scenario C: 3-Item Aisle-Grouped Sequential Sweep (Aisle 1 -> Aisle 1 -> Aisle 3)
            var scnDemoA = new ScenarioDefinition
            {
                scenarioId = "SCN-DEMO-A",
                scenarioName = "Demo Scenario A: 3-Item Aisle-Grouped Sequence",
                description = "3-item demo order picking both Aisle 1 items in a single pass before visiting Aisle 3 (user-defined; not optimised).",
                orderId = "ORD-DEMO-0001",
                skuSequence = new List<string> { "SKU-0001", "SKU-0008", "SKU-0039" },
                startPosition = new Vector3(0f, 0f, -8.5f),
                walkSpeed = 1.4f,
                initialWaitSeconds = 1.0f,
                searchSeconds = 2.0f,
                packingSeconds = 2.0f,
                timeStep = 0.1f
            };

            // 4. Demo Scenario D: 3-Item Ungrouped Backtracking Sequence (Aisle 1 -> Aisle 3 -> Aisle 1)
            var scnDemoB = new ScenarioDefinition
            {
                scenarioId = "SCN-DEMO-B",
                scenarioName = "Demo Scenario B: 3-Item Ungrouped Backtracking",
                description = "Same 3-item demo order visited with inter-aisle backtracking (Aisle 1 -> Aisle 3 -> Aisle 1).",
                orderId = "ORD-DEMO-0001",
                skuSequence = new List<string> { "SKU-0001", "SKU-0039", "SKU-0008" },
                startPosition = new Vector3(0f, 0f, -8.5f),
                walkSpeed = 1.4f,
                initialWaitSeconds = 1.0f,
                searchSeconds = 2.0f,
                packingSeconds = 2.0f,
                timeStep = 0.1f
            };

            availableScenarios.Add(scnA);
            availableScenarios.Add(scnB);
            availableScenarios.Add(scnDemoA);
            availableScenarios.Add(scnDemoB);

            // Order Presets
            orderPresets.Add(new OrderPreset
            {
                presetId = "PRESET-SCENARIO",
                displayName = "Scenario Defined Order (Matches Selected Scenario)",
                orderId = "ORD-SCN-0001",
                skuIds = new List<string>(),
                useScenarioSequence = true
            });

            orderPresets.Add(new OrderPreset
            {
                presetId = "PRESET-ORD-SCN-0001",
                displayName = "ORD-SCN-0001 (4 Items: SKU-0003, SKU-0030, SKU-0045, SKU-0066)",
                orderId = "ORD-SCN-0001",
                skuIds = new List<string> { "SKU-0003", "SKU-0030", "SKU-0045", "SKU-0066" },
                useScenarioSequence = false
            });

            orderPresets.Add(new OrderPreset
            {
                presetId = "PRESET-ORD-DEMO-0001",
                displayName = "ORD-DEMO-0001 (3 Items: SKU-0001, SKU-0008, SKU-0039)",
                orderId = "ORD-DEMO-0001",
                skuIds = new List<string> { "SKU-0001", "SKU-0008", "SKU-0039" },
                useScenarioSequence = false
            });

            orderPresets.Add(new OrderPreset
            {
                presetId = "PRESET-ORD-QUICK-2",
                displayName = "ORD-QUICK-0002 (2 Items: SKU-0001, SKU-0002)",
                orderId = "ORD-QUICK-0002",
                skuIds = new List<string> { "SKU-0001", "SKU-0002" },
                useScenarioSequence = false
            });
        }

        public void ResolveSceneReferences()
        {
            if (desktopPicker == null)
            {
                desktopPicker = FindAnyObjectByType<DesktopPickerController>(FindObjectsInactive.Include);
            }
            if (vrPlayerRig == null)
            {
                vrPlayerRig = FindAnyObjectByType<VRPlayerRig>(FindObjectsInactive.Include);
            }
            if (routeVisualizer == null)
            {
                routeVisualizer = FindAnyObjectByType<ScenarioRouteVisualizer>(FindObjectsInactive.Include);
                if (routeVisualizer == null)
                {
                    routeVisualizer = gameObject.AddComponent<ScenarioRouteVisualizer>();
                }
            }
        }

        private void SubscribeOrderEvents()
        {
            var om = OrderManager.Instance;
            if (om != null)
            {
                om.OnOrderStarted -= HandleOrderStarted;
                om.OnOrderStarted += HandleOrderStarted;

                om.OnItemPicked -= HandleItemPicked;
                om.OnItemPicked += HandleItemPicked;

                om.OnOrderCompleted -= HandleOrderCompleted;
                om.OnOrderCompleted += HandleOrderCompleted;
            }
        }

        private void UnsubscribeOrderEvents()
        {
            var om = OrderManager.Instance;
            if (om != null)
            {
                om.OnOrderStarted -= HandleOrderStarted;
                om.OnItemPicked -= HandleItemPicked;
                om.OnOrderCompleted -= HandleOrderCompleted;
            }
        }

        /// <summary>
        /// Step 1: Select scenario by index.
        /// </summary>
        public ScenarioDefinition SelectScenario(int index)
        {
            if (availableScenarios.Count == 0) InitializeCatalogAndPresets();
            selectedScenarioIndex = Mathf.Clamp(index, 0, availableScenarios.Count - 1);

            // Default comparison scenario to the paired scenario
            if (selectedScenarioIndex == 0) comparisonScenarioIndex = 1;
            else if (selectedScenarioIndex == 1) comparisonScenarioIndex = 0;
            else if (selectedScenarioIndex == 2) comparisonScenarioIndex = 3;
            else if (selectedScenarioIndex == 3) comparisonScenarioIndex = 2;

            SetStep(DemoStep.Step2_SelectOrder);
            SimulationErrorHandler.ReportInfo("DemoFlow", $"Step 1: Selected scenario '{SelectedScenario.scenarioName}' ({SelectedScenario.scenarioId}).");
            return SelectedScenario;
        }

        /// <summary>
        /// Step 2: Select order preset by index.
        /// </summary>
        public OrderPreset SelectOrderPreset(int index)
        {
            if (orderPresets.Count == 0) InitializeCatalogAndPresets();
            selectedOrderPresetIndex = Mathf.Clamp(index, 0, orderPresets.Count - 1);
            SetStep(DemoStep.Step3_StartDesktopOrVR);
            SimulationErrorHandler.ReportInfo("DemoFlow", $"Step 2: Selected order preset '{SelectedOrderPreset.displayName}'.");
            return SelectedOrderPreset;
        }

        /// <summary>
        /// Step 3: Start Desktop, VR, or Scripted Virtual Picker mode for the selected scenario & order.
        /// </summary>
        public bool StartSession(DemoExecutionMode mode, out string error)
        {
            error = null;
            ResolveSceneReferences();
            SubscribeOrderEvents();

            var scenario = SelectedScenario;
            if (scenario == null || !scenario.Validate(out error))
            {
                SimulationErrorHandler.ReportError("DemoFlow", $"Cannot start session: {error ?? "No scenario selected."}");
                return false;
            }

            var roster = VirtualDarkStore.Domain.Participants.ParticipantRoster.Instance;
            if (roster.ActiveParticipant == null)
            {
                if (roster.RegisteredCount == 0)
                {
                    roster.TryRegisterParticipant("Participant 01", out _, out _);
                }
                else
                {
                    error = "No active participant selected. Please select a registered participant in the Researcher Dashboard [F2] before starting.";
                    SimulationErrorHandler.ReportWarning("DemoFlow", error);
                    return false;
                }
            }

            var preset = SelectedOrderPreset;
            string orderId = (preset != null && !preset.useScenarioSequence && !string.IsNullOrWhiteSpace(preset.orderId))
                ? preset.orderId
                : scenario.orderId;
            IReadOnlyList<string> skuList = (preset != null && !preset.useScenarioSequence && preset.skuIds != null && preset.skuIds.Count > 0)
                ? preset.skuIds
                : scenario.skuSequence;

            activeMode = mode;

            var om = OrderManager.Instance;
            var logger = WorkEventLogger.Instance;
            var classifier = ActivityClassifier.Instance;

            if (om == null || logger == null || classifier == null)
            {
                error = "Core simulation managers (OrderManager / WorkEventLogger / ActivityClassifier) are missing.";
                SimulationErrorHandler.ReportError("DemoFlow", error);
                return false;
            }

            if (mode == DemoExecutionMode.ScriptedVirtualPicker)
            {
                var runScenario = scenario.Clone();
                runScenario.orderId = orderId;
                runScenario.skuSequence = new List<string>(skuList);

                try
                {
                    lastCompletedRun = ScenarioRunner.Run(runScenario, om, logger, classifier, 1.0f);
                    comparisonRunA = lastCompletedRun;
                    SetStep(DemoStep.Step6_ViewWorkStudyResults);
                    SimulationErrorHandler.ReportInfo("DemoFlow",
                        $"Completed scripted run '{runScenario.scenarioId}' ({lastCompletedRun.Results.TotalTaskTime:F2}s, {lastCompletedRun.Results.TotalDistance:F2}m).");
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    SimulationErrorHandler.ReportError("DemoFlow", $"Scripted run failed: {ex.Message}");
                    return false;
                }
            }

            // Interactive Desktop or VR Mode
            Vector3 startPos = scenario.startPosition;
            if (mode == DemoExecutionMode.Desktop)
            {
                if (vrPlayerRig != null) vrPlayerRig.gameObject.SetActive(false);
                if (desktopPicker != null)
                {
                    desktopPicker.gameObject.SetActive(true);
                    var cc = desktopPicker.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    desktopPicker.transform.position = startPos;
                    desktopPicker.transform.rotation = Quaternion.identity;
                    if (cc != null) cc.enabled = true;

                    classifier.SetReferences(logger, om, desktopPicker.transform, desktopPicker.GetComponent<PickerInteraction>());
                }
            }
            else if (mode == DemoExecutionMode.VR)
            {
                if (desktopPicker != null) desktopPicker.gameObject.SetActive(false);
                if (vrPlayerRig != null)
                {
                    vrPlayerRig.gameObject.SetActive(true);
                    vrPlayerRig.TeleportToSpawnPoint(startPos, Quaternion.identity);
                    classifier.SetVRReferences(logger, om, vrPlayerRig);
                }
            }

            logger.Clear();
            float startTime = Application.isPlaying ? Time.time : 0f;
            classifier.ResetState(startTime, startPos);

            liveRoutePoints.Clear();
            liveWaypointLabels.Clear();
            liveWaypointPositions.Clear();
            liveRoutePoints.Add(startPos);
            liveWaypointLabels.Add("START");
            liveWaypointPositions.Add(startPos);
            lastRecordedRoutePos = startPos;

            if (!om.TryStartSpecificOrder(orderId, skuList, out Order startedOrder, out error))
            {
                return false;
            }

            SetStep(DemoStep.Step4_PickProducts);
            UpdateTargetBeacon();
            SimulationErrorHandler.ReportInfo("DemoFlow", $"Step 3: Started {mode} mode for Order '{startedOrder.OrderId}' ({startedOrder.TotalItemCount} items).");
            return true;
        }

        /// <summary>
        /// Step 6: Open Work-Study Results UI and return the computed WorkStudyResults.
        /// </summary>
        public WorkStudyResults ViewWorkStudyResults(float samplingIntervalSeconds = 1.0f)
        {
            var logger = WorkEventLogger.Instance;
            var results = WorkStudyCalculator.CalculateFromLogger(logger, samplingIntervalSeconds);

            var resultsUI = VirtualDarkStore.UI.WorkStudyResultsUI.Instance;
            if (resultsUI != null)
            {
                resultsUI.ShowResults();
            }

            SetStep(DemoStep.Step6_ViewWorkStudyResults);
            return results;
        }

        /// <summary>
        /// Step 7: Compare active/completed scenario against another scenario and optionally render 3D routes.
        /// </summary>
        public ScenarioComparison CompareWithScenario(int otherScenarioIndex, out string error)
        {
            error = null;
            if (availableScenarios.Count == 0) InitializeCatalogAndPresets();
            comparisonScenarioIndex = Mathf.Clamp(otherScenarioIndex, 0, availableScenarios.Count - 1);

            var scnA = SelectedScenario;
            var scnB = availableScenarios[comparisonScenarioIndex];

            var om = OrderManager.Instance;
            var logger = WorkEventLogger.Instance;
            var classifier = ActivityClassifier.Instance;

            if (om == null || logger == null || classifier == null)
            {
                error = "Core simulation managers are missing.";
                SimulationErrorHandler.ReportError("DemoFlow", error);
                return null;
            }

            try
            {
                // Run both Scenario A and Scenario B deterministically if not already matching the exact order
                comparisonRunA = ScenarioRunner.Run(scnA, om, logger, classifier, 1.0f);
                comparisonRunB = ScenarioRunner.Run(scnB, om, logger, classifier, 1.0f);
                activeComparison = new ScenarioComparison(comparisonRunA, comparisonRunB);

                if (routeVisualizer != null)
                {
                    routeVisualizer.Clear();
                    routeVisualizer.ShowRoute(comparisonRunA, RouteMapRenderer.ColorA);
                    routeVisualizer.ShowRoute(comparisonRunB, RouteMapRenderer.ColorB);
                }

                SetStep(DemoStep.Step7_CompareAnotherScenario);
                OnComparisonReady?.Invoke(activeComparison);
                SimulationErrorHandler.ReportInfo("DemoFlow",
                    $"Step 7: Compared '{scnA.scenarioId}' ({comparisonRunA.Results.TotalTaskTime:F2}s) vs '{scnB.scenarioId}' ({comparisonRunB.Results.TotalTaskTime:F2}s).");
                return activeComparison;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                SimulationErrorHandler.ReportError("DemoFlow", $"Comparison failed: {ex.Message}");
                return null;
            }
        }

        private void HandleOrderStarted(Order order)
        {
            if (currentStep < DemoStep.Step4_PickProducts)
            {
                SetStep(DemoStep.Step4_PickProducts);
            }
            UpdateTargetBeacon();
        }

        private void HandleItemPicked(OrderItem item, bool allPicked)
        {
            Vector3 pos = GetActivePickerPosition();
            liveRoutePoints.Add(pos);
            if (item != null)
            {
                liveWaypointLabels.Add($"PICK {item.SkuId}");
                liveWaypointPositions.Add(pos);
            }

            if (allPicked)
            {
                SetStep(DemoStep.Step5_CompleteOrder);
            }
            UpdateTargetBeacon();
        }

        private void HandleOrderCompleted(Order order)
        {
            Vector3 pos = GetActivePickerPosition();
            liveRoutePoints.Add(pos);
            liveWaypointLabels.Add("PACK");
            liveWaypointPositions.Add(pos);

            var logger = WorkEventLogger.Instance;
            if (logger != null && SelectedScenario != null)
            {
                var recordedScenario = SelectedScenario.Clone();
                if (order != null)
                {
                    recordedScenario.orderId = order.OrderId;
                    recordedScenario.skuSequence.Clear();
                    foreach (var it in order.Items) recordedScenario.skuSequence.Add(it.SkuId);
                }

                lastCompletedRun = new ScenarioRunResult
                {
                    scenario = recordedScenario,
                    recordedAtLocal = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                    samplingIntervalSeconds = 1.0f
                };
                lastCompletedRun.routePoints.AddRange(liveRoutePoints);
                lastCompletedRun.waypointLabels.AddRange(liveWaypointLabels);
                lastCompletedRun.waypointPositions.AddRange(liveWaypointPositions);
                lastCompletedRun.events.AddRange(logger.LoggedEvents);
            }

            SetStep(DemoStep.Step6_ViewWorkStudyResults);
            UpdateTargetBeacon();
        }

        private void RecordLiveTrajectorySample()
        {
            var om = OrderManager.Instance;
            if (om == null || (!om.HasActiveOrder && !om.IsAwaitingPacking)) return;

            Vector3 pos = GetActivePickerPosition();
            if ((pos - lastRecordedRoutePos).sqrMagnitude >= 0.25f) // every 0.5m
            {
                liveRoutePoints.Add(pos);
                lastRecordedRoutePos = pos;
            }
        }

        private Vector3 GetActivePickerPosition()
        {
            if (activeMode == DemoExecutionMode.VR && vrPlayerRig != null)
            {
                return vrPlayerRig.FloorPosition;
            }
            if (desktopPicker != null)
            {
                return new Vector3(desktopPicker.transform.position.x, 0f, desktopPicker.transform.position.z);
            }
            return Vector3.zero;
        }

        private void EnsureTargetBeaconExists()
        {
            if (targetSlotBeacon != null) return;
            var existing = GameObject.Find("TargetSlotBeacon");
            if (existing != null)
            {
                targetSlotBeacon = existing.transform;
                return;
            }

            var beaconObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beaconObj.name = "TargetSlotBeacon";
            beaconObj.transform.SetParent(transform, false);
            beaconObj.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
            var col = beaconObj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            var renderer = beaconObj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader)
                {
                    color = new Color(0.1f, 0.95f, 1.0f, 1.0f),
                    enableInstancing = true
                };
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.1f, 0.95f, 1.0f, 1.0f));
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            targetSlotBeacon = beaconObj.transform;
            targetSlotBeacon.gameObject.SetActive(false);
        }

        private void UpdateTargetBeacon()
        {
            if (targetSlotBeacon == null) return;

            var om = OrderManager.Instance;
            if (om != null && om.HasActiveOrder && om.ActiveOrder?.CurrentTargetItem != null)
            {
                Vector3 pos = om.ActiveOrder.CurrentTargetItem.TargetWorldPosition;
                targetSlotBeacon.position = pos + new Vector3(0f, 0.28f, 0f);
                if (!targetSlotBeacon.gameObject.activeSelf) targetSlotBeacon.gameObject.SetActive(true);
            }
            else if (om != null && om.IsAwaitingPacking)
            {
                var station = FindAnyObjectByType<PackingStation>();
                Vector3 pos = station != null ? station.transform.position : new Vector3(0f, 0f, -9.5f);
                targetSlotBeacon.position = pos + new Vector3(0f, 1.65f, 0f);
                if (!targetSlotBeacon.gameObject.activeSelf) targetSlotBeacon.gameObject.SetActive(true);
            }
            else
            {
                if (targetSlotBeacon.gameObject.activeSelf) targetSlotBeacon.gameObject.SetActive(false);
            }
        }

        private void SetStep(DemoStep nextStep)
        {
            currentStep = nextStep;
            OnStepChanged?.Invoke(currentStep);
        }
    }
}
