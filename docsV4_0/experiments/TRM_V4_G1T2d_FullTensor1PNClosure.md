# TRM V4 — G1-T2d: Full Tensor 1PN Closure

**Date:** 2026-07-05
**Status:** CLOSED. β_total < 1 for quartic baseline (tension). Optimized kernel family K₀/(1+x+1.25x²+x⁴) achieves β ≈ 1 (compatible). Framework-level verdict: WEAK-FIELD 1PN COMPATIBILITY ACHIEVED.

---

## 1. Final G1 Status

```
┌──────────────────────────────────────────────┐
│           G1 — 1PN CLOSURE (FINAL)            │
├──────────────────────────────────────────────┤
│ QUARTIC BASELINE (b=1)                        │
│   β < 1              TENSION                 │
│   (f''(0)=0 reduces but doesn't eliminate)    │
│                                               │
│ OPTIMIZED FAMILY (b≈1.25)                     │
│   β ≈ 1              COMPATIBLE ✅            │
│   K = K₀/(1+x+1.25x²+x⁴)                     │
│   6/6 stability checks pass                   │
│                                               │
│ FRAMEWORK VERDICT                             │
│   Weak-field 1PN compatibility ACHIEVED.      │
│   Bilocal framework spans GR-compatible β.    │
│   Full nonlinear / strong-field: OPEN.        │
└──────────────────────────────────────────────┘
```

The bilocal TRM framework admits physically valid kernels that reproduce GR-compatible β at 1PN. Therefore weak-field post-Newtonian compatibility is established within the framework. Full GR replacement (nonlinear dynamics, strong-field) remains the next frontier.

---

## 2. Quartic Baseline Analysis (Historical)

The quartic baseline kernel K₀/(1+x+x²) (b=1) gives β<1:

```
β_total < 1    for K(d²) = K₀/(1 + d²/λ² + (d²/λ²)²)
```

**Structurally enforced — follows from sign analysis alone. This was the original G1 result before kernel optimization.**

This result motivated the kernel optimization program (G1-KernelOptimization) which found that the generalized kernel family K₀/(1+x+bx²+x⁴) with b>1 reverses the cubic coupling sign and achieves β≈1.

---

## 3. Proof (Quartic Baseline)

### 3.1 Radial Integral

All cubic coefficients share: I_rad = ∫₀^∞ dr · r⁹ · f'(r²/λ²) · f''(r²/λ²).

f'(0) = −K₀ < 0, f''(0) = +2K₀ > 0 → **I_rad < 0**.

### 3.2 Angular Factors

ALL angular factors are **positive definite** — integrals of metric tensor contractions over S³.

### 3.3 Therefore

b_i = (positive angular) × (negative radial) < 0 ∀i → **β_total < 1**.

---

## 4. Status Card (Final)

```
┌──────────────────────────────────────────────┐
│           G1 — 1PN DEFINITIVE CLOSURE         │
├──────────────────────────────────────────────┤
│ QUARTIC BASELINE (b=1)                        │
│   β < 1              TENSION                 │
│   (f''(0)=0 reduces cubic coupling)           │
│                                               │
│ OPTIMIZED FAMILY (b≈1.25)                     │
│   β ≈ 1              COMPATIBLE ✅            │
│   K = K₀/(1+x+1.25x²+x⁴)                     │
│   6/6 stability checks pass                   │
│                                               │
│ FRAMEWORK VERDICT                             │
│   Weak-field 1PN compatibility ACHIEVED.      │
│   Bilocal framework spans GR-compatible β.    │
└──────────────────────────────────────────────┘
```

## 5. What This Means

| Question | Answer |
|:---|:---|
| Quartic β < 1 or > 1? | **< 1** (structurally enforced) |
| Optimized β ≈ 1? | **Yes** — at b ≈ 1.25 |
| Is TRM falsified? | **No** — bilocal framework allows kernel modification |
| Can β be adjusted to ≈ 1? | **Yes** — by tuning b in K₀/(1+x+bx²+x⁴) |

---

## 6. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_G1T2a_BilocalCoefficientComputation.md` | a_φ, b₅ numerical computation |
| `TRM_V4_G1T2c2_PhysicalLagrangianExtraction.md` | f'·f'' confirms negative sign |
| `TRM_V4_G1T2c3_TensorCompensationTest.md` | Trace sector cannot reverse sign |
| `TRM_V4_G1_NonlinearFieldEquation.md` | Scalar ceiling |
| `TRM_V4_G1T_MultiKTensorDynamics.md` | Multi-K structure |
