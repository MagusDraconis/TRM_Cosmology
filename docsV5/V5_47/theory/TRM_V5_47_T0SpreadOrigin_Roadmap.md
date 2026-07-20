# V5.47 T0 Spread Origin — Roadmap | **UPDATED 2026-07-20**

## TSP_01 Results
**Model A: T0 inherited spread dominates. N=75 is genuinely broad at origin.**
- T0→entry IQR = 1.000, entry→rescue = 0.921
- N=75 IQR/range = 0.93 → bulk-wide, not tail-driven
- Warmup trace: N=75 NOT broad at epoch-0 (IQR=0.004), broadness appears post-warmup at T0

## TSE_01 Results
**Model A: Post-warmup handoff w2→T0 specifically amplifies N=75 bulk spread.**
- N=75 w2 IQR = 0.604, IQR/range = 0.20 (NOT bulk-wide at w2)
- N=75 T0 IQR = 1.200, IQR/range = 0.93 (BECOMES bulk-wide at T0)
- w2→T0 amplification = 1.98x for N=75 (vs 0.11x for N=72)
- **N=72 paradox resolved:** N=72 IS bulk-wide at w2 but COLLAPSES at T0
- w2→T0 amp ~ T0 IQR corr = 0.823 — strong diagnostic association
- Full d/K/lambda per-stage instrumentation confirms moderate diagnostic, not causal
- Stop-Low safe. V6 NOT READY.

## THD_01 Results
**Model E: d/K at w2 near-perfectly diagnostic of handoff delta within each N. Rank-inverting transform.**

- N=75 Spearman = -0.671 (rank-INVERTING), N=72 Spearman = -0.483 (rank-inverting)
- **Contrary to hypothesis:** ranks are NOT preserved during handoff
- d_w2 ~ delta_om: N=72=-0.923, N=75=-0.964 (near-perfect!)
- km_w2 ~ delta_om: N=72=0.956, N=75=0.964 (near-perfect!)
- d/K at w2 strongly diagnostic within each N, but does NOT explain amp vs collapse between N
- N=75 shape: S1 Bulk-wide amplification, N=72: S2 Bulk collapse, N=70: S3 Stable compressed
- Stop-Low safe. V6 NOT READY.

## Open Questions
- Why does rank inversion produce amplification in N=75 but collapse in N=72?
- d/K baseline differs between N (N=72 d_w2=0.439, N=75 d_w2=0.358) — does this baseline gap determine direction?
- Pre-warmup instrumentation still unresolved.

## Next Steps
- TSA: Spread stability audit (how reproducible are w2→T0 dynamics across re-runs?)
- TSI: Instrumentation review (what additional metrics needed to trace w2→T0 mechanism?)
- TSS: Synthesis
