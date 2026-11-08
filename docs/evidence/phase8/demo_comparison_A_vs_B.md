# Scenario A/B Comparison: SCN-DEMO-A vs SCN-DEMO-B

> All values are MEASURED SIMULATION RESULTS from recorded virtual-picker runs in this simulation. They are not predictions and not real-world measurements.

- A: **Demo Scenario A: 3-Item Aisle-Grouped Sequence** (`SCN-DEMO-A`), recorded 2026-11-04T18:37:45.243+05:30
  - SKU sequence: SKU-0001 → SKU-0008 → SKU-0039
- B: **Demo Scenario B: 3-Item Ungrouped Backtracking** (`SCN-DEMO-B`), recorded 2026-11-04T18:37:45.278+05:30
  - SKU sequence: SKU-0001 → SKU-0039 → SKU-0008
- Equivalent starting conditions: **YES**
- Factor varied between A and B: pick/visit sequence
- Duration/percentage reconciliation (both runs): PASS
- Events recorded: A=17, B=17

## KPI comparison (Delta = B − A)

| Metric | Unit | A | B | Delta (B−A) | Delta % |
|---|---|---:|---:|---:|---:|
| Total task time | s | 49.30 | 65.44 | +16.14 | 32.7% |
| Walking / travel time | s | 38.40 | 54.54 | +16.14 | 42.0% |
| Searching time | s | 5.70 | 5.70 | 0.00 | 0.0% |
| Picking time | s | 1.50 | 1.50 | 0.00 | 0.0% |
| Waiting time | s | 1.00 | 1.00 | 0.00 | 0.0% |
| Packing time | s | 1.90 | 1.90 | 0.00 | 0.0% |
| Order-completed element | s | 0.80 | 0.80 | 0.00 | 0.0% |
| Distance travelled | m | 53.20 | 75.80 | +22.60 | 42.5% |
| Successful picks | count | 3.00 | 3.00 | 0.00 | 0.0% |
| Shelf bay visits | count | 3.00 | 3.00 | 0.00 | 0.0% |

## Activity distribution (share of total task time)

| Metric | Unit | A | B | Delta (B−A) | Delta % |
|---|---|---:|---:|---:|---:|
| Walking / travel | % of time | 77.89 | 83.34 | +5.45 | 7.0% |
| Searching | % of time | 11.56 | 8.71 | -2.85 | -24.7% |
| Picking | % of time | 3.04 | 2.29 | -0.75 | -24.7% |
| Waiting | % of time | 2.03 | 1.53 | -0.50 | -24.7% |
| Packing | % of time | 3.85 | 2.90 | -0.95 | -24.7% |
| Order completed | % of time | 1.62 | 1.22 | -0.40 | -24.7% |

Walking / travel = General walking + Moving between locations + Approaching (as defined by `WorkStudyCalculator`).
Distance is the horizontal path length accumulated by the classifier. Routing is a fixed straight-line rule, not NavMesh pathfinding.
