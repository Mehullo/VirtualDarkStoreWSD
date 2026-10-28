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
  Screen-space IMGUI modal interface toggleable via `[Tab]` or shown upon order completion displaying metric cards, time-study table, work-sampling proportions, and reconciliation status badges.

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

### 2.4 Interaction Layer (`Assets/Scripts/Player/` & `Assets/Scripts/Environment/`)
- **Desktop First-Person Rig**:
  - Input System Action bindings for Move (WASD), Look (Mouse Delta), Interact/Pick/Pack (`E` / Left Click), New Order (`N`).
  - Physics raycasting for shelf item grab and packing bench interaction.
- **VR Rig (Deferred)**:
  - OpenXR / XR Interaction Toolkit integration mapped to the identical `IPlayerController` / `IInteractable` interfaces.

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
