# TRM M3 FP22–FP24: Continuous-Domain Decomposition, Exact Epsilon Bounds, and Proof-Obligation Map

## Scope
This note documents the FP22–FP24 proof-scaffolding track, which decomposes the remaining continuous-domain proof gap `lemma_continuous_domain_asymptotic_limits_derived` into smaller, classified proof obligations. With FP01–FP21 completing the finite exact-rational and Lean proof scaffold, FP22–FP24 address the only remaining `sorry` in the final Lean inventory.

No theorem-level claim is made. The track remains within the finite Lean proof scaffold boundary.

---

## 1) FP22: Continuous-Domain Lemma Decomposition

- **Status:** **PASS** (CLI: `decompose-continuous-domain`)
- **Concept:** The monolithic `lemma_continuous_domain_asymptotic_limits_derived` (the sole remaining `sorry` from FP21) is decomposed into three smaller stubs:
  1. **`epsilon_phase_asymptotic_bound`** — For any $\varepsilon > 0$, there exists $Q(\varepsilon)$ such that for all $q > Q(\varepsilon)$, the normalized phase defect $|m - 3|/3 < \varepsilon$ for admissible modes.
  2. **`epsilon_action_asymptotic_bound`** — For any $\varepsilon > 0$, the action residual is bounded by $\varepsilon$ in the limit of large $q$-support.
  3. **`domain_abstention_from_bounds`** — Chains (1) and (2) to prove domain abstention.
- **Proof status:**
  - Stubs 1–2: `sorry` (require real-analysis limit arguments — not trivially provable).
  - Stub 3: **trivially proven** (`trivial`) — purely structural implication.
- **Output:**
  - Lean scaffold: `FP22_continuous_decomposition_scaffold.lean`
  - Log: `FP22_continuous_decomposition_log.txt`
- **Sorry inventory (updated):** 2 `sorry` entries (epsilon-phase, epsilon-action) + 1 `trivial`.

---

## 2) FP23: Exact Continuous-Bounds Definitions

- **Status:** **PASS** (CLI: `exact-continuous-bounds`)
- **Concept:** Exact symbolic/Lean expressions are defined for the epsilon-bound quantities:
  - **`EpsilonPhaseExact(m) = |m - 3| / 3`** — exact Rational, no floating-point.
  - **`EpsilonActionExact(qCoreSupport) = max(0, 1 - qCoreSupport)`** — exact Rational.
  - **`phase_defect_m3_zero`** — $m = 3 \implies \text{EpsilonPhaseExact}(3) = 0$ (proven by `norm_num`).
  - **`action_residual_full_support`** — full $q_{\text{Core}}$ support $\implies \text{EpsilonActionExact}(1) = 0$ (proven by `norm_num`).
- **Classification:**
  - **DEFINED (4):** EpsilonPhaseExact, EpsilonActionExact, phase_defect_m3_zero, action_residual_full_support
  - **ASSUMED (2):** phase_defect_limit (continuous limit of rational expression), action_residual_limit (requires $q_{\text{Core}}$ asymptotics)
  - **PENDING-PROOF (2):** epsilon_phase_bound ($\forall \varepsilon$ existence of $Q$), epsilon_action_bound ($\forall \varepsilon$ existence of $Q$)
- **Output:** `FP23_exact_continuous_bounds_log.txt`

---

## 3) FP24: Proof-Obligation-to-Assumption Map

- **Status:** **PASS** (CLI: `proof-obligation-map`)
- **Concept:** Each remaining proof obligation is mapped to its required first-principles assumptions and classified:
  - **DEFINED (4):** Exact rational definitions; provable by `norm_num` or identity.
  - **ASSUMED (4):** TQM lattice phase closure, minimal lattice action, shared/global normalization, bounded admissible domain.
  - **PENDING-PROOF (3):** epsilon_phase_asymptotic_bound, epsilon_action_asymptotic_bound, phase_defect_m_ne_3_positive ($\forall m \neq 3$).
  - **RESOLVED (1):** domain_abstention_from_bounds (structural implication).
  - **BLOCKED (0):** No items are currently blocked — all gaps are characterized by explicit assumption dependencies.
- **Dependency graph:**
  ```
  epsilon_phase_asymptotic_bound
    └── TQM lattice phase closure (ASSUMED)
    └── Bounded admissible domain (ASSUMED)

  epsilon_action_asymptotic_bound
    └── Minimal lattice action (ASSUMED)
    └── Shared/global normalization (ASSUMED)

  phase_defect_m_ne_3_positive (∀ m ≠ 3)
    └── Finite enumeration → unbounded ℤ (requires well-ordering/induction)
  ```
- **Output:** `FP24_proof_obligation_map_log.txt`

---

## Current Status

| Track | Status |
|:---|:---|
| FP01–FP21 | **PASS** — Finite exact-rational and Lean proof scaffold complete |
| FP22 | **PASS** — Continuous lemma decomposed into 2 epsilon stubs + 1 trivial |
| FP23 | **PASS** — 4 exact definitions + 2 assumptions + 2 pending-proof items classified |
| FP24 | **PASS** — Full obligation map: 4 DEFINED, 4 ASSUMED, 3 PENDING-PROOF, 1 RESOLVED, 0 BLOCKED |
| DS01–DS41 | **PASS** (41/41) — Diagnostics unaffected |

### Sorry Inventory Evolution

| FP | Sorry Count | Description |
|:---|:---:|:---|
| FP15 | ~10 | Initial Lean sorry inventory (after typecheck) |
| FP18 | ~4 | Reduced after Lean phase proofs (FP16–FP17) |
| FP21 | 1 | Only `lemma_continuous_domain_asymptotic_limits_derived` remains |
| FP22 | 2 | Monolithic lemma split into `epsilon_phase` + `epsilon_action` stubs |
| **Remaining** | **2** | Both are `sorry` — require real-analysis or induction for continuous/unbounded domain |

---

## Claim Boundaries

- **Finite Lean proof scaffold only.** FP01–FP21 prove finite-domain results natively in Lean.
- **Continuous domain theorem remains pending.** FP22–FP24 characterize but do not close the gap.
- **Not a universal theorem.** The scaffold applies to the $q_{\text{Core}} = \{16, 17, 18\}$ / $m = 3$ diagnostic framework.
- **No GR replacement.** No general-relativistic claims.
- **No numerology.** All definitions are exact Rational expressions.

---

## Next Direction (FP25+)

Potential continuations of the FP track:

1. **FP25: Real-analysis limit proof for epsilon_phase_asymptotic_bound** — requires importing `Mathlib/Analysis` and proving the $\forall \varepsilon > 0$ existence of $Q(\varepsilon)$ for the rational phase-defect expression.

2. **FP26: Real-analysis limit proof for epsilon_action_asymptotic_bound** — requires a model for $q_{\text{Core}}\text{Support}(q)$ asymptotic behavior (likely involving the rational-band structure from the TQM lattice).

3. **FP27: Unbounded-domain induction for phase_defect_m_ne_3_positive** — extends the finite enumeration proof ($m_{\text{Max}} = 5$) to all $\mathbb{Z}$ via well-ordering or induction.

Alternatively, the track may be paused at FP24 (0 BLOCKED, all gaps characterized) pending the availability of real-analysis or induction tooling.
