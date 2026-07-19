# TRM V5.14 RGE: Residual Geometry Execution

**Suite:** RGE_ResidualGeometryExecution
**Status:** COMPLETE
**Date:** 2026-07-18

---

## Quick Summary

263 residual profiles extracted across N=67/71/72/75/80, both cohorts, with **25+ features across all 6 feature families (F1-F6)**. M3 baseline **reproduced**: N=71/72 selector-useful, N=75 selector-neutral, N=80 saturated, N=67 inaccessible. Preliminary residual separation shows **d_std** (0.49 effect size), **lambda2** (0.38), and **dTailWidth** (0.37) as top candidate residual features for G1 vs G2 separation. N=75 failures show strong separation on **d_velocity** (0.69), **dMean** (0.70), and **orthHiVec** (0.61). Dataset is ready for RGA residual analysis.

---

## 1. M3 Baseline Reproduction (RGE_01)

| N | Cohort | M3 Selected | Strict | Rate | Pathway Rate | Verdict |
|---|--------|------------|--------|------|-------------|---------|
| 67 | Train | 6 | 0 | 0% | 0% | INACCESSIBLE |
| 67 | Hold | 2 | 1 | 50% | 20% | INACCESSIBLE |
| 71 | Train | 9 | 7 | 78% | 64% | **SELECTOR_USEFUL** ✓ |
| 71 | Hold | 8 | 5 | **62%** | 50% | **SELECTOR_USEFUL** ✓ |
| 72 | Train | 12 | 8 | 67% | 60% | **SELECTOR_USEFUL** ✓ |
| 72 | Hold | 11 | 7 | **64%** | 56% | **SELECTOR_USEFUL** ✓ |
| 75 | Train | 12 | 11 | 92% | 79% | SELECTOR_USEFUL |
| 75 | Hold | 8 | 6 | 75% | 75% | **SELECTOR_NEUTRAL** ✓ |
| 80 | Train | 4 | 4 | 100% | 60% | SATURATED |
| 80 | Hold | 7 | 7 | 100% | 92% | **SATURATED** ✓ |

**Gate A: REACHED — M3 baseline reproduced.** Holdout selector lift: N=71 +12%, N=72 +8%, N=75 0%, N=80 saturated. Matches V5.13 HVS findings.

---

## 2. Residual Dataset Inventory (RGE_02)

### State Groups

| N | G1 (M3Sel·S) | G2 (M3Sel·F) | G3 (M3Rej·S) | G4 (M3Rej·F) |
|---|-------------|-------------|-------------|-------------|
| 67 | 0 | 0 | 0 | 0 |
| 71 | 12 | 5 | 14 | 29 |
| 72 | 15 | 8 | 14 | 23 |
| 75 | 17 | 3 | 30 | 10 |
| 80 | 0 | 0 | 0 | 0 |

### M3 Selection vs Rejection

| N | Cohort | M3 Selected | M3 Rejected | Success | Failure |
|---|--------|------------|------------|---------|---------|
| 71 | Train | 9 | 21 | 15 | 15 |
| 71 | Hold | 8 | 22 | 11 | 19 |
| 72 | Train | 12 | 18 | 17 | 13 |
| 72 | Hold | 11 | 19 | 12 | 18 |
| 75 | Train | 12 | 18 | 24 | 6 |
| 75 | Hold | 8 | 22 | 23 | 7 |

**Total: 263 profiles** — sufficient for RGA analysis.

---

## 3. Feature Completeness (RGE_03)

All 25 core features: **100% complete** across all profiles.

| Family | Features | Status |
|--------|----------|--------|
| F1 — Geometry | projHiVec, orthHiVec, distToHi, distToLo | ✓ 100% |
| F2 — d-distribution | dMean, dStd, dP50, dP75, dP90, dP95, dMax, dTailWidth, ratios | ✓ 100% |
| F3 — K-distribution | kMean, kStd, top1%/5%/10% edge share | ✓ 100% |
| F4 — Spectral | lambda1, lambda2, spectralGap, kFrob | ✓ 100% |
| F5 — Trajectory | dVelocity, kVelocity, dAccel, kAccel | ✓ 100% |
| F6 — Intervention | deltaDRel, insufDisp, overComp, postKCollapse | ✓ 100% |

**Gate B: NOT REACHED.** All feature families are fully populated.

---

## 4. Preliminary Residual Separation (RGE_04)

### G1 vs G2 — M3-Selected: Success vs Failure (Train)

| Rank | Feature | PosMean | NegMean | Effect Size |
|------|---------|---------|---------|-------------|
| 1 | **dStd** | 0.2279 | 0.2524 | **0.486** |
| 2 | **lambda2** | 12.9013 | 14.7219 | **0.377** |
| 3 | **dTailWidth** | 0.0534 | 0.0654 | **0.365** |
| 4 | spectralGap | 50.77 | 49.18 | 0.285 |
| 5 | kVelocity | -0.0015 | 0.0436 | 0.283 |
| 6 | kFrob | 65.99 | 66.59 | 0.210 |
| 7 | top1%Edge | 0.0132 | 0.0131 | 0.221 |
| 8 | dVelocity | -0.0286 | -0.1087 | 0.217 |

### G3 vs G4 — M3-Rejected: Success vs Failure (Train)

| Rank | Feature | PosMean | NegMean | Effect Size |
|------|---------|---------|---------|-------------|
| 1 | **dStd** | 0.1438 | 0.1807 | **0.552** |
| 2 | **dVelocity** | 0.0139 | -0.1230 | **0.338** |
| 3 | orthHiVec | 0.0431 | 0.0305 | 0.329 |
| 4 | top5%Edge | 0.0609 | 0.0626 | 0.290 |
| 5 | lambda1 | 69.27 | 67.59 | 0.202 |

### N=75 — Success vs Failure (P1/P1b, Train)

| Feature | SuccMean | FailMean | Effect Size |
|---------|----------|----------|-------------|
| **dMean** | 0.6957 | 0.7978 | **0.70** |
| **dVelocity** | 0.0817 | -0.1638 | **0.69** |
| top1% | 0.0136 | 0.0141 | 0.62 |
| orthHiVec | 0.0616 | 0.0793 | 0.61 |
| dStd | 0.1693 | 0.1933 | 0.57 |
| lambda1 | 62.05 | 59.12 | 0.52 |

**Key finding:** dStd is the most consistent residual separator across all comparisons. dVelocity and orthHiVec show strong N=75 separation.

**Gate D: WEAK —** Some separation exists but no single feature dominates. RGA must test systematically.

---

## 5. Failure Mode Confirmation (RGE_05)

| Mode | Count | % |
|------|-------|---|
| insufficient displacement | 58 | 42.6% |
| inaccessible (N=67) | 53 | 39.0% |
| unresolved | 19 | 14.0% |
| saturated (N=80) | 5 | 3.7% |
| over-compression | 1 | 0.7% |

### By State Group

| Group | Failures | Top Mode |
|-------|----------|----------|
| G2 (M3 selected, fail) | 16 | insufDisp (10) |
| G4 (M3 rejected, fail) | 62 | insufDisp (48) |

**Confirms V5.13 finding:** dominant failure is insufficient displacement. G2 (M3-selected failures) are harder to characterize than G4 (rejected failures).

---

## 6. RGA Readiness Assessment (RGE_06)

| Criterion | Status |
|-----------|--------|
| M3 baseline reproduction | ✅ REPRODUCED |
| Feature completeness (F1-F6) | ✅ 100% for 25+ features |
| State group population | ✅ G1=44, G2=16, G3=58, G4=62 |
| Train/holdout split | ✅ 130 train, 133 holdout |
| Preliminary signal | ✅ dStd (0.49), lambda2 (0.38), dTailWidth (0.37) |
| N-conditioned data | ✅ Per-N profiles available |

**Recommendation: Proceed to RGA residual geometry analysis.**

---

## 7. Decision Gates

| Gate | Status | Evidence |
|------|--------|----------|
| **A — Dataset Ready** | **REACHED** | M3 reproduced, 263 profiles, 25+ features |
| B — Missing Features | NOT REACHED | All 6 families 100% complete |
| C — Baseline Failure | NOT REACHED | M3 baseline matches V5.13 |
| D — Residual Signal | **WEAK** | dStd shows moderate effect (0.49); no dominant feature |
| E — No Signal | POSSIBLE | If RGA finds no transferable residual selector |

---

## 8. Claim Discipline

| Claim | Status |
|-------|--------|
| No hidden-variable claims yet | ✅ |
| No thresholds tuned | ✅ Frozen from V5.13 |
| No selectors built | ✅ |
| Train/holdout separation maintained | ✅ |
| No physical interpretation | ✅ |

---

## 9. Recommended Next: RGA

**RGA_ResidualGeometryAnalysis** — Test residual feature selectors from F1-F6 against M3 baseline. Primary candidates: dStd, lambda2, dTailWidth, orthHiVec, dVelocity. Evaluate holdout transfer and control limit hypotheses.
