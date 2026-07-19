# V5.29 Risk-Stratum Boundary and Threshold Robustness — Roadmap

**Version:** 1.0 | **Date:** 2026-07-19
**Branch:** `feature/v5.29-risk-stratum-boundary-and-threshold-robustness`

## Background

V5.28 validated Stop-Low as a safe post-C3 continuation policy:
- All rescues in Stratum B (c3OmgS > 0.1)
- Zero rescues in Stratum A (c3OmgS ≤ 0.1)
- RSA near-miss audit: max c3OmgS in low stratum = 0.070

## V5.29 Question

How robust is the Stop-Low policy around the c3OmgS = 0.1 boundary?

## Boundary Tests

1. Density scan: profile c3OmgS distribution near [0.0, 0.2]
2. Perturbation test: add noise to c3OmgS, check policy reversal
3. N-specific boundary profiles
4. Cohort-specific boundary profiles
5. Safety margin quantification

## Constraints

M3++ frozen. Threshold 0.1 frozen. No retuning. No new variables.
