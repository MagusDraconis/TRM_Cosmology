# TRM V5.7 Reduced Operator Validation Protocol (ROCP)

**Date:** 2026-07-17  
**Suite:** ROCP (Reduced Operator Calibration Protocol)  
**Status:** DESIGN COMPLETE  
**Branch:** `feature/v5.7-recoverfp-reduced-operator-validation`

---

## Purpose

Pre-register the V5.7 validation protocol: axes, metrics, failure criteria, decision gates, and claim boundaries.

V5.7 does NOT confirm V5.6. It attempts to **falsify** V5.6 by testing the reduced operator mechanism outside the discovery regime.

---

## 1. Protocol Checks

### ROCP.1 — Cross-N Validation Defined

**N values:** 60, 62, 64, 66, 67, 69, 72, 75, 80

Rationale: V5.6 discovered the mechanism at N=67,69,72. V5.7 tests whether the mechanism persists below (60–66) and above (75–80) the discovery range.

For each N, execute:
- B0: Sm→RP→Nm→DL→Cupd
- V1: Sm→RP→DL→Cupd
- V1+Op: Sm→RP→DL→[d'=d+0.5*d_mean]→Cupd
- V9: Sm→RP→DL→Cupd→[d'=d+0.5*d_mean]→Cupd (at selected N)

Compare:
- High-branch fraction per condition per N
- d_mean → K_mean correlation per N
- Omega distributions per condition

### ROCP.2 — Cross-Seed Validation Defined

**Blocks:**
- Block 0: seeds 0–29 (original V5.6 calibration block)
- Block 1: seeds 30–59 (independent validation block)
- Block 2: seeds 60–99 (independent validation block)

For each block and selected N, execute B0, V1, V1+Op.

Compare:
- High-branch fraction per block
- Operator effect size per block
- Block homogeneity (are seed blocks interchangeable?)

### ROCP.3 — Reduced Operator Evaluation Metrics

| Metric | Definition | Pass Threshold |
|--------|------------|----------------|
| HiB0 | High-branch fraction in B0 | Must be reproducible across blocks |
| HiV1 | High-branch fraction in V1 | Must increase vs B0 at all N |
| HiOp | High-branch fraction in V1+Op | Must be ≤ HiB0 at N=67,69,72 |
| d→K r | d_mean vs K_mean correlation | Must be < −0.9 |
| K→Ω r | K_mean vs Omega correlation | Must be > 0.7 |
| Invalid rate | Fraction of seeds producing invalid d/K | Must be 0% |
| Block stability | Max difference in HiOp across blocks | Must be within ±20% of mean |

### ROCP.4 — Failure Criteria Defined

| ID | Condition | Threshold |
|----|-----------|-----------|
| F1 | Reduced operator fails at N outside 67–72 | HiOp > HiB0 at any N < 66 or > 72 |
| F2 | Reduced operator fails on Block 1 or Block 2 | HiOp differs from Block 0 by > 20% |
| F3 | Cupd2 amplification not reproducible | V9 Hi ≤ V1 Hi at tested N |
| F4 | d_mean no longer predicts K | d→K correlation > −0.7 at any N |
| F5 | State-conditioned operator unstable | Any invalid d/K at any N or seed |

### ROCP.5 — Branch Thresholds Frozen

**Threshold:** Omega > 1.783 (V5.3 frozen)

This threshold is NOT redefined, NOT recomputed, and NOT tuned for V5.7.

### ROCP.6 — V5.6 Mechanism as Hypothesis Under Test

All V5.6 findings are treated as hypotheses to be falsified, not as established truths to be confirmed.

### ROCP.7 — No Physical Interpretation

No physical interpretation is introduced or tested. Stay within RecoverFP operator mechanics.

### ROCP.8 — Decision Gates Pre-Registered

| Gate | Condition | Interpretation |
|------|-----------|----------------|
| **A** | All metrics pass at all N, all blocks | Mechanism robust — supports V5.6 model |
| **B** | Metrics pass at N=67–72, degrade at N<66 or N>72 | Partially robust — N-conditioned refinement needed |
| **C** | Metrics only pass at N=67 | Weak generalization — mechanism highly regime-dependent |
| **D** | Metrics fail at most conditions | Mechanism does NOT generalize — V5.6 was overfit |

---

## 2. Claim Discipline

### NOT CLAIMED in V5.7

- Physical time, space, length, or c
- Physical constants or their derivation
- Relativity, quantum mechanics, cosmology
- Emergence or attractor decomposition
- Universal criticality
- Mathematical minimality proof
- Generalization beyond tested N=60–80 and seeds 0–99

### CONDITIONAL on Validation Results

- d_mean as minimal suppressive coordinate (conditional on cross-N and cross-seed)
- State-conditioned operator as Nm-equivalent (conditional on operator robustness)
- V9 amplification mechanism (conditional on mechanism stability)
- S2 suppressibility (conditional on reproducibility)

---

## 3. Recommended Next Suite

**ROCE:** Reduced Operator Calibration Execution — execute the pre-registered cross-N and cross-seed validation plan.

---

## 4. Files

| File | Description |
|------|-------------|
| `docsV5_7/protocols/TRM_V5_7_ReducedOperatorValidation_Protocol.md` | This document |
| `TRM.Tests/V5_7/V5_7_ReducedOperatorValidationProtocol_Tests.cs` | Protocol test suite |
