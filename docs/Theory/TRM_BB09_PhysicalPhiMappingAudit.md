# BB09 — Physical φ Mapping Audit

## Scope

BB08 + CML11 established: φ ≈ 0.17 → Ω* ≈ 1.17 works perfectly in dynamics.
Remaining gap: φ = 0.17 has no physical origin in the dimensionless CML (no M, r).

This audit determines whether φ ≈ 0.17 can be mapped to a physical quantity
already present in the repository: φ = GM/(c²r).

**Rules:** No derivations. No new assumptions. Only mapping of existing structure.

---

## # Existing φ Definitions

### 1. PhotonTransportModel.Phi() — Canonical Definition

| Field | Value |
|:---|:---|
| **File** | `PhotonTransportModel.cs:111-114` |
| **Definition** | `public static double Phi(double G, double M, double c, double r) => G * M / (c * c * r);` |
| **Units** | Dimensionless in G=c=1; dimensional in physical units (SI) |
| **Classification** | **PHYSICAL φ — standard Newtonian gravitational potential** |

### 2. Direct Computation in TRM34–TRM41 (RealityTests)

| Field | Value |
|:---|:---|
| **File** | `TRM_Realtiy_Tests.cs:1529, 1551, 1619, 1694, 1790, 1917` |
| **Computation** | `phi = G * M / (c * c * r)` in each step |
| **Physical units** | SI: G = 6.67430e-11, M = M_Solar (1.989e30 kg), c = 299792458, r in meters |
| **Classification** | **PHYSICAL φ — full SI computation** |

### 3. FixationTests MC14 — Physical φ for Coherence Tracking

| Field | Value |
|:---|:---|
| **File** | `PhotonTransportModel_FixationTests.cs:893-903` |
| **Computation** | `phi = PhotonTransportModel.Phi(G=1.0, M=0.01, c=1.0, r)` |
| **r range** | {8.0, 10.0, 12.0, 16.0, 20.0, 24.0, 30.0} |
| **M** | 0.01 (dimensionless) |
| **Classification** | **DIMENSIONLESS TEST φ — G=c=1, M arbitrary** |

### 4. CML Clock-Bias Parameter

| Field | Value |
|:---|:---|
| **File** | `CollectiveModeLockingTests.cs` (ModeLockConfig.ClockBiasPhi) |
| **Definition** | Free parameter added directly to the phase update equation |
| **Range tested** | 0.0 (baseline), 1e-3, 1e-2 (CML10), 0.17 (CML11) |
| **Classification** | **DIMENSIONLESS TEST φ — no connection to PhotonTransportModel.Phi()** |

---

## # Observed φ Ranges in Repository

### Physical (SI) φ Ranges

| Test | M | r (min) | φ (max) | Regime |
|:---|:---|:---|:---|:---|
| TRM36 (solar) | M_sun | b0 = 6.9634e8 m | ~2.1 × 10⁻⁶ | Solar surface |
| TRM38 (stronger) | 10 M_sun | 0.1 b0 | up to ~1.0 (guarded) | Compact-object |
| TRM39 (compactness sweep) | variable | variable | traces up to φ → 1 | Strong-field probe |

### Dimensionless (G=c=1) φ Ranges

| Test | M | r range | φ range | Classification |
|:---|:---|:---|:---|:---|
| MC14 | 0.01 | [8, 30] | [3.33e-4, 1.25e-3] | DIMENSIONLESS TEST φ |
| MC13 | N/A | N/A | [1e-6, 2e-3] | DIMENSIONLESS TEST φ |
| CML10 | N/A | N/A | [1e-3, 1e-2] | DIMENSIONLESS TEST φ |
| CML11 | N/A | N/A | 0.17 | DIMENSIONLESS TEST φ |

### Transport φ (n_eff computation)

| Test | φ range | Context |
|:---|:---|:---|
| TRM78 (n_eff) | [1e-6, 0.10] | n_eff = 2 + λ_t·φ + λ_s·φ²·|μ̇| — formula validity check |
| TRM84 (memory) | [1e-3, 4e-3] | φ² scaling of local memory term |
| All transport RK4 steps | Dynamic: φ = GM/(c²r) per position | Full deflection computation |

---

## # Physical Scale Mapping

### Can Realistic (M, r) Produce φ ≈ 0.17?

#### Physical φ Formula

```
φ = GM/(c²r) = (G/c²) · (M/r) ≈ 7.426 × 10⁻²⁸ · (M[kg] / r[m])
```

#### Real Physical Scenarios

| Scenario | M [kg] | r [m] | φ | r/r_s |
|:---|:---|:---|:---|:---|
| Solar surface | 1.989 × 10³⁰ | 6.96 × 10⁸ | **2.1 × 10⁻⁶** | 4.7 × 10⁵ |
| Earth surface | 5.97 × 10²⁴ | 6.37 × 10⁶ | **7.0 × 10⁻¹⁰** | 7.2 × 10⁸ |
| Galaxy (MW, 10 kpc) | ~2 × 10⁴² | ~3 × 10²⁰ | **~5 × 10⁻⁶** | ~10¹⁵ |
| Neutron star surface | 3 × 10³⁰ | 1 × 10⁴ | **0.22** | ~3 |
| Black hole horizon | any | r_s = 2GM/c² | **0.50** | 1.0 |
| **BRIDGE-BAND TARGET** | **—** | **—** | **0.17** | **~3 r_s** |

#### Key Finding

φ ≈ 0.17 corresponds to r ≈ 3 × Schwarzschild radius (r_s = 2GM/c²). This is:

| Regime | φ | r/r_s | Exists in repository? |
|:---|:---|:---|:---|
| Galactic | ~10⁻⁶ .. 10⁻⁵ | ~10¹⁵ | YES — SPARC data (V2.2), but NO CLOCK-BIAS TESTING |
| Solar | ~10⁻⁶ | ~10⁵ | YES — TRM34-41, RealityTests |
| Neutron star | ~0.1 .. 0.3 | ~2 .. 5 | YES — TRM38 massFactors up to 10×, TRM39 compactness sweep |
| Black hole horizon | 0.5 | 1.0 | NO — explicitly guarded (phi >= 1.0 triggers invalid) |
| **Bridge-band target** | **0.17** | **~3 r_s** | **NO — no galactic test reaches this φ** |

**Bridge-band φ ≈ 0.17 is the compact-object regime (r ~ 3r_s), not the galactic regime.**

---

## # Cross-Check with V2.2

### Does a0 or SPARC Imply Any φ Scale?

| Field | Value |
|:---|:---|
| **V2.2 calibration** | g_eff = g_bar + √(g_bar·a0) fitted to SPARC (2839 data points) |
| **Result** | a0 ≈ 1.02 × 10⁻¹⁰ m/s², σ ≈ 0.141 dex |
| **Physical meaning of a0** | Acceleration scale below which the √(g_bar·a0) term dominates — galactic outer regions |
| **Link to φ?** | **NONE.** a0 is dimensional (m/s²). φ is dimensionless. They live in completely different sub-systems. |
| **Link to γ?** | **NONE.** BB06 established: no path from a0 to EulerBridgeScale = 0.85. |
| **Link to Ω?** | **NONE.** The bridge band [1.16, 1.19] is a CML grid-scan result; a0 determines acceleration, not frequency. |

### PhysicalConstants.A0_Cosmic

```
PhysicalConstantsSI.cs: `public const double A0_Cosmic = 1.2e-8; // cm/s²`
= 1.2 × 10⁻¹⁰ m/s²
```

This is comparable to the V2.2 SPARC fit (a0 ≈ 1.02 × 10⁻¹⁰) but stored as an independent constant with no code path connecting it to EulerBridgeScale, γ, Ω, or φ in the CML.

### The g_bar → φ Relationship (Indirect)

In the transport model, for a point source at distance r:
```
g_bar = GM/r²
φ = GM/(c²r)
→ g_bar = φ·(c²/r)
```

For a fixed r, g_bar ∝ φ. When g_bar ≈ a0, we have:
```
a0 = φ_a0 · (c²/r)
→ φ_a0 = a0 · r / c²
```

For r = 10 kpc ≈ 3.086 × 10²⁰ m:
φ_a0 = (1.02 × 10⁻¹⁰) × (3.086 × 10²⁰) / (8.98755 × 10¹⁶) ≈ 3.5 × 10⁻⁷

This is the φ at which the MOND-like transition occurs — **200,000× smaller than the bridge-band φ = 0.17**.

**Classification: NO PATH EXISTS. a0 and φ = 0.17 operate at incompatible physical scales.**

---

## # Dependency Map

```
M (source mass)                                      [DEFINED — PhysicalConstantsSI.M_Solar]
│
├── r (distance)                                     [DEFINED — PhysicalConstantsSI.b]
│
└── φ = GM/(c²r)                                     [DEFINED — PhotonTransportModel.Phi()]
        │
        ├── Solar: φ ~ 2 × 10⁻⁶                      [PHYSICAL — TRM34-41]
        ├── Galactic: φ ~ 5 × 10⁻⁶                   [PHYSICAL — V2.2 SPARC data]
        ├── Compact: φ ~ 0.1–0.5                     [PHYSICAL — TRM38/39]
        │
        └── → CML ClockBiasPhi = 0.17?               [NOT PHYSICAL — dimensionless parameter]
                │
                ├── Gap: 10⁻⁶ (galactic) → 0.17       [GAP ~ 5 × 10⁴ too large]
                ├── Gap: 0.17 = r ≈ 3r_s              [COMPACT-OBJECT, not galactic]
                │
                └── α·φ                               [α = 1.0 from FixationTests]
                        │
                        └── Ω* = 1.0 + α·φ             [VALIDATED — CML10/CML11]
                                │
                                └── γ = 1/Ω*           [DEFINITIONAL]
                                        │
                                        └── bridge band [TARGET — dynamically reachable]

PHYSICAL CHAIN:
Real galaxy: φ ~ 5×10⁻⁶ → α·φ ~ 5×10⁻⁶ (α=1.0) → Ω* ~ 1.000005 → γ ~ 0.999995
→ Bridge band NOT reached

CML CHAIN:
CML parameter: φ = 0.17 → α·φ = 0.17 → Ω* = 1.17 → γ = 0.85 → bridge band reached
→ BUT: no physical M, r produce φ = 0.17 in a galactic context
```

Mark each step: **DEFINED / PHYSICAL / OPEN / GAP / VALIDATED**

---

## # Minimal Mapping Gap

### What is missing to connect φ = 0.17 to physical inputs?

**Three nested gaps:**

#### Gap 1: Scale Mismatch (5 × 10⁴)

| Source | φ | Ratio to bridge-band φ = 0.17 |
|:---|:---|:---|
| Solar surface | 2.1 × 10⁻⁶ | 8.1 × 10⁻⁵ |
| Galaxy (10 kpc) | 5 × 10⁻⁶ | 2.9 × 10⁻⁵ |
| MOND transition φ | 3.5 × 10⁻⁷ | 2.1 × 10⁻⁶ |
| **Bridge-band target** | **0.17** | **1.0** |

Real galactic φ values are ~50,000× smaller than the bridge-band requirement. To close this gap with α = 1.0, the CML would need r ≈ 3r_s (compact object), not r ≈ galaxy radius.

#### Gap 2: Alpha Calibration (Undefined for Galactic Context)

α = 1.0 is the ONLY value in the repository (FixationTests, MC13/MC14 validated). For α·φ_galactic ≈ 0.176 with φ_galactic ≈ 5 × 10⁻⁶:

```
α_required = 0.176 / (5 × 10⁻⁶) ≈ 3.5 × 10⁴
```

An amplification factor of 35,000× — completely unjustified by any existing mechanism or calibration.

**The clock-bias term α·φ was designed and tested for φ ~ 10⁻⁶ (weak-field, FixationTests). Using α = 1.0 with φ = 0.17 represents the compact-object regime at ~3× Schwarzschild radius — not the galactic regime where the bridge band is supposed to operate.**

#### Gap 3: CML Normalization (ω_i Baseline)

The CML ω_i = 1.0 is a dimensionless normalization ("one tick per tick"). The clock-bias shift α·φ = 0.17 means the collective frequency is 17% above the intrinsic baseline. In physical terms, this would mean:

- Galactic time dilation is ~17% — **impossible** (real galactic time dilation is ~10⁻⁶)
- OR: ω_i = 1.0 is NOT the "flat space" baseline and represents a different physical scale

**The bridge-band success (CML11: Ω* = 1.17) works in dimensionless dynamics but corresponds to the compact-object regime (r ~ 3r_s) in physical units, not the galactic regime. The CML tick normalization at 1.0 does not align with any physically-derived frequency scale.**

---

## # Failure Modes

1. **Scale collapse.** φ = 0.17 in the CML works dynamically but maps to r ~ 3r_s physically. This is compact-object physics, not galactic dynamics. A test designed to explain galactic rotation curves produces φ values characteristic of neutron-star surfaces.

2. **Alpha as deus ex machina.** To make galactic-scale φ (~10⁻⁶) produce Ω* ≈ 1.17, α would need to be ~35,000. No such amplification is defined, derived, or calibrated anywhere in the repository.

3. **Independence of M and r.** The CML has no spatial structure. Lattice sites are indexed (0..N−1), not positioned. The concept of "distance from a mass source" has no natural representation in the CML framework. This is a category mismatch between the dimensionless phase-lattice and the physical transport model.

4. **SPARC data is disconnected from the frequency domain.** The V2.2 SPARC fit calibrates a0 ≈ 1.02 × 10⁻¹⁰ m/s² for the acceleration scale, but this calibration lives entirely in the transport/deflection subsystem. It does not influence the CML frequency scale, the ω_i baseline, or the clock-bias parameter.

---

## # Final Question

### Is φ ≈ 0.17:

**A)** achievable from realistic physical inputs
**B)** indirectly implied by existing physical scale
**C)** only reachable by rescaling / reinterpretation
**D)** purely a dimensionless free parameter

**Answer: D — purely a dimensionless free parameter in the CML.**

**Justification:**

| Criterion | Status |
|:---|:---|
| **Can realistic (M, r) produce φ = 0.17?** | **NO.** Galactic φ ~ 10⁻⁶. Solar φ ~ 10⁻⁶. φ = 0.17 requires r ~ 3r_s (compact-object, not galactic). |
| **Is φ = 0.17 implied by any existing physical scale?** | **NO.** The SPARC fit calibrates a0 (m/s²), not φ. No path maps a0 → φ → Ω*. The MOND transition φ ~ 4 × 10⁻⁷ is 400,000× smaller. |
| **Does the CML have a mass or distance scale?** | **NO.** The CML operates in pure dimensionless phase space. Lattice sites have no spatial positions. M and r are undefined. |
| **Is φ = 0.17 needed from physics or from the chosen normalization?** | **FROM THE NORMALIZATION.** The CML ω_i baseline at 1.0 is a convenient dimensionless normalization ("one tick"). The bridge-band offset 0.17 (= 1/0.85 − 1) is the reciprocal relation to the hardcoded γ = 0.85. φ = 0.17 was chosen to produce the desired Ω* via α·φ, not because any physical φ evaluates to 0.17. |
| **Could φ = 0.17 become physical through reinterpretation?** | **POSSIBLE (C).** If the ω_i = 1.0 baseline is reinterpreted as representing an already-shifted galactic frequency (not "flat space"), then φ = 0.17 would represent an incremental offset. But no such reinterpretation exists in the repository. |
| **Current status** | **D.** φ = 0.17 is a dimensionless parameter with no physical anchor in the repository. It was selected because α·φ = 0.17 = 1/0.85 − 1 = 1/γ − 1, connecting it to the hardcoded EulerBridgeScale via the clock-bias mechanism. The value traces to γ = 0.85, which traces to the rational fraction 17/20 = 1/1.176, which traces to a mid-band candidate. |

**The complete chain:**

```
Convenient rational: 20/17 ≈ 1.176 → γ = 17/20 = 0.85 → Ω = 1/γ ≈ 1.176
                                                              │
Clock-bias: dθ/dt = ω_i + α·φ → need α·φ = 1/γ − 1 = 0.176  │
                                                              │
With α = 1.0 → φ = 0.17 ←───── dimensionless free parameter ──┘
```

**φ = 0.17 has no physical origin. It is the value required to produce the hardcoded γ = 0.85 via the clock-bias mechanism with α = 1.0. The dynamics work (CML11), but the physics link to any real gravitational potential is absent.**
