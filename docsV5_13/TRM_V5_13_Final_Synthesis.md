# TRM V5.13 Final Synthesis

**Branch:** feature/v5.13-high-basin-pathway-validation-and-scaling
**Status:** COMPLETE
**Date:** 2026-07-18
**Base:** V5.12 COMPLETE (2565 tests, 0 failed)

---

## 1. Branch Metadata

| Item | Value |
|------|-------|
| Branch | `feature/v5.13-high-basin-pathway-validation-and-scaling` |
| Suite inventory | HVP, HVE, HVA, HVI, HVS |
| Commits | d9ea29d, d9481d7, 207edd3 |
| V5.13 test count | 25 (HVP:2, HVE:2, HVA:3, HVI:10, HVS:8) |
| Cumulative test count | 2590 (all passed, 0 failed) |
| Zero-failure status | CONFIRMED |

---

## 2. V5.13 Research Question

**Do the V5.12 High-basin entry pathways generalize across larger seed sets, N ranges, and holdout cohorts?**

Answer: **Partially.** Compression-Room (P1/P1b) transfers with directional signal but degrades on holdout. Crypto-Hi Mild (P2) does not robustly transfer. A hidden-variable selector (projHiVec) improves holdout performance at specific N windows. The resulting model is N-conditioned, not universal.

---

## 3. Suite Summaries

### HVP — Protocol Registration

Two protocol tests registering the V5.13 validation framework, claim discipline, and frozen V5.12 pathway definitions (Compression-Room P1/P1b, Crypto-Hi Mild P2).

**Status:** COMPLETE (2 tests, <1s)

### HVE — Out-of-Sample Validation

Validated V5.12 pathway model against holdout seeds (100-199) at N=67/71/72 with extended N=70/75/80. Matched strategy retains directional signal but degrades on holdout. N=67 remains inaccessible. N=75 shows strong pathway signal. N=80 is broadly inducible.

**Status:** COMPLETE (2 tests, ~10 min)

### HVA — Pathway Scaling Analysis

Per-class candidate quality and success rate analysis. Demonstrated that holdout degradation is NOT explained by candidate quality differences — similar d0 and pathway class produce different outcomes. Current pathway features are incomplete. N=80 is the only N where holdout transfer is strong (30-40%).

**Status:** COMPLETE (3 tests, ~5 min)

### HVI — Hidden Variable and Residual Failure Audit

Comprehensive audit of 384 profiled seeds across N=67-80. Identified **projHiVec** (projection onto Hi-entry vector) as the best transferable hidden variable with 67% balanced accuracy. Ranked 28 candidate features by effect size and threshold separation. Confirmed P2 pathway overfit — holdout P2 candidates have identical quality to train but zero transfer. Classified 82% of failures as insufficient displacement.

**Gates reached:** A (hidden variable found), B (Compression-Room residual explained), C (Crypto-Hi overfit confirmed)

**Status:** COMPLETE (10 tests, ~5.2 min)

### HVS — Hidden Variable Scaling and Selector Validation

Validated projHiVec > -0.3281 as a residual selector. Confirmed S1 improves holdout persistence by +12.5% at N=71 and +8.1% at N=72. Selector is neutral at N=75 (baseline already 75%). P2 including dilutes precision (37-43% vs 50-56% for P1-only). Lambda1 does not add value beyond projHiVec. Established final model M3: N-conditioned P1/P1b + projHiVec.

**Gates reached:** A (projHiVec validated), B (N-conditioned required), C (P2 demoted), D (N=80 saturation), G (final model M3 established)

**Status:** COMPLETE (8 tests, ~5.4 min)

---

## 4. Supported Findings

| # | Finding | Evidence |
|---|---------|----------|
| 1 | Compression-Room P1/P1b remains transferable | HVE holdout 50-56% at N=71/72 |
| 2 | projHiVec is the strongest validated residual selector | HVI ranking, HVS validation |
| 3 | projHiVec improves holdout persistence at N=71 (+12.5%) and N=72 (+8.1%) | HVS S1 vs S0 holdout |
| 4 | P2 Crypto-Hi does not robustly transfer | HVE/HVA/HVI holdout zero/near-zero |
| 5 | P2 should be demoted to exploratory/local status | HVS S5 dilutes precision |
| 6 | N=67 remains inaccessible | HVE/HVA low candidate count, near-zero success |
| 7 | N=75 is the strongest pathway window | HVA 89% P1 persistence, 70% P1b |
| 8 | N=80 behaves as a saturated regime | HVA universal protocols ~pathway-matched |
| 9 | Final model must be N-conditioned | HVS selector useful at N=71/72, neutral at N=75 |

---

## 5. Hidden-Variable Result

**projHiVec > -0.3281** is the best validated residual selector (threshold frozen from HVI, trained on seeds 0-99).

projHiVec = scalar projection of a seed's (d_mean, K_mean, K_std) vector onto the direction from Lo-centroid to Hi-centroid.

| N | Pathway-only (S0) | +projHiVec (S1) | Improvement |
|---|-------------------|-----------------|-------------|
| 71 holdout | 50.0% | 62.5% | **+12.5%** |
| 72 holdout | 55.6% | 63.6% | **+8.1%** |
| 75 holdout | 75.0% | 75.0% | 0.0% (neutral) |

**Interpretation:** Pathway classification alone is insufficient. Residual geometry (projHiVec) provides additional predictive value at N=71/72. At N=75, pathway classification already achieves optimal separation.

---

## 6. Final V5.13 Model — M3

### Specification

**Pathway:** P1/P1b Compression-Room only
**Selector:** projHiVec > -0.3281 at N=71/72 (bypass at N≥75)
**Intervention:** Matched strong relative compression (50% d reduction)
**P2 status:** Demoted to exploratory/local

### N-Conditioned Regime Logic

| N | Regime | Action |
|---|--------|--------|
| 67 | Inaccessible | No pathway claim; insufficient candidates |
| 71 | Selector-useful | P1/P1b + projHiVec selector |
| 72 | Selector-useful | P1/P1b + projHiVec selector |
| 75 | Strong pathway | P1/P1b only (selector bypass) |
| ≥80 | Saturated | Universal protocols; pathway selection unnecessary |

---

## 7. Weakened Claims

Relative to V5.12, the following claims are weakened or removed:

| Claim | V5.12 Status | V5.13 Status |
|-------|-------------|-------------|
| Robust Crypto-Hi pathway | Claimed | **Demoted to exploratory/local** |
| Pathway symmetry (two equal pathways) | Implied | **P1 dominates, P2 is non-transferable** |
| Universal transfer across N | Implied | **N-conditioned model required** |
| Universal controllability | Not claimed | Not claimed (unchanged) |
| Lambda1 as required selector feature | Not applicable | **Not supported — S3=∅** |
| Pathway-only explanation of persistence | Implied | **Residual selector needed at N=71/72** |

---

## 8. Not Claimed Boundaries

V5.13 explicitly does NOT claim:

- Universal Low→High control
- Sufficient persistence conditions
- Complete hidden-variable discovery (projHiVec explains ~8-12% of holdout variance)
- Physical interpretation of projHiVec
- Attractor topology proof
- Universal scaling across all N
- P2 pathway validity beyond training seeds
- Generalization beyond N=67-80, seeds 0-199

---

## 9. Final V5.13 Conclusion

V5.13 narrows the V5.12 model. Compression-Room remains the primary transferable pathway for High-basin entry. projHiVec provides measurable holdout improvement as a residual selector. Crypto-Hi does not transfer robustly and is demoted. The resulting model is N-conditioned rather than universal.

The central V5.12 question — "Do the pathways generalize?" — has a clear answer: **partially**. P1 transfers directionally; P2 does not. A hidden variable exists but explains only part of the residual variance. The model is useful but not complete.

---

## 10. Recommended V5.14

**Branch:** `feature/v5.14-residual-geometry-and-control-limits`

**Central question:** Can the remaining unexplained persistence variance be reduced by additional geometric selectors, or has the current model reached its explanatory limit?

**Purpose:** Determine whether further predictive structure exists beyond Compression-Room + projHiVec + N-conditioned logic.

**Planned suites:** RGP (protocol), RGE (execution), RGA (analysis), RGI (limit audit), RGS (synthesis)

**Core questions:**
1. What explains the remaining failures within P1/P1b?
2. Can unexplained persistence variance be reduced further?
3. Are additional geometric selectors real or overfit?
4. What are the control limits of the current model?
5. Is projHiVec the last useful selector?
6. Has the model reached a predictive ceiling?

---

## Claims Legend

| Symbol | Meaning |
|--------|---------|
| SUPPORTED | Verified by holdout evidence across multiple N |
| CONDITIONAL | N-conditioned or cohort-specific |
| HYPOTHESIS | Plausible but untested on independent data |
| NOT CLAIMED | Explicitly excluded from claim scope |
