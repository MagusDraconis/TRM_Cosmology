# TRM V4.1 — Emergent Space and Causal Structure

**Date:** 2026-07-07
**Status:** EXPLORATORY — post-V4 extension program
**Branch:** `feature/v4.1-emergent-space`
**Predecessors:** V3.4 (frozen core), V4 (interpretation layer, frozen)

> V4.1 extends the interpretation layer toward spacetime emergence.
> V4 remains frozen — no modifications to the V3.4 core or V4 claims.

---

## 1. Problem Statement

V4 provides time from oscillator dynamics: the collective frequency Ω* defines a local clock rate,
anchored to the cesium SI second via I3 (f_ref = 9.192631770×10⁹ Hz).

What V4 does **not** provide is spatial structure:

- The coupling matrix K_ij connects oscillators, but the physical distance between them (Δx)
  is treated as an external embedding — either assumed (c_K = c) or introduced as I4 (Δx).
- The bilocal kernel K(x,y) depends on the squared geodesic distance d²(x,y), which is
  presupposed, not emergent.
- The wave equation □K = 0 has propagation speed c_K, currently **ASSUMED** (c_K = c).

**Open question:** Can space and causal propagation speed be expressed in TRM-native terms?

---

## 2. Core Hypothesis

> Space emerges as a relational structure of oscillator couplings.
> Distance is defined as graph distance on the coupling network.

### 2.1 Graph Distance

Define the distance between oscillators i and j as the minimal number of coupling steps:

```
d(i, j) = min number of edges on any path i → ... → j
```

This is purely combinatorial — no metric, no embedding, no coordinates.
It depends only on the coupling topology K_ij.

### 2.2 Spatial Dimension

The effective spatial dimension emerges from the scaling of graph volume with graph radius:

```
N(r) ∝ r^D    →    D = effective spatial dimension
```

For a cubic lattice graph, D = 3. For a random graph, D is determined by connectivity.
In TRM, D = 3 is **ASSUMED** (matches observed 3+1 spacetime), but the framework allows
D ≠ 3 as a testable extension.

---

## 3. Causal Propagation

### 3.1 Maximal Propagation Rate

In a discrete oscillator network, information propagates via coupling — one oscillator
perturbs its neighbors, which perturb theirs, and so on. The fastest possible propagation
is:

```
one lattice step per oscillator cycle
```

In TRM-native units:

```
c_TRM = 1    (dimensionless)
```

One coupling edge per oscillation period. This is the causal bound of the network —
no signal can propagate faster, because coupling is the only propagation mechanism.

### 3.2 Propagation Speed in Physical Units

The physical propagation speed is obtained by converting TRM-native units to SI:

```
c = Δx · f_ref · c_TRM
  = Δx · f_ref · 1
  = Δx · f_ref
```

This recovers Candidate B from B4-T1:

```
c_K = K₀ · Δx · f_ref
```

where K₀ is set to 1 (normalised coupling strength) for the causal bound.

### 3.3 Time-Distance Equivalence

If propagation is causal and bounded by c_TRM = 1, then space can be defined
from time alone. The key insight:

> **Spatial distance = minimal propagation time between nodes.**

**Propagation time.** Define the propagation time T(i, j) as the minimum number
of oscillator cycles required for a signal to travel from node i to node j.
Since each step traverses exactly one edge per cycle:

```
T(i, j) = d(i, j)
```

where d(i, j) is the graph distance (number of edges on the shortest path).

**Spatial distance.** The physical spatial distance is then:

```
d_space(i, j) = c_TRM · T(i, j) = T(i, j)
```

In TRM-native units (c_TRM = 1), spatial distance and propagation time are
numerically identical. Distance is measured in cycles — no pre-existing metric
or coordinate system is required. Geometry emerges from causal connectivity:
two nodes are close if few propagation cycles separate them; distant if many
cycles are needed.

**Continuum limit.** For a regular lattice graph, the graph distance d(i, j)
converges to Euclidean distance in the large-scale limit:

```
d_space(r) → |r|    as    r ≫ Δx
```

This recovers the familiar 3D spatial metric from the discrete causal structure.
No metric tensor is presupposed — the metric emerges from the causal graph.

**Classification:**

| Claim | Classification |
|:---|---|
| T(i, j) = d(i, j) | **DERIVABLE CANDIDATE** — follows from c_TRM ≤ 1 and discrete propagation |
| d_space = minimal propagation time | **FRAMEWORK** — definitional; consistent with causal set theory |
| Spatial metric from causal graph | **HYPOTHESIS** — requires continuum-limit proof and Lorentz-invariance verification |

### 3.4 Connection to Speed of Light

If the causal bound of the oscillator network is identified with the speed of light:

```
c = Δx · f_ref    →    Δx = c / f_ref ≈ 0.033 m
```

This gives Δx ≈ 3.3 cm (with c_TRM = 1) — an order of magnitude smaller than the
B4 Candidate A prediction (Δx ≈ 0.33 m) because K₀ ≈ 0.10 is factored out differently.

**Important:** This identification (network causal bound = speed of light) is a
**HYPOTHESIS**, not a derivation. It assumes that the oscillator network's coupling
propagation is the fundamental speed limit of spacetime — a claim that requires
independent verification.

### 3.5 Causal Bound Argument

The claim that the maximal propagation speed in a discrete oscillator network is
one graph step per cycle rests on the following structural facts:

**1. Locality of coupling.** Each oscillator is coupled only to its immediate
neighbours in the graph. Influence propagates exclusively via coupling edges —
there are no long-range direct connections. A perturbation at node A can affect
node B only through intermediate nodes along some path A → … → B.

**2. Discrete-time dynamics.** The oscillator phase θ_i evolves in discrete steps
with period τ = 1/f_ref. During one cycle, an oscillator responds to the state of
its neighbours at the beginning of that cycle. Information arriving from beyond
the immediate neighbourhood is delayed by at least one cycle per intermediate node.

**3. No edge-skipping.** Since oscillators do not have access to non-local oscillator
states, a signal cannot skip edges. The shortest path between two nodes defines
the minimum number of cycles required for information to propagate between them.

**4. Maximal rate.** From (1)–(3), the propagation distance per cycle is bounded:

```
Δd_max = 1 edge per cycle
```

In TRM-native units (cycle = time unit, edge = length unit):

```
c_TRM ≤ 1    (causal bound)
```

The equality c_TRM = 1 is achieved when propagation is unfrustrated — i.e., when
coupling is nearest-neighbour and symmetric, and no delays are introduced by
phase mismatch or coupling inhomogeneity.

**5. Wave propagation.** In the continuum limit of a regular lattice, this bound
corresponds to linear dispersion:

```
ω = c_TRM · k    (with c_TRM = 1)
```

The massless wave equation □K = 0 follows as the long-wavelength limit of the
discrete propagation described above. The propagation speed c_K that appears in
□K = 0 is therefore identified with the causal bound c_TRM = 1.

**6. SI conversion.** The physical value of the propagation speed is obtained by
multiplying by the spatial and temporal unit scales:

```
c = Δx · f_ref · c_TRM = Δx · f_ref
```

The dimensionless bound c_TRM = 1 is a structural consequence of discrete
propagation. The SI value of c is **CALIBRATED** from the spatial anchor Δx and
the temporal anchor f_ref.

**Classification of c_TRM = 1: DERIVABLE CANDIDATE.** The argument above is
structural — it follows from the assumptions of local coupling and discrete-time
dynamics. A formal proof requires:
- Rigorous demonstration that no super-causal propagation protocol exists in
  the oscillator network model.
- Verification that the network's causal structure is consistent with Lorentz
  invariance in the continuum limit.
- Confirmation that K₀ ≤ 1 (coupling strength does not exceed the causal bound).

### 3.6 Metric Properties

The causal distance d(i, j) satisfies the axioms of a discrete metric:

**Symmetry.** Graph distance is symmetric by construction — the shortest path
from i to j has the same length as the shortest path from j to i:

```
d(i, j) = d(j, i)
```

This follows from the undirected nature of the coupling graph (K_ij = K_ji).

**Triangle inequality.** For any three nodes i, j, k, the shortest path from i
to k cannot be longer than the shortest path from i to j plus the shortest path
from j to k:

```
d(i, k) ≤ d(i, j) + d(j, k)
```

This holds because concatenating the shortest path i → j with the shortest path
j → k produces a valid (though not necessarily shortest) path i → k.

**Positive definiteness.** d(i, j) ≥ 0, with d(i, j) = 0 if and only if i = j.

**Discrete metric.** The pair (V, d) — where V is the set of oscillator nodes and
d is the graph distance — forms a discrete metric space. No continuum embedding
is required for this structure to be well-defined.

**Classification of discrete metric: DERIVABLE CANDIDATE.** The metric axioms
follow directly from the graph-theoretic definition of distance and the symmetry
of the coupling matrix. The result is structural — no additional assumptions beyond
the existence of a connected, undirected coupling graph.

### 3.7 Continuum Limit

The discrete graph distance approximates Euclidean distance in the large-scale limit
for regular lattice graphs.

**Scaling.** Introduce a physical length scale Δx (the spatial embedding parameter
or graph edge length). The physical distance corresponding to graph distance d is:

```
r = d · Δx
```

**Convergence.** For a regular cubic lattice in D = 3 dimensions, the graph distance
between nodes at lattice coordinates n_i and n_j converges to the Euclidean norm:

```
d(i, j) · Δx → |x_i - x_j|    as    d(i, j) → ∞
```

where x_i = Δx · n_i are the continuum coordinates.

**Conditions.** The convergence requires:
- Regular lattice structure (uniform spacing, uniform connectivity)
- Large separation (d ≫ 1, so lattice discretisation effects are negligible)
- D = 3 (matches observed spatial dimensionality)

**Deviations.** At small scales (d ~ O(1)), the discrete graph distance deviates
from the Euclidean metric. These deviations are a falsifiable prediction: the
graph-Laplacian Green's function K(r) ~ 1/r is exact only in the continuum limit;
at lattice scale, corrections of order Δx/r appear.

**Classification of continuum limit: HYPOTHESIS.** The convergence of graph
distance to Euclidean distance for regular lattices is a standard result in graph
theory. However, the identification of this mathematical convergence with the
physical emergence of spacetime geometry is a hypothesis requiring:
- Derivation of D = 3 from coupling topology (not assumed)
- Verification that the emergent metric satisfies the Einstein equations in the
  appropriate limit
- Consistency with Lorentz invariance

---

## 4. From Causal Metric to Spatial Operators

The causal metric d(i, j) defined in §3 provides the foundation. This section shows
how standard continuum spatial operators — specifically the Laplacian ∇² — emerge
from the discrete adjacency structure without presupposing a background manifold.

### 4.1 Discrete Neighborhood Structure

The oscillator graph G = (V, E) consists of nodes V (oscillators) and edges E
(couplings K_ij > 0). The graph distance d(i, j) induces a natural notion of locality:

```
N(i) = { j ∈ V : d(i, j) = 1 }
```

The set N(i) is the immediate neighbourhood of node i — all oscillators directly
coupled to it. Iterating neighbourhoods generates concentric shells at distances
d = 1, 2, 3, …, defining the discrete analogue of spherical surfaces.

### 4.2 Graph Laplacian

From the adjacency structure, define the discrete graph Laplacian acting on a
node-valued field φ: V → ℝ:

```
(Δ_φ φ)(i) = Σ_{j ∈ N(i)} (φ(j) − φ(i))
```

In matrix form: Δ_G = D − A, where A_ij = K_ij is the adjacency (coupling) matrix
and D_ii = Σ_j A_ij is the degree matrix.

This operator is the discrete analogue of the continuum Laplacian. It measures the
excess of a field value at a node relative to its neighbours — the discrete
divergence of the discrete gradient.

### 4.3 Continuum Limit

For a regular cubic lattice with spacing Δx in D = 3 dimensions, the discrete
Laplacian converges to the Euclidean Laplacian in the limit Δx → 0:

```
Σ_{j ∈ N(i)} (φ(j) − φ(i)) → Δx² · ∇²φ(x)
```

More precisely, for a smooth function φ sampled at lattice points:

```
(Δ_G φ)(i) / Δx² → ∇²φ(x_i)    as    Δx → 0
```

The convergence follows from the Taylor expansion of φ around x_i:

```
φ(x_i + Δx e_μ) = φ(x_i) + Δx ∂_μφ + ½Δx² ∂_μ²φ + O(Δx³)
```

Summing over the 2D nearest neighbours cancels the first-order terms (by symmetry
of the regular lattice), leaving the second-order terms that compose the Laplacian.

### 4.4 Physical Interpretation

**Space is not assumed — it is reconstructed.** The chain of emergence is:

```
adjacency K_ij
  → graph distance d(i, j)
    → discrete Laplacian Δ_G
      → continuum Laplacian ∇² (at large scales)
```

Differential geometry — metric tensors, covariant derivatives, curvature —
emerges only in the continuum limit. At the fundamental level of the oscillator
network, only adjacency and causal propagation exist. The familiar tools of
continuum field theory are **effective descriptions** valid at scales r ≫ Δx.

This connects directly to the V4 result: the 1/r gravitational form arises as
the Green's function of ∇²K = 0 (B3B). In V4.1, ∇² itself is not fundamental —
it is the continuum limit of the graph Laplacian, which in turn follows from
adjacency and causal distance.

### 4.5 Claim Classification

| Claim | Classification | Justification |
|:---|---|:---|
| Graph Laplacian from adjacency | **DERIVABLE CANDIDATE** | Follows from graph definition; Δ_G = D − A is the standard construction |
| Continuum recovery of ∇² | **HYPOTHESIS** | Requires regular lattice, D = 3, and Δx → 0 limit; physical identification pending |
| Euclidean large-scale space | **HYPOTHESIS** | Graph theory result; physical emergence requires D = 3 derivation from coupling topology |

---

## 5. Claim Classification

| Claim | Classification | Justification |
|:---|---|:---|
| Space emerges from coupling topology | **HYPOTHESIS** | No derivation from oscillator dynamics; graph distance is a definition, not a prediction |
| Graph distance as physical distance | **FRAMEWORK** | Consistent with TRM structure; not yet tested against observables |
| c_TRM = 1 (causal bound) | **DERIVABLE CANDIDATE** | Follows from discrete propagation physics; requires formal proof of maximal signalling rate |
| Discrete metric (symmetry, triangle inequality) | **DERIVABLE CANDIDATE** | Follows from undirected graph structure; no additional assumptions |
| Continuum limit → Euclidean metric | **HYPOTHESIS** | Standard graph theory result; physical identification requires D=3 derivation and Lorentz consistency |
| SI value of c = Δx · f_ref | **CALIBRATED** | Requires Δx (external spatial anchor) and f_ref (I3) |
| Network causal bound = speed of light | **HYPOTHESIS** | Plausible but not derived; assumes oscillator coupling is the fundamental speed limit |
| Graph Laplacian Δ_G from adjacency | **DERIVABLE CANDIDATE** | Standard graph construction Δ_G = D − A; no additional assumptions |
| Continuum recovery ∇² from Δ_G | **HYPOTHESIS** | Requires regular lattice, D = 3, Δx → 0; physical emergence pending |
| Euclidean large-scale space from graph | **HYPOTHESIS** | Graph theory convergence; D = 3 must be derived, not assumed |

---

## 6. Scope Boundary

This document does **NOT** modify:

- **V3.4 core** — oscillator dynamics, I1 (closure family), I2 (bridge band), D1 (normalisation)
  remain unchanged.
- **V4 interpretation layer** — C5 energy density mapping, B1–B6 benchmarks, G1–G6 closure
  remain unchanged.
- **B4-T1 propagation speed closure** — the current ASSUMED classification (c_K = c) is not
  superseded; this document explores an alternative framing.

This is an **exploratory extension**. It defines a research program, not established results.

---

## 7. Relation to B4-T1

| | B4-T1 (V4) | V4.1 (this document) |
|:---|---|:---|
| Propagation speed | c_K = c (ASSUMED) | c_TRM = 1 (network causal bound) |
| Spatial anchor | I4 = Δx or assumed from c | Δx defined as graph spacing |
| Status | V4 closure | Exploratory hypothesis |

V4.1's `c_TRM = 1` is more fundamental than B4's `c_K = c` — it expresses the causal
bound in TRM-native units without reference to the speed of light. The SI value of `c`
is recovered via calibration, not derivation.

---

## 8. Falsification

The V4.1 emergent-space hypothesis is falsified if:

| Condition | Classification |
|:---|---|
| The observed speed of light deviates from Δx · f_ref (with known Δx) | INVALID |
| Graph distance does not reproduce 3+1 spacetime structure at large scales | INVALID |
| The network causal bound contradicts special relativity (Lorentz invariance) | INVALID |

---

## 9. Next Steps

1. **Formal proof** — demonstrate that c_TRM = 1 is the maximal signal propagation rate
   in a discrete oscillator network.
2. **Continuum limit** — show that graph distance converges to Euclidean distance for
   regular lattice graphs at large scales.
3. **Dimensional emergence** — derive D = 3 from coupling topology constraints.
4. **Lorentz structure** — investigate whether the discrete causal structure implies
   Lorentz invariance in the continuum limit.

---

## 10. Cross-References

| Document | Role |
|:---|:---|
| `docsV4/experiments/TRM_V4_B4_T1_PropagationSpeedClosure.md` | B4 propagation speed analysis |
| `docsV4/theory/TRM_V4_B3B_BoundaryDefectOrigin.md` | 1/r from graph Laplacian (§4 connects to discrete Δ_G) |
| `docsV4/theory/TRM_V4_Interpretation_Core.md` | C5 energy density interpretation |
| `docsV4/review/TRM_V4_Claim_Boundaries.md` | V4 claim boundaries |
| `docsV4_1/theory/TRM_V4_1_Emergent_Space.md` §3 | Causal metric and time-distance equivalence |
| `docsV4_1/theory/TRM_V4_1_Emergent_Space.md` §4 | Graph Laplacian and continuum operator emergence |
