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
| **Installed .NET SDK** | `10.0.402` | `dotnet --version` |
| **Version Control** | Git `2.55.0.windows.5` | `git --version` |

## 2. Project Location
- Root Directory: `C:\Users\Mehul\OneDrive\Documents\VirtualDarkStoreWSD`
- Primary Scene: `Assets/Scenes/SampleScene.unity` (`EditorBuildSettings.asset`)

## 3. Launching & Opening the Project

### Option A: Unity Hub
1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select `C:\Users\Mehul\OneDrive\Documents\VirtualDarkStoreWSD`.
4. Ensure Editor Version is set to **6000.6.7f1**.
5. Launch the project.

### Option B: Command Line (Headless / Batch Mode Verification)
To execute tests or verify compilation via the command line:
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -projectPath "C:\Users\Mehul\OneDrive\Documents\VirtualDarkStoreWSD" -batchmode -nographics -quit -logFile "Library\UnityBuild.log"
```

## 4. IDE Integration
- **VS Code**: Workspace contains `.vscode/settings.json`, `.vscode/launch.json`, and `.vscode/extensions.json`. Recommended extensions: C# Dev Kit and Unity extension.
- **Rider / Visual Studio**: Packages `com.unity.ide.rider` (3.1.0) and `com.unity.ide.visualstudio` (2.0.28) are present in `Packages/manifest.json`.

## 5. Dependency Verification Rules
- Do NOT install Python dependencies unless formally approved.
- Do NOT introduce untracked external DLLs or assets without updating `docs/RESOURCE_REGISTER.md`.
