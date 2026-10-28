# Project Brief: Virtual Dark Store Work-Study & Simulation Platform

## 1. Executive Summary
The **Virtual Dark Store Work-Study & Simulation Platform** is an interactive 3D simulation and industrial engineering tool built in Unity. Its purpose is to simulate warehouse order fulfillment inside a fast-paced "dark store" (micro-fulfillment center) and automate standard work-study, time-and-motion analysis, and work-sampling methodologies.

## 2. Source of Truth & Provenance
- **Repository Inspection Date**: 2026-10-28
- **External Brief Document**: Checked repository root and adjacent directories for external brief files (`*brief*`). None found (Status: NOT PRESENT in repo).
- **Authoritative Source**: `AGENTS.md` (commit `e1a3434`), repository configuration, and Unity project files.

## 3. Core Objective
Build a virtual and VR-ready dark store environment enabling precise, automated measurement of operator fulfillment tasks:
1. **Work-Study Measurement**: Capture standardized elemental breakdown (e.g., travel to shelf, search for SKU, reach/pick item, scan barcode, transfer to tote/cart, pack/stage order).
2. **Work-Sampling**: Capture observational state sampling at continuous or discrete intervals (e.g., Traveling, Searching, Picking, Waiting/Idle, Handling Cart).
3. **Simulation Environment**: Provide a functional 3D micro-fulfillment center with configurable aisles, shelving racks, bins, SKUs, pick carts, and packing benches.

## 4. Non-Negotiable Boundaries (from AGENTS.md)
- **No AI/ML Prediction**: This is an industrial engineering measurement and simulation platform, NOT an AI/ML product. No predictive neural networks or synthetic dataset generation pipelines are to be created unless explicitly requested.
- **No Fabricated Real-World Data**: Benchmark parameters and logs must represent simulated telemetry or documented standard data, never fabricated real-world claims.
- **No Python Without Approval**: Primary engine and logic remain C# / Unity. No external Python processes without explicit justification and user approval.
- **Desktop-First Execution**: Desktop (keyboard/mouse first-person simulation) MUST be fully functional and verified before VR implementation begins.
- **Shared Simulation Core**: Desktop and VR modes must share the identical backend simulation, inventory model, and WorkStudy telemetry core.
- **Instrumentation Honesty**: Do not claim eye-gaze tracking, fatigue measurement, cognitive load, or physiological data without real, physically connected instrumentation.

## 5. Target Workflow
1. **Order Dispatch**: Picker receives a pick list (SKU, bin location, quantity).
2. **Travel**: Picker navigates through aisles to the target shelf location.
3. **Search & Identification**: Picker visually identifies the target SKU.
4. **Acquisition & Verification**: Picker grabs the item and scans it (virtual barcode/RFID confirmation).
5. **Placement**: Item is placed into a cart tote or batch bin.
6. **Consolidation & Handoff**: Order is transported to the packing/staging station for dispatch.
7. **Telemetry & Work-Study Logging**: Every elemental movement, timestamp, distance, and interaction is recorded into structured logs (CSV/JSON).

## 6. Success Criteria
- Deterministic and verifiable measurement of cycle times and elemental steps.
- Clear separation between the presentation layer (Desktop/VR) and the WorkStudy telemetry engine.
- Reproducible simulation runs without external runtime dependencies.
