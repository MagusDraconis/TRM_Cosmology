# TRM V5.8 — Branch Predictability and Forecasting

**Branch:** `feature/v5.8-branch-predictability-and-forecasting`  
**Date:** 2026-07-17  
**Base:** V5.7 COMPLETE (a80fb6c)  
**Status:** INITIALIZED  
**Type:** Prediction and forecasting branch

---

## Purpose

How early can the final branch identity (high/low Omega) be predicted from RecoverFP internal state?

V5.6–V5.7 characterized the mechanism (d→Cupd→K controls branch outcome). V5.8 asks: **when does the branch outcome become predictable?**

---

## Directory Structure

```
docsV5_8/
├── README.md
├── experiments/TRM_V5_8_Experiment_Log.md
├── theory/TRM_V5_8_BranchPredictability_Roadmap.md
├── protocols/TRM_V5_8_BranchPredictability_Protocol.md
└── analysis/

TRM.Tests/V5_8/
└── V5_8_BranchPredictabilityProtocol_Tests.cs
```

---

## Core Question

Given RecoverFP internal state at each epoch, can we predict which seeds will end up as high-branch (Omega > 1.783) vs low-branch?

---

## Key Constraints

- Branch threshold frozen at Omega > 1.783 (V5.3)
- No physical interpretation
- No new theory — this is a prediction problem using known RecoverFP state

---

## Potential Predictors

- d_mean before/after Cupd at each epoch
- K_mean, K_std, KLam1, KFrob at each epoch
- dRatio (d_before_Cupd2 / d_after_Cupd1 for V9)
- Omega at intermediate epoch
- MeanDist
- State-conditioned operator d-shift magnitude
- Per-epoch branch label (intermediate threshold check)

---

## Suites

| Suite | Purpose |
|-------|---------|
| BPP | Protocol: define predictors, evaluation metrics, train/test policy |
| BPE | Execution: sweep predictors across N and epochs |
| BPA | Analysis: evaluate predictor accuracy, earliest-prediction epoch |
| BPS | Synthesis: branch completion |
