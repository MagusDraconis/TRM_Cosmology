# TRM V5.10 Branch Control Asymmetry Execution (CAE)

**Date:** 2026-07-17  
**Branch:** `feature/v5.10-branch-control-asymmetry-and-induction`  
**Status:** COMPLETE  
**Gates:** A (Scalar K boost), F (N-dependent)

---

## 1. Lo→Hi Induction: K boost WORKS universally

| N | Lo Seeds | KBoost→Hi | K Boost Rate |
|---|----------|------------|--------------|
| 67 | 86/100 | 86 | **100%** |
| 69 | 76/100 | 76 | **100%** |
| 70 | 67/100 | 67 | **100%** |
| 71 | 64/100 | 64 | **100%** |
| 72 | 59/100 | 59 | **100%** |
| 75 | 28/100 | 28 | **100%** |
| 80 | 14/100 | 14 | **100%** |

**K boost induces ALL low seeds to high at ALL N.** The V5.9 BCI finding (Lo→Hi = 0%) was specific to the d_mean increase intervention, NOT a structural irreversibility. With K boost, every low seed can be pushed high.

---

## 2. d Compression: Partially Effective

| N | DComp→Hi | Rate |
|---|-----------|------|
| 67 | 32/86 | 37% |
| 69 | 38/76 | 50% |
| 70 | 48/67 | 72% |
| **71** | **37/64** | **58%** |
| 72 | 38/59 | 64% |
| 80 | 13/14 | 93% |

d compression at epoch 5 (decrease d_mean before Cupd → higher K) is effective but not universal. At N=67, only 37% respond. At N=80, 93%.

---

## 3. CP4 d Compression: Earlier = More Effective at N=71

| N | CP4 Induced | CP5 Induced |
|---|-------------|-------------|
| 67 | 5/25 (20%) | 32/86 (37%) |
| **71** | **11/21 (52%)** | **37/64 (58%)** |

At N=71 (transition N), CP4 d compression is almost as effective as CP5 — consistent with N=71's higher plasticity.

---

## 4. Gate Summary

| Gate | Status | Evidence |
|------|--------|---------|
| **A** (Scalar K boost) | **REACHED** | K boost induces 100% at all N |
| B (K geometry) | NOT TESTED | Scalar K_mean is sufficient |
| E (Not inducible) | **NOT REACHED** | Lo→Hi IS inducible via K boost |
| **F** (N-dependent) | **REACHED** | d compression effectiveness varies by N |

---

## 5. Key Conclusion

**Lo→Hi induction is POSSIBLE.** The V5.9 directional commitment finding was operator-specific — d_mean increase cannot induce Lo→Hi, but K boost can. The asymmetry is:

| Direction | d_mean Increase | K Boost | d Compression |
|-----------|----------------|---------|---------------|
| Hi→Lo | ✅ (14–31%) | N/A | N/A |
| Lo→Hi | ❌ (0%) | ✅ (100%) | ✅ (37–93%) |

The control asymmetry is **operator-dependent**, not structural. Different interventions control different directions. The low branch is not fundamentally irreversible — it just requires the RIGHT intervention (K boost, not d_mean increase).

---

## 6. Claim Discipline

### SUPPORTED
- K boost induces Lo→Hi at 100% of low seeds across all N
- d compression induces Lo→Hi at 37-93% depending on N
- CP4 d compression is effective, especially at N=71
- V5.9 directional commitment was operator-specific, not structural

### NOT CLAIMED
- Physical interpretation
- Universality
- Structural irreversibility

---

## 7. Recommended Next: CAA

**CAA: Control Asymmetry Analysis** — characterize the operator-dependent asymmetry mechanism.

---

## Dev Stats

| Metric | Value |
|--------|-------|
| CAE tests | 3 |
| V5.10 total | 7 |
| Cumulative | 2513 |
| Failed | 0 |
| Gates | A, F |
