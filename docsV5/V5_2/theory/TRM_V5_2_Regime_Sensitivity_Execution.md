# TRM V5.2 — Regime Sensitivity Execution

**Suite:** V5_2_RegimeSensitivityExecution_Tests.cs
**Tag:** RSE
**Date:** 2026-07-15

---

## Overview

Executes the frozen regime-sensitivity campaign across 23 regime points (69 total runs). 5 phases covering xi, K0, load, N, and coupling-law variation.

## Regime Grid

| Phase | Parameter | Range | Runs |
|-------|-----------|-------|:----:|
| P1 | xi | {1.50, 1.65, 1.80, 1.95, 2.10} | 15 |
| P2 | K0 | {0.90, 1.05, 1.15, 1.25, 1.40} | 15 |
| P3 | s (load) | {0.04, 0.08, 0.12, 0.16, 0.20} | 15 |
| P4 | N | {80, 100, 200, 500, 800} | 15 |
| P5 | law | {exp, gauss, pow} | 9 |

## Per-Regime Metrics

- Ensemble mean, CV, outlier rate per metric
- ENSEMBLE-A/B/C classification per metric
- REGIME-A/B/C classification per regime point

## Recommended Next Suite

`V5_2_RegimeSensitivityAudit_Tests.cs`
