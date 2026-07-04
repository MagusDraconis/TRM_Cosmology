# CML13 — Global Lapse Reconstruction Test

---

## Goal

Test whether the global time-dependent shift B(t)
can be reconstructed from the emergent collective frequency Ω*(t).

This verifies that B(t) is:

- directly encoded in the dynamics
- uniquely recoverable
- not entangled with synchronization structure

---

## Context

CML12 validated:

    Ω*(t) = ⟨ω_i⟩ + B(t)

This implies:

    B(t) = Ω*(t) − ⟨ω_i⟩

CML13 tests whether this inversion holds in practice — whether the background can be read back from the observables without knowing it in advance.

**Why this matters:** If B(t) is a physical background (like the metric in GR), it must be reconstructible from observables. A background that influences dynamics but cannot be recovered from them is not a physical field — it is a hidden variable. CML13 tests whether B(t) is observable.

---

## Core Question

Can we recover:

    B_reconstructed(t) ≈ B(t)

using only observed Ω*(t)?

---

## Test Setup

Use same simulation as CML12:

```
dθ_i/dt = ω_i + B(t) + coupling
```

Define:

```
B(t) = B0 + ε·t
```

Parameters:

| Parameter | Value | Source |
|:---|:---|:---|
| B0 | 0.0 | Baseline at intrinsic mean |
| ε | {0.0001, 0.001, 0.01} | Same sweep as CML12 |
| Dt | ~0.08 | CML default |
| Steps | ≥ 1500 | As CML12 |
| SettleSteps | ~600 | Transient excluded from measurement |
| CollectiveWeight | 0 | Self-organizing mode |
| CouplingKappa | 0.10 | CML default |
| ω_i spread | ~0.12 | CML default frequency pattern |

**Relation to CML12:** CML13 runs on the same simulation data. The difference is in the analysis: CML12 asks "does Ω*(t) track B(t)?", CML13 asks "can we recover B(t) from Ω*(t) without knowing it?"

---

## Measurements

At each window:

### 1. Measure Ω*(t)

```
Ω*(t) = (1/N) · Σ (φ_i(t + Δt_window) − φ_i(t)) / Δt_window
```

BB03D extraction method — mean unwrapped phase slope.

### 2. Compute reconstructed shift

```
B_rec(t) = Ω*(t) − ⟨ω_i⟩
```

Where ⟨ω_i⟩ ≈ 1.0 (verified by CML09 baseline).

**Key point:** ⟨ω_i⟩ is known independently from the static case (φ = 0, Ω* ≈ 1.0). This is not a fit — it is a prior measurement.

### 3. Compare with true B(t)

```
error(t) = B_rec(t) − B(t)
```

B(t) is the known input (ground truth). The reconstruction is blind to B(t) — it uses only Ω*(t) and ⟨ω_i⟩.

---

## Expected Results (STRICT)

### 1. Exact Recovery

```
B_rec(t) ≈ B(t)
```

to within extraction noise (standard deviation of Ω* measurement, typically < 1e-4 in CML09/CML11).

### 2. Error Flatness

```
error(t) ≈ 0
```

- No drift — mean error must not trend away from zero
- No slope — linear regression of error(t) must yield slope ≈ 0
- No curvature — no quadratic or secular trend
- Variance comparable to Ω* extraction noise in the static case

### 3. Linearity

```
B_rec(t) increases linearly with t
slope of B_rec ≈ ε
```

For each ε, the fitted slope must match ε to within measurement tolerance.

### 4. Spread invariance (diagnostic)

```
σ(t) = std(ω_i + B(t)) = std(ω_i) = constant
```

If the spread is constant, the shift is confirmed uniform. If spread changes, B(t) was not applied uniformly — implementation error or coupling interference.

---

## Failure Conditions

**FAIL if any occurs:**

| Failure | Interpretation |
|:---|:---|
| B_rec(t) deviates systematically from B(t) | Ω*(t) does not uniquely encode B(t) — coupling or nonlinearity injects additional dynamics |
| error(t) shows drift (non-zero slope) | ⟨ω_i⟩ is not constant over time — baseline shifts, invalidating the subtraction |
| error(t) shows curvature | B(t) interacts with coupling in a nonlinear way — the additive separation fails |
| slope(B_rec) ≠ ε | Reconstruction error is systematic, not random — B(t) is not fully recoverable |
| Noisy or unstable reconstruction (variance >> static baseline) | Time-dependence amplifies extraction noise beyond the static case |
| σ(t) changes with t | B(t) not applied uniformly — implementation error |

---

## Interpretation Rules

### If PASS (all assertions):

```
B(t) is fully encoded in Ω*(t).

→ Dynamics are invertible.
→ B(t) acts as a true global observable background.
→ An external observer measuring Ω*(t) can reconstruct B(t)
  without knowing it in advance.
→ Supports physical interpretation of B(t) as a real field.
```

### If FAIL (any assertion):

```
Ω*(t) and B(t) are not uniquely linked.

→ Hidden dynamics present — coupling or nonlinearity
  contributes to Ω*(t) beyond the additive shift.
→ B(t) is not fully observable from Ω*(t) alone.
→ The global lapse interpretation is incomplete.
→ B(t) may be partially entangled with synchronization structure.
```

---

## Diagnostic Extension

Optional: reconstruct B(t) using only a subset of oscillators:

```
B_rec_subset(t) = Ω*_subset(t) − ⟨ω_i⟩_subset
```

If B(t) is truly global, any subset should yield the same reconstruction. Disagreement between subsets indicates non-uniformity.

---

## Key Output

Log format:

```
t, B(t), Ω*(t), B_rec(t), error(t), R(t)
```

Example expected output (ε = 0.001):

```
t=0.00   B=0.000  Ω*=1.000  B_rec=0.000  error=0.00000  R=0.889
t=48.00  B=0.048  Ω*=1.048  B_rec=0.048  error=0.00000  R=0.889
t=96.00  B=0.096  Ω*=1.096  B_rec=0.096  error=0.00000  R=0.889
t=120.00 B=0.120  Ω*=1.120  B_rec=0.120  error=0.00000  R=0.889
```

B_rec(t) = B(t) exactly. error(t) = 0 throughout. R(t) flat.

---

## Key Statement

If B(t) can be reconstructed from Ω*(t)
with zero drift and correct slope,
then the global lapse behaves as a fully observable
and dynamically consistent background field.

**The bridge band is not a parameter — it is a measurement of B(t) at the epoch of observation.**
