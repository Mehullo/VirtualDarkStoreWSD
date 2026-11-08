using System;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualDarkStore.VR
{
    /// <summary>
    /// Displays a 3D in-VR gaze reticle and floating prompt badge when looking at or aiming at grabable objects.
    /// Billboarded directly toward the player's HMD camera.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRGazePromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera hmdCamera;
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private RectTransform promptBadgeRect;
        [SerializeField] private Text promptText;
        [SerializeField] private Image reticleDot;

        [Header("Reticle Settings")]
        [SerializeField] private float defaultReticleDistance = 2.0f;
        [SerializeField] private Color reticleDefaultColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private Color reticleHoverColor = new Color(1f, 0.82f, 0.20f, 0.95f); // Amber

        private bool isHovering = false;
        private Vector3 targetPromptPosition;

        public bool IsHovering => isHovering;
        public string CurrentPromptText => promptText != null ? promptText.text : string.Empty;

        private void Awake()
        {
            EnsureComponents();
        }

        private void LateUpdate()
        {
            UpdateReticleAndPromptPose();
        }

        public void SetHmdCamera(Camera cam)
        {
            if (promptText == null) EnsureComponents();
            hmdCamera = cam;
        }

        public void ShowPrompt(string text, Vector3 worldTargetPosition)
        {
            if (promptText == null) EnsureComponents();

            if (string.IsNullOrEmpty(text))
            {
                HidePrompt();
                return;
            }

            isHovering = true;
            targetPromptPosition = worldTargetPosition + Vector3.up * 0.20f;

            if (promptText != null)
            {
                promptText.text = text;
            }

            if (promptBadgeRect != null)
            {
                promptBadgeRect.gameObject.SetActive(true);
            }

            if (reticleDot != null)
            {
                reticleDot.color = reticleHoverColor;
                reticleDot.rectTransform.sizeDelta = new Vector2(16f, 16f);
            }
        }

        public void HidePrompt()
        {
            if (promptBadgeRect == null && promptText == null) EnsureComponents();
            isHovering = false;

            if (promptBadgeRect != null)
            {
                promptBadgeRect.gameObject.SetActive(false);
            }

            if (reticleDot != null)
            {
                reticleDot.color = reticleDefaultColor;
                reticleDot.rectTransform.sizeDelta = new Vector2(10f, 10f);
            }
        }

        private void UpdateReticleAndPromptPose()
        {
            if (hmdCamera == null)
            {
                hmdCamera = Camera.main;
                if (hmdCamera == null) return;
            }

            Transform camT = hmdCamera.transform;

            // Reticle position (always centered in front of gaze)
            float dist = defaultReticleDistance;
            if (isHovering && targetPromptPosition != Vector3.zero)
            {
                dist = Mathf.Clamp(Vector3.Distance(camT.position, targetPromptPosition), 0.8f, 3.5f);
            }

            Vector3 reticlePos = camT.position + camT.forward * dist;
            transform.position = reticlePos;
            transform.rotation = Quaternion.LookRotation(transform.position - camT.position);

            // Floating prompt badge position: Anchor in 3D world space directly above the target object
            if (promptBadgeRect != null && promptBadgeRect.gameObject.activeSelf)
            {
                if (isHovering && targetPromptPosition != Vector3.zero)
                {
                    promptBadgeRect.position = targetPromptPosition;
                    promptBadgeRect.rotation = Quaternion.LookRotation(promptBadgeRect.position - camT.position);
                }
                else
                {
                    promptBadgeRect.position = reticlePos + camT.up * -0.15f;
                    promptBadgeRect.rotation = Quaternion.LookRotation(promptBadgeRect.position - camT.position);
                }
            }
        }

        private void EnsureComponents()
        {
            worldCanvas = GetComponent<Canvas>();
            if (worldCanvas == null) worldCanvas = gameObject.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 50;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null) DestroyImmediate(raycaster);

            var col = GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 150f);
            rt.localScale = Vector3.one * 0.0012f;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Center Reticle Dot
            var reticleTransform = transform.Find("CenterReticleDot");
            GameObject reticleObj;
            if (reticleTransform == null)
            {
                reticleObj = new GameObject("CenterReticleDot", typeof(RectTransform));
                reticleObj.transform.SetParent(transform, false);
            }
            else
            {
                reticleObj = reticleTransform.gameObject;
            }

            var reticleRt = reticleObj.GetComponent<RectTransform>();
            reticleRt.anchorMin = new Vector2(0.5f, 0.5f);
            reticleRt.anchorMax = new Vector2(0.5f, 0.5f);
            reticleRt.sizeDelta = new Vector2(10f, 10f);
            reticleRt.localPosition = Vector3.zero;

            reticleDot = reticleObj.GetComponent<Image>() ?? reticleObj.AddComponent<Image>();
            reticleDot.color = reticleDefaultColor;
            reticleDot.raycastTarget = false;

            // 2. Floating Prompt Badge
            var badgeTransform = transform.Find("PromptBadge");
            GameObject badgeObj;
            if (badgeTransform == null)
            {
                badgeObj = new GameObject("PromptBadge", typeof(RectTransform));
                badgeObj.transform.SetParent(transform, false);
            }
            else
            {
                badgeObj = badgeTransform.gameObject;
            }

            promptBadgeRect = badgeObj.GetComponent<RectTransform>();
            promptBadgeRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptBadgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptBadgeRect.pivot = new Vector2(0.5f, 0.5f);
            promptBadgeRect.sizeDelta = new Vector2(320f, 44f);
            promptBadgeRect.localPosition = new Vector3(0f, -40f, 0f);

            var badgeBg = badgeObj.GetComponent<Image>() ?? badgeObj.AddComponent<Image>();
            badgeBg.color = new Color(0.05f, 0.08f, 0.14f, 0.92f); // Dark background
            badgeBg.raycastTarget = false;

            // 3. Prompt Text
            var textTransform = badgeObj.transform.Find("PromptText");
            GameObject textObj;
            if (textTransform == null)
            {
                textObj = new GameObject("PromptText", typeof(RectTransform));
                textObj.transform.SetParent(badgeObj.transform, false);
            }
            else
            {
                textObj = textTransform.gameObject;
            }

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            promptText = textObj.GetComponent<Text>() ?? textObj.AddComponent<Text>();
            promptText.font = font;
            promptText.fontSize = 13;
            promptText.fontStyle = FontStyle.Bold;
            promptText.color = new Color(1.0f, 0.88f, 0.40f); // Bright golden amber
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.raycastTarget = false;
            promptText.text = "[Trigger / Grip] Grab Item";

            badgeObj.SetActive(false);
        }
    }
}
