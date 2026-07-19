# V5.28 Rescue Risk Stratum and Control Policy — Protocol

**Version:** 1.0 | **Date:** 2026-07-19

## 1. Frozen Model

M3++ unchanged. c3OmegaShift > 0.1 risk stratum. P_A=0.0%, P_B=9.6%.

## 2. Policy Variants

- Policy 1: Pre-C3 gating (predict, then apply)
- Policy 2: Post-C3 gating (apply, measure, stop if ≤ 0.1)
- Policy 3: Unconditional baseline (current M3++)

## 3. Forbidden Actions

No M3++ modification. No threshold retuning. No new variables.
No correction classes. No post-hoc optimization. No holdout tuning.

## 4. Evaluation

For each policy: measure interventions, rescues, damage.
Compute efficiency vs baseline. Verify zero damage.
Audit split/cohort/N stability.

## 5. Acceptable Claims

SUPPORTED/CONDITIONAL: reduced interventions, preserved zero damage, improved efficiency.
NOT CLAIMED: universal control, physical interpretation.
