# TRM Current Frontier

**Version:** 1.3
**Date:** 2026-07-19

**Current Version:** V5.39 COMPLETE
**Current Branch:** `feature/v5.39-attractor-absorption-and-perturbation-resistance`
**Cumulative Tests:** 2833
**Failed:** 0

---

## Current Preferred Restart Documents

For any new LLM chat or Copilot session, read in this order:

1. `TRM_Current_Frontier.md` (this file) — active frontier, model, constraints, next prompt
2. `TRM_Project_QuickStart_For_New_Chats.md` — operational briefing, supported findings, open problems
3. `TRM_Project_Lineage_Overview.md` — complete V1 → V5.27 historical lineage

---

## One-Sentence Current State

V5.28 validated Stop-Low as a safe post-C3 continuation policy. V5.38 established
lambda1/K-state as the strongest diagnostic separator of c3OmegaShift. V5.39
confirmed that the attractor absorbs K-state perturbations, explaining why
diagnostic variables resist direct causal manipulation. Causal closure
remains incomplete. V6 remains NOT READY.

---

## Current Preferred Model

**M3++** — unchanged. **Stop-Low policy:**
- c3OmgS > 0.1: continue M3++ persistence validation
- c3OmgS ≤ 0.1: stop continuation (low rescue probability)
- Preserves all rescues. Zero damage. 75% work reduction.

**Diagnostic hierarchy (V5.38):**
1. lambda1 / K-state (|corr| ≈ 0.618)
2. rebMagnitude (|corr| ≈ 0.528)
3. omDist / Omega proximity (|corr| ≈ 0.518)
4. Omega T1 (|corr| ≈ 0.518)
5. kSensitivity (|corr| ≈ 0.467)
6. d_tail (|corr| ≈ 0.400)

**Attractor absorption (V5.39):**
K-state perturbation (±15%) → lambda1 delta < 0.03. The attractor resists
direct state manipulation. Model C — Omega restoration dominates downstream.

---

## Current Preferred Model

**M3++** — the preferred validated adaptive model:

- **P1/P1b** Compression-Room pathway (primary transferable entry pathway)
- **projHiVec** selector (validated residual selector, +12.5% holdout at N=71)
- **orthHiVec** refinement at N=72 (+14.1% holdout)
- **rebMagnitude** adaptive probe (post-intervention rebound signal)
- **C3** entry-vector re-alignment correction (adaptive correction that breaches static ceiling)
- **Stop-Low** post-C3 continuation policy (c3OmgS > 0.1 threshold, frozen)

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
| V5.27 | **COMPLETE** — c3OmegaShift > 0.1 frozen two-stratum risk table. P_A=0.0%, P_B=9.6%. Independent validation passed. |
| V5.28 | **COMPLETE** — Stop-Low policy validated. c3OmgS > 0.1 continue, ≤ 0.1 stop. 75% work reduction, zero damage. |
| V5.29 | **COMPLETE** — Boundary robustness confirmed. Safety gap 0.247. Noise ±0.05 produces 0 flips. Model A. |
| V5.30 | **COMPLETE** — Generalization to 1322 profiles, 6 cohorts. 64 rescues, 0 missed. Hostile audit passed. |
| V5.31 | **COMPLETE** — Failure search. 7 stress regimes. Zero low-stratum rescues. Gap stable at 0.056. |
| V5.32 | **COMPLETE** — Reproducibility. 3 execution modes — all IDENTICAL. Deterministic. |
| V5.33 | **COMPLETE** — Operational efficiency. 72% workload reduction, 3.6× efficiency gain. |
| V5.34 | **COMPLETE** — Foundation consolidation. 26 supported findings, 30 claims audited. V6 NOT READY. |
| V5.35 | **COMPLETE** — Mechanism closure. Causal chain falsified. c3OmgS = Model B diagnostic. |
| V5.36 | **COMPLETE** — c3OmegaShift origin. lambda1 (-0.619), omDist (-0.512), rebMag (-0.531). Partial causal evidence (later downgraded). |
| V5.37 | **COMPLETE** — Omega proximity scaling. Dose-response non-monotonic. V5.36 causal claim downgraded. Model C — weak conditional. |
| V5.38 | **COMPLETE** — Response-state interaction. lambda1 = strongest diagnostic separator (|corr|=0.618). Diagnostic hierarchy only. |
| V5.39 | **COMPLETE** — Attractor absorption. K-perturbation absorbed at lambda1 stage. Explains V5.38 RII failure. Model C — Omega restoration dominates. |

---

## Current V5.39 Research Question

**Why does the system absorb K-state / lambda1 perturbations?**

**Answer:** The attractor resists perturbation at the lambda1 stage.
±15% K-scaling → lambda1 delta < 0.03. Perturbation partially propagates
downstream. Omega T2 restoration is the dominant absorption mechanism
(Model C). This explains why V5.38 RII's lambda1 intervention produced
no causal signal.

**Key result:** Diagnostic hierarchy variables resist direct manipulation
because the attractor protects its internal state coordinates.

---

## V5.39 Constraints

- Do not modify M3++.
- Do not retune c3OmegaShift threshold (0.1 is frozen).
- Do not add new correction classes.
- Do not add new variables.
- Do not modify Stop-Low.
- Do not claim physical interpretation.
- Do not attempt V6 derivations.

---

## Claim Discipline

### SUPPORTED

- M3++ adaptive model validated within tested domain.
- Stop-Low policy: c3OmgS > 0.1 continue, ≤ 0.1 stop. Preserves all rescues, zero damage.
- V5.27: c3OmegaShift > 0.1 defines rescue-enriched risk stratum. P_A = 0.0%, P_B = 9.6%.
- Full C3 gain chain is mechanistically valid.
- c3OmegaShift is the minimal robust predictive summary.
- Rescue is probabilistic, not deterministic.
- V5.38: lambda1 / K-state is the strongest diagnostic separator of c3OmegaShift (|corr| ≈ 0.618).
- V5.38: Diagnostic hierarchy: lambda1 > rebMag > omDist.
- V5.39: Attractor absorbs K-state perturbations at lambda1 stage (delta < 0.03 for ±15% K-scaling).
- V5.39: Absorption model = Model C — Omega restoration dominates downstream.

### CONDITIONAL

- All findings are finite-N and operator-class limited.
- V5.27 calibration is conservative, not precise.
- N=50–64 is inaccessible. N=65–79 is adaptive-active.
- V5.39 absorption model derived from 6 N, single perturbation class.
- Diagnostic hierarchy is observational, not causal.

### HYPOTHESIS

- V5.40: Causal testing against attractor topology may reveal new leverage.

### NOT CLAIMED

- Physical constants, spacetime, relativity, quantum mechanics, cosmology
- Universal Low→High control
- Universal adaptive control
- N<65 impossibility under all future operators
- Deterministic rescue threshold
- Physical interpretation of N-boundaries
- Physical criticality
- Full-chain predictive superiority
- lambda1 causality
- rebound causality
- c3OmegaShift causal sufficiency
- Full causal closure
- V6 readiness
- Length, space, velocity, or c derivation

---

## Recommended V5.40 Next Prompt

```
You are acting as a TRM/TQM V5.40 causal closure and attractor topology agent.

Current branch:
feature/v5.40-causal-closure-and-attractor-topology

Base:
V5.39 COMPLETE

Current cumulative state:
2833 tests passed
0 failed

Purpose:
If the attractor resists simple state perturbations (V5.39), what approaches
can test causal closure without fighting attractor restoration?

Frozen:
M3++, Stop-Low policy, c3OmegaShift > 0.1 threshold.

Core questions:
1. Can perturbation strategies be designed that work with (not against) attractor dynamics?
2. Does multi-stage perturbation bypass early-stage absorption?
3. Can attractor topology itself be probed for causal leverage?
4. Does any approach improve causal closure?
5. Does V6 remain not ready?

Do not modify M3++.
Do not retune c3OmegaShift threshold.
Do not add new variables or correction classes.
Do not claim physical interpretation.
Do not attempt length, space, velocity, or c derivations.
```

---

*Generated 2026-07-19. This document is the authoritative current-state reference for the
TRM/TQM project. Read TRM_Project_Lineage_Overview.md for complete historical context and
TRM_Project_QuickStart_For_New_Chats.md for operational briefing.*
