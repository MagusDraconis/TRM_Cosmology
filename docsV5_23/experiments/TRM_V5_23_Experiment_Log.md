# V5.23 Experiment Log | **Created:** 2026-07-18
| Suite | Status | Tests |
|-------|--------|-------|
| CGI | COMPLETE | 6 tests | 6/6 passed, ~5 min |

## CGI Summary (2026-07-19)
- **d_tail perturbation CAN increase deltaD** (40x at N=64 +50%) but c3OmegaShift stays flat
- **Classification:** d_tail is NECESSARY BUT NOT SUFFICIENT for C3 gain
- **Best N=64:** c3OmgS=0.666 via d_tail matching — near threshold but no strict persistence
- **Gate D REACHED:** Gain without persistence at N=64
- **Hidden bottleneck:** omegaPerK — the gain factor between deltaK and c3OmegaShift
- **d_tail controls input (deltaD) but not gain (omegaPerK)**

## CGA Summary (2026-07-18)
- **d→K coupling model:** c3OmegaShift ≈ deltaD × kSensitivity × omegaPerK
- **Bottleneck:** deltaD magnitude — 14x smaller at N=64 than N=65 rescued
- **kSensitivity stable:** ~0.01 at both N=64 and N=65 rescued (ratio only 1.3x)
- **Best pre-C3 predictor:** d_tail > 0.3 (84% accuracy for kSensitivity)
- **N=65 activation:** 2.1x larger dMean, 2.8x larger dTail → 14x larger C3 movement
- **Gates reached:** A (driver), B (suppression), C (activation), E (coupling validated)
- **Model:** C — d→K coupling controlled
