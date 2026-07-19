# V5.41 Causal Test Design Protocol

**Version:** 1.0 | **Date:** 2026-07-19
**Suite:** TDP
**Status:** DRAFT

---

## 1. Purpose

Define the taxonomy of causal test designs, evaluate which are invalidated
by attractor absorption, and freeze constraints for V5.41 methodology.

## 2. Frozen Policy

- M3++ model: FROZEN
- c3OmegaShift threshold 0.1: FROZEN
- Stop-Low policy: FROZEN
- No new variables.
- No new correction classes.
- No V6 derivations (length, space, velocity, c).
- No physical interpretation.

## 3. Causal Test Taxonomy (Draft)

| Class | Name | Perturbation Required? | Valid Under Absorption? |
|:------|:-----|:----------------------:|:-----------------------:|
| A | Direct intervention | Yes | NO (V5.38-V5.40) |
| B | Natural experiment | No | Potentially |
| C | Mediation analysis | No | Requires temporal ordering |
| D | Invariance testing | No | Potentially |
| E | Counterfactual matching | No | Observational only |
| F | Granger precedence | No | Requires time series |
| G | Instrumental variable | Indirect | Difficult |
| H | Acceptance | No | Pragmatic |

## 4. Validity Criteria

For any test to be valid under attractor absorption:
1. It must not require perturbing K-state or lambda1 directly
2. It must work with (not against) the attractor's restoration dynamics
3. It must use existing variables only
4. It must distinguish causal from diagnostic relationships

## 5. Allowed Variables

- c3OmegaShift, Omega T1, Omega T2, omDist
- lambda1, rebMagnitude, d_tail
- deltaD, deltaK, kSensitivity, omegaPerK
- strict persistence, rescue yes/no, damage yes/no
- N, cohort

## 6. Decision Gates

| Gate | Description | Criterion |
|:-----|:------------|:----------|
| A | Test taxonomy defined | All classes categorized |
| B | Invalidated tests identified | Perturbation-based tests flagged |
| C | Candidate tests proposed | At least one non-perturbation approach |
| D | Validity criteria met | Candidate passes absorption constraints |
| E | Stop-Low independence confirmed | Predictive validity does not require causal closure |
| F | V6 not ready | Confirmed |

---

*Generated 2026-07-19. V5.41 protocol draft.*
