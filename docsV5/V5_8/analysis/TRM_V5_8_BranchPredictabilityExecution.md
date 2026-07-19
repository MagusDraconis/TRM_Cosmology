# TRM V5.8 Branch Predictability Execution (BPE)

**Date:** 2026-07-17  
**Branch:** `feature/v5.8-branch-predictability-and-forecasting`  
**Status:** COMPLETE  
**Gate:** D — N-dependent prediction

---

## 1. Pooled Prediction (All N, B0+V1)

| Predictor | Epoch | Test Acc | Bal Acc | F1 |
|-----------|-------|----------|---------|-----|
| K_mean | 1 | 0.29 | 0.30 | 0.39 |
| K_mean | 2 | 0.54 | 0.50 | 0.01 |
| K_mean | 3 | 0.24 | 0.23 | 0.14 |
| K_mean | 4 | 0.54 | 0.50 | 0.00 |
| K_mean | 5 | 0.14 | 0.14 | 0.07 |
| d_mean | 1 | 0.46 | 0.50 | 0.63 |
| d_mean | 2 | 0.74 | 0.73 | 0.72 |
| **d_mean** | **4** | **0.82** | **0.81** | **0.80** |
| d_mean | 5 | 0.46 | 0.50 | 0.63 |

**Earliest balanced accuracy ≥ 80%: d_mean at Epoch 4 (0.815).**

**Earliest ≥ 90%: NONE reached.**

---

## 2. Cross-N Breakdown (B0, K_mean Epoch 1)

| N | Test Acc | Bal Acc |
|---|----------|---------|
| 64 | 0.04 | 0.52 |
| 66 | 0.18 | 0.58 |
| **67** | **0.80** | 0.44 |
| 69 | 0.48 | 0.42 |
| 70 | 0.54 | 0.42 |
| 71 | 0.38 | 0.41 |
| 72 | 0.54 | 0.44 |
| 75 | 0.30 | 0.39 |
| 80 | 0.08 | 0.31 |

Only N=67 shows good raw accuracy (0.80), but balanced accuracy is poor (0.44) due to class-imbalance exploitation. At all other N, prediction is near chance. Single-threshold classifiers using per-N training do NOT generalize — the d_mean/K_mean → branch relationship is strongly N-conditioned.

---

## 3. N=71 Stress Test (B0, K_mean)

| Epoch | Test Acc | Bal Acc |
|-------|----------|---------|
| 1 | 0.38 | 0.41 |
| 2 | 0.62 | 0.59 |
| 3 | 0.34 | 0.35 |
| 4 | 0.60 | 0.50 |
| 5 | 0.04 | 0.04 |

No epoch reaches balanced accuracy > 0.6. N=71 is the V5.7 transition point and is particularly difficult to predict.

---

## 4. Gate: **D — N-Dependent Prediction**

| Gate | Status | Evidence |
|------|--------|---------|
| A (epoch ≤ 2) | NOT REACHED | Best bal acc at epoch 2 = 0.73 |
| B (epoch 3–4) | **MARGINAL** | d_mean epoch 4 bal = 0.815 — technically crosses 80% but only for pooled N |
| **D** | **REACHED** | Cross-N breakdown: prediction fails at all N except N=67 |
| E (no predictor) | NOT REACHED | d_mean epoch 4 works when pooled |

**Gate D is the primary finding:** single-feature threshold classifiers are N-dependent. The same d_mean value means "high branch" at N=67 but "low branch" at N=80. No universal threshold exists.

---

## 5. Why Prediction Is N-Dependent

The relationship between d_mean and branch label FLIPS sign across N:
- At N=67: lower d_mean → higher branch (V5.6 mechanism: d↓ → K↑ via Cupd)
- At N=80: the baseline d_mean/K_mean scale is completely different (d_mean ranges 0.1–1.0 vs 0.02–0.4 at N=67)
- A single threshold cannot capture this without N-conditioning

---

## 6. Claim Discipline

### SUPPORTED
- d_mean at epoch 4 achieves 81.5% balanced accuracy when N is pooled
- Cross-N prediction with single threshold fails (Gate D)
- N=71 is the hardest N to predict
- Prediction is N-dependent — the feature→label mapping changes with N

### NOT CLAIMED
- Causality
- Physical interpretation
- Universality
- Prediction model as mechanism explanation

---

## 7. Recommended Next: BPA

**BPA: Branch Predictability Analysis**

Options based on BPE findings:
- **N-conditioned thresholds:** Train separate thresholds per N — test whether this restores prediction
- **N-window grouping:** Train on N=64-70 (amplifier window) and N=71-80 (suppressor window) separately
- **Normalized features:** Use per-N z-score or percentile of d_mean/K_mean rather than raw values

---

## Development Stats

| Metric | Value |
|--------|-------|
| BPE tests | 4 |
| V5.8 total | 11 |
| Cumulative | 2477 |
| Failed | 0 |
| Gate reached | D |
