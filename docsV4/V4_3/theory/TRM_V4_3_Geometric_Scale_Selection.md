# TRM V4.3 — Geometric Scale Selection

**Status:** SELECTION COMPLETE
**Suite:** `V4_3_GeometricScaleSelection_Tests.cs`
**Tag:** `V4_3_GSS`
**Branch:** `feature/v4.3-geometric-scale-interpretation`
**Base:** `v4.2-physical-calibration-complete` (1744 tests)
**Date:** 2026-07-15

---

## 1. Motivation

The V4.3 evidence chain (GSCS → GSC → GVLS → GSH → GSI) has systematically characterized the geometric scale landscape of the TRM attractor. GSS addresses the final step:

> **Which geometric scale should be recommended for a future prediction branch?**

This is forward-looking selection — it does not modify any existing V4.2 prediction, recalculate c_eff_SI or G_eff_SI, or use physical comparison. It selects purely on geometric merit.

---

## 2. Candidate Pool

| Group | Candidate | GSCS Class | GSI Role |
|:---|:---|:--:|:---|
| GLOBAL | MeanDist | A | PRIMARY/SECONDARY |
| GLOBAL | MedianDist | B | SECONDARY/DERIVED |
| GLOBAL | TrimmedMeanDist | B | SECONDARY/DERIVED |
| GLOBAL | PercentileP90 | B | DERIVED |
| LOCAL | LocalShellScale | B | SECONDARY/UNIQUE |
| LOCAL | CurvatureShellScale | B | DERIVED |
| LOCAL | ObserverFrameScale | B | UNIQUE/DERIVED |
| CAUSAL | CausalHorizonScale | B | BRIDGE/SECONDARY |

---

## 3. Selection Criteria

All criteria are geometric. No physical comparison is used.

### 3.1 Stability (weight 0.25)

```
Stability = 1 - 0.6 × min(CV, 0.5) - 0.2 × min(N-drift, 0.3) - 0.2 × min(law-drift, 0.2)
```

Combines seed CV, N-scaling drift, and coupling-law drift into a single stability score.

### 3.2 Hierarchy Contribution (weight 0.25)

```
Hierarchy = 0.6 × (1 - level/maxLevel) + 0.4 × (outdegree/totalEdges)
```

Root-level scales with high outdegree score highest.

### 3.3 Geometric Uniqueness (weight 0.20)

```
Uniqueness = 1 - max(R² with any other candidate)
```

Scales with R² > 0.95 (redundancy threshold) score near zero.

### 3.4 Null Separation (weight 0.15)

```
NullSep = min(|structured - null| / max(|structured|, 1e-9), 1.0)
```

Measures sensitivity to geometric structure vs. random noise.

### 3.5 Bridge Value (weight 0.15)

```
Bridge = harmonic_mean(max_R²_to_GLOBAL, max_R²_to_LOCAL)
```

High bridge score indicates a scale that connects the GLOBAL and LOCAL geometric domains.

### 3.6 Composite SelectionScore

```
SelectionScore = 0.25×Stability + 0.25×Hierarchy + 0.20×Uniqueness + 0.15×NullSep + 0.15×Bridge
```

Weights are fixed a priori and not tuned to any outcome.

---

## 4. Selection Classification

| Classification | Criterion | Meaning |
|:---|:---|:---|
| **PRIMARY** | Top composite score, not redundant | Recommended for future prediction branch |
| **SECONDARY** | Next-best, complementary to PRIMARY | Alternative or cross-check candidate |
| **RESERVE** | Acceptable but superseded | Fallback candidates |
| **REJECT** | Stability < 0.2 or redundancy confirmed | Not recommended for future use |

---

## 5. Selection Rationale

The weighting philosophy:
- **50% stability + hierarchy** — a good length anchor must be reproducible and structurally central
- **20% uniqueness** — avoids redundant selections
- **30% null separation + bridge** — ensures the scale is structure-dependent with geometric breadth

This is not a physical selection. It is a geometric one. A geometrically well-behaved scale may or may not improve G_eff_SI agreement — that question is deferred to a future prediction branch.

---

## 6. Recommended Next Suite

`V4_3_ProspectiveLengthAnchorProtocol_Tests.cs` — define the protocol for adopting the selected scale in a future branch, including re-freeze and re-audit requirements without modifying V4.2.

---

## 7. Claim Discipline

### SUPPORTED

- Composite SelectionScore computed for all 8 candidates.
- Selection criteria: stability, hierarchy, uniqueness, null separation, bridge value.
- Weights fixed a priori: 0.25/0.25/0.20/0.15/0.15.
- PRIMARY, SECONDARY, RESERVE, REJECT classes assigned.
- No physical constants, SI comparisons, or astrophysical data used.
- No V4.2 predictions modified. No c_eff_SI or G_eff_SI recalculated.

### CONDITIONAL

- Selection weights are informed by geometric goals, not derived from first principles.
- All metrics depend on finite N (40–80), nS (6–12), primary regime.
- The selected PRIMARY candidate is a recommendation, not an adopted replacement.

### HYPOTHESIS

- A geometrically-selected length scale may serve as a refined anchor for a future branch.
- The selection framework is forward-looking and non-circular.

### NOT CLAIMED

- Physical c or G derived, compared, or used.
- SI calibration performed or modified.
- Any scale adopted as the V4.3 length anchor.
- V4.2 baseline MeanDist replaced.
- Spacetime, Lorentz, SR, GR, Einstein equations derived.
- Astrophysical data used.

---

## Appendix: Test Suite Structure

| # | Test | Purpose |
|:--:|:---|:---|
| 01 | FrozenInputsVerified | Confirm full V4.3 chain as frozen inputs |
| 02 | CandidatePoolLoaded | 8 candidates in 3 groups |
| 03 | StabilityScoresComputed | Seed CV, N-drift, law-drift |
| 04 | HierarchyScoresComputed | DAG level + outdegree contribution |
| 05 | UniquenessScoresComputed | 1 - max R² |
| 06 | BridgeScoresComputed | Harmonic mean cross-group R² |
| 07 | SelectionScoresComputed | Composite weighted ranking |
| 08 | PrimaryCandidateSelected | Top-ranked non-redundant |
| 09 | SecondaryCandidateSelected | Next-best complementary |
| 10 | ReserveCandidateSelected | Fallback candidates |
| 11 | NoPhysicalComparisonUsed | Verification gate |
| 12 | NoRetrospectiveOptimization | Fixed a priori weights |
| 13 | DocumentationGenerated | Output summary |
| 14 | ClaimDisciplineReport | Full claim discipline |
