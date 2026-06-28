# TRM Memory Channel Microscopic Derivation (Gap 1)

## Scope and status

This document defines a **derive-or-falsify** path for Gap 1:

\[
\phi^2|\dot{\mu}|
\]

Current status from existing tests and implementation:

- **tested-effective / hypothesis-supported:** MEM01–MEM02, TRM84–TRM87, MC01–MC12, HOA01
- **strongly supported derivation path:** MC09–MC12 support \(A_{\mathrm{dyn}}\propto\phi\), admissibility of \(A_{\mathrm{dyn}}^2|\dot{\mu}|\), and bridge-level substitution consistency up to an effective coupling scale
- **not theorem-level:** no microscopic closure proof yet

Reference surfaces used: `TRM_Memory_Channel_Derivation_Attempt.md`, `TRM_Geodesic_Derivation.md`, `TRM_Finsler_Optical_Action.md`, `PhotonTransportModel.cs`.

---

## Core question

Can TQM phase-lattice coherence produce
\[
A \propto \phi
\]
so that
\[
A^2|\dot{\mu}| \Rightarrow \phi^2|\dot{\mu}| \, ?
\]

---

## 1) Microscopic lattice variables

Let \(\theta_a\) be phase variables on lattice sites \(a\).

Use a weak-field local clock-bias form:
\[
\dot{\theta}_a
=
\omega_0
+\alpha\,\phi(x_a)
+\sum_{b\in\mathcal N(a)}K_{ab}\sin(\theta_b-\theta_a)
+\xi_a,
\]
with \(\xi_a\) denoting noise/higher-order residual terms.

For small phase differences:
\[
\sin(\theta_b-\theta_a)\approx \theta_b-\theta_a.
\]

---

## 2) Local coherence amplitude \(A(x)\)

Define local order/coherence amplitude from a window \(\mathcal W(x)\):
\[
Z(x)=\frac{1}{|\mathcal W(x)|}\sum_{a\in\mathcal W(x)}e^{i\theta_a},
\qquad
A(x)=|Z(x)|.
\]

Weak-field coarse-graining target:
\[
A(x)\approx A_0+\kappa_A\phi(x),
\]
and after subtracting baseline/background:
\[
A_{\text{dyn}}(x)\approx \kappa_A\phi(x).
\]

This is the required bridge step \(A\propto\phi\) (to be derived or falsified).

---

## 3) Directional transport observable

Use the directional-rate scalar along path parameter \(s\):
\[
\mu(s)=\hat v(s)\cdot\hat r(s),
\qquad
\kappa(s)=\left|\frac{d}{ds}(\hat v\cdot\hat r)\right|=|\dot{\mu}|.
\]

This matches the current transport-channel observable in photon transport tests.

---

## 4) Why the lowest admissible memory coupling is \(A^2\kappa\)

Assume coarse-grained correction:
\[
\Delta n_{\text{mem}} = F(A,\kappa).
\]

Admissibility constraints:
1. no-memory limit at zero coherence: \(A\to0 \Rightarrow \Delta n_{\text{mem}}\to0\),
2. no-turning limit: \(\kappa\to0 \Rightarrow \Delta n_{\text{mem}}\to0\),
3. sign-symmetric coherence fluctuation near local synchronization state (no odd linear bias in \(A\) around baseline-subtracted dynamics).

Then leading admissible term is:
\[
\Delta n_{\text{mem}} \sim c_2 A^2\kappa + O(A^4\kappa, A^2\kappa^2).
\]

Insert \(A_{\text{dyn}}\approx\kappa_A\phi\):
\[
\Delta n_{\text{mem}}
\sim
c_2\kappa_A^2\phi^2|\dot{\mu}|
\equiv
\lambda_s\phi^2|\dot{\mu}|.
\]

---

## 5) Why nearby alternatives fail (derive-or-falsify logic)

### a) \(\phi|\dot{\mu}|\)

Fails weak-field hierarchy in current guard windows (MC/TRM block): too close to time-channel order; loses subleading separation behavior.

### b) \(\phi^2|\dot{\mu}|^2\)

Over-penalizes low-turning regime and shifts leading behavior to higher directional order; not the first admissible correction if linear \(|\dot{\mu}|\) already satisfies positivity/separation.

### c) \(\phi^3|\dot{\mu}|\)

Too high field order for leading weak-field correction; suppressed beyond observed tested-effective window and not selected as first admissible term in current invariant block.

These are tested-effective selection results, not theorem-level proofs.

---

## 6) Explicit falsification criteria

This derivation path fails if any of the following occurs:

1. \(A \not\propto \phi\) in weak-field coarse-grained lattice response,
2. \(A\kappa\) is shown admissible and dominant as first correction (then \(\phi^2\) loses priority),
3. \(\Delta n_{\text{mem}}\) can be reabsorbed into a pure time-channel reparameterization (memory interpretation fails),
4. lattice reduction gives a different leading invariant than \(A^2\kappa\).

---

## 7) Completed derivation-gate tests (MC09–MC12)

1. `MC09_CoherenceAmplitude_Should_Scale_With_Phi_From_Lattice`  
   Supports weak-field lattice-proxy linear response \(A_{\mathrm{dyn}}\propto\phi\) in the tested window.
2. `MC10_QuadraticCoherenceCoupling_Should_Be_First_Admissible_MemoryInvariant`  
   Supports \(A|\dot{\mu}|\) as hierarchy-violating and \(A^2|\dot{\mu}|\) as first admissible memory order in the tested window.
3. `MC11_DerivedMemoryInvariant_Should_Match_PhotonTransport_Form`  
   Supports strong linear proportionality between \(A_{\mathrm{dyn}}^2|\dot{\mu}|\) and \(\phi^2|\dot{\mu}|\) up to an effective coupling scale (\(R^2=0.999799\)).
4. `MC12_DerivedMemoryInvariant_Should_Reproduce_ELBridge_When_Substituted`  
   Supports dynamic bridge consistency when substituting \(A_{\mathrm{dyn}}^2|\dot{\mu}|\) for \(\phi^2|\dot{\mu}|\) in the EL/Fermat bridge path (\(\text{bridgeRetention}=0.957676\), \(\text{meanGap}=1.64\times 10^{-4}\)).

These remain derivation-gate results, not theorem-level closure.

---

## Claim boundary (mandatory)

Do not claim fundamental closure yet:

> Gap 1 is strongly supported by lattice-proxy derivation tests MC09–MC12.  
> \(\phi^2|\dot{\mu}|\) is no longer only an effective invariant selection; it has a tested microscopic derivation path up to an effective coupling scale.  
> It is not yet theorem-level first-principles-derived.
>
> Claim-safe shorthand: MC09–MC12 strongly support a lattice-proxy derivation path for the memory channel \(\phi^2|\dot{\mu}|\) up to an effective coupling scale. This is not yet theorem-level microscopic closure.
