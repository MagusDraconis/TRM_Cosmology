# TRM V5.8 — Branch Predictability Roadmap

**Date:** 2026-07-17  
**Base:** V5.6–V5.7 (mechanism characterized)  
**Branch:** `feature/v5.8-branch-predictability-and-forecasting`

---

## 1. Core Question

**How early can the final branch identity (high/low Omega) be predicted from RecoverFP internal state?**

---

## 2. Motivation

V5.6–V5.7 established that d→Cupd→K controls branch outcome. V5.8 shifts from **mechanism discovery** to **prediction**: given the mechanism, when does the outcome become knowable?

Key practical question: can we predict at epoch 1, 2, or 3 which seeds will end up high-branch? Or is the outcome only knowable at epoch 5?

---

## 3. Prediction Problem

- **Target:** Final branch label (Omega > 1.783)
- **Predictors:** RecoverFP internal state metrics at each epoch
- **Training:** seeds 0–29 (V5.6 calibration block)
- **Testing:** seeds 30–59, 60–99 (independent blocks)
- **N values:** 67, 69, 72 (V5.6 discovery N), plus 65, 70, 75

---

## 4. Predictor Candidates

| Predictor | Epoch Availability | Rationale |
|-----------|-------------------|-----------|
| d_mean before Cupd | 1–5 | V5.6: controls K_mean |
| d_std before Cupd | 1–5 | Distribution shape |
| K_mean after Cupd | 1–5 | V5.6: mediates Omega |
| K_std after Cupd | 1–5 | Variance |
| KLam1 after Cupd | 1–5 | Spectral |
| Omega at intermediate epoch | 1–5 | Direct branch proxy |
| dRatio (V9 only) | 2 (Cupd1→Cupd2) | V5.7: 100% V9 role predictor |
| Per-epoch branch label | 1–5 | Intermediate threshold check |

---

## 5. Evaluation Metrics

| Metric | Definition |
|--------|------------|
| Accuracy | Fraction of correct branch predictions |
| Precision | True positives / predicted positives |
| Recall | True positives / actual positives |
| F1 | Harmonic mean of precision and recall |
| Earliest epoch | First epoch where balanced accuracy > 0.8 |
| AUC | Area under ROC (continuous Omega) |

---

## 6. Train/Test Policy

- **NO parameter tuning** — predictors are pre-registered
- **NO threshold tuning** — branch threshold is frozen (V5.3)
- **NO cross-validation overfitting** — independent seed blocks
- Simple threshold classifiers only — no black-box models

---

## 7. Decision Gates

| Gate | Condition | Interpretation |
|------|-----------|----------------|
| A | Prediction > 0.8 accuracy at epoch ≤ 2 | Early prediction possible |
| B | Prediction > 0.8 accuracy at epoch 3–4 | Mid-pipeline prediction |
| C | Prediction only accurate at epoch 5 | Late prediction — full pipeline needed |
| D | Prediction < 0.8 accuracy at any epoch | Branch outcome not predictable from observed state |
