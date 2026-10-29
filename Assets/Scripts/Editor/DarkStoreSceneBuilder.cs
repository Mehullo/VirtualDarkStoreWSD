#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VirtualDarkStore.Domain;
using VirtualDarkStore.Environment;
using VirtualDarkStore.Interaction;
using VirtualDarkStore.Management;
using VirtualDarkStore.Player;
using VirtualDarkStore.Scenarios;
using VirtualDarkStore.UI;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.Editor
{
    public static class DarkStoreSceneBuilder
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string ScenePath = "Assets/Scenes/DarkStore_Phase1.unity";
        private const string MaterialFolder = "Assets/Materials";

        [MenuItem("DarkStore/Build Phase 1 Scene")]
        public static void BuildDarkStorePhase1Scene()
        {
            Debug.Log("[DarkStoreSceneBuilder] Starting Dark Store Scene Build (Phase 8 Polish & RTX 2050 Optimization)...");

            if (!Directory.Exists(MaterialFolder))
            {
                Directory.CreateDirectory(MaterialFolder);
            }
            if (!Directory.Exists(SceneFolder))
            {
                Directory.CreateDirectory(SceneFolder);
            }

            // Create a new blank scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create shared GPU-instanced URP materials
            var floorMat = GetOrCreateMaterial("Mat_Floor_Concrete", new Color(0.32f, 0.34f, 0.36f), 0.08f, 0.28f);
            var wallMat = GetOrCreateMaterial("Mat_Wall_Drywall", new Color(0.76f, 0.78f, 0.80f), 0.0f, 0.2f);
            var shelfMat = GetOrCreateMaterial("Mat_Shelf_Metal", new Color(0.20f, 0.30f, 0.44f), 0.4f, 0.55f); // Industrial blue uprights
            var beamMat = GetOrCreateMaterial("Mat_Shelf_Beams", new Color(0.88f, 0.46f, 0.12f), 0.2f, 0.5f); // Safety orange beams
            var benchMat = GetOrCreateMaterial("Mat_Packing_Bench", new Color(0.26f, 0.28f, 0.31f), 0.15f, 0.4f);
            var zoneMat = GetOrCreateMaterial("Mat_Staging_Zone", new Color(0.95f, 0.78f, 0.15f), 0.0f, 0.25f); // Safety yellow
            var laneMat = GetOrCreateMaterial("Mat_Aisle_Guide", new Color(0.22f, 0.55f, 0.78f), 0.0f, 0.25f); // Aisle center stripe
            var toteMat = GetOrCreateMaterial("Mat_Tote_Container", new Color(0.15f, 0.48f, 0.82f), 0.1f, 0.45f); // Dispatch tote bin

            // 1. Root [Environment] Hierarchy
            var envRoot = new GameObject("[Environment]");

            SetupLighting(envRoot);
            SetupBuildingEnvelope(envRoot, floorMat, wallMat, zoneMat, laneMat);

            // Aisles & Shelving Racks (4 Aisles)
            var aislesRoot = new GameObject("Aisles");
            aislesRoot.transform.SetParent(envRoot.transform);

            float[] aisleXPositions = { -6.0f, -2.0f, 2.0f, 6.0f };
            float aisleLength = 12.0f;
            float rackDepth = 0.6f;
            float rackHeight = 2.4f;
            float aisleCorridorWidth = 1.8f;

            for (int i = 0; i < aisleXPositions.Length; i++)
            {
                int aisleNum = i + 1;
                float x = aisleXPositions[i];
                BuildAisle(aislesRoot, aisleNum, x, aisleLength, aisleCorridorWidth, rackDepth, rackHeight, shelfMat, beamMat, zoneMat);
            }

            // Packing & Staging Station
            BuildPackingStation(envRoot, benchMat, zoneMat, toteMat);

            // Physical Shelf Products (72 SKUs across all 4 aisles)
            BuildShelfProducts(envRoot);

            // 2. Player Rigs (Desktop active by default; VR ready & inactive until selected)
            BuildPlayerController();
            BuildVRPlayerRig(null, false);

            // 3. Core Simulation & Work-Study Managers
            var storeObj = new GameObject("[StoreManager]");
            storeObj.AddComponent<StoreManager>();

            var orderObj = new GameObject("[OrderManager]");
            orderObj.AddComponent<OrderManager>();

            var hudObj = new GameObject("[SimulationHUD]");
            hudObj.AddComponent<SimulationHUD>();

            var workStudyObj = new GameObject("[WorkStudyManager]");
            workStudyObj.AddComponent<WorkEventLogger>();
            workStudyObj.AddComponent<ActivityClassifier>();
            workStudyObj.AddComponent<WorkStudyResultsUI>();
            workStudyObj.AddComponent<ScenarioComparisonUI>();
            workStudyObj.AddComponent<ScenarioRouteVisualizer>();
            workStudyObj.AddComponent<DemoFlowController>();
            workStudyObj.AddComponent<SimulationErrorHandler>();
            var perfMgr = workStudyObj.AddComponent<PerformanceSettingsManager>();
            perfMgr.ApplyOptimizations();

            // 4. Bake NavMesh Geometry
            BakeNavMeshGeometry();

            // 5. Save Scene & Register in Build Settings
            EditorSceneManager.SaveScene(newScene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DarkStoreSceneBuilder] Successfully generated and saved scene to {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);
        }

        private static Material GetOrCreateMaterial(string name, Color color, float metallic, float smoothness)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetupLighting(GameObject parent)
        {
            var lightingRoot = new GameObject("Lighting");
            lightingRoot.transform.SetParent(parent.transform);

            var lightObj = new GameObject("Directional Light");
            lightObj.transform.SetParent(lightingRoot.transform);
            lightObj.transform.position = new Vector3(0f, 6f, 0f);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.98f, 0.94f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;

            // RTX 2050 budget: 8 high-bay ceiling point lights (2 per aisle) with LightShadows.None
            var ceilingLights = new GameObject("Ceiling_HighBay_Lights");
            ceilingLights.transform.SetParent(lightingRoot.transform);

            float[] xPos = { -6f, -2f, 2f, 6f };
            float[] zPos = { -1.5f, 5.5f };

            foreach (var x in xPos)
            {
                foreach (var z in zPos)
                {
                    var ptLight = new GameObject($"HighBayLight_X{x}_Z{z}");
                    ptLight.transform.SetParent(ceilingLights.transform);
                    ptLight.transform.position = new Vector3(x, 3.8f, z);

                    var pl = ptLight.AddComponent<Light>();
                    pl.type = LightType.Point;
                    pl.range = 8.5f;
                    pl.intensity = 1.6f;
                    pl.color = new Color(0.95f, 0.97f, 1.0f);
                    pl.shadows = LightShadows.None;
                }
            }
        }

        private static void SetupBuildingEnvelope(GameObject parent, Material floorMat, Material wallMat, Material safetyMat, Material laneMat)
        {
            var envelope = new GameObject("Envelope");
            envelope.transform.SetParent(parent.transform);

            // Floor: 24m wide (X) x 28m deep (Z)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(envelope.transform);
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(24.0f, 0.2f, 28.0f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);

            // Perimeter walls (Height: 4.5m, Thickness: 0.3m)
            float wallHeight = 4.5f;
            float wallThickness = 0.3f;
            float halfX = 12.0f;
            float halfZ = 14.0f;

            CreateWall(envelope, "Wall_North", new Vector3(0f, wallHeight * 0.5f, halfZ), new Vector3(24.0f, wallHeight, wallThickness), wallMat);
            CreateWall(envelope, "Wall_South", new Vector3(0f, wallHeight * 0.5f, -halfZ), new Vector3(24.0f, wallHeight, wallThickness), wallMat);
            CreateWall(envelope, "Wall_East", new Vector3(halfX, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, 28.0f), wallMat);
            CreateWall(envelope, "Wall_West", new Vector3(-halfX, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, 28.0f), wallMat);

            // Floor Safety & Navigation Markings (No colliders, zero shadow casting)
            var markingsRoot = new GameObject("FloorMarkings");
            markingsRoot.transform.SetParent(envelope.transform);

            CreateFloorStripe(markingsRoot, "CrossAisle_SafetyLine_Front", new Vector3(0f, 0.005f, -5.2f), new Vector3(16.0f, 0.01f, 0.12f), safetyMat);
            CreateFloorStripe(markingsRoot, "CrossAisle_SafetyLine_Back", new Vector3(0f, 0.005f, 8.8f), new Vector3(16.0f, 0.01f, 0.12f), safetyMat);

            float[] aisleX = { -6.0f, -2.0f, 2.0f, 6.0f };
            for (int i = 0; i < aisleX.Length; i++)
            {
                CreateFloorStripe(markingsRoot, $"Aisle_{i + 1:D2}_CenterLine", new Vector3(aisleX[i], 0.004f, 2.0f), new Vector3(0.08f, 0.01f, 12.0f), laneMat);
            }
        }

        private static void CreateFloorStripe(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = name;
            stripe.transform.SetParent(parent.transform);
            stripe.transform.position = pos;
            stripe.transform.localScale = scale;
            var col = stripe.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var mr = stripe.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(stripe, StaticEditorFlags.BatchingStatic);
        }

        private static void CreateWall(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent.transform);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
        }

        private static void BuildAisle(GameObject parent, int aisleNum, float aisleCenterX, float length, float aisleWidth, float rackDepth, float rackHeight, Material shelfMat, Material beamMat, Material signMat)
        {
            var aisleObj = new GameObject($"Aisle_{aisleNum:D2}");
            aisleObj.transform.SetParent(parent.transform);
            aisleObj.transform.position = new Vector3(aisleCenterX, 0f, 2.0f);

            var frontWaypoint = new GameObject("FrontEntrance").transform;
            frontWaypoint.SetParent(aisleObj.transform);
            frontWaypoint.position = new Vector3(aisleCenterX, 0f, -4.5f);

            var backWaypoint = new GameObject("BackExit").transform;
            backWaypoint.SetParent(aisleObj.transform);
            backWaypoint.position = new Vector3(aisleCenterX, 0f, 8.5f);

            var aisleComp = aisleObj.AddComponent<DarkStoreAisle>();
            aisleComp.Initialize(aisleNum, $"Aisle {aisleNum:D2}", aisleWidth, length, frontWaypoint, backWaypoint);

            // Overhead Aisle Header Arch / Signboard at front entrance (Z = -4.0m)
            var signArch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signArch.name = $"Aisle_{aisleNum:D2}_HeaderSign";
            signArch.transform.SetParent(aisleObj.transform);
            signArch.transform.position = new Vector3(aisleCenterX, rackHeight + 0.15f, -3.95f);
            signArch.transform.localScale = new Vector3(aisleWidth + rackDepth * 2f, 0.28f, 0.08f);
            var signCol = signArch.GetComponent<Collider>();
            if (signCol != null) Object.DestroyImmediate(signCol);
            var signRenderer = signArch.GetComponent<MeshRenderer>();
            signRenderer.sharedMaterial = signMat;
            signRenderer.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(signArch, StaticEditorFlags.BatchingStatic);

            // Left Rack
            float leftRackX = aisleCenterX - (aisleWidth * 0.5f + rackDepth * 0.5f);
            BuildRack(aisleObj, aisleNum, ShelfSide.Left, new Vector3(leftRackX, 0f, 2.0f), length, rackDepth, rackHeight, shelfMat, beamMat);

            // Right Rack
            float rightRackX = aisleCenterX + (aisleWidth * 0.5f + rackDepth * 0.5f);
            BuildRack(aisleObj, aisleNum, ShelfSide.Right, new Vector3(rightRackX, 0f, 2.0f), length, rackDepth, rackHeight, shelfMat, beamMat);
        }

        private static void BuildRack(GameObject aisleParent, int aisleNum, ShelfSide side, Vector3 centerPos, float length, float depth, float height, Material uprightMat, Material beamMat)
        {
            var rackObj = new GameObject($"Rack_{side}_{aisleNum:D2}");
            rackObj.transform.SetParent(aisleParent.transform);
            rackObj.transform.position = centerPos;

            var shelfUnit = rackObj.AddComponent<ShelfUnit>();
            shelfUnit.Initialize(aisleNum, side, 1, 3, depth, length, height);

            int bayCount = 3;
            float bayLength = length / bayCount;
            float startZ = -length * 0.5f;

            for (int b = 0; b <= bayCount; b++)
            {
                float z = startZ + b * bayLength;
                var upright = GameObject.CreatePrimitive(PrimitiveType.Cube);
                upright.name = $"Upright_{b}";
                upright.transform.SetParent(rackObj.transform);
                upright.transform.position = centerPos + new Vector3(0f, height * 0.5f, z);
                upright.transform.localScale = new Vector3(depth * 0.95f, height, 0.08f);
                var upCol = upright.GetComponent<Collider>();
                if (upCol != null) Object.DestroyImmediate(upCol);
                upright.GetComponent<MeshRenderer>().sharedMaterial = uprightMat;
                GameObjectUtility.SetStaticEditorFlags(upright, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
            }

            float[] tierHeights = { 0.25f, 1.05f, 1.85f };
            for (int t = 0; t < tierHeights.Length; t++)
            {
                var shelfBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelfBeam.name = $"ShelfTier_{t + 1}";
                shelfBeam.transform.SetParent(rackObj.transform);
                shelfBeam.transform.position = centerPos + new Vector3(0f, tierHeights[t], 0f);
                shelfBeam.transform.localScale = new Vector3(depth, 0.06f, length);
                var beamCol = shelfBeam.GetComponent<Collider>();
                if (beamCol != null) Object.DestroyImmediate(beamCol);
                shelfBeam.GetComponent<MeshRenderer>().sharedMaterial = beamMat;
                GameObjectUtility.SetStaticEditorFlags(shelfBeam, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
            }

            // Solid Rack BoxCollider for physics collision and raycast shelf detection
            var mainColliderObj = new GameObject("Rack_Collider");
            mainColliderObj.transform.SetParent(rackObj.transform);
            mainColliderObj.transform.position = centerPos + new Vector3(0f, height * 0.5f, 0f);
            var col = mainColliderObj.AddComponent<BoxCollider>();
            col.size = new Vector3(depth, height, length);
            GameObjectUtility.SetStaticEditorFlags(mainColliderObj, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
        }

        private static void BuildPackingStation(GameObject parent, Material benchMat, Material zoneMat, Material toteMat)
        {
            var stationRoot = new GameObject("PackingStation_01");
            stationRoot.transform.SetParent(parent.transform);
            stationRoot.transform.position = new Vector3(0f, 0f, -9.5f);

            // Packing Bench (Table)
            var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "PackingBench";
            bench.transform.SetParent(stationRoot.transform);
            bench.transform.position = new Vector3(0f, 0.45f, -9.5f);
            bench.transform.localScale = new Vector3(3.2f, 0.9f, 1.2f);
            bench.GetComponent<MeshRenderer>().sharedMaterial = benchMat;
            GameObjectUtility.SetStaticEditorFlags(bench, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);

            // Station Computer Terminal
            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "StationTerminal";
            screen.transform.SetParent(stationRoot.transform);
            screen.transform.position = new Vector3(0f, 1.25f, -9.3f);
            screen.transform.localScale = new Vector3(0.65f, 0.42f, 0.06f);
            screen.GetComponent<MeshRenderer>().sharedMaterial = benchMat;
            GameObjectUtility.SetStaticEditorFlags(screen, StaticEditorFlags.BatchingStatic);

            // Order Tote Bins on Staging Bench
            var tote1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tote1.name = "DispatchTote_01";
            tote1.transform.SetParent(stationRoot.transform);
            tote1.transform.position = new Vector3(-1.0f, 1.02f, -9.5f);
            tote1.transform.localScale = new Vector3(0.55f, 0.24f, 0.40f);
            tote1.GetComponent<MeshRenderer>().sharedMaterial = toteMat;
            GameObjectUtility.SetStaticEditorFlags(tote1, StaticEditorFlags.BatchingStatic);

            var tote2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tote2.name = "DispatchTote_02";
            tote2.transform.SetParent(stationRoot.transform);
            tote2.transform.position = new Vector3(1.0f, 1.02f, -9.5f);
            tote2.transform.localScale = new Vector3(0.55f, 0.24f, 0.40f);
            tote2.GetComponent<MeshRenderer>().sharedMaterial = toteMat;
            GameObjectUtility.SetStaticEditorFlags(tote2, StaticEditorFlags.BatchingStatic);

            // Tote Staging / Drop Zone Pad on Floor
            var dropPad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dropPad.name = "ToteDropZone";
            dropPad.transform.SetParent(stationRoot.transform);
            dropPad.transform.position = new Vector3(2.2f, 0.01f, -9.5f);
            dropPad.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);
            dropPad.GetComponent<MeshRenderer>().sharedMaterial = zoneMat;
            GameObjectUtility.SetStaticEditorFlags(dropPad, StaticEditorFlags.BatchingStatic);

            // Worker Spawn Point
            var spawnPoint = new GameObject("SpawnPoint").transform;
            spawnPoint.SetParent(stationRoot.transform);
            spawnPoint.position = new Vector3(0f, 0f, -8.0f);
            spawnPoint.rotation = Quaternion.identity;

            var comp = stationRoot.AddComponent<PackingStation>();
            comp.Initialize("PACK-01", "Dispatch Packing Station 1", bench.transform, dropPad.transform, spawnPoint);
        }

        private static void BuildPlayerController()
        {
            var playerObj = new GameObject("DesktopPicker");
            playerObj.transform.position = new Vector3(0f, 0f, -8.0f);
            playerObj.transform.rotation = Quaternion.identity;

            var cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.08f;
            cc.minMoveDistance = 0.001f;

            var camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(playerObj.transform);
            camHolder.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camHolder.transform.localRotation = Quaternion.identity;

            var camObj = new GameObject("MainCamera");
            camObj.transform.SetParent(camHolder.transform);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;
            camObj.tag = "MainCamera";

            var cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 45f; // RTX 2050 optimization: covers 24m x 28m warehouse envelope cleanly
            cam.fieldOfView = 75f;

            var urpCamData = camObj.AddComponent<UniversalAdditionalCameraData>();
            if (urpCamData != null)
            {
                urpCamData.renderPostProcessing = false; // Conserve fill-rate on RTX 2050-class GPUs
            }

            camObj.AddComponent<AudioListener>();

            var controller = playerObj.AddComponent<DesktopPickerController>();
            var camField = typeof(DesktopPickerController).GetField("cameraTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (camField != null)
            {
                camField.SetValue(controller, camHolder.transform);
            }

            playerObj.AddComponent<PickerInteraction>();
        }

        public static GameObject BuildVRPlayerRig(GameObject parent = null, bool active = false)
        {
            var vrObj = new GameObject("[VRPlayerRig]");
            if (parent != null) vrObj.transform.SetParent(parent.transform);
            vrObj.transform.position = new Vector3(0f, 0f, -8.0f);
            vrObj.transform.rotation = Quaternion.identity;

            var cc = vrObj.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var origin = vrObj.AddComponent<Unity.XR.CoreUtils.XROrigin>();
            origin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;

            var offsetObj = new GameObject("CameraOffset");
            offsetObj.transform.SetParent(vrObj.transform, false);
            origin.CameraFloorOffsetObject = offsetObj;

            var camObj = new GameObject("VRMainCamera");
            camObj.transform.SetParent(offsetObj.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 1.70f, 0f);
            var cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 45f; // RTX 2050 budget
            var urpData = camObj.AddComponent<UniversalAdditionalCameraData>();
            if (urpData != null) urpData.renderPostProcessing = false;
            camObj.AddComponent<VirtualDarkStore.VR.VRHeadTracking>();
            origin.Camera = cam;

            var leftObj = new GameObject("LeftHandController");
            leftObj.transform.SetParent(offsetObj.transform, false);
            leftObj.transform.localPosition = new Vector3(-0.25f, 1.2f, 0.35f);
            var leftInput = leftObj.AddComponent<VirtualDarkStore.VR.VRControllerInput>();
            var handField = typeof(VirtualDarkStore.VR.VRControllerInput).GetField("hand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handField != null) handField.SetValue(leftInput, VirtualDarkStore.VR.VRHand.Left);

            var rightObj = new GameObject("RightHandController");
            rightObj.transform.SetParent(offsetObj.transform, false);
            rightObj.transform.localPosition = new Vector3(0.25f, 1.2f, 0.35f);
            var rightInput = rightObj.AddComponent<VirtualDarkStore.VR.VRControllerInput>();
            if (handField != null) handField.SetValue(rightInput, VirtualDarkStore.VR.VRHand.Right);

            var loco = vrObj.AddComponent<VirtualDarkStore.VR.VRLocomotionController>();
            loco.SetReferences(leftInput, camObj.transform, cc);

            var picker = vrObj.AddComponent<VirtualDarkStore.VR.VRPickerInteraction>();
            picker.SetReferences(rightInput, cam);

            var rig = vrObj.AddComponent<VirtualDarkStore.VR.VRPlayerRig>();
            rig.InitializeRig();

            vrObj.SetActive(active);
            return vrObj;
        }

        private static void BuildShelfProducts(GameObject parent)
        {
            var productsRoot = new GameObject("ShelfProducts");
            productsRoot.transform.SetParent(parent.transform);

            var tempStoreObj = new GameObject("[TempStore]");
            var tempStore = tempStoreObj.AddComponent<StoreManager>();
            tempStore.InitializeStore();

            var catalog = tempStore.GetAllSkus();
            var catMaterials = new Dictionary<string, Material>(System.StringComparer.OrdinalIgnoreCase);

            var matBev = GetOrCreateMaterial("Mat_Prod_Beverages", new Color(0.18f, 0.48f, 0.88f), 0.1f, 0.45f);
            var matDairy = GetOrCreateMaterial("Mat_Prod_Dairy", new Color(0.92f, 0.95f, 0.98f), 0.05f, 0.5f);
            var matBakery = GetOrCreateMaterial("Mat_Prod_Bakery", new Color(0.86f, 0.62f, 0.26f), 0.0f, 0.3f);
            var matProduce = GetOrCreateMaterial("Mat_Prod_Produce", new Color(0.22f, 0.72f, 0.28f), 0.0f, 0.25f);
            var matSnacks = GetOrCreateMaterial("Mat_Prod_Snacks", new Color(0.88f, 0.24f, 0.24f), 0.1f, 0.4f);
            var matCanned = GetOrCreateMaterial("Mat_Prod_Canned", new Color(0.64f, 0.68f, 0.74f), 0.35f, 0.55f);
            var matCare = GetOrCreateMaterial("Mat_Prod_PersonalCare", new Color(0.64f, 0.36f, 0.78f), 0.1f, 0.4f);
            var matHouse = GetOrCreateMaterial("Mat_Prod_Household", new Color(0.92f, 0.82f, 0.18f), 0.1f, 0.35f);

            catMaterials["Beverages"] = matBev;
            catMaterials["Dairy"] = matDairy;
            catMaterials["Dairy & Alternatives"] = matDairy;
            catMaterials["Bakery"] = matBakery;
            catMaterials["Bakery & Breakfast"] = matBakery;
            catMaterials["Produce"] = matProduce;
            catMaterials["Fresh Produce"] = matProduce;
            catMaterials["Snacks"] = matSnacks;
            catMaterials["Snacks & Confectionery"] = matSnacks;
            catMaterials["Canned Goods"] = matCanned;
            catMaterials["Pantry & Canned Goods"] = matCanned;
            catMaterials["Personal Care"] = matCare;
            catMaterials["Household"] = matHouse;
            catMaterials["Household Cleaning"] = matHouse;

            Material defaultMat = GetOrCreateMaterial("Mat_Prod_Default", new Color(0.5f, 0.5f, 0.5f), 0.1f, 0.3f);

            foreach (var sku in catalog)
            {
                if (tempStore.TryGetSlotForSku(sku.SkuId, out var slot))
                {
                    var prodObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    prodObj.name = $"Product_{sku.SkuId}";
                    prodObj.transform.SetParent(productsRoot.transform);

                    // Size visual proportionally to SKU dimensions (clamped for clear visibility and raycast hit reliability)
                    Vector3 dim = sku.DimensionsMeters;
                    float sx = Mathf.Clamp(dim.x * 1.6f, 0.18f, 0.28f);
                    float sy = Mathf.Clamp(dim.y * 1.2f, 0.18f, 0.32f);
                    float sz = Mathf.Clamp(dim.z * 1.6f, 0.18f, 0.28f);

                    float xOffset = slot.Side == ShelfSide.Left ? 0.15f : -0.15f;
                    Vector3 itemPos = new Vector3(slot.WorldPosition.x + xOffset, slot.WorldPosition.y + (sy * 0.5f - 0.05f), slot.WorldPosition.z);
                    prodObj.transform.position = itemPos;
                    prodObj.transform.localScale = new Vector3(sx, sy, sz);

                    if (!catMaterials.TryGetValue(sku.Category, out Material mat))
                    {
                        mat = defaultMat;
                    }

                    var mr = prodObj.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = mat;
                    mr.shadowCastingMode = ShadowCastingMode.Off; // RTX 2050 optimization: saves 72 shadow-caster draw calls

                    var visualComp = prodObj.AddComponent<PickableItemVisual>();
                    visualComp.Initialize(sku.SkuId, sku.Name, slot.SlotId, mat);
                }
            }

            Object.DestroyImmediate(tempStoreObj);
            Debug.Log($"[DarkStoreSceneBuilder] Spawned {catalog.Count} physical pickable shelf product visuals across all 4 aisles.");
        }

        private static void BakeNavMeshGeometry()
        {
            try
            {
                var buildSettings = UnityEngine.AI.NavMesh.GetSettingsByID(0);
                var bounds = new Bounds(new Vector3(0f, 2f, 0f), new Vector3(30f, 10f, 32f));
                var sources = new List<UnityEngine.AI.NavMeshBuildSource>();
                var markups = new List<UnityEngine.AI.NavMeshBuildMarkup>();

                UnityEngine.AI.NavMeshBuilder.CollectSources(bounds, ~0, UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
                var data = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(buildSettings, sources, bounds, Vector3.zero, Quaternion.identity);

                if (data != null)
                {
                    string navAssetPath = "Assets/Scenes/DarkStore_Phase1_NavMesh.asset";
                    AssetDatabase.CreateAsset(data, navAssetPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[DarkStoreSceneBuilder] NavMesh built successfully with {sources.Count} collider sources and saved to {navAssetPath}.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DarkStoreSceneBuilder] Note on NavMesh bake: {ex.Message}");
            }
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var scene in scenes)
            {
                if (scene.path == scenePath) return;
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i] = scenes[i];
            }
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
            Debug.Log($"[DarkStoreSceneBuilder] Registered {scenePath} in EditorBuildSettings.");
        }
    }
}
#endif
