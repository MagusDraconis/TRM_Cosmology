# TRM V5.46 Final Synthesis — Entry-State Distribution Shape and N-Window Origin

**Version:** 1.0 | **Date:** 2026-07-19
**Branch:** `feature/v5.46-entry-state-distribution-shape-and-n-window-origin`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.46-entry-state-distribution-shape-and-n-window-origin` |
| Base | V5.45 COMPLETE |
| Suites | EDP, EDE, EDA, EDI |
| V5.46 tests | 7 (3 EDP + 1 EDE + 1 EDA + 1 EDI) |
| Cumulative tests | 2871 passed, 0 failed |
| Commits | `3084eaa` (init), `390816f` (EDE), `2d4d2e9` (EDA), `0de6922` (EDI) |

---

## 2. Research Question

**Why do some N windows produce broad c3EntryOm bulk distributions while others**
**produce compressed distributions?**

Answer: **Entry distribution is inherited from T0 post-warmup spread.** N=75 is
uniquely broad at T0 (IQR=1.200 vs next highest 0.047). N=70/72 broaden temporarily
at T1 but collapse at T2 (retention 0.05-0.07). Model A — T0 inherited spread
dominates (ROBUST, survives leave-one-N-out).

---

## 3. Suite Summaries

### EDP — Protocol (3 tests)
Diagnostic trace only. Frozen: M3++, Stop-Low, c3OmgS. No selectors/corrections/V6.

### EDE — Execution (1 test, 51s)
Per-N T0/T1/T2 transition map. N=75 BROAD preserved, N=70/72 T1-broad/T2-collapse.
Initial: Model C.

### EDA — Analysis (1 test, 50s)
T0→Entry IQR=1.000. Entry IQR→rescue=0.926. Model A — T0 inherited spread.
T1→T2 collapse: N=72 retention=0.048, N=70=0.067.

### EDI — Audit (1 test, 52s)
**Model A ROBUST.** Without N=75: T0→T2=0.807. N=75 uniquely broad (T0 IQR=1.200).
T1→T2 collapse confirmed. Model survives audit. No downgrade.

---

## 4. Supported Findings

1. **T0 inherited spread dominates** c3EntryOm bulk distribution (T0→Entry IQR≥0.807).
2. **N=75 is uniquely broad at T0** (IQR=1.200 vs next 0.047 — 25×).
3. **N=70/72 broaden at T1 but collapse at T2** (retention 0.05-0.07).
4. **T1 broadening is not sufficient** for broad C3 entry — must survive to T2.
5. **Entry IQR strongly associates with rescue** (0.926, drops to 0.534 w/o N=75).
6. **Stop-Low safe. V6 NOT READY.**
7. **Causal closure partially improved, not closed.**

---

## 5. N-Specific Interpretation

| N | T0 IQR | T1 IQR | T2 IQR | T1→T2 | Class |
|--:|-------:|-------:|-------:|------:|:------|
| 67 | 0.012 | 0.012 | 0.022 | 1.92 | COMPRESSED |
| 70 | 0.037 | 0.341 | 0.023 | 0.07 | T1-broad → T2-collapse |
| 72 | 0.047 | 0.657 | 0.032 | 0.05 | T1-broad → T2-collapse |
| 75 | **1.200** | 0.829 | **0.947** | 1.14 | BROAD preserved |

---

## 6. Causal Closure

**Partially improved, not closed.** Origin localized to T0 but T0 spread origin remains unexplained.

---

## 7. Recommended V5.47

**Branch:** `feature/v5.47-post-warmup-t0-spread-origin-and-n-window-formation`
**Question:** Why does T0 post-warmup spread differ by N? **Planned:** TSP, TSE, TSA, TSI, TSS.

---

## 8. Suite Reference

| Suite | Tests | Status |
|:------|------:|:------:|
| EDP | 3 | COMPLETE |
| EDE | 1 | COMPLETE |
| EDA | 1 | COMPLETE |
| EDI | 1 | COMPLETE |
| EDS | — | THIS DOCUMENT |
| **Total** | **7** | **0 failed** |

---

*Generated 2026-07-19. V5.46 final synthesis.*
