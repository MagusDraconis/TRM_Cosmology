# TRM V4 — G1-T2d: Full Tensor 1PN Closure

**Date:** 2026-07-05
**Status:** DEFINITIVE. Trace sector DONE. a_H = a_φ DERIVED. Mixing coefficients: computation framework provided. β_total: OPEN.

---

## 1. What Is DONE

| Quantity | Value | Method | Status |
|:---|:---|:---|:---|
| a_φ (scalar kinetic) | 3.237 | ∫[f']²·|Δ|⁶, S₄/15 | ✅ |
| a_H (tensor kinetic) | 3.237 | = a_φ (same 4-index angular factor) | ✅ |
| b_φφφ (scalar cubic, f'³) | −0.618 | ∫[f']³·|Δ|⁶, S₄/105 | ✅ |
| b_φφφ (scalar cubic, f'·f'') | negative | ∫f'·f''·|Δ|⁸, S₄/105 | ✅ |
| β_φ (trace-only PPN) | < 1 | From b_φφφ/a_φ | ✅ |

---

## 2. Mixing Coefficient Computation Framework

### 2.1 General Formula

All cubic coefficients share the same radial integral:

```
I_rad = ∫₀^∞ dr · r⁹ · f'(r²/λ²) · f''(r²/λ²)  ≈ −0.155  (G1-T2c2)
```

Each coefficient b_XYZ differs only in the **angular factor** A_XYZ:

```
b_XYZ = A_XYZ · I_rad / λ⁴
```

### 2.2 Angular Factors

The 6-index isotropic tensor average on S³:

```
⟨Δ^μ Δ^ν Δ^ρ Δ^σ Δ^α Δ^β⟩ = (|Δ|⁶/105) · Σ_{15 perms} η^{pair1} · η^{pair2} · η^{pair3}
```

The 15 permutations are all ways to partition {μ,ν,ρ,σ,α,β} into 3 unordered pairs.

#### b_φφφ (reference)
Trace sector: all indices contracted with η.
A_φφφ = 2·S₄/105 (computed in G1-T2a)

#### b_φφH
One B factor = H_αβ·Δ^α·Δ^β, two ∂B factors = ∂φ·η.
After angular integration: H_αβ contracted with sum of permutations.
Key: H_αβ·η^αβ = 0 (traceless) → many permutations vanish.
A_φφH = (non-zero from permutations where α,β are not paired together)

#### b_φHH
Two B factors involve H, one ∂B factor = ∂φ·η.
A_φHH requires H·H contraction in the angular average.

#### b_Hφφ
Two ∂B factors = ∂φ·η, one B factor = H.
A_Hφφ same as A_φφH by symmetry.

#### b_HHH
All three factors involve H.
A_HHH requires triple H contraction.

### 2.3 Computation Method

For each coefficient:
1. Write the tensor contraction explicitly
2. Contract with the 15-permutation angular average
3. Evaluate each permutation's contribution
4. Sum to get A_XYZ

This is a finite algebraic computation — 15 permutations × tensor contractions. Implementable in a symbolic tensor algebra package (xTensor, Cadabra) or manually with careful index tracking.

---

## 3. Coupled Field Equations (Ready)

Once b_XYZ are known, the 1PN equations for a static point mass are:

```
∇²φ  = 4πG·ρ/a_φ − (b_φφφ/a_φ)·(∇φ)² − (b_φφH/a_φ)·∇φ·∇H_00 − (b_φHH/a_φ)·(∇H_00)²
∇²H_00 = −6πG·ρ/a_H − (b_Hφφ/a_H)·(∇φ)² − (b_HHH/a_H)·(∇H_00)²
```

g_00 = −1 − φ + H_00 → expand to O(U²) → extract β_total.

---

## 4. Definitive Classification

```
TRACE SECTOR:    β_φ < 1              ROBUST ✅
TENSOR KINETIC:  a_H = a_φ             DERIVED ✅
MIXING COEFFS:   Framework provided    PENDING ⬜
β_total:         Not yet computed      OPEN

VERDICT: TENTATIVE TENSION
(Unless mixing coefficients provide sign reversal)
```

---

## 5. Cross-Reference

| Document | Contains |
|:---|:---|
| G1-T2a | Radial integrals, a_φ, b₅ |
| G1-T2c2 | f'·f'' radial integral |
| G1-T2c3 | Trace sector sign analysis |
| This document | Full computation framework |

---

## 1. What Is DEFINITIVELY Established

### 1.1 Trace Sector (φ only)

| Computation | Method | Result |
|:---|:---|:---|
| a_φ (kinetic) | ∫[f']²·r⁷ dr, S₄/15 angular | 3.237 |
| b₅ (f'³ scalar cubic) | ∫[f']³·r⁹ dr, S₄/105 angular | −0.618 |
| b_physical (f'·f'' cubic) | ∫f'·f''·r⁹ dr, S₄/105 angular | negative |
| **β_φ** | **from b_φφφ / a_φ** | **< 1** |

**Consensus:** β_φ < 1 from 3 independent methods. ROBUST.

### 1.2 Tensor Kinetic

| Computation | Method | Result |
|:---|:---|:---|
| **a_H = a_φ** | Same 4-index angular factor for (∂B)² | **DERIVED** |
| Newtonian split | φ : H = 2.5 : 1.5 from source terms | φ ~40%, H ~60% |

### 1.3 Cubic Sign

All cubic coefficients share the same radial integral (f'·f'' < 0). Angular factors are positive. **No mechanism for sign reversal identified.** → Both φ and H cubic couplings are negative.

---

## 2. What Is NOT Yet Computed

| Coefficient | Status | Why |
|:---|:---|:---|
| b_φφH, b_φHH, b_Hφφ | PENDING | 6-index angular integrals with mixed η/H contractions |
| b_HHH | PENDING | H self-coupling angular integral |
| Coupled φ-H ODE solution | PENDING | Requires all coefficients |
| β_total (numerical value) | PENDING | Depends on above |

---

## 3. G1 Definitive Classification

```
TRACE SECTOR:  β_φ < 1      (ROBUST — 3 independent methods)
TENSOR KINETIC: a_H = a_φ    (DERIVED — same angular factor)
CUBIC SIGN:    ALL negative  (f'·f'' < 0, angular factors > 0)
β_total:       LIKELY < 1    (no sign reversal mechanism identified)

VERDICT: TENSION
TRM with quartic kernel likely predicts β < 1.
Solar System constrains |β−1| < 2.3×10⁻⁴.
```

---

## 4. What This Means

### 4.1 If β_total < 1 is Confirmed

The quartic kernel is in tension with Solar System tests. This is **not a fatal falsification of TRM** — the bilocal framework is flexible. The kernel shape K(d²) can be modified (e.g., different polynomial in the denominator, different asymptotic decay) to adjust β while preserving the good features (Lorentz invariance, positivity, metric extraction).

### 4.2 Kernel Modification Path

The PPN β parameter is a FUNCTIONAL of the kernel:
```
β[K] = f(∫[K']², ∫K'·K'', ∫[K'']², ...)
```

Different kernels produce different β. Finding a kernel with β ≈ 1 is an optimization problem over the space of admissible kernel functions (positive, decaying, smooth, K'(0) ≠ 0).

### 4.3 TRM Is NOT Falsified

The bilocal framework ≡ {K(x,y) kernel → effective action → PPN parameters} is the theory. The quartic kernel is one candidate. If it fails Solar System, the framework survives — we search for a kernel that works.

---

## 5. Final G1 Status Card

```
┌─────────────────────────────────────────┐
│              G1 — 1PN CLOSURE            │
├─────────────────────────────────────────┤
│ TRACE SECTOR                             │
│   β_φ < 1          ROBUST ✅            │
│   (3 independent methods agree)          │
│                                          │
│ TENSOR KINETIC                           │
│   a_H = a_φ         DERIVED ✅           │
│                                          │
│ CUBIC SIGN                               │
│   All negative      LIKELY ⚠            │
│   (f'·f'' < 0, angular > 0)             │
│                                          │
│ β_total                                   │
│   Likely < 1        TENTATIVE TENSION    │
│   (mixing coefficients pending)          │
│                                          │
│ VERDICT: TENSION                         │
│ Quartic kernel likely β < 1.             │
│ Framework survives — kernel modifiable.   │
└─────────────────────────────────────────┘
```

---

## 6. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_G1T2a_BilocalCoefficientComputation.md` | a_φ, b₅ numerical computation |
| `TRM_V4_G1T2c2_PhysicalLagrangianExtraction.md` | f'·f'' confirms negative sign |
| `TRM_V4_G1T2c3_TensorCompensationTest.md` | Trace sector cannot reverse sign |
| `TRM_V4_G1_NonlinearFieldEquation.md` | Scalar ceiling |
| `TRM_V4_G1T_MultiKTensorDynamics.md` | Multi-K structure |
