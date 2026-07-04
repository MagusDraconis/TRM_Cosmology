# BB07 — Clock-Bias Integration Plan

## Scope

BB06 concluded: γ ≈ 0.85 is a hardcoded default. The ONLY structural candidate for generating the frequency offset is the documented clock-bias equation:

```
dθ/dt = ω₀ + α·φ(x) + ΣK·sin(Δθ)
```

This document audits whether the existing repository contains enough infrastructure to integrate this mechanism into the BB03C emergent-Ω pipeline — without introducing new physics.

**Rules:** No derivations. No new assumptions. No code generation. Only evidence mapping and integration planning.

---

## # Existing Clock-Bias Infrastructure

### 1. Theory Documentation (Triple-Validated)

| Field | Value |
|:---|:---|
| **File** | `TRM_Geodesic_Derivation.md:514-524` |
| **Equation** | `dθ_a/dt = ω_0 + α·φ(x_a) + Σ K·sin(θ_b − θ_a)` |
| **Context** | Working derivation track §15.1: "Introduce a local phase state θ_a on a TQM lattice site a, with nearest-neighbor coupling and a baryonic source-dependent clock bias." |
| **Classification** | **READY — documented** |

| Field | Value |
|:---|:---|
| **File** | `TRM_Memory_Channel_Microscopic_Derivation.md:40-45` |
| **Equation** | `dθ_a/dt = ω_0 + α·φ(x_a) + Σ K_ab·sin(θ_b − θ_a) + ξ_a` |
| **Context** | Weak-field local clock-bias form with noise residual. §1 of the microscopic derivation chain. |
| **Classification** | **READY — documented** |

| Field | Value |
|:---|:---|
| **File** | `TRM_V3_1_Action_Based_Memory_Closure.md:54-58` |
| **Equation** | `dθ_a/dt = ω_a + α·φ(x_a) + Σ K_ab·sin(θ_b − θ_a)` |
| **Context** | TQM lattice-response interpretation §4. Notes that the local coherence amplitude A(x) tracks φ linearly in weak field. |
| **Classification** | **READY — documented** |

### 2. Working Code Implementation (FixationTests)

| Field | Value |
|:---|:---|
| **File** | `PhotonTransportModel_FixationTests.cs:1430-1488` |
| **Function** | `SimulateCoherenceAmplitudeFromLatticeProxy(double phi)` |
| **Role** | Full lattice simulation with clock-bias term: `dTheta[i] = omega[i] + alpha * phi + 0.5 * kEff * coupling` |
| **Alpha** | Hardcoded: `const double alpha = 1.0;` |
| **Omega centering** | `omega[i] = omegaSpread * Math.Sin(phase)` — **centered at 0**, spread ±0.08 |
| **Phi range tested** | 0, 1e-6, 2e-6, 5e-6, 1e-5, 2e-5, 3e-5, 1e-4, 2e-4, 5e-4, 1e-3, 2e-3 |
| **Returns** | Mean order parameter R = |⟨e^(iθ)⟩| (coherence amplitude) |
| **Classification** | **READY — working code, DIFFERENT scaling convention** |

### 3. Principle Validation Tests

| Test | File | What It Validates | Status |
|:---|:---|:---|:---|
| **MC13** | FixationTests:839 | Lattice clock-bias produces linear A_dyn(φ) response. R² > 0.95, positive slope. | **PASS** |
| **MC14** | FixationTests:890 | A_dyn tracks physical φ(r) = GM/(c²r) with Green-function proportionality. R² > 0.95, ratio gap < 0.25. | **PASS** |
| **MC15** | FixationTests:938 | A_phi scaling breaks under non-Newtonian response kernels. | **PASS** |

### 4. PhotonTransportModel.Phi() — Physical φ Definition

| Field | Value |
|:---|:---|
| **File** | `PhotonTransportModel.cs:111-114` |
| **Definition** | `public static double Phi(double G, double M, double c, double r) => G * M / (c * c * r);` |
| **Units** | Dimensionless (G=c=1 in framework). For M=0.01, r=8: φ ≈ 1.25 × 10⁻³. |
| **Availability** | `TRM.Core` assembly — accessible from any test project including CML tests |
| **Classification** | **READY — accessible from CML pipeline** |

---

## # Active CML Pipeline Compatibility

### 5. SimulateModeLock — Current Phase Update

| Field | Value |
|:---|:---|
| **File** | `CollectiveModeLockingTests.cs:7930-8070` |
| **Phase update line** | `phases[i] += config.Dt * (omegas[i] + config.CouplingKappa * couplings[i] + config.CollectiveWeight * align);` |
| **Omega centering** | `omegas[i] = 1.0 + 0.05·sin(angle) + 0.03·cos(2·angle)` — **centered at 1.0**, spread ±~0.06 |
| **External drive** | `config.CollectiveWeight * align` where `align = sin(Ωt − φ_i)` — external forcing |
| **Self-organizing mode** | `CollectiveWeight = 0` — removes external drive, BB03D verified Ω* ≈ 1.0 |

### 6. BB03D Infrastructure (Emergent Ω)

| Field | Value |
|:---|:---|
| **Phase snapshots** | `phasesAtSettle`, `phasesAtFinal` — captured at step==SettleSteps and step==Steps-1 |
| **ExtractEmergentOmega** | `Ω* = (1/N)·Σ(φ_final − φ_settle)/Δt` |
| **ModeLockResult** | Extended with `double? EmergentOmega`, `double EmergentOmegaStability` |
| **CML09 test** | Validates Ω* extraction with CollectiveWeight=0; Ω* ≈ 1.0, MeanOrder ≥ 0.85 |

### 7. Mechanical Insertion Feasibility

The current SimulateModeLock phase update:

```csharp
phases[i] += config.Dt * (omegas[i] + config.CouplingKappa * couplings[i] + config.CollectiveWeight * align);
```

Could become:

```csharp
phases[i] += config.Dt * (omegas[i] + alpha * phi + config.CouplingKappa * couplings[i] + config.CollectiveWeight * align);
```

**Mechanical difficulty: TRIVIAL.** One term added. No structural change to the dynamics.

**What needs to be added to ModeLockConfig:**
- `double Alpha` — clock-bias coupling coefficient
- `double Phi` — gravitational potential at the lattice site (or computed from M, r)

**Classification: READY — 1-line insertion, requires parameter additions**

---

## # Scaling Compatibility

### 8. Omega Centering Mismatch

| Aspect | FixationTests | SimulateModeLock | Compatible? |
|:---|:---|:---|:---|
| ω_i center | **0.0** (sinusoidal ±0.08) | **1.0** (sinusoidal ±0.06) | **DISCONNECTED** |
| α·φ role | DOMINANT frequency term | SHIFT from baseline 1.0 | Semantically compatible |
| Collective frequency | ~α·φ (since ω_i≈0) | ~1.0 + α·φ (since ω_i≈1.0) | Offset is the same physics |

**Note:** The FixtureTests convention (ω_i centered at 0) is a proxy for coherence amplitude extraction. Adding 1.0 to all ω_i would produce Ω* ≈ 1.0 + α·φ instead of Ω* ≈ α·φ. The offset ΔΩ = α·φ is identical in both conventions.

### 9. Alpha Calibration Gap

| Aspect | FixationTests | SimulateModeLock | Gap |
|:---|:---|:---|:---|
| Alpha value | 1.0 (hardcoded) | **UNDEFINED** | Alpha is not calibrated for any value in the CML context |
| Alpha physical meaning | Coupling between φ and frequency | Same physics, different regime | The theory is identical; the numerical value is transferable |

**Alpha = 1.0 CAN be used as a starting default**, since it is the only value defined anywhere in the repository and has been validated (MC13, MC14). But alpha=1.0 means φ-values must be on the order of 0.17 to produce ΔΩ ≈ 0.17, which is physically demanding (see §10).

### 10. Phi Value Analysis — The Core Gap

| Aspect | FixationTests | Desired Bridge Band | Gap |
|:---|:---|:---|:---|
| φ range tested | 0 .. 2e-3 | Needs φ ≈ 0.17 | **4 orders of magnitude too small** |
| M used (MC14) | 0.01 | Must be selected | No mass scale in CML |
| r used (MC14) | 8 .. 30 | Must be selected | No distance scale in CML |
| φ formula | GM/(c²r) | GM/(c²r) | Same formula |
| To get φ=0.17 (M=0.01) | r ≈ 0.059 | — | r = 3× Schwarzschild radius (r_s = 0.02) |
| To get φ=0.17 (M=1e6) | r ≈ 5.9×10⁸ | — | Plausible galactic scale but M arbitrary |

**The CML framework has no mass parameter.** It operates in pure dimensionless numbers. The ω_i values are hardcoded as 1.0 ± 0.06 with no connection to any physical source. To use φ = GM/(c²r), we must assign physical meaning to M and r in the CML context — which currently doesn't exist.

### 11. What φ Value Would Be Appropriate?

The theory documents suggest φ(x_a) is the gravitational potential "at the lattice site" — implying the CML lattice is embedded in a physical space with a mass source. The FixationTests use M=0.01 with r ∈ [8, 30] (φ ∈ [3.3e-4, 1.25e-3]), which tests the WEAK-FIELD regime.

For the CML, a representative φ must be chosen. Options:

| Option | M | r | φ = GM/(c²r) | Physical interpretation |
|:---|:---|:---|:---|:---|
| A | 0.01 | 10 | 1e-3 | Weak field (FixationTests range) |
| B | 0.01 | 0.059 | 0.17 | 3× Schwarzschild — NOT weak field |
| C | 1.0 | 5.9 | 0.17 | Plausible with different mass |
| D | Arbitrary | Any | 0.17 | Free parameter — defeats purpose |

**Classification: PARTIAL — φ is mechanically available but its value in the CML context is undefined. The CML has no spatial structure (lattice sites are indexed, not positioned), no mass parameter, and no physics-based means of selecting φ.**

---

## # Dependency Map

```
PhotonTransportModel.Phi(G, M, c, r)           [DEFINED — accessible static method]
   │
   └── φ = GM/(c²r)                            [DEFINED — standard Newtonian]
           │
           ├── M (source mass)                  [OPEN — no mass scale in CML]
           ├── r (lattice distance)             [OPEN — no spatial positions in CML]
           │
           └── α·φ                              [ASSUMED — α needs CML context value]
                   │
                   ├── α = 1.0                  [EMPIRICAL — from FixationTests only]
                   │
                   └── frequency shift Δω       [OPEN — to be measured]
                           │
                           └── Ω* = 1.0 + α·φ   [OPEN — to be extracted via BB03D]
                                   │
                                   └── γ = 1/Ω* [DEFINED — reciprocal mapping]
                                           │
                                           └── bridge band [TARGET — not yet reachable]
```

---

## # Minimal Integration Path

### Step 1: Add Clock-Bias Parameters to ModeLockConfig

Extend `ModeLockConfig` record:
- `double Alpha = 1.0` — clock-bias coupling (default from FixationTests)
- `double ClockBiasPhi = 0.0` — gravitational potential at the lattice (default 0 = no bias, backward compat)

**Impact:** Backward compatible (default 0.0 preserves existing behavior). 81+ existing call sites unaffected.

### Step 2: Insert Clock-Bias into SimulateModeLock Phase Update

One line changed:
```csharp
phases[i] += config.Dt * (omegas[i] + config.Alpha * config.ClockBiasPhi + ...);
```
Before the main loop, precompute `clockBiasShift = config.Alpha * config.ClockBiasPhi`.

**Impact:** Negligible performance impact (constant added).

### Step 3: Define Test Configs with φ Values

Three test configs spanning physically distinct φ regimes:

| Config | Alpha | Phi | φ source | Expected Ω* |
|:---|:---|:---|:---|:---|
| Baseline | 1.0 | 0.0 | — | ≈ 1.0 (CML09 baseline) |
| WeakA | 1.0 | 1e-3 | M=0.01, r=10 | ≈ 1.001 (barely detectable) |
| WeakB | 1.0 | 1e-2 | M=0.01, r=1 | ≈ 1.01 (10× baseline) |
| Exploratory | 1.0 | 0.17 | M=1.0, r=5.9 | ≈ 1.17 (bridge band test) |

### Step 4: Write Minimal Test

**Name:** `CML10_ClockBias_Should_Shift_EmergentOmega`

- Self-organizing mode: `CollectiveWeight = 0.0`
- Three φ values: 0.0 (baseline), 1e-3, 1e-2
- Assert: Ω*_φ − Ω*_0 ≈ α·φ (within tolerance)
- Assert: Ω* shifts monotonically with φ
- Assert: MeanOrder ≥ 0.85 (synchronization maintained)

**Not required:** Bridge-band matching. The test validates the SHIFT PRINCIPLE, not the band location.

### Step 5: Exploratory Test (Optional, Not Required for Integration Validation)

**Name:** `CML11_ClockBias_Exploratory_BridgeBandProximity`

- φ = 0.17 (exploratory only — to see if Ω* approaches 1.17)
- Expected behavior: Ω* ≈ 1.17
- Allowed failure: φ=0.17 may be unphysical for CML context; Ω* may not stabilize; MeanOrder may degrade
- Classification: **EXPLORATORY** — success not required

### Code Touchpoints Summary

| Touchpoint | File | Change |
|:---|:---|:---|
| ModeLockConfig | CollectiveModeLockingTests.cs:9089 | Add `double Alpha = 1.0`, `double ClockBiasPhi = 0.0` |
| SimulateModeLock | CollectiveModeLockingTests.cs:8040 | Add `+ config.Alpha * config.ClockBiasPhi` to phase update |
| New test CML10 | CollectiveModeLockingTests.cs | ~30 lines, validates φ→Ω* shift |
| New test CML11 (optional) | CollectiveModeLockingTests.cs | Exploratory bridge-band proximity check |

---

## # Failure Modes

1. **φ not available in CML path.** φ IS mechanically available via `PhotonTransportModel.Phi(G, M, c, r)`. But the CML framework has no M or r. A φ value must be HARDCODED or computed from arbitrarily-assigned M and r. **Risk: MEDIUM** — φ can be set, but its physical meaning is undefined.

2. **FixationTests path not transferable.** The FixtureTests implementation (`SimulateCoherenceAmplitudeFromLatticeProxy`) and CML (`SimulateModeLock`) share the same Kuramoto structure but differ in ω_i centering (0 vs 1.0) and purpose (coherence amplitude extraction vs mode-lock scoring). The clock-bias TERM is transferable; the overall pipeline is not. **Risk: LOW** — only the single term needs to transfer.

3. **Alpha undefined.** Alpha = 1.0 is the only value in the repository (from FixtureTests). It has been validated in the weak-field regime (φ ∈ [1e-6, 2e-3]) through MC13 and MC14. For larger φ values (e.g., 0.17), alpha=1.0 may not produce stable locking. **Risk: MEDIUM** — alpha is defined but may not calibrate correctly for large φ.

4. **Incompatible omega scaling.** FixtureTests centers ω_i at 0 (sinusoidal). CML centers ω_i at 1.0. The clock-bias shift α·φ is ADDITIVE in both cases. In FixtureTests: Ω* ≈ 0 + α·φ ≈ α·φ. In CML: Ω* ≈ 1.0 + α·φ. The OFFSET from baseline is identical. **Risk: LOW** — the scaling is mathematically identical modulo the centering constant.

5. **Bridge band still not reached.** With the weak-field φ values tested in FixtureTests (φ ≤ 2e-3), Ω* would shift from 1.0 to at most ~1.002 — far short of 1.17. To reach the bridge band, φ ≈ 0.17 is needed, which requires either very small r (~0.059 for M=0.01, near the Schwarzschild radius) or arbitrarily large M. The bridge band is not reachable without defining the CML spatial context. **Risk: HIGH** — the bridge band is unlikely to be produced by the weak-field clock-bias alone.

6. **φ = 0.17 may destroy synchronization.** At φ = 0.17, the frequency shift is 17% of the baseline. With CouplingKappa = 0.10 (default), the coupling strength-to-frequency-spread ratio drops, potentially preventing synchronization (MeanOrder < 0.85). **Risk: HIGH** — large frequency spread may prevent locking.

---

## # Final Question

### Is the repository ready for a minimal clock-bias integration test inside the BB03C emergent-Ω prototype path?

**Answer: PARTIALLY — with justification.**

**What is ready:**

1. **Theory:** The clock-bias equation is documented in 3 theory files with identical formulation.
2. **Principle validation:** MC13 and MC14 prove the mechanism works (linear A_dyn(φ), Green-function tracking).
3. **Code implementation:** A working implementation exists in `SimulateCoherenceAmplitudeFromLatticeProxy` (FixationTests:1430).
4. **φ definition:** `PhotonTransportModel.Phi()` provides standard Newtonian potential.
5. **α definition:** α = 1.0 is the only value in the repository, validated in the weak-field regime.
6. **BB03D infrastructure:** Phase snapshots, ExtractEmergentOmega, ModeLockResult, CML09 test — all operational.
7. **Mechanical insertion:** Adding α·φ to SimulateModeLock requires 1 line of code plus 2 ModeLockConfig fields. Backward compatible.
8. **Test design:** CML10 (φ → Ω* shift validation) is well-defined and independently testable.

**What is NOT ready:**

1. **No M or r in CML context.** The CML operates in dimensionless phase space without spatial positions or source mass. φ = GM/(c²r) requires both, which are currently undefined in the CML framework.
2. **Weak-field φ is too small.** The validated φ range (1e-6 to 2e-3) produces frequency shifts of at most 0.2% — far below the 17% needed for the bridge band.
3. **Large φ is untested.** φ = 0.17 is ~100× larger than the tested range. The Kuramoto model may not synchronize under such a large frequency spread with Kappa = 0.10.
4. **Bridge band is not reachable without defining the CML spatial context.** This is a PHYSICS gap, not a CODE gap.

**Verdict:**

The repository can support a **clock-bias SHIFT TEST** (CML10): does adding α·φ shift Ω* by the expected amount? This requires only mechanical insertion and validates the principle within the CML pipeline.

The repository is **NOT ready** for a clock-bias BRIDGE-BAND TEST: the bridge band requires φ ≈ 0.17, which requires defining M and r in the CML context — a physics decision that goes beyond the scope of a code integration audit. The CML framework simply doesn't know what "distance from a mass source" means yet.

**The integration is READY for a principle-level test (CML10). It is NOT READY for a bridge-band-level test without an additional physics choice: assigning physical meaning to spatial position in the dimensionless CML lattice.**
