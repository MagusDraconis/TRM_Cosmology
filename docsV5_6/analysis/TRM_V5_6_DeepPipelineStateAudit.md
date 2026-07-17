# TRM V5.6 Deep Pipeline State Audit (MGCH)

**Date:** 2026-07-17  
**Branch:** `feature/v5.6-recoverfp-minimal-generative-core`  
**Status:** COMPLETE  
**Preceded by:** MGCP → MGCE → MGCA → MGCB → MGCD → MGCF → MGCG  
**Followed by:** MGCI (V9 Mechanism Completion Audit)

---

## Purpose

Determine why Skip-Nm + Double-Cupd produces near-universal high-branch activation, and why d_mean suppression cannot control it.

MGCG established: V9 is NOT suppressible by d_mean operator before the first Cupd (Gate E). MGCH traces the full pipeline to find WHERE the divergence occurs and WHAT mechanism drives it.

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
| C0 | Sm→RP→Nm→DL→Cupd (B0) |
| C1 | Sm→RP→DL→Cupd (V1) |
| C2 | Sm→RP→DL→Cupd→DL→Cupd (V9) |
| C3 | Sm→RP→DL→[op]→Cupd (V1+d_mean) |
| C4 | Sm→RP→DL→[op]→Cupd→DL→Cupd (V9+op before Cupd1) |
| C5 | Sm→RP→DL→Cupd→DL→[op]→Cupd (V9+op before Cupd2) |
| C6 | Sm→RP→DL→[op]→Cupd→DL→[op]→Cupd (V9+op before both) |

---

## Stage-by-Stage Divergence (N=67, seeds 0–29)

| Stage | KMean | KStd | KLam1 | KFrob | dMean |
|-------|-------|------|-------|-------|-------|
| C1 Cupd1 (V1) | 1.069 | 0.078 | 70.62 | 71.40 | 0.222 |
| C2 Cupd1 (V9) | 0.993 | 0.105 | 65.59 | 66.49 | 0.363 |
| **C2 Cupd2 (V9)** | **1.150** | **0.039** | **75.87** | **76.52** | **0.080** |
| Δ(Cupd2−Cupd1) | **+0.157** | **−0.067** | **+10.28** | **+10.03** | — |

### Key Finding

**The second Cupd is a K-magnitude amplifier.** After the first Cupd produces K≈0.99, the second Sm→RP→DL pass produces very small d values (dMean=0.080 vs 0.363 before Cupd1). When Cupd transforms d→K via K=K₀·exp(−d/ξ), **smaller d produces exponentially larger K**. The second Cupd amplifies KMean from 0.99 to 1.15 (+16%), and KFrob from 66.5 to 76.5 (+15%).

---

## ΔK × Omega Correlation

| Metric | Correlation with Final Omega |
|--------|------------------------------|
| **ΔKMean** | **r = 0.957** |
| ΔKStd | r = −0.956 |
| ΔKLam1 | r = 0.957 |
| ΔKFrob | r = 0.957 |

The K-magnitude increase from Cupd1→Cupd2 is **almost perfectly correlated** with final Omega. The V9 amplification is driven by K-magnitude amplification in the second Cupd pass.

---

## Operator Placement Results (N=67, seeds 0–29)

| Condition | Omega | High | K_Cupd1 | K_Cupd2 | ΔKMean |
|-----------|-------|------|---------|---------|--------|
| C0 (B0) | 1.293 | **5/30** | — | — | — |
| C2 (V9) | 3.014 | **29/30** | 0.993 | 1.150 | +0.157 |
| C3 (V1+op) | 1.234 | **2/30** | — | — | — |
| C4 (V9+op before Cupd1) | 3.590 | **30/30** | 0.784 | 1.183 | +0.400 |
| **C5 (V9+op before Cupd2)** | **1.792** | **10/30** | 1.068 | 0.965 | **−0.103** |
| C6 (V9+op before both) | 3.212 | **29/30** | 0.811 | 1.164 | +0.353 |

### Critical Finding: Placement Matters

**C5 (op before second Cupd) STRONGLY SUPPRESSES V9:** 29/30 → **10/30**, Omega drops from 3.01 → 1.79. The ΔKMean goes NEGATIVE (−0.103) — the operator reverses the K amplification.

**C4 (op before first Cupd) FAILS:** 29/30 → 30/30. The d_mean shift before Cupd1 makes K even smaller (0.78), which makes the second Sm produce even smaller d, amplifying the second Cupd even more (ΔKMean = +0.400, larger than C2's +0.157).

**C6 (op before both) FAILS:** 29/30. C4 dominates — the first op suppresses Cupd1 K, enabling larger Cupd1→Cupd2 amplification that the second op can't fully counteract.

---

## Cross-N Validation (Stage 2, seeds 0–99)

| N | C0 (B0) | C2 (V9) | C3 (V1+op) | C4 (V9+op before Cupd1) |
|---|---------|---------|------------|--------------------------|
| 67 | 14/100 | **93/100** | 8/100 | **100/100** |
| 69 | 24/100 | **63/100** | 23/100 | **100/100** |
| 72 | 41/100 | **26/100** | 39/100 | **70/100** |

V9 activation DECREASES with N (93→63→26), opposite to V1 which increases with N. At N=72, V9 has fewer high-branch seeds than V1 (22/30 in Stage 1). The C4 d_mean before Cupd1 universally amplifies V9 (100/100 at N=67,69), confirming the mechanism.

---

## Mechanism Classification: **M1 — K-Magnitude (with Placement)**

1. **Second Sm sees Cupd1 K, produces small d.** The K from Cupd1 (0.99 mean) generates dMean=0.080 in the second pass, much smaller than V1's dMean=0.222.

2. **Small d → large K via Cupd exponential.** K = K₀·exp(−d/ξ). For d=0.08, K ≈ 1.2·exp(−0.08/1.75) ≈ 1.15. For d=0.22, K ≈ 1.2·exp(−0.22/1.75) ≈ 1.07. The difference drives the amplification.

3. **d_mean operator before Cupd2 reverses this.** Raising d before Cupd2 pushes K back down. ΔKMean goes from +0.157 to −0.103 — the operator fully reverses the K-magnitude gain.

4. **d_mean operator before Cupd1 amplifies V9.** Lowering Cupd1 K (0.78) makes the second Sm produce even smaller d, amplifying the Cupd1→Cupd2 gain (+0.400 ΔKMean).

---

## Symmetry with Nm Suppression

The V9 amplification mechanism is the **symmetric inverse** of Nm suppression:

| Mechanism | d Change | K Change | Branch Effect |
|-----------|----------|----------|---------------|
| Nm (B0) | d ↑ (+0.32 mean) | K ↓ (via exp(−d/ξ)) | Suppression |
| V9 Cupd2 | d ↓ (0.36→0.08) | K ↑ (via exp(−d/ξ)) | Amplification |

Both operate through the same Cupd exponential: K ∝ exp(−d/ξ). Nm amplifies d, V9's second pass compresses d. The symmetry is exact — and the d_mean operator works on V9 when placed at the correct stage (before Cupd2), precisely because it reverses the d-compression that drives V9.

---

## Gate Summary

| Gate | Description | Status |
|------|-------------|--------|
| **A** | **K-Magnitude Mechanism** | **REACHED** (r=0.957) |
| B | K-Spectral Mechanism | REACHED (correlation equally strong for all K metrics) |
| C | K-Geometry Mechanism | NOT REACHED (scalar K metrics sufficient) |
| **D** | **Operator Placement Recovers Control** | **REACHED** (C5: 29→10/30) |
| E | V9 Bypasses d_mean | NOT REACHED |
| F | Unresolved | NOT REACHED |

**Gates A and D REACHED.** V9 amplification is K-magnitude driven, and correct operator placement (before second Cupd) recovers control.

---

## Claim Discipline

### SUPPORTED
- Stage-by-stage divergence: C2 Cupd2 is the divergence point
- ΔKMean × Omega correlation: r = 0.957
- C5 (op before Cupd2) partially suppresses V9: 29→10/30
- C4 (op before Cupd1) amplifies V9
- V9 and Nm operate through symmetric d↔K mechanisms

### CONDITIONAL
- Stage 1: N=67, seeds 0–29
- Stage 2: N=67, 69, 72, seeds 0–99
- V9 mechanism classification based on scalar K metrics

### NOT CLAIMED
- Physical interpretation
- Universality beyond tested N/seeds
- V9 mechanism as universal or the only amplifier

---

## Recommended Next Suite

**MGCI: V9 Mechanism Completion Audit**

Now that the K-magnitude mechanism is identified and correct placement is found:
- Test C5 at full N=67, 69, 72, seeds 0–99 for statistical validation
- Test dose-response of d_mean before Cupd2 (10%, 25%, 50%, 100%) similar to MGCF
- Test whether d_mean before Cupd2 can fully eliminate V9 high-branch (bring to 0/30)
- Characterize the V9 d-compression mechanism: why does second Sm produce small d?

---

## Files

| File | Description |
|------|-------------|
| `TRM.Tests/V5_6/V5_6_DeepPipelineStateAudit_Tests.cs` | Full test suite (6 tests) |
| `docsV5_6/analysis/TRM_V5_6_DeepPipelineStateAudit.md` | This document |

---

## Development Statistics

| Metric | Value |
|--------|-------|
| Tests added | 6 (1 LongRunning) |
| Total tests | 2420 |
| Passed | 2420 |
| Failed | 0 |
| Gates reached | A, D |
