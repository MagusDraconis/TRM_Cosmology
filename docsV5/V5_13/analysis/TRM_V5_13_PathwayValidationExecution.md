# TRM V5.13 HVE: Pathway Validation Execution

**Branch:** feature/v5.13-high-basin-pathway-validation-and-scaling
**Status:** COMPLETE
**Date:** 2026-07-17

---

## Quick Summary

Out-of-sample validation of V5.12 two-pathway model on seeds 0-199 across N=67-80. **Pathway model partially transfers**: matched maintains advantage at N=71 (2.3% vs 2.0%) and N=72 (4.4% vs 4.0%), but degrades significantly from training rates. **N=75 emerges as strongest pathway signal** (18.2% matched vs 12.9% universal). N=80 shows no pathway advantage despite high overall rates. N=67 remains fully inaccessible.

---

## 1. N=67 — Pathway-Negative (Confirmed)

| Cohort | N | P1 | P2 | Matched | UnivS | UnivM | Mis |
|--------|---|-----|-----|---------|-------|-------|-----|
| Train (0-99) | 50 | 12 | 24 | **0.0%** | 0.0% | 0.0% | 0.0% |
| Holdout (100-199) | 50 | 6 | 32 | **0.0%** | 0.0% | 0.0% | 0.0% |

Zero strict persistence across all protocols and cohorts.

---

## 2. N=71 — Partial Transfer

| Cohort | N | P1 | P2 | Matched | UnivS | UnivM | Mis |
|--------|---|-----|-----|---------|-------|-------|-----|
| **Train** | 50 | 21 | 19 | **10.0%** | 6.0% | 2.0% | 0.0% |
| Holdout | 50 | 21 | 23 | **2.3%** | 2.0% | 0.0% | 0.0% |

Training reproduces V5.12. Holdout degrades to 2.3% — matched still edges universal (2.3% vs 2.0%).

---

## 3. N=72 — Partial Transfer

| Cohort | N | P1 | P2 | Matched | UnivS | UnivM | Mis |
|--------|---|-----|-----|---------|-------|-------|-----|
| Train | 50 | 23 | 16 | **7.7%** | 4.0% | 6.0% | 2.6% |
| Holdout | 50 | 28 | 17 | **4.4%** | 4.0% | 0.0% | 0.0% |

Small matched advantage on holdout (4.4% vs 4.0%).

---

## 4. Extended N (seeds 0-99)

| N | P1 | P2 | Matched | UnivS | UnivM | Mis |
|---|-----|-----|---------|-------|-------|-----|
| 70 | 26 | 15 | **0.0%** | 0.0% | 2.0% | 0.0% |
| 71 | 21 | 19 | **10.0%** | 6.0% | 2.0% | 0.0% |
| 72 | 23 | 16 | **7.7%** | 4.0% | 6.0% | 2.6% |
| **75** | 19 | 3 | **18.2%** | 12.9% | 9.7% | 9.1% |
| 80 | 10 | 0 | **40.0%** | 40.0% | 30.0% | 30.0% |

**N=75 has strongest pathway separation** (18.2% vs 12.9%). N=80 shows no pathway advantage (matched=universal). P2 collapses above N=72.

---

## 5. Updated N-Window

```
N=67 ─── 0% ─── INACCESSIBLE
N=70 ─── 0% ─── TRANSITION BELOW
N=71 ── 10% ── PATHWAY ACTIVE
N=72 ── 8% ─── PATHWAY ACTIVE
N=75 ── 18% ── STRONGEST (new!)
N=80 ── 40% ── NO PATHWAY ADVANTAGE
```

---

## 6. Decision Gates

| Gate | Status |
|------|--------|
| **A** — Out-of-sample reproduction | **PARTIAL** (directional, degraded) |
| **B** — Pathway transfer | **REACHED** (P1/P2 stable) |
| **C** — N-window | **REFINED** (N=71-75) |
| **D** — Model degradation | **REACHED** (10%→2.3%) |
| **E** — N=67 inaccessible | **REACHED** |
| **F** — Pathway collapse | **NOT REACHED** |

## 7. Recommended Next

**HVA** — scaling analysis at N=73-78 to map the transition boundary where N=75 has strongest pathway signal.

## Test Summary

- File: `TRM.Tests/V5_13/V5_13_PathwayValidationExecution_Tests.cs`
- Tests: 2 (HVE_01, HVE_02)
- Passed: 2
- Runtime: ~6min (seeds 0-199, N=67/71/72/70/75/80)
- Tagged: LongRunning
