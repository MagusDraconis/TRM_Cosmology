# TRM V5.2 — Regime Sensitivity Interpretation

**Suite:** V5_2_RegimeSensitivityInterpretation_Tests.cs
**Tag:** RSI
**Date:** 2026-07-15
**Status:** INTERPRETATION-A — COMPLETE

---

## Stability Tiers

| Metric | Stability | Driver |
|--------|:---------:|--------|
| Omega | HIGHLY STABLE | Frequency proxy robust |
| c_eff | MODERATELY STABLE | ω-buffered |
| MeanDist | REGIME SENSITIVE | Topology + parameter |
| G_eff | MOST SENSITIVE | MD³ amplification |

## Dominant Drivers (ranked)

1. G_eff — cubic MD³
2. MeanDist — topology dependence
3. c_eff — ω-buffered
4. Omega — synchronization robust

## Recommended Next Suite

`V5_2_RegimeSensitivityBranchSynthesis_Tests.cs`
