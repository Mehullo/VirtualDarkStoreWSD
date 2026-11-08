using System;
using UnityEngine;

namespace VirtualDarkStore.VR
{
    /// <summary>
    /// Renders visible, lightweight 3D hand and controller visuals in the VR environment.
    /// Provides clear spatial orientation for both hands, laser pointer ray for the right hand,
    /// and anchor points for wrist-mounted UI.
    /// All colliders are removed to ensure zero interference with physics, picking, or locomotion.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRHandVisual : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VRHand hand = VRHand.Right;
        [SerializeField] private bool showPointerRay = true;
        [SerializeField] private float pointerDistance = 2.8f;

        [Header("Ergonomic Offsets")]
        [Tooltip("Forward angle offset (in degrees away from user) for natural pointing and thumbstick orientation.")]
        [SerializeField] private float tiltAngleAwayFromUser = 50f;

        [Header("References")]
        [SerializeField] private VRControllerInput controllerInput;
        [SerializeField] private LineRenderer pointerLine;
        [SerializeField] private Transform pointerTip;
        [SerializeField] private Transform wristAnchor;

        public VRHand Hand => hand;
        public float TiltAngleAwayFromUser
        {
            get => tiltAngleAwayFromUser;
            set
            {
                tiltAngleAwayFromUser = value;
                BuildHandVisualHierarchy();
            }
        }
        public Transform WristAnchor => wristAnchor;
        public Transform PointerTip => pointerTip;
        public LineRenderer PointerLine => pointerLine;

        private static Material sharedLeftHandMat;
        private static Material sharedRightHandMat;
        private static Material sharedGripMat;
        private static Material sharedLaserMat;

        private void Awake()
        {
            if (controllerInput == null)
            {
                controllerInput = GetComponent<VRControllerInput>();
                if (controllerInput != null)
                {
                    hand = controllerInput.Hand;
                }
            }

            BuildHandVisualHierarchy();
        }

        private void LateUpdate()
        {
            UpdatePointerRay();
        }

        public void Initialize(VRHand handType)
        {
            hand = handType;
            BuildHandVisualHierarchy();
        }

        private void BuildHandVisualHierarchy()
        {
            EnsureMaterials();

            // 1. Root visual holder
            var visualHolder = transform.Find("HandModelRoot");
            if (visualHolder != null)
            {
                DestroyImmediate(visualHolder.gameObject);
            }

            var rootObj = new GameObject("HandModelRoot");
            rootObj.transform.SetParent(transform, false);
            rootObj.transform.localPosition = Vector3.zero;
            rootObj.transform.localRotation = Quaternion.Euler(tiltAngleAwayFromUser, 0f, 0f);

            // 2. Controller Grip Body (Ergonomic cylinder/capsule shape)
            var grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "ControllerGrip";
            grip.transform.SetParent(rootObj.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.04f, -0.02f);
            grip.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.038f, 0.055f, 0.034f);
            StripCollider(grip);
            grip.GetComponent<MeshRenderer>().sharedMaterial = sharedGripMat;

            // 3. Controller Head / Thumbpad Face (Sleek Quest 3 Touch Plus profile)
            var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "ControllerFace";
            head.transform.SetParent(rootObj.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.015f, 0.012f);
            head.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            head.transform.localScale = new Vector3(0.040f, 0.010f, 0.042f);
            StripCollider(head);
            head.GetComponent<MeshRenderer>().sharedMaterial = (hand == VRHand.Left) ? sharedLeftHandMat : sharedRightHandMat;

            // 4. Index Trigger Visual
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.name = "TriggerVisual";
            trigger.transform.SetParent(rootObj.transform, false);
            trigger.transform.localPosition = new Vector3(0f, 0.005f, 0.028f);
            trigger.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            trigger.transform.localScale = new Vector3(0.014f, 0.022f, 0.018f);
            StripCollider(trigger);
            trigger.GetComponent<MeshRenderer>().sharedMaterial = sharedGripMat;

            // 5. Wrist Anchor point (for wrist UI tablet)
            // Shifted cleanly behind the controller grip (Z = -0.07m) and elevated (Y = 0.15m)
            // angled so it faces the player's HMD with ZERO controller intersection.
            var wristObj = transform.Find("WristAnchor");
            Vector3 targetPos = new Vector3(hand == VRHand.Left ? -0.02f : 0.02f, 0.15f, -0.07f);
            Quaternion targetRot = Quaternion.Euler(38f, hand == VRHand.Left ? 15f : -15f, -4f);
            if (wristObj == null)
            {
                var w = new GameObject("WristAnchor");
                w.transform.SetParent(transform, false);
                w.transform.localPosition = targetPos;
                w.transform.localRotation = targetRot;
                wristAnchor = w.transform;
            }
            else
            {
                wristAnchor = wristObj;
                wristAnchor.localPosition = targetPos;
                wristAnchor.localRotation = targetRot;
            }

            // 7. Pointer tip & line renderer (only needed on right hand or if enabled)
            // Tilted 50 degrees away from user so laser points straight forward along the controller
            var tipObj = transform.Find("PointerTip");
            if (tipObj == null)
            {
                var t = new GameObject("PointerTip");
                t.transform.SetParent(transform, false);
                t.transform.localPosition = new Vector3(0f, 0.025f, 0.06f);
                t.transform.localRotation = Quaternion.Euler(tiltAngleAwayFromUser, 0f, 0f);
                pointerTip = t.transform;
            }
            else
            {
                pointerTip = tipObj;
                pointerTip.localRotation = Quaternion.Euler(tiltAngleAwayFromUser, 0f, 0f);
            }

            if (hand == VRHand.Right && showPointerRay)
            {
                SetupPointerLaser();
            }
        }

        private void SetupPointerLaser()
        {
            pointerLine = GetComponent<LineRenderer>();
            if (pointerLine == null)
            {
                pointerLine = gameObject.AddComponent<LineRenderer>();
            }

            pointerLine.useWorldSpace = true;
            pointerLine.positionCount = 2;
            pointerLine.startWidth = 0.006f;
            pointerLine.endWidth = 0.002f;
            pointerLine.material = sharedLaserMat;
            pointerLine.startColor = new Color(0.95f, 0.60f, 0.15f, 0.85f); // Amber ray
            pointerLine.endColor = new Color(0.95f, 0.60f, 0.15f, 0.15f);
            pointerLine.enabled = true;
        }

        private void UpdatePointerRay()
        {
            if (pointerLine == null || !pointerLine.enabled || pointerTip == null) return;

            Vector3 startPos = pointerTip.position;
            Vector3 fwd = pointerTip.forward;
            Vector3 endPos = startPos + fwd * pointerDistance;

            if (Physics.Raycast(startPos, fwd, out RaycastHit hit, pointerDistance))
            {
                endPos = hit.point;
            }

            pointerLine.SetPosition(0, startPos);
            pointerLine.SetPosition(1, endPos);
        }

        private static void StripCollider(GameObject obj)
        {
            var col = obj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }

        private static void EnsureMaterials()
        {
            if (sharedGripMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                sharedGripMat = new Material(shader) { name = "Mat_VR_Grip_Dark" };
                sharedGripMat.color = new Color(0.12f, 0.14f, 0.16f, 1f);
                if (sharedGripMat.HasProperty("_BaseColor")) sharedGripMat.SetColor("_BaseColor", sharedGripMat.color);
                if (sharedGripMat.HasProperty("_Smoothness")) sharedGripMat.SetFloat("_Smoothness", 0.4f);
            }

            if (sharedLeftHandMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                sharedLeftHandMat = new Material(shader) { name = "Mat_VR_LeftHand_Cyan" };
                sharedLeftHandMat.color = new Color(0.18f, 0.65f, 0.88f, 1f); // Cyan accent
                if (sharedLeftHandMat.HasProperty("_BaseColor")) sharedLeftHandMat.SetColor("_BaseColor", sharedLeftHandMat.color);
            }

            if (sharedRightHandMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                sharedRightHandMat = new Material(shader) { name = "Mat_VR_RightHand_Amber" };
                sharedRightHandMat.color = new Color(0.95f, 0.58f, 0.12f, 1f); // Safety Amber accent
                if (sharedRightHandMat.HasProperty("_BaseColor")) sharedRightHandMat.SetColor("_BaseColor", sharedRightHandMat.color);
            }

            if (sharedLaserMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                sharedLaserMat = new Material(shader) { name = "Mat_VR_Pointer_Laser" };
                sharedLaserMat.color = new Color(0.95f, 0.60f, 0.15f, 0.8f);
            }
        }
    }
}
