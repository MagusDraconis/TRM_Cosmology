# TRM V4 — G2: Tensor Bridge Investigation

**Date:** 2026-07-05
**Status:** Investigating whether the discrete K_ij can produce an emergent rank-2 tensor
**Context:** Scalar K(x) has 1 DOF — insufficient for GR (needs 2 graviton polarizations, 6 physical metric DOF). K_ij is a two-point object — potentially rich enough.

---

## 1. The Degree-of-Freedom Problem

| Object | DOF (3+1D) | Sufficient for GR? |
|:---|:---|:---|
| Scalar K(x) | 1 | ✗ (GR needs 2 polarizations, 6 physical metric DOF) |
| Vector K_μ(x) | 3 | ✗ (Proca/vector gravity — no bending of light) |
| Symmetric tensor K_μν(x) | 10 − 4 gauge = 6 | ✓ (matches GR) |
| Two-point function K(x,y) | ∞ (function of 8 variables) | ✓ (overcomplete — metric is a projection) |
| Discrete matrix K_ij (N×N) | N(N−1)/2 independent entries | → ∞ in continuum limit |

**Key insight:** K_ij is NOT a scalar field — it's a **two-point object**. The continuum limit K(x,y) has enough degrees of freedom to encode a full metric. The question is: what is the natural projection K(x,y) → g_μν(x)?

---

## 2. Candidate Mappings

### Candidate A — Metric from Scalar Gradients

```
g_μν(x) = A(φ)·η_μν + B(φ)·∂_μφ·∂_νφ
```

**DOF:** 1 (scalar φ determines everything)

**What it can describe:**
- Isotropic gravity (Schwarzschild-like) ✓
- Gravitational redshift ✓
- Light deflection ✓
- One GW polarization (scalar — breathing mode) ✓
- Two GW polarizations (h_+, h_×) ✗
- Kerr metric (rotating source) ✗

**Classification:** Scalar-tensor theory (Brans-Dicke class). **INSUFFICIENT for GR replacement.**

---

### Candidate B — Metric from Local Coupling Anisotropy

**Discrete picture:** K_ij depends on the direction of the link i→j. If K is anisotropic, different directions have different coupling strengths.

**Continuum picture:** For a unit direction vector v^μ:
```
g_μν(x) · v^μ · v^ν = f(K(x, v))
```
where K(x, v) is the coupling strength in direction v.

The metric is extracted by probing K in all directions:
```
g_μν(x) = ∂²[f(K(x, v))] / ∂v^μ ∂v^ν  |_{v=0}
```

**DOF:** K(x, v) depends on 3 direction angles → effectively 3 scalar functions on spacetime. After spherical harmonic decomposition: 1 monopole (trace) + 3 dipole (vector) + 5 quadrupole (tensor) = up to 9 DOF.

**What it can describe:**
- Anisotropic gravitational field ✓
- Preferred-frame effects (if anisotropy is absolute) ✓
- Two GW polarizations ✓ (quadrupole modes)
- Kerr-like solutions ✓ (anisotropy from rotation)
- Full GR equivalence ✗ (depends on field equations, not just DOF count)

**Advantage over Candidate A:** The coupling anisotropy is naturally present in ANY non-trivial oscillator network. If oscillators are arranged in space, the coupling depends on direction.

**Classification:** PROMISING. The DOF count is sufficient. The question is whether the dynamics of K(x, v) produce the correct field equations.

---

### Candidate C — Metric from Two-Point Function

**Discrete picture:** K_ij is the coupling strength between oscillator i and oscillator j. It's a function of TWO positions, not one.

**Continuum picture:** K(x,y) is the coupling two-point function. The metric distance between x and y is encoded in how strongly they are coupled:

```
K(x,y) = K₀ · exp(−σ(x,y) / λ²)     (Gaussian coupling)
```
where σ(x,y) = ½·(geodesic distance)² is the Synge world function.

The metric is extracted from the second derivatives at coincidence:
```
g_μν(x) = −(2/λ²) · [∂_μ ∂_ν K(x,y) / K(x,x)]_{y=x}
```

Equivalently, from the coincidence limit of the two-point function's Hessian:
```
g_μν(x) ∝ −∂_μ ∂_ν K(x,y) |_{y=x}
```

**DOF:** K(x,y) has N(N−1)/2 ≈ ∞ independent entries in the continuum → fully overcomplete for encoding a metric. The metric is a projection (6 DOF from ∞).

**What it can describe:**
- Everything a metric can describe ✓ (the metric IS a projection of K)
- Two GW polarizations ✓ (encoded in transverse-traceless part of g_μν)
- Kerr metric ✓ (off-diagonal terms from asymmetric coupling)
- Full GR equivalence ✓ (DOF count matches; field equations are the constraint)

**Classification:** THEORETICALLY COMPLETE. K(x,y) → g_μν(x) is mathematically well-defined. The open question is deriving the Einstein equations from the dynamics of K(x,y).

---

### Candidate D — Multi-Field Generalization

**Discrete picture:** The coupling matrix K_ij is not a single number per link — it could have internal structure. Decompose K_ij into irreducible representations:

```
K_ij = K^(0)_ij  (scalar trace — isotropic part)
     + K^(1)_ij  (vector — antisymmetric part)
     + K^(2)_ij  (tensor — symmetric traceless part)
```

**Continuum picture:** Each component becomes a separate field:
- K^(0) → scalar field φ(x) (Newtonian potential)
- K^(1) → vector field A_μ(x) (gravitomagnetism / frame-dragging)
- K^(2) → tensor field h_μν(x) (gravitational waves)

**DOF:** 1 + 3 + 5 = 9 (more than GR's 6). Gauge freedom reduces this.

**Classification:** STRUCTURALLY RICH. The decomposition mirrors GR's decomposition of the metric into Newtonian, gravitomagnetic, and radiative components. This is the most direct path to GR.

---

## 3. Comparison

| Candidate | DOF | GW polarizations | Kerr metric | Naturalness for TRM | Status |
|:---|:---|:---|:---|:---|:---|
| **A — Scalar gradients** | 1 | ✗ (breathing only) | ✗ | Simple but insufficient | INSUFFICIENT |
| **B — Coupling anisotropy** | ~9 | ✓ (quadrupole) | ✓ | Natural (K depends on direction) | PROMISING |
| **C — Two-point function** | ∞ → 6 | ✓ | ✓ | Very natural (K IS a two-point object) | THEORETICALLY COMPLETE |
| **D — Multi-field** | 1+3+5=9 | ✓ | ✓ | Systematic (irreducible decomposition) | STRUCTURALLY RICH |

---

## 4. The Most Natural Path: Candidate C (Two-Point Function)

### 4.1 Why C is the natural choice

K_ij is a two-point object by definition. In the oscillator model:
```
dθ_i/dt = ω_i + Σ_j K_ij · sin(θ_j − θ_i)
```

K_ij lives on pairs (i,j). The continuum limit K(x,y) is the most faithful representation of the discrete coupling.

### 4.2 Metric Extraction

```
K(x,y) = K₀ · exp(−d²(x,y) / 2λ²)      (Gaussian ansatz, motivated by locality)
```

The squared distance d²(x,y) = g_μν(x) · Δx^μ · Δx^ν + ... (Riemann normal coordinates).

Taking −∂_μ∂_ν at coincidence:
```
−∂_μ ∂_ν K(x,y) |_{y=x} = (K₀/λ²) · g_μν(x)
```

So:
```
g_μν(x) = −(λ²/K₀) · ∂_μ ∂_ν K(x,y) |_{y=x}
```

This is a clean, parameter-free extraction. If K(x,y) is known, g_μν(x) follows.

### 4.3 The Field Equation Problem

The metric g_μν is extracted from K. The dynamics of K must produce g_μν that satisfies the Einstein equations:

```
G_μν[g] = 8πG · T_μν
```

This requires a PDE for K(x,y) whose solutions, when projected to g_μν via the extraction formula, satisfy the Einstein equations. This is a **hard inverse problem** — given the desired g_μν, find the PDE for K that produces it.

### 4.4 A Conjecture

```
If K(x,y) satisfies:  □_x K(x,y) − (m²/λ²)·K(x,y)·ln K(x,y) = −4π·α·ρ(x)·δ(x−y)
where □_x acts on the first argument and the coincidence limit y→x is taken,
then g_μν ∝ −∂_μ∂_ν K|_{y=x} satisfies the linearized Einstein equations.
```

This is speculative. But it illustrates the program: postulate a PDE for K, extract g_μν, check against GR.

---

## 5. Viability Classification

| Question | Answer |
|:---|:---|
| Can K_ij produce a rank-2 tensor? | **Yes** — K_ij IS a two-point object, naturally encoding a metric. |
| Are there enough DOF for full GR? | **Yes** — K(x,y) has ∞ DOF; g_μν has 6; the projection is valid. |
| Is the metric extraction natural? | **Yes** — g_μν ∝ −∂_μ∂_ν K\|_{y=x} from Gaussian coupling ansatz. |
| Can the Einstein equations be derived? | **OPEN** — this is the hard core of G2. Requires finding the PDE for K that projects to G_μν = 8πG·T_μν. |
| Is C more promising than B or D? | **Yes** — C is the continuum limit of the NATIVE discrete object K_ij. B and D are intermediate decompositions. |

---

## 6. Recommendation

### Primary path: Candidate C (two-point function)

1. Formalize: K_ij → K(x,y) continuum limit with metric extraction g_μν ∝ −∂_μ∂_ν K\|_{y=x}
2. Test in 1D/2D: verify that a simple coupling function produces the correct metric for known spacetimes
3. Derive the linearized Einstein equations from a simple PDE for K
4. Generalize to nonlinear

### Honest assessment

G2 is a **multi-year research program**, not a documentation task. The conceptual bridge (K_ij → K(x,y) → g_μν) is clear. The dynamical bridge (PDE for K → Einstein equations for g_μν) is the hard open problem.

**TRM has a structural advantage:** K_ij is already a two-point object. Most modified gravity theories start from a scalar or vector and struggle to get tensor structure. TRM starts from something richer.

---

## 7. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_GR_Replacement_Roadmap.md` | Full gap analysis (G1–G6) |
| `TRM_V4_B3B_BoundaryDefectOrigin.md` | K_ij → coupling defect → 1/r |
| `TRM_V4_Final_Status.md` | Current V4 closure |
| This document | G2 tensor bridge investigation |
