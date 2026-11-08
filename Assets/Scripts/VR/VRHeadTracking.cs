using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

namespace VirtualDarkStore.VR
{
    /// <summary>
    /// Component providing head tracking for the VR player rig.
    /// Strictly tracks physical HMD transform orientation and position;
    /// excludes uninstrumented eye-gaze, cognitive, or physiological claims.
    /// </summary>
    public class VRHeadTracking : MonoBehaviour
    {
        [Header("Camera & Tracking")]
        [SerializeField] private Camera hmdCamera;
        [SerializeField] private float eyeHeight = 1.68f;

        [Header("State")]
        [SerializeField] private bool isTracked = false;
        [SerializeField] private Vector3 headLocalPosition = Vector3.zero;
        [SerializeField] private Quaternion headLocalRotation = Quaternion.identity;

        public Camera HmdCamera => hmdCamera;
        public bool IsTracked => isTracked;
        public Vector3 HeadPosition => hmdCamera != null ? hmdCamera.transform.position : transform.position;
        public Quaternion HeadRotation => hmdCamera != null ? hmdCamera.transform.rotation : transform.rotation;
        public Vector3 Forward => hmdCamera != null ? hmdCamera.transform.forward : transform.forward;

        /// <summary>
        /// Ray projecting along the physical HMD head forward axis.
        /// Strictly represents head-orientation raycast, NOT physiological eye-tracking.
        /// </summary>
        public Ray HeadRay => new Ray(HeadPosition, Forward);

        private void Awake()
        {
            EnsureCameraReference();
        }

        public void EnsureCameraReference()
        {
            if (hmdCamera == null)
            {
                hmdCamera = GetComponent<Camera>();
                if (hmdCamera == null) hmdCamera = GetComponentInChildren<Camera>();
            }

            if (hmdCamera != null && hmdCamera.transform.localPosition == Vector3.zero)
            {
                hmdCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            }
        }

        private void Update()
        {
            PollHeadTracking();
        }

        public void PollHeadTracking()
        {
            EnsureCameraReference();
#if ENABLE_INPUT_SYSTEM
            var hmd = InputSystem.GetDevice<XRHMD>();
            if (hmd != null && hmd.added)
            {
                isTracked = hmd.trackingState.ReadValue() != 0;
                if (isTracked)
                {
                    headLocalPosition = hmd.centerEyePosition.ReadValue();
                    headLocalRotation = hmd.centerEyeRotation.ReadValue();

                    if (hmdCamera != null)
                    {
                        hmdCamera.transform.localPosition = headLocalPosition;
                        hmdCamera.transform.localRotation = headLocalRotation;
                    }
                    return;
                }
            }
#endif
            // Fallback: UnityEngine.XR.InputDevices
            var headDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye);
            if (!headDevice.isValid) headDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
            if (headDevice.isValid)
            {
                isTracked = true;
                if (headDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 dPos)) headLocalPosition = dPos;
                if (headDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion dRot)) headLocalRotation = dRot;
                if (hmdCamera != null)
                {
                    hmdCamera.transform.localPosition = headLocalPosition;
                    hmdCamera.transform.localRotation = headLocalRotation;
                }
                return;
            }

            // Fallback for editor simulation or unattached HMD
            if (hmdCamera != null)
            {
                headLocalPosition = hmdCamera.transform.localPosition;
                headLocalRotation = hmdCamera.transform.localRotation;
            }
        }

        /// <summary>
        /// Manually set simulated head pose (useful for headless testing).
        /// </summary>
        public void SetSimulatedPose(Vector3 localPosition, Quaternion localRotation)
        {
            EnsureCameraReference();
            headLocalPosition = localPosition;
            headLocalRotation = localRotation;
            if (hmdCamera != null)
            {
                hmdCamera.transform.localPosition = localPosition;
                hmdCamera.transform.localRotation = localRotation;
            }
            isTracked = true;
        }
    }
}
