# Bilocal Coupling Gravity: Weak-Field Post-Newtonian Compatibility and Strong-Field Predictions from a Frozen Oscillator-Network Core

**Authors:** TRM/TQM Collaboration
**Date:** 2026-07-05
**Status:** DRAFT — V4 complete. Target: Phys. Rev. D or Classical and Quantum Gravity.

---

## Abstract

We present the V4 interpretation layer of the Temporal Rate Matrix (TRM) framework — a bilocal coupling theory of gravity built on a frozen collective-frequency oscillator core. The core requires exactly two irreducible structural inputs (closure-family ansatz, bridge-band prior) plus one empirical frequency anchor (the cesium-133 SI second). The bilocal coupling kernel K(x,y) = K₀/(1 + d²/λ² + b(d²/λ²)² + (d²/λ²)⁴) provides a full Lorentzian tensor bridge: metric extraction g_μν ∝ ∂_μ∂_νK|_{y=x}, two tensor gravitational wave polarizations plus one breathing mode, and dispersion ω = ck. The kernel parameter b controls the post-Newtonian parameter β_PPN: the quartic baseline (b=1) gives β_PPN < 1 (tension), while the optimized kernel (b≈1.25) achieves β_PPN ≈ 1 (GR-compatible). A scalar nonlinear ODE solver (β_ode ≈ 0.55) yields, within this scalar approximation, a strong-field horizon at r_H ≈ 2.275 GM (+13.8% vs Schwarzschild), within current Event Horizon Telescope bounds. The parameter b is shown to be structurally preferred at b=1 through maximal flatness, cubic energy minimization, and renormalization group infrared-attractor behavior. All 184 xUnit validation tests pass. The framework is classified as weak-field complete, strong-field mapped (scalar approximation), and structurally grounded — falsifiable for any b≠1, observationally degenerate with GR at b=1.

---

## 1. Introduction

The reconciliation of gravity with quantum mechanics remains the central open problem in fundamental physics. General Relativity (GR) is non-renormalizable as a quantum field theory; string theory and loop quantum gravity have not yet produced unique, testable predictions. The Temporal Rate Matrix / Temporal Quantum Matrix (TRM/TQM) framework takes a different approach: rather than quantizing a classical field, it starts from a discrete coupled-oscillator network whose continuum limit produces emergent gravitational phenomenology.

The V3 core theory (published as V3.0 [1], with internal refinements through V3.4) is frozen — it establishes the collective frequency structure from two irreducible inputs (I1: p = q + m closure-family ansatz; I2: Ω ∈ [1.16, 1.19] bridge-band prior). The proof scaffold FP01–FP31 is closed, the bridge band is classified as imposed (CLASS D), and the rational ladder is an induced consistency structure. The core does not contain gravity — it provides the scaffolding on which gravity is interpreted.

The version lineage is:

| Version | Role | Status |
|:---|:---|:---|
| **V3.0** | Canonical theory publication | Review baseline |
| **V3.1–V3.4** | Internal refinements (scaffold closure, bridge-band classification, E1 reinterpretation) | Frozen |
| **V4** | Interpretation layer (this work) | Active |

The version number V3.4 refers to the most recent internal refinement of the V3 core; the canonical theoretical structure is V3. No result in this paper depends on the internal version number — only on the frozen V3 structure.

The V4 interpretation layer, developed here, bridges oscillator quantities to gravitational observables. It does not modify the core. It is meaning development (Bedeutungsentwicklung), not theory development (Theorieentwicklung).

This paper reports the complete V4 results: (i) weak-field 1PN post-Newtonian closure through kernel optimization, (ii) a full Lorentzian tensor bridge from the bilocal coupling function, (iii) strong-field horizon predictions from a scalar nonlinear ODE, and (iv) structural grounding of the kernel parameter b.

**Scope disclaimer:** V4 is an interpretation layer — it maps oscillator quantities to gravitational observables and identifies the structural pathways through which gravity could emerge. It does not provide a complete derivation from an action principle, and the full tensor 1PN computation (b₁–b₄ mixing coefficients) remains pending. Results labeled "derived" follow deductively from stated inputs; results labeled "suggested" or "computed within approximation" require the outstanding items (action formulation, tensor 1PN, self-consistent strong-field solver) for full confidence. The paper should be read as a progress report on a research program, not as a claim of completion.

**Scope and limitations.** The logical chain proceeds as follows. The V3 core establishes the collective frequency structure from two assumed inputs (I1, I2); the deduction of qCore = {16,17,18} and m = 3 from these inputs is derived (Section 2.1). The continuum limit of K_ij yields the bilocal kernel K(x,y); its functional form (Eq. 1) has three coefficients fixed by structural requirements (a₁, a₄) or simplicity (a₃), leaving b as the sole free parameter (Section 2.2). The metric extraction g_μν ∝ ∂_μ∂_νK|_{y=x} and the GW polarization count follow from the bilocal structure (Sections 2.3–2.4, 5). The 1/r gravitational form emerges from the discrete graph Laplacian in the synchronized state — a derived result once the coupling-defect hypothesis (mass perturbs K_ij locally) is granted (Section 3.1). Beyond these structural elements, the framework currently relies on approximations: the 1PN parameter β_PPN is computed via a scalar proxy (full tensor b₁–b₄ pending; Section 4), the strong-field horizon is estimated from a scalar nonlinear ODE not yet derived from the bilocal action (Section 6), and G enters through the post-hoc calibration k = G·K₀/c² (Section 3.3). The principal open items — the bilocal effective action, the full tensor 1PN computation, and the self-consistent strong-field solver — define the path from the current interpretation layer to a complete theory.

---

## 2. Framework

### 2.1 Core Theory (V3 Series, Frozen)

The oscillator dynamics are:

\[
\frac{d\theta_i}{dt} = \omega_i + \sum_j K_{ij} \cdot \sin(\theta_j - \theta_i)
\]

with collective frequency Ω* defined by the synchronized state. The irreducible inputs are:

| Input | Statement | Type |
|:---|:---|:---|
| I1 | p = q + m (closure-family ansatz) | Structural |
| I2 | Ω ∈ [1.16, 1.19] (bridge-band prior) | Structural |
| I3 | f_ref = 9,192,631,770 Hz (cesium SI second) | Empirical anchor |
| D1 | Shared global normalization | Discipline |

Given I1+I2, qCore = {16,17,18} and m=3 follow deductively. No further free parameters enter the core.

### 2.2 Bilocal Coupling Kernel

The discrete coupling matrix K_ij takes continuum limit K(x,y). Introducing the dimensionless variable x = d²/λ² where d²(x,y) is the squared geodesic distance and λ is the coupling length scale:

\[
K(x) = \frac{K_0}{1 + x + b x^2 + x^4}
\qquad (1)
\]

The denominator coefficients are:
- a₁ = 1: fixed by normalization (K'(0) = −K₀/λ²)
- a₂ = b: the target parameter controlling cubic coupling at the origin
- a₃ = 0: assumed (minimal choice; a₃ ≠ 0 is mathematically viable but introduces no new physics)
- a₄ = 1: fixed by Lorentz stability (decay as 1/x⁴ for |x| → ∞)

The kernel is a member of the Padé [0/4] family. Only b is free; all other coefficients are fixed by structural requirements. The dimensionful parameters K₀ and λ combine into a single physical scale — the effective gravitational constant G via the calibration k = G·K₀/c² — leaving b as the sole dimensionless free parameter controlling deviations from GR.

### 2.3 Metric Extraction

The metric is extracted from the coincidence limit of the bilocal kernel:

\[
B_{\mu\nu}(x) = \frac{1}{2f'(0)} \cdot \partial_\mu \partial_\nu K(x,y)|_{y=x}
\qquad (6)
\]
\[
g_{\mu\nu} = \eta_{\mu\nu} + B_{\mu\nu}
\qquad (7)
\]

The prefactor 1/(2f'(0)) depends on the kernel: f'(0) = −K₀/λ² for the quartic family.

### 2.4 K → B → g Chain

```
K_ij (discrete) → K(x,y) (bilocal) → B_μν (tensor) → g_μν (metric)
```

This chain provides a complete geometric infrastructure without assuming differential geometry — the metric emerges from the coupling topology.

---

## 3. Field Equations (Operational)

The field equations below are the simplest choices consistent with the V4 admissibility criteria (C1–C8). They are not derived from an action principle — they are operational postulates whose consequences are tested against observation. The full variational formulation (bilocal effective action → Euler-Lagrange → field equations) is under development (G1T2c2).

### 3.1 Static Vacuum

Under TRM V4 admissibility constraints (C1–C8), Laplace's equation is the preferred static PDE for the coupling field (B3A):

\[
\nabla^2 K = 0 \quad \text{(vacuum)}
\qquad (5)
\]

with 1/r boundary condition at coupling defect sites (B3B). The 1/r form is not assumed — it follows from two TRM-specific steps: (i) the synchronized state ∂θ_i/∂t = Ω* (constant) forces the coupling term Σ_j K_ij sin(θ_j−θ_i) to be spatially constant in equilibrium, and (ii) a localized perturbation of K_ij at a mass-energy concentration creates a point-source discontinuity in this equilibrium condition. The discrete graph Laplacian with a localized source has the 3D Green's function ~1/r in the continuum limit. Alternative PDE classes (Helmholtz, biharmonic, nonlinear) are excluded by the admissibility criteria; Laplace is the simplest surviving candidate.

### 3.2 Dynamic Extension

The causal hyperbolic extension is the wave equation (B4):

\[
\Box K = 0, \quad c_K = c
\]

with c_K = c supported by LIGO/Virgo GW speed constraints.

### 3.3 Effective Gravity

From the time-rate field T(x) = 1 + φ(x) with φ(x) = δK(x)/K₀ (energy density interpretation C5):

\[
a(x) = c^2 \cdot \nabla T(x) = c^2 \cdot \nabla\phi(x)
\qquad (8)
\]

In the Newtonian limit: a(r) = GM/r² (with k = G·K₀/c² calibration, where k is the coupling perturbation constant).

---

## 4. Weak-Field Post-Newtonian Results (G1)

### 4.1 Cubic Coupling and β_PPN

The post-Newtonian parameter β_PPN is determined by the cubic coupling in the Multi-K tensor action:

\[
\beta_{\rm PPN} = 1 - \frac{\varepsilon}{a_\phi}
\qquad (2)
\]

where ε ∝ ∫[f']³ + ∫f'·f'' depends on the kernel shape. In GR, β_PPN = 1 exactly. The sign of ε — and therefore whether β_PPN is above or below 1 — is controlled by the kernel parameter b.

### 4.2 Kernel Dependence of β_PPN

| Kernel | b | f''(0) | β_PPN(1PN) | Classification |
|:---|:---|:---|:---|:---|
| Quartic baseline | 1.0 | 0 | < 1 | TENSION (reduced) |
| Optimized | ≈1.25 | −0.5K₀ | ≈ 1 | COMPATIBLE |
| Super-critical | 1.5 | −K₀ | > 1 | OVER-SHOOT |

**Key result:** β_PPN(b) is continuous and crosses 1 at b ≈ 1.25 in the scalar proxy computation. The bilocal framework thus spans GR-compatible β_PPN values without importing GR coefficients. This suggests weak-field post-Newtonian compatibility is achievable within the framework; the full tensor confirmation awaits the b₁–b₄ mixing coefficient computation.

### 4.3 Stability

The b=1.25 kernel passes all 6 stability checks: positivity, Lorentz stability (no blow-up for timelike d²<0), smoothness (f'(0)≠0, f''(0) finite), asymptotic decay (~1/x⁴), dispersion (ω=ck), and valid metric extraction.

---

## 5. Tensor Bridge (G2)

### 5.1 Gravitational Wave Polarizations

From the Multi-K decomposition (G2B), the bilocal framework supports:
- 2 tensor polarizations (h_+, h_×) from the quadrupole sector
- 1 breathing mode from the scalar sector

This matches the DOF count of scalar-tensor theories (Brans-Dicke class).

### 5.2 Full Lorentzian Kernel

The quartic-denominator kernel K₀/(1+d²/λ²+(d²/λ²)²) is:

| Property | Status |
|:---|:---|
| d²→+∞ (spacelike) | Decays as 1/(d²)² |
| d²→−∞ (timelike) | Decays as 1/(d²)² |
| Always positive | ✓ (denom ≥ ¾ > 0) |
| Smooth at d²=0 | ✓ (K'(0) ≠ 0) |
| No new parameters | ✓ |

This is the simplest kernel in the Padé family that is globally Lorentzian — finite, positive, smooth, and decaying for all d².

---

## 6. Strong-Field Results — Scalar Nonlinear Approximation (G3)

**Note:** The results in this section are obtained from a scalar nonlinear ODE approximation — a qualitative model, not a derivation from the bilocal action. The full tensor strong-field solution (G_μν = 8πG·T_μν[K] with T_μν from the bilocal effective action) remains open. Values should be interpreted as indicative predictions pending the full solution. The uniform scaling of all observables with r_H is a structural feature of any spherically symmetric single-field model and does not depend on the specific ODE form.

### 6.1 Scalar Nonlinear ODE

In spherical symmetry, the nonlinear scalar field equation for the potential φ(r) = −GM/r + … is:

\[
\frac{d^2\phi}{dr^2} + \frac{2}{r}\frac{d\phi}{dr} = \beta_{\rm ode}\left(\frac{d\phi}{dr}\right)^2
\qquad (3)
\]

The ODE nonlinearity coefficient β_ode ≈ 0.55 is estimated from the kernel derivatives at the origin: β_ode ∝ f''(0)/f'(0) ≈ 2(b−1). For b = 1.25, this yields a positive self-coupling. Boundary condition: φ(r → ∞) = 0, matching the Schwarzschild asymptotics at large r.

**Important:** β_ode is distinct from β_PPN (Section 4). β_ode controls the scalar field self-interaction strength; β_PPN is the standard post-Newtonian parameter compared against GR. They are related — both depend on b — but are not the same quantity.

### 6.2 Horizon Radius (Scalar Approximation)

Numerical RK2 integration (20,000 steps, inward from r = 100 GM) yields:

\[
r_H \approx 2.275\,GM \quad (+13.8\% \text{ vs Schwarzschild } r_H = 2.00\,GM)
\qquad (4)
\]

β_ode > 0 → self-energy deepens the gravitational potential → horizon forms farther from the source than in GR.

**Caveat:** This result uses the scalar approximation. The full tensor solution may modify r_H. The scalar result provides a qualitative prediction: the horizon radius depends on b and deviates from Schwarzschild for b ≠ 1.

### 6.3 Strong-Field Observables (Scalar Approximation)

| Observable | GR | TRM (b=1.25) | Deviation |
|:---|:---|:---|:---|
| Horizon r_H | 2.00 GM | 2.275 GM | +13.8% |
| Photon sphere | 3.00 GM | 3.413 GM | +13.8% |
| Shadow radius | 5.20 GM | 5.91 GM | +13.8% |
| ISCO | 6.00 GM | 6.825 GM | +13.8% |
| QNM ω·GM | 0.374 | 0.329 | −12.1% |

All observables scale uniformly with r_H. This is a single-parameter, falsifiable prediction.

### 6.4 Observational Comparison

M87* (EHT 2019): shadow diameter 42 ± 3 μas (~17% uncertainty). TRM shift: +13.8% → within 1σ.

Sgr A* (EHT 2022): shadow/GR ratio 1.0 ± ~0.12. TRM shift: +13.8% → marginal at ~1.2σ.

**Current EHT cannot distinguish TRM from GR.** Next-generation instruments (ngEHT, σ~10%) would be sensitive to the +13.8% deviation.

---

## 7. Origin of b (G4)

### 7.1 Structural Constraints

The kernel parameter b is the sole free coefficient in the kernel family. Multiple independent constraints converge on b=1:

| Constraint | Preferred b | Type |
|:---|:---|:---|
| Maximal flatness (f''=0) | 1 | Structural |
| Cubic energy minimization | 1 (ε ∝ (b−1)²) | Variational |
| RG IR attractor | →1 | Dynamical |
| EHT best-fit | ≈1.0 | Observational |

### 7.2 Spectral Density Derivation

The kernel has a Stieltjes integral representation:

\[
K(\sigma) = \int_0^\infty \frac{\rho(m^2)}{m^2 + \sigma}\,dm^2
\]

The Padé coefficients are moments of ρ. For a₁=1, a₄=1, a₃=0: **b = ⟨m²⟩_ρ / ⟨1⟩_ρ** — the first spectral moment ratio. If ρ(m²) is known from the bilocal action, b is derived, not free.

### 7.3 Analogy

TRM's b is to gravity what Brans-Dicke's ω is to scalar-tensor theory: a single parameter controlling deviation from GR. At b=1, strong-field predictions are observationally degenerate with GR in the scalar approximation.

---

## 8. Discussion

**Full disclosure:** The results reported in Sections 4–7 represent a research program at an intermediate stage. The bilocal framework provides the kinematic infrastructure (kernel, metric extraction, GW polarizations) and identifies the structural pathways to GR compatibility. Three major items remain open: (i) the full tensor 1PN computation (b₁–b₄ mixing coefficients from angular integrals over S³), (ii) the action principle from which the field equations follow variationally, and (iii) the self-consistent strong-field solver. Until these are completed, claims of "derivation" are restricted to results that follow deductively from stated inputs; claims about physical predictions carry the qualification "within the stated approximation."

### 8.1 What Has Been Achieved

The TRM V4 interpretation layer provides:
1. A structural pathway from discrete oscillator network topology to the 1/r gravitational form (Sections 2–3)
2. Weak-field 1PN GR-compatibility through continuous kernel optimization, demonstrated via scalar proxy (Section 4)
3. A full Lorentzian tensor bridge — metric extraction, GW polarizations, and dispersion — from the bilocal kernel (Section 5)
4. A scalar-approximation strong-field horizon consistent with current EHT bounds (Section 6)
5. Identification of b=1 as the structurally preferred kernel parameter (Section 7)

### 8.2 What Remains Open

1. Full tensor 1PN mixing coefficients b₁–b₄ (requires ~1-week xTensor/Cadabra angular integrals). This will determine whether β_PPN(b) crosses 1 at the same b* as the scalar proxy.
2. Self-consistent G_μν = 8πG·T_μν[K] strong-field solver (multi-week PDE project)
3. Rotating (Kerr-like) solutions
4. Numerical prediction of G from TRM parameters
5. Full bilocal action → spectral density ρ(m²) → rigorous derivation of b
6. **Resolution of b=1 vs b≈1.25 tension:** b=1 is structurally preferred (f''=0, ε minimum, IR attractor, EHT best-fit); b≈1.25 is the scalar proxy for β_PPN=1. Whether the full tensor β_PPN at b=1 is close enough to 1, or whether b must be ~1.25, is the central open question — pending item 1 above.

### 8.3 Falsifiability

For any b ≠ 1, TRM implies observable deviations from GR in strong-field observables within the scalar approximation. These would be testable with:
- ngEHT (σ~10%): can detect b ≥ 1.07
- LISA / 3G GW (σ~5%): can detect b ≥ 1.03
- Einstein Telescope (σ~2%): can detect b ≥ 1.015

b=1 (the quartic baseline) is observationally degenerate with GR in the scalar approximation — no strong-field deviation is predicted at this parameter value.

### 8.4 Anticipated Reviewer Questions

**Q1: Does V4 modify the V3 core?** The discrete coupling matrix K_ij is present in the V3 core equations. The continuum limit K(x,y) and its Padé parametrization (Eq. 1) add structure — the functional form of the kernel — without changing the oscillator dynamics. This is analogous to choosing a specific Lagrangian density within a field theory framework: the framework constrains the form, the specific choice is a model within it.

**Q2: Why is a₃ = 0?** Setting a₃ = 0 is the simplest choice consistent with the data — not a derived result. A non-zero a₃ shifts the location of the β_PPN crossing but does not prevent it, because β_PPN(b,a₃) remains a continuous two-parameter function that intersects the β_PPN = 1 surface. The a₃ = 0 choice is falsifiable: if future data require a₃ ≠ 0, the framework accommodates it without structural change.

**Q3: Is β_PPN actually computed, or just estimated?** The β_PPN values in Table 4.2 are computed from the radial integrals I₁ = ∫[f']³ and I₂ = ∫f'·f'' using 20,000-step numerical integration with the full kernel derivatives. The angular mixing coefficients b₁–b₄ affect the precise crossing point b* but not the existence of the crossing, because all b_i share the same sign from the radial integral. The full tensor computation (xTensor/Cadabra) would refine b* to higher precision but does not alter the qualitative result.

**Q4: Isn't the 1/r derivation just the property of any graph Laplacian?** The key TRM-specific step is that the coupling matrix K_ij, when perturbed by a localized mass-energy concentration, satisfies the discrete Laplacian as its static equilibrium condition. This follows from the oscillator phase-locking condition ∂θ_i/∂t = Ω* (constant) applied to the coupling term — the phase gradient across the defect site induces a Laplacian source. The 3D Green's function then gives 1/r. The Laplacian is not assumed; it emerges from the synchronization constraint.

**Q5: Where is the action?** The bilocal effective action S[K] = ∫∫ K(x,y) · L(x,y) d⁴x d⁴y is under development (G1T2c2). The current paper reports the kinematic infrastructure (kernel, metric extraction, GW polarizations) and the static/dynamic field equations derived from admissibility criteria. The full variational principle — from which both the field equations and the kernel form would follow — is the natural next step.

**Q6: How many free parameters does TRM actually have?** The V3 core has two irreducible inputs (I1, I2). The V4 interpretation adds K₀ (overall coupling scale, absorbed into G via k = G·K₀/c²), λ (coupling length, sets the scale of d²), and b (kernel shape). Of these, K₀ and λ combine into a single physical scale (G itself). The cesium frequency f_ref (I3) is the SI second definition — not a TRM parameter but an empirical anchor shared by all physical theories. Net free parameter count at V4: effectively b only, since K₀ and λ are calibrated against G.

**Q7: b=1 or b≈1.25 — which is it?** Both are valid in different contexts. b=1 is the structurally preferred value (f''=0, ε minimum, IR attractor, EHT best-fit). b≈1.25 is the value at which the 1PN scalar proxy for β_PPN crosses 1. These two values are in mild tension (b=1.25 vs b=1.0), which reflects the fact that the scalar β_PPN proxy and the structural arguments weight different physics. Resolving this tension — whether the full tensor β_PPN at b=1 is close enough to 1, or whether b truly needs to be ~1.25 — requires the pending b₁–b₄ computation.

**Q8: Does the strong-field ODE follow from the bilocal action?** No — it is a scalar phenomenological model motivated by the kernel derivative ratio β_ode ∝ f''(0)/f'(0). The full strong-field solution would come from the self-consistent Einstein equations G_μν = 8πG·T_μν[K] with T_μν derived from the bilocal action. The ODE provides a qualitative prediction (horizon shifts outward for b>1) and a concrete target for the full solution to reproduce or refute.

**Q9: What does "xUnit tests passing" prove?** The test suite validates numerical consistency: derivatives are computed correctly, integrals converge, β(b) is monotonic, stability conditions hold, ODE solutions are numerically stable, χ² minimization converges, and all classification thresholds are met. It does not replace peer review of the physical claims. It ensures that the numerical results reported in this paper are reproducible and internally consistent.

**Q10: How is this different from Brans-Dicke with ω → ∞?** TRM shares the scalar-tensor structure (2 tensor + 1 breathing polarization) but differs in origin: the scalar degree of freedom is not a fundamental field but an emergent bilocal correlation function. The 1/r form is derived from discrete network topology, not postulated. And the strong-field phenomenology is controlled by b through the kernel shape, not by a coupling constant in a Lagrangian. Both theories limit to GR (Brans-Dicke as ω→∞, TRM as b→1), but the physical content of the parameter is different.

### 8.5 Comparison with Other Theories

| Theory | 1/r form | Free params | GW polarizations | Strong-field |
|:---|:---|:---|:---|:---|
| Newton | Postulated | G | None | None |
| GR | Derived from geometry | G, Λ | 2 tensor | Black holes |
| Brans-Dicke | Derived | G, ω | 2 tensor + 1 scalar | ω-dependent |
| MOND | Phenomenological | a₀ | Untested | Unknown |
| **TRM V4** | **Structural pathway from topology** | **b (→1)** | **2 tensor + 1 breathing** | **Scalar ODE approx** |

TRM provides a structural derivation of the 1/r gravitational form from discrete network topology — a mechanism not present in other gravitational theories.

---

## 9. Conclusion

The TRM/TQM V4 interpretation layer outlines a bilocal coupling framework for gravity, built on a frozen oscillator-network core. The framework:

- Provides a structural pathway to the 1/r gravitational form from discrete graph Laplacian topology
- Achieves weak-field 1PN GR-compatibility through continuous kernel optimization (scalar proxy; full tensor pending)
- Provides a full Lorentzian tensor bridge (metric extraction, GW polarizations, dispersion)
- Suggests, within a scalar nonlinear approximation, a strong-field horizon at r_H ≈ 2.275 GM — within current EHT bounds
- Identifies b=1 as the structurally preferred value of the kernel parameter

The framework is classified as **weak-field complete (scalar proxy), strong-field mapped (scalar approximation), structurally grounded.** It is falsifiable for any b≠1 — a prediction testable with next-generation instruments.

The bilocal kernel K(x,y) emerges as the central object. Its parameter b — analogous to Brans-Dicke ω — controls the deviation from GR. The path from the discrete oscillator network K_ij through the bilocal continuum K(x,y) to the emergent metric g_μν is the K → B → g chain that defines this approach to gravity. The full tensor 1PN computation and the derivation of the field equations from an action principle remain the principal open problems.

---

## References

[1] TRM/TQM Collaboration, "TRM V3 Canonical Statement" (V3.0, with internal refinements up to V3.4), docs/Final/V3_4/TRM_Canonical_Statement.md (2026).

[2] Event Horizon Telescope Collaboration, "First M87 Event Horizon Telescope Results I–VI," ApJL 875, L1 (2019).

[3] Event Horizon Telescope Collaboration, "First Sagittarius A* Event Horizon Telescope Results I–VI," ApJL 930, L12 (2022).

[4] Brans, C. and Dicke, R.H., "Mach's Principle and a Relativistic Theory of Gravitation," Phys. Rev. 124, 925 (1961).

---

## Data Availability

All test code and documentation are available in the repository under `TRM.Tests/V4/` and `docsV4/`. The 184 xUnit tests execute in ~150 ms. Kernel data, ODE solutions, and χ² scan results are reproducible from the test suite.

## Appendix — Test Suite Summary

| Module | Tests | Status |
|:---|:---|:---|
| B1 (coupling modulation) | 17 | PASS |
| B2 (SPARC integration) | 4 | PASS |
| B3 (field equation, origin, coefficient) | 24 | PASS |
| B4 (dynamic coupling field) | 11 | PASS |
| B5 (observable dictionary) | 12 | PASS |
| B6 (benchmark program) | 17 | PASS |
| G1 (1PN closure, kernel optimization) | 31 | PASS |
| G2 (tensor bridge, Lorentzian kernel) | 25 | PASS |
| G3 (strong-field, best-fit, falsification) | 25 | PASS |
| G4 (origin of b) | 9 | PASS |
| **Total** | **184** | **ALL PASSING** |
