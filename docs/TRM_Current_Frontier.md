# TRM Current Frontier

**Version:** 1.1
**Date:** 2026-07-19

**Current Version:** V5.28 INITIALIZED
**Current Branch:** `feature/v5.28-rescue-risk-stratum-and-control-policy`
**Cumulative Tests:** 2762
**Failed:** 0

---

## Current Preferred Restart Documents

For any new LLM chat or Copilot session, read in this order:

1. `TRM_Current_Frontier.md` (this file) — active frontier, model, constraints, next prompt
2. `TRM_Project_QuickStart_For_New_Chats.md` — operational briefing, supported findings, open problems
3. `TRM_Project_Lineage_Overview.md` — complete V1 → V5.27 historical lineage

---

## One-Sentence Current State

The project has completed V5.27 probability calibration: c3OmegaShift > 0.1 defines a validated
rescue-enriched risk stratum (P_B=9.6%, P_A=0.0%). V5.28 now moves to policy evaluation of this
risk stratum.

---

## Current Preferred Model

**M3++** — unchanged from V5.17–V5.18.

**V5.27 Risk Stratum:**
- **c3OmegaShift > 0.1** defines a rescue-enriched stratum
- P(rescue | c3OmgS > 0.1) = 9.6% [6.6%–13.6%]
- P(rescue | c3OmgS ≤ 0.1) = 0.0% [0.0%–0.3%]
- Full C3 gain chain is mechanistically valid but not the best practical predictor
- c3OmegaShift is the minimal robust sufficient summary

---

## Current Preferred Model

**M3++** — the preferred validated adaptive model:

- **P1/P1b** Compression-Room pathway (primary transferable entry pathway)
- **projHiVec** selector (validated residual selector, +12.5% holdout at N=71)
- **orthHiVec** refinement at N=72 (+14.1% holdout)
- **rebMagnitude** adaptive probe (post-intervention rebound signal)
- **C3** entry-vector re-alignment correction (adaptive correction that breaches static ceiling)

---

## Validated Operating Domain

| N Range | Status |
|:--------|:-------|
| N=50–64 | Inaccessible / rescue-immune under tested operator classes |
| N=65–79 | Adaptive-active — C3 correction produces lift |
| N=72 | Peak adaptive response |
| N=80+ | Saturated — static success already high |

Domain is bounded, not universal.

---

## Complete Validated C3 Gain Chain

```
d_tail → deltaD → deltaK → omegaPerK sign → c3OmegaShift → rescue
```

**Layer notes:**

1. **d_tail** — Enables C3 movement. Necessary but not sufficient for rescue.
2. **deltaD** — Transfers through stable d→K coupling.
3. **deltaK** — Intermediate gain stage.
4. **omegaPerK sign** — Final gain bottleneck. Controlled by enrichment rule:
   `omDist < 0.5 AND lambda1 < 0.95`. Precision ~81%, enrichment ~5.5×.
5. **c3OmegaShift** — Aggregate Omega response to C3 correction. Enriches rescue.
6. **rescue** — Probabilistic outcome. No single layer guarantees it.

Movement quality (direction, alignment) dominates movement quantity. V5.26 rejected
deterministic threshold rescue.

---

## Most Recent Key Findings

| Version | Finding |
|:--------|:--------|
| V5.21 | N<65 remains rescue-immune under tested operator classes. Transferred-reference hypothesis falsified. |
| V5.22 | C3 gain activates at N=65 via resonant reversal: anti-aligned seed + large dT1 + C3 correction → large Omega response → rescue. |
| V5.23 | C3 gain decomposed: d_tail is necessary but not sufficient; omegaPerK is final gain bottleneck. |
| V5.24 | omegaPerK sign rule discovered: omDist < 0.5 AND lambda1 < 0.95. |
| V5.25 | Sign rule validated: precision ~81%, enrichment ~5.5×. Rule-positive rescue ~11%, rule-negative ~2%. Sign rule is not sufficient for rescue. |
| V5.26 | Movement magnitude does not predict rescue. Rescue conversion is probabilistic. Quality > quantity. |
| V5.27 | **COMPLETE** — c3OmegaShift > 0.1 frozen two-stratum risk table. P_A=0.0%, P_B=9.6%. Independent validation passed. Zero inversions. |
| V5.28 | **INITIALIZED** — risk-stratum policy: can the validated risk stratum define safe adaptive control? |

---

## Current V5.28 Research Question

**Can the validated c3OmegaShift risk stratum define a safe adaptive control policy
that reduces wasted interventions while preserving zero damage?**

Expected tasks:
- Gate C3 application by c3OmegaShift expectation or observation
- Measure reduction in wasted interventions
- Measure reduction in false-positive adaptive actions
- Verify zero-damage preservation
- Evaluate practical rescue efficiency gain

---

## V5.28 Constraints

- Do not modify M3++.
- Do not retune c3OmegaShift threshold (0.1 is frozen).
- Do not add new correction classes.
- Do not add new variables.
- Policy gating only — no mechanism modification.
- Use only validated chain variables and the frozen risk table.

---

## Claim Discipline

### SUPPORTED

- M3++ adaptive model validated within tested domain.
- V5.27: c3OmegaShift > 0.1 defines rescue-enriched risk stratum.
- V5.27: P_A = 0.0% [0.0%–0.3%], P_B = 9.6% [6.6%–13.6%].
- Full C3 gain chain is mechanistically valid.
- c3OmegaShift is the minimal robust predictive summary.
- Rescue is probabilistic, not deterministic.

### CONDITIONAL

- All findings are finite-N and operator-class limited.
- V5.27 calibration is conservative, not precise.
- N=50–64 is inaccessible. N=65–79 is adaptive-active.

### HYPOTHESIS

- V5.28: Risk-stratum policy can reduce wasted interventions.
- V5.28: Policy gating can preserve zero damage.

### NOT CLAIMED

- Physical constants, spacetime, relativity, quantum mechanics, cosmology
- Universal Low→High control
- Universal adaptive control
- N<65 impossibility under all future operators
- Deterministic rescue threshold
- Physical interpretation of N-boundaries
- Physical criticality
- Full-chain predictive superiority

---

## Recommended V5.28 Next Prompt

```
You are acting as a TRM/TQM V5.28 risk-stratum policy agent.

Current branch:
feature/v5.28-rescue-risk-stratum-and-control-policy

Base:
V5.27 COMPLETE

Current cumulative state:
2762 tests passed
0 failed

Purpose:
Evaluate whether the validated c3OmegaShift > 0.1 risk stratum
can define a safe adaptive control policy.

Frozen model:
M3++ with c3OmegaShift > 0.1 two-stratum risk table

Core questions:
1. Should C3 be applied only when c3OmegaShift > 0.1?
2. Can risk-stratum gating reduce wasted interventions?
3. Does gating preserve zero damage?
4. Does policy improve practical rescue efficiency?

Do not modify M3++.
Do not retune c3OmegaShift threshold.
Do not add new variables or correction classes.
Do not claim physical interpretation.
```

---

*Generated 2026-07-19. This document is the authoritative current-state reference for the
TRM/TQM project. Read TRM_Project_Lineage_Overview.md for complete historical context and
TRM_Project_QuickStart_For_New_Chats.md for operational briefing.*
