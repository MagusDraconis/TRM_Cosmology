# V5.47 T0 Spread Origin — Roadmap | **UPDATED 2026-07-20**

Background: V5.46 found T0 inherited spread dominates. N=75 uniquely broad (IQR=1.200).
Candidate factors: warmup dynamics, initial branch distribution, N-window geometry,
d/K pre-state, lambda1 dispersion. V6 NOT READY.

## TSP_01 Results (2026-07-20)

**Model A: T0 inherited spread dominates. N=75 is genuinely broad at origin.**
- T0→entry IQR = 1.000, entry→rescue = 0.921
- N=75 IQR/range = 0.93 → bulk-wide, not tail-driven
- Warmup trace: N=75 NOT broad at epoch-0 (IQR=0.004), broadness appears post-warmup at T0
- N=75 T0 broadness origin = NOT INSTRUMENTED before warmup
- d/K pre-state provides moderate diagnostic association only (not causal)
- Model A survives without N=75 (0.740)
- Stop-Low safe. V6 NOT READY.

## Next Steps
- TSE: T0-T3 spread evolution audit (how does N=75 preserve spread across stages?)
- TSA: Stability audit (how stable is N=75 broadness across re-runs?)
- TSI: Instrumentation review (what additional metrics needed to trace origin?)
- TSS: Synthesis
