# TRM V5.1 — Replication Expansion and Ensemble Validation

**Status:** EXPLORATORY
**Branch:** `feature/v5.1-replication-expansion-and-ensemble-validation`
**Base:** `v5.0-independent-replication-complete`
**Date:** 2026-07-15

---

## Goal

Expand independent replication from single-campaign replication (V5.0) to ensemble-based validation across multiple seeds, regimes, and robustness dimensions.

## Predecessor

V5.0 completed the first independent replication campaign of the V4.5 prospective prediction pipeline:

```
Protocol → Execution → Audit → Comparison → Interpretation → Branch Synthesis
```

V5.1 extends this with ensemble-based analysis to characterize distributional stability, regime sensitivity, and failure boundaries.

## Objectives

1. **Multi-seed Ensemble Analysis**
   - Run 10+ independent seeds
   - Characterize distribution of prediction values
   - Compute ensemble statistics (mean, variance, percentiles)
   - Identify outlier-sensitive metrics

2. **Regime Expansion**
   - Sweep xi: 1.50, 1.65, 1.80, 1.95, 2.10
   - Sweep K0: 0.90, 1.05, 1.15, 1.25, 1.40
   - Identify regime boundaries where replication degrades

3. **Coupling-Law Robustness**
   - Test Gaussian coupling law
   - Test power-law coupling
   - Compare replication stability across laws

4. **Continuum Replication**
   - Extend N to 500, 800, 1000
   - Characterize finite-N bias at ensemble scale

5. **Ensemble Error Budget**
   - Decompose variance: seed, N, regime, coupling law
   - Identify dominant uncertainty sources

6. **External Reviewer Reproduction**
   - Define protocol for independent third-party replication
   - Package frozen artifacts for external validation

## Proposed Suite Sequence

| Suite | Tag | Purpose |
|-------|-----|---------|
| Replication Ensemble Protocol | REP | Define ensemble governance |
| Multi-seed Ensemble Execution | REE | Run 10+ independent seeds |
| Ensemble Statistics | RES | Compute distribution statistics |
| Regime Sensitivity | RRS | Sweep xi, K0 parameter space |
| Coupling-Law Robustness | RCL | Test alternative coupling laws |
| Continuum Ensemble | RCE | Extend N to 500, 800, 1000 |
| Ensemble Error Budget | REB | Full variance decomposition |
| Ensemble Interpretation | REI | Claim-disciplined interpretation |
| Ensemble Branch Synthesis | REBS | V5.1 branch synthesis |

## Claim Discipline

Inherited from V5.0 and V4.5:
- SUPPORTED / CONDITIONAL / HYPOTHESIS / NOT CLAIMED at every phase
- No anchor reselection, no parameter tuning, no post-hoc optimization
- All comparisons governed by inherited governance rules
