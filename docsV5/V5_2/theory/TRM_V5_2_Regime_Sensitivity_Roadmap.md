# TRM V5.2 — Regime Sensitivity Roadmap

**Status:** EXPLORATORY
**Date:** 2026-07-15

---

## A. Motivation

V5.1 characterized ensemble stability at a single regime point (xi=1.80, K0=1.15, N=100, exponential). V5.2 expands this to characterize how ensemble stability varies across the parameter space.

## B. V5.1 Baseline

| Metric | Stability | CV |
|--------|:---------:|:--:|
| Omega | HIGHLY STABLE | ~0.01 |
| c_eff | MODERATELY STABLE | ω-dominated |
| MeanDist | STRUCTURALLY VARIABLE | ~0.30 |
| G_eff | STRUCTURALLY VARIABLE | MD³ amplified |

## C-G. Strategy

- xi sweep: {1.50, 1.65, 1.80, 1.95, 2.10} at K0=1.15
- K0 sweep: {0.90, 1.05, 1.15, 1.25, 1.40} at xi=1.80
- Load sweep: {0.04, 0.08, 0.12, 0.16, 0.20}
- Coupling laws: Exponential, Gaussian, Power-law
- N extension: {100, 200, 500, 800}

## J. Planned Suites

1. RSP — Regime Sensitivity Protocol
2. RSE — Regime Sensitivity Execution
3. RSA — Regime Sensitivity Audit
4. RSC — Regime Sensitivity Comparison
5. RSI — Regime Sensitivity Interpretation
6. RSBS — Regime Sensitivity Branch Synthesis

**Recommended first suite:** `V5_2_RegimeSensitivityProtocol_Tests.cs`
