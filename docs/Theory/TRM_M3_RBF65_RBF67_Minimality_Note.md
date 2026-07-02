# TRM M3 RBF65–RBF67 Minimality and Theorem Readiness Note

## Scope

This note summarizes RBF65–RBF67, shifting focus from proving that the selection rule works to demonstrating *how* it is structured: minimal, not overparameterized, and prepared for a formal theorem derivation.

---

## 1) RBF65: Minimality and Non-Overparameterization

`RBF65_SelectionRule_Should_Be_Minimal_NotOverparameterized` uses component ablation to provide diagnostic evidence that no constraint in the shared functional (Phase, Bridge, Action) is superfluous.
When any single component is ablated (weight set to `0.0`, tolerance opened), `m=3` loses its unique margin dominance. This supports the interpretation that the functional is minimal and not artificially overparameterized.

---

## 2) RBF66: Rejection of Alternative Functional Forms

`RBF66_SelectionRule_Should_RejectAlternativeFunctionalForms` evaluates the baseline constraint values under alternative combination forms (e.g., L2 squared norms, multiplicative combinations). 
The diagnostic verifies that alternative forms may preserve `m=3` or abstain/fail gracefully, but they **must not confidently select a false positive** (m≠3) within the same admissible domain.

---

## 3) RBF67: Formal Theorem Readiness Checklist

`RBF67_SelectionRule_Should_ReportFormalTheoremReadinessChecklist` bridges the gap between empirical candidate tests and formal analytical proof paths by categorizing conditions into explicit statuses:
- `PASS` / `DIAGNOSTIC-PASS` for empirically satisfied preconditions.
- `PENDING-ANALYTICAL` for formal proofs that must be derived mathematically.
- `FAIL` for blocked paths.

This explicitly delineates the boundary between our numerical scaffold and the required analytical theorem.

---

## Claim boundaries

- diagnostic/candidate only
- not theorem-level proof
- not full first-principles closure
- not universal m=3 selection
- not GR replacement
- no numerology claim
