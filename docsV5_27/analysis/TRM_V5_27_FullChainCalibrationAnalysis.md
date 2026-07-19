# TRM V5.27 Full Chain Calibration Analysis

**Version:** 1.0
**Date:** 2026-07-19
**Branch:** `feature/v5.27-full-chain-rescue-calibration-and-probability`
**Predecessor Suites:** FCE_01–FCE_05 (complete)
**Tests:** 2748 passed, 0 failed (as of V5.27 initialization)

---

## 1. Executive Summary

The Full Chain Execution (FCE) suite has established that the frozen c3OmegaShift > 0.1
two-stratum probability table is the best validated practical rescue-risk model for V5.27.
The full mechanistic chain is retained as explanatory structure but not as a predictive model.

**Preferred model:**

| Stratum | Definition | P(rescue) | 95% Wilson CI |
|:--------|:-----------|----------:|:--------------|
| A | c3OmegaShift ≤ 0.1 | 2.1% | [0.7%, 5.9%] |
| B | c3OmegaShift > 0.1 | 17.1% | [8.1%, 32.7%] |

Enrichment: 8.3×. Null permutation p < 0.01%.

---

## 2. Model Comparison

| Metric | Sign Rule | c3OmgS Binary | Full-Chain | Two-Stratum |
|:-------|:---------:|:-------------:|:----------:|:-----------:|
| Rescue rate | 6.0% | 8.6% | 6.2% | 8.6% / 17.1% |
| Enrichment | 1.8× | 2.6× | 1.9× | 8.3× |
| Brier Δ | −0.0003 | −0.0005 | −0.0001 | ±0.004 |
| Split wins | 1/5 | 4/5 | 1/5 | 13/13 |
| Cohort stable | — | YES | NO | YES |
| Inversions | 0 | 0 | 3/13 | 0 |
| Null p-value | — | — | — | <0.01% |
| Interpretability | Simple | Simple | Complex | Calibrated |

**Winner:** Two-stratum c3OmgS > 0.1 probability table.

---

## 3. Full-Chain Collapse Analysis

The full-chain selector collapsed (< c3Bin by >5pp) in 3/13 stress splits.

### Root Causes

1. **Over-conditioning:** 5 binary conditions on populations of n=14–16 selected profiles.
   Rare events cannot support this many conditions.

2. **Population-dependent thresholds:** Mechanism flags use median splits of the training
   distribution. When the test distribution shifts (new cohort, new N-window), medians shift
   and different profiles are selected. This is a form of implicit train-test leakage via
   the feature construction.

3. **Cohort overfitting:** The selector performs adequately on its implicit training cohort
   but fails on unseen cohorts (cohort 2–3 FCE_03: chain = 0.0%).

4. **Sparsity:** With only ~15 rescues across 361 profiles, a 5-condition filter leaves
   virtually no signal.

### Not a Mechanism Failure

The mechanistic decomposition (d_tail → deltaD → deltaK → omegaPerK sign → c3OmegaShift →
rescue) remains valid. The collapse is a failure of the binary selector operationalization,
not of the underlying chain structure.

---

## 4. Minimal Robust Summary

c3OmegaShift > 0.1 captures essentially all predictive signal from the full chain.

Evidence:

1. Adding omegaPerK sign to c3OmgS: no improvement (identical 8.8% in FCE_02).
2. Adding mechanism variables (d-tail, deltaD, deltaK): degrades to 5.6% or baseline.
3. Full chain (c3OmgS + mechanisms): 7.1%, strictly worse than c3OmgS alone.
4. c3OmgS alone: 2.6× enrichment, stable across 13 stress splits, null p < 0.01%.

**Conclusion:** c3OmegaShift > 0.1 is the minimal robust sufficient summary of the full
validated gain decomposition for rescue probability purposes.

---

## 5. Probability Calibration Status

**Classification:** Risk stratification with calibrated point estimates.

| Property | Status |
|:---------|:------:|
| Enrichment demonstrated | YES (8.3×) |
| Calibrated probabilities | YES (with caveats) |
| Null stress survived | YES (p < 0.01%) |
| Predictive model | CONDITIONAL |
| Independent validation | RECOMMENDED |

**Limitations:**
- Not continuous calibration (FCE_02: continuous fails)
- Not deterministic (best P_B = 17.1%)
- Wilson CIs are wide: [8.1%–32.7%]
- Bootstrap Brier CIs overlap with constant baseline
- N≤70 is rescue-immune under tested operators
- Event counts are low (15 rescues / 361 profiles)

---

## 6. Decision Gates

| Gate | Description | Status |
|:-----|:------------|:------:|
| A | Two-Stratum Calibration Supported | **REACHED** |
| B | Full Chain Predictive Superiority Rejected | **REACHED** |
| C | Mechanistic Chain Retained | **REACHED** |
| D | Probability Model Validated | CONDITIONALLY REACHED |
| E | Enrichment Only | SURPASSED |
| F | More Validation Required | RECOMMENDED |
| G | Calibration Unstable | NOT REACHED |

---

## 7. Claim Classification

**Model C: Two-Stratum Probability Table**

The frozen c3OmgS > 0.1 two-stratum table is the preferred calibrated rescue-risk model
for V5.27. The full mechanistic chain is retained as explanatory structure but not as a
practical predictive model.

### SUPPORTED

- Two-stratum c3OmgS > 0.1 probability table is the best validated model.
- P_B = 17.1% [8.1%, 32.7%], P_A = 2.1% [0.7%, 5.9%], enrichment = 8.3×.
- Full mechanistic chain is valid but the full-chain binary selector is unstable.
- c3OmegaShift is the minimal robust sufficient summary.
- Null permutation test passed: p < 0.01%.

### CONDITIONAL

- Low event counts (15 rescues / 361 profiles).
- Wilson CIs are wide.
- Bootstrap Brier CIs overlap with constant baseline.
- Finite-N, cohort-limited, operator-class-limited.
- N=50–64 is SUPPORTED as inaccessible; N=65–79 is the adaptive-active domain.

### HYPOTHESIS

- H31 (FCA): c3OmegaShift > 0.1 defines a stable rescue-enriched risk stratum
  replicable across splits, cohorts, and N≥72.

### NOT CLAIMED

- Deterministic rescue threshold
- Continuous probability calibration
- Full-chain predictive superiority
- Causality
- Physical interpretation of N-boundaries
- Universal control
- Modified M3++
- Retuned thresholds
- New variables or correction classes

---

## 8. Recommended Next Suite

**FCI: Probability Validation Audit**

Independent validation of the frozen two-stratum table on a new simulation batch (different
seeds, same M3++ pipeline). If FCI passes, proceed to FCS_FinalSynthesis. If FCI fails,
revert to enrichment-only claim.

---

## 9. FCE Suite Reference

| Suite | Commit | Finding |
|:------|:-------|:--------|
| FCE_01 | `71b75e1` | No information accumulation; c3OmgS strongest predictor |
| FCE_02 | `8084277` | c3OmgS > 0.1 binary best frozen model; continuous calibration fails |
| FCE_03 | `34fdf4a` | c3OmgS > 0.1 replicates across splits/cohorts; chain fails on unseen data |
| FCE_04 | `a36b467` | Two-stratum table calibrated: P_B = 17.1% [8.1%–32.7%], 8.3× enrichment |
| FCE_05 | `27042f9` | 13/13 stress splits, zero inversions, null p < 0.01% |
| FCA | (this) | Meta-analysis: two-stratum table is preferred V5.27 model |

---

*Generated 2026-07-19. This document is the authoritative calibration analysis for V5.27.*
