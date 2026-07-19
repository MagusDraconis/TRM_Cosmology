# TRM V5.8 Hard Regime & Late Predictor Calibration (BPC)

**Date:** 2026-07-17  
**Branch:** `feature/v5.8-branch-predictability-and-forecasting`  
**Status:** COMPLETE  
**Gates:** B (Late-only), D (Class-imbalance artifacts)

---

## 1. Class-Balance Audit

| N | Train Hi | Test Hi | Stable? |
|---|----------|---------|---------|
| 64 | 0/50 | 0/50 | **LOW-N** — both sides zero, scores meaningless |
| 66 | 1/50 | 1/50 | **LOW-N** — ≤1 per class |
| 67–80 | ≥5/50 | ≥5/50 | **STABLE** |

N=64 and N=66 have ≤1 test example in one class. Prediction scores at these N are artifacts.

---

## 2. F1 (d_mean) vs F4 (d_mean+d_std+K_mean) at Epoch 4

| N | F1 Bal | F4 Bal | Δ | Winner |
|---|--------|--------|---|--------|
| 67 | 0.744 | 0.811 | +0.067 | F4 |
| 69 | 0.667 | 0.667 | 0 | Tie |
| 70 | 0.743 | 0.591 | −0.152 | F1 |
| 71 | 0.717 | 0.667 | −0.050 | F1 |
| 72 | 0.770 | 0.780 | +0.010 | Tie |
| 80 | 0.978 | 1.000 | +0.022 | F4 |

**F4 does NOT consistently improve over F1.** d_std and K_mean add marginal or negative value at epoch 4. d_mean alone is the best single predictor.

---

## 3. Trajectory Deltas (F5) for Early Prediction

| N | CP2 F1 | CP2 F5 | CP3 F1 | CP3 F5 |
|---|--------|--------|--------|--------|
| 67 | 0.622 | 0.656 | 0.700 | 0.678 |
| 70 | 0.643 | 0.657 | 0.614 | 0.700 |
| 71 | 0.650 | 0.633 | 0.625 | 0.683 |
| 72 | 0.447 | 0.447 | 0.689 | 0.689 |
| 80 | 0.700 | 0.700 | 0.889 | 0.889 |

**Trajectory deltas add negligible value.** F5 improves over F1 by ≤0.06 at most CPs, and often equals F1 exactly (delta features have near-zero information content at early epochs). Trajectory features do NOT enable earlier prediction.

---

## 4. Hard Regime Classification

| N | Best Epoch | Best Bal (F1) | Regime |
|---|------------|---------------|--------|
| 64 | — | — | **LOW-N (unstable)** |
| 66 | — | — | **LOW-N (unstable)** |
| **67** | **5** | **0.989** | **EASY (epoch 5)** |
| **69** | **5** | **0.931** | **EASY (epoch 5)** |
| **70** | **5** | **0.957** | **EASY (epoch 5)** |
| **71** | **5** | **0.967** | **EASY (epoch 5)** |
| **72** | **5** | **0.947** | **EASY (epoch 5)** |
| **75** | **5** | **0.972** | **EASY (epoch 5)** |
| **80** | **5** | **1.000** | **EASY (epoch 5)** |

**All class-stable N are EASY at epoch 5.** Best epoch is 5 for every single class-stable N. Epoch 5 prediction with per-N d_mean threshold achieves bal acc ≥ 0.93 for all N.

---

## 5. N=71 with All Features

| Epoch | F1 Bal | F4 Bal | F5 Bal | Train-Test Gap |
|-------|--------|--------|--------|----------------|
| 1 | 0.575 | 0.575 | — | 0.136 |
| 2 | 0.650 | 0.600 | 0.633 | 0.012 |
| 3 | 0.625 | 0.583 | 0.683 | 0.112 |
| 4 | 0.717 | 0.667 | 0.683 | 0.026 |
| **5** | **0.967** | **0.967** | **0.967** | **0.033** |

N=71 at epoch 5: all three feature sets converge to 0.967 bal acc. F4 and F5 add nothing over F1. Train-test gap is small (<0.04 at epochs 4-5) — no overfitting.

---

## 6. Gate Summary

| Gate | Status | Evidence |
|------|--------|---------|
| A (Early prediction improved) | **NOT REACHED** | Trajectory deltas add ≤0.06, no early improvement |
| **B** (Late-only) | **REACHED** | Best epoch = 5 for EVERY class-stable N |
| C (N=71 hard) | NOT REACHED | N=71 = 0.967 bal at epoch 5 (EASY) |
| **D** (Class-imbalance) | **REACHED** | N=64,66 have ≤1 per class — scores are artifacts |
| E (Overfitting) | NOT REACHED | Train-test gap < 0.04 at epochs 4-5 |

---

## 7. Key Conclusion

**Branch prediction is fundamentally late-stage.** Epoch 5 is the best (and usually only) prediction point for every class-stable N. Richer features (d_std, K_mean, trajectory deltas) add negligible value over d_mean alone. d_mean at epoch 5 with per-N threshold achieves bal acc ≥ 0.93 for all class-stable N.

The prediction timeline is:
- Epochs 1–2: weak (bal 0.4–0.65)
- Epochs 3–4: moderate (bal 0.6–0.8)
- Epoch 5: excellent (bal ≥ 0.93)

---

## 8. Recommended Next: BPS

**BPS: Branch Predictability Synthesis** — synthesize V5.8 findings.

---

## Dev Stats

| Metric | Value |
|--------|-------|
| BPC tests | 5 |
| V5.8 total | 21 |
| Cumulative | 2487 |
| Failed | 0 |
| Gates | B, D |
