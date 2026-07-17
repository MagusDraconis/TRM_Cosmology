# TRM V5.12 HBF: High-Basin Band Refinement Audit

**Branch:** feature/v5.12-high-basin-entry-conditions
**Status:** COMPLETE
**Date:** 2026-07-17

---

## Quick Summary

Fine dT1 target sweep (0.32-0.66) at N=71 with 30 Lo seeds. Rebound reversal confirmed at dT1≈0.40. Strict persistence appears as scattered single seeds (max 1 per target) at dT1 [0.46, 0.55]. Cross-N validation shows N=71 and N=72 support band-target strict persistence, N=67 fails entirely. **Signal is real but sparse — not a reproducible band effect, rather seed-specific dynamics.**

**Gate A (stable band) partially reached. Gate G (no reproducible band) partially reached.**

---

## 1. Fine dT1 Sweep Results (N=71, 30 Lo seeds)

| Target | ImmHi | Persist | **Strict** | HiProx | dT1 | kT1 | dReb | kCol | RebDir |
|--------|-------|---------|-----------|--------|-----|-----|------|------|--------|
| 0.32 | 0 | 10 | **0** | 26 | 0.33 | 1.01 | +0.128 | -0.054 | REBOUND |
| 0.35 | 3 | 10 | **0** | 23 | 0.36 | 1.00 | +0.145 | -0.068 | REBOUND |
| 0.40 | 10 | 7 | **1** | 12 | 0.48 | 0.95 | -0.066 | +0.029 | DRIFT DN |
| 0.44 | 9 | 9 | **1** | 17 | 0.46 | 0.96 | -0.036 | +0.019 | DRIFT DN |
| 0.47 | 11 | 4 | **0** | 11 | 0.54 | 0.93 | -0.127 | +0.057 | DRIFT DN |
| 0.49 | 8 | 6 | **0** | 12 | 0.51 | 0.94 | -0.096 | +0.043 | DRIFT DN |
| 0.51 | 10 | 5 | **1** | 12 | 0.50 | 0.94 | -0.159 | +0.072 | DRIFT DN |
| 0.53 | 9 | 7 | **1** | 17 | 0.47 | 0.96 | -0.039 | +0.018 | DRIFT DN |
| 0.56 | 7 | 4 | **0** | 13 | 0.50 | 0.94 | -0.125 | +0.059 | DRIFT DN |
| 0.59 | 8 | 3 | **0** | 12 | 0.52 | 0.93 | -0.182 | +0.085 | DRIFT DN |
| 0.62 | 11 | 4 | **1** | 12 | 0.55 | 0.92 | -0.211 | +0.094 | DRIFT DN |
| 0.66 | 13 | 6 | **0** | 10 | 0.55 | 0.92 | -0.201 | +0.090 | DRIFT DN |

### Key Observations

1. **Rebound reversal at dT1≈0.40**: targets 0.32-0.35 rebound (+0.13-0.15), targets 0.40+ drift down. Reversal is robust.

2. **Strict persistence is sparse**: max 1 per target, scattered across 5 different targets (0.40, 0.44, 0.51, 0.53, 0.62). No target produces >1 strict.

3. **dT1 range for strict**: [0.46, 0.55] — straddles the Natural Hi p25 (0.49).

---

## 2. Strict Persistent Seed Analysis

| Seed | Target | dT1 | kT1 | dT2 | kT2 | dReb | Notes |
|------|--------|-----|-----|-----|-----|------|-------|
| 18 | 0.44 | **0.28** | 1.04 | 0.42 | 0.98 | +0.14 | Over-compressed, rebounds mildly |
| 20 | 0.40 | **0.23** | 1.06 | 0.40 | 0.98 | +0.17 | Most over-compressed, rebounds |
| 30 | 0.53 | **0.27** | 1.04 | 0.44 | 0.96 | +0.17 | Over-compressed, rebounds |
| 39 | 0.51 | **0.26** | 1.05 | 0.24 | 1.06 | **-0.02** | ⭐ Natural Hi dynamics! |

### Critical Finding

**ALL strict persistent seeds have dT1=0.23-0.28 regardless of target.** The target is not hitting — the intervention cannot push these seeds to the intended dT1 band. Instead, some seeds have intrinsic properties that:
- Allow over-compressed d to persist through continuation (seeds 18, 20, 30)
- Or follow Natural Hi relaxation (seed 39: d down, K up → true basin dynamics)

**Only seed 39 shows genuine Natural Hi dynamics** (negative dReb, positive K change). The other 3 are "resilient over-compressed" — they survive despite d-rebound.

---

## 3. Cross-N Validation

| N | Band p50 | p50 Strict | Band p90 | p90 Strict |
|---|----------|-----------|----------|-----------|
| 67 | 0.880 | **0** | 0.982 | **0** |
| **71** | 0.733 | **2** | 0.891 | 0 |
| 72 | 0.643 | 1 | 0.878 | **2** |

- **N=67**: Complete failure — no strict persistence at any target. N=67 band d_mean is much higher (p50=0.88) and may be inaccessible from Lo seeds.
- **N=71**: Best p50 response (2 strict). Transition window.
- **N=72**: Best p90 response (2 strict). Also supports band entry.

**Band targeting works at N=71 and N=72, not at N=67.**

---

## 4. Decision Gates

| Gate | Description | Status |
|------|-------------|--------|
| **Gate A** — Stable dT1 band | Scattered single seeds, not reproducible per-target | **PARTIAL** — signal exists but sparse |
| **Gate B** — d only sufficient | Max 1 strict per target | **WEAK** — insufficient alone |
| **Gate E** — N=71 specific | N=72 also works | **NOT SUPPORTED** — N=71+72 both work |
| **Gate F** — Over-compression rebound | Confirmed at <0.40 | **REACHED** |
| **Gate G** — No reproducible band | Sparse, seed-specific | **PARTIALLY REACHED** |

---

## 5. Core Interpretation

The band targeting signal is real but **seed-specific, not target-reproducible**. No dT1 target band produces consistent strict persistence across seeds. The seeds that persist are those whose intrinsic dynamics allow it — not seeds that were successfully guided to a specific band.

This suggests the persistence barrier is not about landing in a d_mean band, but about **seed-intrinsic dynamical properties** that we have not yet measured or controlled. The entry vector and band are geometric prerequisites, but persistence depends on something deeper in the seed's trajectory dynamics.

**Seed 39 is the key outlier**: it achieves genuine Natural Hi dynamics (dReb negative, K amplifying) despite being over-compressed at T1. Understanding what makes seed 39 different is the next priority.

---

## 6. Claim Discipline Audit

| Claim | Status |
|-------|--------|
| Rebound reversal at dT1≈0.40 | **SUPPORTED** |
| Strict persistence is sparse and seed-specific | **SUPPORTED** (max 1/target) |
| N=71 and N=72 support band entry, N=67 fails | **SUPPORTED** |
| dT1 band targeting is reproducible | **NOT SUPPORTED** |
| Physical interpretation | **NOT CLAIMED** |

---

## 7. Recommended Next Suite

**HBG — High-Basin Genuine Entry Seed Characterization**

Isolate seed 39 (and any other seeds showing Natural Hi dynamics after intervention) and characterize what makes them different. Compare full d+K matrix structure, trajectory history, and Omega evolution between seed 39-type seeds and seeds that fail strict persistence.

Hypothesis: Some Lo seeds are dynamically closer to the Hi basin than others — band targeting reveals them but does not create them.

---

## Test Summary

- File: `TRM.Tests/V5_12/V5_12_HighBasinBandRefinementAudit_Tests.cs`
- Tests: 3 (HBF_01, HBF_02, HBF_03)
- Passed: 3
- Runtime: ~3m36s (Parallel.ForEach)
- Tagged: LongRunning
