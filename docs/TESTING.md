# Testing Strategy & Verification Records

## 1. Verified Test Execution Status
> [!NOTE]
> **Current Status**: **84 TESTS EXECUTED AND PASSED** (Configurable Multi-Order Simulation & Shelf Population Verification).
> All 84 EditMode tests passed with 0 failures and 0 warnings on 2026-11-02 (`9.56s`). Evidence recorded in `TestResults-MultiOrderCheck2.xml`.

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

## 9. Phase 10 Structured Store Location & SKU Mapping Tests (`Assets/Tests/Editor/Domain/StructuredStoreLocationTests.cs`)
Executed 2026-11-02: full EditMode suite **47/47 passed (100% pass rate in 0.799s)**.
New automated tests:
1. `UniqueLocationIds_AllSlotsInStore_HaveDistinctFormattedIds`: Verifies across 4-aisle baseline (288 slots) and 6-aisle quick-commerce configurations (432 slots) that all slot IDs conform to `A{aisle:D2}-{side}-B{bay:D2}-T{tier:D2}-S{slot:D2}`, parse into `StoreLocation`, and are strictly unique.
2. `ValidSkuMappings_AllSkus_MapToOneToOneValidPhysicalLocation`: Verifies that all 72 catalog SKUs map 1:1 to unique physical slots with bidirectional consistency (`skuToSlotMap` and `slotToSkuMap`), `slot.IsOccupied == true`, and `ValidateSkuLocationMappings` returns 0 errors.
3. `WorldPositionResolution_SkuAndLocationId_ResolveToValidWorldCoordinates`: Verifies `TryResolveSkuLocation` and `TryResolveLocationPosition` return identical physical coordinates with non-NaN, finite values elevated from the floor ($y > 0.2\text{ m}, y < 3.0\text{ m}$) within the store perimeter.
4. `MissingLocation_NonExistentSkuOrLocationId_ReturnsFalseWithoutThrowing`: Verifies that querying nonexistent, null, or empty SKU IDs or Location IDs returns `false` with `Vector3.zero` and does not throw unhandled exceptions.
5. `DuplicateLocation_DetectsCollision_WhenTwoSkusAssignedSameSlot`: Verifies that simulated slot collisions where two distinct SKUs map to the same slot are immediately caught and flagged by `ValidateSkuLocationMappings`.
6. `InvalidMapping_MalformedLocationStrings_AreDetectedAndRejected`: Verifies that `StoreLocation.TryParse` and `StoreManager.ValidateLocationId` reject invalid prefixes, wrong side characters ('X', 'M'), non-numeric bays/tiers/slots, and out-of-range indices with informative error messages.

## 10. Phase 11 Controlled Order Generation Tests (`Assets/Tests/Editor/Domain/ControlledOrderGenerationTests.cs`)
Executed 2026-11-02: full EditMode suite **56/56 passed (100% pass rate)**.
New automated tests:
1. `SameSeed_ProducesIdenticalOrder`: Verifies two runs with same seed (`42001`) produce identical SKU IDs, quantities, order ID, and physical locations.
2. `DifferentSeeds_ProduceDifferentOrders`: Verifies seeds 11111 and 99999 produce different SKU selections.
3. `AllGeneratedSkus_ExistInStoreCatalog`: Verifies every generated SKU exists in `StoreManager` and matches catalog names.
4. `AllGeneratedLocations_HaveValidWorldPositions`: Verifies every generated item has a non-empty `targetSlotId` and valid elevated 3D position within rack boundaries.
5. `DuplicatePolicy_Disallow_ProducesDistinctSkus`: Verifies that `DuplicateSkuPolicy.DisallowDuplicates` produces unique SKU selections without repeats.
6. `BatchGeneration_ProducesRequestedCountAndReproducibleSequences`: Verifies `GenerateBatch` generates the requested count with repeatable sequences.
7. `PresetOrders_RemainUnchanged`: Verifies `OrderManager.StartSpecificOrder` and legacy presets remain 100% intact and functional.
8. `GeneratedOrder_DispatchesAndExecutesSuccessfully`: Verifies dispatching a generated order activates it in `OrderManager` and allows successful pick validation.

## 11. Phase 12 Routing Optimization & Travel Cost Matrix Tests (`Assets/Tests/Editor/Routing/RoutingOptimizationTests.cs`)
Executed 2026-11-02: full EditMode suite **65/65 passed (100% pass rate in 5.98s)**.
New automated tests:
1. `CostMatrix_CalculatesDistances_WithZeroDiagonal`: Verifies square $N \times N$ matrix dimensions, zero diagonal ($Costs[i, i] == 0$), positive costs between different nodes, and `matrix.Validate(out _)` returns true.
2. `CostMatrix_HandlesUnreachableNodes_Explicitly`: Verifies that unreachable nodes are marked `float.PositiveInfinity` and `IsReachable(from, to) == false` without throwing exceptions.
3. `Routing_ValidStartAndEnd`: Verifies route starts at Node 0 (Order Desk) and terminates at Node $K+1$ (Packing Bench).
4. `Routing_EveryRequiredSkuExactlyOnce`: Verifies that every required SKU stop appears exactly once in the TSP solution without dropped or duplicated SKUs.
5. `Routing_NoUnexpectedSku`: Verifies that no foreign node indices or unknown SKUs are present in the optimized route.
6. `Routing_RouteEndsAtPacking`: Verifies that the final stop of the tour matches the physical coordinates of the packing bench (`PACK-01`).
7. `Routing_BaselineSequenceRemainsAvailable`: Verifies that `BaselineRouteOptimizer` preserves the original order sequence for A/B work-study comparison.
8. `Routing_SolverFailure_FallsBackSafely`: Verifies that in case of unreachable nodes or solver exceptions, the optimizer safely falls back to the baseline sequence without throwing.
9. `OrTools_OptimizesVisitSequence_WhenOutOfOrder`: Verifies that OR-Tools optimizes an out-of-order 3-stop corridor order with backtracking, reducing travel distance from $75.0\text{ m}$ (baseline) to $35.0\text{ m}$ (optimized), achieving $>50\%$ travel reduction with proven constraint satisfaction.

## 12. Phase 13 & Phase 14 Operational Workflow & Comparison Tests (`Assets/Tests/Editor/Scenarios/OperationalWorkflowTests.cs`)
Executed 2026-11-02: full EditMode suite **70/70 passed (100% pass rate in 7.21s)**.
New automated tests:
1. `OperationalStage_FullCycleProgression_ReachesCompleted`: Verifies the complete 7-stage state progression: `Entry` $\to$ `CollectOrder` $\to$ `Picking` $\to$ `AwaitingPacking` $\to$ `DepositOrder` $\to$ `WalkToExit` $\to$ `Completed`.
2. `OperationalEvents_RecordedMonotonically_WithZeroNegativeDurations`: Verifies that all work-study events generated across an operational cycle maintain monotonic non-decreasing start timestamps ($t_{start, i} \le t_{start, i+1}$), non-negative durations ($\Delta t \ge 0$), and non-negative distances.
3. `AisleTransitionsAndBacktracking_CalculatesAccurately`: Verifies that `OperationalCycleManager.CalculateAisleMetrics` accurately collapses consecutive picks in the same aisle, counts aisle transitions (e.g. 2 transitions for A01 $\to$ A03 $\to$ A01), and identifies aisle backtracking (1 backtrack detected on revisit to A01; 0 backtracks on monotonic sweep).
4. `OperationalCycleRunner_ExecutesEndToEndWorkflow_Deterministically`: Verifies that the scripted virtual picker executes the entire operational workflow headlessly, verifies waypoint labels (`ENTRY`, `COLLECT_ORDER`, `DEPOSIT_PACKING`, `EXIT`), and reconciles work-study metrics with 100% integrity.
5. `BaselineVsOptimized_SameOrder_IdenticalStartingConditions_VariesOnlyPickSequence`: Executes Baseline (Run A) vs OR-Tools Optimized (Run B) on order `ORD-STUDY-001`, validates 100% equivalent starting conditions, measures travel distance reduction (from 96.38m to 73.18m, -24.1%), cycle time reduction (from 80.54s to 63.97s, -20.6%), and aisle backtrack reduction (from 1 to 0), and exports evidence bundle to `docs/evidence/phase14/`.

## 13. Physical Location Labels & Store Signage Tests (`Assets/Tests/Editor/Environment/PhysicalLocationLabelTests.cs`)
Executed 2026-11-02: full EditMode suite **75/75 passed (100% pass rate in 7.78s)**.
Evidence recorded in `TestResults-FinalVerification.xml`.
New automated tests:
1. `AisleHeaderLabel_InitializesWithCorrectLocationIdAndProperties`: Verifies World Space canvas configuration, primary text (`AISLE A01`), secondary text, zero colliders (preventing NavMesh/physics interference), and non-blocking graphics (`raycastTarget = false`).
2. `BayRackPlacard_MatchesStructuredLocationHierarchy`: Verifies bay placard (`A03-L-B02` / `BAY 02 • LEFT`) matches the structured `StoreLocation` domain hierarchy and preserves Aisle, Side, and Bay properties.
3. `ShelfLevelTag_AlignsWithTierIndices`: Verifies that shelf level badges (`LEVEL 03` / `TIER 03`) align with tier indices and corresponding location IDs (`A02-R-B01-T03`).
4. `ProductSlotTicket_MatchesSimulationLocationAndSkuMapping`: Verifies that shelf-edge product tickets correspond 1:1 with catalog SKUs, match `slot.SlotId`, and parse accurately into `StoreLocation` with matching Aisle, Side, Bay, Tier, and Slot indices.
5. `PickerHoverPrompt_IncludesExactSlotLocationId`: Verifies that center-screen reticle raycast on shelf items surfaces the exact physical slot location ID in the interaction prompt (e.g. `[E / LMB] Pick: <SkuName> (<SkuId>) [<SlotId>]`).



