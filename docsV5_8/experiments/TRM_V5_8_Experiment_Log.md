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
| 2026-07-17 | BPE | 4 | GATE D (N-dependent): d_mean Epoch 4 bal=0.815 for pooled N. Cross-N prediction fails at all N except N=67. N=71 is hardest (bal < 0.6 at all epochs). No universal threshold exists — prediction is N-conditioned. |
