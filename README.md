# Virtual Dark Store Work-Study & Simulation Platform

## Purpose
The **Virtual Dark Store Work-Study & Simulation Platform** is a 3D simulation and industrial engineering measurement environment built in Unity. It simulates grocery micro-fulfillment ("dark store") operations to automate industrial time-and-motion studies, elemental cycle time analysis, and work-sampling observations.

## Current Status
- **Current Phase**: Phase 5 (Work-Study Calculator, Results Model & Results UI)
- **Status**: Completed (2026-10-28)
- **Next Step**: Phase 6 (VR Rig Integration & Desktop-VR Parity)

## Features
### Completed (Phase 0, Phase 1, Phase 2, Phase 3, Phase 4 & Phase 5)
- **Baseline Audit (Phase 0)**: Unity 6 (`6000.6.7f1`) and URP environment verified; comprehensive documentation established.
- **Clean URP 3D Scene (Phase 1)**: `Assets/Scenes/DarkStore_Phase1.unity` constructed with 4 picking aisles, 24m x 28m concrete warehouse envelope, boundary colliders, ambient high-bay lighting, and dispatch packing bench.
- **Desktop Locomotion (Phase 1)**: `DesktopPickerController.cs` with CharacterController, WASD movement, mouse pitch/yaw look, sprint, and gravity.
- **AI Navigation (Phase 1)**: `Assets/Scenes/DarkStore_Phase1_NavMesh.asset` baked using `UnityEngine.AI.NavMeshBuilder` from 144 collider sources.
- **Pure Domain Topology (Phase 2)**: `Sku.cs`, `ShelfSlot.cs`, `Shelf.cs`, `Aisle.cs`, and `StoreConfiguration.cs` completely decoupled from GameObjects.
- **Verified Product Catalog (Phase 2)**: 72 distinct, verified dark-store SKUs across 8 departments (`VerifiedSampleCatalog.cs`).
- **Store & Inventory Manager (Phase 2)**: `StoreManager.cs` managing 288 shelf slots (4 aisles * 2 sides * 3 bays * 3 tiers * 4 slots) with 72 SKUs mapped 1:1 with zero collisions and verified bidirectional lookups.
- **Order Engine & Pick Validation (Phase 3)**: Pure domain entities `Order.cs` and `OrderItem.cs` strictly validating picks against the active SKU; wrong or missing items never silently count as correct picks.
- **Order Dispatch & Lifecycle Manager (Phase 3)**: `OrderManager.cs` handling automatic sample order dispatch, pick action validation, packing completion, and lifecycle event notifications.
- **Visual Shelf Products & Raycast Interaction (Phase 3)**: 72 physical pickable item boxes spawned across shelves with category materials (`PickableItemVisual.cs`), raycast selection via `PickerInteraction.cs` with `[E]` and LMB triggers.
- **Simulation HUD (Phase 3, 4 & 5)**: `SimulationHUD.cs` screen-space terminal with central reticle, dynamic interaction prompt, target SKU & slot location, live distance to bin in meters, active classified activity, logged work event counter, and controls guide with `[Tab]` results toggle.
- **Work-Study Telemetry & Event Schema (Phase 4)**: Pure domain class `WorkEvent.cs` capturing strictly measurable values (ID, activity, start/end time, duration, position, location, related SKU/order, distance traveled). Strictly excludes uninstrumented physiological/eye-gaze claims.
- **Monotonic Event Logger (Phase 4)**: `WorkEventLogger.cs` in-memory repository enforcing ordered timestamps and non-negative event durations with automated integrity validation.
- **Activity State Machine & Classifier (Phase 4)**: `ActivityClassifier.cs` classifying operator movements across all 8 activities (`Walking`, `Waiting`, `Searching`, `Approaching`, `Picking`, `MovingBetweenLocations`, `Packing`, `OrderCompleted`) using explicit physical measurement rules.
- **Work-Study Results Model (Phase 5)**: `WorkStudyResults.cs` data model holding aggregated industrial engineering metrics: total task time, walking/searching/picking/waiting/packing durations, percentages, distance, picks, shelf visits, and mathematical reconciliation validation (`VerifyReconciliation`).
- **Work-Study Calculator & Sampling Engine (Phase 5)**: `WorkStudyCalculator.cs` pure computational engine aggregating metrics from event logs, supporting configurable interval-based work-sampling without fabricated metrics or unvalidated standards compliance claims.
- **Work-Study Results UI (Phase 5)**: `WorkStudyResultsUI.cs` IMGUI overlay modal toggleable via `[Tab]` or shown upon order completion displaying metric cards, time-study table, work-sampling proportions, and reconciliation status badges.
- **Automated Test Suite**: 24 EditMode tests passing (100% pass rate) covering store topology, SKU integrity, pick validation, work event invariants, activity classification, duration reconciliation ($\sum T_c = T_{total}$), percentage balance ($\sum P_c = 100\%$), and deterministic sample order fulfillment.

### Deferred (Phase 6)
- VR rig integration (OpenXR / XR Interaction Toolkit) - deferred until Desktop simulation and measurement parity are fully verified.

## Requirements
- **Operating System**: Windows (x64)
- **Unity Editor**: `6000.6.7f1` (Revision: `b22767f04f04`) with Universal Render Pipeline (URP)
- **Installed Editor Path**: `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`
- **.NET SDK**: 10.0+ (Tested with 10.0.402)
- **Git**: 2.55+

## Installation
1. Clone repository to your local drive.
2. Open **Unity Hub** and select **Add** -> **Add project from disk**.
3. Choose the repository root (`VirtualDarkStoreWSD`).
4. Select Unity Editor version **6000.6.7f1**.

## Packages
Inspected and verified from `Packages/manifest.json`:

| Package | Version | Purpose | Verified |
|---|---|---|---|
| `com.unity.render-pipelines.universal` | `17.6.0` | Scriptable Render Pipeline (URP) | Yes |
| `com.unity.inputsystem` | `1.20.1` | Input System (Keyboard, Mouse, XR) | Yes |
| `com.unity.ugui` | `2.6.0` | Unity UI / Canvas system | Yes |
| `com.unity.test-framework` | `1.4.6` | Unity Test Framework (EditMode & PlayMode) | Yes |
| `com.unity.ide.visualstudio` | `2.0.28` | Visual Studio Editor integration | Yes |
| `com.unity.ide.rider` | `3.1.0` | JetBrains Rider Editor integration | Yes |
| `com.unity.collab-proxy` | `2.13.6` | Unity Version Control | Yes |
| `com.unity.pipeline` | `0.9.1-exp.1` | Pipeline tools | Yes |
| `com.unity.ai.assistant` | `2.21.0-pre.1` | Editor Assistant (Template default) | Yes |
| `com.unity.ai.inference` | `2.6.1` | Sentis Inference (Template default; inactive per AGENTS.md) | Yes |

## Run the Project
1. Open the project in Unity Editor `6000.6.7f1`.
2. Open `Assets/Scenes/DarkStore_Phase1.unity`.
3. Press **Play** in the Unity Editor. Use `W`,`A`,`S`,`D` to navigate aisles and `Mouse` to look around. Press `Left Shift` to sprint.

## Controls (Desktop First-Person Mode)
| Action | Input Binding | Description |
|---|---|---|
| **Move** | `W`, `A`, `S`, `D` | Omnidirectional navigation through store aisles |
| **Look** | `Mouse Delta` | First-person look / pitch & yaw |
| **Sprint** | `Left Shift` | Fast picker locomotion |
| **Cursor Lock/Unlock** | `Escape` / `Left Click` | Unlock or lock mouse cursor to game window |
| **Interact / Pick / Pack** | `E` or `Left Click` | Pick hovered SKU from shelf or complete order at packing bench |
| **New Sample Order** | `N` | Dispatch a new sample 3-item order when idle or completed |
| **Scan Item (Planned Phase 4)** | `F` or `Right Click` | Trigger virtual barcode scanner verification |
| **Place in Cart (Planned Phase 4)** | `Q` or `Left Click` on Tote | Transfer picked item into cart tote |

## Architecture
The platform enforces clean decoupling between the presentation/input layer and the simulation/work-study core:
- **Presentation Layer**: Desktop Controller (`DesktopPickerController.cs`), `PickerInteraction.cs`, `PickableItemVisual.cs`, and `SimulationHUD.cs`.
- **Domain Topology & Inventory**: Pure C# data models (`Sku`, `ShelfSlot`, `Shelf`, `Aisle`, `StoreConfiguration`) completely decoupled from GameObjects.
- **Fulfillment Engine**: Pure domain entities `Order.cs` and `OrderItem.cs` managed by `OrderManager.cs` and `StoreManager.cs`.
- **WorkStudy Core (Planned Phase 4)**: Tracks elemental states (`TRAVEL`, `SEARCH`, `PICK`, `SCAN`, `PLACE`, `STAGE`, `IDLE`), logs cycle metrics, samples activity states, and exports deterministic CSV/JSON records.
- See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for complete details.

## Work-Study Method
- **Time-Study**: Measures discrete elemental durations, cumulative travel distance, and cycle times for each order item pick cycle.
- **Work-Sampling**: Periodically records the active state of the operator to calculate percentage utilization across tasks ($p = \frac{n_i}{N}$).
- **Compliance**: Follows traditional industrial engineering time-and-motion study practices without AI/ML estimation or uninstrumented biometrics.

## Testing
- Automated unit testing via Unity Test Framework (`com.unity.test-framework` `1.4.6`).
- **Current Status**: **24 TESTS EXECUTED AND PASSED** (0 failures). Evidence recorded in `TestResults-EditMode.xml`.
- See [docs/TESTING.md](docs/TESTING.md) for test execution instructions and full test breakdown.

## Known Limitations
- VR hardware instrumentation and desktop-VR parity are scheduled for Phase 6.
- All metrics represent deterministic virtual simulation telemetry; no claims of ANSI/ISO laboratory certification without empirical external validation.

## External Resources & Licenses
All external packages and third-party assets are tracked in [docs/RESOURCE_REGISTER.md](docs/RESOURCE_REGISTER.md). Currently, only standard Unity packages are included.

## Documentation Index
- [AGENTS.md](AGENTS.md) - Project constraints, non-negotiables, and operational guidelines
- [docs/PROJECT_BRIEF.md](docs/PROJECT_BRIEF.md) - Project mission, scope, and objectives
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - Technical architecture and data schemas
- [docs/SETUP.md](docs/SETUP.md) - System setup, prerequisites, and launch guide
- [docs/PHASE_STATUS.md](docs/PHASE_STATUS.md) - Lifecycle progression tracker
- [docs/DEVELOPMENT_LOG.md](docs/DEVELOPMENT_LOG.md) - Detailed chronological audit log
- [docs/DECISIONS.md](docs/DECISIONS.md) - Architecture Decision Records (ADRs)
- [docs/RESOURCE_REGISTER.md](docs/RESOURCE_REGISTER.md) - Package and asset inventory
- [docs/TESTING.md](docs/TESTING.md) - Test strategy and verification records
- [CHANGELOG.md](CHANGELOG.md) - Version and milestone history

## Next Step
Proceed to **Phase 6: VR Rig Integration & Desktop-VR Parity**:
- Integrate OpenXR / XR Interaction Toolkit into an equivalent VR rig.
- Maintain 100% parity with desktop simulation and WorkStudy core.
- Verify identical time-study and work-sampling data structures across both desktop and VR modalities.
