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

## Open Questions
- Why does N=75 amplify in the w2→T0 handoff while N=72 collapses?
- What mechanism distinguishes N=75 from N=72 at the w2 state?
- Pre-warmup to w0 still NOT INSTRUMENTED.

## Next Steps
- TSA: Spread stability audit (how reproducible are w2→T0 dynamics across re-runs?)
- TSI: Instrumentation review (what additional metrics needed to trace w2→T0 mechanism?)
- TSS: Synthesis
