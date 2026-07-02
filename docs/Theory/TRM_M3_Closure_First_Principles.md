# TRM \(m=3\) Closure: First-Principles Formalization Track

## Scope

This document formalizes the current derive-or-falsify structure for the closure-index question:
\[
\text{Why is } m=3 \text{ selected as minimal closure mode?}
\]

It does **not** claim a completed uniqueness theorem yet.

---

## 1. Current empirical baseline

The current RBF chain supports:

1. closure-family candidate
\[
\Omega=\frac{q+m}{q},
\]
2. robust \(m=3\) competitiveness and balance behavior,
3. threshold-ablation stability (`RBF12`),
4. non-uniform constraint role (`RBF13`),
5. tradeoff structure vs neighboring bands (`RBF14`),
6. derive-or-falsify boundary (`RBF15`):
   - with all three constraints: unique \(m=3\),
   - without action/tick: non-unique and minimal collapse to \(m=2\).

---

## 2. Formal closure constraints (operational level)

For a closure candidate \((m,q)\) define:

1. **Phase closure constraint**
\[
C_{\text{phase}}(m,q):\quad \bar Q_{\text{closure}}(m,q)\ge \tau_{\text{phase}}
\]

2. **Direction/transport closure constraint**
\[
C_{\text{dir}}(m,q):\quad N_{\text{band}}(m,q)\ge \tau_{\text{dir}}
\]

3. **Action/tick consistency constraint**
\[
C_{\text{act}}(m,q):\quad
N_{\text{band}}(m,q)\ge \tau_{\text{act}}
\;\wedge\;
\mathcal C_{\text{EL}}(m,q)\le \tau_{\text{act-cost}}
\]

where \(\tau_{\text{phase}},\tau_{\text{dir}},\tau_{\text{act}},\tau_{\text{act-cost}}\) are explicit thresholds.

---

## 3. Minimal simultaneous closure index

Define admissible closure set:
\[
\mathcal A=\{(m,q)\mid C_{\text{phase}}\wedge C_{\text{dir}}\wedge C_{\text{act}}\}
\]

Define minimal closure index:
\[
m^\star=\min\{m\mid \exists q:(m,q)\in\mathcal A\}
\]

Current operational result (test-backed):
\[
m^\star=3
\]
for the current tested window and threshold family.

---

## 4. Derive-or-falsify boundary

The current \(m=3\) statement is upgraded only if both hold:

1. **Uniqueness stability:** \(m^\star=3\) remains stable under justified threshold and window perturbations.
2. **Microscopic support:** closure constraints can be mapped to TQM phase/topology structure, not only to effective scoring.

It is falsified/downgraded if any of the following occurs:

1. Another \(m\neq 3\) survives all constraints with equal or lower minimal index under robust perturbations.
2. Constraint definitions can be relaxed without losing bridge performance while changing minimal index.
3. A microscopic derivation prefers a different closure family.

---

## 5. Current claim boundary (review-safe)

Use:

> \(m=3\) is currently the best-supported minimal effective closure index under the full tested closure-constraint set.

Avoid:

> \(m=3\) is already a proven fundamental closure theorem.

---

## 6. Next theory tasks for theorem-level closure

1. Derive \(C_{\text{phase}}, C_{\text{dir}}, C_{\text{act}}\) from microscopic TQM assumptions (not only effective metrics).
2. Prove threshold/window invariance region for \(m^\star=3\), or explicitly characterize non-invariant regions.
3. Provide uniqueness conditions (or explicit non-uniqueness theorem) with clear hypothesis set.
