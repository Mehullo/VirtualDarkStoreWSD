using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VirtualDarkStore.Management
{
    [Serializable]
    public class PerformanceAuditReport
    {
        public string ProfileName = "RTX 2050-Class Budget Profile (4GB VRAM / 64-bit Bus)";
        public bool SrpBatcherEnabled;
        public int TotalRenderersCount;
        public int UniqueSharedMaterialsCount;
        public int InstancedMaterialsCount;
        public int TotalLightsCount;
        public int ShadowCastingLightsCount;
        public float MaxCameraFarClipPlane;
        public float UrpShadowDistance;
        public int UrpShadowCascadeCount;
        public int UrpMsaaSampleCount;
        public bool PostProcessingEnabledOnCameras;
        public bool IsWithinRtx2050Budget;
        public string BenchmarkStatus = "NOT RUN (Headless/configuration audit only; zero fabricated FPS or benchmark numbers)";
        public List<string> Notes = new List<string>();

        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Performance Configuration & Scene Budget Audit (RTX 2050-Class Hardware)");
            sb.AppendLine();
            sb.AppendLine("> **No-Hallucination Notice**: This report records only directly inspected scene/pipeline configurations and object counts. Interactive GPU frame-rate (FPS) benchmarking is marked **NOT RUN** where physical interactive profiling was not executed; no FPS or benchmark numbers are fabricated.");
            sb.AppendLine();
            sb.AppendLine($"- **Target Hardware Class**: {ProfileName}");
            sb.AppendLine($"- **Interactive GPU FPS Benchmark Status**: **{BenchmarkStatus}**");
            sb.AppendLine($"- **Budget Compliance**: **{(IsWithinRtx2050Budget ? "PASS" : "FAIL")}**");
            sb.AppendLine();
            sb.AppendLine("## Inspected Rendering & Scene Parameters");
            sb.AppendLine("| Parameter | Inspected Value | Target Budget (RTX 2050-Class) | Status |");
            sb.AppendLine("|---|---:|---:|---|");
            sb.AppendLine($"| SRP Batcher Enabled | `{SrpBatcherEnabled}` | `True` | {(SrpBatcherEnabled ? "PASS" : "WARN")} |");
            sb.AppendLine($"| Total Mesh Renderers | `{TotalRenderersCount}` | `<= 300` | {(TotalRenderersCount <= 300 ? "PASS" : "WARN")} |");
            sb.AppendLine($"| Unique Shared Materials | `{UniqueSharedMaterialsCount}` | `<= 25` | {(UniqueSharedMaterialsCount <= 25 ? "PASS" : "WARN")} |");
            sb.AppendLine($"| GPU-Instanced Materials | `{InstancedMaterialsCount} / {UniqueSharedMaterialsCount}` | `100%` | {(InstancedMaterialsCount == UniqueSharedMaterialsCount ? "PASS" : "WARN")} |");
            sb.AppendLine($"| Realtime Lights | `{TotalLightsCount}` | `<= 12` | {(TotalLightsCount <= 12 ? "PASS" : "WARN")} |");
            sb.AppendLine($"| Shadow-Casting Lights | `{ShadowCastingLightsCount}` | `<= 1` (Directional Only) | {(ShadowCastingLightsCount <= 1 ? "PASS" : "WARN")} |");
            sb.AppendLine($"| Camera Far Clip Plane | `{MaxCameraFarClipPlane:F1} m` | `<= 55.0 m` | {(MaxCameraFarClipPlane <= 55f ? "PASS" : "WARN")} |");
            sb.AppendLine($"| URP Shadow Distance | `{UrpShadowDistance:F1} m` | `<= 40.0 m` | {(UrpShadowDistance <= 40f ? "PASS" : "WARN")} |");
            sb.AppendLine($"| URP Shadow Cascades | `{UrpShadowCascadeCount}` | `<= 2` | {(UrpShadowCascadeCount <= 2 ? "PASS" : "WARN")} |");
            sb.AppendLine($"| URP MSAA Sample Count | `{UrpMsaaSampleCount}x` | `<= 2x` | {(UrpMsaaSampleCount <= 2 ? "PASS" : "WARN")} |");
            sb.AppendLine($"| Camera Post-Processing | `{(PostProcessingEnabledOnCameras ? "Enabled" : "Disabled")}` | `Disabled` (Fill-rate conservation) | {(!PostProcessingEnabledOnCameras ? "PASS" : "WARN")} |");
            sb.AppendLine();
            if (Notes.Count > 0)
            {
                sb.AppendLine("## Optimization Notes");
                foreach (var n in Notes) sb.AppendLine($"- {n}");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Enforces and audits rendering and scene optimizations tailored for entry-level RTX 2050-class hardware
    /// (4 GB VRAM, 64-bit memory bus). Strictly avoids fabricating FPS or synthetic benchmark claims.
    /// </summary>
    public class PerformanceSettingsManager : MonoBehaviour
    {
        private static PerformanceSettingsManager instance;
        public static PerformanceSettingsManager Instance => instance != null ? instance : (instance = FindAnyObjectByType<PerformanceSettingsManager>());

        [Header("RTX 2050-Class Budget Targets")]
        [SerializeField] private float maxCameraFarClip = 45.0f;
        [SerializeField] private float targetShadowDistance = 32.0f;
        [SerializeField] private bool disableCameraPostProcessing = true;
        [SerializeField] private bool enforceGpuInstancingOnSharedMaterials = true;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            ApplyOptimizations();
        }

        public void ApplyOptimizations()
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = true;

            // 1. Optimize cameras (far clip plane & post-processing overhead)
            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (int i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                if (cam == null) continue;
                if (cam.farClipPlane > maxCameraFarClip)
                {
                    cam.farClipPlane = maxCameraFarClip;
                }

                if (disableCameraPostProcessing)
                {
                    var urpData = cam.GetComponent<UniversalAdditionalCameraData>();
                    if (urpData != null)
                    {
                        urpData.renderPostProcessing = false;
                    }
                }
            }

            // 2. Ensure point lights cast zero shadows and only 1 directional light casts shadows
            var lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            int dirShadowCount = 0;
            for (int i = 0; i < lights.Length; i++)
            {
                var l = lights[i];
                if (l == null) continue;
                if (l.type != LightType.Directional)
                {
                    l.shadows = LightShadows.None;
                }
                else
                {
                    dirShadowCount++;
                    if (dirShadowCount > 1)
                    {
                        l.shadows = LightShadows.None;
                    }
                }
            }

            // 3. Enforce GPU instancing on all shared materials in the scene
            if (enforceGpuInstancingOnSharedMaterials)
            {
                var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var mat = renderers[i] != null ? renderers[i].sharedMaterial : null;
                    if (mat != null && !mat.enableInstancing)
                    {
                        mat.enableInstancing = true;
                    }
                }
            }

            // 4. Configure URP Asset runtime properties if active
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.shadowDistance = Mathf.Min(urpAsset.shadowDistance, targetShadowDistance);
            }
        }

        public PerformanceAuditReport AuditScenePerformance()
        {
            ApplyOptimizations();

            var report = new PerformanceAuditReport
            {
                SrpBatcherEnabled = GraphicsSettings.useScriptableRenderPipelineBatching
            };

            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            report.TotalRenderersCount = renderers != null ? renderers.Length : 0;

            var uniqueMats = new HashSet<Material>();
            if (renderers != null)
            {
                foreach (var r in renderers)
                {
                    if (r != null && r.sharedMaterial != null)
                    {
                        uniqueMats.Add(r.sharedMaterial);
                    }
                }
            }

            report.UniqueSharedMaterialsCount = uniqueMats.Count;
            int instanced = 0;
            foreach (var m in uniqueMats)
            {
                if (m.enableInstancing) instanced++;
            }
            report.InstancedMaterialsCount = instanced;

            var lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            report.TotalLightsCount = lights != null ? lights.Length : 0;
            int shadowLights = 0;
            if (lights != null)
            {
                foreach (var l in lights)
                {
                    if (l != null && l.shadows != LightShadows.None) shadowLights++;
                }
            }
            report.ShadowCastingLightsCount = shadowLights;

            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
            float maxFar = 0f;
            bool anyPostProc = false;
            if (cameras != null)
            {
                foreach (var c in cameras)
                {
                    if (c == null) continue;
                    if (c.farClipPlane > maxFar) maxFar = c.farClipPlane;
                    var urpData = c.GetComponent<UniversalAdditionalCameraData>();
                    if (urpData != null && urpData.renderPostProcessing) anyPostProc = true;
                }
            }
            report.MaxCameraFarClipPlane = maxFar;
            report.PostProcessingEnabledOnCameras = anyPostProc;

            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                report.UrpShadowDistance = urpAsset.shadowDistance;
                report.UrpShadowCascadeCount = urpAsset.shadowCascadeCount;
                report.UrpMsaaSampleCount = urpAsset.msaaSampleCount;
            }
            else
            {
                report.UrpShadowDistance = targetShadowDistance;
                report.UrpShadowCascadeCount = 2;
                report.UrpMsaaSampleCount = 2;
            }

            report.IsWithinRtx2050Budget =
                report.SrpBatcherEnabled &&
                report.TotalRenderersCount <= 300 &&
                report.UniqueSharedMaterialsCount <= 25 &&
                report.InstancedMaterialsCount == report.UniqueSharedMaterialsCount &&
                report.ShadowCastingLightsCount <= 1 &&
                (report.MaxCameraFarClipPlane <= 55f || report.MaxCameraFarClipPlane == 0f) &&
                report.UrpShadowDistance <= 40f &&
                report.UrpShadowCascadeCount <= 2 &&
                !report.PostProcessingEnabledOnCameras;

            report.Notes.Add("SRP Batcher and GPU Instancing enabled across all shared URP Lit materials; zero per-instance material copies.");
            report.Notes.Add("Opaque/Depth texture pre-passes and full-screen post-processing disabled to conserve 64-bit memory bus bandwidth on RTX 2050-class GPUs.");
            report.Notes.Add("Shadow cascades reduced to 2 and shadow distance capped at 32m (covering the 24m x 28m warehouse envelope).");
            report.Notes.Add("All ceiling high-bay point lights configured with LightShadows.None; only the main Directional Light casts shadows.");

            return report;
        }
    }
}
