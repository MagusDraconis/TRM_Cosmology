# TRM M3 FP04–FP06 Exact Energy Margin Proof Scaffold Note

## Scope

This note documents the design, implementation, and results of the exact-rational formal energy margin proof scaffolding (FP04–FP06) implemented in `TRM.FormalProofs` and `TRM.FormalProofs.Cli`. This scaffolding extends the `m=3` exact-rational framework to compute shared functional components, exact energy margins, and boundary abstention logic without relying on floating-point arithmetic.

---

## 1) FP04: Exact Shared Functional Computation

- **Concept:** Computes the individual components of the shared functional (`PhaseDefect`, `BridgePenalty`, `ActionResidual`) using only exact arbitrary-precision rational arithmetic.
- **Formulation:**
  - `PhaseDefect` = Average exact normalized phase defect across `qCore`.
  - `BridgePenalty` = `1 - (supportCount / qCore.Count)`.
  - `ActionResidual` = `(E_m - E_min) / (E_max - E_min)`, where `E_m = 1 / inBandCount` (a minimal exact rational proxy for action scaling based directly on in-band density).
- **Results:**
  - **Search Space:** `m ∈ [1, 5]`, `q <= 10000`
  - **Status:** **PASS**
  - **Values Computed:**
    - `m=1`: Total = `8/3` (~2.6667) | Admissible=False
    - `m=2`: Total = `5/3` (~1.6667) | Admissible=False
    - `m=3`: Total = `1/9` (~0.1111) | **Admissible=True**
    - `m=4`: Total = `13/9` (~1.4444) | Admissible=False
    - `m=5`: Total = `5/3` (~1.6667) | Admissible=False
  - **Details:** Validates that the shared functional can be perfectly expressed using exact rationals and uniquely selects `m=3` as the only admissible mode.

---

## 2) FP05: Exact Energy Margin Positivity Proof

- **Concept:** Proves that the exact rational energy margin `ΔE = E_competitor - E_m3` is strictly positive (`> 0`) for neighboring structural modes (`m=2` and `m=4`).
- **Formulation:**
  - `ΔE_2 = E_2 - E_3`
  - `ΔE_4 = E_4 - E_3`
- **Results:**
  - **Status:** **PASS**
  - **Computed Margins:**
    - Margin (`m=2` vs `m=3`): `14/9` (~1.5556)
    - Margin (`m=4` vs `m=3`): `4/3` (~1.3333)
  - **Details:** `m=3` maintains strict positive energy margin dominance. No competitor matches or outcompetes `m=3` inside the exact framework.

---

## 3) FP06: Domain Boundary Abstention Proof

- **Concept:** Verifies that the formal diagnostic rule fails gracefully (abstains) across analytical constraint boundaries rather than confidently locking onto a false positive.
- **Tested Boundaries:**
  - **No qCore support:** Bridge penalty forces `Admissible = False`.
  - **Phase-defect violation:** Evaluating `m=2` over `m=3` core yields exact phase defect `1/3` (forces `Admissible = False`).
  - **Action-residual violation:** Simulated exact residual of `99/100` (forces `Admissible = False`).
  - **Margin <= 0:** Simulating a competitor tie/loss forces selection abstention.
- **Results:**
  - **Status:** **PASS**
  - **Details:** The formal rule correctly identifies all exact analytical boundary failures and strictly abstains, confirming that it is safely bounded.

---

## Current status statement

Current reviewer-safe status:

> exact-rational finite-domain proof scaffold for shared-functional energy margin and boundary abstention.

---

## Remaining gap

Primary remaining gaps:
1. Formal analytical proof of topological necessity and sufficiency across the infinite continuous domain.
2. Formal analytical derivation of the structural bridge-scale coupling limits directly from microscopic lattice-energy principles.

---

## Claim boundaries

- exact finite-domain proof scaffold only
- not universal theorem
- not full first-principles closure
- no GR replacement
- no numerology claim