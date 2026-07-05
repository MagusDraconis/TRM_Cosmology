# TRM V4 — G1-T2c1: Tensor Cubic Coefficient Execution

**Date:** 2026-07-05
**Status:** Numerical computation of b₁ via specific B_μν configuration
**Predecessors:** G1-T2c (framework), G1-T2a (b₅ computed)

---

## 1. Approach

Rather than computing all 5 coefficients independently, we compute the full cubic effective Lagrangian for specific B_μν configurations and extract the total cubic coupling to the trace sector (which determines PPN β).

### Method

1. Choose B_μν = ε · diag(1, 0, 0, 0) — pure time-time perturbation
2. Compute the bilocal effective action to O(ε³) by numerical Δ-integration
3. Extract the total cubic coefficient b_total for the trace sector
4. From b_total, compute β_total (the physical PPN parameter)

### Why this works

The PPN β parameter depends only on the g_00 component to O(U²). A pure time-time perturbation B_00 probes exactly the combination of b₁–b₅ that contributes to β. We don't need individual b₁–b₄ — we need their combined effect on g_00.

---

## 2. Computation

### 2.1 Setup

For B_μν = ε · diag(1, 0, 0, 0), the effective metric is:
```
g_μν = η_μν + ε · δ_μ⁰·δ_ν⁰
```

The interval d² in the perturbed metric:
```
d² = η_μν·Δ^μ·Δ^ν + ε·(Δ⁰)²
    = −(Δt)² + |Δx|² + ε·(Δt)²
    = −(1−ε)·(Δt)² + |Δx|²
```

In Euclidean (Wick-rotated t→iτ):
```
d_E² = (1−ε)·τ² + |Δx|²
```

### 2.2 Kernel in Perturbed Metric

```
K(d_E²) = K₀ / (1 + d_E²/λ² + (d_E²)²/λ⁴)
```

### 2.3 Effective Lagrangian

The effective Lagrangian at x is the integral over Δ:
```
L_eff(ε) = ∫ d⁴Δ [K'(d_E²)]² · d_E²     (kinetic term, simplified)
```

Actually, the full effective action is more complex. Let me compute the simplest meaningful quantity: the ε-dependence of ∫ d⁴Δ K(d_E²), which gives the cosmological constant and its ε-dependence. Then expand to O(ε³).

The derivative terms (kinetic, cubic with derivatives) come from ∫ d⁴Δ [K']² · d_E² and its ε-expansion.

For the PPN β, what matters is the term ∝ ε³ in the effective Lagrangian. The ratio of this to the ε² (kinetic) term determines β.

### 2.4 Numerical Computation

We compute I(ε) = ∫ d⁴Δ K(d_E²(ε)) for small ε and extract the Taylor coefficients:

```
I(ε) = I₀ + I₁·ε + I₂·ε² + I₃·ε³ + ...
```

I₀: cosmological constant (irrelevant — renormalized)
I₁: tadpole (vanishes for symmetric kernel in flat space → 0)
I₂: kinetic term → gives a
I₃: cubic term → gives b_total

From I₂ and I₃:
```
b_total / a = I₃ / I₂   (up to numerical factors from tensor structure)
β_total = 1 + κ · (b_total / a)
```
where κ is a numerical factor from the PPN reduction (~−½ for the scalar sector).

### 2.5 Expected Outcome

If b_total < 0 (same sign as b₅): β_total < 1, tension with GR.
If b_total ≈ 0: β_total ≈ 1, GR-compatible — tensor compensation successful!
If b_total > 0: β_total > 1, different tension.
