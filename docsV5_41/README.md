# V5.41 — Causal Test Design Under Attractor Absorption

**Version:** INITIALIZED | **Date:** 2026-07-19
**Branch:** `feature/v5.41-causal-test-design-under-attractor-absorption`
**Base:** V5.40 COMPLETE

---

## Purpose

V5.40 confirmed that all perturbation families (direct, aligned, combined, mismatched)
are absorbed before c3OmegaShift. Attractor-aligned perturbations do not survive
better. Weak directional signals are baseline-state artifacts. Causal closure remains
BLOCKED.

V5.41 pivots from perturbation attempts to causal-test methodology: if simple
perturbation cannot establish causality, what test designs remain valid under
strong attractor restoration?

## Central Question

**If direct and attractor-aligned perturbations are absorbed, what causal test**
**designs remain valid under strong attractor restoration?**

## Planned Suites

| Suite | Name | Purpose |
|:------|:-----|:--------|
| TDP | Test Design Protocol | Freeze constraints, define causal test taxonomy |
| TDE | Causal Test Design Execution | Evaluate candidate test designs against attractor absorption |
| TDA | Identifiability Analysis | Determine which causal questions are answerable |
| TDI | Methodology Audit | Independent review of test design validity |
| TDS | Final Synthesis | V5.41 closure and V5.42 recommendation |

## Core Questions

1. Which causal tests are invalidated by attractor absorption?
2. Can causal influence be tested using natural variation instead of perturbation?
3. Can causal closure be approached through invariance, mediation, or counterfactual trace?
4. What evidence would establish causality under strong restoration?
5. Are current variables sufficient for causal testing?
6. Does Stop-Low require causal closure, or only predictive stability?
7. Does V6 remain NOT READY?

## Frozen Policy

- Do not modify M3++.
- Do not retune c3OmegaShift threshold (0.1 frozen).
- Do not modify Stop-Low.
- Do not add variables or correction classes.
- Do not claim physical interpretation.
- Do not attempt V6 derivations (length, space, velocity, c).

## Expected Caution

V5.41 may conclude that causal closure is fundamentally inaccessible under current
variable/operator constraints. This is an acceptable outcome — predictive validity
(Stop-Low) may not require causal closure. V6 remains NOT READY regardless.

## References

- `docsV5_40/TRM_V5_40_Final_Synthesis.md` — V5.40 causal closure blocked
- `docsV5_39/TRM_V5_39_Final_Synthesis.md` — V5.39 attractor absorption
- `docs/TRM_Current_Frontier.md` — current project state
