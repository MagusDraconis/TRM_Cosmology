# TRM M3 RBF74–RBF76 Analytical Constraint Boundaries Note

## Scope

This note summarizes RBF74–RBF76, transitioning the scaffolding from numerical continuous-envelope diagnostics toward formal analytical constraint boundary conditions.

---

## 1) RBF74: Analytical Phase Boundary

`RBF74_PhaseBoundary_Should_Be_Expressible_AsAnalyticalClosureCondition` expresses phase admissibility as an exact analytical condition: `|qΩ - p| / targetShift <= ε_phase`.
It validates that the numerical phase-threshold behavior is mathematically equivalent to this analytical closure condition. No mismatches were found in the tested diagnostics.

---

## 2) RBF75: Analytical Action Stationarity Boundary

`RBF75_ActionBoundary_Should_Be_Expressible_AsAnalyticalStationarityCondition` expresses action admissibility as a stationarity residual condition: `δE_action <= ε_action`.
It confirms that the operational action tolerance accurately corresponds to the lattice-energy stationarity residual boundary.

---

## 3) RBF76: Necessary and Sufficient Diagnostic Conditions

`RBF76_FormalSelectionRule_Should_Be_Expressible_AsNecessarySufficientDiagnosticConditions` combines the phase closure condition, bridge/qCore prior condition, action stationarity condition, and energy-margin dominance condition.
It outputs a checklist of necessary and sufficient diagnostic conditions for `m=3` selection inside the tested domain, correctly marking empirical components as `STRUCTURAL-PASS` or `DIAGNOSTIC-PASS`, while theorem-level necessities are flagged as `PENDING-ANALYTICAL`.

---

## Updated status

Current reviewer-safe status:

> m=3 is a bounded shared-functional selection candidate with an analytical constraint-boundary scaffold.

---

## Remaining gap

Primary remaining gaps:
Formal proof of topological necessity, sufficiency, and domain closure remains pending analytical work.

---

## Claim boundaries

- diagnostic/candidate only
- not theorem-level proof
- not full first-principles closure
- not universal m=3 selection
- not GR replacement
- no numerology claim
