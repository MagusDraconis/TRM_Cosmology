# BB06 — Origin of γ (EulerBridgeScale Audit)

## Scope

BB05 established that ΔΩ ≈ 0.176 is induced by transport scaling:
γ ≈ 0.85 → Ω = 1/γ ≈ 1.176. This document traces the ORIGIN of γ ≈ 0.85 itself.

**Rules:** No derivations. No new assumptions. Only evidence mapping from existing repository content.

---

## # γ Definition

### γ in the Transport Model

| Field | Value |
|:---|:---|
| **File** | `PhotonTransportModel.cs:81` |
| **Definition** | `public double EulerBridgeScale { get; init; } = 0.85;` — default value, configurable |
| **Role** | Scales the EL/Fermat transport acceleration: `accelScale = c·c·nEff·EulerBridgeScale` |
| **Classification** | **IMPLEMENTATION DEFAULT** |

### γ in the Synchronization Score (Primary)

| Field | Value |
|:---|:---|
| **File** | `PhotonTransportModel_GeodesicSolverTests.cs:1145-1159` |
| **Function** | `ComputeCollectiveSynchronizationScore(double gamma)` |
| **Role** | Hypothesis-level scoring proxy: creates a Gaussian peaked at `collectiveCenter = 0.85` and penalizes `localCenter = 1.0` |
| **Return value** | `Math.Exp(-(gamma-0.85)²/0.03²) − 0.45·Math.Exp(-(gamma-1.0)²/0.06²)` |
| **Classification** | **HARDCODED PRIOR CENTER** |
| **Evidence of circularity** | The function is explicitly built to maximize at γ = 0.85. Scanning a γ grid always returns 0.85 because the scoring function IS a Gaussian centered at 0.85. A comment acknowledges this: "Hypothesis-level synchronization proxy: collective mode around gamma≈0.85 is favored." |

### γ in the Phase-Synchronization Solver Score (Secondary)

| Field | Value |
|:---|:---|
| **File** | `PhotonTransportModel_GeodesicSolverTests.cs:1045-1053` |
| **Function** | `RunPhaseSynchronizationSolverScore(double gamma)` |
| **Role** | Runs actual Kuramoto dynamics (SimulateModeLock-like), but mixes in a 0.85-centered prior |
| **Score formula** | `score = 0.35·orderScore + 0.65·Gaussian(gamma, 0.85, 0.05) + 0.12·Gaussian(Ω, 20/17, 0.025)` |
| **Classification** | **HARDCODED PRIOR DOMINANCE (65%)** |
| **Evidence of circularity** | 65% of the score weight is a Gaussian centered at 0.85. Only 23% is actual synchronization dynamics. The test "EL05_EulerBridgeScale_Should_Match_CollectiveGammaPeak" calls this function, which returns ≈ 0.85 because the prior-dominated score was built to peak there. The test then asserts that `EulerBridgeScale = gammaPeak` produces acceptable EL ratios — this is a CONSISTENCY check, NOT a derivation. |

---

## # Physical Anchors

### V2.2 SPARC Calibration

| Field | Value |
|:---|:---|
| **File** | `TRM_V2_2.tex:290-301` |
| **Concept** | g_eff = g_bar + √(g_bar·a0) fitted to SPARC dataset (2839 data points). The fit recovers a0 ≈ 1.02 × 10⁻¹⁰ m/s² with σ ≈ 0.141 dex. |
| **What is calibrated** | a0 — the characteristic TRM acceleration scale. This is a DIMENSIONAL constant with physical units. |
| **Relationship to γ** | **NONE.** The V2.2 document does not mention EulerBridgeScale, γ, 0.85, or 20/17. The SPARC calibration determines a0, not γ. |
| **Classification** | **EMPIRICAL CALIBRATION — unrelated to γ** |

### PhysicalConstants.cs

| Field | Value |
|:---|:---|
| **File** | `PhysicalConstants.cs:27` |
| **Definition** | `public const double A0_Cosmic = 1.2e-8; // cm/s^2` — Milgrom acceleration constant |
| **Value** | 1.2 × 10⁻¹⁰ m/s² (comparable to V2.2 SPARC fit: a0 ≈ 1.02 × 10⁻¹⁰ m/s²) |
| **Relationship to γ** | **NONE.** Stored as an independent physical constant with no mapping to EulerBridgeScale. |
| **Classification** | **EMPIRICAL CONSTANT — unrelated to γ** |

### Code-to-Theory Audit Classification

| Field | Value |
|:---|:---|
| **File** | `TRM_Code_To_Theory_Audit.md:199-209` |
| **Classification of γ** | Not listed among calibrated/fitted/heuristic parameters. The audit lists a0 (DefaultA0_Ms2 = 1.2e-10) as "kalibrierter Arbeitswert" (calibrated working value), DefaultPhiBeta (0.4) as "heuristic/calibrated", and DefaultRegimeGamma (0.25) as "heuristic/calibrated". EulerBridgeScale is NOT listed — implying it is treated as a default, not a calibrated value. |
| **Classification** | **NOT IN CALIBRATED PARAMETER REGISTRY** |

---

## # Structural Candidates

### Candidate 1: EL/Fermat Weak-Field Validation Window

| Field | Value |
|:---|:---|
| **Files** | `PhotonTransportModel_GeodesicSolverTests.cs`, EL04, EL08 |
| **Concept** | EL deflection ratio relative to Schwarzschild reference must stay in [0.85, 1.25] for weak-field consistency. |
| **How it could constrain γ** | If γ is too small or too large, the EL/Schwarzschild ratio leaves the acceptable window. |
| **Does it fix γ ≈ 0.85 specifically?** | **NO.** EL08 shows that with gammaPeak ≈ 0.85, EL ratio ∈ [0.85, 1.25] at tested ε values. But this only verifies CONSISTENCY — it doesn't SELECT 0.85. Any γ producing EL ratio between ~0.75 and ~1.30 would pass. The test window is broad. |
| **Classification** | **BROAD CONSISTENCY CHECK — not a selective constraint** |

### Candidate 2: EL07 — Epsilon-Independent Best-γ

| Field | Value |
|:---|:---|
| **File** | `PhotonTransportModel_GeodesicSolverTests.cs:264-322` |
| **Concept** | For each ε, scans 0.65..1.05 to find the γ that MINIMIZES |α_EL − α_Schwarz|. The "best" γ varies across ε values with spread < 0.20. |
| **Observed results** | meanScale of best γ across ε values is roughly 0.85-0.90 region, spread up to 0.17. |
| **Does it fix γ ≈ 0.85?** | **WEAKLY.** The ε-independent best-γ clusters in a similar region, providing a WEAK independent anchor. But the spread is large (up to 0.20), and the test asserts only that |meanScale − gammaPeak| ≤ 0.12 — where gammaPeak is the PRIOR-DOMINATED value (0.85). This is a cross-consistency check between two methods that both operate in the same region. |
| **Classification** | **WEAK CROSS-CONSISTENCY — confirms region, doesn't select value** |

### Candidate 3: The Microscopic Clock-Bias Equation

| Field | Value |
|:---|:---|
| **File** | `TRM_Geodesic_Derivation.md:514-524` |
| **Equation** | dθ_a/dt = ω_0 + α·φ(x_a) + Σ K·sin(θ_b − θ_a) |
| **Potential for γ** | If α·φ_galactic ≈ 0.17, then the collective frequency baseline shifts from ω_0 to ω_0 + α·φ, providing a structural source for 1/γ − 1 = 0.176. |
| **Status** | α is undefined. φ is not connected to CML. The clock-bias is not implemented in SimulateModeLock. Not a working constraint. |
| **Classification** | **DOCUMENTED MECHANISM — not activated** |

### Candidate 4: a0-to-γ Mapping Through Transport

| Field | Value |
|:---|:---|
| **Concept** | Since a0 ≈ 1.0-1.2 × 10⁻¹⁰ m/s² is calibrated from SPARC, and γ controls transport acceleration via `accelScale = c²·n_eff·γ`, perhaps a0 implies γ. |
| **Does such a mapping exist?** | **NO.** No document, code path, or derivation connects a0 (dimensional, m/s²) to γ (dimensionless, default 0.85). The transport model operates in dimensionless units (G=c=1) with a0 already absorbed into normalization. The EulerBridgeScale operates at a different level — it scales the EL/Fermat path against the transport RK4 path, not against physical a0. |
| **Classification** | **NO PATH EXISTS** |

---

## # Dependency Map

```
SPARC data (2839 galactic data points)
   │
   ├── V2.2 fit: g_eff = g_bar + √(g_bar·a0)          [EMPIRICAL CALIBRATION]
   │       │
   │       └── a0 ≈ 1.02 × 10⁻¹⁰ m/s²                [CALIBRATED physical constant]
   │               │
   │               └── PhysicalConstants.A0_Cosmic     [IMPLEMENTATION DEFAULT ≈ 1.2 × 10⁻¹⁰]
   │                       │
   │                       └── → γ?                   [NO PATH EXISTS — independent parameter]
   │
   ├── EulerBridgeScale γ = 0.85                        [HARDCODED DEFAULT]
   │       │
   │       ├── PhotonTransportModel.Parameters:81       [default value]
   │       ├── ComputeCollectiveSynchronizationScore    [hardcoded priorCenter = 0.85]
   │       └── RunPhaseSynchronizationSolverScore       [65% weight priorCenter = 0.85]
   │
   ├── EL07 ε-independent best-γ scan                   [WEAK CROSS-CONSISTENCY]
   │       └── meanScale ≈ 0.85-0.90, spread < 0.20   [confirms region, not value]
   │
   ├── γ = 1/Ω                                          [DEFINITIONAL — reciprocal mapping]
   │       │
   │       └── Ω ≈ 1.176 (20/17)                       [CONSEQUENCE of γ ≈ 0.85]
   │               │
   │               └── bridge band [1.16, 1.19]        [CML grid scan + prior assisted]
   │
   └── Clock-bias eq: dθ/dt = ω_0 + α·φ + ΣK·sin       [DOCUMENTED — not implemented]
           └── Could connect γ to a0 via φ              [OPEN — α uncalibrated]
```

---

## # Minimal Origin

### What Is the Smallest Existing Mechanism That Can Fix γ?

**Answer: NONE.** No repository mechanism fixes γ to 0.85.

The current value enters the system at three points, all of which are hardcoded:

1. `PhotonTransportModel.cs:81`: `EulerBridgeScale = 0.85` as default parameter
2. `ComputeCollectiveSynchronizationScore`: `collectiveCenter = 0.85` as hypothesis target
3. `RunPhaseSynchronizationSolverScore`: `priorCenter = 0.85` dominating 65% of score weight

The only mechanism that COULD fix γ is the microscopic clock-bias equation dθ/dt = ω_0 + α·φ, with α calibrated so that α·φ_galactic ≈ 1/γ − 1 = 0.176. But α is undefined, φ is not connected, and the equation is not implemented in the active CML framework.

### What Is the Historical Origin?

The value 20/17 ≈ 1.176 (γ = 17/20 = 0.85) appears as a "representative candidate inside the band" (per `TRM_Collective_Mode_Locking_BridgeScale.md:49`). The CML diagnostics show a competitive band around Ω ∈ [1.16, 1.19], and 20/17 was selected as the most convenient rational representative within that band. The γ = 0.85 value is the reciprocal: 0.85 = 17/20. It was then hardcoded into test scoring functions as the hypothesis target, and into PhotonTransportModel as the default.

The value was never derived from a0, from SPARC data, from galactic rotation curves, or from any structural constraint. It was chosen because it is the reciprocal of the mid-band rational cadence candidate.

---

## # Failure Modes

1. **Circular scoring at γ = 0.85.** Both scoring functions embed 0.85 as a Gaussian prior. Any scan over γ will return ≈ 0.85 regardless of the dynamics because the score peak is structurally determined by the prior, not by the synchronization order parameter.

2. **The SPARC calibration is disconnected.** a0 ≈ 1.0-1.2 × 10⁻¹⁰ m/s² is calibrated from galactic data. No code path connects a0 to EulerBridgeScale. The two parameters live in independent subsystems with no documented mapping.

3. **EL07 cross-consistency is fundamentally circular.** EL07 compares two methods of estimating γ: the prior-dominated score (EL05) and ε-dependent best-fit. The test asserts they agree within 0.12. They both land near 0.85: the prior-dominated method because it was BUILT to, and the ε-fit method because the EL path with γ ≈ 0.85 happens to match Schwarzschild deflection. This is a consistency check, not a derivation — both methods could agree on 0.85 without either deriving it.

4. **The clock-bias equation is the only structural mechanism, and it's dormant.** The equation dθ/dt = ω_0 + α·φ(x) would connect the gravitational potential to the intrinsic frequency baseline, potentially explaining why ω_0 = 1.0 isn't the correct baseline for the galactic regime. But with α undefined and φ unconnected, this remains theoretical infrastructure only.

---

## # Final Question

### Is γ ≈ 0.85:

**Answer: D — arbitrary/default choice.**

**Justification:**

1. **No derivation exists.** Every repository document that mentions γ explicitly states it is NOT derived: "γ ≈ 0.85 is not a uniquely derived fundamental constant" (Geodesic_Derivation.md:495, Peer_Review_Request.md:292); "EulerBridgeScale ≈ 0.85: validated as prior-assisted collective bridge scale; not yet first-principles emergent" (Collective_Mode_Locking_20_17.md:39).

2. **No observational calibration exists for γ.** The SPARC dataset calibrates a0 ≈ 1.0-1.2 × 10⁻¹⁰ m/s², a dimensional acceleration scale. There is no documented relationship between a0 and the dimensionless γ. EulerBridgeScale is not listed in the Code-to-Theory Audit's calibrated parameter registry.

3. **No structural constraint selects 0.85.** The EL validation window [0.85, 1.25] constrains γ to produce physically reasonable deflection ratios, but this is a broad bound. EL07 shows that the ε-independent best-γ clusters in the 0.85-0.90 region, but with spread up to 0.20. The specific value 0.85 is not uniquely selected by any structural constraint.

4. **The value enters the system as a hardcoded prior.** Both scoring functions hardcode `priorCenter = 0.85`. The `EulerBridgeScale` default is 0.85. These are implementation defaults, not derived outcomes. The scoring functions then "discover" that γ ≈ 0.85 is preferred — a circular result.

5. **The historical path supports D.** The bridge band was identified empirically (CML01–CML08) as Ω ∈ [1.16, 1.19]. The rational candidate 20/17 ≈ 1.176 was chosen as a convenient mid-band representative. γ = 17/20 = 0.85 was then adopted as the EulerBridgeScale default and baked into scoring priors. The value traces to a convenient rational fraction, not to a physical constraint.

**Not A** because: No derivation from theory exists. The repository explicitly disclaims derivation status.

**Not B** because: The SPARC calibration fits a0 (dimensional), not γ (dimensionless). No path maps a0 to γ.

**Not C** because: While the EL window provides a BROAD structural consistency check, the specific value 0.85 is not uniquely selected. Any γ producing EL ratio ∈ [0.85, 1.25] would satisfy the constraint.

**γ ≈ 0.85 is the reciprocal of a convenient mid-band rational candidate (20/17), hardcoded as default and prior center, with no independent physical justification.**

---

## # Implication for the Bridge-Band Program

BB05 showed that ΔΩ ≈ 0.176 is induced by γ ≈ 0.85 → Ω = 1/γ ≈ 1.176.

BB06 shows that γ ≈ 0.85 itself has no physical origin in the repository.

**Therefore the entire bridge-band chain traces back to a hardcoded default:**

```
γ = 0.85 (hardcoded default / prior center)
  → Ω = 1/γ ≈ 1.176 (definitional reciprocal)
    → Band [1.16, 1.19] (CML grid scan + prior)
      → m = 3 (ansatz-dependent empirical fit)
```

The bridge band is THREE LEVELS removed from any physical constraint:
1. γ is a hardcoded default (not derived, not calibrated)
2. Ω = 1/γ is definitional (not emergent)
3. The band bounds are determined by CML score ranking with prior assistance (not structural)

**The minimal missing link to close this chain is:**

> Calibrate α in the microscopic clock-bias equation dθ/dt = ω_0 + α·φ such that α·φ_galactic ≈ 1/γ − 1 = 0.176, establishing a structural path from the gravitational potential (φ) through the transport model (a0) to the collective frequency shift (γ = 1/Ω).

Until this link is closed, γ ≈ 0.85 and the entire bridge band remain empirical inputs with no structural origin within the repository.
