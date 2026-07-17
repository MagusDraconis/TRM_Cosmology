# TRM V5.7 Reduced Operator Cross-Regime Execution (ROCE)

**Date:** 2026-07-17  
**Branch:** `feature/v5.7-recoverfp-reduced-operator-validation`  
**Status:** COMPLETE  
**Falsification result:** PARTIALLY FALSIFIED — Gate B

---

## 1. Cross-N Validation (seeds 0-29)

| N | B0 Hi | V1 Hi | R1 Hi | R1≤B0? | V9 Hi | V9 Amplifies? |
|---|-------|-------|-------|---------|-------|---------------|
| 60 | 0/30 | 0/30 | 0/30 | ✅ | — | — |
| 62 | 0/30 | 0/30 | 0/30 | ✅ | — | — |
| 64 | 0/30 | 1/30 | 0/30 | ✅ | — | — |
| **66** | **1/30** | **9/30** | **2/30** | **❌** | — | — |
| 67 | 5/30 | 12/30 | 2/30 | ✅ | 29/30 | ✅ |
| 69 | 7/30 | 13/30 | 5/30 | ✅ | — | — |
| 72 | 15/30 | 22/30 | 14/30 | ✅ | **8/30** | **❌** |
| 75 | 23/30 | 26/30 | 18/30 | ✅ | — | — |
| 80 | 25/30 | 29/30 | 22/30 | ✅ | **1/30** | **❌** |

**R1 ≤ B0 at 8/9 N values.** Only N=66 fails (R1=2/30 vs B0=1/30).

**V9 amplification FAILS at N≥72.** The Double-Cupd mechanism is N=67-specific — it does not generalize.

---

## 2. d→K→Omega Correlation

| N | d→K r | K→Ω r |
|---|-------|-------|
| 60 | −0.998 | **−0.021** |
| 62 | −0.996 | **0.054** |
| 64 | −0.998 | **0.124** |
| 66 | −0.997 | 0.615 |
| 67 | −0.998 | 0.608 |
| 69 | −0.999 | 0.794 |
| 72 | −0.996 | 0.587 |
| 75 | −0.952 | 0.713 |
| 80 | −0.979 | 0.890 |

**d→K is universal (r < −0.95 at all N).** The Cupd exponential is deterministic everywhere.

**K→Ω is NOT universal.** At N=60,62,64, K→Ω correlation is ~0 — K_mean does NOT predict Omega at low N. The V5.6 finding that K_mean mediates branch outcome is **N-dependent** — it only holds above N≈66 where the branch split exists.

---

## 3. Cross-Seed Blocks

| N | Block | B0 Hi | R1 Hi | R1≤B0? |
|---|-------|-------|-------|---------|
| 67 | 0–29 | 5/30 | 2/30 | ✅ |
| 67 | 30–59 | 5/30 | 2/30 | ✅ |
| 67 | 60–99 | 4/40 | 4/40 | ✅ |
| 72 | 0–29 | 15/30 | 14/30 | ✅ |
| **72** | **30–59** | **8/30** | **11/30** | **❌** |
| 72 | 60–99 | 18/40 | 14/40 | ✅ |
| 80 | 0–29 | 25/30 | 22/30 | ✅ |
| 80 | 30–59 | 25/30 | 22/30 | ✅ |
| **80** | **60–99** | **36/40** | **38/40** | **❌** |

**F2 triggered at N=72 Block 1 and N=80 Block 2.** Block stability fails. The V5.6 operator was calibrated on Block 0 only — it does not perfectly transfer to independent seed blocks.

---

## 4. Failure Criteria

| Criterion | Triggered? | Details |
|-----------|------------|---------|
| **F1** (N-local) | **PARTIALLY** | R1 ≤ B0 at 8/9 N, but N=66 is borderline fail |
| **F2** (Seed-block) | **YES** | Block instability at N=72 (maxDev 15%) and N=80 (maxDev 39%) |
| **F3** (Amplification) | **YES** | V9 fails at N=72 and N=80 — N=67 specific |
| F4 (d-K decoupling) | **NO** | d→K r < −0.95 everywhere |
| F5 (Instability) | **NO** | Zero invalid runs across all conditions |

---

## 5. Gate Classification: **B — Partially Robust**

| Gate | Description | Status |
|------|-------------|--------|
| A | Robust: all metrics pass | NOT REACHED |
| **B** | **Partially robust: works at most N, thresholds vary** | **REACHED** |
| C | Weak: only N=67 | NOT REACHED |

**Evidence for Gate B:**
- R1 ≤ B0 at 8/9 N values (strong cross-N)
- d→K correlation universal (strong mechanism core)
- V9 amplification fails at N≥72 (limited scope)
- K→Omega correlation breaks at N<66 (N-conditioned)
- Seed-block instability at N=72,80 (operator sensitivty to seeds)

---

## 6. What V5.7 Falsified

| V5.6 Claim | V5.7 Status |
|------------|-------------|
| d_mean → Cupd → K controls branch | **SUPPORTED** — d→K r < −0.95 everywhere |
| State-conditioned operator works cross-N | **SUPPORTED WITH CAVEATS** — works at 8/9 N, fails at N=66 |
| V9 Double-Cupd amplifies | **FALSIFIED AT N≥72** — N=67 specific |
| K→Omega mediates branch outcome | **N-CONDITIONED** — breaks at N<66 (no branch split) |
| Operator generalizes across seed blocks | **FALSIFIED** — block instability at N=72,80 |

---

## 7. Claim Discipline

### SUPPORTED
- d→K Cupd exponential is universal (r < −0.95 at all N)
- State-conditioned operator works at 8/9 N values (60–80)
- Zero invalid runs — operator is numerically stable

### FALSIFIED
- V9 amplification is NOT universal — N=67 specific
- K→Omega mediation is N-conditioned — only holds where branch split exists
- Cross-seed block stability is NOT guaranteed

### NOT CLAIMED
- Physical interpretation
- Universality beyond N=60–80
- Complete mechanism invariance

---

## 8. Recommended Next: ROCA

**ROCA: Reduced Operator Calibration Analysis**

- Determine N=66 boundary: why does R1 fail here?
- Characterize V9 amplification threshold: between which N does it disappear?
- Model K→Omega breakdown at N<66
- Recommend N-conditioned operator refinement

---

## Files

| File | Description |
|------|-------------|
| `TRM.Tests/V5_7/V5_7_ReducedOperatorCrossRegimeExecution_Tests.cs` | 6 tests |
| `docsV5_7/analysis/TRM_V5_7_ReducedOperatorCrossRegimeExecution.md` | This document |

## Development Stats

| Metric | Value |
|--------|-------|
| ROCE tests | 6 |
| V5.7 total | 14 |
| Cumulative | 2449 |
| Failed | 0 |
| Gate reached | B |
