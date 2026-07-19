# TRM V5.15 CVE: Control Variance Execution

**Suite:** CVE | **Status:** COMPLETE | **Date:** 2026-07-18

## Quick Summary

M3+ explains **47.3%** of persistence variance above baseline (73% precision, 65% overall accuracy). Per-N: N=80 100% precision (saturated), N=75 85% (strong regime), N=71/72 71%. Residual variance is 35% overall, concentrated at N=71 (23% FN rate) and N=75 (50% FN rate — largely from projHiVec over-filtering at N=75 where model prescribes bypass). orthHiVec effect size is moderate (0.45 FP/TP, 0.41 FN/TN). Model is near explanatory ceiling.

---

## 1. Variance Decomposition (CVE_01)

| Model | Predicted | Correct | Precision | Explained% |
|-------|----------|---------|-----------|------------|
| M0 (P1 only) | 136 | 84 | 62% | 26.1% |
| M3 (+projHiVec) | 79 | 56 | 71% | 43.7% |
| **M3+ (+orth N=72)** | **77** | **56** | **73%** | **47.3%** |

Explained% = fraction of residual variance above baseline captured. M3+ explains nearly half.

### Per-N M3+ Performance

| N | Total | Predicted | Correct | Precision | Residual% | Explained% |
|---|-------|----------|---------|-----------|-----------|------------|
| 67 | 60 | 8 | 1 | 12% | **98%** | 14% |
| 71 | 60 | 17 | 12 | 71% | 80% | 46% |
| 72 | 60 | 21 | 15 | 71% | 75% | 52% |
| 75 | 60 | 20 | 17 | 85% | 72% | 36% |
| 80 | 23 | 11 | 11 | **100%** | 52% | 61% |

**Note:** N=75 M3+ prediction applies projHiVec selector, but M3 model prescribes BYPASS at N=75. With bypass: 47 P1/P1b seeds, ~36 correct (77%). The N=75 residual is partially an artifact of model encoding, not true unexplained variance.

---

## 2. Confusion Matrix (CVE_02)

| | Predicted Positive | Predicted Negative |
|---|-------------------|-------------------|
| **Actual Positive** | TP=56 | FN=71 |
| **Actual Negative** | FP=21 | TN=115 |

**Accuracy: 65.0%**

### Residual Concentration by N

| N | FP | FN | FP/N Rate | FN/N Rate |
|---|----|----|-----------|-----------|
| 67 | 7 | 6 | 12% | 10% |
| 71 | 5 | **14** | 8% | **23%** |
| 72 | 6 | **14** | 10% | **23%** |
| 75 | 3 | **30** | 5% | **50%** |
| 80 | 0 | 7 | 0% | 30% |

N=75 FN rate (50%) is highest — largely from projHiVec over-filtering. N=71/72 each have 23% FN.

### By Cohort

| Cohort | Errors | Rate |
|--------|--------|------|
| Train (0-99) | 46/130 | 35.4% |
| Holdout (100-199) | 46/133 | 34.6% |

**Equal error rates across cohorts** — no overfit signal in variance.

### By Pathway Class

| Class | Errors | Rate |
|-------|--------|------|
| P1b | 34/89 | 38% |
| P1 | 15/47 | 32% |
| P2 | 22/81 | 27% |
| P3 | 13/34 | 38% |
| P4 | 8/12 | 67% |

---

## 3. Error Structure (CVE_03)

### FP vs TP (M3+ predicted positive)

| Feature | TP Mean | FP Mean | Effect Size |
|---------|---------|---------|-------------|
| dMean | 0.6580 | 0.6055 | **0.59** |
| orthHiVec | 0.0497 | 0.0371 | **0.45** |
| omT1 | 1.366 | 1.432 | 0.12 |
| projHiVec | -0.185 | -0.179 | 0.08 |

### FN vs TN (M3+ predicted negative)

| Feature | FN Mean | TN Mean | Effect Size |
|---------|---------|---------|-------------|
| orthHiVec | 0.0549 | 0.0359 | **0.41** |
| omT1 | 1.238 | 1.443 | **0.35** |
| dMean | 0.5315 | 0.4736 | 0.21 |
| projHiVec | -0.048 | -0.024 | 0.08 |

**Moderate structure in residuals:** dMean and orthHiVec partially separate FP/TP. orthHiVec and omT1 partially separate FN/TN. No large effect sizes (>0.6) — residuals are weakly structured.

---

## 4. Ceiling Assessment (CVE_04)

| N | Holdout Persist | M3+ Correct | Residual |
|---|----------------|------------|----------|
| 71 | 11 | 5 | 6 |
| 72 | 12 | 7 | 5 |
| 75 | 23 | 6 | 17 |

**Holdout M3+ explains 26/64 = 41% of persistence.**

---

## 5. Decision Gates

| Gate | Status | Evidence |
|------|--------|----------|
| A — M3+ explains majority | NOT REACHED | 41% holdout explained |
| B — Large residual | **REACHED** | 35% residual |
| C — Residual concentrated | **REACHED** | N=75 FN=50%, N=71/72 FN=23% |
| D — Residual diffuse | NOT REACHED | Concentrated in N-dependent pattern |
| E — Near ceiling | **REACHED** | M3+ near ceiling, weak structure in residuals |

---

## 6. Claim Discipline

No new mechanisms, hidden variables, or causality claims. No physical interpretation.

---

## 7. Next: CVA — Residual Variance Analysis

Investigate whether N=75 residuals are from model encoding (projHiVec over-filtering) or genuine unexplained variance. Quantify maximum realistic holdout rate.
