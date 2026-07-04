# BB21 — Mathematical Origin of the Global Lapse B(t)

---

## Context

BB17 established empirical constraints on B(t). BB18 showed
that a perfectly global B(t) is operationally equivalent to
a time reparameterization. BB19 concluded B(t) should not be
treated as a physical variable in TRM/TQM. BB20 strips B(t)
from the core dynamics.

BB21 explains **why** B(t) appears in the first place — and
why it is always removable.

---

## Core Statement

**B(t) originates from the freedom to redefine time in
first-order dynamical systems.**

---

## Model

Given the phase dynamics:

```
    dθ_i/dt = ω_i + B(t) + coupling
```

where B(t) is a global, observer-independent additive term.

---

## Time Transformation

Define a new time coordinate:

```
    t' = t + ∫₀ B(τ) dτ
```

Then:

```
    dt'/dt = 1 + B(t)
```

and consequently:

```
    d/dt = (dt'/dt) · d/dt' = (1 + B(t)) · d/dt'
```

---

## Result

The dynamics become:

```
    dθ_i/dt' = ω_i + coupling
```

Therefore:

**B(t) disappears completely from the equations of motion.**

---

## Interpretation

B(t) is **not a physical variable**,
but a manifestation of **time parametrization freedom**.

In any first-order dynamical system, the choice of time
coordinate is arbitrary up to a reparametrization
`t → t'`. A global additive term in `dθ/dt` is precisely
the infinitesimal signature of such a reparametrization.

---

## Key Insight

**In first-order systems, any global additive term in dθ/dt
can be absorbed into a redefinition of time.**

This is a mathematical identity, not a physical hypothesis.
It follows directly from the chain rule:

```
    dθ/dt' = (dθ/dt) · (dt/dt')
```

If `dt/dt'` contains a factor `1 + B(t)`, then any additive
B(t) in `dθ/dt` is exactly compensated.

---

## Relation to CML

CML measurements fix a **specific** time coordinate
(typically the one in which the numerical integration is
performed). Within that coordinate, B(t) *appears* observable
— it shows up in the equations and must be fitted.

But the appearance is a coordinate artifact. Change the time
parametrization, and B(t) vanishes.

---

## Relation to BB18/BB19

| Document | Result |
|----------|--------|
| BB18     | B(t) is unobservable (operational equivalence) |
| BB19     | B(t) is removable (model simplification) |

BB21 provides the **mathematical reason** for both: B(t) is
the gauge degree of freedom associated with time
reparametrization in first-order phase dynamics.

---

## Analogy: Gauge Freedom in Electromagnetism

| Electromagnetism | TRM Phase Dynamics |
|------------------|--------------------|
| Vector potential A_μ | Global lapse B(t) |
| Gauge transformation A → A + ∂χ | Time reparametrization t → t' |
| Observable fields E, B unchanged | Observable phases θ_i(t') unchanged |

Just as the vector potential carries gauge degrees of freedom
that do not affect physical observables, B(t) carries time
reparametrization freedom that does not affect phase dynamics.

---

## Final Statement

**B(t) is the gauge degree of freedom associated with time
reparametrization in first-order phase-lattice dynamics.**

It appears whenever time is not uniquely fixed by an external
clock. It disappears whenever the time coordinate is
consistently redefined.

---

## Status

| Property            | Value     |
|---------------------|-----------|
| Mathematical origin | RESOLVED  |
| Physical role       | NONE      |
| Classification      | Gauge-like redundancy of time parametrization |
