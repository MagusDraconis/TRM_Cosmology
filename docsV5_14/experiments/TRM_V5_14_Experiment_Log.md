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
| RGA | **COMPLETE** | 6 | 2026-07-18 |
| RGI | PENDING | — | — |
| RGS | PENDING | — | — |

---

## Experiment History

### RGA — Residual Geometry Analysis (2026-07-18)
6 tests, ~4.5 min. 10 residual features tested against M3 on holdout. orthHiVec is best (+3.5% holdout, +14.1% at N=72). No other feature >+3%. dTailWidth overfit. Most features neutral. M3 is near control ceiling. Gates A, E reached; G marginally reached.

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
