# TRM V4 — Claim Boundaries

**Date:** 2026-07-05
**Status:** CLAIM BOUNDARIES COMPLETE. C1–C9 defined. G1–G4 closure claims added. V4 interpretation layer has explicit falsification conditions.

---

## 1. What V4 Claims

### 1.1 C5-Era Claims (B1–B6)

| # | Claim | Strength | Dependency |
|:---|:---|:---|:---|
| V4-C1 | A time-rate field T(x) = Ω*(x) can be consistently interpreted as a local physical quantity | **HYPOTHESIS** | Requires passing all 3 consistency checks |
| V4-C2 | The clock-bias φ can be mapped to a physical field φ(x) in at least one regime | **HYPOTHESIS** | Depends on candidate evaluation |
| V4-C3 | An effective acceleration a(x) = c²·∇T(x) can be derived from the gradient | **DERIVED** (given a valid φ(x) mapping) | Follows from T(x) definition |
| V4-C4 | Deviations from Newtonian gravity can be predicted from T(x) structure | **HYPOTHESIS** | Requires numerical evaluation |

### 1.2 G1 Claims — 1PN Post-Newtonian Closure

| # | Claim | Strength | Dependency |
|:---|:---|:---|:---|
| V4-C5 | The bilocal TRM framework admits physically valid kernels that reproduce GR-compatible β at 1PN | **SUPPORTED** | β(b) crosses 1 at b≈1.25; 6/6 stability checks pass |
| V4-C6 | β_total < 1 for the quartic baseline K₀/(1+x+x²) | **PROVEN** (sign analysis) | f'(0)<0, f''(0)=0, angular integrals positive → cubic b_i < 0 |
| V4-C7 | β(b) is continuous and crosses 1 — no fine-tuning required | **PROVEN** (numerical scan) | Two-pass refinement confirms crossing at b≈1.25 |

### 1.3 G2 Claims — Tensor Bridge

| # | Claim | Strength | Dependency |
|:---|:---|:---|:---|
| V4-C8 | Metric g_μν can be extracted from K(x,y) at coincidence: g_μν ∝ ∂_μ∂_νK\|_{y=x} | **DERIVED** | f'(0) ≠ 0 ensures extraction works |
| V4-C9 | The quartic kernel K₀/(1+d²/λ²+(d²/λ²)²) is globally Lorentzian (finite, positive, smooth for all d²) | **PROVEN** | Denominator ≥ ¾ > 0 ∀ real d² |
| V4-C10 | TRM supports 2 tensor GW polarizations (h_+, h_×) + 1 breathing mode | **DERIVED** | From Multi-K decomposition |

### 1.4 G3 Claims — Strong-Field

| # | Claim | Strength | Dependency |
|:---|:---|:---|:---|
| V4-C11 | A horizon forms in the scalar nonlinear approximation at r_H ≈ 2.275 GM (+13.8% vs GR) | **COMPUTED** (scalar ODE) | β ≈ 0.55 from b=1.25 kernel |
| V4-C12 | All strong-field observables scale uniformly with r_H | **DERIVED** | Photon sphere, shadow, ISCO, QNM all ∝ r_H |
| V4-C13 | Current EHT data cannot distinguish TRM from GR | **OBSERVATION** | M87* ~17%, Sgr A* ~14% uncertainty |
| V4-C14 | TRM strong-field predictions are falsifiable for any b≠1 | **ESTABLISHED** | Detection thresholds mapped to instrument classes |

### 1.5 G4 Claims — Origin of b

| # | Claim | Strength | Dependency |
|:---|:---|:---|:---|
| V4-C15 | b=1 is the structurally preferred value (f''=0, ε minimum, IR attractor) | **DERIVED** (structural analysis) | Multiple independent constraints converge |
| V4-C16 | b is derivable from spectral density ρ(m²): b = ⟨m²⟩_ρ/⟨1⟩_ρ | **FRAMEWORK** | Requires bilocal action → future step |
| V4-C17 | b is TRM's Brans-Dicke ω — controls deviation from GR | **ANALOGY** | b=1 is the GR-identical limit |

---

## 2. What V4 Does NOT Claim

- V4 does **not** claim that T(x) is a fundamental field of nature
- V4 does **not** claim to replace General Relativity (weak-field 1PN compatible; full nonlinear open)
- V4 does **not** claim the classical gravity mapping works at galactic scales (scale mismatch: 34,000×)
- V4 does **not** claim that φ = 0.17 is derived from first principles
- V4 does **not** claim the bridge band emerges from dynamics
- V4 does **not** introduce new adjustable parameters — φ mappings must be testable with existing constraints
- V4 does **not** claim β(b) = 1 is proven for the full tensor 1PN (only scalar proxy; mixing coefficients pending)
- V4 does **not** claim the scalar ODE horizon is the final strong-field result (full tensor G_μν solution pending)
- V4 does **not** claim b is rigorously derived from the bilocal action (structurally preferred, not yet derived)
- V4 does **not** claim the quartic kernel is the unique possible kernel (it is the simplest viable member of a family)

---

## 3. Interpretation vs. Theory

| Layer | Status | Scope |
|:---|:---|:---|
| **TRM V3.4 Core** | FROZEN | Oscillator dynamics, I1, I2, D1, FP01–FP31 |
| **TRM V4 Interpretation** | ACTIVE | Physical meaning of φ, T(x), effective gravity |

V4 is **Bedeutungsentwicklung** (meaning development), not **Theorieentwicklung** (theory development).

---

## 4. Falsification of V4 Interpretations

A V4 interpretation candidate is falsified if:

| # | Condition | Classification |
|:---|:---|:---|
| F-V4-1 | The φ(x) mapping violates any of the 3 consistency checks | INVALID |
| F-V4-2 | The derived a(x) contradicts known gravitational data where the mapping should apply | INVALID |
| F-V4-3 | The φ(x) mapping requires a new free parameter not present in V3.4 | INVALID (violates V4 design principle) |
