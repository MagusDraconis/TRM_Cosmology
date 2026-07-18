# TRM V5.14 Experiment Log

**Branch:** feature/v5.14-residual-geometry-and-control-limits
**Created:** 2026-07-18
**Base:** V5.13 COMPLETE (2590 tests, 0 failed)

---

## Suite Status

| Suite | Status | Tests | Date |
|-------|--------|-------|------|
| RGP | INITIALIZED | 8 | 2026-07-18 |
| RGE | PENDING | — | — |
| RGA | PENDING | — | — |
| RGI | PENDING | — | — |
| RGS | PENDING | — | — |

---

## Experiment History

### RGP — Residual Geometry Protocol (2026-07-18)
Protocol initialization. 8 verification tests. Defines frozen M3 baseline, 6 residual feature families (F1-F6), decision gates A-G, failure criteria, and validation design.

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
