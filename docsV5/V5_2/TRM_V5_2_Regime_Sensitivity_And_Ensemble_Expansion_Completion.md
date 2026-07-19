# TRM V5.2 — Regime Sensitivity Completion

**Branch:** `feature/v5.2-regime-sensitivity-and-ensemble-expansion`
**Base:** `v5.1-replication-ensemble-validation-complete`
**Date:** 2026-07-15
**Status:** COMPLETE

---

## A. Executive Summary

V5.2 completed the first regime-sensitivity campaign. 23 regime points across 5 axes. The central finding: **seed stability and regime stability are distinct.** Omega is seed-stable but xi-regime-sensitive. MeanDist is seed-variable but xi-regime-robust.

## H. Seed Stability vs Regime Stability (Central Finding)

| Metric | Seed Stability (same regime) | Regime Stability (across xi) |
|--------|:---------------------------:|:---------------------------:|
| Omega | HIGHLY STABLE | REGIME SENSITIVE |
| MeanDist | STRUCTURALLY VARIABLE | HIGHLY STABLE |
| c_eff | MODERATELY STABLE | MODERATELY STABLE |
| G_eff | STRUCTURALLY VARIABLE | MODERATELY STABLE |

## M. Corrected Stability Picture

**OLD (V5.1):** Omega always stable, MeanDist always variable.
**NEW (V5.2):** Omega is seed-stable but regime-sensitive (xi changes frequency). MeanDist is seed-variable (topology) but regime-robust (geometric scale persists).

## S. Recommended Next Branch

`feature/v5.3-stability-mechanism-and-control-parameters`
