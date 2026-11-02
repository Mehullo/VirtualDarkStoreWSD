# Performance Configuration & Scene Budget Audit (RTX 2050-Class Hardware)

> **No-Hallucination Notice**: This report records only directly inspected scene/pipeline configurations and object counts. Interactive GPU frame-rate (FPS) benchmarking is marked **NOT RUN** where physical interactive profiling was not executed; no FPS or benchmark numbers are fabricated.

- **Target Hardware Class**: RTX 2050-Class Budget Profile (4GB VRAM / 64-bit Bus)
- **Interactive GPU FPS Benchmark Status**: **NOT RUN (Headless/configuration audit only; zero fabricated FPS or benchmark numbers)**
- **Budget Compliance**: **PASS**

## Inspected Rendering & Scene Parameters
| Parameter | Inspected Value | Target Budget (RTX 2050-Class) | Status |
|---|---:|---:|---|
| SRP Batcher Enabled | `True` | `True` | PASS |
| Total Mesh Renderers | `284` | `<= 300` | PASS |
| Unique Shared Materials | `17` | `<= 25` | PASS |
| GPU-Instanced Materials | `17 / 17` | `100%` | PASS |
| Realtime Lights | `10` | `<= 12` | PASS |
| Shadow-Casting Lights | `1` | `<= 1` (Directional Only) | PASS |
| Camera Far Clip Plane | `45.0 m` | `<= 55.0 m` | PASS |
| URP Shadow Distance | `32.0 m` | `<= 40.0 m` | PASS |
| URP Shadow Cascades | `2` | `<= 2` | PASS |
| URP MSAA Sample Count | `2x` | `<= 2x` | PASS |
| Camera Post-Processing | `Disabled` | `Disabled` (Fill-rate conservation) | PASS |

## Optimization Notes
- SRP Batcher and GPU Instancing enabled across all shared URP Lit materials; zero per-instance material copies.
- Opaque/Depth texture pre-passes and full-screen post-processing disabled to conserve 64-bit memory bus bandwidth on RTX 2050-class GPUs.
- Shadow cascades reduced to 2 and shadow distance capped at 32m (covering the 24m x 28m warehouse envelope).
- All ceiling high-bay point lights configured with LightShadows.None; only the main Directional Light casts shadows.
