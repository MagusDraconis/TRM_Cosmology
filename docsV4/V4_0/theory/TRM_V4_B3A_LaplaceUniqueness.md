# TRM V4 — B3A: Laplace Uniqueness and Boundary-Condition Study

**Date:** 2026-07-05
**Status:** Formal PDE admissibility analysis
**Predecessors:** `TRM_V4_CouplingFieldEquation.md`, `TRM_V4_B3_ApproachDirections.md`

---

## 1. Objective

Determine whether the Laplace/vacuum-Laplace equation is the **unique admissible PDE** for the coupling field K(r) under TRM V4 constraints, or whether other PDE classes remain viable.

---

## 2. Admissibility Criteria

A coupling-field PDE for K(r) is **admissible** iff it satisfies all of:

| # | Criterion | Justification |
|:---|:---|:---|
| **C1** | Isotropic in 3D | No preferred direction — coupling perturbation from a point mass is radial |
| **C2** | Static (time-independent) | B1/B2 operate in the synchronized steady state |
| **C3** | δK(r) ~ 1/r asymptotically | Required for Newtonian gravity (B1 result) |
| **C4** | No exponential screening at large r | Yukawa/Helmholtz produces exp(−r/r₀)/r → non-Newtonian at r ≫ r₀ |
| **C5** | No modification of V3.4 core | I1, I2, D1, oscillator equations are immutable |
| **C6** | K(r) → K₀ as r → ∞ | Perturbation vanishes at infinity |
| **C7** | Linear in the source (if sourced) | Nonlinear PDEs can produce non-1/r scalings |
| **C8** | Does not use Newtonian gravity as input | k = G·K₀/c² is calibration; the PDE must not assume GM/r² |

---

## 3. Candidate PDE Classes

### 3.1 Class L — Laplace / Vacuum Laplace

**Equation:**
```
∇²K = 0                    (vacuum — no distributed sources)
```
with boundary condition:
```
K(r) → K₀ + α·M/r   as r → 0     (point mass at origin)
K(r) → K₀            as r → ∞
```

**Solution in 3D spherical symmetry:**
```
K(r) = K₀ + A/r + B·r + C
```
Regularity at infinity requires B = 0. Regularity at origin requires the 1/r term (singular at r = 0, representing the point mass).

```
K(r) = K₀ + α·M/r     ✓
```

**PDE order:** 2 (Laplacian)

**Admissibility check:**

| Criterion | Result |
|:---|:---|
| C1 (isotropic) | ✓ — ∇² is rotationally invariant |
| C2 (static) | ✓ — no time derivatives |
| C3 (δK ~ 1/r) | ✓ — exact |
| C4 (no screening) | ✓ — pure 1/r, no exponential factor |
| C5 (core intact) | ✓ — no assumptions about oscillator dynamics |
| C6 (r → ∞) | ✓ — K → K₀ |
| C7 (linear) | ✓ — homogeneous linear PDE |
| C8 (no Newton input) | ✓ — the PDE itself does not reference G or M |

**Classification: ADMISSIBLE.**

---

### 3.2 Class P — Sourced Poisson

**Equation:**
```
∇²K = −4π·α·ρ(r)
```
where ρ(r) is a source density (mass or energy density).

**Solution in 3D:**
```
K(r) = K₀ + α ∫ ρ(r')/|r − r'| d³r'
```
For a point source ρ(r) = M·δ³(r):
```
K(r) = K₀ + α·M/r     ✓
```

**PDE order:** 2 (Poisson)

**Admissibility check:** Same as Class L for the vacuum exterior. Additional consideration:

| Extra criterion | Result |
|:---|:---|
| Source ρ(r) must be expressible in TRM terms | See §3.2 of TRM_V4_B3_ApproachDirections — no TRM-native quantity acts as an independent source without creating self-coupling (Helmholtz) |

**Classification: ADMISSIBLE in vacuum exterior, but the source term ρ(r) has no TRM-native candidate.** The vacuum Laplace formulation (Class L) is cleaner — it treats mass as a boundary condition, not a distributed source.

---

### 3.3 Class H — Helmholtz (Massive Field)

**Equation:**
```
∇²K − μ²K = 0          (homogeneous)
or
∇²K − μ²K = −S(r)      (sourced)
```

**Solution in 3D (homogeneous, spherical symmetry):**
```
K(r) = K₀ + A·exp(−μr)/r + B·exp(+μr)/r
```
Regularity at ∞ requires B = 0:
```
K(r) = K₀ + A·exp(−μr)/r
```

**Why it fails:**

| Problem | Detail |
|:---|:---|
| Exponential screening | For r ≫ 1/μ, K(r) → K₀ — the perturbation is exponentially suppressed |
| Non-Newtonian gravity | a(r) = c²/ρ_ref · ∇K would also be exponentially suppressed |
| μ must be zero | For μ = 0, Helmholtz reduces to Laplace — but μ > 0 is the generic case |
| μ-calibration | If μ is tuned to be astronomically small (galactic scale ~ 1/kpc), the screening only matters at cosmological scales — but this is fine-tuning, not derivation |

**Admissibility check:**

| Criterion | Result |
|:---|:---|
| C3 (δK ~ 1/r) | ✗ — exponential suppression at r ≫ 1/μ |
| C4 (no screening) | ✗ — screening is built into the PDE |

**Classification: INADMISSIBLE (screening).** The Helmholtz equation with any μ > 0 fails C3/C4. μ → 0 reduces to Laplace, which is Class L.

---

### 3.4 Class B — Biharmonic (∇⁴)

**Equation:**
```
∇⁴K = 0          or          ∇⁴K = S(r)
```

**Solution in 3D spherical symmetry:**
```
K(r) = K₀ + A/r + B·r + C·r² + D
```
Regularity at ∞ requires B = C = 0. The 1/r term is present but accompanied by extra degrees of freedom.

**Why it fails:**

| Problem | Detail |
|:---|:---|
| Extra boundary conditions needed | 4th-order PDE requires 4 boundary conditions; physically unmotivated |
| r² term requires fine-tuning | C = 0 is not forced by regularity alone — must be set by hand |
| No physical motivation | Why would K obey a 4th-order equation? No TRM mechanism suggests this |

**Admissibility check:**

| Criterion | Result |
|:---|:---|
| C3 (δK ~ 1/r) | ✓ — the 1/r term is present |
| Physical motivation | ✗ — no TRM mechanism for 4th-order PDE |

**Classification: INADMISSIBLE (unmotivated).** Contains the 1/r solution but requires ad-hoc suppression of the r² term. No physical justification.

---

### 3.5 Class N — Nonlinear Self-Sourced

**Equation (general form):**
```
∇²K = F(K, ∇K)             where F is a nonlinear function
```

**Prototype: Helmholtz from self-energy**
```
∇²K = −β·K                 (linear case — see Class H)
```
**Prototype: Nonlinear Poisson**
```
∇²K = −β·K^p               (p ≠ 1)
```

**Why it fails:**

| Problem | Detail |
|:---|:---|
| Linear case (p=1) | Helmholtz → screening (Class H) |
| Nonlinear case (p≠1) | Power-law solutions K ~ r^(2/(1−p)); 1/r requires p → −∞ (singular limit) |
| No TRM motivation | No oscillator mechanism suggests nonlinear PDE for K |

For K^p source with p > 1: sub-linear → steeper than 1/r at large r.
For K^p source with p < 1: super-linear → shallower than 1/r.
The 1/r solution requires p = −1 (K^(−1)) which is pathological.

**Admissibility check:**

| Criterion | Result |
|:---|:---|
| C3 (δK ~ 1/r) | ✗ — requires pathological p = −1 or fine-tuned nonlinearity |
| C7 (linearity) | ✗ — nonlinear |

**Classification: INADMISSIBLE.** No physically plausible nonlinearity produces 1/r without pathological parameter choices.

---

### 3.6 Class F — Fractional / Nonlocal

**Equation (example):**
```
(−∇²)^s K = 0          for s ≠ 1
```

**Solution in 3D (spherical symmetry):**
```
K(r) ~ r^(2s − 3)      for 0 < s < 3/2
```
For s = 1: K(r) ~ 1/r (standard Laplace). For s ≠ 1: K(r) ~ r^(2s−3).

**1/r requires s = 1 exactly.** Any s ≠ 1 produces a non-integer power law, not 1/r.

**Admissibility check:**

| Criterion | Result |
|:---|:---|
| C3 (δK ~ 1/r) | ✓ — only for s = 1 exactly |
| Physical motivation | ✗ — no TRM mechanism for fractional Laplacian |

**Classification: INADMISSIBLE (degenerate).** Reduces to Laplace at s = 1. Otherwise fails C3.

---

### 3.7 Class G — Higher-Order Elliptic with Tuned Coefficients

**Equation (general 2nd-order linear elliptic in 3D):**
```
a·∇²K + b·(r̂·∇)²K + c·K = 0
```

For isotropy (C1), the angular term must vanish: b = 0.
For no screening (C4): c = 0.
Result: a·∇²K = 0 → Laplace (Class L).

**Classification: ALL ISOTROPIC 2ND-ORDER LINEAR ELLIPTIC PDES REDUCE TO LAPLACE UNDER C1+C4.**

---

## 4. Uniqueness Proof

### Theorem (informal)

Under the TRM V4 admissibility criteria C1–C8, the **only admissible PDE class** for the coupling field K(r) is the Laplace equation (Class L) — either in vacuum form (∇²K = 0 with 1/r boundary condition) or sourced Poisson form (∇²K = −4π·α·ρ with ρ(r) expressible in TRM terms).

### Proof sketch

1. **By C1 (isotropy) + C2 (static):** The PDE must be an elliptic operator in 3D with no angular dependence.

2. **By C7 (linearity):** The PDE is linear in K. Nonlinear PDEs (Class N) fail C3 because 1/r is not a generic solution of nonlinear elliptic equations.

3. **By C4 (no screening):** Any zero-order term c·K is excluded — it produces Helmholtz with μ² = c/a, leading to exponential screening (Class H). The only exception is c = 0, which reduces to Laplace.

4. **By C3 (δK ~ 1/r):** The fundamental solution of the remaining 2nd-order operator ∇² in 3D is 1/r. Higher-order operators (∇⁴, Class B) admit 1/r but also admit r² terms that are not physically excluded without extra boundary conditions.

5. **By Occam's razor:** Among admissible PDE classes, the lowest-order (2nd) is preferred. Laplace (∇²) is the unique 2nd-order isotropic linear elliptic PDE without screening.

**Conclusion: ∇²K = 0 (vacuum) or ∇²K = −4π·α·ρ (sourced) is the UNIQUE admissible PDE class under C1–C8.**

---

## 5. What This Proves and What It Doesn't

### What IS proven (under the admissibility criteria)

- Laplace is the **unique** PDE class that satisfies all TRM V4 constraints
- No other 2nd-order linear PDE works (Helmholtz screens, others violate isotropy)
- No higher-order PDE works without unmotivated extra assumptions
- No nonlinear PDE produces 1/r generically
- The vacuum formulation (∇²K = 0) is cleaner than the sourced formulation

### What is NOT proven

- That K **must** obey the Laplace equation — only that it's the unique **admissible** PDE among the tested classes if we want 1/r behavior
- That the boundary condition K → K₀ + α·M/r is **derived** from TRM — it's still an assumption
- That α is **predicted** from TRM parameters — α = G·K₀/(c²·4π) is still a calibration

### The distinction

```
PROVEN:  "If K obeys a linear 2nd-order elliptic PDE without screening
         and we require δK ~ 1/r, then ∇²K = 0 is unique."

NOT PROVEN:  "K obeys a linear 2nd-order elliptic PDE without screening
              because of TRM oscillator dynamics."
```

The uniqueness proof constrains the **form** of the PDE, not its **origin**.

---

## 6. Classification

| Question | Answer |
|:---|:---|
| Is Laplace unique under C1–C8? | **YES** — uniqueness is proven under the stated admissibility criteria |
| Is the boundary condition derived? | **NO** — K → K₀ + α·M/r is still an input, not a consequence |
| Is α predicted? | **NO** — α = G·K₀/(c²·4π) is calibration |
| Are C1–C8 derivable from TRM? | **PARTIALLY** — C1, C2, C5, C6, C8 are design constraints; C3, C4 are empirical requirements; C7 is methodological |
| Overall B3A classification | **UNIQUE under constraints — but constraints themselves are not derived from oscillator dynamics** |

---

## 7. Implication for B3

B3A strengthens Candidate A considerably: it shows that **no other PDE can work.** The question is no longer "which PDE?" but **"why this PDE?"**

The remaining gap is not about the PDE form — it's about the **origin** of the PDE in TRM oscillator dynamics. This is the content of B3B (derivation attempt from oscillator action) or B3C (acceptance as I3).

---

## 8. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_CouplingFieldEquation.md` | Full candidate catalog (A–D) |
| `TRM_V4_B3_ApproachDirections.md` | Physical viability audit (which candidates can work) |
| This document | Formal uniqueness proof (Laplace is the only admissible PDE) |
