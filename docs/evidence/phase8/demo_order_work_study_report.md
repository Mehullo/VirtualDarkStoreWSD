# Industrial Work-Study & Work-Sampling Report: ORD-DEMO-0001

> **Methodology Notice**: All figures in this report are MEASURED SIMULATION TELEMETRY derived deterministically from recorded WorkEvent logs. No AI/ML prediction, synthetic dataset generation, or uninstrumented physiological/eye-gaze estimation is used.

## 1. Session & Study Metadata
| Field | Value |
|---|---|
| **Order ID** | `ORD-DEMO-0001` |
| **Scenario ID** | `SCN-DEMO-A` |
| **Execution Mode** | `Desktop` |
| **Total Logged Events** | `18` |
| **Work-Sampling Interval** | `1.00 s` (`39` observations) |
| **Mathematical Reconciliation** | **PASS (100.0% Reconciled)** |

## 2. Executive KPI Summary
| KPI Metric | Value | Unit |
|---|---:|---|
| **Total Task Time** | `38.00` | `s` |
| **Total Distance Traveled** | `33.18` | `m` |
| **Successful Item Picks** | `3` | `items` |
| **Distinct Shelf Bays Visited** | `3` | `bays` |
| **Mean Cycle Time per Pick** | `12.67` | `s/item` |
| **Mean Active Walking Speed** | `1.12` | `m/s` |

## 3. Continuous Time-Study Elemental Breakdown
| Activity Category | Duration (s) | Share of Task Time (%) |
|---|---:|---:|
| **Walking / Travel (Total)** | `29.58` | `77.8%` |
|   ↳ *Moving Between Locations (Macro Transit)* | `18.58` | `48.9%` |
|   ↳ *Approaching Target Zone (<= 3.0m)* | `11.00` | `28.9%` |
|   ↳ *General Unassigned Walking* | `0.00` | `0.0%` |
| **Searching Shelf (Stationary Orientation)** | `0.00` | `0.0%` |
| **Item Picking (Validated SKU Grasp)** | `1.50` | `3.9%` |
| **Waiting / Idle** | `6.12` | `16.1%` |
| **Order Packing (At Dispatch Bench)** | `0.00` | `0.0%` |
| **Order Completed (Dispatch Finalization)** | `0.80` | `2.1%` |
| **TOTAL RECONCILED** | **`38.00`** | **`100.0%`** |

## 4. Interval Work-Sampling Observations
| Activity State | Sample Observations (n) | Sample Proportion (%) |
|---|---:|---:|
| `Waiting` | `6` | `15.4%` |
| `Approaching` | `11` | `28.2%` |
| `Picking` | `2` | `5.1%` |
| `MovingBetweenLocations` | `19` | `48.7%` |
| `OrderCompleted` | `1` | `2.6%` |
| **Total Observations (N)** | **`39`** | **`100.0%`** |

## 5. Chronological Elemental Event Log
| Event ID | Activity | Start (s) | End (s) | Duration (s) | Distance (m) | Location | SKU | Order | World Position (x, y, z) |
|---|---|---:|---:|---:|---:|---|---|---|---|
| `EVT-0001` | `Waiting` | 0.00 | 0.16 | 0.16 | 0.00 | `A01-L-B01-T01-S01` | `SKU-0001` | `ORD-DEMO-0001` | `(0.0, 0.0, -8.5)` |
| `EVT-0002` | `MovingBetweenLocations` | 0.16 | 1.00 | 0.84 | 0.00 | `A01-L-B01-T01-S01` | `SKU-0001` | `ORD-DEMO-0001` | `(0.0, 0.0, -8.5)` |
| `EVT-0003` | `Waiting` | 1.00 | 6.96 | 5.96 | 6.68 | `A01-L-B01-T01-S01` | `SKU-0001` | `ORD-DEMO-0001` | `(-6.5, 0.0, -3.2)` |
| `EVT-0004` | `Approaching` | 6.96 | 9.96 | 3.00 | 1.67 | `A01-L-B01-T01-S01` | `SKU-0001` | `ORD-DEMO-0001` | `(-6.5, 0.0, -3.2)` |
| `EVT-0005` | `Searching` | 9.96 | 9.96 | 0.00 | 0.00 | `A01-L-B03-T02-S01` | `SKU-0008` | `ORD-DEMO-0001` | `(-6.5, 0.0, -3.2)` |
| `EVT-0006` | `Picking` | 9.96 | 10.46 | 0.50 | 0.00 | `A01-L-B01-T01-S01` | `SKU-0001` | `ORD-DEMO-0001` | `(-6.5, 0.0, -3.2)` |
| `EVT-0007` | `MovingBetweenLocations` | 10.46 | 16.18 | 5.71 | 6.40 | `A01-L-B03-T02-S01` | `SKU-0008` | `ORD-DEMO-0001` | `(-6.5, 0.0, 4.8)` |
| `EVT-0008` | `Approaching` | 16.18 | 19.18 | 3.00 | 1.60 | `A01-L-B03-T02-S01` | `SKU-0008` | `ORD-DEMO-0001` | `(-6.5, 0.0, 4.8)` |
| `EVT-0009` | `Searching` | 19.18 | 19.18 | 0.00 | 0.00 | `A03-L-B01-T03-S01` | `SKU-0039` | `ORD-DEMO-0001` | `(-6.5, 0.0, 4.8)` |
| `EVT-0010` | `Picking` | 19.18 | 19.68 | 0.50 | 0.00 | `A01-L-B03-T02-S01` | `SKU-0008` | `ORD-DEMO-0001` | `(-6.5, 0.0, 4.8)` |
| `EVT-0011` | `MovingBetweenLocations` | 19.68 | 27.76 | 8.08 | 9.05 | `A03-L-B01-T03-S01` | `SKU-0039` | `ORD-DEMO-0001` | `(1.6, 0.0, -3.2)` |
| `EVT-0012` | `Approaching` | 27.76 | 30.76 | 3.00 | 2.26 | `A03-L-B01-T03-S01` | `SKU-0039` | `ORD-DEMO-0001` | `(1.6, 0.0, -3.2)` |
| `EVT-0013` | `Searching` | 30.76 | 30.76 | 0.00 | 0.00 | `PACK-01` | `-` | `ORD-DEMO-0001` | `(1.6, 0.0, -3.2)` |
| `EVT-0014` | `Picking` | 30.76 | 31.26 | 0.50 | 0.00 | `A03-L-B01-T03-S01` | `SKU-0039` | `ORD-DEMO-0001` | `(1.6, 0.0, -3.2)` |
| `EVT-0015` | `MovingBetweenLocations` | 31.26 | 35.20 | 3.94 | 5.52 | `PACK-01` | `-` | `ORD-DEMO-0001` | `(0.0, 0.0, -8.5)` |
| `EVT-0016` | `Approaching` | 35.20 | 37.20 | 2.00 | 0.00 | `PACK-01` | `-` | `ORD-DEMO-0001` | `(0.0, 0.0, -8.5)` |
| `EVT-0017` | `Packing` | 37.20 | 37.20 | 0.00 | 0.00 | `GENERAL_FLOOR` | `-` | `ORD-DEMO-0001` | `(0.0, 0.0, -8.5)` |
| `EVT-0018` | `OrderCompleted` | 37.20 | 38.00 | 0.80 | 0.00 | `PACK-01` | `-` | `ORD-DEMO-0001` | `(0.0, 0.0, -8.5)` |
