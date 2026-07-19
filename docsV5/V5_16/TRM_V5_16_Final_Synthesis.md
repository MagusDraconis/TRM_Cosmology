# TRM V5.16 Final Synthesis

**Branch:** feature/v5.16-explanatory-gap-and-feature-discovery
**Status:** COMPLETE | **Date:** 2026-07-18
**Base:** V5.15 COMPLETE (M3+ at ceiling, 2631 tests)

## 1. Branch Metadata

| Item | Value |
|------|-------|
| Branch | `feature/v5.16-explanatory-gap-and-feature-discovery` |
| Suites | EGP, EGE, EGA (EGS = this doc) |
| Commits | 621b014, df218ef, 355277a |
| V5.16 tests | 13 (EGP:3, EGE:5, EGA:5) |
| Cumulative | 2644 (all passed, 0 failed) |

## 2. V5.16 Research Question

**What measurable feature families could explain the remaining structured but currently uncontrollable variance after M3+?**

Answer: **Static pre-intervention geometry is largely exhausted. The explanatory gap is predominantly post-intervention response dynamics, specifically rebMagnitude.** Most new static features (F1-F4) are redundant with M3+. rebMagnitude (FP/TP ES=2.63, independent from M3+ with max|r|=0.12) powerfully explains why M3+ predictions succeed or fail — but only after intervention. No pre-intervention proxy exists.

## 3. Suite Summaries

### EGP — Protocol (3 tests)
Registered V5.16 feature-discovery framework, frozen M3+ baseline, shift from control to explanation.

### EGE — Feature Discovery Execution (5 tests, ~3 min)
7 new feature families extracted. F1/F2/F4 redundant with M3+ (r>0.7). F3/F5/F6/F7 non-redundant. **rebMagnitude** (F7) shows strongest residual signal (ES=1.73 FN/TN, 2.63 FP/TP). Shift identified: gap is intervention dynamics, not geometry.

### EGA — Response Dynamics Analysis (5 tests, ~4 min)
rebMagnitude validated: FP/TP ES=2.63 (2.15 holdout), max|r|=0.12 with M3+, simple threshold (reb>0.01 → 97% correct). No pre-intervention proxy (best |r|=0.16). **Explanatory and actionable post-hoc, not pre-actionable.**

## 4. Supported Findings

| # | Finding | Evidence |
|---|---------|----------|
| 1 | Remaining gap is NOT pre-intervention geometry | F1/F2/F4 redundant with M3+ |
| 2 | rebMagnitude is strongest explanatory feature | ES=2.63 TP/FP, 2.15 holdout |
| 3 | rebMagnitude independent of M3+ | max\|r\|=0.12 |
| 4 | rebMagnitude provides post-intervention explanation | Threshold 97% correct |
| 5 | No pre-intervention proxy exists | Best \|r\|=0.16 (lambda1) |
| 6 | Gap is intervention-response dynamics | TP=+0.46, FP=-0.16 rebMagnitude |
| 7 | N=72 strongest response signal | ES=2.87 |
| 8 | M3+ remains best control model | No pre-actionable improvement found |

## 5. EGA Key Result

| Metric | Value |
|--------|-------|
| rebMagnitude FP/TP ES | 2.63 (2.15 holdout) |
| Independence from M3+ | max\|r\|=0.12 |
| Simple threshold (reb>0.01) | 97% correct |
| Pre-intervention proxy | None (best \|r\|=0.16) |

**rebMagnitude explains outcomes but is only measurable after intervention.**

## 6. Final V5.16 Model Impact

**M3+ remains unchanged** as preferred control model. V5.16 does not produce M4. Instead, it identifies **why** M3+ reached a ceiling: the decisive variable (rebound dynamics) becomes visible only after intervention.

## 7. Weakened / Rejected

Matrix localization, node concentration, spectral shape — all redundant. Pre-intervention geometry sufficient for residual — **rejected**. Immediate control improvement from new features — **rejected**.

## 8. Not Claimed

rebMagnitude is causal, controllable, pre-actionable. Universal control. Complete explanation. Physical interpretation.

## 9. Final V5.16 Conclusion

V5.16 shows the remaining explanatory gap after M3+ is dominated by **intervention-response dynamics rather than unmeasured static pre-intervention geometry**. rebMagnitude strongly explains outcomes but emerges only after intervention. This explains **why** M3+ reached a practical control ceiling: the decisive variable is invisible before intervention. The path forward is adaptive control, not better static classification.

## 10. Recommended V5.17

**Branch:** `feature/v5.17-adaptive-response-and-rebound-control`

**Question:** Can post-intervention rebMagnitude be used in an adaptive second-stage control loop?

**Suites:** ARP → ARE → ARA → ARI → ARS

Shift from static classification + explanation → **adaptive response control**.
