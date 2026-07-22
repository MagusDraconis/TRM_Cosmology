# TRM V5.58 Final Synthesis — d0 Generation Audit

**Version:** V5.58 | **Date:** 2026-07-21 | **Status:** COMPLETE

---

## Executive Determination

V5.58 asked: is d0 fundamental or another shadow? Answer: **km (coupling matrix mean) is more fundamental than d0.** SKL_01 showed km retains 0.73σ after d0 removal while d0 retains only 0.06σ — d0 is derived from km. KMG_01 confirmed km survives all predecessor removal (1.67σ after DL). The structural kernel is km; d0 and lambda are linear transforms. Bottom of SAC chain reached.

---

## Completed Suites

| Suite | Finding |
|:------|:--------|
| D0G_01 | d0 is fundamental among d-variables (1.75σ survives d2) |
| SKL_01 | **km > d0**: km→λ r=0.9998 (single kernel). km 0.73σ, d0 0.06σ |
| KMG_01 | **km is FUNDAMENTAL**: 1.67σ after DL removal, no predecessor beats it |

---

## Variable Hierarchy

| Rank | Variable | Effect (σ) | Status |
|:----:|:---------|-----------:|:-------|
| 1 | **km** | **1.71** | Fundamental |
| 2 | lambda | 1.71 | = km (r=0.9998) |
| 3 | d0 | 1.74 | Derived from km |
| 4 | rawIQR | 0.11 | Shadow signal |

---

## Supported Findings

1. km (coupling matrix mean) is the fundamental structural variable.
2. d0 and lambda are linear transforms of km.
3. km survives all predecessor removal.
4. Stop-Low safe. V6 NOT READY.

---

*Cumulative: 2931 tests, 0 failed.*
