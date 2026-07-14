# TRM V4.2 — Physical Calibration and Prediction

**Status:** EXPLORATORY
**Branch:** `feature/v4.2-physical-calibration-and-prediction`
**Date:** 2026-07-14
**Base:** V4.1 Calibration Framework Complete (1450/1450)

---

## Directory Rules (CRITICAL)

| Directory | Version | Status |
|:---|:---|:---|
| `/docs` | V3.4 | FROZEN |
| `/docsV4` | V4 | FROZEN |
| `/docsV4_1` | V4.1 | COMPLETED — internal causal geometry |
| `/docsV4_2` | V4.2 | ACTIVE — physical calibration and prediction |

**Never mix V4.2 documents into docsV4_1 or earlier directories.**

---

## V4.2 Goal

Convert verified V4.1 internal anchors into externally calibrated prediction protocols without fitting physical constants, astrophysical data, or introducing circularity.

---

## Starting Assumptions

### SUPPORTED from V4.1

- Omega is the primary internal attractor clock (CV ~0.01).
- MeanDist is the internal length anchor candidate.
- OmegaSource is the internal source anchor candidate.
- c_eff_internal is measurable and internally universal.
- G_eff design is internally defined as alpha × L²/(T²·M).
- Calibration governance and anti-circularity are complete.
- Internal causal geometry is complete under tested conditions.

### CONDITIONAL

- All results depend on finite N, tested regimes, proxy definitions, seeds, load range, and coupling laws.
- No true N→∞ proof exists.
- No external physical calibration has yet been executed.

### HYPOTHESIS

- c_eff_internal may become comparable to physical c after external time and length calibration.
- G_eff design may become comparable to physical G after external time, length, and source calibration.
- Internal causal geometry may be a precursor to physical spacetime interpretation.

### NOT CLAIMED

Physical c, physical G, physical mass, physical energy, SI units, D=3, physical spacetime, physical metric tensor, Lorentz invariance, Special Relativity, General Relativity, Einstein equations, gravity, SPARC explanation, dark matter replacement.

---

## Workspace Structure

```
docsV4_2/
  README.md                                          ← This file
  theory/
    TRM_V4_2_Physical_Calibration_Principles.md      ← Calibration theory
    TRM_V4_2_External_Anchor_Policy.md               ← Anchor policies
  review/
    TRM_V4_2_V4_1_Baseline_Review.md                 ← Frozen baseline summary
  experiments/
    TRM_V4_2_Experiment_Log.md                       ← Append-only experiment log
    TRM_V4_2_External_Time_Calibration_Execution.md  ← ETCE experiment design
  papers/
    TRM_V4_2_Physical_Calibration_And_Prediction_Outline.md  ← Paper outline
```
