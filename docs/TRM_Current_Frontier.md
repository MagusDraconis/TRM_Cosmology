# TRM Current Frontier

**Version:** 1.6
**Date:** 2026-07-21

**Current Version:** V5.54 COMPLETE (TSS_01 Final Synthesis)
**Current Branch:** feature/v5.54-rawiqr-origin-and-profile-structure
**Cumulative Tests:** 2908
**Failed:** 0

---

## Current Preferred Restart Documents

For any new LLM chat or Copilot session, read in this order:

1. `TRM_Current_Frontier.md` (this file) — active frontier, model, constraints, next prompt
2. `TRM_Project_QuickStart_For_New_Chats.md` — operational briefing, supported findings, open problems
3. `TRM_Project_Lineage_Overview.md` — complete V1 → V5.27 historical lineage

---

## One-Sentence Current State

V5.53 identified the SAC P1/P1b predicate: rawIQR (spread) is the dominant discriminator
(10× normalized), with rawMean providing independent per-N stabilization. A rank-based
rawIQR+rawMean composite yields consistent P1>P1b direction across all N. This refines
V5.52 — SAC creates ordering through a spread-dominant profile predicate. Stop-Low safe.
Causal closure blocked. V6 NOT READY.

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
| V5.44 | **COMPLETE** — C3 Correction Response Instrumentation. c3ExitOm2 identified; N-dependent bridge. |
| V5.45 | **COMPLETE** — C3 Response Autonomy and N-Dependent Bridge. Bridge explained via IQR/distribution shape. |
| V5.46 | **COMPLETE** — Entry-State Distribution Shape and N-Window Origin. T0 inherited spread dominates; N=75 uniquely broad. |
| V5.47 | **COMPLETE** — Post-Warmup T0 Spread Origin and N-Window Formation. Handoff transform. d/K near-perfect diagnostic. Model A+E. |
| V5.48 | **COMPLETE** — Distribution Shape vs Mean State — N-Window Formation. |
| V5.49 | **COMPLETE** — Spread Generation and Distribution Origin. |
| V5.50 | **COMPLETE** — Kernel-Class Assignment and Boundary Origin. |
| V5.51 | **COMPLETE** — Spread-Order Origin and Kernel-Assignment Mechanism. |
| V5.52 | **COMPLETE** — Raw-Frequency Ensemble Sampling Origin. SAC creates K1>K3>K2 ordering. |
| V5.53 | **COMPLETE** — SelectAndClassify Predicate Origin. rawIQR dominant discriminator (10×). rawMean complementary stabilizer. Model B+. TSS_01 Final Synthesis complete. |

---

## Current V5.54 Research Question

**Where does rawIQR variation come from, and does it propagate into P1/P1b?**

**Answer:** rawIQR has two components. Between-seed (93.6%): generator seed realization — large but SAC-irrelevant (seed→P1 r=0.044). Within-seed residual (6.4%): N-driven sampling — small but SAC-relevant (P1−P1b delta=0.00105). Residual rawIQR is orthogonal to residual rawMean (r=0.0000) and dominates (8.7×). Pooled composite (V5.53) and residual discriminator (V5.54) are distinct layers. V6 NOT READY.

**Final synthesis:** `docsV5/V5_54/TRM_V5_54_Final_Synthesis.md`

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
- V5.53: rawIQR is the strongest P1/P1b discriminator (~10× normalized dominance over rawMean).
- V5.53: rawIQR + rawMean rank composite yields consistent P1>P1b direction (2/3 N jackknife-stable).
- V5.53: V5.52 mean-only interpretation is incomplete — SAC predicate is spread-primary, mean-complement (Model B+).

### CONDITIONAL

- All findings are finite-N and operator-class limited.
- V5.27 calibration is conservative, not precise.
- N=50–64 is inaccessible. N=65–79 is adaptive-active.
- V5.41 identifiability classifications are conditional on current pipeline design.
- Diagnostic hierarchy is observational, not causal.
- V5.53 findings are conditional on profile-count limits (10–11 P1, 3–10 P1b per N).
- V5.53 rawIQR per-N stability is profile-count-sensitive.
- V5.53 composite is diagnostic, not causal.

### HYPOTHESIS

- V5.42: Counterfactual trace and natural variation may strengthen causal rejection boundaries.
- V5.53: rawIQR may encode the dominant SAC predicate.
- V5.53: rawMean may stabilize the predicate across N.
- V5.53: SAC may operate on profile-shape descriptors.
- V5.53: A hidden SAC discriminator may remain.

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
