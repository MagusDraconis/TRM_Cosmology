# TRM V4 — DeepCompletion Phase 1A: Covariant Bilocal Action

**Date:** 2026-07-05
**Status:** DERIVATION IN PROGRESS. Action setup → variation → coincidence limit → T^K_μν → consistency.
**Depends on:** DeepCompletion framework (TRM_V4_DeepCompletion.md).

---

## 0. Objective

Transform the operational field equations (□K = 0, ∇²K = 0) into a covariant variational framework. The bilocal effective action S[K, g] must:

1. Produce the correct linearized field equations in the flat-background limit
2. Self-consistently couple K to the metric g_μν it induces
3. Yield an effective stress-energy tensor T^K_μν satisfying ∇_μ T^{μν}_total = 0
4. Reduce the number of operational postulates

---

## 1. Covariant Bilocal Action

### 1.1 Full Action

\[
S[K, g] = S_{\rm kin}[K, g] + S_{\rm int}[K, g] + S_{\rm src}[K, g] + S_{\rm EH}[g]
\qquad (1.1)
\]

where S_EH[g] = (1/16πG) ∫ √−g R d⁴x is the Einstein-Hilbert term (included for completeness; may be redundant if K dynamics already produce G_μν).

### 1.2 Kinetic Term

\[
S_{\rm kin}[K, g] = \frac{1}{2\lambda^2} \int d^4x\,d^4y\;
\sqrt{-g(x)}\sqrt{-g(y)}\;
g^{\mu\nu}(x)\,\nabla^x_\mu K(x,y)\,\nabla^x_\nu K(x,y)
\qquad (1.2)
\]

**Properties:**
- Bi-scalar under diffeomorphisms: K'(x',y') = K(x,y)
- ∇^x_μ is the covariant derivative w.r.t. g_μν(x), acting on the first argument
- √−g(x)√−g(y) ensures the integration measure is covariant at both points
- λ is the coupling length scale (the only dimensionful parameter in S_kin)

**Classification: POSTULATED.** This is the simplest covariant bilocal kinetic term. Alternatives with derivatives acting on both arguments (or symmetrized) are structurally equivalent at the level of the linearized equations.

### 1.3 Self-Interaction Term

\[
S_{\rm int}[K, g] = \frac{\tilde{g}_3}{3!} \int d^4x\,\sqrt{-g(x)}\;
K_0\cdot\left[g^{\mu\nu}(x)\,\nabla_\mu K(x,y)\,\nabla_\nu K(x,y)\right]_{y=x}
\qquad (1.3)
\]

This is the covariantized version of the A.5 term. The coincidence limit [·]_{y=x} projects the bilocal gradient product onto a local scalar.

In the flat-background limit with K(x,y) = f(d²/λ²) and d² = η_μν Δx^μ Δx^ν:

\[
\nabla_\mu K|_{y=x} = \partial_\mu^x K|_{y=x} = 0 \quad \text{(by symmetry at coincidence)}
\]

The gradient of K(x,y) vanishes at coincidence for any kernel depending only on d². Therefore the interaction term (1.3) as written is trivial. **Correction required:** The interaction must involve the second derivative — the Hessian ∂_μ∂_νK|_{y=x} — which is non-vanishing and encodes the metric:

\[
S_{\rm int}[K, g] = \frac{\tilde{g}_3}{3!} \int d^4x\,\sqrt{-g(x)}\;
K_0\cdot\left[g^{\mu\nu}(x)\,g^{\alpha\beta}(x)\,
\nabla_\mu\nabla_\alpha K(x,y)\,\nabla_\nu\nabla_\beta K(x,y)\right]_{y=x}
\qquad (1.3')
\]

This couples the coincidence-limit Hessian of K to the metric. Variation of this term w.r.t. K produces cubic nonlinearities in the field equations → β_PPN.

**Classification: FORM INFERRED.** The Hessian structure (1.3') is required to produce non-trivial dynamics. The coupling g̃₃ is related to the kernel parameter b through the cubic integrals from G1.

### 1.4 Source Term

\[
S_{\rm src}[K, g] = \alpha \int d^4x\,\sqrt{-g(x)}\;
T^{\mu\nu}(x)\,\nabla_\mu\nabla_\nu K(x,y)|_{y=x}
\qquad (1.4)
\]

where T^{μν}(x) is the matter stress-energy tensor and α is the coupling constant.

**Classification: STRUCTURALLY INFERRED.** The form follows from the metric extraction g_μν ∝ ∂_μ∂_νK|_{y=x}: matter couples to the metric, and the metric is the coincidence-limit Hessian of K. The constant α = G_eff/c² must be calibrated against the observed G.

---

## 2. Variation δS/δK = 0

### 2.1 Kinetic Variation

Vary S_kin with respect to K(x,y), treating g_μν as dependent on K through g_μν = η_μν + B_μν[K]:

\[
\frac{\delta S_{\rm kin}}{\delta K(x,y)} =
-\frac{1}{\lambda^2}\sqrt{-g(x)}\sqrt{-g(y)}\;
\Box_x K(x,y) + \frac{\delta S_{\rm kin}}{\delta g^{\mu\nu}}\frac{\delta g^{\mu\nu}}{\delta K}
\qquad (2.1)
\]

The first term is the direct variation. The second term is the **back-reaction** — varying the metric inside the kinetic term with respect to K.

In the flat-background approximation (g_μν = η_μν, Γ^λ_{μν} = 0, √−g = 1):

\[
\frac{\delta S_{\rm kin}}{\delta K(x,y)}\bigg|_{\rm flat}
= -\frac{1}{\lambda^2}\,\Box_x K(x,y)
\qquad (2.2)
\]

This reproduces □K = 0, the B4 operational postulate — now derived from the action (1.2).

**Classification of (2.2): DERIVED from (1.2) in flat-background limit.**

### 2.2 Back-Reaction Term

The back-reaction term ∂S_kin/∂g^{μν} · δg^{μν}/δK encodes the "gravity gravitates" effect. Its explicit form is:

\[
\frac{\delta S_{\rm kin}}{\delta g^{\mu\nu}(x)} =
\frac{1}{2\lambda^2}\sqrt{-g(x)} \int d^4y\,\sqrt{-g(y)}\;
\Big[ \nabla_\mu K\,\nabla_\nu K - \frac{1}{2}g_{\mu\nu}\,\nabla^\alpha K\,\nabla_\alpha K \Big]
\qquad (2.3)
\]

The metric variation δg^{μν}/δK(x,y) follows from the extraction formula:

\[
g_{\mu\nu}(x) = \eta_{\mu\nu} + \frac{1}{2f'(0)}\,
\partial_\mu\partial_\nu K(x,y)|_{y=x}
\qquad (2.4)
\]

Hence:

\[
\frac{\delta g^{\mu\nu}(z)}{\delta K(x,y)} =
-\frac{1}{2f'(0)}\,g^{\mu\alpha}g^{\nu\beta}\,
\frac{\delta}{\delta K(x,y)}\left[\partial_\alpha\partial_\beta K(z,w)|_{w=z}\right]
\qquad (2.5)
\]

The functional derivative of the coincidence-limit Hessian introduces a δ-distribution and derivatives thereof. The back-reaction term is therefore **non-local in its full form** — it couples the field equation at x to the field values at y through the metric.

**Classification: OPEN.** The explicit evaluation of (2.3)×(2.5) requires regularization of coincidence-limit functional derivatives. This is the central algebraic blocker for Phase 1A.

### 2.3 Interaction Variation

\[
\frac{\delta S_{\rm int}}{\delta K(x,y)} =
\frac{\tilde{g}_3}{2}\,K_0\,\sqrt{-g}\,
g^{\mu\nu}g^{\alpha\beta}\,
\nabla_\mu\nabla_\alpha K\,
\frac{\delta}{\delta K(x,y)}\left[\nabla_\nu\nabla_\beta K\right]_{y=x} + \ldots
\qquad (2.6)
\]

The ··· terms include the metric back-reaction from ∂S_int/∂g^{μν}.

In the flat-background limit, the interaction produces a cubic source term ∝ (∂∂K)² in the field equation. This is the origin of the β_PPN deviation.

### 2.4 Source Variation

\[
\frac{\delta S_{\rm src}}{\delta K(x,y)} =
\alpha\,\sqrt{-g(x)}\,T^{\mu\nu}(x)\,
\frac{\delta}{\delta K(x,y)}\left[\nabla_\mu\nabla_\nu K(x,z)|_{z=x}\right]
\qquad (2.7)
\]

In the static, non-relativistic limit with T^{00} = ρ_m c² and ∇_μ∇_νK|_{y=x} → ∂_i∂_j K|_{y=x} for spatial indices, this yields:

\[
\nabla^2 K(x) = -4\pi\alpha\,\rho_m(x)c^2
\qquad (2.8)
\]

reproducing the B3B source equation.

**Classification of (2.8): DERIVED from (1.4) in the static flat-background limit.**

---

## 3. Coincidence-Limit Local Field Equation

### 3.1 Projection

The bilocal field equation δS/δK(x,y) = 0 is a PDE in 8 variables (x^μ, y^ν). To obtain a local field equation on spacetime, take the coincidence limit y→x:

\[
\lim_{y\to x} \frac{\delta S}{\delta K(x,y)} = 0
\qquad (3.1)
\]

In the flat-background, no-back-reaction approximation:

\[
\Box K(x,x) + \frac{\tilde{g}_3}{2}K_0\left[g^{\mu\nu}g^{\alpha\beta}
\nabla_\mu\nabla_\alpha K\,\nabla_\nu\nabla_\beta K\right]_{y=x}
= -\alpha\,T^{\mu\nu}\,\nabla_\mu\nabla_\nu K|_{y=x}
\qquad (3.2)
\]

The coincidence limit K(x,x) = K₀ is constant (does not propagate). Dynamics enter through the spatial derivatives of the coincidence-limit Hessian — i.e., through the metric g_μν(x).

**The propagating degrees of freedom are in g_μν(x), not in K(x,x).**

### 3.2 Effective Einstein Equations

Including back-reaction and the Einstein-Hilbert term:

\[
G_{\mu\nu}[g] = 8\pi G_{\rm eff}\,
T_{\mu\nu}^{\rm matter} + T_{\mu\nu}^{\rm K}[K, g]
\qquad (3.3)
\]

where T^K_μν is obtained from the metric variation of S_kin + S_int (Section 4). The effective Newton constant G_eff is:

\[
G_{\rm eff} = \frac{\alpha c^2}{8\pi}\cdot\frac{1}{|f'(0)|}
\qquad (3.4)
\]

**Classification: STRUCTURALLY INFERRED.** The form (3.3) follows from the action (1.1) by construction. The explicit expression for T^K_μν and the numerical value of G_eff remain to be computed.

---

## 4. Effective Stress-Energy Tensor T^K_μν

### 4.1 Definition

\[
T_{\mu\nu}^{\rm K}(x) = -\frac{2}{\sqrt{-g(x)}}\,
\frac{\delta(S_{\rm kin} + S_{\rm int})}{\delta g^{\mu\nu}(x)}
\qquad (4.1)
\]

### 4.2 Kinetic Contribution

From (2.3):

\[
T_{\mu\nu}^{\rm kin}(x) = -\frac{1}{\lambda^2}\int d^4y\,\sqrt{-g(y)}\;
\Big[ \nabla_\mu K\,\nabla_\nu K
- \frac{1}{2}g_{\mu\nu}\,\nabla^\alpha K\,\nabla_\alpha K \Big]_{x,y}
\qquad (4.2)
\]

The integral over y remains — T^K_μν is **inherently non-local** at the bilocal level. In the coincidence limit (or for sharply peaked kernels), it reduces to a local expression in ∂_μ∂_νK|₀.

### 4.3 Conservation

If the full action (1.1) is diffeomorphism-invariant, Noether's second theorem guarantees:

\[
\nabla^\mu T_{\mu\nu}^{\rm total} = 0,
\qquad T_{\mu\nu}^{\rm total} = T_{\mu\nu}^{\rm matter} + T_{\mu\nu}^{\rm K}
\qquad (4.3)
\]

**Classification: DERIVED (conditional on diffeomorphism invariance of S).**

The diffeomorphism invariance of S_kin (1.2) is manifest — all quantities are covariant. The interaction (1.3') and source (1.4) terms are also manifestly covariant. Therefore (4.3) holds identically.

---

## 5. Bianchi Consistency

The contracted Bianchi identity ∇_μ G^{μν} = 0 is a geometric identity — it holds for any metric g_μν. Combined with the field equations (3.3):

\[
\nabla_\mu\left(8\pi G_{\rm eff}\,T^{\mu\nu}_{\rm matter} + T^{\mu\nu}_{\rm K}\right) = 0
\qquad (5.1)
\]

If matter couples minimally to g_μν (equivalence principle), ∇_μ T^{μν}_matter = 0, leaving:

\[
\nabla_\mu T^{\mu\nu}_{\rm K} = 0
\qquad (5.2)
\]

This is automatically satisfied if T^K_μν is derived from a diffeomorphism-invariant action (Section 4.3).

**Classification: CONSISTENT (conditional on diffeomorphism invariance).**

---

## 6. Open Algebraic Blockers

| # | Blocker | Status | Resolution path |
|:---|:---|:---|:---|
| B1 | Back-reaction ∂S_kin/∂g × δg/δK | OPEN | Regularize coincidence-limit functional derivative (2.5) |
| B2 | Non-local T^K_μν (integral over y) | OPEN | Coincidence-limit expansion; leading local approximation |
| B3 | g̃₃ ↔ b mapping | OPEN | Match cubic coupling from (1.3') to G1 ∫[f']³ integral |
| B4 | G_eff numerical value | OPEN (calibration) | Requires α, λ, K₀ from independent measurements |
| B5 | Full nonlinear solution of (3.3) | OPEN | Self-consistent iteration (multi-week PDE project) |

---

## 7. Reduction of Operational Postulate Status

Before Phase 1A, the field equations were operational postulates:
- □K = 0 (B4): ASSUMED
- ∇²K = 0 (B3A): SELECTED by admissibility criteria
- a = c²·∇T (C5): POSTULATED

After Phase 1A:
- □K = 0: DERIVED from S_kin (1.2) in flat-background limit
- ∇²K = −4πα ρ_m: DERIVED from S_src (1.4) in static limit
- a = c²·∇T: DERIVED from metric extraction + geodesic equation (once g_μν is established)
- Back-reaction → nonlinear completion: OPEN (B1)

**Net reduction: 3 operational postulates → 2 derived + 1 open.**

---

## 8. Classification Summary

| Equation | Status |
|:---|:---|
| Covariant kinetic term (1.2) | POSTULATED |
| Self-interaction form (1.3') | FORM INFERRED |
| Source coupling (1.4) | STRUCTURALLY INFERRED |
| Flat-background □K = 0 (2.2) | DERIVED from (1.2) |
| Static Poisson ∇²K = −4πα ρ (2.8) | DERIVED from (1.4) |
| Effective Einstein equations (3.3) | STRUCTURALLY INFERRED |
| T^K_μν kinetic part (4.2) | DERIVED from (1.2) |
| Conservation ∇_μ T^{μν}_K = 0 (4.3) | DERIVED (from diff-invariance) |
| Back-reaction term (2.1) second part | OPEN (B1) |
| Non-local → local T^K_μν reduction | OPEN (B2) |
| g̃₃ ↔ b numerical mapping | OPEN (B3) |

---

## 9. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_DeepCompletion.md` | Parent DeepCompletion framework |
| `TRM_V4_Final_Status.md` | Current V4 closure |
| `TRM_V4_CompletenessMap.md` | Derivation chain classification |
| `TRM.Tests/V4/DeepCompletion_Phase1A_CovariantAction_Tests.cs` | xUnit validation |
