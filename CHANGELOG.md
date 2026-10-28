# Changelog

All notable changes to the Virtual Dark Store Work-Study & Simulation Platform will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to Semantic Versioning.

## [Unreleased]

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
