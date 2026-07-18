# TRM V5.15 CVA: Residual Variance Analysis

**Suite:** CVA | **Status:** COMPLETE | **Date:** 2026-07-18

## Quick Summary

M3+ with N=75 bypass achieves 71.9% precision, 60.5% recall, 67.7% accuracy. **False negatives dominate (45) over false positives (27)** — the model is conservative. **Residuals are structurally modelable**: FN have large effect sizes (projHiVec ES=1.56, dMean ES=1.58), FP are dominated by insufficient displacement (48%). **N=75 selector (85%) outperforms bypass (77%)** — M3's "selector-neutral" label is too conservative. Residual structure exists — ceiling NOT reached.

---

## 1. Confusion Decomposition with N=75 Bypass (CVA_01)

| N | Cohort | TP | FP | TN | FN | Prec | Rec | FPR | FNR |
|---|--------|----|----|----|----|------|-----|-----|-----|
| 67 | Train | 0 | 6 | 23 | 0 | 0% | — | 21% | 0% |
| 71 | Train | 7 | 2 | 13 | 8 | 78% | 47% | 13% | 53% |
| 71 | Hold | 5 | 3 | 16 | 6 | 62% | 45% | 16% | 55% |
| 72 | Train | 8 | 4 | 9 | 9 | 67% | 47% | 31% | 53% |
| 72 | Hold | 7 | 2 | 16 | 5 | 78% | 58% | 11% | 42% |
| 75 | Train | 15 | 4 | 2 | 9 | 79% | 62% | 67% | 38% |
| 75 | Hold | 15 | 5 | 2 | 8 | 75% | 65% | 71% | 35% |
| 80 | Hold | 7 | 0 | 1 | 0 | 100% | 100% | 0% | 0% |

**Overall: TP=69, FP=27, TN=109, FN=45. Precision=71.9%, Recall=60.5%.**

---

## 2. False Negative Analysis (CVA_02)

### FN by N

| N | E4 (true FN) | E5 (bypass FN) | Total | Cause |
|---|-------------|----------------|-------|-------|
| 71 | 14 | 0 | 14 | **OVER_FILTER** |
| 72 | 14 | 0 | 14 | **OVER_FILTER** |
| 75 | 0 | 17 | 17 | **BYPASS_MISMATCH** |
| 67/80 | 0 | 0 | 0 | Inaccessible/Saturated |

### FN vs TP Features

| Feature | FN Mean | TP Mean | **Effect Size** |
|---------|---------|---------|----------------|
| dMean | 0.401 | 0.702 | **1.58** |
| projHiVec | 0.089 | -0.233 | **1.56** |
| lambda1 | 70.95 | 61.73 | **1.38** |
| dStd | 0.150 | 0.203 | **0.84** |
| orthHiVec | 0.035 | 0.060 | 0.58 |

**FN are highly structured.** Rejected seeds have: lower dMean (0.40 vs 0.70), positive projHiVec (0.09 vs -0.23), higher lambda1 (70.95 vs 61.73). These are seeds that cross the projHiVec threshold in the wrong direction but still succeed — suggesting the threshold could be relaxed or N-conditioned.

---

## 3. False Positive Analysis (CVA_03)

### FP by N

| N | Count | dMean | Dominant Failure |
|---|-------|-------|-----------------|
| 67 | 7 | 0.576 | insufDisp |
| 71 | 5 | 0.630 | insufDisp |
| 72 | 6 | 0.617 | mixed |
| 75 | 9 | 0.806 | insufDisp |

### FP vs TN Features

| Feature | FP Mean | TN Mean | **Effect Size** |
|---------|---------|---------|----------------|
| projHiVec | -0.239 | -0.001 | **1.25** |
| dMean | 0.672 | 0.450 | **1.18** |
| lambda1 | 60.87 | 66.53 | **1.04** |
| dStd | 0.228 | 0.169 | **1.02** |

### FP Failure Modes
- **insufDisp: 48%** — never reached High basin
- unresolved: 26%
- K_collapse/d_rebound: remainder

**FP are dominated by insufficient displacement.** The model correctly identifies compression-room candidates, but the intervention doesn't displace far enough. This is a displacement problem, not a selection problem.

---

## 4. N=75 Analysis (CVA_04)

### Selector vs Bypass

| Strategy | Predicted | Correct | Rate |
|----------|----------|---------|------|
| **projHiVec selector** | 20 | 17 | **85%** |
| Bypass (all P1/P1b) | 39 | 30 | 77% |

**SELECTOR IS SUPERIOR at N=75 (+8pp).** M3's "selector-neutral" for N=75 was too conservative.

### N=75 Success vs Failure Features

| Feature | Succ | Fail | Effect Size |
|---------|------|------|-------------|
| omT1 | 1.279 | 1.848 | **0.80** |
| orthHiVec | 0.071 | 0.092 | **0.76** |
| projHiVec | -0.261 | -0.347 | **0.53** |
| dMean | 0.725 | 0.806 | **0.52** |

---

## 5. Residual Structure Verdict (CVA_05)

| Gate | Status | Evidence |
|------|--------|----------|
| **A — Structurally modelable** | **REACHED** | FN ES>1.5 on projHiVec/dMean, FP dominated by insufDisp |
| **B — FN dominate** | **REACHED** | FN=45 > FP=27 |
| C — FP dominate | NOT REACHED | |
| **D — N=75 separate** | **REACHED** | N=75 selector (85%) > bypass (77%) |
| E — N=71/72 explained | NOT REACHED | FN rate 42-55% |
| **F — Ceiling** | **NOT REACHED** | Actionable structure exists |

**Verdict: Residuals are structurally modelable.** Ceiling NOT reached. Two clear opportunities:
1. **Relax projHiVec threshold** to reduce over-filtering (FN reduction)
2. **Increase intervention displacement** to address insufficient-disp FP/TP failures

---

## 6. Claim Discipline

No new selectors proposed. No thresholds tuned (analysis only). No physical interpretation.

---

## 7. Next: CVI — Control-Limit Audit

Test whether relaxing projHiVec threshold (or applying N-specific thresholds) can reduce false negatives without increasing false positives. Quantify maximum achievable holdout rate.
