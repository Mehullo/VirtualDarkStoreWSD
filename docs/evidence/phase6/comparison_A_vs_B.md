# Scenario A/B Comparison: SCN-A vs SCN-B

> All values are MEASURED SIMULATION RESULTS from recorded virtual-picker runs in this simulation. They are not predictions and not real-world measurements.

- A: **Scenario A: catalogue-number sequence** (`SCN-A`), recorded 2026-11-02T21:06:42.500+05:30
  - SKU sequence: SKU-0003 → SKU-0030 → SKU-0045 → SKU-0066
- B: **Scenario B: alternate sequence** (`SCN-B`), recorded 2026-11-02T21:06:42.524+05:30
  - SKU sequence: SKU-0003 → SKU-0045 → SKU-0030 → SKU-0066
- Equivalent starting conditions: **YES**
- Factor varied between A and B: pick/visit sequence
- Duration/percentage reconciliation (both runs): PASS
- Events recorded: A=21, B=21

## KPI comparison (Delta = B − A)

| Metric | Unit | A | B | Delta (B−A) | Delta % |
|---|---|---:|---:|---:|---:|
| Total task time | s | 72.66 | 78.37 | +5.71 | 7.9% |
| Walking / travel time | s | 59.36 | 65.07 | +5.71 | 9.6% |
| Searching time | s | 7.60 | 7.60 | 0.00 | 0.0% |
| Picking time | s | 2.00 | 2.00 | 0.00 | 0.0% |
| Waiting time | s | 1.00 | 1.00 | 0.00 | 0.0% |
| Packing time | s | 1.90 | 1.90 | 0.00 | 0.0% |
| Order-completed element | s | 0.80 | 0.80 | 0.00 | 0.0% |
| Distance travelled | m | 82.40 | 90.40 | +8.00 | 9.7% |
| Successful picks | count | 4.00 | 4.00 | 0.00 | 0.0% |
| Shelf bay visits | count | 4.00 | 4.00 | 0.00 | 0.0% |

## Activity distribution (share of total task time)

| Metric | Unit | A | B | Delta (B−A) | Delta % |
|---|---|---:|---:|---:|---:|
| Walking / travel | % of time | 81.69 | 83.03 | +1.33 | 1.6% |
| Searching | % of time | 10.46 | 9.70 | -0.76 | -7.3% |
| Picking | % of time | 2.75 | 2.55 | -0.20 | -7.3% |
| Waiting | % of time | 1.38 | 1.28 | -0.10 | -7.3% |
| Packing | % of time | 2.62 | 2.42 | -0.19 | -7.3% |
| Order completed | % of time | 1.10 | 1.02 | -0.08 | -7.3% |

Walking / travel = General walking + Moving between locations + Approaching (as defined by `WorkStudyCalculator`).
Distance is the horizontal path length accumulated by the classifier. Routing is a fixed straight-line rule, not NavMesh pathfinding.
