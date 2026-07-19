# TRM V5.8 Final Synthesis: Branch Predictability and Forecasting

**Date:** 2026-07-17  
**Branch:** `feature/v5.8-branch-predictability-and-forecasting`  
**Status:** COMPLETE  
**Suites:** BPP, BPE, BPA, BPC (4 suites)  
**Tests:** 21 V5.8 tests, 2487 cumulative, 0 failed  
**Followed by:** `feature/v5.9-branch-commitment-and-irreversibility`

---

## 1. V5.8 Research Question

**How early can final branch identity be predicted from RecoverFP internal state?**

V5.8 was a forecasting branch — prediction without mechanism inference.

---

## 2. Final Answer

Branch identity is forecastable but **strongly N-conditioned and late-stage**.

### Forecastability Summary

| Aspect | Strength |
|--------|----------|
| Global (pooled) forecasting | **WEAK** |
| Per-N forecasting | **GOOD** |
| Early forecasting (epochs 1–3) | **WEAK** |
| Late forecasting (epoch 5) | **STRONG** |

---

## 3. Suite-by-Suite Summary

### BPP — Protocol (7 tests)
Pre-registered prediction problem: target = final branch label (Omega > 1.783), predictors = RecoverFP internal state at each epoch. Train/test split: seeds 0–49 / 50–99. Decision gates A–E pre-registered.

### BPE — Cross-Regime Execution (4 tests)
**Gate D (N-dependent).** Pooled prediction across all N weak: best = d_mean epoch 4, bal acc = 0.815. Cross-N breakdown shows prediction fails at all N except N=67. Single global thresholds are invalid — feature→label mapping changes with N.

### BPA — N-Conditioned Predictor Analysis (5 tests)
**Gates B, D.** Per-N threshold training restores prediction. Mean bal acc across N = 0.791. N=80 near-perfect (0.978). N=71 predictable at epoch 5 (0.967). Pooled z-score normalization (0.703) weaker than per-N thresholds. N-conditioning is necessary.

### BPC — Hard Regime Calibration (5 tests)
**Gates B, D.** Best epoch = 5 for EVERY class-stable N. d_mean alone at epoch 5 + per-N threshold: bal ≥ 0.93 for all N. Richer features (d_std, K_mean, trajectory deltas) add ≤ 0.06 — negligible improvement. N=64,66 are class-imbalance artifacts. Branch prediction is fundamentally late-stage.

---

## 4. Supported Performance (d_mean Epoch 5, per-N threshold)

| N | Bal Acc | Status |
|---|---------|--------|
| 64 | — | LOW-N (artifact) |
| 66 | — | LOW-N (artifact) |
| **67** | **0.989** | STABLE |
| 69 | 0.931 | STABLE |
| 70 | 0.957 | STABLE |
| **71** | **0.967** | STABLE |
| 72 | 0.947 | STABLE |
| 75 | 0.972 | STABLE |
| **80** | **1.000** | STABLE |

---

## 5. Supported Findings

- Branch prediction is N-conditioned — global pooled thresholds fail
- Per-N thresholds substantially improve forecasting
- d_mean is the strongest practical predictor — richer features add little
- Epoch 5 prediction is highly accurate (bal ≥ 0.93 for all class-stable N)
- N=71 is predictable at epoch 5 (0.967) — not a forecasting blind spot
- Train-test gaps are small (< 0.04) — no overfitting evidence
- N=64 and N=66 are class-imbalance artifacts — scores not meaningful

---

## 6. Conditional Findings

- d_mean is the strongest forecasting coordinate (tested features: d_mean, d_std, K_mean, trajectory deltas)
- Forecasting requires N-conditioning (per-N threshold or N as feature)
- Earlier prediction may require richer state representations not tested

---

## 7. Weakened / Disconfirmed

- Universal global prediction threshold (BPE Gate D)
- Early branch prediction (epochs 1–3 weak at all class-stable N)
- Strong gains from feature expansion (F4/F5 don't beat F1)
- Universal predictor transfer across N (N-conditioning required)

---

## 8. NOT CLAIMED

- Causality from prediction accuracy
- Mechanism from forecasting performance
- Physical interpretation
- Time, space, length, c, relativity, quantum mechanics, cosmology
- Universality beyond tested N and seed splits

---

## 9. Final V5.8 Conclusion

Branch identity is forecastable, but forecasting is strongly N-conditioned and fundamentally late-stage. Reliable prediction appears at epoch 5 for all class-stable N. Per-N d_mean thresholds provide near-perfect prediction (bal ≥ 0.93), while global pooled thresholds fail. Richer features (d_std, K_mean, trajectory deltas) add negligible value over d_mean alone.

V5.8 supports **late-stage branch predictability** rather than early universal forecasting.

---

## 10. Recommended V5.9

**Branch:** `feature/v5.9-branch-commitment-and-irreversibility`

**Central question:** When does branch identity become effectively irreversible?

V5.8 showed branches become predictable late. V5.9 asks: **when do they become committed?** Can a branch flip after epoch 3? After epoch 4? Can intervention change the outcome late?

---

## 11. Development Statistics

| Suite | Tests |
|-------|-------|
| BPP | 7 |
| BPE | 4 |
| BPA | 5 |
| BPC | 5 |
| **V5.8 total** | **21** |
| **Cumulative** | **2487** |
| **Failed** | **0** |

## 12. Gate Summary

| Suite | Gates Reached |
|-------|---------------|
| BPE | D (N-dependent) |
| BPA | B (Partially predictable), D (Late-only) |
| BPC | B (Late-only), D (Class-imbalance) |
