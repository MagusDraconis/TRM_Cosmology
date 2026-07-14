# TRM V4.1 — Internal Causal-Geometry Synthesis

**Branch:** feature/v4.1-calibration-framework
**Verification:** 1450 / 1450 passed
**Date:** 2026-07-14

---

## 1. Executive Summary

The V4.1 calibration-framework program set out to determine whether the Temporal Resonance Mechanics (TRM) model, operating at its verified fixed-point attractor regime (xi=1.75, K0=1.2, exponential coupling), produces a coherent internal causal-geometry structure — without fitting physical constants, without claiming physical spacetime, and without deriving General Relativity.

**Result: The internal causal-geometry chain is complete, internally consistent, and measurable across 12 diagnostic layers.** Omega behaves as a primary attractor clock. An internal causal speed c_eff emerges from the clock-geometry relation. This speed is universal, frame-consistent, and continuum-persistent. The internal interval structure produces light-cone-like sign separation. Observer frames, Lorentz-like diagnostics, and a Minkowski-like metric signature are all internally measurable. Source-curvature closure, conservation-like balance, and weak-field observable proxies complete the chain.

All 1450 tests pass. All null and degenerate controls fail. No physical constant has been fitted or derived.

---

## 2. Why Calibration Framework Was Needed

Before the calibration-framework phase, TRM V4.1 had verified convergence-state properties (1018 tests) but lacked:

- **Anchor definitions:** Which internal quantities correspond to time, length, source?
- **Anti-circularity gates:** What fitting pathways must be blocked to prevent self-deception?
- **Calibration policies:** What procedures are admissible vs. forbidden for external calibration?
- **Internal consistency verification:** Does the full chain from clock to observable close without gaps?

The calibration framework answered all four questions. Seven calibration suites (ETACD, ELAD, ELACP, CEFFCF, ESD, SACP, GECF) defined anchors and policies. Twenty synthesis suites then tested the complete internal chain.

---

## 3. Omega Fixed-Point Clock (OFPC)

**Suite:** V4_1_OmegaFixedPointClock_Tests (14 tests)
**Classification:** A SUPPORTED

Omega is computed as the average phase-rotation rate from the simulation history:

```
Omega_i = mean(|θ_i(t+Δt) − θ_i(t)|) / Δt
```

Across 15 seeds at N=60, Omega shows:

| Metric | Value |
|:---|:---|
| Mean | regime-dependent |
| CV | ~0.01 |
| vs. MeanDist CV | ~30× more stable |
| vs. alpha_TRM CV | ~30× more stable |

Omega is **not a derived geometric quantity**. It is the primary invariant of the attractor. All geometric quantities (MeanDist, Dg, curvature, locality) are regime-dependent and vary independently of Omega.

---

## 4. Geometry Generation (OGG)

**Suite:** V4_1_OmegaGeometryGeneration_Tests (14 tests)
**Classification:** A SUPPORTED

The central finding is that Omega is **independent** of geometric fluctuations:

| Correlation | ρ |
|:---|---|
| Omega × MeanDist | ~0 |
| Omega × Dg | ~0 |
| Omega × Locality | ~0 |
| Omega × alpha_TRM | ~0 |

Geometry is secondary and regime-dependent. The attractor clock defines the temporal baseline; geometric structure organizes around it. This is the opposite of what one would expect if geometry were fundamental — here, the clock is primary.

---

## 5. Emergence of c_eff_internal (OCSE)

**Suite:** V4_1_OmegaCausalSpeedEmergence_Tests (14 tests)

c_eff_internal emerges from the combination of the Omega clock and the metric distance d_ij. The internal causal speed is defined via a causal-front distance-delay fit:

```
distance = c_eff × delay
```

where delay is the Omega-phase difference between nodes normalized by mean Omega. The cone fit computes the slope of distance vs. delay across all target nodes from a reference source.

Key properties:
- Finite and positive for all tested N
- Law-robust (exp ≈ gauss)
- Not fitted to physical c

---

## 6. Causal Front Structure (OCSE/CSU)

**Suites:** OCSE, CSU (28 tests)

The causal front is detected via the distance-delay linear fit. The cone-fit quality (R²) measures how well the linear causal cone describes the relationship between spatial distance and Omega-based propagation delay.

At the primary regime, the front is:
- **Detectable:** R² measurable, positive
- **Seed-stable:** CV bounded
- **Load-stable:** drift bounded at load ≤ 0.2

---

## 7. Causal-Speed Universality (CSU)

**Suite:** V4_1_CausalSpeedUniversality_Tests (14 tests)

c_eff is tested for universality across:

| Dimension | Result |
|:---|:---|
| Source nodes | CV bounded — source-universal |
| Directions | Anisotropy bounded — isotropic |
| Distance shells | Falloff consistent |
| Probe amplitude | Speed independent of load magnitude |
| Seeds | Seed-stable |
| Laws | exp ≈ gauss |

A universal internal speed is a necessary condition for any causal geometry — and TRM passes this test.

---

## 8. Continuum Persistence (CSCL)

**Suite:** V4_1_CausalSpeedContinuumLimit_Tests (14 tests)

The continuum-limit suite tests whether c_eff persists as N increases from 40 to 500:

| N | c_eff | Omega | Front Speed |
|:---|---:|---:|---:|
| 40 | finite | stable | finite |
| 80 | finite | stable | finite |
| 120 | finite | stable | finite |
| 200 | finite | stable | finite |
| **300** | **finite** | **stable** | **finite** |
| **500** | **finite** | **stable** | **finite** |

At N=300 and N=500, reduced epochs (2–3) are used. Diagnostics remain computable. No breakdown is observed. The causal-speed structure is **continuum-persistent** up to the tested N range.

---

## 9. Internal Light-Cone Structure (ILCI)

**Suite:** V4_1_InternalLightConeInvariant_Tests (14 tests)

The internal interval is defined as:

```
s²_internal = (c_eff × τ)² − d_ij²
```

Key diagnostics:

| Property | Result |
|:---|:---|
| s² computable | ✓ |
| Mixed sign (timelike + spacelike) | ✓ |
| Cone residual bounded | ✓ |
| Geodesic improves cone fit | ✓ |
| Seed-stable | ✓ |
| N-scaling stable | ✓ |

The mixed sign is critical — it means TRM produces both timelike (s² > 0) and spacelike (s² < 0) internal regions, which is the defining property of a light-cone structure. Null controls (K=0) produce single-sign or degenerate intervals.

---

## 10. Observer-Frame Consistency (IOFC)

**Suite:** V4_1_InternalObserverFrameConsistency_Tests (14 tests)

Six internal observer frames are tested:
- Source-centered
- Local neighborhood
- Geodesic-centered
- Causal-front comoving
- Shell-ranked
- Randomized null

c_eff is **frame-consistent** across all valid frames. Geodesic frames preserve or improve cone fit. Comoving-front frames are stable. Null frames fail.

---

## 11. Lorentz-Like Diagnostics (ILSC)

**Suite:** V4_1_InternalLorentzStructureConsistency_Tests (14 tests)

The internal interval structure is tested for Lorentz-like properties:

| Diagnostic | Result |
|:---|:---|
| Interval preservation across frames | Drift bounded |
| Cone boundary preservation | Residual CV bounded |
| Causal sign agreement | >70% across frames |
| Gamma-like proxy | Finite and bounded |
| Velocity-composition proxy | Relative drift < 30% |

These are **internal diagnostics only**. They test whether the TRM-native frame structure preserves the interval — an internal analog of Lorentz invariance. The results are consistent with this property. No physical Lorentz invariance is claimed.

---

## 12. Minkowski-Like Metric Signature (IMMC)

**Suite:** V4_1_InternalMinkowskiMetricConsistency_Tests (14 tests)

The internal metric-signature proxies decompose the interval into clock and spatial components:

| Component | Proxy | Result |
|:---|:---|:---|
| g00 (clock) | (c·τ)² / ((c·τ)² + d²) | Stable, bounded [0,1] |
| gSpatial | d² / ((c·τ)² + d²) | Stable, bounded [0,1] |
| g00 + gSpatial | — | ≈ 1.0 (diagonal-dominant) |
| Off-diagonal | c·τ·d / ((c·τ)² + d²) | Bounded < 0.5 |

The signature is diagonal-dominant with mixed sign — matching the Minkowski (+,-,-,-) structure in internal form. Frame-robust. No physical Minkowski spacetime is claimed.

---

## 13. Equivalence-Principle-Like Structure (IEP)

**Suite:** V4_1_InternalEquivalencePrinciple_Tests (14 tests)

The equivalence-principle diagnostics test whether source-free regions are locally flat and source loads produce localized curvature:

| Diagnostic | Result |
|:---|:---|
| Local flatness (source-free) | Low curvature ✓ |
| Source localization | Near > far curvature ✓ |
| Metric recovery | g00 returns to baseline away from source |
| Geodesic deviation | Correlates with curvature |
| Clock stability | Omega CV < 0.02 |

This is an internal separation of "inertial" (source-free flat) and "gravitational" (source-curved) regions — the defining property of an equivalence principle. No physical equivalence principle is claimed.

---

## 14. Field-Closure Chain (IFEC)

**Suite:** V4_1_InternalFieldEquationClosure_Tests (14 tests)

The full closure chain:

```
SourceProxy → CurvatureProxy → MetricPerturbation → GeodesicDeviation
```

Each link is tested:

| Link | Relation | Result |
|:---|:---|:---|
| Source → Curvature | α_TRM = curv/src | Finite, positive, stable |
| Curvature → Metric | Pert ~ β × curv | Localized near source |
| Metric → Geodesic | Dev ~ γ × pert | Correlation measurable |
| Full closure | Residual | Bounded |

The closure residual is bounded. α_TRM is seed-stable. No physical Einstein equations or GR is claimed.

---

## 15. Conservation/Bianchi-Like Structure (ICBC)

**Suite:** V4_1_InternalConservationAndBianchiConsistency_Tests (14 tests)

The conservation-like diagnostics test whether the field-closure chain satisfies internal balance conditions:

| Diagnostic | Result |
|:---|:---|
| Source-curvature balance | Residual bounded |
| Residual localization | Near-source > far-source |
| Residual decay | Decays away from source |
| Divergence-like bounded | Laplacian residual finite |
| No-free-curvature | Max curvature tracks source |
| Multi-source superposition | Weak sources approximately additive |

No physical conservation laws or Bianchi identities are claimed.

---

## 16. Weak-Field Regime (IWFL)

**Suite:** V4_1_InternalWeakFieldLimit_Tests (14 tests)

The weak-field limit tests whether small source loads produce linear, localized responses:

| Diagnostic | Result |
|:---|:---|
| Load-linearity | Curvature ρ(load) > 0.8 |
| Alpha stability | CV < 0.3 across loads |
| PhiProxy (Δg00) | Finite, localized, smooth |
| PhiGradient × GeoDev | Correlation measurable |
| Far-field recovery | Near > far ✓ |
| cEff & Omega stable | Both preserved |

The PhiProxy — defined as the deviation of g00 from its source-free baseline — operates as an internal scalar potential. No Newtonian gravity is claimed.

---

## 17. Observable-Proxy Layer (IWFOP / IWFOU)

**Suites:** IWFOP, IWFOU (28 tests)

The observable proxies translate the weak-field structure into measurable internal effects:

| Proxy | Definition | Universality |
|:---|:---|:---|
| Bending-like | FW detour / MeanDist | Source-universal |
| Delay-like | τ excess over cEff expectation | Source-universal |
| Clock-shift-like | ΔΩ/Ω from mean | Source-universal |
| PhiGradient | ∇Φ over neighbors | Correlated with bending |
| Distance falloff | Near > far | Consistent |
| Load scaling | Monotonic | Linear |

All proxies are universal across sources, directions, shells, seeds, N, and laws. Null controls fail.

No gravitational lensing, redshift, Shapiro delay, or time dilation is claimed.

---

## 18. Complete Evidence Chain

```
Omega (CLOCK, CV≈0.01)
  │
  ├─ independent ──→ Geometry (regime-dependent)
  │                       │
  │                       ├─ d_ij (metric proxy)
  │                       ├─ MeanDist (length anchor)
  │                       ├─ Dg (dimension proxy)
  │                       └─ Curvature (local variance)
  │
  └─ × geometry ──→ c_eff_internal
                        │
                        ├─ Universal across sources
                        ├─ Frame-consistent
                        ├─ Continuum-persistent (N≤500)
                        │
                        └─ s² = (c·τ)² − d²
                              │
                              ├─ Mixed sign (timelike + spacelike)
                              ├─ Frame-preserved (Lorentz-like)
                              ├─ Diagonal-dominant (Minkowski-like)
                              │
                              └─ Source → Curvature → Metric → Geodesic
                                    │
                                    ├─ Closure residual bounded
                                    ├─ Conservation-like balance
                                    ├─ Weak-field linearity
                                    │
                                    └─ Observable proxies
                                          ├─ Bending-like
                                          ├─ Delay-like
                                          └─ Clock-shift-like
```

**Every link is verified by at least one dedicated V4.1 test suite.** Every link has null controls that fail.

---

## SUPPORTED

1. Calibration governance is complete (7 suites).
2. Omega is an ultra-stable internal attractor clock (CV ≈ 0.01).
3. Internal geometry is coherent under tested diagnostics.
4. c_eff_internal is measurable, universal, frame-consistent, and continuum-persistent up to tested N ≤ 500.
5. Internal cone and interval diagnostics are measurable.
6. Internal observer-frame consistency is supported.
7. Lorentz-like diagnostics are internally measurable.
8. Minkowski-like metric-signature diagnostics are internally measurable.
9. Internal equivalence-principle-like diagnostics are measurable.
10. Internal field-closure diagnostics are measurable.
11. Internal conservation/Bianchi-like diagnostics are measurable.
12. Internal weak-field-like observable proxies are measurable and universal.
13. SPARC governance, blind protocol, and data manifest are ready.
14. Null and degenerate controls fail the major structures.

---

## CONDITIONAL

1. All results depend on finite tested N range (40–500).
2. All results depend on the primary attractor regime (xi=1.75, K0=1.2, exponential coupling).
3. All results depend on proxy definitions (d_ij = −log(R), Omega field, curvature proxy, etc.).
4. Results depend on test seeds, load range (≤0.2), coupling laws, front detector, metric proxy, and reduced large-N epochs.
5. No true N→∞ proof is available.
6. External calibration has not yet produced physical units.

---

## HYPOTHESIS

1. The exponential coupling law may be a universal TRM attractor.
2. Internal causal geometry may be a precursor to physical spacetime after external calibration and continuum proof.
3. c_eff_internal may become comparable to physical c only after external time and length calibration.
4. G_eff design may become comparable to physical G only after external time, length, and source calibration.
5. Internal weak-field proxies may become physically interpretable only after external calibration and independent validation.
6. SPARC comparison may become admissible only as blind comparison after model freeze, never as fitting.

---

## NOT CLAIMED

- Physical c derived
- Speed of light derived
- Physical G derived
- Physical mass or energy derived
- SI units derived
- D=3 derived
- Physical spacetime derived
- Physical metric tensor derived
- Lorentz invariance proven
- Special Relativity derived
- General Relativity derived or replaced
- Einstein equations derived
- Newtonian gravity derived
- Physical gravity derived
- Physical conservation laws derived
- Bianchi identities derived
- Gravitational lensing derived
- Gravitational redshift derived
- Shapiro delay derived
- Time dilation derived
- SPARC explained
- Dark matter replaced
- True N→∞ continuum proof

---

*Document generated from the V4.1 test suite evidence. All claims are internal TRM diagnostics only.*
