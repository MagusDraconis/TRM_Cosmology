# BB24 — Physical Core of the TRM/TQM Model

---

## Context

BB21 identified B(t) as a Lie algebra element of `Diff(R)`.

BB22 gauge-fixed the time reparametrization, setting `B(t) = 0`.

BB23 formulated the minimal TRM/TQM model:

```
    dθ_i/dt = ω_i + Σ_j K_ij · f(θ_i − θ_j)
```

with intrinsic frequencies `ω_i`, coupling structure `K_ij`,
and lattice topology as the only physical degrees of freedom.

BB24 now asks: **what actually generates the observed
phenomena?**

---

## Goal

Identify which physical mechanisms — in the minimal,
gauge-fixed model — generate:

- synchronization
- the bridge band (Ω ≈ 1.16–1.19)
- phase locking
- mode selection

No global parameters. No hidden fields. Only `{ω_i}`, `K_ij`,
and topology.

---

## Core Components

### 1. Intrinsic Frequency Distribution `{ω_i}`

**Role:**
- Defines the natural timescale of each oscillator
- Determines the baseline emergent frequency `Ω* = mean(ω_i)`

**Key effect:**
- Sets the **location** of the emergent frequency on the
  number line

**Physical interpretation:**
- The `{ω_i}` distribution is the **raw material** — the
  spectrum of uncoupled natural rates. Everything else is
  built on top of it.

---

### 2. Coupling Structure `K_ij`

**Role:**
- Introduces interaction between oscillators
- Drives synchronization by opposing frequency differences

**Key effect:**
- Reduces phase differences `Δθ_ij`
- Generates collective behavior from individual oscillators

**Physical interpretation:**
- `K_ij` is the **force** that binds the lattice together.
  Without coupling, each oscillator runs at its own `ω_i`
  and no collective phenomena exist.

---

### 3. Topology (Connectivity Graph)

**Role:**
- Defines **which** oscillators interact
- Determines the **paths** along which phase information
  propagates

**Examples:**
| Topology | Description |
|----------|-------------|
| Nearest-neighbor lattice | Local interactions only |
| Fully connected network | All-to-all coupling |
| Sparse / random graph | Intermediate connectivity |
| Small-world | Short path lengths, high clustering |

**Key effect:**
- Determines **propagation speed** of phase information
- Affects **stability** and **synchronization threshold**
- Shapes **mode structure** (which collective patterns are
  supported)

---

## Mechanism Breakdown

### Synchronization

```
    Cause:  Coupling strength K_ij relative to frequency spread σ_ω
    Condition: K > K_crit(σ_ω)
    Result:  Order parameter R(t) → 1 (phase locking)
```

Synchronization is a **threshold phenomenon**: when coupling
overcomes the dispersion of natural frequencies, oscillators
lock into a common rhythm.

No external field is needed — the coupling alone is
sufficient.

---

### Emergent Frequency `Ω*`

```
    Cause:  Conservation of mean drift
    Result: Ω* = mean(ω_i)
```

The emergent frequency is the **average intrinsic frequency**
of the ensemble. It is independent of topology, coupling
strength, and initial conditions.

This is a mathematical identity, not a fitted parameter:
the center of mass of the frequency distribution cannot drift.

---

### Phase Locking

```
    Cause:  Balance between coupling and frequency mismatch
    Result: Constant phase differences Δθ_ij
```

Once synchronized, oscillators maintain fixed relative phases.
The pattern of locked differences is determined by:
- The coupling function `f(Δθ)`
- The frequency mismatches `ω_i - ω_j`
- The topology (which oscillators feel each other's pull)

---

### Bridge Band (Ω ≈ 1.16–1.19)

**Hypothesis — not caused by a global parameter:**

The bridge band emerges from the **nonlinear interaction** of:

1. **Frequency distribution shape** — skew, spread, modality
   of `{ω_i}`
2. **Nonlinear coupling function** `f(Δθ)` — sinusoidal,
   sawtooth, or more general
3. **Topology constraints** — which modes the lattice can
   support

The band `1.16–1.19` is a **collective resonance** — a
frequency window where the lattice sustains stable,
low-energy synchronized motion. It is not imposed; it
**emerges**.

---

## Key Insight

> **All observed phenomena arise from the interaction
> between `{ω_i}`, `K_ij`, and topology.**

No global parameter is required. No B(t). No external
medium. No background field.

The bridge band is not a **parameter** of the model —
it is a **prediction** of the model.

---

## Reduced Causal Structure

| Phenomenon         | Cause                          |
|--------------------|--------------------------------|
| `Ω*`               | `mean(ω_i)`                    |
| Synchronization    | `K_ij` vs `σ_ω`                |
| Phase locking      | Coupling vs frequency mismatch |
| Mode selection     | Topology + coupling function   |
| Bridge band        | Emergent (nonlinear mix of all three) |

---

## Testable Predictions

The minimal model makes falsifiable predictions:

1. **Changing `{ω_i}` distribution:** shifts `Ω*`
   — test by varying the input frequency spectrum

2. **Changing `K_ij`:** changes synchronization threshold
   — test by varying coupling strength

3. **Changing topology:** alters stability and propagation
   — test by rewiring the lattice (nearest-neighbor →
     all-to-all → sparse)

4. **Removing coupling:** destroys all collective behavior
   — test by setting `K_ij = 0`

5. **Bridge band dependence:** the band location and width
   should vary predictably with `{ω_i}` statistics and
   coupling nonlinearity

---

## Final Statement

> The physical content of TRM/TQM is fully captured by:
>
>     intrinsic frequencies + coupling + topology
>
> All higher-level phenomena — synchronization, phase locking,
> mode selection, the bridge band — are **emergent
> consequences** of these interactions.

There is no hidden fifth force, no global lapse, no external
clock. The model is **self-contained**.

---

## Status

| Property            | Value                          |
|---------------------|--------------------------------|
| Model               | MINIMAL                        |
| Degrees of freedom  | `{θ_i, ω_i, K_ij}`             |
| Redundancy          | NONE                           |
| Physics             | IDENTIFIED — fully mapped      |
| Bridge band origin  | EMERGENT — nonlinear collective resonance |

---

## Next Step

**BB25 — Quantitative Decomposition**

- Measure the contribution of each component (`{ω_i}`,
  `K_ij`, topology) to `Ω*` and the bridge band
- Isolate the bridge-band origin numerically
- Build a reduced predictive model that maps
  `({ω_i}, K_ij, topology)` → `(Ω*, bridge band)`
