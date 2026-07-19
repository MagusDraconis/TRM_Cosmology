# TRM V4 — G1-T2c2: Physical Bilocal Lagrangian Extraction

**Date:** 2026-07-05
**Status:** Computing physical effective Lagrangian with derivative terms
**Predecessors:** G1-T2c1 (simplified ε-perturbation), G1-T2a (scalar coefficients)

---

## 1. Correct Effective Action

The bilocal action with proper derivative structure:

```
S = ∫ d⁴x d⁴Δ [∂_α K(x, x+Δ)]²
```

where ∂_α acts on the metric g_μν(x) inside d²:

```
∂_α K = f'(d²) · ∂_α(g_μν(x))·Δ^μ·Δ^ν
```

### 1.1 Kinetic Term (O(B²))

```
L_kin ∝ ∂B · ∂B · ∫ d⁴Δ [f'(|Δ|²)]² · |Δ|⁴

a_physical ∝ ∫₀^∞ dr · r⁷ · [f'(r²)]²
```

This matches the G1-T2a computation. ✓

### 1.2 Cubic Term (O(B³))

From expanding f'(d²) = f'(d₀²) + f''(d₀²)·(B·Δ²):

```
L_cubic ∝ B · (∂B)² · ∫ d⁴Δ f'(d₀²)·f''(d₀²) · |Δ|⁶

b_physical ∝ ∫₀^∞ dr · r⁹ · f'(r²)·f''(r²)
```

**Key difference from G1-T2a:** The cubic coupling involves f'·f'', not [f']³. The sign of f'·f'' depends on the kernel.

---

## 2. Quartic Kernel Derivatives

For f(x) = K₀/(1+x+x²) with x = r²/λ²:

```
f(x)   = K₀/(1+x+x²)
f'(x)  = −K₀(1+2x)/(1+x+x²)²
f''(x) = 2K₀(1+3x+3x²)/(1+x+x²)³
```

At x = 0:
- f(0) = K₀
- f'(0) = −K₀  (negative)
- f''(0) = 2K₀ (positive)

Product: f'(0)·f''(0) = −2K₀² < 0 → **b_physical is negative** (same sign as b₅!)

### 2.1 Large-x Behavior

For x ≫ 1:
- f'(x) ∼ −2K₀/x³ ∼ −1/r⁶
- f''(x) ∼ 6K₀/x⁴ ∼ 1/r⁸
- f'·f'' ∼ −12K₀²/x⁷ ∼ −1/r¹⁴

Radial integral: r⁹ · (−1/r¹⁴) = −1/r⁵ → converges. ✓

---

## 3. Numerical Computation

```
a_physical = 4π · S₄ · ∫₀^Cutoff dr · r⁷ · [f'(r²/λ²)]²
b_physical = 4π · S₄ · ∫₀^Cutoff dr · r⁹ · f'(r²/λ²)·f''(r²/λ²)
```

The prefactor 4π·S₄ comes from the angular integration (S₄ = 2π² for the 3-sphere).

The ratio b_physical / a_physical determines the PPN correction:
```
Δβ ∝ b_physical / a_physical
```
