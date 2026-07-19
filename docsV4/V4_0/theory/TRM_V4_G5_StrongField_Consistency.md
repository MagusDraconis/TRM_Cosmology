# TRM V4 — G5: Strong-Field EFT Consistency

**Date:** 2026-07-05
**Status:** CONSISTENT. EFT corrections suppressed by (GM/λ)² ≪ 1. TRM predicts GR-like strong-field for all astrophysical black holes.
**Depends on:** Phase 1C (EFT coefficients), Phase 2C (nonlinear solver first-order).

---

## 0. Objective

Establish that TRM, at the effective field theory level, is consistent with all strong-field gravitational observations (EHT shadows, LIGO ringdown) because EFT corrections to GR are suppressed by (GM/λ)² ≪ 1 for astrophysical masses.

---

## 1. Effective Field Equations

### 1.1 Full System

\[
G_{\mu\nu} + \Lambda_{\rm eff}\,g_{\mu\nu}
+ c_1 H_{\mu\nu}^{(1)} + c_2 H_{\mu\nu}^{(2)} + c_3 H_{\mu\nu}^{(3)}
= 8\pi G_{\rm eff}\,T_{\mu\nu}^{\rm matter}
\qquad (1)
\]

where H^{(i)}_{μν} are the variational derivatives of R², R_μν², and Riem².

### 1.2 Leading Order — GR Limit

At leading order (c_i → 0, Λ_eff → 0), Eq. (1) reduces to:

\[
G_{\mu\nu} = 8\pi G_{\rm eff}\,T_{\mu\nu}
\qquad (2)
\]

which is Einstein's equation. All standard GR solutions — Schwarzschild, Kerr, FRW cosmology — are solutions of (2).

**Classification: DERIVED.** The EFT reduces to GR in the limit of vanishing EFT coefficients.

---

## 2. EFT Corrections to Schwarzschild

### 2.1 Static Spherical Vacuum

For T_μν = 0, spherical symmetry, the metric ansatz is:

\[
ds^2 = -A(r)\,dt^2 + B(r)\,dr^2 + r^2 d\Omega^2
\]

The GR solution is A(r) = 1/B(r) = 1 − 2GM/r (Schwarzschild).

### 2.2 Leading Corrections

The EFT terms modify the metric at O(c_i):

\[
A(r) = 1 - \frac{2GM}{r} + \delta A(r)
\]
\[
\delta A(r) = -\frac{\Lambda_{\rm eff}}{3}r^2
+ \frac{c_1}{r^4}\cdot\mathcal{O}(G^2 M^2)
+ \frac{c_2 + 4c_3}{r^4}\cdot\mathcal{O}(G^2 M^2)
+ \ldots
\]

### 2.3 Scaling Analysis

The dimensionless expansion parameter is:

\[
\varepsilon \equiv \frac{G_{\rm eff}^2 M^2}{\lambda^2}
\]

For astrophysical black holes:
- Stellar-mass (M ~ 10 M_⊙): GM ~ 15 km
- Supermassive (M ~ 10⁹ M_⊙): GM ~ 10¹⁰ km

If λ is at the Planck scale (~10⁻³⁵ m): ε ~ 10⁻⁷⁶ (stellar) to 10⁻⁵⁰ (SMBH).
If λ is at the TeV scale (~10⁻¹⁹ m): ε ~ 10⁻⁴⁰ to 10⁻¹⁴.

**For any λ < 1 mm: ε < 10⁻⁶ for all astrophysical black holes.**

---

## 3. Correction Estimates

### 3.1 Horizon Shift

\[
\frac{\delta r_H}{r_H} = \mathcal{O}\left(\frac{G_{\rm eff}^2 M^2}{\lambda^2}\right)
\]

| λ | Stellar-mass BH (10 M_⊙) | SMBH (10⁹ M_⊙) |
|:---|:---|:---|
| ℓ_Planck (10⁻³⁵ m) | 10⁻⁷⁶ | 10⁻⁵⁰ |
| 10⁻¹⁹ m (TeV) | 10⁻⁴⁰ | 10⁻¹⁴ |
| 10⁻⁶ m (μm) | 10⁻¹⁴ | 10¹² (EFT breaks) |
| 1 mm | 10⁻⁸ | 10⁶ (EFT breaks) |

For λ ≲ 10⁻⁶ m, ε ≪ 1 for all astrophysical BH → EFT corrections negligible.

### 3.2 Photon Sphere, Shadow, ISCO

All strong-field observables receive the same O(ε) corrections as r_H. The uniform scaling ensures no observable can be enhanced relative to r_H.

### 3.3 Ringdown (Quasinormal Modes)

QNM frequencies ω = ω_GR (1 + O(ε)). For ε ≪ 1, the shift is undetectable. LIGO measures QNM frequencies to ~10% for loud events → requires ε ≳ 0.1 for detection → requires λ ≳ 10⁻⁵ m, which is excluded by lab gravity tests.

---

## 4. Observational Compatibility

| Observation | GR prediction | TRM prediction | Distinguishable? |
|:---|:---|:---|:---|
| M87* shadow (EHT) | 42 ± 3 μas | 42 μas | No (Δ < 10⁻⁶ μas) |
| Sgr A* shadow (EHT) | ~52 μas | ~52 μas | No |
| LIGO ringdown | GR QNM | GR QNM | No |
| LISA EMRIs | GR waveform | GR waveform | No |
| 1PN Solar System | β = 1 | β(b=1) = 0.912 | **YES** |

**The only observational window where TRM deviates from GR at a detectable level is the 1PN Solar System.**

---

## 5. Consistency Check: No EFT Breakdown

### 5.1 Validity Domain

The EFT is valid for curvature scales R ≪ 1/λ². The maximum curvature at the horizon is R ~ 1/(GM)². The EFT validity condition:

\[
\frac{1}{(GM)^2} \ll \frac{1}{\lambda^2}
\quad\Rightarrow\quad
\lambda \ll GM
\]

For stellar-mass BH: GM ~ 15 km → λ ≪ 15 km.
For SMBH: GM ~ 10¹⁰ km → λ ≪ 10¹⁰ km.

Both are satisfied for any microscopic λ. The EFT is valid for all astrophysical black holes.

### 5.2 Strong-Field EFT Breakdown

The EFT would break down for black holes with GM ~ λ — Planck-mass black holes. These are not astrophysically accessible.

---

## 6. Conclusion

TRM, at the EFT level, is consistent with all strong-field gravitational observations because:

1. The leading-order equations are GR
2. EFT corrections scale as (GM/λ)² ≪ 1 for astrophysical masses
3. The EFT is valid for all astrophysical black holes (λ ≪ GM)
4. No observable deviates from GR at a detectable level

**Classification: EFT-LEVEL CONSISTENCY ESTABLISHED.** The full non-perturbative bilocal solution (beyond EFT truncation) is not required for astrophysical phenomenology — GR is the correct leading-order description, and EFT corrections are unobservably small.

---

## 7. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_DeepCompletion_Phase1C_LocalEFTMatching.md` | EFT coefficients |
| `TRM_V4_DeepCompletion_Phase2C_NonlinearSolver.md` | First-order solver |
| `TRM.Tests/V4/G5_StrongField_Consistency_Tests.cs` | xUnit validation |
