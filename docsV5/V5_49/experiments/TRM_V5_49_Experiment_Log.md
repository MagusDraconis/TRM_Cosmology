# V5.49 Experiment Log | **Created:** 2026-07-20
| Suite | Status | Tests | Outcome |
|-------|--------|-------|---------|
| DSG | COMPLETE | 1 (DSG_01) | Model C — Spread generated in w1→w2 transition. N=72 collapses while N=75 grows. |
| DSS | PLANNED | — | — |

## DSG_01 — Spread Generation Audit (2026-07-20)
- **Passed.** 48s execution, 52 profiles.
- **Decision: Model C — Spread generated in a specific warmup transition.**
- km IQR 75/72 ratio FLIPS: w0=0.42x → w1=0.56x → w2=1.48x
- **N=75 is NARROWER than N=72 at w0** (km IQR 0.024 vs 0.057)
- N=72 w1→w2: IQR collapses 0.154→0.087 (0.57x)
- N=75 w1→w2: IQR grows 0.086→0.129 (1.50x)
- Spread difference emerges from N=72 collapse + N=75 growth
- N=75 shape: LATE BROADENING (w0 I/R=0.15 → w2 I/R=0.43)
- N=72 shape: EARLY BROAD (w0 I/R=0.41 → w2 I/R=0.32)
- Stop-Low: 35 stop, 0 rescues → SAFE
