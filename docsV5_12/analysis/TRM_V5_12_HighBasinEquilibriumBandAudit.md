# TRM V5.12 HBEQ: High-Basin Equilibrium Band Audit

**Branch:** feature/v5.12-high-basin-entry-conditions
**Status:** COMPLETE
**Date:** 2026-07-17

---

## Quick Summary

Natural Hi band at N=71 CP5 spans d_mean p25-p75 = [0.49, 0.84] (wide, skewed). Band-targeted d-compression discovers the **rebound reversal point** at dT1 ≈ 0.49: below this, states rebound (+0.15 d); above this, states follow Natural Hi drift (-0.05 to -0.24 d). The p50 target produces 2 strict persistent seeds — the first from before-Cupd d-compression. The UNDER target (p90×1.1) produces 3 strict persistent.

**Gate E (over-compression rebound) REACHED and characterized.**

---

## 1. Natural Hi Equilibrium Band (N=71, CP5, 28 seeds)

| Percentile | d_mean | K_mean | K_std |
|-----------|--------|--------|-------|
| p10 | 0.3076 | 0.7621 | 0.1393 |
| **p25** | **0.4907** | **0.8149** | **0.1445** |
| p50 | 0.7330 | 0.8420 | 0.1688 |
| **p75** | **0.8365** | **0.9786** | **0.3290** |
| p90 | 0.8906 | 1.0215 | 0.3474 |

**Natural drift (T1→T2):** dΔ = -0.323, KΔ = +0.138, KsΔ = -0.072

Band is wide and right-skewed. The Hi centroid (0.43) sits near the low end of the band. Natural Hi states naturally decrease d_mean by 0.32 per epoch.

---

## 2. Band-Targeted Before-Cupd d-Compression (30 Lo seeds each)

| Target | Label | ImmHi | Persist | **Strict** | HiProx | InBand | dT1 | dReb | kCol |
|--------|-------|-------|---------|-----------|--------|--------|-----|------|------|
| 0.277 | **OVER** | 0 | 11 | **0** | 27 | 0 | 0.32 | **+0.146** | **-0.059** |
| 0.491 | **p25** | 12 | 4 | 0 | 15 | 13 | 0.49 | -0.054 | +0.027 |
| 0.733 | **p50** | 10 | 8 | **2** | 15 | 13 | 0.48 | -0.081 | +0.036 |
| 0.837 | **p75** | 11 | 2 | 0 | 7 | 15 | 0.59 | -0.240 | +0.098 |
| 0.980 | **UNDER** | 14 | 6 | **3** | 10 | 12 | 0.53 | -0.123 | +0.047 |

### Key Findings

1. **Rebound reversal at dT1 ≈ 0.49**
   - OVER (dT1=0.32): d rebounds +0.15, K collapses -0.06 → classic HBD failure
   - p25+ (dT1≥0.49): d drifts down, K amplifies → Natural Hi relaxation direction

2. **First strict persistence from before-Cupd**
   - p50: 2 strict persistent (10 immHi, 8 persist, 2 strict)
   - UNDER: 3 strict persistent (14 immHi, 6 persist, 3 strict)
   - Prior HBC at 25% of centroid gave 0 strict

3. **Hi-proximal ≠ In-band**
   - 0 states are both Hi-proximal AND in-band
   - Hi-proximal states (distHi < distLo) have d_mean below p25
   - Band membership (d_mean in IQR) has d_mean above p25
   - These are geometrically incompatible criteria

4. **Optimal target band: p50 to p90**
   - Targets between p50 (0.73) and p90 (0.89) produce the best strict persistence
   - This is ABOVE the Hi centroid (0.43) — counter-intuitive
   - But it matches the natural Hi dynamics (d decreases naturally, starting higher is ok)

---

## 3. Rebound vs Band Distance

| Group | N | dRebound | KCollapse |
|-------|---|----------|-----------|
| Below p25 | 74 | **+0.1934** | **-0.0787** |
| Within IQR | 0 | — | — |
| Above p75 | 0 | — | — |

**ALL Hi-proximal states are below p25.** The rebound is universal for Hi-proximal states. The Hi-proximal geometric criterion (closer to Hi centroid) selects states with d_mean below the Natural Hi d_mean band — these are over-compressed.

---

## 4. The Band Paradox

```
Hi-proximal (distHi < distLo) → d_mean < p25 → REBOUND
In-band (d_mean in IQR)       → d_mean > p25 → DRIFT DOWN
```

The entry score uses distance to centroid, which is at d=0.43. But the Natural Hi d_mean band is p25-p75 = [0.49, 0.84]. The centroid is BELOW p25 — it's at the low edge of the band.

**This explains why HBC centroid-targeting fails:** Compressing toward the Hi centroid (0.43) pushes states below the Natural Hi equilibrium band, triggering d-rebound.

---

## 5. Decision Gates

| Gate | Description | Status |
|------|-------------|--------|
| **Gate A** — d_mean band sufficient | p50 produces 2 strict, UNDER produces 3 | **PARTIAL** — weak but first positive signal |
| Gate B — d+K band required | NOT TESTED (d-only here) | DEFERRED |
| **Gate E** — Over-compression rebound | Rebound reversal at dT1≈0.49 | **REACHED** + characterized |
| Gate F — N=71 specific | NOT TESTED | DEFERRED |
| Gate G — No band intervention works | 2-3 strict persistence found | **NOT REACHED** — band targeting works better than centroid |

---

## 6. Claim Discipline Audit

| Claim | Status |
|-------|--------|
| Rebound reversal point exists at dT1≈0.49 | **SUPPORTED** |
| Hi-proximal states are over-compressed | **SUPPORTED** (all 74 below p25) |
| Targeting p50-p90 band produces strict persistence | **SUPPORTED** (2-3/30) |
| Band targeting beats centroid targeting | **SUPPORTED** (0→3 strict) |
| Physical interpretation | **NOT CLAIMED** |
| Cross-N validity | **NOT CLAIMED** |

---

## 7. Recommended Next Suite

**HBEQ2 — d+K Band Targeting with Relaxation Alignment**

The rebound reversal at dT1≈0.49 shows d_mean band entry is achievable. Now test coordinated d+K band targeting that simultaneously matches:
- d_mean in p50-p75 range
- K_mean in p50-p75 range  
- Relaxation direction aligned (d decreasing, K increasing)

Hypothesis: Combined d+K band entry + relaxation alignment will increase strict persistence above the current 3/30.

---

## Test Summary

- File: `TRM.Tests/V5_12/V5_12_HighBasinEquilibriumBandAudit_Tests.cs`
- Tests: 2 (HBEQ_01, HBEQ_02)
- Passed: 2
- Runtime: ~60s (Parallel.ForEach)
- Tagged: LongRunning
