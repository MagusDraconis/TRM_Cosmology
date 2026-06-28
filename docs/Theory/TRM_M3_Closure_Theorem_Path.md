# TRM \(m=3\) Closure Theorem Path (Gap 2)

## Scope

This document upgrades the Gap-2 track from "derive-or-falsify support" to a theorem-path structure.
It is claim-safe by design: current status is a strongly constrained theorem path, but not theorem-level proof.

---

## 1. Current tested evidence

From `CollectiveModeLockingTests.cs` (RBF01–RBF23) and the related theory notes:

- **RBF01–RBF05:** rational-band and closure-family structure is operationally supported; \(q\Omega-p=0\) and \(\Omega=(q+3)/q\) reproduce the observed bridge band.
- **RBF06–RBF10:** across occupancy/cost compromise and score-weight ablations, \(m=3\) is robustly competitive and typically leading.
- **RBF11–RBF12:** with explicit phase + direction + action/tick constraints, \(m=3\) is the minimal satisfying mode in resolved threshold cases.
- **RBF13:** constraint-removal ablation identifies action/tick as the key discriminator; without it, minimal mode drops to \(m=2\).
- **RBF14:** competing bands show a lock-vs-EL tradeoff, not blanket dominance by one window.
- **RBF15:** all constraints give unique \(m=3\); removing action/tick yields non-uniqueness and minimal \(m=2\).
- **RBF16:** unique \(m=3\) persists over a connected threshold region including the baseline point.
- **RBF17:** action/tick discriminator is derived from a coarse-grained microscopic action proxy, not only imposed as an external threshold.
- **RBF18:** derived action/tick-based unique \(m=3\) selection persists across tested solver-step families.
- **RBF19:** failure-by-family exclusion is explicit (\(m=2\) fails action/tick, \(m=4\) fails phase/closure, \(m=1,5\) fail direction/band).
- **RBF20:** operational-artifact audit over candidate range, q-window, normalization, occupancy gates, threshold formation, and tie-breaking keeps \(m=3\) in all resolved scenarios.
- **RBF21:** under one minimal three-constraint rule (phase, direction/band, derived action/tick), \(m=3\) is selected as unique minimal admissible mode with explicit failure reasons for neighboring families.
- **RBF22:** this minimal-model selection remains stable under bounded q-window and threshold perturbations (shared rule, no per-family retuning).
- **RBF23:** the action/tick discriminator is derived from a microscopic phase-lattice energy proxy (weighted order-defect, closure-defect, and transport-defect density), and \(m=3\) remains unique minimal in the same three-constraint closure model.

Status label:

> Gap 2 now has a strongly constrained theorem path for \(m=3\): RBF16–RBF23 reduce operational-artifact risk, strengthen structural exclusion logic, add bounded perturbation stability of the minimal closure model, and derive the action/tick discriminator from a microscopic phase-lattice energy proxy. It is still not a theorem-level microscopic proof.

---

## 2. Closure family \(q\Omega - p = 0\)

Operational closure is written as
\[
q\Omega - p = 0,\qquad p,q\in\mathbb Z,\ q>0.
\]

Interpretation:

1. phase locking is represented as integer closure compatibility,
2. rational candidates are enumerated structurally rather than by free continuous tuning,
3. closure defects can be indexed by integer offsets.

This is currently an operational formalization validated by RBF behavior, not yet a microscopic theorem.

---

## 3. \(p = q + m\)

Reparameterization gives
\[
p=q+m \;\Rightarrow\; \Omega=\frac{q+m}{q}=1+\frac{m}{q}.
\]

Consequences:

1. \(m\) is a discrete closure-order index (effective defect order),
2. \(q\) scans denominator-scale/lattice-like resolution,
3. each \(m\)-family defines a structured candidate manifold instead of isolated ratios.

In tested windows (\(q\) ranges used in RBF06–RBF20), the \(m=3\) family maps into the observed bridge-relevant band with strong support.

---

## 4. Why \(m=3\) is selected only with phase + direction + action/tick

The current selection stack is:

1. **Phase closure** (closure quality threshold),
2. **Direction closure** (bridge-band occupancy),
3. **Action/tick closure** (occupancy plus EL/action cost threshold).

Observed in RBF11, RBF12, RBF15, and reinforced by RBF19:

- \(m=1\): phase-only admissible, fails higher stack;
- \(m=2\): phase+direction admissible, fails action/tick;
- \(m=3\): first mode satisfying all three together; unique in RBF15 full-stack selection.

Therefore, current \(m=3\) selection is a **joint-constraint result**, not a single-metric optimum claim.

---

## 5. Why removing action/tick collapses to \(m=2\)

RBF13 and RBF15 (with consistent exclusion logic in RBF19) show:

1. removing phase alone does not displace minimal \(m=3\),
2. removing direction alone does not displace minimal \(m=3\),
3. removing action/tick restores multi-mode admissibility and minimal \(m=2\).

Interpretation:

- action/tick currently provides the decisive closure discriminator that prevents lower-order under-constrained selection,
- without it, the system does not support unique \(m=3\) selection.

This is exactly why Gap 2 remains open at theorem level: uniqueness still depends on the operational action/tick criterion definition.

---

## 6. What would count as theorem-level proof

A theorem-level closure for \(m=3\) must provide all of the following:

1. **Microscopic derivation:** derive phase, direction, and action/tick constraints from TQM lattice dynamics (not threshold post-selection).
2. **Uniqueness theorem:** prove \(m=3\) is necessary (or strictly bounded as unique minimal admissible mode) over a clearly defined model class.
3. **Parameter-independence:** show the result is invariant under admissible threshold/weight/solver families, not just one calibration region.
4. **Competing-family exclusion:** prove neighboring modes (\(m=2,4\), etc.) cannot satisfy the same full constraint set without violating derived axioms.
5. **Domain of validity:** explicit assumptions and boundary conditions where theorem holds and where it does not.

Until these are met, status remains: strongly constrained theorem path, not theorem-level proof.

---

## 7. Falsification criteria

The current theorem path is falsified if any of the following occurs:

1. a physically admissible variant of the full constraint set selects \(m\neq3\) as minimal/unique in the same weak-field regime;
2. uniqueness of \(m=3\) vanishes under non-ad-hoc threshold/weight continuation that should preserve the same physics;
3. a competing \(m\)-family reproduces closure quality, band occupancy, and action/tick consistency without structural penalty;
4. microscopic derivation yields a different closure-order index or invalidates one of the three constraints;
5. action/tick discriminator cannot be derived from first principles and remains purely procedural.

---

## 8. Proposed next tests

Status note:

1. RBF16–RBF20 are implemented and passing.
2. The next step is microscopic theorem closure work, not additional operational threshold variants.

Current claim-safe summary:

> \(m=3\) is strongly supported as a structurally robust closure-order candidate under a strongly constrained theorem path.  
> It is not yet a theorem-level microscopic proof.

---

## 9. Minimal theory statement for RBF21

RBF21 should test the following claim-safe theorem-path statement:

> Under a minimal three-constraint closure model (phase closure, direction/band closure, and derived action/tick closure), the \(m=3\) family is the first admissible mode that satisfies all constraints jointly, while neighboring \(m\)-families fail at least one structural constraint.

Operational form for the test:

1. use closure family \(\Omega=(q+m)/q\) with fixed admissible \(q\)-window;
2. define one explicit three-constraint decision rule (no per-family retuning);
3. evaluate \(m\in\{1,2,3,4,5\}\) under the same rule;
4. require \(m=3\) to be selected as unique minimal satisfying mode in the baseline model;
5. report explicit failure reasons for non-selected families.

RBF21 name anchor:

- `RBF21_M3_Should_Follow_From_MinimalThreeConstraintClosureModel`

Claim boundary:

> Passing RBF21 upgrades the structural derivation path for \(m=3\) under a minimal closure model, but still does not constitute theorem-level microscopic closure.

RBF22 extension:

- `RBF22_M3_MinimalClosureModel_Should_Remain_Stable_Under_QWindowAndThresholdPerturbation`

Claim boundary:

> RBF22 strengthens robustness of the minimal closure-model path for \(m=3\) under bounded perturbations, but does not establish theorem-level microscopic closure.

RBF23 extension:

- `RBF23_ActionTickDiscriminator_Should_Emerge_From_PhaseLatticeEnergy`

Claim boundary:

> RBF23 tests whether the action/tick discriminator used in the \(m=3\) closure path can be obtained from a microscopic phase-lattice energy proxy rather than imposed operationally; it does not establish theorem-level microscopic closure.
