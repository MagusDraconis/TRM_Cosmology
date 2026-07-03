# TRM M3 FP19–FP21 Lean Domain Abstention Note

## Scope

This note documents the design, implementation, and results of the formal domain abstention proofs (FP19–FP21) implemented in `TRM.FormalProofs` and `TRM.FormalProofs.Cli`. This step finalizes the finite-domain `sorry` reduction phase by structurally decomposing the domain abstention rules into specific exact boundaries and proving them analytically within Lean 4.

---

## 1) FP19: Lean Domain Abstention Decomposed

- **Concept:** The monolithic `lemma_domain_abstention` placeholder is structurally decomposed into its formal sub-components representing the exact analytical boundaries of the functional envelope.
- **Decomposed Boundary Rules:**
  - `boundary_no_qCore_support_abstains`
  - `boundary_phase_defect_violation_abstains`
  - `boundary_action_residual_violation_abstains`
  - `boundary_margin_nonpositive_abstains`
- **Status:** **PASS** (Decomposition successfully isolates finite exact cases from continuous open theorems).

---

## 2) FP20: Lean Finite Boundary Cases Proven

- **Concept:** The explicit finite boundary violations derived in FP19 are formally proven to guarantee an abstention state (`¬ IsAdmissible`) natively in Lean using structural evaluation and `norm_num`.
- **Proof Mechanism:** 
  - An exact rational threshold structure (`PhaseTol = 35/100`, `ActionTol = 95/100`) and penalty function are defined.
  - The hypothesis `IsAdmissible` is destructured via `rcases` and mathematically contradicted using `norm_num`.
- **Status:** **PASS** (Formal placeholders completely removed for all exact finite boundary cases).

---

## 3) FP21: Final Lean Sorry Inventory

- **Concept:** Concludes the formal verification phase of the scaffold by reporting the final gap.
- **Inventory (1 Remaining Theorem):**
  - `lemma_continuous_domain_asymptotic_limits_derived`: Pending microscopic/continuous bridge and action limit derivation from primary first-principles.
- **Results:**
  - **Status:** **PASS**
  - **Details:** The finite proof scaffold is exhausted. Every finite, exact, and discrete component of the `m=3` selection rule has been strictly bounded, symbolically expressed, and mathematically proven within the proof-assistant scaffold. The only remaining `sorry` mathematically enforces the final continuous/microscopic physics gap.

---

## Current status statement

Current reviewer-safe status:

> exact-rational finite-domain / proof-assistant scaffold (finite boundaries proven natively, continuous limits pending).

---

## Remaining gap

Primary remaining gaps:
1. Formal analytical proof of the `lemma_continuous_domain_asymptotic_limits_derived` across the infinite continuous domain from underlying first principles (TQM lattice/synchronization origin).

---

## Claim boundaries

- finite Lean proof scaffold only
- continuous domain theorem remains pending
- not universal theorem
- not full first-principles closure
- no GR replacement
- no numerology claim
