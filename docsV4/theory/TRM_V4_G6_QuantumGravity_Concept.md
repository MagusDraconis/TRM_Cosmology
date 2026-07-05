# TRM V4 — G6: Quantum Gravity Concept Program

**Date:** 2026-07-05
**Status:** CONCEPTUAL PROGRAM. Lattice-regulated, bilocal, ghost-free classical → candidate UV-complete quantum theory. Executable subproblems defined.
**Depends on:** V3 core, V4 bilocal action, DeepCompletion Phases 1A–2C, G2 tensor bridge, G4-EP/SEP, G5 strong-field.

---

## 0. Objective

Establish the conceptual pathway from the discrete TRM oscillator network to a finite quantum theory of gravity. The discrete lattice provides a natural UV cutoff — a feature neither GR (continuous, non-renormalizable) nor perturbative quantum gravity possesses. This document outlines the conceptual framework; no claim of a complete theory is made.

---

## 1. Discrete Lattice as UV Regulator

### 1.1 The Fundamental Discreteness

The V3 core describes N coupled phase oscillators on a lattice with spacing a:

\[
\frac{d\theta_i}{dt} = \omega_i + \sum_j K_{ij}\,\sin(\theta_j - \theta_i)
\]

The lattice spacing a (distance between adjacent oscillator sites) is the fundamental length scale of the theory. It is NOT a regulator introduced by hand and removed at the end — it is a physical parameter of the discrete system.

### 1.2 Comparison with Lattice Gauge Theory

| Aspect | Lattice QCD | TRM |
|:---|:---|:---|
| Fundamental DOF | Quark fields on lattice sites | Oscillator phases on lattice sites |
| Gauge fields | Link variables U_μ | Coupling matrix K_ij |
| Continuum limit | a → 0, g → 0 | a → 0, K_ij → K(x,y) |
| UV regulator | Lattice spacing a | Lattice spacing a |
| Physical cutoff | Λ ~ 1/a | Λ ~ 1/a |
| Renormalizability | Proven | **Open** |

The analogy is structural: both theories are defined on a discrete lattice and have a continuum limit. The key difference: in lattice QCD, the continuum limit is taken with a carefully tuned bare coupling; in TRM, the continuum limit is a postulated effective description (Section 2.5 of the main paper).

### 1.3 Natural UV Cutoff

In the discrete formulation, momentum integrals are replaced by discrete sums over the Brillouin zone:

\[
\int \frac{d^4k}{(2\pi)^4} \;\longrightarrow\; \frac{1}{V} \sum_{k \in BZ}
\]

The Brillouin zone has a maximum momentum |k_max| = π/a. All loop integrals are finite because the integration domain is compact. **There are no UV divergences in the discrete theory.** Divergences can only appear in the continuum limit a → 0.

**Classification: STRUCTURAL.** The finiteness of the discrete theory follows from the compactness of the Brillouin zone.

---

## 2. Quantized Oscillator Phases

### 2.1 Phase Operators

Promote the oscillator phases to quantum operators:

\[
\theta_i \to \hat{\theta}_i,\quad [\hat{\theta}_i, \hat{p}_j] = i\hbar\,\delta_{ij}
\]

where p_j is the conjugate momentum (related to the phase velocity). The Hamiltonian is:

\[
\hat{H} = \sum_i \frac{\hat{p}_i^2}{2I} + \sum_{i,j} K_{ij}\,[1 - \cos(\hat{\theta}_i - \hat{\theta}_j)]
\]

This is the quantum XY model with coupling matrix K_ij. It is a well-defined, finite quantum system for any finite N.

### 2.2 Collective Excitations

The synchronized state |Ω*⟩ has all phases locked: ⟨θ̂_i − θ̂_j⟩ = 0. Small perturbations around this state are collective phase waves (phonons of the oscillator lattice). These are the candidate graviton states.

### 2.3 Continuum Limit

In the continuum limit N → ∞, a → 0 with Na³ = V fixed, the phase field becomes:

\[
\hat{\theta}_i \to \hat{\phi}(x),\quad K_{ij} \to K(x,y)
\]

The effective action for the phase field includes the bilocal kinetic term (Phase 1A, Eq. 9), which yields the graviton propagator.

**Classification: CONCEPTUAL. The mapping from discrete quantum oscillators to continuum quantum fields is standard in condensed matter physics. The specific form of the continuum action is the bilocal ansatz of Phase 1A.**

---

## 3. Schematic Graviton Propagator

### 3.1 From Bilocal Kernel

The graviton propagator is obtained from the bilocal kinetic term S_kin. In momentum space:

\[
\mathcal{G}_{\mu\nu\alpha\beta}(k) =
\frac{1}{k^2}\,
\left(\eta_{\mu\alpha}\eta_{\nu\beta} + \eta_{\mu\beta}\eta_{\nu\alpha}
- \eta_{\mu\nu}\eta_{\alpha\beta}\right)
\times \mathcal{F}(k^2 a^2)
\qquad (1)
\]

where F(k²a²) is the kernel form factor from the Fourier transform of K(d²/a²). For the quartic kernel:

\[
\mathcal{F}(k^2 a^2) = \int d^4r\,e^{ik\cdot r}\,
\frac{K_0}{1 + r^2/a^2 + (r^2/a^2)^2 + (r^2/a^2)^4}
\]

### 3.2 UV Behavior

In the continuum limit (a → 0), F(k²a²) → 1 for physical momenta k ≪ 1/a, recovering the standard GR graviton propagator ∼ 1/k². For k ∼ 1/a, the form factor provides a smooth UV cutoff:

\[
\mathcal{F}(k^2 a^2) \sim \frac{1}{(k^2 a^2)^2} \quad \text{as } k^2 \to \infty
\]

This is a **softer UV behavior than GR** (which has no form factor). Loop integrals that diverge as ∫ d⁴k/k² in GR are regulated by the additional 1/(k²a²)² suppression:

\[
\int d^4k\,\frac{1}{k^2} \to \infty \quad\text{(GR, non-renormalizable)}
\]
\[
\int d^4k\,\frac{\mathcal{F}(k^2 a^2)}{k^2} \sim \int d^4k\,\frac{1}{k^2}\cdot\frac{1}{(k^2 a^2)^2}
= \text{finite}
\quad\text{(TRM)}
\]

**Classification: CONCEPTUAL. The bilocal kernel provides a natural form factor that renders loop integrals finite. This is the central quantum-gravity motivation for the bilocal framework.**

---

## 4. Finiteness Arguments

### 4.1 Discrete Sum vs. Continuous Integral

All physical quantities in TRM are computed from the discrete oscillator network. The lattice sum is manifestly finite for any finite N. The continuum limit is taken only for macroscopic (IR) quantities.

### 4.2 No Point-like Vertices

In the bilocal action, interactions are mediated by the kernel K(x,y) which has finite extent ∼ a. There are no point-like vertices — the bilocal structure smears interactions over the kernel width. This is analogous to string theory's extended objects, but implemented through a bilocal field rather than worldsheets.

### 4.3 Power-Counting

The bilocal kinetic term has canonical dimension [S_kin] = 0 (dimensionless action). The interaction terms contain the kernel and its derivatives evaluated at finite separation — no contact terms. Power-counting suggests the theory is superficially finite.

**Classification: CONCEPTUAL. A rigorous proof of finiteness requires explicit computation of loop integrals in the bilocal theory — not attempted here.**

---

## 5. Relation to Other Approaches

| Approach | UV regulator | Graviton | Key object |
|:---|:---|:---|:---|
| String theory | Extended objects | Closed string excitation | Worldsheet |
| LQG | Spin networks | Coherent states | Holonomies |
| Asymptotic Safety | RG fixed point | Metric fluctuation | FRGE |
| Causal Dynamical Triangulations | Simplicial lattice | Geometric observable | Simplex ensemble |
| **TRM** | **Oscillator lattice** | **Phase wave** | **Bilocal kernel K(x,y)** |

TRM is closest in spirit to lattice approaches (CDT, lattice gravity) but differs in using coupled phase oscillators rather than simplicial geometry as the fundamental degrees of freedom. The bilocal kernel K(x,y) plays a role analogous to the string worldsheet — a two-point object that encodes geometry.

---

## 6. Open Questions

1. **Continuum limit existence:** Does the discrete theory have a second-order phase transition allowing a continuum limit with finite G? (Analogous to the Wilson-Fisher fixed point in scalar φ⁴ theory.)

2. **Unitarity:** Is the bilocal quantum theory unitary? The classical spectral positivity (Phase 1D) is encouraging but does not guarantee quantum unitarity.

3. **Renormalization group:** What is the RG flow of the bilocal coupling K(x,y)? Does b run? (Preliminary analysis in G4 suggests b→1 in the IR.)

4. **Graviton scattering:** Can graviton-graviton scattering amplitudes be computed in the bilocal theory? Do they match GR at low energies?

5. **Cosmological constant:** Does the discrete theory predict Λ? (Phase 1C: Λ_eff from kernel moments, but magnitude is a cosmological constant problem.)

**Classification: OPEN. These are research-program questions, not blockers for the classical V4 framework.**

---

## 7. Status

TRM provides a **conceptual framework for finite quantum gravity** based on:
- Discrete oscillator lattice (natural UV cutoff)
- Bilocal kernel (smeared interactions, form factor)
- Spectral positivity (ghost-free classical propagator)
- Lattice gauge theory analogy (well-understood continuum limit program)

**No claim of a complete quantum gravity theory is made.** The framework identifies the structural ingredients and the open questions. The classical V4 results (action, EFT, tensor 1PN, strong-field) are independent of the quantum gravity program.

---

## 8. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_DeepCompletion_Phase1D_GhostAnalysis.md` | Spectral positivity |
| `TRM_V4_DeepCompletion_Phase1A_CovariantAction.md` | Bilocal action |
| `TRM_V4_G4_EP_EquivalencePrinciple.md` | WEP derivation |
| `TRM.Tests/V4/G6_QuantumGravity_Concept_Tests.cs` | xUnit validation |

---

## APPENDIX A — Classification Map

### A.1 What Is Already Established (Classical Level)

| Result | Classification | Section |
|:---|:---|:---|
| Discrete lattice provides natural UV cutoff | STRUCTURAL | 1 |
| Bilocal action S[K,g] with covariant kinetic term | POSTULATED (ansatz) | Phase 1A |
| Local EFT limit (R + c_i R² + ...) | DERIVED (coincidence limit) | Phase 1C |
| Spectral positivity ρ(m²) ≥ 0 | NUMERICALLY VERIFIED | Phase 1D |
| Ghost-free classical propagator | DERIVED (from spectral positivity) | 7 (paper) |
| Kernel form factor F(k²) ~ 1/(k² a²)² (UV suppression) | STRUCTURAL (from kernel form) | 3.2 |

### A.2 What Is Conceptual (Requires Development)

| Concept | Status | Key open question |
|:---|:---|:---|
| Quantized oscillator phases → graviton states | CONCEPTUAL | Mapping discrete phase waves ↔ spin-2 graviton |
| Lattice regulator → finite quantum loops | CONCEPTUAL | Explicit 1-loop computation needed |
| Bilocal smearing → no point vertices | CONCEPTUAL | Rigorous power-counting proof |
| Kernel form factor → UV-complete propagator | CONCEPTUAL | Unitarity of the dressed propagator |
| Lattice QCD analogy → solvable continuum limit | CONCEPTUAL | Does the theory have a 2nd order phase transition? |

### A.3 What Is Speculative (Requires New Insights)

| Speculation | Rationale | Risk |
|:---|:---|:---|
| TRM could be UV-complete without further structure | Lattice + bilocal might be sufficient | High — most lattice theories need fine-tuning |
| Graviton = collective phase wave of oscillator lattice | Attractive analogy with phonons | Medium — mapping to spin-2 is non-trivial |
| b runs to 1 in IR, finite in UV (asymptotic safety-like) | G4 RG analysis suggests IR attractor | High — no UV fixed point computed |
| Cosmological constant predicted from lattice ground state | Λ_eff from kernel moments (Phase 1C) | High — magnitude is CC problem |

---

## APPENDIX B — Executable Subproblems

These are concrete, well-defined tasks that would advance the QG program from conceptual to computational:

### B.1 Lattice Path Integral (6 months)

**Task:** Formulate the Euclidean path integral for the discrete oscillator network:

\[
Z = \int \mathcal{D}\theta_i\; e^{-S_E[\theta_i, K_{ij}]}
\]

**Deliverables:**
- Discrete action S_E in terms of phase differences
- Continuum limit → bilocal action S[K]
- Measure: flat (phase variables) or curved (constrained)?
- First quantum correction to the classical bilocal action

**Difficulty:** MEDIUM. Standard lattice field theory techniques.

### B.2 1-Loop Graviton Propagator (6 months)

**Task:** Compute the 1-loop correction to the graviton propagator in the bilocal theory:

\[
\Pi_{\mu\nu\alpha\beta}(k) = \int \frac{d^4p}{(2\pi)^4}\,
\mathcal{V}(k,p)\,\mathcal{G}(p)\,\mathcal{V}(k,p)
\]

**Deliverables:**
- Vertex Feynman rules from S_int (Phase 1A, Eq. 10)
- Momentum-space kernel form factor
- Demonstrate UV finiteness (if confirmed)
- Running of G_eff and b at 1-loop

**Difficulty:** HIGH. Requires bilocal Feynman rules and careful treatment of the form factor.

### B.3 Continuum Limit Analysis (12 months)

**Task:** Determine whether the discrete oscillator theory has a second-order phase transition allowing a continuum limit with finite G.

**Deliverables:**
- Phase diagram in (a, b) parameter space
- Critical exponents at the transition
- RG flow from lattice Monte Carlo or analytical FRG
- Evidence for/against a UV fixed point

**Difficulty:** VERY HIGH. Requires lattice simulation or functional RG expertise.

### B.4 Spectral Positivity at Quantum Level (6 months)

**Task:** Extend the classical Källén-Lehmann analysis (Phase 1D) to the quantum theory.

**Deliverables:**
- Quantum spectral density from the full propagator
- Optical theorem check at 1-loop
- Unitarity of the S-matrix in the bilocal theory

**Difficulty:** MEDIUM-HIGH. Builds directly on Phase 1D.

---

## APPENDIX C — What Would Constitute a "True Finite QG Claim"

To claim TRM provides a finite quantum theory of gravity, the following would be required:

1. ✅ Classical action exists (Phase 1A) — DONE
2. ✅ Classical propagator is ghost-free (Phase 1D) — DONE
3. ⬜ Quantum path integral is well-defined (B.1) — OPEN
4. ⬜ 1-loop corrections are finite (B.2) — OPEN
5. ⬜ Unitarity holds at quantum level (B.4) — OPEN
6. ⬜ Continuum limit exists with finite G (B.3) — OPEN
7. ⬜ Low-energy EFT matches GR + higher-derivative corrections (Phase 1C) — DONE

**Current status: 3 of 7 requirements met. The remaining 4 are the QG program defined above.**

