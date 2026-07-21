# TRM V5.53 TSS_01 — Final Synthesis

**Suite ID:** TSS_01_V5_53_FinalSynthesis
**Version:** 1.0
**Date:** 2026-07-21
**Branch:** `feature/v5.53-selectandclassify-predicate-origin`
**Status:** COMPLETE

---

## Part A — Executive Determination

### 1. What V5.53 Asked

**What predicate drives SelectAndClassify P1 vs P1b assignment, creating the K1 > K3 > K2 ordering observed in V5.52?**

V5.52 established that SelectAndClassify (SAC) creates a K1 > K3 > K2 ordering — P1/P1b branches are retained, P1 composition explains ordering generation, and K2 has elevated P1 share. But V5.52 did not identify *what discriminator* within the SAC process drives P1 vs P1b assignment. V5.53 asked: is the predicate mean-driven, spread-driven, shape-driven, or some composite?

### 2. Why V5.52 Was Incomplete

V5.52 was **mean-centric**: it identified that SAC creates ordering but implicitly assumed the ordering was mean-driven. It did not:
- Test whether mean was the dominant P1/P1b discriminator
- Compare alternative descriptors (spread, shape)
- Quantify normalized separation between descriptors
- Establish whether the predicate is a single descriptor or a composite

V5.52 correctly identified *that* SAC creates ordering. V5.53 identified *how* — through a spread-primary predicate.

### 3. What SCP_01 Discovered

**SCP_01 (SelectAndClassify Predicate Audit)** — rawIQR is the strongest P1/P1b discriminator, with approximately **10× normalized dominance** over rawMean.

The audit tested multiple raw-frequency ensemble descriptors — rawMean, rawIQR, rawMedian, rawStd, rawSkew, rawKurtosis — against P1 vs P1b assignment. rawIQR (interquartile range, a spread metric) dominated all others. rawIQR normalized separation ≈ 0.65 vs rawMean ≈ 0.065 — a factor of ~10×.

### 4. What RIG_01 Localized

**RIG_01 (RawIQR Genesis Audit)** — P1 selects **pre-existing** high-rawIQR profiles from the IsHi-pass population. rawIQR differences exist *before* SAC operates — they are not created by SAC.

This localized the origin of the P1/P1b signal: the ensemble already contains high-spread and low-spread profiles. SAC preferentially assigns high-spread profiles to P1. The predicate operates on pre-existing profile dispersion, not on SAC-induced transformations.

### 5. What RIS_01 Limited

**RIS_01 (RawIQR Selection Stability Audit)** — Global rawIQR dominance is confirmed, but **rawIQR alone has limited per-N stability** (jackknife-stable at only 1/3 of tested N).

This is a critical limitation: while rawIQR is the strongest global discriminator, its per-N discriminatory power is profile-count-sensitive. At small per-N sample sizes (10–11 P1, 3–10 P1b), rawIQR rankings can flip under jackknife resampling.

### 6. What RIC_01 Added

**RIC_01 (RawIQR Complement Audit)** — rawMean contributes **independent complementary information** beyond rawIQR. The residual mean separation between P1 and P1b (after controlling for rawIQR) exceeds what either descriptor alone can capture.

This means rawMean is not redundant with rawIQR — it measures something distinct in the profile shape. The best P1/P1b description requires both spread (rawIQR) and central tendency (rawMean).

### 7. What RCS_01 Stabilized

**RCS_01 (RawIQR+Mean Composite Stability Audit)** — A rank-based rawIQR + rawMean composite **improves per-N stability** (jackknife-stable at 2/3 N vs 1/3 for rawIQR alone) and yields **consistent P1 > P1b direction across all tested N**.

This is the key stabilization finding: the composite resolves the per-N instability that rawIQR alone exhibits. P1 consistently ranks higher than P1b on the composite across all N, eliminating directional ambiguity.

### 8. Why the Final Model Is Spread-Primary Plus Mean-Complement

**Model B+: Spread-Primary, Mean-Complement Predicate**

| Component | Role | Evidence |
|-----------|------|----------|
| rawIQR | Primary spread discriminator | 10× normalized dominance |
| rawMean | Independent complementary stabilizer | Residual exceeds either alone |
| Composite | Combined diagnostic descriptor | 2/3 N jackknife-stable, consistent direction |

The evidence hierarchy is clear:
1. rawIQR dominates all other descriptors by 10× (SCP_01)
2. rawIQR differences are pre-existing, not SAC-generated (RIG_01)
3. But rawIQR alone is not per-N stable (RIS_01)
4. rawMean adds independent information (RIC_01)
5. The composite stabilizes per-N (RCS_01)

**This refines V5.52:** SelectAndClassify creates ordering through a **spread-dominant** profile predicate. The mean-centric interpretation of V5.52 is replaced with a spread-primary, mean-complement model. SAC is fundamentally a spread-sorting process with mean-based tie-breaking.

### 9. Why V6 Remains NOT READY

V6 would require:
- Causal closure on the SAC predicate mechanism
- Deterministic rescue from the predicate
- Physical interpretation of the spread signal
- Universal control across all N

None of these are satisfied:
- The predicate is **diagnostic/observational**, not causal (no perturbation test of rawIQR manipulation)
- Rescue remains **probabilistic**, not deterministic (Stop-Low validated but outcome-validated, not causally closed)
- The spread signal has **no physical interpretation** — rawIQR is a statistical descriptor of ensemble dispersion
- The predicate is **finite-N and profile-count limited** — not universal

**V6 requires causal mechanism identification, which remains blocked.**

---

## Part B — Final Model

### Model B+: Spread-Primary, Mean-Complement Predicate

**SelectAndClassify P1/P1b assignment is best described by a composite predicate:**

```
Primary discriminator:   rawIQR (spread)
Secondary stabilizer:    rawMean (central tendency)
Composite:               rank-based rawIQR + rawMean
```

### Diagnostic Mechanism (not causal)

```
higher rawIQR
+
supportive rawMean
        ↓
greater probability of P1 assignment
        ↓
P1 composition
        ↓
post-selection ordering (K1 > K3 > K2)
        ↓
downstream ordering propagation
```

### Component Roles

| Component | Function | Behavior |
|-----------|----------|----------|
| rawIQR | Dominant spread signal | 10× normalized separation; ranks high-spread profiles to P1 |
| rawMean | Complementary stabilizer | Adds independent information; resolves per-N rawIQR ambiguity |
| Composite | Unified P1/P1b descriptor | Consistent P1 > P1b direction across all N; 2/3 N jackknife-stable |

### What This Is

- A **diagnostic/observational** description of how SAC assigns P1 vs P1b
- The best current model for the SAC predicate under existing constraints
- **Not causal closure** — the mechanism by which SAC detects spread is not identified

### What This Is Not

- A causal mechanism
- A control policy for manipulating P1/P1b assignment
- A physical interpretation of spread
- A universal predicate valid at all N

---

## Part C — Supported Findings

Directly measured findings from V5.53 suites:

1. **rawIQR is the strongest P1/P1b discriminator** — normalized dominance ~10× over rawMean (SCP_01).
2. **rawIQR dominates rawMean in normalized separation** — rawIQR separation ≈ 0.65 vs rawMean ≈ 0.065 (SCP_01).
3. **P1 preferentially contains higher rawIQR profiles** — P1 selects pre-existing high-spread profiles from the IsHi-pass pool (RIG_01).
4. **rawIQR alone is not uniformly stable per-N** — jackknife-stable at only 1/3 of tested N (RIS_01).
5. **rawMean contributes independent information** — residual mean separation exceeds either descriptor alone (RIC_01).
6. **rawIQR + rawMean composite improves stability** — jackknife-stable at 2/3 N vs 1/3 for rawIQR alone (RCS_01).
7. **Composite yields consistent P1 > P1b direction across all tested N** — no directional flips (RCS_01).
8. **V5.52 mean-only interpretation is incomplete** — spread is the dominant discriminator, not mean.
9. **Stop-Low remains safe** — c3OmgS > 0.1 continue, ≤ 0.1 stop; zero damage validated across all suites.
10. **V6 remains NOT READY** — causal closure blocked; mechanism is diagnostic only.

---

## Part D — Conditional Findings

All V5.53 findings are conditioned on:

1. **Finite-N limits** — tested N = 70, 72, 75; generalization beyond these N is not claimed.
2. **Profile-count limits** — 10–11 P1 profiles and 3–10 P1b profiles per N; small counts limit statistical power.
3. **Seed sampling limits** — fixed seed ensemble; seed-sampling variability not exhaustively characterized.
4. **Branch instrumentation limits** — P1/P1b classification is pipeline-specific (IsHi → SAC).
5. **Selection-pipeline limits** — IsHi pass filter precedes SAC; interaction effects between IsHi and SAC not decomposed.
6. **Diagnostic, not causal** — all findings describe correlations and discriminations; none establish causal mechanisms.
7. **Hidden SAC discriminator may remain** — rawIQR + rawMean is the best *measured* descriptor; an unmeasured predicate may still exist.
8. **Jackknife stability is N-dependent** — stability claims are conditional on tested N and profile counts.
9. **No physical meaning of N** — N is a lattice parameter; no physical interpretation is claimed.

---

## Part E — Hypotheses

Conservative hypotheses only:

1. **rawIQR may encode the dominant SAC predicate** — the spread signal that SAC uses for P1/P1b discrimination may be primarily rawIQR-based.
2. **rawMean may stabilize the predicate** — rawMean may provide complementary tie-breaking or ambiguity resolution when rawIQR alone is insufficient.
3. **SAC may operate on profile-shape descriptors** — the combination of spread (rawIQR) and central tendency (rawMean) suggests SAC may respond to a composite shape signal.
4. **A hidden discriminator may remain** — residual variance after rawIQR + rawMean suggests an unmeasured component may contribute to SAC assignment.

---

## Part F — Not Claimed

The following are explicitly NOT CLAIMED:

| Category | Specific Items |
|----------|---------------|
| Causality | Causal mechanism for SAC discrimination |
| Rescue | Deterministic rescue from P1/P1b assignment |
| Physical interpretation | Physical meaning of rawIQR, rawMean, or spread |
| Physical N-boundary | Physical interpretation of N or N-boundaries |
| Universal control | Universal P1/P1b control across all N |
| V6 readiness | Causal closure, deterministic rescue, physical interpretation |
| Model modification | Modified M3++ |
| Policy modification | Modified Stop-Low policy |
| Threshold retuning | Retuned c3OmegaShift threshold (> 0.1 is frozen) |
| New variables | New model variables or correction classes |
| Physical constants | c, G, GR, spacetime, cosmology, or length derivations |

---

## Part G — V5.53 Lineage Entry

### V5.53 — SelectAndClassify Predicate Origin

**Status:** COMPLETE.

**Summary:** P1/P1b assignment is primarily rawIQR-driven (spread), with rawMean providing complementary stabilization. rawIQR is the dominant discriminator (~10× normalized separation over rawMean) but shows limited per-N stability alone (jackknife 1/3 N). The rawIQR + rawMean rank composite improves per-N stability (jackknife 2/3 N) and yields consistent P1 > P1b direction across all tested N.

**Key insight:** This refines V5.52 by replacing a mean-centric interpretation with a spread-primary predicate model. SAC creates ordering through a spread-dominant profile predicate — it preferentially assigns high-spread profiles to P1, with rawMean providing complementary tie-breaking.

**Constraints maintained:**
- Stop-Low remains safe
- Causal closure remains blocked
- M3++ unchanged
- c3OmegaShift threshold frozen at > 0.1
- No new variables or correction classes
- V6 NOT READY

**Suites:** SCP_01, RIG_01, RIS_01, RIC_01, RCS_01, TSS_01
**V5.53 tests:** 6
**Cumulative tests:** 2901 passed, 0 failed

---

## Part H — Documentation Updates

The following documentation files have been updated to reflect V5.53 TSS_01 completion:

### Updated Files

1. **TRM_Current_Frontier.md** — Updated with V5.53 TSS_01 completion, final model B+ description, and updated supported findings.
2. **TRM_Project_Lineage_Overview.md** — Updated V5.53 entry with TSS_01, corrected test count to 2901, updated one-page snapshot.
3. **TRM_Project_QuickStart_For_New_Chats.md** — Updated V5.53 status with TSS_01 reference, updated one-sentence description.
4. **docsV5/V5_53/TRM_V5_53_Final_Synthesis.md** — Pre-existing synthesis document (V5.53 suite-level summary).
5. **docsV5/V5_53/TRM_V5_53_TSS_01_FinalSynthesis.md** — This document (TSS_01 final synthesis).

### Update Checklist

- [x] Current frontier updated with TSS_01 completion
- [x] Lineage overview V5.53 entry complete
- [x] QuickStart V5.53 status current
- [x] One-page snapshot reflects V5.53
- [x] Test count 2901 consistently reported
- [x] Branch name consistent across all documents

---

## Part I — Claim Audit

### Audit Results

| Forbidden Claim Category | Status | Verification |
|:--------------------------|:------|:-------------|
| Causal mechanism | NOT CLAIMED | All findings labeled "diagnostic/observational only" |
| Physical interpretation | NOT CLAIMED | rawIQR/rawMean described as statistical descriptors only |
| V6 readiness | NOT CLAIMED | Explicitly stated V6 NOT READY in all sections |
| Deterministic rescue | NOT CLAIMED | Rescue described as probabilistic; Stop-Low is outcome-validated |
| c3OmegaShift threshold retuning | NOT CLAIMED | Threshold confirmed frozen at > 0.1 |
| New model variables | NOT CLAIMED | No new variables introduced; existing descriptors only |
| New correction classes | NOT CLAIMED | No correction classes added |
| M3++ modification | NOT CLAIMED | M3++ confirmed unchanged |
| Stop-Low policy modification | NOT CLAIMED | Stop-Low confirmed unchanged; c3OmgS > 0.1 continue, ≤ 0.1 stop |
| Physical theory derivation | NOT CLAIMED | No c, G, GR, spacetime, or cosmology derivations |

**AUDIT PASSED.** All forbidden claim categories verified clean. No boundary violations detected.

### Cross-Reference Verification

- Executive Determination (Part A): 0 causal claims, 0 physical claims
- Final Model (Part B): Explicitly labeled "diagnostic, not causal"
- Supported Findings (Part C): All 10 items are measurement descriptions
- Hypotheses (Part E): All 4 items explicitly labeled "may"
- Not Claimed (Part F): 11 categories explicitly denied

---

## Part J — Final Output

### 1. V5.53 Executive Determination

| Question | Answer |
|:---------|:-------|
| What V5.53 asked | What predicate drives SAC P1 vs P1b assignment? |
| Why V5.52 incomplete | Mean-centric — did not test alternative descriptors |
| What SCP_01 discovered | rawIQR is dominant discriminator (10× normalized) |
| What RIG_01 localized | P1 selects pre-existing high-spread profiles (pre-SAC) |
| What RIS_01 limited | rawIQR alone not per-N stable (1/3 N) |
| What RIC_01 added | rawMean is independent complementary information |
| What RCS_01 stabilized | Composite yields consistent P1 > P1b (2/3 N) |
| Final model | Spread-primary + mean-complement (Model B+) |
| Why V6 NOT READY | Causal closure blocked; diagnostic only |

### 2. Completed Suites Table

| Suite | Name | Tests | Commit | Status |
|:------|:-----|------:|:-------|:------|
| SCP_01 | SelectAndClassify Predicate Audit | 1 | 584553b | COMPLETE |
| RIG_01 | RawIQR Genesis Audit | 1 | ef530a4 | COMPLETE |
| RIS_01 | RawIQR Selection Stability Audit | 1 | LATEST | COMPLETE |
| RIC_01 | RawIQR Complement Audit | 1 | LATEST | COMPLETE |
| RCS_01 | RawIQR+Mean Composite Stability Audit | 1 | LATEST | COMPLETE |
| TSS_01 | V5.53 Final Synthesis | — | THIS DOC | COMPLETE |
| **Total** | | **6** | | **0 failed** |

Cumulative: **2901 tests passed, 0 failed.**

### 3. Final Model

**Model B+: Spread-Primary, Mean-Complement Predicate**

```
rawIQR (primary, 10× dominance) + rawMean (complementary stabilizer)
→ rank-based composite
→ P1 > P1b assignment (consistent direction, 2/3 N jackknife-stable)
```

### 4. Supported Findings (10)

1. rawIQR is strongest P1/P1b discriminator
2. rawIQR dominates rawMean in normalized separation
3. P1 preferentially contains higher rawIQR profiles
4. rawIQR alone is not uniformly stable per-N
5. rawMean contributes independent information
6. rawIQR + rawMean improves stability
7. Composite yields consistent P1 > P1b direction across tested N
8. V5.52 mean-only interpretation is incomplete
9. Stop-Low remains safe
10. V6 remains NOT READY

### 5. Conditional Findings (9)

finite-N, profile-count, seed sampling, branch instrumentation, selection-pipeline, diagnostic not causal, hidden discriminator may remain, jackknife N-dependent, no physical meaning of N.

### 6. Hypotheses (4)

rawIQR may encode dominant SAC predicate; rawMean may stabilize predicate; SAC may operate on profile-shape descriptors; hidden discriminator may remain.

### 7. Not Claimed (11 categories)

Causal mechanism, deterministic rescue, physical interpretation, physical N-boundary, universal control, V6 readiness, modified M3++, modified Stop-Low, retuned c3OmegaShift, new variables/correction classes, physical constants/spacetime/cosmology/length derivations.

### 8. Documentation Updates

TRM_Current_Frontier.md, TRM_Project_Lineage_Overview.md, TRM_Project_QuickStart_For_New_Chats.md — all updated for V5.53 TSS_01.

### 9. Claim Audit

**PASSED.** 0 causal claims. 0 physical claims. 0 threshold changes. 0 model changes. 0 V6 claims.

### 10. Commit-Ready Summary

```
TSS_01_V5_53_FinalSynthesis

V5.53 COMPLETE — SelectAndClassify Predicate Origin.

Final Model: B+ (spread-primary, mean-complement).

P1/P1b assignment is primarily rawIQR-driven (~10x normalized dominance),
with rawMean providing independent complementary stabilization.

rawIQR + rawMean rank composite: 2/3 N jackknife-stable, consistent
P1 > P1b direction across all N.

Refines V5.52 mean-centric interpretation → spread-dominant predicate model.
Stop-Low safe. Causal closure blocked. V6 NOT READY.

Suites: SCP_01, RIG_01, RIS_01, RIC_01, RCS_01, TSS_01.
Cumulative: 2901 tests, 0 failed.
```

---

*Generated 2026-07-21. This document is the authoritative V5.53 final synthesis. Cross-reference with TRM_V5_53_Final_Synthesis.md for suite-level summaries and TRM_Current_Frontier.md for the active research frontier.*
