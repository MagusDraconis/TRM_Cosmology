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
| 2026-07-17 | BPA | 5 | GATES B,D: Per-N threshold restores prediction (mean bal=0.791). N=80 perfect (0.978). N=71 predictable at epoch 5 (0.967). Pooled z-score weak (0.703). Late-epoch dominates. |
