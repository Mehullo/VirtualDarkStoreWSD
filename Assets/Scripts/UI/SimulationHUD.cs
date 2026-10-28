using System;
using UnityEngine;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Management;
using VirtualDarkStore.Player;

namespace VirtualDarkStore.UI
{
    /// <summary>
    /// Screen-space simulation HUD for the dark store picker.
    /// Renders crosshair, interaction prompts, active pick list, location guidance, and validation alert banners.
    /// Completely code-driven via IMGUI for rock-solid reliability across all platforms and test runners.
    /// </summary>
    public class SimulationHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PickerInteraction pickerInteraction;
        [SerializeField] private DesktopPickerController playerController;

        [Header("Banner Settings")]
        [SerializeField] private float bannerDuration = 4.0f;

        private string activeBannerMessage = string.Empty;
        private Color bannerColor = Color.white;
        private float bannerTimer = 0f;

        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle targetStyle;
        private GUIStyle promptStyle;
        private GUIStyle bannerStyle;
        private GUIStyle bottomStyle;
        private Texture2D panelBackground;
        private Texture2D bannerBackground;
        private Texture2D crosshairTexture;
        private bool stylesInitialized = false;

        private void Awake()
        {
            if (pickerInteraction == null) pickerInteraction = FindAnyObjectByType<PickerInteraction>();
            if (playerController == null) playerController = FindAnyObjectByType<DesktopPickerController>();
        }

        private void OnEnable()
        {
            var om = OrderManager.Instance;
            if (om != null)
            {
                SubscribeOrderEvents(om);
            }
        }

        private void OnDisable()
        {
            var om = OrderManager.Instance;
            if (om != null)
            {
                UnsubscribeOrderEvents(om);
            }
        }

        private void Start()
        {
            var om = OrderManager.Instance;
            if (om != null)
            {
                SubscribeOrderEvents(om);
                if (om.ActiveOrder != null)
                {
                    ShowBanner($"Order {om.ActiveOrder.OrderId} Active. Pick items in sequence.", new Color(0.3f, 0.8f, 1f));
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
        }

        public void ShowBanner(string message, Color color)
        {
            activeBannerMessage = message;
            bannerColor = color;
            bannerTimer = bannerDuration;
        }

        private void HandleOrderStarted(Order order)
        {
            ShowBanner($"NEW ORDER [{order.OrderId}] - {order.TotalItemCount} items to pick", new Color(0.2f, 0.8f, 1.0f));
        }

        private void HandleItemPicked(OrderItem item, bool allPicked)
        {
            if (allPicked)
            {
                ShowBanner($"PICK COMPLETE! All items collected. Proceed to Packing Station.", new Color(0.3f, 1.0f, 0.4f));
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
            ShowBanner($"★ ORDER [{order.OrderId}] COMPLETED & PACKED! Press [N] for next order.", new Color(1.0f, 0.85f, 0.2f));
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            panelBackground = MakeColorTexture(new Color(0.08f, 0.10f, 0.14f, 0.88f));
            bannerBackground = MakeColorTexture(new Color(0.04f, 0.05f, 0.07f, 0.92f));
            crosshairTexture = MakeColorTexture(new Color(1f, 1f, 1f, 0.85f));

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.75f, 1.0f) },
                alignment = TextAnchor.UpperLeft
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white },
                alignment = TextAnchor.UpperLeft
            };

            targetStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.9f, 0.3f) },
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
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            bottomStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.7f, 0.75f, 0.8f) },
                alignment = TextAnchor.MiddleCenter
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            var om = OrderManager.Instance;
            if (pickerInteraction == null) pickerInteraction = FindAnyObjectByType<PickerInteraction>();
            if (playerController == null) playerController = FindAnyObjectByType<DesktopPickerController>();

            // 1. Draw Reticle in Center Screen
            DrawCrosshair();

            // 2. Draw Interaction Prompt below Reticle
            DrawInteractionPrompt();

            // 3. Draw Active Order Terminal (Top-Left)
            DrawOrderTerminal(om);

            // 4. Draw Feedback Banner (Top-Center)
            DrawBanner();

            // 5. Draw Bottom Controls Guide
            DrawBottomBar();
        }

        private void DrawCrosshair()
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float size = 5f;
            float thickness = 2f;

            Color col = (pickerInteraction != null && (pickerInteraction.HoveredItem != null || pickerInteraction.HoveredPackingStation != null))
                ? Color.green
                : new Color(1f, 1f, 1f, 0.75f);

            GUI.color = col;
            // Horizontal bar
            GUI.DrawTexture(new Rect(cx - size, cy - thickness * 0.5f, size * 2f, thickness), crosshairTexture);
            // Vertical bar
            GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - size, thickness, size * 2f), crosshairTexture);
            GUI.color = Color.white;
        }

        private void DrawInteractionPrompt()
        {
            if (pickerInteraction == null) return;
            string prompt = pickerInteraction.CurrentPrompt;
            if (string.IsNullOrEmpty(prompt)) return;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f + 25f;
            float width = 450f;
            float height = 30f;

            Rect bgRect = new Rect(cx - width * 0.5f, cy, width, height);
            GUI.DrawTexture(bgRect, panelBackground);

            promptStyle.normal.textColor = pickerInteraction.HoveredItem != null ? new Color(0.4f, 1.0f, 0.5f) : Color.cyan;
            GUI.Label(bgRect, prompt, promptStyle);
        }

        private void DrawOrderTerminal(OrderManager om)
        {
            float panelWidth = 330f;
            float panelHeight = 200f;
            Rect panelRect = new Rect(15f, 15f, panelWidth, panelHeight);

            GUI.DrawTexture(panelRect, panelBackground);

            GUILayout.BeginArea(new Rect(25f, 25f, panelWidth - 20f, panelHeight - 20f));

            GUILayout.Label("DARK STORE PICKER TERMINAL", headerStyle);
            GUILayout.Space(4);

            if (om == null || om.ActiveOrder == null)
            {
                GUILayout.Label("No active order dispatched.", bodyStyle);
                GUILayout.Label("Press [N] to dispatch sample order.", bodyStyle);
                GUILayout.EndArea();
                return;
            }

            var order = om.ActiveOrder;
            string statusBadge = order.Status.ToString().ToUpper();
            GUILayout.Label($"Order: {order.OrderId}  |  Status: {statusBadge}", bodyStyle);

            if (order.Status == OrderStatus.InProgress)
            {
                var target = order.CurrentTargetItem;
                if (target != null)
                {
                    GUILayout.Label($"Progress: Item {order.CurrentItemIndex + 1} of {order.TotalItemCount}", bodyStyle);
                    GUILayout.Space(6);
                    GUILayout.Label($"TARGET SKU:", bodyStyle);
                    GUILayout.Label($"  {target.SkuName}", targetStyle);
                    GUILayout.Label($"  ID: {target.SkuId} | Slot: {target.TargetSlotId}", bodyStyle);

                    if (playerController != null && target.TargetWorldPosition != Vector3.zero)
                    {
                        float dist = Vector3.Distance(playerController.transform.position, target.TargetWorldPosition);
                        GUILayout.Label($"  Distance to Bin: {dist:F1} m", bodyStyle);
                    }
                }
            }
            else if (order.Status == OrderStatus.AwaitingPacking)
            {
                GUILayout.Space(6);
                GUILayout.Label("ALL ITEMS PICKED!", targetStyle);
                GUILayout.Label("Deliver tote to Packing Station [PACK-01]", bodyStyle);
                GUILayout.Label("Located at store front (behind aisles).", bodyStyle);
            }
            else if (order.Status == OrderStatus.Completed)
            {
                GUILayout.Space(6);
                GUILayout.Label("ORDER COMPLETE & PACKED!", targetStyle);
                GUILayout.Label($"Total Orders Completed: {om.CompletedOrdersCount}", bodyStyle);
                GUILayout.Label("Press [N] to start next order.", bodyStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawBanner()
        {
            if (string.IsNullOrEmpty(activeBannerMessage) || bannerTimer <= 0f) return;

            float width = 620f;
            float height = 45f;
            float x = (Screen.width - width) * 0.5f;
            float y = 20f;

            Rect bannerRect = new Rect(x, y, width, height);
            GUI.DrawTexture(bannerRect, bannerBackground);

            bannerStyle.normal.textColor = bannerColor;
            GUI.Label(bannerRect, activeBannerMessage, bannerStyle);
        }

        private void DrawBottomBar()
        {
            float width = 640f;
            float height = 24f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 30f;

            Rect barRect = new Rect(x, y, width, height);
            GUI.DrawTexture(barRect, panelBackground);
            GUI.Label(barRect, "WASD: Walk | Shift: Sprint | Mouse: Look | E / LMB: Pick/Pack | N: New Order", bottomStyle);
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
