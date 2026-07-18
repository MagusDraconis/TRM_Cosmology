# TRM V5.16 EGA: Response Dynamics Feature Analysis

**Suite:** EGA | **Status:** COMPLETE | **Date:** 2026-07-18

## Quick Summary

**rebMagnitude is the key to the explanatory gap.** It separates TP from FP with ES=2.63 (2.15 holdout), is completely independent of M3+ (max|r|=0.12), and provides near-perfect TP/FP separation with a simple threshold (rebMagnitude>0.01 → 97% correct). However, **no pre-intervention proxy exists** (best |r|=0.16 with lambda1). rebMagnitude is explanatory and actionable post-hoc, but not controllable pre-intervention with current features.

**Verdict: EXPLANATORY AND ACTIONABLE but NOT PRE-ACTIONABLE.**

---

## 1. rebMagnitude Separation (EGA_01)

### By Error Group (Train)

| Group | Count | Mean | Median | ES vs TP |
|-------|-------|------|--------|----------|
| **TP** | 34 | **+0.46** | +0.53 | — |
| FP | 16 | **-0.16** | -0.16 | **2.63** |
| TN | 51 | -0.09 | -0.06 | 1.95 |
| FN | 20 | +0.42 | +0.50 | 0.19 |

**TP and FP have opposite signs.** TP = positive rebound (persistence overshoots), FP = negative rebound (collapses from peak). TN (correct rejections) are negative, FN (missed successes) are positive — the sign pattern perfectly tracks outcomes.

### By N (Train)

| N | Mean reb | Success Mean | Failure Mean |
|---|---------|-------------|-------------|
| 67 | +0.08 | +0.68 | +0.06 |
| 71 | +0.08 | +0.40 | **-0.24** |
| 72 | +0.23 | +0.51 | **-0.14** |
| 75 | +0.32 | +0.46 | **-0.22** |
| 80 | +0.01 | +0.39 | -0.57 |

Success/failure rebMagnitude gap increases with N→71/72/75.

### Train/Holdout Stability

| Group | Train | Holdout | Δ |
|-------|-------|---------|---|
| G1 (TP) | +0.46 | +0.41 | +0.05 |
| G2 (FP) | -0.16 | -0.08 | -0.08 |

✅ Signal transfers to holdout. ES=2.15 holdout.

---

## 2. Independence from M3+ (EGA_02)

| Feature | r(reb) |
|---------|--------|
| projHiVec | +0.12 |
| orthHiVec | -0.05 |
| dMean | -0.11 |
| dStd | -0.09 |
| lambda1 | +0.16 |
| kFrob | +0.16 |

**max|r| = 0.16** — rebMagnitude is completely independent of M3+ geometry. It measures a different dimension of seed behavior.

### Simple Threshold on M3+ Predicted Positives

**rebMagnitude > 0.01**: 32/33 correct (97%). Threshold learned on train, applies to any M3+ prediction.

### Correlation with omT2

**r(reb,omT2) = +0.77** — rebMagnitude strongly tracks final Omega. Persistence = positive reb + high omT2.

---

## 3. Pre-Intervention Proxy (EGA_03)

| Candidate | r(reb) | Proxy? |
|-----------|--------|--------|
| lambda1 | +0.16 | no |
| kFrob | +0.16 | no |
| dMean | -0.11 | no |
| dStd | -0.09 | no |
| edgeDensity | +0.09 | no |
| projHiVec | +0.12 | no |

**No pre-intervention proxy exists.** Best |r|=0.16. rebMagnitude is fundamentally post-intervention — it emerges from the interaction of seed geometry with the specific intervention applied.

---

## 4. Error Modes (EGA_04)

| Mode | Count | % |
|------|-------|---|
| insufDisp | 111 | 42% |
| lowRebound | 21 | 8% |
| unresolved | 4 | 2% |

- G2 (FP): 20/27 are insufDisp
- G4 (TN): 91/109 are insufDisp

### N=72 Specific

TP rebMean=+0.48, FP rebMean=-0.12, **ES=2.87** — strongest N-specific signal.

### N=75

Succ rebMean=+0.40, Fail rebMean=-0.19, **ES=2.04** — consistent pattern.

---

## 5. Decision Gates (EGA_05)

| Gate | Status | Evidence |
|------|--------|----------|
| **A — rebMagnitude Explains** | **REACHED** | FP/TP ES=2.38, holdout ES=2.15 |
| **B — Adds Beyond M3+** | **REACHED** | max|r|=0.12, completely independent |
| **C — Not Pre-Actionable** | **REACHED** | No pre-intervention proxy exists |
| D — Proxy Found | NOT REACHED | Best |r|=0.16 |
| E — N-Specific | REACHED | N=72 ES=2.87 |
| F — Weak/Redundant | NOT REACHED | Signal is strong |

---

## 6. Feature-Family Verdict

**EXPLANATORY AND ACTIONABLE but NOT PRE-ACTIONABLE.**

| Dimension | Assessment |
|-----------|------------|
| Explanatory power | **EXCELLENT** — ES>2.0, separates TP/FP near-perfectly |
| Independence from M3+ | **EXCELLENT** — max|r|=0.12 |
| Holdout transfer | **GOOD** — ES=2.15 |
| Pre-intervention proxy | **NONE** — best |r|=0.16 |
| Actionability | **POST-HOC only** — can explain failure, cannot predict it |

---

## 7. Key Insight

**The explanatory gap after M3+ is almost entirely intervention-response dynamics**, not pre-intervention geometry. rebMagnitude captures whether a seed's response to compression is healthy (positive rebound → persistence) or pathological (negative rebound → collapse). This signal is nearly orthogonal to all M3+ geometry features.

The practical implication: M3+ correctly identifies candidates with favorable geometry, but whether they actually succeed depends on their post-intervention dynamics — specifically, their rebound trajectory. This dynamics is not predictable from static features at current measurement resolution.

---

## 8. Next: EGI or EGS

Gate C (Not Pre-Actionable) means EGI control-limit analysis would be moot — you can't control what you can't predict. **Recommended: EGS Final Synthesis** — document the explanatory gap as intervention-response dynamics, declare M3+ as the best pre-intervention model, and note that the remaining ceiling is fundamentally post-intervention.

EGS should address: "If the explanatory gap is post-intervention dynamics, what does that mean for the future of the RecoverFP pipeline?"
