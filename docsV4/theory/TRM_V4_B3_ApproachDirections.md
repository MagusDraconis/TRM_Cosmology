# TRM V4 — B3 Approach Directions: Which Candidates Can Realistically Work?

**Date:** 2026-07-05
**Status:** B3A and B3B complete. Laplace uniqueness proven. 1/r origin identified (discrete coupling defect → graph Laplacian Green's function). Classification: mechanism explained, coefficient calibrated.
**Predecessor:** `TRM_V4_CouplingFieldEquation.md` (candidate catalog)

---

## 1. Executive Summary

Of the four candidates A–D, only **Candidate A (Laplace/Poisson)** is realistic as a phenomenological framework. The question is not "which candidate works?" but **"can Candidate A be derived from oscillator dynamics, or must it remain an irreducible assumption?"**

**The honest assessment:** No derivation route currently exists. But one route — **action formalism with a non-K-proportional source** — is the most promising direction. This document maps the route, identifies the hard step, and states what would close the gap.

---

## 2. Why Candidates B, C, D Are Effectively Dead

### Candidate B — Synchronization Energy

```
E_sync = −(K/N) Σ cos(θ_i − θ_j)
δE_sync/δK = 0  →  Σ cos(Δθ) = 0  (in full sync: cos(0) = 1 → no constraint)
```

**Why it fails:** K appears linearly in E_sync. The stationarity condition δE_sync/δK = 0 produces a constraint on the phase differences, not on K. In the synchronized state where the mechanism must operate, cos(Δθ) ≈ 1 and the condition is trivially violated — there's no way to extract K(r) from this.

**Verdict: DEAD.** No rescue path exists without fundamentally changing how K couples to phases.

### Candidate D — Information / Phase Coherence Field

```
∇·(K·∇θ) = source(r)
```

**Why it fails:** Requires θ to be a continuous field — but TRM has θ_i on discrete oscillators. The continuum limit is not defined. The phase gradient ∇θ is not a natural TRM quantity — it's an interpolation artifact. Even if constructed, the resulting PDE structure depends entirely on the interpolation scheme, not on the physics.

**Verdict: DEAD.** Requires infrastructure (continuum phase field) that does not exist in TRM.

### Candidate C (as formulated) — Simple Action

```
S = S_oscillator + S_K
S_K = ∫ ½(∂K)² + K·ρ_E
→ ∇²K = −ρ_E
```

**Why it fails when ρ_E ∝ K:** If the energy density is the coupling energy ρ_E ∝ K·R², the equation becomes ∇²K = −β·K (Helmholtz), producing exponential screening, not 1/r.

**Why it MIGHT be rescuable:** If ρ_E is NOT proportional to K — if the source of the coupling field is something other than the coupling energy itself — then the Laplace equation is possible.

**Verdict: CONDITIONALLY ALIVE.** The rescue depends entirely on finding a source term that is not proportional to K.

---

## 3. Candidate A — The Only Game in Town

```
∇²K = −4π·α·ρ_m(r)     →     K(r) = K₀ + α·M/r     →     Newton ✓
```

Candidate A works. It's the only form that produces δK ~ 1/r. The question is not "does it work?" but "can it be justified?"

### 3.1 What Candidate A Requires

The Laplace/Poisson equation ∇²K ∝ −ρ_m has three ingredients:

| Ingredient | TRM Status |
|:---|:---|
| K as a continuous scalar field | Requires continuum limit of K_ij — plausible but not defined |
| A source term proportional to mass density ρ_m | ρ_m must be expressed in TRM terms (energy, coupling, phase) |
| The Laplacian operator ∇² | Requires spatial metric — from coupling topology? |

### 3.2 The Continuum Limit of K_ij

```
K_ij  (discrete N×N coupling matrix)
   ↓  coarse-graining
K(x)  (continuous scalar field)
```

This is the least problematic step. K_ij is already spatially structured — neighbors have stronger coupling. The continuum limit K(x) = lim_{cell→0} K_i,mean is a standard coarse-graining procedure.

### 3.3 The Source Term — The Hard Step

Mass density ρ_m(r) must be expressed in TRM terms. Candidates:

| Source candidate | TRM expression | Problem |
|:---|:---|:---|
| Coupling energy density | ρ_E ∝ K·R² | → ∇²K = −β·K (Helmholtz → screening) |
| Phase defect density | |∇θ|² (local phase gradient energy) | → depends on K itself (self-consistent equation) |
| Action residual | δS/δθ (Euler residual) | → zero in sync state |
| Intrinsic frequency deviation | ω_i − ω_mean | → not obviously related to mass |

**The fundamental problem:** Every TRM-native energy-like quantity is either proportional to K (coupling energy, sync energy) or zero in the synchronized state (action residual, phase defect). **There is no TRM-native quantity that acts as an independent source for K.**

### 3.4 The Spatial Metric

∇² requires a notion of distance. In TRM, distance is encoded in the coupling topology: strongly coupled oscillators are "close," weakly coupled are "far." This suggests:

```
d_ij ∝ 1/K_ij     (spatial distance from coupling strength)
```

But this makes the metric depend on K — which makes ∇²K a nonlinear self-referential equation. Not necessarily fatal, but complicates the analysis significantly.

---

## 4. The Only Realistic Route: Non-K-Proportional Source

### 4.1 The Key Insight

Every TRM energy density is proportional to K. This produces the Helmholtz equation and screening. To get the Laplace equation, the source must be **independent of K**.

### 4.2 What Could an Independent Source Be?

**Mass as boundary condition, not as source.**

```
∇²K = 0   (in vacuum — no sources)
K(r) → K₀ + α·M/r   as r → 0   (point mass as boundary condition)
```

This is the **vacuum Laplace equation with singular boundary conditions.** It's exactly how Newtonian gravity is formulated: ∇²Φ = 0 in vacuum, with Φ → −GM/r near the source.

**Why this is promising:**
- No source term needed — ∇²K = 0 in the bulk
- K(r) = K₀ + α·M/r is the unique solution with the right boundary condition
- The boundary condition "a mass M produces a 1/r coupling perturbation" can be treated as an empirical input — analogous to how G is an empirical input in Newtonian gravity

**Why this is still post-hoc:**
- The boundary condition "K → K₀ + α·M/r at the source" is exactly the statement we're trying to derive
- It says "masses perturb coupling as 1/r" — which is what we want to prove

### 4.3 The Honest Re-formulation

The boundary condition approach re-frames B3 from:

```
"Why does ∇²K ∝ −M?"     (hard — requires source term)
```
to:
```
"Why does mass impose K → K₀ + α·M/r as a boundary condition?"     (different hard question)
```

Both are hard. The boundary condition version is slightly cleaner because it separates the field equation (Laplace in vacuum) from the mass-coupling relation (boundary condition at source).

---

## 5. What Would Close B3?

B3 is closed when ONE of the following is demonstrated:

| Closure condition | Difficulty | Current status |
|:---|:---|:---|
| Derive ∇²K = −4π·α·ρ from oscillator action with an independent source term | **HIGH** | No candidate source exists |
| Derive the 1/r boundary condition from the discrete K_ij perturbation by a mass node | **HIGH** | Mass is not a TRM-native concept |
| Show that the Laplace equation is the unique consistent PDE for K in the synchronized continuum limit | **MEDIUM** | Could be a uniqueness proof, not a derivation |
| Accept Candidate A as an irreducible structural input (I3) and close the theory with 3 assumptions | **LOW** | Adds I3 to I1, I2, D1; honest but reduces theoretical economy |

---

## 6. Recommendation

### Short term: Accept the status honestly

B3 is currently unsolved. C5 is the unique phenomenological bridge. Classification remains PARTIAL. This is documented and consistent across all files.

### Medium term: Pursue the boundary condition route

```
∇²K = 0 in vacuum
K(r) → K₀ + α·M/r at point mass (boundary condition)
```

This is cleaner than the sourced Poisson equation and aligns with how Newtonian gravity is formulated.

### Long term: Explore whether "mass as coupling perturbation" can be derived

The central question is whether the discrete oscillator lattice, when a "mass node" is introduced (a site with modified intrinsic frequency or coupling), naturally produces δK_ij ~ 1/r_ij at large distances. This is a numerical/computational question that might be testable with the existing CML infrastructure.

### If all routes fail: Consider I3

If no derivation route works, the honest resolution is to add I3 ("Coupling field obeys Laplace equation with mass as boundary condition") as a third irreducible input. This:
- Keeps the theory consistent
- Makes the assumption explicit
- Does not claim false derivation
- Reduces theoretical economy (3 inputs instead of 2) — but is honest

---

## 7. Summary

| Candidate | Viability | Why |
|:---|:---|:---|
| A — Laplace/Poisson | **ALIVE** | Only form producing δK ~ 1/r. Derivation open, but framework is clean. |
| B — Sync energy | **DEAD** | No constraint on K in sync state. |
| C — Action (naive) | **DEAD** | ρ_E ∝ K → Helmholtz, not Laplace. |
| C — Action (non-K source) | **THEORETICALLY POSSIBLE** | Requires a source term independent of K — no candidate exists. |
| D — Information field | **DEAD** | Requires undefined continuum phase field. |

### The honest summary

> **Candidate A is the only viable framework. The derivation of ∇²K = 0 (vacuum) with K → K₀ + α·M/r (boundary condition) remains open. If no derivation route is found, I3 (coupling field equation as irreducible input) is the honest fallback.**
