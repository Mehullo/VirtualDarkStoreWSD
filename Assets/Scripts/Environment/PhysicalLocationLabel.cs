using System;
using UnityEngine;
using UnityEngine.UI;
using VirtualDarkStore.Domain;

namespace VirtualDarkStore.Environment
{
    public enum LocationLabelType
    {
        AisleHeader,
        EndCapPlacard,
        BayRack,
        ShelfLevel,
        ProductSlot
    }

    /// <summary>
    /// Reusable physical visual label and signage component for the dark store environment.
    /// Renders high-contrast, lightweight physical signage (aisle overheads, bay placards, shelf tier badges, and product slot tags)
    /// fully synchronized with the existing StoreLocation hierarchy (Store -> Aisle -> Side -> Bay -> Shelf Level -> Slot).
    /// All elements use non-blocking raycast settings and zero colliders to ensure zero interference with NavMesh, walking, or interaction.
    /// </summary>
    [DisallowMultipleComponent]
    public class PhysicalLocationLabel : MonoBehaviour
    {
        [Header("Label Configuration")]
        [SerializeField] private LocationLabelType labelType;
        [SerializeField] private string primaryText = string.Empty;
        [SerializeField] private string secondaryText = string.Empty;
        [SerializeField] private string locationId = string.Empty;

        [Header("Hierarchy Data")]
        [SerializeField] private int aisleNumber;
        [SerializeField] private ShelfSide side;
        [SerializeField] private int bayIndex;
        [SerializeField] private int tierLevel;
        [SerializeField] private int slotIndex;

        [Header("Physical Dimensions (Meters)")]
        [SerializeField] private float widthMeters = 1.0f;
        [SerializeField] private float heightMeters = 0.3f;

        public LocationLabelType LabelType => labelType;
        public string PrimaryText => primaryText;
        public string SecondaryText => secondaryText;
        public string LocationId => locationId;
        public int AisleNumber => aisleNumber;
        public ShelfSide Side => side;
        public int BayIndex => bayIndex;
        public int TierLevel => tierLevel;
        public int SlotIndex => slotIndex;
        public float WidthMeters => widthMeters;
        public float HeightMeters => heightMeters;

        /// <summary>
        /// Configures and builds the physical label visual hierarchy in world space.
        /// </summary>
        public void Configure(
            LocationLabelType type,
            string primary,
            string secondary,
            string locId,
            int aisle = 0,
            ShelfSide shelfSide = ShelfSide.Left,
            int bay = 0,
            int tier = 0,
            int slot = 0,
            float width = 1.0f,
            float height = 0.3f,
            Color? bgColor = null,
            Color? primaryColor = null,
            Color? secondaryColor = null)
        {
            labelType = type;
            primaryText = primary ?? string.Empty;
            secondaryText = secondary ?? string.Empty;
            locationId = locId ?? string.Empty;
            aisleNumber = aisle;
            side = shelfSide;
            bayIndex = bay;
            tierLevel = tier;
            slotIndex = slot;
            widthMeters = width;
            heightMeters = height;

            BuildVisuals(bgColor, primaryColor, secondaryColor);
        }

        private void BuildVisuals(Color? customBg, Color? customPrimary, Color? customSecondary)
        {
            // 1. Setup World Space Canvas (Canvas converts root Transform to RectTransform)
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;

            // Remove or disable GraphicRaycaster if present
            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                DestroyImmediate(raycaster);
            }

            // Remove any accidental collider
            var col = GetComponent<Collider>();
            if (col != null)
            {
                DestroyImmediate(col);
            }

            // High pixel density for sharp rendering from picker viewing distances (500 px/meter)
            float pixelsPerMeter = 500f;
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(widthMeters * pixelsPerMeter, heightMeters * pixelsPerMeter);
            rt.localScale = Vector3.one * (1.0f / pixelsPerMeter);

            // 2. Background Panel
            var bgTransform = transform.Find("Background");
            GameObject bgObj;
            if (bgTransform == null)
            {
                bgObj = new GameObject("Background", typeof(RectTransform));
                bgObj.transform.SetParent(transform, false);
            }
            else
            {
                bgObj = bgTransform.gameObject;
            }

            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgRt.localPosition = Vector3.zero;

            var bgImage = bgObj.GetComponent<Image>() ?? bgObj.AddComponent<Image>();
            bgImage.color = customBg ?? GetDefaultBackgroundColor(labelType);
            bgImage.raycastTarget = false;

            // 3. Border / Accent Line (for header signs and placards)
            if (labelType == LocationLabelType.AisleHeader || labelType == LocationLabelType.BayRack || labelType == LocationLabelType.EndCapPlacard)
            {
                var borderTransform = transform.Find("AccentStripe");
                GameObject borderObj;
                if (borderTransform == null)
                {
                    borderObj = new GameObject("AccentStripe", typeof(RectTransform));
                    borderObj.transform.SetParent(transform, false);
                }
                else
                {
                    borderObj = borderTransform.gameObject;
                }

                var borderRt = borderObj.GetComponent<RectTransform>();
                borderRt.anchorMin = new Vector2(0f, 0f);
                borderRt.anchorMax = new Vector2(1f, 0.08f);
                borderRt.offsetMin = Vector2.zero;
                borderRt.offsetMax = Vector2.zero;

                var borderImg = borderObj.GetComponent<Image>() ?? borderObj.AddComponent<Image>();
                borderImg.color = (labelType == LocationLabelType.AisleHeader) ? new Color(0.98f, 0.85f, 0.15f) : new Color(0.92f, 0.65f, 0.18f);
                borderImg.raycastTarget = false;
            }

            // 4. Primary Text
            var pTransform = transform.Find("PrimaryText");
            GameObject pObj;
            if (pTransform == null)
            {
                pObj = new GameObject("PrimaryText", typeof(RectTransform));
                pObj.transform.SetParent(transform, false);
            }
            else
            {
                pObj = pTransform.gameObject;
            }

            var pRt = pObj.GetComponent<RectTransform>();
            bool hasSecondary = !string.IsNullOrEmpty(secondaryText);

            if (hasSecondary)
            {
                pRt.anchorMin = new Vector2(0.02f, 0.35f);
                pRt.anchorMax = new Vector2(0.98f, 0.95f);
            }
            else
            {
                pRt.anchorMin = new Vector2(0.02f, 0.05f);
                pRt.anchorMax = new Vector2(0.98f, 0.95f);
            }
            pRt.offsetMin = Vector2.zero;
            pRt.offsetMax = Vector2.zero;

            var pText = pObj.GetComponent<Text>() ?? pObj.AddComponent<Text>();
            pText.text = primaryText;
            pText.font = GetDefaultFont();
            pText.fontSize = GetPrimaryFontSize(labelType);
            pText.fontStyle = FontStyle.Bold;
            pText.alignment = TextAnchor.MiddleCenter;
            pText.color = customPrimary ?? GetDefaultPrimaryTextColor(labelType);
            pText.raycastTarget = false;

            // 5. Secondary Text (if applicable)
            if (hasSecondary)
            {
                var sTransform = transform.Find("SecondaryText");
                GameObject sObj;
                if (sTransform == null)
                {
                    sObj = new GameObject("SecondaryText", typeof(RectTransform));
                    sObj.transform.SetParent(transform, false);
                }
                else
                {
                    sObj = sTransform.gameObject;
                }

                var sRt = sObj.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.02f, 0.08f);
                sRt.anchorMax = new Vector2(0.98f, 0.40f);
                sRt.offsetMin = Vector2.zero;
                sRt.offsetMax = Vector2.zero;

                var sText = sObj.GetComponent<Text>() ?? sObj.AddComponent<Text>();
                sText.text = secondaryText;
                sText.font = GetDefaultFont();
                sText.fontSize = GetSecondaryFontSize(labelType);
                sText.fontStyle = FontStyle.Normal;
                sText.alignment = TextAnchor.MiddleCenter;
                sText.color = customSecondary ?? GetDefaultSecondaryTextColor(labelType);
                sText.raycastTarget = false;
            }
        }

        public static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return font;
        }

        private static Color GetDefaultBackgroundColor(LocationLabelType type)
        {
            switch (type)
            {
                case LocationLabelType.AisleHeader:
                    return new Color(0.08f, 0.10f, 0.14f, 0.95f); // Deep industrial navy
                case LocationLabelType.EndCapPlacard:
                case LocationLabelType.BayRack:
                    return new Color(0.12f, 0.15f, 0.20f, 0.95f); // Dark slate
                case LocationLabelType.ShelfLevel:
                    return new Color(0.85f, 0.42f, 0.10f, 0.95f); // Safety orange shelf tier badge
                case LocationLabelType.ProductSlot:
                    return new Color(0.95f, 0.97f, 0.98f, 0.95f); // Clean white/light gray ticket
                default:
                    return new Color(0.1f, 0.1f, 0.1f, 0.9f);
            }
        }

        private static Color GetDefaultPrimaryTextColor(LocationLabelType type)
        {
            switch (type)
            {
                case LocationLabelType.AisleHeader:
                    return new Color(0.98f, 0.85f, 0.15f); // Safety yellow
                case LocationLabelType.EndCapPlacard:
                case LocationLabelType.BayRack:
                    return Color.white;
                case LocationLabelType.ShelfLevel:
                    return Color.white;
                case LocationLabelType.ProductSlot:
                    return new Color(0.08f, 0.10f, 0.14f); // Charcoal black on white ticket
                default:
                    return Color.white;
            }
        }

        private static Color GetDefaultSecondaryTextColor(LocationLabelType type)
        {
            switch (type)
            {
                case LocationLabelType.AisleHeader:
                    return new Color(0.80f, 0.88f, 0.96f); // Soft cyan/white
                case LocationLabelType.EndCapPlacard:
                case LocationLabelType.BayRack:
                    return new Color(0.95f, 0.80f, 0.20f); // Safety yellow
                case LocationLabelType.ShelfLevel:
                    return new Color(1.0f, 0.95f, 0.80f); // Warm cream
                case LocationLabelType.ProductSlot:
                    return new Color(0.15f, 0.38f, 0.70f); // Industrial blue
                default:
                    return Color.gray;
            }
        }

        private static int GetPrimaryFontSize(LocationLabelType type)
        {
            switch (type)
            {
                case LocationLabelType.AisleHeader:
                    return 56;
                case LocationLabelType.EndCapPlacard:
                    return 38;
                case LocationLabelType.BayRack:
                    return 34;
                case LocationLabelType.ShelfLevel:
                    return 18;
                case LocationLabelType.ProductSlot:
                    return 14;
                default:
                    return 20;
            }
        }

        private static int GetSecondaryFontSize(LocationLabelType type)
        {
            switch (type)
            {
                case LocationLabelType.AisleHeader:
                    return 24;
                case LocationLabelType.EndCapPlacard:
                case LocationLabelType.BayRack:
                    return 20;
                case LocationLabelType.ShelfLevel:
                    return 14;
                case LocationLabelType.ProductSlot:
                    return 11;
                default:
                    return 14;
            }
        }

        /// <summary>
        /// Creates a configured PhysicalLocationLabel GameObject in the scene hierarchy.
        /// </summary>
        public static GameObject CreateLabelObject(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            LocationLabelType type,
            string primaryText,
            string secondaryText,
            string locationId,
            int aisle = 0,
            ShelfSide side = ShelfSide.Left,
            int bay = 0,
            int tier = 0,
            int slot = 0,
            float width = 1.0f,
            float height = 0.3f)
        {
            var labelObj = new GameObject(objectName);
            labelObj.transform.SetParent(parent, false);
            labelObj.transform.localPosition = localPosition;
            labelObj.transform.localRotation = localRotation;

            var labelComp = labelObj.AddComponent<PhysicalLocationLabel>();
            labelComp.Configure(type, primaryText, secondaryText, locationId, aisle, side, bay, tier, slot, width, height);

            return labelObj;
        }
    }
}
