# TRM V4 — B3B: Boundary / Defect Origin Study

**Date:** 2026-07-05
**Status:** Investigating whether mass can be modeled as a coupling defect producing 1/r imprint
**Predecessors:** B3A (Laplace uniqueness proven), B3 (candidate catalog)
**Key question:** Why does K(r) → K₀ + α·M/r at a mass source?

---

## 1. Objective

B3A proved that ∇²K = 0 is the unique admissible PDE. The remaining question is **origin**: why does the coupling field obey Laplace behavior with a 1/r boundary condition at mass concentrations?

This study investigates whether **mass as a discrete coupling defect** in the oscillator network can naturally produce:
- Harmonic bulk behavior: ∇²K = 0 (vacuum)
- 1/r far-field imprint: δK(r) ~ 1/r at large distances from the defect

---

## 2. The Discrete-to-Continuum Bridge

### 2.1 Discrete Coupling Laplacian

In the oscillator network, the coupling matrix K_ij defines adjacency. The discrete Laplacian at site i is:

```
(∇²_disk K)_i = Σ_j (K_ij − K_ii·δ_ij)     (unnormalized)
```

or more precisely, the graph Laplacian:

```
(L K)_i = Σ_j K_ij · (K_j − K_i)             (weighted graph Laplacian)
```

In the continuum limit (lattice spacing → 0, N → ∞), this converges to:

```
(L K)(x) → ∇²K(x)                            (standard Laplacian)
```

**This is a standard result in discrete differential geometry and lattice field theory.** The graph Laplacian on a regular lattice converges to the continuum Laplacian.

### 2.2 Green's Function of the Discrete Laplacian

For a point defect at site 0 (a single perturbed coupling), the discrete response solves:

```
(L δK)_i = δ_{i0} · ΔK_defect               (unit source at origin)
```

In 3D on a regular lattice with spacing a, the Green's function at large distances is:

```
δK_i ∝ 1 / |r_i|          for |r_i| ≫ a
```

**This is the discrete origin of the 1/r boundary condition.** A point defect in the coupling network naturally produces a 1/r far-field perturbation — not because we assume it, but because the discrete Laplacian's Green's function is ~1/r in 3D.

---

## 3. Candidate Defect Mechanisms

### 3.1 Candidate A — Local Coupling Defect at a Site Cluster

**Mechanism:**

A "mass site" is a node (or cluster of nodes) where the coupling is perturbed:

```
K_ij = K₀ + δK_defect     for i, j in defect cluster
K_ij = K₀                  elsewhere
```

**Continuum limit:**

The defect appears as a singular source in the discrete Laplacian. The response is the Green's function:

```
δK(r) ∝ 1/r     at large r
```

**Why this works:**
- The discrete Laplacian converges to ∇² in the continuum
- The Green's function of the discrete Laplacian in 3D is ~1/r
- No new physics — uses only existing K_ij structure
- The bulk remains harmonic: ∇²K = 0 away from the defect

**TRM expression:**

The coupling defect δK_defect is a modification of the coupling matrix entries K_ij in a localized region. This is expressible purely in oscillator terms — K_ij is a native TRM quantity.

**Prediction:**

If a single oscillator in the CML has its coupling modified (K_local > K_global), the collective frequency Ω* at distance r should show a 1/r perturbation. This is **directly testable** with the existing CML simulation infrastructure — a spatially varying coupling test.

**Classification: PROMISING.** ⭐

This is the most natural mechanism. No new physics. Uses only K_ij. The 1/r emerges from the discrete Laplacian's Green's function, not from an assumed PDE.

---

### 3.2 Candidate B — Topological Defect in Adjacency Structure

**Mechanism:**

A "mass site" changes the adjacency graph itself — additional links, missing links, or modified connection topology:

```
A_ij = 1     →    A_ij = 1 + δA_defect     (adjacency matrix perturbation)
K_ij = K₀·A_ij
```

**Continuum limit:**

Similar to Candidate A — the topological defect is a specific type of coupling defect. The response is still a 1/r Green's function because the perturbation enters through the same discrete Laplacian.

**Why this is more complicated than A:**

| Aspect | Candidate A | Candidate B |
|:---|:---|:---|
| Perturbation type | Continuous (δK) | Discrete (link present/absent) |
| Recovery of 1/r | Standard Green's function | Requires many defect links for smooth 1/r |
| Single-link defect | Produces 1/r (asymptotically) | Produces 1/r with angular structure (anisotropic) |
| TRM expression | K_ij modification | Adjacency matrix modification |

A single missing link produces an anisotropic perturbation (depends on direction), not a clean isotropic 1/r. To get isotropy, the defect must span multiple links in all directions — essentially a cluster defect, reducing to Candidate A.

**Classification: PARTIALLY SUPPORTED.** Reduces to Candidate A for isotropic defects. Single-link defects are anisotropic.

---

### 3.3 Candidate C — Fixed Boundary Phase Defect

**Mechanism:**

A "mass site" has its phase θ pinned to a fixed value, creating a phase gradient that imprints on K through the dynamics:

```
θ_defect = θ_fixed     (pinned phase)
dθ_i/dt = ω_i + Σ K_ij·f(θ_i − θ_j)     (other oscillators free)
```

The phase gradient around the pinned site affects the local synchronization, which could modulate the effective coupling.

**Why this fails:**

| Problem | Detail |
|:---|:---|
| K is a parameter, not a dynamical variable | The coupling K_ij is fixed in the oscillator model — it doesn't evolve with θ |
| Phase affects coupling only indirectly | There's no θ → K coupling in the standard Kuramoto model |
| Requires new physics | A θ → K feedback mechanism would be new physics beyond V3.4 core |
| 1/r not guaranteed | Even with θ → K coupling, the phase profile around a pinned site is not obviously 1/r |

**Classification: NOT SUPPORTED.** Requires K to be a dynamical variable coupled to θ, which is not present in the V3.4 oscillator model.

---

### 3.4 Candidate D — Coarse-Grained Defect Source

**Mechanism:**

This is not a separate mechanism — it's the **mathematical formalization** of how Candidates A and B produce a continuous PDE:

```
Discrete coupling defect at sites {i₀}
    ↓ coarse-graining (lattice spacing → 0)
Singular source in continuum PDE
    ↓ Green's function
δK(r) ~ 1/r
```

**What D adds:** The rigorous demonstration that the discrete-to-continuum limit of the graph Laplacian with a point defect converges to ∇²K = δ(x) · ΔK_defect, whose solution is K(r) = K₀ + const/r.

**Classification: MATHEMATICAL FRAMEWORK (not a separate mechanism).** D provides the rigorous bridge between Candidate A and the Laplace equation.

---

## 4. The Winning Candidate: A + D

### Complete Mechanism (Candidate A formalized through D)

```
Step 1: A mass concentration is a localized region where oscillator
        coupling K_ij is perturbed: K_ij = K₀ + δK_defect.

Step 2: The discrete Laplacian L_disk, applied to K, has a singular
        source at the defect site.

Step 3: In the continuum limit (N → ∞, lattice spacing → 0),
        L_disk → ∇², and the singular source becomes a delta function.

Step 4: The PDE is ∇²K = −4π·α·δ³(r) in the sourced formulation,
        or equivalently ∇²K = 0 with K → K₀ + α/r boundary condition.

Step 5: The solution is δK(r) = α/r → a(r) ∝ 1/r² → Newtonian gravity.
```

### Why This Works Without Assuming Newton

| Element | Source |
|:---|:---|
| K_ij perturbation | Native TRM quantity — coupling matrix |
| Discrete Laplacian | Graph Laplacian on the oscillator network |
| 1/r Green's function | Mathematical property of 3D discrete Laplacian — **no physics input needed** |
| Continuum limit | Standard coarse-graining — lattice spacing → 0 |
| ∇²K = 0 in bulk | Consequence of discrete Laplacian with localized defect |

**At no step is Newtonian gravity or G used as input.** The 1/r emerges from the mathematics of the discrete Laplacian, not from assuming the answer.

---

## 5. What B3B Establishes

### Proven (within the defect model)

- A localized coupling defect in the discrete oscillator network produces δK ~ 1/r at large distances
- The bulk field obeys ∇²K = 0 (harmonic, no sources)
- The mechanism uses only TRM-native quantities (K_ij)

### NOT proven (remaining gaps)

- That α (the defect strength) is **predicted** from TRM parameters — it's still a free parameter describing "how much" the coupling is perturbed
- That a "mass concentration" in physical reality corresponds to a coupling defect — the mapping from physical mass M to defect strength δK_defect is not derived
- That the continuum limit is valid for finite N — the CML operates at N ~ 10–25, not N → ∞

### The remaining calibration

Even with B3B, the coupling constant α must be empirically determined:

```
α = δK_defect · (cell volume)     in the discrete → continuum mapping
```

This relates to the physical mass M through:

```
α ∝ M           (defect strength proportional to mass)
```

The proportionality constant is k (B1 calibration: k = G·K₀/c²). **B3B explains the 1/r form, not the coefficient.**

---

## 6. Testability

### Direct CML Test

**B3B-T1 — Single-Site Coupling Defect**

```
Setup:
  - Ring of N = 20 oscillators with baseline coupling K₀
  - One oscillator i₀ has modified coupling: K_i₀j = K₀ + δK for all j
  - Measure emergent Ω*(r) at varying distances r from defect site
  - Fit Ω*(r) − Ω*_baseline ∝ 1/r

Prediction: δΩ*(r) ∝ δK/r at large r (in the synchronized state)
```

This is implementable with the existing `SimulateModeLockCore` using `spatialBiasFunc` or custom coupling matrices. It would directly test whether a coupling defect produces 1/r behavior in the TRM simulation.

---

## 7. Classification Summary

| Candidate | Mechanism | 1/r emerges? | Uses only TRM terms? | Classification |
|:---|:---|:---|:---|:---|
| **A — Coupling defect** | K_ij perturbation at a site | ✓ (discrete Laplacian Green's function) | ✓ (K_ij is native) | **PROMISING** ⭐ |
| **B — Topological defect** | Adjacency matrix perturbation | ✓ (reduces to A for clusters) | ✓ (A_ij is native) | **PARTIALLY SUPPORTED** |
| **C — Phase boundary** | Pinned phase → K imprint | ✗ (no θ → K coupling) | ✗ (requires new dynamics) | **NOT SUPPORTED** |
| **D — Coarse-grained** | Formal continuum limit of A/B | ✓ (mathematical framework) | ✓ | **MATHEMATICAL BRIDGE** |

---

## 8. Impact on B3 Closure

| Before B3B | After B3B |
|:---|:---|
| "Why 1/r? Unknown. Assumed." | "1/r emerges from discrete Laplacian Green's function when mass is a coupling defect." |
| "The PDE is assumed." | "The PDE is the continuum limit of the discrete graph Laplacian." |
| "The boundary condition is imposed." | "The boundary condition is the far-field imprint of a localized defect." |

### Is B3 closer to closure?

**YES — significantly.** B3B provides the missing mechanistic link: mass as a coupling defect → discrete Laplacian → 1/r Green's function → continuum ∇²K = 0.

### Is I3 still needed?

**PARTIALLY.** The 1/r form is now explained. But the coefficient α (coupling defect strength → physical mass) is still a calibration, not a prediction. The mapping M ↔ δK_defect requires one empirical constant.

**Revised status:** B3 is now at the level of **"mechanism explained, coefficient calibrated"** — analogous to how Newtonian gravity explains the 1/r² form but G is measured, not predicted.

---

## 9. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_CouplingFieldEquation.md` | B3 candidate catalog (A–D) |
| `TRM_V4_B3_ApproachDirections.md` | Physical viability audit |
| `TRM_V4_B3A_LaplaceUniqueness.md` | Uniqueness proof (Laplace is the only PDE) |
| This document | Origin of the 1/r boundary condition via discrete coupling defects |
