# Environment Setup & Prerequisites

## 1. Verified Environment Specifications
All values below were directly inspected and verified from the local system:

| Parameter | Inspected Value | Evidence / Source |
|---|---|---|
| **Operating System** | Windows (x64) | Local environment |
| **Unity Editor Version** | `6000.6.7f1` (Revision: `b22767f04f04`) | `ProjectSettings/ProjectVersion.txt` |
| **Unity Executable Path** | `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe` | Verified via filesystem test |
| **Render Pipeline** | Universal Render Pipeline (`com.unity.render-pipelines.universal` `17.6.0`) | `Packages/manifest.json` |
| **Input System** | Unity Input System (`com.unity.inputsystem` `1.20.1`) | `Packages/manifest.json` |
| **UI System** | Unity uGUI (`com.unity.ugui` `2.6.0`) | `Packages/manifest.json` |
| **XR Management** | Unity XR Plugin Management (`com.unity.xr.management` `4.7.0`) | `Packages/manifest.json` |
| **OpenXR Plugin** | Unity OpenXR Plugin (`com.unity.xr.openxr` `1.18.0`) | `Packages/manifest.json` |
| **XR Interaction Toolkit** | Unity XR Interaction Toolkit (`com.unity.xr.interaction.toolkit` `3.6.1`) | `Packages/manifest.json` |
| **XR Core Utilities** | Unity XR Core Utilities (`com.unity.xr.core-utils` `2.6.0`) | `Packages/manifest.json` |
| **Meta Quest OpenXR** | Unity OpenXR Meta Feature (`com.unity.xr.meta-openxr` `2.6.1`) | `Packages/manifest.json` |
| **Android Build Support** | Installed (`AndroidPlayer`) | `Editor/Data/PlaybackEngines/AndroidPlayer` |
| **Android SDK & Tools** | Installed (with `platform-tools/adb.exe`) | `AndroidPlayer/SDK` |
| **Android NDK** | Installed | `AndroidPlayer/NDK` |
| **OpenJDK** | Installed | `AndroidPlayer/OpenJDK` |
| **Installed .NET SDK** | `10.0.402` | `dotnet --version` |
| **Version Control** | Git `2.55.0.windows.5` | `git --version` |

## 2. Project Location
- Root Directory: `C:\Users\Mehul\OneDrive\Documents\VirtualDarkStoreWSD`
- Primary Simulation Scene: `Assets/Scenes/DarkStore_Phase1.unity` (`EditorBuildSettings.asset`)

## 3. Launching, Rebuilding & Testing the Project

### Option A: Unity Hub (Interactive Mode)
1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select `C:\Users\Mehul\OneDrive\Documents\VirtualDarkStoreWSD`.
4. Ensure Editor Version is set to **6000.6.7f1**.
5. Launch the project, open `Assets/Scenes/DarkStore_Phase1.unity`, and press **Play**.

### Option B: Command Line (Reproducible Scene Rebuild & 41-Test Suite)
1. **Verify C# Compilation (`0 Warnings, 0 Errors`)**:
   ```powershell
   dotnet build VirtualDarkStore.Runtime.csproj --no-restore
   dotnet build VirtualDarkStore.Editor.csproj --no-restore
   dotnet build VirtualDarkStore.Tests.Editor.csproj --no-restore
   ```
2. **Deterministically Rebuild Dark Store Scene & NavMesh**:
   ```powershell
   $p = Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" `
     -ArgumentList "-batchmode -nographics -projectPath . -executeMethod VirtualDarkStore.Editor.DarkStoreSceneBuilder.BuildDarkStorePhase1Scene -quit -logFile Logs/SceneBuild.log" `
     -PassThru -Wait
   Write-Output "ExitCode: $($p.ExitCode)"
   ```
3. **Execute Full 41-Test EditMode Suite**:
   ```powershell
   $p = Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" `
     -ArgumentList "-batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults-EditMode.xml -logFile Logs/TestRun.log" `
     -PassThru -Wait
   Write-Output "ExitCode: $($p.ExitCode)"
   ```

## 4. IDE Integration
- **VS Code**: Workspace contains `.vscode/settings.json`, `.vscode/launch.json`, and `.vscode/extensions.json`. Recommended extensions: C# Dev Kit and Unity extension.
- **Rider / Visual Studio**: Packages `com.unity.ide.rider` (3.1.0) and `com.unity.ide.visualstudio` (2.0.28) are present in `Packages/manifest.json`.

## 5. VR / XR Rig & Headset Verification
- **OpenXR Runtime**: Managed via `com.unity.xr.openxr` and `com.unity.xr.management` (`Assets/XR/XRGeneralSettingsPerBuildTarget.asset`).
- **Target Profiles**: Standalone (Windows x64) and Android (Meta Quest).
- **Physical Headset Verification Status**:
  - Headset detection is executed automatically via `VRHardwareDetector.CheckHardware()` and automated tests.
  - If no physical headset is connected via USB/Link/OpenXR runtime, hardware verification is marked strictly as **NOT RUN** without fabricating devices or results.
  - Verification Report location: `docs/evidence/phase7/vr_hardware_report.md`.

## 6. Dependency Verification Rules
- Do NOT install Python dependencies unless formally approved.
- Do NOT introduce untracked external DLLs or assets without updating `docs/RESOURCE_REGISTER.md`.
