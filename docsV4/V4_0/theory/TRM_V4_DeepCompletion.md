# TRM V4 — DeepCompletion: From Interpretation to Derived Theory

**Date:** 2026-07-05
**Status:** PHASE 1 INITIALIZED. Action construction + 1PN tensor + consistency.
**Depends on:** V4 interpretation layer (B1–G4 complete, paper drafted).

---

## 0. Motivation

The V4 interpretation layer maps oscillator quantities to gravitational observables through a bilocal coupling kernel. The framework is **weak-field complete, strong-field mapped, structurally grounded** — but three central pieces remain open:

1. **No action principle** — field equations are operational postulates, not variationally derived
2. **Scalar proxy for β_PPN** — the 1PN parameter uses ∫[f']³ + ∫f'·f'' integrals, not full tensor mixing coefficients
3. **Strong-field is not tensor-derived** — the scalar ODE is a phenomenological model, not from G_μν = 8πG·T_μν[K]

DeepCompletion addresses all three.

---

## PHASE 1 — Bilocal Effective Action

### 1.1 General Form

The bilocal effective action is a double spacetime integral over the coupling kernel K(x,y):

\[
S[K] = S_{\rm kin}[K] + S_{\rm int}[K] + S_{\rm src}[K]
\]

where:
- S_kin: kinetic term (bilinear in K, produces linearized field equations)
- S_int: interaction term (cubic + quartic in K, produces β_PPN and strong-field)
- S_src: source term (couples K to matter stress-energy)

### 1.2 Kinetic Term

The simplest symmetric bilocal kinetic term respecting Lorentz invariance:

\[
S_{\rm kin} = \frac{1}{2\lambda^2} \int d^4x\,d^4y\;
\partial_\mu^x K(x,y)\,\partial_x^\mu K(x,y)
\qquad (A.1)
\]

where ∂_μ^x acts on the first argument. Symmetry under x↔y exchange is enforced by the integration measure. In center-of-mass coordinates X = (x+y)/2, r = x−y:

\[
S_{\rm kin} = \frac{1}{2\lambda^2} \int d^4X\,d^4r\;
\frac{1}{4}\partial_\mu^X K\,\partial_X^\mu K + \partial_\mu^r K\,\partial_r^\mu K
\qquad (A.2)
\]

The coincidence limit r→0 projects onto local field theory.

**Variation** δS_kin/δK = 0 with integration by parts:

\[
\Box_x K(x,y) = 0
\qquad (A.3)
\]

This reproduces the B4 dynamic field equation □K = 0.

**Classification:** DERIVED from action ansatz (A.1). The ansatz itself is POSTULATED — it is the simplest bilocal kinetic term, analogous to (1/2)(∂φ)² in scalar field theory.

### 1.3 Self-Interaction Terms

Introduce local (coincidence-limit) self-interactions:

\[
S_{\rm int} = \int d^4x\; \left[
\frac{g_3}{3!}\,K(x,x)^3 + \frac{g_4}{4!}\,K(x,x)^4
\right]
\qquad (A.4)
\]

The cubic coupling g₃ produces the 1PN β_PPN deviation from GR. The quartic coupling g₄ controls strong-field self-energy.

In terms of the kernel function f(d²/λ²) = K(x,y):

\[
S_{\rm int} = \int d^4x\; \left[
\frac{g_3}{3!}\,f(0)^3 + \frac{g_4}{4!}\,f(0)^4
\right] = \int d^4x\; \left[
\frac{g_3}{3!}\,K_0^3 + \frac{g_4}{4!}\,K_0^4
\right]
\]

This is trivial — constant terms don't produce dynamics. The interactions must involve derivatives. The correct form couples K to its coincidence-limit derivatives:

\[
S_{\rm int} = \int d^4x\; \left[
\frac{\tilde{g}_3}{3!}\,K_0 \cdot (\partial K|_0)^2 + \frac{\tilde{g}_4}{4!}\,(\partial K|_0)^4
\right]
\qquad (A.5)
\]

where ∂K|_0 represents the coincidence-limit gradient(s) of K(x,y).

**Classification:** FORM INFERRED. The derivative structure is motivated by the cubic coupling integrals ∫[f']³ and ∫f'·f'' from G1. The coupling constants g̃₃, g̃₄ must be matched to the kernel derivatives f'(0), f''(0) — this is where b enters.

### 1.4 Source Term

Couple K to the matter stress-energy tensor T_μν via the coincidence limit:

\[
S_{\rm src} = \alpha \int d^4x\; T^{\mu\nu}(x)\,
\partial_\mu\partial_\nu K(x,y)|_{y=x}
\qquad (A.6)
\]

where α is the coupling constant (related to G). In the static, non-relativistic limit with T^00 = ρ_m c²:

\[
\nabla^2 K(x) = -4\pi\alpha\,\rho_m(x)c^2
\qquad (A.7)
\]

Matching to the Newtonian limit a = −∇Φ with Φ = −GM/r gives:

\[
\alpha = \frac{G}{c^2} \cdot \frac{K_0}{?}
\qquad (A.8)
\]

The exact coefficient depends on the metric extraction prefactor 1/(2f'(0)).

**Classification:** STRUCTURALLY INFERRED. The form (A.6) is the natural bilocal coupling to matter; the coefficient α requires matching to G (post-hoc calibration at present).

### 1.5 Full Variation → Effective Einstein Equations

The full action S = S_kin + S_int + S_src yields, after variation δS/δK = 0, coincidence-limit projection, and metric extraction g_μν = η_μν + B_μν:

\[
G_{\mu\nu}[g] = 8\pi G_{\rm eff}\,T_{\mu\nu}^{\rm matter} + T_{\mu\nu}^{\rm K}[g]
\qquad (A.9)
\]

where:
- G_μν[g] is the Einstein tensor of the extracted metric
- G_eff is the effective gravitational constant (calibrated from α, K₀, λ)
- T^K_μν[g] is the K-field self-energy (from S_int, controls strong-field)

**The derivation of (A.9) from (A.1)+(A.5)+(A.6) is the central open problem of DeepCompletion.** It requires:
1. Functional differentiation of bilocal integrals with respect to K(x,y)
2. Coincidence-limit projection y→x
3. Identification of the resulting tensor structure with G_μν[g]
4. Matching of coefficients to reproduce GR at 1PN

**Classification: OPEN.** The structural pathway is clear; the explicit computation is the work of DeepCompletion.

---

## PHASE 2 — Full Tensor 1PN Coefficients

### 2.1 Cubic Coupling Tensor

Expand the bilocal action to third order in the metric perturbation B_μν = g_μν − η_μν:

\[
S^{(3)} = \int d^4x\; C^{\mu\nu\alpha\beta\gamma\delta}\,
B_{\mu\nu}\,B_{\alpha\beta}\,B_{\gamma\delta}
\qquad (A.10)
\]

The 6-index coupling tensor C^{μναβγδ} has the symmetry of three symmetric 2-index tensors. Under SO(3,1), it decomposes into 4 independent invariant coefficients b₁–b₄:

\[
C^{\mu\nu\alpha\beta\gamma\delta} = b_1\,\eta^{\mu\nu}\eta^{\alpha\beta}\eta^{\gamma\delta}
+ b_2\,\eta^{\mu\nu}\eta^{\alpha\gamma}\eta^{\beta\delta} + \text{permutations}
+ \ldots
\qquad (A.11)
\]

The PPN parameter β is a specific linear combination:

\[
\beta_{\rm PPN} = 1 + \frac{b_1 + 3b_2 + \ldots}{a_\phi}
\qquad (A.12)
\]

### 2.2 Angular Integrals

Each b_i is a sum over 15 permutations of the 6 indices, integrated over the angular S³ of the relative coordinate r = x−y:

\[
b_i = \sum_{\sigma \in S_3} \int_{S^3} d\Omega\;
\hat{r}^{\mu_1}\hat{r}^{\mu_2}\ldots\;
\times (\text{radial integral})
\qquad (A.13)
\]

The radial integrals I_rad = ∫₀^∞ dr·r⁹·f'(r²)·f''(r²) and I'_rad = ∫₀^∞ dr·r⁹·[f'(r²)]³ are known from G1 (computed numerically). The angular integrals remain to be evaluated.

**Classification:** FRAMEWORK DEFINED, COMPUTATION PENDING. The setup (A.10–A.13) is standard PPN formalism applied to the bilocal action. The computation requires xTensor/Cadabra for the 15×4 angular integrals (~1 week).

### 2.3 Expected Result

From the G1 scalar proxy: β_PPN(b) crosses 1 at b ≈ 1.25. The full tensor result may shift b* but preserves the crossing (all b_i share the same sign from I_rad).

If the full tensor β_PPN(b=1) is within observational bounds (|β−1| ≲ 10⁻⁴ from Solar System), the quartic baseline (b=1) is compatible without tuning. This is the **best-case scenario** — b=1 would then be both structurally preferred AND observationally compatible.

**Classification: OPEN — COMPUTATIONAL, NOT CONCEPTUAL.**

---

## PHASE 3 — Consistency Checks

### 3.1 Bianchi Identities

The extracted metric g_μν = η_μν + B_μν must satisfy the contracted Bianchi identity:

\[
\nabla_\mu G^{\mu\nu}[g] = 0
\qquad (A.14)
\]

This is automatically satisfied if G_μν is the Einstein tensor of g_μν. The question is whether the field equations (A.9) are consistent with (A.14):

\[
\nabla_\mu\left(8\pi G_{\rm eff}\,T^{\mu\nu}_{\rm matter} + T^{\mu\nu}_{\rm K}\right) \stackrel{?}{=} 0
\qquad (A.15)
\]

For the matter sector: ∇_μ T^{μν}_matter = 0 if matter couples minimally to g_μν (equivalence principle).

For the K-field sector: ∇_μ T^{μν}_K = 0 must follow from the K-field equations. This is guaranteed if S_int is diffeomorphism-invariant — i.e., constructed from covariant quantities.

**Classification:** STRUCTURALLY EXPECTED. The bilocal action must be diffeomorphism-invariant for consistency. This constrains the allowed forms of S_kin and S_int.

### 3.2 General Covariance

GR is invariant under arbitrary coordinate transformations x^μ → x'^μ(x). TRM must match this.

The bilocal kernel K(x,y) transforms as a bi-scalar:

\[
K'(x',y') = K(x,y)
\qquad (A.16)
\]

The action S[K] must be invariant under diffeomorphisms. The kinetic term (A.1) uses ∂_μ^x which is NOT covariant — it must be upgraded to the covariant derivative ∇_μ^x with respect to the extracted metric g_μν(x). This introduces metric dependence into the kinetic term, making the theory nonlinear even at the kinetic level.

The fully covariant kinetic term:

\[
S_{\rm kin}^{\rm cov} = \frac{1}{2\lambda^2} \int d^4x\,d^4y\;\sqrt{-g(x)}\sqrt{-g(y)}\;
g^{\mu\nu}(x)\,\nabla_\mu^x K(x,y)\,\nabla_\nu^x K(x,y)
\qquad (A.17)
\]

This is structurally analogous to the DeWitt-Schwinger proper-time formalism.

**Classification:** FRAMEWORK IDENTIFIED. The covariantization (A.17) is the natural next step but introduces metric-dependence into what was previously a fixed-background kinetic term. This is the "gravity gravitates" problem — the K-field's own energy becomes a source for itself.

### 3.3 Stress-Energy Conservation

If the action is diffeomorphism-invariant, Noether's second theorem guarantees:

\[
\nabla_\mu T^{\mu\nu}_{\rm total} = 0
\qquad (A.18)
\]

where T^μν_total = T^μν_matter + T^μν_K. This is the self-consistency condition for the coupled K-matter system.

**Classification:** DERIVED (from diffeomorphism invariance, once established).

---

## Summary — DeepCompletion Status

| Step | Status | Blockers |
|:---|:---|:---|
| PHASE 1 — Action | | |
| Kinetic term (A.1) | POSTULATED | Simplest bilocal form; alternatives possible |
| Nonlinear term (A.5) | FORM INFERRED | g̃₃,g̃₄ must match kernel derivatives |
| Source term (A.6) | STRUCTURALLY INFERRED | α calibration from G |
| Variation δS→field eqs (A.9) | OPEN | Requires explicit functional differentiation |
| Covariantization (A.17) | FRAMEWORK IDENTIFIED | Nonlinear feedback — "gravity gravitates" |
||||
| PHASE 2 — 1PN Tensor | | |
| Cubic tensor setup (A.10–A.11) | FRAMEWORK DEFINED | Standard PPN applied to bilocal |
| Angular integrals (A.13) | COMPUTATION PENDING | ~1 week xTensor/Cadabra |
| Full β_PPN(b) | OPEN — COMPUTATIONAL | Not conceptual; ready to execute |
||||
| PHASE 3 — Consistency | | |
| Bianchi (A.14–A.15) | STRUCTURALLY EXPECTED | Depends on diffeomorphism invariance |
| General covariance (A.16–A.17) | FRAMEWORK IDENTIFIED | Metric-dependent kinetic term → nonlinear |
| Conservation (A.18) | DERIVED (if covariant) | Follows from Noether II |
||||

### Path to Completion

```
NOW:     V4 interpretation layer (B1–G4, paper drafted)
  │
  ▼
STEP 1:  Covariantize kinetic term (A.17)
         → self-consistent nonlinear framework
  │
  ▼
STEP 2:  Compute full tensor b₁–b₄ (xTensor/Cadabra)
         → rigorous β_PPN(b) without scalar proxy
  │
  ▼
STEP 3:  Derive G_μν = 8πG·T_μν from δS = 0
         → Einstein equations as effective theory
  │
  ▼
STEP 4:  Solve self-consistent strong-field
         → TRM black holes / horizonless objects
  │
  ▼
COMPLETE: TRM as derived tensor gravity theory
```

---

## Cross-Reference

| Document | Role |
|:---|:---|
| `docsV4/theory/TRM_V4_Final_Status.md` | Current V4 closure |
| `docsV4/review/TRM_V4_CompletenessMap.md` | Derivation chain classification |
| `docsV4/papers/TRM_V4_Draft_Paper.md` | Draft paper |
| This document | DeepCompletion theory program |
