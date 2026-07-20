# V5.48 Experiment Log | **Created:** 2026-07-20
| Suite | Status | Tests | Outcome |
|-------|--------|-------|---------|
| DSM | COMPLETE | 1 (DSM_01) | Model B — Shape/spread, not mean state, distinguishes N-window outcome |
| DSS | PLANNED | — | — |

## DSM_01 — Distribution Shape vs Mean State Audit (2026-07-20)
- **Passed.** 50s execution, 52 profiles.
- **Decision: Model B — Shape/spread explains N-window outcome.**
- d_w2 mean: N=72=0.457, N=75=0.458 — NEARLY IDENTICAL (ratio=1.00)
- km_w2 mean: N=72=0.958, N=75=0.949 — NEARLY IDENTICAL (ratio=0.99)
- km_w2 IQR: N=72=0.087, N=75=0.129 — 1.48x difference
- lam_w2 IQR: N=72=0.086, N=75=0.127 — 1.48x difference
- **km IQR gap / mean gap = 46.1x** — shape separation dwarfs mean separation
- Mean-only N-level prediction: 0/3 accuracy
- Shape superiority persists under leave-one-N-out
- Stop-Low: 35 stop, 0 rescues → SAFE
