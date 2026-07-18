# V5.24 Experiment Log | **Created:** 2026-07-19
| Suite | Status | Tests |
|-------|--------|-------|
| OGA | COMPLETE | 5 tests | 5/5 passed, ~5.7 min |

## OGA Summary (2026-07-19)
- **Two-condition sign rule:** Positive omegaPerK requires omDist<0.5 AND lambda1<0.95
- **Combined condition:** 79% positive sign, 45% rescue rate (vs 34%/6% otherwise)
- **N=64:** Rarely meets both (omDist ~0.68, lambda1 ~0.99) → mostly negative
- **N=65 rescued:** Meets both (omDist=0.17, lambda1=0.86) → 100% positive
- **Best model: F — Mixed threshold + spectral (71% accuracy)**
- **Gates reached:** B, C, D, G

## OGE Summary (2026-07-19)
- **Key finding:** N=64 omegaPerK is large (-280) but NEGATIVE — C3 pushes Omega AWAY from threshold
- **N=65 rescued:** omegaPerK=+89, omDist=0.17 — C3 pushes Omega toward threshold
- **Sign reversal at onset:** C3 changes from anti-rescue to pro-rescue direction
- **omDist (distance to THR):** 4x closer at N=65 rescued (0.17 vs 0.68)
- **Best predictor:** omegaPerK>100 has 89% rescue accuracy
- **Gates reached:** A, B, C
