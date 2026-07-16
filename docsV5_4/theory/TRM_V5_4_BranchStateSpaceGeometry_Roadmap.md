# TRM V5.4 — Branch State-Space Geometry Roadmap

**Status:** INITIALIZED | **Date:** 2026-07-16

---

## A. Motivation

V5.3 established that RecoverFP produces two Ω outcome families connected by a narrow bridge, and that a multi-diagnostic LDA signature classifies branches with 86–100% accuracy. However, V5.3 used scalar summary features (matrix means, standard deviations, leading eigenvalues). V5.4 determines whether these features span the intrinsic branch state space or are a compressed projection.

## B. Research Questions

1. What is the intrinsic dimensionality of the branch state space?
2. Does the high branch occupy a compact basin, filament, sheet, or sparse manifold?
3. Is the bridge between basins continuous, fragmented, or a topological bottleneck?
4. How many effective dimensions are required to classify branch identity?
5. Does the M14 signature correspond to a low-dimensional manifold coordinate?
6. Is the geometry stable across N=67, 69, 72?

## C. Methods

- PCA on full flattened K + d matrices (4422 dimensions for N=67)
- Participation ratio and effective rank for dimensionality
- Local dimension estimation via k-NN
- UMAP / diffusion map / spectral embedding
- k-NN graph connectivity and bridge analysis
- Cross-N embedding consistency

## D. Planned Suites

1. SSGP — State-Space Geometry Protocol
2. SSGE — State-Space Geometry Execution
3. SSGA — State-Space Geometry Analysis
4. SSGB — Branch Synthesis

**Recommended first suite:** `V5_4_StateSpaceGeometryProtocol_Tests.cs`
