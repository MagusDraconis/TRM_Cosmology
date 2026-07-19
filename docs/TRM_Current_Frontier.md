# TRM Current Frontier

**Version:** 1.0
**Date:** 2026-07-19

**Current Version:** V5.27 INITIALIZED
**Current Branch:** `feature/v5.27-full-chain-rescue-calibration-and-probability`
**Cumulative Tests:** 2748
**Failed:** 0

---

## One-Sentence Current State

The project has moved from static branch control to adaptive response control and now to
calibrated rescue probability over the validated C3 gain chain.

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
| V5.27 | INITIALIZED — calibrate rescue probability from full chain. |

---

## Current V5.27 Research Question

**Can rescue probability be calibrated from the complete validated chain better than any
single threshold rule?**

Expected comparisons:
- Full-chain probability model vs sign-rule-only baseline
- Full-chain probability model vs c3OmegaShift-only baseline
- Full-chain probability model vs omegaPerK-only baseline

Validate across N and cohorts. Avoid deterministic rescue claims.

---

## V5.27 Constraints

- Do not modify M3++.
- Do not add correction classes.
- Do not retune thresholds.
- Do not add new variables.
- Use only validated chain variables:
  - d_tail
  - deltaD
  - deltaK
  - omegaPerK sign
  - omegaPerK magnitude
  - c3OmegaShift
  - omDist
  - lambda1

---

## Claim Discipline

### SUPPORTED

- M3++ adaptive model validated within tested domain.
- C3 gain chain decomposed and validated.
- omegaPerK sign rule validated as high-precision enrichment.
- Rescue is probabilistic, not deterministic.
- Movement quality dominates movement quantity for rescue conversion.

### CONDITIONAL

- All findings are finite-N and operator-class limited.
- M3++ validated only for tested N ranges, seed cohorts, and operators.
- Low-N inaccessibility supported only under current operator classes.
- Sign rule is diagnostic/enrichment, not sufficient for rescue.

### HYPOTHESIS

- Full-chain rescue probability can be calibrated from validated chain variables.
- Remaining rescue variance reflects unresolved quality-of-movement factors.
- c3OmegaShift and omegaPerK sign enrich rescue likelihood but do not determine it.

### NOT CLAIMED

- Physical constants, spacetime, relativity, quantum mechanics, cosmology
- Universal Low→High control
- Universal adaptive control
- N<65 impossibility under all future operators
- omDist/lambda1 causality
- Deterministic rescue threshold
- Physical interpretation of N-boundaries
- Physical criticality
- Emergence or universal control

---

## Recommended V5.27 Next Prompt

```
You are acting as a TRM/TQM V5.27 full-chain rescue calibration agent.

Current branch:
feature/v5.27-full-chain-rescue-calibration-and-probability

Base:
V5.26 COMPLETE

Current cumulative state:
2748 tests passed
0 failed

Purpose:
Calibrate rescue probability from the complete validated C3 gain chain.

Validated chain:
d_tail -> deltaD -> deltaK -> omegaPerK sign -> c3OmegaShift -> rescue

Use only validated variables:
d_tail, deltaD, deltaK, omegaPerK sign, omegaPerK magnitude, c3OmegaShift, omDist, lambda1.

Do not modify M3++.
Do not add correction classes.
Do not retune thresholds.
Do not introduce new variables.
Do not claim physical interpretation.

Next suite:
FCE_FullChainExecution

Goal:
Determine whether rescue probability can be calibrated from the full chain and whether
the full-chain model outperforms sign-rule-only and c3OmegaShift-only baselines.
```

---

*Generated 2026-07-19. This document is the authoritative current-state reference for the
TRM/TQM project. Read TRM_Project_Lineage_Overview.md for complete historical context and
TRM_Project_QuickStart_For_New_Chats.md for operational briefing.*
