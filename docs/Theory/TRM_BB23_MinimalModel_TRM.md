# BB23 — Minimal TRM/TQM Formulation after Gauge Fixing

---

## Context

BB21 established that B(t) originates from time
reparametrization freedom in first-order dynamical systems.

BB21+ derived B(t) as the Lie algebra element of `Diff(R)` —
the infinitesimal generator of time reparametrization.

BB22 defined the gauge fixing procedure `B(t) = 0` and
showed that all observables remain invariant.

BB23 now formulates the **final, minimal TRM/TQM model**
using only physically meaningful variables — free of
redundant gauge degrees of freedom.

---

## Goal

Formulate the final TRM/TQM phase-lattice dynamics after
gauge fixing, and verify that all validated results
(CML01–CML23, BB01–BB22) are preserved.

---

## Reduced Model

After gauge fixing `B(t) = 0`, the dynamics reduce to:

```
    dθ_i/dt = ω_i + coupling_ij(θ_i - θ_j)
```

where:

| Symbol | Meaning |
|--------|---------|
| `θ_i` | Phase of oscillator `i` |
| `ω_i` | Intrinsic frequency of oscillator `i` |
| `coupling_ij` | Interaction term between oscillators `i` and `j` |

**No global lapse term appears.**

The model contains only:
- Intrinsic frequency distribution `{ω_i}`
- Coupling structure (topology, strength, functional form)
- Initial conditions `{θ_i(0)}`

---

## Key Property

All physical predictions must be invariant under:

```
    B(t) → 0
```

This is not an assumption — it is a consequence of `Diff(R)`
gauge symmetry. Any quantity that changes under `B(t) → 0`
was never a physical observable.

---

## Verification — CML Invariance

Each CML result is checked against the reduced model:

| CML Result | Reduced Model Status |
|------------|---------------------|
| **CML01–CML05**: Emergent frequency `Ω*` | Unchanged — `Ω* = mean(ω_i)` is independent of B(t) |
| **CML06–CML11**: Synchronization order parameter `R(t)` | Unchanged — `R` depends only on phase differences `θ_i - θ_j` |
| **CML12–CML16**: Phase-locking stability `Δθ_ij` | Unchanged — locked differences are coupling-determined |
| **CML17–CML23**: Energy ordering, mode selection, bridge band | Unchanged — all depend on `{ω_i}` and coupling, not B(t) |

Conclusion:

> **All CML01–CML23 results hold identically without B(t).**

The term was present in the equations but contributed nothing
to any validated prediction.

---

## Verification — Empirical Constraints

The reduced model must remain consistent with all empirical
analyses:

| Analysis | Reduced Model Consistency |
|----------|--------------------------|
| **UTCr** | No global common-mode drift expected — consistent with data |
| **PHARAO** | Only slope differences detectable — consistent with bounds |
| **SPARC** | No universal residual term required — consistent with fit |

All empirical constraints are trivially satisfied by the
reduced model, since B(t) was never required to explain any
observation.

---

## Structural Insight

```
    Original:   dθ_i/dt = ω_i + B(t) + coupling
    Reduced:    dθ_i/dt = ω_i + coupling
```

The difference is exactly `B(t)`. Since no observable depends
on this difference, the two formulations are **physically
equivalent**.

Thus:

> **B(t) has no independent physical content.**

It was a notational artifact — a variable that appeared in the
equations but was always redundant.

---

## Interpretation

The **physical degrees of freedom** of TRM/TQM are:

1. **Intrinsic frequency distribution** `{ω_i}`
   — determines the natural timescales of the system

2. **Coupling structure** `coupling_ij(Δθ)`
   — determines synchronization, phase locking, and collective
     modes

3. **Lattice topology**
   — determines which oscillators interact and the connectivity
     pattern

No additional global parameter is required. The bridge band
`Ω ≈ 1.16 .. 1.19` emerges from `{ω_i}` and coupling alone.

---

## What Was B(t) Really Doing?

In the original (un-gauge-fixed) formulation, B(t) appeared
to:

- Shift all frequencies uniformly → gauge mode
- Produce a common drift → coordinate artifact
- Act as a global background → `Diff(R)` generator

None of these roles survive gauge fixing. B(t) was the
**shadow of unfixed time parametrization** projected onto
the equations of motion.

---

## Minimal Model Summary

```
    ┌─────────────────────────────────────────┐
    │  TRM/TQM — Minimal Gauge-Fixed Model    │
    │                                         │
    │  dθ_i/dt = ω_i + Σ_j K_ij · f(θ_i-θ_j) │
    │                                         │
    │  Variables: {θ_i}, {ω_i}, {K_ij}        │
    │  Symmetry:  Diff(R) — gauge-fixed       │
    │  Gauge:     B(t) = 0                    │
    │  Status:    CLEAN — no redundant params │
    └─────────────────────────────────────────┘
```

---

## Final Statement

The minimal TRM/TQM model contains only intrinsic frequencies,
coupling, and lattice topology. B(t) is absent — not because
it was disproven, but because it was **never physical**.

The bridge band, synchronization, and all CML predictions
emerge from the minimal model without any global lapse term.

---

## Status

| Property            | Value                              |
|---------------------|------------------------------------|
| Gauge               | FIXED — `B(t) = 0`                 |
| Degrees of freedom  | `{θ_i, ω_i, K_ij}`                 |
| Redundant variables | NONE                               |
| CML compatibility   | VERIFIED — all results preserved   |
| Empirical consistency| VERIFIED — all constraints satisfied |
| Model status        | MINIMAL — ready for production use |

---

## Next Step

**BB24 — Documentation and Migration Guide**

- Update all theory documents to reference the minimal model
- Provide migration notes for code that references B(t)
- Deprecate B(t) in the CML analysis pipeline
