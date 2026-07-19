# V5.40 Causal Testing Protocol

**Version:** 1.0 | **Date:** 2026-07-19
**Suite:** CTP
**Status:** DRAFT

---

## 1. Purpose

Define the constraints, allowed variables, and testing framework for V5.40
causal closure testing against attractor topology.

## 2. Frozen Policy

- M3++ model: FROZEN
- c3OmegaShift threshold 0.1: FROZEN
- Stop-Low policy: FROZEN
- No new variables.
- No new correction classes.
- No V6 derivations (length, space, velocity, c).
- No physical interpretation.

## 3. Allowed Variables

- c3OmegaShift
- Omega T1, Omega T2
- omDist
- lambda1
- rebMagnitude
- d_tail
- deltaD
- deltaK
- kSensitivity
- omegaPerK
- strict persistence
- rescue yes/no
- damage yes/no
- N
- cohort

## 4. Perturbation Families (Draft)

| Family | Name | Description |
|:-------|:-----|:------------|
| P0 | Baseline replay | Reproduce V5.38/V5.39 baseline |
| P1 | Multi-stage coupled | Perturb d_tail + K simultaneously |
| P2 | Attractor-aligned | Perturb along natural attractor gradient direction |
| P3 | Topology grid | Systematic parameter grid mapping attractor response |
| P4 | Timing-gated | Vary perturbation timing relative to C3 |

## 5. Validity Requirements

- Valid d, K, Omega state
- No NaN / Inf
- No threshold-only artifacts
- No Stop-Low mutation
- No M3++ mutation

## 6. Decision Gates

| Gate | Description | Criterion |
|:-----|:------------|:----------|
| A | Baseline reproduced | V5.38/V5.39 baseline hierarchy matches |
| B | Directional signal | Any perturbation produces directional c3OmgS shift |
| C | Causal vs diagnostic | Effect distinguishable from diagnostic correlation |
| D | Hostile audit | Effect survives independent audit |
| E | Stop-Low safe | No policy violation |
| F | V6 not ready | Confirmed |

---

*Generated 2026-07-19. V5.40 protocol draft.*
