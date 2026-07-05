# TRM V4 — B3: Coupling Field Equation

**Date:** 2026-07-05
**Status:** B3A+B3B complete. B3C open (coefficient mapping). Form explained, coefficient calibrated.
**Classification:** B3 = FORM EXPLAINED, COEFFICIENT CALIBRATED, NOT FULLY DERIVED.

---

## 1. Current B3 Status (Post B3A+B3B)

B1 established K1×F1 as the unique phenomenological mechanism. B3A+B3B have now resolved two of the three open questions:

```
B3A:  ∇²K = 0 is the unique admissible PDE     → CLOSED ✅
B3B:  1/r emerges from discrete coupling        → CLOSED ✅
      defect → graph Laplacian Green's function
B3C:  α ↔ M coefficient mapping                  → OPEN ⬜
```

The remaining open question is the **coefficient**: how does the physical mass M relate to the coupling defect strength α, without post-hoc calibration k = G·K₀/c²?

> **B3 current question: Can α = α(M, K₀, ω_i, N) be derived from TRM quantities, or must it remain an empirical constant (analogous to G in Newtonian gravity)?**

---

## 2. Design Constraints

| Constraint | Source |
|:---|:---|
| Must produce δK ~ 1/r in 3D for a point mass | B1 Newtonian limit requirement |
| Must NOT introduce new free parameters beyond K₀, ρ_ref | V4 design principle |
| Must be expressible in terms of existing TRM quantities | Core oscillator model |
| Must NOT modify I1, I2, D1, or oscillator equations | V3.4 freeze |
| If derived from oscillator dynamics, the classification upgrades to VALID | Critical review criteria |

---

## 3. Candidate Governing Equations

### Candidate A — Laplace / Poisson-like: ∇²K ∝ −M

**Equation:**
```
∇²K(r) = −4π · α · ρ_m(r)
```
where ρ_m(r) is the mass density and α is a coupling-to-mass constant.

**Why this produces δK ~ 1/r:**
The Green's function of ∇² in 3D is 1/(4πr). For a point mass M:
```
K(r) = K₀ + α·M/r   →   δK(r) = α·M/r   ✓
```

**TRM quantities involved:**
- ρ_m(r) must be expressed in oscillator terms (energy density, phase gradient, coupling topology)
- α must be related to TRM parameters (K₀, coupling scale, frequency baseline)

**Status: ASSUMED, not derived.**
The Laplace equation is the simplest elliptic PDE that produces the right answer. But we have not shown WHY K(r) should obey ∇²K ∝ −M from oscillator dynamics. This is a modeling choice — the same gap identified in the B1 critical review.

**Classification: FORM EXPLAINED (B3A+B3B), COEFFICIENT CALIBRATED.** B3A proved Laplace is the unique admissible PDE class under C1–C8. B3B showed that 1/r emerges from the discrete graph Laplacian's Green's function when mass is modeled as a localized coupling defect in K_ij. The remaining gap is the coefficient mapping α ↔ M (B3C).

---

### Candidate B — Synchronization Energy Field

**Equation:**
```
E_sync(r) = −(K(r)/N) · Σ_{i,j} cos(θ_i − θ_j)
```
with the hypothesis that K(r) is determined by the condition that synchronization energy is stationary:
```
δE_sync / δK = 0   →   K(r) determined by phase coherence structure
```

**Why this might produce δK ~ 1/r:**
If the phase coherence field around a mass perturbation has the form cos(Δθ) ~ 1/r, then K ~ 1/r follows. But the phase coherence structure itself depends on K — this is a self-consistency condition.

**TRM quantities involved:**
- Phase coherence cos(Δθ)
- Order parameter R
- Coupling matrix K_ij

**Status: SPECULATIVE.** The synchronization energy is a function of K, not a governing equation for K. The stationarity condition δE/δK = 0 is not obviously connected to mass sources.

**Classification: NOT SUPPORTED** — no clear path from oscillator E_sync to ∇²K ∝ −M.

---

### Candidate C — Action Density Field

**Equation:**
```
S[K] = ∫ dt Σ_i [½(dθ_i/dt)² + (K/N) Σ_j cos(θ_i − θ_j)]
```
with K(r) determined by the condition that the action is stationary with respect to K variations:
```
δS / δK = 0   →   governing PDE for K
```

**Why this might produce δK ~ 1/r:**
The action includes coupling terms ∝ K·cos(Δθ). If the phase field around a mass perturbation has a specific structure (determined by the oscillator dynamics), the stationarity condition on K produces a PDE.

**TRM quantities involved:**
- Full oscillator action
- Phase dynamics dθ/dt
- Coupling-dependent terms

**Status: SPECULATIVE but more promising than B.** The action formalism is the natural language for deriving field equations. If the oscillator action can be expressed as a functional of a continuous coupling field K(x), the Euler-Lagrange equation δS/δK = 0 yields the governing PDE.

**Classification: EFFECTIVE ONLY (currently)** — the action contains K terms, but the step from discrete K_ij to continuous K(x) and from mass sources to coupling sources is not yet derived.

---

### Candidate D — Information / Phase Coherence Field

**Equation:**
```
K(r) determined by the condition that phase information propagates consistently:
∇·(K(r) · ∇θ(r)) = source(r)
```
where source(r) represents a phase perturbation from a mass concentration.

**Why this might produce δK ~ 1/r:**
If ∇θ has the structure of a monopole field (θ ~ 1/r for a point perturbation), and K scales with the local phase gradient magnitude, then K ~ |∇θ| ~ 1/r² — too steep. But if K is determined by a conservation law ∇·(K∇θ) = source, then K ~ 1/r is possible depending on the source structure.

**TRM quantities involved:**
- Phase gradient ∇θ
- Coupling topology
- Information flow along the oscillator lattice

**Status: SPECULATIVE.** The phase field θ_i is defined on discrete oscillators, not as a continuous field. The continuum limit requires a coarse-graining procedure that is not yet defined.

**Classification: NOT SUPPORTED** — requires a continuum phase-field formulation that does not currently exist in TRM.

---

## 4. Summary Table

| Candidate | Equation Type | δK ~ 1/r? | Derivation Status | Classification |
|:---|:---|:---|:---|:---|
| **A — Laplace/Poisson** | ∇²K ∝ −M | Yes (Green's function) | Assumed, not derived | **EFFECTIVE ONLY** |
| **B — Sync energy** | δE_sync/δK = 0 | Unclear | Speculative | **NOT SUPPORTED** |
| **C — Action density** | δS/δK = 0 | Possible | Speculative, most promising | **EFFECTIVE ONLY (currently)** |
| **D — Information/phase** | ∇·(K∇θ) = source | Possible | Requires continuum limit | **NOT SUPPORTED** |

---

## 5. The Most Promising Path: Candidate C (Action Formalism)

### 5.1 Why C is the natural choice

The oscillator action:
```
S = ∫ dt Σ_i [½(dθ_i/dt)² + (K/N) Σ_j cos(θ_i − θ_j)]
```

contains K explicitly in the coupling term. The Euler-Lagrange equation for K:
```
δS/δK = 0   →   Σ_j cos(θ_i − θ_j) = 0   (trivial in sync state)
```

This is too simple — in the synchronized state, cos(0) = 1, and the stationarity condition doesn't constrain K. We need a more sophisticated formulation.

### 5.2 Required Extension

The action must include:
1. **A kinetic term for K itself** — ∂_μK ∂^μK (scalar field action)
2. **A source term coupling K to mass/energy** — K·T (where T is the energy-momentum trace)
3. **The oscillator coupling term** — K·cos(Δθ)

The total action:
```
S_total = S_oscillator + S_K
S_K = ∫ d⁴x [½(∂K)² + K·ρ_E(x)]
```

The Euler-Lagrange equation:
```
∇²K = −ρ_E(x)
```

This is Candidate A (Laplace/Poisson) — but now embedded in an action formalism with a physical source term ρ_E(x).

### 5.3 The Crucial Step

**Can ρ_E(x) be expressed purely in TRM oscillator terms?**

If the energy density ρ_E is the oscillator coupling energy density:
```
ρ_E(x) ∝ K(x) · R²   (from F1 and F3, in the synchronized state)
```

Then the field equation is:
```
∇²K = −β · K
```

This is the **Helmholtz equation**, not the Laplace equation. Its solutions are:
```
K(r) ~ exp(−√β·r) / r   (Yukawa-like — screened!)
```

**This does NOT produce δK ~ 1/r — it produces exponential screening.** The Newtonian limit is lost.

### 5.4 The Fundamental Tension

```
DESIRE:   ∇²K = −M          (Laplace)  →  δK ~ 1/r  →  Newton ✓
REALITY:  ∇²K = −β·K        (Helmholtz) →  δK ~ exp(−r)/r → screened ✗
```

If the source of K is K itself (energy density proportional to coupling), the equation is Helmholtz, not Laplace. This is the same problem K3 (exponential) had in B1.

---

## 6. What Would Close B3 Completely

To upgrade from "form explained, coefficient calibrated" to "fully derived", the following must be demonstrated:

| Requirement | Status |
|:---|:---|
| **Derive ∇²K = 0 as the unique admissible PDE** | **CLOSED** — B3A uniqueness proof ✅ |
| **Explain 1/r origin from discrete coupling defect** | **CLOSED** — B3B graph Laplacian Green's function ✅ |
| **Derive α ↔ M mapping from TRM parameters** | **OPEN** — B3C: requires physical frequency scale or defect-energy relation ⬜ |
| **Predict G from TRM parameters (K₀, ω_i baseline, N)** | **OPEN** — follows from B3C closure ⬜ |

### Current B3 Classification

```
FORM:        EXPLAINED ✅   (B3A: unique PDE, B3B: 1/r mechanism)
COEFFICIENT: CALIBRATED ⬜   (k = G·K₀/c² post-hoc)
DERIVATION:  NOT FULLY DERIVED
```

B3 is now analogous to Newtonian gravity: the 1/r² form is explained by the field equation and Green's function, but G is measured, not predicted.

---

## 7. Honest Assessment

The coupling field equation has progressed significantly through B3A and B3B:

- **B3A** proved that ∇²K = 0 is the **unique admissible PDE class** under TRM V4 constraints — Helmholtz, biharmonic, nonlinear, and fractional alternatives are all excluded.
- **B3B** identified the **mechanistic origin** of the 1/r form: a localized coupling defect in K_ij acts as a singular source in the discrete graph Laplacian, whose Green's function in 3D is ~1/r. The continuum limit yields ∇²K = 0 in the bulk.

The remaining open problem is **B3C — coefficient mapping**: how does physical mass M relate to the coupling defect strength α? Currently α is calibrated via k = G·K₀/c², which uses G as input. To close B3 completely, α must be expressed in terms of TRM-native quantities (K₀, ω_i baseline, N, coupling topology) without using G.

> **B3 status: FORM EXPLAINED, COEFFICIENT CALIBRATED, NOT FULLY DERIVED.**

This is analogous to the status of G in Newtonian gravity: the 1/r² form is a consequence of the field equation, but G itself is an empirical constant.

---

## 8. Next Steps

1. **B3C** — Coefficient mapping: derive α(M, K₀, ω_i, N) without using G as input
2. **B3B-T1** — Numerical test: single-site coupling defect in CML → verify δΩ* ∝ 1/r
3. **Physical frequency scale** — anchor ω_i = 1.0 (dimensionless CML tick) to a physical frequency to determine K₀ in SI units

---

## 9. Updated Candidate Summary (Post B3A+B3B)

| Candidate | PDE Form | 1/r Origin | Coefficient | Overall |
|:---|:---|:---|:---|:---|
| **A — Laplace** | ∇²K = 0 (unique: B3A) | Graph Laplacian Green's function (B3B) | k = G·K₀/c² (B3C open) | **FORM EXPLAINED** |
| B — Sync energy | No constraint on K | N/A | N/A | DEAD |
| C — Action (naive) | Helmholtz (screening) | N/A | N/A | DEAD |
| C — Action (non-K) | Laplace possible | Same as A | Same as A | THEORETICAL (no source candidate) |
| D — Information | Undefined continuum | N/A | N/A | DEAD |
