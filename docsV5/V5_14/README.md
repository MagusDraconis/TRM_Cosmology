# TRM V5.14: Residual Geometry and Control Limits

**Branch:** feature/v5.14-residual-geometry-and-control-limits
**Status:** INITIALIZED
**Date:** 2026-07-18
**Base:** V5.13 COMPLETE (2590 tests, 0 failed)

---

## Quick Reference

| Item | Value |
|------|-------|
| Purpose | Determine whether remaining unexplained persistence variance can be reduced by additional validated geometry, or whether the model has reached its practical explanatory limit |
| Baseline model | M3: N-conditioned P1/P1b + projHiVec > -0.3281 |
| Primary N | 71, 72, 75 |
| Suites | RGP → RGE → RGA → RGI → RGS |
| Methodology | Protocol → Execution → Freeze → Audit → Comparison → Interpretation → Synthesis |
| Claim scope | RecoverFP residual geometry and control limits only |

---

## Document Index

| Document | Path |
|----------|------|
| Experiment Log | `docsV5_14/experiments/TRM_V5_14_Experiment_Log.md` |
| Research Roadmap | `docsV5_14/theory/TRM_V5_14_ResidualGeometryAndControlLimits_Roadmap.md` |
| Protocol (RGP) | `docsV5_14/protocols/TRM_V5_14_ResidualGeometryAndControlLimits_Protocol.md` |
| RGP Tests | `TRM.Tests/V5_14/V5_14_ResidualGeometryAndControlLimitsProtocol_Tests.cs` |
| Status JSON | `TRM.App/wwwroot/data/trm-v5-14-status.json` |

---

## Suite Map

| Suite | Phase | Purpose |
|-------|-------|---------|
| RGP | Protocol | Define frozen baseline, feature families, gates, failure criteria |
| RGE | Execution | Collect residual features, test against baseline |
| RGA | Analysis | Rank features, test holdout transfer, classify failures |
| RGI | Limit Audit | Determine whether ceiling is reached |
| RGS | Synthesis | Final V5.14 model or ceiling declaration |

---

## Core Question

Can the remaining unexplained High-basin persistence variance be reduced by additional validated geometry, or has the current model reached its practical explanatory and control limit?

---

## Key Constraints

- No new pathways
- No threshold tuning
- No holdout optimization
- No P2 rescue without new evidence
- No physical interpretation
- No black-box models
- Strict train/holdout separation
- Conservative claim discipline
