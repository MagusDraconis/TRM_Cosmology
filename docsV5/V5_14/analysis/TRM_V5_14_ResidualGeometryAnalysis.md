# TRM V5.14 RGA: Residual Geometry Analysis

**Suite:** RGA_ResidualGeometryAnalysis
**Status:** COMPLETE
**Date:** 2026-07-18

---

## Quick Summary

10 residual features tested against frozen M3 baseline on holdout data (train on 0-99, test on 100-199). **orthHiVec** (orthogonal distance from High-entry vector) is the only feature that improves holdout beyond M3: **+3.5% overall, +14.1% at N=72**. No other feature exceeds +3% holdout lift. dTailWidth shows overfit (improves train, harms N=71 holdout). N=75 specific: orthHiVec gives +25% on small subset (4 seeds). The residual signal is real but weak — M3 is near the control ceiling.

**Model: M3 + orthHiVec > 0.015. Marginal improvement. N-specific at N=72.**

---

## 1. M3 Baseline (Frozen)

| N | Cohort | Pathway S0 | M3 S1 | Verdict |
|---|--------|-----------|--------|---------|
| 71 | Train | 64% | 78% | SELECTOR_USEFUL |
| 71 | Hold | 50% | 62% | SELECTOR_USEFUL |
| 72 | Train | 60% | 67% | SELECTOR_USEFUL |
| 72 | Hold | 56% | 64% | SELECTOR_USEFUL |
| 75 | Train | 79% | 92% | SELECTOR_USEFUL |
| 75 | Hold | 75% | 75% | SELECTOR_NEUTRAL |

M3 baseline reproduced — matches V5.13 HVS findings.

---

## 2. Single-Feature Residual Validation (RGA_01)

Trained on reference 0-99 (M3-selected only), tested on holdout 100-199.

| Feature | Threshold | Tr Acc | Holdout S0 | S+ Rate | Sel Cnt | **Lift** | Verdict |
|---------|-----------|--------|-----------|---------|---------|----------|---------|
| orthHiVec | 0.0153 | 74% | 72% | 76% | 33 | **+3.5%** | MARGINAL |
| dTailWidth | 0.0390 | 70% | 72% | 71% | 34 | -1.6% | NEUTRAL |
| dStd | 0.1241 | 67% | 72% | 71% | 35 | -0.8% | NEUTRAL |
| lambda2 | 4.1053 | 67% | 72% | 71% | 35 | -0.8% | NEUTRAL |
| spectralGap | 42.4217 | 70% | 72% | 72% | 36 | 0.0% | NEUTRAL |
| kVelocity | -0.3790 | 67% | 72% | 72% | 36 | 0.0% | NEUTRAL |
| dVelocity | -0.6259 | 67% | 72% | 72% | 36 | 0.0% | NEUTRAL |
| top1% | 0.0125 | 67% | 72% | 71% | 34 | -1.6% | NEUTRAL |
| dMean | 0.5192 | 72% | 72% | 71% | 35 | -0.8% | NEUTRAL |
| kFrob | 63.2350 | 70% | 72% | 67% | 27 | **-5.6%** | HARMFUL |

**orthHiVec is the only feature with positive holdout lift.**

---

## 3. Per-N Holdout Lift (RGA_01)

| Feature | N=71 | N=72 | N=75 |
|---------|------|------|------|
| dStd | 0.0% | 0.0% | 0.0% |
| lambda2 | 0.0% | 0.0% | 0.0% |
| dTailWidth | **-12.5%** | 0.0% | 0.0% |
| orthHiVec | 0.0% | **+14.1%** | 0.0% |
| spectralGap | 0.0% | 0.0% | 0.0% |
| kVelocity | 0.0% | 0.0% | 0.0% |

**Key finding:** orthHiVec's lift is N=72-specific (+14.1%). At N=71 it's neutral. At N=75 it's neutral. dTailWidth actively harms N=71.

---

## 4. Cross-N Stability (RGA_02)

All features trained on aggregate reference 0-99, tested per-N holdout.

| Feature | N=71 Lift | N=72 Lift | N=75 Lift | Stable |
|---------|----------|----------|----------|--------|
| dStd | 0.0% | 0.0% | 0.0% | ✅ |
| lambda2 | 0.0% | 0.0% | 0.0% | ✅ |
| dTailWidth | -12.5% | 0.0% | 0.0% | ❌ N=71 |
| orthHiVec | 0.0% | **+14.1%** | 0.0% | N-specific |
| dVelocity | 0.0% | 0.0% | 0.0% | ✅ |

---

## 5. N=75 Residual Analysis (RGA_03)

Trained on N=75 reference (0-99), tested on N=75 holdout (100-199).

| Feature | Threshold | Holdout S0 | S+ Rate | Sel Cnt | **Lift** |
|---------|-----------|-----------|---------|---------|----------|
| orthHiVec | 0.0705 | 75% | **100%** | 4 | **+25.0%** |
| top1% | 0.0143 | 75% | 86% | 14 | +10.7% |
| dStd | 0.2644 | 75% | 82% | 17 | +7.4% |
| lambda2 | 10.0589 | 75% | 79% | 14 | +3.6% |
| dMean | 0.5107 | 75% | 75% | 20 | 0.0% |
| dVelocity | -0.5450 | 75% | 72% | 18 | -2.8% |

orthHiVec at N=75 achieves 100% on 4 seeds — strong but ultra-sparse. top1% gives +10.7% on 14 seeds. These are N=75-specific signals only (trained on N=75 reference, not cross-N).

---

## 6. Overfit Audit (RGA_04)

| Feature | Train Lift | Holdout Lift | Gap | N-Harm | Verdict |
|---------|-----------|-------------|-----|---------|---------|
| orthHiVec | +4.6% | +3.5% | +1.1% | — | **OK** |
| dStd | -0.7% | -0.8% | +0.1% | — | OK |
| lambda2 | -0.7% | -0.8% | +0.1% | — | OK |
| dTailWidth | +1.0% | -1.6% | +2.6% | N71 | **OVERFIT** |
| dVelocity | -0.7% | 0.0% | -0.7% | — | OK |
| kVelocity | -0.7% | 0.0% | -0.7% | — | OK |
| spectralGap | +1.0% | 0.0% | +1.0% | — | OK |

**dTailWidth flagged: improves train but harms N=71 holdout.** orthHiVec has small but consistent train→holdout transfer.

---

## 7. Model Comparison (RGA_05)

| N | Cohort | M3 | M3+orthHiVec | Sel Cnt | Better |
|---|--------|-----|-------------|---------|--------|
| 71 | Train | 78% | 78% | 9 | M3+ ✓ |
| 71 | Hold | 62% | 62% | 8 | M3+ ✓ |
| 72 | Train | 67% | 67% | 12 | M3+ ✓ |
| 72 | Hold | 64% | **78%** | 9 | **M3+** |
| 75 | Train | 92% | 91% | 11 | M3 |
| 75 | Hold | 75% | 75% | 8 | M3+ ✓ |

**M3+orthHiVec improves N=72 holdout from 64%→78% (+14.1%).** Maintains parity at N=71 and N=75.

---

## 8. Decision Gates

| Gate | Status | Evidence |
|------|--------|----------|
| **A — Residual Feature Improves** | **REACHED** | orthHiVec +3.5% holdout (+14.1% at N=72) |
| B — dStd Validated | NOT REACHED | dStd neutral across all N |
| C — Spectral Validated | NOT REACHED | lambda2/spectralGap neutral |
| D — N=75 Specific | **PARTIALLY REACHED** | orthHiVec +25% (4 seeds), top1% +10.7% (14 seeds) — N=75-specific training |
| **E — Overfit Warning** | **REACHED** | dTailWidth harms N=71 |
| F — Control Ceiling | NOT REACHED | orthHiVec provides marginal improvement |
| **G — Residual Model** | **MARGINALLY REACHED** | M3 + orthHiVec improves N=72 but is N-specific |

---

## 9. Control Limit Assessment

orthHiVec provides **+3.5%** aggregate and **+14.1%** N=72-specific holdout lift. This is a real but weak signal:

- Effect is N=72-specific (not universal)
- No other feature exceeds +3% holdout lift
- dTailWidth overfits (harms N=71)
- Most features are strictly neutral (0% lift)
- N=75 signal exists but is sparse (orthHiVec: 4 seeds, 100%)

**Interpretation:** The model has ONE additional geometric degree of freedom (orthHiVec) that modestly improves the N=72 regime. No feature provides universal improvement. M3 is near the control ceiling — further refinement is N-specific and sparsely populated.

**Current model: M3 + orthHiVec > 0.015 (N=72-specific). M3 unchanged at N=71/75.**

---

## 10. Claim Discipline

| Claim | Status |
|-------|--------|
| No physical interpretation | ✅ |
| orthHiVec improvement requires holdout evidence | ✅ N=72 holdout |
| Thresholds trained on 0-99 only | ✅ |
| No hidden variable claims beyond evidence | ✅ orthHiVec is marginal |
| orthHiVec not claimed as universal selector | ✅ N=72-specific |
| dTailWidth rejected (overfit) | ✅ |

---

## 11. Recommended Next: RGI

**RGI_ResidualInterventionLimitAudit** — Determine whether the orthHiVec + M3 model has reached practical control limits, investigate whether sparse N=75 signals are usable, and produce final V5.14 synthesis.
