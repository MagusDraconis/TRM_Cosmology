# CML12 — Time-Evolving Global Lapse Validation

---

## Goal

Test whether a time-dependent global clock-bias term B(t)
acts as a pure uniform shift and preserves synchronization.

---

## Context

BB11 established:

    α·φ ≡ B (global shift)

BB12 extends:

    B → B(t)

with:

    Ω*(t) = ⟨ω_i⟩ + B(t)

---

## Core Question

Does a time-dependent global shift:

1. translate directly into Ω*(t)
2. preserve synchronization
3. leave frequency spread invariant

---

## Test Setup

Modify the phase evolution:

```
dθ_i/dt = ω_i + B(t) + K·coupling
```

Define:

```
B(t) = B0 + ε·t
```

Parameters:

| Parameter | Value | Rationale |
|:---|:---|:---|
| B0 | 0.0 | Start from intrinsic baseline |
| ε | {0.0001, 0.001, 0.01} | Span two orders of magnitude — slow drift to rapid ramp |
| Dt (step size) | ~0.08 | Existing CML default |
| Steps | ≥ 1500 | Sufficient for B(t) to accumulate measurable shift |
| SettleSteps | ~600 | Transient decay before measurement begins |
| CollectiveWeight | 0 | Self-organizing mode (standard BB03D pipeline) |
| CadenceScoreWeight | 0 | No external drive |
| CouplingKappa | 0.10 | CML default |
| ω_i definition | 1.0 + 0.05·sin(angle) + 0.03·cos(2·angle) | CML default, spread ≈ 0.12 |

**ε calibration:**

| ε | B(t) after 1500 steps (Δt ≈ 120) | Max Ω* shift | Regime |
|:---|:---|:---|:---|
| 0.0001 | 0.012 | ~1.012 | Slow drift — barely above noise |
| 0.001 | 0.12 | ~1.12 | Moderate — enters bridge-band neighborhood |
| 0.01 | 1.20 | ~2.20 | Rapid — large shift, stress-test |

---

## Measurements

At each time window (sliding or block-averaged):

### 1. Extract Ω*(t)

```
Ω*(t) = (1/N) · Σ (φ_i(t + Δt_window) − φ_i(t)) / Δt_window
```

BB03D method — mean unwrapped phase slope.

### 2. Compute residual

```
Δ(t) = Ω*(t) − B(t)
```

This should equal ⟨ω_i⟩ ≈ 1.0 at all t if tracking is exact.

### 3. Measure synchronization

```
R(t) = |(1/N) · Σ exp(i·θ_i(t))|
```

MeanOrder — standard Kuramoto order parameter.

---

## Expected Results (STRICT)

### 1. Linear Tracking

```
Ω*(t) ≈ 1.0 + B(t)
```

Ω*(t) must follow B(t) with slope 1.0 to within numerical tolerance (~1% for ε = 0.0001, tighter for larger ε).

### 2. Invariance

```
Δ(t) ≈ ⟨ω_i⟩ ≈ constant ≈ 1.0
```

The residual Δ(t) must be flat — no drift, no slope, no secular trend. Variance around 1.0 should match the static case (CML09/CML11 baseline).

### 3. Synchronization intact

```
R(t) ≥ 0.85 for all t
```

MeanOrder must not degrade as B(t) grows. The Kuramoto theory predicts R is independent of B(t) — this test verifies that prediction numerically.

### 4. Spread invariance (diagnostic)

```
σ_ω(t) ≈ σ_ω(0) — constant
```

The frequency spread across oscillators must remain unchanged. B(t) adds the same value to all ω_i; it cannot change the variance.

---

## Failure Conditions

**FAIL if any occurs:**

| Failure | Interpretation |
|:---|:---|
| Ω*(t) does NOT track B(t) with slope ≈ 1.0 | B(t) is not a pure additive shift — coupling or nonlinearity interferes |
| Δ(t) drifts over time (non-zero slope) | Residual systematic error in extraction or B(t) implementation |
| Synchronization degrades (R < 0.85) | Time-dependence breaks the Kuramoto cancellation — requires investigation |
| Phase instability or divergence | dt too large for B(t) rate of change; numerical instability |
| Spread σ_ω(t) grows with t | B(t) not applied uniformly — implementation error |

---

## Interpretation Rules

### If PASS (all 3 assertions):

```
B(t) is a pure global lapse term.
→ Supports: α·φ = time-rate background (BB12).
→ The bridge band is a snapshot of B(t), not a fixed constant.
→ Time-dependent clock-bias is structurally sound.
```

### If FAIL (any assertion):

```
Global lapse interpretation is incomplete.
→ Additional coupling or feedback may be required.
→ Time-dependence may introduce non-uniform effects.
→ BB12's claim that B(t) is a pure shift must be qualified.
```

---

## Minimal Implementation Hint

In `SimulateModeLock` loop:

Replace the static clock-bias:

```csharp
// Current (CML10/CML11):
double clockBiasShift = config.ClockBiasAlpha * config.ClockBiasPhi;

// Replace with time-dependent:
double B_t = B0 + epsilon * (step * Dt);
effectiveOmega = omegas[i] + B_t;
```

Phase snapshot extraction (BB03D) remains unchanged — `ExtractEmergentOmega` operates on stored phase arrays. To measure Ω*(t) at multiple windows, store phases at multiple checkpoints:

```
if (step % windowSize == 0 && step > SettleSteps)
    StorePhaseSnapshot(phases, step);
```

---

## Classification

This test validates:

- **Structural correctness of BB12** — does B(t) behave as a time-lapse background?
- **Independence from synchronization dynamics** — does R(t) remain constant?
- **Physical plausibility of time-evolving global shift** — can the bridge band be understood as a dynamical snapshot?

---

## Key Output

Log format:

```
t, B(t), Ω*(t), Δ(t), R(t)
```

Example expected output (ε = 0.001):

```
t=0.00   B=0.000  Ω*=1.000  Δ=1.000  R=0.889
t=48.00  B=0.048  Ω*=1.048  Δ=1.000  R=0.889
t=96.00  B=0.096  Ω*=1.096  Δ=1.000  R=0.889
t=120.00 B=0.120  Ω*=1.120  Δ=1.000  R=0.889
```

Δ(t) flat at 1.0. R(t) flat at 0.889. Ω*(t) = 1.0 + B(t) exactly.

---

## Key Statement

If Ω*(t) tracks B(t) exactly while R remains stable,
then the global clock-bias behaves as a true time-lapse background.

**The bridge band is not a fixed number — it is the value of B(t) at the epoch of observation.**
