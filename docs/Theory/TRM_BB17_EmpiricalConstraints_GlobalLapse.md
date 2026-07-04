## Title

BB17 — Empirical Constraints on the Global Lapse B(t)

---

## Context

Previous results established:

CML01–CML23:
- B(t) behaves as a global, additive, non-emergent time-rate shift
- B(t) is not equivalent to multiplicative time-rescaling
- B(t) must be strictly global to preserve synchronization

BB14–BB16:
- the physical origin of B(t) remains open
- empirical signatures were defined
- real observables were mapped to atomic clocks, UTC networks, and astrophysical timing

UTCr analysis:
- no robust universal common-mode drift detected in laboratory time data
- median-based statistics do not support a clean global signal

PHARAO-style differential analysis:
- a perfectly global B(t) cancels in ground-vs-space clock differences
- only departures from perfect globality are detectable

---

## Goal

Formulate empirical constraints on B(t) in terms of measurable non-globality.

---

## Core Principle

Experiments do not directly constrain the perfectly global component of B(t).

They constrain only deviations from exact globality.

Define:

    B_ground(t) = B0 + ε·t
    B_space(t)  = B0·(1 + spatialGradientFraction)
                  + ε·(1 + slopeDifferenceFraction)·t

Observable quantities:

    ΔB0      = B0_space - B0_ground
    ΔBdot    = dB_space/dt - dB_ground/dt

Only these differences are experimentally accessible.

---

## Constraint Class 1 — Ground Network (UTCr / clock ensembles)

Observation:
- no robust universal common-mode drift across stable laboratories
- mean-based trends are not supported by median/common-mode evidence

Implication:
- no positive evidence for a measurable global B(t) signature
  in current UTCr laboratory residuals

Constraint:
- any effective common-mode drift must lie below the residual noise floor
- any apparent drift dominated by a subset of laboratories is not acceptable
  as a candidate global B(t)

---

## Constraint Class 2 — Differential Space-Ground Tests

Observation:
- differential clock comparisons remove common-mode time shifts
- only non-global differences survive in the residual

Implication:
- a perfectly global B(t) is not excluded by such experiments
- only gradient-like departures from exact globality are constrained

Detectable parameters:
- spatialGradientFraction
- slopeDifferenceFraction

---

## Constraint Class 3 — Atomic Clock Precision

Observation:
- state-of-the-art clock comparisons strongly limit unexplained residual drift

Implication:
- any non-global component of B(t) must be smaller than
  current clock-comparison sensitivity

This constrains:
- |ΔB0|
- |ΔBdot|

not the perfectly common global component itself

---

## Empirical Interpretation

Two components must be separated:

### Component A — Perfectly Global B(t)

Properties:
- identical for all observers
- cancels in differential measurements
- may be absorbed into operational time definition

Status:
- not directly measurable
- not excluded

### Component B — Non-Global Residual

Properties:
- differs across observers, locations, or trajectories
- survives in differential measurements
- produces residual drift after removing known physics

Status:
- directly constrained by UTCr / PHARAO / precision clock tests

---

## Quantified Non-Globality

The relevant empirical parameters are:

    spatialGradientFraction
    slopeDifferenceFraction

Interpretation:

- spatialGradientFraction measures static non-globality
- slopeDifferenceFraction measures differential time evolution

PHARAO-like simulation results (5σ thresholds):

### Spatial Gradient

All tested fractions from 1e-6 to 1e-1 are **not detectable**.
A constant spatial offset in B(t) cancels in differential ground-vs-space
clock comparisons — only a slope difference produces a residual drift.
The spatial gradient fraction is therefore not constrained by this class
of experiment.

Constraint:

    |spatialGradientFraction| — unconstrained (constant offset invisible in Δ)

### Slope Difference

Minimum detectable slope difference fraction: **1.0 × 10⁻⁴** (5σ).

Constraint:

    |slopeDifferenceFraction| < 1.0 × 10⁻⁴   (5σ)

### Combined Constraints

    ΔB/B         — unconstrained (constant offset)
    |Δ(dB/dt)/(dB/dt)| < 1.0 × 10⁻⁴   (5σ)

Residual sensitivity:

    detectable residual slope ≈ 9 × 10⁻⁶ ns/day
    noise level ≈ 1.5 × 10⁻⁵ ns per epoch

---

## Rejected Scenarios

The following are inconsistent with current evidence:

- large non-global B(t)
- spatially varying medium-like B(t)
- strongly time-varying differential B(t)
- laboratory-specific effects masquerading as global lapse

---

## Surviving Possibilities

Only two possibilities remain:

### Case 1 — Perfectly Global / Operationally Hidden
B(t) is exactly common-mode and therefore invisible to differential experiments.

### Case 2 — Non-Global but Below Detection Threshold
B(t) has small departures from perfect globality,
but these are below current sensitivity.

---

## Key Insight

Current experiments do not rule out a perfectly global B(t).

They rule out only non-global or differential components
above the measured detection thresholds.

---

## Quantitative Goal

Constraints from PHARAO-style simulation (cross-checked against UTCr/common-mode analysis):

    |spatialGradientFraction| — unconstrained (constant offset cancels in differential measurements)
    |slopeDifferenceFraction| < 1.0 × 10⁻⁴   (5σ)

These are simulation-derived bounds from the `PharaoResidualAnalyzer`
gradient-fraction scans (`pharao_gradient_scan.csv`, `pharao_slope_scan.csv`).

---

## Key Statement

Empirical data constrain B(t) only through departures from perfect globality.

A perfectly global B(t) remains operationally hidden,
while any measurable non-global component is strongly limited.

---

## Status

Mechanism: CLOSED
Origin: OPEN
Perfect global component: unconstrained directly
Non-global component: strongly constrained

---

## Next Step

BB18 — Interpretational Consequences

- Is a perfectly global B(t) physically meaningful?
- Or operationally equivalent to a redefinition of time?
