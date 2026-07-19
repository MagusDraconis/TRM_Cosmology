# TRM V4.4 — Prospective Length Anchor Validation Roadmap

**Status:** EXPLORATORY
**Date:** 2026-07-15

---

## A. Motivation

V4.3 completed a systematic geometric-scale interpretation of the TRM attractor. 12 candidates were surveyed, 8 advanced to classification, correlation classes were identified, a hierarchy DAG was constructed, geometric roles were assigned, and a PRIMARY candidate was selected via composite geometric criteria. No physically superior alternative to MeanDist was found.

V4.4 addresses the natural next question: **Can the recommended geometric scale be prospectively validated as a length anchor without circularity?**

---

## B. V4.3 Findings

| Finding | Detail |
|:---|:---|
| Candidate pool | 12 candidates, 8 non-C advanced |
| Geometric classes | Global-distance estimators form a tight cluster |
| Hierarchy DAG | Reproducible directed dependency structure |
| Primary selected | Top-ranked by composite SelectionScore |
| c_eff_SI robust | Invariant to multiplicative proxy choice |
| G_eff_SI sensitive | Cubic dependence on proxy CV |
| Anti-circularity | All 10 gates pass |
| Freeze protocol | 4-phase protocol defined |

---

## C. MeanDist Baseline

MeanDist remains the V4.2 baseline and continues as the recommended geometric scale. No clearly superior alternative has been identified. The ~30% CV persists to N=1000 and may represent genuine attractor ensemble variance.

---

## D. Prospective Validation Rules

1. No physical c or G used in selection or tuning
2. No SI comparison outcomes used
3. Freeze before any comparison (blind protocol)
4. No re-freeze after comparison
5. Anti-feedback gates locked
6. SHA-256 manifest generated at freeze
7. All predictions auditable and reproducible

---

## E. Freeze-Audit-Compare Protocol (from PLAP)

1. **Pre-freeze:** select candidate, verify anti-circularity
2. **Freeze:** compute L_scale, re-freeze predictions, generate SHA-256
3. **Audit:** verify manifest hash, no mutable deps, gate integrity
4. **Compare:** blind comparison to SI values, no re-freeze

---

## F. Stress-Test Plan

| Test | Purpose |
|:---|:---|
| N-stress | N=40-200, stability of selected proxy |
| Seed-stress | 50+ independent seeds, CV convergence |
| Law-stress | Exponential, gaussian, power-law |
| Load-stress | s=0.01-0.30, attractor robustness |
| Regime-stress | xi=1.0-3.0, K0=0.5-2.0 |

---

## G. Null Controls

- Random distances: destroy geometric structure
- K=0: no coupling, no attractor
- Shuffled topology: randomized adjacency

---

## H. Open Risks

| Risk | Mitigation |
|:---|:---|
| Proxy CV not improving on MeanDist | MeanDist remains baseline |
| G_eff prediction shifting significantly | Documented as expected cubic sensitivity |
| Observer-frame dependence | Verify frame-invariance for primary |

---

## I. Planned Suites

| Suite | Purpose |
|:---|:---|
| V4_4_ProspectiveLengthAnchorValidation_Tests.cs | Core validation of selected anchor |
| V4_4_LengthAnchorStressTesting_Tests.cs | N/seed/law/load/regime stress |
| V4_4_MeanDistRobustness_Tests.cs | Deep robustness analysis of MeanDist |
| V4_4_FutureFrozenPredictionBranch_Tests.cs | Execute freeze protocol with new anchor |
| V4_4_ExternalValidationAfterFreeze_Tests.cs | Blind comparison gated by freeze |

---

## J. Claim Discipline

**SUPPORTED:** Geometric-scale evidence chain exists. MeanDist is the baseline.
**CONDITIONAL:** All V4.3 findings depend on finite N, proxy definitions, primary regime.
**HYPOTHESIS:** A refined anchor may reduce G_eff uncertainty.
**NOT CLAIMED:** Physical c, G, SI units, spacetime, GR, astrophysical data.
