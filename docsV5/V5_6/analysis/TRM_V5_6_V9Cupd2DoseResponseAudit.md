# TRM V5.6 V9 Cupd2 Dose-Response Audit (MGCI)

**Date:** 2026-07-17  
**Branch:** `feature/v5.6-recoverfp-minimal-generative-core`  
**Status:** COMPLETE  
**Preceded by:** MGCP → MGCE → MGCA → MGCB → MGCD → MGCF → MGCG → MGCH  
**Followed by:** MGCJ (V9 Two-Component Decomposition Audit)

---

## Purpose

Quantify the dose-response relationship between pre-Cupd2 d_mean, K_mean amplification, and high-branch activation in the V9 Double-Cupd path.

MGCH established: V9's second Cupd compresses d (0.36→0.08) → amplifies K (0.99→1.15) with r=0.957. MGCI determines the precise dose-response and whether K_mean mediates the effect.

---

## Settings

| Parameter | Value |
|-----------|-------|
| N | 67, 69, 72 |
| Seeds (Stage 1) | 0–29 |
| Seeds (Stage 2) | 0–99 |
| Epochs | 5 |
| Branch threshold | Omega > 1.783 (V5.3 frozen) |

---

## Conditions

| ID | Pipeline |
|----|----------|
| B0 | Sm→RP→Nm→DL→Cupd |
| V9 (0% dose) | Sm→RP→DL→Cupd→DL→Cupd |
| V9+{dose}% | Sm→RP→DL→Cupd→DL→[dose]→Cupd |
| V9+{dose}%_C1 | Sm→RP→DL→[dose]→Cupd→DL→Cupd (placement control) |

---

## Cupd2 Dose-Response (N=67, seeds 0–29)

| Dose | Omega | High | dMean B4 Cupd2 | KMean After Cupd2 | Δ vs V9 KMean |
|------|-------|------|----------------|-------------------|---------------|
| 0% (V9) | 3.023 | **29/30** | 0.080 | 1.149 | — |
| 10% | 2.892 | **27/30** | 0.094 | 1.142 | −0.008 |
| 25% | 2.460 | **20/30** | 0.146 | 1.113 | −0.038 |
| 40% | 2.122 | **14/30** | 0.210 | 1.079 | −0.071 |
| 50% | 2.019 | **12/30** | 0.253 | 1.056 | −0.094 |
| **75%** | **1.756** | **9/30** | 0.351 | 1.007 | −0.143 |
| 100% | 1.792 | 10/30 | 0.438 | 0.965 | −0.185 |
| 125% | 1.742 | 9/30 | 0.526 | 0.927 | −0.224 |

### Threshold Analysis

| Milestone | Dose | Notes |
|-----------|------|-------|
| V9 baseline | 0% | 29/30 |
| First below V9 | **10%** | 27/30 (marginal) |
| Below 20/30 | **40%** | 14/30 |
| **Suppression plateau** | **75%** | **9/30 minimum** |

**First ≤B0: NONE.** Even at 125% dose, the d_mean operator cannot reduce V9 to B0 levels (9/30 vs 5/30).

**First zero: NONE.** Complete elimination is not achievable with d_mean alone. There appears to be a V9 component that is **d_mean-resistant**.

### Non-Monotonicity

The dose-response is **not strictly monotonic**: 75%→9/30, 100%→10/30, 125%→9/30. This suggests a floor effect — the d_mean operator hits a suppression limit around 9/30 that further dose cannot overcome.

---

## Mediation Analysis (240 seed×dose pairs)

| Relationship | Correlation |
|-------------|-------------|
| **d_mean → K_mean** | **r = −0.9985** |
| K_mean → Omega | r = 0.847 |
| d_mean → Omega | r = −0.824 |
| K_mean → HiBranch | r = 0.855 |
| d_mean → HiBranch | r = −0.838 |

### Gate B REACHED — K_mean is a near-perfect mediator

The d_mean → K_mean correlation is **r = −0.9985** — essentially deterministic. The Cupd exponential K = K₀·exp(−d/ξ) maps d to K with negligible noise.

K_mean → Omega (r = 0.847) and K_mean → HiBranch (r = 0.855) are strong but not perfect, indicating that **K_mean explains ~72% of Omega/branch variance** but other factors contribute to the remaining ~28%.

---

## Placement Control

| Dose | Placement | Omega | High | dMean B4 | KMean After |
|------|-----------|-------|------|----------|-------------|
| 0% | Cupd2 | 3.023 | 29/30 | 0.080 | 1.149 |
| 0% | Cupd1 | 3.014 | 29/30 | 0.080 | 1.150 |
| 50% | **Cupd2** | **2.019** | **12/30** | **0.253** | **1.056** |
| 50% | Cupd1 | 3.419 | 30/30 | 0.029 | 1.180 |
| 100% | **Cupd2** | **1.792** | **10/30** | 0.438 | 0.965 |
| 100% | Cupd1 | 3.590 | 30/30 | 0.025 | 1.183 |

### Gate E REACHED — Placement control confirmed

- **Cupd2 placement:** Suppression. Raising d before Cupd2 reduces K (1.15→0.97 at 100%), suppresses Omega (3.02→1.79), drops high-branch (29→10/30).
- **Cupd1 placement:** Amplification. Raising d before Cupd1 reduces Cupd1 K (making second d even smaller), which amplifies Cupd2 K (1.15→1.18 at 100%) and pushes high-branch to 30/30.

---

## Mechanism Classification

The V9 high-branch activation has **two components**:

| Component | Mechanism | Suppressible? |
|-----------|-----------|---------------|
| **C1: d-Compressible** | Second Cupd compresses d → amplifies K via exp(−d/ξ) | **YES** — d_mean dose before Cupd2 reduces from 29→~9/30 |
| **C2: d-Resistant** | Remaining ~9/30 high-branch that persists even at 125% dose | **NO** — not suppressible by d_mean alone |

The d-compressible component is the **dominant mechanism** (20/30 seeds respond). It operates through the deterministic d→K Cupd exponential (r=−0.998). But there is a **floor effect**: approximately 9 seeds are structurally resistant to d_mean-based suppression, suggesting they access a different region of K-state space.

---

## Gate Summary

| Gate | Description | Status |
|------|-------------|--------|
| A | Cupd2 dose-response monotonic | **NOT REACHED** (non-monotonic, floor effect) |
| **B** | **K_mean mediates d→Omega** | **REACHED** (r=−0.998 d→K, r=0.847 K→O) |
| C | Threshold response | NOT REACHED (gradual, not sharp drop) |
| D | N-dependent dose | Stage 2 (LongRunning) |
| **E** | **Placement confirmed** | **REACHED** (Cupd2 suppresses, Cupd1 amplifies) |

---

## Claim Discipline

### SUPPORTED
- Cupd2 dose-response curve as measured
- K_mean mediation: r(d→K) = −0.998, r(K→O) = 0.847
- Placement control: Cupd2 placement suppresses, Cupd1 placement amplifies
- Two-component V9: compressible (d_mean-responsive) + resistant (floor at ~9/30)
- Suppression plateau at 75% dose

### CONDITIONAL
- Stage 1: N=67, seeds 0–29
- Stage 2: N=67, 69, 72, seeds 0–99 (LongRunning)
- Two-component decomposition based on N=67 only

### NOT CLAIMED
- Physical interpretation
- Universality beyond tested N/seeds
- Complete V9 mechanism explanation

---

## Recommended Next Suite

**MGCJ: V9 Two-Component Decomposition Audit**

MGCI identified a floor effect: ~9/30 seeds resist d_mean suppression. Next step:
- Characterize the d-resistant seeds: what distinguishes them?
- Compare K-state of resistant vs susceptible seeds at 0% dose
- Test whether the d-resistant component can be suppressed by combining d_mean with another operator
- Determine whether the floor is N-dependent

---

## Files

| File | Description |
|------|-------------|
| `TRM.Tests/V5_6/V5_6_V9Cupd2DoseResponseAudit_Tests.cs` | Full test suite (6 tests) |
| `docsV5_6/analysis/TRM_V5_6_V9Cupd2DoseResponseAudit.md` | This document |

---

## Development Statistics

| Metric | Value |
|--------|-------|
| Tests added | 6 (1 LongRunning) |
| Total tests | 2425 |
| Passed | 2425 |
| Failed | 0 |
| Gates reached | B, E |
