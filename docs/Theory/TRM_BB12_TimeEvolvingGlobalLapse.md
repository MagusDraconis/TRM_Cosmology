# BB12 — Time-Evolving Global Lapse Hypothesis

---

## Context

V3.4 established:

- Ω* = ⟨ω_i⟩ + α·φ explains the global frequency shift
- α·φ acts as a uniform additive term (common-mode shift)
- synchronization is invariant under this shift
- φ ≈ 0.17 has no physical mapping via GM/(c²r)

BB11 identified:

- α·φ must be interpreted as a GLOBAL time-rate offset (not local potential)
- valid interpretation: α·φ ≡ B (global background shift)

---

## Hypothesis

Replace the static interpretation:

    α·φ = constant

with a dynamic background:

    α·φ → B(t)

where:

- B(t) is a global, time-dependent clock-rate offset
- identical for all oscillators
- independent of ω_i and coupling

---

## Core Equation

```
dθ_i/dt = ω_i + B(t) + K · Σ sin(θ_j − θ_i)
```

Resulting collective frequency:

```
Ω*(t) = ⟨ω_i⟩ + B(t)
```

---

## Structural Properties

| Property | Result |
|:---|:---|
| Global | YES (same for all i) |
| Constant per time slice | YES |
| Time-dependent | YES |
| Affects mean only | YES |
| Affects spread | NO |
| Breaks synchronization | NO |

**Why synchronization is preserved:**

```
Locking condition: |ω_i + B(t) − Ω*(t)| ≤ K·R

Ω*(t) = ⟨ω_i⟩ + B(t)
→ |ω_i + B(t) − ⟨ω_i⟩ − B(t)| = |ω_i − ⟨ω_i⟩| ≤ K·R
```

The B(t) terms cancel at every t. The locking criterion depends only on the time-independent frequency spread. A time-varying B(t) shifts the collective frequency in real time without affecting the relative phase differences that govern synchronization.

---

## Interpretation

B(t) represents:

- a global time-rate background
- a system-wide clock normalization
- not a local gravitational potential

Equivalent to:

- a uniform lapse / time-scaling factor
- a common-mode frequency modulation

**Key distinction:** B(t) is not a driving force. It is not a coupling between oscillators. It is a property of the global time coordinate in which the oscillators are embedded. All oscillators experience the same time rate at each instant. Differences between oscillators arise only from ω_i and coupling — neither of which B(t) modifies.

---

## Relation to Observables

The global shift itself is not observable.

Only differences in B across time produce measurable effects:

```
ΔΩ* = B(t_observe) − B(t_emit)
```

This has the same formal structure as redshift:

```
z ~ ΔB / ω
```

BUT:

- B(t) is not redshift itself
- it is the underlying time evolution that produces it
- B(t) is the global clock-rate background; redshift is the observable consequence of its variation

**Analogy:** B(t) is to redshift as the metric is to geodesic deviation. The metric is not directly observable; differences in the metric along a path produce observable effects. Similarly, B(t) is not directly observable; changes in B(t) between emission and observation produce frequency shifts.

---

## Minimal Consistency Check

Simulation test (conceptual only — no code required):

1. Define:
   ```
   B(t) = B0 + ε·t
   ```

2. Run:
   ```
   dθ_i/dt = ω_i + B(t) + coupling
   ```

3. Verify:
   ```
   Ω*(t) = 1.0 + B(t)
   MeanOrder ≥ 0.85
   ```

Expected:

- linear tracking of Ω*(t) with B(t)
- unchanged synchronization quality across all t
- Ω*(t) − B(t) = ⟨ω_i⟩ ≈ 1.0 (constant)

**This is a direct generalization of CML11.** CML11 tested the static case B(t) = constant = α·φ = 0.17. The time-dependent case B(t) = B0 + ε·t is the next structural step — it tests whether the cancellation |ω_i + B(t) − Ω*(t)| = |ω_i − ⟨ω_i⟩| holds at every time step.

---

## Implications

- The bridge-band shift is not caused by local physics
- It originates from a global time background B(t)
- The evolution of the time-rate background drives the collective frequency
- The static case (CML11: φ = 0.17, Ω* = 1.17) is the t = constant slice of B(t)
- If B(t) evolves, Ω*(t) evolves with it — continuously and without breaking synchronization

**What this separates:**

| Old view | New view |
|:---|:---|
| φ is a local potential (GM/c²r) | B(t) is a global time-rate background |
| φ must be sourced by mass | B(t) may have intrinsic dynamics |
| φ = 0.17 is unexplained | B(t) = 0.17 is one value of an evolving background |
| Static bridge band | Dynamic bridge band tracking B(t) |

---

## Status

| Aspect | Classification |
|:---|:---|
| Mechanism (static) | **VALIDATED** — CML11: Ω* = 1.17 at α·φ = 0.17 |
| Interpretation (medium/buoyancy) | **EXTENDED** — BB11 reinterpretation → time-dependent B(t) |
| Time-dependent generalization | **CONCEPTUAL** — not yet simulated |
| Physical grounding | **OPEN** — what determines B(t)? |

---

## Key Statement

α·φ is not a potential.

α·φ is a global time-rate offset.

Its time evolution B(t) explains why
the system-wide phase rate changes at all.

**The bridge band is a snapshot of B(t), not a fixed constant of nature.**
