# TRM V5.8 — Experiment Log

**Branch:** `feature/v5.8-branch-predictability-and-forecasting`  
**Base:** V5.7 COMPLETE (`a80fb6c`)  
**Date:** 2026-07-17  
**Cumulative tests at branch start:** 2466 passed, 0 failed

---

## Initialization

V5.8 initialized from V5.7 COMPLETE (`a80fb6c`).

**Type:** Prediction and forecasting branch.

**Central question:** How early can final branch identity be predicted from RecoverFP internal state?

**Status:** INITIALIZED

---

## Experiment History

| Date | Suite | Tests | Outcome |
|:---|:---|:--:|:---|
| 2026-07-17 | — | — | BRANCH INITIALIZED from V5.7 COMPLETE |
| 2026-07-17 | BPP | — | Protocol designed. Predictors and evaluation metrics pre-registered. |
| 2026-07-17 | BPE | 4 | GATE D (N-dependent): d_mean Epoch 4 bal=0.815 pooled. Cross-N prediction fails. N=71 hardest. |
| 2026-07-17 | BPA | 5 | GATES B,D: Per-N threshold restores prediction. N=71 predictable at epoch 5 (0.967). |
| 2026-07-17 | BPC | 5 | GATES B,D: Best epoch = 5 for ALL class-stable N. F1 (d_mean) ≥ F4/F5 — richer features add nothing. N=64,66 are class-imbalance artifacts. Trajectory deltas don't help early prediction. d_mean epoch 5 + per-N threshold: bal ≥ 0.93 for all N. |
