# Phase Status Tracker

## Overview
Status of project phases for the Virtual Dark Store Work-Study & Simulation Platform.

| Phase | Description | Status | Completion Date |
|---|---|---|---|
| **Phase 0** | Baseline & Workspace Verification | **COMPLETED** | 2026-10-28 |
| **Phase 1** | Clean URP Scene, Layout Blockout & Desktop Controller | **COMPLETED** | 2026-10-28 |
| **Phase 2** | Dark Store Topology, Inventory Model & SKU-Location Mapping | **COMPLETED** | 2026-10-28 |
| **Phase 3** | Order Engine & Fulfillment Simulation | **COMPLETED** | 2026-10-28 |
| **Phase 4** | Desktop Interactive Mode & Instrumentation | **COMPLETED** | 2026-10-28 |
| **Phase 5** | Work-Sampling & Automated Exporters (CSV/JSON) | **COMPLETED** | 2026-10-28 |
| **Phase 6** | Scenarios, A/B Comparison, Route Visualization & KPI Comparison | **COMPLETED** | 2026-10-28 |
| **Phase 7** | VR Rig Integration & Desktop-VR Parity | **COMPLETED** | 2026-10-28 |
| **Phase 8** | Polish, RTX 2050 Optimization, Report Exporters & Final Demo Workflow | **COMPLETED** | 2026-10-29 |
| **Phase 9** | Quick-Commerce Dark Store Environment Upgrade | **COMPLETED** | 2026-11-02 |
| **Phase 10** | Structured Store Location & SKU Mapping | **COMPLETED** | 2026-11-02 |
| **Phase 11** | Controlled Order Generation | **COMPLETED** | 2026-11-02 |
| **Phase 12** | Navigation Cost Matrix & Routing Optimization | **COMPLETED** | 2026-11-02 |
| **Phase 13** | End-to-End Operational Workflow | **COMPLETED** | 2026-11-02 |
| **Phase 14** | Baseline vs Optimized Work-Study Comparison | **COMPLETED** | 2026-11-02 |
| **Visual & Usability** | Physical Location Labels & Store Signage | **COMPLETED** | 2026-11-02 |
| **Visual & Usability** | Shelf Population (144 SKUs Across All Usable Racks) | **COMPLETED** | 2026-11-02 |
| **Simulation Enhancement** | Configurable Multi-Order Simulation | **COMPLETED** | 2026-11-02 |

---

## Detailed Phase Breakdown

### Phase 0: Baseline & Workspace Verification
- **Goal**: Inspect repository, verify Unity version, inspect packages and project settings, create comprehensive documentation foundation, and formulate execution plan without altering application code.
- **Status**: **COMPLETED** (2026-10-28)
- **Delivered**:
  - Verified Unity 6000.6.7f1 and URP 17.6.0 environment.
  - Inspected existing assets and packages.
  - Created documentation baseline (`AGENTS.md`, `README.md`, `PROJECT_BRIEF.md`, `ARCHITECTURE.md`, `SETUP.md`, `PHASE_STATUS.md`, `DEVELOPMENT_LOG.md`, `DECISIONS.md`, `RESOURCE_REGISTER.md`, `TESTING.md`, `CHANGELOG.md`).
  - Formulated Phase 0 implementation plan.

### Phase 1: Clean URP Scene, Layout Blockout & Desktop Controller
- **Goal**: Build clean URP dark store scene with 4 aisles, solid shelving colliders, concrete envelope, dispatch packing bench, directional/ambient aisle lighting, desktop picker controller, and baked NavMesh.
- **Status**: **COMPLETED** (2026-10-28)
- **Delivered**:
  - `Assets/Scenes/DarkStore_Phase1.unity`: 4 aisles, 24m x 28m concrete warehouse envelope, boundary walls.
  - `Assets/Scripts/Environment/DarkStoreAisle.cs`, `ShelfUnit.cs`, `PackingStation.cs`.
  - `Assets/Scripts/Player/DesktopPickerController.cs`: First-person controller with CharacterController, WASD locomotion, mouse look, sprint, and gravity.
  - `Assets/Scenes/DarkStore_Phase1_NavMesh.asset`: Baked using `UnityEngine.AI.NavMeshBuilder` from 72 collider sources.

### Phase 2: Dark Store Topology, Inventory Model & SKU-Location Mapping
- **Goal**: Implement StoreManager, Aisle, Shelf, ShelfSlot, Sku, StoreConfiguration, and 50–100 verified placeholder/sample SKUs across multiple shelf levels with unique IDs, separating visual GameObjects from pure domain data; verify bidirectional mapping and zero collision via EditMode test runner.
- **Status**: **COMPLETED** (2026-10-28)
- **Delivered**:
  - Domain models: `Sku.cs`, `ShelfSlot.cs`, `Shelf.cs`, `Aisle.cs`, `StoreConfiguration.cs`.
  - Verified sample catalog: 72 distinct SKUs in `VerifiedSampleCatalog.cs` across 8 grocery categories.
  - Central manager: `StoreManager.cs` managing 288 shelf slots (4 aisles * 2 sides * 3 bays * 3 tiers * 4 slots) with 72 SKUs mapped 1:1.
  - Automated EditMode unit tests: 8 tests passing in `StoreTopologyTests.cs`.
  - Primary scene configured in `EditorBuildSettings.asset`.

### Phase 3: Order Engine & Fulfillment Simulation
- **Goal**: Implement Order, OrderItem, OrderManager, picker raycast interaction, physical shelf SKU items, order completion at packing station, and simulation HUD. Enforce strict validation: wrong/missing items never count as correct. Run complete sample order cycle.
- **Status**: **COMPLETED** (2026-10-28)
- **Delivered**:
  - `Order.cs`: Pure domain entity managing line items, order status progression, and strict SKU pick validation (`ValidateAndPick`).
  - `OrderItem.cs`: Domain line item model tracking SKU, target shelf slot, required vs. picked quantities.
  - `OrderManager.cs`: Central singleton manager for sample/deterministic order dispatch, pick attempt validation, packing completion, and event broadcasting.
  - `PickableItemVisual.cs`: Component on shelf products providing collider hit detection, visual disable upon picking, and reset.
  - `PickerInteraction.cs`: First-person reticle raycast interaction (2.8m range) detecting shelf items and packing bench with `[E]` / LMB triggers.
  - `SimulationHUD.cs`: Screen-space IMGUI HUD with central reticle, dynamic interaction prompts, active pick list, target bin distance, and color-coded feedback banners (green pick, red mismatch error, gold completion).
  - 72 physical pickable product boxes spawned across shelves in `DarkStore_Phase1.unity` with category materials.
  - NavMesh rebaked with 144 collider sources.
  - 15 EditMode tests executed and passing (7 new order fulfillment tests + 8 topology tests).

### Phase 4: Desktop Interactive Mode & Instrumentation
- **Goal**: Implement WorkEvent, WorkEventLogger, and ActivityClassifier state machine covering all 8 activities (`Walking`, `Waiting`, `Searching`, `Approaching`, `Picking`, `MovingBetweenLocations`, `Packing`, `OrderCompleted`) with strictly measurable physical parameters, non-negative durations, and monotonic ordered timestamps. Enforce strict prohibition of uninstrumented eye-gaze/fatigue/physiological claims. Run sample order and inspect events.
- **Status**: **COMPLETED** (2026-10-28)
- **Delivered**:
  - `ActivityType.cs`: Enum defining all 8 industrial work-study activities.
  - `WorkEvent.cs`: Immutable measurement record encapsulating ID, activity, start/end time, duration, position, location, related SKU/order, and distance traveled with strict invariant validation.
  - `WorkEventLogger.cs`: Central monotonic event recorder validating chronological ordering and non-negative duration invariants.
  - `ActivityClassifier.cs`: State machine and activity classifier with clear, measurable classification rules for all 8 activities (e.g. `Searching` defined as stationary within 3m of target shelf bay with raycast intersecting shelf geometry).
  - `SimulationHUD.cs` updated to display live activity state and logged event counter.
  - `DarkStoreSceneBuilder.cs` updated to include `[WorkStudyManager]` in scene hierarchy.
  - `WorkStudyInstrumentationTests.cs`: 5 unit and integration tests verifying duration math, negative/inverted timestamp rejection, monotonic logger ordering, rule evaluation, and end-to-end sample order run with 13 inspected events.
  - 20/20 EditMode tests executed and passing.

### Phase 5: Work-Sampling & Automated Exporters
- **Goal**: Implement WorkStudyCalculator, results data model, and results UI. Calculate total task time, walking, searching, picking, waiting, packing, distance, picks, shelf visits, and activity percentages from actual event logs. Provide configurable interval-based work sampling and verify category duration and percentage reconciliation without fabricated metrics.
- **Status**: **COMPLETED** (2026-10-28)
- **Delivered**:
  - `WorkStudyResults.cs`: Results data model holding aggregated industrial engineering metrics: total task time, walking/searching/picking/waiting/packing durations, percentages, distance, picks, shelf visits, and mathematical reconciliation validation (`VerifyReconciliation`).
  - `WorkStudyCalculator.cs`: Pure computational engine aggregating metrics from event logs, supporting configurable interval-based work-sampling without fabricated metrics or unvalidated standards compliance claims.
  - `WorkStudyResultsUI.cs`: Screen-space IMGUI overlay modal toggleable via `[Tab]` or shown upon order completion displaying metric cards, time-study table, work-sampling proportions, and reconciliation status badges.
  - Scene builder updated to attach `WorkStudyResultsUI` to `[WorkStudyManager]`.
  - `WorkStudyCalculatorTests.cs`: 4 unit and integration tests verifying duration reconciliation ($\sum T_c = T_{total}$), percentage reconciliation ($\sum P_c = 100\%$), exact pick/shelf visit counts, configurable work-sampling intervals, and full sample order evaluation.
  - 24/24 EditMode tests executed and passing.

### Phase 6: Scenarios, A/B Comparison, Route Visualization & KPI Comparison
- **Goal**: Scenario definition, save/load, A/B comparison of the same order under equivalent starting conditions using actual recorded runs; route visualisation; KPI comparison. Measured simulation results only; no AI/optimisation.
- **Status**: **COMPLETED** (2026-10-28)
- **Delivered**: `Assets/Scripts/Scenarios/*`, `ScenarioComparisonUI.cs`, `ScenarioTests.cs` (5 tests), saved scenarios in `Scenarios/`, evidence in `docs/evidence/phase6/`. 29/29 EditMode tests passed. A: 55.09 s, 57.80 m; B: 44.80 s, 43.40 m (pick sequence only varied).

### Phase 7: VR Rig Integration & Desktop-VR Parity
- **Goal**: Integrate OpenXR / XR Interaction Toolkit into an equivalent VR player rig sharing the exact same WorkStudy and Simulation core. Verify head tracking, controller input, basic pick interaction, continuous locomotion and teleportation with exact displacement preservation. Check physical headset and mark hardware verification NOT RUN if none exists.
- **Status**: **COMPLETED** (2026-10-28)
- **Delivered**:
  - OpenXR integration with Standalone and Android build target support.
  - Shared `IPickerInteraction.cs` contract.
  - VR Rig components (`VRPlayerRig.cs`, `VRHeadTracking.cs`, `VRControllerInput.cs`, `VRPickerInteraction.cs`, `VRLocomotionController.cs`, `VRHardwareDetector.cs`).
  - 36/36 EditMode tests executed and passing.
  - Hardware Verification marked **NOT RUN** (evidence in `docs/evidence/phase7/vr_hardware_report.md`).

### Phase 8: Polish, RTX 2050 Optimization, Report Exporters & Final Demo Workflow
- **Goal**: Polish UI, work-study report export, store visuals, performance for RTX 2050-class hardware, error handling, scene organization, demo scenario, and reproducible setup. Execute 7-step Final Demo workflow without adding AI/ML or inventing benchmark numbers.
- **Status**: **COMPLETED** (2026-10-29)
- **Delivered**:
  - Distribution across 4 aisles (`A01`–`A04`), proportional items, header signage, safety lines, dispatch tote bins.
  - `SimulationErrorHandler.cs` and `OrderManager.TryStartSpecificOrder`.
  - RTX 2050 Performance Profile: SRP Batcher, GPU instancing, single shadow caster.
  - `WorkStudyReportExporter.cs` Markdown and CSV bundle exporter.
  - `DemoFlowController.cs` 7-step workflow with `[F1]` HUD, `[Tab]` results, `[F3]` comparison.
  - 41/41 EditMode tests passing.

### Phase 9: Quick-Commerce Dark Store Environment Upgrade
- **Goal**: Upgrade the environment into a realistic generic quick-commerce dark store with A01–A06 aisles, two product-facing rack sides per aisle, closed backs where appropriate, designated entry zone, order collection desk, picking area, packing/deposit desk, exit zone, overhead aisle labels, perimeter walls, realistic walkable corridors, and full NavMesh coverage while maintaining RTX 2050 GPU budget.
- **Status**: **COMPLETED** (2026-11-02)
- **Delivered**:
  - **6 Picking Aisles (`A01`–`A06`)**: Expanded from 4 to 6 aisles at $X = -7.5\text{ m}, -4.5\text{ m}, -1.5\text{ m}, +1.5\text{ m}, +4.5\text{ m}, +7.5\text{ m}$ ($3.0\text{ m}$ pitch).
  - **Closed Rack Backs**: Solid back panel dividers between back-to-back rack blocks ($X = -6.0\text{ m}, -3.0\text{ m}, 0.0\text{ m}, +3.0\text{ m}, +6.0\text{ m}$) and outer warehouse perimeter racks ($X = -9.0\text{ m}, +9.0\text{ m}$).
  - **Designated Operational Zones**:
    - Worker Entry Zone ($X = -8.0\text{ m}, Z = -10.5\text{ m}$) with arch, overhead sign, walkway pad, and barrier rails with solid collision.
    - Order Collection Desk ($X = -4.0\text{ m}, Z = -10.0\text{ m}$) with desk table, computer terminal, pick cart tote bins, and signage.
    - Picking Area spanning Aisles `A01`–`A06` with $1.8\text{ m}$ walkable corridors, $3.0\text{ m}$ front cross-aisle, $3.5\text{ m}$ rear cross-aisle, and aisle guide stripes.
    - Packing & Deposit Desk ($X = +2.0\text{ m}, Z = -10.0\text{ m}$) with packing bench, computer terminal, dispatch tote bins, floor staging pad, and `PackingStation` component ("PACK-01").
    - Exit / Dispatch Handover Zone ($X = +8.0\text{ m}, Z = -10.5\text{ m}$) with arch, overhead sign, egress pad, and barrier rails.
  - **Aisle Labels**: Overhead high-contrast front entrance signage (`Aisle_01_HeaderSign`–`Aisle_06_HeaderSign`) and rear entrance signage (`Aisle_01_BackSign`–`Aisle_06_BackSign`).
  - **Boundaries & Collision**: $26\text{ m} \times 28\text{ m}$ warehouse envelope, $4.5\text{ m}$ perimeter walls, and solid `BoxCollider`s on all racks, desks, barriers, and envelope boundaries.
  - **NavMesh Coverage**: Re-baked using `UnityEngine.AI.NavMeshBuilder` from 102 collider sources covering all walkable corridors and operational desks (`Assets/Scenes/DarkStore_Phase1_NavMesh.asset`).
  - **RTX 2050 Budget**: 212 total mesh renderers ($\le 300$), 17 shared GPU-instanced materials ($100\%$), 10 realtime lights ($\le 12$), 1 shadow caster, camera far clip $45.0\text{ m}$.

### Phase 10: Structured Store Location & SKU Mapping
- **Goal**: Establish stable machine-readable physical locations using the $\text{Store} \to \text{Aisle} \to \text{Side} \to \text{Bay} \to \text{Shelf Level (Tier)} \to \text{Slot}$ hierarchy (e.g. `A01-L-B01-T01-S01`). Implement stable unique location IDs, SKU $\to$ Location ID $\to$ World Position resolution, deterministic mapping, and validation for missing, invalid, and duplicate mappings without creating competing models.
- **Status**: **COMPLETED** (2026-11-02)
- **Delivered**:
  - `StoreLocation` domain struct (`Assets/Scripts/Domain/StoreLocation.cs`): Machine-readable location encapsulation, `TryParse`, `Parse`, `Format`, equality operators, component accessors (`StoreId`, `AisleNumber`, `Side`, `BayIndex`, `TierLevel`, `SlotIndex`).
  - `ShelfSlot` integration: Exposes `StoreLocation Location` and delegates `GenerateSlotId` to `StoreLocation.Format`.
  - `StoreConfiguration` extension: Added `CreateQuickCommerce6AisleConfig()` supporting 432 total slots across 6 aisles while preserving `CreateDefault()` (288 slots, 4 aisles) for legacy baseline test parity.
  - `StoreManager` location query & resolution APIs:
    - `TryResolveSkuLocation(string skuId, out string locationId, out Vector3 worldPosition)`
    - `TryResolveLocationPosition(string locationId, out Vector3 worldPosition)`
    - `TryGetSkuAtLocation(string locationId, out Sku sku)`
    - `ValidateSkuLocationMappings(out List<string> errors)`
    - `ValidateLocationId(string locationId, out string error)`
  - Automated EditMode unit tests (`Assets/Tests/Editor/Domain/StructuredStoreLocationTests.cs`):
    1. `UniqueLocationIds_AllSlotsInStore_HaveDistinctFormattedIds`
    2. `ValidSkuMappings_AllSkus_MapToOneToOneValidPhysicalLocation`
    3. `WorldPositionResolution_SkuAndLocationId_ResolveToValidWorldCoordinates`
    4. `MissingLocation_NonExistentSkuOrLocationId_ReturnsFalseWithoutThrowing`
    5. `DuplicateLocation_DetectsCollision_WhenTwoSkusAssignedSameSlot`
    6. `InvalidMapping_MalformedLocationStrings_AreDetectedAndRejected`
  - Automated Verification: **47 / 47 EditMode Tests Passing** (`0.799s`, 0 failures, 0 warnings).

### Phase 11: Controlled Order Generation
- **Goal**: Generate repeatable, controlled pick orders for industrial work-study experiments while preserving preset and deterministic sample orders.
- **Status**: **COMPLETED** (2026-11-02)
- **Delivered**:
  - `OrderGenerationConfig`: Configuration entity holding integer random `Seed`, `ItemCount`, `OrderCount`, `DuplicateSkuPolicy` (`DisallowDuplicates`, `AllowDuplicates`), and `OrderIdPrefix`.
  - `OrderGenerator` domain generator (`Assets/Scripts/Domain/OrderGenerator.cs`): Pure C# deterministic order generator using `System.Random` with explicit seeds; zero Python runtime or external dependencies. Resolves physical shelf locations via `StoreManager.TryResolveSkuLocation`.
  - `OrderManager` integration:
    - `TryDispatchGeneratedOrder(OrderGenerationConfig config, out Order order, out string errorReason)`
    - `DispatchOrder(Order order)`: Sets active order, resets shelf visuals, and fires `OnOrderStarted`.
    - Preserves existing presets (`StartSampleOrder`, `StartSpecificOrder`, `TryStartSpecificOrder`, `OrderPreset`) 100% unchanged.
  - Minimal HUD integration:
    - Interactive Seed stepper `[-]` / `[+]`, Item count stepper `[-]` / `[+]`, and generation button `[Gen (Seed)]` in `SimulationHUD.cs`.
    - Keybinding `[G]` triggers deterministic order generation directly from first-person view.
  - Automated EditMode unit tests (`Assets/Tests/Editor/Domain/ControlledOrderGenerationTests.cs`):
    1. `SameSeed_ProducesIdenticalOrder`: Verifies two runs with same seed produce identical SKU IDs, quantities, order ID, and locations.
    2. `DifferentSeeds_ProduceDifferentOrders`: Verifies different seeds select different SKU sequences.
    3. `AllGeneratedSkus_ExistInStoreCatalog`: Verifies every generated SKU exists in StoreManager.
    4. `AllGeneratedLocations_HaveValidWorldPositions`: Verifies every generated item has valid location ID and 3D position.
    5. `DuplicatePolicy_Disallow_ProducesDistinctSkus`: Verifies distinct SKUs under `DisallowDuplicates`.
    6. `BatchGeneration_ProducesRequestedCountAndReproducibleSequences`: Verifies batch generation reproducibility.
    7. `PresetOrders_RemainUnchanged`: Verifies legacy presets remain intact.
    8. `GeneratedOrder_DispatchesAndExecutesSuccessfully`: Verifies dispatch and subsequent valid picking.
  - Automated Verification: **8 / 8 Tests Passing**.

### Phase 12: Navigation Cost Matrix & Routing Optimization
- **Goal**: Separate physical pathfinding ($A \to B$ via Unity NavMesh) from multi-stop pick sequence visit order ($A \to B \to C \to D$ via TSP/routing solver). Build reusable travel-cost matrix, integrate Google OR-Tools locally behind an adapter, provide baseline sequence preservation and automatic safe fallback.
- **Status**: **COMPLETED** (2026-11-02)
- **Delivered**:
  - **Part A — Travel Cost & Navigation**:
    - `INavPathDistanceCalculator`: Traversal calculation abstraction decoupling matrix math from NavMesh environment.
    - `NavMeshPathDistanceCalculator`: Accurately computes path length along corridor corners via `NavMesh.CalculatePath`; flags unreachable positions explicitly (`float.PositiveInfinity`).
    - `MockDistanceCalculator`: Direct Manhattan/Euclidean calculator for deterministic unit testing.
  - **Part B — Reusable Cost Matrix**:
    - `TravelCostMatrix` (`Assets/Scripts/Routing/TravelCostMatrix.cs`): $N \times N$ matrix encapsulating node positions, descriptive labels, and traversable path costs.
    - Validates square dimensions, zero diagonal ($Costs[i, i] == 0$), non-negative values, and explicit unreachable handling.
  - **Part C — Google OR-Tools Integration & Safe Fallback**:
    - Local execution verified on Unity 6000.6.7f1 (Windows x64).
    - `IRouteOptimizer` interface and `RouteOptimizationResult` model.
    - `BaselineRouteOptimizer`: Reference optimizer preserving original order sequence for A/B comparison and safe fallback.
    - `OrToolsRouteOptimizer`: Formulates TSP with fixed start (Order Desk: Node 0) and fixed end (Packing Bench: Node $K+1$) using `RoutingIndexManager`, `RoutingModel`, and `GuidedLocalSearch`.
    - Safety isolation: Automatic try/catch fallback to baseline sequence if OR-Tools throws an exception, encounters an unsupported platform (e.g. Android Meta Quest standalone), or fails to find a solution.
    - Mathematical honesty: Does not claim global optimality unless proven (`IsOptimalProven = false`).
  - **Order Pick Sequence Reordering**:
    - `Order.TryReorderItems`: Allows dynamically reordering pick checklist to follow the optimized sequence prior to picking while strictly verifying SKU multiset conservation.
    - `OrderManager.OptimizeActiveOrderRoute`: One-call optimization for HUD and simulation runtime.
  - Minimal HUD integration:
    - Keybinding `[O]` and UI button `[Opt (OR-Tools)]` in `SimulationHUD.cs`. Displays baseline vs optimized travel cost reduction.
  - Automated EditMode unit tests (`Assets/Tests/Editor/Routing/RoutingOptimizationTests.cs`):
    1. `CostMatrix_CalculatesDistances_WithZeroDiagonal`: Verifies matrix dimensions, zero diagonal, positive costs, and validation.
    2. `CostMatrix_HandlesUnreachableNodes_Explicitly`: Verifies explicit unreachable handling.
    3. `Routing_ValidStartAndEnd`: Verifies route starts at Node 0 and ends at Node $K+1$.
    4. `Routing_EveryRequiredSkuExactlyOnce`: Verifies every required SKU stop appears exactly once.
    5. `Routing_NoUnexpectedSku`: Verifies no foreign or duplicated nodes.
    6. `Routing_RouteEndsAtPacking`: Verifies route terminates at packing bench.
    7. `Routing_BaselineSequenceRemainsAvailable`: Verifies baseline sequence is preserved.
    8. `Routing_SolverFailure_FallsBackSafely`: Verifies safe fallback to baseline without throwing.
    9. `OrTools_OptimizesVisitSequence_WhenOutOfOrder`: Verifies OR-Tools reduces backtracking on an out-of-order route from 75m to 35m (>50% travel reduction).
  - Automated Verification: **9 / 9 Tests Passing** (Total EditMode suite: **65 / 65 Passing**, 0 failures).

### Phase 13: End-to-End Operational Workflow
- **Goal**: Connect the environment, order system, picking, and work-study into one continuous operational cycle: `ENTRY` $\to$ `COLLECT ORDER` $\to$ `PICKING` $\to$ `AWAITING PACKING` $\to$ `DEPOSIT ORDER` $\to$ `WALK TO EXIT` $\to$ `COMPLETED`.
- **Status**: **COMPLETED** (2026-11-02)
- **Delivered**:
  - `OperationalCycleManager` (`Assets/Scripts/Management/OperationalCycleManager.cs`):
    - 7-stage state machine (`Entry` $\to$ `CollectOrder` $\to$ `Picking` $\to$ `AwaitingPacking` $\to$ `DepositOrder` $\to$ `WalkToExit` $\to$ `Completed`).
    - Fixed physical waypoint coordinates (`EntryPosition`, `CollectionDeskPosition`, `PackingDeskPosition`, `ExitPosition`).
    - Proximity checks and zone detection for interactive desktop/VR controllers.
    - Deterministic aisle transition and backtracking analysis (`CalculateAisleMetrics`).
    - Summary KPI record generator (`OperationalCycleSummary`).
  - Desktop Player Interaction Extensions (`Assets/Scripts/Player/PickerInteraction.cs`):
    - Added raycast detection and `[E / LMB]` prompts for Order Collection Desk and Packing Desk deposit.
  - Scripted Virtual Picker Execution (`Assets/Scripts/Scenarios/OperationalCycleRunner.cs`):
    - Virtual simulation agent executing the full 7-stage cycle, corridor transit, search delay, pick interaction, packing, and exit progression without manual input.
  - Runtime Integration:
    - Attached `OperationalCycleManager` to `[WorkStudyManager]` in `DarkStore_Phase1.unity`.
    - Scene rebuilt and verified via Unity batchmode.

### Phase 14: Baseline vs Optimized Work-Study Comparison
- **Goal**: Conduct an empirical industrial engineering comparison between baseline (original/preset SKU sequence) and solver-optimized pick routes under 100% identical environmental, inventory, and navigation conditions.
- **Status**: **COMPLETED** (2026-11-02)
- **Delivered**:
  - `OperationalComparison` (`Assets/Scripts/Scenarios/OperationalComparison.cs`):
    - Compares Run A (Baseline) vs Run B (Optimized).
    - Measures actual total cycle time, walking time, searching time, picking time, waiting time, packing time, measured distance, picks, shelf visits, aisle transitions, and aisle backtracking.
    - Exports Markdown comparison report and tabular CSV bundles to `docs/evidence/phase14/`.
  - UI & HUD Integration (`Assets/Scripts/UI/SimulationHUD.cs`):
    - Displays current operational cycle stage banner.
    - Added shortcut key `[F4]` and UI button `[Phase 14: Run Baseline vs Opt Study [F4]]` to trigger automated comparison and export reports on demand.
  - Empirical Evidence Artifacts:
    - Generated `docs/evidence/phase14/baseline_vs_optimized_report.md` and `.csv`.
    - Observed in 4-item test case: travel distance reduced from 96.38m to 73.18m (-24.1%), cycle time reduced from 80.54s to 63.97s (-20.6%), and aisle backtracking eliminated (from 1 to 0).
  - Automated EditMode unit tests (`Assets/Tests/Editor/Scenarios/OperationalWorkflowTests.cs`):
    1. `OperationalStage_FullCycleProgression_ReachesCompleted`: Verifies complete 7-stage cycle progression.
    2. `OperationalEvents_RecordedMonotonically_WithZeroNegativeDurations`: Verifies monotonic timestamps and non-negative event durations.
    3. `AisleTransitionsAndBacktracking_CalculatesAccurately`: Verifies industrial engineering aisle transition and backtracking mathematics.
    4. `OperationalCycleRunner_ExecutesEndToEndWorkflow_Deterministically`: Verifies scripted picker execution and work-study reconciliation.
    5. `BaselineVsOptimized_SameOrder_IdenticalStartingConditions_VariesOnlyPickSequence`: Verifies comparison execution, identical conditions, metric improvements, and report generation.
  - Automated Verification: **5 / 5 Phase 13/14 Tests Passing** (Total EditMode suite: **70 / 70 Passing**, 0 failures).

### Visual & Usability Improvement: Physical Location Labels & Store Signage
- **Goal**: Add clear, readable, lightweight physical labels to the existing dark-store environment representing the existing Store -> Aisle -> Side -> Bay -> Shelf Level (Tier) -> Slot hierarchy (e.g. `A01-L-B01-T01-S01`) synchronized with simulation location IDs, without duplicating location models, without interfering with NavMesh or raycasting, and fully complying with the RTX 2050 GPU budget.
- **Status**: **COMPLETED** (2026-11-02)
- **Delivered**:
  - `PhysicalLocationLabel` component (`Assets/Scripts/Environment/PhysicalLocationLabel.cs`):
    - World Space Canvas signage component with high pixel density (500 px/meter) for sharp in-world legibility from normal picker distances (1m–15m).
    - Fully collider-free with `raycastTarget = false` across all graphic elements, preventing any interference with NavMesh, walking, or picker interaction.
    - Supports `AisleHeader` ($2.4\text{ m} \times 0.26\text{ m}$), `EndCapPlacard` ($0.50\text{ m} \times 0.22\text{ m}$), `BayRack` ($0.60\text{ m} \times 0.22\text{ m}$), `ShelfLevel` ($0.24\text{ m} \times 0.055\text{ m}$), and `ProductSlot` tickets ($0.22\text{ m} \times 0.05\text{ m}$).
  - Full Store Signage Integration (`Assets/Scripts/Editor/DarkStoreSceneBuilder.cs`):
    - 12 Overhead Aisle Header Signs across all 6 aisles (Front South-facing and Rear North-facing).
    - 12 End-Cap Rack Placards visible from the front cross-aisle.
    - 36 Bay Placards mounted at eye level ($Y = 2.15\text{ m}$) atop each bay facing the aisle corridor.
    - 108 Shelf Level Badges (`LEVEL 01`, `LEVEL 02`, `LEVEL 03`) aligned with shelf tier heights.
    - 72 Product Slot Tickets mounted on the shelf beam directly beneath each active SKU.
  - Interaction Prompt Enhancements (`PickerInteraction.cs`, `VRPickerInteraction.cs`):
    - Desktop and VR reticle prompts display the exact slot ID: `[E / LMB] Pick: <SkuName> (<SkuId>) [<SlotId>]` / `[Trigger] Pick: <SkuName> (<SkuId>) [<SlotId>]`.
  - Automated Verification: **75 / 75 EditMode Tests Passing** (5 new tests in `PhysicalLocationLabelTests.cs` + 70 existing tests, 0 failures, 0 warnings).

### Visual & Usability Improvement: Shelf Population (144 SKUs Across All Usable Racks)
- **Goal**: Populate usable shelves across the quick-commerce dark store with lightweight simulation-valid products, establishing a believable stocked micro-fulfillment inventory. Ensure strict 1:1 mapping between visual GameObjects and simulation records, deterministic placement, broad coverage across multiple aisles (`A01`–`A06`), sides (`L`, `R`), bays (`B01`–`B03`), and tiers (`T01`–`T03`), without duplicating inventories, without disrupting order simulation or routing, and fully adhering to the RTX 2050 GPU budget.
- **Status**: **COMPLETED** (2026-11-02)
- **Delivered**:
  - Expanded Catalog (`VerifiedSampleCatalog.CreateQuickCommerceCatalog`):
    - Added 72 verified grocery SKUs (`SKU-0073` to `SKU-0144`) spanning all 8 existing categories (Beverages, Dairy, Bakery, Snacks, Pantry, Produce, Personal Care, Household Cleaning) with realistic weights, barcodes (`890100000073`–`890100000144`), and metric dimensions.
    - Preserved base 72-SKU catalog (`CreateCatalog()`) untouched for backward compatibility.
  - StoreManager Inventory Resolution (`Assets/Scripts/Management/StoreManager.cs`):
    - Automatically loads `VerifiedSampleCatalog.CreateQuickCommerceCatalog()` (144 SKUs) when configured with 6 or more aisles, and `CreateCatalog()` (72 SKUs) for 4-aisle configurations.
    - Preserves deterministic pass-by-pass slot allocation, ensuring base SKUs retain identical historical coordinates (`SKU-0001` at `A01-L-B01-T01-S01`, etc.).
  - Scene Population (`Assets/Scripts/Editor/DarkStoreSceneBuilder.cs`):
    - Populates 144 physical pickable items (`PickableItemVisual`) across all 6 aisles with category-specific GPU-instanced materials and unique shelf tickets.
    - NavMesh baked with 174 collider sources.
    - Strict RTX 2050 GPU budget compliance: 284 total mesh renderers (<= 300 budget ceiling), 17 GPU-instanced shared materials, 1 shadow-casting directional light.
  - Unit & Integration Test Suite (`StructuredStoreLocationTests.cs`, `StoreTopologyTests.cs`):
    - `ValidSkuMappings_AllSkus_MapToOneToOneValidPhysicalLocation`: Validates 1:1 bidirectional mapping, uniqueness, and world coordinate resolution across all 144 SKUs.
    - `PopulatedShelves_SpanAcrossMultipleAislesSidesBaysAndTiers`: Verifies that all 6 aisles have >= 18 items, both Left and Right sides have >= 9 items, all 3 bays have >= 6 items, and multiple shelf tiers have items.
    - `VerifiedSampleCatalog_QuickCommerceCatalog_Contains144UniqueSkus`: Verifies 144 unique SKU IDs and barcodes with valid domain attributes.
  - Automated Verification: **77 / 77 EditMode Tests Passing** (2 new tests + 75 existing tests, 0 failures, 0 warnings).

### Simulation Enhancement: Configurable Multi-Order Simulation
- **Goal**: Implement configurable, deterministic multi-order simulation enabling users to specify Number of Orders ($N$), Items per Order ($K$), and Random Seed, generating $N$ physically executable orders with $N \times K$ item selections from actual store inventory, executing them sequentially (`Generate Simulation` $\to$ `Order 1` $\to$ `Pick` $\to$ `Complete/Deposit` $\to$ `Order 2` $\dots$ $\to$ `Simulation Complete`), maintaining individual order measurability and calculating true aggregate work-study metrics without fabrication.
- **Status**: **COMPLETED** (2026-11-02)
- **Delivered**:
  - `MultiOrderSimulation` entity (`Assets/Scripts/Domain/MultiOrderSimulation.cs`):
    - Manages ordered sequence of generated orders, active order progression, completed order tracking, and completion status.
    - Explicit distinction between Number of Orders ($N$) and Items Per Order ($K$).
  - Strict Input Validation & Duplicate Policy (`OrderGenerationConfig.cs`, `OrderGenerator.cs`):
    - `Validate()` checks: `orderCount >= 1`, `itemCount >= 1`, `store != null`, and `itemCount <= store.RegisteredSkuCount` under unique SKU policy (`DisallowDuplicates`).
    - Explicit validation error feedback without clamping or crashing.
  - Multi-Order Progression in `OrderManager` (`Assets/Scripts/Management/OrderManager.cs`):
    - `TryStartMultiOrderSimulation`: Dispatches the first order of the batch.
    - `CompletePacking`: Automatically advances to the next order in sequence upon packing completion, restocking shelf visuals and notifying listeners.
    - Events: `OnSimulationStarted`, `OnSimulationOrderChanged`, `OnSimulationCompleted`.
  - Minimal In-World Simulation Setup UI (`Assets/Scripts/UI/SimulationHUD.cs`):
    - Added "SIMULATION SETUP" interface to the Demo Panel with Number of Orders, Items per Order, Seed controls, and `[Generate Simulation]` action button.
    - Terminal order panel displays active simulation ID, current order progression, and total requested item selections.
  - Multi-Order Simulation Runner (`Assets/Scripts/Scenarios/OperationalCycleRunner.cs`, `MultiOrderSimulationResult.cs`):
    - `RunMultiOrderSimulation`: Runs multi-order batches headlessly with simulated picker motion, compiling individual order telemetry and mathematically reconciled aggregate work-study reports.
  - Automated Verification: **84 / 84 EditMode Tests Passing** (7 new tests in `MultiOrderSimulationTests.cs` covering Cases 1–6 + 77 existing tests, 0 failures, 0 warnings).



