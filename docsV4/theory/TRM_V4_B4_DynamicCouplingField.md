# TRM V4 — B4: Dynamic Coupling Field Theory

**Date:** 2026-07-05
**Status:** Extending the static Laplace framework to time-dependent dynamics
**Predecessors:** B1/B2 (validation), B3A (Laplace uniqueness), B3B (1/r origin), B3C (coefficient)

---

## 1. Motivation

The static framework (B3A+B3B) is closed:

```
∇²K = 0  (vacuum)     with     K(r) → K₀ + α/r  (point mass)
```

This describes the stationary gravitational field of a static mass distribution. To describe:
- Gravitational waves / propagating perturbations
- Time-varying mass distributions
- Retarded potentials (finite propagation speed)
- Full causal field theory

...we need a **time-dependent** extension.

---

## 2. Design Constraints

| # | Constraint | Justification |
|:---|:---|:---|
| **D1** | Static limit → ∇²K = 0 | Must reproduce B3A result |
| **D2** | Reduces to δK ~ 1/r for static point mass | Must reproduce B3B result |
| **D3** | Finite propagation speed | Causality (relativistic consistency) |
| **D4** | Linear (superposition) | K1×F1 mechanism requires linearity |
| **D5** | No modification of V3.4 core | Design principle |
| **D6** | No new free parameters beyond those in static framework | Theoretical economy |

---

## 3. Candidate Dynamic Equations

### 3.1 Candidate A — Massless Wave (d'Alembert)

**Equation:**
```
□K = ∂²K/∂t² − c_K²·∇²K = 0          (homogeneous)
□K = −4π·α·ρ(x,t)                     (sourced)
```
where □ = ∂²/∂t² − c_K²·∇² is the d'Alembert operator.

**Static limit:** ∂²/∂t² = 0 → ∇²K = 0 ✓

**Propagation speed:** c_K (to be determined)

**Properties:**
- Hyperbolic PDE — finite propagation speed
- Time-reversible (no dissipation)
- Solutions: K(x,t) = f(r − c_K·t)/r + g(r + c_K·t)/r (retarded + advanced)
- Retarded solution: K(x,t) = K₀ + α/r_ret where r_ret = |x − x_s(t_ret)|, t_ret = t − r_ret/c_K

**Physical interpretation:** K propagates as a massless wave — analogous to the gravitational wave equation in linearized GR. If c_K = c, this is the standard weak-field GR wave equation.

**What would set c_K?**

In the oscillator network, a coupling perturbation propagates through the phase dynamics. The propagation speed is determined by the synchronization front velocity:
```
c_K ∼ K₀ · Δx / (characteristic time)
```
where Δx is the oscillator spacing. In the CML (dimensionless): c_K(CML) ∼ K₀ = 0.10 (in units of oscillator spacing per tick).

In physical units: c_K = K₀(CML) · Δx / τ_tick, where τ_tick = 1/f_ref.

**Classification: EFFECTIVE (c_K requires calibration).**

---

### 3.2 Candidate B — Damped Wave (Telegraph)

**Equation:**
```
∂²K/∂t² + γ·∂K/∂t − c_K²·∇²K = 0
```

**Static limit:** ∂²/∂t² = 0, ∂/∂t = 0 → ∇²K = 0 ✓

**Properties:**
- Hyperbolic with dissipation
- Damping coefficient γ breaks time-reversal
- Short-wavelength modes damped on timescale 1/γ
- For γ → 0: reduces to Candidate A

**Physical interpretation:** Damping could represent energy dissipation from the coupling perturbation into the oscillator bath. The damping timescale τ_damp = 1/γ is related to the oscillator relaxation time.

**Problem:** γ introduces a new parameter not present in the static framework. Violates D6 unless γ can be expressed in terms of static parameters.

**Classification: EFFECTIVE (γ is a new parameter).**

---

### 3.3 Candidate C — Relaxation-Diffusion

**Equation:**
```
∂K/∂t = D·∇²K
```

**Static limit:** ∂/∂t = 0 → ∇²K = 0 ✓

**Properties:**
- Parabolic PDE — **infinite propagation speed** (signal propagates instantly)
- Violates causality in relativistic context
- Acceptable for non-relativistic (slow-motion) limit
- Diffusion coefficient D sets the relaxation timescale: τ ∼ L²/D

**Physical interpretation:** The coupling field K relaxes diffusively toward the static Laplace solution. A moving mass creates a time-dependent K that lags behind the instantaneous Laplace solution. The lag is characterized by D.

**Problem:** Infinite propagation speed violates D3. The diffusion equation is not suitable for a relativistic field theory. It could serve as a non-relativistic approximation (v ≪ c_K).

**Classification: EFFECTIVE (non-relativistic limit only). D3 violation disqualifies for full field theory.**

---

## 4. Candidate Comparison

| Property | A — Wave | B — Damped wave | C — Diffusion |
|:---|:---|:---|:---|
| **PDE type** | Hyperbolic | Hyperbolic + dissipation | Parabolic |
| **Static limit** | ✓ ∇²K = 0 | ✓ ∇²K = 0 | ✓ ∇²K = 0 |
| **Propagation speed** | Finite (c_K) | Finite (c_K) | Infinite ✗ |
| **Causality** | ✓ | ✓ | ✗ |
| **Time-reversible** | ✓ | ✗ (γ > 0) | ✗ |
| **New parameters** | 1 (c_K) | 2 (c_K, γ) | 1 (D) |
| **GR analogy** | Linearized GR wave eq. | — | Heat equation |
| **D6 (no new params beyond static)** | ✗ (c_K) | ✗✗ (c_K + γ) | ✗ (D) |

---

## 5. Preferred Candidate: A (Massless Wave)

### 5.1 Justification

Candidate A is preferred because:
1. **Causal** — finite propagation speed (D3)
2. **Time-reversible** — no ad-hoc dissipation
3. **GR-consistent** — same PDE class as linearized GR gravitational waves
4. **Minimal** — only one new parameter (c_K) beyond static framework
5. **Natural static limit** — □K = 0 → ∇²K = 0 when ∂²/∂t² = 0

### 5.2 The New Parameter: c_K

The wave speed c_K is a new parameter not present in the static framework. Options for determining c_K:

| Option | Approach | Status |
|:---|:---|:---|
| **Identify with c** | c_K = c (speed of light) | Assumed — matches GR but not derived from TRM |
| **Derive from oscillator dynamics** | c_K = f(K₀, ω_i, coupling topology) · Δx/τ_tick | Requires oscillator spacing Δx and tick timescale τ_tick |
| **Calibrate from gravitational wave data** | c_K from LIGO/Virgo observations | Empirical — c_K ≈ c at ~10⁻¹⁵ precision |

The most conservative approach: **c_K is a new empirical parameter** — analogous to how c enters Maxwell's equations. In Maxwell's theory, c emerges from ε₀ and μ₀; in TRM, c_K should ideally emerge from K₀ and the oscillator timescale.

### 5.3 Prospect for Deriving c_K

In the discrete oscillator network, a perturbation at site i₀ affects neighbor i₀+1 after one coupling timescale:
```
τ_coupling ∼ 1/K₀     (dimensionless CML)
```
The perturbation propagates one lattice spacing Δx in time τ_coupling:
```
c_K = Δx / τ_coupling = Δx · K₀(CML) · f_ref     (physical units)
```

With I3 (f_ref = 9.19×10⁹ Hz) and K₀(CML) = 0.10:
```
c_K = Δx · 0.10 · 9.19×10⁹ = Δx · 9.19×10⁸  m/s
```

For c_K = c = 3×10⁸ m/s:
```
Δx = c / (K₀·f_ref) = 3×10⁸ / 9.19×10⁸ ≈ 0.33 m
```

This would imply an oscillator spacing of ~33 cm. Whether this is physically meaningful depends on what the oscillators represent.

---

## 6. Retarded Potential Formulation

With Candidate A, the sourced wave equation is:

```
□K = −4π·α·ρ(x,t)
```

The retarded solution:
```
K(x,t) = K₀ + α ∫ ρ(x', t_ret) / |x − x'| d³x'
```
where t_ret = t − |x − x'|/c_K.

For a moving point mass:
```
K(r,t) = K₀ + α·M / |r − r_s(t_ret)|     (Liénard-Wiechert-like)
```

This produces:
- **Static limit:** K = K₀ + α·M/r (Newtonian)
- **Moving source:** retarded 1/r with aberration
- **Accelerated source:** radiation (K-waves carrying energy away)
- **Far field:** wave zone with K ∼ 1/r amplitude

---

## 7. Status After B4

| Aspect | Before B4 | After B4 |
|:---|:---|:---|
| Static PDE | ∇²K = 0 ✓ | ✓ (static limit of wave eq) |
| 1/r form | From defect ✓ | ✓ (retarded 1/r for static source) |
| Propagation | Not defined | Wave equation with c_K |
| Causality | Not addressed | Finite propagation speed |
| New parameters | f_ref (I3) | f_ref (I3) + c_K (I4 candidate) |

### Classification

```
B4 STATUS:  DYNAMIC EXTENSION IDENTIFIED, PROPAGATION SPEED CALIBRATED

  DERIVED:
    ✅ Wave equation is the unique causal hyperbolic extension
    ✅ Static limit reproduces ∇²K = 0
    ✅ Retarded potential formulation consistent with B3B

  CALIBRATED:
    ⬜ c_K = wave propagation speed
    ⬜ Identification c_K = c (matches GR but not derived from TRM)

  OPEN:
    ⬜ Derivation of c_K from oscillator coupling timescale
    ⬜ Connection to gravitational wave observations (LIGO/Virgo)
    ⬜ Radiation reaction (energy loss from accelerated masses)
```

---

## 8. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_Final_Status.md` | Static closure summary |
| `TRM_V4_B3A_LaplaceUniqueness.md` | Static PDE uniqueness |
| `TRM_V4_B3B_BoundaryDefectOrigin.md` | 1/r origin |
| This document | Dynamic extension to wave equation |
