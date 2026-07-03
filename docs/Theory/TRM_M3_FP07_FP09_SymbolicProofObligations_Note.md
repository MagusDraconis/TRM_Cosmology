# TRM M3 FP07–FP09 Symbolic Proof Obligations Note

## Scope

This note documents the design, implementation, and results of the symbolic proof obligations scaffolding (FP07–FP09) implemented in `TRM.FormalProofs` and `TRM.FormalProofs.Cli`. This scaffolding prepares the exact-rational finite-domain results for transition to analytical/symbolic theorem proofs and potential formalization in a proof assistant.

---

## 1) FP07: Exact Functional Symbolic Inequalities Export

- **Concept:** Exports the exact rational inequalities required to formally prove `m=3` dominance into a machine-readable format.
- **Results:**
  - **Status:** **PASS**
  - **Verified Inequalities (q <= 10000):**
    - `E_3 < E_2` (Verified, margin: `14/9`)
    - `E_3 < E_4` (Verified, margin: `4/3`)
    - `PhaseDefect(m=3) == 0` (Verified, margin: `0`)
    - `PhaseDefect(m!=3) > 0 in qCore` (Verified, margin: `true`)
  - **Details:** These inequalities are strictly validated across the specified finite domain and output in JSON format for use in analytical proof planning.

---

## 2) FP08: Proof Obligations Decomposed By Lemma

- **Concept:** Maps the empirical and exact-rational results to the formal mathematical lemmas defining the `m=3` selection constraints.
- **Results:**
  - **Status:** **PASS**
  - **Lemma 1 (Topological Necessity):** 
    - Prove `|qΩ - p|` uniquely minimized by `m=3` inside `qCore`.
    - Marked: `[EXACT-FINITE-PASS]`, `[PENDING-SYMBOLIC]`, `[PENDING-PROOF-ASSISTANT]`
  - **Lemma 2 (Energy-Margin Sufficiency):** 
    - Prove `ΔE = E_competitor - E_m3 > 0` for all admissible competitors globally.
    - Marked: `[EXACT-FINITE-PASS]`, `[PENDING-SYMBOLIC]`, `[PENDING-PROOF-ASSISTANT]`
  - **Lemma 3 (Domain Closure):** 
    - Prove rule abstains under invalid boundary conditions.
    - Marked: `[EXACT-FINITE-PASS]`, `[PENDING-SYMBOLIC]`, `[PENDING-PROOF-ASSISTANT]`
  - **Details:** Successfully structures the boundaries between the computationally verified finite domains and the pending analytical mathematical proofs.

---

## 3) FP09: Counterexample Search and Minimal Witnesses Export

- **Concept:** Exports minimal competitor margins as mathematical witnesses of strict dominance, or exports the exact properties of counterexamples if any exist.
- **Results:**
  - **Status:** **PASS**
  - **Witnesses:**
    - `m=2` margin: `14/9`
    - `m=4` margin: `4/3`
  - **Counterexamples:** `None`
  - **Details:** Exports machine-readable witnesses demonstrating that within the `m <= 5`, `q <= 10000` domain, there are zero counterexamples and strict positive margins hold throughout.

---

## Current status statement

Current reviewer-safe status:

> exact-rational finite-domain / symbolic proof scaffold for shared-functional energy margin and boundary abstention.

---

## Remaining gap

Primary remaining gaps:
1. Formal analytical proof of topological necessity and sufficiency across the infinite continuous domain (addressing `[PENDING-SYMBOLIC]`).
2. Formalization of constraints in a proof assistant like Lean or Coq (`[PENDING-PROOF-ASSISTANT]`).

---

## Claim boundaries

- exact finite-domain / symbolic scaffold only
- not universal theorem
- not full first-principles closure
- no GR replacement
- no numerology claim
