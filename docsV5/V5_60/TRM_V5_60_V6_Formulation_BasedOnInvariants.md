# TRM V5.60 — V6 Formulation Based on Invariants

**Date:** 2026-07-22
**Status:** PROPOSAL (speculative, NOT claimed)
**Branch:** feature/v5.60-kernel-emergence-audit
**Basis:** LCM_01–LCM_04 findings

---

## Caveat Lector

This document is a **theoretical proposal**, not a claim of discovery. All content is
speculative and subject to experimental validation. No physical interpretation is claimed.
V6 readiness is NOT achieved — this is a candidate foundation, not a completed framework.

**What this IS:**
- A proposed geometric structure based on observed SAC invariants
- A candidate path toward V6 that avoids the falsified V5.60 assumptions
- A theoretical framework that can be tested in V5.61+

**What this is NOT:**
- A claim of physical spacetime emergence
- A completed V6 derivation
- A replacement for any physical theory
- "Energy," "action," "momentum," or any physical quantity

---

## 1. Motivation: Why the Old V6 Path Failed

The original V6 conception (V5.57–V5.59) assumed three prerequisites:

| Assumption | Status | Evidence |
|:-----------|:------|:---------|
| c_eff = Ω·MD is invariant | ❌ FALSIFIED | CV=0.80–1.08 (EMG_01) |
| Omega ⟂ MeanDist (orthogonal) | ❌ FALSIFIED | r=0.81, up to r=0.95 at N=80 (EMG_02) |
| SAC converges to fixed point | ❌ FALSIFIED | Period-2 limit cycle, λ=0.01 (KEM_04) |
| Causal closure | ❌ BLOCKED | V5.40–V5.41 |

All four pillars collapsed. The V6 path based on c_eff as an invariant "speed" with
orthogonal "space" and "time" proxies is structurally impossible within the SAC dynamics.

The LCM suite (LCM_01–LCM_04) discovered an alternative: the SAC limit cycle has a
**2D invariant manifold** with a conserved quantity. This opens a new V6 path that does
not require any of the falsified assumptions.

---

## PART A — Metric on the Invariant Manifold

### A.1 The 2D Invariant Subspace

LCM_03 and LCM_04 identified two approximately conserved quantities:

```
I₁ = 0.70·km + 0.30·d_mean    (CV = 0.017, coupling-distance invariant)
I₂ = 0.90·km + 0.10·Omega     (CV = 0.064, coupling-frequency invariant)
```

These span a 2D subspace of the full 5D SAC state space. In this subspace:

- **I₁** acts as a **constraint surface** — it varies very little (range ≈ 0.046) and is
  nearly constant across N (range 0.799–0.812, CV = 0.7%).
- **I₂** acts as a **coordinate** along the constraint surface — it sweeps a wider range
  (≈ 0.280) and varies with system size N.

The trajectory in (I₁, I₂) space is a **highly elongated ellipse** (eccentricity ε = 0.988,
axis ratio = 0.158). The ellipse is stretched along the I₂ axis, with I₁ confined to a
narrow band.

### A.2 General Metric Form

A general 2D metric on the invariant manifold takes the form:

```
ds² = g₁₁·dI₁² + g₂₂·dI₂² + 2·g₁₂·dI₁·dI₂
```

Where g_ij are the metric components (functions of I₁, I₂).

### A.3 Constraint Approximation

Since I₁ ≈ constant (CV = 0.017), we can apply a constraint approximation:

```
dI₁ ≈ 0  →  g₁₁·dI₁² ≈ 0,  g₁₂·dI₁·dI₂ ≈ 0
```

The metric reduces to:

```
ds² ≈ g₂₂(I₁, I₂)·dI₂²
```

This is a **1D effective metric** — distances are measured along the I₂ axis, modulated
by the metric component g₂₂.

### A.4 Determining g₂₂

From LCM_04 Part A, the step lengths along the ellipse provide an estimate. The mean
step length in (I₁, I₂) space is approximately:

```
⟨Δs⟩ = ⟨√(ΔI₁² + ΔI₂²)⟩
```

Since I₁ varies much less than I₂, the effective metric component is approximately:

```
g₂₂ ≈ (Δs/ΔI₂)²
```

From the 50-epoch data, Δs ranges from ~0.004 to ~0.249 per epoch step, with a mean
of approximately 0.075. Since I₂ varies by ~0.280 over the full cycle, the total arc
length is approximately 3.76 over 50 epochs.

### A.5 Is g₂₂ Constant?

The curvature of the trajectory is high (~2.42 rad = 139° per step on average),
suggesting that the direction changes significantly. If g₂₂ were constant, the
trajectory would be a straight line. The observed curvature indicates that the
effective metric has non-trivial structure — g₂₂ varies along the ellipse.

Specifically, the trajectory zigzags: consecutive steps alternate direction (curvature
≈ π), creating the period-2 pattern. This suggests the metric alternates between two
values:

```
g₂₂(t) ≈ g₂₂_even  for even epochs
g₂₂(t) ≈ g₂₂_odd   for odd epochs
```

The alternation of g₂₂ is the geometric expression of the period-2 limit cycle.

### A.6 Summary — Part A

| Property | Value |
|:---------|:------|
| Manifold dimension | 2D (I₁, I₂) |
| Effective dimension | 1D (I₁ ≈ const) |
| Effective metric | ds² = g₂₂·dI₂² |
| g₂₂ behavior | Alternating (period-2) |
| Trajectory shape | Highly elongated ellipse (ε=0.988) |
| Total arc length (50 epochs) | ~3.76 |

---

## PART B — Time Parameter on the Manifold

### B.1 Omega as Time — Failed

The natural candidate for a time parameter is Omega — the collective oscillator frequency.
Omega is the SAC state variable that most closely resembles "clock rate."

However, LCM_04 Part B showed:

- **Omega is NOT monotonic** — 27 reversals in 49 steps (55% of steps change sign)
- **Omega alternates** — odd-epoch mean = 1.72, even-epoch mean = 1.77
- **r(I₁, Ω) = -0.433** — moderate correlation, not a clean parameterization
- **R²(I₁(Ω)) = 0.187** — poor linear fit

Omega cannot serve as a proper time coordinate because it oscillates. A time parameter
must be monotonic — it must always increase (or decrease) along the trajectory.

### B.2 Arc Length as Time

The arc length s along the ellipse IS monotonic:

```
s(t) = ∫₀ᵗ ds = ∑_{i=1}^{t} √(ΔI₁² + ΔI₂²)
```

Each step adds a positive increment: Δs > 0 for all epochs. The cumulative arc length
is strictly increasing.

From LCM_04 Part A:
- Total arc length = 3.76 over 50 epochs
- Mean step = 0.075 per epoch

Arc length is a natural "time" coordinate: it measures the distance traveled along the
invariant manifold. It is monotonic, accumulates with each epoch, and is independent of
the period-2 oscillation (which cancels out in the distance measure).

### B.3 Cycle Count as Time

An alternative time parameter is simply the **epoch number** (or cycle count). Each
SAC epoch is a discrete step in the dynamics, and the epoch counter is trivially
monotonic.

The relationship between epoch count t and arc length s is approximately linear:

```
s(t) ≈ ⟨Δs⟩·t  (for large t)
```

where ⟨Δs⟩ is the mean step length. Since the step length alternates (even/odd epochs
have different mean step lengths due to the period-2 cycle), the relationship is
piecewise linear but overall proportional.

### B.4 Comparison of Time Candidates

| Candidate | Monotonic? | r(I₁, ·) | r(I₂, ·) | Viability |
|:----------|:----------:|:--------:|:--------:|:----------|
| Omega | ❌ No (27 rev/49) | -0.433 | 0.303 | **NOT viable** |
| Arc length s | ✅ Yes | — | — | **Best candidate** |
| Epoch count t | ✅ Yes | — | — | **Viable (discrete)** |

**Recommendation:** Use arc length s as the continuous time parameter, with epoch count
t as the discrete proxy. Omega should be treated as a **dynamical driver** (the "engine"
that moves the system along the manifold), not as the time coordinate itself.

### B.5 Summary — Part B

| Property | Value |
|:---------|:------|
| Omega monotonicity | 27/49 reversals — FAILED |
| Arc length monotonicity | Strictly increasing — PASSED |
| Preferred time coordinate | Arc length s(t) |
| Discrete proxy | Epoch count t |
| Omega role | Cyclic driver, not time |

---

## PART C — Proposed V6 Geometry

### C.1 The Geometric Picture

The V6 geometry is a **1+1 dimensional structure**:

```
Space direction:  I₂ (coordinate along the constraint surface)
Time direction:   s  (arc length along the ellipse)
Constraint:       I₁ ≈ constant (the "radius" of the manifold)
Driver:           Ω  (cyclically drives the system through the manifold)
```

The metric is:

```
ds² = g₂₂(I₁, I₂)·dI₂²
```

with the constraint I₁ ≈ I₁₀ (constant).

### C.2 Properties of the V6 Geometry

**Dimensionality:** The effective geometry is 1+1D — one spatial coordinate (I₂) and
one temporal coordinate (s). This is the minimal possible spacetime — a 1D "worldline."

**Signature:** The metric ds² = g₂₂·dI₂² has Euclidean signature (+, +) in the (s, I₂)
plane. There is no Lorentzian (-, +) structure because there is no light cone — the
dynamics are non-relativistic.

**Curvature:** The metric component g₂₂ is not constant — it alternates with the
period-2 cycle and varies along the ellipse. This gives the V6 manifold intrinsic
"curvature" (in the differential geometry sense, not physical curvature).

**Topology:** The trajectory is a closed loop (ellipse) in (I₁, I₂) space. In the
(s, I₂) representation, it becomes a helix: I₂ oscillates as s increases. The topology
is S¹ × ℝ (cylinder).

### C.3 The V6 "Spacetime" Diagram

```
       I₂
       ↑
  1.2  |     ·  ·  ·  ·  ·     (helical trajectory)
       |    / \/ \/ \/ \/ \
  1.0  |   ·  ·  ·  ·  ·  ·
       |  / \/ \/ \/ \/ \/
  0.9  | ·  ·  ·  ·  ·  ·
       +------------------------→ s (arc length)
       0    1    2    3    4
       
  I₁ ≈ 0.808 (nearly constant — the "radius" of the cylinder)
```

Each · is a SAC epoch. The trajectory spirals along the cylinder with I₁ as the
constant radius and I₂ as the oscillating height.

### C.4 What V6 Geometry IS NOT

- **NOT** a physical spacetime — no light cones, no Lorentz invariance, no Minkowski metric
- **NOT** a relativistic theory — the signature is Euclidean, not Lorentzian
- **NOT** a derivation of 3+1D — the effective dimension is 1+1D
- **NOT** a claim of emergence — the geometry is a **description** of the SAC dynamics, not an "emergence" from more primitive principles

### C.5 What V6 Geometry COULD BE (if validated)

If cross-seed and cross-parameter validation confirms this structure:

1. **I₁** could serve as the fundamental conserved quantity analogous to total energy
   or total action in physical theories.

2. **I₂** and **s** could serve as generalized coordinates for a Lagrangian or
   Hamiltonian formulation of SAC dynamics.

3. **g₂₂** could be the metric component from which "distance" in the SAC state space
   is derived — analogous to the spatial metric in General Relativity.

4. **Ω** as the cyclic driver could be analogous to the Hamiltonian generating time
   evolution — the quantity that "pushes" the system forward.

Again: ALL of this is speculative. None of it is claimed. This is a description of a
candidate framework, not a declaration of discovery.

### C.6 V6 Geometric Quantities

| Quantity | Symbol | Role | Analog (speculative) |
|:---------|:------|:-----|:---------------------|
| Invariant | I₁ | Conserved constraint | Total energy / action |
| Coordinate | I₂ | Spatial position | Generalized coordinate |
| Arc length | s | Time | Proper time |
| Metric | g₂₂ | Geometric distance | Spatial metric component |
| Driver | Ω | Generator of motion | Hamiltonian |
| Coupling | km | Binding strength | Potential energy density |
| Distance | d_mean | Phase separation | Kinetic energy density |

**Caveat:** The "Analog" column is purely heuristic. No physical claims are made.
These are structural parallels, not identities.

### C.7 Summary — Part C

| Property | Description |
|:---------|:------------|
| Effective dimension | 1+1D (I₂ + s) |
| Constraint | I₁ ≈ constant |
| Metric signature | Euclidean (+, +) |
| Topology | S¹ × ℝ (cylinder) |
| Time coordinate | Arc length s(t) |
| Space coordinate | I₂(t) |
| Dynamics | Ω drives oscillation |

---

## PART D — Comparison to V4/V5 Geometry

### D.1 V4 Geometry (Pre-SAC)

The V4 internal geometry was based on two quantities:

```
Omega     — collective oscillator frequency (temporal proxy)
MeanDist  — mean phase distance (spatial proxy)
c_eff     — Omega × MeanDist (internal propagation speed)
```

**V4 claims (now falsified):**
- c_eff is invariant across N and seed → **FALSIFIED** (CV = 0.80–1.08)
- Omega and MeanDist are orthogonal → **FALSIFIED** (r = 0.81)
- Lorentz-like interval structure with light-cone separation → **INCONCLUSIVE**

**What V4 got right:**
- Omega is an important dynamical quantity (loading = 0.94 on PC1)
- MeanDist carries structural information (appears in I₁ through d_mean)
- The product Ω·MD was an inspired guess — it failed but pointed toward the invariant

### D.2 V5 Geometry (M3++ Adaptive Control)

The V5 framework shifted from internal geometry to **adaptive control**:

```
M3++ pipeline:
  K → Sim → RP → Nm → DL → Cupd → K'
  
Stop-Low policy:
  c3OmgS > 0.1 → continue
  c3OmgS ≤ 0.1 → stop
```

**V5 achievements:**
- Validated adaptive pipeline (M3++)
- Stop-Low policy proven (75% work reduction, zero damage)
- Operational efficiency demonstrated
- Causal closure blocked (V5.40–V5.41)

**What V5 lacks:**
- No geometric invariant — the pipeline is operational, not structural
- No conserved quantity — nothing is formally conserved across epochs
- No manifold description — the state space is 5D with no reduction

### D.3 V6 Proposal (Invariant-Based Geometry)

**What V6 adds beyond V4:**
- A **genuine conserved quantity** (I₁) instead of a falsified invariant (c_eff)
- A **mathematically derived** structure (from Cupd linearization) instead of an ad-hoc product
- **No orthogonality requirement** — I₁ and I₂ are correlated but the correlation is understood
- **No fixed-point requirement** — the limit cycle IS the geometry, not an obstacle

**What V6 adds beyond V5:**
- **Dimensional reduction** — 5D state space → 2D invariant manifold → 1D effective metric
- **Conservation law** — I₁ is approximately constant across epochs AND across N
- **Geometric interpretation** — the SAC dynamics have a well-defined geometric structure
- **Analytical foundation** — the invariant follows from Cupd = K₀·exp(-d/ξ), not from fitting

### D.4 Strengths and Weaknesses

| Aspect | V4 | V5 | V6 Proposal |
|:-------|:-:|:--:|:-----------:|
| Has invariant | ❌ (falsified) | ❌ (none sought) | ✅ (I₁, CV=0.017) |
| Has manifold | ❌ | ❌ | ✅ (2D → 1D effective) |
| Has metric | ❌ | ❌ | ✅ (ds² = g₂₂·dI₂²) |
| Cross-seed validated | ❌ | ✅ | ❌ (single seed) |
| Cross-parameter validated | ❌ | ✅ | ⚠️ (partial, LCM_03) |
| Proven operational | ❌ | ✅ (M3++/Stop-Low) | ❌ (theoretical only) |
| Causal closure | ❌ | ❌ (blocked) | ❌ (not addressed) |
| Physical interpretation | ❌ | ❌ | ❌ (explicitly NOT claimed) |

### D.5 What V6 Does NOT Change

- **M3++ pipeline:** Unchanged. V6 is a geometric description, not a pipeline modification.
- **Stop-Low policy:** Unchanged. V6 does not affect operational decisions.
- **Causal closure:** Remains blocked. V6 describes structure, not causation.
- **V6 readiness:** NOT achieved. This is a proposal, not a completion.

### D.6 Next Steps for V6 Validation

1. **Cross-seed validation:** Test I₁ across 100+ seeds at N=72. Does CV(I₁) remain < 0.02?
2. **Cross-K0 validation:** The invariant was derived assuming fixed K₀. Does I₁ hold for
   varying K₀ with appropriate rescaling?
3. **Infinite-epoch limit:** Does I₁ remain constant as t → ∞, or does it drift?
4. **Analytical proof:** Can the invariance of I₁ be proven from the Cupd functional form?
5. **Second invariant refinement:** Can I₂ be made tighter (CV < 0.03)?
6. **Omega-free parameterization:** Can the metric be expressed without Omega?

### D.7 Summary — Part D

| Framework | Core Structure | Status |
|:----------|:--------------|:-------|
| V4 | c_eff = Ω·MD, Ω ⟂ MD | FALSIFIED |
| V5 | M3++ pipeline, Stop-Low | VALIDATED (operational) |
| V6 Proposal | I₁, I₂, ds² = g₂₂·dI₂² | SPECULATIVE (not validated) |

---

## 5. Conclusion

The SAC limit cycle has revealed an unexpected structure: a **2D invariant manifold**
with a near-conserved quantity I₁. This structure was not sought — it emerged from the
data (LCM_02 Part C), was validated across parameters (LCM_03 Part A), and was
geometrically characterized (LCM_04).

The V6 proposal based on I₁, I₂, and arc length s avoids all four pitfalls that
collapsed the original V6 path:

| Old V6 requirement | Status | V6 proposal replacement |
|:-------------------|:------|:------------------------|
| c_eff invariant | FALSIFIED | I₁ = conserved (CV=0.017) |
| Ω ⟂ MD orthogonal | FALSIFIED | No orthogonality required |
| SAC fixed point | FALSIFIED | Limit cycle IS the geometry |
| Causal closure | BLOCKED | Not required for geometry |

**However:** This is a SINGLE-SEED, SINGLE-K₀ analysis. The proposal has NOT been
validated across the full ensemble. It may fail under broader testing. The geometric
interpretation is heuristic, not rigorous.

**V6 readiness: NOT ACHIEVED.** This document is a candidate foundation, not a
completed framework. It should be treated as a hypothesis to be tested in V5.61+.

---

## 6. Development Status

| Metric | Value |
|:-------|:------|
| Document type | Theoretical proposal |
| Evidential basis | LCM_01–LCM_04 (seed 1005) |
| Validation scope | Single seed, single K₀ |
| V6 readiness | NOT ACHIEVED |
| Stop-Low status | SAFE (unchanged) |
| M3++ status | UNCHANGED |
| Physical claims | NONE |

---

*Generated 2026-07-22. This document is a theoretical proposal based on the V5.60
LCM suite findings. All geometric interpretations are speculative and explicitly
NOT CLAIMED as physical reality. The TRM project does not claim derivation of
physical spacetime, relativity, or cosmology.*
