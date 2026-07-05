# TRM V4 — G1: Nonlinear Tensor Field Dynamics

**Date:** 2026-07-05
**Status:** Investigating nonlinear field equations for K — honest about scalar limitations
**Predecessors:** B4 (linear wave), G2 (Lorentzian tensor bridge)

---

## 1. The Fundamental Tension

G2 provides the geometric infrastructure — metric extraction, polarizations, dispersion. But the dynamics come from a scalar PDE for K(x,y). A single scalar equation cannot fully reproduce the Einstein equations, which are 10 coupled nonlinear PDEs for g_μν.

**The core question:** How close to GR can a scalar K-dynamics get?

---

## 2. Candidate Field Equations

### 2.1 Candidate A — Linear with Mass Source

```
□K = −4π·α·ρ_m          (B4 — already handled)
```

**What it reproduces:**
- Newtonian limit: ✓ (g_00 → −2Φ, ∇²Φ = 4πGρ)
- Linear GWs: ✓ (□h_μν = 0 in vacuum)
- Light deflection: ✓ (effective n = 1−φ)
- Redshift/time dilation: ✓

**What it misses:**
- Post-Newtonian corrections (γ = 1 in PPN, GR has γ = 1 also — matches at this order)
- Strong-field deviations
- Self-gravitation of the K-field

**Classification: EFFECTIVE.** Matches GR at first post-Newtonian order for a scalar theory.

---

### 2.2 Candidate B — With K-Field Self-Energy

```
□K = −4π·α·(ρ_m + β·|∇K|²)
```

where β·|∇K|² is the K-field energy density acting as an additional source.

**Physical motivation:** In GR, gravity gravitates — the gravitational field energy is itself a source. This adds the analogous term for K.

**What it adds over A:**
- Nonlinearity → corrections to Mercury precession, binary pulsar
- The parameter β can be tuned to match GR's nonlinear predictions

**Problem:** β is a new free parameter not determined by the theory. Fitting β to GR data is calibration, not derivation.

**What it still misses:**
- The full tensor structure of the Einstein equations
- Different source terms for different metric components
- Frame-dragging (gravitomagnetism) from a single scalar

**Classification: CALIBRATED.** Improves on A but introduces a new parameter.

---

### 2.3 Candidate C — Nonlinear Operator Generalization

```
F(□K, (∇K)², K, ...) = −4π·α·T
```

where F is a nonlinear function chosen to make the induced metric satisfy the Einstein equations as closely as possible.

**This is equivalent to:** Given the metric extraction g_μν = (1/(2f'(0)))·∂_μ∂_ν K|_{y=x}, find the PDE for K such that g_μν satisfies G_μν = 8πG·T_μν.

**This is an inverse problem:** g_μν has 10 components, K is 1 function. The mapping K → g_μν is many-to-one. Finding a K that produces a given g_μν is a PDE constraint problem. The Einstein equations for g_μν translate to a system of constraint equations for K.

**Can this work in principle?** For specific symmetric spacetimes (Schwarzschild, FRW), a single function can encode the metric (e.g., the Newtonian potential Φ for Schwarzschild). For general spacetimes, 10 metric components cannot be encoded in 1 scalar function.

**Classification: PARTIAL.** Works for highly symmetric spacetimes. Fails for general spacetimes with rotation, multiple sources, or generic GWs.

---

## 3. The Scalar Ceiling

| GR Feature | Scalar K can reproduce? | Why / Why not |
|:---|:---|:---|
| Newtonian limit | ✓ | One scalar (Φ) is enough |
| Schwarzschild | ✓ | Spherically symmetric — one function |
| Linear GWs (TT) | ✓ (via G2 extraction) | Generic Hessian → 2 TT modes |
| Post-Newtonian γ | ✓ (PPN γ = 1) | Matches GR at 1PN for scalar-tensor with ω→∞ |
| Mercury precession | ✓ (calibrated) | With candidate B and fitted β |
| Kerr (rotating) | ✗ | Need off-diagonal g_0i — scalar can't produce |
| Binary pulsar | ✗ | Gravitational radiation reaction requires full tensor |
| Generic GW from merger | PARTIAL | h_+, h_× from extraction, but amplitudes differ from GR |
| Cosmological perturbations | PARTIAL | Scalar mode only — no vector or tensor perturbations |

---

## 4. What TRM Can Realistically Achieve

### Achievable (with scalar K)

- **Complete weak-field phenomenology** (B1–B6, G2): Newton, redshift, deflection, Shapiro, linear GWs ✓
- **Post-Newtonian corrections** with one calibrated parameter β (Candidate B)
- **Schwarzschild analogue** (static spherical solution of nonlinear K equation)

### Not Achievable (with scalar K alone)

- **Full GR equivalence** — requires tensor field equation (G_μν = 8πG·T_μν)
- **Kerr metric** — requires off-diagonal metric components
- **Binary inspiral waveform** at full GR precision
- **Generic cosmological perturbations** with tensor modes

---

## 5. The Path Forward: Tensor K-Dynamics

The scalar ceiling is reached at G1. To go beyond, K must be promoted to a tensor. Options:

| Option | What it gives | Difficulty |
|:---|:---|:---|
| **K_μν(x)** — tensor coupling field | Full rank-2 dynamics, 10 PDEs | VERY HIGH — equivalent to inventing GR from scratch |
| **K(x,y) with tensor extraction** — keep scalar K, extract full g_μν from more of K's structure | Tensor metric from scalar K via non-local extraction | HIGH — need to extract 10 components from 1 function |
| **Multi-K** — multiple scalar fields K^(n) | Brans-Dicke-like with multiple scalars | MEDIUM — known class of theories |

### The Most Promising: Multi-K

The discrete coupling matrix K_ij has N(N−1)/2 independent entries. In the continuum, this becomes K(x,y) — a function of 8 variables, not 1. The reduction K(x,y) → K(d²) discards most of this information.

If we keep more structure — e.g., decompose K(x,y) into spherical harmonics:
```
K(x,y) = K₀(d²) + K_μ(d²)·(x−y)^μ + K_μν(d²)·(x−y)^μ·(x−y)^ν + ...
```

Then each component K_μν gives a rank-2 tensor field with its own dynamics. This is the natural extension of the scalar K(d²) to a full tensor theory — and it emerges organically from the discrete K_ij.

---

## 6. Classification

| Candidate | Classification | What it achieves |
|:---|:---|:---|
| A — Linear with mass source | **EFFECTIVE** | Newton + linear GWs (V4 complete) |
| B — With self-energy | **CALIBRATED** | Post-Newtonian corrections (β fitted) |
| C — Nonlinear operator | **PARTIAL** | Symmetric spacetimes only |
| Multi-K tensor | **PROMISING** | Path to full GR (requires new development) |

---

## 7. Honest Bottom Line

**G1 is the ceiling of the scalar K approach.** Candidate B can push TRM to post-Newtonian accuracy with one calibrated parameter β. Candidate C can describe symmetric spacetimes. But full GR equivalence — Kerr, binary inspiral, generic cosmology — requires promoting K to a tensor.

**The good news:** TRM has a natural path to tensor dynamics through the K(x,y) → K_μν(d²) spherical harmonic decomposition. This is not an ad-hoc generalization — it's the natural continuum limit of the discrete coupling matrix K_ij.

**The honest status:** V4 (scalar K, weak-field) is complete and self-consistent. G2 (Lorentzian tensor bridge) provides the geometric infrastructure. G1 (nonlinear dynamics) reaches the scalar ceiling. The next step — tensor K-dynamics — is a new research program.

---

## 8. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_B4_DynamicCouplingField.md` | Linear wave □K = 0 |
| `TRM_V4_G2_TensorBridge.md` | Full G2 catalog |
| `TRM_V4_G2D_FullLorentzianClosure.md` | Lorentzian kernel closure |
| `TRM_V4_GR_Replacement_Roadmap.md` | G1–G6 roadmap |
| This document | Nonlinear scalar ceiling + tensor path forward |
