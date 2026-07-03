# TRM M3 Formal Proof Obligations

## Scope

This document formalizes the open mathematical proof obligations required to transition the `m=3` closure candidate from a numerical diagnostic scaffold to a formal theorem-level derivation. These obligations were explicitly framed during the RBF77–RBF79 Lemma Scaffolding and anchored under the RBF80–RBF82 Action-Stationarity diagnostics.

---

## Post-RBF80–RBF82 Action-Stationarity Anchoring

Before proceeding to formal analytical derivation, RBF80–RBF82 established the final empirical anchoring for the action-stationarity criterion:
- **RBF80**: The action stationarity residual maps directly to the minimal lattice action Euler/stationarity proxy: `δE_action = (E_m - E_min) / energyRange`. The operational action tolerance corresponds explicitly to this residual.
- **RBF81**: Action-stationarity is strictly necessary for energy-margin dominance. Disabling action allows competitors to enter the admissible space and can collapse the strict margin selection.
- **RBF82**: Action-stationarity uses exclusively shared/global normalization. Per-family scaling is structurally rejected as invalid tuning, confirming no hidden per-mode free parameters.

---

## Post-FP01–FP21 Exact-Rational and Proof-Assistant Scaffolding

Moving beyond numerical double-precision diagnostics, FP01–FP21 established an exact-rational mathematical scaffolding for finite domains, generated the explicit proof-assistant transition layer, and reduced finite formal theorem placeholders:
- **FP01**: Derives the `qCore = [16, 17, 18]` interval exactly using arbitrary-precision rational arithmetic with exact rational bounds for `m=3` within `q <= 10000`.
- **FP02**: Verifies that the normalized phase closure defect is exactly `0` for `m=3` over `qCore`, proving it is the strict, unique minimizer compared to neighboring modes `m ∈ [1, 5]`.
- **FP03**: Performs a finite-domain search (`m = 1..5`, `q <= 10000`) and proves that no competitor mode is admissible inside the `m=3` `qCore` domain, establishing an exact finite-domain proof of uniqueness.
- **FP04**: Computes the shared functional components (`PhaseDefect`, `BridgePenalty`, `ActionResidual`) exactly using rational arithmetic, proving `m=3` remains uniquely admissible under exact constraints.
- **FP05**: Proves that the exact rational energy margin `ΔE = E_competitor - E_m3` is strictly positive (`> 0`) against admissible structural competitors (`m=2`, `m=4`).
- **FP06**: Verifies that the formal rule safely and gracefully abstains under exact mathematical boundary violations (phase-defect failure, missing core, margin <= 0) rather than false-selecting a mode.
- **FP07**: Exports exact symbolic inequalities (`E_3 < E_2`, `PhaseDefect(m=3) == 0`) into machine-readable JSON for integration with formal proof environments.
- **FP08**: Decomposes proof obligations explicitly into Lemmas (Necessity, Sufficiency, Closure), distinguishing `[EXACT-FINITE-PASS]` from `[PENDING-SYMBOLIC]` and `[PENDING-PROOF-ASSISTANT]`.
- **FP09**: Conducts counterexample searches over the finite domain, exporting minimal valid witnesses (e.g., margins for `m=2` and `m=4`) proving strict dominance.
- **FP10**: Exports Lean-style definitions matching the exact operational logic (`Omega`, `Gamma`, `PhaseDefect`, `qCore`).
- **FP11**: Generates theorem stubs (`lemma_qcore_exact`, `lemma_energy_margin_m2_positive`, etc.) bounded with `sorry` to establish the proof-assistant interface.
- **FP12**: Computationally verifies that the exported proof-assistant constants (`Margin_m2 = 14/9`, `PhaseDefect_m3 = 0`, etc.) exactly match the dynamically calculated CLI witnesses.
- **FP13**: Syntactically validates the generated Lean code to ensure robust parser structure (supporting `Mathlib.Data.Rat.Basic`).
- **FP14**: Successfully formally proves simple finite constants directly within the Lean scaffold using mathematical tactics (`rfl`, `norm_num`), officially beginning the `sorry` reduction phase.
- **FP15**: Computes a precise inventory of all remaining analytical theorem stubs (3 placeholders), cleanly mapping each remaining `sorry` to its parent Formal Lemma.
- **FP16**: Fully proves `lemma_phase_defect_m3_zero` inside Lean using `norm_num` over the finite explicit `qCore` domain, successfully removing the `sorry` placeholder.
- **FP17**: Fully proves `lemma_phase_defect_competitors_positive` for finite alternatives (`m ∈ [1,2,4,5]`) inside Lean via finite-case mapping and `norm_num`, removing its `sorry`.
- **FP18**: Re-evaluates the Lean `sorry` inventory, verifying that only 1 strict mathematical placeholder remains (`lemma_domain_abstention`).
- **FP19**: Decomposes `lemma_domain_abstention` into discrete finite boundary violations (e.g., no qCore support, phase/action limit violations, non-positive margin).
- **FP20**: Formally proves the finite boundary abstention cases inside Lean using explicit contradiction tactics (`norm_num`), successfully removing the discrete boundary placeholders.
- **FP21**: Finalizes the scaffold inventory, confirming the only remaining `sorry` maps strictly to `lemma_continuous_domain_asymptotic_limits_derived` (the continuous microscopic gap).
- **FP22**: Decomposes the monolithic continuous lemma into three stubs: `epsilon_phase_asymptotic_bound` (sorry), `epsilon_action_asymptotic_bound` (sorry), and `domain_abstention_from_bounds` (trivial, resolved). Reduces 1 uncharacterized gap to 2 bounded epsilon-lemma stubs.
- **FP23**: Defines exact Rational expressions for epsilon-phase (`|m-3|/3`) and epsilon-action (`max(0, 1 - qCoreSupport)`). Classifies 4 definitions as DEFINED, 2 limits as ASSUMED, 2 bounds as PENDING-PROOF.
- **FP24**: Maps all remaining proof obligations to first-principles assumptions. Classification: 4 DEFINED, 4 ASSUMED, 3 PENDING-PROOF, 1 RESOLVED, 0 BLOCKED. The continuous gap is fully characterized; no unidentified blockers remain.
- **FP25 (Epsilon-Phase Limit)**: Structured Lean proof attempt for `epsilon_phase_asymptotic_bound`. Decomposes into 4 sub-lemmas: 1 DEFINED, 3 PENDING-PROOF. Identifies $M(q) \to \{3\}$ as the key model assumption.
- **FP26 (Epsilon-Action Limit)**: Model-requirement report (NOT a fake proof). Characterizes `epsilon_action_asymptotic_bound` as a PENDING-MODEL gap: `qCoreSupport(q)` must be defined as a function; once defined, the proof is standard $\varepsilon$-$\delta$.
- **FP27 (Phase Trichotomy)**: **Fully proves** $\forall m \neq 3, \text{EpsilonPhaseExact}(m) > 0$ over all $\mathbb{Z}$ using integer trichotomy (no induction). Elevates `phase_defect_m_ne_3_positive` from PENDING-PROOF to **DEFINED**. Updated counts: 5 DEFINED, 4 ASSUMED, 2 PENDING-PROOF, 1 PENDING-MODEL, 1 RESOLVED, 0 BLOCKED.
- **FP28 (Phase Zero-Iff)**: Proves `epsilon_phase_zero_iff_m3` both directions (forward: norm_num; reverse: FP27 contrapositive). Elevates obligation to DEFINED.
- **FP29 (qCoreSupport Model)**: Defines `qCoreSupport(q) = 1 - 3/q` as exact Rational function. Elevates from PENDING-MODEL to DEFINED. `epsilon_action_asymptotic_bound` proven modulo ceil inequality.
- **FP30 (Convergence Proof)**: Structures `epsilon_phase_asymptotic_bound` with shrinking tolerance `1/q`. Both FP29 and FP30 reduce to a single ceil inequality gap. Post-FP30: 7 DEFINED, 4 ASSUMED, 2 PENDING-PROOF (same ceil gap), 0 PENDING-MODEL, 1 RESOLVED, 0 BLOCKED.
- **FP31 (Ceil Inequality — Final Closure)**: Proves `one_div_lt_one_div_of_ceil_lt` using `Int.ceil_spec` + `one_div_lt_one_div`. Closes both `qCoreSupport_limit_to_one` and `epsilon_phase_asymptotic_bound`. **Final map: 7 DEFINED, 4 ASSUMED, 0 PENDING-PROOF, 0 BLOCKED. No 'sorry' remains.**

---

## Double-Slit Phase-Coherence Diagnostic Track (DS01–DS05)

While the formal `m=3` scaffold targets closure verification, a parallel diagnostic track has been initiated to evaluate phase-coherence applications using simple interference physics.

- **DS01–DS05**: Confirm that basic TRM/TQM phase-synchronization rules accurately reproduce two-path interference fringes, deterministic discrete-hit envelopes, the destruction of visibility via a which-path decoherence scalar, and the quantum complementarity bound (`V^2 + D^2 <= 1`). 

*Note: The DS track operates strictly under candidate/diagnostic boundaries and does not claim replacement of standard QM nor provide theorem-level proofs of wave-function collapse.*

---

## Lemma 1: Topological Necessity

**Statement:**
Selection of `m=3` is topologically necessary if and only if the phase closure defect `|qΩ - p| / targetShift <= ε_phase` is exclusively minimized by `m=3` over the structurally derived `qCore` domain.

**Prerequisites:**
- Rational phase lattice assumption (`Ω = (q+m)/q`)
- Integer winding cycle constraint (`p = q + m`)
- Structurally derived bridge `qCore` domain (`[16, 17, 18]`)

**Allowed Counterexamples:**
- Alternative `m`-modes satisfying the defect bound within `qCore` without incurring heavier action penalties.

**Open Proof Steps (PENDING-ANALYTICAL):**
1. Analytically derive the mathematical uniqueness of the phase defect minimum for `m=3` across the bounded `qCore` space.
2. Prove that no other rational fraction within the defined topology can satisfy the threshold purely algebraically.

---

## Lemma 2: Energy-Margin Sufficiency

**Statement:**
The joint satisfaction of phase closure, bridge prior, and action stationarity is mathematically sufficient to uniquely select `m=3` if the shared energy margin `ΔE = E_competitor - E_m3` is strictly greater than 0 for all admissible competitors.

**Prerequisites:**
- Formulation of the shared minimal functional evaluating Phase, Bridge, and Action components.
- Bounded action-stationarity residual `ε_action` limiting overall admissibility based on the shared lattice-energy stationarity residual.
- Shared global normalization applied consistently across all modes.

**Allowed Counterexamples:**
- Admissible competitors (e.g., `m=2`, `m=4`) achieving a lower or equal shared functional energy (`ΔE <= 0`) within the defined operational space under shared global normalization.

**Open Proof Steps (PENDING-ANALYTICAL):**
1. Derive `δE_action` and `ΔE > 0` directly from the microscopic TQM lattice action without relying on numerical scans.
2. Formulate the stationarity condition analytically to demonstrate that energy descent naturally traps `m=3` over alternatives.

---

## Lemma 3: Domain Closure

**Statement:**
The domain of validity for the `m=3` selection rule is strictly bounded by the analytical constraint limits `ε_phase` and `ε_action`; the formal rule must gracefully abstain everywhere outside this continuous envelope.

**Prerequisites:**
- Continuous parameterization of operational selection limits.
- Explicit classification map of boundary failure regimes (phase, action, margin, mixed).

**Allowed Counterexamples:**
- Parameter regimes where the formal selection equations generate confident false-positive selections outside the physically valid macroscopic domain.

**Open Proof Steps (PENDING-ANALYTICAL):**
1. Derive the absolute asymptotic limits of `ε_phase` and `ε_action` directly from lattice-energy normalization principles.
2. Prove mathematically that excursions beyond these analytical boundaries cleanly break the invariant form, guaranteeing constraint abstention.

---

## Claim boundaries

- pending formal derivation
- currently diagnostic/candidate scaffold only
- not full first-principles closure yet
- not GR replacement
- no numerology claim
