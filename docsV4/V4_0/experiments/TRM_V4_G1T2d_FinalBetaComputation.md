# TRM V4 — G1-T2d-final: Direct β_total Computation

**Date:** 2026-07-05
**Status:** Attempted direct angular computation. Result: numerically unstable within simplified radial model. Sign confirmed (< 1). Exact value requires full tensor implementation.

---

## 1. Direct Angular Computation Attempt

### 1.1 Method

For B_μν = ε·diag(1,0,0,0), compute I(ε) = ∫ d⁴Δ K(d_E²(ε)) where d_E² = r² + ε·r²·cos²θ, and extract β from the ratio of cubic to quadratic Taylor coefficients.

### 1.2 Angular Averages (S³, d=4)

⟨cos²θ⟩ = 1/4, ⟨cos⁴θ⟩ = 1/8, ⟨cos⁶θ⟩ = 5/64.

### 1.3 Result

The simplified radial model (isotropic angular integration with ε-perturbation) gives β ≪ 1, consistent with the scalar-only result β_φ ≈ 0.095.

**However:** this model does NOT capture the full tensor degrees of freedom. The anisotropic components H_μν require non-isotropic angular integration that the simplified model cannot represent.

---

## 2. Why Direct Computation Fails at Current Infrastructure

| Limitation | Impact |
|:---|:---|
| Simplified radial model captures only trace sector | Misses H_μν contributions to g_00 |
| Isotropic ε-perturbation averages over tensor structure | Cannot distinguish b₁–b₅ |
| Full tensor requires anisotropic angular integration over 6-index tensor | Requires xTensor/Cadabra or equivalent |

---

## 3. What IS Definitively Known

```
β_total < 1    (structurally enforced — sign analysis)
β_φ ≈ 0.095    (scalar-only — computed numerically)
Exact β_total  PENDING (requires full tensor angular integration)
```

---

## 4. Classification

```
VERDICT: TENSION
β_total < 1 ≠ β_GR = 1 (structurally enforced)
Exact value pending (full tensor computation)
Framework survives via kernel modification
```
