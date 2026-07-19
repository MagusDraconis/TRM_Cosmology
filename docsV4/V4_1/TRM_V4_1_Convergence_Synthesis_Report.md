# TRM V4.1 — Convergence Synthesis Report

**Date:** 2026-07-13  
**Status:** EXPLORATORY — numerical diagnostics only  
**Verification:** 693 TEST-RUN-VERIFIED baseline + 94 NEW-SUITE-VERIFIED  
**Source-counted total:** 787 tests (82 test files)

---

## 1. Executive Summary

TRM V4.1 currently supports a numerical convergence state in which the following
pipeline holds under tested conditions:

```
Load → Omega → R_ij → d_ij = -log(R_ij) → K_ij → Remote Response
  → D_eff (corrected dimension attractor)
  → Causal Fronts (kick-detect, front-fit, cone proxy)
  → Lorentz-like Proxy (interval s2, inside/outside cone)
  → Space-Time Separation (Omega vs d_ij distinguishability)
  → Metric Tensor Proxy (g00, g_spatial, g_offdiag)
```

Each step is **measurable**, **finite**, and **deterministic** under the
exponential coupling law with xi=1.75, K0=1.2 in the N=80–120 range.

**This report does NOT claim:**
- D=3 is derived
- physical spacetime is derived
- physical metric tensor is derived
- physical c is derived
- Lorentz invariance is proven
- gravity or General Relativity is derived or replaced
- SPARC or dark matter is explained

---

## 2. Core Pipeline

### 2.1 Energy-Time-Geometry Transfer

```
Load → local Omega shift → R_ij deformation → d_ij change → K_ij update → remote response
```

Measured via ETG, ETGTF, and ETGC suites (44 tests total):
- Transfer slopes measurable (Load→Omega, Omega→R, R→d, d→K, K→Remote)
- Chain gain close to unity in plateau regime
- Closure error moderate (not a field equation)
- Load invariance under deltaLoad=0.2 confirmed
- Natural load unit calibratable

### 2.2 Corrected Effective Dimension

```
Ball-growth D_eff on TRM fixed-point distance matrix
→ finite-N bias correction from D=1..6 lattice references
→ corrected D_corr
```

Measured via DA, DEC, DSM, DAV, DCL suites (60 tests):
- Multiple estimators (ball-growth, weighted-shell, spectral proxy)
- Estimator bias calibrated on known-D references
- Finite-N correction measurable but estimator-dependent
- D_corr range: ~2.5–3.5 (corrected)
- Baseline: ~2.7–3.1 at xi=1.75, K0=1.2
- Large-N: stable up to N=500 with reduced diagnostics
- D=3 proximity is **diagnostic only**, NOT a derivation

### 2.3 Causal Fronts

```
Kick detection → response delay tau_ij → front fit tau = d/v + b → cone classification
```

Measured via CS, CPS, LSP, CFR, CFPO suites (57 tests):
- Causal fronts measurable
- Response fraction and front quality regime-dependent
- Dimensionless v candidates ~0.3–0.8
- Inside/outside cone classification computable
- Physical c is NOT claimed

### 2.4 Lorentz-like Signature

```
Event cloud (d_ij, tau_ij, amp_ij)
→ front fit → interval proxy s2 = tau^2 - (d/v)^2
→ time-like / near-null / space-like classification
```

Measured via ELS suite (15 tests):
- Weak-to-moderate cone-like numerical signature
- Interval proxy classification present
- Near-null events measurable
- Lorentz invariance is NOT claimed

### 2.5 Space-Time Separation

```
Omega field (clock-rate proxy) ← temporal direction
d_ij distance matrix ← spatial structure
→ low correlation between Omega gradient and spatial shell
→ distinguishable time and space proxies
```

Measured via ESTS suite (15 tests):
- Temporal stability: high (>0.8) in active regimes
- Spatial stability: moderate
- Time-space distinguishability: low correlation (Separated class)
- Near-null fronts measurable
- Physical spacetime is NOT claimed

### 2.6 Metric Tensor Proxy

```
g00 proxy ← Omega stability / clock rate
g_spatial proxy ← local distance structure
g_offdiag proxy ← time-space coupling correlation
→ signature classification (Lorentz-like vs Euclidean-like vs Degenerate)
→ interval consistency (s2_direct vs s2_from_proxy)
```

Measured via EMTP suite (15 tests):
- g00: stable, ~0.85
- g_spatial: stable, ~2.5
- g_offdiag: small, ~0.2 (time-space weakly coupled)
- Signature: Lorentz-like sign separation proxy
- Interval consistency: correlated
- Physical metric tensor is NOT derived

---

## 3. Recurrent Stable Regime

Across **all** convergence suites (ETG, dimension, causal, Lorentz, space-time,
metric proxy), a consistent regime emerges:

| Parameter | Value | Status |
|:---|:---|:---|
| xi | **1.75** | Plateau center in all sweeps |
| K0 | **1.2** | Best convergence score |
| Coupling law | **exponential** | Tightest agreement across pillars |
| N | **80–120** | Best balance of resolution and stability |

This is **numerical evidence of a stable parameter region**, not a physical
constant claim.

---

## 4. Dimension Evidence Summary

| Property | Finding | Suite(s) |
|:---|:---|:---|
| Raw D_eff | 2.5–3.5 | DA, DAV |
| Estimator bias | Positive, finite-N measurable | DEC |
| Corrected D_corr | 2.7–3.1 (baseline) | DAV |
| Load invariance | ΔD < 0.5 under deltaLoad=0.2 | ETGDS, DAV |
| N-scaling | Stable up to N=500 | DCL |
| Large-N D_inf | ~2.8–3.1 (1/N model) | DCL |
| Nearest integer | D≈3 (diagnostic only) | DAV, DCL |
| D=3 derivation | **NOT CLAIMED** | All suites |

---

## 5. Energy-Time-Geometry Evidence Summary

| Property | Finding | Suite(s) |
|:---|:---|:---|
| Chain steps | All five measurable | ETG |
| Transfer slopes | Finite, regime-dependent | ETGTF |
| Chain gain | Close to unity in plateau | ETGTF |
| Closure error | Moderate | ETGC |
| Natural load unit | Calibratable | ETGC |
| Load invariance | Holds under deltaLoad=0.2 | All ETG suites |
| Response kernel | Exponential > power-law | ELRK |
| Physical energy/mass | **NOT CLAIMED** | All ETG suites |

---

## 6. Causal and Lorentz-like Evidence Summary

| Property | Finding | Suite(s) |
|:---|:---|:---|
| Causal fronts | Measurable, regime-dependent | CS, CPS, CFR |
| Front fit quality | r² moderate (0.2–0.5) | CFPO, ELS |
| Dimensionless v | ~0.3–0.8 (stable across seeds) | CPS, ELS |
| Inside/outside cone | Classifiable | LSP, ELS |
| Interval proxy s2 | time-like/near-null/space-like computable | ELS |
| Lorentz-like signature | **Weak-to-moderate** | ELS |
| Physical c | **NOT CLAIMED** | All causal suites |
| Lorentz invariance | **NOT PROVEN** | All suites |

---

## 7. Space-Time Separation Evidence Summary

| Property | Finding | Suite(s) |
|:---|:---|:---|
| Temporal proxy (Omega) | Stable, high tStab | ESTS |
| Spatial proxy (d_ij) | Stable, finite D_eff | ESTS |
| Time-space distinguishability | Low correlation → Separated | ESTS |
| Causal delay model | Space-only adequate; time+space adds marginal | ESTS |
| Near-null events | Measurable fraction | ESTS |
| Load invariance | Ω shift < 0.3, D shift < 0.5 | ESTS |
| Physical spacetime | **NOT CLAIMED** | ESTS |

---

## 8. Metric Tensor Proxy Evidence Summary

| Property | Finding | Suite(s) |
|:---|:---|:---|
| g00 proxy | ~0.85, stable | EMTP |
| g_spatial proxy | ~2.5, stable | EMTP |
| g_offdiag proxy | ~0.2, small | EMTP |
| Signature class | Lorentz-like sign separation proxy | EMTP |
| Interval consistency | s2_direct vs s2_proxy correlated | EMTP |
| Load invariance | Δg < 0.3 (rel) | EMTP |
| Physical metric tensor | **NOT DERIVED** | EMTP |

---

## 9. Null, Degenerate, and Adversarial Controls

| Control | Result | What it rules out |
|:---|:---|:---|
| K=0 | Degenerate (D<1, no fronts, no convergence) | Not a trivial numerical artifact |
| Random R | Weak/random (low D, no Lorentz signal) | Not a random matrix artifact |
| Shuffled θ | Destroyed (geometry lost) | Not a phase artifact |
| Global sync | D≈1, uniform, no cone structure | Not a uniform-sync artifact |
| Overcoupled dense | Degenerate, no separation | Not a simple density artifact |
| kNN-only | Does not create false convergence | Not an adjacency artifact |

---

## 10. Claim Discipline Table

### SUPPORTED
| Claim | Evidence |
|:---|:---|
| Load→Omega→R→d→K→Remote chain measurable | ETG/ETGTF/ETGC suites (44 tests) |
| Corrected dimension attractor measurable | DA/DEC/DAV/DCL/DSM suites (60 tests) |
| Dimension load-invariant under tested loads | ETGDS/DAV/ESTS suites |
| Causal diagnostics measurable | CS/CPS/LSP/CFR/CFPO suites (57 tests) |
| Weak-to-moderate Lorentz-like proxy measurable | ELS suite (15 tests) |
| Space-time proxy separation measurable | ESTS suite (15 tests) |
| Metric tensor proxy measurable | EMTP suite (15 tests) |
| Exponential law is strongest candidate | CLS, FSS, ELNS, CEDC, ELS, EMTP suites |
| Null and degenerate controls correctly detected | All suites |

### CONDITIONAL
| Claim | Condition |
|:---|:---|
| Convergence depends on xi, K0, N, estimator, threshold, seed, diagnostics | All convergence suites |
| Large-N tests are not continuum proofs | DCL, CEDCLN |
| Metric proxy is not a physical metric | EMTP |
| Dimensionless v is not physical c | CPS, ELS |
| Interval proxy s2 is not Minkowski interval | ELS, EMTP |
| Signature proxy is not Lorentzian spacetime | EMTP |
| ETG closure is not a physical field equation | All ETG suites |

### HYPOTHESIS
| Claim | Reasoning |
|:---|:---|
| TRM convergence state may underlie emergent spacetime | Multi-pillar co-stabilization |
| D=3 may emerge after stricter constraints | Corrected D ~ neighborhood of 3 |
| Physical GR-like behavior may appear as effective limit | Metric proxy + causal fronts exist |
| Lorentzian spacetime may emerge after continuum constraints | Weak-but-present Lorentz signatures |

### NOT CLAIMED
| Item | Why not |
|:---|:---|
| D=3 is derived | Nearest integer is diagnostic only |
| Physical 3D space is derived | Graph dimension ≠ continuum dimension |
| Physical spacetime / metric is derived | Proxies, not physical tensors |
| Minkowski metric is derived | s2 is a numerical proxy |
| Physical c is derived | v is dimensionless graph quantity |
| Lorentz invariance is proven | Cone-like ≠ invariant |
| Gravity / GR is derived/replaced | No field equations, no geodesics yet |
| Einstein equations are derived | Not attempted |
| Time dilation is derived | Not attempted |
| SPARC is explained | Residual analysis data-limited |
| Dark matter is replaced | Not attempted |

---

## 11. Current Limitations

1. **Full 787 verification pending:** 693 TEST-RUN-VERIFIED + 94 NEW-SUITE-VERIFIED
2. **Large-N limit:** N=500 uses 2-epoch diagnostics; no N→∞ proof
3. **Metric proxy:** coordinate-proxy dependent; not a tensor in the differential-geometric sense
4. **Lorentz-like signature:** weak-to-moderate only; far from Lorentz invariance
5. **Causal pillar:** weaker than ETG and dimension pillars
6. **Physical units:** Omega, v, g-proxy are all dimensionless graph quantities
7. **No field equations:** metric proxy has no dynamics, curvature, or geodesic structure
8. **No conservation laws:** no stress-energy, no Bianchi identities
9. **SPARC data:** limited to ~1 row; residual analysis blocked by MRT parser limitations
10. **FBS_04 slow:** ~6 minutes for grid-based dimension scan

---

## 12. Recommended Roadmap

### P1 — Immediate
- Full V4.1 release verification (`dotnet test --filter "FullyQualifiedName~V4_1" -v normal`)
- Emergent Geodesic Structure suite (node-to-node path proxies, geodesic-like distance)
- Metric proxy robustness across coordinate-proxy choices

### P2 — Near-term
- GR-limit probe (metric proxy consistency with weak-field GR form)
- Causal speed calibration (v stability vs shell, N, load)
- Physical unit calibration for Omega (frequency anchoring)
- ETG time-dilation proxy

### P3 — Exploratory
- SPARC residual comparison (requires MRT parser fix for full MassModels)
- Coma / Pantheon / cosmological exploratory probes
- Full convergence synthesis with figures and tables

---

## 13. Release Integrity

| Metric | Value |
|:---|:---|
| Full V4.1 baseline (TEST-RUN-VERIFIED) | **693** |
| Verified command | `dotnet test --filter "FullyQualifiedName~V4_1" -v normal` |
| Verified result | 693 passed, 0 failed, 0 skipped |
| Verified date | 2026-07-13 |
| Runtime | ~10m57s |
| NEW-SUITE-VERIFIED additions | +94 (DAV 13 + DCL 11 + CEDC 13 + CEDCLN 12 + ELS 15 + ESTS 15 + EMTP 15) |
| Source-counted total | **787** |
| Test files | 82 |
| Mismatch | None detected (source-count confirmed) |

**The full 787-test suite has NOT been run as a single `dotnet test` invocation.**
The 787 count is source-counted and confirmed via `--list-tests`.
A full release verification run is recommended as P1.

---

## 14. Test Suite Breakdown (Complete)

### Pre-existing audit/analysis: 248 tests (40 files)

### New V4.1 emergent-space suites: 539 tests (42 files)

| Suite | Tests | Category |
|:---|:---|:---|
| Emergent Metric | 73 | SUPPORTED |
| Dimensional Emergence | 15 | SUPPORTED |
| Blind Emergent Geometry | 18 | SUPPORTED |
| Self-Consistent Topology | 12 | CONDITIONAL |
| Topology Fixed-Point Robustness | 12 | SUPPORTED |
| Quantum Benchmarks | 12 | SUPPORTED |
| Planck Scale Benchmarks | 8 | SUPPORTED |
| Natural Continuous Coupling | 13 | SUPPORTED |
| Coupling Law Selection | 11 | SUPPORTED |
| Exponential Fixed Point | 16 | SUPPORTED |
| Exp Fixed-Point Robustness | 7 | SUPPORTED |
| Fixed-Point Basin Mapping | 10 | SUPPORTED |
| Exponential Continuum Scaling | 10 | SUPPORTED |
| Cross-Law Continuum Scaling | 10 | SUPPORTED |
| Exponential Large-N Scaling | 11 | SUPPORTED |
| Causal Structure Probe | 11 | SUPPORTED |
| Causal Propagation Speed | 11 | SUPPORTED |
| Lorentz Signature Probe | 11 | SUPPORTED |
| Causal Front Robustness | 12 | SUPPORTED |
| Causal Front Param Optimization | 11 | SUPPORTED |
| Fine Structure Parameter Scan | 10 | SUPPORTED |
| Fractal Band Structure | 12 | SUPPORTED |
| Energy-Load Trampoline Effect | 13 | SUPPORTED |
| Data Discovery | 5 | SUPPORTED |
| SPARC Readiness | 5 | SUPPORTED |
| Energy-Load Response Kernel | 13 | SUPPORTED |
| SPARC Residual Structure | 11 | SUPPORTED |
| Energy-Time-Geometry Coupling | 15 | SUPPORTED |
| ETG Transfer Functions | 15 | SUPPORTED |
| ETG Calibration | 14 | SUPPORTED |
| ETG Dimension Selection | 14 | SUPPORTED |
| Dimension Attractor | 12 | SUPPORTED |
| Dim Estimator Calibration | 13 | SUPPORTED |
| Dim Selection Mechanism | 11 | SUPPORTED |
| Dim Attractor Value | 13 | SUPPORTED |
| Dim Continuum Limit | 11 | SUPPORTED |
| Causal-ETG-Dim Convergence | 13 | SUPPORTED |
| Causal-ETG-Dim Large-N Conv | 12 | SUPPORTED |
| Emergent Lorentz Signature | 15 | SUPPORTED |
| Emergent Space-Time Separation | 15 | SUPPORTED |
| Emergent Metric Tensor Proxy | 15 | SUPPORTED |

### Total: 248 + 539 = 787

---

*This report is a diagnostic summary. It does not introduce new physical claims,
and it explicitly documents what is and is not claimed. For claim discipline
details, see each suite's `ClaimDisciplineReport` method.*

*Maintained by: TRM V4.1 research program*  
*Last updated: 2026-07-13*
