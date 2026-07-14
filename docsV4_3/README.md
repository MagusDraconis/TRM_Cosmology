# TRM V4.3 — Geometric Scale Interpretation

**Status:** EXPLORATORY
**Branch:** `feature/v4.3-geometric-scale-interpretation`
**Base:** `v4.2-physical-calibration-complete` (1744 tests)
**Date:** 2026-07-14

---

## Directory Rules (CRITICAL)

| Directory | Version | Status |
|:---|:---|:---|
| `/docs` | V3.4 | FROZEN |
| `/docsV4` | V4 | FROZEN |
| `/docsV4_1` | V4.1 | COMPLETED |
| `/docsV4_2` | V4.2 | COMPLETED |
| `/docsV4_3` | V4.3 | ACTIVE |

Never mix V4.3 documents into earlier directories.

---

## V4.3 Objective

V4.2 found that **c_eff_SI is Omega-dominated** (MeanDist cancels, CV ~0.01) while **G_eff_SI is MeanDist³-dominated** (CV ~0.90). The MeanDist variance persists to N=1000 — it is structural, not noise.

V4.3 investigates the **geometric meaning of the TRM length scale**:
- Why does MeanDist vary across seeds?
- Is there a deeper geometric invariant hiding behind MeanDist?
- Can a fundamentally different geometric scale proxy reduce G_eff uncertainty?

---

## Starting Assumptions (from V4.2)

### SUPPORTED
- c_eff_SI = Kr86/Cs133 × Omega (MeanDist cancels exactly)
- c_eff_SI uncertainty: Omega-dominated (CV ~0.01)
- G_eff_SI uncertainty: MeanDist-dominated (cubic, CV ~0.90)
- MeanDist variance persists to N=1000
- All calibration anchors are non-circular

### CONDITIONAL
- Kr-86 primary length path
- L³/(T²·M) G_eff dimensional form
- Finite N, proxy definitions, primary regime

### HYPOTHESIS
- Persistent MeanDist variance may represent genuine attractor geometry
- Alternative geometric scales may capture the invariant structure

### NOT CLAIMED
Physical c, G, gravity, SI units, spacetime, Lorentz, SR, GR, Einstein equations, Newton, lensing, SPARC, dark matter, N→∞ proof.

---

## Workspace Structure

```
docsV4_3/
  README.md
  theory/
    TRM_V4_3_Geometric_Scale_Interpretation_Roadmap.md
  review/
  experiments/
  papers/
```
