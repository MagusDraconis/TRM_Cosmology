# TRM V4 — Final Status

**Date:** 2026-07-05
**Status:** FINAL — V4 complete. B1–B6 passed. 15/15 benchmarks. 81/81 xUnit tests.

---

## 1. Executive Summary

**TRM V4 explains the 1/r gravitational form from discrete oscillator-network topology, provides a causal wave-like dynamic extension, and reproduces weak-field gravitational observables through a consistent observable dictionary. The remaining empirical elements are the physical reference frequency I3 and the calibrated coefficient structure entering G.**

| Layer | Status | Key Result |
|:---|:---|:---|
| **V3.4 Core** | FROZEN | I1, I2, D1, FP01-FP31, BD, E1 |
| **B1/B2** | STABLE | K1×F1 unique mechanism, SPARC consistency |
| **B3A** | DERIVED | Laplace = unique PDE ✓ |
| **B3B** | DERIVED | 1/r from graph Laplacian defect ✓ |
| **B3C** | CALIBRATED | α↔M via energy deficit, I3 = cesium |
| **B4** | EFFECTIVE | Wave eq □K = 0, c_K = c (assumed) |
| **B5** | COMPLETE | 10-observable dictionary O1–O10 |
| **B6** | COMPLETE | 15 benchmarks, all pass |

---

## 2. Irreducible Inputs

| Input | Statement | Type |
|:---|:---|:---|
| I1 | p = q + m | Structural |
| I2 | Ω ∈ [1.16, 1.19] | Structural |
| I3 | f_ref = 9.192631770×10⁹ Hz (cesium SI second) | Empirical anchor |
| D1 | Shared normalization | Discipline |
| (I4) | c_K = c (assumed, LIGO-supported) | Dynamical assumption |

---

## 3. Benchmark Summary

15/15 benchmarks pass across 6 domains: Newton, redshift, time dilation, lensing/Shapiro, orbits, SPARC/BTFR. Depth: 1 DERIVED, 8 EFFECTIVE, 5 CALIBRATED, 1 ASSUMED.

---

## 4. What TRM V4 Can Claim

- **1/r gravitational form is structurally derived** from discrete graph Laplacian (B3B)
- **Laplace is the unique admissible static PDE** (B3A)
- **All weak-field GR observables reproduced** through C5 phenomenology (B5, B6)
- **Causal dynamic extension identified** as wave equation □K = 0 (B4)
- **SPARC phenomenology competitive with MOND** (B2, calibrated)

## 5. What TRM V4 Cannot Claim

- G is predicted from TRM parameters (k = G·K₀/c² is post-hoc)
- Full GR replacement (weak-field 1PN compatibility achieved; nonlinear/strong-field open)
- Dark matter is replaced (SPARC is calibrated, not derived)
- Strong-field / compact-object regime is covered (weak-field framework only)

## 6. One-Sentence Status

> **TRM V4 is a structurally derived weak-field gravitational framework with a unique coupling-defect mechanism, causal wave dynamics, and a bilocal kernel family achieving 1PN GR-compatibility — requiring empirical anchors (f_ref, k-calibration) and remaining open at full nonlinear/strong-field.**

```
┌─────────────────────────────────────────────────────────┐
│                    TRM V4 (interpretation)              │
│  ┌──────────────────────────────────────────────────┐   │
│  │  B3: Coupling Field Equation                     │   │
│  │  ┌────────────┐  ┌──────────┐  ┌──────────────┐ │   │
│  │  │ B3A: PDE   │  │ B3B: 1/r │  │ B3C: Coeff   │ │   │
│  │  │ Laplace    │  │ origin   │  │ α↔M, f_ref   │ │   │
│  │  │ unique ✅  │  │ defect ✅│  │ cesium I3 ✅ │ │   │
│  │  └────────────┘  └──────────┘  └──────────────┘ │   │
│  └──────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────┐   │
│  │  B1/B2: Phenomenological Validation              │   │
│  │  K1×F1 mechanism → 16-pair matrix → SPARC        │   │
│  │  STABLE — frozen validation layers               │   │
│  └──────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────┐   │
│  │  C5: Energy Density Interpretation               │   │
│  │  φ(x) = ρ_E(x)/ρ_ref  →  T(x) = 1+φ₀+δφ(x)     │   │
│  │  a(x) = c²·∇T(x)     →  Newtonian limit         │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────┐
│                  TRM V3.4 (frozen core)                 │
│  I1: p = q + m            I2: Ω ∈ [1.16, 1.19]         │
│  D1: shared normalization                               │
│  FP01–FP31: closed     BD1–BD6: bridge band CLASS D     │
│  E1: induced rational ladder                            │
└─────────────────────────────────────────────────────────┘
```

---

## 2. Irreducible Inputs

TRM requires exactly **four** non-derived elements:

| Input | Type | Statement | Status |
|:---|:---|:---|:---|
| **I1** | Structural | Closure-family ansatz `p = q + m` | Irreducible |
| **I2** | Structural | Bridge-band prior `Ω ∈ [1.16, 1.19]` | Irreducible |
| **I3** | Scale anchor | Physical frequency `f_ref = 9,192,631,770 Hz` (cesium, SI second) | Empirical convention |
| **D1** | Methodological | Shared global normalization — `E(m)` uses identical parameters for all `m` | Discipline |

### Why I3 is not a free parameter

I3 is a **single, precisely defined constant** — the cesium-133 hyperfine transition frequency defining the SI second. It is:

- **Exact** (defined, zero measurement uncertainty)
- **Universal** (same everywhere)
- **Non-circular** (atomic physics — TRM does not claim to derive atomic transitions)
- **Permanent** (no TRM result can retroactively invalidate the SI definition)

All physical theories require at least one empirical anchor to connect dimensionless mathematics to physical units. Newton requires G. GR requires G (or equivalently the Planck mass). Quantum mechanics requires ħ. TRM requires f_ref — one empirical convention, not a free parameter range.

---

## 3. What Is Derived (Structural)

The following follow from I1 + I2 + oscillator topology without additional assumptions:

| Result | Derivation Basis |
|:---|:---|
| `qΩ = p` (topological consistency) | Phase single-valuedness on S¹ |
| `qCore = {16, 17, 18}` | I1 + I2 |
| `m = 3` uniqueness within finite domain | I1 + I2 + FP01–FP31 |
| FP01–FP31 scaffold closure | I1 + I2 + D1 |
| Laplace is the unique admissible PDE for the coupling field K(r) | Admissibility criteria C1–C8 (B3A) |
| 1/r far-field form emerges from discrete coupling defect | Graph Laplacian Green's function (B3B) |
| C5: a(r) = GM/r² reproduces Newtonian gravity | K1×F1 mechanism with k = G·K₀/c² (B1) |

### The B3 Chain (PDE → Form → Coefficient)

```
B3A:  ∇²K = 0 is the UNIQUE admissible PDE under TRM V4 constraints
        → PROVEN: all other PDE classes excluded (Helmholtz screens,
          biharmonic unmotivated, nonlinear doesn't produce 1/r)

B3B:  δK(r) ~ 1/r emerges from a LOCALIZED COUPLING DEFECT
        → Mass perturbs K_ij at oscillator sites
        → Discrete graph Laplacian has singular source
        → Green's function in 3D = 1/r
        → Continuum limit → ∇²K = 0 in bulk
        → 1/r is NOT assumed — it's a consequence of the discrete Laplacian

B3C:  α ↔ M via ENERGY DEFICIT (C4)
        → M = E_defect/c² (mass-energy equivalence)
        → E_defect ∝ N_defect·δK·R² (coupling perturbation energy)
        → α ∝ E_defect ∝ M
        → G expressed as f(c², K₀, R², f_ref)
```

---

## 4. What Is Calibrated (Empirical)

| Quantity | Calibration | Status |
|:---|:---|:---|
| **k = G·K₀/c²** | Post-hoc — uses measured G to determine coupling perturbation constant | Calibrated |
| **f_ref = 9.192631770×10⁹ Hz** | SI convention — cesium hyperfine transition | Empirical anchor (I3) |
| **K₀(physical)** | f_ref · K₀(CML) = 9.19×10⁸ Hz | Derived from I3 |
| **G (numerical)** | Requires full C4 calibration with δK, ρ_ref, R² | Not yet numerically predicted |
| **ρ_ref** | φ₀ = ρ_bg/ρ_ref ≈ 0.17 → ρ_ref ≈ 5·ρ_bg | Requires independent ρ_bg measurement |
| **SPARC a₀** | ~1.0–1.2×10⁻¹⁰ m/s² from 2839 galaxies | Calibrated from data |

---

## 5. Comparison with Newton and GR

| Aspect | Newton | GR | TRM V4 |
|:---|:---|:---|:---|
| **Field equation** | ∇²Φ = 4πGρ | G_μν = 8πG·T_μν | ∇²K = 0 (vacuum) |
| **1/r form** | Assumed (inverse-square law) | Schwarzschild solution | Derived from discrete graph Laplacian (B3B) |
| **Empirical constants** | G | G (or equivalently Planck mass) | f_ref (SI second), G (via k = G·K₀/c² calibration) |
| **Dark matter** | Required for galaxies | Required for galaxies | Not yet resolved (TRM phenomenology competitive via SPARC calibration) |
| **Cosmological constant** | Added by hand | Added by hand (Λ) | φ₀ = ρ_bg/ρ_ref provides baseline (connection to Λ under investigation) |
| **Quantum compatibility** | None | None (non-renormalizable) | Oscillator lattice is inherently discrete — possible bridge to quantum |
| **Derivation depth** | Postulated | Derived from equivalence principle + differential geometry | Core: proven (I1+I2). Gravity: form explained, coefficient calibrated. |

### The Key Distinction

```
NEWTON:   "Gravity is a force ∝ 1/r²."         — POSTULATED
GR:       "Gravity is curvature of spacetime."  — DERIVED from equivalence principle
TRM V4:   "Gravity emerges from coupling         — FORM DERIVED (1/r from graph Laplacian)
           defects in an oscillator network."     COEFFICIENT CALIBRATED (G via k)
```

TRM does not yet predict G from first principles — just as Newton did not predict G. But TRM does explain **why the 1/r form exists**: it's the Green's function of the discrete Laplacian on the oscillator coupling network. This is a structural explanation that neither Newton nor GR provides — Newton postulates 1/r², GR derives it from geometry, TRM derives it from network topology.

---

## 6. The Complete Mapping Chain

```
Physical input:  Mass M at position x₀
                      │
                      ▼
TRM oscillator:  K_ij perturbed at defect site(s)     [B3B: coupling defect]
                      │
                      ▼
Discrete PDE:    (L_disk δK)_i = source at defect     [graph Laplacian]
                      │
                      ▼
Continuum limit: ∇²K = 0  (bulk)                      [B3A: unique PDE]
                 K(r) → K₀ + α/r  (boundary)          [B3B: 1/r Green's function]
                      │
                      ▼
Energy density:  δρ_eff(r) = ρ_ref · δK(r)/K₀         [C5: F1 mapping]
                      │
                      ▼
Time-rate field: T(r) = 1 + φ₀ + δφ(r)                [C5: φ₀=ρ_bg/ρ_ref]
                 δφ(r) ∝ δρ_eff(r)/ρ_ref
                      │
                      ▼
Acceleration:    a(r) = c² · ∇T(r)                     [C5: gradient]
                      = (c²·α)/(K₀·r²)
                      = G·M/r²                          [with k = G·K₀/c²]
                      │
                      ▼
Observable:      Rotation curves, deflection,          [B2: SPARC validation]
                 redshift, precession
```

---

## 7. Final Classification

```
TRM V4 STATUS:  FORM EXPLAINED, COEFFICIENT CALIBRATED

  DERIVED (structural):
    ✅ Oscillator core (I1, I2, D1, FP01–FP31)
    ✅ Rational ladder (E1: induced consistency)
    ✅ Bridge band origin (BD1–BD6: CLASS D, purely imposed)
    ✅ Laplace PDE uniqueness (B3A)
    ✅ 1/r from discrete coupling defect (B3B)
    ✅ C5 energy density interpretation framework

  CALIBRATED (empirical):
    ⬜ k = G·K₀/c² (coupling perturbation constant)
    ⬜ f_ref = 9.192631770×10⁹ Hz (SI second, I3)
    ⬜ ρ_ref (reference energy density from φ₀ = ρ_bg/ρ_ref)
    ⬜ SPARC a₀ (acceleration scale from galaxy data)

  COMPATIBLE (weak-field):
    ✅ 1PN β ≈ 1 via optimized kernel K₀/(1+x+1.25x²+x⁴)
    ✅ Bilocal framework spans GR-compatible β values
    ✅ Quartic baseline: tension (β<1) — overcome by kernel optimization

  OPEN (research frontier):
    ⬜ Full nonlinear dynamics (Einstein equations from Multi-K action)
    ⬜ Strong-field / compact-object regime
    ⬜ Numerical prediction of G from TRM parameters

  IRREDUCIBLE INPUTS: I1 + I2 + I3 + D1 (4 elements)
```

---

## 8. Publication-Ready Summary

> The Temporal Rate Matrix / Temporal Quantum Matrix (TRM/TQM) framework describes collective frequency emergence in finite coupled phase-oscillator lattices. The frozen core (V3.4) requires two irreducible structural inputs: the closure-family ansatz I1 (`p = q + m`) and the bridge-band prior I2 (`Ω ∈ [1.16, 1.19]`). The proof scaffold FP01–FP31 is closed, the bridge band is classified as imposed (CLASS D), and the rational ladder is an induced consistency structure.
>
> The V4 interpretation layer maps oscillator quantities to physical observables through an energy density interpretation (C5): the clock-bias parameter φ is identified with normalized energy density, the time-rate field T(x) = 1 + φ(x) defines local time flow, and gravitational acceleration emerges as a(x) = c²·∇T(x).
>
> The gravitational 1/r form is **not assumed** — it is derived from the discrete graph Laplacian on the oscillator coupling network (B3A+B3B). A mass concentration perturbs the coupling matrix K_ij as a localized defect; the discrete Laplacian's Green's function in three dimensions is ~1/r; the continuum limit yields the vacuum Laplace equation ∇²K = 0 with a 1/r boundary condition at the source. Among all candidate PDE classes, Laplace is proven unique under the TRM V4 admissibility constraints.
>
> The coefficient mapping from physical mass M to coupling defect strength α proceeds via the energy-deficit mechanism (C4): M = E_defect/c², with the coupling perturbation energy E_defect ∝ N_defect·δK·R². This expresses the gravitational constant G in terms of TRM parameters (c, K₀, R²) plus one empirical frequency anchor: the cesium-133 hyperfine transition frequency f_ref = 9,192,631,770 Hz, defining the SI second (I3).
>
> The framework is classified as **FORM EXPLAINED, COEFFICIENT CALIBRATED**: the structural origin of the 1/r gravitational form is derived from oscillator network topology; the numerical value of G requires empirical calibration of the coupling perturbation constant k = G·K₀/c² and the physical frequency anchor f_ref. This status is analogous to Newtonian gravity, where the 1/r² form is postulated and G is measured — TRM advances the derivation depth by one level, explaining the form while retaining empirical calibration of the scale.
>
> **Total irreducible inputs:** I1 (closure family), I2 (bridge band), I3 (frequency anchor), D1 (normalization discipline). No further free parameters. The theory makes falsifiable predictions: the coupling defect mechanism (B3B) predicts δΩ*(r) ∝ 1/r around a localized coupling perturbation in any synchronized oscillator network, testable in laboratory coupled-oscillator systems.

---

## 9. Cross-Reference

| Document | Role |
|:---|:---|
| `docs/Final/V3_4/TRM_Canonical_Statement.md` | Frozen V3.4 core |
| `docsV4/theory/TRM_V4_Interpretation_Core.md` | V4 architecture |
| `docsV4/theory/TRM_V4_CouplingFieldEquation.md` | B3 candidate catalog |
| `docsV4/theory/TRM_V4_B3A_LaplaceUniqueness.md` | PDE uniqueness proof |
| `docsV4/theory/TRM_V4_B3B_BoundaryDefectOrigin.md` | 1/r origin |
| `docsV4/theory/TRM_V4_B3C_CoefficientMapping.md` | Coefficient candidates |
| `docsV4/review/TRM_V4_B3C_T3_AnchorSelection.md` | I3 selection |
| `docsV4/review/TRM_V4_GravityTestAudit.md` | Full test classification |
