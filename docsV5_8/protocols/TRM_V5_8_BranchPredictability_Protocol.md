# TRM V5.8 Branch Predictability Protocol (BPP)

**Date:** 2026-07-17  
**Suite:** BPP  
**Status:** DESIGN COMPLETE

---

## Purpose

Pre-register the V5.8 prediction protocol: predictors, evaluation metrics, train/test policy, decision gates, and claim boundaries.

---

## 1. Protocol Checks

### BPP.1 — Prediction Problem Defined

Target: final branch label (Omega > 1.783, V5.3 frozen). Predictors: RecoverFP internal state at each epoch 1–5.

### BPP.2 — Train/Test Split Pre-Registered

Training: seeds 0–29 (V5.6 calibration). Testing: seeds 30–59, 60–99 (independent blocks). No parameter tuning, no threshold tuning.

### BPP.3 — Predictors Pre-Registered

d_mean, d_std, K_mean, K_std, KLam1 at each epoch. Per-epoch Omega. dRatio for V9.

### BPP.4 — Evaluation Metrics Defined

Accuracy, precision, recall, F1, earliest-prediction epoch (balanced accuracy > 0.8).

### BPP.5 — No Black-Box Models

Simple threshold classifiers only. Transparent methods.

### BPP.6 — Decision Gates Pre-Registered

Gate A (epoch ≤ 2), Gate B (epoch 3–4), Gate C (epoch 5 only), Gate D (not predictable).

### BPP.7 — No Physical Interpretation

Stay within RecoverFP state prediction.

---

## 2. Claim Discipline

### NOT CLAIMED
- Physical interpretation
- Universality
- Causality beyond correlation
- Generalization beyond tested N and seeds

---

## 3. Next Suite

**BPE:** Branch Predictability Execution — sweep predictors across N and epochs.
