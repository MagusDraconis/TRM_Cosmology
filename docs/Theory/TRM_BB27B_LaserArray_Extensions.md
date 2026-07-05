# BB27B — Laser Array Extensions Beyond the Phase-Only Model

---

## Context

BB27 designed the external validation protocol for TRM/TQM.

BB27A defined the execution plan using coupled laser arrays
as the first target system (Weizmann 400-laser array data).

BB27B now asks: **if the phase-only model does not fully
reproduce experimental scaling collapse, which physical
ingredient is missing — and which extension is minimally
required?**

---

## Goal

Identify which missing physical ingredients prevent full
scaling collapse in real laser-array experiments, and
determine the **minimal extension** needed to recover
agreement.

---

## The Phase-Only Baseline

The minimal TRM/TQM model (BB23):

```
    dθ_i/dt = ω_i + Σ_j K_ij · f(θ_i − θ_j)
```

assumes:

- Pure phase dynamics — no amplitude evolution
- Identical oscillators except for `ω_i`
- Ideal, symmetric coupling
- No dissipation or gain
- No noise
- Uncorrelated `ω_i` disorder

Real lasers violate several of these assumptions.

---

## Missing Physical Ingredients

### 1. Amplitude Dynamics

**What the phase-only model misses:**

Real lasers have evolving intensities `A_i(t)`. The full
dynamics are:

```
    dA_i/dt = (gain_i − loss_i) · A_i + coupling_amplitude
    dθ_i/dt = ω_i + (A_j / A_i) · K_ij · f(θ_i − θ_j)
```

**Effect on synchronization:**

- Amplitude imbalance `A_j / A_i ≠ 1` breaks the symmetry
  of the coupling term
- Oscillators with higher amplitude dominate the collective
  phase — the centroid condition `Ω = mean(ω_i)` generalizes
  to `Ω = weighted_mean(ω_i, A_i)`
- Amplitude fluctuations introduce effective frequency noise
  via the gain–frequency coupling (Henry factor / linewidth
  enhancement)

**Severity:** HIGH — amplitude dynamics are inseparable from
phase dynamics in semiconductor lasers.

---

### 2. Gain / Loss Imbalance

**What the phase-only model misses:**

Each laser has:

- Pump-dependent gain `g_i`
- Cavity loss `κ_i`
- The net gain `g_i − κ_i` determines whether the laser
  oscillates and at what amplitude

**Effect on synchronization:**

- Lasers with different net gain reach different steady-state
  amplitudes
- Gain competition can destabilize locking
- Pump fluctuations modulate both amplitude and frequency
  simultaneously (correlated noise)

**Severity:** MODERATE — gain/loss imbalance is a systematic
offset, not a fundamental obstruction.

---

### 3. Pump Dependence

**What the phase-only model misses:**

The pump current `I_pump` affects:

- Laser frequency (thermal tuning)
- Output power (gain)
- Linewidth (noise floor)

**Effect on synchronization:**

- Ramping pump current shifts `ω_i` — the `{ω_i}` distribution
  is not fixed but pump-dependent
- Pump noise adds a common-mode fluctuation that can mimic
  or mask synchronization

**Severity:** LOW–MODERATE — pump effects can be calibrated
out in controlled experiments.

---

### 4. Non-Ideal Coupling

**What the phase-only model misses:**

Real coupling is not perfectly symmetric:

```
    K_ij ≠ K_ji          (non-reciprocal coupling)
    K_ij = K_ij(λ)       (wavelength-dependent)
    K_ij = K_ij(r_ij)    (distance-dependent, not nearest-neighbour only)
```

**Effect on synchronization:**

- Non-reciprocal coupling breaks the double-sum cancellation
  that yields the centroid condition `Ω = mean(ω_i)`
- Long-range coupling tails modify the effective topology
- Wavelength dependence couples frequency disorder to
  coupling strength — a correlation the phase-only model
  treats as independent

**Severity:** HIGH — coupling non-ideality directly alters
the synchronization threshold and cluster scaling.

---

### 5. Disorder Correlations

**What the phase-only model misses:**

The phase-only model assumes `ω_i` are independent random
variables.

In real laser arrays:

- Adjacent lasers have correlated frequencies (thermal
  crosstalk, fabrication proximity)
- Frequency disorder correlates with coupling strength
  (both depend on waveguide geometry)
- Amplitude and frequency disorder are coupled (Henry factor)

**Effect on synchronization:**

- Correlated disorder reduces the effective `σ_ω` — the
  synchronization threshold is **lower** than predicted
  from uncorrelated `ω_i`
- Positive correlations enhance synchronization; negative
  correlations suppress it

**Severity:** MODERATE — disorder correlations can shift
the apparent threshold but are quantifiable.

---

## Which Extension Is Minimally Required?

### Candidate Extensions

| Extension | What It Adds | Complexity | Expected Impact |
|-----------|-------------|------------|-----------------|
| **A: Amplitude-weighted coupling** | `A_j/A_i` factor in `K_ij` | LOW — one additional variable per oscillator | Fixes centroid shift |
| **B: Gain/loss terms** | `dA_i/dt` equation | MODERATE — doubles state space | Stabilizes locking |
| **C: Non-reciprocal coupling** | `K_ij ≠ K_ji` matrix | MODERATE — breaks symmetry | Alters threshold |
| **D: Correlated disorder** | `Cov(ω_i, ω_j) ≠ 0` | LOW — correlation matrix | Shifts effective `σ_ω` |
| **E: Amplitude-phase coupling (Henry factor)** | `dθ/dt` depends on `dA/dt` | MODERATE — cross-coupling | Broadens locking window |
| **F: Full Lang–Kobayashi / SALT model** | Full laser rate equations | HIGH — 3–5 variables per laser | Complete description |

---

### Recommendation: Minimal Viable Extension

The **minimal extension** that recovers scaling collapse
without exploding complexity is:

```
    Extension A + Extension D
```

— **amplitude-weighted coupling** plus **correlated
frequency disorder**.

#### Rationale

1. **Amplitude weighting** (`A_j/A_i`):
   - Addresses the largest deviation: the centroid shift
     from `mean(ω_i)` to `weighted_mean(ω_i, A_i)`
   - Does not require solving full amplitude dynamics —
     steady-state amplitudes can be estimated from pump
     settings
   - Single additional observable per laser

2. **Correlated disorder** (`Cov(ω_i, ω_j)`):
   - Adjusts the effective `σ_ω` used in the threshold
     condition `K/σ_ω_eff > 1`
   - Can be extracted from spectral measurements of
     adjacent lasers
   - Does not change the structure of the equations — only
     the statistics of `{ω_i}`

With these two additions, the TRM/TQM structural predictions
(centroid, threshold, cluster scaling) should recover
agreement with laser array experiments.

---

## Testable Prediction

If Extensions A + D are sufficient, then:

1. After amplitude-weighting, the corrected centroid
   `Ω*_weighted = Σ(A_i²·ω_i) / Σ(A_i²)` should match
   the observed collective frequency within uncertainty.

2. After correcting for disorder correlations,
   `K / σ_ω_eff` should predict the synchronization
   threshold with the same critical value (~1) as in
   the phase-only model.

3. Cluster scaling `SyncMetric ~ 1/(Ω_eff/K)²` should
   recover an R² > 0.7 when using corrected parameters.

---

## If Extensions A + D Are Not Sufficient

Escalate to:

- **Extension B** (gain/loss dynamics) — if amplitude
  evolves significantly during locking transients
- **Extension C** (non-reciprocal coupling) — if symmetry
  breaking is observed in directional coupling experiments
- **Extension E** (Henry factor) — if linewidth broadening
  during locking indicates strong amplitude-phase coupling

Only if all of A–E fail should the full Lang–Kobayashi
or SALT model (Extension F) be required.

---

## Status

| Property              | Value                                    |
|-----------------------|------------------------------------------|
| Baseline model        | Phase-only (BB23)                        |
| Gaps identified       | 5 categories                             |
| Minimal extension     | Amplitude weighting + correlated disorder (A + D) |
| Fallback              | Full laser rate equations (F)            |
| Experimental test     | PENDING — requires amplitude data from laser array experiments |
