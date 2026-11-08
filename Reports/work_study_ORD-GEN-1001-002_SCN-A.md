# Industrial Work-Study & Work-Sampling Report: ORD-GEN-1001-002

> **Methodology Notice**: All figures in this report are MEASURED SIMULATION TELEMETRY derived deterministically from recorded WorkEvent logs. No AI/ML prediction, synthetic dataset generation, or uninstrumented physiological/eye-gaze estimation is used.

## 1. Session & Study Metadata
| Field | Value |
|---|---|
| **Order ID** | `ORD-GEN-1001-002` |
| **Scenario ID** | `SCN-A` |
| **Execution Mode** | `Desktop` |
| **Total Logged Events** | `43` |
| **Work-Sampling Interval** | `1.00 s` (`38` observations) |
| **Mathematical Reconciliation** | **PASS (100.0% Reconciled)** |

## 2. Executive KPI Summary
| KPI Metric | Value | Unit |
|---|---:|---|
| **Total Task Time** | `37.80` | `s` |
| **Total Distance Traveled** | `41.20` | `m` |
| **Successful Item Picks** | `4` | `items` |
| **Distinct Shelf Bays Visited** | `5` | `bays` |
| **Mean Cycle Time per Pick** | `9.45` | `s/item` |
| **Mean Active Walking Speed** | `2.39` | `m/s` |

## 3. Continuous Time-Study Elemental Breakdown
| Activity Category | Duration (s) | Share of Task Time (%) |
|---|---:|---:|
| **Walking / Travel (Total)** | `17.22` | `45.5%` |
|   ↳ *Moving Between Locations (Macro Transit)* | `15.02` | `39.7%` |
|   ↳ *Approaching Target Zone (<= 3.0m)* | `2.20` | `5.8%` |
|   ↳ *General Unassigned Walking* | `0.00` | `0.0%` |
| **Searching Shelf (Stationary Orientation)** | `4.90` | `13.0%` |
| **Item Picking (Validated SKU Grasp)** | `2.00` | `5.3%` |
| **Waiting / Idle** | `12.17` | `32.2%` |
| **Order Packing (At Dispatch Bench)** | `0.72` | `1.9%` |
| **Order Completed (Dispatch Finalization)** | `0.80` | `2.1%` |
| **TOTAL RECONCILED** | **`37.80`** | **`100.0%`** |

## 4. Interval Work-Sampling Observations
| Activity State | Sample Observations (n) | Sample Proportion (%) |
|---|---:|---:|
| `Waiting` | `13` | `34.2%` |
| `Searching` | `7` | `18.4%` |
| `Approaching` | `2` | `5.3%` |
| `MovingBetweenLocations` | `15` | `39.5%` |
| `OrderCompleted` | `1` | `2.6%` |
| **Total Observations (N)** | **`38`** | **`100.0%`** |

## 5. Chronological Elemental Event Log
| Event ID | Activity | Start (s) | End (s) | Duration (s) | Distance (m) | Location | SKU | Order | World Position (x, y, z) |
|---|---|---:|---:|---:|---:|---|---|---|---|
| `EVT-0001` | `Waiting` | 224.38 | 224.38 | 0.00 | 0.00 | `A01-L-B01-T03-S01` | `SKU-0003` | `ORD-SCN-0001` | `(0.0, 0.0, -8.5)` |
| `EVT-0002` | `MovingBetweenLocations` | 224.38 | 224.38 | 0.00 | 0.00 | `A01-L-B01-T03-S01` | `SKU-0003` | `ORD-SCN-0001` | `(0.0, 0.0, -8.5)` |
| `EVT-0003` | `Waiting` | 224.38 | 225.76 | 1.38 | 0.00 | `A01-L-B01-T03-S01` | `SKU-0003` | `ORD-SCN-0001` | `(0.0, 0.1, -8.5)` |
| `EVT-0004` | `MovingBetweenLocations` | 225.76 | 227.86 | 2.10 | 7.89 | `A01-L-B01-T03-S01` | `SKU-0003` | `ORD-SCN-0001` | `(-6.8, 0.1, -4.6)` |
| `EVT-0005` | `Approaching` | 227.86 | 228.11 | 0.25 | 0.35 | `A01-L-B01-T03-S01` | `SKU-0003` | `ORD-SCN-0001` | `(-7.1, 0.1, -4.3)` |
| `EVT-0006` | `Searching` | 228.11 | 228.63 | 0.52 | 0.00 | `A02-R-B01-T03-S01` | `SKU-0030` | `ORD-SCN-0001` | `(-7.1, 0.1, -4.3)` |
| `EVT-0007` | `Picking` | 228.63 | 229.13 | 0.50 | 0.00 | `A01-L-B01-T03-S01` | `SKU-0003` | `ORD-SCN-0001` | `(-7.1, 0.1, -4.3)` |
| `EVT-0008` | `MovingBetweenLocations` | 229.13 | 229.13 | 0.00 | 0.00 | `A02-R-B01-T03-S01` | `SKU-0030` | `ORD-SCN-0001` | `(-7.1, 0.1, -4.3)` |
| `EVT-0009` | `Waiting` | 229.13 | 233.46 | 4.33 | 0.00 | `A02-R-B01-T03-S01` | `SKU-0030` | `ORD-SCN-0001` | `(-7.1, 0.1, -4.3)` |
| `EVT-0010` | `MovingBetweenLocations` | 233.46 | 234.53 | 1.07 | 2.40 | `A02-R-B01-T03-S01` | `SKU-0030` | `ORD-SCN-0001` | `(-5.3, 0.1, -4.4)` |
| `EVT-0011` | `Approaching` | 234.53 | 235.08 | 0.55 | 1.10 | `A02-R-B01-T03-S01` | `SKU-0030` | `ORD-SCN-0001` | `(-4.6, 0.1, -3.5)` |
| `EVT-0012` | `Searching` | 235.08 | 237.79 | 2.72 | 0.00 | `A02-R-B01-T03-S01` | `SKU-0030` | `ORD-SCN-0001` | `(-4.6, 0.1, -3.5)` |
| `EVT-0013` | `Waiting` | 237.79 | 238.28 | 0.48 | 0.00 | `A02-R-B01-T03-S01` | `SKU-0030` | `ORD-SCN-0001` | `(-4.6, 0.1, -3.5)` |
| `EVT-0014` | `Searching` | 238.28 | 239.43 | 1.15 | 0.00 | `A03-L-B03-T03-S01` | `SKU-0045` | `ORD-SCN-0001` | `(-4.6, 0.1, -3.5)` |
| `EVT-0015` | `Picking` | 239.43 | 239.93 | 0.50 | 0.00 | `A02-R-B01-T03-S01` | `SKU-0030` | `ORD-SCN-0001` | `(-4.6, 0.1, -3.5)` |
| `EVT-0016` | `MovingBetweenLocations` | 239.93 | 239.93 | 0.00 | 0.00 | `A03-L-B03-T03-S01` | `SKU-0045` | `ORD-SCN-0001` | `(-4.6, 0.1, -3.5)` |
| `EVT-0017` | `Waiting` | 239.93 | 240.23 | 0.30 | 0.00 | `A03-L-B03-T03-S01` | `SKU-0045` | `ORD-SCN-0001` | `(-4.6, 0.1, -3.5)` |
| `EVT-0018` | `MovingBetweenLocations` | 240.23 | 241.59 | 1.37 | 2.84 | `A03-L-B03-T03-S01` | `SKU-0045` | `ORD-SCN-0001` | `(-2.2, 0.1, -4.5)` |
| `EVT-0019` | `Waiting` | 241.59 | 242.11 | 0.52 | 0.00 | `A03-L-B03-T03-S01` | `SKU-0045` | `ORD-SCN-0001` | `(-2.2, 0.1, -4.5)` |
| `EVT-0020` | `MovingBetweenLocations` | 242.11 | 242.99 | 0.88 | 1.75 | `A03-L-B03-T03-S01` | `SKU-0045` | `ORD-SCN-0001` | `(-1.4, 0.1, -3.0)` |
| `EVT-0021` | `Waiting` | 242.99 | 243.64 | 0.65 | 0.00 | `A03-L-B03-T03-S01` | `SKU-0045` | `ORD-SCN-0001` | `(-1.4, 0.1, -3.0)` |
| `EVT-0022` | `MovingBetweenLocations` | 243.64 | 246.18 | 2.53 | 6.08 | `A03-L-B03-T03-S01` | `SKU-0045` | `ORD-SCN-0001` | `(-1.6, 0.1, 2.8)` |
| `EVT-0023` | `Approaching` | 246.18 | 246.56 | 0.38 | 0.69 | `A03-L-B03-T03-S01` | `SKU-0045` | `ORD-SCN-0001` | `(-1.8, 0.1, 3.4)` |
| `EVT-0024` | `Searching` | 246.56 | 246.76 | 0.20 | 0.00 | `A04-R-B01-T03-S01` | `SKU-0066` | `ORD-SCN-0001` | `(-1.8, 0.1, 3.4)` |
| `EVT-0025` | `Picking` | 246.76 | 247.26 | 0.50 | 0.00 | `A03-L-B03-T03-S01` | `SKU-0045` | `ORD-SCN-0001` | `(-1.8, 0.1, 3.4)` |
| `EVT-0026` | `MovingBetweenLocations` | 247.26 | 247.26 | 0.00 | 0.00 | `A04-R-B01-T03-S01` | `SKU-0066` | `ORD-SCN-0001` | `(-1.8, 0.1, 3.4)` |
| `EVT-0027` | `Waiting` | 247.26 | 247.48 | 0.22 | 0.00 | `A04-R-B01-T03-S01` | `SKU-0066` | `ORD-SCN-0001` | `(-1.8, 0.1, 3.4)` |
| `EVT-0028` | `MovingBetweenLocations` | 247.48 | 250.51 | 3.03 | 6.98 | `A04-R-B01-T03-S01` | `SKU-0066` | `ORD-SCN-0001` | `(-1.9, 0.1, -2.3)` |
| `EVT-0029` | `Waiting` | 250.51 | 251.06 | 0.55 | 0.00 | `A04-R-B01-T03-S01` | `SKU-0066` | `ORD-SCN-0001` | `(-1.9, 0.1, -2.3)` |
| `EVT-0030` | `MovingBetweenLocations` | 251.06 | 252.63 | 1.57 | 3.42 | `A04-R-B01-T03-S01` | `SKU-0066` | `ORD-SCN-0001` | `(-1.0, 0.1, -5.5)` |
| `EVT-0031` | `Waiting` | 252.63 | 253.73 | 1.10 | 0.00 | `A04-R-B01-T03-S01` | `SKU-0066` | `ORD-SCN-0001` | `(-1.0, 0.1, -5.5)` |
| `EVT-0032` | `MovingBetweenLocations` | 253.73 | 254.64 | 0.92 | 2.10 | `A04-R-B01-T03-S01` | `SKU-0066` | `ORD-SCN-0001` | `(0.8, 0.1, -4.5)` |
| `EVT-0033` | `Approaching` | 254.64 | 255.08 | 0.43 | 0.82 | `A04-R-B01-T03-S01` | `SKU-0066` | `ORD-SCN-0001` | `(1.4, 0.1, -4.0)` |
| `EVT-0034` | `Searching` | 255.08 | 255.39 | 0.32 | 0.00 | `PACK-01` | `-` | `ORD-SCN-0001` | `(1.4, 0.1, -4.0)` |
| `EVT-0035` | `Picking` | 255.39 | 255.89 | 0.50 | 0.00 | `A04-R-B01-T03-S01` | `SKU-0066` | `ORD-SCN-0001` | `(1.4, 0.1, -4.0)` |
| `EVT-0036` | `MovingBetweenLocations` | 255.89 | 255.89 | 0.00 | 0.00 | `PACK-01` | `-` | `ORD-SCN-0001` | `(1.4, 0.1, -4.0)` |
| `EVT-0037` | `Waiting` | 255.89 | 258.53 | 2.63 | 0.00 | `PACK-01` | `-` | `ORD-SCN-0001` | `(1.4, 0.1, -4.0)` |
| `EVT-0038` | `MovingBetweenLocations` | 258.53 | 260.08 | 1.55 | 3.56 | `PACK-01` | `-` | `ORD-SCN-0001` | `(2.1, 0.1, -7.0)` |
| `EVT-0039` | `Approaching` | 260.08 | 260.66 | 0.58 | 1.19 | `PACK-01` | `-` | `ORD-SCN-0001` | `(2.0, 0.1, -8.2)` |
| `EVT-0040` | `Packing` | 260.66 | 261.38 | 0.72 | 0.00 | `GENERAL_FLOOR` | `-` | `ORD-SCN-0001` | `(2.0, 0.1, -8.2)` |
| `EVT-0041` | `OrderCompleted` | 261.38 | 262.18 | 0.80 | 0.00 | `PACK-01` | `-` | `ORD-SCN-0001` | `(2.0, 0.1, -8.2)` |
| `EVT-0042` | `Waiting` | 262.18 | 262.18 | 0.00 | 0.00 | `A01-R-B03-T03-S02` | `SKU-0126` | `ORD-GEN-1001-002` | `(2.0, 0.1, -8.2)` |
| `EVT-0043` | `MovingBetweenLocations` | 262.18 | 262.18 | 0.00 | 0.00 | `A01-R-B03-T03-S02` | `SKU-0126` | `ORD-GEN-1001-002` | `(2.0, 0.1, -8.2)` |
