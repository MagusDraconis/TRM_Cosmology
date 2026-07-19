# TRM V5.27 Final Synthesis — Full-Chain Rescue Calibration and Probability

**Version:** 1.0
**Date:** 2026-07-19
**Branch:** `feature/v5.27-full-chain-rescue-calibration-and-probability`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.27-full-chain-rescue-calibration-and-probability` |
| Base | V5.26 COMPLETE |
| Suites | FCP, FCE, FCA, FCI |
| V5.27 tests | 14 (3 FCP + 5 FCE + 5 FCA + 1 FCI) |
| Cumulative tests | 2762 passed, 0 failed |
| Commits | `71b75e1`, `8084277`, `34fdf4a`, `a36b467`, `27042f9`, `c8cd14d`, `0cf8e6a` |

---

## 2. V5.27 Research Question

**Can rescue probability be calibrated from the complete validated C3 gain chain?**

Answer: **Yes, but not as a full-chain continuous model.** The complete chain is mechanistically valid but not the strongest practical predictor. The best calibrated model is a frozen two-stratum risk table based on c3OmegaShift > 0.1.

---

## 3. Suite Summaries

### FCP — Protocol

3 tests. Defined the V5.27 protocol: freeze M3++, use only validated chain variables,
no retuning, no new variables, no physical interpretation. All claims limited to
RecoverFP rescue-probability calibration.

### FCE — Full-Chain Execution

5 tests (LongRunning). 361 profiles across 9 N values, 4 cohorts.

**Key findings:**

- No useful information accumulation from full-chain continuous models (FCE_01).
- c3OmegaShift is the strongest single predictor in the validated chain (FCE_01–02).
- Continuous c3OmegaShift calibration fails due to rarity and non-monotonicity (FCE_02).
- c3OmegaShift > 0.1 frozen binary is the best holdout model: 8.6% rescue, 2.6× enrichment (FCE_02).
- c3OmegaShift > 0.1 replicates across splits/cohorts; full-chain selector fails on unseen data (FCE_03).
- Two-stratum probability table calibrated from training: P_A=2.1%, P_B=17.1%, 8.3× enrichment (FCE_04).
- 13/13 stress splits pass directional test; null permutation p < 0.01% (FCE_05).

### FCA — Full-Chain Calibration Analysis

5 tests. Meta-analysis of FCE results.

**Key findings:**

- Two-stratum c3OmgS > 0.1 probability table is the preferred calibrated model.
- Full-chain predictive superiority is rejected.
- Mechanistic chain is retained as explanatory structure.
- c3OmegaShift is the minimal robust sufficient summary.
- Full-chain collapse (3/13 splits) is due to over-conditioning, not mechanism failure.

### FCI — Independent Probability Validation

1 test (LongRunning). 11 independent validation splits.

**Key findings:**

- 10/10 valid splits show P_B > P_A. Zero inversions.
- Pooled independent validation (n=1377):
  - Stratum A (c3OmgS ≤ 0.1): P = 0.0%, 95% CI [0.0%, 0.3%]
  - Stratum B (c3OmgS > 0.1): P = 9.6%, 95% CI [6.6%, 13.6%]
- Pooled CIs are statistically separated for the first time.
- P_A is uniformly 0.0% in independent holdouts (lower than training estimate of 2.1%).
- P_B is lower than training (9.6% vs 17.1%) but enrichment remains strong.

---

## 4. Supported Findings

1. The complete validated gain chain remains mechanistically supported:
   `d_tail → deltaD → deltaK → omegaPerK sign → c3OmegaShift → rescue`.

2. The full-chain model is NOT the best practical predictor.

3. c3OmegaShift > 0.1 is the strongest validated rescue-risk stratifier.

4. The frozen threshold c3OmgS > 0.1 survives stress testing (13/13 splits, null p<0.01%)
   and independent validation (10/10 splits, zero inversions).

5. P_B > P_A across all valid independent splits. Direction is unanimous.

6. The low-risk stratum (c3OmgS ≤ 0.1) is near-zero rescue probability
   (independent validation: 0.0% [0.0%–0.3%]).

7. Rescue remains probabilistic, not deterministic. Best P_B = 9.6%, CI [6.6%–13.6%].

8. c3OmegaShift is the minimal robust sufficient summary of the full chain
   for rescue-probability purposes.

---

## 5. Final Calibrated Table

The **independent validation pool** is used as the final conservative calibration:

| Stratum | Definition | P(rescue) | 95% CI |
|:--------|:-----------|----------:|:-------|
| A | c3OmegaShift ≤ 0.1 | **0.0%** | [0.0%, 0.3%] |
| B | c3OmegaShift > 0.1 | **9.6%** | [6.6%, 13.6%] |

**Interpretation:** c3OmegaShift > 0.1 defines a rescue-enriched risk stratum.
It does NOT guarantee rescue. Stratum A is near-zero rescue in tested data.

**Training vs Validation:**

| Estimate | P_A | P_B | Source |
|:---------|----:|----:|:-------|
| Training (FCE_04) | 2.1% | 17.1% | 180 profiles, potentially optimistic |
| **Final (FCI pooled)** | **0.0%** | **9.6%** | **1377 profiles, conservative** |

Direction and enrichment survive validation. Training P_B was likely inflated by
small-sample optimism. Training P_A was likely inflated by 3 rescues in 145 profiles.
Use independent validation estimates as the final calibration.

---

## 6. Final V5.27 Model

The preferred V5.27 rescue-probability model is a **frozen two-stratum risk table**
based on `c3OmegaShift > 0.1`.

- The complete chain remains the mechanistic explanation.
- The c3OmegaShift threshold is the minimal robust predictive summary.
- P_A = 0.0% [0.0%–0.3%], P_B = 9.6% [6.6%–13.6%].
- Enrichment ≈ 5–6× over baseline (20+× between strata).

---

## 7. Weakened Hypotheses

The following were active at V5.27 start but are now weakened or rejected:

- Full-chain continuous calibration (FCE_01–02: fails)
- Full-chain predictive superiority (FCE_03–05: collapsed on unseen data)
- Deterministic rescue threshold (V5.26: already rejected; FCE confirms)
- High-confidence continuous rescue probability (FCE_02: fails)
- Movement magnitude as predictor (V5.26: already rejected)
- Training P_B = 17.1% as final calibration (FCI: independent estimate is 9.6%)

---

## 8. Retained from Pre-V5.27

The following V5.26 and earlier findings are unchanged:

- M3++ is the preferred adaptive model (V5.17–V5.18)
- N=50–64 is inaccessible/rescue-immune under tested operator classes (V5.21)
- N=65–79 is adaptive-active; N=72 is peak (V5.19)
- C3 gain chain decomposition is valid (V5.23)
- omegaPerK sign rule is valid enrichment but not sufficient (V5.24–V5.25)
- Rescue conversion is probabilistic (V5.26)

---

## 9. Not Claimed Boundaries

V5.27 does NOT claim:

- Deterministic rescue prediction
- Universal adaptive control
- Physical interpretation of any kind
- Causal sufficiency of c3OmegaShift
- Full continuous probability calibration
- Validity outside tested N, cohorts, operators, and M3++ regime
- Proof that Stratum A can never rescue under future operators
- Full-chain predictive superiority
- Modified M3++
- Retuned thresholds or new variables

---

## 10. Final V5.27 Conclusion

V5.27 demonstrates that the complete C3 gain chain is mechanistically valid but
not the strongest practical probability model. The robust practical calibration
is a frozen two-stratum risk table based on c3OmegaShift > 0.1.

Independent validation confirms that the high-c3OmegaShift stratum is significantly
rescue-enriched (P_B = 9.6% [6.6%–13.6%]) while the low stratum is near-zero in
the tested data (P_A = 0.0% [0.0%–0.3%]).

Therefore V5.27 converts the C3 gain mechanism into a **conservative probabilistic
rescue-risk model** — not a deterministic rescue predictor. The model identifies
a rescue-enriched risk stratum; it does not guarantee rescue.

---

## 11. Recommended V5.28

**Branch:** `feature/v5.28-rescue-risk-stratum-and-control-policy`

**Central question:** Can the validated c3OmegaShift risk stratum be used to
define a safe adaptive control policy?

**Purpose:** Move from probability calibration to policy evaluation.

**Planned suites:**

| Suite | Name | Purpose |
|:------|:-----|:--------|
| RSP | Risk-Stratum Policy Protocol | Define policy gating rules |
| RSE | Risk-Stratum Execution | Test stratum-based policies |
| RSA | Policy Analysis | Analyze policy performance |
| RSI | Policy Intervention Audit | Verify safety and efficiency |
| RSS | Final Synthesis | Conclude V5.28 |

**Core questions:**

1. Should C3 be applied only when c3OmegaShift is expected/observed > 0.1?
2. Can the risk stratum reduce wasted interventions?
3. Can it reduce false-positive adaptive actions?
4. Does policy gating preserve zero damage?
5. Does risk-stratum policy improve practical rescue efficiency?
6. Is c3OmegaShift observed only after C3, and if so, can it guide second-stage
   policy rather than first-stage selection?

---

## 12. Suite Reference

| Suite | File | Tests | Status |
|:------|:-----|------:|:------:|
| FCP | `V5_27_FullChainRescueCalibrationProtocol_Tests.cs` | 3 | COMPLETE |
| FCE | `V5_27_FullChainExecution_Tests.cs` | 5 | COMPLETE |
| FCA | `V5_27_FullChainCalibrationAnalysis_Tests.cs` | 5 | COMPLETE |
| FCI | `V5_27_ProbabilityValidationAudit_Tests.cs` | 1 | COMPLETE |
| **Total** | | **14** | **0 failed** |

---

*Generated 2026-07-19. This is the authoritative V5.27 final synthesis.*
