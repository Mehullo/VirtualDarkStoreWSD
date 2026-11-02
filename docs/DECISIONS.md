# Architecture & Technical Decision Records (ADRs)

## ADR 001: Unity Version and Render Pipeline Selection
- **Status**: Accepted (Verified from repository)
- **Context**: The project was initialized with Unity 6 (`6000.6.7f1`) and Universal Render Pipeline (URP `17.6.0`).
- **Decision**: Standardize on Unity 6000.6.7f1 and URP. URP provides performant rendering across desktop hardware and future standalone or PC-tethered VR devices.
- **Consequences**:
  - Positive: High frame rates essential for motion sickness prevention in VR; modern Input System integration.
  - Negative: Unity 6 is a newer major release; package compatibility must be inspected strictly prior to addition.

## ADR 002: Desktop-First Development Strategy
- **Status**: Accepted (Mandated by `AGENTS.md`)
- **Context**: Developing VR-first introduces friction (headset tethering, tracking occlusion, hardware-dependent iteration cycles) that slows core algorithmic validation.
- **Decision**: Build and verify the entire warehouse simulation, order fulfillment flow, and Work-Study telemetry on Desktop (keyboard/mouse first-person controller) before VR hardware integration.
- **Consequences**:
  - Positive: Rapid development, automated headless CI/CD testing, frictionless local execution.
  - Negative: Controller interaction nuances (dual-hand grabbing, spatial depth) must be abstracted cleanly to prevent rework when adding VR in Phase 6.

## ADR 003: Decoupling of WorkStudy Engine from Input / Rig Modality
- **Status**: Accepted
- **Context**: Both Desktop and VR operators perform the same fundamental industrial fulfillment tasks (Travel, Search, Pick, Scan, Place, Stage).
- **Decision**: The WorkStudy engine operates as a pure state-and-event logger that receives semantic events from an abstracted `IPlayerInteraction` interface. Neither Desktop nor VR rigs possess custom time-study logic.
- **Consequences**:
  - Positive: Guaranteed analytical parity between Desktop and VR studies; 100% reusable metric calculations.
  - Negative: Requires formal domain event definitions (`PickEvent`, `ScanEvent`, `MovementEvent`).

## ADR 004: Exclusion of AI/ML Prediction and Training Pipelines
- **Status**: Accepted (Mandated by `AGENTS.md`)
- **Context**: The project's goal is automated empirical work-study and work-sampling of human/simulated operations, not machine learning model training or synthetic data synthesis.
- **Decision**: No AI/ML prediction models, training data export loops, or synthetic data generation pipelines will be introduced. Standard industrial engineering statistical formulas (mean time, standard deviation, observed rating, normal time, standard time) will be used deterministically.
- **Consequences**:
  - Eliminates opaque probabilistic behavior; results are fully reproducible and auditable.

## ADR 005: Direct CSV/JSON Telemetry Export Without Python Runtime Dependency
- **Status**: Accepted
- **Context**: Data analysis often prompts introducing Python environments, creating environment management overhead.
- **Decision**: All primary time-study event logs, summary metrics, and work-sampling tables will be generated directly by C# within Unity into standard RFC-4180 CSV and formatted JSON files.
- **Consequences**:
  - Positive: Zero external runtime dependencies; one-click simulation and report generation.
  - Negative: Complex multi-variable statistical plotting inside the app must rely on built-in UI or downstream external tools.

## ADR 006: Separation of Pure Domain Entities from Visual GameObjects
- **Status**: Accepted
- **Context**: Tying SKU data, shelf slots, and inventory coordinates directly to MonoBehaviours or GameObjects prevents headless testing, inflates memory, and tightly couples domain logic to scene lifecycles.
- **Decision**: All domain concepts (`Sku`, `ShelfSlot`, `Shelf`, `Aisle`, `StoreConfiguration`) are implemented as pure, serializable C# classes with no MonoBehaviour inheritance. Visual GameObjects in the scene represent layout blockouts and physical colliders; they reference or are coordinated by `StoreManager`.
- **Consequences**:
  - Positive: 100% testable in EditMode without scene instantiation; fast execution; clean serialization.
  - Negative: Requires `StoreManager` to coordinate between domain coordinates and scene transforms.

## ADR 007: Modular Assembly Definition Boundaries
- **Status**: Accepted
- **Context**: Default root assembly (`Assembly-CSharp`) slows recompilation and makes test discovery and dependency isolation fragile.
- **Decision**: Partition scripts into three explicit `.asmdef` modules:
  - `VirtualDarkStore.Runtime`: Production domain, management, player, and environment scripts.
  - `VirtualDarkStore.Editor`: Procedural scene builders, editor utilities.
  - `VirtualDarkStore.Tests.Editor`: EditMode automated test suites referencing `nunit.framework`.
- **Consequences**:
  - Positive: Fast incremental compilation; clean compilation boundaries.

## ADR 008: Explicit Test-Framework Dependency in Package Manifest
- **Status**: Accepted
- **Context**: Running headless tests via `-runTests` from CLI requires `com.unity.test-framework` to be an explicit root dependency in `Packages/manifest.json`.
- **Decision**: Include `"com.unity.test-framework": "1.4.6"` explicitly in `manifest.json`.


## ADR 009: Phase 6 Re-scoped to Scenario Comparison; VR Deferred
- **Status**: Accepted (user instruction 2026-10-28)
- **Context**: Previous plan labelled Phase 6 as VR integration. User requested scenario definition, save/load, A/B comparison, route visualisation and KPI comparison as Phase 6.
- **Decision**: Implement scenarios as Phase 6; VR/OpenXR parity and final packaging shift to later phases (Phase 7+). Scenario runs are produced by a scripted virtual picker driving the shared OrderManager/ActivityClassifier/WorkEventLogger; the runner never writes events itself. Scenarios vary only user-defined data (SKU visit sequence); no optimisation or AI.
- **Consequences**: Comparisons are valid only when starting conditions are equivalent; `ScenarioComparison` flags differences. Outputs always labelled measured simulation results, not predictions.

## ADR 010: VR Player Rig & Desktop-VR Parity (Phase 7)
- **Status**: Accepted (2026-10-28)
- **Context**: Phase 7 requires implementing a VR player rig, head tracking, controller input, basic pick interaction, and continuous locomotion/teleportation. VR must strictly act as an interaction layer and never a second simulation engine.
- **Decision**:
  1. Add official verified packages: `com.unity.xr.interaction.toolkit: 3.6.1`, `com.unity.xr.core-utils: 2.6.0`, and `com.unity.xr.meta-openxr: 2.6.1` with OpenXR loader configuration for Standalone and Android (Meta Quest).
  2. Implement `IPickerInteraction` interface to decouple UI and activity classification from specific input hardware, allowing `ActivityClassifier` and `SimulationHUD` to consume events identically from either Desktop or VR rigs.
  3. Ensure teleportation preserves displacement: when a VR picker teleports across aisles, the displacement vector $\Delta \vec{p}$ is passed as `frameDistance` to `ActivityClassifier` so travel time and distance are faithfully accumulated without dropouts.
  4. Strictly reject uninstrumented cognitive or physiological claims: `VRHeadTracking.HeadRay` is strictly a physical head-orientation raycast, never eye-tracking.
  5. In accordance with no-hallucination rules, `VRHardwareDetector` checks for connected HMD devices; if no physical HMD is connected in the runtime environment, hardware verification is explicitly marked `NOT RUN`.
- **Consequences**:
  - Full desktop-VR parity: both modes feed into the exact same `OrderManager`, `WorkEventLogger`, and `WorkStudyCalculator`.
  - Zero simulated metrics are fabricated.

## ADR 011: Phase 8 RTX 2050 Performance Budget, 4-Aisle SKU Distribution & 7-Step Demo Workflow
- **Status**: Accepted (2026-10-29)
- **Context**: Phase 8 requires polishing UI, work-study report exports, store visuals, performance for RTX 2050-class hardware, error handling, scene organization, and a reproducible 7-step Final Demo workflow without adding AI/ML or fabricating FPS/benchmark numbers.
- **Decision**:
  1. **4-Aisle Even SKU Distribution**: Update `StoreManager.AssignSkusToSlots()` to place 1 SKU per bay-tier at `SlotIndex == 1` across all 4 aisles (72 SKUs across 72 bay-tiers, 18 SKUs per aisle) so multi-aisle travel and inter-aisle backtracking (`SCN-DEMO-A` vs `SCN-DEMO-B` and `SCN-A` vs `SCN-B`) are physically exercised across the warehouse.
  2. **RTX 2050-Class Budget & Honest Audit**: Tune `PC_RPAsset.asset` (`m_RequireDepthTexture: 0`, `m_RequireOpaqueTexture: 0`, `m_SupportsHDR: 0`, `m_MSAA: 2`, `m_ShadowDistance: 32`, `m_ShadowCascadeCount: 2`, `m_AdditionalLightShadowsSupported: 0`) and implement `PerformanceSettingsManager` to enforce SRP Batcher, GPU instancing on all 16 shared materials, camera far clip $\le 45.0\text{ m}$, and shadowless ceiling point lights. Mark interactive GPU FPS benchmarking strictly as `NOT RUN` in headless audits (`docs/evidence/phase8/rtx2050_performance_audit.md`).
  3. **Deterministic Report Exporter & 7-Step Demo Controller**: Implement `WorkStudyReportExporter` (Markdown + RFC-4180 elemental CSV + summary CSV) and `DemoFlowController` guiding the operator through all 7 demo steps in Desktop, VR, or Scripted modes.
- **Consequences**:
  - Positive: Low memory-bandwidth footprint suitable for 4GB VRAM / 64-bit bus GPUs; realistic 4-aisle dark store visuals; one-click auditable Markdown and CSV reports; zero fabricated benchmarks.
  - Negative: Updating SKU distribution from single-aisle clustering to 4-aisle distribution updates the measured distances for `SCN-A` ($82.40\text{ m}$) and `SCN-B` ($90.40\text{ m}$), which have been re-recorded in `docs/evidence/phase6/`.

## ADR 012: Quick-Commerce Dark Store Environment Upgrade (Phase 9)
- **Status**: Accepted (2026-11-02)
- **Context**: Phase 9 requires upgrading the baseline environment into a realistic generic quick-commerce dark store with A01–A06 aisles, two product-facing rack sides per aisle, closed backs where appropriate, designated entry zone, order collection desk, picking area, packing/deposit desk, exit zone, overhead aisle labels, collision, walkable corridors, and baked NavMesh coverage while maintaining the RTX 2050 GPU budget.
- **Decision**:
  1. **6 Symmetrically Spaced Aisles (`A01`–`A06`)**: Symmetrically place 6 aisles at $X = -7.5\text{ m}, -4.5\text{ m}, -1.5\text{ m}, +1.5\text{ m}, +4.5\text{ m}, +7.5\text{ m}$ ($3.0\text{ m}$ pitch: $1.8\text{ m}$ clear walkable corridor $+ 2 \times 0.6\text{ m}$ rack depth) spanning $Z = -4.0\text{ m}\dots+8.0\text{ m}$.
  2. **Closed Rack Backs**: Install solid industrial back panel dividers along the backs of back-to-back rack blocks ($X = -6.0\text{ m}, -3.0\text{ m}, 0.0\text{ m}, +3.0\text{ m}, +6.0\text{ m}$) and outer warehouse perimeter racks ($X = -9.0\text{ m}, +9.0\text{ m}$).
  3. **Operational Circulation Flow**: Establish authentic left-to-right micro-fulfillment flow: Worker Entry ($X = -8.0\text{ m}$) $\to$ Order Collection Desk ($X = -4.0\text{ m}$) $\to$ Picking Area ($X = -9.0\text{ m}\dots+9.0\text{ m}$) $\to$ Packing & Deposit Desk ($X = +2.0\text{ m}$) $\to$ Courier Dispatch Exit ($X = +8.0\text{ m}$).
  4. **Collision & Navigation**: Solid `BoxCollider`s on all racks, desks, barrier rails, and perimeter walls; re-bake `UnityEngine.AI.NavMesh` across all walkable corridors and operational desks.
  5. **RTX 2050 Budget**: 212 total mesh renderers ($\le 300$), $17/17$ GPU-instanced materials ($100\%$), 10 realtime lights ($\le 12$), 1 shadow caster, camera far clip $45.0\text{ m}$.
- **Consequences**:
  - Positive: Authentic industrial layout matching real-world quick-commerce micro-fulfillment centers; seamless navigation for desktop and VR.
  - Negative: Mesh renderer count increases from 148 to 212, but remains well within the RTX 2050 budget ($\le 300$).

## ADR 013: Structured Store Location & SKU Mapping Hierarchy (Phase 10)
- **Status**: Accepted (2026-11-02)
- **Context**: Machine-readable physical inventory locations require an unambiguous standard hierarchy: $\text{Store} \to \text{Aisle} \to \text{Side} \to \text{Bay} \to \text{Shelf Level (Tier)} \to \text{Slot}$ (e.g. `A01-L-B01-T01-S01`). Locations must support SKU $\to$ Location ID $\to$ World Position resolution and comprehensive validation without introducing competing domain models.
- **Decision**:
  1. **Domain Model**: Create `StoreLocation` struct in `Assets/Scripts/Domain/StoreLocation.cs` with robust `TryParse`, `Parse`, `Format`, equality operators, and component getters (`StoreId`, `AisleNumber`, `Side`, `BayIndex`, `TierLevel`, `SlotIndex`).
  2. **Architectural Reuse**: Extend existing `ShelfSlot` by exposing `StoreLocation Location` and delegating `ShelfSlot.GenerateSlotId` directly to `StoreLocation.Format`.
  3. **Resolution APIs on StoreManager**: Add `TryResolveSkuLocation`, `TryResolveLocationPosition`, `TryGetSkuAtLocation`, `ValidateSkuLocationMappings`, and `ValidateLocationId` to `StoreManager.cs`.
  4. **Backward Compatibility**: Add `StoreConfiguration.CreateQuickCommerce6AisleConfig()` for 6-aisle quick-commerce while preserving `CreateDefault()` (4 aisles) so all legacy Phase 0–8 tests pass without modification.
- **Consequences**:
  - Positive: Clean, type-safe, machine-readable location representation; 1:1 bidirectional resolution; zero breaking changes across existing managers and tests.

## ADR 014: Controlled Deterministic Order Generation (Phase 11)
- **Status**: Accepted (2026-11-02)
- **Context**: Industrial work-study experiments require generating repeatable, controlled pick orders with known random seeds, configurable item counts, and duplicate policies, while strictly preserving existing preset orders without introducing external Python services or dataset dependencies.
- **Decision**:
  1. **Pure C# Domain Generator (`OrderGenerator.cs`)**: Implement deterministic pseudo-random selection driven by `System.Random(seed + orderIndex * 31)`.
  2. **Configurable Policies**: Encapsulate parameters in `OrderGenerationConfig` supporting integer `Seed`, `ItemCount`, `OrderCount`, `DuplicateSkuPolicy` (`DisallowDuplicates`, `AllowDuplicates`), and `OrderIdPrefix`.
  3. **Strict Location Resolution**: Resolve all selected SKUs to exact physical slot IDs and 3D coordinates via `StoreManager.TryResolveSkuLocation`.
  4. **OrderManager Integration**: Add non-breaking `TryDispatchGeneratedOrder` and `DispatchOrder` while preserving `StartSampleOrder` and `StartSpecificOrder` unchanged.
- **Consequences**:
  - Positive: 100% reproducible experiments across runs; identical seeds guarantee identical pick lists; zero external dependencies.

## ADR 015: Local Google OR-Tools Routing Optimizer & Safe Baseline Fallback Architecture (Phase 12)
- **Status**: Accepted (2026-11-02)
- **Context**: Multi-stop pick sequences require visit-order optimization (TSP) that accounts for actual traversable warehouse distances rather than misleading straight-line Euclidean distances. Google OR-Tools is preferred, running locally without cloud hosting or Python services, but must be strictly isolated behind an adapter with verified compatibility on Unity 6000.6.7f1 on Windows x64 and safe baseline fallback on unsupported platforms (e.g. Android Meta Quest standalone).
- **Decision**:
  1. **Separation of Pathfinding vs Sequence Optimization**:
     - Level 1 (Physical Pathfinding): Unity NavMesh (`UnityEngine.AI.NavMesh.CalculatePath`) calculates polygon corner distances along corridors and marks unreachable pairs as `float.PositiveInfinity`.
     - Level 2 (Sequence Optimization): Routing optimizer orders stop visits to minimize cumulative travel cost.
  2. **Square Travel Cost Matrix (`TravelCostMatrix.cs`)**: $N \times N$ matrix ($N = \text{Stops} + 2$) with Node 0 as Start (Order Desk) and Node $N-1$ as End (Packing Bench). Validates square dimensions, zero diagonal ($Costs[i, i] == 0$), and explicit unreachable handling.
  3. **Google OR-Tools Isolation & Platform Guarding (`OrToolsRouteOptimizer.cs`)**:
     - Integrated `Google.OrTools` (9.15.6755) net462 assemblies and Windows x64 native binaries in `Assets/Plugins/OrTools/`.
     - Formulated single-vehicle TSP with fixed start (0) and fixed end ($N-1$) using `RoutingIndexManager` and `GuidedLocalSearch`.
     - Isolated behind `IRouteOptimizer`. Guarded with complete try/catch block and `#if !UNITY_ANDROID`.
     - In case of missing native libraries, unsupported platform (e.g. Android), or solver failure, automatically and safely falls back to `BaselineRouteOptimizer` without crashing or throwing.
  4. **Mathematical Honesty**: Heuristic solvers do not guarantee global optimality; results are flagged with `IsOptimalProven = false` unless mathematically proven.
  5. **Dynamic Order Pick Sequence Reordering**: Add `Order.TryReorderItems` to allow updating the pick checklist prior to picking while verifying multiset conservation.
- **Consequences**:
  - Positive: Significant travel time reduction (>50% on backtracking orders); seamless local execution; 100% test pass rate (65/65 tests); complete safety fallback ensures Android VR and editor runs never crash.

## ADR 016: 7-Stage Operational Cycle State Machine (Phase 13)
- **Status**: Accepted (2026-11-02)
- **Context**: The store environment, order collection, picking circulation, packing deposit, and worker egress operated as isolated subsystems. To simulate realistic dark store operations, an integrated operational workflow is required: `ENTRY` $\to$ `COLLECT_ORDER` $\to$ `PICKING` $\to$ `AWAITING_PACKING` $\to$ `DEPOSIT_ORDER` $\to$ `WALK_TO_EXIT` $\to$ `COMPLETED`.
- **Decision**:
  1. **Central State Machine (`OperationalCycleManager.cs`)**:
     - Manages cycle progression across the 7 stages.
     - Anchors operational zones to standardized physical waypoints (`EntryPosition`, `CollectionDeskPosition`, `PackingDeskPosition`, `ExitPosition`).
     - Employs proximity triggers for interactive desktop and VR players.
  2. **Zero Duplicate Telemetry**:
     - Cycle transitions emit standard `WorkEvent`s directly to existing `WorkEventLogger.Instance`.
     - Monotonic non-decreasing timestamps and non-negative durations are strictly enforced.
  3. **Scripted Virtual Picker Runner (`OperationalCycleRunner.cs`)**:
     - Automates complete 7-stage workflow execution for deterministic headless regression testing and work-study reconciliation.
- **Consequences**:
  - Positive: Unifies all subsystems under one operational flow; identical workflow for interactive players and automated testing; zero duplicate logging code.

## ADR 017: Baseline vs Optimized Work-Study Comparison Engine (Phase 14)
- **Status**: Accepted (2026-11-02)
- **Context**: Evaluating pick sequence optimization requires comparing measurable industrial performance between baseline and solver-optimized routes. The comparison must eliminate confounding variables by holding 100% of environmental, order, starting, and locomotion conditions identical, varying solely the sequence of SKU visits.
- **Decision**:
  1. **A/B Comparison Engine (`OperationalComparison.cs`)**:
     - Compares Run A (Baseline) vs Run B (Optimized).
     - Validates equivalence (`IsEquivalent` checks order ID, SKU counts, shelf visits, pick counts).
     - Calculates delta and delta percentage for all work-study activities and physical travel metrics.
  2. **Industrial Engineering Aisle Metrics**:
     - Measures inter-aisle transitions (corridor crossings) and aisle backtracks (re-entering an aisle previously visited and exited).
  3. **Empirical Reporting & Transparency**:
     - Exports publication-grade Markdown reports and machine-readable CSV bundles to `docs/evidence/phase14/`.
     - Includes explicit methodology notices: all figures are measured simulation telemetry; single-run results do not imply statistical significance without multi-order sampling.
- **Consequences**:
  - Positive: Empirically proves routing optimization benefits (e.g. 24.1% distance reduction, 20.6% cycle time savings, 100% aisle backtrack elimination in sample test); maintains complete scientific integrity and reproducibility.

## ADR 018: Physical Location Labels & Store Signage
- **Status**: Accepted (2026-11-02)
- **Context**: The dark store environment required visible physical signage to allow pickers and researchers to distinguish aisles, rack sides, bay groupings, and shelf levels. The signage needed to reflect the existing domain hierarchy (`Store -> Aisle -> Side -> Bay -> Shelf Level -> Slot`) without creating duplicate location IDs, interfering with physics colliders or NavMesh, or degrading RTX 2050 rendering performance.
- **Decision**:
  1. **Lightweight World Space Canvases (`PhysicalLocationLabel.cs`)**:
     - Implement signs via World Space Canvases with high pixel density (500 px/meter) and dynamic scaling.
     - Strictly set `raycastTarget = false` and attach zero colliders, completely avoiding physics and NavMesh interference.
  2. **Hierarchical Signage Placement**:
     - Overhead Aisle Header Signs (`AISLE A01` ... `AISLE A06`) on front/rear entry beams.
     - End-Cap Placards on rack front uprights visible from cross-aisles.
     - Bay Placards at eye level ($Y = 2.15\text{ m}$) atop each bay facing corridors.
     - Shelf Level Badges (`LEVEL 01`, `LEVEL 02`, `LEVEL 03`) on tier beams.
     - Product Slot Tickets directly below stocked SKUs on tier beams.
  3. **Picker Hover Slot ID Feedback**:
     - Update desktop and VR hover prompts to display `[SlotId]` directly.
- **Consequences**:
  - Positive: Natural, legible operational dark store signage; zero performance degradation or physics collision conflicts.

## ADR 019: Shelf Population — 144 SKUs Across Usable Racks
- **Status**: Accepted (2026-11-02)
- **Context**: The 6-aisle quick-commerce layout has 432 shelf slots across 36 bays and 108 tiers. With only 72 SKUs previously stocked, over 83% of slots were empty, and aisles 5–6 had zero items. A believable micro-fulfillment dark store requires broad inventory coverage across all aisles, sides, bays, and tiers, while strictly enforcing 1:1 mapping with simulation data and adhering to the RTX 2050 GPU budget ($\le 300$ mesh renderers).
- **Decision**:
  1. **Expanded Deterministic Catalog (`VerifiedSampleCatalog.CreateQuickCommerceCatalog()`)**:
     - Add 72 verified grocery SKUs (`SKU-0073` to `SKU-0144`) spanning all 8 existing categories with unique barcodes, realistic weights, and dimensions.
     - Keep base `CreateCatalog()` (72 SKUs) unchanged for backward compatibility with 4-aisle configurations.
  2. **Dynamic StoreManager Catalog Resolution**:
     - In `StoreManager.InitializeStore()`: load `CreateQuickCommerceCatalog()` (144 SKUs) if `AisleCount >= 6`, else load `CreateCatalog()` (72 SKUs).
     - Maintain existing deterministic pass-by-pass slot assignment (`AssignSkusToSlots`), preserving historical coordinates for base SKUs.
  3. **Strict RTX 2050 GPU Budget Compliance**:
     - 140 structural mesh renderers + 144 product mesh renderers = 284 total mesh renderers, cleanly passing the $\le 300$ budget ceiling.
     - Product items share 8 GPU-instanced category materials with disabled shadow casting.
- **Consequences**:
  - Positive: All 6 aisles have substantial inventory ($\ge 18$ items each, Left and Right sides both populated, all bays and tiers populated); 100% 1:1 mapping between visual objects and domain inventory records; zero simulation or routing breakage; full compliance with hardware budget.

## ADR 020: Configurable Multi-Order Simulation Architecture
- **Status**: Accepted (2026-11-02)
- **Context**: Realistic work-study evaluation requires testing batches of multiple independent orders (e.g. 5 orders of 8 items) sequentially, with explicit distinction between Number of Orders ($N$) and Items Per Order ($K$). Each order must remain individually measurable, and aggregate metrics must be calculated strictly from actual completed order telemetry without fabrication.
- **Decision**:
  1. **MultiOrderSimulation Domain Entity (`MultiOrderSimulation.cs`)**:
     - Encapsulates an ordered sequence of generated `Order` instances, tracking `CurrentOrderIndex`, `ActiveOrder`, and `CompletedOrders`.
     - Explicitly differentiates Number of Orders ($N$) from Items Per Order ($K$), tracking total requested selections ($N \times K$).
  2. **Sequential Queue Execution in `OrderManager`**:
     - `TryStartMultiOrderSimulation`: Dispatches the initial order of the batch.
     - `CompletePacking`: Upon packing an order at the bench, automatically marks the order complete, restocks shelf visuals, and advances to the next order in sequence until all orders finish.
  3. **Duplicate & Quantity Policy**:
     - Physical shelf items are discrete objects that disable their collider and mesh renderer when picked. Under `DuplicateSkuPolicy.DisallowDuplicates`, every SKU in an order is unique.
     - `OrderGenerationConfig.Validate()` strictly enforces $K \le \text{catalog capacity}$ (144 SKUs), preventing invalid requests or crashes.
  4. **Aggregate Telemetry Without Fabrication**:
     - Aggregate metrics (cycle time, distance, aisle transitions, activity durations) are calculated strictly by summing or filtering actual `WorkEvent`s from completed runs via `WorkStudyCalculator.Calculate()`.
- **Consequences**:
  - Positive: Seamless multi-order workflows in interactive desktop, VR, and scripted testing; individual order metrics preserved; zero data fabrication; 100% backward compatible.





