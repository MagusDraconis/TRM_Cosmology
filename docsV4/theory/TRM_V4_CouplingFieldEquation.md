# TRM V4 — B3: Coupling Field Equation

**Date:** 2026-07-05
**Status:** Initial exploration — this is the open physics question
**Predecessors:** B1 (mechanism identified), B2 (phenomenological consistency)
**Classification:** B3 is the missing piece that separates PARTIAL from VALID

---

## 1. The Open Gate

B1 established that K1×F1 is the **unique** mechanism that produces Newtonian asymptotics among 16 candidates. But the derivation is incomplete:

```
B1 result:   δK(r) = k·M/r      →  a(r) = GM/r²   ✅ (phenomenological)
B1 gap:      WHY does δK ~ M/r?                     ❌ (not derived)
```

The coupling field equation — the governing PDE for K(r) — is the single missing element. Without it, k = G·K₀/c² is post-hoc calibration, not a prediction.

> **B3 question: Can a governing equation for the coupling field K(r) be derived or constrained from TRM oscillator structure?**

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

**Classification: EFFECTIVE ONLY** — works phenomenologically, but the governing equation is assumed, not derived.

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

## 6. What Would Close the Gap

To upgrade from PARTIAL to VALID, one of these must be demonstrated:

| Requirement | Current Status |
|:---|:---|
| **Derive ∇²K ∝ −M from oscillator dynamics** | NOT DONE — all candidates are assumed |
| **Show that K source is mass density, not K itself** | NOT DONE — energy density ∝ K creates Helmholtz, not Laplace |
| **Predict G from TRM parameters (K₀, ω_i baseline, N)** | NOT DONE — requires physical frequency scale |
| **Show that the Laplace equation is the unique admissible form** | NOT DONE — no uniqueness proof |

---

## 7. Honest Assessment

The coupling field equation is the **hardest open problem** in TRM V4. All simple approaches fail:

- **Assume Laplace** → works but is post-hoc (current B1 status)
- **Derive from action** → produces Helmholtz (K sources itself) → screening, not Newton
- **Derive from sync energy** → no constraint on K in sync state
- **Derive from information flow** → requires undefined continuum limit

**The gap is structural, not technical.** The oscillator model has K as a coupling constant between discrete sites. Making K a continuous field that obeys a PDE with mass as source requires introducing new physics — a kinetic term for K and a source term K·T — that are not present in the original model.

### The most honest formulation:

> **B3 is currently UNSOLVED.** The coupling field equation ∇²K ∝ −M is the phenomenological assumption that makes C5 work. Deriving it from oscillator dynamics is the central research problem for TRM V4 going forward. Until then, C5 remains PARTIAL — a unique phenomenological bridge, not a first-principles derivation.

---

## 8. Next Steps

1. **Formalize the action approach** — write the full action S[θ, K] including a kinetic term for K and a source term
2. **Explore whether the source can be decoupled from K** — if ρ_E(x) is NOT proportional to K(x), the Helmholtz problem is avoided
3. **Investigate whether mass acts as a boundary condition**, not a source — K(r) → K₀ + M/r at the boundary, with ∇²K = 0 in the bulk
4. **Consider whether the coupling field is not a scalar but a tensor** — K_ij is already a matrix; the continuum limit might be a rank-2 field with different PDE structure
5. **Accept the current status honestly** — B3 is an open research problem; B1/B2 are stable phenomenological validation layers
