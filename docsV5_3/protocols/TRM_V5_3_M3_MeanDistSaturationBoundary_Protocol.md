# TRM V5.3 — M3 MeanDist Saturation Boundary Protocol

**Suite:** V5_3_MeanDistSaturationBoundaryProtocol_Tests.cs
**Tag:** MSBP
**Date:** 2026-07-16
**Status:** PROTOCOL DEFINED — AWAITING EXECUTION
**Prerequisites:** N1 (COMPLETE), M1 (EXECUTED — Gate A), M2 (EXECUTED — Gate B)

---

## A. Purpose

Determine whether MeanDist xi-robustness and seed-variability are TRM-specific properties or generic consequences of computing mean pairwise distance on a fixed graph with large subsets.

---

## B. Three Baselines

| # | Name | Dynamics | Distance Source | Xi-Dependence |
|:--|:-----|:---------|:----------------|:--------------|
| 1 | Random Subset | None | Fixed graph (BFS) | None (by construction) |
| 2 | Generic Kuramoto | Single-epoch Sm | Emergent (RP→DL) | Through coupling K |
| 3 | TRM RecoverFP | Multi-epoch Rfp+Sm | Emergent (RP→DL) | Through iterative K update |

---

## C. Prior Constraints

| Result | Source |
|:-------|:-------|
| Baseline 1 CV_seed ~0.028 (seed-stable) | N1 |
| Baseline 1 CV_xi ~0.002 (xi-robust) | N1 |
| Baseline 2 MeanDist NOT YET COMPUTED | M2 only measured Ω |
| Baseline 3 MeanDist NOT YET COMPUTED | M1 only measured Ω |

---

## D. Normalization

| Method | Formula | Primary/Secondary |
|:-------|:--------|:------------------|
| D_90 | MD / 90th percentile distance | PRIMARY |
| D_median | MD / median distance | SECONDARY (robustness check) |

Each baseline normalizes by its OWN distance matrix distribution. Cross-baseline comparisons use CVs and fractional shifts only, not absolute values.

---

## E. Metrics and Thresholds

| Metric | Definition | Seed-Variable if | Xi-Robust if |
|:-------|:-----------|:----------------:|:------------:|
| CV_seed | std(MD_norm)/mean at fixed xi | > 0.15 | — |
| CV_xi | std(MD_xi_means)/mean across xi | — | < 0.05 |

V5.2 reference: CV_seed ~0.30 (variable), CV_xi ~0.05 (robust).

---

## F. Decision Gates

| Gate | Condition | H10 Impact | H11 Impact | Next |
|:-----|:----------|:-----------|:-----------|:-----|
| **A: Saturation** | Baseline 1 or 2 reproduces BOTH signatures | WEAKENED | WEAKENED | Revise H10/H11 |
| **B: TRM Residual** | Neither B1 nor B2 reproduces ≥1 signature; B3 reproduces both | CONDITIONALLY SUPPORTED | MORE TESTABLE | M4 |
| **C: Mixed** | Baselines reproduce one signature but not the other | PARTIALLY SUPPORTED | Requires decomposition | Characterize components |

---

## G. Run Plan

| Baseline | Runs | Type |
|:---------|:----:|:-----|
| B1 (random subset) | 5 computations | No simulation |
| B2 (generic dynamics) | 25 Sm calls | ~2s |
| B3 (TRM RecoverFP) | 25 runs × 6 Sm = 150 Sm calls | ~12s |
| **Total** | **175 Sm calls** | **~15s** |

---

## H. Forbidden Actions

1. Do NOT change normalization after execution.
2. Do NOT change thresholds (0.15 / 0.05).
3. Do NOT reselect seeds.
4. Do NOT change xi sweep.
5. Do NOT add baselines post-hoc.
6. Do NOT compare raw MD across baselines.
7. Do NOT claim H10/H11/H12 confirmed.
8. Do NOT claim geometry-control or attractor decomposition.

---

## I. Claim Discipline

| Statement | Classification |
|:----------|:--------------|
| Three baselines structurally defined | SUPPORTED |
| Normalization strategy defined | SUPPORTED |
| N1 showed Baseline 1 CV_seed ~0.028 | SUPPORTED |
| Results depend on normalization method | CONDITIONAL |
| TRM MeanDist exceeds baseline explanations | HYPOTHESIS |
| H10/H11/H12 confirmed | NOT CLAIMED |

---

*Protocol frozen 2026-07-16. All thresholds pre-registered. Execution deferred to V5_3_MeanDistSaturationBoundaryExecution_Tests.cs.*
