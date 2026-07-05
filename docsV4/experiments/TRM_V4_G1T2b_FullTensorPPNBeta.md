# TRM V4 — G1-T2b: Full Tensor PPN Beta Extraction

**Date:** 2026-07-05
**Status:** Framework for full tensor β — honest about what's computed vs pending
**Predecessors:** G1-T2a (b = −0.618 from scalar reduction), G1-T2 (Candidate C)

---

## 1. The Problem

G1-T2a computed a, b from the scalar reduction of the bilocal action. b < 0 suggests β < 1. But the full Multi-K action has multiple tensor contractions beyond the scalar trace. These could:
- Cancel the scalar b contribution → β ≈ 1 (GR-compatible)
- Enhance it → β ≪ 1 (tension with Solar System)
- Leave it unchanged → β from scalar b/a is the full result

---

## 2. Tensor Decomposition of the Effective Action

### 2.1 Field Content

```
B_μν = φ·η_μν + H_μν
```
- φ = B^μ_μ/4 — scalar trace (1 DOF)
- H_μν — traceless symmetric tensor (9 DOF)
- Gauge fixing removes 4 DOF → 6 physical

### 2.2 Kinetic Term

```
(∂B)² ≡ ∂_α B_μν · ∂^α B^μν
      = 4·(∂φ)² + (∂H)²
```

Scalar and tensor kinetic terms decouple (H is traceless → cross term η^μν·H_μν = 0).

### 2.3 Cubic Term — Multiple Contractions

The most general cubic term built from B_μν and ∂B is:

```
L_3 = b₁·B^μν·∂_α B_μν·∂^α B^ρ_ρ              (trace-trace-trace)
    + b₂·B^μν·∂_α B_μρ·∂^α B_ν^ρ               (Riemann-like)
    + b₃·B^μν·∂_μ B^αβ·∂_ν B_αβ                 (derivative on trace)
    + b₄·B^μν·∂_μ B_να·∂_β B^αβ                  (divergence coupling)
    + b₅·B·(∂B)²                                  (scalar reduction — what G1-T2a computed)
```

**The single b from G1-T2a is only b₅.** The full theory has b₁–b₅, each computed from different moments of the quartic kernel.

### 2.4 Scalar Sector Reduction

Keeping only φ (trace) terms:
```
B_μν → φ·η_μν
(∂B)² → 4(∂φ)²
B·(∂B)² → φ·η_μν · (4(∂φ)²) · (contraction)
```

The contraction structure for the scalar sector:
```
η^μν · η_μν = 4     (trace of identity in 4D)
```

So the scalar cubic term (b₅ contribution):
```
L_3^scalar = b₅ · 4 · φ · (∂φ)²
```

The G1-T2a computation gave b₅ = −0.618. This is ONE of the five coefficients.

---

## 3. PPN Beta from Scalar Sector (Partial)

### 3.1 Static Field Equation (Scalar Only)

From S = ∫ [4a(∂φ)² + 4b₅·φ·(∂φ)²]:

```
δS/δφ = 0 → 8a·□φ − 4b₅·(∂φ)² − 8b₅·φ·□φ = 8πG·ρ
```

In the static limit (□φ → −∇²φ):
```
8a·∇²φ + 4b₅·(∇φ)² = −8πG·ρ
```

For a point mass: φ(r) = α/r + β_2/r² + ...

Plugging in: 8a·(2α/r³) + 4b₅·(α²/r⁴) = 0 (vacuum)...
Actually this gives α and the correction.

The PPN expansion:
```
g_00 = −1 + B_00 = −1 − φ     (for static, isotropic case)
     = −1 + 2U − 2β·U² + ...
```
where U = GM/r = −Φ_N.

From the static solution: φ = 2U + (b₅/a)·U² + ...
So: g_00 = −1 − 2U − (b₅/a)·U²

Comparing with the PPN form: 2β = −b₅/a → **β = −b₅/(2a)**

With G1-T2a values (a = 3.24, b₅ = −0.618):
```
β_scalar = −(−0.618)/(2·3.24) = 0.618/6.48 ≈ 0.095
```

**This is FAR from β=1.** If this were the full result, TRM would be strongly ruled out by Solar System tests (|β−1| < 2.3×10⁻⁴).

### 3.2 But This Is Only the Scalar Sector

The full β receives contributions from ALL b₁–b₅ and the tensor sector H_μν. The scalar-only β = 0.095 is NOT the physical prediction — it's a partial result that demonstrates why the full tensor computation is essential.

---

## 4. What the Full Computation Requires

| Step | Status |
|:---|:---|
| Expand K(x, x+Δ) to O(Δ⁴) | Required for cubic terms |
| Identify all independent tensor contractions (b₁–b₅) | 5 coefficients to compute |
| Compute each bᵢ from quartic kernel integrals | Same numerical method as G1-T2a |
| Derive field equations for φ, H_μν, B_0i | From δS/δB = 0 |
| Extract 1PN metric | Solve coupled scalar-tensor equations |
| Compute β | From g_00 to O(U²) |

This is a ~1-week computational task, not a documentation task.

---

## 5. What We CAN Say Now

| Statement | Confidence |
|:---|:---|
| b₅ < 0 (scalar cubic coefficient is negative) | **HIGH** — direct numerical computation |
| Scalar-only β ≈ 0.095 ≪ 1 | **HIGH** — follows from b₅/a and PPN formula |
| Full β may differ significantly from scalar-only β | **HIGH** — tensor contributions b₁–b₄ are independent |
| β ≠ 1 is a TRM prediction IF the full tensor computation confirms | **MEDIUM** — requires full computation |
| TRM is ruled out by Solar System IF full β ≈ 0.095 | **LOW** — scalar-only β ≠ full β |

---

## 6. Classification

| Aspect | Status |
|:---|:---|
| Scalar cubic coefficient b₅ = −0.618 | **COMPUTED** |
| Scalar-only β ≈ 0.095 | **COMPUTED** (partial — scalar sector only) |
| Full tensor β | **OPEN** (requires b₁–b₄ computation) |
| Is b < 0 compensated by tensor contributions? | **OPEN** |
| Is TRM compatible with Solar System β ≈ 1? | **OPEN** (depends on full computation) |

### Honest Status

> **The scalar sector alone gives β ≈ 0.095 — far from GR's β = 1. But this is a partial result. The full tensor decomposition (b₁–b₅) may compensate. Until the full computation is done, no conclusion about TRM's compatibility with Solar System tests can be drawn.**

---

## 7. Next Step

Compute b₁–b₄ from the quartic kernel using the same numerical method as G1-T2a. This requires expanding K(x, x+Δ) to O(Δ⁴) and identifying the tensor contraction structure. Estimated effort: ~1 week of computational work.

If the full β ≈ 1 → TRM is GR-compatible at 1PN (major milestone).
If the full β ≪ 1 → TRM is ruled out by Solar System (falsified at current kernel).
If β is in tension but not ruled out → constrains the kernel shape (could motivate kernel modification).
