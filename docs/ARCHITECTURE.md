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
  Enforces and audits rendering budgets tailored for RTX 2050-class hardware (4GB VRAM, 64-bit bus): SRP Batcher enabled, GPU instancing on $16/16$ shared materials, $148$ total mesh renderers, camera far clip plane $\le 45.0\text{ m}$, URP shadow distance $32.0\text{ m}$, 2 shadow cascades, $2\times$ MSAA, 1 shadow-casting directional light + 8 shadowless point lights, and camera post-processing disabled. Generates `docs/evidence/phase8/rtx2050_performance_audit.md` with interactive GPU FPS benchmarking marked `NOT RUN` when audited headlessly.
