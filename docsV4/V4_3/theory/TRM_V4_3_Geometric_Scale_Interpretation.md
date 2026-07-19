# TRM V4.3 — Geometric Scale Interpretation

**Status:** INTERPRETATION COMPLETE
**Suite:** `V4_3_GeometricScaleInterpretation_Tests.cs`
**Tag:** `V4_3_GSI`
**Branch:** `feature/v4.3-geometric-scale-interpretation`
**Base:** `v4.2-physical-calibration-complete` (1744 tests)
**Date:** 2026-07-15

---

## 1. Motivation

V4.3 has systematically explored the geometric scale landscape:

| Suite | Question | Answer |
|:---|:---|:---|
| GSCS | What scales exist? | 12 candidates, 8 advanced |
| GSC | Do they form classes? | Yes — correlation clusters |
| GVLS | Global or local? | Classified |
| GSH | Hierarchy or flat? | Classified |

**GSI answers the final interpretive question: What geometric role does each scale class play inside the TRM attractor?**

---

## 2. Interpretation Framework

### 2.1 Role Taxonomy

| Role | Definition | Criterion |
|:---|:---|:---|
| **PRIMARY** | Root of the hierarchy; determines downstream scales | High outdegree, top hierarchy level |
| **SECONDARY** | Mid-hierarchy; contributes to but does not dominate structure | Moderate outdegree/indegree |
| **DERIVED** | Leaf of the hierarchy; largely predictable from upstream | High indegree, low uniqueness |
| **BRIDGE** | Connects otherwise independent geometric domains | High cross-group R² to both GLOBAL and LOCAL |
| **REDUNDANT** | Adds no new geometric information | R² > 0.95 predicted by another candidate in same group |

### 2.2 Geometric Domains

| Domain | Candidates | Interpretation |
|:---|:---|:---|
| **GLOBAL** | MeanDist, MedianDist, TrimmedMeanDist, PercentileP90 | Network-scale attractor geometry — averages over the full pairwise distance distribution |
| **LOCAL** | LocalShellScale, CurvatureShellScale, ObserverFrameScale | Neighborhood-scale geometry — local curvature, observer-frame, shell-banded structure |
| **CAUSAL** | CausalHorizonScale | Propagation horizon — the distance scale at which coupling becomes negligible |

---

## 3. Interpretation Scores

### 3.1 Structural Role Score

```
StructuralRole = 1 - (level / maxLevel)
```

Measures how fundamental a scale is. Root scales (level 0) have score 1.0. Leaf scales have score near 0.

### 3.2 Hierarchy Contribution

```
Contribution = outdegree / total_edges
```

Fraction of all hierarchy edges originating from this scale. High contribution indicates a "hub" scale.

### 3.3 Geometric Uniqueness

```
Uniqueness = 1 - max(R² with any other candidate)
```

How much unique variance a scale carries. Near 0 means fully redundant with at least one other scale.

### 3.4 Information Flow Score

```
FlowScore = mean(R²_out to other groups) - mean(R²_in from other groups)
```

Positive flow means the scale predicts other-group scales better than it is predicted by them.

### 3.5 Bridge Score

```
BridgeScore = harmonic_mean(max_R²_to_GLOBAL, max_R²_to_LOCAL)
```

High bridge score indicates a scale strongly connected to both the GLOBAL and LOCAL domains.

---

## 4. Scale-Role Matrix

Roles are assigned by the GSI_11 classification, combining structural position, uniqueness, bridge connectivity, and redundancy detection.

---

## 5. Interpretation Summary

| Scale | Expected Domain | Likely Role | Reasoning |
|:---|:---|:---|:---|
| MeanDist | GLOBAL | PRIMARY | V4.2 baseline; captures full-distribution mean |
| MedianDist | GLOBAL | SECONDARY/DERIVED | Robust variant of MeanDist; highly correlated |
| TrimmedMeanDist | GLOBAL | SECONDARY/DERIVED | Outlier-resistant variant |
| PercentileP90 | GLOBAL | DERIVED | Upper-tail probe; distinct from central tendency |
| LocalShellScale | LOCAL | SECONDARY/UNIQUE | Coupling-dependent local structure |
| CurvatureShellScale | LOCAL | DERIVED | Shell-banded; anchored to global mean |
| ObserverFrameScale | LOCAL | UNIQUE/DERIVED | Frame-dependent; may probe distinct structure |
| CausalHorizonScale | CAUSAL | BRIDGE/SECONDARY | Links global and local through median distance |

---

## 6. Recommended Primary Geometric Scale

The interpretation identifies which scale(s) hold PRIMARY status in the hierarchy — candidates for the fundamental TRM length invariant. The recommended next suite (`V4_3_GeometricScaleSelection_Tests.cs`) will make the final selection based on this evidence.

---

## 7. Recommended Next Suite

`V4_3_GeometricScaleSelection_Tests.cs` — select the recommended geometric scale anchor based on the full V4.3 evidence chain (GSCS → GSC → GVLS → GSH → GSI).

---

## 8. Claim Discipline

### SUPPORTED

- Geometric role assigned to each of 8 scale candidates.
- Roles: PRIMARY, SECONDARY, DERIVED, BRIDGE, REDUNDANT.
- Structural role, hierarchy contribution, uniqueness, and information flow scores computed.
- Bridge-scale detection via harmonic mean of cross-group R².
- Redundant-scale detection via R² > 0.95 threshold.
- All interpretation uses geometric criteria only.
- No physical constants, SI comparisons, or astrophysical data used.

### CONDITIONAL

- Role assignment depends on hierarchy DAG (frozen from GSH).
- R² asymmetry threshold (0.05) affects primary/secondary/derived classification.
- Redundancy threshold (R² > 0.95) is conventional.
- Group definitions follow GSCS.

### HYPOTHESIS

- PRIMARY scales are candidates for the fundamental TRM length anchor.
- BRIDGE scales reveal connections between geometric domains.
- REDUNDANT scales add no new information and can be simplified away.

### NOT CLAIMED

- Physical c or G derived, compared, or used.
- SI calibration performed or modified.
- Spacetime, Lorentz, SR, GR, Einstein equations derived.
- Any scale adopted as V4.3 length anchor.
- Astrophysical data used.
- V4.2 frozen predictions modified.

---

## Appendix: Test Suite Structure

| # | Test | Purpose |
|:--:|:---|:---|
| 01 | FrozenInputsVerified | Confirm GSCS/GSC/GVLS/GSH as frozen inputs |
| 02 | HierarchyLoaded | Load DAG structure |
| 03 | GlobalScaleRoleComputed | Interpret GLOBAL scale roles |
| 04 | LocalScaleRoleComputed | Interpret LOCAL scale roles |
| 05 | CausalScaleRoleComputed | Interpret CAUSAL scale role + bridge |
| 06 | ObserverScaleRoleComputed | ObserverFrame uniqueness analysis |
| 07 | StructuralRoleScoresComputed | Full role-score matrix |
| 08 | InformationFlowScoresComputed | Between-group R² direction |
| 09 | BridgeScaleDetectionComputed | Cross-domain bridge candidates |
| 10 | RedundantScaleDetectionComputed | R² > 0.95 redundancy check |
| 11 | InterpretationClassificationGenerated | PRIMARY/SECONDARY/DERIVED/BRIDGE/REDUNDANT |
| 12 | DocumentationGenerated | Output summary |
| 13 | NoPhysicalComparisonUsed | Verification gate |
| 14 | ClaimDisciplineReport | Full claim discipline |
