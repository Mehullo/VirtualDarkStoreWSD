# Virtual Dark Store Work-Study & Simulation Platform

## Purpose
The **Virtual Dark Store Work-Study & Simulation Platform** is a 3D simulation and industrial engineering measurement environment built in Unity 6 (`6000.6.7f1`) with the Universal Render Pipeline (URP). It simulates grocery micro-fulfillment ("dark store") operations to automate industrial time-and-motion studies, elemental cycle time analysis, interval work-sampling observations, and deterministic A/B scenario comparisons.

> **Scope & Honesty Guarantee**: This platform is an industrial engineering measurement and simulation tool—**not** an AI/ML prediction product. All outputs are **measured simulation telemetry** derived deterministically from recorded `WorkEvent` logs. No AI/ML models, synthetic dataset pipelines, uninstrumented physiological/eye-gaze claims, or fabricated FPS/benchmark numbers are included.

## Current Status
- **Current Phase**: Phases 0–14 Completed + Usability Improvements (Physical Labels, Shelf Population & Configurable Multi-Order Simulation)
- **Status**: **COMPLETED** (2026-11-02)
- **Automated Verification**: **84 / 84 EditMode Tests Passing** (`9.56s`, 0 failures, 0 warnings)
- **Primary Scene**: `Assets/Scenes/DarkStore_Phase1.unity` (6 aisles `A01`–`A06`, 144 stocked physical pickable SKUs across all racks, closed rack backs, entry zone, order collection desk, picking area, packing/deposit desk, exit zone, physical aisle/bay/level/slot signage, collision, baked NavMesh, operational state machine, multi-order simulation setup)

---

## Features Across All Phases (Phases 0–14)

### Visual & Usability Improvement: Physical Location Labels & Store Signage
- **Physical In-World Signage Hierarchy**:
  - Adds crisp, lightweight physical signage reflecting the existing $\text{Store} \to \text{Aisle} \to \text{Side} \to \text{Bay} \to \text{Shelf Level (Tier)} \to \text{Slot}$ hierarchy (e.g. `A01-L-B01-T01-S01`).
  - **Aisle Header Signboards**: High-contrast overhead signs (`AISLE A01` ... `AISLE A06`) on front (South-facing) and rear (North-facing) entrance beams.
  - **End-Cap Placards**: Front rack upright placards (`A01-L (LEFT)` / `A01-R (RIGHT)`) visible from the front cross-aisle.
  - **Bay Placards**: Eye-level placards (`A01-L-B01` | `BAY 01 • LEFT`) mounted atop each bay facing the corridor.
  - **Shelf Level Badges**: Clear tier identifiers (`LEVEL 01`, `LEVEL 02`, `LEVEL 03`) aligned with shelf tier heights.
  - **Product Slot Tickets**: Shelf-edge barcode tickets (`A01-L-B01-T01-S01` / `SKU-0001`) mounted directly beneath each stocked SKU.
- **Zero Simulation / Physics Interference**:
  - Built with `PhysicalLocationLabel` using World Space Canvases, strictly zero colliders, and `raycastTarget = false`.
  - Zero interference with NavMesh pathfinding, player locomotion, or raycasting.
- **Immediate Location Feedback in Picker View**:
  - First-person reticle and VR controller prompts display the exact slot ID on hover: `[E / LMB] Pick: <SkuName> (<SkuId>) [<SlotId>]`.

### Visual & Usability Improvement: Shelf Population (144 SKUs Across Usable Racks)
- **Believable Quick-Commerce Inventory**:
  - Populates 144 physical pickable items (`PickableItemVisual`) across all 6 aisles (`A01`–`A06`), stocking both Left and Right racks, all 3 bays per rack, and multiple shelf tiers.
  - Strict 1:1 mapping between visual objects and `StoreManager` domain inventory records—every physical product is known to the simulation, and every registered SKU has a physical representation with a shelf-edge barcode ticket.
  - Expanded catalog in `VerifiedSampleCatalog.CreateQuickCommerceCatalog()` provides 144 verified grocery SKUs across all 8 micro-fulfillment categories with realistic barcodes (`890100000001`–`890100000144`), weights, and dimensions.
- **Hardware Budget & Performance Parity**:
  - RTX 2050 GPU budget maintained at 284 total scene mesh renderers ($\le 300$ budget ceiling) with 100% GPU-instanced shared materials and 0 shadow draw call overhead.
  - Retains backward compatibility with 4-aisle configurations and preserves existing scenario pick locations.

### Simulation Enhancement: Configurable Multi-Order Simulation
- **Multi-Order Batch Simulation**:
  - Supports configurable multi-order generation with user-specified Number of Orders ($N$), Items per Order ($K$), and Random Seed.
  - Explicit parameter distinction: $N$ independent orders containing $K$ selected items each, yielding $N \times K$ requested item selections.
  - 100% physically executable: all generated orders draw exclusively from verified populated store inventory (144 SKUs) and map to valid shelf slots.
- **Sequential Execution & Automatic Progression**:
  - High-level flow: $\text{Generate Simulation} \longrightarrow \text{Order 1} \longrightarrow \text{Pick} \longrightarrow \text{Complete/Deposit} \longrightarrow \text{Order 2} \dots \longrightarrow \text{Simulation Complete}$.
  - Seamlessly resets shelf product visuals and advances upon packing completion at the packing bench.
- **Strict Validation & Non-Fabricated Telemetry**:
  - Graceful validation prevents crashes for invalid inputs ($N \le 0$, $K \le 0$, $K > 144$ unique SKU capacity under `DisallowDuplicates`).
  - Preserves individual order measurability and derives aggregate metrics strictly from actual recorded `WorkEvent`s.


### Phase 13: End-to-End Operational Workflow
- **7-Stage Operational State Machine**:
  - Unifies dark store circulation into one continuous operational cycle:
    $$\text{ENTRY} \longrightarrow \text{COLLECT\_ORDER} \longrightarrow \text{PICKING} \longrightarrow \text{AWAITING\_PACKING} \longrightarrow \text{DEPOSIT\_ORDER} \longrightarrow \text{WALK\_TO\_EXIT} \longrightarrow \text{COMPLETED}$$
  - Standardized physical waypoints: `EntryPosition` (-8.0, 0, -10.5), `CollectionDeskPosition` (-4.0, 0, -8.8), `PackingDeskPosition` (2.0, 0, -8.8), and `ExitPosition` (8.0, 0, -10.5).
  - Proximity detection and forward raycasting prompts (`[E / LMB]`) to collect orders and deposit packing totes.
- **Industrial Engineering Aisle Metrics**:
  - `CalculateAisleMetrics`: Measures distinct inter-aisle transitions and identifies aisle backtracking (re-entering an aisle corridor previously visited and exited).
- **Headless Virtual Picker Runner**:
  - `OperationalCycleRunner`: Fully automated, deterministic virtual picker for repeatable headless simulation and work-study regression testing.

### Phase 14: Baseline vs Optimized Work-Study Comparison
- **Empirical A/B Work-Study Comparison Engine**:
  - `OperationalComparison`: Compares Scenario A (Baseline sequence) vs Scenario B (Solver-optimized sequence) under 100% identical starting conditions, layout, order items, and navigation.
  - Measures total cycle time, walking transit time, searching time, picking time, waiting time, packing time, measured travel distance, picks, shelf visits, aisle transitions, and aisle backtracks.
- **Telemetry & Report Exporter**:
  - Exports publication-grade Markdown reports and RFC-4180 CSV tables directly to `docs/evidence/phase14/`.
  - Transparent methodology disclaimer ensuring honesty and avoiding false claims of statistical significance from single-run studies.
- **HUD Integration & Shortcuts**:
  - `SimulationHUD.cs`: Operational stage status banner, keybinding `[F4]`, and button `[Phase 14: Run Baseline vs Opt Study [F4]]` to run the comparison and export evidence bundles on demand.

### Phase 11: Controlled Order Generation
- **Reproducible Deterministic Orders**:
  - `OrderGenerationConfig` (`Assets/Scripts/Domain/OrderGenerator.cs`): Configurable `Seed`, `ItemCount`, `OrderCount`, `DuplicateSkuPolicy` (`DisallowDuplicates`, `AllowDuplicates`), and `OrderIdPrefix`.
  - Driven by `System.Random(seed + orderIndex * 31)` with pure C# logic (zero Python or external services).
  - Identical seed guarantees the exact same order items, quantities, and physical shelf locations.
- **Physical Shelf Location Binding**:
  - All generated SKUs are verified against `StoreManager` catalog and resolved to physical shelf slot IDs and 3D coordinates via `TryResolveSkuLocation`.
- **Runtime Dispatch & Minimal UI**:
  - `OrderManager.TryDispatchGeneratedOrder` and `OrderManager.DispatchOrder` allow executing controlled orders with automatic shelf visual resets.
  - Interactive seed and item steppers in `SimulationHUD.cs` and keybinding `[G]` to trigger controlled orders directly in-game.

### Phase 12: Navigation Cost Matrix & Routing Optimization
- **Physical Pathfinding vs Sequence Optimization Separation**:
  - **Level 1 (Physical Pathfinding)**: Evaluates traversable paths along corridor corners via Unity `NavMesh.CalculatePath`. Rejects misleading straight-line Euclidean shortcuts through shelving or walls; explicitly flags unreachable pairs (`float.PositiveInfinity`).
  - **Level 2 (Multi-Stop Pick Sequence Optimization)**: Formulates multi-stop warehouse TSP with fixed start (Order Collection Desk: Node 0) and fixed end (Packing Bench: Node $K+1$).
- **Square Travel Cost Matrix (`TravelCostMatrix.cs`)**:
  - Reusable $N \times N$ matrix ($N = K + 2$) validating square dimensions, zero diagonal ($Costs[i, i] == 0$), non-negative weights, and explicit unreachable handling.
- **Local Google OR-Tools Routing Solver (`OrToolsRouteOptimizer.cs`)**:
  - Local execution verified on Unity 6000.6.7f1 (Windows x64) via `Google.OrTools.ConstraintSolver` with millimeter-precision integer costs and `GuidedLocalSearch`.
  - Automatic safe fallback to `BaselineRouteOptimizer` on solver exceptions, unreachable entries, or unsupported platforms (such as Meta Quest Android).
  - Mathematical honesty: Does not claim global optimality unless proven (`IsOptimalProven = false`).
- **Dynamic Pick Sequence Reordering**:
  - `Order.TryReorderItems` updates active pick checklist prior to picking while strictly verifying SKU multiset conservation.
  - Interactive button `[Opt (OR-Tools)]` and keybinding `[O]` in `SimulationHUD.cs` displaying baseline vs optimized travel distance reduction.

### Phase 9: Quick-Commerce Dark Store Environment Upgrade
- **6 Picking Aisles (`A01`–`A06`)**:
  - Symmetrically spaced with standard $3.0\text{ m}$ pitch ($1.8\text{ m}$ walkable corridor $+ 2 \times 0.6\text{ m}$ rack depth) at $X = -7.5\text{ m}, -4.5\text{ m}, -1.5\text{ m}, +1.5\text{ m}, +4.5\text{ m}, +7.5\text{ m}$.
  - Two product-facing rack sides per aisle with $12.0\text{ m}$ length, $2.4\text{ m}$ height.
  - Closed backs between back-to-back rack blocks ($X = -6.0\text{ m}, -3.0\text{ m}, 0.0\text{ m}, +3.0\text{ m}, +6.0\text{ m}$) and outer warehouse perimeter racks.
- **Operational Flow Zones (Left-to-Right Quick-Commerce Flow)**:
  - **Worker Entry Zone** ($X = -8.0\text{ m}, Z = -10.5\text{ m}$): Archway, overhead "ENTRY / RECEIVING" signboard, walkway floor pad, turnstile barrier rails with solid collision.
  - **Order Collection Desk** ($X = -4.0\text{ m}, Z = -10.0\text{ m}$): Table, computer terminal, pick cart tote bins, overhead signage, solid physics collider.
  - **Picking Area** ($X = -9.0\text{ m}\dots+9.0\text{ m}, Z = -4.0\text{ m}\dots+8.0\text{ m}$): 6 aisles, $1.8\text{ m}$ wide corridors, front cross-aisle ($3.0\text{ m}$ clear width), rear cross-aisle ($3.5\text{ m}$ clear width), safety boundary lines.
  - **Packing & Deposit Desk** ($X = +2.0\text{ m}, Z = -10.0\text{ m}$): Packing bench, terminal screen, dispatch tote bins, floor drop zone pad, "PACK-01" `PackingStation` integration.
  - **Exit / Dispatch Handover Zone** ($X = +8.0\text{ m}, Z = -10.5\text{ m}$): Archway, overhead "EXIT / DISPATCH HANDOVER" signboard, egress walkway pad, barrier rails with solid collision.
- **Aisle Labels & Orientation**:
  - High-contrast overhead signboards at front entrances (`Aisle_01_HeaderSign` through `Aisle_06_HeaderSign`) and rear entrances (`Aisle_01_BackSign` through `Aisle_06_BackSign`).
- **Boundaries, Collision & Navigation**:
  - $26\text{ m} \times 28\text{ m}$ warehouse envelope, $4.5\text{ m}$ high perimeter walls, solid `BoxCollider`s on all racks, desks, barriers, and perimeter boundaries.
  - Complete `UnityEngine.AI.NavMesh` coverage across all aisles, cross corridors, entry, collection desk, packing bench, and exit.

### Phase 10: Structured Store Location & SKU Mapping
- **Hierarchical Machine-Readable Physical Location**:
  - Strictly implements: $\text{Store} \to \text{Aisle} \to \text{Side} \to \text{Bay} \to \text{Shelf Level (Tier)} \to \text{Slot}$.
  - Standard format: `A01-L-B01-T01-S01` (or optional store prefix `DS01-A01-L-B01-T01-S01`).
- **Domain Entity & Location Resolution**:
  - `StoreLocation` struct in `Assets/Scripts/Domain/StoreLocation.cs` with robust `TryParse`, `Parse`, `Format`, component properties (`StoreId`, `AisleNumber`, `Side`, `BayIndex`, `TierLevel`, `SlotIndex`), and equality operators.
  - Bidirectional resolution: $\text{SKU} \to \text{Location ID} \to \text{World Position}$ via `StoreManager.TryResolveSkuLocation` and `StoreManager.TryResolveLocationPosition`.
  - Fully backward compatible with existing `ShelfSlot`, `Shelf`, `Aisle`, and `StoreManager` architecture.
- **Integrity & Validation Suite**:
  - `StoreManager.ValidateSkuLocationMappings` and `StoreManager.ValidateLocationId` validating syntax, uniqueness, non-collision, and finite physical 3D coordinates.
  - Automated EditMode tests in `Assets/Tests/Editor/Domain/StructuredStoreLocationTests.cs`:
    1. Unique location IDs
    2. Valid SKU mappings
    3. World-position resolution
    4. Missing location handling (non-throwing)
    5. Duplicate location detection
    6. Invalid mapping rejection

### Phases 0–8 Baseline Features (Fully Preserved)
- **Domain Topology & Inventory (Phases 0–2)**: 72 verified catalog SKUs across 8 departments (`VerifiedSampleCatalog.cs`) with 1:1 slot assignment and zero collision.
- **Order Engine & Pick Validation (Phase 3)**: Strict SKU validation (`PickResult.WrongItem`), `OrderManager.cs`, `PickableItemVisual.cs`, and `SimulationErrorHandler.cs`.
- **Work-Study Telemetry & Activity Classifier (Phase 4)**: Monotonic `WorkEventLogger.cs` and `ActivityClassifier.cs` evaluating all 8 industrial activities (`Walking`, `Waiting`, `Searching`, `Approaching`, `Picking`, `MovingBetweenLocations`, `Packing`, `OrderCompleted`).
- **Work-Study Calculator & Automated Reports (Phase 5 & Phase 8)**: `WorkStudyCalculator.cs` continuous cycle analysis, configurable work-sampling ($0.5\text{ s}\dots5.0\text{ s}$), reconciliation verification, and report bundle export (`WorkStudyReportExporter.cs`).
- **Scenario Comparison & Route Visualization (Phase 6)**: `ScenarioComparisonUI.cs` (`[F3]`) and top-down route maps.
- **VR Player Rig & Parity (Phase 7)**: Dual Desktop and VR support via `IPickerInteraction.cs` with exact displacement preservation ($\Delta p$).
- **RTX 2050-Class Performance Budget (Phase 8)**: SRP Batcher enabled, $100\%$ GPU instancing ($17/17$ materials), $212$ total renderers ($\le 300$), 10 realtime lights ($\le 12$), 1 shadow caster, camera far clip $45.0\text{ m}$.

---

## Reproducible Setup Instructions

### 1. System Prerequisites
- **OS**: Windows 10/11 (x64)
- **Unity Editor**: `6000.6.7f1` (Revision `b22767f04f04`) with Universal Render Pipeline (URP `17.6.0`)
- **.NET SDK**: `10.0+` (Verified with `10.0.402`)
- **Git**: `2.55+`

### 2. Clone & Open in Unity Editor
1. Clone this repository and open **Unity Hub**.
2. Click **Add** -> **Add project from disk** and select the `VirtualDarkStoreWSD` root directory.
3. Launch with Unity Editor **`6000.6.7f1`**.
4. In the Project window, open **`Assets/Scenes/DarkStore_Phase1.unity`**.

### 3. Reproducible Scene Rebuild & 47-Test Suite (CLI)
1. **Rebuild Dark Store Scene & NavMesh**:
   ```powershell
   $p = Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" `
     -ArgumentList "-batchmode -nographics -projectPath . -executeMethod VirtualDarkStore.Editor.DarkStoreSceneBuilder.BuildDarkStorePhase1Scene -quit -logFile Logs/SceneBuild.log" `
     -PassThru -Wait
   Write-Output "ExitCode: $($p.ExitCode)"
   ```
2. **Execute Full 47-Test EditMode Suite**:
   ```powershell
   $p = Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" `
     -ArgumentList "-batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults-EditMode.xml -logFile Logs/TestRun.log" `
     -PassThru -Wait
   Write-Output "ExitCode: $($p.ExitCode)"
   ```
