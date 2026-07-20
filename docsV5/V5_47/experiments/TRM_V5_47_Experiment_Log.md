# V5.47 Experiment Log | **Created:** 2026-07-19
| Suite | Status | Tests | Outcome |
|-------|--------|-------|---------|
| TSP | COMPLETE | 1 (TSP_01) | Model A ROBUST — T0 inherited spread dominates, N=75 bulk-wide at origin |
| TSE | PLANNED | — | — |
| TSA | PLANNED | — | — |
| TSI | PLANNED | — | — |
| TSS | PLANNED | — | — |

## TSP_01 — T0 Spread Origin Protocol (2026-07-20)
- **Passed.** 50s execution, 63 profiles across N={67,70,72,75}.
- T0→entry IQR corr = 1.000 (confirms V5.46)
- N=75 T0 IQR = 1.200, IQR/range = 0.93 → bulk-wide (not tail-driven)
- Warmup trace: N=75 NOT broad at epoch-0 (IQR=0.004), becomes broad at T0
- d/K diagnostic: d0→T0 = -0.351, km0→T0 = 0.550, lam0→T0 = 0.552
- Model A survives without N=75 (T0→entry = 0.740)
- Stop-Low: 41 profiles at c3≤0.1, 0 rescues → SAFE
