# V5.49 Experiment Log | **Created:** 2026-07-20
| Suite | Status | Tests | Outcome |
|-------|--------|-------|---------|
| DSG | COMPLETE | 1 (DSG_01) | Model C — Spread generated in w1→w2 transition. N=72 collapses while N=75 grows. |
| DGT | COMPLETE | 1 (DGT_01) | Model C — w1→w2 is rank-inverting. N=72 compresses inward, N=75 expands outward. |
| DSS | PLANNED | — | — |

## DGT_01 — w1→w2 Growth/Collapse Transition Audit (2026-07-20)
- **Passed.** 47s execution. **Decision: Model C — w1→w2 is rank-inverting. N=72 compresses inward, N=75 expands outward.**
- All N show NEGATIVE Spearman w1→w2 (-0.54 to -0.66) — rank-inverting like w2→T0
- N=72: 14 inward vs 6 outward, amp=0.57 → INWARD COMPRESSION collapse
- N=75: 7 outward vs 5 inward, amp=1.50 → OUTWARD EXPANSION growth
- N=70: 11 outward vs 9 inward, amp=1.04 → STABLE BROAD
- delta_d ~ delta_km: N=72=-0.991, N=75=-0.994 (near-perfect diagnostic)
- km_w1 ~ delta_km: N=72=-0.930, N=75=-0.926
- IQR flip mechanism: N=72 starts 1.79× wider at w1, but compresses harder
- Jackknife: Spearman and amp sign-stable for both N
- Stop-Low: 35 stop, 0 rescues → SAFE

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
