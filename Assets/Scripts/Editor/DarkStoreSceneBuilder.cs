#if UNITY_EDITOR
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
using VirtualDarkStore.UI;

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
            Debug.Log("[DarkStoreSceneBuilder] Starting Phase 1 Dark Store Scene Build...");

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

            // Create materials
            var floorMat = GetOrCreateMaterial("Mat_Floor_Concrete", new Color(0.35f, 0.36f, 0.37f), 0.1f, 0.3f);
            var wallMat = GetOrCreateMaterial("Mat_Wall_Drywall", new Color(0.78f, 0.79f, 0.80f), 0.0f, 0.2f);
            var shelfMat = GetOrCreateMaterial("Mat_Shelf_Metal", new Color(0.22f, 0.32f, 0.45f), 0.4f, 0.6f); // Industrial blue
            var beamMat = GetOrCreateMaterial("Mat_Shelf_Beams", new Color(0.85f, 0.45f, 0.12f), 0.2f, 0.5f); // Safety orange
            var benchMat = GetOrCreateMaterial("Mat_Packing_Bench", new Color(0.28f, 0.28f, 0.30f), 0.1f, 0.4f);
            var zoneMat = GetOrCreateMaterial("Mat_Staging_Zone", new Color(0.95f, 0.78f, 0.15f), 0.0f, 0.2f); // Yellow outline

            // Root environment parent
            var envRoot = new GameObject("[Environment]");

            // 1. Lighting & Post-Processing
            SetupLighting(envRoot);

            // 2. Floor & Perimeter Walls
            SetupBuildingEnvelope(envRoot, floorMat, wallMat);

            // 3. Aisles & Shelving Racks (4 Aisles)
            var aislesRoot = new GameObject("Aisles");
            aislesRoot.transform.SetParent(envRoot.transform);

            // 4 Aisles: X offsets: -6m, -2m, +2m, +6m
            float[] aisleXPositions = { -6.0f, -2.0f, 2.0f, 6.0f };
            float aisleLength = 12.0f;
            float rackDepth = 0.6f;
            float rackHeight = 2.4f;
            float aisleCorridorWidth = 1.8f;

            for (int i = 0; i < aisleXPositions.Length; i++)
            {
                int aisleNum = i + 1;
                float x = aisleXPositions[i];
                BuildAisle(aislesRoot, aisleNum, x, aisleLength, aisleCorridorWidth, rackDepth, rackHeight, shelfMat, beamMat);
            }

            // 4. Packing & Staging Station
            BuildPackingStation(envRoot, benchMat, zoneMat);

            // 5. Desktop Picker Player Controller
            BuildPlayerController();

            // 6. Domain StoreManager
            var storeObj = new GameObject("[StoreManager]");
            storeObj.AddComponent<StoreManager>();

            // 7. OrderManager
            var orderObj = new GameObject("[OrderManager]");
            orderObj.AddComponent<OrderManager>();

            // 8. Simulation HUD
            var hudObj = new GameObject("[SimulationHUD]");
            hudObj.AddComponent<SimulationHUD>();

            // 9. Physical Shelf Products
            BuildShelfProducts(envRoot);

            // Mark navigation static and bake NavMesh
            BakeNavMeshGeometry();

            // Save Scene
            EditorSceneManager.SaveScene(newScene, ScenePath);
            Debug.Log($"[DarkStoreSceneBuilder] Successfully generated and saved scene to {ScenePath}");

            // Ensure scene is registered in EditorBuildSettings
            RegisterSceneInBuildSettings(ScenePath);
        }

        private static Material GetOrCreateMaterial(string name, Color color, float metallic, float smoothness)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                // Use URP Lit shader
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader)
                {
                    color = color
                };

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void SetupLighting(GameObject parent)
        {
            var lightObj = new GameObject("Directional Light");
            lightObj.transform.SetParent(parent.transform);
            lightObj.transform.position = new Vector3(0f, 6f, 0f);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.97f, 0.92f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;

            // Ambient ceiling high-bay lights over aisles
            var ceilingLights = new GameObject("Ceiling_HighBay_Lights");
            ceilingLights.transform.SetParent(parent.transform);

            float[] xPos = { -6f, -2f, 2f, 6f };
            float[] zPos = { -3f, 2f, 7f };

            foreach (var x in xPos)
            {
                foreach (var z in zPos)
                {
                    var ptLight = new GameObject($"PointLight_X{x}_Z{z}");
                    ptLight.transform.SetParent(ceilingLights.transform);
                    ptLight.transform.position = new Vector3(x, 3.8f, z);

                    var pl = ptLight.AddComponent<Light>();
                    pl.type = LightType.Point;
                    pl.range = 8.0f;
                    pl.intensity = 1.8f;
                    pl.color = new Color(0.96f, 0.97f, 1.0f);
                    pl.shadows = LightShadows.None;
                }
            }
        }

        private static void SetupBuildingEnvelope(GameObject parent, Material floorMat, Material wallMat)
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

        private static void BuildAisle(GameObject parent, int aisleNum, float aisleCenterX, float length, float aisleWidth, float rackDepth, float rackHeight, Material shelfMat, Material beamMat)
        {
            var aisleObj = new GameObject($"Aisle_{aisleNum:D2}");
            aisleObj.transform.SetParent(parent.transform);
            aisleObj.transform.position = new Vector3(aisleCenterX, 0f, 2.0f); // Center of aisle along Z

            var frontWaypoint = new GameObject("FrontEntrance").transform;
            frontWaypoint.SetParent(aisleObj.transform);
            frontWaypoint.position = new Vector3(aisleCenterX, 0f, -4.5f);

            var backWaypoint = new GameObject("BackExit").transform;
            backWaypoint.SetParent(aisleObj.transform);
            backWaypoint.position = new Vector3(aisleCenterX, 0f, 8.5f);

            var aisleComp = aisleObj.AddComponent<DarkStoreAisle>();
            aisleComp.Initialize(aisleNum, $"Aisle {aisleNum:D2}", aisleWidth, length, frontWaypoint, backWaypoint);

            // Left Rack: placed at aisleCenterX - (aisleWidth / 2 + rackDepth / 2)
            float leftRackX = aisleCenterX - (aisleWidth * 0.5f + rackDepth * 0.5f);
            BuildRack(aisleObj, aisleNum, ShelfSide.Left, new Vector3(leftRackX, 0f, 2.0f), length, rackDepth, rackHeight, shelfMat, beamMat);

            // Right Rack: placed at aisleCenterX + (aisleWidth / 2 + rackDepth / 2)
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

            // 1. Upright End Posts & Dividers (4 vertical frames dividing into 3 bays)
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
                upright.GetComponent<MeshRenderer>().sharedMaterial = uprightMat;
                GameObjectUtility.SetStaticEditorFlags(upright, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
            }

            // 2. Shelving Tiers (3 horizontal shelves: bottom 0.25m, middle 1.05m, top 1.85m)
            float[] tierHeights = { 0.25f, 1.05f, 1.85f };
            for (int t = 0; t < tierHeights.Length; t++)
            {
                var shelfBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelfBeam.name = $"ShelfTier_{t + 1}";
                shelfBeam.transform.SetParent(rackObj.transform);
                shelfBeam.transform.position = centerPos + new Vector3(0f, tierHeights[t], 0f);
                shelfBeam.transform.localScale = new Vector3(depth, 0.06f, length);
                shelfBeam.GetComponent<MeshRenderer>().sharedMaterial = beamMat;
                GameObjectUtility.SetStaticEditorFlags(shelfBeam, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
            }

            // 3. Solid Back/Center Obstacle BoxCollider for absolute physical collision enforcement
            // Ensures player cannot walk through or clip through the shelving unit
            var mainColliderObj = new GameObject("Rack_Collider");
            mainColliderObj.transform.SetParent(rackObj.transform);
            mainColliderObj.transform.position = centerPos + new Vector3(0f, height * 0.5f, 0f);
            var col = mainColliderObj.AddComponent<BoxCollider>();
            col.size = new Vector3(depth, height, length);
            GameObjectUtility.SetStaticEditorFlags(mainColliderObj, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
        }

        private static void BuildPackingStation(GameObject parent, Material benchMat, Material zoneMat)
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

            // Packing Computer Terminal / Display Placeholder
            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "StationTerminal";
            screen.transform.SetParent(stationRoot.transform);
            screen.transform.position = new Vector3(0f, 1.25f, -9.3f);
            screen.transform.localScale = new Vector3(0.6f, 0.45f, 0.05f);
            screen.GetComponent<MeshRenderer>().sharedMaterial = benchMat;

            // Tote Staging / Drop Zone Pad on Floor
            var dropPad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dropPad.name = "ToteDropZone";
            dropPad.transform.SetParent(stationRoot.transform);
            dropPad.transform.position = new Vector3(2.2f, 0.01f, -9.5f);
            dropPad.transform.localScale = new Vector3(1.0f, 0.02f, 1.0f);
            dropPad.GetComponent<MeshRenderer>().sharedMaterial = zoneMat;

            // Worker Spawn Point
            var spawnPoint = new GameObject("SpawnPoint").transform;
            spawnPoint.SetParent(stationRoot.transform);
            spawnPoint.position = new Vector3(0f, 0f, -8.0f);
            spawnPoint.rotation = Quaternion.identity; // Looking forward towards aisles (+Z)

            var comp = stationRoot.AddComponent<PackingStation>();
            comp.Initialize("PACK-01", "Dispatch Packing Station 1", bench.transform, dropPad.transform, spawnPoint);
        }

        private static void BuildPlayerController()
        {
            var playerObj = new GameObject("DesktopPicker");
            playerObj.transform.position = new Vector3(0f, 0f, -8.0f);
            playerObj.transform.rotation = Quaternion.identity;

            // Character Controller
            var cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.08f;
            cc.minMoveDistance = 0.001f;

            // Camera Holder at eye height (1.65m)
            var camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(playerObj.transform);
            camHolder.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camHolder.transform.localRotation = Quaternion.identity;

            // Main Camera
            var camObj = new GameObject("MainCamera");
            camObj.transform.SetParent(camHolder.transform);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;
            camObj.tag = "MainCamera";

            var cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 75f;

            // Add UniversalAdditionalCameraData if available
            var urpCamData = camObj.AddComponent<UniversalAdditionalCameraData>();
            if (urpCamData != null)
            {
                urpCamData.renderPostProcessing = true;
            }

            camObj.AddComponent<AudioListener>();

            // Picker Controller Component
            var controller = playerObj.AddComponent<DesktopPickerController>();
            var camField = typeof(DesktopPickerController).GetField("cameraTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (camField != null)
            {
                camField.SetValue(controller, camHolder.transform);
            }

            // Picker Raycast Interaction Component
            playerObj.AddComponent<PickerInteraction>();
        }

        private static void BuildShelfProducts(GameObject parent)
        {
            var productsRoot = new GameObject("ShelfProducts");
            productsRoot.transform.SetParent(parent.transform);

            // Create temporary StoreManager to get deterministic mapping
            var tempStoreObj = new GameObject("[TempStore]");
            var tempStore = tempStoreObj.AddComponent<StoreManager>();
            tempStore.InitializeStore();

            var catalog = tempStore.GetAllSkus();
            var catMaterials = new System.Collections.Generic.Dictionary<string, Material>();

            catMaterials["Beverages"] = GetOrCreateMaterial("Mat_Prod_Beverages", new Color(0.18f, 0.45f, 0.85f), 0.1f, 0.4f);
            catMaterials["Dairy"] = GetOrCreateMaterial("Mat_Prod_Dairy", new Color(0.92f, 0.95f, 0.98f), 0.1f, 0.5f);
            catMaterials["Bakery"] = GetOrCreateMaterial("Mat_Prod_Bakery", new Color(0.85f, 0.62f, 0.28f), 0.0f, 0.3f);
            catMaterials["Produce"] = GetOrCreateMaterial("Mat_Prod_Produce", new Color(0.22f, 0.68f, 0.25f), 0.0f, 0.2f);
            catMaterials["Snacks"] = GetOrCreateMaterial("Mat_Prod_Snacks", new Color(0.85f, 0.22f, 0.22f), 0.1f, 0.4f);
            catMaterials["Canned Goods"] = GetOrCreateMaterial("Mat_Prod_Canned", new Color(0.65f, 0.68f, 0.72f), 0.4f, 0.6f);
            catMaterials["Personal Care"] = GetOrCreateMaterial("Mat_Prod_PersonalCare", new Color(0.62f, 0.35f, 0.75f), 0.1f, 0.4f);
            catMaterials["Household"] = GetOrCreateMaterial("Mat_Prod_Household", new Color(0.90f, 0.82f, 0.20f), 0.1f, 0.3f);

            Material defaultMat = GetOrCreateMaterial("Mat_Prod_Default", new Color(0.5f, 0.5f, 0.5f), 0.1f, 0.3f);

            foreach (var sku in catalog)
            {
                if (tempStore.TryGetSlotForSku(sku.SkuId, out var slot))
                {
                    var prodObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    prodObj.name = $"Product_{sku.SkuId}";
                    prodObj.transform.SetParent(productsRoot.transform);

                    // Compute item position: resting on shelf tier, shifted towards aisle corridor
                    float xOffset = slot.Side == ShelfSide.Left ? 0.15f : -0.15f;
                    Vector3 itemPos = new Vector3(slot.WorldPosition.x + xOffset, slot.WorldPosition.y + 0.04f, slot.WorldPosition.z);
                    prodObj.transform.position = itemPos;
                    prodObj.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);

                    if (!catMaterials.TryGetValue(sku.Category, out Material mat))
                    {
                        mat = defaultMat;
                    }
                    prodObj.GetComponent<MeshRenderer>().sharedMaterial = mat;

                    var visualComp = prodObj.AddComponent<PickableItemVisual>();
                    visualComp.Initialize(sku.SkuId, sku.Name, slot.SlotId, mat);
                }
            }

            Object.DestroyImmediate(tempStoreObj);
            Debug.Log($"[DarkStoreSceneBuilder] Spawned {catalog.Count} physical pickable shelf product visuals.");
        }

        private static void BakeNavMeshGeometry()
        {
            try
            {
                var buildSettings = UnityEngine.AI.NavMesh.GetSettingsByID(0);
                var bounds = new Bounds(new Vector3(0f, 2f, 0f), new Vector3(30f, 10f, 32f));
                var sources = new System.Collections.Generic.List<UnityEngine.AI.NavMeshBuildSource>();
                var markups = new System.Collections.Generic.List<UnityEngine.AI.NavMeshBuildMarkup>();

                UnityEngine.AI.NavMeshBuilder.CollectSources(bounds, ~0, UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
                var data = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(buildSettings, sources, bounds, Vector3.zero, Quaternion.identity);

                if (data != null)
                {
                    string navAssetPath = "Assets/Scenes/DarkStore_Phase1_NavMesh.asset";
                    AssetDatabase.CreateAsset(data, navAssetPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[DarkStoreSceneBuilder] NavMesh built successfully with {sources.Count} sources and saved to {navAssetPath}.");
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
