# TRM V5.14 Experiment Log

**Branch:** feature/v5.14-residual-geometry-and-control-limits
**Created:** 2026-07-18
**Base:** V5.13 COMPLETE (2590 tests, 0 failed)

---

## Suite Status

| Suite | Status | Tests | Date |
|-------|--------|-------|------|
| RGP | INITIALIZED | 8 | 2026-07-18 |
| RGE | **COMPLETE** | 6 | 2026-07-18 |
| RGA | PENDING | — | — |
| RGI | PENDING | — | — |
| RGS | PENDING | — | — |

---

## Experiment History

### RGE — Residual Geometry Execution (2026-07-18)
6 tests, ~4.5 min. 263 residual profiles across N=67-80, 25+ features across F1-F6. M3 baseline reproduced. dStd (0.49 ES), lambda2 (0.38), dTailWidth (0.37) identified as top residual candidates. Dataset ready for RGA.

---

## Baseline Model (Frozen from V5.13)

**M3:** N-conditioned P1/P1b Compression-Room + projHiVec > -0.3281

- N=67: inaccessible (no pathway claim)
- N=71/72: apply projHiVec selector
- N=75: strong pathway, selector bypass
- N=80: saturated, universal protocols
- P2 Crypto-Hi: demoted to exploratory

---

## Planned Residual Feature Families

| Family | Name | Examples |
|--------|------|----------|
| F1 | Geometry residuals | orthHiVec, distToHi, distToLo, entry score residual |
| F2 | d-distribution residuals | d_std, d_p90, d_max, d_tail_width |
| F3 | K-distribution residuals | K_std, top 1%/5%/10% edge share |
| F4 | Spectral residuals | lambda1, lambda2, spectral gap, K_Frob |
| F5 | Trajectory residuals | d/K velocity, acceleration, curvature, rebound |
| F6 | Intervention response | delta-dRel, displacement, over-compression, collapse |
