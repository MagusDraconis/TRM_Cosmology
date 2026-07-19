# V5.40 Experiment Log | **Created:** 2026-07-19 | **Last Updated:** 2026-07-19
| Suite | Status | Tests | Outcome |
|-------|--------|-------|---------|
| CTP | COMPLETE | 3 | Protocol frozen |
| CTE | COMPLETE | 1 | Absorption Model A. Directional c3OmgS found (weak, 55%+). Gate D FAILED. |
| CTA | COMPLETE | 1 | Weak signals = artifacts (Model C). 52.3% correct. Effect-to-noise 0.886. Causal closure BLOCKED. |
| CTI | SKIPPED | — | CTA provides sufficient audit evidence |
| CTS | PLANNED | — | Final V5.40 synthesis |

## CTA Results

**728 profiles, 7 perturbation families.** Overall correct rate 52.3% (near chance).
Effect-to-noise ratio 0.886 (< 1.0). Best perturbation P3 at 56.7%. Strong baseline-dependence:
negative c3OmgS = 63.8% correct, high c3OmgS = 36.1% correct. Absorption is direction-invariant
(Model D). **Weak signals classified as Model C — perturbation-pattern artifact.**
Causal closure remains BLOCKED (Gate F NOT REACHED, Gate G REACHED). Stop-Low safe. V6 not ready.
