# TRM — Gravity Test Suite Audit

**Date:** 2026-07-05
**Purpose:** Classify every gravity-related test honestly — what does each test actually prove?
**Context:** V3.4 core is frozen. We must distinguish derivation from calibration from phenomenology.

---

## Classification System

| Class | Meaning | Example |
|:---|:---|:---|
| **DERIVED** | Follows from oscillator equations I1+I2 without additional fitting | FP28: ε=0 ⇔ m=3 |
| **CALIBRATED** | Requires one or more empirically-fit parameters to match data | SPARC a₀ fit from 2839 galaxies |
| **EFFECTIVE** | Tested against observation, not derived; works with calibration | Photon deflection at γ=0.85 |
| **EXPLORATORY** | Diagnostic or investigative — not claimed as proven | Emergent gravity end-to-end probes |

---

## Domain 1 — Newton Gravity / Photon Transport / Deflection

### Source: `TRM.Tests/RealityTests/TRM_Realtiy_Tests.cs`

| Test | What It Tests | Classification | Honest Assessment |
|:---|:---|:---|:---|
| TRM_Should_Reproduce_Newton_Gravity_PhaseModel | Phase model produces ~1/r² acceleration | **EFFECTIVE** | Reproduces Newton numerically via phase gradient; depends on φ=GM/(c²r) input |
| TRM_Should_Reproduce_Gravitational_Redshift_Phase | Phase shift matches GR redshift | **EFFECTIVE** | Correct sign and scaling; calibrated against GR expectation |
| TRM_Should_Match_Redshift_Between_Two_Heights_Phase | Differential redshift between altitudes | **EFFECTIVE** | Two-point phase difference matches GR |
| TRM_Should_Reproduce_Gravitational_Redshift_Phase_DIFFERENTIAL | Small-difference redshift limit | **EFFECTIVE** | Differential limit consistent |
| TRM_Should_Reproduce_Light_Deflection_Phase | Photon deflection from phase gradient | **EFFECTIVE** | Deflection angle correct at calibrated γ=0.85 |
| TRM19–TRM31 (Mercury precession) | Perihelion advance from phase dynamics | **EFFECTIVE** | Multiple methods (RK4, Verlet, etc.) reproduce 43"/century — but φ=GM/(c²r) is input, not output |
| TRM32–TRM60 (Photon convergence, index models) | Transport model convergence and anisotropic index | **EXPLORATORY** | Tests numerical behavior of transport model; not claimed as derived |
| TRM67–TRM77 (Shapiro delay, memory, unified) | Shapiro delay, memory channel, geometric vs local | **EXPLORATORY / EFFECTIVE** | Delay reproduced; memory channel probed; unified model tested against GR |

### Source: `TRM.Tests/RealityTests/PhotonTransportModel_GeodesicSolverTests.cs`

| Test | What It Tests | Classification | Honest Assessment |
|:---|:---|:---|:---|
| EL01–EL04 (Euler-Lagrange geodesic) | EL/Fermat bridge preserves photon speed, bounded deflection | **EFFECTIVE** | Euler-Lagrange solver is tested and bounded; depends on γ=0.85 |
| EL05–EL08 (BridgeScale calibration) | γ=0.85 emerges from collective γ peak; independent of ε | **CIRCULAR (partial)** | γ=0.85 is hardcoded in scoring prior; tests "discover" what they were built to find |
| EL09–EL17 (Cadence prior ablation) | Robustness of γ=0.85 across prior weights, kappa, cell count | **EXPLORATORY** | Tests scoring-function behavior, not physical mechanism |
| MEM01–MEM02 (Memory channel) | Memory improves bridge in weak field | **EFFECTIVE** | Memory channel correction validated |

### Source: `TRM.Tests/RealityTests/PhotonTransportModel_FixationTests.cs`

| Test | What It Tests | Classification | Honest Assessment |
|:---|:---|:---|:---|
| TRM78–TRM87 (Effective index, memory) | n_eff positivity, photon speed, memory scaling | **EFFECTIVE** | Transport model internals are self-consistent |
| MC01–MC08 (Memory invariant testing) | φ²|μ̇| as the leading admissible memory invariant | **DERIVED (within model)** | Power counting selects φ² scaling within the transport model; good internal logic |
| MC09–MC12 (Coherence amplitude → memory) | Lattice coherence amplitude scales with φ; derived invariant matches transport | **DERIVED (within model)** | Internal consistency proven within the transport model framework |
| MC13–MC16 (Clock-bias linearity, Green's function) | φ response linear; coherence follows Green's function | **EFFECTIVE** | Validates the clock-bias mechanism; uses calibrated parameters |
| CLAIM01 (No frame-dragging in scalar) | Documents scalar TRM does not claim frame-dragging | **CLAIM BOUNDARY** | Not a test — documents claim limits |

---

## Domain 2 — SPARC / RAR / Galaxy Rotation

### Source: `TRM.Tests/CoreTests/RarRelationTests.cs`

| Test | What It Tests | Classification | Honest Assessment |
|:---|:---|:---|:---|
| RAR01–RAR10 (Basic RAR, mass models, statistics) | SPARC data loading, mass model variants, asymptotic limits | **CALIBRATED** | Baryonic models calibrated against SPARC data |
| RAR11–RAR12 (a₀ fit, TRM vs MOND) | Global non-linear fit for acceleration scale a₀; comparison with MOND | **CALIBRATED** | a₀ ≈ 1.0–1.2×10⁻¹⁰ m/s² from 2839 galaxies; competitive with MOND |
| RAR13–RAR27 (Turning memory, service layer) | Memory correction, gate diagnostics, cross-split transfer | **CALIBRATED** | Sophisticated calibration pipeline with holdout validation; no-refit constraints |

### Source: `TRM.Tests/CoreTests/BtfRelationTests.cs`

| Test | What It Tests | Classification | Honest Assessment |
|:---|:---|:---|:---|
| BTF tests | Baryonic Tully-Fisher relation | **CALIBRATED** | BTFR reproduced from baryonic data; calibration-dependent |

---

## Domain 3 — Emergent Gravity (End-to-End)

### Source: `TRM.Tests/QuantumTests/EmergentGravityEndToEndTests.cs`

| Test | What It Tests | Classification | Honest Assessment |
|:---|:---|:---|:---|
| E2E01–E2E02 | Gravity proxy emerges from oscillator dynamics; energy density strengthens gradient | **EXPLORATORY** | Probes emergent behavior; uses fitted normalization K; not derived from core |
| E2E03–E2E07 | Normalization alignment; frozen K generalization; holdout testing | **CALIBRATED** | Normalization K is fitted, then frozen for holdout predictions |
| E2E08–E2E15 (Tick fluctuation, conserved tick matrix, scale breaking) | Nonlinear error probing, dynamic vs static K, scale dependence | **EXPLORATORY** | Diagnostic probes of normalization behavior; not claimed as derived |

---

## Domain 4 — Frame Dragging / Vector Sector

### Source: `TRM.Tests/CoreTests/FrameDraggingVectorExtensionTests.cs`

| Test | What It Tests | Classification | Honest Assessment |
|:---|:---|:---|:---|
| FD01–FD20 | Weak-field frame-dragging candidate; k_T workflow | **EFFECTIVE / EXPLORATORY** | Non-fitted effective k_T; candidate-level behavior; not derived from core |

---

## Domain 5 — Core Oscillator (V3.4 Frozen)

### Source: `TRM.Tests/QuantumTests/CollectiveModeLockingTests.cs`

| Test | What It Tests | Classification | Honest Assessment |
|:---|:---|:---|:---|
| CML01–CML23 | Synchronization, clock-bias, mode-locking, time-dependent lapse | **DERIVED (within model)** | Core oscillator behavior; validates I1/I2, clock-bias linearity, E1 rational ladder |
| BD1–BD6 (BridgeBand_DynamicalOrigin) | Bridge band has zero dynamical origin | **DERIVED** | Proven: bridge band is CLASS D (purely imposed) |
| E1a, E1b (Rational ladder) | Rational ladder is induced consistency, not dynamical quantization | **DERIVED** | Proven: Ω* constant across N; m_eff = round(N·(Ω*−1)) |

---

## Master Summary Table

| Domain | Test Count | DERIVED | CALIBRATED | EFFECTIVE | EXPLORATORY |
|:---|:---|:---|:---|:---|:---|
| Newton / Redshift / Deflection | ~77 | 0 | 0 | ~30 | ~47 |
| Photon Transport (EL/MEM) | ~17 | 0 | 2 (circular) | 4 | 11 |
| Memory Channel (MC) | 16 | 4 (within model) | 0 | 6 | 6 |
| SPARC / RAR / BTFR | ~56 | 0 | 56 | 0 | 0 |
| Emergent Gravity (E2E) | 15 | 0 | ~7 | 0 | ~8 |
| Frame Dragging (FD) | 20 | 0 | 0 | ~10 | ~10 |
| Core Oscillator (CML, BD, E1) | ~35 | ~30 | 0 | ~5 | 0 |
| **TOTAL** | **~236** | **~34** | **~65** | **~55** | **~82** |

---

## What Remains Valid From the Old Gravity Program

### STILL VALID (tested, calibrated, effective)

1. **Photon deflection reproduces GR** at γ=0.85 — but γ is a calibrated input, not a prediction
2. **Gravitational redshift** matches GR from the phase-field model — with φ=GM/(c²r) as input
3. **SPARC galaxy rotation curves** are well-fit by TRM baryonic models — competitive with MOND; a₀ calibrated from data
4. **BTFR** emerges from baryonic data — calibration-dependent
5. **Mercury precession** (~43"/century) reproduced by multiple numerical methods — with φ as input
6. **Memory channel** φ²|μ̇| is the leading admissible invariant — internally derived within the transport model
7. **Shapiro delay** reproduced — calibrated parameters

### NOT VALID (not derived, not claimed)

1. **Newtonian gravity is NOT derived from oscillator equations** — it's reproduced with φ=GM/(c²r) as a phenomenological input. The governing equation for the coupling field (∇²(δK) ∝ −M) is not derived.
2. **γ=0.85 is NOT a physical constant** — it's the algebraic reciprocal of 20/17, chosen as a convenient rational in the bridge band. It enters via hardcoded scoring priors.
3. **Dark matter is NOT explained** — baryonic models fit rotation curves via calibration, not via a first-principles mechanism that replaces DM.
4. **The bridge band is NOT emergent** — it's CLASS D (purely imposed). BD1-BD6 prove this conclusively.

### THE KEY DISTINCTION

```
OLD:  "TRM reproduces Newton, redshift, deflection, galaxy rotation"
      → True, but via calibrated parameters (γ, a₀, φ), not derivation.

NEW:  "TRM core (I1+I2+D1) constrains collective frequency structure"
      → True, rigorously proven (FP01-FP31, BD1-BD6, E1).

GAP:  From core oscillator → gravitational acceleration
      → NOT derived. k = G·K₀/c² is post-hoc. K₀ uncalibrated.
```

---

## Final Honest Paragraph

The old gravity program demonstrated that the TRM **phenomenological framework** — with calibrated parameters (γ=0.85, a₀≈10⁻¹⁰ m/s², φ=GM/(c²r)) — reproduces a wide range of gravitational and cosmological observations at a level competitive with ΛCDM+MOND. These results are not "wrong" — they are **phenomenological successes**. What they are NOT is **derivations from the frozen V3.4 oscillator core**. The core constrains collective frequency structure (I1, I2, D1, FP01-FP31) but does not contain a governing equation for the coupling field that produces gravity. The V4 C5 program (K1×F1, energy density interpretation) provides a phenomenological bridge between the core and gravity, but k = G·K₀/c² remains a post-hoc calibration. The honest status is: **phenomenology is strong; derivation is not yet closed.**
