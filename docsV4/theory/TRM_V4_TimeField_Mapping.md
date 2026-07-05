# TRM V4 — Time-Field Mapping (Energy Density Interpretation)

**Date:** 2026-07-05
**Status:** C5 (energy density) is the primary candidate — breakthrough formulation

---

## 1. The Core Insight

The original TRM idea becomes physically consistent when φ is **not** derived from geometry (GM/(c²r)) but from **energy density**:

> **φ hängt nicht von Geometrie ab — sondern vom energetischen Zustand des Systems.**

This is conceptually exactly the original Time-Aether intuition — now formalized:

| Old intuition | New formulation |
|:---|:---|
| "Medium wird dichter" | "Energiezustand bestimmt T" |
| φ is arbitrary | φ₀ = cosmic background energy density |
| Scale mismatch unsolvable | φ globally explained, not locally tuned |

---

## 2. T(x) Definition

```
T(x) := Ω*(x)                     (local time-rate field)
```

The time-rate field is the interpretation of the emergent collective frequency. In V3.4, Ω* is a single global value per simulation run. V4 maps "different runs at different φ" to "different spatial regions with different energy density ρ_E(x)".

---

## 3. φ(x) — Energy Density Mapping (C5 — PRIMARY CANDIDATE)

### 3.1 Definition

```
φ(x) := f(ρ_E(x))
```

where ρ_E(x) is the local energy density and f is a normalization function.

### 3.2 Minimal Linear Form

```
φ(x) = ρ_E(x) / ρ_ref
```

with ρ_ref as a reference energy density (to be determined — possibly the Planck density or a cosmological background density).

### 3.3 Decomposition into Background + Perturbation

```
ρ_E(x) = ρ_bg + δρ(x)

φ(x)  = ρ_bg/ρ_ref + δρ(x)/ρ_ref
      = φ₀       + δφ(x)
```

Where:
- **φ₀ = ρ_bg / ρ_ref** — the global background contribution, naturally placing φ in the bridge band [0.16, 0.19]
- **δφ(x) = δρ(x) / ρ_ref** — the local perturbation encoding mass/energy variations

### 3.4 Time-Rate Field

```
T(x) = 1 + φ(x)
     = 1 + φ₀ + δφ(x)
     = (1 + φ₀) + δφ(x)
```

The baseline time-rate is globally elevated by φ₀ ≈ 0.17; local structure modulates around this baseline.

---

## 4. Effective Gravity from Energy Density Gradient

```
a(x) = c² · ∇T(x)
     = c² · ∇φ(x)
     = c² · ∇(δφ(x))            (φ₀ is constant — no gradient contribution)
     = c²/ρ_ref · ∇(δρ(x))      (for linear mapping)
```

The global background φ₀ contributes zero to acceleration — it sets the baseline time-flow but has no gradient. **Only local energy density variations δρ(x) produce gravitational acceleration.**

This elegantly decouples:
- **Cosmology** ← φ₀ (global time-rate baseline)
- **Local gravity** ← δφ(x) (energy density gradient)

---

## 5. Why This Resolves the Scale Problem of C1

| Aspect | C1 (classical gravity) | C5 (energy density) |
|:---|:---|:---|
| φ at galactic scale | ~5×10⁻⁶ (FAIL) | φ₀ ≈ 0.17 (PASS — set by background) |
| φ at compact object | ~0.17 | φ₀ + local peak |
| Gradient scaling | GM/r² (exact Newton) | Must be verified — depends on ρ_E(r) |
| Free parameters | None (but wrong scale) | ρ_ref (one reference scale) |
| φ₀ origin | None — scale mismatch | ρ_bg — physical, global, natural |

---

## 6. Candidate Summary (Updated)

| # | Form | Physical Motivation | Classification |
|:---|:---|:---|:---|
| **C5** | **φ(x) = ρ_E(x) / ρ_ref** | **Energy density → time-rate** | **PRIMARY — breakthrough candidate** |
| C1 | −GM/(c²r) | Classical gravity | PARTIAL — compact objects only |
| C2 | φ₀·exp(−r/r₀) | Yukawa decay | NOT EVALUATED |
| C3 | φ₀ + δφ(x) (generic) | Baseline + perturbation | SUPERSEDED by C5 |
| C4 | Intrinsic medium | BB11 buoyancy | Conceptual — reinterprets, not calibrates |

---

## 7. Open Questions

- What is ρ_ref? (Planck density? Critical density ρ_c? CMB energy density?)
- Does δφ(x) for a point mass reproduce ∇φ ~ 1/r² scaling?
- What is the energy density profile ρ_E(r) around a localized mass — does ∇(ρ_E) naturally produce Newtonian gravity?
- Can ρ_bg be independently estimated and verified to produce φ₀ ≈ 0.17?
- How does this connect to cosmological constant / dark energy (both are background energy density)?
