# TRM M3 FP16–FP18 Lean Phase Proofs Note

## Scope

This note documents the design, implementation, and results of the formal phase defect proofs (FP16–FP18) implemented in `TRM.FormalProofs` and `TRM.FormalProofs.Cli`. This step continues the `sorry` reduction phase by formally proving the discrete phase-defect lemmas over the finite structural `qCore`.

---

## 1) FP16: Lean Phase Defect m=3 Zero

- **Concept:** Replaces the `sorry` placeholder for `lemma_phase_defect_m3_zero` with a complete, explicit proof in Lean over the finite subset `qCore = [16, 17, 18]`.
- **Proof Mechanism:** 
  - Exhaustive finite-case enumeration via `rcases`.
  - Exact rational arithmetic reduction using the `norm_num` tactic to confirm that the `PhaseDefect` evaluates exactly to `0` for all structural core branches.
- **Status:** **PASS** (Formal placeholder removed).

---

## 2) FP17: Lean Competitor Phase Defects Positive

- **Concept:** Replaces the `sorry` placeholder for `lemma_phase_defect_competitors_positive` with an explicit formal proof over the same `qCore`.
- **Proof Mechanism:**
  - Case splitting over the finite set of evaluated competitors (`m ∈ [1, 2, 4, 5]`) and the finite `qCore` elements.
  - Applying `norm_num` to prove that `PhaseDefect > 0` everywhere outside `m=3` in this finite domain, demonstrating strict phase-defect isolation natively in Lean syntax.
- **Status:** **PASS** (Formal placeholder removed).

---

## 3) FP18: Lean Sorry Inventory (Updated)

- **Concept:** Recounts the remaining analytical `sorry` placeholders mapped to the formal Lemma obligations.
- **Inventory (1 Pending Theorem):**
  - `lemma_domain_abstention`: Maps to *Lemma 3: Domain Closure* (Proof that the bounding limits mathematically guarantee graceful abstention across the continuous structural envelope).
- **Results:**
  - **Status:** **PASS**
  - **Details:** The inventory has been successfully reduced from 3 to 1. The discrete/finite topological necessity logic (Lemma 1) has been structurally proven within the finite `qCore` bounds natively via explicit tactics.

---

## Current status statement

Current reviewer-safe status:

> exact-rational finite-domain / proof-assistant scaffold (finite-qCore phase lemmas proven natively, domain abstention pending).

---

## Remaining gap

Primary remaining gaps:
1. Formal analytical proof of `lemma_domain_abstention` across the infinite continuous domain (replacing the single remaining `sorry` placeholder).
2. Formalization of bridge-scale coupling limits directly from microscopic lattice-energy principles in Lean/Coq.

---

## Claim boundaries

- finite qCore Lean proof only
- not universal theorem
- not full first-principles closure
- no GR replacement
- no numerology claim