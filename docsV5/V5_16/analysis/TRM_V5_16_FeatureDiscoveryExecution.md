# TRM V5.16 EGE: Feature Discovery Execution

**Suite:** EGE | **Status:** COMPLETE | **Date:** 2026-07-18

## Quick Summary

7 new feature families (F1-F7) extracted across 263 profiles. **Most new features are redundant with M3+** (matEntropy r=0.94 with dMean, spEffRank r=0.93 with projHiVec, lambda3 r=0.98 with dStd). A few carry independent signal: **rebMagnitude** (ES=1.73 FN/TN, 2.63 FP/TP), **loOverlap** (ES=0.82 FP/TP), **edgeDensity** (ES=0.70 FN/TN), **dRespElasticity** (ES=0.52 FN/TN). **Intervention response features** (F7) show strongest residual separation. N=75: rebMagnitude ES=2.04, nodeConcIdx ES=0.89. **Gate G: No new feature family dramatically outperforms M3+.** Explanatory gap likely in intervention dynamics, not pre-intervention geometry.

---

## 1. Feature Availability (EGE_01)

All 7 families, 14 features — **100% extractable** with current engine. No missing data.

| Family | Key Features | Status |
|--------|-------------|--------|
| F1 Matrix Loc | matEntropy, concRatio | ✓ |
| F2 Node Conc | rowSumMax, nodeConcIdx | ✓ |
| F3 Topology | meanDeg, edgeDensity | ✓ |
| F4 Spectral | lambda3, spEffRank | ✓ |
| F5 Motif | hiOverlap, loOverlap | ✓ |
| F6 Memory | cumDmov, dirConsistency | ✓ |
| F7 Response | rebMagnitude, dRespElasticity | ✓ |

---

## 2. Redundancy Analysis (EGE_04)

**DEAD ENDS** (max|r|>0.7 with M3+ features):

| Feature | r(projHiVec) | r(dMean) | Redundant |
|---------|-------------|----------|-----------|
| matEntropy | -0.89 | 0.94 | **YES** |
| concRatio | -0.76 | 0.82 | **YES** |
| nodeConcIdx | -0.90 | 0.92 | **YES** |
| lambda3 | -0.55 | 0.47 (0.98 w/ dStd) | **YES** |
| spEffRank | -0.93 | 0.89 | **YES** |

**NON-REDUNDANT** (max|r|<0.7):

| Feature | max|r| |
|---------|------|
| edgeDensity | 0.25 |
| hiOverlap | 0.67 |
| loOverlap | 0.59 |
| cumDmov | 0.39 |
| dirConsistency | 0.23 |
| **rebMagnitude** | **0.12** |
| dRespElasticity | 0.35 |

F1, F2, F4 are redundant dead ends — they re-measure M3+ geometry. F3, F5, F6, F7 carry independent information.

---

## 3. Residual Contrast (EGE_02)

### FN vs TN (G4 vs G3) — Train

| Feature | FN Mean | TN Mean | **Effect Size** |
|---------|---------|---------|----------------|
| **rebMagnitude** | 0.42 | -0.09 | **1.73** |
| edgeDensity | 0.500 | 0.500 | 0.70 |
| dRespElasticity | 22.3 | 5.9 | 0.52 |
| lambda3 | 6.33 | 8.07 | 0.33 |

### FP vs TP (G2 vs G1) — Train

| Feature | FP Mean | TP Mean | **Effect Size** |
|---------|---------|---------|----------------|
| **rebMagnitude** | -0.16 | 0.46 | **2.63** |
| loOverlap | 0.52 | 0.28 | 0.82 |
| nodeConcIdx | 1.14 | 1.17 | 0.58 |

### N=75 Success vs Failure

| Feature | Succ | Fail | **Effect Size** |
|---------|------|------|----------------|
| **rebMagnitude** | 0.40 | -0.19 | **2.04** |
| nodeConcIdx | 1.17 | 1.20 | 0.89 |
| hiOverlap | 0.95 | 0.90 | 0.44 |
| lambda3 | 8.18 | 10.32 | 0.43 |

---

## 4. Feature Family Ranking (EGE_03)

Aggregate ES low because most features are redundant. Individual contrast shows real signal:

| Family | Best Signal | ES | Actionable? |
|--------|------------|-----|-------------|
| F7 Response | rebMagnitude | 1.73-2.63 | **PROMISING** |
| F3 Topology | edgeDensity | 0.70 | Marginal |
| F5 Motif | loOverlap | 0.82 | Marginal |
| F6 Memory | dirConsistency | 0.12 | Dead end |
| F1/F2/F4 | — | — | **Redundant dead ends** |

---

## 5. Candidate Shortlist (EGE_05)

**1. F7 — Intervention Response (rebMagnitude, dRespElasticity)**
- ES: 1.73 (FN/TN), 2.63 (FP/TP), 2.04 (N=75)
- Non-redundant: max|r|=0.35 with M3+
- **Strongest new signal.** Rebound dynamics carry explanatory power.

**2. F3 — Topology (edgeDensity)**
- ES: 0.70 (FN/TN)
- Fully non-redundant: max|r|=0.25
- Weak but independent.

**3. F5 — Motif Overlap (loOverlap)**
- ES: 0.82 (FP/TP)
- Max|r|=0.59 — some redundancy
- Moderate signal.

---

## 6. Decision Gates

| Gate | Status |
|------|--------|
| A — New Feature Found | NOT REACHED (family aggregate) |
| B — Matrix Localization | NOT REACHED (redundant) |
| C — Topology/Motif | **WEAK** (edgeDensity 0.70, loOverlap 0.82) |
| D — Spectral Shape | NOT REACHED (redundant) |
| E — Trajectory Memory | NOT REACHED |
| F — Provenance | NOT REACHED |
| **G — No New Feature** | **REACHED** (no family dramatically outperforms M3+) |

---

## 7. Key Insight

**Most new geometry features are redundant with M3+.** The explanatory gap is not in pre-intervention geometry — it's in **intervention response dynamics** (rebMagnitude, dRespElasticity). False negatives show large positive rebound (can't sustain High), false positives show negative rebound (oscillation around threshold). N=75 success/failure shows same pattern (ES=2.04).

**Shift recommended:** From geometry discovery to **intervention-dynamics characterization**. The remaining variance may be about HOW seeds respond to intervention, not WHAT they look like before it.

---

## 8. Next: EGA — Feature Validation

Validate rebMagnitude and loOverlap on holdout. If intervention-response features transfer, V5.16 shifts to dynamics characterization.
