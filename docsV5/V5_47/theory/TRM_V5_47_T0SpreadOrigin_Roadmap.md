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

## TSA_01 Results
**SUPPORTED stability — all 8 claims survive robustness cuts.**

- 10 random splits per N: all d/K sign-stable, rank inversion sign-stable
- Leave-one-profile jackknife: no single profile flips any conclusion
- N=75 amp mean jackknife = 9.26 (min 1.59) — direction stable, magnitude varies
- Cross-N: mean d_w2 nearly identical (0.457 vs 0.458) — baseline gap ≠ mean
- km_w2 IQR differs: N=75=0.129 vs N=72=0.087 (1.48x)
- Stop-Low zero damage across all N and cuts
- Shape classes stable at full-N, subset-sensitive due to n=12-20

## Open Questions
- km/lam w2-state IQR differs between N=75 and N=72 (1.48x) while mean d_w2 is nearly identical
- Does w2-state spread (not just mean) determine handoff direction?
- Pre-warmup instrumentation still unresolved.
- TSS synthesis pending — what is the overall V5.47 finding?

## Next Steps
- TSA: Spread stability audit (how reproducible are w2→T0 dynamics across re-runs?)
- TSI: Instrumentation review (what additional metrics needed to trace w2→T0 mechanism?)
- TSS: Synthesis
