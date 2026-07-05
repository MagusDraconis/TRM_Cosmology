# TRM V4 — G2C: Lorentzian Kernel and Wave Dispersion

**Date:** 2026-07-05
**Status:** Evaluating Lorentz-compatible kernels and dispersion relations
**Predecessors:** G2A (spacelike metric extraction), G2B (GW polarizations)

---

## 1. The Two Open Problems

| Problem | G2A status | What G2C adds |
|:---|:---|:---|
| Lorentzian kernel: K(x,y) well-behaved for timelike separations | OPEN (both Gaussian and rational fail) | Candidate kernels for full 4D |
| Dispersion: do TT modes propagate at ω = ck? | OPEN | Verification from □K = 0 |

---

## 2. Candidate Lorentzian Kernels

### 2.1 Candidate A — Rational Lorentzian with |d²|

```
K(x,y) = K₀ / (1 + |d²(x,y)| / 2λ²)
```

**Positivity:** ✓ Always positive (|d²| ≥ 0)
**Decay at ∞:** ✓ K ∼ 2λ²K₀/|d²| → 0
**Metric extraction:** ⚠ Non-analytic at d² = 0 (light cone). The derivative K'(0) is undefined because |d²| has a cusp at d² = 0. **Cannot extract metric at coincidence.**

**Classification: NOT SUPPORTED.** Non-analytic at the light cone breaks the metric extraction formula.

---

### 2.2 Candidate B — Wick-Rotated (Euclidean) Kernel

**Approach:** Work in Euclidean signature ℝ⁴ (t → iτ).

```
Euclidean distance: d_E² = τ² + |x|²        (always ≥ 0)
K_E(x,y) = K₀ · exp(−d_E² / 2λ²)            (Gaussian — well-behaved everywhere)
```

**Step 1:** Extract Euclidean metric at coincidence:
```
g_μν^E = −(λ²/K₀) · ∂_μ∂_ν K_E|_{y=x} = δ_μν     (Euclidean flat)
```

**Step 2:** Wick rotate back: τ → it. Under Wick rotation:
```
g_00^E = +1  →  g_00^L = −1     (Lorentzian signature)
g_ij^E = δ_ij →  g_ij^L = δ_ij
```

**Physical kernel K_L(x,y):** The analytic continuation of K_E:
```
K_E(τ, x) = K₀ · exp(−(τ²+|x|²)/2λ²)
K_L(t, x) = K₀ · exp(−(−t²+|x|²)/2λ²)     (analytic continuation τ→it)
           = K₀ · exp(−d²/2λ²)              ← SAME as original Gaussian!
```

This is just the original Gaussian kernel — which we already know blows up for timelike separations! The Wick rotation gives the correct Euclidean metric, but the analytically-continued Lorentzian kernel is the same problematic Gaussian.

**The issue:** Wick rotation works for the METRIC (extracted at coincidence), not for the KERNEL (evaluated at finite separation). At coincidence, d²=0 and there's no problem. At finite timelike separation, the Gaussian kernel blows up.

**Classification: PARTIAL.** Correct metric extraction at coincidence. Kernel still problematic at finite timelike separation.

---

### 2.3 Candidate C — Feynman Propagator Kernel

**Approach:** Use the Feynman propagator form, which handles Lorentzian signature correctly.

```
K(x,y) = K₀ · [i·Δ_F(x−y)]
```

where Δ_F is the Feynman propagator for a massless scalar field:

```
Δ_F(x) = −i/(4π²) · 1/(d² − iε)
```

The real part gives:
```
Re(K) = K₀/(4π²) · ε/((d²)² + ε²)     → δ(d²) as ε→0
```

This is distributional — not a smooth kernel. Not suitable for the metric extraction formula which requires smoothness at coincidence.

**Classification: NOT SUPPORTED.** Distributional kernels break the derivative-based metric extraction.

---

### 2.4 Candidate D — Regulated Feynman (Schwinger Proper Time)

**Approach:** Use the Schwinger proper-time representation with a UV cutoff:

```
K(x,y) = K₀ · ∫_{1/Λ²}^{∞} ds/s² · exp(−s·(d² − iε)/2)
```

For large cutoff Λ: K ∼ exp(−d²/2Λ²) near the light cone, but with proper analytic structure.

At coincidence (d² = 0): the integral gives a finite value K(0) = K₀·Λ². The metric extraction formula works.

For timelike d² < 0: the iε prescription gives a well-defined imaginary part (absorptive — causal). The real part oscillates but remains finite.

**Classification: PROMISING.** The Schwinger proper-time representation is the most rigorous approach. It handles Lorentzian signature correctly, has a built-in UV cutoff (Λ), and reduces to the Gaussian in the Euclidean limit.

---

## 3. Dispersion Relation

### 3.1 From K-Dynamics to Metric Dynamics

The dynamic equation (B4): □K = 0 (vacuum wave equation).

For a perturbation δK:
```
□_x δK(x,y) = 0          (wave equation acting on first argument)
```

The metric perturbation:
```
h_μν(x) = −(1/K'(0)) · ∂_μ ∂_ν δK(x,y) |_{y=x}
```

Taking the wave operator:
```
□ h_μν = −(1/K'(0)) · ∂_μ ∂_ν □_x δK|_{y=x} = 0
```

**Therefore: h_μν satisfies the vacuum wave equation □h_μν = 0.**

### 3.2 Mode Decomposition

Plane wave ansatz: h_μν(x) = A_μν · exp(ik·x)

```
□h_μν = (−k₀² + k²)·h_μν = 0
→ ω² = c²·k²     (with c_K = c)
```

**All physical modes (tensor, vector, scalar) satisfy ω = ck.** The dispersion is the same for all polarizations — identical to linearized GR.

### 3.3 Breathing Mode Speed

The breathing mode (trace of h_ij) also satisfies □h_breath = 0 → propagates at c. This is a TRM-specific prediction: a scalar GW mode propagating at the speed of light.

---

## 4. Summary

| Aspect | Status |
|:---|:---|
| **Lorentzian kernel (coincidence)** | **SUPPORTED** — Wick rotation or Schwinger proper-time gives correct metric |
| **Lorentzian kernel (finite separation)** | **PARTIAL** — Gaussian blows up, Feynman is distributional, Schwinger is promising |
| **Dispersion ω = ck** | **SUPPORTED** — follows from □K = 0 → □h_μν = 0 |
| **All modes propagate at c** | **SUPPORTED** — tensor, vector, scalar all satisfy same wave equation |
| **Recommended approach** | **Schwinger proper-time (D)** — handles Lorentzian, has UV cutoff, reduces to Gaussian in Euclidean limit |

### Honest Status

```
G2C:  METRIC EXTRACTION AT COINCIDENCE — SUPPORTED
       FULL LORENTZIAN KERNEL — PARTIAL (Schwinger promising, not fully verified)
       DISPERSION — SUPPORTED (□K=0 → □h_μν=0)
```

The coincidence-limit metric extraction is robust. The kernel behavior at finite timelike separation is an open problem — the Schwinger proper-time approach is the most promising path, but full verification requires numerical evaluation.

---

## 5. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_G2A_MetricExtraction.md` | Metric extraction proof (spacelike) |
| `TRM_V4_G2B_LinearizedPolarizations.md` | GW polarization count |
| This document | Lorentzian kernel + dispersion |
