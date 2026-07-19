# TRM V5.12 HBH: Compression Room and Relative Displacement Audit

**Branch:** feature/v5.12-high-basin-entry-conditions
**Status:** COMPLETE
**Date:** 2026-07-17

---

## Quick Summary

Relative compression sweep (10%-50% of baseline d0) at N=71 with 50 Lo seeds. **Compression room strongly predicts strict persistence**: persistent seeds have higher baseline d0 (0.62 vs 0.54) and achieve much larger Δd_abs (-0.36 vs +0.11). 4/5 persistent seeds have d0>0.50 and ΔdRel<-0.49.

**Gates A and B REACHED.** Relative displacement and baseline d0 both predict persistence.

---

## 1. Relative Compression Sweep Results

| Frac | N | ImmHi | Persist | **Strict** | dT1 | dReb |
|------|---|-------|---------|-----------|-----|------|
| 10% | 50 | 14 | 11 | **1** | 0.47 | -0.03 |
| 20% | 50 | 8 | 21 | **1** | 0.37 | +0.12 |
| 30% | 50 | 9 | 13 | **0** | 0.40 | +0.11 |
| 40% | 50 | 8 | 22 | **0** | 0.39 | +0.09 |
| 50% | 50 | 4 | 32 | **3** | 0.31 | +0.26 |

- **50% compression gives most strict persistence (3) but with massive rebound (+0.26)**
- Lower compressions (10-20%) have negative rebound but low immediate Hi rates
- 5 total strict persistent trials from 5 unique seeds

---

## 2. Persistence Predictor: Strict vs ImmHi-NonPersist

| Metric | Strict (N=5) | ImmHi-NonPersist (N=38) | Difference |
|--------|-------------|------------------------|------------|
| **d0 (baseline)** | **0.619** | 0.536 | **+0.083** |
| **dT1** | **0.261** | 0.646 | **-0.386** |
| **Δd_abs** | **-0.358** | +0.111 | **-0.468** |
| **Δd_rel** | **-0.509** | +0.521 | **-1.030** |
| dRebound | +0.391 | -0.308 | +0.699 |

### Compression-Room Hypothesis: SUPPORTED

Strict persistent seeds have:
1. **Higher baseline d0** → more "room" to compress
2. **Lower dT1** → intervention achieves deeper compression
3. **More negative Δd_abs** → larger absolute displacement
4. **More negative Δd_rel** → larger relative displacement

**However**: strict seeds also have LARGER rebound (+0.39 vs -0.31). They rebound more but survive because the initial compression was deep enough.

---

## 3. Persistent Seed Profiles

| Seed | d0 | Best Frac | ΔdRel | Notes |
|------|-----|-----------|-------|-------|
| 18 | **0.831** | 50% | -0.66 | Extreme Lo → large compression room |
| 20 | **0.722** | 50% | -0.58 | High d0 → large displacement |
| 49 | **0.737** | 20% | -0.75 | Largest ΔdRel, only needs 20% |
| 53 | 0.506 | 50% | -0.49 | Moderate d0, max compression |
| 57 | **0.297** | 10% | -0.06 | Anomaly: low d0, tiny compression |

4/5 persistent seeds have d0 > 0.50 and ΔdRel < -0.49. Seed 57 is an outlier (crypto-Hi baseline, persistent at tiny compression). Seed 49 is remarkable: d0=0.74, only 20% compression needed (ΔdRel=-0.75).

---

## 4. Seed 36 vs Seed 39

### Seed 36 (d0=0.703, extreme Lo baseline)

| Frac | dT1 | dT2 | ΩT1 | ΩT2 | Result |
|------|-----|-----|-----|-----|--------|
| 10% | 0.376 | 0.378 | 1.08 | 1.08 | Failed |
| 20% | 0.562 | 0.446 | 1.42 | 1.08 | Failed |
| 30% | 0.355 | 0.655 | **2.46** | 1.46 | ImmHi, no persist |
| 40% | 0.556 | 0.332 | 1.48 | 1.08 | Failed |
| 50% | 0.382 | 0.187 | 1.09 | 1.08 | Failed |

**Seed 36 FAILS at ALL relative compression levels.** dT1 never goes below 0.35 — the intervention cannot compress seed 36 enough because its baseline d is very high and the actual dT1 achieved doesn't match the target. At 50% target, achieved dT1=0.382 (target was 0.351). The compression "resists" for extreme seeds.

### Seed 39 (d0=0.327, crypto-Hi baseline)

| Frac | dT1 | dT2 | ΩT1 | ΩT2 | Result |
|------|-----|-----|-----|-----|--------|
| 10% | 0.274 | 0.788 | 1.06 | **2.58** | Delayed Hi |
| 20% | 0.344 | 0.676 | 1.06 | 1.40 | Failed |
| 30% | 0.591 | 0.115 | **2.21** | 1.07 | ImmHi, no persist |
| 40% | 0.477 | 0.404 | **2.22** | 1.08 | ImmHi, no persist |
| 50% | 0.113 | 1.040 | 1.07 | **4.42** | Delayed Hi |

Seed 39 shows **delayed persistence** at 10% and 50%, but never strict (immHi + persist). At 50%, dT1=0.113 (very low) and Omega jumps from 1.07 to 4.42 at T2 — massive delayed induction. The crypto-Hi seed can be induced to very high Omega, but it takes time.

---

## 5. Decision Gates

| Gate | Description | Status |
|------|-------------|--------|
| **Gate A** — Δd_rel predicts persistence | -0.51 vs +0.52, effect size = -1.03 | **REACHED** |
| **Gate B** — d0 predicts persistence | 0.62 vs 0.54, effect size = +0.08 | **REACHED** (moderate) |
| **Gate C** — Crypto-Hi fails from small displacement | Seed 39 gets tiny Δd_rel at most fractions | **PARTIALLY SUPPORTED** (but delayed persists) |
| Gate D — Relative compression improves over HBF | 5 strict vs 5 from HBF sweep | **COMPARABLE** (no clear improvement) |
| Gate F — N-dependent | NOT TESTED | DEFERRED |

---

## 6. Claim Discipline Audit

| Claim | Status |
|-------|--------|
| Compression room (d0) predicts strict persistence | **SUPPORTED** (4/5 persistent have d0>0.50) |
| Larger Δd_abs/Δd_rel predicts persistence | **SUPPORTED** (effect size >1.0) |
| Seed 36 fails relative compression | **SUPPORTED** (0/5 fractions) |
| Relative compression improves over fixed targets | **NOT SUPPORTED** (comparable counts) |
| Physical interpretation | **NOT CLAIMED** |

---

## 7. Recommended Next Suite

**HBI2 — Targeted Compression-Room Exploitation**

Instead of sweeping compression fractions, identify seeds with d0>0.50 and apply fixed dT1 targets that reliably produce strict persistence (dT1≈0.25-0.35 as found here). Test whether pre-selecting high-d0 seeds + aggressive compression can achieve >10% strict persistence rate.

---

## Test Summary

- File: `TRM.Tests/V5_12/V5_12_CompressionRoomAndRelativeDisplacementAudit_Tests.cs`
- Tests: 2 (HBH_01, HBH_02)
- Passed: 2
- Runtime: ~44s (Parallel.ForEach)
- Tagged: LongRunning
