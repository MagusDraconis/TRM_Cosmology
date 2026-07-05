# TRM V4 — G2A: Metric Extraction from Two-Point Coupling Kernel

**Date:** 2026-07-05
**Status:** Testing the metric extraction hypothesis: g_μν = −(λ²/K₀) · ∂_μ∂_ν K\|_{y=x}
**Predecessor:** `TRM_V4_G2_TensorBridge.md`

---

## 1. Working Hypothesis

```
K(x,y) = K₀ · exp(−d²(x,y) / (2λ²))          (Gaussian coupling kernel)
g_μν(x) = −(λ²/K₀) · ∂_μ ∂_ν K(x,y) |_{y=x}   (metric extraction)
```

where d²(x,y) = g_αβ(x) · (y−x)^α · (y−x)^β + O(Δx³) in Riemann normal coordinates.

### 1.1 Verification

```
∂_μ K = f'(d²) · 2g_μν(x)·(y−x)^ν      (chain rule through d²)
∂_μ ∂_ν K|_{y=x} = 2f'(0) · g_μν(x)      (∂d²/∂y|_{y=x} = 0 eliminates f'' term)
→ g_μν(x) = (1/(2f'(0))) · ∂_μ ∂_ν K|_{y=x}  ✓
```

The prefactor depends on f'(0). For Gaussian f'(0) = −K₀/(2λ²) → 1/(2f'(0)) = −λ²/K₀. For quartic f'(0) = −K₀/λ² → 1/(2f'(0)) = −λ²/(2K₀). The general formula is kernel-dependent through f'(0).

---

## 2. Mathematical Properties

### 2.1 Symmetry

∂_μ∂_ν = ∂_ν∂_μ (partial derivatives commute on smooth functions)
→ g_μν = g_νμ ✓ (metric is symmetric)

### 2.2 Flat-Space Limit

For Minkowski metric η_μν = diag(−1, 1, 1, 1):

```
d²(x,y) = −(Δt)² + (Δx)² + (Δy)² + (Δz)²
K(x,y) = K₀ · exp(−[−(Δt)² + (Δx)² + (Δy)² + (Δz)²] / (2λ²))
```

At coincidence (Δt=Δx=Δy=Δz=0):

```
∂_μ∂_ν K|_{0} = (−K₀/λ²) · η_μν
→ g_μν = η_μν  ✓
```

**Metric correctly recovered.**

### 2.3 The Timelike Separation Problem ⚠

For timelike separations (|Δt| > |Δx|):

```
d² = −(Δt)² + (Δx)² + ... < 0
→ −d²/2λ² > 0
→ K(x,y) = K₀ · exp(+|d²|/2λ²) → ∞ as |d²| → ∞
```

**The coupling kernel BLOWS UP for timelike separations.** This is a fundamental problem with the Euclidean-distance Gaussian ansatz in Lorentzian signature.

**Possible resolutions:**

| Resolution | Approach | Status |
|:---|:---|:---|
| **R1 — Absolute value** | K = K₀·exp(−\|d²\|/2λ²) | Works but non-analytic at d² = 0 (light cone) |
| **R2 — Spacelike-only** | Metric extraction only valid for spacelike separations | Limits applicability to static/elliptic regime |
| **R3 — Lorentz-invariant kernel** | K = K₀/(1 + d²/λ²) or other rational form | No singularity at d²=0, decays for both signs |
| **R4 — Wick rotation** | Work in Euclidean signature (imaginary time), analytically continue | Standard QFT technique |
| **R5 — Light-cone cutoff** | K = K₀·exp(−\|d²\|/2λ²) · Θ(−d²) (spacelike only) | Causal — coupling only exists within light cone |

### 2.4 Resolution R3 — Rational Kernel (PARTIAL)

```
K(x,y) = K₀ / (1 + d²(x,y) / 2λ²)
```

**Advantage over Gaussian:** Slower decay at large spacelike separations. Same metric extraction formula.

**Problem:** For timelike d² < −2λ², the denominator becomes negative → K < 0 (unphysical negative coupling). The rational kernel avoids exponential blow-up but introduces sign-flip at finite timelike separation.

**Assessment:** PARTIAL. Better than Gaussian (no blow-up at infinity) but still fails for sufficiently timelike separations. The rational kernel is usable in a regime |d²| < 2λ² (near the light cone), but not globally.

### 2.5 Final Timelike Status

| Kernel | Spacelike (d² > 0) | Timelike near (0 > d² > −2λ²) | Timelike far (d² < −2λ²) | Verdict |
|:---|:---|:---|:---|:---|
| Gaussian | ✓ decays | ✗ blows up | ✗ blows up | Static only |
| Rational (R3) | ✓ decays | ✓ positive | ✗ negative (unphysical) | Near-light-cone only |
| Ideal kernel | ✓ decays | ✓ positive | ✓ positive | **OPEN — not yet found** |

### 2.6 Resolution Paths (Open)

| Path | Approach | Status |
|:---|:---|:---|
| **Wick rotation** | Work in Euclidean signature (t → iτ), extract metric, analytically continue | Standard QFT technique — promising |
| **|d²| absolute value** | K = K₀/(1 + \|d²\|/2λ²) — non-analytic at light cone | Works but non-smooth |
| **Accept static limit** | G2A valid for spacelike/static. Lorentzian = separate project. | Honest scope limitation |

### 2.5 Sign Structure / Lorentzian Viability

The metric signature is determined by the sign of ∂_μ∂_ν K|_{y=x}:

- For spacelike-dominated kernel: ∂_i∂_j K|_{0} ∝ +δ_ij (positive spatial part)
- For timelike-dominated kernel: ∂_t∂_t K|_{0} ∝ −1 (negative temporal part)

With the rational kernel R3:
```
K(x,y) = K₀ / (1 + [−(Δt)² + |Δx|²] / 2λ²)
∂_t∂_t K|_{0} = +K₀/λ²  → g_00 = −1  (after sign flip: g_μν = −(λ²/K₀)·∂_μ∂_ν K)
```

Wait, let me recalculate for the rational kernel:

K = K₀ · (1 + d²/2λ²)^(−1)
∂_μ K = −K₀ · (1 + d²/2λ²)^(−2) · (1/λ²) · g_μα·Δx^α
∂_μ ∂_ν K|_{0} = −(K₀/λ²) · g_μν

Same as the Gaussian! The extraction formula is robust — it doesn't depend on the specific kernel form, only on the leading Taylor expansion of K(d²).

**Theorem:** For ANY smooth kernel f(d²) with f'(0) ≠ 0:
```
g_μν(x) = (1/(2f'(0))) · ∂_μ ∂_ν f(d²(x,y)) |_{y=x}
```
where f'(0) = df/d(d²)|_{d²=0}.

The Gaussian gives f'(0) = −K₀/(2λ²) → g_μν = −(λ²/K₀)·∂_μ∂_ν f|_{0}.
The rational R3 gives f'(0) = −K₀/(2λ²) → same prefactor as Gaussian.
The quartic gives f'(0) = −K₀/λ² → g_μν = −(λ²/(2K₀))·∂_μ∂_ν f|_{0}.

---

## 3. Degree of Freedom Count

| Object | Components | Constraints | Physical DOF |
|:---|:---|:---|:---|
| g_μν(x) | 10 (4×4 symmetric) | 4 diffeomorphism gauge | **6** |
| K(x,y) | ∞ (two-point function) | Metric extraction (4 constraints) | ∞ − 4 |
| Linearized g_μν | 10 | 4 gauge | 6 = 2 tensor + 2 vector + 2 scalar |

**The extraction g_μν = −(1/K'(0))·∂_μ∂_ν K\|_{y=x} produces exactly a symmetric rank-2 tensor with 6 physical DOF — matching GR.** This is the correct DOF count for a gravitational theory with two polarization states.

---

## 4. Test Kernels

### 4.1 Kernel A — Gaussian (Euclidean ansatz)

```
K(x,y) = K₀ · exp(−|x−y|² / 2λ²)     (3D Euclidean distance)
```
- Symmetric ✓
- g_ij = δ_ij in flat space ✓
- No timelike behavior (3D only) — static limit ✓
- **Classification: SUPPORTED for 3D static metric extraction.**

### 4.2 Kernel B — Anisotropic Gaussian

```
K(x,y) = K₀ · exp(−½ Σ_i (Δx^i)²/λ_i²)
```
Diagonal metric: g_ii = (λ²/λ_i²) · δ_ii (no sum)
- Encodes anisotropic spatial metric ✓
- g_ii inversely proportional to coupling range λ_i ✓
- **Classification: SUPPORTED. Anisotropy naturally encoded in directional coupling ranges.**

### 4.3 Kernel C — Defect-Perturbed Kernel

```
K(x,y) = K₀ · exp(−|x−y|²/2λ²) · (1 + α/|x−x₀| + α/|y−x₀|)
```
Point mass at x₀ perturbs coupling between x and y.
- At coincidence (x=y): K(x,x) = K₀ · (1 + 2α/|x−x₀|)
- Metric extraction: g_μν gets a perturbation ∝ α/|x−x₀|³
- Not a simple 1/r metric — requires careful analysis
- **Classification: PARTIAL. Defect perturbs K, but the extracted metric needs verification.**

---

## 5. What This Proves and What It Doesn't

| Proves | Doesn't prove |
|:---|:---|
| K(x,y) → g_μν extraction is mathematically well-defined | The extracted g_μν satisfies G_μν = 8πG·T_μν |
| 6 physical DOF — matches GR | The dynamics of K produce Einstein equations |
| Works for any K(d²) with K'(0) ≠ 0 | K(x,y) has the correct timelike behavior (R3 or R4 needed) |
| Anisotropic coupling → anisotropic metric | The Kerr metric can be extracted (requires full dynamics) |

---

## 6. Final Status

**G2A is SUPPORTED for spacelike/static metric extraction. Full Lorentzian extension remains OPEN.**

| Regime | Status |
|:---|:---|
| Spacelike / static | **SUPPORTED** — metric extraction is mathematically rigorous |
| Timelike near light cone | **OPEN** — rational kernel works for \|d²\| < 2λ² but not globally |
| Timelike far | **OPEN** — no kernel currently handles this without blow-up or negative values |
| Full 4D Lorentzian | **OPEN** — Wick rotation or a new kernel is required |

---

## 7. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_G2_TensorBridge.md` | Full G2 analysis, candidate catalog |
| `TRM_V4_GR_Replacement_Roadmap.md` | G1–G6 roadmap |
| This document | Concrete metric extraction test |
