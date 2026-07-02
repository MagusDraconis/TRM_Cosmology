# TRM \(m=3\) Closure Derivation Attempt

## Scope

Goal: formalize a first-principles derivation path for the closure family
\[
\Omega=\frac{q+3}{q}
\]
and the current \(m=3\) minimal closure/balance selection, without claiming theorem closure.

---

## 1. Phase closure family \(q\Omega-p=0\)

Operational closure relation:
\[
q\Omega-p=0
\]
with integer pair \((p,q)\) defining rational cadence candidates.

Interpretation:

1. phase-closure defects are naturally tracked in integer arithmetic,  
2. rational windows become explicit candidate sets for collective locking analyses.

**Status:** derived (operational formalization) + tested support (RBF01/RBF05).

---

## 2. \(p=q+m\)

Reparameterize:
\[
p=q+m \quad\Rightarrow\quad \Omega=\frac{q+m}{q}.
\]

This yields closure families indexed by \(m\), with \(q\) scanning lattice-like denominators.

**Status:** derived (family definition) + tested usage across RBF06–RBF15.

---

## 3. Meaning of \(m\) as closure defect/order

Working interpretation:

1. \(m\) measures the discrete closure offset/defect between synchronized cycle count \(p\) and baseline denominator \(q\),  
2. larger \(m\) generally increases closure offset and may alter occupancy-cost tradeoff,  
3. \(m\) is currently an effective closure-order index, not yet a microscopic topological invariant.

**Status:** hypothesis-supported; not derived yet at theorem level.

---

## 4. Why \(m=3\) is selected under phase + direction + action/tick constraints

Current tested constraint stack (RBF11/RBF12/RBF15 operational form):

1. **phase closure constraint** (closure quality threshold),  
2. **direction/transport closure constraint** (bridge-band occupancy),  
3. **action/tick consistency constraint** (occupancy + EL/action cost threshold).

Empirical outcome in current windows:

1. the explicit \(m=3\) family is
   \[
   \Omega=\frac{q+3}{q},
   \]
2. \(m=3\) satisfies all three constraints,  
3. \(m<3\) typically fails at least one of the stricter combined constraints,  
4. under full-stack constraints, \(m=3\) is uniquely selected in RBF15.

**Status:** tested effective selection; hypothesis-supported first-principles candidate.

---

## 5. Why removing action/tick collapses selection toward \(m=2\)

From RBF13 and RBF15:

1. removing phase alone: minimal stays \(m=3\),  
2. removing direction alone: minimal stays \(m=3\),  
3. removing action/tick: satisfying set becomes non-unique and minimal collapses to \(m=2\).

Interpretation:

1. action/tick consistency currently acts as the key stabilizing discriminator for \(m=3\),  
2. without that channel, closure selection weakens and lower-order modes become admissible.

**Status:** tested (operational ablation result).

---

## 6. What is proven by tests

1. Rational closure family \(\Omega=(q+m)/q\) is structurally relevant to the observed bridge band (RBF05).  
2. \(m=3\) is robustly competitive in occupancy-cost compromise analyses (RBF06–RBF10).  
3. \(m=3\) is minimal satisfying mode in the tested three-constraint formalization and robust under threshold ablation (RBF11–RBF12).  
4. Constraint-ablation behavior is non-uniform and specifically identifies action/tick as key for unique \(m=3\) selection (RBF13, RBF15).  
5. Neighboring bands reveal locking-vs-EL tradeoff rather than unique single-band dominance (RBF14).

Evidence classification:

- **tested:** yes  
- **derived:** operational formalization only  
- **calibrated:** thresholds and score structures are effective/test-operational  
- **hypothesis-supported:** yes (candidate first-principles closure index)

---

## 7. What is not yet a theorem

1. No microscopic proof yet that \(m=3\) is uniquely implied by TQM phase/lattice dynamics.  
2. No fully parameter-independent uniqueness theorem across all admissible thresholds/windows.  
3. No completed derivation connecting closure constraints directly to a fundamental variational/topological principle.

Current boundary statement:

> \(m=3\) is currently the best-supported minimal effective closure/balance index under the tested full constraint set, but it is not yet a proven fundamental theorem.

Status:

- **not derived yet:** theorem-level uniqueness  
- **limitation:** dependence on operational threshold families remains explicit
