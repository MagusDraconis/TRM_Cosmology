# TRM M3 FP31: Ceil Inequality — Final Gap Closure

## Scope
This note documents FP31, which closes the final remaining proof obligation in the TRM/TQM $m = 3$ formal proof scaffold. After FP30, two PENDING-PROOF items (`qCoreSupport_limit_to_one` and `epsilon_phase_asymptotic_bound`) shared a single gap: the ceil inequality $q > \lceil c \rceil \implies 1/(q:\mathbb{Q}) < 1/c$. FP31 proves this lemma using standard Mathlib primitives and uses it to close both obligations.

---

## Core Lemma

**`one_div_lt_one_div_of_ceil_lt`**

> $\forall q \in \mathbb{Z}_{>0}, c \in \mathbb{Q}_{>0},\; q > \lceil c \rceil \implies \frac{1}{q} < \frac{1}{c}$

**Proof:**
1. `Int.ceil_spec c` gives $c \leq \lceil c \rceil$ in $\mathbb{R}$.
2. Cast to $\mathbb{Q}$: $c \leq (\lceil c \rceil : \mathbb{Q})$.
3. From $q > \lceil c \rceil$ (in $\mathbb{Z}$), cast preserves order: $(q : \mathbb{Q}) > (\lceil c \rceil : \mathbb{Q})$.
4. By transitivity: $(q : \mathbb{Q}) > c$.
5. Apply `one_div_lt_one_div` (Mathlib) with positivity of $q$ and $c$: $\frac{1}{q} < \frac{1}{c}$.

**Dependencies:** `Mathlib.Data.Rat.Basic`, `Mathlib.Data.Int.Basic`, `Mathlib.Algebra.Order.Field.Basic`.

---

## Closed Obligations

### 1. `qCoreSupport_limit_to_one` (from FP29)

> $\forall \varepsilon > 0, \exists Q, \forall q > Q,\; 1 - \text{qCoreSupport}(q) < \varepsilon$

- Setting $c = 3/\varepsilon$, $Q = \lceil c \rceil$, the goal reduces to $3/q < \varepsilon$.
- By the ceil lemma: $q > Q = \lceil 3/\varepsilon\rceil \implies 1/q < \varepsilon/3$, so $3/q < \varepsilon$.

### 2. `epsilon_phase_asymptotic_bound` (from FP30)

> $\forall \varepsilon > 0, \exists Q, \forall q > Q, \forall m \in M(q),\; \text{EpsilonPhaseExact}(m) < \varepsilon$

- $\text{EpsilonPhaseTolerance}(q) = 1/q$, and $m \in M(q)$ means $\text{EpsilonPhaseExact}(m) < 1/q$.
- Setting $c = \varepsilon^{-1}$, $Q = \lceil c \rceil$, the ceil lemma gives $1/q < \varepsilon$.
- Transitivity: $\text{EpsilonPhaseExact}(m) < 1/q < \varepsilon$.

---

## Final Obligation Map

| Category | Count | Items |
|:---|:---:|:---|
| **DEFINED** | **7** | EpsilonPhaseExact, EpsilonActionExact, phase_defect_m3_zero, action_residual_full_support, epsilon_phase_positive_for_all_m_ne_3, epsilon_phase_zero_iff_m3, qCoreSupport(q) |
| **ASSUMED** | 4 | TQM lattice phase closure, minimal lattice action, shared normalization, bounded domain |
| **PENDING-PROOF** | **0** | — |
| **PENDING-MODEL** | 0 | — |
| **RESOLVED** | 1 | domain_abstention_from_bounds |
| **BLOCKED** | **0** | — |

**No `sorry` remains in the entire continuous-domain Lean proof scaffold.**

The 4 ASSUMED items are model hypotheses declared explicitly in the scaffold — they are not hidden gaps. They define the domain of validity within which all proof obligations have been discharged.

---

## Claim Boundaries

- **Lean proof scaffold only.** All 7 lemmas/theorems in the DEFINED category have complete proof structures in Lean.
- **Not a universal theorem.** The 4 ASSUMED model hypotheses remain — they are explicit, not hidden.
- **No full first-principles closure.** Discharging the 4 assumptions would require additional theory (e.g., deriving TQM lattice phase closure from first principles).
- **No GR replacement.** No general-relativistic claims.
- **No numerology.** All definitions are exact Rational expressions.

---

## Status: ✅ FP01–FP31 Complete

The formal proof track is now fully scaffolded. The obligation map shows **0 PENDING-PROOF, 0 BLOCKED**. The remaining 4 assumptions are documented and explicit.
