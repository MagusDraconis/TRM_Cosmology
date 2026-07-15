# TRM V5.2 — Regime Sensitivity Comparison

**Suite:** V5_2_RegimeSensitivityComparison_Tests.cs
**Tag:** RSC
**Date:** 2026-07-15

---

## Overview

Compares all 23 regime points against primary reference (xi=1.75, K0=1.2, s=0.10, N=100, exp). Computes stability scores, degradation scores, and regime stability map.

## Primary Reference

| Parameter | Value |
|-----------|-------|
| xi | 1.75 |
| K0 | 1.2 |
| s (load) | 0.10 |
| N | 100 |
| law | exp |

## Key Findings

| Phase | Stability | Dominant Driver |
|-------|:---------:|-----------------|
| P1-xi | HIGHLY/MODERATELY | G_eff (MD³) |
| P2-K0 | MODERATELY | G_eff |
| P3-s | MODERATELY/SENSITIVE | MeanDist |
| P4-N | MODERATELY/SENSITIVE | MeanDist |
| P5-law | MODERATELY/SENSITIVE | G_eff |

## Recommended Next Suite

`V5_2_RegimeSensitivityInterpretation_Tests.cs`
