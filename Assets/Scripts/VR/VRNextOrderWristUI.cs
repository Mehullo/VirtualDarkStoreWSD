using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Management;
using VirtualDarkStore.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VirtualDarkStore.VR
{
    /// <summary>
    /// Renders the prominent Next Order Checklist in 3D VR space directly attached to the player's hand/wrist,
    /// with support for static view pinning in the environment.
    /// Strictly replicates the exact typography, headers, quantities, aisles, and highlight indicators
    /// from the verified desktop HUD Next Order panel.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRNextOrderWristUI : MonoBehaviour
    {
        [Header("Mount & Position Settings")]
        [Tooltip("Transform representing the wrist or hand anchor. If null, will search on parent.")]
        [SerializeField] private Transform wristAnchor;

        [Tooltip("When true, the UI floats in a fixed static position in front of the player instead of tracking the hand.")]
        [SerializeField] private bool isPinnedStatic = false;

        [Tooltip("Static floating offset in front of camera when pinned.")]
        [SerializeField] private Vector3 staticCameraOffset = new Vector3(0f, -0.12f, 1.15f);

        [Header("UI Element References")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text columnHeaderText;
        [SerializeField] private Text itemRowsText;
        [SerializeField] private Text productColumnText;
        [SerializeField] private Text qtyColumnText;
        [SerializeField] private Text aisleColumnText;

        [Header("Ergonomic Offsets")]
        [SerializeField] private Vector3 localOffset = Vector3.zero;

        private Canvas canvas;
        private RectTransform canvasRect;
        private Transform cameraTransform;

        public bool IsPinnedStatic => isPinnedStatic;
        public string TitleText => titleText != null ? titleText.text : string.Empty;
        public string SubtitleText => subtitleText != null ? subtitleText.text : string.Empty;
        public string ItemRowsText => itemRowsText != null ? itemRowsText.text : string.Empty;

        private void Awake()
        {
            EnsureHierarchyAndComponents();
        }

        private void Start()
        {
            RefreshOrderDisplay();
        }

        private void Update()
        {
            // Optional shortcut to toggle static floating vs wrist mount
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                TogglePinMode();
            }
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.T))
                {
                    TogglePinMode();
                }
            }
            catch
            {
                // Legacy input fallback
            }

            UpdateTransformPosition();
            RefreshOrderDisplay();
        }

        public void SetWristAnchor(Transform anchor)
        {
            if (titleText == null) EnsureHierarchyAndComponents();
            wristAnchor = anchor;
            if (!isPinnedStatic && wristAnchor != null)
            {
                transform.SetParent(wristAnchor, false);
                transform.localPosition = localOffset;
                transform.localRotation = Quaternion.identity;
            }
        }

        public void TogglePinMode()
        {
            SetPinnedStatic(!isPinnedStatic);
        }

        public void SetPinnedStatic(bool pinned)
        {
            if (titleText == null) EnsureHierarchyAndComponents();
            isPinnedStatic = pinned;

            if (cameraTransform == null)
            {
                var cam = Camera.main;
                if (cam != null) cameraTransform = cam.transform;
            }

            if (isPinnedStatic)
            {
                // Pin static in front of the camera
                if (cameraTransform != null)
                {
                    transform.SetParent(null, true);
                    transform.position = cameraTransform.position + cameraTransform.forward * 1.15f + cameraTransform.up * -0.05f;
                    transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
                }
            }
            else if (wristAnchor != null)
            {
                // Snap back to hand/wrist
                transform.SetParent(wristAnchor, false);
                transform.localPosition = localOffset;
                transform.localRotation = Quaternion.identity;
            }
        }

        private void UpdateTransformPosition()
        {
            if (cameraTransform == null)
            {
                var cam = Camera.main;
                if (cam != null) cameraTransform = cam.transform;
            }

            if (isPinnedStatic && cameraTransform != null)
            {
                // In static mode, gently billboard toward the player HMD
                transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
            }
            else if (!isPinnedStatic && wristAnchor != null)
            {
                // Firmly anchored to the wrist
                if (transform.parent != wristAnchor)
                {
                    transform.SetParent(wristAnchor, false);
                }
                transform.localPosition = localOffset;
                transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Reads active/upcoming order from OrderManager and formats the checklist.
        /// </summary>
        public void RefreshOrderDisplay()
        {
            if (titleText == null) EnsureHierarchyAndComponents();
            var om = OrderManager.Instance;
            if (om == null)
            {
                SetEmptyDisplay();
                return;
            }

            var order = SimulationHUD.ResolveNextPendingOrder(om, out string title, out string subtitle, out _);

            if (titleText != null) titleText.text = title.ToUpperInvariant();
            if (subtitleText != null) subtitleText.text = subtitle.ToUpperInvariant();

            if (order == null || order.Items == null || order.Items.Count == 0)
            {
                if (itemRowsText != null)
                {
                    itemRowsText.text = "<color=#90A4AE>No active order items.</color>";
                }
                if (productColumnText != null) productColumnText.text = "<color=#90A4AE>No active order items.</color>";
                if (qtyColumnText != null) qtyColumnText.text = string.Empty;
                if (aisleColumnText != null) aisleColumnText.text = string.Empty;
                return;
            }

            // Build dynamic items table matching exact columns: PRODUCT, QTY, AISLE
            var sbAll = new System.Text.StringBuilder();
            var sbProd = new System.Text.StringBuilder();
            var sbQty = new System.Text.StringBuilder();
            var sbAisle = new System.Text.StringBuilder();

            var items = order.Items;

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                VirtualDarkStore.UI.SimulationHUD.ResolveProductLocation(it, out string aisle, out _);

                string skuName = it.SkuName;
                if (skuName.Length > 28) skuName = skuName.Substring(0, 26) + "..";

                if (it.IsFulfilled)
                {
                    // Completed item (subdued green)
                    sbProd.AppendLine($"<color=#81C784>✔ {skuName}</color>");
                    sbQty.AppendLine($"<color=#81C784>{it.RequiredQuantity}</color>");
                    sbAisle.AppendLine($"<color=#81C784>{aisle}</color>");
                    sbAll.AppendLine($"<color=#81C784>✔ {skuName,-24}  {it.RequiredQuantity,2}    {aisle}</color>");
                }
                else if (order.CurrentTargetItem != null && order.CurrentTargetItem.SkuId == it.SkuId)
                {
                    // Active picking target (bold golden yellow with arrow ▶)
                    sbProd.AppendLine($"<color=#FFD54F><b>▶ {skuName}</b></color>");
                    sbQty.AppendLine($"<color=#FFD54F><b>{it.RequiredQuantity}</b></color>");
                    sbAisle.AppendLine($"<color=#FFD54F><b>{aisle}</b></color>");
                    sbAll.AppendLine($"<color=#FFD54F><b>▶ {skuName,-24}  {it.RequiredQuantity,2}    {aisle}</b></color>");
                }
                else
                {
                    // Upcoming pending item
                    sbProd.AppendLine($"<color=#E0E0E0>○ {skuName}</color>");
                    sbQty.AppendLine($"<color=#E0E0E0>{it.RequiredQuantity}</color>");
                    sbAisle.AppendLine($"<color=#FFD54F>{aisle}</color>");
                    sbAll.AppendLine($"<color=#E0E0E0>○ {skuName,-24}  {it.RequiredQuantity,2}    {aisle}</color>");
                }
            }

            if (itemRowsText != null)
            {
                itemRowsText.text = sbAll.ToString();
            }
            if (productColumnText != null)
            {
                productColumnText.text = sbProd.ToString();
            }
            if (qtyColumnText != null)
            {
                qtyColumnText.text = sbQty.ToString();
            }
            if (aisleColumnText != null)
            {
                aisleColumnText.text = sbAisle.ToString();
            }
        }

        private void SetEmptyDisplay()
        {
            if (titleText != null) titleText.text = "NEXT ORDER | NO ORDERS";
            if (subtitleText != null) subtitleText.text = "DISPATCH AN ORDER TO BEGIN FULFILLMENT";
            if (itemRowsText != null) itemRowsText.text = "<color=#78909C>Press [F1] or Trigger to dispatch order.</color>";
            if (productColumnText != null) productColumnText.text = "<color=#78909C>Press [F1] or Trigger to dispatch order.</color>";
            if (qtyColumnText != null) qtyColumnText.text = string.Empty;
            if (aisleColumnText != null) aisleColumnText.text = string.Empty;
        }

        private void EnsureHierarchyAndComponents()
        {
            // 1. World Space Canvas
            canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null) DestroyImmediate(raycaster);

            var col = GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            // Size: 380px x 230px scaled by 0.0006 => ~22.8cm x 13.8cm (standard industrial tablet / wrist scanner size)
            canvasRect = GetComponent<RectTransform>();
            if (canvasRect == null) canvasRect = gameObject.AddComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(380f, 230f);
            canvasRect.localScale = Vector3.one * 0.0006f;
            canvasRect.pivot = new Vector2(0.5f, 0.0f); // Pivot at bottom edge so card extends upwards above the hand and avoids the thumbstick disc

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 2. Background Card Panel
            var bgTransform = transform.Find("CardBackground");
            GameObject bgObj;
            if (bgTransform == null)
            {
                bgObj = new GameObject("CardBackground", typeof(RectTransform));
                bgObj.transform.SetParent(transform, false);
            }
            else
            {
                bgObj = bgTransform.gameObject;
            }

            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgRt.localPosition = Vector3.zero;

            var bgImg = bgObj.GetComponent<Image>() ?? bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.043f, 0.071f, 0.125f, 0.94f); // #0B1220F0 midnight card
            bgImg.raycastTarget = false;

            // 3. Top Amber Accent Stripe
            var stripeTransform = transform.Find("TopAccentStripe");
            GameObject stripeObj;
            if (stripeTransform == null)
            {
                stripeObj = new GameObject("TopAccentStripe", typeof(RectTransform));
                stripeObj.transform.SetParent(transform, false);
            }
            else
            {
                stripeObj = stripeTransform.gameObject;
            }

            var stripeRt = stripeObj.GetComponent<RectTransform>();
            stripeRt.anchorMin = new Vector2(0f, 1f);
            stripeRt.anchorMax = new Vector2(1f, 1f);
            stripeRt.pivot = new Vector2(0.5f, 1f);
            stripeRt.sizeDelta = new Vector2(0f, 4f);
            stripeRt.localPosition = Vector3.zero;

            var stripeImg = stripeObj.GetComponent<Image>() ?? stripeObj.AddComponent<Image>();
            stripeImg.color = new Color(0.95f, 0.60f, 0.15f, 0.95f); // Safety amber
            stripeImg.raycastTarget = false;

            // 4. Header Text (Order ID)
            var titleTransform = transform.Find("TitleText");
            GameObject titleObj;
            if (titleTransform == null)
            {
                titleObj = new GameObject("TitleText", typeof(RectTransform));
                titleObj.transform.SetParent(transform, false);
            }
            else
            {
                titleObj = titleTransform.gameObject;
            }

            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.04f, 0.82f);
            titleRt.anchorMax = new Vector2(0.96f, 0.96f);
            titleRt.sizeDelta = Vector2.zero;

            titleText = titleObj.GetComponent<Text>() ?? titleObj.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.raycastTarget = false;
            titleText.text = "NEXT ORDER | ORD-SCN-0001";

            // 5. Subtitle Text (Item Count & Status)
            var subTransform = transform.Find("SubtitleText");
            GameObject subObj;
            if (subTransform == null)
            {
                subObj = new GameObject("SubtitleText", typeof(RectTransform));
                subObj.transform.SetParent(transform, false);
            }
            else
            {
                subObj = subTransform.gameObject;
            }

            var subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.04f, 0.70f);
            subRt.anchorMax = new Vector2(0.96f, 0.82f);
            subRt.sizeDelta = Vector2.zero;

            subtitleText = subObj.GetComponent<Text>() ?? subObj.AddComponent<Text>();
            subtitleText.font = font;
            subtitleText.fontSize = 11;
            subtitleText.fontStyle = FontStyle.Bold;
            subtitleText.color = new Color(1.0f, 0.76f, 0.20f); // Amber yellow (#FFC107)
            subtitleText.alignment = TextAnchor.MiddleLeft;
            subtitleText.raycastTarget = false;
            subtitleText.text = "TOTAL ITEMS: 4 • STATUS: INPROGRESS";

            // 6. Column Headers
            var colTransform = transform.Find("ColumnHeader");
            GameObject colObj;
            if (colTransform == null)
            {
                colObj = new GameObject("ColumnHeader", typeof(RectTransform));
                colObj.transform.SetParent(transform, false);
            }
            else
            {
                colObj = colTransform.gameObject;
            }

            var colRt = colObj.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0.04f, 0.58f);
            colRt.anchorMax = new Vector2(0.96f, 0.70f);
            colRt.sizeDelta = Vector2.zero;

            columnHeaderText = colObj.GetComponent<Text>() ?? colObj.AddComponent<Text>();
            columnHeaderText.font = font;
            columnHeaderText.fontSize = 11;
            columnHeaderText.fontStyle = FontStyle.Bold;
            columnHeaderText.color = new Color(0.35f, 0.75f, 0.95f); // Cyan
            columnHeaderText.alignment = TextAnchor.MiddleLeft;
            columnHeaderText.raycastTarget = false;
            columnHeaderText.text = "PRODUCT                                      QTY       AISLE";

            // 7. Divider Line
            var divTransform = transform.Find("DividerLine");
            GameObject divObj;
            if (divTransform == null)
            {
                divObj = new GameObject("DividerLine", typeof(RectTransform));
                divObj.transform.SetParent(transform, false);
            }
            else
            {
                divObj = divTransform.gameObject;
            }

            var divRt = divObj.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.04f, 0.57f);
            divRt.anchorMax = new Vector2(0.96f, 0.575f);
            divRt.sizeDelta = Vector2.zero;

            var divImg = divObj.GetComponent<Image>() ?? divObj.AddComponent<Image>();
            divImg.color = new Color(0.20f, 0.35f, 0.48f, 0.7f);
            divImg.raycastTarget = false;

            // 8. Structured Multi-Column Item Rows
            // 8a. Product Column (Left aligned, wide)
            var prodColTransform = transform.Find("ProductColumn");
            GameObject prodColObj = prodColTransform != null ? prodColTransform.gameObject : new GameObject("ProductColumn", typeof(RectTransform));
            prodColObj.transform.SetParent(transform, false);
            var prodRt = prodColObj.GetComponent<RectTransform>();
            prodRt.anchorMin = new Vector2(0.04f, 0.04f);
            prodRt.anchorMax = new Vector2(0.72f, 0.56f);
            prodRt.sizeDelta = Vector2.zero;
            productColumnText = prodColObj.GetComponent<Text>() ?? prodColObj.AddComponent<Text>();
            productColumnText.font = font;
            productColumnText.fontSize = 11;
            productColumnText.color = Color.white;
            productColumnText.alignment = TextAnchor.UpperLeft;
            productColumnText.supportRichText = true;
            productColumnText.raycastTarget = false;
            productColumnText.lineSpacing = 1.15f;

            // 8b. Quantity Column (Center aligned)
            var qtyColTransform = transform.Find("QtyColumn");
            GameObject qtyColObj = qtyColTransform != null ? qtyColTransform.gameObject : new GameObject("QtyColumn", typeof(RectTransform));
            qtyColObj.transform.SetParent(transform, false);
            var qtyRt = qtyColObj.GetComponent<RectTransform>();
            qtyRt.anchorMin = new Vector2(0.72f, 0.04f);
            qtyRt.anchorMax = new Vector2(0.84f, 0.56f);
            qtyRt.sizeDelta = Vector2.zero;
            qtyColumnText = qtyColObj.GetComponent<Text>() ?? qtyColObj.AddComponent<Text>();
            qtyColumnText.font = font;
            qtyColumnText.fontSize = 11;
            qtyColumnText.color = Color.white;
            qtyColumnText.alignment = TextAnchor.UpperCenter;
            qtyColumnText.supportRichText = true;
            qtyColumnText.raycastTarget = false;
            qtyColumnText.lineSpacing = 1.15f;

            // 8c. Aisle Column (Right/Center aligned, bold amber)
            var aisleColTransform = transform.Find("AisleColumn");
            GameObject aisleColObj = aisleColTransform != null ? aisleColTransform.gameObject : new GameObject("AisleColumn", typeof(RectTransform));
            aisleColObj.transform.SetParent(transform, false);
            var aisleRt = aisleColObj.GetComponent<RectTransform>();
            aisleRt.anchorMin = new Vector2(0.84f, 0.04f);
            aisleRt.anchorMax = new Vector2(0.96f, 0.56f);
            aisleRt.sizeDelta = Vector2.zero;
            aisleColumnText = aisleColObj.GetComponent<Text>() ?? aisleColObj.AddComponent<Text>();
            aisleColumnText.font = font;
            aisleColumnText.fontSize = 11;
            aisleColumnText.fontStyle = FontStyle.Bold;
            aisleColumnText.color = new Color(1.0f, 0.85f, 0.30f);
            aisleColumnText.alignment = TextAnchor.UpperCenter;
            aisleColumnText.supportRichText = true;
            aisleColumnText.raycastTarget = false;
            aisleColumnText.lineSpacing = 1.15f;

            // 8d. Fallback Combined Item Rows (for test compatibility)
            var rowsTransform = transform.Find("ItemRows");
            GameObject rowsObj = rowsTransform != null ? rowsTransform.gameObject : new GameObject("ItemRows", typeof(RectTransform));
            rowsObj.transform.SetParent(transform, false);
            var rowsRt = rowsObj.GetComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0.04f, 0.04f);
            rowsRt.anchorMax = new Vector2(0.96f, 0.56f);
            rowsRt.sizeDelta = Vector2.zero;
            itemRowsText = rowsObj.GetComponent<Text>() ?? rowsObj.AddComponent<Text>();
            itemRowsText.font = font;
            itemRowsText.fontSize = 11;
            itemRowsText.color = Color.clear; // Subdued since separate columns render prominently
            itemRowsText.alignment = TextAnchor.UpperLeft;
            itemRowsText.supportRichText = true;
            itemRowsText.raycastTarget = false;
            itemRowsText.lineSpacing = 1.15f;
            itemRowsText.text = "Loading order...";
        }
    }
}
