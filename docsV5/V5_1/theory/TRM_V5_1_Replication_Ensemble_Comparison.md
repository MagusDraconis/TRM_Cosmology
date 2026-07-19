# TRM V5.1 — Replication Ensemble Comparison

**Suite:** V5_1_ReplicationEnsembleComparison_Tests.cs
**Tag:** REC
**Branch:** feature/v5.1-replication-expansion-and-ensemble-validation
**Date:** 2026-07-15

---

## Overview

Compares the 10-seed ensemble distribution against V4.5 reference and V5.0 replication. Computes distribution overlap, reproducibility scores, and stability assessments.

## Comparison Framework

| Metric | Ensemble vs V4.5 | Distribution Overlap | Reproducibility |
|--------|:----------------:|:--------------------:|:---------------:|
| omega_anchor | RelErr computed | IQR contains V4.5? | CV-based score |
| meanDist_anchor | RelErr computed | IQR contains V4.5? | CV-based score |
| c_eff | RelErr computed | IQR contains V4.5? | CV-based score |
| G_eff | RelErr computed | IQR contains V4.5? | CV-based score |

## Stability Tiers

| Tier | CV Range | Meaning |
|------|:--------:|---------|
| HIGHLY REPRODUCIBLE | ≤ 0.02 | Sharp distribution, seed-stable |
| MODERATELY REPRODUCIBLE | ≤ 0.15 | Broader but structured |
| REALIZATION SENSITIVE | > 0.15 | Significant topology dependence |

## Recommended Next Suite

`V5_1_ReplicationEnsembleInterpretation_Tests.cs`
