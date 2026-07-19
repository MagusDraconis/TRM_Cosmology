# TRM V5.10 Final Synthesis: Branch Control Asymmetry and Induction

**Date:** 2026-07-17  
**Branch:** `feature/v5.10-branch-control-asymmetry-and-induction`  
**Status:** COMPLETE  
**Suites:** CAP, CAE, CAA, CAI, CAJ, CAK, CAL (7 suites)  
**Tests:** 13 V5.10 tests, 2523 cumulative, 0 failed  
**Followed by:** `feature/v5.11-branch-accessibility-and-basin-structure`

---

## 1. V5.10 Research Question

**Why can high branches be suppressed while low branches resist persistent induction?**

---

## 2. Final Answer

**Branch control is asymmetric.** High→Low suppression is reproducible. Low→High can produce transient or delayed high states, but **no intervention produced strict persistent Low→High conversion** under tested criteria.

---

## 3. Suite Summary

| Suite | Key Finding |
|-------|-------------|
| CAP | Protocol pre-registered |
| CAE | K boost appeared to induce 100% Lo→Hi |
| CAA | K boost = threshold artifact (0% persistence) |
| CAI | K-graft works strongest at N=71 |
| CAJ | Full K graft is best immediate inducer |
| CAK | CAJ "persistence" = delayed induction; strict persistence ≈ 0 |
| CAL | d+K graft still 0 strict persistence — coordinated state fails |

---

## 4. Induction Validity Hierarchy

| Evidence Level | Sufficient? |
|----------------|-------------|
| Threshold crossing (Omega > 1.783) | ❌ Not sufficient |
| Immediate induction (post-graft high) | ❌ Not sufficient |
| Delayed induction (high after +1 epoch) | ❌ Not sufficient |
| **Strict persistence** (immediate + survives +1 epoch) | **Required** |

Only strict persistence counts as genuine induction. None was demonstrated.

---

## 5. Control Asymmetry Summary

| Direction | Outcome |
|-----------|---------|
| **High→Low** | ✅ Reproducible (d_mean increase, 14-31%) |
| Low→High (immediate) | ⚠️ Partially (K-graft, 41% at N=71) |
| **Low→High (persistent)** | **❌ NOT DEMONSTRATED** |

---

## 6. N=71 Transition Regime

N=71 is the most intervention-responsive N across V5.7, V5.9, and V5.10. It is:
- V5.7 transition point (V9 flip, R1 fail)
- V5.9 most plastic regime
- V5.10 strongest graft induction N

---

## 7. Final Conclusion

V5.10 tested whether branch-control asymmetry could be overcome through stronger interventions. Transient and delayed induction were observed. No intervention produced strict persistent Low→High conversion. **V5.9 directional commitment is strengthened**, not rejected.

---

## 8. Development Stats

| Suite | Tests |
|-------|-------|
| CAP | 4 |
| CAE | 3 |
| CAA | 2 |
| CAI | 2 |
| CAJ | 2 |
| CAK | 2 |
| CAL | 2 |
| **Total** | **17** |

---

## 9. Recommended V5.11

**Branch:** `feature/v5.11-branch-accessibility-and-basin-structure`  
**Question:** Why are High→Low transitions persistently accessible while Low→High persistent transitions are not?
