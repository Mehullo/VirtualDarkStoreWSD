# Testing Strategy & Verification Records

## 1. Verified Test Execution Status
> [!NOTE]
> **Current Status**: **41 TESTS EXECUTED AND PASSED** (Phase 8 Final Verification).
> All 41 EditMode tests passed with 0 failures and 0 warnings on 2026-10-29 (`0.771s`). Evidence recorded in `TestResults-EditMode.xml` and `docs/evidence/phase8/`.

### Verified Test Run Evidence (2026-10-28 11:06:50Z)
| Test Suite / Fixture | Test Case | Status | Duration |
|---|---|---|---|
| `VirtualDarkStore.Tests.Domain.OrderFulfillmentTests` | `OrderItem_InitialState_AndPickRecording` | **PASSED** | 0.003s |
| `VirtualDarkStore.Tests.Domain.OrderFulfillmentTests` | `Order_Validation_RejectsWrongSku_WithoutAdvancingOrCounting` | **PASSED** | 0.003s |
| `VirtualDarkStore.Tests.Domain.OrderFulfillmentTests` | `Order_Validation_RejectsNullOrEmptySku` | **PASSED** | 0.003s |
| `VirtualDarkStore.Tests.Domain.OrderFulfillmentTests` | `Order_Validation_AcceptsCorrectSku_AdvancesSequentially` | **PASSED** | 0.005s |
| `VirtualDarkStore.Tests.Domain.OrderFulfillmentTests` | `Order_FinalizePacking_RejectsPrematurePacking` | **PASSED** | 0.073s |
| `VirtualDarkStore.Tests.Domain.OrderFulfillmentTests` | `Order_FinalizePacking_SucceedsWhenAwaitingPacking` | **PASSED** | 0.005s |
| `VirtualDarkStore.Tests.Domain.OrderFulfillmentTests` | `OrderManager_CompleteFulfillmentCycle_EndToEnd` | **PASSED** | 0.008s |
| `VirtualDarkStore.Tests.Domain.StoreTopologyTests` | `VerifiedSampleCatalog_Count_IsBetween50And100` | **PASSED** | 0.001s |
| `VirtualDarkStore.Tests.Domain.StoreTopologyTests` | `VerifiedSampleCatalog_AllSkus_HaveValidUniqueIdentifiersAndAttributes` | **PASSED** | 0.005s |
| `VirtualDarkStore.Tests.Domain.StoreTopologyTests` | `StoreManager_InitializesTopology_WithExpectedSlotCapacity` | **PASSED** | 0.001s |
| `VirtualDarkStore.Tests.Domain.StoreTopologyTests` | `StoreManager_MultipleShelfLevels_ExistInEveryBay` | **PASSED** | 0.002s |
| `VirtualDarkStore.Tests.Domain.StoreTopologyTests` | `StoreManager_EverySku_HasAValidUniqueLocation` | **PASSED** | 0.002s |
| `VirtualDarkStore.Tests.Domain.StoreTopologyTests` | `StoreManager_BidirectionalMapping_IsConsistent` | **PASSED** | 0.002s |
| `VirtualDarkStore.Tests.Domain.StoreTopologyTests` | `StoreManager_ValidateIntegrity_ReturnsTrueWithZeroErrors` | **PASSED** | 0.001s |
| `VirtualDarkStore.Tests.Domain.StoreTopologyTests` | `StoreManager_QueryNonExistent_ReturnsFalseGracefully` | **PASSED** | 0.001s |
| `VirtualDarkStore.Tests.WorkStudy.WorkStudyInstrumentationTests` | `WorkEvent_CalculatesDuration_AndValidatesNonNegativeValues` | **PASSED** | 0.006s |
| `VirtualDarkStore.Tests.WorkStudy.WorkStudyInstrumentationTests` | `WorkEvent_RejectsNegativeOrInvertedTimestamps` | **PASSED** | 0.008s |
| `VirtualDarkStore.Tests.WorkStudy.WorkStudyInstrumentationTests` | `WorkEventLogger_MaintainsMonotonicOrderedTimestamps` | **PASSED** | 0.020s |
| `VirtualDarkStore.Tests.WorkStudy.WorkStudyInstrumentationTests` | `ActivityClassifier_EvaluatesAll8Activities_AccordingToMeasurableRules` | **PASSED** | 0.009s |
| `VirtualDarkStore.Tests.WorkStudy.WorkStudyInstrumentationTests` | `SampleOrderFulfillment_GeneratesOrderedNonNegativeEvents` | **PASSED** | 0.044s |
| `VirtualDarkStore.Tests.WorkStudy.WorkStudyCalculatorTests` | `EmptyEventsList_ProducesZeroResults_ReconcilesSuccessfully` | **PASSED** | 0.002s |
| `VirtualDarkStore.Tests.WorkStudy.WorkStudyCalculatorTests` | `DeterministicScenario_CalculatesAccurateMetrics_AndReconciles` | **PASSED** | 0.003s |
| `VirtualDarkStore.Tests.WorkStudy.WorkStudyCalculatorTests` | `WorkSampling_ConfigurableInterval_MatchesDiscreteObservationRule` | **PASSED** | 0.003s |
| `VirtualDarkStore.Tests.WorkStudy.WorkStudyCalculatorTests` | `SampleOrderExecution_GeneratesFullyReconciledResults` | **PASSED** | 0.040s |

- **Total Test Cases**: 24
- **Passed**: 24
- **Failed**: 0
- **Duration**: 0.332s

### Deterministic Scenario Reconciliation Verification Evidence
Validated via `DeterministicScenario_CalculatesAccurateMetrics_AndReconciles`:
- **Scenario Event Log**:
  - `[0.0s - 10.0s, 15.0m] MovingBetweenLocations` (Target SKU-1, Bay A01-L-B01)
  - `[10.0s - 12.0s, 0.0m] Searching` (Target SKU-1, Bay A01-L-B01)
  - `[12.0s - 13.0s, 0.0m] Picking` (Target SKU-1, Bay A01-L-B01)
  - `[13.0s - 20.0s, 10.0m] MovingBetweenLocations` (Target SKU-2, Bay A02-R-B03)
  - `[20.0s - 22.0s, 0.0m] Searching` (Target SKU-2, Bay A02-R-B03)
  - `[22.0s - 23.0s, 0.0m] Picking` (Target SKU-2, Bay A02-R-B03)
  - `[23.0s - 25.0s, 2.0m] Walking` (Target SKU-2, Bay A02-R-B03)
  - `[25.0s - 27.0s, 0.0m] Approaching` (Packing Station)
  - `[27.0s - 29.0s, 0.0m] Packing` (Packing Station)
  - `[29.0s - 30.0s, 0.0m] Waiting` (Order Completed)
- **Computed Metrics**:
  - Total Task Time: $30.00\text{ s}$
  - Category Duration Sum: $10.0 + 12.0 + 13.0 ... = 30.00\text{ s}$ ($\Delta = 0.000\text{ s}$)
  - Category Percentage Sum: $100.00\%$ ($\Delta = 0.000\%$)
  - Total Distance: $27.00\text{ m}$ (15m + 10m + 2m)
  - Total Picks: $2$
  - Distinct Shelf Bays Visited: $2$ (`A01-L-B01`, `A02-R-B03`)
  - Work-Sampling: 6 observations at 5.0s intervals, sum of proportions = $100.00\%$
  - Math Reconciliation Result: **PASSED (Zero Reconciliation Discrepancy)**

### Sample Order Event Log Inspection Evidence
Generated during `SampleOrderFulfillment_GeneratesOrderedNonNegativeEvents` and validated for monotonic timestamps and non-negative durations:
```
[EVT-0001] Waiting | T: 0.00s - 0.34s (0.34s) | Dist: 0.00m | Order: ORD-0001 | SKU: SKU-0001 | Loc: A01-L-B01-T01-S01 | Pos: (0.0, 0.0, -8.0)
[EVT-0002] MovingBetweenLocations | T: 0.34s - 4.00s (3.66s) | Dist: 9.00m | Order: ORD-0001 | SKU: SKU-0001 | Loc: A01-L-B01-T01-S01 | Pos: (-7.2, 0.4, -1.7)
[EVT-0003] Approaching | T: 4.00s - 5.50s (1.50s) | Dist: 0.00m | Order: ORD-0001 | SKU: SKU-0001 | Loc: A01-L-B01-T01-S01 | Pos: (-7.2, 0.4, -1.7)
[EVT-0004] Searching | T: 5.50s - 5.50s (0.00s) | Dist: 0.00m | Order: ORD-0001 | SKU: SKU-0002 | Loc: A01-L-B01-T01-S02 | Pos: (-7.2, 0.4, -1.7)
[EVT-0005] Picking | T: 5.50s - 6.00s (0.50s) | Dist: 0.00m | Order: ORD-0001 | SKU: SKU-0001 | Loc: A01-L-B01-T01-S01 | Pos: (-7.2, 0.4, -1.7)
[EVT-0006] MovingBetweenLocations | T: 6.00s - 8.50s (2.50s) | Dist: 4.00m | Order: ORD-0001 | SKU: SKU-0002 | Loc: A01-L-B01-T01-S02 | Pos: (-7.2, 0.4, -2.8)
[EVT-0007] Approaching | T: 8.50s - 12.20s (3.70s) | Dist: 2.00m | Order: ORD-0001 | SKU: SKU-0002 | Loc: A01-L-B01-T01-S02 | Pos: (-7.2, 0.4, -1.2)
[EVT-0008] Searching | T: 12.20s - 12.20s (0.00s) | Dist: 0.00m | Order: ORD-0001 | SKU:  | Loc: PACK-01 | Pos: (-7.2, 0.4, -1.2)
[EVT-0009] Picking | T: 12.20s - 12.70s (0.50s) | Dist: 0.00m | Order: ORD-0001 | SKU: SKU-0002 | Loc: A01-L-B01-T01-S02 | Pos: (-7.2, 0.4, -1.2)
[EVT-0010] MovingBetweenLocations | T: 12.70s - 17.50s (4.80s) | Dist: 8.00m | Order: ORD-0001 | SKU:  | Loc: PACK-01 | Pos: (0.0, 0.0, -8.5)
[EVT-0011] Approaching | T: 17.50s - 18.70s (1.20s) | Dist: 0.00m | Order: ORD-0001 | SKU:  | Loc: PACK-01 | Pos: (0.0, 0.0, -8.5)
[EVT-0012] Packing | T: 18.70s - 18.70s (0.00s) | Dist: 0.00m | Order: ORD-0001 | SKU:  | Loc: GENERAL_FLOOR | Pos: (0.0, 0.0, -8.5)
[EVT-0013] OrderCompleted | T: 18.70s - 19.50s (0.80s) | Dist: 0.00m | Order: ORD-0001 | SKU:  | Loc: PACK-01 | Pos: (0.0, 0.0, -8.5)
```

## 2. Testing Framework & Tooling
- **Unit & Integration Framework**: Unity Test Framework (`com.unity.test-framework` `1.4.6`, added to `Packages/manifest.json`).
- **Assembly Definition**: `Assets/Tests/Editor/VirtualDarkStore.Tests.Editor.asmdef`.
- **Execution Mode**: Headless CLI via Unity batch mode (`-runTests -testPlatform EditMode`).

## 3. Planned Test Coverage (Phase 1 onwards)

### 3.1 WorkStudy Core Unit Tests (`Assets/Tests/Editor/WorkStudy/`)
- `TimeStudyTimerTests`:
  - Verify timer start, pause, lap, and stop accuracy.
  - Verify zero-duration or negative-duration protection.
- `ElementalStateMachineTests`:
  - Verify valid transitions: `IDLE` -> `TRAVEL_TO_SHELF` -> `SEARCH_ITEM` -> `PICK_ITEM` -> `SCAN_VERIFY` -> `PLACE_IN_TOTE` -> `TRAVEL_TO_STAGING` -> `DEPOSIT_ORDER` -> `IDLE`.
  - Verify invalid transition rejection (e.g. attempting to deposit before picking).
- `WorkSamplingMathTests`:
  - Verify proportion-of-time calculations: $p_i = \frac{n_i}{N}$.
  - Verify margin of error calculations: $e = z \sqrt{\frac{p(1-p)}{N}}$ at 95% confidence ($z = 1.96$).
- `CsvExporterTests`:
  - Verify RFC-4180 compliance, header consistency, comma escaping, and newline formatting.

### 3.2 Simulation Domain Tests (`Assets/Tests/Editor/Simulation/`)
- `InventoryCoordinateTests`:
  - Verify coordinate parsing (Aisle, Bay, Shelf, Bin).
- `OrderGenerationTests`:
  - Verify pick lists generate valid existing SKUs and positive quantities.
- `PickVerificationTests`:
  - Verify scanning correct SKU validates successfully; scanning incorrect SKU raises mismatch event.

## 4. Headless Test Execution Command
To run PlayMode and EditMode tests from the command line once test assemblies are created:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" `
  -batchmode `
  -runTests `
  -projectPath "C:\Users\Mehul\OneDrive\Documents\VirtualDarkStoreWSD" `
  -testPlatform EditMode `
  -testResults "Library\TestResults-EditMode.xml" `
  -logFile "Library\TestExecution.log"
```

## 5. Manual Desktop Verification Checklist
For interactive verification in Desktop Play Mode (Phase 4):
- [ ] First-person player spawns at the dispatch/staging area.
- [ ] Player moves smoothly via WASD and looks via Mouse Delta.
- [ ] Pick list UI displays current required SKU and bin location.
- [ ] Reticle raycast highlights interactable shelf item.
- [ ] Pressing interact pick action grabs target item.
- [ ] Pressing scan action triggers barcode scan confirmation audio/visual prompt.
- [ ] Item is deposited in cart tote; state machine transitions to next pick or staging.
- [ ] CSV file is written to disk upon session termination with accurate elemental timestamps.

## 6. Phase 6 Scenario Tests (`Assets/Tests/Editor/Scenarios/ScenarioTests.cs`)
Executed 2026-10-28: full EditMode suite 29/29 passed. New tests: definition validation, JSON save/load round-trip, deterministic run using real classifier events, non-equivalent condition flagging, two-scenario run + comparison + evidence export (writes `docs/evidence/phase6/` and `Scenarios/`).

## 7. Phase 7 VR Rig & Parity Tests (`Assets/Tests/Editor/VR/VRRigTests.cs`)
Executed 2026-10-28: full EditMode suite **36/36 passed (100% pass rate in 0.915s)**.
New automated tests:
1. `VRPlayerRig_InitializesHierarchy_AndTrackingComponents`: Verifies XROrigin, camera offset, HMD camera, head tracking, dual controllers, pick interaction, and locomotion.
2. `VRHeadTracking_ExposesOrientationAndRay_WithoutPhysiologicalClaims`: Verifies spatial head pose tracking and orientation forward ray without physiological/eye-gaze claims.
3. `VRControllerInput_TracksSpatialPose_AndSimulatesButtons`: Verifies dual-hand spatial poses and trigger, grip, and thumbstick axes.
4. `VRPickerInteraction_ValidatesAndPicksItems_SharingSameOrderSystem`: Verifies raycast hit detection, pick validation against active order line items, and staging packing bench finalization.
5. `VRLocomotion_ContinuousAndTeleportation_PreservesTravelDistanceMeasurement`: Verifies thumbstick movement and teleportation with exact displacement accumulation ($\Delta p$) to ensure travel metrics are preserved.
6. `VRFulfillment_IntegrationWithActivityClassifier_AndWorkStudyCalculator`: Verifies end-to-end VR order fulfillment with monotonic event logging, duration reconciliation ($\sum T_i = T_{total}$), and percentage reconciliation ($\sum P_i = 100\%$).
7. `VRHardware_Detection_ReportsActualStatus`: Executes `VRHardwareDetector.CheckHardware()` and exports report to `docs/evidence/phase7/vr_hardware_report.md`. In headless/automated test environments without connected headsets, status is strictly marked **NOT RUN**.

## 8. Phase 8 Polish, Performance Budget & 7-Step Final Demo Tests (`Assets/Tests/Editor/Demo/Phase8PolishAndDemoTests.cs`)
Executed 2026-10-29: full EditMode suite **41/41 passed (100% pass rate in 0.771s)**.
New automated tests:
1. `StoreManager_Distributes72Skus_AcrossAllFourAisles`: Verifies that the 72 verified catalog SKUs are distributed evenly across all 4 aisles (`18` SKUs in `A01`, `18` in `A02`, `18` in `A03`, `18` in `A04`) with 100% topology integrity.
2. `ErrorHandling_And_ShelfVisualReset_HandleInvalidInputsAndRestockCleanly`: Verifies non-throwing `OrderManager.TryStartSpecificOrder` rejection of empty/unknown SKUs, `SimulationErrorHandler` logging, and `OrderManager.ResetShelfVisuals()` restoring picked `PickableItemVisual` objects on new order dispatch.
3. `WorkStudyReportExporter_ExportsValidMarkdownAndCsvFiles`: Verifies `WorkStudyReportExporter.ExportFullReportBundle` generating Markdown (`.md`), RFC-4180 elemental events CSV (`_events.csv`), and summary KPI CSV (`_summary.csv`).
4. `PerformanceAudit_EnforcesRtx2050Budget_WithoutFabricatedBenchmarks`: Opens `Assets/Scenes/DarkStore_Phase1.unity`, runs `PerformanceSettingsManager.AuditScenePerformance()`, verifies `148` renderers ($\le 300$), `16/16` GPU-instanced shared materials, `1` shadow-casting directional light + `8` shadowless point lights, camera far clip `45.0 m`, URP shadow distance `32.0 m`, `2` shadow cascades, post-processing disabled, and exports `docs/evidence/phase8/rtx2050_performance_audit.md` with interactive GPU FPS benchmarking marked `NOT RUN`.
5. `FinalDemoWorkflow_ExecutesAll7Steps_ExportsReport_AndComparesScenarios`: Executes the complete 7-step Final Demo workflow (`1. SelectScenario` -> `2. SelectOrder` -> `3. StartMode` -> `4. PickProducts` -> `5. CompleteOrder` -> `6. ViewWorkStudyResults` -> `7. CompareAnotherScenario`), exports `docs/evidence/phase8/demo_order_work_study_report.*` (`38.00 s`, `33.18 m`, `18` events, `100.0%` reconciled), and compares `SCN-DEMO-A` ($49.30\text{ s}, 53.20\text{ m}$) vs `SCN-DEMO-B` ($65.44\text{ s}, 75.80\text{ m}$) in `docs/evidence/phase8/demo_comparison_A_vs_B.md`.


