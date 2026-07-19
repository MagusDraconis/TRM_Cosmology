# TRM V4.3 — Geometric Scale Candidate Survey

**Status:** SURVEY COMPLETE
**Suite:** `V4_3_GeometricScaleCandidateSurvey_Tests.cs`
**Tag:** `V4_3_GSCS`
**Branch:** `feature/v4.3-geometric-scale-interpretation`
**Base:** `v4.2-physical-calibration-complete` (1744 tests)
**Date:** 2026-07-15

---

## 1. Motivation

V4.2 established that:

- `c_eff_SI = Kr86/Cs133 × Omega` (MeanDist cancels exactly, Omega CV ≈ 0.01)
- `G_eff_SI = alpha_TRM × L³ / (T² × M)` — dominated by the length channel
- MeanDist CV ≈ 0.30 persists to N=1000 (CBN500)

The persistent ~30% seed variance in MeanDist is the dominant uncertainty source for G_eff_SI (effective CV ~0.90 due to cubic dependence). Understanding whether this variance represents genuine attractor geometry or a suboptimal proxy choice is the central V4.3 question.

This suite surveys all plausible geometric scale candidates derivable from TRM attractor geometry, without modifying any frozen V4.2 predictions or performing physical comparison.

---

## 2. Relation to V4.2

| V4.2 Suite | Finding | V4.3 Response |
|:---|:---|:---|
| CBN500 | MeanDist CV ~0.30 persists to N=1000 | Is this the true geometric invariant? |
| MDAR | Alternative proxies evaluated but none adopted | Expand to 12 candidate scales with deeper definitions |
| ATR | alpha_TRM refinement is secondary to length uncertainty | Focus on length-scale candidates |
| SIPC | c_eff_SI structurally precise (Omega-dominated) | Survey length scales only; c_eff not targeted |

**Freeze discipline:** All V4.2 frozen predictions, calibration anchors, and audit hashes remain immutable. No recalibration occurs in V4.3.

---

## 3. MeanDist Baseline

MeanDist is the V4.2 baseline length anchor:

```
MeanDist = (2 / N(N-1)) × Σ_{i<j} d_ij
```

where `d_ij = -log(R_ij)` is the distance in the normalized correlation-attractor metric.

| Property | Value |
|:---|:---|
| Definition | Global mean of pairwise distances |
| CV (N=80, 15 seeds) | ~0.30 |
| N-scaling | CV persists across N=40–1000 |
| Cancellation in c_eff | Exact (multiplicative proxy) |
| Dominance in G_eff | Cubic (CV ~0.90 effective) |

---

## 4. Candidate Catalog

Twelve geometric scale candidates (A–L) are defined, all computable from the TRM distance matrix with no external inputs:

| ID | Candidate | Definition | Category |
|:--:|:---|:---|:---|
| A | **MeanDist** | `(2/N(N-1)) Σ d_ij` | Statistical — global mean |
| B | **MedianDist** | Median of all `d_ij` | Statistical — robust central tendency |
| C | **TrimmedMeanDist** | Mean after trimming 5% extremes | Statistical — outlier-resistant |
| D | **GeodesicMeanDist** | Floyd-Warshall shortest-path mean | Topological — geodesic |
| E | **LocalShellScale** | Mean distance within coupled (K_ij > 0.01) pairs | Structural — local neighborhood |
| F | **CurvatureRadiusProxy** | `1 / sqrt(mean(1/d_ij²))` | Geometric — curvature |
| G | **CausalHorizonScale** | Median of all `d_ij` (threshold proxy) | Causal — horizon |
| H | **SpectralScale** | `1 / sqrt(Σ d_ij·exp(-d_ij/ξ) / N_pairs)` | Algebraic — spectral |
| I | **PercentileDistanceScale** | P90 upper-tail distance | Statistical — tail |
| J | **MetricProxyScale** | `sqrt(Σ|g_ij| / N_pairs)` where `g_ij = (d₀ᵢ² + d₀ⱼ² - d_ij²)/2` | Metric — Gram-volume |
| K | **CurvatureShellScale** | Mean distance in band [0.5×MeanDist, 1.5×MeanDist] | Geometric — shell |
| L | **ObserverFrameScale** | Mean distance from median-omega observer to all others | Causal — observer |

### 4.1 Computational Notes

- **GeodesicMeanDist (D):** O(N³) Floyd-Warshall. Feasible at N ≤ 200; expensive at N=500; infeasible at N=1000 for routine pipelines.
- **SpectralScale (H):** Experimental definition using weighted exponential proxy. A full Laplacian eigenvalue decomposition would be O(N³).
- **MetricProxyScale (J):** Experimental Gram-like construction. Stability properties not yet fully characterized.
- **CurvatureRadiusProxy (F):** Sensitive to small `d_ij` values (1/d_ij² weights short distances heavily).

---

## 5. Stability Analysis

### 5.1 Seed Stability (CV across 15 seeds, N=80)

Each candidate is evaluated over 15 independent seeds at N=80. Lower CV indicates better seed stability.

Results are ranked in the test output (`V4_3_GSCS_04`). MeanDist baseline CV ≈ 0.30 is the reference.

### 5.2 N-Scaling Stability

CV of candidate values across N ∈ {40, 80} measured. Low N-CV indicates the scale definition is insensitive to system size.

### 5.3 Coupling-Law Robustness

CV drift between exponential and gaussian coupling laws. Low drift (ΔCV < 0.10) indicates the candidate is robust to the choice of coupling functional form.

---

## 6. Compatibility Analysis

### 6.1 Weak-Field Compatibility

Pearson correlation `r` between candidate scale and MeanDist across seeds. High |r| indicates the candidate tracks the same dimensional pathway (L³ in G_eff) as MeanDist.

### 6.2 Geodesic Compatibility

Candidate value computed on direct distance matrix vs. Floyd-Warshall geodesic matrix. Low relative delta indicates the candidate respects shortest-path structure.

### 6.3 Causal-Front Compatibility

CausalHorizonScale (G) is the primary causal-front candidate. Compatibility assessed via seed stability and law robustness of the threshold-based scale.

### 6.4 Observer-Frame Compatibility

Candidate value ratio (half-N sub-frame / full-N) CV across seeds. Low frame CV (< 0.15) indicates the candidate does not depend sensitively on observer frame choice.

---

## 7. Null-Control Separation

Null control: homogeneous random distance matrix (no attractor structure). High relative delta between structured and null values indicates the candidate is sensitive to genuine geometric structure rather than arbitrary distance distributions.

All candidates show Δ > 0.05, confirming sensitivity to attractor geometry.

---

## 8. Ranking Framework

Composite score (lower = better):

```
composite = CV_seed
          + min(CV_N, 0.5)
          + min(ΔCV_law, 0.3)
          + (1 - min(null_sep, 1.0)) × 0.3
          + (1 - min(|wf_corr|, 1.0)) × 0.2
          + min(CV_frame, 0.5)
```

Rankings are non-circular: no physical constant agreement is used.

---

## 9. Candidate Classification

| ID | Candidate | Classification | Rationale |
|:--:|:---|:---|:---|
| A | MeanDist | **A — BASELINE** | Proven baseline; persistent CV but structurally valid |
| B | MedianDist | **B** | Robust central tendency; outlier-resistant |
| C | TrimmedMeanDist | **B** | Outlier-resistant; comparable to MeanDist |
| D | GeodesicMeanDist | **C** | O(N³) cost prohibitive; geodesic captures topology |
| E | LocalShellScale | **B** | Local structure well-defined; coupling-dependent |
| F | CurvatureRadiusProxy | **C** | Experimental definition; sensitive to small distances |
| G | CausalHorizonScale | **B** | Causal interpretation well-motivated |
| H | SpectralScale | **C** | Spectral definition experimental; needs refinement |
| I | PercentileDistanceScale | **B** | Simple; P90 probes upper-tail structure |
| J | MetricProxyScale | **C** | Gram-det volume proxy experimental |
| K | CurvatureShellScale | **B** | Shell-banded; curvature-zone motivated |
| L | ObserverFrameScale | **B** | Observer-frame motivated; omega-anchored |

### Classification Criteria

- **A CANDIDATE:** Low CV, stable across N/laws/frames, strong null separation. Adoptable baseline.
- **B CANDIDATE:** One or more metrics borderline but not rejected. Merits further investigation.
- **C CANDIDATE:** Significant concerns (experimental definition, computational cost, or stability) but conceptually interesting. Research only.
- **REJECT:** Fails core geometric or computational criteria. No candidate rejected in this survey.

---

## 10. Recommended Follow-up Studies

| Priority | Suite | Focus |
|:---|:---|:---|
| 1 | `V4_3_GeometricScaleClassification_Tests.cs` | Deep-dive on A/B candidates with expanded statistics |
| 2 | `V4_3_CurvatureRadiusInvestigation_Tests.cs` | Refine curvature proxy definition and stability |
| 3 | `V4_3_CausalHorizonScaleInvestigation_Tests.cs` | Causal-front-derived length scale with rigorous threshold |
| 4 | `V4_3_SpectralScaleInvestigation_Tests.cs` | Full Laplacian eigenvalue approach |
| 5 | `V4_3_GeodesicScaleInvestigation_Tests.cs` | Geodesic invariants and computational tradeoffs |
| 6 | `V4_3_GeometricScaleRefinement_Tests.cs` | Comparative ranking with V4.2 G_eff propagation |

---

## 11. Claim Discipline

### SUPPORTED

- 12 geometric scale candidates (A–L) are defined and computable from TRM distance matrices.
- Seed stability CV, N-scaling, law robustness, null separation measured for all candidates.
- Weak-field, geodesic, causal-front, observer-frame compatibility assessed.
- Candidate ranking uses geometric criteria only.
- MeanDist remains the V4.2 baseline; no frozen predictions modified.
- c_eff_SI and G_eff_SI not recalibrated.

### CONDITIONAL

- All results depend on finite N (40–80 for detailed metrics), proxy definitions, and primary regime (ξ=1.75, K₀=1.2, exponential baseline).
- Law robustness tested on exponential vs. gaussian only. Power-law and other forms not fully explored.
- Spectral, curvature, and metric proxy definitions are experimental and liable to refinement.
- Computational constraints (Floyd-Warshall O(N³), full eigendecomposition) limit large-N evaluation for some candidates.

### HYPOTHESIS

- A refined geometric scale candidate may capture the TRM attractor's true length invariant more stably than MeanDist.
- Lower-CV length-scale candidates may reduce G_eff_SI uncertainty (L³ channel).
- Observer-frame and causal-horizon scales may reveal deeper causal structure in the attractor geometry.

### NOT CLAIMED

- Physical c (299,792,458 m/s) derived, compared, or used for ranking
- Physical G (6.67430×10⁻¹¹ m³/(kg·s²)) derived, compared, or used for ranking
- Spacetime, Lorentz invariance, special relativity, general relativity derived
- Einstein field equations derived
- Astrophysical data (SPARC, lensing, CMB, etc.) used or compared
- Any candidate adopted as replacement for MeanDist baseline
- SI-unit calibration performed or modified
- Physical interpretation of geometric scales

---

## Appendix A: Test Suite Structure

| Test | Name | Purpose |
|:---|:---|:---|
| 01 | FrozenPredictionManifestVerified | Confirm V4.2 freeze intact |
| 02 | MeanDistBaselineLoaded | Load and validate baseline |
| 03 | CandidateCatalogGenerated | Generate A–L catalog |
| 04 | StabilityMetricsComputed | Seed CV for all candidates |
| 05 | LawRobustnessComputed | Exponential vs. gaussian drift |
| 06 | NullControlsComputed | Structured vs. null separation |
| 07 | WeakFieldCompatibilityComputed | Correlation with MeanDist |
| 08 | GeodesicCompatibilityComputed | Direct vs. geodesic comparison |
| 09 | ObserverFrameCompatibilityComputed | Sub-frame stability |
| 10 | CandidateRankingComputed | Multi-metric composite ranking |
| 11 | CandidateClassificationGenerated | A/B/C/REJECT classification |
| 12 | NoPhysicalComparisonUsed | Verification gate |
| 13 | SurveyClassification | Self-assessment score |
| 14 | ClaimDisciplineReport | Full claim discipline |

## Appendix B: Frozen Parameters

| Parameter | Value | Status |
|:---|:---|:---|
| Base seed (BS) | 42 | FROZEN |
| Dt | 0.05 | FROZEN |
| St | 300 | FROZEN |
| Hd | 4 | FROZEN |
| ξ (xi) | 1.75 | FROZEN |
| K₀ | 1.2 | FROZEN |
| Coupling law | Exponential (baseline) | FROZEN |
| R_eps | 1e-6 | FROZEN |
