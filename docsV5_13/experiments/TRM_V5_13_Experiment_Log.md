# TRM V5.13 Experiment Log

**Branch:** feature/v5.13-high-basin-pathway-validation-and-scaling
**Created:** 2026-07-17
**Finalized:** 2026-07-18
**Base:** V5.12 COMPLETE (2565 tests, 0 failed)
**Result:** COMPLETE (2590 tests, 0 failed)

---

## Suite Status

| Suite | Status | Tests | Date |
|-------|--------|-------|------|
| HVP | **COMPLETE** | 2 | 2026-07-17 |
| HVE | **COMPLETE** | 2 | 2026-07-17 |
| HVA | **COMPLETE** | 3 | 2026-07-17 |
| HVI | **COMPLETE** | 10 | 2026-07-18 |
| HVS | **COMPLETE** | 8 | 2026-07-18 |

---

## Experiment History

### HVP — Protocol Registration (2026-07-17)
Two protocol tests. Registered V5.13 validation framework, claim discipline, and frozen V5.12 pathway definitions.

### HVE — Out-of-Sample Validation (2026-07-17)
Validated V5.12 pathway model against holdout seeds (100-199) at N=67/71/72/75/80. Matched strategy retains directional signal but degrades on holdout. N=67 inaccessible. N=75 strongest. N=80 broadly inducible.

### HVA — Pathway Scaling Analysis (2026-07-17)
Per-class candidate quality analysis. Demonstrated holdout degradation is NOT from candidate quality. P2 transfer failure identified. N-conditioned behavior discovered.

### HVI — Hidden Variable and Residual Failure Audit (2026-07-18)
Audited 384 profiled seeds across N=67-80. Identified projHiVec as best transferable hidden variable. P2 overfit confirmed. 82% of failures are insufficient displacement. Fixed optimization bug (EnsureProfiles caching).

### HVS — Hidden Variable Scaling and Selector Validation (2026-07-18)
Validated projHiVec > -0.3281 as residual selector. +12.5% holdout improvement at N=71, +8.1% at N=72. P2 demoted. Lambda1 does not add value. Final model M3 established.

---

## Synthesis

See `docsV5_13/TRM_V5_13_Final_Synthesis.md` for full synthesis document.

**Final model:** M3 — N-conditioned P1/P1b + projHiVec > -0.3281.

**Next:** V5.14 — Residual Geometry and Control Limits.
