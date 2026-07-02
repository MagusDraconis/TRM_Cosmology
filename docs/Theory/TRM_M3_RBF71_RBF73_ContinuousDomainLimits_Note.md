# TRM M3 RBF71–RBF73 Continuous Domain Limits Note

## Scope

This note summarizes RBF71–RBF73, which transition the `m=3` selection rule from discrete bounds to continuous threshold limits, demonstrating that operational parameters are anchored to exact physical/structural defect limits.

---

## 1) RBF71: Phase Threshold Convergence

`RBF71_PhaseThreshold_Should_Converge_To_IntegerClosureDefectLimit` scans the phase tolerance continuously around the exact `m=3` integer closure-defect limit.
It confirms that the operational threshold converges cleanly at this structural boundary, meaning the phase admission is not an arbitrary cut-off but dictated directly by the lattice geometry constraint.

---

## 2) RBF72: Action Tolerance Convergence

`RBF72_ActionTolerance_Should_Converge_To_StationarityResidualLimit` evaluates action tolerance limits continuously.
It demonstrates that the stationarity residual exactly defines the tolerance boundary. Modulating the tolerance cleanly tracks where `m=3` is admissible, where it abstains, and where competitor modes enter the regime, verifying the action threshold as a physical limit.

---

## 3) RBF73: Continuous Validity Envelope

`RBF73_FormalSelectionRule_Should_Define_ContinuousValidityEnvelope` combines phase, action, and energy-margin scans to map a multidimensional continuous validity envelope.
It explicitly categorizes outside-domain failures (e.g., phase-limit, action-limit, margin-limit, mixed-limit) and verifies that the formal selection rule consistently fails gracefully outside this defined continuous envelope rather than generating false positives.

---

## Updated status

Current reviewer-safe status:

> m=3 is a bounded shared-functional selection candidate with structural-invariant and continuous domain-limit scaffolding toward analytical theorem readiness.

---

## Remaining gap

Primary remaining gaps:
- Transition from numerical scaffold limits to purely analytical continuous theorem proofs.
- Formal proof of necessary/sufficient closure conditions without relying on bounded operational search spaces.

---

## Claim boundaries

- diagnostic/candidate only
- not theorem-level proof
- not full first-principles closure
- not universal m=3 selection
- not GR replacement
- no numerology claim
