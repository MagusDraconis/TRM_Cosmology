# TRM V4 — G3: Strong-Field and Nonlinear Testbed

**Date:** 2026-07-05
**Status:** Framework initialized. Full strong-field solutions require nonlinear PDE solver (~multi-week project).
**Predecessors:** G1 (1PN closure), G2 (tensor bridge)

---

## 1. Static Spherically Symmetric Ansatz

### 1.1 Metric Form

For a static, spherically symmetric source:

```
ds² = −A(r)·dt² + B(r)·dr² + r²·dΩ²
```

In TRM: g_μν = η_μν + B_μν, with:

```
B_00 = 1 − A(r)     (time component)
B_rr = B(r) − 1     (radial component)
B_θθ = B_φφ = 0     (angular — absorbed in r²)
```

### 1.2 Effective Action in Spherical Symmetry

The bilocal action reduces to an effective 1D action for A(r), B(r). The field equations are:

```
G_μν[g] = 8πG·T_μν     (target — Einstein equations)
```

For TRM to reproduce Schwarzschild: A(r) = 1/B(r) = 1 − 2GM/r.

### 1.3 Horizon Condition

The event horizon occurs where g_00 = 0:

```
A(r_H) = 0  →  1 + B_00(r_H) = 0  →  B_00(r_H) = −1
```

For Schwarzschild: r_H = 2GM (the Schwarzschild radius).

In TRM, B_00(r) is determined by the kernel K(d²) through the effective field equations. Whether B_00 = −1 occurs at finite r depends on the nonlinear completion.

---

## 2. Nonlinear Solution Strategy

### 2.1 Self-Consistent Field Method

1. **Ansatz:** g_μν = η_μν + B_μν(r) with A(r), B(r)
2. **Compute d²(x,y):** geodesic distance in the trial metric
3. **Evaluate K(d²):** quartic-optimized kernel with b=1.25
4. **Compute effective T_μν[K]:** from the bilocal integral
5. **Solve G_μν = 8πG·T_μν:** iterate until convergence

### 2.2 Expected Behavior

| Regime | φ = GM/r | B_00 | Behavior |
|:---|:---|:---|:---|
| Weak-field | ≪ 1 | ≈ −2φ | Newtonian ✓ |
| Intermediate | ~0.1 | ≈ −0.2 | PPN corrections |
| Strong-field | ~0.5 | → −1? | Horizon? |
| Ultra-strong | > 1 | ? | Singularity? |

---

## 3. Horizon Formation

### 3.1 Critical Question

Does B_00(r) = −1 occur at finite r for the b=1.25 kernel?

This requires solving the nonlinear field equations. In GR, the horizon forms because g_00 = 1 − 2GM/r = 0 at r = 2GM. The Einstein equations guarantee this for any mass M.

In TRM, whether a horizon forms depends on the kernel shape. The quartic-optimized kernel may produce:
- **GR-like horizon:** B_00 → −1 at r_H = 2GM + corrections
- **No horizon:** B_00 remains > −1 for all r (regular — no black holes)
- **Different horizon structure:** B_00 = −1 at r_H ≠ 2GM

### 3.2 Redshift Divergence

Gravitational redshift: z = 1/√(A(r)) − 1. Diverges as A(r) → 0.

In TRM: z = 1/√(1 + B_00(r)) − 1. Same divergence if B_00 → −1.

---

## 4. Rotating Case (Kerr-like)

### 4.1 Metric Form

```
ds² = −(1 − 2GMr/Σ)·dt² − (4GMar·sin²θ/Σ)·dt·dφ + (Σ/Δ)·dr² + Σ·dθ² + (r²+a²+2GMa²r·sin²θ/Σ)·sin²θ·dφ²
```

where Σ = r² + a²·cos²θ, Δ = r² − 2GMr + a².

### 4.2 TRM Encoding

The off-diagonal term g_tφ is encoded in B_0φ (traceless tensor sector). From G2, Multi-K supports off-diagonal B_0i → frame-dragging.

The Kerr metric requires:
- B_00(r,θ): radial and polar dependence
- B_0φ(r,θ): off-diagonal frame-dragging
- B_rr(r,θ), B_θθ(r,θ): spatial metric

Multi-K (B_μν decomposition) has enough DOF (6 physical) to encode the full Kerr metric.

---

## 5. Nonlinear Wave Interactions

### 5.1 □K = 0 in Curved Background

For strong background fields, the wave equation becomes:

```
□_g K = 0     (curved-spacetime d'Alembertian)
```

Perturbations δK on a Schwarzschild-like background propagate on the effective metric. This produces:
- Quasinormal modes (ringdown)
- Gravitational lensing in strong field
- Photon sphere at r = 3GM

### 5.2 Consistency Check

The wave equation □K = 0 must be consistent with the background solution. If the background satisfies the static field equations, perturbations satisfy the linearized equations on that background — structurally guaranteed.

---

## 6. Classification

| Aspect | Status |
|:---|:---|
| Static spherical ansatz | **FRAMEWORK READY** |
| Schwarzschild-like solution | **OPEN** (requires nonlinear PDE solver) |
| Horizon formation | **OPEN** (depends on kernel behavior at strong field) |
| Kerr-like structure | **STRUCTURALLY SUPPORTED** (Multi-K has enough DOF) |
| Nonlinear wave consistency | **STRUCTURALLY GUARANTEED** (□_g K = 0 on background) |

### Honest Assessment

> **G3 is the strong-field frontier. The framework is set up. Full solutions require a nonlinear PDE solver — a multi-week computational project. The structural ingredients (DOF, gauge, tensor fields) are in place. Whether TRM produces GR-like black holes or regular horizonless objects depends on the kernel behavior in the strong-field regime — this is a genuine open question.**

---

## 7. Next Steps

1. Implement self-consistent field solver for static spherical case
2. Test horizon formation for b=1.25 kernel
3. If no horizon → TRM predicts regular compact objects (testable)
4. If GR-like horizon → TRM reproduces Schwarzschild (compatibility)
5. Extend to Kerr-like rotating solutions
