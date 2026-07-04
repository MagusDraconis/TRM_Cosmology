# BB21+ — Lie Group Origin of the Global Lapse B(t)

---

## Context

BB21 established that B(t) originates from the mathematical
freedom to redefine time in first-order dynamical systems.

BB21+ goes one level deeper: it derives B(t) as the
infinitesimal generator of the time reparametrization symmetry
group `Diff(R)`, using Lie group theory.

---

## Goal

Derive B(t) as the **generator of time reparametrization
symmetry** — not as an ad-hoc term, but as a structural
necessity of any first-order theory with unfixed time
parametrization.

---

## Symmetry Structure

The phase dynamics

```
    dθ_i/dt = ω_i + coupling
```

is invariant under

```
    t → f(t)
```

where `f(t)` is any smooth, strictly monotonic function.

This defines a continuous, infinite-dimensional Lie group:

```
    G = Diff(R)
```

— the diffeomorphism group of the real line.

`Diff(R)` is the most general symmetry group of a
one-dimensional time coordinate.

---

## Infinitesimal Transformation

Expand near the identity:

```
    t → t + ε(t)    where |ε(t)| ≪ 1
```

The infinitesimal generator is the vector field:

```
    X_ε = ε(t) · ∂/∂t
```

These generators span the Lie algebra:

```
    Lie(Diff(R)) = Vect(R)
```

— the algebra of smooth vector fields on the real line.

---

## Action on Dynamics

Under the infinitesimal transformation, the derivative
transforms as:

```
    d/dt → d/dt · (1 - dε/dt) + O(ε²)
```

Applied to the phase dynamics:

```
    dθ/dt  →  dθ/dt + (dε/dt) · (dθ/dt) + O(ε²)
```

For first-order dynamics where `dθ_i/dt ≈ ω_i`, the leading
correction is:

```
    δ(dθ/dt) = dε/dt
```

---

## Emergence of B(t)

Identify:

```
    B(t) ≡ dε/dt
```

Then the transformed dynamics read:

```
    dθ_i/dt = ω_i + B(t) + coupling
```

**B(t) is the image of the infinitesimal generator of
`Diff(R)` acting on first-order phase velocities.**

---

## Interpretation

| Concept | Role |
|---------|------|
| `Diff(R)` | Symmetry group of time reparametrization |
| `Lie(Diff(R))` | Algebra of infinitesimal generators |
| `ε(t)` | Local shift parameter |
| `B(t)` = `dε/dt` | Infinitesimal action on `dθ/dt` |

B(t) is **not an external field** introduced by hand.

It is the **coordinate expression** of the freedom to choose
any smooth time parametrization.

---

## Gauge Fixing

A choice of time coordinate is equivalent to **choosing ε(t)**.

Fixing a specific time parametrization (e.g., the CML
integration coordinate) **eliminates B(t)** — just as fixing
a gauge eliminates redundant degrees of freedom in
electromagnetism.

Conversely, changing the time coordinate **generates B(t)**
from the Lie algebra element `ε(t)`.

---

## Noether Perspective

Time reparametrization symmetry has an unusual Noether
structure:

- The symmetry group `Diff(R)` is infinite-dimensional
- The conserved current is **trivial** — no new observable
  charge emerges
- The generator `B(t)` is a **pure gauge mode**: it carries
  no energy, no momentum, no physical content

This is characteristic of **redundant symmetries**: the
conservation law exists mathematically but encodes no
independent physical information.

---

## Key Insight

```
    B(t) ∈ Lie(Diff(R))
```

B(t) is an element of the Lie algebra of time
reparametrization. It acts **additively** on the first-order
phase velocity `dθ/dt` because the group action on time
derivatives is linear at infinitesimal order.

---

## Consequence: Universality

**Any** theory with first-order time derivatives and unfixed
time parametrization **must** admit a B(t)-like term.

This is not a feature of TRM specifically — it is a structural
consequence of `Diff(R)` symmetry.

Examples:
- Classical mechanics with arbitrary time parametrization
- Relativistic point-particle action (reparametrization
  invariance)
- Any first-order phase or oscillator model

---

## Connection to BB01A–BB21

| Document | Result | Lie Group Perspective |
|----------|--------|-----------------------|
| BB01A    | Synchronization descriptive, not derivational | Fixing t → locking ε(t) |
| BB02A    | Energy minimization ranks modes, not Ω | Gauge-fixed analysis |
| BB17     | Non-global components constrained | Broken `Diff(R)` → observable |
| BB18     | Global B(t) = time redefinition | Finite `Diff(R)` transformation |
| BB19     | B(t) should be removed | Gauge fixing |
| BB21     | B(t) = reparametrization freedom | Infinitesimal `Diff(R)` |
| BB21+    | B(t) = Lie(Diff(R)) generator | Lie algebraic origin |

---

## Final Statement

**B(t) is not discovered — it is required by symmetry.**

It is the Lie algebra element of the time reparametrization
group `Diff(R)`, acting additively on first-order phase
dynamics. Its appearance is structurally inevitable in any
theory that does not fix the time coordinate a priori.

---

## Status

| Property            | Value                              |
|---------------------|------------------------------------|
| Mathematical origin | FULLY RESOLVED                     |
| Lie group           | `Diff(R)` — diffeomorphism group of R |
| Lie algebra         | `Vect(R)` — smooth vector fields   |
| Generator           | `B(t) = dε/dt`                     |
| Interpretation      | Pure gauge mode of time coordinate |
| Physical content    | NONE                               |
