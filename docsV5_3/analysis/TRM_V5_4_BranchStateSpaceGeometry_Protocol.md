# TRM V5.4 — Branch State-Space Geometry Protocol

**Status:** RESEARCH DESIGN
**Date:** 2026-07-16
**Base:** V5.3 COMPLETE (N1–M16, 123 tests, 0 failed)
**Branch:** To be created: `feature/v5.4-branch-state-space-geometry`

---

## A. Motivation

V5.3 established that RecoverFP produces two ω outcome families separated by a finite-N threshold, and that a multi-diagnostic coupling/distance signature classifies branches with high accuracy. However, V5.3 used **scalar summary features** (matrix means, standard deviations, leading eigenvalues). This raises a critical question:

> Is the M14 signature the actual branch geometry, or a compressed projection of a deeper RecoverFP state space?

V5.4 answers this by studying the **full coupling and distance matrix state space** rather than scalar summaries. If the branch structure has low intrinsic dimensionality, the M14 features may span it. If the state space is high-dimensional, the M14 signature is a projection and misses structure.

---

## B. What V5.3 Established (Building Blocks for V5.4)

| Finding | Source | Relevance to V5.4 |
|:--------|:-------|:------------------|
| Two ω branches with ~1.0 gap | M7 | Defines the outcome to explain |
| Finite-N threshold at N≈66 | M5, M6 | Tests whether geometry changes across threshold |
| Coupling/distance separates before ω | M9, M11 | The state space of interest is K and d |
| M14 signature: 86–100% accuracy | M14 | Baseline classifier for comparison |
| M15: signature disruption → branch flips | M15 | Signature is interventionally linked to outcomes |
| M16: weak bridge (silhouette 0.32–0.47) | M16 | Branches are connected, not fully disconnected |

---

## C. Research Questions

| # | Question | Method |
|:--|:---------|:-------|
| Q1 | What is the intrinsic dimensionality of the coupling/distance state space? | PCA, participation ratio, effective rank |
| Q2 | Does the high branch occupy a compact basin, filament, sheet, or sparse manifold? | Local dimension estimation, density analysis |
| Q3 | Is the bridge between basins continuous, fragmented, or a topological bottleneck? | k-NN graph analysis, shortest-path bridging |
| Q4 | How many effective dimensions are required to classify branch identity? | Dimensionality reduction + classifier accuracy vs dimension |
| Q5 | Does the M14 signature correspond to a low-dimensional manifold coordinate? | Correlation between M14 score and embedding coordinates |
| Q6 | Is the geometry stable across N=67, 69, 72? | Cross-N embedding consistency |

---

## D. Experimental Design

### D.1 State-Space Representation

For each seed at each N, extract **Epoch 5 coupling matrix K** and **distance matrix d**. The raw state space has dimension N×(N−1) = 67×66 = 4422 for N=67.

**Strategies for dimensionality reduction:**

| Strategy | Input Dimension | Method |
|:---------|:--------------:|:-------|
| S1: Full matrix | N×(N−1) | Flatten upper triangle → PCA |
| S2: Spectral | N | Top-k eigenvalues of K and d |
| S3: Graph-theoretic | O(N) | Degree distribution, path lengths, clustering |
| S4: M14 features | 7–10 | Scalar summary statistics (V5.3 baseline) |

**Primary:** S1 (full matrix PCA) for unbiased geometry.
**Secondary:** S2 + S3 for interpretability.
**Control:** S4 (M14 features) for comparison.

### D.2 V5.4 Suites

| Suite | Tag | Purpose | Runs |
|:------|:----|:--------|:----:|
| SSGP | State-Space Geometry Protocol | Define methods, thresholds, gates | 0 |
| SSGE | State-Space Geometry Execution | Compute full K,d matrices + embeddings | ~300 Sm |
| SSGA | State-Space Geometry Analysis | Dimensionality, clustering, bridge analysis | 0 |
| SSGB | Branch Synthesis | V5.4 completion report | 0 |

### D.3 Specific Analyses

#### A. Intrinsic Dimensionality

1. **PCA on flattened K + d upper triangle** (N=67 → 2211 features per matrix, 4422 total)
   - Explained variance vs component count
   - Participation ratio: PR = (Σ λ_i)² / Σ λ_i²
   - Effective rank: number of components to explain 90% variance

2. **Local dimension estimation** via maximum likelihood on k-NN distances
   - Compute local dimension per point
   - Compare low-branch vs high-branch local dimension

#### B. State-Space Embedding

1. **PCA projection** to 2D and 3D
   - Color by branch label
   - Color by Ω value
   - Visualize bridge structure

2. **UMAP embedding** (if available) or **diffusion map**
   - n_neighbors = 5, 10, 30
   - min_dist = 0.1, 0.5
   - Check stability across parameters

3. **Spectral embedding** using normalized graph Laplacian of k-NN graph
   - k = 5, 10
   - Compare first 3 embedding coordinates with M14 features

#### C. Basin Geometry

1. **Density estimation:**
   - Kernel density in PCA space
   - Density ratio: high-branch density / low-branch density
   - Identify density minima (bridge regions)

2. **Bridge characterization:**
   - Shortest path between branch centroids in k-NN graph
   - Number of bridge points (points with mixed k-NN neighbors)
   - Bridge width (distance between nearest pure-branch points on each side)

3. **Compactness:**
   - High-branch volume / low-branch volume (convex hull ratio)
   - Mean pairwise distance within each branch
   - Diameter of each branch

#### D. Minimal Coordinate Set

1. **Classifier accuracy vs PCA dimension:**
   - Train LDA on first k PCA components, k = 1, 2, 3, 5, 10, 20
   - Compare to M14 baseline accuracy

2. **Correlation between M14 score and PCA coordinates:**
   - Regress M14 score against first 5 PCA components
   - R² indicates how much of the M14 signature is captured by linear PCA

---

## E. Decision Gates

| Gate | Condition | Interpretation |
|:-----|:----------|:---------------|
| **A: Low-Dimensional Basin** | 90% variance in ≤5 dimensions AND branches separated in embedding | Branch structure governed by few latent coordinates; M14 signature spans relevant manifold |
| **B: High-Dimensional Manifold** | 90% variance requires >20 dimensions | Branch structure distributed across many variables; M14 signature is a projection |
| **C: Bridge-Dominated** | Bridge points >30% of total AND silhouette <0.25 | Branches are weakly separated; bridge is structural, not transitional |
| **D: No Stable Geometry** | Embedding varies substantially across N or parameters | Branch model is projection-dependent; geometry not robust |

---

## F. Interpretation Framework

| If V5.4 finds… | Then… |
|:---------------|:------|
| Low effective dimensionality (≤5) | M14 features likely span the branch manifold. The signature IS the geometry. |
| Moderate dimensionality (5–15) | M14 features capture most but not all structure. Some information is lost in scalar summaries. |
| High dimensionality (>20) | M14 signature is a projection. The branch structure requires the full matrix state space. |
| Stable across N=67,69,72 | Branch geometry is a robust RecoverFP property. |
| Changes across N | Branch geometry is N-dependent; the finite-N threshold changes state-space topology. |

---

## G. Risks and Mitigations

| Risk | Mitigation |
|:-----|:-----------|
| 4422-dimensional state space is computationally heavy | Use randomized PCA (power iteration on covariance) |
| UMAP/diffusion map may not be available | Fall back to PCA + spectral embedding |
| 60–100 seeds may under-sample high-dimensional space | Focus on local rather than global geometry |
| M14 features may already capture most variance | This is a SUCCESS outcome (Gate A) |

---

## H. Claim Discipline

| Statement | Classification |
|:----------|:--------------|
| Two ω branches exist | SUPPORTED (V5.3) |
| Branches are weakly bridged | SUPPORTED (M16) |
| M14 signature classifies branches | SUPPORTED (M14) |
| Branch state space has low dimensionality | HYPOTHESIS (V5.4) |
| M14 signature spans the branch manifold | HYPOTHESIS (V5.4) |
| Bridge is a topological bottleneck | HYPOTHESIS (V5.4) |
| Physical attractor decomposition | NOT CLAIMED |
| Physical phase transition | NOT CLAIMED |
| H11, H12 confirmed | NOT CLAIMED |

---

## I. Recommended V5.4 Execution

1. **SSGP** — Protocol definition (this document, frozen)
2. **SSGE** — Extract K,d matrices for 100 seeds × 3 N values; compute PCA, local dimension, k-NN graphs
3. **SSGA** — Dimensionality analysis, embedding, bridge characterization, M14 correlation
4. **SSGB** — V5.4 branch completion report

**Estimated runtime:** ~6 minutes (300 Sm calls for state extraction + PCA/embedding computation).

---

*Protocol drafted 2026-07-16. V5.4 shifts focus from branch mechanism (V5.3) to branch state-space geometry. The goal is to determine whether the M14 signature is the geometry or a projection of it.*
