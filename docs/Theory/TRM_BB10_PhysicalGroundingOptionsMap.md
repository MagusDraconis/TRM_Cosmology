# BB10 — Physical Grounding Options Map

## Scope

This document maps possible routes to physically ground γ ≈ 0.85 (or equivalently φ ≈ 0.17)
using only existing repository structure. No derivation is attempted. No new physics is introduced.

---

## # Candidate Routes

### Route A: M, r → φ = GM/(c²r) → α·φ → Ω*

| Aspect | Status |
|:---|:---|
| **Concept** | Compute φ from a physical mass M at distance r, feed into clock-bias α·φ to produce Ω* shift |
| **Classification** | **PARTIAL — infrastructure exists; scale mismatch blocks execution** |

#### What Exists

| Component | File | Status |
|:---|:---|:---|
| `PhotonTransportModel.Phi(G, M, c, r)` | `PhotonTransportModel.cs:111` | **DEFINED** — standard Newtonian φ |
| Physical constants (G, c, M_Solar) | `PhysicalConstantsSI.cs` | **DEFINED** — SI-calibrated |
| Physical φ ranges (TRM34–TRM41) | `TRM_Realtiy_Tests.cs:1529–2130` | **VALIDATED** — solar to compact-object |
| Clock-bias phase update | `CollectiveModeLockingTests.cs:8207` | **INTEGRATED** — `dθ/dt = ω_i + α·φ + K·coupling` |
| α = 1.0 | `PhotonTransportModel_FixationTests.cs:1439` | **DEFINED** — valid for FixationTests regime |
| Dimensionless φ in MC14 | `PhotonTransportModel_FixationTests.cs:893` | **VALIDATED** — φ ∈ [3.3e-4, 1.25e-3] at M = 0.01, r ∈ [8, 30] |

#### What Is Missing

| Gap | Detail |
|:---|:---|
| **Spatial positions in CML** | CML lattice sites are indexed (0…N−1), not positioned. No r_i exists. |
| **M and r definition** | No mass source or distance scale in the dimensionless CML framework. M = 0.01 in MC14 is dimensionless and arbitrary. |
| **Scale bridge** | Galactic φ ≈ 5 × 10⁻⁶ with α = 1.0 gives ΔΩ* = 5 × 10⁻⁶ — **34,000× too small** for ΔΩ ≈ 0.17. |
| **φ = 0.17 physical mapping** | Maps to r ≈ 3 r_s (compact-object regime), not galactic. No galaxy produces this φ. |

#### Compatibility with CML

| Check | Result |
|:---|:---|
| Clock-bias insertion is mechanical? | **YES** — one line: `clockBiasShift = alpha * phi` |
| α defined? | **YES** — 1.0, from FixationTests |
| φ computable from positions? | **NO** — no spatial positions in CML |
| φ can reach 0.17 from realistic M, r? | **NO** — galaxy φ ~ 10⁻⁶, gap factor ~34,000× |
| | **OR** requires α ~ 34,000 — unjustified amplification |

**Blocked by: spatial structure + scale mismatch.**

---

### Route B: Galactic φ + Calibrated α → Ω*

| Aspect | Status |
|:---|:---|
| **Concept** | Accept galactic-scale φ (~10⁻⁶‥10⁻⁵), calibrate α so α·φ ≈ 0.176 in the CML frequency domain |
| **Classification** | **MISSING — α uncalibrated; no structural principle sets its value** |

#### What Exists

| Component | File | Status |
|:---|:---|:---|
| α = 1.0 (hardcoded) | `PhotonTransportModel_FixationTests.cs:1439` | **DEFINED** — validated in FixationTests for φ ~ 1e-6‥2e-3 |
| `ClockBiasAlpha` parameter | `CollectiveModeLockingTests.cs` (ModeLockConfig) | **AVAILABLE** — configurable, default 1.0 |
| Linear φ response validated | MC13, MC14, CML10 | **VALIDATED** — α·φ shift is demonstrably linear |
| Galactic RAR | SPARC data, 2839 galaxies | **CALIBRATED** — g_obs vs g_bar relation fitted |
| a0 acceleration scale | a0 ≈ 1.02 × 10⁻¹⁰ m/s² (SPARC) | **CALIBRATED** — but disconnected from frequency |

#### What Is Missing

| Gap | Detail |
|:---|:---|
| **α calibration** | No mechanism relates FixationTests lattice α (= 1.0, ω_i ~ 0) to CML α (= 1.0, ω_i ~ 1.0). |
| **Frequency-scale translation** | FixationTests ω_i center = 0 (sinusoidal). CML ω_i center = 1.0 (tick baseline). α operates in different regimes. |
| **Physical α from theory** | α = 1.0 is a test convenience, not a derived value. No equation sets α. |
| **Required α** | For galactic φ ~ 5 × 10⁻⁶ and ΔΩ* = 0.176: α_required ≈ 35,000. No justification exists. |

#### Compatibility with CML

| Check | Result |
|:---|:---|
| α is configurable? | **YES** — `ClockBiasAlpha` in ModeLockConfig |
| α magnitude justified? | **NO** — α = 1.0 is 35,000× too small for galactic φ → bridge-band |
| α calibration possible from existing data? | **NO** — no cross-calibration between FixationTests and CML frequency regimes |

**Blocked by: α is a free parameter with no structural constraint.**

---

### Route C: a0 → γ → Ω

| Aspect | Status |
|:---|:---|
| **Concept** | Map the MOND acceleration scale a0 to bridge-scale γ via a structural frequency-domain relation |
| **Classification** | **MISSING — no link between a0 and γ exists in any repository code path** |

#### What Exists

| Component | File | Status |
|:---|:---|:---|
| SPARC RAR analysis | `SparcRarAnalysis.cs` + `RarRelationTests.cs` (RAR01–RAR15) | **CALIBRATED** — a0 ≈ 1.02 × 10⁻¹⁰ m/s² from 2839 galaxies |
| MOND comparison | `SparcRarAnalysis.FitA0(ModelType.MOND)` — RMS comparison with TRM | **BENCHMARKED** — TRM competitive with MOND on SPARC |
| `A0_Cosmic = 1.2e-8 cm/s²` | `PhysicalConstants.cs:27` | **STORED** — independently defined, no mapping to γ |
| `TrmDerivedParameters.GetA0_Ms2()` | Multiple orbital test files | **DEFINED** — returns derived a0 in m/s² |
| `EulerBridgeScale = 0.85` | `PhotonTransportModel.cs:81` | **DEFAULT** — no connection to a0 |
| `gTRM = √(gNewt · A0_Cosmic) / lambda` | `GalacticRotationAnalysis.cs:95` | **USED** — in TRM field solver, but λ is a free parameter, not γ |
| `n_eff = 2 + λ_t·φ + λ_s·φ²·|μ̇|` | `PhotonTransportModel.cs:330–334` | **TRANSPORT** — spatial domain; no frequency mapping |
| `accelScale = c²·nEff·EulerBridgeScale` | `PhotonTransportModel.cs:389` | **TRANSPORT** — γ used as acceleration factor, not derived from a0 |

#### What Is Missing

| Gap | Detail |
|:---|:---|
| **Dimensional mismatch** | a0 is dimensional (m/s²). γ is dimensionless (frequency ratio). No conversion exists. |
| **No a0 → γ code path** | Zero files connect `A0_Cosmic` or SPARC-fit a0 to `EulerBridgeScale`. |
| **No a0 → φ code path** | The MOND transition φ ~ a0·r/c² ≈ 4 × 10⁻⁷ is 400,000× smaller than bridge-band φ = 0.17. |
| **No a0 → ω_i relation** | CML ω_i = 1.0 is a normalization. a0 determines acceleration, not phase rate. |
| **No dimensional bridge** | c² is the only conversion factor available, yielding g ∼ c²/r for frequency-to-acceleration. No repository code performs this. |

#### Compatibility with CML

| Check | Result |
|:---|:---|
| a0 is calibrated? | **YES** — SPARC, 2839 data points |
| a0 maps to any CML parameter? | **NO** — completely disconnected subsystem |
| γ = 0.85 and a0 = 1.02 × 10⁻¹⁰ consistent? | **UNRELATED** — no shared code path, no shared formula |

**Blocked by: no infrastructure. The acceleration and frequency domains are structurally disconnected.**

---

### Route D: Transport Coupling → γ (via n_eff or memory channel)

| Aspect | Status |
|:---|:---|
| **Concept** | Derive γ from the transport model's internal structure (n_eff scaling, λ parameters, memory channel) |
| **Classification** | **INDIRECT — transport parameters exist but no mechanism yields γ = 0.85** |

#### What Exists

| Component | Detail | Status |
|:---|:---|:---|
| n_eff = 2 + λ_t·φ + λ_s·φ²·|μ̇| | Transport index with λ_t = 1.0, λ_s = 30.0 | **CALIBRATED** |
| Memory channel φ² μ̇ | FixationTests validates φ² scaling | **VALIDATED** |
| kEff = kBase + kPhi·φ | Coupling grows with φ in FixationTests (kPhi = 120.0) | **VALIDATED** |
| EulerBridgeScale = 0.85 | Scales EL/Fermat acceleration to match Schwarzschild deflection | **EMPIRICAL FIT** |
| EL01–EL17 | Geodesic solver tests using γ = 0.85 | **VALIDATED** |

#### What Is Missing

| Gap | Detail |
|:---|:---|
| **No n_eff → γ relation** | n_eff is spatial (refractive index), γ is temporal (frequency ratio). No equation connects them. |
| **No λ → γ mapping** | λ_t, λ_s are calibrated from transport deflection, not from frequency/phase dynamics. |
| **No memory → frequency path** | The memory channel (φ²·|μ̇|) contributes to refractive index, not to phase rate. |
| **EL07 is circular** | γ = 0.85 is hardcoded in the scoring function; EL tests "discover" what they were built to find (BB06). |

**Blocked by: transport parameters determine spatial deflection, not temporal frequency.**

---

### Route E: Rational Cadence → γ (Phenomenological)

| Aspect | Status |
|:---|:---|
| **Concept** | Accept γ = 17/20 = 0.85 as a rational approximation inside the empirical bridge band. No physical derivation claimed. |
| **Classification** | **READY — this is the current state; it is phenomenological, not physical** |

#### What Exists

| Component | Detail | Status |
|:---|:---|
| CML grid scan | Ω ∈ [1.16, 1.19] identified with cadence prior | **EMPIRICAL** |
| γ = 17/20 | Rational reciprocal of mid-band candidate 20/17 | **CONVENIENT RATIONAL** |
| Cadence filter scoring | Penalizes deviations from rational target | **IMPLEMENTED** |
| CML11 | Ω* = 1.17 at φ = 0.17 — dynamically reachable | **VALIDATED** |

#### Assessment

This route is the **status quo**. γ = 0.85 is accepted as a phenomenological parameter. The bridge band is a consequence of this choice (plus clock-bias dynamics), not a prediction. This is a defensible position for a phenomenological theory — parameters are measured, not derived. But it does **not** answer the physical grounding question.

**Not blocked — this route works. It simply does not claim derivation.**

---

## # Required Ingredients — Minimal Missing Elements per Route

| Route | Missing Ingredient | Classification |
|:---|:---|:---|
| **A** (M, r → φ) | Spatial positions in CML lattice + realistic M, r producing φ = 0.17 | **BLOCKED** — CML has no spatial structure |
| **B** (galactic φ + α) | α calibration principle fixing α ~ 35,000 (or rescaling ω_i) | **BLOCKED** — α = 1.0 is 35,000× too small |
| **C** (a0 → γ) | Dimensional bridge: a0 (m/s²) → γ (dimensionless frequency ratio) | **BLOCKED** — no infrastructure exists |
| **D** (transport → γ) | Relation mapping n_eff or λ parameters to frequency shift γ | **BLOCKED** — temporal and spatial domains disconnected |
| **E** (phenomenological) | Nothing — this is the status quo | **READY** |

---

## # Comparison Table

| Criterion | Route A | Route B | Route C | Route D | Route E |
|:---|:---|:---|:---|:---|:---|
| **Uses existing repo** | Partially | Partially | Minimally | Partially | Fully |
| **Infrastructure exists** | φ + clock-bias | α configurable + clock-bias | a0 calibrated | Transport λ calibrated | All validated |
| **Minimal missing piece** | Spatial positions in CML | α calibration principle | a0→γ mapping | n_eff→γ relation | None |
| **New code needed** | Extensive (spatial CML) | Moderate (α scan/calibrate) | Extensive (new theory) | Extensive (new theory) | None |
| **Scale mismatch** | 34,000× | 35,000× (in α) | 400,000× (in φ) | N/A (different domain) | None |
| **Structural change required** | Major (lattice geometry) | Minor (α parameter) | Major (new subsystem) | Major (new coupling) | None |
| **Produces physical grounding?** | Yes (if spatial) | Potentially (if α principled) | Yes (if mapping found) | Potentially | No (phenomenological) |
| **Classification** | **PARTIAL** | **MISSING** | **MISSING** | **MISSING** | **READY** |

---

## # Closest-to-Implementation Assessment

**Route E (phenomenological) is the only route currently implemented and validated.**

Of the physical-grounding routes:

1. **Route B (α calibration)** is the closest to implementation: α is already configurable via `ClockBiasAlpha`. The missing piece is a principle that sets its value. However, no such principle exists anywhere in the repository — α = 1.0 is a single hardcoded value.

2. **Route A (spatial φ)** is second-closest: the clock-bias mechanism works, φ is well-defined in `PhotonTransportModel.Phi()`. But adding spatial positions to the CML is a structural change requiring lattice geometry, M definition, and r computation — a non-trivial extension.

3. **Routes C and D** require entirely new theoretical infrastructure connecting currently disconnected subsystems. No implementation path exists.

---

## # Final Recommendation

### Most realistic next path: **Route E — Phenomenological**

**Justification:**

- γ = 0.85 is the current state and is fully validated.
- No existing repository infrastructure supports physical derivation of γ.
- The clock-bias mechanism explains *how* γ maps to Ω in the frequency domain, which is a structural advance over the prior state (γ as pure input).
- Accepting γ as a phenomenological parameter is defensible: many physical theories contain parameters measured from data rather than derived from first principles.

### If physical grounding is required: **Route B — α calibration (via rescaling ω_i baseline)**

**Justification:**

- The core problem is that α = 1.0 with galactic φ ~ 10⁻⁶ produces ΔΩ* ~ 10⁻⁶, while the bridge band requires ΔΩ* ~ 0.17.
- This gap is a factor of ~35,000 — the same order as the ratio between CML ω_i = 1.0 (dimensionless tick) and FixationTests ω_i ~ 0 (no baseline).
- If the ω_i = 1.0 baseline were reinterpreted as representing the *already-shifted* frequency at some reference gravitational potential (rather than "flat space"), the effective α·φ would be relative, not absolute.
- **No such reinterpretation exists in the repository.** But it would require only a rescaling argument, not new spatial structure.

### Least realistic: Routes A, C, D

These require either new spatial structure in the CML (Route A) or entirely new theoretical connections between disconnected subsystems (Routes C, D). None have implementation readiness.
