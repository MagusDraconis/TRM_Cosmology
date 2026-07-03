# TRM M3 FP10–FP12 Proof Assistant Export Note

## Scope

This note documents the design, implementation, and results of the proof assistant transition scaffolding (FP10–FP12) implemented in `TRM.FormalProofs` and `TRM.FormalProofs.Cli`. This scaffolding exports the verified exact-rational constants and proof obligations from the C# diagnostic framework into formal definitions and theorem stubs suitable for automated theorem provers (like Lean 4).

---

## 1) FP10: Proof Assistant Definitions Export

- **Concept:** Translates the exact `Rational` C# logic into mathematically equivalent formal definitions in Lean (`ℚ`).
- **Exported Definitions:**
  - `Omega (q m : ℚ) : ℚ := (q + m) / q`
  - `Gamma (q m : ℚ) : ℚ := q / (q + m)`
  - `PhaseDefect (q m targetShift : ℚ) : ℚ := |q * Omega q m - (q + targetShift)| / targetShift`
  - `qCore : List ℚ := [16, 17, 18]`
- **Results:**
  - **Status:** **PASS**
  - **Target Output:** A strictly formatted `.lean` scaffold file is correctly generated containing these definitions without floating-point artifacts.

---

## 2) FP11: Proof Assistant Lemmas Export

- **Concept:** Maps the identified Lemma Proof Obligations (from FP08) into explicit theorem stubs in Lean, anchoring them to the exported exact constants (Witnesses).
- **Exported Lemmas (Stubbed with `sorry`):**
  - `lemma_qcore_exact`
  - `lemma_phase_defect_m3_zero`
  - `lemma_phase_defect_competitors_positive`
  - `lemma_energy_margin_m2_positive`
  - `lemma_energy_margin_m4_positive`
  - `lemma_domain_abstention`
- **Results:**
  - **Status:** **PASS**
  - **Details:** The stubs successfully bridge the computational domain to the analytical formal-proof domain. No claims of completed theorem proofs are generated (`sorry` is explicitly used).

---

## 3) FP12: Proof Assistant Export Validation

- **Concept:** Verifies that the formal constants written into the `.lean` template are mathematically identical to the exact computational witnesses found by the C# arbitrary-precision framework (FP09).
- **Verifications Performed:**
  - `qCore` strictly matches `[16, 17, 18]`
  - `Margin_m2` strictly matches `14/9`
  - `Margin_m4` strictly matches `4/3`
  - `PhaseDefect_m3` strictly matches `0`
- **Results:**
  - **Status:** **PASS**
  - **Details:** All exported proof-assistant constants strictly map to the exact CLI computational witnesses. Verification succeeds.

---

## Current status statement

Current reviewer-safe status:

> exact-rational finite-domain / proof-assistant scaffold (theorems stubbed, not proven) for shared-functional energy margin and boundary abstention.

---

## Remaining gap

Primary remaining gaps:
1. Formal analytical proof of topological necessity and sufficiency across the infinite continuous domain (replacing `sorry` in Lean).
2. Formalization of bridge-scale coupling limits directly from microscopic lattice-energy principles in Lean/Coq.

---

## Claim boundaries

- proof-assistant scaffold only
- theorems stubbed, not proven
- not universal theorem
- not full first-principles closure
- no GR replacement
- no numerology claim
