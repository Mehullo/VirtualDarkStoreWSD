# Operational Work-Study Comparison: Baseline vs Optimized Route

> **Methodology Notice**: All figures in this comparison are MEASURED SIMULATION TELEMETRY derived deterministically from recorded virtual-picker operational cycles. They are not predictions or real-world measurements. This is a single-order deterministic work-study comparison; no statistical significance is claimed without a multi-order sample.

## 1. Study & Order Configuration
| Parameter | Baseline (Scenario A) | Optimized (Scenario B) |
|---|---|---|
| **Order ID** | `ORD-STUDY-001` | `ORD-STUDY-001` |
| **SKU Count** | `4` items | `4` items |
| **Pick Sequence** | SKU-0001 → SKU-0039 → SKU-0008 → SKU-0066 | SKU-0001 → SKU-0008 → SKU-0039 → SKU-0066 |
| **Optimization Mode** | Baseline (Original Sequence) | Google OR-Tools Local Solver (Guided Local Search) |
| **Recorded At** | 2026-11-02T21:06:41.105+05:30 | 2026-11-02T21:06:42.133+05:30 |
| **Equivalent Conditions** | **YES** | - |

## 2. Operational Cycle KPI Comparison
| Metric | Unit | Baseline (A) | Optimized (B) | Delta (B − A) | Delta % |
|---|---|---:|---:|---:|---:|
| Total Operational Cycle Time | s | 80.54 | 63.97 | -16.57 | -20.6% |
| Walking / Transit Time | s | 68.64 | 52.07 | -16.57 | -24.1% |
| Searching Shelf Time | s | 5.60 | 5.60 | 0.00 | 0.0% |
| Item Picking Time | s | 2.00 | 2.00 | 0.00 | 0.0% |
| Waiting / Desk Time | s | 1.50 | 1.50 | 0.00 | 0.0% |
| Packing & Deposit Time | s | 1.90 | 1.90 | 0.00 | 0.0% |
| Order Completed Element | s | 0.80 | 0.80 | 0.00 | 0.0% |
| Measured Picker Distance | m | 96.38 | 73.18 | -23.20 | -24.1% |
| Algorithmic Route Cost (Solver Matrix) | m | 65.20 | 34.29 | -30.91 | -47.4% |
| Successful Item Picks | count | 4.00 | 4.00 | 0.00 | 0.0% |
| Distinct Shelf Bays Visited | count | 4.00 | 4.00 | 0.00 | 0.0% |
| Inter-Aisle Transitions | count | 3.00 | 2.00 | -1.00 | -33.3% |
| Aisle Backtracks (Revisited Aisle) | count | 1.00 | 0.00 | -1.00 | -100.0% |

## 3. Activity Distribution Comparison (Share of Cycle Time)
| Activity Category | Unit | Baseline (A) | Optimized (B) | Delta (B − A) |
|---|---|---:|---:|---:|
| Walking / Transit | % of time | 85.3% | 81.5% | -3.8% |
| Searching Shelf | % of time | 7.0% | 8.8% | +1.8% |
| Item Picking | % of time | 2.5% | 3.1% | +0.6% |
| Waiting / Desk | % of time | 1.9% | 2.3% | +0.5% |
| Packing & Deposit | % of time | 2.4% | 3.0% | +0.6% |
| Order Completed | % of time | 1.0% | 1.3% | +0.3% |

## 4. Key Work-Study Observations
- **Cycle Time Reduction**: `16.57 s saved`.
- **Physical Travel Reduction**: `23.20 m` measured distance reduction (`24.1%` travel savings).
- **Aisle Backtracking**: Reduced from `1` to `0` backtracks.
- **Aisle Transitions**: Changed from `3` to `2` inter-aisle crossings.
- **Order Completion**: Both baseline and optimized routes fulfilled all required SKUs and deposited the order at `PACK-01`.

> *Note: Results represent deterministic measurement of this specific order layout. Statistical significance across broader order distributions requires multi-batch sampling.*
