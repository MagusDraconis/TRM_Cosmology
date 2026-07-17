# TRM V5.12 HBD: High-Basin Persistence Barrier Audit

**Branch:** feature/v5.12-high-basin-entry-conditions
**Status:** COMPLETE
**Date:** 2026-07-17

---

## Quick Summary

The persistence barrier has been identified. Two distinct collapse mechanisms operate:

1. **Before-Cupd d-compression**: Induced states are geometrically "over-compressed" (d too low, K too high vs Natural Hi equilibrium). During continuation, **d rebounds +0.29**, **K collapses -0.13**, pushing states back toward Lo.

2. **Coordinated d+K**: Induced states match Natural Hi geometry at T1 but **Omega crashes -1.94** during continuation. Threshold crossing was an artifact of intervention, not stable dynamics.

Both produce **0 strict persistence** at the best geometric dose (25% before-Cupd). The barrier is primarily d-rebound (+ Gate A) and secondarily K-collapse (+ Gate B).

---

## 1. Before-Cupd 25% d-Compression: Collapse Analysis

### State Class Inventory (30 Lo seeds)

| Class | N | Description |
|-------|---|-------------|
| Hi-proximal (distHi < distLo) | **23** | Nominal geometric success |
| Immediate Hi (Omega > THR) | 1 | Threshold crossing |
| Strict persistent | **0** | None survive +1 epoch |
| Not Hi-proximal | 7 | Intervention didn't reach Hi basin |

### Collapse Path: T1 → T2 (23 Hi-proximal non-persistent)

| Metric | Delta T1→T2 | Direction | Interpretation |
|--------|------------|-----------|----------------|
| **d_mean** | **+0.2882** | ⬆ REBOUND | d inflates massively — intervention undone |
| **K_mean** | **-0.1303** | ⬇ COLLAPSE | K evaporates via Cupd(rebounded d) |
| **K_std** | **+0.0761** | ⬆ WIDEN | Coupling distribution spreads |
| Omega | +1.0006 | ⬆ | Paradoxical increase (off-manifold) |
| Entry Score | -0.0407 | ⬇ | Score collapses |
| distHi | +0.0557 | ⬆ | Moves AWAY from Hi |
| distLo | +0.0150 | ⬆ | Moves away from Lo too |
| **Projection** | **-0.3233** | ⬇ AGAINST | Massive reversal along entry vector |

### Natural High T1→T2 (20-seed control)

| Metric | Delta T1→T2 | Direction |
|--------|------------|-----------|
| **d_mean** | **-0.2284** | ⬇ decreases naturally |
| **K_mean** | **+0.0967** | ⬆ amplifies naturally |
| **K_std** | **-0.0528** | ⬇ narrows naturally |
| Omega | -1.3489 | decreases |
| Entry Score | +0.0228 | slight improvement |

### Critical Asymmetry

Natural High **decreases d_mean** during continuation — this is the basin's natural relaxation. Induced states **increase d_mean** — the compressed d springs back. The RecoverFP dynamics restore equilibrium, which for Lo-origin seeds is toward Lo.

### Hi-proximal vs Natural Hi at T1

| Metric | Induced Hi-prox | Natural Hi |
|--------|----------------|------------|
| d_mean | **0.2802** | 0.6161 |
| K_mean | **1.0375** | 0.8931 |
| K_std | **0.1222** | 0.2181 |
| Entry Score | +0.0371 | -0.0109 |

**Key finding**: Induced states are MORE "Hi-like" than Natural Hi (lower d, higher K). But this is pathological — they are over-compressed, and the dynamics push them back.

---

## 2. Coordinated d+K 100%: Collapse Analysis

### State Class Inventory

| Class | N |
|-------|---|
| Hi-proximal | 10 |
| Immediate Hi | 13 |
| Strict persistent | **2** |
| Non-persistent immHi | 11 |

### Collapse Path: immHi non-persistent (N=11) T1→T2

| Metric | Delta T1→T2 | Direction |
|--------|------------|-----------|
| d_mean | **-0.5516** | ⬇ decreases |
| K_mean | **+0.2450** | ⬆ amplifies |
| K_std | -0.1197 | narrows |
| Omega | **-1.9382** | ⬇ MASSIVE DROP |
| distHi | -0.2253 | toward Hi |
| distLo | -0.1554 | toward Lo |

### immHi induced vs Natural Hi at T1

| Metric | Induced | Natural Hi |
|--------|---------|------------|
| d_mean | 0.7183 | 0.6924 |
| K_mean | 0.8456 | 0.8635 |
| K_std | 0.2153 | 0.2379 |

Induced states match Natural Hi geometry very closely at T1. They even move in the "correct" direction (d down, K up) during continuation — but **Omega crashes by 1.94**. The threshold crossing was an artifact.

---

## 3. The Two Collapse Mechanisms

### Mechanism 1: d-Rebound (Before-Cupd)

```
T1: over-compressed d (0.28 vs natural 0.62)
    → dynamics restore equilibrium
T2: d rebounds to ~0.57
    → Cupd maps higher d to lower K
    → K collapses from 1.04 to 0.91
    → State drifts toward Lo equilibrium
```

### Mechanism 2: Omega Artifact (Coordinated)

```
T1: d/K geometry matches Natural Hi
    but Omega was boosted by intervention shock
T2: dynamics normalize
    → Omega drops 1.94 to below threshold
    → d/K geometry actually IMPROVES
    → but state is no longer "high branch"
```

---

## 4. Component Barrier Ranking

| Rank | Barrier | Evidence | Severity |
|------|---------|----------|----------|
| **1** | **d-Rebound** | +0.288 d_mean increase in 23 Hi-proximal states | ⬛⬛⬛⬛ CRITICAL |
| **2** | **K-Collapse** | -0.130 K_mean drop (consequence of d-rebound) | ⬛⬛⬛⬛ CRITICAL |
| 3 | KStd Widening | +0.076 spread | ⬛⬛ MODERATE |
| 4 | Entry Score Collapse | -0.041 | ⬛⬛ MODERATE |
| 5 | Omega Artifact | -1.938 for coordinated | ⬛⬛⬛ SEVERE (coordinated) |
| 6 | Missing Trajectory History | Dynamics restore Lo-equilibrium | ⬛⬛⬛⬛⬛ FUNDAMENTAL |

---

## 5. Decision Gates

| Gate | Description | Status |
|------|-------------|--------|
| **Gate A** — d-Rebound Barrier | d_mean increases during continuation | **REACHED** (+0.288 in 23/23 Hi-prox) |
| **Gate B** — K-Collapse Barrier | K_mean drops during continuation | **REACHED** (-0.130, consequence of d-rebound) |
| Gate C — K-Structure Barrier | K_std widens | PARTIAL (+0.076) |
| **Gate D** — Entry Score Insufficient | Hi-proximal states fail despite good geometry | **REACHED** (23 Hi-prox, 0 strict) |
| **Gate E** — Trajectory History | Natural Hi naturally decreases d; induced rebounds | **REACHED** (opposite-sign delta) |
| Gate F — Rare Persistent Signature | Distinct signature for persistent seeds | 0 strict for before-Cupd, 2 for coordinated |
| **Gate G** — No Stable Barrier | Barrier clearly identified | **NOT REACHED** (d-rebound is measurable) |

---

## 6. Core Finding

**Induced Hi-proximal states are over-compressed relative to Natural Hi equilibrium.** Natural Hi sits at d_mean≈0.62 with a natural tendency to decrease d further. The 25% before-Cupd compression pushes states to d_mean≈0.28 — far below natural Hi levels. During continuation, the dynamics restore the seed's characteristic d_level, which for Lo-origin seeds is naturally higher.

The persistence barrier is fundamentally: **Lo-seed dynamics restore Lo-level d_mean, regardless of geometric intervention.** The intervention creates a transient geometric state, but the seed's intrinsic dynamics override it during continuation.

---

## 7. Claim Discipline Audit

| Claim | Status |
|-------|--------|
| d-rebound is the primary persistence barrier | **SUPPORTED** (+0.288, opposite sign from Natural Hi) |
| Before-Cupd states are over-compressed vs Natural Hi | **SUPPORTED** (d=0.28 vs 0.62) |
| Natural Hi dynamics decrease d during continuation | **SUPPORTED** (-0.228) |
| Hi-proximity is insufficient for persistence | **SUPPORTED** (23/23 fail strict) |
| Physical interpretation | **NOT CLAIMED** |
| Cross-N validity | **NOT CLAIMED** |

---

## 8. Recommended Next Suite

**HBE_D — High-Basin Equilibrium Entry via d-Targeting**

Instead of compressing d toward the Hi centroid (which overshoots), target the Natural Hi equilibrium d_mean (≈0.43 at CP5, accounting for natural drift). Apply mild, equilibrium-matched d-compression and test whether states can enter the Hi basin without triggering the d-rebound collapse.

Hypothesis: Entry requires matching the Hi equilibrium band, not the Hi centroid. Over-compression triggers restorative dynamics that undo the intervention.

---

## Test Summary

- File: `TRM.Tests/V5_12/V5_12_HighBasinPersistenceBarrierAudit_Tests.cs`
- Tests: 3 (HBD_01, HBD_02, HBD_03)
- Passed: 3
- Runtime: ~1m30s (Parallel.ForEach)
- Tagged: LongRunning
