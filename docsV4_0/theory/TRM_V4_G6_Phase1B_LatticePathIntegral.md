# TRM V4 — G6 Phase1B: Lattice Path Integral and Quantum Bilocal Mapping

**Date:** 2026-07-05
**Status:** FORMULATED. Discrete path integral defined. Continuum bridge to bilocal action established. Ready for 1-loop execution.
**Depends on:** G6 concept, G6 Phase1A (UV propagator), V3 core.

---

## 0. Objective

Formulate the Euclidean path integral for the discrete TRM oscillator network and establish the mapping to the continuum bilocal action S[K]. This is Subproblem B.1 of the G6 quantum gravity program.

---

## 1. Discrete Oscillator Hamiltonian

### 1.1 Phase and Momentum Variables

The quantum oscillator at site i has phase operator θ̂_i and conjugate momentum p̂_i:

\[
[\hat{\theta}_i, \hat{p}_j] = i\hbar\,\delta_{ij}
\]

The Hamiltonian:

\[
\hat{H} = \sum_i \frac{\hat{p}_i^2}{2I}
+ \sum_{i,j} K_{ij}\,[1 - \cos(\hat{\theta}_i - \hat{\theta}_j)]
\qquad (1)
\]

where I is the moment of inertia (related to the intrinsic frequency ω_i) and K_ij is the coupling matrix.

**Classification: DERIVED from V3 core.** The quantum XY model with coupling K_ij is the standard quantization of the classical oscillator network.

### 1.2 Classical Continuum Limit

In the classical continuum limit (a → 0, N → ∞):

\[
\theta_i \to \phi(x),\quad
p_i \to \pi(x)\,a^3,\quad
K_{ij} \to K(x,y)
\]

\[
H \to \int d^3x\left[\frac{\pi(x)^2}{2\rho}
+ \frac{1}{2}\int d^3y\,K(x,y)\,[\nabla\phi(x)\cdot\nabla\phi(y)]\right]
\]

The cosine expands to leading order: 1 − cos(Δθ) ≈ (∇ϕ·a)²/2.

---

## 2. Euclidean Path Integral

### 2.1 Discrete Action

Wick-rotate t → −iτ. The Euclidean action for N oscillators on a lattice with spacing a:

\[
S_E[\{\theta_i(\tau)\}] = \int_0^\beta d\tau\;
\sum_i \left[\frac{I}{2}\left(\frac{d\theta_i}{d\tau}\right)^2
+ \sum_j K_{ij}\,(1 - \cos(\theta_i - \theta_j))\right]
\qquad (2)
\]

where β = 1/(k_B T) is the inverse temperature (β → ∞ for the ground state).

### 2.2 Path Integral

\[
Z = \int \prod_i \mathcal{D}\theta_i(\tau)\;
\exp\left(-S_E[\theta_i]\right)
\qquad (3)
\]

with periodic boundary conditions θ_i(0) = θ_i(β) + 2πn_i (n_i ∈ ℤ, winding numbers).

**The measure is the flat product measure over phase variables.** No Faddeev-Popov determinant — phases are compact U(1) variables, not gauge fields.

### 2.3 Lattice Spacing as UV Regulator

The lattice has finite spacing a > 0. Fourier modes are restricted to the Brillouin zone |k| ≤ π/a. The path integral (3) is manifestly finite for any finite N and β — no UV divergences.

**Classification: STRUCTURAL. The finiteness of the discrete path integral follows from the compactness of the integration domain (compact phase space × finite lattice).**

---

## 3. Continuum Limit → Bilocal Action

### 3.1 Coarse-Graining

At scales much larger than a, the discrete phases are approximated by a continuum field. The coupling matrix K_ij becomes the bilocal kernel K(x,y):

\[
\sum_{i,j} K_{ij}\,\cos(\theta_i - \theta_j)
\to \int d^3x\,d^3y\;K(x,y)\,
\cos(\phi(x) - \phi(y))
\]

### 3.2 Gradient Expansion

For slowly varying ϕ(x), expand ϕ(y) ≈ ϕ(x) + (y−x)·∇ϕ + …:

\[
\cos(\phi(x) - \phi(y)) \approx 1 - \frac{1}{2}[(y-x)\cdot\nabla\phi]^2
\]

The spatial part of the action becomes:

\[
S_{\rm space} \approx \frac{1}{2}\int d^3x\,d^3y\;
K(x,y)\,[(y-x)\cdot\nabla\phi(x)]\,[(y-x)\cdot\nabla\phi(y)]
\]

### 3.3 Bilocal Kinetic Term

Including the temporal derivative and taking the relativistic limit (c = a/δτ → finite as a,δτ → 0):

\[
S_E[\phi] \to \frac{1}{2\lambda^2}\int d^4x\,d^4y\;
\partial_\mu\phi(x)\,K(x,y)\,\partial^\mu\phi(y)
\qquad (4)
\]

where λ carries the dimension of the coupling scale. This is structurally identical to the bilocal kinetic term of Phase 1A, with ϕ in place of K.

**Classification: STRUCTURALLY INFERRED.** The gradient expansion and continuum limit follow standard procedures from lattice field theory. The specific form of K(x,y) as K₀/(1+d²/λ²+...) is the Padé ansatz — POSTULATED, not derived.

### 3.4 What Is Derived vs. Postulated

| Step | Classification |
|:---|:---|
| Discrete Hamiltonian (1) | DERIVED (quantized V3 core) |
| Euclidean action (2) | DERIVED (Wick rotation) |
| Path integral measure (3) | DERIVED (flat measure for U(1) phases) |
| Continuum limit a→0 | STRUCTURAL (standard lattice → continuum) |
| Gradient expansion | DERIVED (Taylor expansion, valid for slow fields) |
| Bilocal kinetic form (4) | STRUCTURALLY INFERRED (from K_ij continuum limit) |
| Padé [0/4] kernel ansatz | POSTULATED (effective model assumption) |
| Covariantization (g_μν enters) | POSTULATED (Phase 1A ansatz) |

---

## 4. First Quantum Correction

### 4.1 Perturbation Theory

Split ϕ = ϕ_cl + δϕ, where ϕ_cl is the classical solution. The quadratic action for fluctuations:

\[
S_E^{(2)}[\delta\phi] = \frac{1}{2}\int d^4x\,d^4y\;
\delta\phi(x)\,\mathcal{O}(x,y)\,\delta\phi(y)
\]

where O(x,y) = −□_x K(x,y)/λ² (in flat background).

### 4.2 1-Loop Effective Action

\[
\Gamma_{\rm 1-loop}[\phi_{\rm cl}] = S_E[\phi_{\rm cl}]
+ \frac{1}{2}\text{Tr}\ln\mathcal{O}
\]

The functional trace is:

\[
\text{Tr}\ln\mathcal{O} = \int \frac{d^4k}{(2\pi)^4}\,
\ln\left[\frac{k^2}{\lambda^2}\,\mathcal{F}(k^2 a^2)\right]
\]

where F(k²a²) is the kernel form factor from G6 Phase1A.

### 4.3 UV Finiteness

The form factor F(k²a²) ∼ 1/(k²a²)² provides UV suppression:

\[
\int d^4k\,\ln\mathcal{F}(k^2 a^2) \sim \int dk\,k^3\,\ln(k^{-4})
\sim -4\int dk\,k^3\ln k
\]

This integral converges with the form factor — no UV divergence in the 1-loop effective action.

**Classification: CONCEPTUAL. The power-counting argument is sound; explicit evaluation of the functional trace is the next step (Subproblem B.2).**

---

## 5. Classification Map

| Result | Classification | Ready for computation? |
|:---|:---|:---|
| Discrete path integral Z (Eq. 3) | DERIVED | ✅ |
| Continuum limit → bilocal form (Eq. 4) | STRUCTURALLY INFERRED | ✅ |
| Padé kernel ansatz | POSTULATED | ✅ (parameterized) |
| 1-loop effective action | FORMULATED | ✅ (B.2) |
| UV finiteness (1-loop) | POWER-COUNTING CONFIRMED | ✅ (B.2 for explicit) |
| Quantum spectral positivity | OPEN | ⬜ (B.4) |
| Continuum limit existence (phase transition) | OPEN | ⬜ (B.3) |

---

## 6. Recommended Next Step

**B.2 — 1-Loop Graviton Propagator** (6 months, HIGH difficulty)

With the path integral formulated and the bilocal mapping established, the next executable step is explicit 1-loop computation:
- Feynman rules from S_int (Phase 1A, Eq. 10)
- Vertex factors in momentum space with form factors
- 1-loop graviton self-energy Π_{μναβ}(k)
- Verify UV finiteness explicitly
- Extract running of G_eff and b

---

## 7. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_G6_QuantumGravity_Concept.md` | Parent program + subproblem definitions |
| `TRM_V4_G6_Phase1A_UVPropagator.md` | UV power counting |
| `TRM_V4_DeepCompletion_Phase1A_CovariantAction.md` | Bilocal action |
| `TRM.Tests/V4/G6_Phase1B_LatticePathIntegral_Tests.cs` | xUnit validation |
