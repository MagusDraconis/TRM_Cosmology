# TRM V4.2 — Physical Calibration Principles

**Date:** 2026-07-14
**Status:** THEORY — EXPLORATORY

---

## 1. Core Principle: External Anchors Only

TRM calibration is a **one-way mapping** from verified internal quantities to external reference standards. Internal quantities are never adjusted to match external values.

```
External Reference → T_scale/L_scale/M_scale → Internal units
                     (one-way, no feedback)
```

---

## 2. Anti-Circularity

| Rule | Enforcement |
|:---|:---|
| External reference selected BEFORE evaluation | Compile-time constant or pre-registered |
| No fitting Omega to c | Omega computed from frozen model only |
| No fitting MeanDist to c | Length independent of time calibration |
| No fitting G_eff to G | G_eff is derived, not fitted |
| Single anchor per dimension | One time, one length, one source anchor |
| No post-hoc adjustment | Reference frozen after first evaluation |
| No multi-anchor averaging | No blending anchors to improve agreement |

---

## 3. Calibration Sequence

The calibration must proceed in order:

1. **Time calibration** — Omega → T_scale (independent of length and source)
2. **Length calibration** — MeanDist → L_scale (independent of source, uses T_scale)
3. **Source calibration** — OmegaSource → M_scale (uses T_scale and L_scale)
4. **c_eff prediction** — Compute from T_scale × L_scale, compare to physical c (no fitting)
5. **G_eff prediction** — Compute from alpha × L²/(T²·M), compare to physical G (no fitting)

---

## 4. Prediction Protocol

1. Freeze model version, parameters, anchors
2. Compute calibration scales (T, L, M)
3. Compute c_eff and G_eff predictions
4. Compare to physical constants
5. Report ALL comparisons (favorable and unfavorable)
6. Do NOT tune after seeing results

---

## 5. Uncertainty Budget

Every calibrated quantity must report:
- Seed variance contribution
- N-scaling systematic contribution
- Load sensitivity contribution
- Law sensitivity contribution
- Total stochastic uncertainty

---

## 6. Claim Discipline

**SUPPORTED:** Calibration scales are computable from frozen internal + external anchors.
**CONDITIONAL:** All results depend on finite N, tested regimes, proxy definitions.
**HYPOTHESIS:** c_eff and G_eff may approach physical values after calibration.
**NOT CLAIMED:** Physical c, G, spacetime, GR, Einstein equations, gravity.
