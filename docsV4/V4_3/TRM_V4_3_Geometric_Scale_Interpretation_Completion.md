# TRM V4.3 — Geometric Scale Interpretation — COMPLETION REPORT

**Status:** COMPLETE
**Suite:** `V4_3_GeometricScaleBranchSynthesis_Tests.cs`
**Tag:** `V4_3_GSBS`
**Branch:** `feature/v4.3-geometric-scale-interpretation`
**Base:** `v4.2-physical-calibration-complete` (1744 tests)
**Date:** 2026-07-15

---

## Executive Summary

V4.3 has systematically investigated the geometric meaning of the TRM length scale. Starting from the V4.2 finding that MeanDist variance (~30% CV) persists to N=1000 and dominates G_eff_SI uncertainty, V4.3 asked:

> **Are there alternative geometric scales that capture the attractor's true length invariant more stably than MeanDist?**

The answer is nuanced. Through 8 suites and 113 tests, V4.3 found:

1. **12 geometric scale candidates exist** — from simple statistical variants (MedianDist, TrimmedMeanDist) to fundamentally different geometric probes (LocalShellScale, ObserverFrameScale, CausalHorizonScale).

2. **Candidates form correlation classes** — global-distance estimators (MeanDist, MedianDist, TrimmedMeanDist) are tightly coupled, while local and causal candidates carry partially independent information.

3. **A directed hierarchy DAG exists** — some scales function as roots (fundamental), others as leaves (derived). This hierarchy is reproducible across seeds, N, coupling laws, and load amplitudes.

4. **No multiplicative proxy dramatically improves on MeanDist's seed CV** — all GLOBAL and LOCAL candidates show similar ~30% CV. The variance appears to be a genuine attractor property, not a poor proxy choice.

5. **c_eff_SI is structurally robust** — the length proxy cancels in c_eff_SI regardless of which multiplicative scale is used. Only G_eff_SI is proxy-sensitive (cubic dependence).

6. **A geometrically-selected PRIMARY candidate is identified** — purely from geometric criteria with no physical comparison.

**Bottom line:** MeanDist remains the recommended baseline. The geometric evidence does not identify a clearly superior alternative, but it does provide a rigorous framework for evaluating any future candidate.

---

## V4.2 Background

V4.2 established:

- `c_eff_SI = Kr86/Cs133 × Omega` (MeanDist cancels, CV ~0.01)
- `G_eff_SI = alpha_TRM × L³ / (T² × M)` (MeanDist³-dominated, CV ~0.90)
- MeanDist CV ~0.30 persists to N=1000 (CBN500)
- α_TRM CV ~0.15 (post-ATR)
- All calibration anchors are non-circular

The central V4.3 question: **Is MeanDist CV ~0.30 a fundamental attractor property, or can a different geometric scale proxy reduce this variance?**

---

## Candidate Discovery (GSCS — 14 tests)

12 candidates surveyed. All computable from TRM distance matrices without external inputs. Seed stability, N-scaling, law robustness, null separation, weak-field, geodesic, causal-front, and observer-frame compatibility assessed.

| Class | Count | Candidates |
|:---|:--:|:---|
| A | 1 | MeanDist (baseline) |
| B | 7 | MedianDist, TrimmedMeanDist, LocalShellScale, CausalHorizonScale, PercentileDistanceScale, CurvatureShellScale, ObserverFrameScale |
| C | 4 | GeodesicMeanDist, CurvatureRadiusProxy, SpectralScale, MetricProxyScale |

---

## Scale Classes (GSC — 15 tests)

Hierarchical clustering on correlation distance confirms class structure. Global-distance estimators form a tight cluster. Local and observer-frame candidates form looser groupings. All classes destroyed under null (random distances).

---

## Global vs. Local (GVLS — 14 tests)

GLOBAL (4), LOCAL (3), and CAUSAL (1) groups defined. Globality and locality scores computed. Classification: GLOBAL-DOMINATED / LOCAL-DOMINATED / MULTI-SCALE / UNRESOLVED. Cross-scale correlation matrix confirms partial independence of local scales from global averages.

---

## Hierarchy (GSH — 14 tests)

Directed DAG via R² asymmetry constructed. Hierarchy strength, depth, and bootstrap reproducibility measured. N-scaling, law, and load robustness verified. Null controls (random distances, K=0) destroy hierarchy.

---

## Scale Roles (GSI — 14 tests)

Geometric roles assigned:

| Role | Definition |
|:---|:---|
| PRIMARY | Root-level, high outdegree — fundamental |
| SECONDARY | Mid-level, moderate contribution |
| DERIVED | Leaf-level, largely predictable |
| BRIDGE | Connects GLOBAL and LOCAL domains |
| REDUNDANT | R² > 0.95 predicted by another |

---

## Selection (GSS — 14 tests)

Composite SelectionScore with fixed a priori weights (stability 0.25, hierarchy 0.25, uniqueness 0.20, null separation 0.15, bridge 0.15). PRIMARY / SECONDARY / RESERVE / REJECT classification. No physical comparison used.

---

## Prospective Anchor Protocol (PLAP — 14 tests)

- c_eff_SI invariant to multiplicative proxy change ✓
- G_eff_SI cubic-sensitive to proxy CV △
- All 10 anti-circularity gates pass ✓
- 4-phase freeze protocol defined ✓

---

## Supported Findings

| Domain | Finding |
|:---|:---|
| Candidates | 12 geometric scales defined, 8 advanced |
| Classes | Correlation + clustering structure confirmed |
| Global/Local | GLOBAL-DOMINATED / MULTI-SCALE classification |
| Hierarchy | Directed DAG reproducible across conditions |
| Roles | PRIMARY/SECONDARY/DERIVED/BRIDGE/REDUNDANT |
| Selection | Composite Score with fixed a priori weights |
| Protocol | c_eff robust, freeze protocol defined |
| Discipline | No physical c/G used in any V4.3 suite |

---

## Conditional Findings

All results depend on: finite N (40–80), primary regime (ξ=1.75, K₀=1.2), exponential baseline, R² asymmetry threshold (0.05), redundancy threshold (0.95), SI-unit placeholders (1.0).

---

## Hypotheses

- MeanDist CV ~0.30 may be genuine attractor variance
- Lower-CV proxy may reduce G_eff uncertainty
- Hierarchy reflects true structural dependency
- Geometrically-selected anchor may serve future branches

---

## Not Claimed

Physical c, G, SI calibration, spacetime, Lorentz, SR, GR, Einstein equations, astrophysical data, MeanDist replacement, N→∞ proof, dark matter/energy.

---

## Remaining Open Problems

1. **MeanDist variance** — Why does CV ~0.30 persist to N=1000?
2. **N→∞ limit** — Do classes converge or diverge?
3. **Regime dependence** — ξ and K₀ parameter space exploration
4. **C-candidate refinement** — Experimental definitions need stabilization
5. **Observer-frame invariance** — Does it converge to the global mean?
6. **Gravitational interpretation** — Deferred to prediction branch
7. **Causal structure** — Propagation limit or statistical proxy?
8. **Multi-scale G_eff** — Do global and local both contribute?

---

## Recommended Next Branch

```
feature/v4.4-prospective-length-anchor-validation
```

Objectives:
1. Adopt the GSS-selected PRIMARY geometric scale as the length anchor
2. Execute the PLAP 4-phase freeze protocol
3. Generate new SHA-256 manifest with the selected proxy
4. Verify c_eff_SI unchanged (Omega-dominated)
5. Compute new G_eff_SI with refined length anchor
6. Execute blind comparison to physical G (gated, post-freeze)
7. Document uncertainty budget with new proxy CV

---

## V4.3 Final Classification

**COMPLETE** — 113 tests across 8 suites, all passed. No physical comparison executed. Geometric evidence chain complete.
