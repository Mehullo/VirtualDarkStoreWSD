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
- **Elemental State Machine**: Tracks picker activity states according to industrial engineering standards:
  - `IDLE`: Not actively engaging with an order.
  - `TRAVEL_TO_SHELF`: Movement between station and target rack.
  - `SEARCH_ITEM`: Visual search and localization in bin.
  - `PICK_ITEM`: Grabbing item from shelf.
  - `SCAN_VERIFY`: Scanning barcode/RFID.
  - `PLACE_IN_TOTE`: Transferring item into pick cart/tote.
  - `TRAVEL_TO_STAGING`: Navigating completed order to packing/dispatch.
  - `DEPOSIT_ORDER`: Dropping tote at packing station.
- **Continuous Metrics Engine**: Tracks cumulative distance traveled (meters), elevation/reach heights, and timestamps ($t_{start}, t_{end}$).
- **Work-Sampling Sampler**: Periodically samples instantaneous state for proportion-of-time calculations ($p = \frac{n_i}{N}$).
- **Telemetry Exporter**: Streams observations to local CSV/JSON files with UTC timestamps and deterministic identifiers.

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

### 2.3 Interaction Layer (`Assets/Scripts/Player/` & `Assets/Scripts/Environment/`)
- **Desktop First-Person Rig**:
  - Input System Action bindings for Move (WASD), Look (Mouse Delta), Interact/Pick (E / Left Click), Scan (Right Click / F), Drop (Q).
  - Physics raycasting for pickup and placement.
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
