# TRM V5.54 Final Synthesis — RawIQR Origin and Profile Structure

**Suite ID:** TSS_01_V5_54_FinalSynthesis
**Version:** 1.0
**Date:** 2026-07-21
**Branch:** `feature/v5.54-rawiqr-origin-and-profile-structure`
**Status:** COMPLETE

---

## Part A — Executive Determination

### 1. What V5.54 Asked

**Where does rawIQR variation come from, and does seed-level rawIQR propagate into P1/P1b assignment?**

V5.53 identified rawIQR as the dominant P1/P1b discriminator. V5.54 asked: what generates this rawIQR variation? Is it a seed-level trait that propagates through the selection pipeline, or a profile-local feature?

### 2. What RIO_01 Discovered

**rawIQR is seed realization under the uniform weight generator.** Mean rawIQR = 0.0998 matches generator expectation S = 0.10 (−0.23% bias). Between-seed variance is ~10× larger than within-seed. Topology is independent. N does not explain rawIQR. The generator sets the expected scale; the seed determines the realized value.

### 3. What SRA_01 Clarified

**Cross-N rawIQR stability is a generator artifact, not an independent seed trait.** Correlations r = 0.90–0.95 across N because each seed uses `new Random(seed)` producing identical first-70 draws. Between-seed variance = 93.6%. Common-70 verification r = 1.0000 confirms this is generator seed-reuse, not a deeper property.

### 4. What SRP_01 and SRX_01 Ruled Out

**Seed-level rawIQR does NOT propagate into P1 assignment.** SRP_01 found only 7 SAC-retained profiles from 100 seeds — too sparse. SRX_01 expanded to 300 seeds (900 profiles) but found seed→P1 r = 0.044 — essentially zero. All seed→outcome correlations |r| < 0.05. The V5.53 pooled discriminator works; seed-level prediction does not.

### 5. What RLP_01 Recovered

**rawIQR survives seed matching at the profile level.** After seed-centering, residual rawIQR still separates P1 from P1b (delta = −0.00105). Only 10 of 300 seeds have mixed outcomes, but pooled profile-level analysis confirms rawIQR remains the dominant local discriminator (100% normalized delta).

### 6. What RPD_01 Localized

**Within-seed residual is N-driven sampling variation.** Between-seed std = 0.0109; within-seed residual std = 0.0025 (4.2× ratio). N correlates with residual (|r| = 0.101) — using different N values with the same Random(seed) sequence produces slightly different IQR. Topology cannot explain within-seed residual (structural independence).

### 7. What RPC_01 Finalized

**Residual rawIQR is orthogonal to residual rawMean (r = 0.0000).** rIQR P1−P1b delta = 0.00105 (8.7× rMean delta). The residual composite (|rIQR−rMean|) degrades separation rather than improving it. The V5.53 composite advantage applies at the pooled (cross-seed) level, not at the residual (within-seed) level.

### 8. Why the Frontier Changed from Seed-Level to Residual-Level

V5.54 began asking "does seed rawIQR propagate?" and found **no**. But it ended asking "what profile-local feature does?" and found **yes** — rawIQR residual. The frontier shifted because:

- Seed-level rawIQR is large but SAC-irrelevant (r = 0.044 for P1 prediction)
- Residual rawIQR is small but SAC-relevant (delta = 0.00105 P1 vs P1b)
- SAC operates on profile-local spread deviations, not seed-level spread means

### 9. Why V6 Remains NOT READY

V6 requires causal mechanism identification. V5.54 traced rawIQR origin to generator realization and residual to N-driven sampling, but:
- No causal mechanism for SAC's sensitivity to spread residuals
- No physical interpretation
- No deterministic rescue from spread signals
- No universal control across N

---

## Part B — Final Model

### Model A+: Two-Level rawIQR Structure

| Level | Component | Variance Share | SAC Relevance |
|:------|:----------|---------------:|:-------------|
| **Between-seed** | Seed realization | **93.6%** | Low (r=0.044) |
| **Within-seed** | N-driven residual | 6.4% | **High** (delta=0.00105) |

**Interpretation:** rawIQR is a two-component quantity. The large between-seed component (determined by which seed draws the weights) dominates total variance but is SAC-irrelevant. The small within-seed residual (determined by N — how many draws from the same seed sequence) carries the P1/P1b discriminatory signal.

**Refinement of V5.53:**
- Pooled level: rawIQR + rawMean composite (V5.53 Model B+)
- Residual level: rawIQR only (V5.54 Model A+)
- Composite advantage is pooled-only; residual composite degrades

---

## Part C — Supported Findings

1. rawIQR origin traced to generator + seed realization (mean bias = −0.23%).
2. Between-seed variance dominates (93.6% of total).
3. Cross-N stability explained by generator seed-reuse (common-70 r = 1.0000).
4. Seed-level rawIQR does NOT propagate to P1 assignment (r = 0.044).
5. IsHi pass rates are rawIQR-independent (45–49% across strata).
6. rawIQR survives seed matching — residual separates P1/P1b (delta = −0.00105).
7. Within-seed residual is N-driven sampling variation (|r| = 0.101 with N).
8. Residual rawIQR orthogonal to residual rawMean (r = 0.0000).
9. Residual rawIQR dominates residual rawMean (8.7× delta).
10. Residual composite degrades separation.
11. Pooled composite (V5.53) and residual discriminator (V5.54) are different layers.
12. Stop-Low remains safe across all suites.

---

## Part D — Conditional Findings

- finite-N limits (N=70,72,75)
- sparse SAC retention (18 profiles from 900, limiting statistical power)
- generator-design dependence (System.Random, uniform distribution)
- seed-reuse structure creates inherent cross-N correlation
- diagnostic not causal
- hidden residual descriptors may remain

---

## Part E — Hypotheses

- SAC may operate on profile-local spread residuals rather than seed-level spread.
- The pooled composite (rawIQR+rawMean) and residual discriminator (rawIQR only) may be distinct operational layers.
- Alternative RNG implementations may produce different seed-to-rawIQR mappings.
- Hidden residual descriptors beyond rawIQR may contribute to P1/P1b separation.

---

## Part F — Not Claimed

- Causal mechanism for SAC's spread sensitivity
- Deterministic rescue from rawIQR signals
- Physical interpretation of rawIQR or residuals
- Physical N-boundary
- Universal adaptive control
- V6 readiness
- Modified M3++
- Modified Stop-Low policy
- Retuned c3OmegaShift threshold
- New model variables or correction classes
- Physical c, G, GR, spacetime, or cosmology derivation

---

## Part G — V5.54 Lineage Entry

### V5.54 — RawIQR Origin and Profile Structure

**Status:** COMPLETE.

**Summary:** rawIQR variance has two components: a large between-seed component (93.6%, dominated by generator seed realization) and a small within-seed residual (6.4%, driven by N-specific draw extensions). The between-seed component does not propagate to P1 assignment (seed→P1 r=0.044). The within-seed residual carries the P1/P1b discriminatory signal (delta=−0.00105). At the residual level, rawIQR is orthogonal to rawMean (r=0.0000) and dominates separation (8.7×). The V5.53 composite advantage (rawIQR+rawMean) applies at the pooled cross-seed level, not at the residual within-seed level.

**Key refinement over V5.53:** The P1/P1b discriminator operates on profile-local spread deviations (residuals), not seed-level spread means. Pooled composite and residual discriminator are distinct operational layers.

**Constraints maintained:**
- Stop-Low safe
- Causal closure blocked
- V6 NOT READY

**Suites:** RIO_01, SRA_01, SRP_01, SRX_01, RLP_01, RPD_01, RPC_01, TSS_01
**V5.54 tests:** 8
**Cumulative tests:** 2908 passed, 0 failed

---

## Part H — Status Update

Updated `TRM.App/wwwroot/data/trm-v5-54-status.json`: status = COMPLETE, final_model = Model A+, all 7 suites + TSS_01 recorded.

---

## Part I — Claim Audit

| Check | Status |
|:------|:------|
| Causal mechanism | NOT CLAIMED |
| Physical interpretation | NOT CLAIMED |
| V6 readiness | NOT CLAIMED (V6 NOT READY) |
| Deterministic rescue | NOT CLAIMED |
| Threshold retuning | NOT CLAIMED (frozen at 0.1) |
| M3++ modification | NOT CLAIMED (unchanged) |
| Stop-Low modification | NOT CLAIMED (unchanged) |

**AUDIT PASSED.**

---

## Part J — Commit-Ready Summary

```
TSS_01_V5_54_FinalSynthesis

V5.54 COMPLETE — RawIQR Origin and Profile Structure.

Final Model: A+ — Two-level rawIQR structure.

Seed-level (93.6% variance): generator realization.
  Does NOT propagate to P1 assignment (r=0.044).

Residual-level (6.4% variance): N-driven sampling.
  Carries P1/P1b signal (delta=0.00105).
  Orthogonal to residual mean (r=0.0000).
  Dominates 8.7× over residual mean.

Refines V5.53: pooled composite ≠ residual discriminator.
Stop-Low safe. Causal closure blocked. V6 NOT READY.

Suites: RIO_01, SRA_01, SRP_01, SRX_01, RLP_01, RPD_01, RPC_01, TSS_01.
Cumulative: 2908 tests, 0 failed.
```

---

*Generated 2026-07-21. This document is the authoritative V5.54 final synthesis.*
