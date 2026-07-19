# TRM V5.11 Final Synthesis: Branch Accessibility and Basin Structure

**Date:** 2026-07-17  
**Branch:** `feature/v5.11-branch-accessibility-and-basin-structure`  
**Status:** COMPLETE  
**Suites:** BAP, BAE, BAA (3 suites)  
**Tests:** 8 V5.11 tests, 2531 cumulative, 0 failed  
**Followed by:** `feature/v5.12-high-basin-entry-conditions`

---

## 1. V5.11 Research Question

**Why are High→Low transitions persistently accessible while Low→High persistent transitions are not?**

---

## 2. Final Answer

**Basin-accessibility asymmetry.** High→Low suppression reaches the natural Low basin. Low→High induction creates off-manifold states that remain geometrically closer to the Low basin.

---

## 3. Key Results

### BAE: High→Low Reaches Natural Low Basin

| N | SupprHi→NatLo | SupprHi→NatHi | Closer To |
|---|---------------|---------------|-----------|
| 67 | **0.023** | 0.101 | **Lo (4.4×)** |
| 71 | **0.163** | 0.304 | **Lo (1.9×)** |
| 72 | **0.325** | 0.486 | **Lo (1.5×)** |

### BAA: Low→High Stays in Lo Basin

| Induced Class | DistToLo | DistToHi | Closer To |
|---------------|----------|----------|-----------|
| Transient | **0.810** | 1.260 | **Lo (1.6×)** |
| Delayed | **0.642** | 0.980 | **Lo (1.5×)** |
| Failed | **0.445** | 0.606 | **Lo (1.4×)** |

---

## 4. Conclusion

High→Low works because suppression moves states into the target Low basin. Low→High fails because induction creates threshold-crossing off-manifold states that never enter the natural High basin.

---

## 5. Recommended V5.12

**Branch:** `feature/v5.12-high-basin-entry-conditions`  
**Question:** What conditions are necessary for a state to enter the natural High basin?

---

## Dev Stats

| Suite | Tests |
|-------|-------|
| BAP | 4 |
| BAE | 2 |
| BAA | 2 |
| **Total** | **8** |
| **Cumulative** | **2531** |
