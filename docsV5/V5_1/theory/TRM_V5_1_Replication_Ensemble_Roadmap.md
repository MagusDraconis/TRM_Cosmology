# TRM V5.1 — Replication Ensemble Roadmap

**Status:** EXPLORATORY
**Date:** 2026-07-15

---

## A. Motivation

V5.0 established that the V4.5 prospective prediction pipeline can be independently replicated under the same regime with different seeds. However, the V5.0 comparison was binary (seed=45 vs seed=50). A statistical ensemble characterization is needed to:

1. Quantify the distribution of replication outcomes
2. Identify regime boundaries where replication degrades
3. Test whether coupling-law choice affects replication stability
4. Characterize finite-N bias at ensemble scale
5. Provide a foundation for external third-party validation

## B. V5.0 Baseline

| Finding | Classification |
|---------|---------------|
| Pipeline independently replicated | SUPPORTED |
| Structural pattern preserved | SUPPORTED |
| Metric-level variation exists | CONDITIONAL |
| Two-seed comparison only | CONDITIONAL |
| Single regime tested | CONDITIONAL |

## C. Ensemble Replication Goals

| Goal | Criterion |
|------|-----------|
| Ensemble coverage | 10+ independent seeds per regime point |
| Distribution characterization | Mean, std, min, max, P25, P50, P75 per metric |
| Reproducibility score | Ensemble-weighted (replaces single-seed score) |
| Outlier detection | Identify metrics with >2σ spread |
| Regime boundaries | Identify xi/K0 where >50% of ensemble is REPLICATION-C |

## D. Multi-seed Strategy

| Parameter | Value |
|-----------|-------|
| Primary seed range | 100-109 (10 seeds) |
| Same regime | xi=1.80, K0=1.15, N=100, exponential |
| Same primitives | Sm, RP, Nm, DL, ExpUpd, OmegaField, MeanDistProxy |
| Independent UUIDs | Per seed |
| Independent hashes | SHA-256 per seed |

## E. Regime Sensitivity Strategy

Sweep parameters independently:
- xi ∈ {1.50, 1.65, 1.80, 1.95, 2.10} at K0=1.15
- K0 ∈ {0.90, 1.05, 1.15, 1.25, 1.40} at xi=1.80

For each regime point: 5-seed ensemble.

## F. Coupling-Law Robustness

Test alternative coupling update laws:
- Gaussian: K_ij = K0 * exp(-d_ij² / (2 * xi²))
- Power-law: K_ij = K0 / (1 + d_ij)^xi

Compare replication stability (ensemble CV) across laws.

## G. Continuum Replication

Extend N to 500, 800, 1000:
- Same regime (xi=1.80, K0=1.15)
- 3-seed ensemble per N point
- Compare with V4.5 V45FrozenPredictions at each N

## H. Ensemble Error Budget

Variance decomposition:
- σ²_total = σ²_seed + σ²_N + σ²_regime + σ²_law + σ²_residual

Identify dominant source for each metric.

## I. Claim Discipline

Same as V5.0:
- SUPPORTED: Ensemble statistics, regime characterization
- CONDITIONAL: Finite-N, regime-specific, proxy-defined
- HYPOTHESIS: Structural stability, regime boundaries
- NOT CLAIMED: Physical claims, validation, derivation

## J. Planned Suites

1. REP — Replication Ensemble Protocol
2. REE — Multi-seed Ensemble Execution
3. RES — Ensemble Statistics
4. RRS — Regime Sensitivity
5. RCL — Coupling-Law Robustness
6. RCE — Continuum Ensemble
7. REB — Ensemble Error Budget
8. REI — Ensemble Interpretation
9. REBS — Ensemble Branch Synthesis

**Recommended first suite:** `V5_1_ReplicationEnsembleProtocol_Tests.cs`
