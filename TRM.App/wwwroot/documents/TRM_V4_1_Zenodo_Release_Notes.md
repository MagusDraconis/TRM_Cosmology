# TRM V4.1 — Zenodo Release Notes

**Version:** V4.1 (pre-release / exploratory)  
**Date:** 2026-07-13  
**Status:** EXPLORATORY — not a finished physical theory  
**Test baseline:** 693/693 TEST-RUN-VERIFIED + 94 NEW-SUITE-VERIFIED (source-counted 787)  
**Branch:** feature/v4.1-convergence-state  
**Milestone:** v4.1-convergence-state-milestone

---

## What is TRM V4.1?

TRM (Temporal Resonance Mechanics) is a theoretical framework where gravity
and spacetime structure emerge from phase-synchronized oscillator dynamics.

**V4** (frozen) provides:
- Time from collective oscillator frequency Ω\* (anchored to SI second via I3)
- GR-compatible gravitational dynamics at 1PN (β_PPN ≈ 1.000 at b ≈ 1.248)
- Equivalence principle (WEP, SEP) from T(x) universality
- Strong-field GR-like solutions (EFT)
- Conditional all-orders quantum finiteness theorem

**V4.1** (exploratory) extends V4 toward spacetime emergence:
- Spatial topology as a self-consistent fixed point of temporal resonance dynamics
- Blind geometric reconstruction from oscillator time series alone
- Parameter-sweep hardening against numerical artifacts
- Quantum-mechanics-like benchmark behaviors
- Internal TRM scale candidates for future Planck comparison

> **IMPORTANT:** V4.1 is exploratory. No claim is made that space, quantum
> mechanics, or Planck scales are derived. See claim discipline policy.

---

## Test Evidence

| Metric | Value |
|:---|---:|
| Test command | `dotnet test --filter "FullyQualifiedName~V4_1" -v normal` |
| Total tests | 730 |
| Passed | 730 |
| Failed | 0 |
| Skipped | 0 |
| Test files | 77 |
| Test framework | xUnit 2.9.3, .NET 10.0 |

### Suite Breakdown

| Suite | Tests | Status |
|:---|:---|:---|
| Pre-existing audit/analysis suites | 248 | PASS |
| Emergent metric | 73 | PASS |
| Dimensional emergence | 15 | PASS |
| Blind emergent geometry | 18 | PASS |
| Self-consistent topology | 12 | PASS |
| Topology fixed-point robustness | 12 | PASS |
| Quantum benchmarks | 12 | PASS |
| Planck-scale benchmarks | 8 | PASS |
| Exponential fixed point | 16 | PASS |
| Exp fixed-point robustness | 7 | PASS |
| Fixed-point basin mapping | 10 | PASS |
| Continuum scaling | 10 | PASS |
| Cross-law continuum scaling | 10 | PASS |
| Exponential large-N scaling | 11 | PASS |
| Causal structure probe | 11 | PASS |
| Causal propagation speed | 11 | PASS |
| Lorentz signature probe | 11 | PASS |
| Causal front robustness | 12 | PASS |
| Causal front param opt | 11 | PASS |
| Fine structure scan | 10 | PASS |
| Fractal band structure | 12 | PASS |
| Energy-load trampoline | 13 | PASS |
| Data discovery | 5 | PASS |
| SPARC readiness | 5 | PASS |
| Energy-load response kernel | 13 | PASS |
| SPARC residual structure | 11 | PASS |
| Energy-time-geometry | 15 | PASS |
| ETG transfer functions | 15 | PASS |
| ETG calibration | 14 | PASS |
| ETG dimension selection | 14 | PASS |
| Dimension attractor | 12 | PASS |
| Dim estimator calibration | 13 | PASS |
| Dim selection mechanism | 11 | PASS |
| Dim attractor value | 13 | PASS |
| Dim continuum limit | 11 | PASS |
| Causal-ETG-Dim convergence | 13 | PASS |

---

## Claim Discipline

**SUPPORTED** (14 claims): Numerical pipeline behavior, metric validity, blind
reconstruction, loop stability, null-model detection, quantum-like benchmarks,
internal scale candidates, no D=3 bias.

**CONDITIONAL** (6 claims): Convergence depends on parameters, D_eff stabilizes
with Jaccard, method ranking, ħ_eff dimensionless, scales depend on discretization.

**HYPOTHESIS** (7 claims): Physical emergent space, D=3 selection, continuum limit,
Lorentzian spacetime, gravitational dynamics, physical QM emergence, Planck correspondence.

**NOT CLAIMED** (9 denials): GR replacement, D=3 derived, G/ħ/c derived,
Planck length/time derived, QM fully derived, QG solved.

---

## Key Documents

| Document | Purpose |
|:---|:---|
| `TRM_V4_1_SelfConsistent_Emergent_Space_Formalism.md` | Full formalism, pipeline, claim discipline table |
| `TRM_V4_1_Current_Status_And_Test_Evidence.md` | Test counts, suite table, release integrity check |
| `TRM_V4_1_Emergent_Space.md` | Original problem statement, graph-distance approach |
| `TRM_V4_1_Sync_Stability_Dimension_Selection.md` | F(D) functional, dimensional selection hypothesis |
| `TRM_V4_GR_Replacement_Roadmap.md` | V4 claim policy, G1–G6 status (inherited by V4.1) |

---

## Scope Boundary

V4.1 does **NOT**:
- Modify V3.4 core (oscillator dynamics, I1, I2, D1)
- Modify V4 interpretation results (C5, B1–B6, G1–G6)
- Claim D=3 is derived
- Claim GR is replaced
- Claim quantum mechanics is derived
- Claim Planck length/time are derived

V4.1 **DOES**:
- Provide numerical evidence for self-consistent topology emergence
- Test blind geometric reconstruction from oscillator data
- Harden the pipeline against numerical artifacts
- Demonstrate quantum-like benchmark behaviors
- Define internal TRM scale candidates
- Maintain strict claim discipline throughout

---

## Build Instructions

```bash
# Restore and build
dotnet restore TRM_Cosmology.slnx
dotnet build TRM_Cosmology.slnx

# Run all V4.1 tests
dotnet test TRM.Tests/TRM.Tests.csproj --filter "FullyQualifiedName~V4_1" -v normal

# Run specific suite
dotnet test TRM.Tests/TRM.Tests.csproj --filter "FullyQualifiedName~V4_1_BlindEmergentGeometry" -v normal
```

---

## References

- Kuramoto, Y. (1975). *Self-entrainment of a population of coupled non-linear oscillators.*
- Watts, D.J. & Strogatz, S.H. (1998). *Collective dynamics of 'small-world' networks.*
- Event Horizon Telescope Collaboration (2019, 2022). M87\* and Sgr A\* shadow observations.
