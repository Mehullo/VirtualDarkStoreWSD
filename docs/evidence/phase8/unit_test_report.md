# Industrial Work-Study & Work-Sampling Report: ORD-RPT-01

> **Methodology Notice**: All figures in this report are MEASURED SIMULATION TELEMETRY derived deterministically from recorded WorkEvent logs. No AI/ML prediction, synthetic dataset generation, or uninstrumented physiological/eye-gaze estimation is used.

## 1. Session & Study Metadata
| Field | Value |
|---|---|
| **Order ID** | `ORD-RPT-01` |
| **Scenario ID** | `SCN-TEST` |
| **Execution Mode** | `Desktop` |
| **Total Logged Events** | `7` |
| **Work-Sampling Interval** | `1.00 s` (`17` observations) |
| **Mathematical Reconciliation** | **PASS (100.0% Reconciled)** |

## 2. Executive KPI Summary
| KPI Metric | Value | Unit |
|---|---:|---|
| **Total Task Time** | `16.30` | `s` |
| **Total Distance Traveled** | `18.00` | `m` |
| **Successful Item Picks** | `1` | `items` |
| **Distinct Shelf Bays Visited** | `1` | `bays` |
| **Mean Cycle Time per Pick** | `16.30` | `s/item` |
| **Mean Active Walking Speed** | `1.80` | `m/s` |

## 3. Continuous Time-Study Elemental Breakdown
| Activity Category | Duration (s) | Share of Task Time (%) |
|---|---:|---:|
| **Walking / Travel (Total)** | `10.00` | `61.3%` |
|   ↳ *Moving Between Locations (Macro Transit)* | `10.00` | `61.3%` |
|   ↳ *Approaching Target Zone (<= 3.0m)* | `0.00` | `0.0%` |
|   ↳ *General Unassigned Walking* | `0.00` | `0.0%` |
| **Searching Shelf (Stationary Orientation)** | `2.00` | `12.3%` |
| **Item Picking (Validated SKU Grasp)** | `0.50` | `3.1%` |
| **Waiting / Idle** | `1.00` | `6.1%` |
| **Order Packing (At Dispatch Bench)** | `2.00` | `12.3%` |
| **Order Completed (Dispatch Finalization)** | `0.80` | `4.9%` |
| **TOTAL RECONCILED** | **`16.30`** | **`100.0%`** |

## 4. Interval Work-Sampling Observations
| Activity State | Sample Observations (n) | Sample Proportion (%) |
|---|---:|---:|
| `Waiting` | `2` | `11.8%` |
| `Searching` | `2` | `11.8%` |
| `MovingBetweenLocations` | `10` | `58.8%` |
| `Packing` | `2` | `11.8%` |
| `OrderCompleted` | `1` | `5.9%` |
| **Total Observations (N)** | **`17`** | **`100.0%`** |

## 5. Chronological Elemental Event Log
| Event ID | Activity | Start (s) | End (s) | Duration (s) | Distance (m) | Location | SKU | Order | World Position (x, y, z) |
|---|---|---:|---:|---:|---:|---|---|---|---|
| `EVT-0001` | `Waiting` | 0.00 | 1.00 | 1.00 | 0.00 | `PACK-01` | `-` | `ORD-RPT-01` | `(0.0, 0.0, -8.5)` |
| `EVT-0002` | `MovingBetweenLocations` | 1.00 | 6.00 | 5.00 | 9.00 | `A01-L-B01-T01-S01` | `SKU-0001` | `ORD-RPT-01` | `(-5.2, 0.0, 0.0)` |
| `EVT-0003` | `Searching` | 6.00 | 8.00 | 2.00 | 0.00 | `A01-L-B01-T01-S01` | `SKU-0001` | `ORD-RPT-01` | `(-5.2, 0.0, 0.0)` |
| `EVT-0004` | `Picking` | 8.00 | 8.50 | 0.50 | 0.00 | `A01-L-B01-T01-S01` | `SKU-0001` | `ORD-RPT-01` | `(-5.2, 0.0, 0.0)` |
| `EVT-0005` | `MovingBetweenLocations` | 8.50 | 13.50 | 5.00 | 9.00 | `PACK-01` | `-` | `ORD-RPT-01` | `(0.0, 0.0, -8.5)` |
| `EVT-0006` | `Packing` | 13.50 | 15.50 | 2.00 | 0.00 | `PACK-01` | `-` | `ORD-RPT-01` | `(0.0, 0.0, -8.5)` |
| `EVT-0007` | `OrderCompleted` | 15.50 | 16.30 | 0.80 | 0.00 | `PACK-01` | `-` | `ORD-RPT-01` | `(0.0, 0.0, -8.5)` |
