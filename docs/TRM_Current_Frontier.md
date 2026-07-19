# TRM Current Frontier

**Version:** 1.3
**Date:** 2026-07-19

**Current Version:** V5.46 COMPLETE
**Current Branch:** `feature/v5.46-entry-state-distribution-shape-and-n-window-origin`
**Cumulative Tests:** 2871
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
diagnostic variables resist direct causal manipulation. V5.40 tested whether
attractor-compatible perturbations could penetrate this protection — they could not.
All perturbation families are absorbed; weak signals are artifacts. V5.41 pivoted
to causal-test methodology: perturbation is INVALIDATED; natural variation,
counterfactual trace, and causal rejection are viable alternatives. V5.42 applied counterfactual trace:
7/11 near-identical profiles diverge on c3OmgS (63.6%). 7 sufficiency claims
REJECTED — no single variable determines the outcome. Causal closure narrowed
but not achieved. V5.43 added temporal trace: 62.5% of divergence appears
only at T4 (c3OmgS computation). Pre-C3 trajectory does not explain
most divergence. Hidden factor = Model G — unrecorded microstate in
C3 correction response. Temporal trace LOCALIZES but does not EXPLAIN.
Causal closure remains blocked. V6 remains NOT READY.

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

**Causal closure blocked (V5.40):**
Attractor-aligned perturbations do NOT survive better than mismatch. All
perturbation families absorbed 87–99%. Weak c3OmgS directional signals are
baseline-state artifacts (Model C — not causal). Absorption is direction-invariant
(Model D). Causal closure remains BLOCKED.

**Causal test methodology (V5.41):**
Perturbation-based testing INVALIDATED. Natural variation = STABLE DIAGNOSTIC.
Invariance = WEAKLY INVARIANT (N-dependent). Mediation = TEMPORALLY AMBIGUOUS.
Counterfactual trace = OBSERVATIONAL. 8 causal claims REJECTED. Stop-Low is
OUTCOME-VALIDATED — does not require causal closure. V6 remains NOT READY.

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
| V5.40 | **COMPLETE** — Causal closure and attractor topology. Aligned perturbations do NOT survive better. Weak signals are artifacts. Absorption direction-invariant. Causal closure BLOCKED. |
| V5.41 | **COMPLETE** — Causal test methodology. Perturbation INVALIDATED. Natural variation = diagnostic. 8 claims REJECTED. Stop-Low is OUTCOME-VALIDATED without causal closure. |
| V5.42 | **COMPLETE** — Counterfactual trace and causal rejection. 7/11 near-identical pairs diverge (63.6%). 7 sufficiency claims REJECTED. No single variable determines outcome. |
| V5.43 | **COMPLETE** — Hidden trace discovery. 62.5% divergence at T4 (C3 computation). Paradox resolved (late-stage amplification). Hidden factor = unrecorded microstate. |

---

## Current V5.42 Research Question

**Which causal explanations can be rejected through matched-profile comparison?**

**Answer:** 7 sufficiency claims REJECTED. Near-identical profiles (matched on lam,
omDist, reb) diverge 63.6% of the time. No single measured variable is sufficient
for c3OmgS or rescue. Causal closure = Model B (narrowed, not achieved). Stop-Low
remains operationally valid without causal closure. V6 NOT READY.

---

## V5.41 Constraints

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
- V5.40: Absorption is direction-invariant (Model D). Causal closure remains BLOCKED.
- V5.41: Perturbation-based causal testing is INVALIDATED.
- V5.41: Natural variation = STABLE DIAGNOSTIC RELATION (not causal).
- V5.41: Invariance = WEAKLY INVARIANT (N-dependent).
- V5.41: Mediation = TEMPORALLY AMBIGUOUS (no staged measurement).
- V5.41: Counterfactual trace = OBSERVATIONAL (rebMag strongly separates outcomes).
- V5.41: 8 causal claims REJECTED by prior evidence.
- V5.41: Stop-Low is OUTCOME-VALIDATED — does not require causal closure.
- V5.41: Predictive validity ≠ causal closure.

### CONDITIONAL

- All findings are finite-N and operator-class limited.
- V5.27 calibration is conservative, not precise.
- N=50–64 is inaccessible. N=65–79 is adaptive-active.
- V5.41 identifiability classifications are conditional on current pipeline design.
- Diagnostic hierarchy is observational, not causal.

### HYPOTHESIS

- V5.42: Counterfactual trace and natural variation may strengthen causal rejection boundaries.

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

## Recommended V5.43 Next Prompt

```
You are acting as a TRM/TQM V5.43 hidden response state and temporal trace agent.

Current branch:
feature/v5.43-hidden-response-state-and-temporal-trace-discovery

Base:
V5.42 COMPLETE

Current cumulative state:
2848 tests passed, 0 failed

Purpose:
If measured same-state profiles diverge in c3OmgS (63.6%), what unmeasured
or temporal trace factor separates them?

Frozen:
M3++, Stop-Low policy, c3OmegaShift > 0.1 threshold.

Core questions:
1. What differs between near-identical profiles that diverge in c3OmgS?
2. Is divergence explained by temporal ordering not captured in current vars?
3. Is there a prior-state trace before the matched profile snapshot?
4. Are path-history variables needed?
5. Can divergence be reduced by adding temporal trace information?
6. Does this improve causal closure?
7. Does V6 remain not ready?

Do not modify M3++. Do not retune c3OmegaShift threshold.
Do not add new variables or correction classes.
Do not claim physical interpretation.
Do not attempt length, space, velocity, or c derivations.
```

---

*Generated 2026-07-19. This document is the authoritative current-state reference for the
TRM/TQM project. Read TRM_Project_Lineage_Overview.md for complete historical context and
TRM_Project_QuickStart_For_New_Chats.md for operational briefing.*
