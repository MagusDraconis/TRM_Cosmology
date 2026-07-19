# V5.28 Rescue Risk Stratum and Control Policy — Roadmap

**Version:** 1.0 | **Date:** 2026-07-19
**Branch:** `feature/v5.28-rescue-risk-stratum-and-control-policy`

## Background

V5.27 established c3OmegaShift > 0.1 as a validated rescue-enriched risk stratum:
- P(rescue | c3OmgS > 0.1) = 9.6% [6.6%–13.6%]
- P(rescue | c3OmgS ≤ 0.1) = 0.0% [0.0%–0.3%]

## V5.28 Question

Can the validated risk stratum define a safe adaptive control policy?

## Policy Design Space

1. **Pre-C3 gating:** Predict c3OmgS before C3. Only apply C3 if predicted > 0.1.
2. **Post-C3 gating:** Apply C3 unconditionally. Stop if c3OmgS ≤ 0.1 after C3.
3. **Unconditional baseline:** Apply C3 unconditionally. Current M3++ behavior.

## Evaluation Metrics

- Intervention reduction vs unconditional
- False-positive reduction
- Zero-damage preservation
- Rescue efficiency gain
- Policy robustness across N, cohorts, splits

## Constraints

M3++ frozen. c3OmgS threshold 0.1 frozen. No new variables. No physical interpretation.
