# TRM V4.3 — Geometric Scale Hierarchy

**Status:** HIERARCHY ANALYSIS COMPLETE
**Suite:** `V4_3_GeometricScaleHierarchy_Tests.cs`
**Tag:** `V4_3_GSH`
**Branch:** `feature/v4.3-geometric-scale-interpretation`
**Base:** `v4.2-physical-calibration-complete` (1744 tests)
**Date:** 2026-07-15

---

## 1. Motivation

GSCS surveyed 12 candidates. GSC clustered them into classes. GVLS classified the global vs. local distinction. This suite addresses the final structural question:

> **Do the geometric scales form a genuine hierarchy, or are they independent dimensions?**

A hierarchy would mean that longer-range scales (global averages) determine shorter-range scales (local shells, observer frames), producing a directed parent→child structure. Independence would mean each scale probes a distinct geometric degree of freedom. Redundancy would mean all scales measure the same underlying quantity through different lenses.

---

## 2. Candidate Classes (frozen from GSCS/GSC/GVLS)

| Class | Candidates | Scale Type |
|:---|:---|:---|
| **GLOBAL** (4) | MeanDist, MedianDist, TrimmedMeanDist, PercentileP90 | Global distance estimators |
| **LOCAL** (3) | LocalShellScale, CurvatureShellScale, ObserverFrameScale | Local/shell/observer scales |
| **CAUSAL** (1) | CausalHorizonScale | Bridge candidate |

---

## 3. Hierarchy Methodology

### 3.1 Scale Ordering

Candidates are ordered by their mean value (shortest → longest). This establishes a natural rank ordering from local to global scales.

### 3.2 Directed Dependency Graph

For each ordered pair (parent, child) where the parent is the larger-scale candidate:

```
R²_forward = R²(parent predicts child)
R²_backward = R²(child predicts parent)
Asymmetry = R²_forward - R²_backward
Edge(parent → child) = true iff Asymmetry > 0.05
```

Positive asymmetry means the larger-scale candidate can predict the smaller one better than vice versa, suggesting a parent→child dependency.

### 3.3 Transitive Reduction

Floyd-Warshall elimination of redundant edges: if A→B and B→C, remove A→C. The remaining edges are the direct parent→child relations.

### 3.4 Hierarchy Strength

```
Strength = (number of directed edges) / (number of ordered pairs)
```

High strength (>0.15) indicates a strongly directional dependency structure. Low strength (<0.01) indicates independence or redundancy.

### 3.5 Hierarchy Depth

The number of levels in the longest path through the directed acyclic graph (DAG). Depth ≥ 3 indicates a multi-level hierarchy. Depth = 1 indicates a flat structure.

---

## 4. Stability Verification

| Metric | Test | Verified |
|:---|:---|:---|
| Seed stability | GSH_06 | Bootstrap (8 iterations) |
| N-scaling | GSH_07 | N=40, 80 |
| Law robustness | GSH_08 | Exponential, gaussian |
| Load robustness | GSH_09 | s = 0.05, 0.10, 0.20 |
| Null controls | GSH_10 | Random distances, K=0 |

---

## 5. Null Controls

| Condition | Expected | Verified |
|:---|:---|:---|
| Random uniform distances | No hierarchy (no attractor structure) | ✓ |
| K=0 (no coupling) | No hierarchy (no geometric structure) | ✓ |
| Shuffled topology | Hierarchy different from structured | (informational) |

---

## 6. Classification

| Classification | Criterion | Interpretation |
|:---|:---|:---|
| **STRONG HIERARCHY** | Strength > 0.15, Depth ≥ 3, Strong asym ≥ 3 | Clear multi-level parent→child dependency |
| **MODERATE HIERARCHY** | Strength > 0.05, Depth ≥ 2 | Some hierarchical structure present |
| **WEAK HIERARCHY** | Strength > 0.01 | Minimal directional dependency |
| **NO HIERARCHY** | Strength ≤ 0.01 | No reproducible parent→child structure |

---

## 7. Interpretation

| Classification | Implication | Geometry Type |
|:---|:---|:---|
| STRONG HIERARCHY | A fundamental scale determines all others | One-dimensional scale manifold |
| MODERATE HIERARCHY | Partial ordering; some scales more fundamental | Partially ordered scale set |
| WEAK HIERARCHY | Scales are largely independent dimensions | Multi-dimensional scale space |
| NO HIERARCHY | All scales are either redundant or unrelated | Flat scale space |

---

## 8. Recommended Next Suite

`V4_3_GeometricScaleInterpretation_Tests.cs` — synthesize all V4.3 geometric findings into a coherent interpretation of what the TRM length scale represents, and whether a refined length anchor is supported by the geometric evidence.

---

## 9. Claim Discipline

### SUPPORTED

- Directed hierarchy graph constructed via R² asymmetry (parent→child).
- Scale dependency R² matrix computed for all 8 candidates.
- Hierarchy strength and depth computed.
- Transitive reduction applied for direct edges.
- Bootstrap stability, N-scaling, law, and load robustness verified.
- Null controls confirmed to destroy hierarchy.
- Classification: STRONG / MODERATE / WEAK / NO HIERARCHY.
- No physical constants, SI comparisons, or astrophysical data used.

### CONDITIONAL

- Hierarchy direction inferred from associative R² asymmetry, not mechanistic causation.
- Results depend on finite N (40–80), nS (6–12), primary regime.
- Transitive reduction may be sensitive to R² asymmetry threshold.

### HYPOTHESIS

- If GLOBAL→LOCAL, the global distance average is the fundamental scale.
- If LOCATION→GLOBAL, local structure determines the apparent global scale.
- A genuine hierarchy would imply a single fundamental geometric invariant.

### NOT CLAIMED

- Physical c or G derived, compared, or used.
- SI calibration performed or modified.
- Spacetime, Lorentz, SR, GR, Einstein equations derived.
- Causality beyond associative R² asymmetry.
- Astrophysical data used.
- V4.2 frozen predictions modified.

---

## Appendix: Test Suite Structure

| # | Test | Purpose |
|:--:|:---|:---|
| 01 | FrozenInputsAvailable | Confirm GSCS/GSC/GVLS as frozen inputs |
| 02 | CandidateClassesLoaded | Load 8 candidates in 3 groups |
| 03 | HierarchyGraphGenerated | Directed parent→child edges via R² asymmetry |
| 04 | ScaleDependencyGraphGenerated | Full 8×8 R² dependency matrix |
| 05 | HierarchyStrengthComputed | Strength, depth, inter-group direction |
| 06 | SeedStabilityVerified | Bootstrap hierarchy reproducibility |
| 07 | NScalingVerified | Hierarchy at N=40, 80 |
| 08 | LawRobustnessVerified | Hierarchy under exp/gaussian |
| 09 | LoadRobustnessVerified | Hierarchy under s=0.05, 0.10, 0.20 |
| 10 | NullControlsDestroyHierarchy | Random distances, K=0 |
| 11 | HierarchyClassification | STRONG/MODERATE/WEAK/NO |
| 12 | DocumentationGenerated | Output summary |
| 13 | NoPhysicalComparisonUsed | Verification gate |
| 14 | ClaimDisciplineReport | Full claim discipline |
