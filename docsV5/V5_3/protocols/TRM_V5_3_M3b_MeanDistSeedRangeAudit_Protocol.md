# TRM V5.3 — M3b MeanDist Seed Range Audit Protocol

**Suite:** V5_3_MeanDistSeedRangeAuditProtocol_Tests.cs
**Tag:** MSRA
**Date:** 2026-07-16
**Status:** PROTOCOL DEFINED — AWAITING EXECUTION
**Prerequisites:** M3 (EXECUTED — Gate C, 6× CV discrepancy found)

---

## A. Purpose

Resolve the 6× MeanDist seed-CV discrepancy between V5.2 (~0.30, seeds 100–109) and M3 (~0.046, seeds 520–524). Determine whether MeanDist seed-variability is robust across seed ranges or block-specific.

---

## B. Seed Blocks

| Block | Name | Seeds | Count | Purpose |
|:------|:-----|:------|:-----:|:--------|
| A | V5.2 original | 100–109 | 10 | V5.1/V5.2 reference block |
| B | M3 current | 520–524 | 5 | M1/M2/M3 anomaly block |
| C | Mid-range | 200–209 | 10 | Independent control |
| D | Upper-mid | 300–309 | 10 | Independent control |
| E | Upper | 400–409 | 10 | Independent control |

All blocks at xi=1.80 (primary regime), full TRM RecoverFP pipeline.

---

## C. Metrics Per Block

| Metric | Formula | Purpose |
|:-------|:--------|:--------|
| CV_raw_MD | std(MD)/mean(MD) | Compatibility with V5.2 reference |
| CV_norm_MD | std(MD/D₉₀)/mean(MD/D₉₀) | Primary normalized metric |
| CV_mednorm_MD | std(MD/D_median)/mean(MD/D_median) | Robustness check |
| CV_Omega | std(Ω)/mean(Ω) | Control — should be ~0.01 |

Classification: seed-variable if CV_norm_MD > 0.15.

---

## D. Decision Gates

| Gate | Condition | Interpretation | H10 | Next |
|:-----|:----------|:---------------|:----|:-----|
| **A: Robust** | ≥3/5 blocks seed-variable + Block A seed-variable | MD variability is robust; M3 block was outlier | Testable again | M4 |
| **B: Block-Specific** | Block A seed-variable but <3 total | V5.2 block is an outlier; MD variability is block-specific | WEAKENED | Investigate block 100–109 |
| **C: No Variability** | No block seed-variable (incl. Block A) | V5.2 classification does not reproduce under M3 protocol | WEAKENED | Audit V5.2 pipeline |
| **D: Norm-Dependent** | D₉₀ and D_median disagree for ≥2 blocks | Classification depends on normalization | UNEVALUABLE | Normalization audit |
| **E: Ω Control Fail** | Ω CV > 0.05 in any block | Anomalous frequency draws | — | Flag block |

---

## E. Run Plan

| Stage | Blocks | Seeds | Sm calls | Est. time |
|:------|:-------|:-----:|:--------:|:----------|
| 1 (immediate) | A + B | 15 | 90 | ~18s |
| 2 (if needed) | C + D + E | 30 | 180 | ~36s |
| **Total** | **All 5** | **45** | **270** | **~54s** |

---

## F. Forbidden Actions

1. Do NOT add/remove blocks post-execution.
2. Do NOT change normalization or thresholds.
3. Do NOT exclude outlier seeds.
4. Do NOT claim H10/H11/H12 confirmed.
5. Do NOT proceed to M4 before M3b resolution.

---

## G. Claim Discipline

| Statement | Classification |
|:----------|:--------------|
| Five seed blocks defined | SUPPORTED |
| Metrics, thresholds, gates frozen | SUPPORTED |
| Results depend on seed block selection | CONDITIONAL |
| MD seed-variability is robust across blocks | HYPOTHESIS |
| H10/H11/H12 confirmed | NOT CLAIMED |

---

*Protocol frozen 2026-07-16. Execution deferred to V5_3_MeanDistSeedRangeAuditExecution_Tests.cs.*
