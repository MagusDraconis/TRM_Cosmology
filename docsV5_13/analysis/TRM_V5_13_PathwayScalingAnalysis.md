# TRM V5.13 HVA: Pathway Scaling Analysis

**Branch:** feature/v5.13-high-basin-pathway-validation-and-scaling
**Status:** COMPLETE
**Date:** 2026-07-17

---

## Quick Summary

Per-class candidate quality and success rate analysis across N=67-80, train vs holdout cohorts. **Holdout degradation is NOT from candidate quality differences** — d0, km0 distributions are nearly identical across cohorts. The degradation is from **intervention effectiveness varying by seed cohort**, not from pathway classification failure. N=80 shows best holdout transfer (30-40%) but no pathway advantage. **Model status: N-conditioned and partially overfit.**

---

## 1. Per-Class Candidate Quality (Train vs Holdout)

### P1b Candidates

| N | Cohort | d0(mean) | d0(med) | km0(mean) | Strict Rate |
|---|--------|----------|---------|-----------|-------------|
| 71 | Train | 0.769 | 0.737 | 0.846 | **13.3%** |
| 71 | Holdout | 0.755 | 0.768 | 0.835 | **6.7%** |
| 72 | Train | 0.757 | 0.771 | 0.828 | 7.7% |
| 72 | Holdout | **0.823** | 0.831 | 0.800 | **0.0%** |
| 75 | Train | 0.859 | 0.885 | 0.757 | **20.0%** |
| 75 | Holdout | **0.876** | 0.895 | 0.751 | **0.0%** |
| 80 | Train | 0.917 | 0.942 | 0.732 | **40.0%** |
| 80 | Holdout | 0.878 | 0.875 | 0.745 | **30.8%** |

**Finding**: Holdout P1b candidates are often HIGHER quality (higher d0) but have LOWER success rates. N=80 is the exception — holdout transfers well.

### P1 Candidates

| N | Cohort | d0(mean) | km0(mean) | Strict Rate |
|---|--------|----------|-----------|-------------|
| 71 | Train | 0.563 | 0.911 | **16.7%** |
| 71 | Holdout | 0.592 | 0.899 | **0.0%** |
| 72 | Train | 0.587 | 0.905 | 10.0% |
| 72 | Holdout | 0.589 | 0.900 | **13.3%** |
| 75 | Train | 0.559 | 0.903 | **22.2%** |
| 75 | Holdout | 0.570 | 0.909 | **0.0%** |

**Finding**: N=75 P1 candidates have identical quality (d0=0.56 vs 0.57) but 22% vs 0% success. Quality is not the issue.

### P2 Candidates

| N | Cohort | N | d0(mean) | Strict Rate |
|---|--------|---|----------|-------------|
| 67 | Train | 24 | 0.277 | **0.0%** |
| 71 | Train | 19 | 0.290 | 5.3% |
| 71 | Holdout | 23 | 0.281 | **0.0%** |
| 72 | Train | 16 | 0.313 | 6.2% |
| 72 | Holdout | 17 | 0.285 | **0.0%** |

**Finding**: P2 candidates are abundant and identical across cohorts, but holdout P2 NEVER produces strict persistence. The P2 pathway is training-specific.

---

## 2. Class Frequency Dynamics

| N | Train P1+P1b | Train P2 | Holdout P1+P1b | Holdout P2 |
|---|-------------|----------|----------------|------------|
| 67 | 12 | **24** | 6 | **32** |
| 71 | **21** | 19 | **21** | 23 |
| 72 | **23** | 16 | **28** | 17 |
| 75 | **19** | 3 | **20** | 3 |
| 80 | **10** | 0 | **13** | 0 |

P2 (Crypto-Hi) collapses above N=72. P1+P1b peaks at N=72 then declines.

---

## 3. Degradation Root Cause

The degradation is **NOT** from:
- Candidate quality (d0, km0 nearly identical across cohorts)
- Class frequencies (similar P1/P2 counts)
- Target achievement (dT1 values similar)

The degradation is from **intervention response varying by cohort**. The same intervention protocol on almost identical seed profiles produces different outcomes depending on whether the seed was in the training set. This suggests the V5.12 pathway model partially overfit to seed-specific dynamics in seeds 0-99.

---

## 4. N=80: The Exception

N=80 is the only N where holdout transfer is strong (30-40%). Key features:
- **Only P1b candidates exist** (no P1, no P2)
- d0 is very high (0.88-0.92) — extreme compression room
- No pathway classification needed — all seeds are P1b
- **Universal protocols work almost as well as matched**

At high N, the pathway model becomes unnecessary because all candidates are P1b and universally responsive.

---

## 5. Model Status Classification

| Status | Assessment |
|--------|------------|
| **Robust** | ❌ Holdout degradation is significant |
| **Partially transferable** | ✅ Directional advantage at N=71/72 |
| **N-conditioned** | ✅ Behavior changes qualitatively with N |
| **Degraded/underfit** | ✅ P2 pathway never transfers |
| **Mostly local** | ✅ P2 is training-specific |

**Final classification: N-conditioned and partially overfit.**

The V5.12 pathway model has genuine but weak signal that degrades on holdout. P1/P1b partially transfers; P2 never transfers. Future models should be N-conditioned and tested on larger seed ensembles.

---

## 6. Decision Gates

| Gate | Status |
|------|--------|
| **A** — Model scales | **PARTIAL** (directional but degraded) |
| **B** — N=75 strongest | **QUALIFIED** (train only, holdout=0%) |
| **C** — N=80 saturation | **REACHED** (no pathway advantage needed) |
| **D** — Degradation explained | **REACHED** (intervention response, not quality) |
| **E** — Model overfit | **PARTIALLY REACHED** (P2 pathway is local) |
| **F** — N-conditioned required | **REACHED** |

---

## 7. Recommended Next

**HVI — Failure Mode Audit**: Characterize why P1b candidates with d0>0.85 fail on holdout at N=75 (0% vs 20% train) but succeed at N=80 (30% vs 40%). Identify the missing feature separating responsive from non-responsive seeds.

---

## Test Summary

- File: `TRM.Tests/V5_13/V5_13_PathwayScalingAnalysis_Tests.cs`
- Tests: 3 (HVA_01, HVA_02, HVA_03)
- Passed: 3
- Runtime: ~3min
- Tagged: LongRunning
