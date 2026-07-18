# TRM V5.15 Final Synthesis

**Branch:** feature/v5.15-control-ceiling-and-unexplained-variance
**Status:** COMPLETE
**Date:** 2026-07-18
**Base:** V5.14 COMPLETE (M3+, 2615 tests)

---

## 1. Branch Metadata

| Item | Value |
|------|-------|
| Branch | `feature/v5.15-control-ceiling-and-unexplained-variance` |
| Suites | CVP, CVE, CVA, CVI (CVS = this document) |
| Commits | cda42f8, 52c99ac, c17bfec, e10f43a |
| V5.15 tests | 16 (CVP:3, CVE:4, CVA:5, CVI:4) |
| Cumulative | 2631 (all passed, 0 failed) |

---

## 2. V5.15 Research Question

**How much unexplained persistence variance remains after M3+, and is that variance actionable?**

Answer: **~35% residual variance; structured but not actionable at current control resolution.** M3+ explains ~47% of variance (41% holdout). False negatives dominate (45 vs 27 FP). Residuals show strong feature correlation (projHiVec ES=1.56, dMean ES=1.58) but tested refinements fail holdout practicality checks. **M3+ is at the practical control ceiling.**

---

## 3. Suite Summaries

### CVP — Protocol (3 tests)
Registered V5.15 variance accounting framework, frozen M3+ baseline, and claim discipline.

### CVE — Variance Execution (4 tests, ~4 min)
M3+ explains 47.3% of persistence variance. Holdout: ~41% explained, ~35% residual. Residuals concentrated by N (N=71/72: 23% FN, N=75: 50% FN from bypass over-filtering). Train/holdout error rates equal — no overfit.

### CVA — Residual Variance Analysis (5 tests, ~4 min)
Residuals structurally modelable. FN dominate (45 vs 27 FP). FN structured: projHiVec ES=1.56, dMean ES=1.58, lambda1 ES=1.38. FP dominated by insufficient displacement (48%). N=75 selector (85%) outperforms bypass (77%). Ceiling NOT reached at analysis level.

### CVI — Control Limit Audit (4 tests, ~4 min)
Three M3++ refinement candidates tested. **All fail holdout practicality:**
- M3++A (N=75 selector): recall drops 55%→41%
- M3++B (FN recovery): +9% recall but harms N=71 (-7pp), N=72 (-14pp)
- Displacement +20%: +11pp but narrower levels destructive

**Gate F (Control Ceiling): REACHED.**

---

## 4. Supported Findings

| # | Finding | Evidence |
|---|---------|----------|
| 1 | M3+ is the best validated model | No refinement outperforms |
| 2 | Residual variance is structured | FN ES: projHiVec 1.56, dMean 1.58 |
| 3 | False negatives dominate | 45 FN vs 27 FP |
| 4 | projHiVec + orthHiVec remain strongest selectors | All alternatives neutral/harmful |
| 5 | N=72 retains unique orthHiVec benefit | +14.1% holdout |
| 6 | N=75 structured but not actionable | Selector corrects but harms recall |
| 7 | N=67 inaccessible | Confirmed |
| 8 | N=80 saturated | Confirmed |
| 9 | Control ceiling reached | All refinements fail |

---

## 5. Variance Accounting

| Component | Value |
|-----------|-------|
| M3+ variance explained | 47.3% |
| Holdout explained | ~41% |
| Residual variance | ~35% |
| FN rate (holdout) | 45% |
| FP rate (holdout) | 16% |

M3+ explains a substantial minority — near-half of observable variation, not a majority.

---

## 6. Control Limit Result

**Three refinements tested — zero pass:**

| Refinement | Rec Δ | Harm | Verdict |
|-----------|-------|------|---------|
| N=75 selector | -14% | Recall loss | ❌ |
| FN recovery | +9% | N=71/72 degrade | ❌ |
| +20% displacement | N/A | Unstable | ❌ |

Structured residual variance remains but current measured geometry cannot convert it into robust control gains. **M3+ is at the practical control ceiling under current observables.**

---

## 7. Final V5.15 Model

**M3+** (unchanged from V5.14):

P1/P1b Compression-Room + projHiVec > -0.3281 + orthHiVec > 0.0153 (N=72)

| N | Model | Expected Holdout |
|---|-------|-----------------|
| 67 | Inaccessible | — |
| 71 | M3+ | 62% |
| 72 | M3+ (w/ orthHiVec) | 78% |
| 75 | M3+ (bypass) | 75% |
| 80 | Saturated | 92%+ |

---

## 8. Weakened / Rejected

| Claim | Status |
|-------|--------|
| Universal residual correction | Rejected |
| N=75 selector model | Rejected |
| FN recovery rule | Rejected |
| Robust displacement repair | Rejected |
| M3++ superiority | Rejected |

---

## 9. Not Claimed

V5.15 does NOT claim: complete explanation, universal control, optimality, physical interpretation, proof that no further feature exists.

---

## 10. Final V5.15 Conclusion

V5.15 quantified the remaining unexplained variance after M3+. Residual variance remains substantial (~35%) and structured. However, tested refinements failed to convert residual structure into reliable holdout improvements. **M3+ remains the preferred validated model.** The current framework has reached a practical control ceiling under presently measured features, while leaving open the possibility of future explanatory variables.

---

## 11. Recommended V5.16

**Branch:** `feature/v5.16-explanatory-gap-and-feature-discovery`

**Central question:** What measurable feature families could explain the remaining structured but currently uncontrollable variance?

**Suites:** EGP → EGE → EGA → EGI → EGS

Shift from control optimization to feature discovery — investigate whether the ceiling is due to missing observables.
