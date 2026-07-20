# V5.47 Experiment Log | **Created:** 2026-07-19
| Suite | Status | Tests | Outcome |
|-------|--------|-------|---------|
| TSP | COMPLETE | 1 (TSP_01) | Model A ROBUST — T0 inherited spread dominates, N=75 bulk-wide at origin |
| TSE | COMPLETE | 1 (TSE_01) | Model A — w2→T0 handoff amplifies N=75 bulk spread (1.98x) |
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

## TSE_01 — Spread Evolution Post-Warmup Handoff Audit (2026-07-20)
- **Passed.** 51s execution, 63 profiles.
- **Decision: Model A — Post-warmup handoff w2→T0 amplifies N=75 bulk spread.**
- N=75 w2 IQR = 0.604, w2 IQR/range = 0.20 (NOT bulk-wide at w2)
- N=75 T0 IQR = 1.200, IQR/range = 0.93 (BECOMES bulk-wide at T0)
- w2→T0 amplification = 1.98x for N=75 (vs 0.11x for N=72)
- N=72 IS bulk-wide at w2 (IQR/range=0.31) but COLLAPSES at T0 (IQR=0.047)
- w2→T0 amp ~ T0 IQR corr = 0.823 (strong diagnostic)
- Stage-by-stage omega IQR mapped for all 7 checkpoints
- Full d/K/lambda per-stage instrumentation added
- Stop-Low: 41 stop, 0 damage → SAFE
- Model A robust without N=75 (T0→entry = 0.740)
- V6 NOT READY. Causal closure not claimed.
