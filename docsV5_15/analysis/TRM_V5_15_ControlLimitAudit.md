# TRM V5.15 CVI: Control Limit Audit

**Suite:** CVI | **Status:** COMPLETE | **Date:** 2026-07-18

## Quick Summary

Three pre-registered M3++ refinement candidates tested against M3+ on holdout data. **No refinement improves holdout without unacceptable cost.** M3++A (N=75 selector) hurts recall (+41% FPR). M3++B (FN recovery) drops N=71 rate. Displacement repair has narrow sweet spot (+20% works but +10% destroys). **M3+ is at the practical control ceiling.** Gate F (Control Ceiling) REACHED.

---

## 1. M3+ Baseline and Refinements (CVI_01)

| Model | Holdout TP | FP | TN | FN | Prec | Rec | FPR | FNR |
|-------|--------|----|----|----|------|-----|-----|-----|
| **M3+** | 35 | 11 | 58 | 29 | **76%** | 55% | 16% | 45% |
| M3++A | 26 | 8 | 61 | 38 | 76% | **41%** | 12% | **59%** |
| M3++B | 41 | 18 | 51 | 23 | 69% | **64%** | 26% | 36% |

### Per-N Holdout Rates

| N | M3+ | M3++A | M3++B |
|---|-----|-------|-------|
| 71 | 62% | 62% | **55%** |
| 72 | 78% | 78% | **64%** |
| 75 | 75% | 75% | 79% |

**M3++B improves N=75 (+4pp) but harms N=71 (-7pp) and N=72 (-14pp).** Overfit at non-target N.

---

## 2. N=75 Selector Correction (CVI_02)

| Model | TP | FP | Prec | Rec |
|-------|----|----|------|-----|
| M3+ (bypass) holdout | 15 | 5 | 75% | 65% |
| M3++A (selector) holdout | 6 | 2 | 75% | **26%** |

**N=75 selector correction dramatically reduces recall (65%→26%)** while precision stays at 75%. The bypass rule was correct — applying projHiVec at N=75 rejects too many seeds with high persistence. M3's "selector-neutral" is validated.

**Gate B: NOT REACHED.**

---

## 3. Displacement Repair (CVI_03)

N=72 train insufDisp candidates (n=9):

| Compression | Strict | Rate | ImmHi |
|-------------|--------|------|-------|
| 50% (baseline) | 7 | 78% | 0% |
| 40% (+10%) | 1 | **11%** | 78% |
| 35% (+15%) | 2 | 22% | 22% |
| 30% (+20%) | 8 | **89%** | 0% |

**+20% works (+11pp above baseline), but +10%/+15% are destructive.** Narrow sweet spot — high risk of over-tuning. Not generalizable.

**Gate C: WEAK — works at one level but unstable.**

---

## 4. Control Ceiling Assessment (CVI_04)

| Refinement | Rec Δ | FPR Δ | Harm? | Viable? |
|-----------|-------|-------|-------|---------|
| M3++A (N=75 sel) | -14.1% | -4% | Rec loss | ❌ |
| M3++B (FN recov) | +9.4% | +10% | N=71 harm | ❌ |
| Displacement +20% | N/A | N/A | Narrow | ⚠️ |

---

## 5. Decision Gates

| Gate | Status | Evidence |
|------|--------|----------|
| A — FN Reducible | NOT REACHED | +9% recall costs +10% FPR, N=71 degrades |
| B — N=75 Validated | NOT REACHED | Selector drops recall 65%→26% |
| C — Displacement Repair | **WEAK** | +20% works (+11pp), +10% destroys |
| D — M3++ Established | NOT REACHED | Combined model harmful |
| E — Overfit Warning | **REACHED** | M3++B harms N=71 |
| **F — Control Ceiling** | **REACHED** | No refinement improves holdout materially |

---

## 6. Final Verdict

**M3+ is at the practical control ceiling.** The structured residual variance identified in CVA (FN with projHiVec ES=1.56, dMean ES=1.58) is descriptive but not actionable at current control granularity. Attempts to relax selection increase false positives; attempts to strengthen displacement are unstable. M3+ represents the best validated model under current features and control resolution.

---

## 7. Claim Discipline

No new selectors claimed. No thresholds retuned on holdout. No physical interpretation.

---

## 8. Next: CVS — V5.15 Final Synthesis

Declare M3+ as the practical control ceiling. Document remaining unexplained variance and its structure. Prepare V5.16 recommendation.
