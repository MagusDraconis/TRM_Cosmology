# TRM V4 — Completeness Map

**Date:** 2026-07-05
**Purpose:** Classify each step in the oscillator → observables chain as DERIVED, ASSUMED, or APPROXIMATED.

---

## Derivation Chain

```
                         V3 CORE (frozen)
                              │
    ┌─────────────────────────┤
    │                         │
 [I1] p=q+m              [I2] Ω∈[1.16,1.19]
 ASSUMED                  ASSUMED
    │                         │
    └─────────┬───────────────┘
              │
         qCore={16,17,18}, m=3
         DERIVED (deductive from I1+I2)
              │
              ▼
    ┌─────────────────────────┐
    │  OSCILLATOR DYNAMICS    │
    │  dθ_i/dt = ω_i + ΣK_ij·sin(θ_j−θ_i)  │
    └─────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  SYNCHRONIZED STATE                 │
    │  dθ_i/dt = Ω* (constant)            │
    │  → coupling term must be spatially  │
    │    constant in equilibrium          │
    │  DERIVED (from synchronization)     │
    └─────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  COUPLING DEFECT (B3B)              │
    │  Mass → local K_ij perturbation     │
    │  → discrete Laplacian source       │
    │  STRUCTURALLY INFERRED              │
    │  (mass↔defect is C4 hypothesis)     │
    └─────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  CONTINUUM LIMIT (B3A)              │
    │  Discrete Laplacian → ∇²K = 0      │
    │  Green's function → K~K₀+α/r       │
    │  DERIVED (from graph Laplacian)     │
    │  (3D continuum is ASSUMED)          │
    └─────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  DYNAMIC EXTENSION (B4)             │
    │  ∇²K=0 → □K=0                      │
    │  c_K = c (LIGO-supported)           │
    │  APPROXIMATED (simplest causal)     │
    └─────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  KERNEL FORM (G2D)                  │
    │  K(x)=K₀/(1+x+bx²+x⁴)              │
    │  a₁=1: FIXED (normalization)        │
    │  a₂=b: FREE (shape parameter)       │
    │  a₃=0: ASSUMED (minimal choice)     │
    │  a₄=1: FIXED (Lorentz stability)    │
    └─────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  METRIC EXTRACTION (G2A)            │
    │  g_μν = (1/(2f'(0)))·∂_μ∂_νK|_{y=x}│
    │  DERIVED (from K→g ansatz)          │
    │  (ansatz itself is POSTULATED)      │
    └─────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  GW POLARIZATIONS (G2B)             │
    │  2 tensor + 1 breathing             │
    │  DERIVED (from Multi-K decomposition)│
    └─────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  EFFECTIVE GRAVITY (C5)             │
    │  T(x)=1+δK/K₀, a=c²·∇T             │
    │  → Newtonian limit: a=GM/r²        │
    │  APPROXIMATED (k=G·K₀/c² calibrated)│
    └─────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  1PN β_PPN (G1)                     │
    │  β = 1−ε/a_φ, ε∝∫[f']³+∫f'·f''    │
    │  APPROXIMATED (scalar proxy)        │
    │  Full tensor b₁–b₄: PENDING         │
    └─────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  STRONG-FIELD (G3)                  │
    │  φ''+(2/r)φ'=β_ode·(φ')²           │
    │  → r_H≈2.275GM                     │
    │  APPROXIMATED (scalar ODE, not      │
    │    from bilocal action)             │
    └─────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────┐
    │  OBSERVABLES (B5, G3-Obs)           │
    │  Shadow, ISCO, QNM, deflection...   │
    │  DERIVED (from r_H + GR formulae)   │
    │  (GR formulae are ASSUMED valid     │
    │   at the effective metric level)    │
    └─────────────────────────────────────┘
```

---

## Classification Summary

| Step | Status | Missing to upgrade |
|:---|:---|:---|
| I1, I2 (core inputs) | ASSUMED | First-principles derivation |
| qCore, m=3 | DERIVED | — |
| Oscillator dynamics | DERIVED (within model) | — |
| Synchronization → Laplacian | DERIVED | — |
| Mass → coupling defect | STRUCTURALLY INFERRED | C4: quantitative defect↔mass mapping |
| Discrete → continuum Laplacian | DERIVED (3D continuum assumed) | General dimension proof |
| □K=0 dynamic extension | APPROXIMATED | Full nonlinear PDE from action |
| Kernel Padé form | PARTIALLY FIXED | a₃ derivation; spectral density ρ(m²) |
| Metric extraction formula | POSTULATED (ansatz) | Derivation from action variational principle |
| GW polarizations | DERIVED | — |
| Effective gravity a=c²·∇T | APPROXIMATED | k = G·K₀/c² → predict G |
| 1PN β_PPN | APPROXIMATED (scalar proxy) | Full tensor b₁–b₄ angular integrals |
| Strong-field r_H | APPROXIMATED (scalar ODE) | Self-consistent G_μν=8πG·T_μν[K] |
| Observables | DERIVED (from r_H + GR) | Full strong-field metric |

---

## Missing Links (blocks to DERIVED status)

```
L1:  Mass↔defect quantitative mapping (C4→DERIVED)
     Currently: qualitative analogy

L2:  Kernel denominator coefficients from action (a₃→DERIVED)
     Currently: a₃=0 assumed

L3:  Bilocal effective action S[K] → Euler-Lagrange → field equations
     Currently: field equations are operational postulates

L4:  Metric extraction from variational principle
     Currently: ansatz g_μν ∝ ∂_μ∂_νK

L5:  Full tensor 1PN (b₁–b₄ angular integrals)
     Currently: scalar proxy only

L6:  Self-consistent strong field (G_μν=8πG·T_μν[K])
     Currently: scalar ODE approximation

L7:  G predicted from TRM parameters
     Currently: k = G·K₀/c² post-hoc calibration
```

---

## "Scope and Limitations" — Paper Paragraph

> **Scope and limitations.** The TRM V4 framework maps the frozen V3 oscillator core to gravitational observables through a bilocal coupling kernel. The logical chain proceeds as follows. The V3 core establishes the collective frequency structure from two assumed inputs (I1, I2); the deduction of qCore = {16,17,18} and m = 3 from these inputs is derived. The continuum limit of the discrete coupling matrix K_ij yields the bilocal kernel K(x,y); its functional form K₀/(1+x+bx²+x⁴) has three coefficients fixed by structural requirements (a₁, a₄) or simplicity (a₃), leaving b as the sole free parameter. The metric extraction g_μν ∝ ∂_μ∂_νK|_{y=x} and the resulting gravitational wave polarization count (2 tensor + 1 breathing) follow from the bilocal structure. The 1/r gravitational form emerges from the discrete graph Laplacian in the synchronized state — a derived result once the coupling-defect hypothesis (mass perturbs K_ij) is granted. Beyond these structural elements, the framework currently relies on approximations: the 1PN post-Newtonian parameter β_PPN is computed via a scalar proxy (full tensor mixing coefficients b₁–b₄ pending), the strong-field horizon is estimated from a scalar nonlinear ODE not yet derived from the bilocal action, and the gravitational constant G enters through the post-hoc calibration k = G·K₀/c². The principal open items — the bilocal effective action, the full tensor 1PN computation, and the self-consistent strong-field solver — define the path from the current interpretation layer to a complete theory.

---

## Sections Needing Clarification in Draft Paper

| Section | Issue | Fix |
|:---|:---|:---|
| 2.2 (Kernel) | a₃=0 not justified beyond "simplest" | Add: a₃≠0 does not prevent β crossing, just shifts it |
| 2.3 (Metric) | Extraction formula is an ansatz | Add: "Postulate motivated by coincidence limit of Gaussian coupling" |
| 3.1 (Laplace) | "Preferred" PDE — why not derived? | Already addressed: operational choice, admissibility criteria |
| 4.1 (β_PPN) | "Scalar proxy" qualifier needed in body | Added in ReviewerRecovery |
| 6 (Strong-field) | ODE not from K(x,y) action | Already flagged as qualitative model |
| 7 (Origin of b) | "Structurally preferred" vs "derived" | Distinction already clear |
