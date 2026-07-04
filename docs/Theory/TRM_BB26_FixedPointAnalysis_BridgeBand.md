# BB26 — Fixed-Point Analysis of the Bridge Band

---

## Context

BB23 formulated the minimal TRM/TQM model:

```
    dθ_i/dt = ω_i + Σ_j K_ij · f(θ_i − θ_j)
```

BB24 identified the three physical components — `{ω_i}`,
`K_ij`, and topology — as the generators of all phenomena.

BB25 designed a quantitative decomposition to measure each
component's contribution to the bridge band.

BB26 now takes the **analytical** route: treat the bridge
band as a **fixed point** of the nonlinear coupled dynamics
and determine whether Ω ≈ 1.16–1.19 emerges as a stable
attractor.

---

## Goal

Identify the bridge band Ω ≈ 1.16–1.19 as a **stable
fixed point** of the nonlinear coupled oscillator system —
not a fitted parameter, but a dynamically selected
collective frequency.

---

## Step 1 — Rotating Frame

Define the collective frequency Ω and transform to the
co-rotating frame:

```
    θ_i(t) = Ω·t + φ_i(t)
```

where `φ_i(t)` are the phase deviations from uniform
rotation.

Substitute into the dynamics:

```
    dφ_i/dt = ω_i − Ω + Σ_j K_ij · f(φ_i − φ_j)
```

The term `ω_i − Ω` is the **frequency detuning** — the
mismatch between each oscillator's natural rate and the
collective rotation.

---

## Step 2 — Stationary Condition

A fixed point in the rotating frame corresponds to
**phase-locked** motion in the original frame:

```
    dφ_i/dt = 0    for all i
```

This yields the stationary equations:

```
    ω_i − Ω + Σ_j K_ij · f(φ_i − φ_j) = 0    (i = 1, …, N)
```

This is a system of `N` equations in `N` unknowns
(`φ_1, …, φ_N`), with Ω as a parameter.

---

## Step 3 — Consistency Condition (Centroid)

Summing over all `i`:

```
    Σ_i (ω_i − Ω) + Σ_i Σ_j K_ij · f(φ_i − φ_j) = 0
```

If the coupling function `f` is odd — `f(−x) = −f(x)` —
then the double sum vanishes term by term:

```
    Σ_i Σ_j K_ij · f(φ_i − φ_j) = 0
```

(Each pair `(i,j)` contributes `f(φ_i−φ_j) + f(φ_j−φ_i) = 0`.)

This forces:

```
    Σ_i (ω_i − Ω) = 0
    ⇒
    Ω = (1/N) · Σ_i ω_i = mean(ω_i)
```

**First result:** the collective frequency is **fixed** to
the mean intrinsic frequency, independent of coupling
strength and topology. This is the **centroid condition**.

---

## Step 4 — Phase Pattern

With Ω determined, the stationary equations reduce to:

```
    ω_i − mean(ω) + Σ_j K_ij · f(φ_i − φ_j) = 0
```

This determines the **locked phase pattern** `{φ_i}`.

The solution exists if the coupling is strong enough to
compensate for the frequency detunings `ω_i − mean(ω)`.

**Existence condition:**

```
    max_i |ω_i − mean(ω)| ≤ K_eff · max|f|
```

where `K_eff` is the effective coupling per oscillator
(sum of incoming `K_ij`). This is the **synchronization
threshold**.

---

## Step 5 — Linear Stability

Linearize around the fixed point `φ_i*`:

```
    φ_i = φ_i* + δ_i
```

The perturbation dynamics are:

```
    dδ_i/dt = Σ_j J_ij · δ_j
```

where the Jacobian is:

```
    J_ij = K_ij · f'(φ_i* − φ_j*)           (i ≠ j)
    J_ii = − Σ_{j≠i} K_ij · f'(φ_i* − φ_j*)   (diagonal)
```

**Stability criterion:**

All eigenvalues of `J` must have non-positive real parts.
The fixed point is **stable** if the coupling function's
derivative `f'` is positive at the locked phase differences.

For `f(Δθ) = sin(Δθ)`, this means:

```
    cos(φ_i* − φ_j*) > 0    for all coupled pairs (i,j)
```

— all phase differences must lie within (−π/2, π/2). This
constrains the admissible frequency spread `σ_ω`.

---

## Step 6 — The Bridge Band as a Fixed-Point Window

The bridge band Ω ≈ 1.16–1.19 is not a single fixed point
but a **window of stable fixed points**.

**Why a band, not a point?**

1. **Frequency distribution width `σ_ω`:**
   The centroid `Ω = mean(ω_i)` is fixed, but the phase
   pattern `{φ_i}` can vary within a continuous family
   parameterized by the initial conditions.

2. **Stability margin:**
   Not all phase patterns satisfying the stationary
   equations are stable. The linear stability condition
   restricts the admissible patterns to a subset — the
   **stability basin**.

3. **The band is the projection** of the stability basin
   onto the Ω-axis. Different stable phase patterns
   correspond to slightly different effective collective
   frequencies — hence a band, not a single value.

---

## Step 7 — Dependence on Model Parameters

| Parameter | Effect on Fixed Point |
|-----------|----------------------|
| `mean(ω_i)` | Sets Ω (centroid) |
| `σ_ω` | Determines existence (detuning must be compensable) |
| `K_ij` strength | Broadens stability region |
| Topology | Determines admissible mode structure |
| `f(Δθ)` form | Sets stability condition via `f'` |

The bridge band **center** is set by `mean(ω_i)`.

The bridge band **width** is determined by the interaction
of `σ_ω`, `K_ij`, and `f'`.

---

## Question: Does Ω ≈ 1.16–1.19 Emerge as a Stable Fixed Point?

**Answer structure:**

1. If `mean(ω_i) ≈ 1.175` (midpoint of 1.16–1.19):
   → centroid condition satisfied

2. If `σ_ω` is such that the detunings are within the
   coupling's compensation range:
   → existence condition satisfied

3. If the locked phase differences lie within (−π/2, π/2):
   → stability condition satisfied

Then:

> **Yes — Ω ≈ 1.16–1.19 is the stable fixed-point window
> of the coupled system.**

The band is a **dynamical attractor**, not a free parameter.
It is selected by the interplay of the frequency distribution
and the coupling nonlinearity.

---

## Output Specification

### Analytical Outputs

| Output | Description |
|--------|-------------|
| `Ω_solutions.csv` | Fixed-point Ω for each parameter set |
| `stability_eigenvalues.csv` | Largest real part of Jacobian eigenvalues |
| `stability_region.csv` | `(σ_ω, K)` → stability (boolean) |

### Derived Quantities

| Quantity | Formula |
|----------|---------|
| Centroid condition | `Ω = mean(ω_i)` |
| Existence condition | `max|ω_i − Ω| ≤ K_eff · max|f|` |
| Stability condition | `cos(φ_i* − φ_j*) > 0` for all `(i,j)` coupled |
| Band width | `ΔΩ ∼ σ_ω · f'(0) / K_eff` (first-order estimate) |

---

## Final Goal

Explain **why** Ω is selected dynamically:

> The bridge band is the set of collective frequencies Ω
> for which the coupled oscillator system admits a **stable
> phase-locked solution** satisfying the centroid condition
> `Ω = mean(ω_i)` and the linear stability criterion
> `cos(Δφ_ij) > 0`.

It is not imposed — it is **selected** by the dynamics.

---

## Status

| Property              | Value                              |
|-----------------------|------------------------------------|
| Analytical framework  | DEFINED                            |
| Centroid condition    | `Ω = mean(ω_i)` — proven            |
| Existence condition   | Parametrized on `σ_ω`, `K_eff`      |
| Stability condition   | Jacobian eigenvalues — defined      |
| Band interpretation   | Stability basin projection onto Ω   |
| Numerical verification | PENDING                            |
