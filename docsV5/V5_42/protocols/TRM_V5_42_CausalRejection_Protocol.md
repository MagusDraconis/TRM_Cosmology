# V5.42 Causal Rejection Protocol

**Version:** 1.0 | **Date:** 2026-07-19
**Suite:** CRP
**Status:** DRAFT

---

## 1. Purpose

Define the methodology for non-perturbative causal rejection using counterfactual
trace and natural variation.

## 2. Frozen Policy

- M3++ model: FROZEN
- c3OmegaShift threshold 0.1: FROZEN
- Stop-Low policy: FROZEN
- No new variables.
- No new correction classes.
- No perturbation-based testing.
- No V6 derivations.
- No physical interpretation.

## 3. Methodology

### Counterfactual Trace
- Match profiles by lambda1 band, N, cohort
- Compare c3OmgS outcomes across matched pairs
- Identify variables that differ between high-c3OmgS and low-c3OmgS matched profiles
- Variables that do NOT differ are rejected as causal drivers

### Natural Variation Rejection
- Stratify by variable terciles (lambda1, rebMag, omDist)
- Test whether c3OmgS separation persists across strata
- If separation fails in any stratum, the variable is not causally robust

## 4. Allowed Variables

- c3OmegaShift, Omega T1, Omega T2, omDist
- lambda1, rebMagnitude, d_tail
- deltaD, deltaK, kSensitivity, omegaPerK
- strict persistence, rescue yes/no, damage yes/no
- N, cohort

## 5. Rejection Criteria

A claim is REJECTED if:
1. Matched profiles with different outcomes show no difference in the candidate variable
2. Natural variation stratification eliminates the candidate relationship
3. The relationship reverses sign across N or cohorts
4. The relationship fails Stop-Low safety audit

## 6. Decision Gates

| Gate | Description | Criterion |
|:-----|:------------|:----------|
| A | Counterfactual matching validated | Matched pairs identified |
| B | Variable separation assessed | Outcome-associated variables identified |
| C | Rejection evidence collected | Claims rejected with counterfactual evidence |
| D | Natural variation audit passed | Stratification does not reverse findings |
| E | Stop-Low safety preserved | No policy violation |
| F | V6 not ready | Confirmed |

---

*Generated 2026-07-19. V5.42 protocol draft.*
