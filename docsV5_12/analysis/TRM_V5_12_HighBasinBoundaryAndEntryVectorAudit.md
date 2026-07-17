# TRM V5.12 HBI: High-Basin Boundary and Entry Vector Audit

**Branch:** feature/v5.12-high-basin-entry-conditions
**Status:** COMPLETE
**Date:** 2026-07-17

---

## Quick Summary

Natural High basin is located in a lower-d, higher-K, lower-Ks region relative to Natural Low. K-graft transient induction pushes states in the OPPOSITE direction: higher-d, lower-K. This explains why transient induction fails to reach the Natural High basin.

**Gate A REACHED:** Induction moves AWAY from High basin entry vector.

---

## 1. Entry Vector (N=71, seeds 0-99)

### Natural Centroids (d_mean, K_mean, K_std)

| Class        | d_mean | K_mean | K_std | Count |
|--------------|--------|--------|-------|-------|
| Natural Low  | 0.4634 | 0.9570 | 0.1918| ~73 |
| Natural High | 0.4205 | 0.9795 | 0.1786| ~27 |

### Lo→Hi Vector

| Component | Delta | % Contribution |
|-----------|-------|-----------------|
| d_mean    | -0.0429 | -85% |
| K_mean    | +0.0225 | +45% |
| K_std     | -0.0132 | -26% |
| **Magnitude** | **0.0502** | |

### Interpretation

The High basin is a **lower-d, higher-K, lower-Ks** region.
- **-85% d_mean contribution**: High-separation (d) is ANTI-correlated with High basin entry.
- **+45% K_mean contribution**: Higher coupling drives toward High basin.
- **-26% K_std contribution**: Narrower coupling distribution accompanies High basin.

**K-graft increases d** (d_max +13.5 from V5.6 MGCA). This pushes states in the exact OPPOSITE direction from the Natural High basin.

---

## 2. State-Class Projections onto Entry Vector

| Class      | Count | Mean Proj | Range | Direction |
|------------|-------|-----------|-------|-----------|
| Natural Lo | ~73   | 0.0000    | [-0.099, 0.122] | centered |
| Natural Hi | ~27   | **+0.0502** | [-0.074, 0.165] | TOWARD |
| Transient  | 12    | **-0.2567** | [-0.467, 0.111] | STRONGLY AWAY |
| Delayed    | 5     | +0.0591   | [-0.221, 0.254] | toward |
| Failed     | 16    | -0.0625   | [-0.410, 0.346] | away |
| Persist    | 1     | +0.0533   | [0.053, 0.053] | toward |

### Key Finding

Transient induced states have the most NEGATIVE projection (-0.257) — they are geometrically FURTHER from the High basin than the original Natural Low states. K-graft pushes in the wrong direction.

The single Persist seed projects at +0.053, closest to Natural High centroid — but this is 1/50 seeds and requires validation.

---

## 3. Decision Gates

| Gate | Description | Status |
|------|-------------|--------|
| **Gate A** | Induction moves away from High entry vector | **REACHED** |
| Gate B | Boundary reached but not persistent | NOT TESTED |
| Gate C | Projection produces persistent High entry | NOT TESTED |
| **Gate D** | K-graft is orthogonal/off-manifold | **CONDITIONALLY SUPPORTED** (transient states strongly anti-aligned with entry vector) |
| Gate E | Entry vector N-dependent | DEFERRED to cross-N |
| Gate F | No stable boundary | NOT REACHED (boundary is stable at N=71) |

---

## 4. Why Low→High Induction Fails

The causal chain:

1. K-graft (or K-boost) increases coupling K
2. Increased K → larger state separation in dynamics
3. Larger separation → larger d after Nm (Nm is d-space amplifier, V5.6 MGCA)
4. Larger d → K decreases via Cupd (K=K0·exp(-d/ξ))
5. Net effect: HIGH d, EXACTLY opposite from Natural High basin (low d)

**K-graft is not just misdirected — it is ANTI-ALIGNED with the High-basin entry vector.**

---

## 5. Structure of the High Basin

Natural High = lower d_mean + higher K_mean + narrower K_std.

This is consistent with V5.9 BCI: High→Low conversion works because we compress d (directly toward Low direction). Low→High fails because amplifying K → amplifies d → away from High direction.

The asymmetry is built into the Cupd exponential coupling K=K0·exp(-d/ξ):
- Decreasing d → increasing K → natural High (amplifies downward-d→upward-K coupling)
- Increasing K → increasing d → natural Low (damping effect via exp(-d/ξ))
- This is a **one-way exponential channel**: d-compression enters the High basin; K-amplification does not.

---

## 6. Claim Discipline Audit

| Claim | Status |
|-------|--------|
| d_mean is ANTICORRELATED with High basin entry | **SUPPORTED** (-85% contribution) |
| K-graft moves away from High entry vector | **SUPPORTED** (transient proj = -0.257) |
| High basin has lower d, higher K | **SUPPORTED** |
| Entry vector is causal | **NOT CLAIMED** |
| Physical interpretation | **NOT CLAIMED** |
| Cross-N validity | **NOT CLAIMED** |
| Universal criticality | **NOT CLAIMED** |

---

## 7. Recommended Next Suite

**HBE_C — High-Basin Entry via Controlled d-Compression**

Since the entry vector is dominated by d_mean (85%), and High→Low suppression succeeds by compressing d, test whether controlled d-compression can produce persistent Low→High entry.

Hypothesis: d-compression before Cupd (not K-amplification) is the correct direction toward the Natural High basin.

Protocol: Apply d-space compression (similar to V9 suppressor but scaled for entry) to Natural Low seeds and test projection onto Hi entry vector.

---

## Test Summary

- File: `TRM.Tests/V5_12/V5_12_HighBasinBoundaryAndEntryVectorAudit_Tests.cs`
- Tests: 2 (HBI_01, HBI_02)
- Passed: 2
- Runtime: ~25s
