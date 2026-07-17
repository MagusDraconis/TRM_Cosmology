# TRM V5.7 Reduced Operator Failure Decomposition (ROCA)

**Date:** 2026-07-17  
**Branch:** `feature/v5.7-recoverfp-reduced-operator-validation`  
**Status:** COMPLETE  
**Gate:** A — Robust suppressor, local amplifier

---

## 1. V9 Stage Decomposition: The d-Compression FLIP

### The Key Finding

V9 amplification fails at N≥72 because **the second Sm→RP→DL pass FLIPS from d-compression to d-expansion.**

| N | d_Cupd1 | d_B4C2 | Ratio | d Behavior | K Change |
|---|---------|--------|-------|-------------|----------|
| 66 | 0.330 | 0.060 | **0.18** | Compression | +15% |
| 67 | 0.363 | 0.080 | **0.22** | Compression | +16% |
| **72** | **0.214** | **0.560** | **2.62** | **EXPANSION** | **−17%** |
| **80** | **0.096** | **0.985** | **10.2** | **EXPANSION** | **−38%** |

At N=66,67: Cupd1 produces K that leads to tight synchronization in the second Sm pass → d collapses (ratio 0.18–0.22) → K amplifies through exp(−d/ξ).

At N=72,80: Cupd1 produces K that leads to LOOSE synchronization → d expands (ratio 2.6–10.2) → K SUPPRESSES through exp(−d/ξ).

**d→K correlation remains deterministic everywhere** (r < −0.995). The Cupd exponential is universal. The FLIP is in the d-compression step, not in the Cupd mapping.

---

## 2. N=66 Borderline Failure

| | B0 | R1 |
|---|-----|-----|
| High seeds | **29** (1 seed) | **5, 23** (2 seeds) |
| Overlap | — | **0** |

R1 fails at N=66 because:
- R1 successfully **suppresses** B0's high seed (29: 2.42→1.07)
- But R1 **induces** 2 NEW high seeds (5, 23) that B0 does not have
- The failure is a **seed-substitution** effect, not a structural collapse

R1's mean Omega at N=66 (1.165) is barely above B0 (1.152). The failure is marginal and seed-specific — consistent with the transition zone where the branch split first appears.

---

## 3. Seed-Block Diagnosis

| N | Block | B0 Hi | R1 Hi | R1 dMean | R1 KMean |
|---|-------|-------|-------|----------|----------|
| 72 | 0 | 15/30 | 14/30 | 0.446 | 0.963 |
| 72 | 1 | 8/30 | 11/30 | 0.481 | 0.948 |
| 72 | 2 | 18/40 | 14/40 | 0.462 | 0.954 |
| 80 | 0 | 25/30 | 22/30 | 0.637 | 0.895 |
| 80 | 1 | 25/30 | 22/30 | 0.629 | 0.897 |
| 80 | 2 | 36/40 | 38/40 | 0.603 | 0.908 |

F2 is triggered by **B0 variability across blocks**, not by R1-specific failure. At N=72 Block 1, B0 has only 8/30 high — much lower than Block 0 (15/30) and Block 2 (18/40). R1 produces 11/30 which is > B0's 8/30, but this is because Block 1 has naturally lower B0 high-branch. The R1 dMean/KMean are nearly identical across blocks — the operator is stable. The branch threshold Ω>1.783 interacts with natural B0 variability.

---

## 4. K→Omega Mediation Windows

| N | Branch Split? | K→Ω r | Omega Range |
|---|---------------|-------|-------------|
| 60 | NO | 0.31 | 1.07–1.16 |
| 62 | NO | −0.12 | 1.06–1.13 |
| **64** | **YES** | **0.92** | 1.06–1.80 |
| 66+ | YES | 0.86–0.97 | wide |

K→Omega mediation is **undefined where no branch split exists.** At N=60,62, Omega is tightly clustered (range ~0.1) and K_mean has zero predictive power — there IS no high/low branch to predict. The mediation exists and is strong (r>0.85) wherever the branch split exists (N≥64).

---

## 5. Model Classification: **A — Robust Suppressor, Local Amplifier**

| Model | Fit |
|-------|-----|
| **A** — Robust suppressor, local amplifier | **✓ BEST FIT** |
| D — Suppression robust, amplification overfit | ✓ Also consistent |
| B — N-window mechanism | Partial (only V9 is windowed) |
| C — Seed-block-sensitive | Partial (F2 is B0-driven, not R1-driven) |
| E — Mechanism insufficient | ✗ NOT SUPPORTED |

**R1 suppression is robust** (8/9 N, d→K universal). **V9 amplification is N≈66–67 local** because the d-compression step FLIPS at N≥72. The second Sm pass with Cupd1 K produces d-expansion instead of d-compression, turning V9 into a suppressor rather than amplifier.

---

## 6. Gate: **A — Robust Suppressor, Local Amplifier**

The V5.6 reduced operator model survives cross-N validation for the **suppression pathway** (R1). The **amplification pathway** (V9) is a special case at N≈66–67 where Cupd1 K produces tight second-pass synchronization. At N≥72, the d-compression FLIPS and V9 becomes a suppressor — the "symmetric inverse pathways" of V5.6 are valid but only within the N=66–67 narrow window.

---

## 7. Claim Discipline

### SUPPORTED
- R1 state-conditioned operator works at 8/9 N (60–80)
- d→K Cupd exponential is universal (r < −0.99 everywhere)
- V9 d-compression FLIPS at N≥72 — Cupd1 K state changes synchronization regime
- K→Omega mediation runs where branch split exists (N≥64)
- Model A: Robust suppressor, local amplifier

### FALSIFIED
- V9 amplification is NOT universal — N≈66–67 only
- V5.6 "symmetric inverse pathways" are a local N-window phenomenon
- Seed-block invariance is weak at N=72,80 due to B0 variability

### NOT CLAIMED
- Physical interpretation
- Universality beyond N=60–80
- Complete mechanism explanation

---

## Files

| File | Description |
|------|-------------|
| `TRM.Tests/V5_7/V5_7_ReducedOperatorFailureDecomposition_Tests.cs` | 6 tests (ROCA_01–ROCA_05 + protocol) |
| `docsV5_7/analysis/TRM_V5_7_ReducedOperatorFailureDecomposition.md` | This document |

## Development Stats

| Metric | Value |
|--------|-------|
| ROCA tests | 6 |
| V5.7 total | 20 |
| Cumulative | 2455 |
| Failed | 0 |
| Gate reached | A |
