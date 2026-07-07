# TRM V4.1 — Synchronization Stability as a Candidate Selector of Spatial Dimension

**Date:** 2026-07-07
**Status:** EXPLORATORY — research program only
**Branch:** `feature/v4.1-emergent-space`
**Predecessors:** `TRM_V4_1_Emergent_Space.md` §3.8, §3.8.6

> This document does not modify V3.4 or V4.
> It defines a forward research direction — no claims of derivation.

---

## 1. Motivation

D = 3 is currently **ASSUMED** throughout V4 and V4.1:

- The bilocal kernel K(x,y) depends on 3D Euclidean distance d²(x,y).
- The wave equation □K = 0 is formulated in 3+1 dimensions.
- The 1/r gravitational form follows from the 3D Laplacian Green's function.
- The continuum limit of the graph Laplacian recovers ∇² only if D = 3 (§4).

Section 3.8 identified the gap. Section 3.8.6 defined five minimal criteria for
a viable 3D emergence candidate. Among these, Criterion (4) — stable
synchronisation under local perturbations — is the most promising because it
connects spatial dimension directly to oscillator dynamics rather than external
geometry.

This document explores the hypothesis:

> **Only graphs with effective dimension D ≈ 3 support stable, isotropic,
> shortcut-free phase-locked synchronisation.**

---

## 2. Problem Setup

Consider a graph G = (V, E) with N oscillators, coupling matrix K_ij, and
effective dimension D defined by:

```
N(r) ∝ r^D    (nodes within graph distance r)
```

The graph is assumed to satisfy:
- **Local coupling:** K_ij decays with graph distance; shortcuts are absent or rare.
- **Approximate isotropy:** No statistically preferred direction.
- **Uniform degree:** ⟨k⟩ ≈ 2D (approximately constant across nodes).

The dynamics are the standard Kuramoto-type phase equations:

```
dθ_i/dt = ω_i + Σ_j K_ij · sin(θ_j − θ_i)
```

where ω_i is the natural frequency of oscillator i.

Compare synchronisation behaviour across three regimes:

| Regime | Dimension | Expected connectivity | Graph Laplacian spectrum |
|:---|:---|:---|:---|
| Sub-3D | D < 3 | ⟨k⟩ < 6 | Sparse; low spectral gap |
| 3D | D = 3 | ⟨k⟩ ≈ 6 | Balanced locality; moderate gap |
| Super-3D | D > 3 | ⟨k⟩ > 6 | Dense; altered mode structure |

---

## 3. Candidate Mechanism

**Hypothesis:** Phase-locked synchronised states are most stable on graphs whose
large-scale structure is effectively 3D. Graphs with D ≠ 3 are dynamically
disfavoured — either synchronisation fails or the resulting state lacks the
properties needed for the continuum limit.

### 3.1 Sub-3D (D < 3): Too Sparse

Graphs with effective dimension below 3 — chains (D = 1), sheets (D = 2) —
have low average degree ⟨k⟩ < 6. Consequences:

- **Spectral gap:** The first non-zero eigenvalue λ₂ of the graph Laplacian is
  small, implying slow information propagation and weak synchronisation pull.
- **Percolation fragility:** Removing a few edges can disconnect the graph,
  breaking global synchronisation.
- **Anisotropy:** Low-dimensional graphs necessarily break isotropy (a chain has
  a preferred axis; a sheet has a normal direction).

**Prediction:** D < 3 graphs either fail to synchronise globally or produce
anisotropic continuum limits incompatible with isotropic ∇².

### 3.2 Super-3D (D > 3): Overconnected

Graphs with effective dimension above 3 — hypercubic lattices, dense random
graphs — have high average degree ⟨k⟩ > 6. Consequences:

- **Dense mode spectrum:** Many eigenvalues cluster near the maximum, producing
  a quasi-continuous spectrum. This obscures the discrete mode structure that
  defines the bridge band (I2: Ω ∈ [1.16, 1.19]).
- **Shortcut proliferation:** Higher-dimensional lattices necessarily contain
  many more paths between distant nodes. Even with local coupling, the effective
  propagation speed may exceed c_TRM = 1.
- **Synchronisation frustration:** High connectivity can induce competing
  coupling phases that prevent stable phase-locking — analogous to spin-glass
  frustration in densely connected networks.

**Prediction:** D > 3 graphs may synchronise but with a mode spectrum that does
not support the bridge-band structure required by V4.

### 3.3 D = 3: Balanced Locality and Isotropy

The cubic lattice (D = 3) occupies a structural sweet spot:

- **Degree** ⟨k⟩ = 6: enough connectivity for robust synchronisation, not so
  much that the mode spectrum becomes dense.
- **Isotropy:** The cubic lattice is isotropic at large scales (continuum limit
  recovers Euclidean ∇²).
- **Propagation:** c_TRM = 1 is achievable without shortcuts — one edge per
  cycle, uniform in all directions.
- **Bridge band:** The discrete mode structure supports a well-defined
  collective frequency Ω* with a narrow bridge band — exactly the conditions
  under which I2 (Ω ∈ [1.16, 1.19]) was observed in CML simulations.

**Prediction:** D = 3 is the unique dimension in which stable, isotropic
synchronisation with a well-defined bridge band is possible.

---

## 4. Mathematical Indicators

The following quantities provide diagnostics for testing the hypothesis.
These are proposed as **non-committal indicators** — no claim is made that
any one of them is sufficient.

| Indicator | Definition | D = 3 expectation |
|:---|---|:---|
| **Spectral gap** | λ₂ = second eigenvalue of Δ_G | Moderate; large enough for fast sync, small enough for discrete mode structure |
| **Sync basin size** | Fraction of random initial conditions reaching phase-lock | Maximal at D = 3 |
| **Perturbation decay rate** | Lyapunov exponent of phase deviations | Fastest return to sync after local perturbation |
| **Isotropy of propagation modes** | Angular variance of front speed | Minimal at D = 3 |
| **Bridge-band width** | ΔΩ = spread of Ω* across random realisations | Narrowest at D = 3 (matches I2) |

These indicators can be evaluated in CML (Coupled Map Lattice) simulations
with controlled graph topology. The expectation is that D = 3 graphs maximise
sync basin size and minimise propagation anisotropy — producing a sharper
bridge band than D ≠ 3 graphs.

---

## 5. Falsifiable Outcomes

The hypothesis is falsified if:

| Observation | Conclusion |
|:---|---|
| Stable synchronisation occurs generically for D ≠ 3 graphs | Mechanism fails; D = 3 is not dynamically selected |
| D = 3 graphs do not outperform D ≠ 3 on any stability indicator | No evidence for dimensional selection |
| A D ≠ 3 graph produces a narrower bridge band than D = 3 | I2 does not constrain dimension |
| Propagation isotropy is achievable in D = 2 or D = 4 graphs | Isotropy does not favour D = 3 |

Conversely, the hypothesis gains support if:

| Observation | Conclusion |
|:---|---|
| Only D ≈ 3 graphs satisfy all five criteria of §3.8.6 simultaneously | D = 3 is the unique viable dimension |
| Sync basin size peaks sharply at D = 3 | Dynamical selection mechanism identified |
| Bridge-band width is minimised at D = 3 | I2 constrains spatial dimension |

---

## 6. Claim Classification

| Claim | Classification | Justification |
|:---|---|:---|
| D = 3 selected by synchronisation stability | **HYPOTHESIS** | Plausible mechanism; no simulation or proof exists |
| Stability indicators as selection diagnostics | **FRAMEWORK** | Defines testable quantities; does not assert outcomes |
| Future derivation from oscillator dynamics | **DERIVABLE CANDIDATE** | In principle computable via CML; formal proof outstanding |
| I2 bridge band constrains spatial dimension | **HYPOTHESIS** | I2 is observed in 3D CML; D-dependence untested |

---

## 7. Scope Boundary

This document does **NOT**:

- Modify the V3.4 core (oscillator dynamics, I1, I2, D1).
- Modify V4 interpretation results (C5 mapping, B1–B6, G1–G6).
- Claim that D = 3 is derived from oscillator dynamics.
- Assert that any specific indicator proves dimensional selection.

This is a **research program definition** — it identifies a falsifiable
hypothesis, proposes diagnostic indicators, and defines success/failure
conditions. No simulation results are reported.

---

## 8. Relation to V4.1 Structure

| Document | Relationship |
|:---|:---|
| `TRM_V4_1_Emergent_Space.md` §3.8 | Identifies D = 3 gap; defines structural requirements |
| `TRM_V4_1_Emergent_Space.md` §3.8.6 | Lists five minimal criteria; Criterion (4) = sync stability |
| **This document** | Develops Criterion (4) into a testable research program |

---

## 9. Next Steps

1. **CML parameter scan:** Run synchronisation simulations on graphs with
   controlled dimension D ∈ {1, 2, 3, 4} and measure the five indicators.
2. **Bridge-band dimension audit:** Test whether I2 (Ω ∈ [1.16, 1.19])
   persists for D ≠ 3 graphs or collapses outside D = 3.
3. **Analytical bound:** Derive a lower bound on the spectral gap λ₂ as a
   function of D for regular lattice graphs.
4. **Frustration analysis:** Compute coupling frustration energy for D ≠ 3
   graphs; test whether D = 3 is the unique minimum.
