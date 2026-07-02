# TRM M3 RBF53–RBF55 Domain-Validity Note

## Scope

This note summarizes RBF53–RBF55 as domain-of-validity diagnostics for the shared-functional m=3 path.

---

## 1) RBF53: explicit validity-boundary scan

`RBF53_SharedFunctional_Domain_Should_Have_ExplicitValidityBoundary` performs an explicit scan over q-support, phase tolerance, action tolerance, and shared functional component weights.

Diagnostic outcome:

- identifies where `m=3` remains admissible/unique,
- identifies boundary classes where selection becomes unresolved (`none`), falls back to `m=2`, or selects another mode,
- reports first transition cases and validity/boundary region counts.

---

## 2) RBF54: dominant-channel boundary classification

`RBF54_BoundaryFailures_Should_Be_Classified_ByDominantConstraintChannel` classifies boundary failures by dominant constraint channel:

1. phase-defect,
2. bridge-prior/qCore,
3. action-stationarity,
4. mixed.

Diagnostic outcome:

- reports channel counts,
- reports representative examples for each failure channel under one shared rule (no per-family retuning).

---

## 3) RBF55: solver-step and q-support boundary stability

`RBF55_DomainBoundary_Should_Be_Stable_UnderSolverAndQSupportVariants` repeats the domain scan under solver-step and q-support variants.

Diagnostic outcome:

- reports boundary drift relative to baseline,
- reports m=3-region size, unresolved cases, and fallback modes,
- enforces bounded near-baseline drift while allowing/reporting failures outside admissible regimes.

---

## 4) Updated status

Current reviewer-safe status:

> bounded shared-functional uniqueness candidate with explicit domain-of-validity diagnostics.

---

## 5) Remaining gap

Primary remaining gap:

> theorem-level necessity/uniqueness and formal proof of the admissible domain remain open.

---

## 6) Claim boundaries

- diagnostic/candidate only
- not theorem-level proof
- not full first-principles closure
- not universal m=3 selection
- not GR replacement
- no numerology claim

