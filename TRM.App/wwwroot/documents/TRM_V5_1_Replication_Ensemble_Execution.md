# TRM V5.1 — Replication Ensemble Execution

**Suite:** V5_1_ReplicationEnsembleExecution_Tests.cs
**Tag:** REE
**Branch:** feature/v5.1-replication-expansion-and-ensemble-validation
**Base:** v5.0-independent-replication-complete
**Date:** 2026-07-15

---

## Overview

The Replication Ensemble Execution (REE) runs the first ensemble replication campaign using the frozen REP protocol. 10 independent seeds (100-109) are executed at baseline regime (xi=1.80, K0=1.15, N=100, exponential), producing a full distribution of prediction values.

## Ensemble Parameters

| Parameter | Value |
|-----------|-------|
| Seeds | 100-109 (10 seeds) |
| xi | 1.80 |
| K0 | 1.15 |
| N | 100 |
| Coupling law | Exponential |
| s (load) | 0.08 |

## Distribution Metrics (per metric)

- E1: Ensemble mean
- E2: Ensemble median
- E3: Ensemble std
- E4: Ensemble CV
- E5: Percentile band (P25, P75)
- E6: Outlier rate (>2σ)

## Sensitivity Metrics

- E7: Seed sensitivity (CV across seed ensemble)
- E8-E10: N, law, load sensitivity (deferred to parameter grid phases)

## Ensemble Classification

| Class | CV ≤ Unc | Outlier ≤ 10% | Mean within Unc |
|-------|:--------:|:-------------:|:---------------:|
| ENSEMBLE-A | ✓ | ✓ | ✓ |
| ENSEMBLE-B | ≤ 10×Unc | ≤ 30% | ≤ 10×Unc |
| ENSEMBLE-C | > 10×Unc | > 30% | > 10×Unc |

## Expected Findings

- Omega: ultra-stable (historical CV ~0.01)
- MeanDist: broader but structured (historical CV ~0.30)
- c_eff: omega-dominated
- G_eff: meanDist-dominated (cubic)

## Recommended Next Suite

`V5_1_ReplicationEnsembleAudit_Tests.cs`
