# TRM V4.1 — Calibration Readiness and External Anchor Policy

**Date:** 2026-07-14  
**Status:** EXPLORATORY — internal readiness only, no physical calibration performed  
**Branch:** feature/v4.1-convergence-state

---

## 1. Executive Summary

TRM V4.1 convergence-state investigation has identified internally stable dimensionless
calibration candidates for:

| Quantity | Internal Proxy | Status |
|:---|:---|:---|
| Time | Omega / Omega_mean | A) Ready internally |
| Length | d_ij MeanDist | A) Ready internally |
| Propagation speed | c_eff = d/tau normalized | A) Ready internally |
| Source (mass/energy) | OmegaSource | A) Ready internally |
| Gravitational coupling | alpha_TRM → G_eff design | A) Ready internally |

**All physical units require external anchors. No physical constants are derived.**
This document defines the calibration chain, readiness scores, missing requirements,
and a safe external-anchor policy.

---

## 2. Calibration Chain

```
Omega anchor
  → time scale
  → tau

d_ij anchor
  → length scale

time + length
  → c_eff design = d_normalized / tau_normalized

source + curvature + time + length
  → alpha_TRM = curvature / source
  → G_eff-like design = alpha × (L²) / (T² · M_source)
```

### Dependency Graph

```
Omega/Time anchor ──────────────────────┐
                                        ├──→ c_eff ──→ physical speed candidate
d_ij/Length anchor ─────────────────────┘       │
                                                │ (requires external speed ref)
Source/OmegaSource anchor ──────────────┐       │
                                        ├──→ alpha_TRM ──→ G_eff candidate
Curvature/LapCurv proxy ────────────────┘       │
                                                │ (requires external L, T, M refs)
                                                │
                                    NOT physical G
```

---

## 3. Internal Anchor Readiness Table

| Anchor | Best Candidate | Status | Missing External Reference | Evidence Suite(s) |
|:---|:---|:---|:---|:---|
| Ω / Time | Ω/Ω_mean | A) Ready | External frequency/time reference | OTAF, ETAD |
| d / Length | MeanDist | A) Ready | External length reference | LAF |
| c_eff | Dimless × MeanDist | A) Ready | External speed reference | CEFF |
| Source | OmegaSource | A) Ready | External mass/energy reference | SAF |
| G_eff design | α × L²/(T²·M) | A) Ready | External L, T, M references | GEFF |

**All "A) Ready" ratings are internal only. No external calibration has been performed.**

---

## 4. What Is Internally Supported

1. Omega clock-rate proxy is stable across N=40–200, seeds 0–19, loads ≤ 0.2, and in the plateau xi=1.75, K0=1.2.
2. d_ij = -log(R_ij) distance scale is stable under same conditions.
3. c_eff = d/tau normalized by Ω/d baselines is stable and load-invariant.
4. OmegaSource provides the most stable internal source proxy for alpha_TRM.
5. alpha_TRM = CurvatureProxy / SourceProxy is measurable, stable, and load-linear for load ≤ 0.2.
6. All five anchors co-stabilize in the same regime (xi=1.75, K0=1.2, exponential law).
7. Null and degenerate controls (K=0, global sync, random R, shuffled theta) correctly produce weak/no calibration readiness.

---

## 5. What Is Conditional

1. All readiness ratings depend on xi=1.75, K0=1.2, exponential coupling law, tested N range (40–500 for sampled diagnostics), seed range (0–19), load range (≤ 0.2), and proxy definitions.
2. Full continuum proof (N→∞) is not available.
3. Full 1018-suite verification may be pending if only new suites were run (693 TEST-RUN-VERIFIED baseline + 325 NEW-SUITE-VERIFIED).
4. Calibration path depends on chosen external anchor policy.

---

## 6. What Remains Hypothesis

1. Omega may become a physical time anchor after external frequency calibration.
2. d_ij may become a physical length anchor after external length calibration.
3. c_eff may become comparable to physical c after time and length calibration.
4. alpha_TRM may become a G_eff-like coupling after time, length, and source calibration.
5. The TRM convergence state may underlie emergent spacetime.
6. D=3 may emerge only after continuum, causal, ETG, and calibration constraints.

---

## 7. What Is NOT Claimed

| Item | Status |
|:---|:---|
| Physical c is derived | NOT CLAIMED |
| Physical G is derived | NOT CLAIMED |
| Physical mass or energy is derived | NOT CLAIMED |
| SI seconds or meters are derived | NOT CLAIMED |
| Physical units are derived | NOT CLAIMED |
| D=3 is derived | NOT CLAIMED |
| Physical spacetime is derived | NOT CLAIMED |
| GR is derived or replaced | NOT CLAIMED |
| Einstein equations are derived | NOT CLAIMED |
| Lorentz invariance is proven | NOT CLAIMED |
| SPARC is explained | NOT CLAIMED |
| Dark matter is replaced | NOT CLAIMED |

---

## 8. External Anchor Options

### A. Time-First Anchor (Recommended)
- Map Omega baseline to external frequency/time reference.
- Safest first anchor: Omega has highest internal stability.
- Unlocks tau, c_eff, and G_eff dimensional mapping.
- **Risk: LOW.** External input, not derived.

### B. Length-First Anchor
- Map MeanDist to external length scale.
- Requires careful interpretation of graph distance vs physical space.
- **Risk: MEDIUM.** Graph distance ≠ continuum length without proof.

### C. Speed-First Anchor
- Set c_eff to external c.
- Constrains both time and length simultaneously.
- **Risk: HIGH.** Overconstrains; risks circular calibration.

### D. Source-First Anchor
- Map OmegaSource or load proxy to physical energy/mass.
- Highest interpretation sensitivity.
- **Risk: HIGH.** Source proxy is least directly anchored.

### E. Astrophysical Anchor
- SPARC/Coma/Pantheon style calibration.
- **Risk: VERY HIGH.** NOT recommended before internal physical calibration policy is fixed and MRT parser is adequate.

---

## 9. Recommended Anchor Order

| Stage | Action | Prerequisites |
|:---|:---|:---|
| 1 | Internal dimensionless baseline | Complete |
| 2 | External time anchor exploration | Omega readiness (A) |
| 3 | External length anchor exploration | Time anchor + d_ij readiness (A) |
| 4 | c_eff consistency check | Time + Length anchors |
| 5 | Source/mass-energy anchor exploration | Time + Length + c_eff |
| 6 | G_eff-like consistency check | All 4 anchors |
| 7 | Astrophysical comparisons | All anchors + continuum + GR-limit |

**This is a suggested roadmap, not a claim of physical derivation.**

---

## 10. Risk Register

| Risk | Severity | Mitigation |
|:---|:---|:---|
| Circular calibration (tune Omega to match c) | HIGH | Anchor one at a time; do not tune |
| Overfitting alpha to known G | HIGH | Do not match G until all anchors are externally set |
| Using SPARC too early | HIGH | Fix MRT parser; do not fit residuals before calibration |
| Confusing dimensionless v_eff with physical c | MEDIUM | Always mark v_eff as internal only |
| Confusing alpha_TRM with physical G | MEDIUM | Always mark alpha as dimensionless proxy |
| Proxy dependence | MEDIUM | Cross-validate across curvature/source definitions |
| Finite-N effects | MEDIUM | Use N=500 sampled where feasible |
| Incomplete continuum proof | MEDIUM | Do not claim N→∞ |
| Full suite verification gap | LOW | Run full 1018 verification when ready |

---

## 11. Release Integrity

| Metric | Value |
|:---|:---|
| Full V4.1 TEST-RUN-VERIFIED baseline | **693** |
| Verified command | `dotnet test --filter "FullyQualifiedName~V4_1" -v normal` |
| Verified result | 693 passed, 0 failed, 0 skipped |
| Verified date | 2026-07-13 |
| NEW-SUITE-VERIFIED additions | +325 (25 convergence-state + calibration suites) |
| Source-counted total | **1018** |
| Full 1018 TEST-RUN-VERIFIED | **Pending** |

**The 1018 suite total has NOT been run as a single `dotnet test` invocation.**
It is confirmed source-counted via `--list-tests`. Run full verification before
proceeding to external calibration.

---

## 12. Convergence-State Suite Inventory

| Suite | Tests | Layer | Status |
|:---|:---|:---|:---|
| Dimension Attractor Value | 13 | Dimension | NEW-SUITE-VERIFIED |
| Dimension Continuum Limit | 11 | Dimension | NEW-SUITE-VERIFIED |
| Causal-ETG-Dim Convergence | 13 | Three-pillar | NEW-SUITE-VERIFIED |
| Causal-ETG-Dim Large-N Conv | 12 | Three-pillar | NEW-SUITE-VERIFIED |
| Emergent Lorentz Signature | 15 | Structure | NEW-SUITE-VERIFIED |
| Emergent Space-Time Separation | 15 | Structure | NEW-SUITE-VERIFIED |
| Emergent Metric Tensor Proxy | 15 | Metric | NEW-SUITE-VERIFIED |
| Emergent Geodesic Structure | 14 | Geodesics | NEW-SUITE-VERIFIED |
| Geodesic Robustness | 14 | Geodesics | NEW-SUITE-VERIFIED |
| GR-Limit Probe | 15 | GR proxy | NEW-SUITE-VERIFIED |
| GR-Limit Robustness | 14 | GR proxy | NEW-SUITE-VERIFIED |
| Curvature Response Proxy | 15 | Curvature | NEW-SUITE-VERIFIED |
| Curvature Response Robustness | 15 | Curvature | NEW-SUITE-VERIFIED |
| Einstein Equation Proxy | 15 | Einstein | NEW-SUITE-VERIFIED |
| Source-Curvature Robustness | 16 | Einstein | NEW-SUITE-VERIFIED |
| Source-Curvature Cont. Scaling | 15 | Continuum | NEW-SUITE-VERIFIED |
| Physical Calibration Prereqs | 13 | Calibration | NEW-SUITE-VERIFIED |
| Omega Time Anchor Feasibility | 15 | Calibration | NEW-SUITE-VERIFIED |
| External Time Anchor Design | 14 | Calibration | NEW-SUITE-VERIFIED |
| Length Anchor Feasibility | 14 | Calibration | NEW-SUITE-VERIFIED |
| c_eff Calibration Design | 13 | Calibration | NEW-SUITE-VERIFIED |
| Source Anchor Feasibility | 15 | Calibration | NEW-SUITE-VERIFIED |
| G_eff Calibration Design | 14 | Calibration | NEW-SUITE-VERIFIED |
| **Total NEW-SUITE-VERIFIED** | **325** | | |

---

## 13. Recommended Next Research Step

| Priority | Task |
|:---|:---|
| **P1** | External Time Anchor Exploratory Calibration Design (ETACD) |
| **P1** | Full 1018-suite release verification |
| **P2** | Length anchor external mapping |
| **P2** | c_eff external consistency check |
| **P3** | G_eff external consistency check |
| **P4** | SPARC residual comparison (requires MRT parser fix) |

---

*This document is a diagnostic policy summary. It does not claim any physical
constant is derived. All external anchors are EXTERNAL INPUT ONLY.*

*Maintained by: TRM V4.1 research program*  
*Last updated: 2026-07-14*
