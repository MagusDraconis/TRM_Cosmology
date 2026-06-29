# TRM V3.1 — Action-Based Memory Closure

## 1. Scope and claim boundary

This V3.1 document defines an **action-based derivation path** for the memory-channel structure
\[
\phi^2 |\dot{\mu}|
\]
using TQM lattice-response reasoning and unified-action consistency guards.

Claim boundary:

- This is **not** a theorem-level fundamental proof of \(\phi^2 |\dot{\mu}|\).
- This does **not** claim TRM replaces GR.
- This is a **derive-or-falsify closure path** built on existing weak-field guard evidence.

---

## 2. V3.0 status of the memory channel

V3.0 is frozen as archive baseline and already established:

- tested-effective memory path (MEM/TRM/MC blocks),
- strong support for \(A_{\mathrm{dyn}} \propto \phi\),
- strong support for \(A_{\mathrm{dyn}}^2 |\dot{\mu}| \leftrightarrow \phi^2 |\dot{\mu}|\) within an effective coupling window,
- no theorem-level microscopic closure.

So V3.1 does not restart Gap 1; it hardens the **action-level motivation** for why the quadratic memory channel is the first admissible term.

---

## 3. Why \(A_{\mathrm{dyn}} \propto \phi\) is the critical step

If a coarse-grained lattice coherence response obeys
\[
A_{\mathrm{dyn}}(x) \approx \kappa_A \phi(x),
\]
then an interaction built from \(A_{\mathrm{dyn}}\) can map directly into a \(\phi\)-power hierarchy without introducing free fit structures.

This bridge is the key reduction:

1. microscopic lattice response variable \(A_{\mathrm{dyn}}\),
2. effective weak-field potential \(\phi\),
3. memory-channel action term scaling.

Without this step, \(\phi^2 |\dot{\mu}|\) remains only phenomenological selection.

---

## 4. TQM lattice-response interpretation

Use lattice phases \(\theta_a\) with weak-field clock bias and nearest-neighbor coupling:
\[
\dot{\theta}_a
=
\omega_a
+\alpha \phi(x_a)
+\sum_{b \in \mathcal N(a)} K_{ab}\sin(\theta_b - \theta_a).
\]

The local coherence amplitude
\[
A(x)=\left|\frac{1}{|\mathcal W(x)|}\sum_{a \in \mathcal W(x)} e^{i\theta_a}\right|
\]
has baseline-subtracted dynamics \(A_{\mathrm{dyn}} = A - A_0\).  
In weak field, V3.1 tests target linear-response consistency of \(A_{\mathrm{dyn}}(\phi)\) under Newtonian Green-function potential scaling and failure under non-Newtonian kernels.

---

## 5. Candidate interaction action

A minimal memory interaction candidate is introduced as a sector term in the unified effective action:
\[
S_{\mathrm{mem}}
=
\int d^4x\;\lambda_{\mathrm{mem}}\; \mathcal I_{\mathrm{mem}}(A_{\mathrm{dyn}}, \kappa),
\quad
\kappa \equiv |\dot{\mu}|.
\]

Admissibility constraints:

1. \(A_{\mathrm{dyn}}\to 0 \Rightarrow \mathcal I_{\mathrm{mem}}\to 0\),
2. \(\kappa \to 0 \Rightarrow \mathcal I_{\mathrm{mem}}\to 0\),
3. local sign-symmetry around baseline coherence suppresses odd-\(A_{\mathrm{dyn}}\) leading terms.

These are structural constraints, not fitted assumptions.

---

## 6. Why \(A_{\mathrm{dyn}}^2 |\dot{\mu}|\) is the leading admissible term

Under the above constraints, the first even, turning-activated term is
\[
\mathcal I_{\mathrm{mem}}^{(2)} \sim A_{\mathrm{dyn}}^2 \kappa.
\]

With \(A_{\mathrm{dyn}} \propto \phi\), this yields
\[
A_{\mathrm{dyn}}^2 \kappa \;\Rightarrow\; \phi^2 |\dot{\mu}|.
\]

V3.1 therefore targets closure of the action-level chain:

\[
\text{lattice response} \rightarrow A_{\mathrm{dyn}}(\phi) \rightarrow S_{\mathrm{mem}}[A_{\mathrm{dyn}}^2 \kappa] \rightarrow \phi^2 |\dot{\mu}|.
\]

---

## 7. Why \(A_{\mathrm{dyn}} |\dot{\mu}|\) should fail or be forbidden

The linear candidate is expected to fail by at least one mechanism:

1. **Hierarchy failure:** scales too strongly relative to weak-field time-channel order.
2. **Symmetry rejection:** odd-in-\(A_{\mathrm{dyn}}\) contribution is not stable under sign-symmetric baseline fluctuations.

V3.1 treats this as a falsifiable guard statement, not as absolute theorem-level prohibition.

---

## 8. Why higher-order terms are subleading

Candidates such as
\[
A_{\mathrm{dyn}}^4\kappa,\quad
A_{\mathrm{dyn}}^2\kappa^2,\quad
A_{\mathrm{dyn}}^3\kappa
\]
remain admissible in principle but are expected to be weak-field subleading against \(A_{\mathrm{dyn}}^2\kappa\).

V3.1 tests enforce that these terms do not become the practical leading correction in the guarded weak-field domain.

---

## 9. V3.1 guard set (finalized)

Memory-channel block:

1. `MC13_LatticeClockBias_Should_Produce_LinearPhiResponse`
2. `MC14_CoherenceAmplitude_Should_Follow_GreenFunctionPotential`
3. `MC15_AphiScaling_Should_Break_When_ResponseKernelIsNonNewtonian`
4. `MC16_MemoryTermPowerCounting_Should_Select_PhiSquaredKappa`

Unified-action block:

1. `UF10_MemoryInteraction_Should_Yield_A2Kappa_As_LeadingTerm`
2. `UF11_LinearAInteraction_Should_Be_Rejected_By_HierarchyOrSymmetry`
3. `UF12_HigherOrderMemoryTerms_Should_Remain_Subleading_InWeakField`
4. `UF13_MemoryTerm_Should_Follow_From_Variation_Of_MinimalEffectiveAction`
5. `UF14_A2Kappa_Should_Be_StationaryLeadingInteraction_Under_WeakFieldExpansion`
6. `UF15_LinearAInteraction_Should_Vanish_Under_SymmetryAveraging`

All tests are derive-or-falsify guards with no new free fit-parameter introduction.

---

## 10. Falsification criteria

The V3.1 action-based closure path is rejected if any guard-consistent result shows:

1. no stable weak-field linear \(A_{\mathrm{dyn}}(\phi)\) response in the lattice proxy,
2. Newtonian Green-function potential does not map coherently to \(A_{\mathrm{dyn}}\),
3. non-Newtonian response kernels do **not** degrade the \(A\)-\(\phi\) closure relation,
4. \(A_{\mathrm{dyn}} |\dot{\mu}|\) survives hierarchy/symmetry guards as leading term,
5. higher-order terms become leading in weak field.

Passing these guards strengthens the closure path; it does not convert it into theorem-level proof.

---

## 11. Final V3.1 claim-safe status

Gap 1 status in V3.1:

- **action-derived memory-closure candidate**
- **not theorem-level first-principles closure**
- **no GR replacement claim**

Claim-safe statement:

> V3.1 strengthens Gap 1 to an action-derived memory-closure candidate. The \(\phi^2|\dot{\mu}|\) term is variationally consistent with a minimal effective action and supported by multiple independent guards. It remains short of theorem-level first-principles microscopic derivation.
