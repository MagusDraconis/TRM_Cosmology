# V4.3 — Geometric Scale Interpretation Roadmap

**Status:** EXPLORATORY
**Date:** 2026-07-14

---

## A. Motivation

V4.2 discovered that MeanDist variance (~0.30 CV) persists to N=1000 and is not a finite-N artifact. This variance dominates G_eff_SI uncertainty (cubic dependence, effective CV ~0.90). Understanding what this variance represents is the central V4.3 question.

---

## B. V4.2 Findings

| Finding | Detail |
|:---|:---|
| c_eff_SI simplified | Kr86/Cs133 × Omega (MeanDist cancels) |
| c_eff_SI uncertainty | Omega-dominated (CV ~0.01) |
| G_eff_SI dominated | MeanDist³ (CV ~0.90) |
| MD variance persists | N=40–1000, all proxies ~0.30 CV |
| Proxy refinement | No multiplicative proxy improves on MeanDist |

---

## C. The MeanDist Problem

MeanDist = average of d_ij = -log(R_ij) over all node pairs. This is a global average distance. Its ~30% seed variance could mean:

1. **The attractor geometry genuinely varies across seeds** — different random initial topologies converge to slightly different geometric configurations.
2. **MeanDist is a poor proxy** for the true geometric invariant — a different measure may be more stable.
3. **The attractor has multiple scales** — MeanDist captures one, but others may be more fundamental.

---

## D. Candidate Geometric Scales

| Scale | Definition | Hypothesis |
|:---|:---|:---|
| Curvature radius | 1/sqrt(curvature proxy) | May be more fundamental than MeanDist |
| Causal horizon scale | Distance at which R_ij crosses threshold | Related to c_eff structure |
| Local shell scale | Mean distance in first neighbor shell | Local geometry proxy |
| Geodesic scale | Mean geodesic (Floyd-Warshall) distance | Accounts for shortest-path structure |
| Spectral scale | 1/sqrt(λ₂) of graph Laplacian | Algebraic geometry invariant |
| Dimensional scale | Scale extracted from dimension diagnostic | D-related geometric invariant |

---

## E. Non-Circular Selection Rules

1. No scale selected using physical c or G agreement
2. No scale selected using astrophysical data
3. No scale selected post-comparison
4. Selection criteria: seed CV, N stability, law robustness, geometric interpretability

---

## F. Planned Test Suites

| Suite | Purpose |
|:---|:---|
| V4_3_GeometricScaleCandidateSurvey | Survey all candidate geometric scales |
| V4_3_CurvatureRadiusInvestigation | Deep-dive on curvature radius as invariant |
| V4_3_CausalHorizonScaleInvestigation | Causal-front-derived length scale |
| V4_3_LocalShellScaleInvestigation | Local vs global geometric scales |
| V4_3_GeodesicScaleInvestigation | Geodesic-based geometric invariants |
| V4_3_SpectralScaleInvestigation | Spectral/Laplacian geometric scales |
| V4_3_GeometricScaleRefinement | Comparative ranking and selection |

---

## G. Claim Discipline

**SUPPORTED:** Geometric scale candidates are computable and comparable.
**CONDITIONAL:** All results depend on finite N, proxy definitions, primary regime.
**HYPOTHESIS:** A refined geometric scale may capture the attractor's true length invariant.
**NOT CLAIMED:** Physical c, G, gravity, SI units, spacetime, GR, Einstein equations.
