# Virtual Dark Store Work-Study & Simulation Platform

## Purpose
The **Virtual Dark Store Work-Study & Simulation Platform** is a 3D simulation and industrial engineering measurement environment built in Unity 6 (`6000.6.7f1`) with the Universal Render Pipeline (URP). It simulates grocery micro-fulfillment ("dark store") operations to automate industrial time-and-motion studies, elemental cycle time analysis, interval work-sampling observations, and deterministic A/B scenario comparisons.

> **Scope & Honesty Guarantee**: This platform is an industrial engineering measurement and simulation tool—**not** an AI/ML prediction product. All outputs are **measured simulation telemetry** derived deterministically from recorded `WorkEvent` logs. No AI/ML models, synthetic dataset pipelines, uninstrumented physiological/eye-gaze claims, or fabricated FPS/benchmark numbers are included.

## Current Status
- **Current Phase**: Phase 8 (Polish, RTX 2050 Optimization, Report Exporters & Final Demo Workflow)
- **Status**: **COMPLETED** (2026-10-29)
- **Automated Verification**: **41 / 41 EditMode Tests Passing** (`0.771s`, 0 failures, 0 warnings)
- **Next Step**: All project phases (Phase 0 through Phase 8) are complete.

---

## Features Across All Phases (Phases 0–8)
- **Baseline & Clean URP Dark Store Scene (Phases 0–1)**:
  - `Assets/Scenes/DarkStore_Phase1.unity`: $24\text{ m} \times 28\text{ m}$ warehouse envelope, 4 picking aisles (`A01`–`A04`), 24 rack bays (3 vertical tiers per bay), aisle header signs, safety/guide floor stripes, dispatch packing bench with staging tote bins, and baked NavMesh (`Assets/Scenes/DarkStore_Phase1_NavMesh.asset`).
- **Pure Domain Topology & 4-Aisle Inventory Mapping (Phase 2 & Phase 8 Polish)**:
  - Pure C# domain models (`Sku`, `ShelfSlot`, `Shelf`, `Aisle`, `StoreConfiguration`) decoupled from `MonoBehaviour`.
  - 72 verified grocery SKUs across 8 departments (`VerifiedSampleCatalog.cs`) distributed evenly across all 4 aisles (18 SKUs per aisle, 1 per bay-tier at `SlotIndex == 1`) with 1:1 bidirectional lookup and zero location collisions (`StoreManager.cs`).
- **Order Engine, Pick Validation & Error Handling (Phase 3 & Phase 8 Polish)**:
  - `Order.cs`, `OrderItem.cs`, and `OrderManager.cs` enforcing strict SKU validation: wrong or missing SKUs are rejected (`PickResult.WrongItem`), logged via `SimulationErrorHandler.cs`, and never silently count as fulfilled.
  - Physical shelf product visuals (`PickableItemVisual.cs`) with proportional dimensions, 8 distinct department materials, and automatic shelf restocking (`ResetShelfVisuals()`) on new order dispatch.
- **Work-Study Telemetry & Activity Classifier (Phase 4)**:
  - `WorkEvent.cs` and `WorkEventLogger.cs` enforcing non-negative durations and monotonic timestamps ($t_{\text{start}, i} \ge t_{\text{start}, i-1}$).
  - `ActivityClassifier.cs` classifying all 8 industrial activities (`Walking`, `Waiting`, `Searching`, `Approaching`, `Picking`, `MovingBetweenLocations`, `Packing`, `OrderCompleted`) using strictly measurable physical parameters (velocity threshold $0.15\text{ m/s}$, approach radius $3.0\text{ m}$, shelf collider raycast intersection).
- **Work-Study Calculator, Results Modal & Report Exporter (Phase 5 & Phase 8 Polish)**:
  - `WorkStudyCalculator.cs` and `WorkStudyResults.cs` computing continuous time-study breakdowns, travel distance, pick counts, distinct shelf bay visits, configurable interval work-sampling ($0.5\text{ s}, 1.0\text{ s}, 2.0\text{ s}, 5.0\text{ s}$), and mathematical reconciliation ($\sum T_c = T_{\text{total}}$, $\sum P_c = 100\%$).
  - `WorkStudyResultsUI.cs` (`[Tab]`) with Summary Breakdown tab (visual percentage bars), Chronological Event Log tab, live sampling interval selector, and one-click export.
  - `WorkStudyReportExporter.cs` exporting full Markdown work-study reports, RFC-4180 elemental event CSVs, and summary KPI CSV bundles (`docs/evidence/phase8/`).
- **Scenario Engine, A/B Comparison & Route Visualization (Phase 6 & Phase 8 Polish)**:
  - `ScenarioDefinition.cs`, `ScenarioStorage.cs`, `ScenarioRunner.cs`, `ScenarioComparison.cs`, `RouteMapRenderer.cs`, `ScenarioRouteVisualizer.cs`, and `ScenarioComparisonUI.cs` (`[F3]`).
  - Compares identical orders under equivalent starting conditions (`SCN-A` vs `SCN-B` and `SCN-DEMO-A` vs `SCN-DEMO-B`) with top-down PNG route maps and 3D floor path rendering.
- **VR Player Rig & Desktop-VR Parity (Phase 7)**:
  - `VRPlayerRig.cs`, `VRHeadTracking.cs`, `VRControllerInput.cs`, `VRPickerInteraction.cs`, `VRLocomotionController.cs`, `VRHardwareDetector.cs`, and shared `IPickerInteraction.cs`.
  - 100% shared simulation and work-study core between Desktop and VR, including exact teleportation displacement preservation ($\Delta p$).
  - Physical VR headset verification marked **NOT RUN** where no physical HMD is attached (`docs/evidence/phase7/vr_hardware_report.md`).
- **RTX 2050-Class Performance Budget & 7-Step Final Demo Workflow (Phase 8)**:
  - `PerformanceSettingsManager.cs` and tuned `Assets/Settings/PC_RPAsset.asset`: SRP Batcher enabled, GPU instancing enabled across all $16/16$ shared materials, $148$ total mesh renderers, 1 shadow-casting directional light + 8 shadowless point lights, camera far clip $45.0\text{ m}$, URP shadow distance $32.0\text{ m}$, 2 shadow cascades, $2\times$ MSAA, and camera post-processing disabled. Interactive GPU FPS benchmarking is marked **NOT RUN** in headless mode (`docs/evidence/phase8/rtx2050_performance_audit.md`).
  - `DemoFlowController.cs` and `SimulationHUD.cs` (`[F1]`): Interactive 7-step Final Demo workflow with 3D `TargetSlotBeacon` above the active target shelf slot or packing bench.

---

## Reproducible Setup Instructions

### 1. System Prerequisites
- **OS**: Windows 10/11 (x64)
- **Unity Editor**: `6000.6.7f1` (Revision `b22767f04f04`) with Universal Render Pipeline (URP `17.6.0`)
  - Default path: `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`
- **.NET SDK**: `10.0+` (Verified with `10.0.402`)
- **Git**: `2.55+`

### 2. Clone & Open in Unity Editor
1. Clone this repository and open **Unity Hub**.
2. Click **Add** -> **Add project from disk** and select the `VirtualDarkStoreWSD` root directory.
3. Launch with Unity Editor **`6000.6.7f1`**.
4. In the Project window, open **`Assets/Scenes/DarkStore_Phase1.unity`**.

### 3. Reproducible Scene Rebuild (CLI or Editor Menu)
The entire dark store scene (`Assets/Scenes/DarkStore_Phase1.unity`), materials (`Assets/Materials/`), and NavMesh (`Assets/Scenes/DarkStore_Phase1_NavMesh.asset`) can be deterministically rebuilt from code at any time:
- **Inside Unity Editor**: Menu bar -> **`Virtual Dark Store`** -> **`Build Phase 1 Dark Store Scene (With NavMesh)`**
- **Via Headless PowerShell CLI**:
  ```powershell
  $p = Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" `
    -ArgumentList "-batchmode -nographics -projectPath . -executeMethod VirtualDarkStore.Editor.DarkStoreSceneBuilder.BuildDarkStorePhase1Scene -quit -logFile Logs/SceneBuild.log" `
    -PassThru -Wait
  Write-Output "ExitCode: $($p.ExitCode)"
  ```

### 4. Reproducible Build & Automated Test Execution (CLI)
1. **Compile C# Assemblies (`0 Warnings, 0 Errors`)**:
   ```powershell
   dotnet build VirtualDarkStore.Runtime.csproj --no-restore
   dotnet build VirtualDarkStore.Editor.csproj --no-restore
   dotnet build VirtualDarkStore.Tests.Editor.csproj --no-restore
   ```
2. **Run Full 41-Test EditMode Suite**:
   ```powershell
   $p = Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" `
     -ArgumentList "-batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults-EditMode.xml -logFile Logs/TestRun.log" `
     -PassThru -Wait
   Write-Output "ExitCode: $($p.ExitCode)"
   Select-String -Path TestResults-EditMode.xml -Pattern '<test-run'
   ```

---

## Final Demo Instructions (7-Step Workflow)

Open `Assets/Scenes/DarkStore_Phase1.unity` and press **Play** in the Unity Editor. The **Top-Right Demo Workflow Panel** (`[F1]` to toggle, `[Backquote / ~]` to unlock/lock mouse cursor for clicking UI buttons) guides you through all 7 steps:

### Step 1: Select Scenario
- In the top-right **Final Demo Workflow** panel (or via shortcuts), choose a scenario preset:
  - **`SCN-DEMO-A` (3-Item Aisle-Grouped)**: `SKU-0001` (`A01-L-B01-T01-S01`) -> `SKU-0008` (`A01-L-B03-T02-S01`) -> `SKU-0039` (`A03-L-B01-T03-S01`).
  - **`SCN-DEMO-B` (3-Item Ungrouped Backtracking)**: `SKU-0001` (`A01`) -> `SKU-0039` (`A03`) -> `SKU-0008` (`A01`).
  - **`SCN-A` / `SCN-B` (4-Item Multi-Aisle)**: 4-item order across Aisles 1, 2, and 3.

### Step 2: Select Order
- Choose the order preset matching your scenario (`ORD-DEMO-0001` 3-item demo order, `ORD-0001` 4-item multi-aisle order, or a random 3-item catalog order).

### Step 3: Start Desktop or VR (or Auto-Run)
- Click **`Start Desktop`**, **`Start VR Rig`**, or **`Auto-Run Demo`** (or press **`[N]`**).
- `OrderManager` restocks all shelf visuals, dispatches the order, and activates the cyan 3D **`TargetSlotBeacon`** above the first target shelf slot.

### Step 4: Pick Products
- **Desktop**: Navigate with `W, A, S, D` (`Left Shift` to sprint), aim the center reticle at the highlighted product box on the target shelf bay (within `2.8 m`), and press **`[E]`** or **Left Mouse Button**.
- **VR**: Navigate via thumbstick or teleportation and pull the **Right Controller Trigger/Grip** on the target item.
- Attempting to pick the wrong SKU triggers a non-crashing red alert banner and logs a warning without advancing the order.

### Step 5: Complete Order
- Once all line items are picked, the order status transitions to **`AwaitingPacking`** and the 3D beacon moves to the **Packing & Dispatch Station** (`(0, 0, -9.5)`).
- Walk to the packing station, aim at the bench, and press **`[E]`** or **Left Click** (or click **`Complete Order at Packing Bench`** in the demo panel).

### Step 6: View Work-Study Results & Export Report
- Upon order completion, the **Work-Study & Work-Sampling Results Modal** (`[Tab]`) opens automatically.
- Inspect reconciled KPI cards, continuous time-study elemental breakdown, interval work-sampling observations ($0.5\text{ s} - 5.0\text{ s}$), and the chronological `WorkEvent` log.
- Click **`Export Report (MD + CSV)`** (or press **`[F2]`**) to write the Markdown report, elemental events CSV, and summary CSV to `docs/evidence/phase8/`.

### Step 7: Compare Another Scenario
- Click **`Step 7: Compare Scenarios [F3]`** (or press **`[F3]`**) to open the **Scenario A/B Comparison Modal**.
- Click **`Run & Compare Demo (SCN-DEMO-A vs B)`** or **`Run & Compare 4-Item (SCN-A vs B)`** to execute both scenarios through the shared simulation core, view the side-by-side KPI delta table (`Delta = B - A`), inspect the top-down route overlay map, and toggle 3D floor paths (`Cyan` = A, `Orange` = B).

---

## Controls Summary

### Desktop Mode
| Action | Input Binding | Description |
|---|---|---|
| **Move / Sprint** | `W, A, S, D` / `Left Shift` | First-person navigation through store aisles |
| **Look** | `Mouse Delta` | Camera pitch and yaw when cursor is locked |
| **Toggle UI Cursor** | `` ` `` (`Backquote`) or `Escape` | Unlock mouse cursor to click HUD/modal buttons, or re-lock for look |
| **Pick Item / Pack Order** | `E` or `Left Click` | Pick hovered SKU from shelf or finalize order at packing bench |
| **Start New Order** | `N` | Dispatch selected demo order when idle or completed |
| **Toggle Demo Guide** | `F1` | Show/hide the 7-step Final Demo Workflow panel |
| **Export Work-Study Report** | `F2` | Export Markdown + CSV work-study bundle to `docs/evidence/phase8/` |
| **Toggle Study Results** | `Tab` | Open/close the Work-Study & Work-Sampling Results modal |
| **Toggle Scenario Compare** | `F3` | Open/close the Scenario A/B Comparison & Route Map modal |

### VR Mode (OpenXR / Meta Quest)
| Action | Input Binding | Description |
|---|---|---|
| **Continuous Move / Turn** | Left / Right Thumbstick | Aisle navigation and horizontal view rotation |
| **Teleport** | Left Thumbstick Forward + Release | Point-to-point translation with exact displacement ($\Delta p$) logging |
| **Pick / Pack** | Right Controller Trigger or Grip | Validate SKU pick at shelf or finalize order at packing bench |

---

## Documentation Index
- [AGENTS.md](file:///c:/Users/Mehul/OneDrive/Documents/VirtualDarkStoreWSD/AGENTS.md) — Non-negotiable project rules and change discipline
- [docs/PROJECT_BRIEF.md](file:///c:/Users/Mehul/OneDrive/Documents/VirtualDarkStoreWSD/docs/PROJECT_BRIEF.md) — Mission, scope, and operational boundaries
- [docs/ARCHITECTURE.md](file:///c:/Users/Mehul/OneDrive/Documents/VirtualDarkStoreWSD/docs/ARCHITECTURE.md) — Decoupled architecture, state rules, and Phase 8 subsystems
- [docs/SETUP.md](file:///c:/Users/Mehul/OneDrive/Documents/VirtualDarkStoreWSD/docs/SETUP.md) — Verified environment specs, CLI build/test commands, and RTX 2050 settings
- [docs/PHASE_STATUS.md](file:///c:/Users/Mehul/OneDrive/Documents/VirtualDarkStoreWSD/docs/PHASE_STATUS.md) — Phase 0 through Phase 8 completion tracker
- [docs/DEVELOPMENT_LOG.md](file:///c:/Users/Mehul/OneDrive/Documents/VirtualDarkStoreWSD/docs/DEVELOPMENT_LOG.md) — Chronological engineering log with verification evidence
- [docs/DECISIONS.md](file:///c:/Users/Mehul/OneDrive/Documents/VirtualDarkStoreWSD/docs/DECISIONS.md) — Architecture Decision Records (`ADR 001` – `ADR 011`)
- [docs/RESOURCE_REGISTER.md](file:///c:/Users/Mehul/OneDrive/Documents/VirtualDarkStoreWSD/docs/RESOURCE_REGISTER.md) — Verified packages, materials, scenarios, and assets
- [docs/TESTING.md](file:///c:/Users/Mehul/OneDrive/Documents/VirtualDarkStoreWSD/docs/TESTING.md) — 41-test verification records and Phase 8 evidence summary
- [CHANGELOG.md](file:///c:/Users/Mehul/OneDrive/Documents/VirtualDarkStoreWSD/CHANGELOG.md) — Release history (`0.0.1` through `0.8.0`)
