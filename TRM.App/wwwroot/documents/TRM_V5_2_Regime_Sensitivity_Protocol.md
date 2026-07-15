# TRM V5.2 — Regime Sensitivity Protocol

**Suite:** V5_2_RegimeSensitivityProtocol_Tests.cs
**Tag:** RSP
**Date:** 2026-07-15
**Status:** PROTOCOL DEFINED

---

## Regime Classes

| Class | Definition |
|-------|-----------|
| PRIMARY | xi=1.80, K0=1.15, N=100, s=0.08, exponential |
| SAFE-BOUNDARY | CV ≤ 2× baseline for all metrics |
| DEGRADED | Any CV > 2× baseline but ≤ 5× |
| FAILURE-PROXIMAL | Any CV > 5× or outlier rate > 50% |

## Variation Axes (5)

xi, K0, load (s), N, coupling law

## Frozen Grid (23 points, 69 runs)

| Phase | Parameter | Range | Runs |
|-------|-----------|-------|:----:|
| 1 | xi | {1.50, 1.65, 1.80, 1.95, 2.10} | 15 |
| 2 | K0 | {0.90, 1.05, 1.15, 1.25, 1.40} | 15 |
| 3 | s | {0.04, 0.08, 0.12, 0.16, 0.20} | 15 |
| 4 | N | {80, 100, 200, 500, 800} | 15 |
| 5 | law | {Exp, Gaussian, Power-law} | 9 |

## Metrics

- 11 comparison metrics (RM1-RM11)
- 10 sensitivity metrics (SM1-SM10)

## Recommended Next Suite

`V5_2_RegimeSensitivityExecution_Tests.cs`
