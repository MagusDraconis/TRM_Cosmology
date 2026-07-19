# V5.29 Risk-Stratum Boundary and Threshold Robustness — Protocol

**Version:** 1.0 | **Date:** 2026-07-19

## Frozen Model

M3++ unchanged. c3OmgS threshold 0.1 frozen. Stop-Low policy from V5.28.

## Forbidden Actions

No M3++ modification. No threshold retuning. No new variables.
No correction classes. No post-hoc optimization.

## Test Plan

1. Density scan: profile c3OmgS distribution near [0.0, 0.2]
2. Perturbation: add noise ±0.01, ±0.02 to c3OmgS
3. N-specific and cohort-specific boundary analysis
4. Safety margin quantification

## Acceptable Claims

SUPPORTED/CONDITIONAL: safety margin exists, boundary is stable, low/high separated.
NOT CLAIMED: universal invariance, physical interpretation.
