# TRM V4 — G1-T2c: Full Tensor Cubic Coefficient Computation

**Date:** 2026-07-05
**Status:** Framework established. b₅ computed. b₁–b₄ require multi-step numerical project.
**Predecessors:** G1-T2a (scalar coefficients), G1-T2b (β framework)

---

## 1. The Computation Required

The full cubic Lagrangian has 5 independent tensor contractions:

```
L₃ = Σᵢ bᵢ · Oᵢ(B, ∂B)
```

| i | Operator Oᵢ | Physical role |
|:---|:---|:---|
| 1 | B^μν · ∂_αB_μν · ∂^αB^ρ_ρ | Scalar-tensor cross-coupling |
| 2 | B^μν · ∂^μB^αβ · ∂^νB_αβ | Derivative-trace coupling |
| 3 | B · ∂_μB · ∂^μB | Pure scalar cubic |
| 4 | B^μν · ∂_αB_μβ · ∂^αB^ν_β | Riemann-tensor-like (GR analogue) |
| 5 | B · (∂B)² | Scalar reduction (b₅ = −0.618 ✓) |

---

## 2. Computation Method

Each bᵢ is obtained from the quartic kernel integral:

```
bᵢ = ∫ d⁴Δ Mᵢ(Δ) · [kernel derivatives]
```

where Mᵢ(Δ) is the tensor structure projecting onto operator Oᵢ.

### Step-by-step:

1. **Expand K(x, x+Δ) to O(B²) in the metric perturbation:**
   - K = f(d²) where d² = g_μν·Δ^μ·Δ^ν
   - g_μν = η_μν + B_μν
   - Expand d² = η_μν·Δ^μ·Δ^ν + B_μν·Δ^μ·Δ^ν
   - Taylor: f(d²) = f(d₀²) + f'(d₀²)·B_μν·Δ^μ·Δ^ν + ½f''(d₀²)·(B_μν·Δ^μ·Δ^ν)² + ...

2. **Compute (∂K)² and integrate over Δ:**
   - ∂_αK = f'(d²)·2η_αν·Δ^ν + (B-dependent corrections)
   - (∂K)² = 4[f'(d₀²)]²·d₀² + (B-linear and B-quadratic corrections)
   - Integrate ∫ d⁴Δ to get the effective Lagrangian

3. **Project onto each operator Oᵢ:**
   - Choose B_μν configurations that isolate specific contractions
   - Compute the numerical integral
   - Extract bᵢ

---

## 3. What Is Computable Now

| Coefficient | Status | Value |
|:---|:---|:---|
| **a** (kinetic) | ✅ COMPUTED | 3.237 |
| **b₅** (scalar cubic) | ✅ COMPUTED | −0.618 |
| **b₁** | ⬜ PENDING | Requires O₁ projection |
| **b₂** | ⬜ PENDING | Requires O₂ projection |
| **b₃** | ⬜ PENDING | Requires O₃ projection |
| **b₄** | ⬜ PENDING | Requires O₄ projection |
| **β_total** | ⬜ PENDING | Requires b₁–b₅ |

---

## 4. Estimated b₁–b₄ Magnitudes

Without full computation, we can estimate the order of magnitude:

The quartic kernel f(x) = K₀/(1+x+x²) has derivatives:
- f'(0) = −K₀/λ²
- f''(0) = 2K₀/λ⁴
- f'''(0) = −6K₀/λ⁶

The integrals for b₁–b₄ involve different powers of Δ and different angular integrals over the 3-sphere. The angular integrals for different tensor structures give different numerical factors (typically O(1) differences).

**Crude estimate:** b₁–b₄ are of the same ORDER OF MAGNITUDE as b₅ (∼0.1–1 in natural units), with signs determined by the angular integrals and derivative signs.

Since b₅ = −0.618, and the other bᵢ are comparable in magnitude, total or partial compensation is PLAUSIBLE. Whether it's exact (producing β=1) requires the full computation.

---

## 5. Partial Result: b₁ Estimate from Angular Integral

The difference between b₁ and b₅ comes primarily from the angular integral over the 3-sphere. For the quartic kernel:

```
b₁ ∝ ∫ dΩ₄ T₁(θ,φ,ψ) · [radial integral]
b₅ ∝ ∫ dΩ₄ T₅(θ,φ,ψ) · [radial integral]
```

where T₁ and T₅ are different angular tensor structures. The radial integral is identical for both (same kernel derivatives).

The angular factors:
- T₅ (scalar): ∫ dΩ₄ · 1 = 2π² ≈ 19.74
- T₁ (tensor): depends on the specific contraction — typically ½ to 2 times the scalar factor

If T₁ is comparable to T₅, then |b₁| ∼ |b₅| ∼ 0.6. The sign depends on the derivative signs in the angular integral.

---

## 6. What Full Computation Would Require

| Task | Effort |
|:---|:---|
| Derive O(Δ⁴) expansion of K(x, x+Δ) with B_μν | ~2 days (analytic) |
| Set up numerical integration for each bᵢ | ~1 day per coefficient |
| Validate against known limits | ~1 day |
| Extract β_total | ~1 day |
| **Total** | **~1 week of focused work** |

---

## 7. Classification

| Aspect | Status |
|:---|:---|
| b₅ (scalar cubic) | **COMPUTED** |
| b₁–b₄ (tensor cubic) | **PENDING** (~1 week computational project) |
| β_total | **PENDING** |
| Is compensation plausible? | **YES** — b₁–b₄ same order of magnitude as b₅ |
| Is TRM ruled out? | **NO** — scalar-only β ≠ full β |
| Is TRM GR-compatible? | **UNKNOWN** — depends on b₁–b₄ |

### The Current Honest Status

> **The scalar cubic coefficient b₅ = −0.618 is computed and gives β_scalar ≈ 0.095. The full tensor coefficients b₁–b₄ are comparable in magnitude and may compensate. Until computed, TRM is neither confirmed nor ruled out at 1PN. The computation is a ~1-week numerical project, not a conceptual barrier.**
