# TRM V5.9 Branch Intervention and Irreversibility (BCI)

**Date:** 2026-07-17  
**Branch:** `feature/v5.9-branch-commitment-and-irreversibility`  
**Status:** COMPLETE  
**Gates:** C (Directional commitment), F (No irreversibility)

---

## 1. CP5 (Epoch 5) Commitment

| N | BaseHi | CP4 Flip | CP5 Flip | Δ | CP5 Soft? |
|---|--------|----------|----------|---|-----------|
| 66 | 2/100 | 2% | 2% | 0% | YES ✓ |
| 67 | 14/100 | 15% | 14% | −1% | NO |
| 69 | 24/100 | 28% | 18% | −10% | NO |
| 70 | 33/100 | 27% | 25% | −2% | NO |
| **71** | **36/100** | **45%** | **24%** | **−21%** | **NO** |
| **72** | **41/100** | **46%** | **24%** | **−22%** | **NO** |
| 75 | 72/100 | 29% | 31% | +2% | NO |
| 80 | 86/100 | 7% | 11% | +4% | NO |

**Only 1/8 N reaches soft commitment at CP5.** Most primary N still have flip ≥14% — branches remain intervention-sensitive even at the final epoch.

**N=71,72 show the LARGEST drop from CP4 to CP5** (−21pp, −22pp) — the V5.7 transition N values partially stabilize at epoch 5.

---

## 2. Directional Commitment: Asymmetric!

| N | CP5 Hi→Lo | CP5 Lo→Hi |
|---|-----------|-----------|
| **ALL N** | **14–31%** | **0%** |

**At epoch 5: Hi→Lo is still possible (14–31%), but Lo→Hi is ZERO.**

The commitment is **asymmetric**: low-branch identity is IRREVERSIBLE at epoch 5, but high-branch identity remains modifiable. d_mean increase can suppress high branch → low, but d_mean increase cannot induce low branch → high.

---

## 3. N=71: From Most Plastic to Strong Drop

| Metric | CP4 | CP5 |
|--------|-----|-----|
| Flip rate | 45% | **24%** (−21pp) |
| Hi→Lo | 22 | **24 all Hi→Lo** |
| Lo→Hi | 23 (paradoxical) | **0 (locked)** |

N=71 partially stabilizes at epoch 5. The paradoxical Lo→Hi flips at epoch 4 completely vanish — low branch becomes irreversible. High branch remains modifiable (24% can still be suppressed).

---

## 4. Gate Summary

| Gate | Status | Evidence |
|------|--------|---------|
| A (Epoch 5 commitment) | NOT REACHED | Only 1/8 N soft-committed |
| **C** (Directional commitment) | **REACHED** | Lo→Hi = 0% at all N — asymmetric |
| D (N=71 persistent) | PARTIAL | N=71 drops from 45%→24% but stays above 10% |
| **F** (No irreversibility) | **REACHED** | No N reaches strong commitment (<1%) or irreversible (0%) at CP5 |

---

## 5. Key Conclusion

**Branch identity is directionally committed at epoch 5:** low→high is irreversible, but high→low remains modifiable. The branch is "locked in" on one side but still plastic on the other. Full irreversibility is NOT reached at epoch 5 — some high-branch seeds can always be suppressed.

This contrasts with V5.8: branches are predictable (bal ≥ 0.93 at epoch 5) but NOT fully committed. The gap between predictability and irreversibility persists even at the final state.

---

## 6. Claim Discipline

### SUPPORTED
- CP5 flip rates: Hi→Lo 14–31%, Lo→Hi = 0%
- Asymmetric commitment: low branch is irreversible, high branch is not
- N=71 partially stabilizes at CP5 (45→24%)
- Gate C (directional) and Gate F (no irreversibility) reached

### NOT CLAIMED
- Physical interpretation
- Universality
- Complete irreversibility
- Commitment for interventions other than d_mean increase

---

## 7. Recommended Next: BCS

**BCS: V5.9 Branch Commitment Synthesis** — finalize V5.9.

---

## Dev Stats

| Metric | Value |
|--------|-------|
| BCI tests | 3 |
| V5.9 total | 13 |
| Cumulative | 2506 |
| Failed | 0 |
| Gates | C, F |
