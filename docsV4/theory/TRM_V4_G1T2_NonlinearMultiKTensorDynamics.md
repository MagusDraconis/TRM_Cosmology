# TRM V4 — G1-T2: Nonlinear Multi-K Tensor Dynamics

**Date:** 2026-07-05
**Status:** Constructing nonlinear field equations for B_μν — honest about derivation depth
**Predecessors:** G1-T (Multi-K structure), G1 (scalar ceiling), G2 (tensor bridge)

---

## 1. Field Content

From G1-T, the coincidence expansion of K(x,y) gives:

```
K(x, x+Δ) = K₀ + ½B_μν(x)·Δ^μ·Δ^ν + O(Δ³)
```

Decompose B_μν:
```
B_μν = φ·η_μν + H_μν
```
where φ = B^μ_μ/4 (scalar trace) and H_μν is traceless (H^μ_μ = 0).

The effective metric on flat background:
```
g_μν = η_μν + B_μν = (1+φ)·η_μν + H_μν
```

---

## 2. Candidate Field Equations

### 2.1 Candidate A — Linearized Einstein Form (EFFECTIVE)

**Equations:**
```
□φ = −4πG·ρ                    (scalar — Newtonian limit)
□H_μν = −16πG·S_μν              (tensor — GWs with source)
∂^μ H_μν = 0                    (Lorenz gauge)
```

**What it reproduces:**
- Newtonian gravity ✓
- Linear GWs with 2 tensor modes ✓
- Gauge structure ✓

**What it assumes:** The form of the source terms (G, 16πG) is calibrated to match GR. The PDE structure (wave equation) follows from □K = 0 (B4).

**Classification: EFFECTIVE.** Matches linearized GR by construction. No derivation of the source coefficients from TRM.

---

### 2.2 Candidate B — Einstein-Hilbert from B_μν Action (CALIBRATED)

**Action:**
```
S = (1/16πG) ∫ d⁴x √−g(B) · R(g(B))
```
where g_μν = η_μν + B_μν and R is the Ricci scalar.

Expanding to quadratic order:
```
S² = ∫ d⁴x [¼(∂H)² − ½(∂φ)² + ...]
```

The field equations from δS = 0:
```
G_μν[η + B] = 8πG·T_μν
```

**What this gives:** By construction, the full Einstein equations for g_μν = η_μν + B_μν.

**What it assumes:** The Einstein-Hilbert action is POSTULATED for the effective metric. No derivation from TRM oscillator dynamics.

**Classification: ASSUMED (GR limit).** This is the target, not the derivation. Shows what Multi-K MUST reproduce, not what it DOES reproduce from TRM first principles.

---

### 2.3 Candidate C — Bilocal Action (PROMISING, OPEN)

**Approach:** Construct an action directly from K(x,y) without assuming GR.

**Action ansatz:**
```
S[K] = ∫ d⁴x d⁴y √−g(x)√−g(y) · L(K, ∂K, ...)
```

The simplest invariants built from K(x,y):
1. Coincidence limit: K(x,x) — scalar (cosmological constant-like)
2. Two-point kinetic: ∫ d⁴y (□_x K(x,y))² — wave dynamics
3. Self-interaction: ∫ d⁴y K(x,y)·(∂K)² — nonlinear coupling

Expanding around coincidence using the G1-T expansion:
```
K(x, x+Δ) = K₀ + ½B_μν·Δ^μ·Δ^ν
```

The bilocal action reduces to a local effective action for B_μν:
```
S_eff[B] = ∫ d⁴x [a·(∂B)² + b·B·(∂B)² + c·B² + ...]
```

The coefficients a, b, c are determined by integrals over the kernel shape f(d²):
```
a ∝ ∫ d⁴Δ f'(d²)² · |Δ|⁴     (kinetic term)
b ∝ ∫ d⁴Δ f'(d²)³ · |Δ|⁶     (cubic term)
c ∝ ∫ d⁴Δ f(d²) · |Δ|²        (mass term = 0 for quartic kernel)
```

**What this gives:** A systematic expansion of the bilocal action in powers of B_μν. The coefficients are determined by the kernel shape — NOT free parameters.

**What's open:** Computing the coefficients for the quartic kernel and checking whether they match the Einstein-Hilbert expansion coefficients.

**Classification: PROMISING.** This is the path to DERIVING the field equations from the bilocal structure — no GR input needed. But the computation is incomplete.

---

## 3. Post-Newtonian Limit

### 3.1 PPN Parameters

The Parameterized Post-Newtonian (PPN) formalism characterizes deviations from GR:

| PPN Parameter | GR Value | What it measures |
|:---|:---|:---|
| γ | 1 | Spatial curvature per unit mass |
| β | 1 | Nonlinearity in superposition |

For Candidate C (bilocal action), γ and β are determined by the coefficients a, b, c:
```
γ = (coefficient of spatial ∂²φ term) / (coefficient of temporal ∂²φ term)
β = (coefficient of φ·∇²φ term) / (γ·(coefficient of (∇φ)² term))
```

Computing these from the bilocal action would be a genuine TRM prediction.

### 3.2 Frame-Dragging (Kerr Limit)

The off-diagonal components B_0i encode gravitomagnetism. In the weak-field slow-motion limit:
```
□B_0i = −16πG·ρ·v_i          (gravitomagnetic Poisson equation)
```

This is structurally identical to GR. The Lense-Thirring precession and frame-dragging follow.

---

## 4. Kerr Metric from Multi-K

A rotating source produces B_0i ≠ 0 via the gravitomagnetic equation. The effective metric:
```
ds² = −(1+2φ)dt² + 2B_0i·dt·dx^i + (1−2ψ)δ_ij·dx^i·dx^j
```

For a rotating point mass, the Kerr metric in weak-field:
```
B_0φ ≈ −2G·J·sin²θ / r          (frame-dragging term)
```

Multi-K can encode this through the off-diagonal B_0i — a feature impossible in scalar K.

---

## 5. Summary

| Candidate | Classification | What it achieves |
|:---|:---|:---|
| **A — Linearized Einstein** | EFFECTIVE | Matches linearized GR (calibrated coefficients) |
| **B — Einstein-Hilbert action** | ASSUMED | Full GR by postulate — target, not derivation |
| **C — Bilocal action** | PROMISING ⭐ | Expansion coefficients from kernel shape — path to derivation |

### Candidate C Status

```
DERIVED:
  ✅ B_μν from K(x,y) expansion                          (G1-T)
  ✅ Bilocal action S[K] exists                          (structural)
  ✅ Effective action S_eff[B] from coincidence expansion (structural)

CALIBRATED / OPEN:
  ⬜ Coefficients a, b, c from quartic kernel integrals   (computable)
  ⬜ PPN parameters γ, β from bilocal action              (follows from a,b,c)
  ⬜ Match to GR values γ=1, β=1                        (requires computation)

PROMISING:
  Candidate C is the path from TRM first principles to GR.
  No GR input needed — coefficients from kernel shape.
  But the computation is incomplete and non-trivial.
```

---

## 6. What Would Close G1-T2

| Milestone | Status |
|:---|:---|
| Compute a, b, c for quartic kernel | OPEN — requires numerical integration over d⁴Δ |
| Derive PPN γ, β from a, b, c | OPEN — follows from coefficient computation |
| Compare with GR values γ=1, β=1 | OPEN |
| If match → Multi-K DERIVES linearized GR | **This would close G1** |
| If mismatch → Multi-K predicts PPN deviations | **Falsifiable prediction** |

### The Honest Status

> **Multi-K provides the structural infrastructure (DOF, gauge, tensor fields). Candidate C (bilocal action) provides the path to deriving the field equations from the kernel shape. The remaining step — computing the effective action coefficients from the quartic kernel — is a concrete computational task, not a conceptual gap.**

---

## 7. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_G1T_MultiKTensorDynamics.md` | Multi-K structure |
| `TRM_V4_G1_NonlinearFieldEquation.md` | Scalar ceiling |
| `TRM_V4_G2D_FullLorentzianClosure.md` | Quartic kernel |
| This document | Nonlinear Multi-K field equations |
