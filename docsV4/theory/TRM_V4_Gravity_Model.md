# TRM V4 — Gravity Model (Energy Density Interpretation)

**Date:** 2026-07-05
**Status:** C5 candidate — formulation complete, numerical verification pending

---

## 1. Model Overview

The C5 gravity model derives gravitational acceleration from a gradient in energy density, interpreted through the TRM time-rate field:

```
ρ_E(x)          energy density (local)
φ(x)            dimensionless time-rate offset
T(x) = 1 + φ(x) time-rate field
a(x)             effective gravitational acceleration
```

### 1.1 The Mapping Chain

```
ρ_E(x)  →  φ(x) = ρ_E(x)/ρ_ref  →  T(x) = 1 + φ(x)  →  a(x) = c² ∇T(x)
```

### 1.2 Decomposition

```
ρ_E(x) = ρ_bg + δρ(x)           (background + perturbation)
φ(x)   = φ₀   + δφ(x)           (φ₀ = ρ_bg/ρ_ref, δφ = δρ/ρ_ref)
T(x)   = 1 + φ₀ + δφ(x)         (global baseline + local modulation)
a(x)   = c²/ρ_ref · ∇(δρ(x))    (only perturbations produce gravity)
```

---

## 2. Key Parameters

| Parameter | Symbol | Meaning | Status |
|:---|:---|:---|:---|
| Reference energy density | ρ_ref | Normalization scale for φ | To be determined |
| Background energy density | ρ_bg | Cosmic background → φ₀ ≈ 0.17 | To be estimated |
| Local perturbation | δρ(x) | Mass/energy variations → local gravity | Model-dependent |

### 2.1 ρ_ref Candidates

| Candidate | Value (J/m³) | Notes |
|:---|:---|:---|
| Planck density | ~5×10¹¹³ | Too extreme — φ would be infinitesimal |
| Critical density ρ_c | ~5×10⁻¹⁰ | Cosmological — plausible scale |
| CMB energy density | ~4×10⁻¹⁴ | Radiation-dominated — too small |
| Nuclear density | ~10³⁵ | Intermediate — worth investigating |

### 2.2 φ₀ Calibration

```
φ₀ = ρ_bg / ρ_ref ≈ 0.17
```

This constrains the ratio ρ_bg / ρ_ref. The most natural candidate:

```
If ρ_ref = ρ_c (critical density ≈ 5×10⁻¹⁰ J/m³):
    ρ_bg ≈ 0.17 × ρ_c ≈ 8.5×10⁻¹¹ J/m³
```

This is of order ρ_c — cosmologically plausible.

---

## 3. Point Mass Case

### 3.1 Energy Density Profile

For a point mass M at the origin, the energy density perturbation is:

```
δρ(r) = M·c² · δ³(r)            (idealized point)
```

For a realistic distribution with characteristic radius R:

```
δρ(r) = M·c² / V(R) · f(r/R)    (V(R) = normalization volume)
```

### 3.2 Effective Acceleration

```
a(r) = c²/ρ_ref · ∇(δρ(r))
```

For the idealized point mass, the gradient must be regularized. For a smoothed distribution, the acceleration in the far field (r ≫ R) should approach:

```
a(r) → G·M/r²                   (Newtonian limit)
```

### 3.3 Newtonian Limit Condition

For C5 to reproduce Newtonian gravity, the energy density perturbation around a point mass must satisfy:

```
c²/ρ_ref · d(δρ)/dr = G·M/r²    (far field)
```

This implies:

```
δρ(r) = −(ρ_ref·G·M) / (c²·r)   (integrated form)
```

In other words: **the energy density perturbation must fall off as ~1/r around a point mass.** This is a testable prediction of the C5 model — does a realistic mass distribution naturally produce δρ ~ 1/r?

---

## 4. Comparison with Standard Gravity

| Aspect | Newton / GR | TRM V4 C5 |
|:---|:---|:---|
| Source field | Mass density ρ_m | Energy density ρ_E |
| Potential | Φ = −GM/r | φ = ρ_E / ρ_ref |
| Field equation | ∇²Φ = 4πGρ_m | ∇²φ ∝ ρ_E (if linear) |
| Acceleration | a = −∇Φ | a = c² ∇φ = c²/ρ_ref ∇ρ_E |
| Newtonian limit | Built-in | Requires δρ ~ 1/r (to be verified) |
| Cosmological constant | Λ (added by hand) | φ₀ = ρ_bg/ρ_ref (built-in) |

---

## 5. Predictions

### P-C5-1 — 1/r Energy Density Profile

**Statement:** For a localized mass M, the energy density perturbation δρ(r) must fall off as ~1/r at large distances to reproduce Newtonian gravity.

**Test:** Numerical simulation of δρ(r) around a mass distribution. Compare with ~1/r.

### P-C5-2 — φ₀ as Cosmological Background

**Statement:** The bridge-band baseline φ₀ ≈ 0.17 corresponds to a cosmic background energy density ρ_bg ≈ 0.17 × ρ_ref.

**Test:** Identify ρ_ref. Estimate ρ_bg from independent cosmological data (CMB, ΛCDM parameters). Check consistency.

### P-C5-3 — No Dark Matter Needed for φ₀

**Statement:** The elevated time-rate baseline φ₀ is a global property, not a local mass effect. It does not require dark matter halos.

**Test:** Compare TRM galaxy rotation curves (already computed SPARC) with the interpretation that φ₀ replaces the need for dark matter at cosmological background scales.

---

## 6. Open Questions

1. **ρ_ref identification:** Which physical energy density scale sets the normalization?
2. **∇(δρ) scaling:** Does a realistic mass/energy distribution produce δρ ~ 1/r?
3. **Connection to E=mc²:** Should ρ_E include rest-mass energy (ρ_E = ρ_m·c² + ...) or only field/interaction energy?
4. **Nonlinear regime:** Is the mapping ρ_E → φ strictly linear, or are there saturation effects?
5. **Quantum origin:** Does the oscillator lattice provide a microscopic derivation of ρ_E?

---

## 7. Status

| Aspect | Status |
|:---|:---|
| Conceptual framework | COMPLETE |
| Consistency checks (E1, I1, I2) | PASS in principle |
| Numerical verification C5.1–C5.4 | PENDING |
| ρ_ref identification | PENDING |
| Newtonian limit proof | PENDING — requires δρ ~ 1/r verification |
