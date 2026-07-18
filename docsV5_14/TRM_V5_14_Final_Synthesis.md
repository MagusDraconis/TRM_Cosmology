# TRM V5.14 Final Synthesis

**Branch:** feature/v5.14-residual-geometry-and-control-limits
**Status:** COMPLETE
**Date:** 2026-07-18
**Base:** V5.13 COMPLETE (2590 tests, 0 failed)

---

## 1. Branch Metadata

| Item | Value |
|------|-------|
| Branch | `feature/v5.14-residual-geometry-and-control-limits` |
| Suites | RGP, RGE, RGA, RGI (RGS = this document) |
| Commits | b1b35ab, 3c6dc76, b9465d9, ead598a |
| V5.14 tests | 25 (RGP:8, RGE:6, RGA:6, RGI:5) |
| Cumulative | 2615 (all passed, 0 failed) |

---

## 2. V5.14 Research Question

**Can the remaining unexplained High-basin persistence variance be reduced by additional validated geometry, or has the current model reached its practical explanatory and control limit?**

Answer: **Near the ceiling, but not fully.** One N-specific residual geometric degree of freedom (orthHiVec) provides a measurable intervention improvement at N=72 (+14.1%). Most candidate features (9/10) fail. The model is close to its explanatory limit but has not completely reached it.

---

## 3. Suite Summaries

### RGP — Residual Geometry Protocol (8 tests)
Defined the frozen M3 baseline, 6 residual feature families (F1-F6), 7 decision gates (A-G), train/holdout separation rules, and model constraints (transparent only, max depth 2).

### RGE — Residual Geometry Execution (6 tests, ~4.5 min)
Extracted 263 residual profiles across N=67-80 with 25+ features across all 6 families. M3 baseline reproduced. Top preliminary candidates: dStd (ES=0.49), lambda2 (0.38), dTailWidth (0.37).

### RGA — Residual Geometry Analysis (6 tests, ~4.5 min)
Tested 10 residual features against M3 on holdout (train on 0-99, test on 100-199):
- **orthHiVec**: +3.5% holdout (+14.1% at N=72) — only positive signal
- dStd, lambda2, spectralGap: all neutral
- dTailWidth: overfit (harms N=71)
- N=75 signals: orthHiVec +25% (4 seeds), top1% +10.7% (14 seeds) — sparse

### RGI — Residual Intervention Limit Audit (5 tests, ~4 min)
Validated orthHiVec at the intervention level:
- **M4 = M3 + orthHiVec > 0.0153: N=72 holdout 64%→78% (+14.1%)** — material intervention improvement
- Neutral at N=71 and N=75 — no harm
- Stronger displacement (+20%): marginal improvement (+8.3%)
- M3-rejected orthHiVec-favorable candidates: 33-75% recoverable
- N=75 orthHiVec < 0.0705: 4 seeds, 100% — real but too sparse

---

## 4. Supported Findings

| # | Finding | Evidence |
|---|---------|----------|
| 1 | M3 remains valid baseline | Reproduced across RGE/RGA/RGI |
| 2 | Most residual geometry candidates fail | 9/10 neutral, harmful, or overfit |
| 3 | dStd provides no practical control gain | Neutral across all N |
| 4 | lambda2 provides no practical control gain | Neutral across all N |
| 5 | spectralGap provides no practical control gain | Neutral across all N |
| 6 | dTailWidth is overfit | Improves train, harms N=71 holdout |
| 7 | **orthHiVec provides reproducible residual signal** | RGA +3.5%, RGI +14.1% at N=72 |
| 8 | orthHiVec improves intervention outcomes at N=72 | 64%→78% holdout |
| 9 | N=75 residual signals are real but sparse | 4 seeds, 100% |
| 10 | N=67 remains inaccessible | Confirmed |
| 11 | N=80 remains saturated | Confirmed |

---

## 5. RGI Key Result

**M4 = M3 + orthHiVec > 0.0153:**

| N | M3 Holdout | M4 Holdout | Lift |
|---|-----------|-----------|------|
| 71 | 62% | 62% | 0.0% |
| **72** | **64%** | **78%** | **+14.1%** |
| 75 | 75% | 75% | 0.0% |

No degradation at N=71 or N=75. M3-rejected orthHiVec-favorable candidates: 33-75% recoverable.

**Interpretation:** Residual geometry exists but is highly limited and N-specific. One degree of freedom at N=72.

---

## 6. Final V5.14 Model — M3+

### Specification

**Pathway:** P1/P1b Compression-Room only
**Selectors:**
- projHiVec > -0.3281 (all N=71/72)
- orthHiVec > 0.0153 (N=72 only)

### N-Conditioned Regime Logic

| N | Model | Selector | Expected Holdout |
|---|-------|----------|-----------------|
| 67 | Inaccessible | — | — |
| 71 | M3 | projHiVec | 62% |
| 72 | **M3+** | projHiVec + orthHiVec | **78%** |
| 75 | M3 | projHiVec (bypass) | 75% |
| 80 | Saturated | Universal | 92%+ |

---

## 7. Weakened Findings

| Claim | Status |
|-------|--------|
| Universal residual selector | **Rejected** — orthHiVec is N=72-specific |
| dStd residual utility | **Rejected** — neutral |
| lambda2 residual utility | **Rejected** — neutral |
| spectralGap residual utility | **Rejected** — neutral |
| dTailWidth residual utility | **Rejected** — overfit |
| Universal M4 control model | **Rejected** — N-specific |
| Broad residual geometry | **Rejected** — one weak DOF |

---

## 8. Not Claimed Boundaries

V5.14 does NOT claim:
- Universal High-basin control
- Complete hidden-variable discovery
- Physical interpretation of orthHiVec
- Attractor topology proof
- Universal scaling across all N
- Proven optimality of M3+
- P2 pathway validity

---

## 9. Final V5.14 Conclusion

V5.14 tested whether the V5.13 M3 model had reached its practical explanatory ceiling. **Most residual geometry candidates (9/10) failed validation.** However, **orthHiVec survived both holdout analysis and intervention testing**, producing a measurable N=72-specific improvement (+14.1%, from 64%→78%).

**The ceiling was NOT fully reached — one residual degree of freedom exists at N=72.** The resulting model is **M3+**: Compression-Room + projHiVec + orthHiVec at N=72. The remaining explainable variance appears small.

**V5.14 core answer:** The model is NEAR but NOT AT the control ceiling. Further improvement is possible but marginal and N-specific.

---

## 10. Recommended V5.15

**Branch:** `feature/v5.15-control-ceiling-and-unexplained-variance`

**Central question:** How much unexplained persistence variance remains after M3+, and is it structured or effectively random?

**Suites:** CVP (protocol) → CVE (execution) → CVA (analysis) → CVI (limit audit) → CVS (synthesis)

**Core questions:**
1. What percentage of outcomes remain unexplained by M3+?
2. Are remaining failures structured or random?
3. Does N=72 contain unique residual geometry?
4. Is orthHiVec the final useful selector?
5. Has the practical ceiling been reached?
6. What is the maximum realistic holdout performance?

---

## Claims Legend

| Symbol | Meaning |
|--------|---------|
| SUPPORTED | Verified by holdout intervention evidence |
| CONDITIONAL | N-conditioned or cohort-specific |
| REJECTED | Tested and failed validation |
| NOT CLAIMED | Explicitly excluded |
