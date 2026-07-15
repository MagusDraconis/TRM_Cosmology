# TRM V5.1 — Replication Ensemble Protocol

**Suite:** V5_1_ReplicationEnsembleProtocol_Tests.cs
**Tag:** REP
**Branch:** feature/v5.1-replication-expansion-and-ensemble-validation
**Base:** v5.0-independent-replication-complete
**Date:** 2026-07-15
**Status:** PROTOCOL DEFINED

---

## A. Motivation

V5.0 completed the first independent replication campaign with a two-seed comparison (V4.5 seed=45 vs IRE seed=50). While this established pipeline reproducibility, it did not characterize the distribution of replication outcomes. V5.1 expands replication to ensemble-level validation to quantify distributional stability and identify regime boundaries.

## B. Relation to V5.0

| Aspect | V5.0 | V5.1 |
|--------|------|------|
| Seeds | 3 (50, 55, 60) | 10+ (100-109) |
| Comparison | Binary (45 vs 50) | Distribution (ensemble vs reference) |
| Classification | Per-run (REPLICATION-A/B/C) | Per-run + Ensemble (ENSEMBLE-A/B/C) |
| Sensitivity | Not characterized | Seed, N, law, load sensitivity |
| Outliers | Not defined | >2σ detection |

## C. Ensemble Design

7-phase pipeline:
1. Protocol (REP) — define ensemble dimensions, metrics, classifications
2. Execute (REE) — run all ensemble runs independently
3. Freeze (REF) — freeze with SHA-256 hashes
4. Audit (REA) — audit each run + ensemble-level
5. Compare (REC) — compute ensemble statistics vs V4.5 reference
6. Interpret (REI) — claim-disciplined interpretation
7. Synthesize (REBS) — branch completion

## D. Frozen Inputs

- V4.5 frozen prediction values and hashes
- V4.5 regime: xi=1.80, K0=1.15, N=100, exponential
- V5.0 replication results
- Seed list: 100-109 (10 seeds)
- Parameter grid: N, law, load values
- Scoring criteria (14 ensemble metrics)

## E. Allowed Inputs

V4.5 + V5.0 artifacts (read-only), frozen ensemble inputs, same primitives and proxies.

## F. Forbidden Inputs (30 items, 6 categories)

- Artifact Mutation (4)
- Parameter Tuning (5)
- Post-hoc Manipulation (6)
- External Feedback (5)
- Anchor Reselection (4)
- Comparison Violation (4)

## G. Ensemble Dimensions

| Dimension | Primary | Extension |
|-----------|---------|-----------|
| Seed | 100-109 (10 seeds) | — |
| N | 100 | 40, 80, 120, 200 |
| Coupling law | Exponential | Gaussian |
| Load (s) | 0.08 | 0.04, 0.12, 0.16, 0.20 |

## H. Success Metrics (14 metrics)

E1-E6: Distribution (mean, median, std, CV, percentiles, outlier rate)
E7-E10: Sensitivity (seed, N, law, load)
E11-E14: Reproducibility (V4.5 agreement, V5.0 agreement, structural ratio, score)

## I. Classification Rules

**ENSEMBLE-A:** CV ≤ Unc, outlier rate ≤ 10%, mean within Unc
**ENSEMBLE-B:** CV ≤ 10×Unc, outlier rate ≤ 30%, mean within 10×Unc
**ENSEMBLE-C:** CV > 10×Unc OR outlier rate > 30%
**REJECT:** Protocol violated, audit failed

## J. Audit Requirements (A1-A9)

A1-A4: Per-run audit (pre-execution, post-generation, post-freeze, replication)
A5-A9: Ensemble-level (coverage, independence, integrity, reproducibility, audit chain)

## K. Claim Discipline

**SUPPORTED:** Protocol fully defined before any execution.
**CONDITIONAL:** Results depend on frozen ensemble dimensions. Stability ≠ correctness.
**HYPOTHESIS:** Omega stable, MeanDist broader, c_eff ω-dominated, G_eff length-dominated.
**NOT CLAIMED:** 12 items — no physical, validation, derivation claims.

## L. Recommended Next Suite

`V5_1_ReplicationEnsembleExecution_Tests.cs`
