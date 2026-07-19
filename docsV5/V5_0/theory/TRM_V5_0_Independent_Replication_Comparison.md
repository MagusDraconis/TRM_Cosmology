# TRM V5.0 — Independent Replication Comparison

**Suite:** V5_0_IndependentReplicationComparison_Tests.cs  
**Tag:** IRC  
**Branch:** feature/v5.0-independent-replication-and-validation  
**Base:** v4.5-prospective-anchor-prediction-complete  
**Date:** 2026-07-15  
**Status:** REPLICATION COMPARISON COMPLETE

---

## Overview

The Independent Replication Comparison (IRC) compares the independently replicated prediction chain (IRE, seed=50) against the frozen V4.5 reference chain (seed=45). It computes per-metric comparisons, structural similarity scores, reproducibility scores, and divergence analysis.

## Comparison Framework

| Metric | V4.5 (seed=45) | IRE (seed=50) | Classification |
|--------|:-------------:|:-------------:|:--------------:|
| omega_anchor | Computed | Computed | Classified |
| meanDist_anchor | Computed | Computed | Classified |
| c_eff | Computed | Computed | Classified |
| G_eff | Computed | Computed | Classified |

## Replication Classes (from IRP)

| Class | Condition | Interpretation |
|-------|-----------|----------------|
| REPLICATION-A | rel_err ≤ 1.0 × Unc | Seed-stable, structurally robust |
| REPLICATION-B | rel_err ≤ 10.0 × Unc | Compatible, some seed sensitivity |
| REPLICATION-C | rel_err > 10.0 × Unc | Genuine realization dependence |
| REJECT | Protocol violated | Audit failed |

## Structural Similarity

Compares the internal structure of predictions (ratios, ordering, dimensional consistency) to determine whether the same structure emerges from different seeds.

## Reproducibility Score

Weighted average of normalized per-metric agreement:
- omega_anchor: weight 0.15 (most stable)
- meanDist_anchor: weight 0.30 (dominates G_eff)
- c_eff: weight 0.25
- G_eff: weight 0.30

**Score interpretation:**
- ≥ 0.75: HIGH — Strong replication
- ≥ 0.50: MODERATE — Partial replication
- < 0.50: LOW — Weak replication

## Divergence Analysis

All divergence is attributed to:
- Different graph topology (different seed)
- Different initial phase distribution
- Different natural frequency spread

NOT attributed to:
- Parameter changes
- Proxy definition changes
- Code path changes
- Protocol violations

## V5.0 Pipeline

| Suite | Status |
|-------|--------|
| IRP | PROTOCOL DEFINED |
| IRE | REPLICATION EXECUTED |
| IRA | AUDIT-A — COMPLETE |
| IRC | REPLICATION COMPARISON COMPLETE ← this suite |
| IRI | PENDING |

## Claim Discipline

**SUPPORTED:** Comparison executed under IRP governance. All 4 metrics compared. Structural similarity and reproducibility scores computed.

**CONDITIONAL:** Same regime, same primitives. Different seeds → different predictions (expected).

**HYPOTHESIS:** Agreement = seed-stability; Disagreement = sensitivity. Neither validates nor invalidates TRM.

**NOT CLAIMED:** Validation, falsification, proof, physical derivation.

## Recommended Next Suite

`V5_0_IndependentReplicationInterpretation_Tests.cs`
