using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VirtualDarkStore.Domain;
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

        private string activeBannerMessage = string.Empty;
        private Color bannerColor = Color.white;
        private float bannerTimer = 0f;

        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle bodyStyle;
        private GUIStyle targetStyle;
        private GUIStyle completedItemStyle;
        private GUIStyle promptStyle;
        private GUIStyle bannerStyle;
        private GUIStyle bottomStyle;
        private Texture2D panelBackground;
        private Texture2D bannerBackground;
        private Texture2D crosshairTexture;
        private bool stylesInitialized = false;

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
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                showDemoPanel = !showDemoPanel;
            }
#else
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showDemoPanel = !showDemoPanel;
            }
#endif
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

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            var om = OrderManager.Instance;
            ResolveReferences();

            // 1. Draw Reticle in Center Screen
            DrawCrosshair();

            // 2. Draw Interaction Prompt below Reticle
            DrawInteractionPrompt();

            // 3. Draw Active Order Terminal (Top-Left)
            DrawOrderTerminal(om);

            // 4. Draw 7-Step Final Demo Workflow Panel (Top-Right)
            if (showDemoPanel)
            {
                DrawDemoWorkflowPanel(om);
            }

            // 5. Draw Feedback Banner (Top-Center)
            DrawBanner();

            // 6. Draw Bottom Controls Guide
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
            string modeBadge = (vrPlayerRig != null && vrPlayerRig.gameObject.activeInHierarchy) ? "VR RIG" : "DESKTOP";
            string scnBadge = (demo != null && demo.SelectedScenario != null) ? demo.SelectedScenario.scenarioId : "DEFAULT";

            GUILayout.Label($"DARK STORE TERMINAL [{modeBadge} | {scnBadge}]", headerStyle);

            var classifier = ActivityClassifier.Instance;
            var logger = WorkEventLogger.Instance;
            if (classifier != null)
            {
                GUILayout.Label($"Activity: [{classifier.CurrentActivity}]  |  Events: {(logger != null ? logger.EventCount : 0)}  |  Dist: {classifier.CumulativeTotalDistance:F1}m", bodyStyle);
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
            float panelHeight = 325f;
            float x = Screen.width - panelWidth - 14f;
            float y = 14f;

            GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), panelBackground);
            GUILayout.BeginArea(new Rect(x + 12f, y + 10f, panelWidth - 24f, panelHeight - 20f));

            GUILayout.Label($"FINAL DEMO WORKFLOW (Step {(int)demo.CurrentStep}/7) [F1]", headerStyle);
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

            GUILayout.EndArea();
        }

        private void DrawBanner()
        {
            if (string.IsNullOrEmpty(activeBannerMessage) || bannerTimer <= 0f) return;

            float width = 620f;
            float height = 42f;
            float x = (Screen.width - width) * 0.5f;
            float y = 16f;

            Rect bannerRect = new Rect(x, y, width, height);
            GUI.DrawTexture(bannerRect, bannerBackground);

            bannerStyle.normal.textColor = bannerColor;
            GUI.Label(bannerRect, activeBannerMessage, bannerStyle);
        }

        private void DrawBottomBar()
        {
            float width = 840f;
            float height = 24f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 28f;

            Rect barRect = new Rect(x, y, width, height);
            GUI.DrawTexture(barRect, panelBackground);
            GUI.Label(barRect, "WASD: Walk | Shift: Sprint | Mouse: Look | E/LMB: Pick/Pack | N: New Order | F1: Demo Panel | Tab: Study Report | F3: Compare Scenarios", bottomStyle);
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
        }
    }
}
