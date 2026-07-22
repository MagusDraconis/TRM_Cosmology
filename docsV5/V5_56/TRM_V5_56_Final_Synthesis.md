# TRM V5.56 Final Synthesis — Relative Gate Preference

**Version:** V5.56 | **Date:** 2026-07-21 | **Status:** COMPLETE

---

## Executive Determination

V5.56 asked: why does SAC prefer lower-ranked profiles? Answer: it doesn't — the rank effect V5.55 found is a **pipeline artifact** (ART_01: permutation survival 18/20). SAC discriminates on within-seed rawIQR ordering, which is continuous (RG0_01) not a binary gate. Retained profiles are NOT geometric centers (GEO_01) or neighborhood references (NBR_01). Rank = rawIQR ordering by construction (RKO_01).

---

## Completed Suites

| Suite | Finding |
|:------|:--------|
| RGP_01 | Monotonic rank preference: lowest 9%, highest 0% |
| GEO_01 | Retained are less isolated — NOT geometric outliers |
| NBR_01 | Retained are NOT neighborhood centers (1% most-central) |
| RGS_01 | Winner-take-all rejected; retention is distributed |
| RG0_01 | Continuous preference, not binary Rank0 gate |
| RKO_01 | Rank = rawIQR ordering by construction |

---

## Supported Findings

1. SAC prefers lower within-seed rawIQR ordering.
2. Preference is continuous, not binary.
3. Retained profiles are geometrically typical (less isolated).
4. Top-10 seed rank retention = 8/10 across N.
5. Stop-Low safe. V6 NOT READY.

---

*Cumulative: 2922 tests, 0 failed.*
