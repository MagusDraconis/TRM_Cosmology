# TRM V5.54 RPC_01 — RawIQR Residual Predicate Closure Audit

**Suite ID:** RPC_01_RawIQRResidualPredicateClosureAudit
**Version:** 1.0
**Date:** 2026-07-21
**Branch:** `feature/v5.54-rawiqr-origin-and-profile-structure`
**Status:** COMPLETE

---

## 1. Research Question

**What profile-local descriptor co-varies with rawIQR residual inside a seed?**

RPD_01 showed that within-seed residual is N-driven. RPC_01 asks: among the available residual descriptors, which co-varies with the rawIQR residual, and which best predicts P1/P1b?

---

## 2. Residual Correlation Audit

Correlations between rawIQR residual and other descriptor residuals (900 profiles):

| Descriptor | r with rIQR | p-value |
|:-----------|------------:|--------:|
| rMean | **0.0000** | 1.0000 |
| rMedian | −0.0016 | 0.9609 |
| rStd | 0.0000 | 1.0000 |
| rQ10 | +0.1591 | <0.0001 |
| rQ90 | −0.1280 | 0.0001 |

**rIQR is orthogonal to rMean (r = 0.0000).** Residual spread and residual central tendency are independent dimensions within a seed. rQ10 and rQ90 show weak correlation — expected since IQR = Q75−Q25, and tail quantiles are partially correlated with spread.

---

## 3. Residual Dominance

| Residual | P1−P1b Delta | Ratio |
|:---------|-------------:|------:|
| **rIQR** | **0.001054** | **1.00×** |
| rMean | 0.000121 | 0.11× |
| \|rIQR−rMean\| composite | 0.000025 | 0.02× |

**rIQR is 8.7× stronger than rMean.** The composite does NOT improve over rIQR alone — unlike V5.53 where rawIQR+rawMean improved per-N stability, the residual composite degrades separation.

---

## 4. Matched Outcome

Same-seed, same-N matching is **impossible** under the current pipeline: each (seed, N) pair produces exactly one profile. Nearest equivalent was done in RLP_01 (same-seed, different-N).

---

## 5. Decision

### Model A: rawIQR residual remains dominant. No other residual descriptor exceeds it.

The P1/P1b discriminator at the profile-local level is rawIQR residual. It is orthogonal to rawMean residual (r=0.0000), 8.7× stronger in separation, and the composite degrades rather than improves performance. The V5.53 composite advantage applies at the pooled (cross-seed) level, not at the residual (within-seed) level.

---

## 6. Supported Findings

1. rIQR residual orthogonal to rMean residual (r=0.0000).
2. rIQR P1−P1b delta = 0.00105 (8.7× rMean delta).
3. Residual composite degrades separation (delta=0.00003).
4. rQ10/rQ90 weakly correlate with rIQR (|r|~0.14) — expected tail behavior.
5. Stop-Low safe.

---

*Generated 2026-07-21.*
