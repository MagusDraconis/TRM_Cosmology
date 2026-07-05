# TRM V4 — G1-T2d: Full Tensor 1PN Closure

**Date:** 2026-07-05
**Status:** DEFINITIVE — G1 closure. a_H = a_φ (derived). Full 1PN mixing coefficients require dedicated tensor algebra project. TENTATIVE: TENSION (β likely < 1).

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
