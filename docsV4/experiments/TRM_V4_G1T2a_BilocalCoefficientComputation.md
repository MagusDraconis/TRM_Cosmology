# TRM V4 — G1-T2a: Bilocal Coefficient Computation

**Date:** 2026-07-05
**Status:** Computing effective action coefficients for the quartic kernel
**Predecessors:** G1-T2 (Candidate C), G2D (quartic kernel)

---

## 1. Coefficient Definitions

From the bilocal action S[K] = ∫ d⁴x d⁴y L(K, ∂K), expanding K(x, x+Δ) around Δ=0:

```
S_eff[B] = ∫ d⁴x [ a·(∂B)² + b·B·(∂B)² + c·B² + ... ]
```

The coefficients (in Euclidean signature, Wick-rotated):

```
a = ∫ d⁴Δ [f'(|Δ|²)]² · |Δ|⁴          (kinetic — determines Newtonian limit)
b = ∫ d⁴Δ [f'(|Δ|²)]³ · |Δ|⁶          (cubic — determines post-Newtonian β)
c = ∫ d⁴Δ f(|Δ|²) · |Δ|²              (tadpole — must vanish for massless theory)
```

where f(x) = K₀/(1+x/λ²+x²/λ⁴) is the quartic kernel.

---

## 2. Convergence Analysis

### 2.1 UV (small |Δ|)

As |Δ| → 0:
- f(|Δ|²) → K₀ (constant)
- f'(|Δ|²) → −K₀/λ² (constant)
- |Δ|⁴ · |Δ|³ d|Δ| → |Δ|⁷ d|Δ| → converges ✓

### 2.2 IR (large |Δ|)

For |Δ| → ∞:
- f(|Δ|²) ∼ K₀·λ⁴/|Δ|⁴
- f'(|Δ|²) ∼ −2K₀·λ⁴/|Δ|⁶

Integral a: [f']²·|Δ|⁴ ∼ 1/|Δ|¹² · |Δ|⁴ = 1/|Δ|⁸. Measure |Δ|³ → ∫ d|Δ|/|Δ|⁵ → **converges** ✓
Integral b: [f']³·|Δ|⁶ ∼ 1/|Δ|¹⁸ · |Δ|⁶ = 1/|Δ|¹². Measure |Δ|³ → ∫ d|Δ|/|Δ|⁹ → **converges** ✓
Integral c: f·|Δ|² ∼ 1/|Δ|⁴ · |Δ|² = 1/|Δ|². Measure |Δ|³ → ∫ |Δ| d|Δ| → **diverges linearly**

**c is IR-divergent.** This is the tadpole/ cosmological constant term. In QFT, this is renormalized. In TRM, we subtract it (vacuum energy renormalization) — the physical theory has c_ren = 0 for a massless K-field. The quartic kernel correctly gives c_ren = 0 after subtraction.

---

## 3. PPN Parameters from Coefficients

### 3.1 Kinetic Normalization

The linearized field equation from δS/δB = 0:
```
a · □B_μν + c · B_μν = −8πG · T_μν
```

With c_ren = 0 (massless): a · □B_μν = −8πG · T_μν.

The Newtonian limit (static, T_00 = ρ):
```
a · ∇²B_00 = −8πG · ρ
→ B_00 = (8πG/a) · Φ_N where ∇²Φ_N = 4πGρ
→ B_00 = 2 · (4πG²/a) · Φ_N
```

For consistency with the metric extraction g_μν = η_μν + B_μν and the standard Newtonian g_00 = −(1+2Φ_N):
```
B_00 = −2Φ_N
→ a = 4πG² (up to numerical factors from the full tensor structure)
```

### 3.2 PPN γ

γ measures spatial curvature per unit mass. In the bilocal action, γ is determined by the ratio of spatial to temporal kinetic coefficients. Since the quartic kernel depends only on d² = η_μν·Δ^μ·Δ^ν (Lorentz invariant), the kinetic term is Lorentz-invariant:

```
γ = 1    (Lorentz invariance of the quartic kernel)
```

**This matches GR.** No computation needed — it's a structural consequence of the kernel depending only on the invariant interval d².

### 3.3 PPN β

β measures nonlinearity. From the cubic term b·B·(∂B)²:
```
β = 1 + (correction from b/a ratio)
```

For GR, β = 1. The bilocal correction to β depends on the ratio b/a:
```
Δβ = b/a · (numerical factor from tensor structure)
```

Computing b/a numerically from the quartic kernel will give the TRM prediction for β. If b/a gives β=1 → TRM matches GR at 1PN. If not → falsifiable prediction.

---

## 4. Numerical Computation (to be done in xUnit)

The integrals a, b are computed numerically in Euclidean ℝ⁴ with cutoff Λ = 10λ:

```
a = ∫₀^{10λ} d|Δ| · |Δ|³ · [f'(|Δ|²)]² · |Δ|⁴ · S₄
b = ∫₀^{10λ} d|Δ| · |Δ|³ · [f'(|Δ|²)]³ · |Δ|⁶ · S₄
```
where S₄ = 2π² is the surface area of the unit 3-sphere in 4D.

The ratio b/a gives the PPN β correction.

---

## 5. Expected Results

Based on the analytic structure:

- **UV convergence:** ✓ (all integrals converge as |Δ|→0)
- **IR convergence (a, b):** ✓ (rapid decay of quartic kernel)
- **IR convergence (c):** ✗ (linear divergence — subtracted via renormalization)
- **γ = 1:** ✓ (Lorentz invariance — structural)
- **β:** To be determined from b/a ratio

---

## 6. Classification

| Aspect | Status |
|:---|:---|
| Coefficients a, b finite | **SUPPORTED** (convergence proven analytically) |
| Coefficient c requires subtraction | **SUPPORTED** (standard QFT renormalization) |
| γ = 1 from Lorentz invariance | **DERIVED** (structural — kernel depends only on d²) |
| β from b/a ratio | **COMPUTABLE** (numerical integration in xUnit) |
| If β = 1 → GR at 1PN | **Would close G1** |
| If β ≠ 1 → falsifiable prediction | **Testable by Solar System** |
