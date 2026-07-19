# V5.42 — Counterfactual Trace and Natural Variation Causal Rejection

**Version:** INITIALIZED | **Date:** 2026-07-19
**Branch:** `feature/v5.42-counterfactual-trace-and-natural-variation-causal-rejection`
**Base:** V5.41 COMPLETE

---

## Purpose

V5.41 established that perturbation-based testing is INVALIDATED under attractor
absorption, and that causal rejection is the strongest current methodology.
V5.42 applies this methodology using counterfactual trace and natural variation
to identify stronger causal rejection boundaries — without perturbation.

## Central Question

**Can counterfactual trace and natural variation identify stronger causal rejection**
**boundaries without relying on perturbation?**

## Planned Suites

| Suite | Name | Purpose |
|:------|:-----|:--------|
| CRP | Causal Rejection Protocol | Freeze constraints, define rejection methodology |
| CRE | Counterfactual Trace Execution | Matched-profile comparison without perturbation |
| CRA | Natural Variation and Rejection Analysis | Identify which causal claims survive rejection |
| CRI | Independent Rejection Audit | Review rejection evidence |
| CRS | Final Synthesis | V5.42 closure and V5.43 recommendation |

## Core Questions

1. Which causal claims can be rejected using counterfactual trace?
2. Which response-state variables remain diagnostic under matched-profile comparison?
3. Can matched profiles reveal why c3OmgS differs without perturbation?
4. Does natural variation strengthen or weaken the diagnostic hierarchy?
5. Can causal closure be narrowed by eliminating impossible causal paths?
6. Does Stop-Low remain operationally sufficient?
7. Does V6 remain NOT READY?

## Frozen Policy

- Do not modify M3++.
- Do not retune c3OmegaShift threshold (0.1 frozen).
- Do not modify Stop-Low.
- Do not add variables or correction classes.
- Do not claim physical interpretation.
- Do not attempt V6 derivations (length, space, velocity, c).

## Expected Caution

V5.42 may not achieve causal closure — it may only narrow the rejection boundary.
This is acceptable. Eliminating impossible causal paths is progress even without
positive causal identification. V6 remains NOT READY regardless.

## References

- `docsV5_41/TRM_V5_41_Final_Synthesis.md` — V5.41 causal test methodology
- `docsV5_40/TRM_V5_40_Final_Synthesis.md` — V5.40 causal closure blocked
- `docs/TRM_Current_Frontier.md` — current project state
