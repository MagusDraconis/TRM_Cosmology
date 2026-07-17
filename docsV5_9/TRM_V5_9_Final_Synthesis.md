# TRM V5.9 Final Synthesis: Branch Commitment and Irreversibility

**Date:** 2026-07-17  
**Branch:** `feature/v5.9-branch-commitment-and-irreversibility`  
**Status:** COMPLETE  
**Suites:** BCP, BCE, BCA, BCI (4 suites)  
**Tests:** 13 V5.9 tests, 2506 cumulative, 0 failed  
**Followed by:** `feature/v5.10-branch-control-asymmetry-and-induction`

---

## 1. V5.9 Research Question

**When does final branch identity become effectively irreversible?**

V5.8 showed branches become predictable at epoch 5. V5.9 asked: **when do they become committed** — the point after which intervention cannot flip the outcome?

---

## 2. Final Answer

Branch identity is **directionally committed at epoch 5, but not symmetrically irreversible.** Low-branch identity is locked (cannot be induced high), but high-branch identity remains suppressible (can still be flipped low).

---

## 3. Suite-by-Suite Summary

### BCP — Protocol (6 tests)
Pre-registered commitment problem. Distinguished commitment (cannot flip) from predictability (can forecast). Intervention methodology: apply d_mean increase at epochs 1–4, track flips. Decision gates A–E pre-registered.

### BCE — Execution (4 tests)
**Gate C (Prediction before commitment).** No primary N (67–72) reaches soft commitment (<10% flip) at epochs 1–4. All primary N remain highly plastic. N=71 is most intervention-sensitive (flip ≥39%).

### BCA — Analysis (6 tests)
**Gates B, C, D.** 7/8 N show non-monotonic plasticity. Only N=66,80 soft-commit. N=71 classified as transition-plastic (highest mean flip, direction flips at epoch 4). Prediction (epoch 5) precedes commitment (>epoch 4) for all primary N.

### BCI — Irreversibility (3 tests)
**Gates C, F.** CP5 epoch 5 intervention reveals **directional commitment**: Hi→Lo remains possible (14–31%), but Lo→Hi is ZERO at all N. Low branch is irreversible at epoch 5; high branch is still suppressible. No N reaches strong commitment (<1%) or full irreversibility (0%).

---

## 4. Directional Commitment (Core Finding)

| N | CP5 Hi→Lo | CP5 Lo→Hi | Direction |
|---|-----------|-----------|-----------|
| 67 | 14% | **0%** | Hi can → Lo |
| 69 | 18% | **0%** | Hi can → Lo |
| 70 | 25% | **0%** | Hi can → Lo |
| 71 | 24% | **0%** | Hi can → Lo |
| 72 | 24% | **0%** | Hi can → Lo |
| 80 | 11% | **0%** | Hi can → Lo |

**Commitment is asymmetric.** d_mean increase can suppress high branch → low at all N, but CANNOT induce low branch → high anywhere. Low-branch identity is intervention-resistant at epoch 5.

---

## 5. N=71: Transition-Plastic

| Property | CP4 | CP5 |
|----------|-----|-----|
| Flip rate | 45% | 24% |
| Hi→Lo | 22 | 24 |
| Lo→Hi | 23 (paradoxical) | 0 (locked) |

N=71 is the most plastic N at epochs 1–4 and partially stabilizes at epoch 5. The paradoxical Lo→Hi flips at epoch 4 completely vanish — the direction locks. Consistent with V5.7's N=71 transition point.

---

## 6. Prediction vs Commitment

| Property | Finding |
|----------|---------|
| Predictable at epoch 5 | ✅ Bal ≥ 0.93 (V5.8) |
| Committed by epoch 4 | ❌ Flip ≥ 15% (V5.9) |
| Committed at epoch 5 | ⚠️ Directional only (Hi→Lo persists) |
| **Gap** | Prediction before full commitment |

**Branches become forecastable before they become irreversible.**

---

## 7. Supported Findings

- Prediction and commitment are distinct — forecastability does not imply irreversibility
- Branch identity remains plastic through epoch 4 for most N
- N=71 is transition-plastic (most intervention-sensitive)
- At CP5, commitment is directional: Hi→Lo possible, Lo→Hi locked
- No class-stable N reaches strong commitment before final state
- No N demonstrates full bidirectional irreversibility

---

## 8. Weakened / Disconfirmed

- Symmetric branch irreversibility
- Full final-state irreversibility
- Early commitment (before epoch 5)
- Monotonic commitment accumulation (7/8 N non-monotonic)
- Prediction as proxy for commitment

---

## 9. NOT CLAIMED

- Physical time, space, length, or c
- Physical constants or their derivation
- Relativity, quantum mechanics, cosmology
- Emergence or attractor decomposition
- Universal irreversibility or universal criticality

---

## 10. Final V5.9 Conclusion

V5.9 shows that RecoverFP branch identity is late-predictable but **not symmetrically irreversible.** Plasticity persists through epoch 4. At epoch 5, commitment becomes directional: high-branch states remain suppressible, while low-branch states resist induction under tested interventions.

V5.9 therefore supports **directional commitment** rather than full bidirectional irreversibility.

---

## 11. Recommended V5.10

**Branch:** `feature/v5.10-branch-control-asymmetry-and-induction`

**Central question:** Why can high branches be suppressed at CP5, but low branches cannot be induced high?

---

## 12. Development Statistics

| Suite | Tests |
|-------|-------|
| BCP | 6 |
| BCE | 4 |
| BCA | 6 |
| BCI | 3 |
| **V5.9 total** | **19** |
| **Cumulative** | **2506** |
| **Failed** | **0** |

## 13. Gate Summary

| Suite | Gates Reached |
|-------|---------------|
| BCE | C (Prediction before commitment) |
| BCA | B (Persistent plasticity), C (N=71 transition), D (Pred vs commit) |
| BCI | C (Directional commitment), F (No full irreversibility) |
