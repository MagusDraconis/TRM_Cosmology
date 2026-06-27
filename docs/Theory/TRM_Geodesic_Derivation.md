# TRM Geodesic Derivation

**Project:** Clockwork Cosmology V3 / Temporal Quantum Matrix (TQM)  
**Module:** TRM Photon Transport Model  
**Status:** Theory and repository document  
**Goal:** Derive TRM photon geodesics from the transport model, without using General Relativity as the starting point.

---

## 1. Starting Point

The current TRM photon model does not describe photon propagation primarily through a predefined curvature of spacetime. Instead, photon propagation is described by an effective transport index.

The working structure is:

\[
n_{\mathrm{eff}}
=
2
+
\lambda_t\phi
+
\lambda_s\phi^2 |\dot\mu|
\]

with:

\[
\phi(r)=\frac{GM}{c^2 r}
\]

and:

\[
\mu = \hat v\cdot \hat r
\]

\[
|\dot\mu| = \left|\frac{d}{dt}(\hat v\cdot \hat r)\right|
\]

Interpretation:

- \(\phi\) is the local time-rate / clock-channel.
- \(\phi^2 |\dot\mu|\) is the transport / direction / memory channel.
- The world-space remains fixed and Euclidean; effective curvature emerges from transport dynamics.

---

## 2. Variational Principle

The central assumption is:

> The physical photon path extremizes the optical transport time.

For a path element \(d\ell\), we write:

\[
dt = \frac{n_{\mathrm{eff}}}{c} d\ell
\]

Therefore, the travel time is:

\[
T = \frac{1}{c}\int n_{\mathrm{eff}}\,d\ell
\]

The TRM geodesic principle is:

\[
\boxed{
\delta \int n_{\mathrm{eff}}\,d\ell = 0
}
\]

This is the TRM analogue of a geodesic condition, but derived from transport rather than from a predefined metric.

---

## 3. Lagrangian Formulation

Parameterize the photon path by \(s\):

\[
\vec x = \vec x(s)
\]

The action is:

\[
S_{\mathrm{TRM}} = \int \mathcal L\,ds
\]

with:

\[
\mathcal L = n_{\mathrm{eff}}(\vec x, \dot{\vec x}, \ddot{\vec x})
\]

The key difference from a simple optical medium is important:

\[
n = n(\vec x)
\]

would be purely local. In TRM, however:

\[
n_{\mathrm{eff}}
=
2+
\lambda_t\phi(\vec x)
+
\lambda_s \phi(\vec x)^2 |\dot\mu|
\]

Since \(\dot\mu\) itself depends on directional change, the effective dynamics are higher-order and memory-like.

---

## 4. Decomposition of the Variation

Structurally, the variation is:

\[
\delta n_{\mathrm{eff}}
=
\lambda_t\delta\phi
+
\lambda_s\delta\left(\phi^2 |\dot\mu|\right)
\]

The first term gives:

\[
\delta\phi \rightarrow \nabla\phi
\]

The second term decomposes into:

\[
\delta\left(\phi^2 |\dot\mu|\right)
=
|\dot\mu|\,\delta(\phi^2)
+
\phi^2\,\delta |\dot\mu|
\]

or structurally:

\[
\delta\left(\phi^2 |\dot\mu|\right)
\rightarrow
|\dot\mu|\nabla(\phi^2)
+
\phi^2 \frac{d}{ds}(\dot\mu)
\]

This produces two different contributions:

1. a field-weighted gradient term,
2. a direction / memory term.

---

## 5. Structural TRM Geodesic Equation

In compact form, the transverse direction change can be written as:

\[
\boxed{
\frac{d\vec v}{ds}
\sim
-
\lambda_t\nabla\phi
-
\lambda_s
\left[
|\dot\mu|\nabla(\phi^2)
+
\phi^2\frac{d}{ds}(\dot\mu)
\right]_{\perp}
}
\]

The symbol \((\cdot)_\perp\) means that only the component orthogonal to the photon velocity contributes to direction change. This keeps the photon speed normalized:

\[
|\vec v| = c
\]

---

## 6. Interpretation of the Terms

### 6.1 Time / Shapiro Channel

\[
-
\lambda_t\nabla\phi
\]

This term represents the local time-rate gradient. It is the natural origin of the travel-time contribution and the Shapiro-like delay component.

### 6.2 Transport / Space Channel

\[
-
\lambda_s |\dot\mu|\nabla(\phi^2)
\]

This term couples field strength to directional change. It enhances direction dynamics where the field is strong and where the photon path rotates.

### 6.3 Dynamic Memory Term

\[
-
\lambda_s \phi^2\frac{d}{ds}(\dot\mu)
\]

This term is not purely local. It reacts to how the directional rotation changes along the trajectory. This is the mathematical source of the memory character.

---

## 7. Why \(\phi^2 |\dot\mu|\)?

The expression

\[
\phi^2 |\dot\mu|
\]

is structurally special because it satisfies three requirements at once:

1. It vanishes when the field is absent.
2. It vanishes when there is no directional change.
3. It couples field strength quadratically to transport rotation.

Therefore, this term is not merely a local index correction. It acts as a geometric transport channel.

In the variation, it generates exactly the two required contributions:

\[
|\dot\mu|\nabla(\phi^2)
\]

and

\[
\phi^2\frac{d}{ds}(\dot\mu)
\]

This makes \(\phi^2 |\dot\mu|\) a natural candidate for the missing spatial / curvature-like contribution in TRM photon transport.

---

## 8. Effective Metric Form

From the optical relation:

\[
dt = \frac{n_{\mathrm{eff}}}{c}d\ell
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
\frac{d\ell^2}
{\left(2+\lambda_t\phi+\lambda_s\phi^2|\dot\mu|\right)^2}
}
\]

Important:

This structure is not assumed as a fundamental GR metric. It is an effective transport metric emerging from photon propagation.

Since \(n_{\mathrm{eff}}\) depends on directional states, the resulting geometry is not only of the form \(g_{\mu\nu}(x)\), but rather:

\[
g_{\mu\nu}(x,v,\dot v)
\]

Structurally, this resembles a direction-dependent, Finsler-like or memory-like geometry.

---

## 9. Numerical Fixation in the Repository

The current implementation is protected by fixation tests:

- the effective index remains positive and finite,
- RK4 preserves \(|v|=c\),
- the local memory channel remains finite and non-negative,
- deflection decreases with larger impact parameter,
- the Shapiro diagnostic is scale-stable for a proportional integration domain,
- time and space channels add consistently.

These tests do not prove the full theory, but they prevent key model invariants from breaking unnoticed.

---

## 10. Executable Bridge-Derivation Track (EL/Fermat)

In addition to the transport RK4 path, the repository now contains an explicit executable Euler-Lagrange/Fermat bridge track in code and tests:

- `EL01_RK4EulerLagrange_Should_Preserve_PhotonSpeed`
- `EL02_Deflection_Should_Decrease_With_ImpactParameter`
- `EL03_Deflection_Should_Remain_Consistent_With_TransportRK4`
- `EL04_EulerAndTransport_Should_Be_Bounded_Against_SchwarzschildReference`

Interpretation:

- This does **not** close the full formal derivation chain yet.
- It does provide a **validated bridge** between the structural variational derivation and executable photon dynamics.
- Current status is therefore upgraded from "pure gap" to **partial executable derivation track**.

---

## 11. Status and Next Theoretical Task

Current status:

\[
\boxed{
\text{TRM geodesic principle formulated from the transport index}
}
\]

\[
\boxed{
\text{Time channel } \phi \text{ and space / memory channel } \phi^2|\dot\mu| \text{ separated}
}
\]

\[
\boxed{
\text{Repository tests are green and model invariants are fixed}
}
\]

Next theoretical step:

\[
\boxed{
\text{Micro-derivation of } \phi^2|\dot\mu|
\text{ from TQM phase / lattice dynamics}
}
\]

This should examine whether:

- the first \(\phi\) follows from local time rate,
- the second \(\phi\) follows from phase / lattice density,
- \(|\dot\mu|\) follows from wavefront rotation,
- and \(\lambda_s\) can be derived from a dimensionless coupling structure.

---

## Short Summary

The TRM geodesic follows from:

\[
\delta\int n_{\mathrm{eff}}d\ell=0
\]

with:

\[
n_{\mathrm{eff}}
=
2+
\lambda_t\phi+
\lambda_s\phi^2|\dot\mu|
\]

This leads structurally to:

\[
\frac{d\vec v}{ds}
\sim
-
\lambda_t\nabla\phi
-
\lambda_s
\left[
|\dot\mu|\nabla(\phi^2)
+
\phi^2\frac{d}{ds}(\dot\mu)
\right]_{\perp}
\]

Thus, geometry in TRM is not fundamental input. It emerges from transport, time rate, directional rotation, and memory.

---

## 12. Open Derivation Task: Collective Synchronization Cadence (`collectiveOmega = 20/17`)

The current EL bridge implementation uses a deterministic synchronization solver prior with

\[
\Omega_{\text{collective}} = \frac{20}{17}.
\]

This choice is currently operational and test-backed (via EL09-EL17), but not yet first-principles-derived.

### 12.1 What is already established

- The EL bridge scale is no longer set directly by photon-fit-only tuning.
- A synchronization-to-EL path exists and is executable.
- Robustness/ablation tests show bounded weak-field behavior for the current priorized setup.

### 12.2 What is still open

The unresolved theoretical question is whether \(\Omega_{\text{collective}}=20/17\) is:

1. an emergent mode-lock ratio of the underlying phase dynamics, or
2. a convenient but non-unique prior value.

### 12.3 Required derivation/constraint program

To elevate this from prior to derivation candidate, the following checks are required:

1. **Ratio competition:** evaluate neighboring rational cadences under identical constraints:
   \[
   \frac{19}{16},\ \frac{20}{17},\ \frac{6}{5},\ \frac{21}{18}
   \]
   and compare objective quality + robustness spread.
2. **Prior-strength ablation:** reduce prior-shaping weight and confirm that the preferred cadence does not collapse to arbitrary grid edges.
3. **Cross-regime stability:** repeat over \((\kappa,\ w_{\text{collective}},\ N_{\text{cells}})\) and require bounded variance of preferred cadence.

### 12.4 Decision rule for status upgrade

\[
\text{Upgrade to "emergent cadence"} \iff
\begin{cases}
\text{preferred over nearby ratios,}\\
\text{stable across ablations,}\\
\text{not dependent on narrow prior shaping.}
\end{cases}
\]

Until then, `collectiveOmega = 20/17` should be treated as a transparent, test-backed hypothesis prior.
