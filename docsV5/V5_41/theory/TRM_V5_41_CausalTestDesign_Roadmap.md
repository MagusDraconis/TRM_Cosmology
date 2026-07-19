# V5.41 Causal Test Design Under Attractor Absorption — Roadmap

**Version:** 1.0 | **Date:** 2026-07-19
**Status:** DRAFT

---

## 1. Background

V5.35–V5.40 have systematically tested causal closure of the C3 gain chain
and response-state diagnostics:

| Version | Approach | Result |
|:--------|:---------|:-------|
| V5.35 | Chain mechanism closure | Falsified — chain is trace, not causal |
| V5.36 | c3OmgS origin perturbation | Partial causal (later downgraded) |
| V5.37 | Omega proximity dose-response | Non-monotonic, causal claim downgraded |
| V5.38 | RII lambda1 intervention | No causal signal — attractor absorbs |
| V5.39 | Absorption characterization | Model C — Omega restoration dominates |
| V5.40 | Attractor-compatible perturbations | All absorbed. Weak signals = artifacts |

**The pattern is clear:** every perturbation-based approach to causal closure
has failed. The attractor absorbs all tested perturbation patterns uniformly.

## 2. Key Insight

The failure of perturbation-based causal testing may not be a methodological
limitation — it may be a **structural feature** of the RecoverFP system.
If the attractor protects its internal state coordinates against external
perturbation, then perturbation is not a valid causal test method.

## 3. Candidate Alternative Approaches

| Approach | Description | Viability |
|:---------|:------------|:----------|
| Natural variation | Use existing profile-to-profile variance as natural experiment | Requires identifiability analysis |
| Mediation analysis | Test whether lambda1 mediates c3OmgS through intermediate variables | Requires temporal ordering |
| Invariance testing | Test whether c3OmgS-lambda1 relationship is invariant across conditions | May reveal structural constraints |
| Counterfactual trace | Trace what WOULD happen if lambda1 were different using propensity matching | Observational, not interventional |
| Granger-style precedence | Test whether lambda1 changes precede c3OmgS changes temporally | Requires time-series data |
| Instrumental variable | Find variable that affects lambda1 but not c3OmgS directly | Difficult with strong attractor |
| Acceptance | Conclude causal closure may be inaccessible; focus on predictive validity | Pragmatic |

## 4. Key Distinction

**Predictive validity ≠ causal closure.**

Stop-Low is predictively valid (preserves all rescues, zero damage) without
causal closure. The question is whether causal understanding adds operational
value beyond predictive stability.

## 5. Decision Criteria

For any candidate approach:
1. Is it valid under strong attractor restoration?
2. Does it require variables we don't have?
3. Does it require perturbation we can't perform?
4. Would a positive result change operational practice?

## 6. Conservative Expectation

V5.41 may conclude that causal closure is structurally inaccessible under
current constraints, and that predictive validity (Stop-Low) is sufficient
for operational purposes. V6 remains NOT READY.

---

*Generated 2026-07-19. V5.41 theory roadmap.*
