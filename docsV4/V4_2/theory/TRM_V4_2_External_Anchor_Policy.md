# TRM V4.2 — External Anchor Policy

**Date:** 2026-07-14
**Status:** POLICY — GOVERNANCE

---

## 1. Time Anchor

| Property | Specification |
|:---|:---|
| Internal anchor | Omega / Omega_mean |
| External reference | Pre-registered dimensionless time interval |
| Calibration formula | T_scale = ExternalTimeRef / Omega_ref |
| Dependencies | None (independent of length, source, c_eff, G_eff) |
| Forbidden | Fitting Omega to physical c, adjusting Omega after calibration, multi-anchor averaging |

---

## 2. Length Anchor

| Property | Specification |
|:---|:---|
| Internal anchor | MeanDist |
| External reference | Pre-registered dimensionless length interval |
| Calibration formula | L_scale = ExternalLengthRef / MeanDist_ref |
| Dependencies | None (independent of source, c_eff, G_eff) |
| Forbidden | Fitting MeanDist to physical c, adjusting after calibration, multi-anchor averaging |

---

## 3. Source Anchor

| Property | Specification |
|:---|:---|
| Internal anchor | OmegaSource |
| External reference | Pre-registered dimensionless mass/energy reference |
| Calibration formula | M_scale = ExternalSourceRef / OmegaSource_ref |
| Dependencies | Uses T_scale and L_scale for G_eff only, not for anchor itself |
| Forbidden | Fitting OmegaSource to G, adjusting after calibration, multi-anchor averaging |

---

## 4. c_eff Prediction

| Property | Specification |
|:---|:---|
| Internal computation | c_eff_internal = Dimless × MeanDist |
| Calibrated prediction | c_eff_calibrated = c_eff_internal × L_scale / T_scale |
| Comparison | Compare to physical c = 299,792,458 m/s |
| Forbidden | Fitting c_eff to match physical c, post-hoc adjustment |

---

## 5. G_eff Prediction

| Property | Specification |
|:---|:---|
| Internal computation | G_eff_design = Alpha × L²/(T²·M) |
| Calibrated prediction | G_eff_calibrated = G_eff_design × L_scale³/(T_scale²·M_scale) |
| Comparison | Compare to physical G = 6.67430×10⁻¹¹ m³/(kg·s²) |
| Forbidden | Fitting G_eff to match physical G, post-hoc adjustment |

---

## 6. Admissible External References

- Published physical constants (CODATA)
- SI unit definitions
- Pre-registered dimensionless reference intervals
- Independent experimental measurements

## 7. Forbidden External References

- SPARC or any astrophysical dataset (during calibration)
- Model-dependent quantities
- Post-hoc selected references
- References chosen after seeing TRM outputs
