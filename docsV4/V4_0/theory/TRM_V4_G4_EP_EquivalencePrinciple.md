# TRM V4 — G4-EP: Equivalence Principle from Time-Rate Universality

**Date:** 2026-07-05
**Status:** DERIVED. Weak equivalence principle follows structurally from time-rate field universality.
**Depends on:** V3 core oscillator dynamics, V4 C5 time-rate interpretation.

---

## 0. Objective

Derive the weak equivalence principle (all objects fall with the same acceleration regardless of composition) from TRM structure. Show that TRM does not assume the equivalence principle — it provides a structural reason for it: gravity is the gradient of a universal time-rate field, not a force that couples to mass.

---

## 1. The Time-Rate Field T(x)

### 1.1 Definition

In the synchronized state, the collective frequency Ω* sets the time-rate at each oscillator site. The continuum limit defines a scalar field:

\[
T(x) \equiv \Omega^*(x) / \Omega^*_{\rm ref}
\qquad (1)
\]

where Ω*_ref is the reference collective frequency (e.g., at spatial infinity). In flat space, T(x) = 1. In the presence of a coupling defect (mass-energy concentration), T(x) = 1 + φ(x) with φ(x) = δK(x)/K₀.

### 1.2 Universality

T(x) is a property of the synchronized oscillator network, not of individual oscillators. It is the SAME for all oscillator subsystems at position x, regardless of their:
- Natural frequencies ω_i
- Coupling strengths K_ij
- Internal phase configurations
- Energy scale

This follows from the phase-locking condition: in the synchronized state, all oscillators at x share the collective frequency Ω*(x). The time-rate T(x) is a collective, emergent field — not a property of any single oscillator.

**Classification: STRUCTURAL.** The universality of T(x) follows from the definition of synchronization in the coupled oscillator network.

---

## 2. Universal Acceleration

### 2.1 All Processes Evolve via T(x)

Any physical process — clock, particle trajectory, light propagation — involves time evolution. In TRM, the local time rate is T(x). The proper time interval at x is:

\[
d\tau(x) = T(x)\,dt
\qquad (2)
\]

where dt is the coordinate time interval. This affects:
- **Clocks:** oscillation period ∝ 1/T(x) → gravitational redshift
- **Matter:** trajectory determined by extremizing proper time → geodesic equation
- **Light:** null geodesics ds² = 0 → light deflection, Shapiro delay

### 2.2 Acceleration from Time-Rate Gradient

For a test particle, the action is proportional to proper time:

\[
S = -mc^2 \int d\tau = -mc^2 \int T(x)\,dt
\qquad (3)
\]

The Euler-Lagrange equation yields the acceleration:

\[
a(x) = c^2\,\nabla\ln T(x) \approx c^2\,\nabla T(x)
\qquad (4)
\]

for T(x) ≈ 1 (weak-field). The mass m cancels — the acceleration is independent of the particle's mass.

### 2.3 Composition Independence

The action (3) depends only on proper time, which depends only on T(x). No property of the test particle enters except its existence as a physical system that evolves in time. Therefore:

\[
\boxed{
a(x) = c^2\,\nabla T(x) \quad \text{— universal, composition-independent}
}
\qquad (5)
\]

**Classification: DERIVED from the time-rate universality (Section 1) and the action principle (3).**

---

## 3. Mapping to GR Equivalence Principle

### 3.1 Weak Equivalence Principle (WEP)

**Standard statement:** The trajectory of a freely falling test particle is independent of its mass and internal composition.

**TRM statement:** The acceleration a(x) = c²∇T(x) is independent of mass and composition because T(x) is a universal collective field.

### 3.2 Einstein Equivalence Principle (EEP)

**Standard statement:** In a freely falling frame, the laws of physics take their special-relativistic form.

**TRM statement:** In a frame where T(x) is locally constant (∇T = 0), the proper time is uniform and the oscillator dynamics reduce to the flat-spacetime form. Local Lorentz invariance follows from the d'Alembertian structure of the kinetic term.

### 3.3 Strong Equivalence Principle (SEP)

**Standard statement:** The WEP and EEP hold for self-gravitating bodies as well as test particles.

**TRM statement:** Self-gravitating bodies source their own T(x) perturbation. The back-reaction (Phase 1B) couples the body's own K-field to its trajectory. Whether SEP holds in TRM depends on the self-force analysis — currently OPEN.

### 3.4 Summary

| Principle | TRM Status |
|:---|:---|
| WEP (universality of free fall) | **DERIVED** (Eq. 5) |
| EEP (local Lorentz invariance) | **STRUCTURALLY SUPPORTED** (□K action) |
| SEP (self-gravitating bodies) | **OPEN** (back-reaction self-force) |

---

## 4. No Fifth Force

### 4.1 Why Composition-Dependence Cannot Enter

A "fifth force" would require T(x) to couple differently to different types of matter. In TRM, T(x) is the collective frequency of the oscillator network — it is the same for all subsystems. There is no "T-charge" analogous to electric charge that could differ between particle species.

The only way composition dependence could enter is if different particles coupled to the oscillator network with different strengths. But the oscillator network is the fundamental substrate — particles ARE excitations of this network. They cannot have different coupling strengths because they are defined by their coupling structure.

### 4.2 Eötvös Parameter

The Eötvös parameter η = |a₁ − a₂|/|a₁ + a₂| measures differential acceleration. In TRM:

\[
\eta_{\rm TRM} = 0 \quad \text{(exactly, at the test-particle level)}
\]

The current experimental bound is η < 10⁻¹⁵ (MICROSCOPE mission). TRM predicts η = 0 — a falsifiable prediction.

**Classification: DERIVED. η=0 at leading order. Back-reaction may introduce O(GM/λ) corrections for extended bodies.**

---

## 5. Minimal Assumptions

The derivation of the equivalence principle in TRM requires:

| Assumption | Status |
|:---|:---|
| V3 core oscillator dynamics | FROZEN (I1+I2) |
| Synchronized state Ω*(x) exists | OBSERVED (CML simulations) |
| Continuum limit K_ij → K(x,y) | POSTULATED (Section 2.5 of paper) |
| T(x) = Ω*(x)/Ω*_ref | DEFINITION |
| Proper time ∝ 1/T(x) | DERIVED (from oscillator period) |
| Action ∝ proper time | POSTULATED (standard Lagrangian mechanics) |

**Net: 2 postulates beyond the frozen V3 core (continuum limit, action principle). All other steps are derived or definitional.**

---

## 6. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_Interpretation_Core.md` | C5 time-rate interpretation |
| `TRM_V4_Final_Status.md` | V4 closure |
| `TRM.Tests/V4/G4_EP_EquivalencePrinciple_Tests.cs` | xUnit validation |
