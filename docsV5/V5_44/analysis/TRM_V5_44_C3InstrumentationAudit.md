# TRM V5.44 C3 Instrumentation Audit

**Version:** 1.0 | **Date:** 2026-07-19 | **Suite:** CII | **Status:** COMPLETE

---

## 1. Summary

CII_01 audited C3 microstate robustness across N, cohorts, matched pairs, boundary
straddles, and rescue/failure groups using 73 C3X profiles.

**Key result: C3 microstate is slice-specific (Model C).** Cross-N correlation std=0.287
— the entry→exit bridge is NOT stable across N. 67% of C3 response remains autonomous.
c3ExitOm2 explains 10/10 matched pairs and 10/10 boundary straddles — the separator
is robust, but its predictability from entry state is N-dependent.

---

## 2. Cross-N Robustness

| N | n | entOm~exitOm2 | exitOm2 T4/Conv |
|--:|--:|-------------:|----------------:|
| 65 | 7 | **0.999** | 1.70 |
| 67 | 11 | **0.199** | 1.19 |
| 70 | 20 | 0.318 | 1.96 |
| 72 | 20 | 0.728 | 1.77 |
| 75 | 12 | 0.618 | 2.08 |

**Mean corr: 0.573, std: 0.287 — VARIABLE across N.**

N=65 is a strong outlier (corr=0.999, n=7) that inflates the mean. N=67 shows
essentially no correlation (0.199). The entry→exit bridge is N-dependent — it
works well at some N values and fails at others.

c3ExitOm2 as a separator (T4/Conv ratio >1.0 for all N) is more robust than the
entry→exit bridge. The microstate is real, but its predictability varies.

---

## 3. Matched-Pair and Boundary Straddle Audit

| Audit | Result |
|:------|:-------|
| Matched pairs explained by c3ExitOm2 | **10/10** |
| Boundary straddles explained by c3ExitOm2 | **10/10** |

**The C3 exit Omega separator is robust within matched pairs.** When comparing
near-identical profiles, c3ExitOm2 consistently explains which side of the
0.1 boundary each profile lands on.

---

## 4. Autonomous Response Quantification

| Source | Variance Explained |
|:-------|-------------------:|
| c3EntryOm | **33.4%** |
| c3TargetD | 17.5% |
| Autonomous (unexplained) | **66.6%** |

**MAJORITY autonomous — C3 response is largely independent of measured entry state.**
Two-thirds of c3ExitOm2 variance cannot be predicted from C3 entry diagnostics.

---

## 5. Microstate Robustness

**Model C — Slice-specific microstate (N-dependent).**

The C3 Omega-response microstate is real (c3ExitOm2 separates outcomes, 10/10
matched pairs explained) but its predictability from entry state is N-dependent
(corr std=0.287). The microstate exists at all N but the entry→exit bridge
strength varies substantially.

---

## 6. Gate Summary

| Gate | Status |
|:-----|:------:|
| A (c3ExitOm2 robust across N) | **NOT REACHED** (std=0.287) |
| B (robust across cohorts) | REACHED |
| C (boundary straddle robust) | REACHED (10/10) |
| D (entry→exit bridge stable) | **NOT REACHED** (std=0.287) |
| E (rescue/failure improved) | REACHED |
| F (autonomous response quantified) | REACHED (67%) |
| G (microstate classified) | REACHED (Model C) |
| H (causal closure improved) | **NOT REACHED** |
| I (Stop-Low preserved) | REACHED |
| J (V6 not ready) | REACHED |

---

## 7. Causal Closure Update

**PARTIALLY improved — microstate robustness limited.** The C3 microstate exists
and separates outcomes, but the entry→exit bridge is not stable enough for
strong causal claims. 67% autonomous response suggests the C3 correction dynamics
have substantial independence from measured pre-C3 state.

## 8. Supported Findings

1. c3ExitOm2 is a robust outcome separator (10/10 matched pairs, 10/10 straddles).
2. Entry→exit bridge is N-dependent (std=0.287) — Model C.
3. 67% of C3 response variance is autonomous.
4. Stop-Low safe. V6 NOT READY.

## 9. Recommendation

Proceed to **CIS (Final Synthesis)** for V5.44 closure. The audit provides a
balanced picture: the microstate is real but the causal bridge is N-dependent.

---

*Generated 2026-07-19. V5.44 CII audit document.*
