# Architecture Specification: Virtual Dark Store Platform

## 1. Architectural Philosophy
The platform strictly decouples **Simulation & Work-Study Logic** from the **Interaction & Presentation Layer**.

```
+-------------------------------------------------------------+
|                Presentation / Input Layer                   |
|   +--------------------------+  +-----------------------+   |
|   | Desktop Controller       |  | VR Rig / XR IT        |   |
|   | (Keyboard, Mouse Raycast)|  | (Deferred to Phase 5) |   |
|   +--------------------------+  +-----------------------+   |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                      Interaction API                        |
|  (IPlayerController, IGrabbable, IScanner, IStationTrigger) |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                      Simulation Core                        |
|   - Order / PickList Engine                                 |
|   - Inventory & SKU Database                                |
|   - Warehouse Topology (Aisles, Shelves, Bins)              |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                      Work-Study Core                        |
|   - Elemental State Machine (Travel, Search, Pick, etc.)    |
|   - Time-Study Timer & Stopwatches                          |
|   - Work-Sampling Engine (Random/Periodic Sampling)         |
|   - Telemetry Recorder & Exporter (CSV, JSON)               |
+-------------------------------------------------------------+
```

## 2. Layer Responsibilities

### 2.1 Work-Study Core (`Assets/Scripts/WorkStudy/`)
- **Activity State Machine & Classifier (`ActivityClassifier.cs`)**:
  Classifies operator actions across 8 standardized industrial activities using strictly measurable simulation telemetry:
  - `Walking`: Picker moving ($v \ge 0.15\text{ m/s}$) with no active target destination.
  - `Waiting`: Picker stationary ($v < 0.15\text{ m/s}$) while unengaged with shelf searching, picking, or packing.
  - `MovingBetweenLocations`: Picker moving ($v \ge 0.15\text{ m/s}$) towards target slot or packing station outside the approach zone ($d > 3.0\text{ m}$).
  - `Approaching`: Picker moving ($v \ge 0.15\text{ m/s}$) within the immediate approach zone of the target rack or bench ($d \le 3.0\text{ m}$).
  - `Searching`: Picker stationary ($v < 0.15\text{ m/s}$) within $3.0\text{ m}$ of the target shelf bay, with the center camera view ray intersecting shelf rack/tier geometry, prior to item acquisition. (Zero uninstrumented eye-gaze or cognitive load claims).
  - `Picking`: Discrete physical acquisition event validated against target SKU (`OrderManager.AttemptPick` -> `PickResult.Success`).
  - `Packing`: Picker positioned at packing station ($d \le 3.0\text{ m}$) with order in `AwaitingPacking` status, engaging packing bench.
  - `OrderCompleted`: Terminal dispatch event upon order finalization (`OrderManager.CompletePacking`).
- **Telemetry Event Schema (`WorkEvent.cs`)**:
  Immutable measurement record containing only physically measurable fields:
  - `Id`: Sequential string identifier (`EVT-0001`).
  - `Activity`: Standard `ActivityType` enum.
  - `StartTime`: Simulation start timestamp in seconds ($t_{start} \ge 0$).
  - `EndTime`: Simulation end timestamp in seconds ($t_{end} \ge t_{start}$).
  - `Duration`: Elapsed seconds ($t_{end} - t_{start} \ge 0$).
  - `Position`: World Vector3 coordinates.
  - `LocationName`: Physical location address (`SlotId` e.g. `A01-L-B01-T01-S01`, `PACK-01`, `GENERAL_FLOOR`).
  - `RelatedSku`: SKU ID (`SKU-0001` or empty).
  - `RelatedOrder`: Order ID (`ORD-0001` or empty).
  - `Distance`: Cumulative horizontal distance traveled during event in meters ($\ge 0$).
- **Monotonic Event Recorder (`WorkEventLogger.cs`)**:
  In-memory event repository enforcing monotonic non-decreasing start timestamps ($t_{start, i} \le t_{start, i+1}$) and non-negative durations ($\Delta t \ge 0$) with runtime integrity verification (`VerifyIntegrity`).
- **Work-Study Calculation Engine (`WorkStudyCalculator.cs`)**:
  Pure computational engine deriving aggregated time-study metrics directly from actual event logs:
  - Total task time: $T_{total} = \sum e_i.Duration$.
  - Category durations: Walking, Searching, Picking, Waiting, Packing, OrderCompleted.
  - Category percentages: $P_c = \frac{T_c}{T_{total}} \times 100\%$.
  - Travel subcategory breakdown: General walking, macro transit, rack bay approach.
  - Operational metrics: cumulative distance ($m$), successful picks count, distinct shelf bay visits.
  - Configurable interval-based work-sampling: discrete observation samples at $t_k = t_0 + k \cdot \Delta t$ mapped to instantaneous active events ($p_c = \frac{n_c}{N} \times 100\%$).
- **Results Data Model (`WorkStudyResults.cs`)**:
  Encapsulates all aggregated time-study and work-sampling figures with built-in mathematical reconciliation verification (`VerifyReconciliation`), ensuring $\sum T_c = T_{total}$ and $\sum P_c = 100.0\%$.
- **Results UI (`WorkStudyResultsUI.cs`)**:
  Screen-space IMGUI modal interface toggleable via `[Tab]` or shown upon order completion displaying metric cards, Summary Breakdown tab (visual percentage bars), Chronological Event Log tab, configurable sampling intervals ($0.5\text{ s}, 1.0\text{ s}, 2.0\text{ s}, 5.0\text{ s}$), one-click Markdown/CSV report export (`[F2]`), and reconciliation status badges.
- **Work-Study Report Exporter (`WorkStudyReportExporter.cs`)**:
  Exports deterministic Markdown reports (`.md`), RFC-4180 elemental event logs (`_events.csv`), and summary KPI tables (`_summary.csv`) directly from recorded `WorkEventLogger` and `WorkStudyResults` data without external Python dependencies.

### 2.2 Domain Topology & Inventory Core (`Assets/Scripts/Domain/` & `Assets/Scripts/Management/`)
- **Strict Visual-Domain Decoupling**: Domain models are pure C# data classes completely independent of Unity GameObjects, transforms, or renderers:
  - `Sku`: Represents product master data (`SkuId`, `Name`, `Barcode`, `Category`, `WeightKg`, `DimensionsMeters`).
  - `ShelfSlot`: Represents physical storage slot address (`SlotId`: `A{aisle}-L/R-B{bay}-T{tier}-S{slot}`, `WorldPosition`, `AssignedSkuId`).
  - `Shelf`: Represents a shelf bay (`ShelfId`) with multiple vertical tiers.
  - `Aisle`: Represents picking aisle (`AisleId`) holding Left and Right shelves and slot collections.
  - `StoreConfiguration`: Configures store parameters (aisle count, bays per side, tiers per bay, slots per tier).
- **StoreManager (`VirtualDarkStore.Management`)**:
  - Central manager holding runtime topology and bidirectional lookup dictionaries (`skuToSlotMap`, `slotToSkuMap`, `skuLookup`, `slotLookup`).
  - Enforces 1:1 deterministic SKU assignment without location collisions.
  - Automated topology integrity verification (`ValidateIntegrity`).
- **Verified Catalog (`VerifiedSampleCatalog`)**:
  - Deterministic catalog of 72 verified grocery SKUs across 8 departments.

### 2.3 Order & Fulfillment Simulation Engine (`Assets/Scripts/Domain/`, `Assets/Scripts/Management/`, `Assets/Scripts/Interaction/`, `Assets/Scripts/UI/`)
- **Order Domain Models**:
  - `Order`: Encapsulates order identity, line items, sequential target progression, and strict pick validation logic (`ValidateAndPick`). Ensures incorrect or missing items strictly fail validation and never silently advance or count as fulfilled.
  - `OrderItem`: Represents an order line item with SKU metadata, target slot address, world position, required quantity, and picked quantity.
- **OrderManager**:
  - Central singleton manager handling automated/manual order dispatch (`StartSampleOrder`, `StartSpecificOrder`), pick attempt routing (`AttemptPick`), and packing finalization (`CompletePacking`).
  - Broadcasts decoupled lifecycle events (`OnOrderStarted`, `OnItemPicked`, `OnPickFailed`, `OnOrderCompleted`).
- **Physical Shelf Products & Visuals**:
  - `PickableItemVisual`: Placed on shelves at slot coordinates with BoxCollider, category material shading, and pick disable/reset state.
- **Picker Raycast Interaction**:
  - `PickerInteraction`: Camera-center raycasting (`Physics.RaycastAll` sorted by distance, 2.8m reach) to select hovered shelf products and packing benches; executes picks and packing finalization.
- **Simulation HUD**:
  - `SimulationHUD`: Screen-space IMGUI terminal displaying active order status, current target SKU, shelf slot address, live distance in meters, central crosshair reticle, dynamic interaction prompts, and color-coded feedback banners (green pick, red mismatch alert, gold completion).

### 2.4 Interaction Layer (`Assets/Scripts/Player/`, `Assets/Scripts/VR/` & `Assets/Scripts/Environment/`)
- **Shared Interaction Abstraction (`IPickerInteraction.cs`)**:
  - Exposes `HoveredItem`, `HoveredPackingStation`, `CurrentPrompt`, and `ReachDistance`.
  - Unifies Desktop and VR interaction pipelines so `ActivityClassifier` and `SimulationHUD` process both identically.
- **Desktop First-Person Rig (`Assets/Scripts/Player/`)**:
  - `DesktopPickerController`: CharacterController movement (WASD), mouse pitch/yaw, gravity.
  - `PickerInteraction`: Center-screen reticle raycast interaction with shelf items and packing bench.
- **VR Player Rig (`Assets/Scripts/VR/`)**:
  - `VRPlayerRig`: Master rig managing `XROrigin`, floor-level tracking origin, and child components.
  - `VRHeadTracking`: Physical HMD tracking and head-orientation forward raycast (excludes uninstrumented physiological or eye-gaze claims).
  - `VRControllerInput`: Spatial tracking and button inputs (trigger, grip, primary button, thumbstick) for left and right hands.
  - `VRPickerInteraction`: Raycast and direct touch interaction validating picks against `OrderManager` and completing packing at staging benches.
  - `VRLocomotionController`: Continuous thumbstick locomotion and discrete teleportation with exact displacement accumulation ($\Delta p$) to ensure travel metrics are preserved.
  - `VRHardwareDetector`: Inspects connected HMD devices; marks hardware verification as `NOT RUN` when no physical headset is attached.

## 3. Telemetry Data Schemas

### 3.1 Elemental Time-Study Record (`time_study_events.csv`)
| Field | Type | Description |
|---|---|---|
| `SessionId` | string | Unique GUID for simulation run |
| `OrderId` | string | Order identifier |
| `CycleNumber` | int | 1-based pick cycle index |
| `Element` | string | Elemental category (TRAVEL, SEARCH, PICK, SCAN, PLACE, STAGE) |
| `StartTimeUtc` | string | ISO-8601 UTC timestamp |
| `EndTimeUtc` | string | ISO-8601 UTC timestamp |
| `DurationSeconds` | float | Elapsed real time in seconds |
| `DistanceMeters` | float | Traveled distance during element |
| `TargetSku` | string | Target item SKU |
| `TargetLocation` | string | Bin coordinate string (e.g., `A02-B03-S1`) |

### 3.2 Work-Sampling Record (`work_sampling_observations.csv`)
| Field | Type | Description |
|---|---|---|
| `SessionId` | string | Unique GUID |
| `ObservationId` | int | Sequential sample number |
| `TimestampUtc` | string | ISO-8601 UTC timestamp |
| `State` | string | Instantaneous active state |
| `PickerPosition` | string | Vector3 `(x, y, z)` |

## 4. Architectural Invariants
1. **No External Python Dependency**: Analysis and export operate within Unity C# standard file I/O.
2. **No Machine Learning/AI Inferences**: No neural networks or heuristic black boxes are used for time study analysis; standard industrial engineering formulas (mean, standard deviation, rating, normal time, standard time) are applied deterministically.
3. **Headless / Batch Testable**: WorkStudy core logic must be testable via Unity Test Framework without requiring physical VR hardware.

## 5. Scenario Layer (Phase 6, `Assets/Scripts/Scenarios/`)
`ScenarioDefinition` (JSON via `ScenarioStorage`) -> `ScenarioRunner` drives a scripted virtual picker through the shared `OrderManager` + `ActivityClassifier` + `WorkEventLogger` -> `ScenarioRunResult` (recorded events + route; KPIs recomputed by `WorkStudyCalculator`) -> `ScenarioComparison` (equivalence check, KPI rows, B-A deltas) -> `RouteMapRenderer` / `ScenarioRouteVisualizer` / `ScenarioComparisonUI`. No prediction or optimisation; routing is a fixed straight-line rule via the front cross-aisle.

## 6. Phase 8 Subsystems: Demo Workflow, Error Handling & Performance Budget
- **7-Step Final Demo Controller (`DemoFlowController.cs`)**:
  Coordinates the end-to-end 7-step demonstration (`1. SelectScenario` -> `2. SelectOrder` -> `3. StartMode` -> `4. PickProducts` -> `5. CompleteOrder` -> `6. ViewWorkStudyResults` -> `7. CompareAnotherScenario`) across Desktop, VR, and Scripted auto-run modes. Manages the in-world 3D `TargetSlotBeacon` above the active target shelf slot or packing bench and records live route trajectories.
- **Simulation Error Handler (`SimulationErrorHandler.cs`)**:
  Centralized non-crashing diagnostic and alert manager (`ReportError`, `ReportWarning`) ensuring invalid scenario/order selections or wrong-SKU pick attempts never cause unhandled exceptions and surface clear operator feedback in `SimulationHUD`.
- **RTX 2050-Class Performance Manager (`PerformanceSettingsManager.cs` & `Assets/Settings/PC_RPAsset.asset`)**:
  Enforces and audits rendering budgets tailored for RTX 2050-class hardware (4GB VRAM, 64-bit bus): SRP Batcher enabled, GPU instancing on $17/17$ shared materials, $212$ total mesh renderers ($\le 300$), camera far clip plane $\le 45.0\text{ m}$, URP shadow distance $32.0\text{ m}$, 2 shadow cascades, $2\times$ MSAA, 1 shadow-casting directional light + 9 shadowless point lights (total 10 lights $\le 12$), and camera post-processing disabled. Generates `docs/evidence/phase8/rtx2050_performance_audit.md` with interactive GPU FPS benchmarking marked `NOT RUN` when audited headlessly.

## 7. Quick-Commerce Dark Store Environment Architecture (Phase 9)
- **Topological Layout (6 Aisles `A01`–`A06`)**:
  - Symmetrically spaced with $3.0\text{ m}$ pitch ($1.8\text{ m}$ corridor width $+ 2 \times 0.6\text{ m}$ rack depth) positioned at $X = -7.5\text{ m}, -4.5\text{ m}, -1.5\text{ m}, +1.5\text{ m}, +4.5\text{ m}, +7.5\text{ m}$ across $Z = -4.0\text{ m}\dots+8.0\text{ m}$.
  - Two product-facing rack sides per aisle with $12.0\text{ m}$ length, $2.4\text{ m}$ height, and 3 bays per side ($4.0\text{ m}$ per bay).
  - Closed backs: Solid backing divider panels placed between back-to-back rack blocks ($X = -6.0\text{ m}, -3.0\text{ m}, 0.0\text{ m}, +3.0\text{ m}, +6.0\text{ m}$) and outer warehouse perimeter racks ($X = -9.0\text{ m}, +9.0\text{ m}$).
- **Operational Zones & Circulation Flow**:
  - Authentic quick-commerce left-to-right circulation:
    1. **Worker Entry Zone** ($X = -8.0\text{ m}, Z = -10.5\text{ m}$): Archway, overhead "ENTRY / RECEIVING" signboard, walkway floor pad, turnstile barrier rails with solid `BoxCollider`.
    2. **Order Collection Desk** ($X = -4.0\text{ m}, Z = -10.0\text{ m}$): Desk table with `BoxCollider`, terminal screen, pick cart tote bins, overhead signage.
    3. **Picking Area** ($X = -9.0\text{ m}\dots+9.0\text{ m}, Z = -4.0\text{ m}\dots+8.0\text{ m}$): 6 aisles, $1.8\text{ m}$ wide corridors, front cross-aisle ($3.0\text{ m}$ clear width), rear cross-aisle ($3.5\text{ m}$ clear width), safety boundary lines.
    4. **Packing & Deposit Desk** ($X = +2.0\text{ m}, Z = -10.0\text{ m}$): Packing bench with `BoxCollider`, terminal screen, dispatch tote bins, floor staging pad, and `PackingStation` component ("PACK-01").
    5. **Exit / Dispatch Handover Zone** ($X = +8.0\text{ m}, Z = -10.5\text{ m}$): Archway, overhead "EXIT / DISPATCH HANDOVER" signboard, egress walkway pad, barrier rails with solid `BoxCollider`.
- **Aisle Labels & Orientation**:
  - High-contrast overhead signboards at front entrances (`Aisle_01_HeaderSign`–`Aisle_06_HeaderSign`) and rear entrances (`Aisle_01_BackSign`–`Aisle_06_BackSign`).
- **Boundaries, Collision & Navigation**:
  - $26\text{ m} \times 28\text{ m}$ warehouse envelope, $4.5\text{ m}$ perimeter walls, and solid `BoxCollider`s on all racks, desks, barriers, and envelope boundaries.
  - Complete `UnityEngine.AI.NavMesh` coverage across all aisles, cross corridors, entry, collection desk, packing bench, and exit.

## 8. Structured Store Location & SKU Mapping Architecture (Phase 10)
- **Machine-Readable Hierarchy**:
  - Strictly implements: $\text{Store} \to \text{Aisle} \to \text{Side} \to \text{Bay} \to \text{Shelf Level (Tier)} \to \text{Slot}$.
  - Machine-readable format: `A01-L-B01-T01-S01` (or optional store prefix `DS01-A01-L-B01-T01-S01`).
- **Domain Models & API Contracts**:
  - `StoreLocation` (`Assets/Scripts/Domain/StoreLocation.cs`): Pure C# struct encapsulating location components, `TryParse`, `Parse`, `Format`, equality operators, component accessors (`StoreId`, `AisleNumber`, `Side`, `BayIndex`, `TierLevel`, `SlotIndex`).
  - `ShelfSlot` (`Assets/Scripts/Domain/ShelfSlot.cs`): Exposes `StoreLocation Location` and delegates `GenerateSlotId` to `StoreLocation.Format`.
  - `StoreManager` (`Assets/Scripts/Management/StoreManager.cs`):
    - `TryResolveSkuLocation(string skuId, out string locationId, out Vector3 worldPosition)`: Resolves SKU $\to$ Location ID $\to$ World Position.
    - `TryResolveLocationPosition(string locationId, out Vector3 worldPosition)`: Resolves Location ID $\to$ 3D World Position.
    - `TryGetSkuAtLocation(string locationId, out Sku sku)`: Resolves Location ID $\to$ SKU.
    - `ValidateSkuLocationMappings(out List<string> errors)`: Enforces uniqueness, format conformity, non-collision, and finite physical 3D coordinates.
    - `ValidateLocationId(string locationId, out string error)`: Validates syntax and presence in store topology.

## 9. Controlled Order Generation Architecture (Phase 11)
- **Domain Generation Pipeline (`Assets/Scripts/Domain/OrderGenerator.cs`)**:
  - `OrderGenerationConfig`: Configuration object encapsulating integer `Seed`, `ItemCount`, `OrderCount`, `DuplicateSkuPolicy` (`DisallowDuplicates`, `AllowDuplicates`), and `OrderIdPrefix`.
  - `OrderGenerator`: Deterministic order generation engine driven by `System.Random(seed + orderIndex * 31)`.
  - Sampling Policies:
    - `DisallowDuplicates`: Fisher-Yates sampling without replacement from available catalog SKUs.
    - `AllowDuplicates`: Deterministic sampling with replacement.
  - Physical Binding: Every selected SKU is resolved to its exact physical location identifier (`SlotId`) and 3D world position (`TargetWorldPosition`) via `StoreManager.TryResolveSkuLocation`.
  - Batch Generation: `GenerateBatch(StoreManager, OrderGenerationConfig)` produces deterministic batches where each order carries a reproducible ID (`ORD-GEN-{seed}-{index:D3}`).
- **Runtime Dispatch & State Integration (`OrderManager.cs`)**:
  - `TryDispatchGeneratedOrder(OrderGenerationConfig, out Order, out string error)`: Validates catalog and config, instantiates order, rests shelf visuals, and triggers `OnOrderStarted`.
  - `DispatchOrder(Order)`: Direct injection point for generated or preset orders.
  - 100% backward-compatibility: Existing presets (`StartSampleOrder`, `StartSpecificOrder`, `TryStartSpecificOrder`) remain unchanged.

## 10. Navigation Cost Matrix & Routing Optimization Architecture (Phase 12)
- **Two-Level Decoupled Architecture**:
  1. **Physical Pathfinding Level ($A \to B$)**: Unity NavMesh (`UnityEngine.AI.NavMesh.CalculatePath`). Accurately sums polygonal corner-to-corner path segment lengths along walkable corridors; strictly rejects misleading straight-line Euclidean shortcuts through shelving or walls. Explicitly marks unreachable nodes (`float.PositiveInfinity`).
  2. **Sequence Optimization Level ($A \to B \to C \to D$)**: Pick-sequence visit-order optimization (Travelling Salesperson Problem / Routing Solver). Formulates a single-vehicle TSP with fixed start (Order Collection Desk: Node 0) and fixed destination (Packing Desk: Node $K+1$).
- **Core Abstractions & Models (`Assets/Scripts/Routing/`)**:
  - `INavPathDistanceCalculator`: Interface decoupling path evaluation from environment state.
    - `NavMeshPathDistanceCalculator`: Production calculator utilizing `NavMesh.CalculatePath`.
    - `MockDistanceCalculator`: Deterministic calculator for unit testing without baked NavMesh scenes.
  - `TravelCostMatrix`: Reusable $N \times N$ matrix ($N = K + 2$) encapsulating node positions, labels, and traversable path costs. Enforces strict validation: square dimensions, zero diagonal ($Costs[i, i] == 0$), non-negative weights, and explicit unreachable handling.
  - `IRouteOptimizer` & `RouteOptimizationResult`:
    - `BaselineRouteOptimizer`: Reference optimizer that traverses in original catalog/order sequence ($0 \to 1 \to 2 \to \dots \to K \to K+1$). Provides guaranteed safe fallback and baseline comparative cost.
    - `OrToolsRouteOptimizer`: Local solver utilizing Google OR-Tools (`Google.OrTools.ConstraintSolver`). Employs `RoutingIndexManager`, `RoutingModel`, millimeter-precision integer costs, and `GuidedLocalSearch` metaheuristics.
    - Safety Isolation & Platform Fallback: Automatically catches runtime exceptions, missing native binaries, or solver failures and safely falls back to `BaselineRouteOptimizer`. Android builds (`UNITY_ANDROID`) bypass native DLL invocation to maintain Meta Quest standalone compatibility.
    - Mathematical Honesty: Never claims global optimality unless mathematically proven (`IsOptimalProven = false` by default for heuristic solvers).
  - `Order.TryReorderItems`: Enables dynamically updating an active order's pick list to match the optimized sequence prior to picking, enforcing strict SKU multiset conservation.

## 11. End-to-End Operational Workflow Architecture (Phase 13)
- **7-Stage Operational State Machine (`OperationalCycleManager.cs`)**:
  - Connects store physical layout, order intake, picking circulation, packing deposit, and worker egress into one continuous operational cycle:
    $$\text{ENTRY} \longrightarrow \text{COLLECT\_ORDER} \longrightarrow \text{PICKING} \longrightarrow \text{AWAITING\_PACKING} \longrightarrow \text{DEPOSIT\_ORDER} \longrightarrow \text{WALK\_TO\_EXIT} \longrightarrow \text{COMPLETED}$$
  - **Standardized Physical Waypoints**:
    - `EntryPosition`: $X = -8.0\text{ m}, Y = 0.0\text{ m}, Z = -10.5\text{ m}$ (Worker entry archway).
    - `CollectionDeskPosition`: $X = -4.0\text{ m}, Y = 0.0\text{ m}, Z = -8.8\text{ m}$ (Order collection desk terminal).
    - `PackingDeskPosition`: $X = +2.0\text{ m}, Y = 0.0\text{ m}, Z = -8.8\text{ m}$ (Dispatch packing bench PACK-01).
    - `ExitPosition`: $X = +8.0\text{ m}, Y = 0.0\text{ m}, Z = -10.5\text{ m}$ (Dispatch handover egress gate).
- **Interactive Proximity & Trigger Detection**:
  - `PickerInteraction.cs`: Extended with forward raycasting and interaction prompts (`[E / LMB]`) to collect and activate orders at the collection desk and deposit completed totes at the packing bench.
  - Proximity detection monitors active player position against waypoints, automatically guiding desktop and VR operators.
- **Industrial Engineering Aisle Metrics**:
  - `CalculateAisleMetrics`: Deterministically parses visited shelf slot location strings (e.g. `A01-L-B01-T01-S01`):
    - **Aisle Transitions**: Number of times the picker crosses between distinct aisle corridors during the pick sequence. Consecutive picks in the same aisle collapse to zero transitions.
    - **Aisle Backtracking**: Number of times the picker re-enters an aisle corridor that was visited and exited earlier in the pick tour.
- **Scripted Virtual Picker Runner (`OperationalCycleRunner.cs`)**:
  - Headless, deterministic test agent that navigates physical waypoints at realistic walking speed ($1.4\text{ m/s}$), applies shelf search pauses ($1.0\text{ s}$), executes picks, traverses to the packing bench, deposits the order, and walks to the exit.
  - Generates verifiable, reproducible work-study telemetry for automated regression tests and headless batch simulations.

## 12. Baseline vs Optimized Work-Study Comparison Architecture (Phase 14)
- **A/B Comparison Engine (`OperationalComparison.cs`)**:
  - Conducts empirical comparisons between Scenario A (Baseline sequence) and Scenario B (Solver-optimized sequence) under 100% identical starting conditions:
    - Same dark store layout (`DarkStore_Phase1`).
    - Same pick order and required SKU set.
    - Same shelf bay physical locations.
    - Same start position (`ENTRY`) and destination (`EXIT`).
    - Same picker locomotion parameters and navigation system.
    - Only the sequence of SKU stops differs.
- **Reconciliation & Metric Delta Accounting**:
  - Formulates side-by-side delta tables:
    - Total operational cycle time ($s$), walking transit time ($s$), searching time ($s$), picking time ($s$), waiting/desk time ($s$), packing time ($s$).
    - Measured horizontal travel distance ($m$) vs algorithmic solver route cost ($m$).
    - Pick count, distinct shelf bay visits, inter-aisle transitions, and aisle backtracks.
    - Activity distribution share percentages ($P_c = \frac{T_c}{T_{total}} \times 100\%$).
- **Evidence Bundle Exporter**:
  - Generates publication-ready Markdown reports (`docs/evidence/phase14/baseline_vs_optimized_report.md`) and standard RFC-4180 CSV tables (`baseline_vs_optimized_report.csv`).
  - Contains strict methodological notices ensuring no false claims of statistical significance from single-run comparisons.
- **HUD & Operator Runtime Triggers**:
  - Integrated into `SimulationHUD.cs` with hotkey `[F4]` and UI button `[Phase 14: Run Baseline vs Opt Study [F4]]` for immediate interactive execution and telemetry export during live gameplay.
