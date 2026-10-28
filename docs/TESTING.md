# Testing Strategy & Verification Records

## 1. Verified Test Execution Status
> [!NOTE]
> **Current Status**: **20 TESTS EXECUTED AND PASSED** (Phase 4).
> All tests passed with 0 failures on 2026-10-28. Evidence recorded in `TestResults-EditMode.xml`.

### Verified Test Run Evidence (2026-10-28 10:44:48Z)
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

- **Total Test Cases**: 20
- **Passed**: 20
- **Failed**: 0
- **Duration**: 0.309s

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
