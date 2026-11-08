using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Management;
using VirtualDarkStore.Player;
using VirtualDarkStore.Scenarios;
using VirtualDarkStore.VR;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.UI
{
    /// <summary>
    /// Screen-space simulation HUD and Final Demo Workflow controller panel.
    /// Renders reticle, interaction prompts, active pick checklist, target slot guidance,
    /// alert banners, and the 7-step interactive Final Demo control panel ([F1]).
    /// </summary>
    public class SimulationHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PickerInteraction pickerInteraction;
        [SerializeField] private DesktopPickerController playerController;
        [SerializeField] private VRPlayerRig vrPlayerRig;

        [Header("UI Settings")]
        [SerializeField] private float bannerDuration = 4.0f;
        [SerializeField] private bool showDemoPanel = true;

        [Header("Controlled Order Generation & Routing")]
        [SerializeField] private int currentGenerationSeed = 1001;
        [SerializeField] private int currentGenerationItemCount = 3;
        [SerializeField] private DuplicateSkuPolicy currentDuplicatePolicy = DuplicateSkuPolicy.DisallowDuplicates;
        private string lastRoutingSummary = string.Empty;

        [Header("Multi-Order Simulation Setup")]
        [SerializeField] private int simulationOrderCount = 5;
        [SerializeField] private int simulationItemCount = 8;
        [SerializeField] private int simulationSeed = 1001;
        private string orderCountStr = "5";
        private string itemCountStr = "8";
        private string seedStr = "1001";

        private string activeBannerMessage = string.Empty;
        private Color bannerColor = Color.white;
        private float bannerTimer = 0f;

        [Header("Next Order Information Panel")]
        [SerializeField] private bool showNextOrderPanel = true;
        private float currentNextOrderPanelHeight = 0f;
        private Vector2 nextOrderScrollPos = Vector2.zero;

        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle bodyStyle;
        private GUIStyle targetStyle;
        private GUIStyle completedItemStyle;
        private GUIStyle promptStyle;
        private GUIStyle bannerStyle;
        private GUIStyle bottomStyle;

        private GUIStyle nextOrderHeaderStyle;
        private GUIStyle nextOrderSubHeaderStyle;
        private GUIStyle nextOrderTableHeaderStyle;
        private GUIStyle nextOrderTableHeaderCenterStyle;
        private GUIStyle nextOrderTableAisleHeaderStyle;
        private GUIStyle nextOrderTableSlotHeaderStyle;
        private GUIStyle nextOrderRowNormalStyle;
        private GUIStyle nextOrderRowTargetStyle;
        private GUIStyle nextOrderRowFulfilledStyle;
        private GUIStyle nextOrderAisleValueStyle;
        private GUIStyle nextOrderSlotValueStyle;
        private GUIStyle nextOrderQtyCenterStyle;
        private GUIStyle nextOrderStatusPickStyle;
        private GUIStyle nextOrderStatusDoneStyle;
        private GUIStyle nextOrderStatusWaitStyle;

        private Texture2D panelBackground;
        private Texture2D bannerBackground;
        private Texture2D crosshairTexture;
        private Texture2D nextOrderBgTexture;
        private Texture2D nextOrderAccentOrange;
        private Texture2D nextOrderAccentCyan;
        private Texture2D nextOrderTableDivider;
        private bool stylesInitialized = false;

        public bool ShowNextOrderPanel
        {
            get => showNextOrderPanel;
            set => showNextOrderPanel = value;
        }

        public bool ShowDemoPanel
        {
            get => showDemoPanel;
            set => showDemoPanel = value;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (pickerInteraction == null) pickerInteraction = FindAnyObjectByType<PickerInteraction>(FindObjectsInactive.Include);
            if (playerController == null) playerController = FindAnyObjectByType<DesktopPickerController>(FindObjectsInactive.Include);
            if (vrPlayerRig == null) vrPlayerRig = FindAnyObjectByType<VRPlayerRig>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            var om = OrderManager.Instance;
            if (om != null) SubscribeOrderEvents(om);

            var err = SimulationErrorHandler.Instance;
            if (err != null)
            {
                err.OnAlertRaised -= HandleAlertRaised;
                err.OnAlertRaised += HandleAlertRaised;
            }
        }

        private void OnDisable()
        {
            var om = OrderManager.Instance;
            if (om != null) UnsubscribeOrderEvents(om);

            var err = SimulationErrorHandler.Instance;
            if (err != null)
            {
                err.OnAlertRaised -= HandleAlertRaised;
            }
        }

        private void Start()
        {
            ResolveReferences();
            var om = OrderManager.Instance;
            if (om != null)
            {
                SubscribeOrderEvents(om);
                if (om.ActiveOrder != null)
                {
                    ShowBanner($"Order {om.ActiveOrder.OrderId} Active. Pick items in sequence or press [F1] for Demo Controls.", new Color(0.3f, 0.85f, 1f));
                }
            }
        }

        private void SubscribeOrderEvents(OrderManager om)
        {
            om.OnOrderStarted -= HandleOrderStarted;
            om.OnOrderStarted += HandleOrderStarted;

            om.OnItemPicked -= HandleItemPicked;
            om.OnItemPicked += HandleItemPicked;

            om.OnPickFailed -= HandlePickFailed;
            om.OnPickFailed += HandlePickFailed;

            om.OnOrderCompleted -= HandleOrderCompleted;
            om.OnOrderCompleted += HandleOrderCompleted;
        }

        private void UnsubscribeOrderEvents(OrderManager om)
        {
            om.OnOrderStarted -= HandleOrderStarted;
            om.OnItemPicked -= HandleItemPicked;
            om.OnPickFailed -= HandlePickFailed;
            om.OnOrderCompleted -= HandleOrderCompleted;
        }

        private void Update()
        {
            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.deltaTime;
                if (bannerTimer <= 0f)
                {
                    activeBannerMessage = string.Empty;
                }
            }

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f1Key.wasPressedThisFrame) showDemoPanel = !showDemoPanel;
                if (Keyboard.current.gKey.wasPressedThisFrame) TriggerGenerateControlledOrder();
                if (Keyboard.current.oKey.wasPressedThisFrame) TriggerOptimizeRoute();
                if (Keyboard.current.f4Key.wasPressedThisFrame) TriggerRunOperationalComparison();
            }
#else
            if (Input.GetKeyDown(KeyCode.F1)) showDemoPanel = !showDemoPanel;
            if (Input.GetKeyDown(KeyCode.G)) TriggerGenerateControlledOrder();
            if (Input.GetKeyDown(KeyCode.O)) TriggerOptimizeRoute();
            if (Input.GetKeyDown(KeyCode.F4)) TriggerRunOperationalComparison();
#endif
        }

        public void TriggerGenerateControlledOrder()
        {
            var om = OrderManager.Instance;
            if (om == null) return;
            var config = new OrderGenerationConfig(currentGenerationSeed, currentGenerationItemCount, 1, currentDuplicatePolicy);
            if (om.TryDispatchGeneratedOrder(config, out var order, out var error))
            {
                ShowBanner($"[Phase 11] Generated Order {order.OrderId} (Seed {currentGenerationSeed}, {order.TotalItemCount} items)", new Color(0.2f, 0.9f, 0.5f));
            }
            else
            {
                ShowBanner($"Order Generation Failed: {error}", Color.red);
            }
        }

        public void TriggerGenerateMultiOrderSimulation()
        {
            var om = OrderManager.Instance;
            var store = StoreManager.Instance;
            if (om == null || store == null)
            {
                ShowBanner("Cannot start simulation: OrderManager or StoreManager missing.", Color.red);
                return;
            }

            if (!int.TryParse(orderCountStr, out int parsedOrders))
            {
                ShowBanner($"Validation Error: Orders input '{orderCountStr}' must be an integer.", Color.red);
                return;
            }

            if (!int.TryParse(itemCountStr, out int parsedItems))
            {
                ShowBanner($"Validation Error: Items input '{itemCountStr}' must be an integer.", Color.red);
                return;
            }

            if (!int.TryParse(seedStr, out int parsedSeed))
            {
                ShowBanner($"Validation Error: Seed input '{seedStr}' must be an integer.", Color.red);
                return;
            }

            simulationOrderCount = parsedOrders;
            simulationItemCount = parsedItems;
            simulationSeed = parsedSeed;

            var config = new OrderGenerationConfig(
                seed: simulationSeed,
                itemCount: simulationItemCount,
                orderCount: simulationOrderCount,
                duplicatePolicy: currentDuplicatePolicy);

            if (!config.Validate(store, out string valError))
            {
                ShowBanner($"Validation Error: {valError}", Color.red);
                return;
            }

            if (om.TryStartMultiOrderSimulation(config, out var sim, out string err))
            {
                int totalItems = sim.TotalOrdersCount * config.ItemCount;
                ShowBanner($"[SIMULATION] Started: {sim.TotalOrdersCount} Orders × {config.ItemCount} Items ({totalItems} total items, Seed {config.Seed}). Order 1/{sim.TotalOrdersCount} Dispatched.", new Color(0.2f, 1.0f, 0.5f));
            }
            else
            {
                ShowBanner($"Simulation Failed: {err}", Color.red);
            }
        }

        public void TriggerOptimizeRoute()
        {
            var om = OrderManager.Instance;
            if (om == null || om.ActiveOrder == null)
            {
                ShowBanner("No active order to optimize.", Color.yellow);
                return;
            }

            var result = om.OptimizeActiveOrderRoute();
            lastRoutingSummary = result.StatusMessage;
            if (result.Success)
            {
                ShowBanner($"[Phase 12] {result.StatusMessage}", new Color(0.3f, 0.9f, 1f));
            }
            else
            {
                ShowBanner($"Routing: {result.StatusMessage}", Color.yellow);
            }
        }

        public void TriggerRunOperationalComparison()
        {
            var om = OrderManager.Instance;
            var logger = WorkEventLogger.Instance;
            var classifier = ActivityClassifier.Instance;
            var store = StoreManager.Instance;

            if (om == null || logger == null || classifier == null || store == null)
            {
                ShowBanner("Cannot run study: Core simulation managers missing.", Color.red);
                return;
            }

            try
            {
                // Controlled cross-aisle order: Aisle 1 -> Aisle 3 -> Aisle 1 -> Aisle 4
                var baselineSkus = new List<string> { "SKU-0001", "SKU-0039", "SKU-0008", "SKU-0066" };

                var runA = VirtualDarkStore.Scenarios.OperationalCycleRunner.RunCycle(
                    skuSequence: baselineSkus,
                    orderId: "ORD-STUDY-001",
                    isOptimized: false,
                    algorithmicCost: 65.2f,
                    orderManager: om,
                    logger: logger,
                    classifier: classifier);

                var stops = new List<Vector3>();
                foreach (var sku in baselineSkus)
                {
                    if (store.TryResolveSkuLocation(sku, out _, out var pos)) stops.Add(pos);
                }

                var matrix = VirtualDarkStore.Routing.TravelCostMatrix.Build(new Vector3(-4.0f, 0f, -8.8f), stops, new Vector3(2.0f, 0f, -8.8f));
                var optResult = new VirtualDarkStore.Routing.OrToolsRouteOptimizer().OptimizeRoute(matrix, baselineSkus);
                var optimizedSkus = optResult.Success ? optResult.SkuSequence : baselineSkus;

                var runB = VirtualDarkStore.Scenarios.OperationalCycleRunner.RunCycle(
                    skuSequence: optimizedSkus,
                    orderId: "ORD-STUDY-001",
                    isOptimized: true,
                    algorithmicCost: optResult.TotalCost,
                    orderManager: om,
                    logger: logger,
                    classifier: classifier);

                var comp = new VirtualDarkStore.Scenarios.OperationalComparison(runA, runB);
                string evidenceDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "docs", "evidence", "phase14"));
                comp.ExportReportBundle(evidenceDir, "baseline_vs_optimized_report");

                float savedMeters = runA.measuredPlayerDistance - runB.measuredPlayerDistance;
                float savedSec = runA.totalCycleDuration - runB.totalCycleDuration;
                ShowBanner($"[Phase 14] Study Complete! Saved {savedSec:F1}s ({savedMeters:F1}m). Report in docs/evidence/phase14/", new Color(0.2f, 1.0f, 0.4f));
            }
            catch (Exception ex)
            {
                ShowBanner($"Comparison failed: {ex.Message}", Color.red);
            }
        }

        public void ShowBanner(string message, Color color)
        {
            activeBannerMessage = message;
            bannerColor = color;
            bannerTimer = bannerDuration;
        }

        private void HandleAlertRaised(SimulationAlert alert)
        {
            if (alert.Severity == SimulationAlertSeverity.Error)
            {
                ShowBanner($"⚠ ERROR: {alert.Message}", new Color(1.0f, 0.3f, 0.3f));
            }
        }

        private void HandleOrderStarted(Order order)
        {
            ShowBanner($"NEW ORDER [{order.OrderId}] - {order.TotalItemCount} items dispatched", new Color(0.2f, 0.85f, 1.0f));
        }

        private void HandleItemPicked(OrderItem item, bool allPicked)
        {
            if (allPicked)
            {
                ShowBanner($"ALL ITEMS PICKED! Proceed to Packing Station [PACK-01] at store front.", new Color(0.3f, 1.0f, 0.4f));
            }
            else
            {
                ShowBanner($"✓ PICKED: {item.SkuName} ({item.SkuId})", new Color(0.3f, 1.0f, 0.4f));
            }
        }

        private void HandlePickFailed(string expected, string attempted, string reason)
        {
            ShowBanner($"✗ WRONG ITEM! Expected '{expected}', attempted '{attempted}'", new Color(1.0f, 0.25f, 0.25f));
        }

        private void HandleOrderCompleted(Order order)
        {
            ShowBanner($"★ ORDER [{order.OrderId}] COMPLETED & PACKED! View Study Report [Tab] or Compare [F3].", new Color(1.0f, 0.85f, 0.2f));
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            panelBackground = MakeColorTexture(new Color(0.07f, 0.09f, 0.14f, 0.90f));
            bannerBackground = MakeColorTexture(new Color(0.04f, 0.05f, 0.08f, 0.94f));
            crosshairTexture = MakeColorTexture(new Color(1f, 1f, 1f, 0.85f));

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.80f, 1.0f) },
                alignment = TextAnchor.UpperLeft
            };

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.90f, 0.95f) },
                alignment = TextAnchor.UpperLeft
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white },
                alignment = TextAnchor.UpperLeft
            };

            targetStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.90f, 0.30f) },
                alignment = TextAnchor.UpperLeft
            };

            completedItemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.40f, 0.95f, 0.50f) },
                alignment = TextAnchor.UpperLeft
            };

            promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan },
                alignment = TextAnchor.MiddleCenter
            };

            bannerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            bottomStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.75f, 0.80f, 0.86f) },
                alignment = TextAnchor.MiddleCenter
            };

            // Dedicated Next Order Information Panel Styles (Top-Centre)
            nextOrderBgTexture = MakeColorTexture(new Color(0.05f, 0.07f, 0.12f, 0.94f));
            nextOrderAccentOrange = MakeColorTexture(new Color(0.95f, 0.60f, 0.15f, 0.95f)); // Amber/Safety Orange
            nextOrderAccentCyan = MakeColorTexture(new Color(0.20f, 0.75f, 1.0f, 0.95f)); // Bright Cyan
            nextOrderTableDivider = MakeColorTexture(new Color(0.20f, 0.28f, 0.38f, 0.70f)); // Subtle steel blue

            nextOrderHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.85f, 1.0f) },
                alignment = TextAnchor.UpperLeft
            };

            nextOrderSubHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.88f, 0.25f) },
                alignment = TextAnchor.UpperLeft
            };

            nextOrderTableHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.72f, 0.78f, 0.85f) },
                alignment = TextAnchor.MiddleLeft
            };

            nextOrderTableHeaderCenterStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.72f, 0.78f, 0.85f) },
                alignment = TextAnchor.MiddleCenter
            };

            nextOrderTableAisleHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.88f, 0.25f) },
                alignment = TextAnchor.MiddleCenter
            };

            nextOrderTableSlotHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.40f, 0.85f, 1.0f) },
                alignment = TextAnchor.MiddleCenter
            };

            nextOrderRowNormalStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.90f, 0.93f, 0.96f) },
                alignment = TextAnchor.MiddleLeft
            };

            nextOrderRowTargetStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.95f, 0.40f) },
                alignment = TextAnchor.MiddleLeft
            };

            nextOrderRowFulfilledStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.45f, 0.88f, 0.55f) },
                alignment = TextAnchor.MiddleLeft
            };

            nextOrderAisleValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.90f, 0.25f) },
                alignment = TextAnchor.MiddleCenter
            };

            nextOrderSlotValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.40f, 0.85f, 1.0f) },
                alignment = TextAnchor.MiddleCenter
            };

            nextOrderQtyCenterStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            nextOrderStatusPickStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.90f, 0.30f) },
                alignment = TextAnchor.MiddleCenter
            };

            nextOrderStatusDoneStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.90f, 0.55f) },
                alignment = TextAnchor.MiddleCenter
            };

            nextOrderStatusWaitStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.60f, 0.65f, 0.72f) },
                alignment = TextAnchor.MiddleCenter
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            // Never render 2D desktop screen-space OnGUI into the VR headset lenses
            if (Camera.current != null && Camera.current.stereoEnabled)
            {
                return;
            }

            InitStyles();

            var om = OrderManager.Instance;
            ResolveReferences();

            bool isVrActive = vrPlayerRig != null && vrPlayerRig.gameObject.activeInHierarchy;

            // 1. Draw Reticle in Center Screen (Desktop only; VR uses 3D in-VR reticle)
            if (!isVrActive)
            {
                DrawCrosshair();
            }

            // 2. Draw Interaction Prompt below Reticle (Desktop only; VR uses 3D in-VR badge)
            if (!isVrActive)
            {
                DrawInteractionPrompt();
            }

            // 3. Draw Active Order Terminal (Top-Left)
            DrawOrderTerminal(om);

            // 4. Draw Dedicated Next Order Information Panel (Top-Center; on VR this is rendered on the wrist tablet)
            if (showNextOrderPanel && !isVrActive)
            {
                DrawNextOrderPanel(om);
            }
            else
            {
                currentNextOrderPanelHeight = 0f;
            }

            // 5. Draw 7-Step Final Demo Workflow Panel (Top-Right, toggled with [F1])
            if (showDemoPanel)
            {
                DrawDemoWorkflowPanel(om);
            }

            // 6. Draw Feedback Banner (Top-Center, below Next Order Panel)
            DrawBanner();

            // 7. Draw Bottom Controls Guide
            DrawBottomBar();
        }

        private void DrawCrosshair()
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float size = 5f;
            float thickness = 2f;

            var activeInt = GetActiveInteraction();
            Color col = (activeInt != null && (activeInt.HoveredItem != null || activeInt.HoveredPackingStation != null))
                ? Color.green
                : new Color(1f, 1f, 1f, 0.75f);

            GUI.color = col;
            GUI.DrawTexture(new Rect(cx - size, cy - thickness * 0.5f, size * 2f, thickness), crosshairTexture);
            GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - size, thickness, size * 2f), crosshairTexture);
            GUI.color = Color.white;
        }

        private VirtualDarkStore.Interaction.IPickerInteraction GetActiveInteraction()
        {
            if (vrPlayerRig != null && vrPlayerRig.gameObject.activeInHierarchy && vrPlayerRig.PickerInteraction != null)
            {
                return vrPlayerRig.PickerInteraction;
            }
            if (pickerInteraction != null && pickerInteraction.gameObject.activeInHierarchy)
            {
                return pickerInteraction;
            }
            return FindAnyObjectByType<VRPickerInteraction>();
        }

        private Vector3 GetActivePlayerPosition()
        {
            if (vrPlayerRig != null && vrPlayerRig.gameObject.activeInHierarchy)
            {
                return vrPlayerRig.FloorPosition;
            }
            if (playerController != null)
            {
                return playerController.transform.position;
            }
            return Vector3.zero;
        }

        private void DrawInteractionPrompt()
        {
            var activeInt = GetActiveInteraction();
            if (activeInt == null) return;
            string prompt = activeInt.CurrentPrompt;
            if (string.IsNullOrEmpty(prompt)) return;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f + 25f;
            float width = 460f;
            float height = 30f;

            Rect bgRect = new Rect(cx - width * 0.5f, cy, width, height);
            GUI.DrawTexture(bgRect, panelBackground);

            promptStyle.normal.textColor = activeInt.HoveredItem != null ? new Color(0.4f, 1.0f, 0.5f) : Color.cyan;
            GUI.Label(bgRect, prompt, promptStyle);
        }

        private void DrawOrderTerminal(OrderManager om)
        {
            float panelWidth = 360f;
            float panelHeight = 290f;
            Rect panelRect = new Rect(14f, 14f, panelWidth, panelHeight);

            GUI.DrawTexture(panelRect, panelBackground);

            GUILayout.BeginArea(new Rect(24f, 22f, panelWidth - 20f, panelHeight - 16f));

            var demo = DemoFlowController.Instance;
            var activePart = VirtualDarkStore.Domain.Participants.ParticipantRoster.Instance.ActiveParticipant;
            string partBadge = activePart != null ? $"{activePart.SlotId}:{activePart.ParticipantName}" : "NO PARTICIPANT [F2]";
            string modeBadge = (vrPlayerRig != null && vrPlayerRig.gameObject.activeInHierarchy) ? "VR RIG" : "DESKTOP";
            string scnBadge = (demo != null && demo.SelectedScenario != null) ? demo.SelectedScenario.scenarioId : "DEFAULT";

            GUILayout.Label($"DARK STORE TERMINAL [{modeBadge} | {scnBadge} | {partBadge}]", headerStyle);

            var classifier = ActivityClassifier.Instance;
            var logger = WorkEventLogger.Instance;
            if (classifier != null)
            {
                GUILayout.Label($"Activity: [{classifier.CurrentActivity}]  |  Events: {(logger != null ? logger.EventCount : 0)}  |  Dist: {classifier.CumulativeTotalDistance:F1}m", bodyStyle);
            }

            var opMgr = VirtualDarkStore.Management.OperationalCycleManager.Instance;
            if (opMgr != null)
            {
                GUILayout.Label($"Cycle Stage: [{opMgr.CurrentStage}] {(opMgr.UseOptimizedRouting ? "(OPT ROUTE)" : "(BASELINE)")}", subHeaderStyle);
            }

            GUILayout.Space(4);

            if (om == null || om.ActiveOrder == null)
            {
                GUILayout.Label("No active order dispatched.", bodyStyle);
                GUILayout.Label("Press [N] or use Demo Panel [F1] to start an order.", bodyStyle);
                GUILayout.EndArea();
                return;
            }

            var order = om.ActiveOrder;
            if (om.HasActiveSimulation && om.ActiveSimulation != null)
            {
                var sim = om.ActiveSimulation;
                GUILayout.Label($"SIMULATION: {sim.SimulationId}", subHeaderStyle);
                GUILayout.Label($"Progress: Order {sim.CurrentOrderIndex + 1}/{sim.TotalOrdersCount} ({sim.CompletedOrdersCount} completed) | {sim.Config.ItemCount} items/order", targetStyle);
            }
            GUILayout.Label($"Order: {order.OrderId}  |  Status: {order.Status.ToString().ToUpper()}", subHeaderStyle);

            if (order.Status == OrderStatus.InProgress)
            {
                var target = order.CurrentTargetItem;
                if (target != null)
                {
                    GUILayout.Label($"Target ({order.CurrentItemIndex + 1}/{order.TotalItemCount}): {target.SkuName}", targetStyle);
                    GUILayout.Label($"SKU: {target.SkuId}  |  Slot: {target.TargetSlotId}", bodyStyle);

                    Vector3 playerPos = GetActivePlayerPosition();
                    if (target.TargetWorldPosition != Vector3.zero)
                    {
                        float dist = Vector3.Distance(playerPos, target.TargetWorldPosition);
                        GUILayout.Label($"Distance to Target Bin: {dist:F1} m", bodyStyle);
                    }
                }
            }
            else if (order.Status == OrderStatus.AwaitingPacking)
            {
                Vector3 playerPos = GetActivePlayerPosition();
                float packDist = Vector3.Distance(playerPos, new Vector3(0f, 0f, -9.5f));
                GUILayout.Label("ALL ITEMS PICKED! DELIVER TO PACKING BENCH", targetStyle);
                GUILayout.Label($"Target: [PACK-01] Dispatch Bench ({packDist:F1} m)", bodyStyle);
            }
            else if (order.Status == OrderStatus.Completed)
            {
                GUILayout.Label("ORDER COMPLETED & PACKED!", targetStyle);
                GUILayout.Label($"Orders Fulfilled: {om.CompletedOrdersCount} | Press [Tab] for Report", bodyStyle);
            }

            GUILayout.Space(4);
            GUILayout.Label("Order Pick List:", subHeaderStyle);
            for (int i = 0; i < order.Items.Count; i++)
            {
                var it = order.Items[i];
                if (it.IsFulfilled)
                {
                    GUILayout.Label($"  ✓ {i + 1}. {it.SkuId} - {it.SkuName} ({it.TargetSlotId})", completedItemStyle);
                }
                else if (i == order.CurrentItemIndex && order.Status == OrderStatus.InProgress)
                {
                    GUILayout.Label($"  ▶ {i + 1}. {it.SkuId} - {it.SkuName} ({it.TargetSlotId})", targetStyle);
                }
                else
                {
                    GUILayout.Label($"  ○ {i + 1}. {it.SkuId} - {it.SkuName} ({it.TargetSlotId})", bodyStyle);
                }
            }

            GUILayout.EndArea();
        }

        private void DrawDemoWorkflowPanel(OrderManager om)
        {
            var demo = DemoFlowController.Instance;
            if (demo == null) return;

            float panelWidth = 370f;
            float panelHeight = 445f;
            float x = Screen.width - panelWidth - 14f;
            float y = 14f;

            GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), panelBackground);
            GUILayout.BeginArea(new Rect(x + 12f, y + 10f, panelWidth - 24f, panelHeight - 20f));

            GUILayout.BeginHorizontal();
            GUILayout.Label($"FINAL DEMO WORKFLOW (Step {(int)demo.CurrentStep}/7) [F1]", headerStyle);
            if (GUILayout.Button("Participants [F2]", GUILayout.Width(115f)))
            {
                var dash = ResearcherDashboardUI.Instance;
                if (dash == null)
                {
                    var dashObj = new GameObject("[ResearcherDashboardUI]");
                    dash = dashObj.AddComponent<ResearcherDashboardUI>();
                }
                dash.ToggleVisibility();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            // Step 1: Select Scenario
            var scn = demo.SelectedScenario;
            GUILayout.Label($"1. Scenario: {(scn != null ? scn.scenarioName : "None")}", subHeaderStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("< Prev Scn", GUILayout.Width(85f)))
            {
                int prev = (demo.SelectedScenarioIndex - 1 + demo.AvailableScenarios.Count) % demo.AvailableScenarios.Count;
                demo.SelectScenario(prev);
            }
            if (GUILayout.Button("Next Scn >", GUILayout.Width(85f)))
            {
                int next = (demo.SelectedScenarioIndex + 1) % demo.AvailableScenarios.Count;
                demo.SelectScenario(next);
            }
            GUILayout.Label(scn != null ? $"[{string.Join(",", scn.skuSequence)}]" : "", bodyStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            // Step 2: Select Order
            var preset = demo.SelectedOrderPreset;
            GUILayout.Label($"2. Order: {(preset != null ? preset.displayName : "Default")}", subHeaderStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("< Prev Ord", GUILayout.Width(85f)))
            {
                int prev = (demo.SelectedOrderPresetIndex - 1 + demo.OrderPresets.Count) % demo.OrderPresets.Count;
                demo.SelectOrderPreset(prev);
            }
            if (GUILayout.Button("Next Ord >", GUILayout.Width(85f)))
            {
                int next = (demo.SelectedOrderPresetIndex + 1) % demo.OrderPresets.Count;
                demo.SelectOrderPreset(next);
            }
            GUILayout.EndHorizontal();

            // SIMULATION SETUP (Configurable Multi-Order Simulation)
            GUILayout.Space(2);
            GUILayout.Label("SIMULATION SETUP", subHeaderStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Orders:", GUILayout.Width(46f));
            if (GUILayout.Button("-", GUILayout.Width(18f)))
            {
                simulationOrderCount = Mathf.Max(1, simulationOrderCount - 1);
                orderCountStr = simulationOrderCount.ToString();
            }
            orderCountStr = GUILayout.TextField(orderCountStr, GUILayout.Width(30f));
            if (int.TryParse(orderCountStr, out int ocVal) && ocVal > 0) simulationOrderCount = ocVal;
            if (GUILayout.Button("+", GUILayout.Width(18f)))
            {
                simulationOrderCount++;
                orderCountStr = simulationOrderCount.ToString();
            }

            GUILayout.Label("Items:", GUILayout.Width(38f));
            if (GUILayout.Button("-", GUILayout.Width(18f)))
            {
                simulationItemCount = Mathf.Max(1, simulationItemCount - 1);
                itemCountStr = simulationItemCount.ToString();
            }
            itemCountStr = GUILayout.TextField(itemCountStr, GUILayout.Width(30f));
            if (int.TryParse(itemCountStr, out int icVal) && icVal > 0) simulationItemCount = icVal;
            if (GUILayout.Button("+", GUILayout.Width(18f)))
            {
                simulationItemCount++;
                itemCountStr = simulationItemCount.ToString();
            }

            GUILayout.Label("Seed:", GUILayout.Width(36f));
            seedStr = GUILayout.TextField(seedStr, GUILayout.Width(44f));
            if (int.TryParse(seedStr, out int sdVal)) simulationSeed = sdVal;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Simulation", GUILayout.Width(152f)))
            {
                TriggerGenerateMultiOrderSimulation();
            }
            if (GUILayout.Button("Opt [O]", GUILayout.Width(58f)))
            {
                TriggerOptimizeRoute();
            }
            if (GUILayout.Button("1-Ord [G]", GUILayout.Width(62f)))
            {
                TriggerGenerateControlledOrder();
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(lastRoutingSummary))
            {
                GUILayout.Label(lastRoutingSummary, bodyStyle);
            }

            GUILayout.Space(3);

            // Step 3: Start Desktop or VR
            GUILayout.Label("3. Start Mode (Resets Store & Starts Order):", subHeaderStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Desktop", GUILayout.Width(108f)))
            {
                demo.StartSession(DemoExecutionMode.Desktop, out _);
            }
            if (GUILayout.Button("Start VR Rig", GUILayout.Width(100f)))
            {
                demo.StartSession(DemoExecutionMode.VR, out _);
            }
            if (GUILayout.Button("Auto-Run Scn", GUILayout.Width(115f)))
            {
                if (demo.StartSession(DemoExecutionMode.ScriptedVirtualPicker, out _))
                {
                    demo.ViewWorkStudyResults();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            // Step 4 & 5 Status
            int pickedCount = 0;
            int totalCount = 0;
            if (om != null && om.ActiveOrder != null)
            {
                totalCount = om.ActiveOrder.TotalItemCount;
                foreach (var it in om.ActiveOrder.Items) if (it.IsFulfilled) pickedCount++;
            }
            GUILayout.Label($"4. Pick Products: {pickedCount} / {totalCount} items picked (Use [E]/LMB or VR Trigger)", bodyStyle);
            string packState = (om != null && om.ActiveOrder != null && om.ActiveOrder.Status == OrderStatus.Completed)
                ? "✓ Completed"
                : (om != null && om.IsAwaitingPacking ? "▶ Ready at PACK-01" : "Awaiting picks");
            GUILayout.Label($"5. Complete Order at Packing Bench: {packState}", bodyStyle);

            GUILayout.Space(4);

            // Step 6 & 7 Buttons
            GUILayout.Label("6 & 7. Work-Study Report & Scenario Comparison:", subHeaderStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("6. View Report [Tab]", GUILayout.Width(160f)))
            {
                demo.ViewWorkStudyResults();
            }
            if (GUILayout.Button("7. Compare Scn [F3]", GUILayout.Width(165f)))
            {
                var cmpUI = FindAnyObjectByType<ScenarioComparisonUI>();
                if (cmpUI != null) cmpUI.OpenAndCompareSelected();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            if (GUILayout.Button("Phase 14: Run Baseline vs Opt Study [F4]", GUILayout.Width(340f)))
            {
                TriggerRunOperationalComparison();
            }

            GUILayout.EndArea();
        }

        private void DrawBanner()
        {
            if (string.IsNullOrEmpty(activeBannerMessage) || bannerTimer <= 0f) return;

            float width = 620f;
            float height = 42f;
            float x = (Screen.width - width) * 0.5f;
            float y = (currentNextOrderPanelHeight > 0f) ? (12f + currentNextOrderPanelHeight + 6f) : 16f;

            Rect bannerRect = new Rect(x, y, width, height);
            GUI.DrawTexture(bannerRect, bannerBackground);

            bannerStyle.normal.textColor = bannerColor;
            GUI.Label(bannerRect, activeBannerMessage, bannerStyle);
        }

        /// <summary>
        /// Dedicated Prominent Next-Order Information Panel positioned at the top-centre of the screen.
        /// Displays Order ID, total item count, and a structured product table with SKU names, quantities,
        /// and dynamically resolved aisle and rack/slot positions.
        /// </summary>
        private void DrawNextOrderPanel(OrderManager om)
        {
            var nextOrder = ResolveNextPendingOrder(om, out string title, out string subtitle, out bool isQueued);

            float panelWidth = Mathf.Clamp(Screen.width * 0.42f, 580f, 660f);
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = 12f;

            if (nextOrder == null || nextOrder.Items == null || nextOrder.Items.Count == 0)
            {
                // Compact "No Upcoming Orders" card
                float emptyHeight = 64f;
                currentNextOrderPanelHeight = emptyHeight;
                Rect cardRect = new Rect(panelX, panelY, panelWidth, emptyHeight);
                GUI.DrawTexture(cardRect, nextOrderBgTexture);
                GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, 3f), nextOrderAccentCyan);

                GUILayout.BeginArea(new Rect(panelX + 16f, panelY + 10f, panelWidth - 32f, emptyHeight - 16f));
                GUILayout.Label(title, nextOrderHeaderStyle);
                GUILayout.Label(subtitle, nextOrderSubHeaderStyle);
                GUILayout.EndArea();
                return;
            }

            int itemCount = nextOrder.Items.Count;
            float calculatedHeight = 92f + itemCount * 22f;
            float panelHeight = Mathf.Min(240f, calculatedHeight);
            currentNextOrderPanelHeight = panelHeight;

            Rect fullPanelRect = new Rect(panelX, panelY, panelWidth, panelHeight);
            GUI.DrawTexture(fullPanelRect, nextOrderBgTexture);

            // Prominent top accent line: Orange for active picking order, Cyan for queued/upcoming order
            Texture2D accentTexture = isQueued ? nextOrderAccentCyan : nextOrderAccentOrange;
            GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, 3f), accentTexture);

            GUILayout.BeginArea(new Rect(panelX + 16f, panelY + 10f, panelWidth - 32f, panelHeight - 16f));

            // 1. Order ID Header
            GUILayout.Label(title, nextOrderHeaderStyle);

            // 2. Subheader (Total Items & Queue Info)
            GUILayout.Label(subtitle, nextOrderSubHeaderStyle);

            GUILayout.Space(4);

            // 3. Table Column Headers
            float tableInnerWidth = panelWidth - 32f;
            float colAisleWidth = 65f;
            float colSlotWidth = 135f;
            float colQtyWidth = 40f;
            float colStatusWidth = 55f;
            float colProductWidth = tableInnerWidth - (colAisleWidth + colSlotWidth + colQtyWidth + colStatusWidth + 18f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("PRODUCT", nextOrderTableHeaderStyle, GUILayout.Width(colProductWidth));
            GUILayout.Label("QTY", nextOrderTableHeaderCenterStyle, GUILayout.Width(colQtyWidth));
            GUILayout.Label("AISLE", nextOrderTableAisleHeaderStyle, GUILayout.Width(colAisleWidth));
            GUILayout.Label("RACK / SLOT", nextOrderTableSlotHeaderStyle, GUILayout.Width(colSlotWidth));
            GUILayout.Label("STATUS", nextOrderTableHeaderCenterStyle, GUILayout.Width(colStatusWidth));
            GUILayout.EndHorizontal();

            // Table header underline divider
            if (nextOrderTableDivider != null)
            {
                Rect divRect = GUILayoutUtility.GetRect(tableInnerWidth, 1f);
                GUI.DrawTexture(divRect, nextOrderTableDivider);
            }
            GUILayout.Space(2);

            // 4. Product Rows with ScrollView
            float tableAreaHeight = panelHeight - 88f;
            nextOrderScrollPos = GUILayout.BeginScrollView(nextOrderScrollPos, GUILayout.Height(tableAreaHeight));

            for (int i = 0; i < nextOrder.Items.Count; i++)
            {
                var it = nextOrder.Items[i];
                ResolveProductLocation(it, out string aisleStr, out string rackSlotStr);

                bool isCurrentTarget = (om != null && nextOrder == om.ActiveOrder && i == nextOrder.CurrentItemIndex && nextOrder.Status == OrderStatus.InProgress);
                bool isFulfilled = it.IsFulfilled;

                GUIStyle rowProductStyle = isCurrentTarget ? nextOrderRowTargetStyle : (isFulfilled ? nextOrderRowFulfilledStyle : nextOrderRowNormalStyle);
                string prefix = isFulfilled ? "✓" : (isCurrentTarget ? "▶" : "○");
                string statusText = isFulfilled ? "DONE" : (isCurrentTarget ? "PICK" : "WAIT");
                GUIStyle statusStyle = isFulfilled ? nextOrderStatusDoneStyle : (isCurrentTarget ? nextOrderStatusPickStyle : nextOrderStatusWaitStyle);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{prefix} {it.SkuName}", rowProductStyle, GUILayout.Width(colProductWidth));
                GUILayout.Label($"{it.RequiredQuantity}", nextOrderQtyCenterStyle, GUILayout.Width(colQtyWidth));
                GUILayout.Label(aisleStr, nextOrderAisleValueStyle, GUILayout.Width(colAisleWidth));
                GUILayout.Label(rackSlotStr, nextOrderSlotValueStyle, GUILayout.Width(colSlotWidth));
                GUILayout.Label(statusText, statusStyle, GUILayout.Width(colStatusWidth));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Dynamically resolves the upcoming/pending order to present on the prominent Next Order panel.
        /// Surfaces active order details during picking progression, advances to queued simulation orders
        /// upon packing completion, and reports "No Upcoming Orders" when finished.
        /// </summary>
        public static Order ResolveNextPendingOrder(OrderManager om, out string title, out string subtitle, out bool isQueuedUpcoming)
        {
            title = "NEXT ORDER | NO UPCOMING ORDERS";
            subtitle = "No orders currently pending or dispatched.";
            isQueuedUpcoming = false;

            if (om == null)
            {
                subtitle = "OrderManager is not active.";
                return null;
            }

            // Case 1: Order is currently active (being fulfilled or ready for packing)
            if (om.ActiveOrder != null && om.ActiveOrder.Status != OrderStatus.Completed)
            {
                var active = om.ActiveOrder;
                title = $"NEXT ORDER | {active.OrderId}";

                if (om.HasActiveSimulation && om.ActiveSimulation != null)
                {
                    var sim = om.ActiveSimulation;
                    int orderNum = sim.CurrentOrderIndex + 1;
                    subtitle = $"TOTAL ITEMS: {active.TotalItemCount}  •  SIMULATION: ORDER {orderNum} OF {sim.TotalOrdersCount}";
                    if (sim.HasNextOrder)
                    {
                        subtitle += $"  (Queue: {sim.Orders[sim.CurrentOrderIndex + 1].OrderId})";
                    }
                }
                else
                {
                    subtitle = $"TOTAL ITEMS: {active.TotalItemCount}  •  STATUS: {active.Status.ToString().ToUpper()}";
                }
                return active;
            }

            // Case 2: Multi-Order Simulation is active, active order is completed, but next order exists in queue
            if (om.HasActiveSimulation && om.ActiveSimulation != null)
            {
                var sim = om.ActiveSimulation;
                if (sim.HasNextOrder)
                {
                    int nextIdx = sim.CurrentOrderIndex + 1;
                    var queued = sim.Orders[nextIdx];
                    title = $"NEXT ORDER | {queued.OrderId}";
                    subtitle = $"TOTAL ITEMS: {queued.TotalItemCount}  •  UPCOMING: ORDER {nextIdx + 1} OF {sim.TotalOrdersCount}";
                    isQueuedUpcoming = true;
                    return queued;
                }
                else
                {
                    title = "NEXT ORDER | NO UPCOMING ORDERS";
                    subtitle = $"Simulation {sim.SimulationId} Completed — All {sim.TotalOrdersCount} Orders Fulfilled.";
                    return null;
                }
            }

            // Case 3: No active order and no simulation
            title = "NEXT ORDER | NO UPCOMING ORDERS";
            subtitle = om.CompletedOrdersCount > 0
                ? $"All orders fulfilled ({om.CompletedOrdersCount} completed). Press [G] or use Demo Panel [F1] to dispatch."
                : "Press [G] or use Demo Panel [F1] to dispatch an order.";
            return null;
        }

        /// <summary>
        /// Dynamically resolves the aisle and rack/slot string for a product item, honoring live StoreManager slot assignments
        /// (which automatically reflect optimized layout or relocation changes) and falling back to parsed StoreLocation data.
        /// </summary>
        public static void ResolveProductLocation(OrderItem item, out string aisleStr, out string rackSlotStr)
        {
            aisleStr = "---";
            rackSlotStr = "UNASSIGNED";

            if (item == null) return;

            // 1. Check StoreManager first for live slot mapping (reflects dynamic layout / routing)
            var store = StoreManager.Instance;
            if (store != null && store.TryGetSlotForSku(item.SkuId, out ShelfSlot liveSlot) && liveSlot != null)
            {
                aisleStr = $"A{liveSlot.AisleNumber:D2}";
                char sideChar = liveSlot.Side == ShelfSide.Left ? 'L' : 'R';
                rackSlotStr = $"{sideChar}-B{liveSlot.BayIndex:D2}-T{liveSlot.TierLevel:D2}-S{liveSlot.SlotIndex:D2}";
                return;
            }

            // 2. Parse from item.TargetSlotId
            if (!string.IsNullOrEmpty(item.TargetSlotId))
            {
                if (StoreLocation.TryParse(item.TargetSlotId, out StoreLocation loc, out _))
                {
                    aisleStr = $"A{loc.AisleNumber:D2}";
                    char sideChar = loc.Side == ShelfSide.Left ? 'L' : 'R';
                    rackSlotStr = $"{sideChar}-B{loc.BayIndex:D2}-T{loc.TierLevel:D2}-S{loc.SlotIndex:D2}";
                    return;
                }

                // If TargetSlotId has format like A01-R-B03-T03-S02
                string raw = item.TargetSlotId.Trim();
                if (raw.StartsWith("A") && raw.Length > 4 && raw[3] == '-')
                {
                    aisleStr = raw.Substring(0, 3);
                    rackSlotStr = raw.Substring(4);
                    return;
                }

                rackSlotStr = raw;
            }
        }

        private void DrawBottomBar()
        {
            float width = 960f;
            float height = 24f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 28f;

            Rect barRect = new Rect(x, y, width, height);
            GUI.DrawTexture(barRect, panelBackground);
            GUI.Label(barRect, "WASD: Walk | Shift: Sprint | Mouse: Look | E/LMB: Pick/Pack | N: Sample | G: Gen Order | O: Opt Route | F4: A/B Study | F1: Demo | Tab: Report", bottomStyle);
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
            if (panelBackground != null) Destroy(panelBackground);
            if (bannerBackground != null) Destroy(bannerBackground);
            if (crosshairTexture != null) Destroy(crosshairTexture);
            if (nextOrderBgTexture != null) Destroy(nextOrderBgTexture);
            if (nextOrderAccentOrange != null) Destroy(nextOrderAccentOrange);
            if (nextOrderAccentCyan != null) Destroy(nextOrderAccentCyan);
            if (nextOrderTableDivider != null) Destroy(nextOrderTableDivider);
        }
    }
}
