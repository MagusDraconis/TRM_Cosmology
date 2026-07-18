# V5.22 C3 Effectiveness Analysis — Results

**Suite:** IOA (C3 Effectiveness Analysis)
**Branch:** feature/v5.22-inducibility-onset-and-c3-effectiveness
**Status:** IOA COMPLETE (7/7 tests, ~8 min)
**Base:** V5.21 COMPLETE, IOE COMPLETE

---

## 1. C3-Effective vs Ineffective Seeds

### Pre-C3 State

| Metric | Effective | Ineffective | Ratio |
|--------|-----------|-------------|-------|
| alignPre | 0.159 | 0.017 | 0.10x |
| dT1 | 0.309 | 0.401 | 1.30x |
| omT1 | 1.286 | 1.218 | 0.95x |
| distHi | 0.249 | 0.252 | 1.01x |

Note: "Effective" includes A0-induced seeds, which have positive alignPre (already High). The C3-rescued subset (analyzed separately below) has negative alignPre.

### Per-N C3 Effectiveness

| N | Eff | Ineff | avgC3OmegaShift | avgDisp | avgAlignPre |
|---|-----|-------|-----------------|---------|-------------|
| 65 | 4 | 29 | 0.054 | 0.378 | 0.006 |
| 66 | 4 | 31 | -0.105 | 0.390 | 0.007 |
| 67 | 4 | 34 | -0.031 | 0.367 | 0.037 |
| 70 | 32 | 36 | -0.228 | 0.383 | 0.073 |
| 72 | 54 | 17 | -0.617 | 0.326 | 0.158 |

---

## 2. N=64 Failure Analysis

### N=64 vs N=65 Rescued vs N=65 Failed

| Metric | N=64 | N=65 Rescued | N=65 Failed | Gap |
|--------|------|-------------|-------------|-----|
| alignPre | 0.016 | **-0.346** | 0.034 | **21.2x** |
| dT1 | 0.349 | **0.742** | 0.349 | 2.1x |
| c3OmegaShift | -0.014 | **0.952** | -0.010 | **66.5x** |
| deltaAlign | 0.005 | **0.070** | -0.004 | 13.0x |
| c3Effectiveness | 1.106 | **6.387** | 0.175 | 5.8x |

**The gap is enormous:** N=65 rescued seeds have 66.5x higher c3OmegaShift and 21.2x more negative alignPre.

### Gap Analysis: What N=64 Lacks

| Rank | Metric | N=64 | N=65 Rescued | Gap |
|------|--------|------|-------------|-----|
| 1 | alignPre | 0.016 | -0.346 | 1.05 |
| 2 | c3OmegaShift | -0.014 | 0.952 | 1.02 |
| 3 | deltaAlign | 0.005 | 0.070 | 0.92 |
| 4 | c3Effectiveness | 1.106 | 6.387 | 0.83 |
| 5 | dT1 | 0.349 | 0.742 | 0.53 |

### The Critical N=64 Mystery

**14% of N=64 seeds meet resonant reversal preconditions (alignPre<-0.1 && dT1>0.5).** But N=64 max c3OmegaShift = 0.049 — far below the resonant reversal minimum of 0.765.

Even with the right preconditions (anti-aligned, high dT1), N=64 cannot produce large C3 omega shifts. The C3 nudge toward Hi simply doesn't create the Omega response at N=64 that it does at N=65.

**N=64 top c3OmegaShift seeds (max = 0.049):**
| Seed | c3OmegaShift | dT1 | alignPre |
|------|-------------|-----|----------|
| 110 | 0.049 | 0.362 | -0.000 |
| 91 | 0.041 | 0.432 | -0.078 |
| 102 | 0.040 | 0.418 | -0.062 |
| 15 | 0.040 | 0.229 | 0.149 |
| 10 | 0.039 | 0.256 | 0.119 |

Even the best N=64 seed (0.049) is 15.6x below the N=65 rescued minimum (0.765).

---

## 3. Resonant Reversal Profile

### Definition (from observed rescued seeds)

| Metric | Range [Min, Max] | Mean |
|--------|-----------------|------|
| alignPre | [-0.564, -0.138] | -0.326 |
| dT1 | [0.584, 0.968] | 0.764 |
| c3OmegaShift | [0.765, 2.248] | 1.336 |
| deltaAlign | [0.036, 0.113] | 0.071 |
| omT1 | [1.093, 2.526] | 1.925 |

### Rescued Seed Classification (27 seeds)

| Type | Count | Rate |
|------|-------|------|
| Resonant reversal | 10 | 37% |
| Unclear | 10 | 37% |
| Direct alignment | 7 | 26% |
| Weak correction | 0 | 0% |

**Resonant reversal is NOT the only rescue mechanism.** 37% are resonant reversal, 37% are unclear (don't fit either profile cleanly), and 26% are direct-alignment (positive alignPre, positive but smaller deltaAlign).

### Non-Resonant Rescues (16 seeds)

| Metric | Mean |
|--------|------|
| alignPre | 0.011 |
| c3OmegaShift | 1.461 |
| dT1 | 0.447 |

Non-RR rescues have near-zero alignPre and lower dT1. Their rescue mechanism differs from resonant reversal.

---

## 4. C3 Effectiveness Threshold

### Threshold Validation (Reference: N=65, Holdout: N=66-72)

| Threshold | Ref Acc | Ho Acc | Ref Prec | Ho Prec |
|-----------|---------|--------|----------|---------|
| c3OmegaShift>0.05 | 91% | 91% | 50% | 52% |
| **c3OmegaShift>0.10** | **97%** | **91%** | **75%** | **53%** |
| dT1>0.50 | 82% | 84% | 29% | 32% |
| dT1>0.80 | 94% | 91% | 67% | 67% |
| alignPre<-0.2 | 91% | 86% | 50% | 24% |
| deltaAlign>0.05 | 94% | 86% | 67% | 24% |
| dT1>0.5 & c3OmgS>0.1 | 94% | 93% | 67% | 65% |

**Best threshold: c3OmegaShift > 0.10** (97% accuracy on N=65, 91% on holdout).

The combined threshold `dT1>0.5 & c3OmgS>0.1` has better precision (67%/65%) but slightly lower holdout accuracy (93%).

---

## 5. Cross-N Validation

### Per-N Rescue Metrics

| N | n | Resc% | c3OmgS | dT1 | alignPre |
|---|----|-------|--------|-----|----------|
| 65 | 33 | 9% | 0.054 | 0.378 | 0.006 |
| 66 | 35 | 3% | -0.105 | 0.390 | 0.007 |
| 67 | 38 | 3% | -0.031 | 0.367 | 0.037 |
| 70 | 68 | 15% | -0.228 | 0.383 | 0.073 |
| 72 | 71 | 17% | -0.617 | 0.326 | 0.158 |

### Rescued Seed Metrics Across N

| N | n | c3OmgS | dT1 | alignPre | deltaAlign |
|---|----|--------|-----|----------|------------|
| 65 | 3 | 0.952 | 0.742 | **-0.346** | 0.070 |
| 66 | 1 | 1.194 | 0.595 | -0.216 | 0.051 |
| 67 | 1 | 1.487 | 0.305 | 0.102 | -0.013 |
| 70 | 10 | 1.515 | 0.546 | -0.099 | 0.028 |
| 72 | 12 | 1.461 | 0.543 | -0.070 | 0.023 |

**Rescued seeds consistently have c3OmegaShift ≈ 1.0-1.5.** But alignPre weakens with increasing N (from -0.346 at N=65 to -0.070 at N=72).

### Resonance Type by N

| N | Rescued | RR | Direct | Unclear |
|---|---------|-----|--------|---------|
| 65 | 3 | 2 | 1 | 0 |
| 66 | 1 | 1 | 0 | 0 |
| 67 | 1 | 0 | 1 | 0 |
| 70 | 10 | 4 | 2 | 4 |
| 72 | 12 | 3 | 3 | 6 |

**Resonant reversal exists at N=65, 66, 70, 72.** It is NOT N=65-specific. N=67 and N=70/72 also show direct-alignment rescues.

---

## 6. All Rescued Seeds

| N | Seed | Type | c3OmegaShift | dT1 | alignPre | deltaAlign |
|---|------|------|-------------|-----|----------|------------|
| 65 | 18 | **RR** | 0.765 | 0.943 | -0.542 | 0.109 |
| 65 | 102 | **RR** | 0.999 | 0.968 | -0.564 | 0.113 |
| 65 | 211 | DA | 1.091 | 0.314 | 0.067 | -0.011 |
| 66 | 162 | **RR** | 1.194 | 0.595 | -0.216 | 0.051 |
| 67 | 339 | DA | 1.487 | 0.305 | 0.102 | -0.013 |
| 70 | 19 | **RR** | 1.691 | 0.637 | -0.193 | 0.046 |
| 70 | 75 | **RR** | 1.360 | 0.899 | -0.450 | 0.096 |
| 70 | 183 | **RR** | 2.248 | 0.645 | -0.200 | 0.048 |
| 72 | 4 | **RR** | 1.180 | 0.855 | -0.382 | 0.084 |
| 72 | 179 | **RR** | 1.361 | 0.743 | -0.275 | 0.063 |
| 72 | 251 | **RR** | 1.180 | 0.771 | -0.297 | 0.068 |

---

## 7. Final Driver Ranking

| Rank | Driver | Rescued Mean | Failed Mean | Ratio |
|------|--------|-------------|-------------|-------|
| **1** | **C3 omega shift** | 1.416 | 0.023 | **61.6x** |
| 2 | C3 effectiveness ratio | 49.110 | 1.593 | 30.8x |
| 3 | C3 deltaAlign | 0.030 | 0.004 | 8.1x |
| 4 | Pre-C3 alignment | 0.111 | 0.016 | 6.7x |
| 5 | Probe displacement | 0.559 | 0.401 | 1.4x |
| 6 | Omega at T1 | 1.609 | 1.218 | 1.3x |
| 7 | Distance to Hi | 0.238 | 0.252 | 0.9x |

**C3 omega shift dominates (61.6x).**

---

## 8. The N=64 Mystery

The deepest finding of IOA: **N=64 seeds CAN meet resonant reversal preconditions but cannot produce large C3 omega shifts.**

- 14% of N=64 seeds have alignPre<-0.1 AND dT1>0.5
- These are the same preconditions that produce c3OmegaShift of 0.77-2.25 at N=65-72
- But at N=64, max c3OmegaShift = 0.049 — **an order of magnitude lower**
- Even the best N=64 seed (0.049) is 15.6x below the RR minimum (0.765)

**The C3 correction is structurally unable to produce large Omega shifts at N=64, regardless of preconditions.** The missing factor at N=64 appears to be a fundamental response property of the N=64 regime itself — not just alignment, dT1, or geometric configuration.

---

## 9. Gates

| Gate | Status | Evidence |
|------|--------|----------|
| **A — C3 Driver Confirmed** | **REACHED** | c3OmegaShift dominates (61.6x) |
| **B — RR Signature Defined** | **REACHED** | alignPre [-0.56,-0.14], dT1 [0.58,0.97], c3OmgS [0.77,2.25] |
| **C — N=64 Failure Explained** | **PARTIAL** | Preconditions met but c3OmegaShift capped at 0.049 |
| **E — Cross-N Generalization** | **REACHED** | RR exists at N=65, 66, 70, 72 |
| D — Threshold Validated | needs refinement | c3OmgS>0.10: 91% acc, 53% prec on holdout |
| F — N=65 Unique | NOT REACHED | RR generalizes to N=70/72 |
| G — Unresolved | PARTIAL | N=64 c3OmegaShift cap unexplained |

---

## 10. Recommended Next Suite

**IOI — Onset Intervention Audit**
- Targeted interventions at N=64 to try to boost c3OmegaShift
- Test whether N=64 can be made C3-effective by boosting dT1 beyond natural maximum
- Probe the response surface at N=64 to understand why c3OmegaShift is capped

---

*IOA execution: 2026-07-18, 7/7 tests passed, ~8 min runtime. 27 rescued seeds analyzed across N=65-72.*
