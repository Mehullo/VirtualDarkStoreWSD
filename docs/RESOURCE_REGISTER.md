# Resource & Asset Register

This document provides a factual inventory of all packages, external assets, 3D models, textures, audio, and third-party dependencies in the repository.

## 1. Verified Packages (`Packages/manifest.json`)
The following package dependencies have been inspected from `Packages/manifest.json` and `Packages/packages-lock.json`:

| Package Name | Specified Version | Locked Version | Source | Purpose | Status |
|---|---|---|---|---|---|
| `com.unity.render-pipelines.universal` | `17.6.0` | `17.6.0` | registry | Scriptable Render Pipeline (URP) | Verified |
| `com.unity.inputsystem` | `1.20.1` | `1.20.1` | registry | New Input System (keyboard, mouse, gamepad, XR) | Verified |
| `com.unity.ugui` | `2.6.0` | `2.6.0` | registry | Unity UI / Canvas framework | Verified |
| `com.unity.ide.visualstudio` | `2.0.28` | `2.0.28` | registry | Visual Studio Editor integration | Verified |
| `com.unity.ide.rider` | `3.1.0` | `3.1.0` | registry | JetBrains Rider Editor integration | Verified |
| `com.unity.collab-proxy` | `2.13.6` | `2.13.6` | registry | Unity Version Control | Verified |
| `com.unity.pipeline` | `0.9.1-exp.1` | `0.9.1-exp.1` | registry | Pipeline utilities (preview) | Verified |
| `com.unity.ai.assistant` | `2.21.0-pre.1` | `2.21.0-pre.1` | registry | Unity Editor Assistant (template default) | Verified |
| `com.unity.ai.inference` | `2.6.1` | `2.6.1` | registry | Sentis/Inference package (template default; inactive per AGENTS.md) | Verified |
| Built-in Unity Modules (36 modules) | `1.0.0` | `1.0.0` | builtin | Physics, Audio, UI, XR, Animation, etc. | Verified |

## 2. Verified In-Project Assets (`Assets/`)
The following assets are currently present in `Assets/` (URP Template defaults):

| Asset Path | Type | Source / Origin | Purpose | Status |
|---|---|---|---|---|
| `Assets/Scenes/DarkStore_Phase1.unity` | Unity Scene | `DarkStoreSceneBuilder.cs` | Primary 4-aisle dark store simulation scene | Verified |
| `Assets/Scenes/DarkStore_Phase1_NavMesh.asset` | NavMeshData | `NavMeshBuilder` | Baked warehouse navigation mesh (90 static colliders) | Verified |
| `Assets/Materials/*.mat` (16 materials) | URP Lit Materials | `DarkStoreSceneBuilder.cs` | GPU-instanced structural, signage, and 8 department SKU materials | Verified |
| `Scenarios/scenario_A.json`, `scenario_B.json`, `scenario_demo_A.json`, `scenario_demo_B.json` | JSON Scenarios | `ScenarioStorage.cs` | Saved A/B comparison scenarios | Verified |
| `Assets/Scenes/SampleScene.unity` | Unity Scene | Unity URP Template | Baseline template scene | Verified |
| `Assets/Settings/PC_RPAsset.asset` | UniversalRenderPipelineAsset | Unity URP Template | High-fidelity PC render pipeline configuration | Verified |
| `Assets/Settings/PC_Renderer.asset` | UniversalRendererData | Unity URP Template | Forward renderer for PC | Verified |
| `Assets/Settings/Mobile_RPAsset.asset` | UniversalRenderPipelineAsset | Unity URP Template | Mobile/Standalone render pipeline configuration | Verified |
| `Assets/Settings/Mobile_Renderer.asset` | UniversalRendererData | Unity URP Template | Forward renderer for Mobile | Verified |
| `Assets/Settings/DefaultVolumeProfile.asset` | VolumeProfile | Unity URP Template | Post-processing volume | Verified |
| `Assets/Settings/SampleSceneProfile.asset` | VolumeProfile | Unity URP Template | Scene-specific post-processing | Verified |
| `Assets/InputSystem_Actions.inputactions` | InputActionAsset | Unity Input System | Default input action asset | Verified |
| `Assets/Editor/HubForceResolve.cs` | C# Script | Unity Hub installer | One-time package resolver script | Verified |
| `Assets/TutorialInfo/` | Assets / Scripts | Unity URP Template | Readme and tutorial scripts (safe to archive/remove) | Verified |

## 3. External Models & Third-Party Assets Register
- **3D Warehouse Shelving / Racks**: None present in repo (Status: NOT PRESENT).
- **Cart / Tote Models**: None present in repo (Status: NOT PRESENT).
- **SKU Item Meshes / Textures**: None present in repo (Status: NOT PRESENT).
- **Barcode Scanner Asset**: None present in repo (Status: NOT PRESENT).

> [!NOTE]
> Any external 3D asset, sound, or script package introduced in subsequent phases MUST be recorded here with license details and verified provenance before commit.
