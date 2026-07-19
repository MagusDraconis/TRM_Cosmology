# TRM V5.6 V9 Two-Component Decomposition (MGCJ)

**Date:** 2026-07-17  
**Branch:** `feature/v5.6-recoverfp-minimal-generative-core`  
**Status:** COMPLETE  
**Preceded by:** MGCP → MGCE → MGCA → MGCB → MGCD → MGCF → MGCG → MGCH → MGCI  
**Followed by:** MGCK (Stronger V9 Suppressor Audit)

---

## Purpose

Characterize the d_mean-resistant V9 residual discovered in MGCI — which seeds remain high-branch after correct Cupd2 d_mean suppression, and what distinguishes them from compressible seeds.

---

## Settings

| Parameter | Value |
|-----------|-------|
| N | 67, 69, 72 |
| Seeds (Stage 1) | 0–29 |
| Seeds (Stage 2) | 0–99 |
| Epochs | 5 |
| Doses | 0%, 40%, 75%, 100%, 125% |

---

## Seed Classification (N=67, seeds 0–29)

| Class | Count | Description |
|-------|-------|-------------|
| **S0** | 1 | Never high in V9 |
| **S1** | 20 | **Compressible** — high in V9, becomes low under d_mean dose |
| **S2** | 9 | **Resistant** — high in V9, stays high at ALL doses |

## S2 Seeds: 0, 2, 12, 14, 16, 17, 20, 26, 28

These 9 seeds form a distinct, non-overlapping subset of the V9 ensemble.

---

## S1 vs S2 Comparison (at 0% dose = V9 baseline)

| Metric | S1 Mean | S1 Std | S2 Mean | S2 Std | Cohen's d |
|--------|---------|--------|---------|--------|-----------|
| **dMean B4 Cupd2** | **0.086** | 0.048 | **0.022** | 0.002 | **1.87** |
| dStd B4 Cupd2 | 0.073 | 0.043 | 0.017 | 0.002 | 1.84 |
| dP90 B4 Cupd2 | 0.170 | 0.097 | 0.041 | 0.004 | 1.90 |
| KMean aft Cupd2 | 1.144 | 0.030 | 1.185 | 0.001 | 1.88 |
| **KStd aft Cupd2** | **0.047** | 0.027 | **0.011** | 0.001 | **1.86** |
| KLam1 aft Cupd2 | 75.53 | 2.00 | 78.20 | 0.08 | 1.88 |
| KFrob aft Cupd2 | 76.19 | 1.95 | 78.80 | 0.08 | 1.89 |
| Omega | 2.855 | 0.521 | 3.611 | 0.161 | 1.96 |

### Key Observations

1. **S2 dMean is 3.9× smaller** (0.022 vs 0.086) — S2 seeds compress d much more aggressively before Cupd2.
2. **S2 KStd is 4.3× smaller** (0.011 vs 0.047) — S2 produces near-uniform K matrices.
3. **S2 Omega is significantly higher** (3.61 vs 2.86, Cohen's d=1.96) — convincingly separated.
4. **S2 seeds are ONLY V9-high:** 0/9 are high in B0 or V1. They are uniquely activated by the Double-Cupd pathway.

---

## B0/V1 Overlap

| Group | B0 High | V1 High |
|-------|---------|---------|
| S1 (20 seeds) | 5/20 (25%) | 11/20 (55%) |
| **S2 (9 seeds)** | **0/9 (0%)** | **0/9 (0%)** |

S2 seeds are a **purely V9 phenomenon** — they show zero high-branch activation in B0 or V1. The Double-Cupd mechanism uniquely activates these seeds.

---

## KMean Residual Analysis

| Metric | Value |
|--------|-------|
| S1 max KMean when LOW | **1.115** |
| S2 min KMean at any dose | **1.174** |
| Gap | **0.059 (5.3%)** |

S2 seeds reach a minimum KMean of 1.174 even at 125% d_mean dose. S1 seeds suppress (go low) when KMean drops to ≈1.115. There is a clean KMean threshold gap:
- KMean ≤ 1.115 → suppressed (S1)
- KMean ≥ 1.174 → resistant (S2)

The d_mean operator before Cupd2 cannot push S2 KMean below 1.174 because S2 seeds start with dMean=0.022 (requiring a ~5.5× multiplicative increase to reach S1-level dMean=0.086). The state-conditioned operator (d += 0.5×dMean_current) only adds ~0.011 at 100% dose — far from enough.

---

## Gate Summary

| Gate | Description | Status |
|------|-------------|--------|
| **A** | **Residual is higher K threshold** | **REACHED** (S2 ≥ 1.174, S1 ≤ 1.115) |
| **B** | **Residual is K-spectral** | **REACHED** (S2 KStd=0.011, 4.3× tighter than S1) |
| C | Residual is d-distribution | REACHED (S2 dMean 3.9× smaller) |
| **D** | **Residual is seed-stable** | **REACHED** (same 9 seeds at all doses) |
| E | N-dependent | Stage 2 (LongRunning) |

---

## Mechanism Classification

### S1 (Compressible, 20 seeds)
- dMean B4 Cupd2 ≈ 0.086 (moderate compression)
- KMean ≈ 1.14, KStd ≈ 0.05 (moderate variance K)
- d_mean op can raise d → lower K → suppress branch
- **Mechanism: d-compressible K-magnitude amplification**

### S2 (Resistant, 9 seeds)
- dMean B4 Cupd2 ≈ 0.022 (extreme compression — 3.9× smaller)
- KMean ≈ 1.19, KStd ≈ 0.01 (near-uniform K)
- d_mean op cannot raise d enough → KMean stays > 1.174 threshold
- Purely V9-activated: 0/9 high in B0 or V1
- **Mechanism: Extreme d-compression producing uniform K above threshold**

The two components are the SAME mechanism (d-compression → K amplification) operating at different magnitudes. S2 is simply the extreme end of the distribution — seeds where Cupd1 produces a K state that leads to near-complete d collapse in the second pass.

---

## Claim Discipline

### SUPPORTED
- Seed classification: S0=1, S1=20, S2=9 at N=67
- S1 vs S2 separation: all metrics Cohen's d > 1.8
- S2 seeds are 0% B0-high, 0% V1-high — purely V9-activated
- KMean threshold gap: S1 suppress ≤ 1.115, S2 floor ≥ 1.174

### CONDITIONAL
- Stage 1: N=67, seeds 0–29
- Stage 2: N=67, 69, 72, seeds 0–99 (LongRunning)

### NOT CLAIMED
- Physical interpretation
- Universality beyond tested N/seeds
- S2 as categorically distinct mechanism (it's the same d→K mechanism at extreme d-compression)

---

## Recommended Next Suite

**MGCK: Stronger V9 Suppressor Audit**

S2 seeds resist because the current d_mean operator (d += 0.5×dMean_current) cannot overcome their extreme d-compression. Options:
- **Higher alpha:** Test α=1.0, 1.5, 2.0 (stronger state-conditioned multipliers)
- **Fixed large shift:** Test absolute d_mean shifts larger than state-conditioned
- **d-scale operator:** Multiply d by factor > 1 instead of adding shift
- **Target S2 specifically:** Calibrate per-seed suppressor and test generalization
- **Combine d_mean + d_std operator:** Test whether spreading d variance also helps

---

## Files

| File | Description |
|------|-------------|
| `TRM.Tests/V5_6/V5_6_V9TwoComponentDecomposition_Tests.cs` | Full test suite (6 tests) |
| `docsV5_6/analysis/TRM_V5_6_V9TwoComponentDecomposition.md` | This document |

---

## Development Statistics

| Metric | Value |
|--------|-------|
| Tests added | 6 (1 LongRunning) |
| Total tests | 2430 |
| Passed | 2430 |
| Failed | 0 |
| Gates reached | A, B, C, D |
