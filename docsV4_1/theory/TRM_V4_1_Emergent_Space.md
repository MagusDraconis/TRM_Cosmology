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

---

## 4. Claim Classification

| Claim | Classification | Justification |
|:---|---|:---|
| Space emerges from coupling topology | **HYPOTHESIS** | No derivation from oscillator dynamics; graph distance is a definition, not a prediction |
| Graph distance as physical distance | **FRAMEWORK** | Consistent with TRM structure; not yet tested against observables |
| c_TRM = 1 (causal bound) | **DERIVABLE CANDIDATE** | Follows from discrete propagation physics; requires formal proof of maximal signalling rate |
| SI value of c = Δx · f_ref | **CALIBRATED** | Requires Δx (external spatial anchor) and f_ref (I3) |
| Network causal bound = speed of light | **HYPOTHESIS** | Plausible but not derived; assumes oscillator coupling is the fundamental speed limit |

---

## 5. Scope Boundary

This document does **NOT** modify:

- **V3.4 core** — oscillator dynamics, I1 (closure family), I2 (bridge band), D1 (normalisation)
  remain unchanged.
- **V4 interpretation layer** — C5 energy density mapping, B1–B6 benchmarks, G1–G6 closure
  remain unchanged.
- **B4-T1 propagation speed closure** — the current ASSUMED classification (c_K = c) is not
  superseded; this document explores an alternative framing.

This is an **exploratory extension**. It defines a research program, not established results.

---

## 6. Relation to B4-T1

| | B4-T1 (V4) | V4.1 (this document) |
|:---|---|:---|
| Propagation speed | c_K = c (ASSUMED) | c_TRM = 1 (network causal bound) |
| Spatial anchor | I4 = Δx or assumed from c | Δx defined as graph spacing |
| Status | V4 closure | Exploratory hypothesis |

V4.1's `c_TRM = 1` is more fundamental than B4's `c_K = c` — it expresses the causal
bound in TRM-native units without reference to the speed of light. The SI value of `c`
is recovered via calibration, not derivation.

---

## 7. Falsification

The V4.1 emergent-space hypothesis is falsified if:

| Condition | Classification |
|:---|---|
| The observed speed of light deviates from Δx · f_ref (with known Δx) | INVALID |
| Graph distance does not reproduce 3+1 spacetime structure at large scales | INVALID |
| The network causal bound contradicts special relativity (Lorentz invariance) | INVALID |

---

## 8. Next Steps

1. **Formal proof** — demonstrate that c_TRM = 1 is the maximal signal propagation rate
   in a discrete oscillator network.
2. **Continuum limit** — show that graph distance converges to Euclidean distance for
   regular lattice graphs at large scales.
3. **Dimensional emergence** — derive D = 3 from coupling topology constraints.
4. **Lorentz structure** — investigate whether the discrete causal structure implies
   Lorentz invariance in the continuum limit.

---

## 9. Cross-References

| Document | Role |
|:---|:---|
| `docsV4/experiments/TRM_V4_B4_T1_PropagationSpeedClosure.md` | B4 propagation speed analysis |
| `docsV4/theory/TRM_V4_B3B_BoundaryDefectOrigin.md` | 1/r from graph Laplacian |
| `docsV4/theory/TRM_V4_Interpretation_Core.md` | C5 energy density interpretation |
| `docsV4/review/TRM_V4_Claim_Boundaries.md` | V4 claim boundaries |
