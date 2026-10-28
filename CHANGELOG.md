# Changelog

All notable changes to the Virtual Dark Store Work-Study & Simulation Platform will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to Semantic Versioning.

## [Unreleased]

## [0.4.0] - 2026-10-28
### Added
- **Work-Study Telemetry & Measurement Event Schema (Phase 4)**:
  - `ActivityType.cs`: Enum defining all 8 industrial work-study activities: `Walking`, `Waiting`, `Searching`, `Approaching`, `Picking`, `MovingBetweenLocations`, `Packing`, `OrderCompleted`.
  - `WorkEvent.cs`: Immutable measurement record capturing strictly measurable telemetry (`Id`, `Activity`, `StartTime`, `EndTime`, `Duration`, `Position`, `LocationName`, `RelatedSku`, `RelatedOrder`, `Distance`). Strictly validates non-negative duration and chronological invariants.
  - `WorkEventLogger.cs`: Central in-memory repository managing recorded work events with monotonic timestamp ordering enforcement and runtime integrity validation (`VerifyIntegrity`).
- **Activity State Machine & Classifier (Phase 4)**:
  - `ActivityClassifier.cs`: Continuous state machine and activity classifier using explicit physical measurement rules:
    - `Waiting`: stationary picker ($v < 0.15\text{ m/s}$) while unengaged.
    - `Walking`: moving picker ($v \ge 0.15\text{ m/s}$) with no active destination.
    - `MovingBetweenLocations`: moving picker ($v \ge 0.15\text{ m/s}$) towards target outside approach zone ($d > 3.0\text{ m}$).
    - `Approaching`: moving picker ($v \ge 0.15\text{ m/s}$) within target approach radius ($d \le 3.0\text{ m}$).
    - `Searching`: stationary picker ($v < 0.15\text{ m/s}$) within $3.0\text{ m}$ of target rack bay with camera raycast oriented towards shelving geometry, prior to item acquisition. Zero uninstrumented eye-gaze or cognitive load claims.
    - `Picking`: discrete physical retrieval event validated against SKU.
    - `Packing`: picker positioned at packing bench ($d \le 3.0\text{ m}$) with order awaiting packing.
    - `OrderCompleted`: terminal event logged upon order completion and dispatch.
- **HUD & Scene Hierarchy Integration (Phase 4)**:
  - `SimulationHUD.cs`: Displays real-time classified activity state and logged event counter in the picker terminal.
  - `DarkStoreSceneBuilder.cs`: Added `[WorkStudyManager]` containing `WorkEventLogger` and `ActivityClassifier` to scene hierarchy.
- **Automated Verification Suite (Phase 4)**:
  - `WorkStudyInstrumentationTests.cs`: 5 unit and integration tests verifying duration math, negative/inverted timestamp rejection, monotonic logger ordering, rule evaluation, and end-to-end sample order run with 13 inspected events.

### Verified
- **20/20 EditMode tests passed** (0 failures, 100% pass rate) via Unity Test Runner CLI (`TestResults-EditMode.xml`).
- Verified sample order run generated 13 work-study events across 6 distinct activity types (`Waiting`, `MovingBetweenLocations`, `Approaching`, `Searching`, `Picking`, `Packing`, `OrderCompleted`).
- Verified monotonic start timestamp ordering ($0.00\text{s} \le 0.34\text{s} \le 4.00\text{s} \le 5.50\text{s} \le 6.00\text{s} \le 8.50\text{s} \le 12.20\text{s} \le 12.70\text{s} \le 17.50\text{s} \le 18.70\text{s}$).
- Verified non-negative duration invariant for every logged event ($\Delta t \ge 0$).
- Verified zero claims of uninstrumented eye-gaze, fatigue, or physiological measurements.

## [0.3.0] - 2026-10-28
### Added
- **Order Engine & Domain Models (Phase 3)**:
  - `Order.cs`: Pure domain entity with line item list, order status progression (`Pending`, `InProgress`, `AwaitingPacking`, `Completed`), and strict validation logic.
  - `OrderItem.cs`: Line item tracking required and fulfilled quantity, target shelf slot ID, and target world position.
  - `OrderManager.cs`: Singleton manager handling automated/manual order dispatch (`StartSampleOrder`, `StartSpecificOrder`), pick attempt validation (`AttemptPick` returning `PickResult`), packing completion (`CompletePacking`), and lifecycle event broadcasting.
- **Physical Shelf Products & Picker Raycast Interaction (Phase 3)**:
  - `PickableItemVisual.cs`: Component attached to physical shelf products with BoxCollider, category material shading, and visual disable on pick.
  - `PickerInteraction.cs`: First-person reticle raycast interaction (2.8m reach, sorted `RaycastAll`) detecting shelf items and packing station with `[E]` / LMB pick triggers and `[N]` new order trigger.
  - 72 physical pickable product boxes spawned across shelves in `Assets/Scenes/DarkStore_Phase1.unity` with 8 distinct category materials (`Mat_Prod_Beverages`, `Mat_Prod_Dairy`, `Mat_Prod_Bakery`, `Mat_Prod_Produce`, `Mat_Prod_Snacks`, `Mat_Prod_Canned`, `Mat_Prod_PersonalCare`, `Mat_Prod_Household`).
- **Screen-Space Simulation HUD (Phase 3)**:
  - `SimulationHUD.cs`: IMGUI HUD with central reticle, dynamic interaction prompt, active order terminal, target item name, slot ID, live distance to bin in meters, controls guide bar, and color-coded toast banners (green pick, red mismatch alert, gold completion).
- **Automated Verification Suite (Phase 3)**:
  - `OrderFulfillmentTests.cs`: 7 unit and integration tests verifying item picking, wrong SKU rejection, null/empty SKU rejection, sequential advancement, premature packing rejection, awaiting-packing transition, and end-to-end full sample order fulfillment cycle.

### Verified
- **15/15 EditMode tests passed** (0 failures, 100% pass rate) via Unity Test Runner CLI (`TestResults-EditMode.xml`).
- Verified that wrong or missing SKU pick attempts strictly fail and never silently count as correct.
- Verified that premature order packing attempts before fulfilling all pick list items are strictly rejected.
- Verified end-to-end sample order cycle: Start order -> Navigate -> Locate SKU -> Pick -> Next item -> Packing -> Complete.
- Rebaked NavMesh with 144 collider sources in `Assets/Scenes/DarkStore_Phase1_NavMesh.asset`.

## [0.2.0] - 2026-10-28
### Added
- **Dark Store Scene Blockout (Phase 1)**:
  - Clean URP scene `Assets/Scenes/DarkStore_Phase1.unity` with 4 picking aisles, 24m x 28m concrete warehouse envelope, boundary colliders, ambient lighting, and packing bench table.
  - Desktop picker player controller (`DesktopPickerController.cs`) with WASD movement, mouse pitch/yaw look, sprint, and gravity.
  - AI Navigation NavMesh (`DarkStore_Phase1_NavMesh.asset`) baked from 72 collider sources via `UnityEngine.AI.NavMeshBuilder`.
- **Pure Domain Topology & Inventory Core (Phase 2)**:
  - Domain models decoupled from GameObjects: `Sku.cs`, `ShelfSlot.cs`, `Shelf.cs`, `Aisle.cs`, `StoreConfiguration.cs`.
  - Deterministic catalog of 72 verified grocery SKUs (`VerifiedSampleCatalog.cs`) across 8 departments.
  - Central manager (`StoreManager.cs`) managing 288 slots with 72 SKUs mapped 1:1, 0 collisions, and bidirectional dictionary lookups.
  - Assembly definitions: `VirtualDarkStore.Runtime`, `VirtualDarkStore.Editor`, `VirtualDarkStore.Tests.Editor`.
  - Added `com.unity.test-framework: 1.4.6` to `Packages/manifest.json`.
  - Automated EditMode test suite with 8 passing tests (`StoreTopologyTests.cs`).

### Verified
- 8/8 EditMode automated tests passed (0 failures) via Unity Test Runner CLI.
- Verified exact SKU count: 72.
- Verified total slot count: 288.
- Verified occupied slot count: 72 (100% unique location mapping).
- Primary scene configured in `EditorBuildSettings.asset`.

## [0.0.1] - 2026-10-28
### Added
- Created formal engineering documentation suite:
  - `docs/PROJECT_BRIEF.md`: Core objective, industrial work-study scope, and non-negotiable boundaries.
  - `docs/ARCHITECTURE.md`: Decoupled architecture, elemental state machine, and telemetry schemas.
  - `docs/SETUP.md`: Verified environment specifications, Unity 6000.6.7f1 paths, and launch commands.
  - `docs/PHASE_STATUS.md`: Phase progression tracking from Phase 0 through Phase 7.
  - `docs/DEVELOPMENT_LOG.md`: Dated engineering change log with mandatory audit fields.
  - `docs/DECISIONS.md`: Architecture decision records (ADR 001 - ADR 005).
  - `docs/RESOURCE_REGISTER.md`: Inventory of verified packages and assets.
  - `docs/TESTING.md`: Test strategy, headless CLI command, and factual verification status.
  - `CHANGELOG.md`: Project change tracking.
- Updated `README.md` with verified environment data, package inventory, and Phase 0 status.

### Verified
- Unity Editor version: `6000.6.7f1` (revision `b22767f04f04`).
- Executable path: `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`.
- Package dependencies: Universal Render Pipeline `17.6.0`, Input System `1.20.1`, uGUI `2.6.0`.
- Repository status: Baseline project verified without modifying application code.
