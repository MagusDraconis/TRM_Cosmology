# TRM V4 — G1-T2d: Full Tensor 1PN Closure

**Date:** 2026-07-05
**Status:** Framework set up. Full computation requires ~1 week dedicated project.
**Predecessors:** G1-T2c3 (trace sector → β < 1)

---

## 1. The Remaining Problem

The trace sector (B_μν = φ·η_μν) robustly gives β < 1. The traceless tensor H_μν may compensate through:

1. **Independent H-cubic coefficients** — H_μν has its own cubic self-coupling with potentially different sign
2. **φ-H mixing** — cross-terms ∝ φ·H·∂φ or φ·(∂H)² modify g_00 at O(U²)
3. **H sources g_00** — H_μν sourced by T_μν contributes to the Newtonian potential through the coupled equations

---

## 2. Coupled Field Equations

### 2.1 Decomposition

```
B_μν = φ·η_μν + H_μν
```

Effective action (quadratic + cubic):
```
S = ∫ d⁴x [ a_φ·(∂φ)² + a_H·(∂H)² + a_mix·∂φ·∂H
           + b_φφφ·φ·(∂φ)² + b_φφH·φ·∂φ·∂H + b_φHH·φ·(∂H)²
           + b_HHH·H·(∂H)² + b_Hφφ·H·(∂φ)² + ... ]
```

### 2.2 Field Equations (Linearized)

```
□φ  = −4πG·T^μ_μ / a_φ          (trace — Newtonian potential)
□H_μν = −8πG·(T_μν − ¼η_μν·T) / a_H   (traceless — GWs, frame-dragging)
```

### 2.3 1PN Corrections

The g_00 component to O(U²) receives contributions from:
```
g_00 = −1 + 2U − 2β·U²
     = −1 + B_00 − ¼B·η_00 + O(B²)
```

B_00 = φ·η_00 + H_00 = −φ + H_00

The H_00 contribution at O(U²) comes from two sources:
1. H_00 sourced by T_00 at linear order → contributes at O(U)
2. φ-H mixing terms at cubic order → contributes at O(U²)

---

## 3. What Determines β_total

The PPN β parameter is:

```
β_total = β_φ + Δβ_H
```

where:
- β_φ < 1 (trace sector — computed, negative cubic coupling)
- Δβ_H = contribution from H_μν (unknown — requires full computation)

Δβ_H depends on:
- The ratio a_H / a_φ (how strongly H is sourced relative to φ)
- The cubic mixing coefficients b_φφH, b_φHH, b_Hφφ
- The H self-coupling b_HHH

---

## 4. Computation Required

| Step | Status |
|:---|:---|
| Compute a_φ (trace kinetic) | ✅ G1-T2a |
| Compute b_φφφ (trace cubic) | ✅ G1-T2a, G1-T2c2 |
| Compute a_H (tensor kinetic) | ⬜ Angular factor differs from a_φ |
| Compute b_φφH, b_φHH, b_Hφφ (mixing) | ⬜ New angular integrals |
| Compute b_HHH (tensor self-coupling) | ⬜ New angular integrals |
| Solve coupled φ-H equations for point mass | ⬜ ODE system |
| Extract g_00 to O(U²) | ⬜ |
| Compute β_total | ⬜ |

---

## 5. Honest Assessment

The full tensor 1PN computation is a **~1 week dedicated project** requiring:
- 4 new angular integrals (a_H, b_mix, b_HHH)
- Solution of coupled nonlinear ODEs
- PPN parameter extraction

It is NOT a documentation task — it's a computational physics project.

### Classification

| Aspect | Status |
|:---|:---|
| Trace sector β | **COMPUTED** (β_φ < 1) |
| Tensor kinetic a_H | **PENDING** |
| φ-H mixing coefficients | **PENDING** |
| β_total | **PENDING** |
| Full 1PN closure | **OPEN** (~1 week project) |

### Verdict: OPEN

**The framework for full 1PN closure is mathematically well-defined. All required coefficients are computable integrals over the quartic kernel. The computation is deterministic — no free parameters, no GR input. It requires dedicated computational work, not conceptual breakthrough.**

---

## 6. What This Means for TRM

| Status | Meaning |
|:---|:---|
| If β_total ≈ 1 | TRM is GR-compatible at 1PN — major milestone |
| If β_total ≈ 0.095 | TRM is ruled out by Solar System (β_scalar survives) |
| If β_total is intermediate | TRM is in tension — constrains kernel or requires modification |

The quartic kernel is the current best candidate. If it fails at 1PN, the kernel shape can be modified (e.g., different denominator polynomial) to adjust β. The bilocal framework is flexible — the kernel is the input, PPN parameters are the output.
