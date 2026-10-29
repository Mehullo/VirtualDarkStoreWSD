using System;
using UnityEngine;
using Unity.XR.CoreUtils;
using VirtualDarkStore.Interaction;

namespace VirtualDarkStore.VR
{
    /// <summary>
    /// Master VR player rig component managing the XROrigin hierarchy, spatial tracking,
    /// dual controller inputs, pick interactions, and locomotion.
    /// Provides identical telemetry interface to the shared WorkStudy and ActivityClassifier core.
    /// </summary>
    public class VRPlayerRig : MonoBehaviour
    {
        [Header("Hierarchy References")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Transform cameraOffset;
        [SerializeField] private Camera hmdCamera;
        [SerializeField] private VRHeadTracking headTracking;
        [SerializeField] private VRControllerInput leftController;
        [SerializeField] private VRControllerInput rightController;
        [SerializeField] private VRPickerInteraction pickerInteraction;
        [SerializeField] private VRLocomotionController locomotionController;
        [SerializeField] private CharacterController characterController;

        public XROrigin XrOrigin => xrOrigin;
        public Camera HmdCamera => hmdCamera;
        public VRHeadTracking HeadTracking => headTracking;
        public VRControllerInput LeftController => leftController;
        public VRControllerInput RightController => rightController;
        public VRPickerInteraction PickerInteraction => pickerInteraction;
        public VRLocomotionController LocomotionController => locomotionController;
        public CharacterController CharacterController => characterController;

        public Vector3 FloorPosition => new Vector3(transform.position.x, 0f, transform.position.z);
        public Vector3 HeadPosition => headTracking != null ? headTracking.HeadPosition : (hmdCamera != null ? hmdCamera.transform.position : transform.position);
        public Vector3 ForwardDirection
        {
            get
            {
                Vector3 fwd = headTracking != null ? headTracking.Forward : transform.forward;
                fwd.y = 0f;
                return fwd.normalized;
            }
        }

        private void Awake()
        {
            InitializeRig();
        }

        public void InitializeRig()
        {
            if (xrOrigin == null) xrOrigin = GetComponent<XROrigin>();
            if (characterController == null) characterController = GetComponent<CharacterController>();

            if (headTracking == null) headTracking = GetComponentInChildren<VRHeadTracking>();
            if (hmdCamera == null && headTracking != null) hmdCamera = headTracking.HmdCamera;
            if (hmdCamera == null) hmdCamera = GetComponentInChildren<Camera>();

            if (pickerInteraction == null) pickerInteraction = GetComponentInChildren<VRPickerInteraction>();
            if (locomotionController == null) locomotionController = GetComponentInChildren<VRLocomotionController>();

            var controllers = GetComponentsInChildren<VRControllerInput>();
            foreach (var c in controllers)
            {
                if (c.Hand == VRHand.Left && leftController == null) leftController = c;
                if (c.Hand == VRHand.Right && rightController == null) rightController = c;
            }

            if (locomotionController != null)
            {
                locomotionController.SetReferences(leftController != null ? leftController : rightController, hmdCamera != null ? hmdCamera.transform : transform, characterController);
            }

            if (pickerInteraction != null)
            {
                pickerInteraction.SetReferences(rightController != null ? rightController : leftController, hmdCamera);
            }
        }

        /// <summary>
        /// Moves the entire rig to the specified warehouse spawn point.
        /// </summary>
        public void TeleportToSpawnPoint(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (characterController != null) characterController.enabled = false;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            if (characterController != null) characterController.enabled = true;
        }
    }
}
