# CML14 — Global Lapse Perturbation Response Test

---

## Goal

Test whether the global background B(t)
behaves like a physically consistent field under perturbations.

---

## Context

| Test | Validated |
|:---|:---|
| CML12 | Forward tracking: Ω*(t) = ⟨ω_i⟩ + B(t) |
| CML13 | Invertibility: B_rec(t) = Ω*(t) − ⟨ω_i⟩ ≈ B(t) |

Next requirement:

> A physical background must respond consistently to perturbations.

If B(t) is a real field, the system's response to changes in B(t) must be instantaneous, uniform, and separable from internal dynamics. Any delay, overshoot, ringing, or coupling interference would indicate that B(t) is not a pure background — it interacts with the oscillator dynamics.

---

## Core Question

What happens if B(t) is perturbed?

Does the system:

1. track changes instantly (uniform response)
2. remain synchronized
3. preserve reconstruction

---

## Test Setup

Base system:

```
dθ_i/dt = ω_i + B(t) + coupling
```

Define baseline:

```
B(t) = B0 + ε·t
```

Parameters as CML12/CML13: B0 = 0.0, ε = 0.001 (moderate drift), Dt ~ 0.08, Steps ≥ 2000, SettleSteps ~ 600, CollectiveWeight = 0, CouplingKappa = 0.10.

---

## Perturbation Cases

### Case A: Step

```
B(t) += ΔB · H(t − t0)

H = Heaviside step
t0 = 1000 · Dt  (well after settling)
ΔB ∈ {+0.01, −0.01, +0.05}
```

Tests instantaneous response to a sudden permanent shift. Ω*(t) must jump by exactly ΔB at t0 with no transient.

### Case B: Pulse

```
B(t) += ΔB · exp(−(t − t0)² / (2σ²))

t0 = 1000 · Dt
σ = 50 · Dt  (width ~4 time units)
ΔB ∈ {+0.01, +0.05}
```

Tests response to a transient perturbation. Ω*(t) must trace the Gaussian envelope with no lag, no broadening, and no ringing after the pulse decays.

### Case C: Noise

```
B(t) += ξ(t)

ξ(t) ~ N(0, σ_noise²)
σ_noise ∈ {0.001, 0.005}
```

Tests whether random fluctuations in B(t) are faithfully transmitted to Ω*(t) without amplification, attenuation, or coupling-induced filtering.

---

## Measurements

| Quantity | Formula | Purpose |
|:---|:---|:---|
| Ω*(t) | Mean phase slope (BB03D) | Collective frequency |
| B_rec(t) | Ω*(t) − ⟨ω_i⟩ | Reconstructed background |
| R(t) | \|(1/N)·Σ exp(iθ_i)\| | Synchronization order parameter |
| Δ(t) | Ω*(t) − B(t) | Residual — should equal ⟨ω_i⟩ |

---

## Expected Results (STRICT)

### 1. Instant Global Response

Ω*(t) follows B(t) immediately:
- No delay between perturbation onset and Ω* response
- No phase lag in pulse tracking
- Cross-correlation peak of B(t) and Ω*(t) at lag = 0

### 2. Reconstruction Stability

B_rec(t) ≈ B(t) during and after perturbation:
- No overshoot at step transitions
- No distortion of pulse shape (Gaussian in → Gaussian out, same σ)
- No ringing after pulse decay
- Noise faithfully reproduced (same σ, no filtering)

### 3. Synchronization Stability

```
R(t) ≥ 0.85 at all t
```

Perturbations must not disrupt phase coherence. A uniform shift to all oscillators cannot break synchronization (Kuramoto cancellation holds at each instant), but this must be verified under discontinuous changes.

### 4. Residual Invariance

```
Δ(t) = Ω*(t) − B(t) ≈ ⟨ω_i⟩ ≈ constant
```

The residual must be flat through all perturbations. A spike in Δ(t) at a step discontinuity would indicate that Ω*(t) needs finite time to catch up — a failure of instantaneous response.

---

## Failure Conditions

**FAIL if any occurs:**

| Failure | Interpretation |
|:---|:---|
| Delay between B(t) and Ω*(t) | The background does not couple instantaneously — propagation or inertia present |
| Overshoot or ringing in Ω*(t) at step | Coupling introduces damping/inertia — B(t) is not purely additive |
| Pulse shape distortion (broadening, asymmetry) | The system filters B(t) — time-resolution is finite and coupling-dependent |
| B_rec(t) deviates or lags during perturbation | Reconstruction fails under transients — B(t) not fully encoded in Ω*(t) |
| R(t) drops significantly (< 0.85) | Perturbation interacts with synchronization — uniform-shift assumption fails |
| Δ(t) not constant across perturbations | The additive separation breaks — B(t) and coupling are not independent |
| Noise amplification/attenuation | B(t) is filtered by dynamics — not a pure background |

---

## Interpretation Rules

### If PASS (all cases, all assertions):

```
B(t) behaves as a true physical background field.

→ Instantaneous global coupling — no propagation delay.
→ No propagation delay — the shift is truly global, not local.
→ Fully separable from internal dynamics — B(t) and coupling
  do not interact.
→ The background is a real, measurable, externally perturbable field.
```

### If FAIL (any case, any assertion):

```
Background interacts with dynamics.

→ Feedback present — B(t) is not purely additive.
→ Coupling not purely additive — the separation
  dθ/dt = ω_i + B(t) + coupling is incomplete.
→ BB12 (global lapse) incomplete — B(t) is entangled
  with oscillator dynamics rather than being a true background.
→ If delay is present: the shift propagates through the lattice
  rather than being instantaneously global — this would
  contradict the BB11 buoyancy interpretation.
```

---

## Diagnostic Extension

### Cross-correlation analysis

```
CC(τ) = ⟨B(t) · Ω*(t + τ)⟩

Peak must be at τ = 0 for instantaneous response.
Width of peak indicates temporal resolution limit.
Asymmetry indicates causality violation or filtering.
```

### Spectral analysis (Case C)

```
PSD_B(f) vs PSD_Ω(f)

Transfer function H(f) = PSD_Ω(f) / PSD_B(f)

Expected: H(f) ≈ 1 for all f (flat transfer function).
Any frequency-dependent filtering indicates coupling interaction.
```

---

## Key Output

```
t, B(t), Ω*(t), B_rec(t), R(t), Δ(t)
```

Example expected output for Case A (step ΔB = +0.05 at t0 = 80.0):

```
t=79.92  B=0.080  Ω*=1.080  B_rec=0.080  Δ=1.000  R=0.889
t=80.00  B=0.130  Ω*=1.130  B_rec=0.130  Δ=1.000  R=0.889  ← step
t=80.08  B=0.130  Ω*=1.130  B_rec=0.130  Δ=1.000  R=0.889
```

Ω* jumps by exactly +0.05 in one timestep. R unchanged. Δ flat.

---

## Key Statement

A true global lapse must propagate instantly
and remain fully separable from synchronization dynamics
under all perturbations.

**If it does, B(t) is not a parameter of the oscillators — it is a field in which they are embedded.**
