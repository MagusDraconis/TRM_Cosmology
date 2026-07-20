# V5.47 T0 Spread Protocol | **COMPLETE — TSP_01 passed 2026-07-20**
Frozen: M3++, Stop-Low, c3OmgS threshold. Diagnostic trace only.
Purpose: Investigate origin of T0 post-warmup Omega spread by N.

## Executive Determination

**Model A: T0 inherited spread dominates and N=75 is genuinely broad at origin.**

- T0→entry IQR corr = 1.000 (confirms V5.46)
- N=75 T0 IQR = 1.200 vs next highest = 0.047 (25×)
- N=75 IQR/range = 0.93 → bulk-wide distribution, not tail-driven
- Warmup origin: N=75 NOT broad at epoch-0 (IQR=0.004), broadness emerges at T0
- d/K diagnostic: d0→T0=-0.351, km0→T0=0.550, lam0→T0=0.552 (moderate, not causal)
- Model A ROBUST without N=75 (T0→entry=0.740)
- Stop-Low: 41 profiles at c3≤0.1, 0 rescues → SAFE. V6 NOT READY.

## Sections

PART A — Protocol Freeze: N={67,70,72,75}, checkpoints warmup+T0+T1+T2+T3+C3
PART B — T0 Origin Audit: N=75 uniquely broad, bulk-wide confirmed
PART C — Warmup Dynamics: N=75 broadness NOT during warmup, appears at T0
PART D — Branch Distribution: N=75 continuous broad bulk, not multi-branch
PART E — d/K Pre-State: moderate diagnostic association only
PART F — Leave-One-N Robustness: Model A ROBUST
PART G — Decision: Model A
PART H — Claim Discipline: diagnostic only, no causal closure claimed
