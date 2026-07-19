# TRM V5.1 — Replication Ensemble Interpretation

**Suite:** V5_1_ReplicationEnsembleInterpretation_Tests.cs
**Tag:** REI
**Branch:** feature/v5.1-replication-expansion-and-ensemble-validation
**Date:** 2026-07-15
**Status:** INTERPRETATION-A — COMPLETE

---

## Stability Tiers

| Metric | Expected Stability | Driver |
|--------|:------------------:|--------|
| omega_anchor | HIGHLY STABLE | Simple frequency proxy, CV~0.01 |
| meanDist_anchor | STRUCTURALLY VARIABLE | Topology-dependent geometric proxy |
| c_eff | MODERATELY STABLE | ω-dominated, inherits ω stability |
| G_eff | STRUCTURALLY VARIABLE | MD³ amplification of topology variation |

## Variability Attribution

**INTRINSIC:** Graph topology (seed), natural frequency spread, Kuramoto dynamics.
**PROTOCOL:** Frozen regime, primitives, proxies, governance — all fixed.

## Recommended Next Suite

`V5_1_ReplicationEnsembleBranchSynthesis_Tests.cs`
