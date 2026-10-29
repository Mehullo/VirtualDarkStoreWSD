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


