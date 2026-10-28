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

