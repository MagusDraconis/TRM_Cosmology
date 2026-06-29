# TRM V3.2 — Minimal Action from TQM Lattice

## 1. Scope and claim boundary

This V3.2 document defines a **candidate derivation path** for a minimal effective action from TQM lattice/synchronization principles.

Claim boundary:

- no GR replacement claim,
- no theorem-level closure claim,
- status: candidate derivation of minimal effective action.

---

## 2. V3.1 status recap

V3.1 hardened Gap 1 to an action-derived memory-closure candidate:
\[
A_{\mathrm{dyn}} \propto \phi
\rightarrow
A_{\mathrm{dyn}}^2|\dot{\mu}|
\rightarrow
\phi^2|\dot{\mu}|.
\]

The result is strongly guard-supported and variation-compatible, but remains below theorem-level first-principles microscopic closure.

---

## 3. TQM lattice variables and synchronization energy

Use lattice phases \(\theta_a\) on sites \(a\), with local synchronization energy proxy:
\[
E_{\mathrm{lat}}
\sim
\sum_{\langle a,b\rangle} K_{ab}\bigl(\theta_a-\theta_b\bigr)^2.
\]

In weak field, clock-bias/source coupling enters phase dynamics through \(\phi\), while coherence amplitude is captured by a coarse variable \(A_{\mathrm{dyn}}\).

---

## 4. From phase-lattice energy to coarse-grained action

Coarse-graining target:
\[
S_{\mathrm{eff}}

\sim
\int d^4x\;\Bigl(
\alpha_T(\nabla T)^2
+\beta_A A_{\mathrm{dyn}}^2
+\lambda_2 A_{\mathrm{dyn}}^2\kappa
+\cdots
\Bigr),\qquad \kappa\equiv |\dot{\mu}|.
\]

V3.2 objective is not to assert uniqueness, but to establish that a minimal weak-field action ansatz is structurally consistent with lattice-energy reduction and existing guard behavior.

---

## 5. Why quadratic coherence terms appear

Near synchronization baseline, odd-in-\(A_{\mathrm{dyn}}\) leading terms are symmetry-suppressed under sign-symmetric fluctuations, while even terms survive.

Therefore \(A_{\mathrm{dyn}}^2\)-order terms are expected as first stable coherence contributions in the weak-field expansion.

---

## 6. Why the minimal interaction yields \(A_{\mathrm{dyn}}^2|\dot{\mu}|\)

With turning activation \(\kappa=|\dot{\mu}|\) and coherence-even leading order, the minimal memory interaction is:
\[
\mathcal I_{\mathrm{mem}}^{\mathrm{min}} \sim A_{\mathrm{dyn}}^2\kappa.
\]

Combined with \(A_{\mathrm{dyn}}\propto\phi\), this recovers:
\[
A_{\mathrm{dyn}}^2|\dot{\mu}|
\Rightarrow
\phi^2|\dot{\mu}|.
\]

This is candidate-level action derivation support, not theorem-level closure.

---

## 7. Relation to scalar \(T\), vector \(\vec A_T\), and \(\Theta\) sectors

The minimal-action candidate must remain compatible with established UF/MC/FD/TO limits:

1. scalar-sector recovery in decoupling limits,
2. vector weak-field scaling guards without hidden retuning,
3. theta/O5 relaxation guards without anti-proxy violations,
4. memory-term hierarchy preservation.

V3.2 guards treat this as derive-or-falsify compatibility, not completed unification proof.

---

## 8. Proposed V3.2 tests

1. `UA16_LatticeEnergy_Should_Reduce_To_MinimalScalarAction`
2. `UA17_CoarseGrainedAction_Should_Preserve_A2Kappa_Interaction`
3. `UA18_MinimalAction_Should_Reproduce_UF13_To_UF15_Without_Retuning`
4. `UA19_NonMinimalActionTerms_Should_Be_Penalized_Or_Subleading`
5. `UA20_MinimalAction_Should_Preserve_MC_FD_TO_Limits`

All tests are guard-style and claim-safe.

---

## 9. Falsification criteria

The V3.2 candidate path fails if any guard-consistent result shows:

1. lattice-energy reduction cannot reproduce minimal scalar action behavior,
2. \(A_{\mathrm{dyn}}^2\kappa\) no longer tracks the transport-form hierarchy,
3. UF13–UF15 behavior requires retuning of core coefficients,
4. nonminimal terms become leading in weak field,
5. MC/FD/TO limit guards are degraded.

---

## 10. Remaining theorem-level gaps

Open items after V3.2 candidate-level hardening:

1. theorem-level first-principles derivation of the minimal effective action from microscopic TQM lattice dynamics,
2. theorem-level uniqueness/necessity proof for leading interaction structure,
3. full theorem-level closure across all first-principles gaps.

These remain explicitly open; no theorem-level or GR-replacement overclaim is made.
