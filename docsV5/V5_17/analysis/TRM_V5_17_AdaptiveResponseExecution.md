# TRM V5.17 ARE: Adaptive Response Execution

**Suite:** ARE | **Status:** COMPLETE | **Date:** 2026-07-18

## Quick Summary

Two-stage adaptive intervention tested on N=71/72 train (41 seeds). **rebMagnitude probe works** (ES=2.07, 0/9 low-reb persist, 28/32 high-reb persist). **Adaptive rescue does NOT materially improve over M3+** (+2.4%, 1 rescue). **Universal second correction is catastrophic** (-51%, 22 damaged). **Wrong-way control confirms** (15%). **Adaptive control ceiling reached — rebMagnitude is explanatory but not effectively actionable.**

---

## 1. Model Comparison

| Model | Seeds | Strict | Rescued | Damaged | Rate | ΔA0 |
|-------|-------|--------|---------|---------|------|-----|
| **A0 (static M3+)** | 41 | 28 | — | — | **68%** | — |
| A2 (reb rescue) | 41 | 29 | 1 | 0 | 71% | +2.4% |
| A4 (universal) | 41 | 7 | 1 | **22** | **17%** | **-51%** |
| A5 (wrong-way) | 41 | 6 | 0 | 22 | 15% | -54% |

### Per-N

| N | A0 | A2 | A4 | A5 |
|---|----|----|----|-----|
| 71 | 71% | 71% | 19% | 19% |
| 72 | 65% | 70% | 15% | 10% |

---

## 2. Probe Verification

| Group | rebMean | Persist |
|-------|---------|---------|
| TP (high reb) | +0.38 | **28/32 (88%)** |
| FP (low reb) | -0.07 | **0/9 (0%)** |

**ES=2.07** — rebMagnitude probe reproduces V5.16 signal. Perfect separation: no low-reb seed persists, 88% of high-reb seeds persist.

---

## 3. Why Adaptive Rescue Fails

The second correction (20% additional d compression) **over-compresses** and destroys healthy seeds. A4 (universal) damages 22/41 seeds. A2 (gated) avoids damage by only correcting seeds already failing, but:
- Only 1 seed is in the low-reb+failing state (rebMag<0.01 AND not persisting)
- That 1 seed gets rescued → marginal +2.4% lift

The core problem: **rebMagnitude correctly identifies failures, but the available correction (additional compression) is too destructive to fix them.** The low-reb seeds that fail are fundamentally resistant to compression-based intervention — they need a different recovery mechanism, not more of the same.

---

## 4. Decision Gates

| Gate | Status | Evidence |
|------|--------|----------|
| A — Adaptive Improves | NOT REACHED | +2.4%, not ≥5% |
| **B — Rescue Works** | **REACHED** | 1 rescue, 0 damage — gating works |
| C — Preserve Prevents Damage | REACHED | A2 vs A4: 0 vs 22 damaged |
| D — Universal Suffices | NOT REACHED | A4 catastrophic |
| E — Adaptive Harms | NOT REACHED | A2 does no harm |
| **F — No Improvement** | **REACHED** | Adaptive ceiling reached |

---

## 5. Conclusion

**rebMagnitude is a near-perfect post-intervention classifier (ES=2.07, 0 false positives). But adaptive rescue does not materially improve outcomes.** The available correction (additional compression) is too coarse to selectively fix failing seeds without destroying successes. The adaptive ceiling is reached — as is the static ceiling.

**V5.17 answer: rebMagnitude explains but does not enable effective adaptive control under tested corrections.**

---

## 6. Next: ARA → ARS

Document the adaptive ceiling. Recommend V5.18: alternative correction mechanisms beyond additional compression.
