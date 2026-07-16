# TRM V5.4 — State-Space Geometry Protocol

**Suite:** V5_4_StateSpaceGeometryProtocol_Tests.cs
**Tag:** SSGP
**Date:** 2026-07-16
**Status:** PROTOCOL DEFINED — AWAITING EXECUTION
**Base:** V5.3 COMPLETE (123 V5.3 tests, cumulative 2312, 0 failed)

---

## A. Purpose

Determine the intrinsic geometry of the RecoverFP branch state space. V5.3 used scalar summary features (M14). V5.4 studies the full coupling (K) and distance (d) matrices to determine whether the M14 signature is the geometry itself or a compressed projection.

---

## B. State Representations

| ID | Name | Dimensions (N=67) | Content |
|:---|:-----|:-----------------:|:--------|
| S1 | Flattened K | 2211 | Upper triangle of coupling matrix |
| S2 | Flattened d | 2211 | Upper triangle of distance matrix |
| S3 | Combined [K\|d] | 4422 | Concatenation of S1 and S2 |

All from Epoch 5 (final RecoverFP epoch). Diagonal excluded.

---

## C. Dimensionality Metrics

| Metric | Formula | Low-Dim | High-Dim |
|:-------|:--------|:-------:|:--------:|
| Participation Ratio | (Σλ)²/Σλ² | ≤5 | >20 |
| Effective Rank (90%) | min k: Σ₁ᵏλ/Σλ ≥ 0.90 | ≤5 | >20 |
| Local Dimension (MLE) | k-NN distance distribution | Per-point | — |

---

## D. Embedding Methods

| Method | Type | Parameters |
|:-------|:-----|:-----------|
| PCA | Linear, global | All PCs |
| Diffusion Map | Nonlinear, manifold | k=10, Gaussian kernel |
| Spectral Embedding | Graph Laplacian | k-NN graph, k=10 |

---

## E. Bridge Metrics

| Metric | Definition | Interpretation |
|:-------|:-----------|:---------------|
| Cross-branch k-NN | % points with opposite-branch neighbor | 0%=disconnected, <20%=bridge, >50%=overlapping |
| Bridge point fraction | % points with ≥40% opposite neighbors (k=5) | Higher = wider bridge |
| Shortest cross-branch path | Dijkstra on k-NN graph | Bottleneck detection |
| Cross/Intra distance ratio | Mean cross / mean intra | >2 = well-separated |

---

## F. M14 Control

The M14 LDA signature is used only as comparison:
1. Correlate M14 score with PCA coordinates (R²)
2. M14 feature variance explained by PCs
3. LDA accuracy vs PCA dimension
4. Cross-representation generalization

---

## G. Decision Gates

| Gate | PR | Interpretation |
|:-----|:--:|:---------------|
| **A: Low-Dim** | ≤5 | Geometry governed by few latent coordinates; M14 spans manifold |
| **B: Moderate** | 6–20 | M14 captures most but not all structure |
| **C: High-Dim** | >20 | M14 is compressed projection; deeper structure exists |
| **D: Unstable** | Varies >50% across N | Geometry is N-dependent |

---

## H. Execution Plan

| Phase | Description | Est. Time |
|:------|:------------|:----------|
| 1 | State extraction (300 seeds × 6 Sm) | ~6s |
| 2 | PCA + dimensionality | ~2s |
| 3 | Embedding (PCA, diffusion, spectral) | ~5s |
| 4 | Bridge characterization (k-NN graphs) | ~3s |
| 5 | M14 control comparison | ~2s |

---

## I. Forbidden Actions

1. Do NOT change state representation, thresholds, or embedding params post-execution.
2. Do NOT reselect seeds or change N values.
3. Do NOT claim physical interpretation.
4. Do NOT claim M14 IS the geometry unless Gate A.
5. Do NOT modify V5.3 conclusions.

---

## J. Claim Discipline

| Statement | Classification |
|:----------|:--------------|
| State representations defined | SUPPORTED |
| Metrics, thresholds, gates frozen | SUPPORTED |
| Results depend on state representation + embedding method | CONDITIONAL |
| Branch geometry is low-dimensional (≤5) | HYPOTHESIS |
| Physical interpretation, attractor decomposition, H9–H12 | NOT CLAIMED |

---

*Protocol frozen 2026-07-16. Execution deferred to V5_4_StateSpaceGeometryExecution_Tests.cs.*
