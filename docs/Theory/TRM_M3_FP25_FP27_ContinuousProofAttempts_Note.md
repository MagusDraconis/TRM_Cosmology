# TRM M3 FP25–FP27: Continuous Proof Attempts — Phase Limit, Action Model, and Phase Trichotomy

## Scope
This note documents FP25–FP27, which provide proof-attempt scaffolds for the three remaining proof obligations identified in FP24's obligation map. FP27 achieves a partial breakthrough: the `phase_defect_m_ne_3_positive` obligation is fully proven over all $\mathbb{Z}$ using integer trichotomy — no induction or well-ordering required.

Claim boundary: finite/continuous proof scaffold only; pending-proof/model items remain; not a universal theorem unless actually proven; no GR replacement; no numerology.

---

## 1) FP25: Epsilon-Phase Asymptotic Bound — Lean Proof Attempt

- **Status:** **PASS** (CLI: `fp25-phase-limit`) — scaffold created; not a completed proof.
- **Concept:** Structured Lean file with the `epsilon_phase_asymptotic_bound` lemma decomposed into sub-lemmas and the key model assumption made explicit.
- **Sub-lemma status:**
  - `epsilon_phase_m3_bounded` → **DEFINED** ($\texttt{norm\_num}$, trivial: $0 < \varepsilon$)
  - `epsilon_phase_zero_iff_m3` ($\leftarrow$ direction) → **PENDING-PROOF** (requires unbounded $\mathbb{Z}$ — delegated to FP27)
  - `epsilon_phase_positive_for_m_ne_3` → **PENDING-PROOF** (delegated to FP27)
  - `epsilon_phase_asymptotic_bound` → **PENDING-PROOF** (depends on $M(q) \to \{3\}$ assumption)
- **Key assumption identified:**
  > $M(q) \to \{3\}$ as $q \to \infty$ — the admissible mode set converges monotonically to $\{m = 3\}$.
  This is the core continuous-domain gap: proving that at sufficiently large $q$, only $m = 3$ (or modes within $\varepsilon$ of it) survive the phase-defect tolerance.
- **Output:** `FP25_phase_limit_scaffold.lean`, `FP25_phase_limit_log.txt`

---

## 2) FP26: Epsilon-Action Asymptotic Bound — Model Requirements Report

- **Status:** **PASS** (CLI: `fp26-action-limit`) — model requirements specified; **no fake proof**.
- **Concept:** This is NOT a proof attempt. It characterizes the model-level assumptions that must be satisfied before `epsilon_action_asymptotic_bound` can be proven. The gap is a **model-definition gap**, not a mathematical gap.
- **Required model assumptions:**
  1. **`qCoreSupport(q)` must be DEFINED** as a function $\mathbb{Z} \to \mathbb{Q}$.
     - Currently `qCoreSupport` is a free parameter; no $q$-dependent model exists.
  2. **Action limit condition:** $\lim_{q \to \infty} \text{qCoreSupport}(q) = 1$.
     - Physical interpretation: as the $q$-lattice extends, the bridge band support fraction approaches full coverage.
  3. **Action tolerance convergence:** $\tau(q) \to 0$ as $q \to \infty$.
     - Follows from the minimal lattice action hypothesis (action residual scales as $O(1/q)$).
- **Proof sketch (once model is defined):**
  > Given $\varepsilon > 0$, choose $Q$ such that $\forall q > Q$, $\text{qCoreSupport}(q) > 1 - \varepsilon$.
  > Then $\text{EpsilonActionExact}(\text{qCoreSupport}(q)) = \max(0, 1 - \text{qCoreSupport}(q)) < \varepsilon$.
  > This is a standard $\varepsilon$-$\delta$ limit argument.
- **Classification:**
  | Item | Status |
  |:---|:---|
  | `EpsilonActionExact` | DEFINED |
  | `qCoreSupport(q)` | **PENDING-MODEL** |
  | `action_limit_condition` | ASSUMED |
  | `action_tolerance_convergence` | ASSUMED |
  | `epsilon_action_asymptotic_bound` | **PENDING-MODEL** |
- **Output:** `FP26_action_limit_log.txt`

---

## 3) FP27: Phase Defect Positivity — $\forall m \neq 3$ over $\mathbb{Z}$ (Trichotomy)

- **Status:** **PASS** (CLI: `fp27-phase-induction`) — **theorem fully proven** for all $\mathbb{Z}$.
- **Concept:** Prove $\forall m \neq 3, \text{EpsilonPhaseExact}(m) > 0$ over all integers, not just the finite set $m = 1{\ldots}5$.
- **Proof approach:** Integer trichotomy — no induction or well-ordering needed.
  - **Case $m > 3$:** $\text{EpsilonPhaseExact}(m) = (m - 3)/3 > 0$ (algebra + `positivity`).
  - **Case $m < 3$:** $\text{EpsilonPhaseExact}(m) = (3 - m)/3 > 0$ (algebra + `positivity`).
  - **Case $m = 3$:** Excluded by hypothesis.
- **Sub-lemma status (all proven):**

  | Lemma | Tactic | Status |
  |:---|:---|:---|
  | `epsilon_phase_pos_m1` through `_m5` | `norm_num` | PROVEN (finite base) |
  | `epsilon_phase_eq_for_m_gt_3` | `exact_mod_cast` + `Rat.abs_of_pos` | PROVEN |
  | `epsilon_phase_pos_for_m_gt_3` | `positivity` | PROVEN |
  | `epsilon_phase_eq_for_m_lt_3` | `exact_mod_cast` + `Rat.abs_of_neg` | PROVEN |
  | `epsilon_phase_pos_for_m_lt_3` | `positivity` | PROVEN |
  | `epsilon_phase_positive_for_all_m_ne_3` | `omega` + case analysis | **PROVEN** |

- **No `sorry` remaining.** All sub-lemmas are proven using only Mathlib tactics (`norm_num`, `exact_mod_cast`, `positivity`, `omega`, `linarith`, `ring`).
- **Verdict:** This obligation is **FULLY CLOSED** — elevated from **PENDING-PROOF** to **DEFINED**.
- **Output:** `FP27_phase_induction_scaffold.lean`, `FP27_phase_induction_log.txt`

---

## Obligation Map Update (Post-FP27)

| Category | Before FP25–FP27 | After FP27 |
|:---|:---:|:---:|
| DEFINED | 4 | **5** |
| ASSUMED | 4 | 4 |
| PENDING-PROOF | 3 | **2** |
| PENDING-MODEL | 0 | **1** (new) |
| RESOLVED | 1 | 1 |
| BLOCKED | 0 | 0 |

**Remaining gaps (2 PENDING-PROOF + 1 PENDING-MODEL):**
1. **`epsilon_phase_asymptotic_bound`** (PENDING-PROOF) — requires $M(q) \to \{3\}$ convergence proof.
2. **`epsilon_action_asymptotic_bound`** (PENDING-MODEL) — requires `qCoreSupport(q)` definition; proof is standard $\varepsilon$-$\delta$ once defined.
3. **`epsilon_phase_zero_iff_m3` ($\leftarrow$)** (PENDING-PROOF) — the forward direction ($m = 3 \implies \text{EpsilonPhaseExact} = 0$) is proven; the reverse direction ($\text{EpsilonPhaseExact} = 0 \implies m = 3$) is a corollary of FP27 (if $m \neq 3$, then $\text{EpsilonPhaseExact} > 0$, so contrapositive gives $\text{EpsilonPhaseExact} = 0 \implies m = 3$).

---

## Claim Boundaries

- **Finite/continuous proof scaffold only.** FP27 proves one lemma; FP25 and FP26 remain scaffolds.
- **Not a universal theorem.** The $M(q) \to \{3\}$ assumption for FP25 is unproven.
- **No GR replacement.** No general-relativistic claims.
- **No numerology.** All definitions are exact Rational expressions.

---

## Next Direction (FP28+)

1. **FP28:** Complete `epsilon_phase_zero_iff_m3` — the reverse direction is now a trivial corollary of FP27 (contrapositive).
2. **FP29:** Define `qCoreSupport(q)` model function — unblocks FP26.
3. **FP30:** Prove $M(q) \to \{3\}$ convergence — the core continuous-domain gap for FP25.
