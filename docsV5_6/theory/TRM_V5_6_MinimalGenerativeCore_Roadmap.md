# TRM V5.6 — Minimal Generative Core Roadmap

## Overview

V5.6 investigates whether the full RecoverFP 5-stage, 5-epoch update map is irreducible or can be reduced to a minimal generative core.

### Motivation

V5.5 established that:
- Branch generation requires the full 5-epoch update cycle.
- No single stage dominates.
- d and K amplify synchronously.
- Omega is downstream.

But V5.5 did NOT test:
- Whether intermediate stages (RP, Nm) are necessary.
- Whether the Sm→RP→Nm preamble is required or if DL→Cupd alone suffices.
- Whether stage ordering matters.
- Whether a simplified surrogate amplifier reproduces branch outcomes.

### Core Question

Is the generative core:
1. The full Sm→RP→Nm→DL→Cupd map (irreducible),
2. The d↔K submap DL→Cupd (sufficient),
3. The amplification profile regardless of operator detail,
4. Or something simpler?

## Research Plan

### Phase 1: MGCP (Protocol)
Define minimal-core hypotheses (MGC1-MGC5), decision gates (A-F), and test matrix.

### Phase 2: MGCE (Reduced Map Execution)
Test MGC2: Is DL→Cupd submap sufficient?

Run reduced map: Sm→DL→Cupd (skip RP, Nm).
Compare to full map baseline.

### Phase 3: MGCA (Reorder/Simplify Audit)
Test MGC3-MGC4: Is ordering necessary? Can stages be simplified?

- Reorder: DL→Cupd→Sm vs Sm→DL→Cupd
- Simplify: replace RP+Nm with identity, replace Cupd with normalized exponential

### Phase 4: MGCS (Surrogate Amplifier Test)
Test MGC5: Can simplified amplifier surrogates reproduce branch split?

- Linear d/K amplifier
- Nonlinear d/K amplifier
- Normalized d/K feedback surrogate

## Metrics

Per condition:
- High-branch fraction
- Omega mean, CV
- d_mean separation between future branches
- K separation between future branches
- d/K synchronization correlation (rho)
- Baseline flip count vs full map

## N and Seeds

| Parameter | Values |
|-----------|--------|
| N | 67, 69, 72 |
| Seeds | 0–99 |
| Threshold | Omega > 1.783 (V5.3 frozen) |

## Expected Outcomes

| Hypothesis | If Supported | Next Step |
|-----------|-------------|-----------|
| MGC1 | Full map irreducible | Formal operator analysis (V5.7) |
| MGC2 | d/K submap sufficient | Formalize d/K operator |
| MGC3 | Ordering necessary | Order-sensitivity audit |
| MGC4 | 5 epochs necessary but stages simplifiable | Stage simplification matrix |
| MGC5 | Amplification profile sufficient | Amplification manifold audit |

## Claim Discipline

- No physical interpretation.
- No attractor decomposition.
- No time/space/length/c.
- Conditional claims only.
- Conservative: prefer falsification.
