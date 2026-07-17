# TRM V5.12 HBC: High Entry Vector Intervention Audit

**Branch:** feature/v5.12-high-basin-entry-conditions
**Status:** COMPLETE
**Date:** 2026-07-17

---

## Quick Summary

Four intervention families tested at N=71 (30 Lo seeds each). **Before-Cupd d-compression at 25% strength produces the strongest geometric approach to the High basin** (23/30 states closer to Hi centroid than Lo). Coordinated d+K interventions paradoxically produce the most immediate Hi states (13-16/30) but push states AWAY from Hi in entry-vector space. Strict persistence remains rare across all interventions (max 2/30).

**Gate assessment:** No intervention achieves genuine Low→High entry with reliable strict persistence. Before-Cupd d-compression is the most geometrically correct direction.

---

## 1. Entry Vector (N=71, seeds 0-99)

Computed from Natural Hi and Natural Lo centroids at post-CP5:

| Component | Value | Direction |
|-----------|-------|-----------|
| dD (d_mean) | -0.0334 | Hi has LOWER d |
| dK (K_mean) | +0.0166 | Hi has HIGHER K |
| dKs (K_std)  | -0.0132 | Hi has NARROWER K |
| |v|        | 0.0399 |

Centroids:
- Lo: dm=0.4607, km=0.9598, ks=0.1934
- Hi: dm=0.4274, km=0.9764, ks=0.1802

Consistent with HBI: Hi basin is lower-d, higher-K, narrower-Ks.

---

## 2. I1: d-Only Projection

| Str | ImmHi | Persist | Strict | Mean Proj | Direction | Mean Score |
|-----|-------|---------|--------|-----------|-----------|------------|
| 25% | 5 | 7 | 0 | -0.0029 | AWAY | -0.0052 |
| 50% | 5 | 8 | **1** | +0.0096 | TWD | +0.0002 |
| 75% | 2 | 8 | 0 | +0.0573 | TWD | +0.0073 |
| 100% | 0 | 10 | 0 | +0.1072 | TWD | +0.0167 |

**Interpretation:** d-compression moves states weakly toward Hi at moderate-to-high doses. Score improves monotonically. Only 1 strict persistence (at 50%). High dose (100%) eliminates immediate Hi entirely — suggesting d over-compression prevents threshold crossing.

---

## 3. I2: K-Only Projection

| Str | ImmHi | Persist | Strict | Mean Proj | Direction | Mean Score |
|-----|-------|---------|--------|-----------|-----------|------------|
| 25% | 5 | 6 | 0 | -0.0319 | AWAY | -0.0055 |
| 50% | 5 | 5 | 0 | -0.0303 | AWAY | +0.0001 |
| 75% | 0 | 9 | 0 | +0.0715 | TWD | +0.0113 |
| 100% | 0 | 6 | 0 | +0.0743 | TWD | +0.0131 |

**Interpretation:** K-only produces no strict persistence at any dose. Low doses move AWAY from Hi (K amplification → larger d via dynamics). High doses move toward but eliminate immediate Hi.

---

## 4. I3: Coordinated d+K Entry-Vector

| Str | ImmHi | Persist | Strict | Mean Proj | Direction | Mean Score | <Hi | <Lo |
|-----|-------|---------|--------|-----------|-----------|------------|-----|-----|
| 25% | 13 | 6 | 0 | -0.0879 | AWAY | -0.0080 | 12 | 18 |
| 50% | 12 | 5 | 0 | -0.0906 | AWAY | -0.0121 | 10 | 20 |
| 75% | 16 | 5 | 0 | -0.1272 | AWAY | -0.0138 | 10 | 20 |
| 100% | 13 | 6 | **2** | -0.1236 | AWAY | -0.0139 | 10 | 20 |
| 125% | 15 | 6 | **2** | -0.1405 | AWAY | -0.0173 | 8 | 22 |

**PARADOXICAL RESULT:** Coordinated d+K produces the highest immediate Hi rates (13-16/30) but has the most NEGATIVE entry-vector projections (-0.09 to -0.14). States cross the Omega threshold but move geometrically AWAY from the Hi basin. This confirms that Omega threshold crossing is NOT basin entry — it's a threshold artifact that moves states off-manifold.

Only 2 strict persistence at 100%/125%, and even those are geometrically Lo-leaning (more states closer to Lo centroid).

---

## 5. I6: Before-Cupd d-Compression Channel

| Str | ImmHi | Persist | Strict | Mean Proj | Direction | Mean Score | <Hi | <Lo |
|-----|-------|---------|--------|-----------|-----------|------------|-----|-----|
| 25% | 1 | 13 | 0 | **+0.1342** | **TWD** | **+0.0199** | **23** | 7 |
| 50% | 2 | 10 | 1 | +0.0690 | TWD | +0.0153 | **22** | 8 |
| 75% | 7 | 9 | 1 | -0.0064 | AWAY | +0.0072 | 18 | 12 |
| 100% | 9 | 7 | 0 | +0.0352 | TWD | +0.0084 | 18 | 12 |

**BEST GEOMETRIC APPROACH:** 25% dose produces 23/30 states closer to Hi centroid. This is the strongest basin-approximation signal. However, persistence is delayed (13 persist, only 1 immediate) and no strict persistence.

Higher doses produce more immediate Hi but lose geometric alignment (50%→22, 75%→18). The 25% sweet spot suggests the entry channel is narrow.

---

## 6. Decision Gates

| Gate | Description | Status |
|------|-------------|--------|
| **Gate A** | d-only sufficient | **PARTIAL** — 1 strict at 50%, moves toward Hi, but unreliable |
| **Gate B** | K-only sufficient | **NOT REACHED** — 0 strict at all doses |
| **Gate C** | Coordinated d+K required | **PARTIAL** — 2 strict at 100%/125% but anti-aligned projection |
| **Gate D** | Full geometry required | NOT TESTED (full d+K matrix projection unsafe) |
| **Gate E** | Before-Cupd channel required | **STRONG** — best geometry (23/30 Hi-proximal at 25%), but only 1 strict |
| **Gate F** | N=71 specific | DEFERRED |
| **Gate G** | No genuine entry | **PARTIALLY REACHED** — strict persistence ≤ 2/30 across all interventions |
| **Gate H** | Invalid/unsafe | NOT REACHED — all interventions valid |

---

## 7. Key Finding: The Before-Cupd Channel

The Before-Cupd d-compression at 25% is the only intervention that produces geometric Hi-basin proximity for a majority of seeds (23/30). This supports:

1. **d-compression before Cupd** (not after) is the correct entry direction
2. The Cupd exponential K=K0·exp(-d/ξ) naturally amplifies K when d is compressed — this is the natural basin-access channel
3. But persistence remains elusive — geometric proximity does not guarantee dynamic stability

Post-hoc interventions after Cupd (I1-I3) bypass the natural d→Cupd→K channel and produce off-manifold states that cross Omega but don't approach Hi geometry.

---

## 8. The Omega-Paradox

Coordinated d+K intervention (I3) at 100% produces:
- **13/30 immediate Hi** (highest immediate rate)
- **-0.124 mean projection** (strongest anti-alignment)
- **10/30 Hi-proximal** (worst geometry)

Omega crossing is **inversely correlated** with Hi-basin geometric proximity. The more aggressively we push toward the threshold, the further states move from actual Hi geometry. This is the core falsification of threshold-based induction.

---

## 9. Claim Discipline Audit

| Claim | Status |
|-------|--------|
| Before-Cupd d-compression produces best Hi-basin geometry | **SUPPORTED** (23/30 at 25%) |
| Omega crossing ≠ basin entry | **STRONGLY SUPPORTED** (I3 paradox) |
| Strict persistence is rare across all interventions | **SUPPORTED** (max 2/30) |
| Before-Cupd channel is correct entry direction | **SUPPORTED** |
| Genuine Low→High entry achieved | **NOT CLAIMED** |
| Physical interpretation | **NOT CLAIMED** |
| Cross-N validity | **NOT CLAIMED** |

---

## 10. Recommended Next Suite

**HBD — High-Basin Entry via d-Compression Continuation**

The Before-Cupd 25% d-compression produces 23/30 Hi-proximal states. Test extended continuation (+2, +3, +5 epochs) to determine whether geometric proximity eventually converts to stable persistence. Also test d-compression doses between 10% and 35% to map the entry channel width.

Hypothesis: Entry requires geometric approach (achieved) + extended relaxation into the basin (not yet tested).

---

## Test Summary

- File: `TRM.Tests/V5_12/V5_12_HighEntryVectorInterventionAudit_Tests.cs`
- Tests: 5 (HBC_01 through HBC_05)
- Passed: 5
- Runtime: ~5 min
