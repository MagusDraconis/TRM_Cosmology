# TRM V5.6 — Minimal Generative Core Protocol

## Goal

Determine the minimal RecoverFP update-map conditions required to reproduce the finite-N branch split discovered in V5.3.

## Minimal-Core Hypotheses

| ID | Hypothesis | Falsification Criterion |
|----|-----------|------------------------|
| MGC1 | Full map irreducible | Any reduced map reproduces >= 80% of baseline branch structure |
| MGC2 | d↔K submap sufficient | DL→Cupd submap reproduces high-branch fraction and separation |
| MGC3 | Epoch ordering necessary | Reordered stages preserve branch outcomes |
| MGC4 | Five epochs necessary but stages simplifiable | Simplified internal stages at 5 epochs preserve outcomes |
| MGC5 | Amplification profile sufficient | Simplified surrogate amplifier reproduces branch split |

## Test Matrix

### Suite MGCE: Reduced Map Execution

Test d↔K submap sufficiency.

| Condition | Description |
|-----------|------------|
| Full baseline | Sm→RP→Nm→DL→Cupd, 5 epochs |
| Reduced submap A | Sm→DL→Cupd (skip RP, Nm), 5 epochs |
| Reduced submap B | DL→Cupd only (no Sm preamble), 5 epochs |
| Reduced submap C | DL→Cupd, 8 epochs (compensate for missing stages) |

Metrics:
- High-branch fraction
- d_mean separation
- K separation
- d/K sync rho
- Omega CV

### Suite MGCA: Reorder and Simplify

Test ordering and simplification necessity.

| Condition | Description |
|-----------|------------|
| Full baseline | Sm→RP→Nm→DL→Cupd |
| Reorder A | DL→Cupd→Sm→RP→Nm (reverse D/E vs A/B/C) |
| Reorder B | Sm→DL→Cupd→RP→Nm (DL early) |
| Simplify A | Sm→DL→Cupd (RP=identity, Nm=identity) |
| Simplify B | Sm→RP→Nm→DL→Cupd but Cupd uses normalized exp(K0) |

Metrics:
- High-branch fraction
- Branch flip count vs baseline
- d/K separation
- Invalid run count

### Suite MGCS: Surrogate Amplifier

Test simplified amplifier surrogates.

| Condition | Description |
|-----------|------------|
| Full baseline | Sm→RP→Nm→DL→Cupd |
| Surrogate A | Linear d→K→d update (no Sm, no RP, no Nm) |
| Surrogate B | Nonlinear d→K→d with saturating exp |
| Surrogate C | Normalized d/K feedback with fixed coupling profile |

Metrics:
- High-branch fraction
- Omega distribution overlap with baseline
- d/K sync rho
- Endpoint d_mean separation

## Regime Settings

| Parameter | Value |
|-----------|-------|
| xi | 1.75 |
| K0 | 1.20 |
| s | 0.10 |
| St | 300 |
| REps | 1e-6 |
| Dt | 0.05 |
| Hd | 4 |

## N and Seeds

| N | Seeds | Purpose |
|---|-------|---------|
| 67 | 0–49 | Primary test |
| 69 | 0–49 | Replication |
| 72 | 0–49 | Larger-N verification |

## Branch Labels

Frozen V5.3 rule:
- High branch: Omega > 1.783
- Low branch: Omega <= 1.783

## Decision Gates

| Gate | Condition | Interpretation |
|------|-----------|---------------|
| A | No reduced/surrogate map reproduces >= 80% branch structure | Full map currently irreducible |
| B | DL→Cupd submap reproduces >= 80% branch structure | d/K submap sufficient |
| C | Reordering destroys branch generation | Epoch ordering necessary |
| D | Simplified stages with preserved 5-epoch count work | Amplification depth over operator detail |
| E | Surrogate amplifier reproduces branch split | Mechanism reducible to simplified operator |
| F | Baseline fails or intervention violates invariants | Stop, audit implementation |

## Claim Discipline

- Do not claim physical interpretation.
- Do not discuss time, space, length, or c.
- Do not claim attractor decomposition or universal criticality.
- Use "conditionally associated" — no causation without controlled intervention.
- Do not generalize beyond tested N and seeds.
- Prefer falsification over confirmation.

## How to Run

```
dotnet test --filter "Category=V5_6"
```

## After Protocol

Recommended next suite: MGCE (Reduced Map Execution).
