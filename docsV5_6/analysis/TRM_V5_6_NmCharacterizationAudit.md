# TRM V5.6 Nm Characterization Audit (MGCB)

**Date:** 2026-07-17  
**Branch:** `feature/v5.6-recoverfp-minimal-generative-core`  
**Status:** COMPLETE  
**Preceded by:** MGCP → MGCE → MGCA  
**Followed by:** MGCD (recommended)

---

## Purpose

Determine **WHAT** Nm suppresses by tracing per-epoch state changes in B0 (with Nm) vs V1 (skip-Nm) across multiple metrics.

MGCA established that Nm is **branch-suppressive** (skip-Nm increases high-branch from 5/30 to 12/30, and skip-Nm + double-Cupd reaches 29/30). MGCB determines the **mechanism** of this suppression.

---

## Core Questions

1. What changes immediately after Nm?
2. Which quantities are most modified by Nm?
3. Does Nm compress distributions, reduce variance, reduce branch separation, reduce d/K amplification, change update direction, or act only on a subset of seeds?
4. Does Nm act mainly on future low-branch or high-branch seeds?

---

## Settings

| Parameter | Value |
|-----------|-------|
| N | 67, 69, 72 |
| Seeds (Stage 1) | 0–29 |
| Seeds (Stage 2) | 0–99 |
| Epochs | 5 |
| dt | 0.05 |
| Hd | 4 |
| xi | 1.75 |
| K0 | 1.2 |
| S | 0.10 |
| St | 300 |
| Branch threshold | Omega > 1.783 (V5.3 frozen) |

---

## Metrics Tracked Per Stage

For each epoch in B0 (Sm→RP→Nm→DL→Cupd), snapshots are taken at four points:

| Stage | State Available | Metrics Computed |
|-------|----------------|------------------|
| Before Nm | R (raw), d (from raw R), K (prev epoch) | d_mean, d_std, d_p75, d_p90, d_max, K_mean, K_std, KLam1, Omega, MeanDist, StateNorm |
| After Nm | Rn, d (from Rn), K (prev epoch) | Same as above |
| After DL | Rn, d (from Rn), K (prev epoch) | Same as above |
| After Cupd | Rn, d (from Rn), K (new) | Same as above |

The Nm delta is computed as: `AfterNm.metric - BeforeNm.metric`

V1 (Sm→RP→DL→Cupd) is also traced as a no-Nm baseline. In V1, BeforeNm == AfterNm (identity).

---

## Tests

All tests in `TRM.Tests/V5_6/V5_6_NmCharacterizationAudit_Tests.cs`:

| Test | Name | LongRunning | Description |
|------|------|-------------|-------------|
| MGCB_01 | Protocol | No | Suite overview and settings |
| MGCB_02 | PerEpochNmDelta | No | Per-epoch Nm delta table (N=67) |
| MGCB_03 | LargestMetricChanges | No | Which metrics change most from Nm |
| MGCB_04 | BranchSelectiveEffects | No | Nm effects split by high/low-branch |
| MGCB_05 | SeparationReduction | No | Before/after Nm separation comparison |
| MGCB_06 | UpdateMagnitudeAnalysis | No | K-update magnitude and direction (B0 vs V1) |
| MGCB_07 | NmUniformityCheck | No | CV of Nm deltas across seeds |
| MGCB_08 | GateClassification | No | Decision gate evaluation |
| MGCB_09 | ClaimAudit | No | Claim discipline verification |
| MGCB_10 | RecommendedNextSuite | No | Next suite recommendation |
| MGCB_11 | Stage2_FullCharacterization | **Yes** | Full N=67,69,72 × 100 seeds |

---

## Gate Classification

### Gate A: Global Compression
**Condition:** Nm uniformly reduces variance and d_p90 across all seeds.
- Nm reduces d_std: check delta(d_std) < 0
- Nm reduces K_std: check delta(K_std) < 0
- Nm reduces d_p90: check delta(d_p90) < 0
- Uniformity: CV of deltas < 0.5
**Interpretation (if reached):** Nm is a global normalization/compression layer.

### Gate B: Branch-Selective Suppression
**Condition:** Nm affects future high-branch seeds much more strongly than low-branch seeds.
- Hi/Lo ratio of |Nm effect| (d+K) > 1.5
**Interpretation (if reached):** Nm is a branch suppressor — it preferentially modifies seeds that would otherwise reach high Omega.

### Gate C: Distance-Focused Suppression
**Condition:** Nm primarily changes d metrics (d_mean, d_std, d_p75, d_p90, d_max).
- |d-delta| / |K-delta| > 1.5
**Interpretation (if reached):** Nm suppresses distance amplification, not coupling.

### Gate D: Coupling-Focused Suppression
**Condition:** Nm primarily changes K metrics (K_mean, K_std, KLam1).
- |d-delta| / |K-delta| < 0.67
**Interpretation (if reached):** Nm suppresses coupling amplification, not distance.

### Gate E: PC1 Suppression
**Condition:** Nm reduces PC1 separation (KLam1 as proxy).
- delta(KLam1) < -0.01
**Interpretation (if reached):** Nm directly suppresses branch geometry.

### Gate F: Mixed Suppression
**Condition:** Two or more gates (A–D) are reached simultaneously.
**Interpretation (if reached):** Multiple Nm suppression mechanisms coexist.

---

## Findings (Stage 1: N=67, seeds 0–29)

### Per-Epoch Nm Delta

Nm operates **exclusively on d-metrics** (K, Omega, KLam1, StateNorm unchanged = 0.000000
because Nm normalizes R only, not K). Nm **increases** d-metrics, not decreases them:

| Epoch | dDMean | dDStd | dDP90 | dKMean | dKStd | dKLam1 | dOmega | dMeanDist | dStateNorm |
|-------|--------|-------|-------|--------|-------|--------|--------|-----------|------------|
| 0 | +0.1913 | +0.3342 | +0.4256 | 0 | 0 | 0 | 0 | −0.1027 | 0 |
| 1 | +0.3246 | +0.4086 | +0.6373 | 0 | 0 | 0 | 0 | −0.2334 | 0 |
| 2 | +0.3990 | +0.4565 | +0.8177 | 0 | 0 | 0 | 0 | −0.2672 | 0 |
| 3 | +0.3451 | +0.4328 | +0.7088 | 0 | 0 | 0 | 0 | −0.2386 | 0 |
| 4 | +0.3401 | +0.4383 | +0.7033 | 0 | 0 | 0 | 0 | −0.2278 | 0 |

The effect grows from epoch 0→2 then stabilizes. Nm increases d spread every epoch. MeanDist
(the raw R off-diagonal mean) decreases slightly (R values are stretched, pushing them apart).

### Largest Metric Changes

Ranking of absolute Nm delta magnitude (averaged across epochs and seeds):

| Rank | Metric | Abs Delta | Delta Mean | Delta Std |
|------|--------|-----------|------------|-----------|
| 1 | **dMax** | 13.503 | +13.503 | 0.157 |
| 2 | dP90 | 0.659 | +0.659 | 0.105 |
| 3 | dP75 | 0.466 | +0.466 | 0.075 |
| 4 | dStd | 0.414 | +0.414 | 0.037 |
| 5 | dMean | 0.320 | +0.320 | 0.043 |
| 6 | MeanDist | 0.214 | −0.214 | 0.024 |
| 7–11 | KMean, KStd, KLam1, Omega, StateNorm | 0.000 | 0.000 | 0.000 |

**d/K ratio: ∞** (Nm does not touch K at all).

### Branch-Selective Effects

High-branch seeds: 1, Low-branch seeds: 29 (N=67, seeds 0-29 baseline).

| Metric | Lo dMean | Lo dStd | Hi dMean | Cohen's d | Hi/Lo |
|--------|----------|---------|----------|-----------|-------|
| dMean | +0.321 | 0.043 | +0.284 | 1.21 | 0.885 |
| dStd | +0.415 | 0.037 | +0.382 | 1.29 | 0.919 |
| dMax | +13.516 | 0.143 | +13.124 | 3.88 | 0.971 |

Nm acts **uniformly** across seeds. Hi/Lo ratios are all near 1.0. The single high-branch seed
(Cohen's d > 1.0 is artifact of n=1). **Nm is not branch-selective** — it applies the same
d-amplification to all seeds.

### Separation Reduction

Before-Nm vs After-Nm Cohen's d separation between high and low branch groups:

| Metric | Before Sep | After Sep | Sep Change | Direction |
|--------|-----------|----------|------------|-----------|
| dMean | 4.189 | 1.804 | −2.386 | **REDUCED** |
| dStd | 4.189 | 1.605 | −2.583 | **REDUCED** |
| dP90 | 4.301 | 1.874 | −2.428 | **REDUCED** |
| MeanDist | 3.810 | 1.764 | −2.046 | **REDUCED** |
| KMean | 1.782 | 1.782 | 0.000 | NEUTRAL |
| KStd | 1.662 | 1.662 | 0.000 | NEUTRAL |
| Omega | 4.112 | 4.112 | 0.000 | NEUTRAL |

Nm **dramatically reduces branch separation in d-space** (Cohen's d drops by ~2.0–2.5) while
leaving K-space and Omega separation untouched (because Nm doesn't touch K at all in the
same epoch).

### Update Magnitude and Direction

**K-update magnitudes** (Frobenius norm of K per epoch, averaged across group):

| Group | Epoch 0 | Epoch 1 | Epoch 2 | Epoch 3 | Epoch 4 | Mean |
|-------|---------|---------|---------|---------|---------|------|
| B0 Hi | 63.05 | 70.13 | 65.86 | 73.78 | 60.30 | 66.62 |
| B0 Lo | 66.88 | 67.46 | 65.55 | 66.90 | 67.78 | 66.92 |
| V1 Hi | 71.63 | 76.76 | 66.35 | 76.99 | 65.22 | 71.39 |
| V1 Lo | 76.21 | 70.32 | 78.77 | 54.33 | 78.68 | 71.66 |

V1 (skip-Nm) has slightly larger K magnitudes (~71 vs ~67), consistent with Nm suppressing
coupling strength downstream.

**Cosine similarity** between B0 and V1 K matrices at each epoch:

| Epoch | Hi CosSim | Lo CosSim |
|-------|-----------|-----------|
| 0 | 0.9997 | 0.9948 |
| 1 | 0.9935 | 0.9882 |
| 2 | 0.9820 | 0.9771 |
| 3 | 0.9956 | 0.9842 |
| 4 | 0.9361 | 0.9807 |

Nm barely changes the **direction** of the K update (cosine > 0.93 at all epochs). The effect
is on **magnitude**, not direction.

### Nm Uniformity

CV of Nm deltas across seeds: **Mean CV = 0.0508** (well below 0.5 threshold). Nm acts
**highly uniformly** — the same d-amplification is applied to every seed.

---

## Gate Reached: **C — Distance-Focused**

| Gate | Condition | Result | Reached? |
|------|-----------|--------|----------|
| A | Nm reduces d_std AND K_std AND d_p90, uniform | d_std +0.41 (increases), K_std 0, d_p90 +0.66 | **NO** |
| B | Hi/Lo ratio > 1.5 | Hi/Lo = 0.885 (near-uniform) | **NO** |
| **C** | **\|d-delta\| / \|K-delta\| > 1.5** | **∞ (K unchanged)** | **YES** |
| D | \|d-delta\| / \|K-delta\| < 0.67 | ∞ (fails) | **NO** |
| E | KLam1 change < −0.01 | 0.000 (K unchanged) | **NO** |
| F | ≥2 gates reached | Only 1 gate (C) | **NO** |

### Mechanism (Gate C):

Nm **amplifies d-spread** (increases d_max by +13.5, d_p90 by +0.66, d_std by +0.41) through
min-max normalization of R → −log transformation. The amplified d then feeds into Cupd as
K = K0 · exp(−d/ξ). Larger d → smaller K → weaker coupling → reduced synchronization →
**branch suppression downstream**.

Nm is a **d-space amplifier that acts uniformly across all seeds**, and its branch-suppressive
effect is mediated entirely through the Cupd exponential: larger distances produce exponentially
smaller coupling weights.

The gate reached determines the recommended next suite:

| Gate | Next Suite | Focus |
|------|-----------|-------|
| A | MGCF | Compression Equivalence Audit |
| B | MGCD | Nm Intervention Audit |
| C | MGCE-d | d-Space Intervention Audit |
| D | MGCE-k | K-Space Intervention Audit |
| E | MGCD | PC1 Intervention |
| F | MGCD first, then iterate | Highest-confidence gate first |

---

## Claim Discipline

### SUPPORTED
- Per-epoch Nm delta values as reported
- Largest metric changes as computed
- Branch-selective effect magnitudes as measured
- Separation reduction as quantified
- Update magnitude and direction comparisons
- Gate classification based on measured thresholds

### CONDITIONAL
- Stage 1 results: N=67, seeds 0–29
- Stage 2 results: N=67, 69, 72, seeds 0–99 (LongRunning)
- PC1 coordinate uses KLam1 as proxy

### NOT CLAIMED
- Physical interpretation of any metric
- Causality beyond measured RecoverFP operator effects
- Universal behavior beyond tested N and seed ranges
- Time, space, length, c, relativity, quantum mechanics
- Attractor decomposition or criticality

---

## Recommended Next Suite

**MGCE-d: d-Space Intervention Audit** (Gate C — Distance-Focused)

Based on MGCB findings:
- Nm acts **exclusively on d-metrics** through R normalization → −log transformation.
- Nm **amplifies** d spread (d_max +13.5, d_p90 +0.66, d_std +0.41).
- Downstream, amplified d → smaller K via Cupd exponential → branch suppression.
- The effect is **uniform** (CV=0.05), not branch-selective.

**MGCE-d tests:** Direct d-manipulation to reproduce Nm's amplification effect:
- d compression (reduce d_max, d_p90) → should restore high-branch access
- d scaling (multiply d by factor < 1) → equivalent to weaker Nm
- d threshold clamping → test which tail drives branch suppression
- Goal: Find minimal d-manipulation operator functionally equivalent to Nm.

**Conservative alternative:** MGCD (Nm Intervention Audit) — always actionable.

---

## Files

| File | Description |
|------|-------------|
| `TRM.Tests/V5_6/V5_6_NmCharacterizationAudit_Tests.cs` | Full test suite (11 tests) |
| `docsV5_6/analysis/TRM_V5_6_NmCharacterizationAudit.md` | This document |

---

## Development Statistics

| Metric | Value |
|--------|-------|
| Tests added | 11 (1 LongRunning) |
| Total tests | 2396 |
| Passed | 2396 |
| Failed | 0 |
