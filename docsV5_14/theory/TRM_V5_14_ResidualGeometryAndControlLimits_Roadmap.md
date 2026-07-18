# TRM V5.14 Residual Geometry and Control Limits — Research Roadmap

**Version:** 1.0
**Date:** 2026-07-18
**Base:** V5.13 COMPLETE (M3 model, 2590 tests, 0 failed)

---

## 1. Research Question

Can the remaining unexplained High-basin persistence variance be reduced by additional validated geometry, or has the current model (M3: P1/P1b + projHiVec) reached its practical explanatory and control limit?

---

## 2. Motivation

V5.13 established:
- projHiVec improves holdout by +12.5% at N=71, +8.1% at N=72
- P2 demoted; dominant failure is insufficient displacement (82%)
- But even with best selector, ~37% of N=71 and ~36% of N=72 holdout candidates fail
- At N=75, 25% unexplained

V5.14 asks: is the remaining variance explainable, or is this the practical limit?

---

## 3. Hypotheses

| ID | Hypothesis | Test Suite |
|----|-----------|------------|
| H1 | Additional geometric features separate successes from failures beyond projHiVec | RGE + RGA |
| H2 | Orthogonal distance (orthHiVec) explains projHiVec misclassifications | RGE F1 |
| H3 | Post-intervention response signatures classify failures better than pre-intervention | RGE F6 |
| H4 | Control ceiling reached — no residual feature improves holdout beyond M3 | RGI |

---

## 4. Experimental Design

### N Values

| N | Role |
|---|------|
| 67 | Inaccessible boundary (negative control) |
| 71 | Primary selector window (M3: 62.5% holdout) |
| 72 | Primary selector window (M3: 63.6% holdout) |
| 75 | Strong pathway window (M3: 75% holdout) |
| 80 | Saturated regime (universal protocols) |

### Seed Cohorts

| Cohort | Range | Use |
|--------|-------|-----|
| Reference | 0-99 | Train selectors |
| Holdout | 100-199 | Validate selectors |
| Second holdout | 200-299 | Optional additional validation |

### Frozen Thresholds

| Parameter | Value | Source |
|-----------|-------|--------|
| Omega branch | > 1.783 | V5.3 |
| projHiVec | > -0.3281 | V5.13 HVI/HVS |
| P1 d0 | > 0.50 | V5.12 |
| P1b d0 | > 0.65 | V5.12 |

---

## 5. Suite Plan

| Suite | Phase | Purpose |
|-------|-------|---------|
| RGP | Protocol | Frozen baseline, feature families, gates, failure criteria |
| RGE | Execution | Profile collection, residual feature extraction |
| RGA | Analysis | Feature ranking, holdout validation, failure classification |
| RGI | Limit Audit | Ceiling determination, control limits |
| RGS | Synthesis | Final model or ceiling declaration |

---

## 6. Success Criteria

**V5.14 succeeds if:** RGA finds a residual feature improving holdout over M3 by ≥ 5% at N=71/72, with holdout transfer.

**V5.14 reaches ceiling if:** No feature improves holdout over M3; RGI confirms variance is within noise/instrumentation limits.

---

## 7. Claim Discipline

V5.14 does NOT claim: universal control, complete hidden-variable discovery, physical interpretation, attractor proof, generalization beyond tested domains. Stays strictly within RecoverFP residual geometry analysis.
