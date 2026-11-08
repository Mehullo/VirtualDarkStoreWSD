# Changelog

All notable changes to the Virtual Dark Store Work-Study & Simulation Platform will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to Semantic Versioning.

## [Unreleased]
### Added
- **Prominent Top-Centre Next-Order Information Panel**:
  - `SimulationHUD.cs`: Dedicated high-visibility Next-Order Information Panel centered horizontally at the top of the screen (`panelY = 12f`, width 42% clamped between 580px and 660px, adaptive height up to 240px with `GUI.BeginScrollView`).
  - Dark semi-transparent card styling (`#0B1220F0`) with cyan top accent strip (`#00E5FF`) and high-contrast amber/cyan headers.
  - Significantly larger typography (15pt bold header, 12pt subheaders and table text) for immediate picker readability without eye strain.
  - Dynamically displays Order ID (`NEXT ORDER | ORD-...`), total item count, and full product table with SKU/product names, quantities, highlighted Aisle numbers (`A01`–`A06` in amber yellow), Rack and Slot positions (`R-B03-T03-S02` in ice cyan), and pick status badges (`PICK`, `DONE`, `WAIT`).
  - Active target pick item highlighted in bold golden yellow (`#FFD54F`) with arrow indicator `▶` for instant picker orientation.
  - Automatically queries live `StoreManager.Instance.TryGetSlotForSku` on every frame to immediately reflect physical re-slotting or routing optimizations without stale cached strings.
  - Automatically advances to the next upcoming queued order upon fulfillment and packing completion, and displays a graceful `NEXT ORDER | NO UPCOMING ORDERS` summary when all orders are finished.
  - Dynamically floats transient notification banners below the panel (`12f + currentNextOrderPanelHeight + 6f`) to prevent UI overlap.
  - Cleanly destroys allocated textures in `OnDestroy()`.
  - `NextOrderPanelTests.cs`: 7 automated EditMode tests validating active order formatting, queued simulation progression, completion summaries, live slot resolution, fallback location parsing, and null safety.
  - All 92 EditMode tests passing (`7.72s`, 0 failures, 0 warnings).

### Fixed
- **Internal Aisle Signage Text Orientation**:
  - Corrected inverted Euler rotation in `DarkStoreSceneBuilder.cs` for internal aisle signage (`faceRot` and `ticketRot`):
    - Left racks: set to `Quaternion.Euler(0f, -90f, 0f)` (-90° yaw) so local X aligns with picker camera right (+Z) when looking West (-X).
    - Right racks: set to `Quaternion.Euler(0f, 90f, 0f)` (+90° yaw) so local X aligns with picker camera right (-Z) when looking East (+X).
  - Regenerated and baked primary scene `Assets/Scenes/DarkStore_Phase1.unity`, updating all internal bay placards, shelf level tags, and shelf tickets while strictly preserving front-facing header signs and end-cap placards (`Quaternion.identity`).
  - Added unit test `InternalAisleSignage_OrientationMatchesPickerViewingDirection_WithoutHorizontalMirroring` in `PhysicalLocationLabelTests.cs`.
  - All 85 EditMode tests passing (`8.19s`, 0 failures, 0 warnings).

### Added
- **Configurable Multi-Order Simulation**:
  - `MultiOrderSimulation.cs`: Domain model coordinating ordered batches of independent orders, tracking active order progression, completed orders, and overall status.
  - `OrderGenerationConfig.cs`: Added `Validate()` enforcing $N \ge 1$ orders, $K \ge 1$ items/order, and capacity boundaries without silent clamping.
  - `OrderManager.cs`: Added `TryStartMultiOrderSimulation`, `StopMultiOrderSimulation`, and sequential multi-order progression upon packing completion at the packing bench.
  - `SimulationHUD.cs`: Added "SIMULATION SETUP" interface with Number of Orders, Items per Order, Seed controls, and `[Generate Simulation]` button; updated order terminal to show simulation progress.
  - `OperationalCycleRunner.cs`: Added `RunMultiOrderSimulation` executing multi-order batches headlessly with simulated picker motion, compiling individual order telemetry and aggregate work-study metrics.
  - `MultiOrderSimulationTests.cs`: 7 automated EditMode tests validating Cases 1 through 6 (single order, 5 orders of 8 items = 40 items, reproducibility, seed variation, input validation, sequential execution, and aggregate work-study).
- **Shelf Population (144 SKUs Across Usable Racks)**:
  - `VerifiedSampleCatalog.CreateQuickCommerceCatalog()`: Expanded deterministic catalog of 144 verified grocery SKUs across all 8 micro-fulfillment categories with realistic barcodes (`890100000001`–`890100000144`), weights, and dimensions.
  - `StoreManager.cs`: Dynamic catalog resolution loading 144 SKUs for $\ge 6$-aisle quick-commerce stores and preserving 72 SKUs for 4-aisle baseline stores.
  - `DarkStoreSceneBuilder.cs`: Populates 144 physical pickable items (`PickableItemVisual`) across all 6 aisles (`A01`–`A06`), Left/Right racks, all 3 bays, and multiple tiers with matching shelf tickets.
  - NavMesh baked with 174 collider sources.
  - Hardware budget compliance: 284 scene mesh renderers ($\le 300$ budget ceiling) for RTX 2050 GPU performance.
  - `StructuredStoreLocationTests.cs` & `StoreTopologyTests.cs`: 2 new unit/integration tests validating 144 SKUs, 1:1 bidirectional mapping, and broad cross-aisle/tier shelf coverage.
- **Physical Location Labels & Store Signage**:
  - `PhysicalLocationLabel.cs`: Component rendering lightweight, high-contrast World Space Canvas signage synchronized with the `StoreLocation` domain hierarchy (`Store -> Aisle -> Side -> Bay -> Shelf Level -> Slot`).
  - Overhead Aisle Header Signboards (`AISLE A01` ... `AISLE A06`) on front and rear entrance beams.
  - End-Cap Rack Placards (`A01-L` / `A01-R`) mounted on front rack uprights facing the cross-aisle.
  - Bay Grouping Placards (`A01-L-B01` | `BAY 01 • LEFT`) mounted atop each bay at eye level ($Y = 2.15\text{ m}$) facing the corridor.
  - Shelf Level Badges (`LEVEL 01`, `LEVEL 02`, `LEVEL 03`) aligned with shelf tier beam lips.
  - Product Slot Tickets (`A01-L-B01-T01-S01` / `SKU-0001`) mounted flush on shelf beams directly below each stocked item.
  - `PickerInteraction.cs` & `VRPickerInteraction.cs`: Updated desktop and VR prompts to display the exact slot location ID on item hover (`[E / LMB] Pick: <SkuName> (<SkuId>) [<SlotId>]`).
  - `PhysicalLocationLabelTests.cs`: 5 automated EditMode tests verifying World Space canvas setup, non-blocking graphics, zero colliders, domain hierarchy alignment, SKU 1:1 mapping, and picker prompt display.

### Verified
- **84/84 EditMode tests passed** (`9.56s`, 0 failures, 0 warnings) recorded in `TestResults-MultiOrderCheck2.xml`.
- Scene `DarkStore_Phase1.unity` rebuilt with 174 NavMesh collider sources and 144 active physical products.


## [0.14.0] - 2026-11-02
### Added
- **Phase 13 End-to-End Operational Workflow**:
  - `OperationalCycleManager.cs`: 7-stage operational state machine (`Entry` $\to$ `CollectOrder` $\to$ `Picking` $\to$ `AwaitingPacking` $\to$ `DepositOrder` $\to$ `WalkToExit` $\to$ `Completed`), fixed store waypoints, proximity checks, and aisle metrics (`CalculateAisleMetrics` for transitions and backtracks).
  - `PickerInteraction.cs`: Raycast prompts (`[E / LMB]`) to collect and activate orders at the collection desk and deposit orders at the packing bench.
  - `OperationalCycleRunner.cs`: Headless, scripted virtual picker executing the full operational cycle for automated testing.
  - Scene Integration: Attached `OperationalCycleManager` to `[WorkStudyManager]` in `DarkStore_Phase1.unity`.
- **Phase 14 Baseline vs Optimized Work-Study Comparison**:
  - `OperationalComparison.cs`: Comparative engine comparing Run A (Baseline) vs Run B (Optimized) under identical layout, order, start condition, and locomotion parameters.
  - Evaluates cycle time, walking transit time, searching time, picking time, waiting time, packing time, measured travel distance, picks, shelf visits, aisle transitions, and aisle backtracking.
  - Evidence Bundle Exporter: Writes Markdown comparison reports (`docs/evidence/phase14/baseline_vs_optimized_report.md`) and standard RFC-4180 CSV tables (`baseline_vs_optimized_report.csv`).
  - `SimulationHUD.cs`: Operational stage banner display, shortcut key `[F4]`, and UI button `[Phase 14: Run Baseline vs Opt Study [F4]]` to trigger comparisons on demand.
  - `OperationalWorkflowTests.cs`: 5 EditMode tests verifying cycle stage progression, monotonic non-negative event telemetry, aisle transitions and backtracking calculations, headless picker execution, and Baseline vs Optimized performance improvement.

### Verified
- **70/70 EditMode tests passed** (`7.21s`, 0 failures, 0 warnings) recorded in `TestResults-Phase13-14.xml`.
- Empirical test report on order `ORD-STUDY-001`: Measured travel distance reduced from 96.38m to 73.18m (-24.1%), cycle time reduced from 80.54s to 63.97s (-20.6%), and aisle backtracking eliminated (from 1 to 0). Zero fabricated data.

## [0.12.0] - 2026-11-02
### Added
- **Phase 11 Controlled Order Generation**:
  - `OrderGenerationConfig.cs`: Configuration entity for deterministic experiments supporting integer `Seed`, `ItemCount`, `OrderCount`, `DuplicateSkuPolicy` (`DisallowDuplicates`, `AllowDuplicates`), and `OrderIdPrefix`.
  - `OrderGenerator.cs`: Pure domain order generator using `System.Random` with explicit seeds; resolves physical shelf locations via `StoreManager.TryResolveSkuLocation`.
  - `OrderManager.cs`: Added `TryDispatchGeneratedOrder` and `DispatchOrder(Order)`. Preserved legacy presets (`StartSampleOrder`, `StartSpecificOrder`, `TryStartSpecificOrder`) unchanged.
  - `SimulationHUD.cs`: Interactive Seed and Item count steppers, duplicate toggle, button `[Gen (Seed)]`, and keybinding `[G]` to trigger controlled orders.
  - `ControlledOrderGenerationTests.cs`: 8 EditMode tests verifying reproducibility, seed variation, catalog existence, physical position resolution, duplicate policies, batch generation, and dispatch.
- **Phase 12 Navigation Cost Matrix & Routing Optimization**:
  - `INavPathDistanceCalculator.cs`: Traversal distance abstraction decoupling path calculation from environment state.
  - `NavMeshPathDistanceCalculator.cs`: Production calculator evaluating actual corner-to-corner path lengths via `NavMesh.CalculatePath`; flags unreachable positions explicitly (`float.PositiveInfinity`).
  - `MockDistanceCalculator.cs`: Direct Manhattan/Euclidean calculator for deterministic unit testing.
  - `TravelCostMatrix.cs`: Reusable square $N \times N$ matrix ($N = K + 2$) validating dimensions, zero diagonal ($Costs[i, i] == 0$), positive entries, and unreachable handling.
  - `IRouteOptimizer.cs` & `RouteOptimizationResult.cs`: Routing contracts and measurement result structure.
  - `BaselineRouteOptimizer.cs`: Reference optimizer preserving original order sequence for A/B comparison and safe fallback.
  - `OrToolsRouteOptimizer.cs`: Local TSP solver powered by Google OR-Tools (`Google.OrTools.ConstraintSolver`, v9.15.6755) with fixed start (Order Desk: Node 0), fixed end (Packing Bench: Node $K+1$), and `GuidedLocalSearch`.
  - Safety Isolation & Safe Fallback: Automatic try/catch fallback to baseline sequence if OR-Tools throws an exception, encounters an unsupported platform (e.g. Android Meta Quest standalone), or fails to find a solution.
  - `Order.TryReorderItems`: Enables dynamically updating the active order's pick list to match the optimized sequence prior to picking while verifying multiset conservation.
  - `SimulationHUD.cs`: UI button `[Opt (OR-Tools)]` and keybinding `[O]` to optimize active order pick sequences.
  - `RoutingOptimizationTests.cs`: 9 EditMode tests verifying cost matrix dimensions and zero diagonal, unreachable handling, valid start/end, permutation integrity, route termination at packing, baseline availability, safe fallback, and real out-of-order backtracking reduction (>50% travel savings).

### Verified
- **65/65 EditMode tests passed** (`5.98s`, 0 failures, 0 warnings) recorded in `TestResults-Phase11-12.xml`.

## [0.10.0] - 2026-11-02
### Added
- **Phase 9 Quick-Commerce Dark Store Environment Upgrade**:
  - `DarkStoreSceneBuilder.cs` & `DarkStore_Phase1.unity`: Upgraded warehouse environment to 6 aisles (`A01`–`A06`, 3.0m pitch) with product-facing racks, solid closed backs between back-to-back blocks and outer walls, designated Worker Entry Zone with turnstiles, Order Collection Desk with tote dispensers, central Picking Area with $1.8\text{ m}$ corridors and front/back cross-aisles, Packing & Deposit Desk ("PACK-01"), Exit / Dispatch Handover Zone, overhead front and rear aisle labels, and $26\text{ m} \times 28\text{ m}$ perimeter boundary walls with solid `BoxCollider`s.
  - `DarkStore_Phase1_NavMesh.asset`: Re-baked NavMesh from 102 collider sources covering all corridors, aisles, and operational desks.
  - Physical shelf products: Distributed 72 catalog SKUs across all 6 aisles (12 SKUs per aisle).
  - RTX 2050 Budget compliance: 212 mesh renderers ($\le 300$), 17 GPU-instanced materials ($100\%$), 10 realtime lights ($\le 12$), 1 shadow caster.
- **Phase 10 Structured Store Location & SKU Mapping**:
  - `StoreLocation.cs`: Pure C# machine-readable location entity strictly implementing $\text{Store} \to \text{Aisle} \to \text{Side} \to \text{Bay} \to \text{Shelf Level (Tier)} \to \text{Slot}$ (format: `A01-L-B01-T01-S01`) with robust `TryParse`, `Parse`, `Format`, component properties, and equality operators.
  - `ShelfSlot.cs`: Integrated `StoreLocation Location` property and delegated `GenerateSlotId` to `StoreLocation.Format`.
  - `StoreConfiguration.cs`: Added `CreateQuickCommerce6AisleConfig()` supporting 432 total slots across 6 aisles while preserving `CreateDefault()` (288 slots, 4 aisles) for legacy baseline test parity.
  - `StoreManager.cs`: Added `TryResolveSkuLocation`, `TryResolveLocationPosition`, `TryGetSkuAtLocation`, `ValidateSkuLocationMappings`, and `ValidateLocationId`.
  - `StructuredStoreLocationTests.cs`: 6 new EditMode tests verifying unique location IDs, valid SKU mappings, world-position resolution, missing location handling, duplicate location collision detection, and invalid mapping rejection.

### Verified
- **47/47 EditMode tests passed** (`0.799s`, 0 failures, 0 warnings) recorded in `TestResults-EditMode.xml`.
- RTX 2050 scene budget audit (`docs/evidence/phase8/rtx2050_performance_audit.md`): PASS across all rendering and lighting metrics.

## [0.8.0] - 2026-10-29
### Added
- **Phase 8 Polish, RTX 2050 Optimization, Report Exporters & 7-Step Final Demo Workflow**:
  - `DemoFlowController.cs`: Orchestrates the 7-step Final Demo workflow (`1. Select scenario -> 2. Select order -> 3. Start desktop or VR -> 4. Pick products -> 5. Complete order -> 6. View work-study results -> 7. Compare another scenario`), manages the 3D `TargetSlotBeacon` above the active target shelf slot or packing bench, and records live trajectories.
  - `WorkStudyReportExporter.cs`: Deterministic exporter generating full Markdown work-study reports (`.md`), RFC-4180 elemental event logs (`_events.csv`), and summary KPI CSVs (`_summary.csv`).
  - `PerformanceSettingsManager.cs` & tuned `Assets/Settings/PC_RPAsset.asset`: Enforces and audits RTX 2050-class rendering budget (`SRP Batcher = true`, `16/16` GPU-instanced shared materials, `148` total mesh renderers, `1` shadow-casting directional light + `8` shadowless ceiling point lights, camera far clip `45.0 m`, URP shadow distance `32.0 m`, `2` shadow cascades, `2x` MSAA, post-processing disabled). Writes `docs/evidence/phase8/rtx2050_performance_audit.md` with interactive GPU FPS benchmarking marked `NOT RUN`.
  - `SimulationErrorHandler.cs`: Non-crashing error and warning handler integrated with `OrderManager.TryStartSpecificOrder` and `SimulationHUD`.
  - `Phase8PolishAndDemoTests.cs`: 5 new EditMode tests verifying 4-aisle SKU distribution, error handling & shelf visual reset, Markdown/CSV report export, RTX 2050 scene budget audit, and the complete 7-step Final Demo workflow.
- **UI & Visual Polish**:
  - `StoreManager.cs`: Distributed all 72 catalog SKUs evenly across all 4 aisles (`18` SKUs per aisle, 1 per bay-tier at `SlotIndex == 1`).
  - `OrderManager.cs`: Added `ResetShelfVisuals()` so picked shelf items automatically restock when starting a new order.
  - `DarkStoreSceneBuilder.cs`: Mapped all 8 department materials (`Mat_Prod_*`), scaled product boxes by `sku.DimensionsMeters`, added aisle header signage, floor safety/guide stripes, and dispatch staging totes, and rebuilt `Assets/Scenes/DarkStore_Phase1.unity` + `DarkStore_Phase1_NavMesh.asset`.
  - `SimulationHUD.cs`, `WorkStudyResultsUI.cs`, and `ScenarioComparisonUI.cs`: Added 7-step Demo Workflow panel (`[F1]`), full order pick checklist, Summary Breakdown vs Chronological Event Log tabs (`[Tab]`), configurable sampling intervals ($0.5\text{ s} - 5.0\text{ s}$), one-click Markdown+CSV export (`[F2]`), and one-click A/B comparison buttons (`[F3]`).

### Verified
- **41/41 EditMode tests passed** (`0.771s`, 0 failures, 0 warnings) recorded in `TestResults-EditMode.xml`.
- `dotnet build` across `VirtualDarkStore.Runtime`, `VirtualDarkStore.Editor`, and `VirtualDarkStore.Tests.Editor`: **0 Warnings, 0 Errors**.
- Verified demo A/B comparison (`docs/evidence/phase8/demo_comparison_A_vs_B.md`): `SCN-DEMO-A` ($49.30\text{ s}, 53.20\text{ m}$) vs `SCN-DEMO-B` ($65.44\text{ s}, 75.80\text{ m}$; $\Delta = +16.14\text{ s} / +22.60\text{ m}$).

## [0.7.0] - 2026-10-28
### Added
- **VR Player Rig & Parity Architecture (Phase 7)**:
  - `VRPlayerRig.cs`: Master rig coordinating XROrigin, spatial head tracking, dual controllers, pick interaction, and locomotion.
  - `VRHeadTracking.cs`: HMD pose tracking with eye-height calibration and forward gaze raycast (strictly excluding uninstrumented physiological or eye-gaze claims).
  - `VRControllerInput.cs`: OpenXR / Input System controller tracking for left and right hands, exposing triggers, grips, primary buttons, and thumbstick axes.
  - `VRPickerInteraction.cs`: Raycast and direct touch interaction validating picks against `OrderManager` and completing packing at staging benches.
  - `VRLocomotionController.cs`: Continuous thumbstick locomotion and discrete teleportation with exact displacement accumulation ($\Delta p$) to preserve industrial travel distance metrics.
  - `VRHardwareDetector.cs`: Hardware inspection utility reporting connected VR devices or marking verification as `NOT RUN`.
  - `IPickerInteraction.cs`: Shared abstraction implemented by both `PickerInteraction` (desktop) and `VRPickerInteraction` (VR).
- **Core Integration & Parity (Phase 7)**:
  - `ActivityClassifier.cs` and `SimulationHUD.cs`: Updated to dynamically support `VRPlayerRig` and `IPickerInteraction`, achieving 100% telemetry parity.
  - `DarkStoreSceneBuilder.cs`: Added `BuildVRPlayerRig` and regenerated scene `DarkStore_Phase1.unity`.
  - Packages: Added `com.unity.xr.interaction.toolkit: 3.6.1`, `com.unity.xr.core-utils: 2.6.0`, and `com.unity.xr.meta-openxr: 2.6.1`. Verified AndroidPlayer + SDK/NDK/OpenJDK.
  - OpenXR Loaders configured in `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`.
- **Automated Verification Suite (Phase 7)**:
  - `VRRigTests.cs`: 7 new unit and integration tests (36/36 tests passing in full test suite):
    - Rig hierarchy and tracking component initialization.
    - Head tracking orientation and forward ray without physiological claims.
    - Controller input pose tracking and button simulations.
    - Pick interaction validating against `OrderManager` and completing packing.
    - Locomotion displacement preservation for continuous move and teleportation.
    - Complete VR order fulfillment run with duration/percentage reconciliation.
    - Hardware detection: marked `NOT RUN` (evidence saved to `docs/evidence/phase7/vr_hardware_report.md`).

## [0.6.0] - 2026-10-28
### Added
- **Scenarios & A/B Comparison (Phase 6)**:
  - `ScenarioDefinition.cs`: Serializable scenario configuration defining SKU visit sequences, start position, walk speed, search time, packing time, and time step.
  - `ScenarioStorage.cs`: JSON serialization and file persistence for scenarios and run results.
  - `ScenarioRunner.cs`: Headless scenario execution driving a simulated picker through `OrderManager`, `ActivityClassifier`, and `WorkEventLogger`.
  - `ScenarioComparison.cs`: Comparative analysis engine calculating KPI deltas (B minus A) and activity distribution between runs under equivalent starting conditions. All outputs labelled as measured simulation results, not predictions.
  - `RouteMapRenderer.cs`: Rasterized top-down 2D route map PNG generator.
  - `ScenarioRouteVisualizer.cs`: In-world line renderer for spatial path inspection.
  - `ScenarioComparisonUI.cs`: Screen-space IMGUI modal (toggleable via `[F3]`) displaying A/B KPI tables and route maps.
  - `ScenarioTests.cs`: 5 automated tests verifying definition validation, save/load, deterministic runner, equivalence checks, and evidence export (`docs/evidence/phase6/`).

## [0.5.0] - 2026-10-28
### Added
- **Work-Study Calculator & Industrial Metrics Engine (Phase 5)**:
  - `WorkStudyResults.cs`: Results data model storing continuous time-study metrics (total task time, travel breakdown, picking, searching, waiting, packing, total distance, total picks, distinct shelf bay visits, activity percentages) and configurable discrete work-sampling observations. Includes mathematical reconciliation engine `VerifyReconciliation(out string reason)`.
  - `WorkStudyCalculator.cs`: Computational engine aggregating raw `WorkEvent` sequences into reconciled industrial metrics. Identifies distinct shelf visits by parsing rack bay identifiers, groups travel time into moving vs approaching, and computes simulation-based interval work sampling without unverified standards claims.
- **Interactive Work-Study Results UI Modal (Phase 5)**:
  - `WorkStudyResultsUI.cs`: Screen-space IMGUI modal displaying overview cards (Total Task Time, Travel Distance, Picks, Shelf Visits), detailed Time-Study category breakdown with percentage distribution, Work-Sampling observation proportions, and real-time reconciliation badges (`RECONCILED: YES / NO`).
  - Automatically pops up upon order completion and is toggleable at any time with `[Tab]`.
  - `SimulationHUD.cs`: Updated controls bar to include `Tab: Study Results`.
- **Scene & Editor Integration (Phase 5)**:
  - `DarkStoreSceneBuilder.cs`: Updated scene generation pipeline to attach `WorkStudyResultsUI` to `[WorkStudyManager]`.
  - Rebuilt scene `Assets/Scenes/DarkStore_Phase1.unity`.
- **Automated Verification Suite (Phase 5)**:
  - `WorkStudyCalculatorTests.cs`: 4 automated unit and integration tests:
    - `EmptyEventsList_ProducesZeroResults_ReconcilesSuccessfully`
    - `DeterministicScenario_CalculatesAccurateMetrics_AndReconciles`
    - `WorkSampling_ConfigurableInterval_MatchesDiscreteObservationRule`
    - `SampleOrderExecution_GeneratesFullyReconciledResults`

### Verified
- **24/24 EditMode tests passed** (0 failures, 100% pass rate in 0.332s) recorded in `TestResults-EditMode.xml`.
- Exact mathematical reconciliation validated on deterministic test scenario:
  - Total task time: 30.00s
  - Category durations sum: 30.00s ($\Delta = 0.000\text{s}$)
  - Category percentages sum: 100.00% ($\Delta = 0.000\%$)
  - Total distance: 27.00m
  - Total picks: 2
  - Distinct shelf visits: 2
  - Work-sampling proportions sum: 100.00%
- Verified sample order run generated fully reconciled industrial metrics ($\text{Reconciled} = \text{true}$).
- Zero unverified ANSI/ISO claims and zero fabricated improvement numbers.

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

