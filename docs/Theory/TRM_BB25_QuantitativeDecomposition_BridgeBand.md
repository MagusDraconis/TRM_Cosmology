# BB25 — Quantitative Decomposition of the Bridge Band

---

## Context

BB23 formulated the minimal TRM/TQM model after gauge fixing.

BB24 identified the three physical components — `{ω_i}`,
`K_ij`, and topology — as the sole generators of all
observed phenomena, including the bridge band.

BB25 now decomposes the bridge band **quantitatively**:
which component contributes what, and why does the band
stabilize near Ω ≈ 1.16–1.19?

---

## Goal

Quantitatively explain the origin of the bridge band:

```
    Ω ≈ 1.16 .. 1.19
```

by measuring the contribution of each model component
and building a predictive mapping:

```
    ({ω_i}, K_ij, topology) → Ω_band
```

---

## Step 1 — Contribution of `{ω_i}` Distribution

**Design:**

Run simulations with controlled `{ω_i}` distributions:

| Case | Distribution | Parameters |
|------|-------------|------------|
| A1   | Narrow       | `σ_ω ≪ mean(ω_i)` |
| A2   | Wide         | `σ_ω ∼ mean(ω_i)` |
| A3   | Skewed       | Positive skew |
| A4   | Bimodal      | Two separated peaks |
| A5   | Uniform      | Flat spectrum |

**Measure:**
- Emergent frequency `Ω*`
- Bridge band location (center, width)
- Stability of the band under perturbations

**Expected insight:**
- `Ω*` should track `mean(ω_i)` regardless of distribution
- Band width should scale with `σ_ω`
- Skew may shift the band center

---

## Step 2 — Coupling Strength `K_ij` Scan

**Design:**

Fix `{ω_i}` distribution. Scan coupling strength:

```
    K ∈ [K_min, K_max]
```

across at least 3 orders of magnitude.

**Measure:**
- Synchronization threshold `K_crit`
- Bridge band existence region `[K_low, K_high]`
- Band center stability vs `K`
- Order parameter `R(t)` at each `K`

**Expected insight:**
- Below `K_crit`: no synchronization, no band
- Within `[K_low, K_high]`: bridge band appears
- Above `K_high`: full synchronization, band may widen or
  collapse
- Band center should be stable across the existence region

---

## Step 3 — Topology Variation

**Design:**

Fix `{ω_i}` and `K_ij`. Compare topologies:

| Topology | Description |
|----------|-------------|
| 1D nearest-neighbor | Ring lattice, local only |
| 2D nearest-neighbor | Grid lattice |
| All-to-all | Fully connected |
| Random (Erdős–Rényi) | Fixed edge probability |
| Small-world (Watts–Strogatz) | Short paths, high clustering |
| Scale-free (Barabási–Albert) | Hub-dominated |

**Measure:**
- Bridge band width per topology
- Stability region size
- Mode structure (which collective patterns emerge)
- Robustness to node removal

**Expected insight:**
- All-to-all: narrowest band, highest coherence
- Nearest-neighbor: wider band, richer mode structure
- Small-world: intermediate behavior, most "realistic"
- The 1.16–1.19 range may be **topology-specific**

---

## Step 4 — Nonlinearity of Coupling Function `f(Δθ)`

**Design:**

Fix `{ω_i}`, `K_ij`, and topology. Vary the coupling
function:

| Case | `f(Δθ)` | Description |
|------|---------|-------------|
| B1   | `sin(Δθ)` | Standard Kuramoto |
| B2   | `sin(2Δθ)` | Higher harmonic |
| B3   | `sin(Δθ) + α·sin(2Δθ)` | Mixed harmonics |
| B4   | Sawtooth | Non-smooth, piecewise linear |
| B5   | Custom nonlinear | Saturating, sigmoid, etc. |

**Measure:**
- Bridge band location shift vs `f(Δθ)`
- Stability basin size
- Harmonic content of locked phases

**Expected insight:**
- The band center at 1.16–1.19 may be **specific to `sin(Δθ)`**
- Higher harmonics shift the band
- The band is a **nonlinear resonance** — changing `f(Δθ)`
  changes the resonance condition

---

## Step 5 — Output Specification

### Plots

| Plot | Content |
|------|---------|
| `omega_star_vs_distribution.png` | `Ω*` vs `{ω_i}` statistics |
| `bridge_band_histogram.png` | Histogram of band centers across all runs |
| `stability_regions.png` | `(K, topology)` → band existence heatmap |
| `nonlinearity_shift.png` | Band center vs `f(Δθ)` parameter |
| `component_contribution.png` | Bar chart: variance explained by each component |

### Data Tables

| Table | Content |
|-------|---------|
| `frequency_contribution.csv` | `Ω*`, band center, band width per `{ω_i}` case |
| `coupling_scan.csv` | `K`, `R`, band exists, band center |
| `topology_scan.csv` | Topology, band width, stability region |
| `nonlinearity_scan.csv` | `f(Δθ)`, band center, harmonic content |

---

## Decomposition Analysis

For each simulation run, attribute the bridge band
variance to:

```
    Var(Ω_band) = Var(ω_i) + Var(K) + Var(topology) + Var(f) + residual
```

Use ANOVA or variance decomposition to quantify:

| Component | Expected Contribution |
|-----------|----------------------|
| `{ω_i}` distribution | Sets baseline `Ω*` |
| `K_ij` strength | Determines band existence |
| Topology | Shapes band width and mode structure |
| `f(Δθ)` nonlinearity | Sets precise band center |
| Residual | Higher-order interactions |

---

## Predictive Mapping

The goal is a reduced model:

```
    Ω_band = F(mean(ω_i), σ_ω, skew, K, topology_type, f_type)
```

where `F` is extracted from the simulation data via:
- Regression (linear, polynomial)
- Machine learning (if complexity warrants)
- Analytical ansatz (if structure reveals itself)

The mapping should predict:
- Band center → target: 1.16–1.19
- Band width → stability margin
- Existence region → where the band appears

---

## Final Question

**Why does Ω stabilize near 1.16–1.19?**

Three hypotheses:

### Hypothesis 1 — Statistical

The band is the **most probable** emergent frequency
given the `{ω_i}` distribution. It is a statistical
attractor — the mean of the distribution of possible
`Ω*` values.

*Test:* Vary `{ω_i}` — if the band shifts with the mean,
it is statistical.

### Hypothesis 2 — Dynamical

The band is a **nonlinear resonance** of the coupling
dynamics. It is a fixed point of the synchronization
manifold, selected by the interplay of `K_ij` and `f(Δθ)`.

*Test:* Change `f(Δθ)` — if the band shifts, it is dynamical.

### Hypothesis 3 — Topological

The band is determined by the **mode structure** of the
lattice. Only certain collective modes are supported by
the connectivity graph; the band is the one with minimum
energy or maximum stability.

*Test:* Change topology — if the band shifts or disappears,
it is topological.

---

## Expected Outcome

The decomposition will reveal whether the bridge band is:

- A **necessary** consequence of the model (robust across
  parameter variations) → fundamental prediction
- A **contingent** feature of specific parameter choices
  → calibration artifact
- A **mixed** phenomenon (multiple components contribute)
  → requires all three for full explanation

---

## Status

| Property          | Value                        |
|-------------------|------------------------------|
| Model             | MINIMAL (BB23)               |
| Physics mapped    | IDENTIFIED (BB24)            |
| Decomposition     | DESIGNED — pending execution |
| Predictive mapping| PENDING                      |
