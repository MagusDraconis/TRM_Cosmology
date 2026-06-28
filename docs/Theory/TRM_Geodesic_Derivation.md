# TRM Geodesic Derivation

**Project:** Clockwork Cosmology V3 / Temporal Quantum Matrix (TQM)  
**Module:** TRM Photon Transport Model  
**Status:** Updated theory and repository document  
**Goal:** Formulate the current TRM photon-geodesic model from a transport/action viewpoint, without using General Relativity as the starting assumption.

---

## 1. Starting Point

The current TRM photon model describes photon propagation through an effective transport index rather than through a predefined curved spacetime metric.

The implemented working structure is:

\[
n_{\mathrm{eff}}
=
2
+\lambda_t\phi
+\lambda_s\phi^2|\dot{\mu}|
\]

with:

\[
\phi(r)=\frac{GM}{c^2r}
\]

and:

\[
\mu=\hat{v}\cdot\hat{r}
\]

\[
|\dot{\mu}|=\left|\frac{d}{ds}(\hat{v}\cdot\hat{r})\right|
\]

Interpretation:

- \(\phi\) is the local time-rate / clock-channel.
- \(\phi^2|\dot{\mu}|\) is the transport / direction / memory channel.
- The world-space remains fixed and Euclidean in the current formulation.
- Effective curvature-like behavior is interpreted as emerging from transport dynamics.

---

## 2. Variational Principle

The central assumption is:

> The physical photon path extremizes the optical transport time.

For a path element \(d\ell\), the optical relation is written as:

\[
dt=\frac{n_{\mathrm{eff}}}{c}\,d\ell
\]

Therefore, the travel-time functional is:

\[
T=\frac{1}{c}\int n_{\mathrm{eff}}\,d\ell
\]

The TRM geodesic principle is:

\[
\boxed{
\delta\int n_{\mathrm{eff}}\,d\ell=0
}
\]

This is the TRM analogue of a geodesic condition, but it is derived from transport rather than from a predefined spacetime metric.

---

## 3. Lagrangian / Optical-Action Formulation

Parameterize the photon path by \(s\):

\[
\vec{x}=\vec{x}(s)
\]

The action is:

\[
S_{\mathrm{TRM}}=\int \mathcal{L}\,ds
\]

with:

\[
\mathcal{L}=n_{\mathrm{eff}}(\vec{x},\dot{\vec{x}},\ddot{\vec{x}})
\]

A purely local optical medium would have:

\[
n=n(\vec{x})
\]

In TRM, however:

\[
n_{\mathrm{eff}}
=
2
+\lambda_t\phi(\vec{x})
+\lambda_s\phi(\vec{x})^2|\dot{\mu}|
\]

Since \(\dot{\mu}\) depends on directional change, the effective dynamics are higher-order and memory-like.

---

## 4. Finsler-Like / Higher-Order Optical Action Status

A standard Finsler-type structure has the form:

\[
F=F(x,\dot{x})
\]

The current TRM photon action is more naturally written as:

\[
F=F(x,\dot{x},\ddot{x})
\]

because the \(\phi^2|\dot{\mu}|\) channel depends on directional change.

Reviewer-facing statement:

> TRM photon transport admits a Finsler-like or higher-order optical-action formulation on a fixed Euclidean base space.

It should **not** yet be claimed that:

> TRM is a completed Finsler geometry.

For a higher-order Lagrangian of the form \(L(x,\dot{x},\ddot{x})\), the stationary-path condition yields:

\[
\frac{\partial L}{\partial x_i}
-
\frac{d}{ds}\frac{\partial L}{\partial \dot{x}_i}
+
\frac{d^2}{ds^2}\frac{\partial L}{\partial \ddot{x}_i}
=0
\]

This is the formal mathematical level required by the current TRM memory channel.

---

## 5. Decomposition of the Variation

Structurally, the variation of the implemented index is:

\[
\delta n_{\mathrm{eff}}
=
\lambda_t\delta\phi
+
\lambda_s\delta\left(\phi^2|\dot{\mu}|\right)
\]

The first term gives:

\[
\delta\phi\rightarrow\nabla\phi
\]

The second term decomposes as:

\[
\delta\left(\phi^2|\dot{\mu}|\right)
=
|\dot{\mu}|\,\delta(\phi^2)
+
\phi^2\,\delta|\dot{\mu}|
\]

or structurally:

\[
\delta\left(\phi^2|\dot{\mu}|\right)
\rightarrow
|\dot{\mu}|\nabla(\phi^2)
+
\phi^2\frac{d}{ds}(\dot{\mu})
\]

This produces two contributions:

- a field-weighted gradient term,
- a direction / memory term.

---

## 6. Structural TRM Geodesic Equation

In compact form, the transverse direction change can be written structurally as:

\[
\boxed{
\frac{d\vec{v}}{ds}
\sim
-
\lambda_t\nabla\phi
-
\lambda_s
\left[
|\dot{\mu}|\nabla(\phi^2)
+
\phi^2\frac{d}{ds}(\dot{\mu})
\right]_{\perp}
}
\]

The symbol \((\cdot)_\perp\) means that only the component orthogonal to the photon velocity contributes to direction change. This preserves the speed constraint:

\[
|\vec{v}|=c
\]

---

## 7. Interpretation of the Terms

### 7.1 Time / Shapiro Channel

\[
-
\lambda_t\nabla\phi
\]

This term represents the local time-rate gradient and is the natural origin of the travel-time / Shapiro-like contribution.

### 7.2 Transport / Space Channel

\[
-
\lambda_s|\dot{\mu}|\nabla(\phi^2)
\]

This term couples field strength to directional change. It becomes relevant where the field is nonzero and the photon path rotates.

### 7.3 Dynamic Memory Term

\[
-
\lambda_s\phi^2\frac{d}{ds}(\dot{\mu})
\]

This term reacts to how the directional rotation changes along the trajectory. It is the mathematical source of the current memory-like interpretation.

---

## 8. Why \(\phi^2|\dot{\mu}|\)?

The expression:

\[
\phi^2|\dot{\mu}|
\]

is structurally useful because it satisfies three requirements:

1. It vanishes when the field is absent.
2. It vanishes when there is no directional change.
3. It couples field strength quadratically to transport rotation.

Current status:

- The term is numerically useful in the EL/Fermat bridge behavior.
- MC01–MC08 complete the local invariant-selection block for this term.
- It is not yet derived from first principles.
- A full derivation should explain why the second factor of \(\phi\), the \(|\dot{\mu}|\) dependence, and the coupling \(\lambda_s\) arise from TQM phase/lattice dynamics.

---

## 9. Effective Transport Metric Form

From:

\[
dt=\frac{n_{\mathrm{eff}}}{c}\,d\ell
\]

one can formally write an effective line structure:

\[
ds^2_{\mathrm{TRM}}
=
c^2dt^2
-
\frac{d\ell^2}{n_{\mathrm{eff}}^2}
\]

therefore:

\[
\boxed{
ds^2_{\mathrm{TRM}}
=
c^2dt^2
-
\frac{d\ell^2}{\left(2+\lambda_t\phi+\lambda_s\phi^2|\dot{\mu}|\right)^2}
}
\]

Important clarification:

This structure is not assumed as a fundamental GR metric. It is an effective transport metric emerging from photon propagation.

Since \(n_{\mathrm{eff}}\) depends on directional states, the resulting geometry is not only of the form \(g_{\mu\nu}(x)\), but structurally closer to:

\[
g_{\mu\nu}(x,v,\dot{v})
\]

This motivates the Finsler-like / higher-order optical-action language.

---

## 10. Numerical Fixation in the Repository

The current implementation is protected by fixation and validation tests. These include:

- positivity and finiteness of the effective index,
- preservation of \(|v|=c\),
- finite and non-negative local memory channel,
- explicit \(\phi^2\)-scaling and \(|\dot{\mu}|\)-scaling checks for the local memory term,
- weak-field subleading behavior of the memory contribution relative to the linear time channel,
- completed local memory invariant-selection block (**MC01–MC08**),
- decreasing deflection with larger impact parameter,
- Shapiro diagnostic behavior under the current proportional integration domain,
- separation of time and space/memory channels,
- explicit higher-order transport-index dependency checks \((x,v,\dot{v})\) structure,
- EL/Fermat bridge comparison against transport RK4 and Schwarzschild null-reference,
- lower weak-field memory-improvement checks for the EL bridge path with explicit upper-boundary documentation,
- collective mode-locking bridge-scale tests,
- rational-band first-principles candidate tests, including constraint-ablation and competing-band comparisons,
- claim-boundary guard checks for the frame-dragging / Lense-Thirring non-coverage boundary.

These tests do not prove the full theory, but they prevent central model invariants from drifting unnoticed.

Reviewer-safe memory-channel status:

> TRM84–TRM87 establish local scaling guards for the memory channel: quadratic field dependence, linear directional-rotation dependence, weak-field subleading behavior, and separation from the pure time channel. MC01–MC08 complete the local invariant-selection block for the memory channel. This strengthens the current model role of \(\phi^2|\dot{\mu}|\), but it is not yet a complete first-principles derivation of that term.

---

## 11. Executable Bridge-Derivation Track: EL/Fermat

The repository contains an executable Euler-Lagrange/Fermat bridge track.

Current status:

- EL01–EL04 validate the EL/Fermat path against the transport RK4 path and a Schwarzschild null-geodesic reference.
- EL05–EL09 test the synchronization-to-EL bridge path.
- MEM01–MEM02 test the role and boundary behavior of the memory channel in the EL/Fermat bridge path.
- EL10–EL12 test robustness against grid choice, omega-prior variation, and parameter ablations.
- EL13–EL17 test cadence ratio competition, prior-weight transition behavior, and the current emergence boundary.

Interpretation:

- This does not close the full formal derivation chain yet.
- It provides a validated executable bridge between the structural variational derivation and photon dynamics.
- The bridge scale is no longer best described as a direct photon-fit-only parameter.
- It is constrained by the collective mode-locking/rational-band track described below.

---

## 12. Collective Mode-Locking BridgeScale

The current collective-mode track reframes the bridge-scale issue.

Instead of treating:

\[
\gamma\approx0.85
\]

as a single fitted constant, the current interpretation is that it is representative of an inverse rational collective mode-locking band:

\[
\Omega\approx1.16..1.19
\quad\Rightarrow\quad
\gamma=\frac{1}{\Omega}\approx0.84..0.86
\]

The relevant CML result is:

- CML05 removes the explicit cadence prior.
- CML06–CML07 show a competitive rational cluster/band.
- CML08 maps the competitive band through \(\gamma=1/\Omega\) into the EL/Fermat weak-field bridge window.

Reviewer-safe statement:

> The EL bridge scale is consistent with an inverse rational collective mode-locking band. The value \(\gamma\approx0.85\) should not be presented as an isolated fitted constant, but as a representative point inside a robust collective locking window.

---

## 13. Rational-Band First-Principles Candidate

The current RBF track asks:

\[
\boxed{
\text{Why does }\Omega\approx1.16..1.19\text{ emerge?}
}
\]

The current candidate structure is the phase-closure family:

\[
q\Omega-p=0
\]

with:

\[
p=q+m
\]

therefore:

\[
\Omega=\frac{q+m}{q}
\]

The tested candidate is:

\[
m=3
\]

so:

\[
\Omega=\frac{q+3}{q}
\]

For \(q=16..19\), this reproduces the observed rational bridge band.

Current RBF interpretation:

- RBF05 provides the explicit closure-family candidate \(\Omega=(q+3)/q\).
- RBF06 shows \(m=3\) as bridge-band competitive.
- RBF07 shows \(m=3\) as a balance mode between band occupancy and action/EL cost.
- RBF08 shows q-window robustness.
- RBF09 adds closure-quality to the compromise.
- RBF10 confirms robustness under score-weight ablation.
- RBF11 proposes \(m=3\) as the minimal mode satisfying three operational closure constraints.
- RBF12 shows threshold-ablation robustness: in all resolved cases, \(m=3\) remains the minimal satisfying mode.
- RBF13 shows a non-uniform constraint role: \(m=3\) remains minimal when phase or direction constraints are removed individually, but collapses to \(m=2\) when action/tick consistency is removed.
- RBF14 shows a locking-vs-EL tradeoff across neighboring rational bands: the lower band can lock better, the upper band can reduce EL error, while the current primary band remains the balanced bridge candidate.
- RBF15 adds a derive-or-falsify boundary check: \(m=3\) is uniquely selected under the full three-constraint set, but the selection becomes non-unique and collapses to minimal \(m=2\) when action/tick is removed.

Reviewer-safe statement:

> The current evidence supports \(m=3\) as a robust first-principles candidate closure order, but not yet as a formal uniqueness theorem.

---

## 14. Current Status and Claim Boundary

Current status:

\[
\boxed{\text{TRM geodesics are formulated as effective transport extremals.}}
\]

\[
\boxed{\text{The EL/Fermat bridge path is executable and weak-field validated.}}
\]

\[
\boxed{\text{The bridge scale is supported by a rational collective locking band.}}
\]

\[
\boxed{\text{The }m=3\text{ closure family is a strong candidate, not yet a theorem.}}
\]

Claims to avoid:

- TRM fully replaces General Relativity.
- The memory term is already derived from first principles.
- \(\lambda_s\) is fundamental.
- \(\gamma\approx0.85\) is a uniquely derived constant.
- \(m=3\) is already a proven fundamental closure index.
- The scalar TRM layer covers frame-dragging / Lense-Thirring effects.

---

## 15. Next Theoretical Tasks

To move from a **band-supported effective model** to a **first-principles-derived theory**, the next phase should be executed as explicit work packages with pass/fail outcomes.

### 15.1 Derive \(\phi^2|\dot{\mu}|\) from Microscopic TQM Structure

Goal:

> Obtain the memory/transport term from phase-lattice dynamics rather than inserting it phenomenologically.

Working derivation track:

1. **Microscopic variables**  
   Introduce a local phase state \(\theta_a\) on a TQM lattice site \(a\), with nearest-neighbor coupling and a baryonic source-dependent clock bias:

\[
\dot{\theta}_a
=
\omega_0
+
\alpha\phi(x_a)
+
\sum_{b\in\mathcal{N}(a)}K\sin(\theta_b-\theta_a)
\]

where \(\phi(r)=GM/(c^2r)\) is the same scalar used in the transport layer.

2. **Directional transport observable**  
   Define directional phase-slip density along the photon trajectory:

\[
\kappa(s)
\equiv
\left|\frac{d}{ds}(\hat{v}\cdot\hat{r})\right|
=
|\dot{\mu}|
\]

and identify \(\kappa\) as the coarse transport-rotation observable.

3. **Coarse-grained synchronization amplitude**  
   Let \(A(x)\) denote local phase-coherence amplitude from the lattice order parameter. First closure ansatz for the weak-field regime:

\[
A(x)\propto\phi(x)
\]

This is to be tested, not assumed exact outside weak field.

4. **Second-order transport coupling**  
   The lowest non-vanishing scalar that couples coherence strength to directional rotation is:

\[
\Delta n_{\mathrm{mem}}
\propto
A^2\kappa
\]

which yields:

\[
\Delta n_{\mathrm{mem}}
\propto
\phi^2|\dot{\mu}|
\]

after inserting the weak-field closure \(A\propto\phi\).

5. **Effective index map**  
   The coarse-grained transport index then takes the operational form:

\[
n_{\mathrm{eff}}
=
2+
\lambda_t\phi+
\lambda_s\phi^2|\dot{\mu}|
\]

with \(\lambda_s\) interpreted as an effective lattice-response coefficient, not yet fundamental.

Acceptance requirements:

- The derivation must explain why the coupling is non-negative in dissipative/phase-mixing sectors, hence \(|\dot{\mu}|\) rather than signed \(\dot{\mu}\) at effective level.
- The scaling must remain subleading to the linear \(\phi\) channel in weak-field time-delay regimes, but non-negligible in bridge-sensitive transport-rotation regimes.
- The memory term must not reproduce pure Shapiro-like behavior under channel ablation; it must retain transport-specific signatures.

Failure boundaries:

- If coarse-graining gives \(A\not\propto\phi\) in the relevant regime, the \(\phi^2\) factor is not justified.
- If the leading allowed coupling is linear \(A\kappa\) or another invariant, \(\phi^2|\dot{\mu}|\) loses first-principles priority.
- If the same term can be reabsorbed into a pure local time channel, the memory-channel interpretation fails.

### 15.2 Derive-or-Falsify Track for \(m=3\) Closure

Goal:

> Test whether \(m=3\) is uniquely implied by microscopic closure/topology constraints.

Current formalization status:

- Operational derive-or-falsify boundary is implemented in RBF15.
- The formal closure track is documented in `docs/Theory/TRM_M3_Closure_First_Principles.md`.

Minimum deliverables:

- admissibility conditions for closure families \(\Omega=(q+m)/q\), or equivalent microscopic form,
- a uniqueness test showing whether only \(m=3\) survives under the same constraints,
- a falsification branch: if multiple \(m\)-families remain admissible, document non-uniqueness and keep \(m=3\) as a robust candidate only.

### 15.3 Close the Higher-Order Optical Action Mathematically

Goal:

> Upgrade the current structural action to a formally controlled higher-order variational model.

Minimum deliverables:

- a precise Lagrangian class \(L(x,\dot{x},\ddot{x})\) with stated regularity assumptions,
- the corresponding Euler-Lagrange structure and reduction limits to the currently tested weak-field bridge behavior,
- a clear boundary between mathematically proven structure and numerically supported ansatz terms.

### 15.4 Keep Review-Safe Claim Layers Explicit

Use and maintain three strict claim layers in all theory/review documents:

- **Layer A:** tested bridge behavior — EL/CML/RBF evidence.
- **Layer B:** candidate first-principles structure — supported but not unique.
- **Layer C:** completed derivation/theorem level — not yet reached.

Upgrade from Layer B to Layer C only when uniqueness or falsification criteria are explicitly passed.

### 15.5 Theta-Observable Closure Track

Goal:

\[
\Theta(r)\rightarrow g_{\mathrm{obs}}(r)
\]

Current formalization status:

- The first-principles track is documented in `docs/Theory/TRM_Theta_Observable_First_Principles.md`.
- The current bridge status remains effective/test-supported, not yet uniqueness-derived.

---

## Short Summary

The TRM geodesic model starts from:

\[
\delta\int n_{\mathrm{eff}}d\ell=0
\]

with:

\[
n_{\mathrm{eff}}
=
2
+
\lambda_t\phi
+
\lambda_s\phi^2|\dot{\mu}|
\]

leading structurally to:

\[
\frac{d\vec{v}}{ds}
\sim
-
\lambda_t\nabla\phi
-
\lambda_s
\left[
|\dot{\mu}|\nabla(\phi^2)
+
\phi^2\frac{d}{ds}(\dot{\mu})
\right]_{\perp}
\]

Current tests support the model as a weak-field effective transport framework with an executable EL/Fermat bridge and a rational collective bridge-scale band.

The open core problem is no longer whether \(\gamma\approx0.85\) works numerically. The open problem is why the rational band, and especially the \(m=3\) closure family, should emerge from the microscopic TRM/TQM structure.
