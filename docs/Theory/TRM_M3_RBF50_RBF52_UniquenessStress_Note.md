# TRM M3 RBF50–RBF52 Uniqueness-Stress Note

## Scope

This note summarizes RBF50–RBF52 as bounded uniqueness-stress diagnostics for the shared functional path.

---

## RBF50: bounded perturbation stability

`RBF50_SharedFunctional_Should_Select_M3_Under_BoundedStructuralPerturbations` perturbs phase-defect, bridge-prior, and action-stationarity component weights within bounded ranges under one shared rule.

Diagnostic result:

> m=3 remains locally stable near baseline under bounded structural perturbations, while bounded failure cases are explicitly reported.

---

## RBF51: non-uniqueness under relaxed assumptions

`RBF51_SharedFunctional_Should_Expose_NonUniqueness_When_CoreAssumptionsAreRelaxed` relaxes one core assumption at a time:

1. phase defect,
2. bridge prior,
3. action stationarity.

Diagnostic result:

> uniqueness weakens under assumption relaxations, and assumption sensitivity is explicitly reported.

---

## RBF52: bounded counterexample search under full shared rule

`RBF52_CounterexampleSearch_Should_Not_Find_CompetingMode_UnderFullSharedRule` runs a deterministic bounded search over q-support, shared weights, and phase/action tolerances with no per-family retuning.

Diagnostic result:

> no `m != 3` admissible counterexample was found in the tested bounded full-rule search domain.

---

## Updated status

Current reviewer-safe status:

> m=3 is supported as a bounded shared-functional uniqueness candidate within the tested admissible rule family.

---

## Claim boundaries

- diagnostic/candidate only
- not theorem-level proof
- not full first-principles closure
- not universal m=3 selection
- not GR replacement
- no numerology claim

