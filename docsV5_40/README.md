# V5.40 — Causal Closure and Attractor Topology

**Version:** INITIALIZED | **Date:** 2026-07-19
**Branch:** `feature/v5.40-causal-closure-and-attractor-topology`
**Base:** V5.39 COMPLETE

---

## Purpose

V5.39 confirmed the attractor resists simple state perturbations. The diagnostic
hierarchy (lambda1 > rebMag > omDist) is robust but diagnostically-driven, not
causally controllable. V5.40 asks: if the attractor resists perturbation, what
approaches can test causal closure without fighting attractor restoration?

## Central Question

**Can perturbation strategies work with (not against) attractor dynamics to test**
**causal closure?**

## Planned Suites

| Suite | Name | Purpose |
|:------|:-----|:--------|
| CTP | Causal Testing Protocol | Freeze constraints, define allowed perturbations |
| CTE | Causal Testing Execution | Test multi-stage, directional, and topology-aware perturbations |
| CTA | Causal Testing Analysis | Analyze whether any approach improves causal closure |
| CTI | Causal Testing Audit | Independent audit of results |
| CTS | Final Synthesis | V5.40 closure and V5.41 recommendation |

## Frozen Policy

- Do not modify M3++.
- Do not retune c3OmegaShift threshold (0.1 frozen).
- Do not modify Stop-Low.
- Do not add variables or correction classes.
- Do not claim physical interpretation.
- Do not attempt V6 derivations (length, space, velocity, c).

## Expected Caution

V5.40 may or may not improve causal closure. The attractor's resistance may be
fundamental. V6 remains NOT READY regardless of V5.40 outcome.

## References

- `docsV5_39/TRM_V5_39_Final_Synthesis.md` — V5.39 absorption model
- `docsV5_38/TRM_V5_38_Final_Synthesis.md` — diagnostic hierarchy
- `docs/TRM_Current_Frontier.md` — current project state
