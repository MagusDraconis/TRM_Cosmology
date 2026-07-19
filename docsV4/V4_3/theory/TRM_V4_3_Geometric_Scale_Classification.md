# TRM V4.3 — Geometric Scale Classification

**Status:** CLASSIFICATION COMPLETE
**Suite:** `V4_3_GeometricScaleClassification_Tests.cs`
**Tag:** `V4_3_GSC`
**Branch:** `feature/v4.3-geometric-scale-interpretation`
**Base:** `v4.2-physical-calibration-complete` (1744 tests)
**Date:** 2026-07-15

---

## 1. Motivation

GSCS surveyed 12 geometric scale candidates and classified 8 as A or B candidates. However, the GSCS analysis treated each candidate as an independent scale estimator. This suite addresses the next question:

> **How many genuinely different geometric scale classes exist inside TRM geometry?**

Some candidates may be merely different estimators of the same underlying geometric quantity (e.g., MeanDist and MedianDist both estimate "global distance"). Others may probe fundamentally different geometric structures (local vs. global, causal vs. statistical, observer-frame vs. ensemble).

Identifying distinct geometric classes is essential before any candidate can be recommended as a refined length anchor, because:
- Redundant candidates within the same class add no new information
- Distinct classes may represent independent geometric degrees of freedom
- Class structure informs which scales are worth deeper investigation

---

## 2. Candidate Set

Eight non-C candidates from GSCS are analyzed:

| ID | Candidate | GSCS Class | Geometric Category |
|:--:|:---|:--:|:---|
| A | MeanDist | A | Global distance (baseline) |
| B | MedianDist | B | Global distance (robust) |
| C | TrimmedMeanDist | B | Global distance (outlier-resistant) |
| E | LocalShellScale | B | Local / neighbor-shell |
| G | CausalHorizonScale | B | Causal / horizon proxy |
| I | PercentileDistanceScale | B | Statistical / tail |
| K | CurvatureShellScale | B | Shell-banded / curvature zone |
| L | ObserverFrameScale | B | Observer-frame / omega-anchored |

C-candidates (D, F, H, J) from GSCS are excluded due to experimental definitions and computational constraints.

---

## 3. Correlation Analysis

### 3.1 Pearson Correlation Matrix

Pairwise Pearson correlation coefficients computed across 12 seeds at N=80. High correlations (|r| > 0.8) indicate candidates track the same underlying variation; low correlations indicate distinct geometric information.

### 3.2 Spearman Rank Correlation Matrix

Rank correlation captures monotonic relationships less sensitive to outlier seeds. Concordance between Pearson and Spearman strengthens confidence in class structure.

### 3.3 Distance Matrix

Inter-candidate distance defined as `d_ij = 1 - |Pearson r_ij|`. This metric space forms the basis for hierarchical clustering.

---

## 4. Class Discovery

### 4.1 Method

Agglomerative hierarchical clustering with complete linkage on the correlation distance matrix. The clustering threshold is adaptive: the median inter-candidate distance (Q50) serves as the natural break point.

### 4.2 Bootstrap Stability

Clustering repeated on 5 bootstrap resamples (re-seeded). Co-clustering frequency matrix measures stability: pairs co-clustering in ≥70% of bootstrap iterations are considered stable.

### 4.3 N-Scaling Stability

Class structure compared at N=40 and N=80. Persistent cluster count across N indicates scale-invariant geometric classes.

### 4.4 Law Robustness

Class structure compared under exponential and gaussian coupling laws. Persistent classes across laws indicate the geometry is coupling-law independent.

---

## 5. Null-Control Verification

Class structure is verified to be **absent** in null (unstructured) data. The number of clustered classes in structured TRM distance matrices differs from the null, confirming that discovered classes represent genuine geometric structure.

---

## 6. Class Purity

For each discovered cluster:

```
Intra-r = mean |Pearson r| between candidates within the same class
Inter-r = mean |Pearson r| between candidates in this class and other classes
Purity   = Intra-r - Inter-r
```

High purity (>0.3) indicates a well-separated geometric class. Moderate purity (0.1–0.3) indicates partial separation. Low purity (<0.1) indicates weak class boundaries.

---

## 7. Expected Geometric Classes

Based on geometric definitions, the following classes are hypothesized:

| Class | Label | Expected Members | Interpretation |
|:---|:---|:---|:---|
| Global Distance Scale | GDS | A (MeanDist), B (MedianDist), C (TrimmedMean) | Different estimators of the same global length scale |
| Local/Shell Scale | LSS | E (LocalShell), K (CurvatureShell) | Local neighborhood and shell-banded geometry |
| Causal/Observer Scale | COS | G (CausalHorizon), L (ObserverFrame) | Observer-frame and causal-front derived scales |
| Tail/Percentile Scale | TPS | I (PercentileDistance) | Upper-tail distribution probes |

Actual clustering results from the test output (`V4_3_GSC_05`, `V4_3_GSC_06`, `V4_3_GSC_12`) may confirm, refine, or contradict these hypotheses.

---

## 8. Stability Analysis

| Metric | Test | Pass Criterion |
|:---|:---|:---|
| Seed stability (bootstrap) | GSC_07 | Stable pairs co-cluster ≥ 70% |
| N-scaling stability | GSC_08 | Cluster count consistent at N=40, 80 |
| Law robustness | GSC_09 | Cluster count consistent exp vs. gaussian |
| Null-control separation | GSC_10 | Structured ≠ null class count |
| Class purity | GSC_11 | At least one class with positive intra-inter difference |

---

## 9. Class Ranking

| Class Label | Strength | Criterion | Recommendation |
|:---|:---|:---|:---|
| **A — STRONG** | Purity > 0.3 | Well-separated from other classes | Priority deep-dive candidate |
| **B — MODERATE** | Purity 0.1–0.3 | Partial separation | Confirm with larger seed samples |
| **C — WEAK** | Purity < 0.1 | Boundaries indistinct | May represent continuum, not classes |

---

## 10. Recommended Interpretation Paths

| Priority | Path | Focus |
|:---|:---|:---|
| 1 | Global vs. Local Scale | Verify whether GDS and LSS are genuinely distinct |
| 2 | Causal vs. Statistical | Investigate whether causal/observer scales form a distinct class |
| 3 | Global estimator convergence | Test whether A, B, C converge to same value as N→∞ |
| 4 | Refined class definitions | Re-run classification with C-candidates after definition refinement |

---

## 11. Recommended Next Suite

`V4_3_GlobalVsLocalScale_Tests.cs` — investigate whether the global distance scale and local/shell scale are genuinely distinct geometric degrees of freedom, or whether one subsumes the other as N→∞.

---

## 12. Claim Discipline

### SUPPORTED

- Pairwise Pearson and Spearman correlation matrices computed for 8 candidates.
- Hierarchical agglomerative clustering (complete linkage) performed on correlation distance.
- Bootstrap seed stability of class membership assessed (5 iterations).
- N-scaling stability tested at N ∈ {40, 80}.
- Law robustness tested under exponential and gaussian coupling.
- Null-control class separation verified.
- Class purity scores (intra vs. inter correlation) computed.
- All classification uses geometric criteria only.
- No physical constants, SI comparisons, or astrophysical data used.

### CONDITIONAL

- Class structure depends on finite N (40–80), nS (6–12 seeds), primary regime (ξ=1.75, K₀=1.2).
- Clustering threshold uses median inter-candidate distance — adaptive and data-driven.
- C-candidates from GSCS excluded; their inclusion might reveal additional classes.
- Bootstrap stability uses 5 iterations — statistical power is limited.
- Cluster labels are heuristic interpretations, not ground-truth categories.

### HYPOTHESIS

- Global-scale candidates (MeanDist, MedianDist, TrimmedMeanDist) form one tightly-correlated class.
- Local/structural candidates (LocalShell, CurvatureShell) may form a second class.
- Observer-frame and causal-horizon scales may occupy distinct geometric classes.
- The number of discovered geometric scale classes reflects true geometric degrees of freedom in the TRM attractor.

### NOT CLAIMED

- Physical c or G derived, compared, or used for classification.
- SI calibration performed or modified.
- Spacetime, Lorentz, SR, GR, Einstein equations derived.
- Any specific number of geometric scale classes as ground truth.
- Astrophysical data (SPARC, lensing, CMB) used.
- V4.2 blind comparison results reinterpreted.

---

## Appendix: Test Suite Structure

| # | Test | Purpose |
|:--:|:---|:---|
| 01 | CandidateSetLoaded | Load and validate 8-candidate set |
| 02 | PairwisePearsonCorrelationMatrix | Linear correlation structure |
| 03 | PairwiseSpearmanCorrelationMatrix | Rank correlation structure |
| 04 | DistanceMatrixComputed | 1 − \|r\| inter-candidate distances |
| 05 | HierarchicalClusteringPerformed | Complete-linkage clustering |
| 06 | ClassDiscoveryPerformed | Natural break-point class assignment |
| 07 | SeedStabilityOfClasses | Bootstrap co-clustering frequency |
| 08 | NScalingStabilityOfClasses | Persistence across N=40, 80 |
| 09 | LawRobustnessOfClasses | Persistence across exp/gaussian |
| 10 | NullControlClassSeparation | Structured vs. null class count |
| 11 | ClassPurityScores | Intra-r vs. inter-r per class |
| 12 | ClassHierarchyGenerated | Dendrogram merge order |
| 13 | NoPhysicalComparisonUsed | Verification gate |
| 14 | Classification | Overall A/B/C class rating |
| 15 | ClaimDisciplineReport | Full claim discipline |
