# TRM M3 FP01–FP03 Exact-Rational Proof Scaffold Note

## Scope

This note documents the design, implementation, and results of the exact-rational formal proof scaffolding (FP01–FP03) implemented in `TRM.FormalProofs` and `TRM.FormalProofs.Cli`. This scaffolding transitions the `m=3` closure framework from numerical double-precision xUnit diagnostics toward exact-rational mathematical proofs within finite-domain constraints.

---

## 1) FP01: Exact qCore Derivation Proof

- **Concept:** Derives the `qCore` interval for `m=3` exactly using arbitrary-precision rational arithmetic (`BigInteger`-backed `Rational` class), confirming that the selection constraints are not double-precision numerical artifacts.
- **Formulation:** 
  - `Omega(q, m) = (q + m) / q`
  - `Gamma(q, m) = q / (q + m)`
  - Exact rational bounds: `Omega ∈ [116/100, 119/100]`, `Gamma ∈ [84/100, 86/100]`.
- **Results:**
  - **Search limit:** `q <= 10000`
  - **Status:** **PASS**
  - **Derived qCore:** `[16, 17, 18]`
  - **Details:** Exactly matches the standard tested-effective numerical `qCore` domain without double-precision tolerance drift or floating-point rounding errors.

---

## 2) FP02: Phase Defect Minimization Proof

- **Concept:** Verifies that the normalized phase closure defect for `m=3` is exactly zero over the derived `qCore`, and that `m=3` is the unique minimizer compared to neighboring modes `m ∈ [1, 5]`.
- **Formulation:**
  - `p_compatible = q + 3`
  - `PhaseDefectNormalized(q, m, targetShift=3) = |q * Omega - p_compatible| / targetShift`
- **Results:**
  - **Domain:** `q ∈ [16, 17, 18]`
  - **Status:** **PASS**
  - **Mode Defects:**
    - `m=1`: average defect = `2/3` (~0.666667)
    - `m=2`: average defect = `1/3` (~0.333333)
    - `m=3`: average defect = `0` (exactly zero)
    - `m=4`: average defect = `1/3` (~0.333333)
    - `m=5`: average defect = `2/3` (~0.666667)
  - **Details:** `m=3` achieves a perfect defect of exactly `0` and is the strict, unique minimizer. No phase defect counterexamples exist inside the `qCore` domain.

---

## 3) FP03: Finite Domain Selection Uniqueness Proof

- **Concept:** Scans a configurable finite domain (`m = 1..5`, `q <= qMax`) to check for any competing modes that can match or outcompete `m=3`'s perfect phase defect inside the `m=3` `qCore` or the broader domain.
- **Results:**
  - **Search Space:** `m ∈ [1, 5]`, `q <= 10000`
  - **Status:** **PASS**
  - **Competing Modes inside m=3 qCore:** None.
  - **Broader Domain Candidates:**
    - `m=2` is admissible at `q ∈ [11, 12]` with average defect `1/3` (~0.333333).
    - `m=4` is admissible at `q ∈ [22, 23, 24]` with average defect `1/3` (~0.333333).
    - Neither of these modes achieves a phase defect of `0` or is admissible inside the `m=3` `qCore` domain `[16, 17, 18]`.
  - **Details:** Confirms an exact finite-domain proof of m=3 uniqueness within the specified qCore/phase-closure domain q <= 10000.

---

## Current status statement

Current reviewer-safe status:

> exact-rational finite-domain proof scaffold for qCore/phase closure.

---

## Remaining gap

Primary remaining gaps:
1. Formal analytical proof of topological necessity and sufficiency across the infinite domain.
2. Formal analytical derivation of the bridge-scale coupling limits from microscopic lattice-energy principles.

---

## Claim boundaries

- finite-domain only
- not universal theorem
- not full first-principles closure
- not universal m=3 selection
- no GR replacement
- no numerology claim
