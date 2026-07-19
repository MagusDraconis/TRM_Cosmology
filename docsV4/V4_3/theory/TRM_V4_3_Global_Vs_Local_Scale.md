# TRM V4.3 — Global vs. Local Scale

**Status:** ANALYSIS COMPLETE
**Suite:** `V4_3_GlobalVsLocalScale_Tests.cs`
**Tag:** `V4_3_GVLS`
**Branch:** `feature/v4.3-geometric-scale-interpretation`
**Base:** `v4.2-physical-calibration-complete` (1744 tests)
**Date:** 2026-07-15

---

## 1. Motivation

GSCS surveyed 12 geometric scale candidates. GSC clustered them by pairwise correlation structure. This suite addresses the next question:

> **Is TRM geometry better characterized by a global geometric scale or a local geometric scale?**

MeanDist is the V4.2 baseline — a global average over all pairwise distances. But the TRM attractor also encodes local structure: neighbor-shell distances (LocalShellScale), curvature-zone distances (CurvatureShellScale), and observer-frame distances (ObserverFrameScale). The central question is whether these local scales carry information that global averages do not capture.

---

## 2. Candidate Groups

| Group | Candidates | Geometric Category |
|:---|:---|:---|
| **GLOBAL** (4) | MeanDist, MedianDist, TrimmedMeanDist, PercentileP90 | Global distance estimators |
| **LOCAL** (3) | LocalShellScale, CurvatureShellScale, ObserverFrameScale | Local / shell / observer geometry |
| **CAUSAL** (1) | CausalHorizonScale | Bridge candidate (median distance) |

---

## 3. Metrics

### 3.1 Within-Group Cohesion

Mean absolute Pearson correlation |r| between all candidates within the GLOBAL group and within the LOCAL group. High within-group cohesion (>0.7) indicates the group forms a coherent class.

### 3.2 Between-Group Leakage

Mean absolute Pearson correlation |r| between GLOBAL and LOCAL candidates. Low leakage (<0.5) indicates the groups are independent.

### 3.3 Globality Score

```
Globality = within-r(GLOBAL) - max( between-r(GLOBAL, LOCAL), between-r(GLOBAL, CAUSAL) )
```

Positive globality (>0.1) indicates GLOBAL candidates are more correlated with each other than with LOCAL/CAUSAL candidates.

### 3.4 Locality Score

```
Locality = within-r(LOCAL) - max( between-r(LOCAL, GLOBAL), between-r(LOCAL, CAUSAL) )
```

Positive locality (>0.1) indicates LOCAL candidates are more correlated with each other than with GLOBAL/CAUSAL candidates.

### 3.5 Hierarchy Score

Normalized difference in mean rank between GLOBAL and LOCAL groups when sorted by scale magnitude. Positive indicates GLOBAL scales tend to be larger; negative indicates LOCAL scales tend to be larger.

### 3.6 Scale Independence

```
Independence = 1 - |Pearson r(global_composite, local_composite)|
```

Composites are z-scored within-group means. High independence (>0.3) indicates global and local composites carry distinct information.

---

## 4. Stability Analysis

| Metric | Test | Verified |
|:---|:---|:---|
| Seed stability | GVLS_06 | Cross-seed CV compared |
| N-scaling | GVLS_07 | Group metrics at N=40, 80 |
| Law robustness | GVLS_08 | Exponential vs. gaussian |
| Null controls | GVLS_09 | Random distances destroy hierarchy |

---

## 5. Classification

| Classification | Criterion | Interpretation |
|:---|:---|:---|
| **GLOBAL-DOMINATED** | Globality > 0.2, Locality < 0.05 | Global scale captures most geometric information; local is redundant |
| **LOCAL-DOMINATED** | Locality > 0.2, Globality < 0.05 | Local scale captures most geometric information; global is redundant |
| **MULTI-SCALE** | Globality > 0.1 and Locality > 0.1 | Both global and local carry independent structure |
| **UNRESOLVED** | Neither meets threshold | Insufficient separation to classify |

---

## 6. Interpretation Paths

| Classification | Implication for G_eff | Recommended Follow-up |
|:---|:---|:---|
| GLOBAL-DOMINATED | Refine MeanDist-like estimator; local provides no new information | Focus on statistical refinement of global scale |
| LOCAL-DOMINATED | Local scale may be the true geometric invariant; global is a noisy average | Deep-dive on LocalShellScale / ObserverFrameScale |
| MULTI-SCALE | Both scales needed for complete G_eff description | Hierarchical scale model |
| UNRESOLVED | Need larger N and more seeds to resolve | Re-run with N=200+, nS=30+ |

---

## 7. Recommended Next Suite

`V4_3_GeometricScaleHierarchy_Tests.cs` — investigate whether the global-local hierarchy forms a stable rank ordering across N and laws, and whether the causal horizon scale serves as a bridge or occupies its own tier.

---

## 8. Claim Discipline

### SUPPORTED

- 8 candidates in 3 groups: GLOBAL (4), LOCAL (3), CAUSAL (1).
- Global and local scale metrics computed: within-group r, between-group r.
- Cross-scale correlation matrix computed across all candidates.
- Hierarchy score (global vs. local rank ordering) computed.
- Scale independence (global composite vs. local composite) computed.
- Seed stability, N-scaling stability, law robustness confirmed.
- Null controls verified: unstructured distances destroy hierarchy metrics.
- Classification: GLOBAL-DOMINATED / LOCAL-DOMINATED / MULTI-SCALE / UNRESOLVED.
- No physical constants, SI comparisons, or astrophysical data used.

### CONDITIONAL

- Group definitions follow GSCS classification.
- Results depend on finite N (40–80), nS (6–12), primary regime.
- Law robustness tested on exponential/gaussian only.
- Scale independence uses z-score compositing — definition experimental.

### HYPOTHESIS

- Global scales form one coherent class; local scales may carry independent structure.
- The causal horizon scale may bridge global and local geometry.
- A true multi-scale hierarchy may require both global and local length scales.

### NOT CLAIMED

- Physical c or G derived, compared, or used.
- SI calibration performed or modified.
- Spacetime, Lorentz, SR, GR, Einstein equations derived.
- Astrophysical data used.
- V4.2 frozen predictions modified.

---

## Appendix: Test Suite Structure

| # | Test | Purpose |
|:--:|:---|:---|
| 01 | FrozenInputsAvailable | Confirm GSCS/GSC inputs |
| 02 | GlobalScaleMetricsComputed | Within-group r, globality score |
| 03 | LocalScaleMetricsComputed | Within-group r, locality score |
| 04 | HierarchyMetricsComputed | Rank ordering, hierarchy score |
| 05 | CrossScaleCorrelationComputed | Full 8×8 correlation matrix |
| 06 | SeedStabilityConfirmed | CV across seeds |
| 07 | NScalingConfirmed | Group metrics at N=40, 80 |
| 08 | LawRobustnessConfirmed | Exp vs. gaussian |
| 09 | NullControlsDestroyHierarchy | Random distances abolish structure |
| 10 | ScaleIndependenceComputed | Global vs. local composite |
| 11 | GlobalVsLocalClassification | GLOBAL/LOCAL/MULTI/UNRESOLVED |
| 12 | DocumentationGenerated | Output summary |
| 13 | NoPhysicalComparisonUsed | Verification gate |
| 14 | ClaimDisciplineReport | Full claim discipline |
