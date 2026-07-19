# TRM V5.3 — M3c Omega Seed Stability Audit Protocol

**Suite:** V5_3_OmegaSeedStabilityAuditProtocol_Tests.cs
**Tag:** OSSA
**Date:** 2026-07-16
**Status:** PROTOCOL DEFINED — AWAITING EXECUTION
**Prerequisites:** M3b forensic audit (COMPLETE — Ω CV ~0.10 found)

---

## A. Purpose

Determine whether Omega seed-stability is robust across seed ranges. Both V5.2 live verification and M3b found Ω CV ~0.10 — 10× the historical expected ~0.01. M3c tests five seed blocks to distinguish outlier seed ranges from genuine overstatement.

---

## B. Seed Blocks

| Block | Seeds | Count | Purpose |
|:------|:------|:-----:|:--------|
| A | 100–109 | 10 | V5.2 ensemble (Ω CV=0.097) |
| B | 520–524 | 5 | M3 ensemble (Ω CV=0.109) |
| C | 200–209 | 10 | Independent control |
| D | 300–309 | 10 | Independent control |
| E | 400–409 | 10 | Independent control |

All at xi=1.80, full TRM RecoverFP pipeline.

---

## C. Metrics and Thresholds

| Metric | Formula | Classification |
|:-------|:--------|:---------------|
| Ω CV | std(Ω)/\|mean(Ω)\| (population) | ≤0.02: HIGHLY STABLE / ≤0.05: STABLE / >0.05: VARIABLE |
| MD CV | std(MD)/\|mean(MD)\| | Secondary control |

Reference: historical expected Ω CV ~0.01; V5.2 measured 0.097; M3b measured 0.097.

---

## D. Decision Gates

| Gate | Condition | Ω Seed-Stability | H9 Impact | Next |
|:-----|:----------|:----------------:|:----------|:-----|
| **A: Robust** | ≥3/5 blocks CV≤0.05 | Broadly supported | Unchanged | Flag outliers, M4 if MD resolved |
| **B: Partial** | Some CV≤0.05, some >0.05 | Block-dependent | Narrowed | Investigate block drivers |
| **C: Not Reproduced** | <3 blocks CV≤0.05 | Not robust | WEAKENED | Audit pipeline, no M4 |
| **D: Pipeline Mismatch** | V5.2 vs M3c pipeline differs | — | — | Pipeline audit first |

---

## E. Run Plan

| Stage | Blocks | Seeds | Est. time |
|:------|:-------|:-----:|:----------|
| 1 | A+B | 15 | ~18s |
| 2 | C+D+E | 30 | ~36s |
| **Total** | **All 5** | **45** | **~54s** |

---

## F. Forbidden Actions

No threshold/seed/pipeline changes after execution. No H9–H12 confirmation claims. No M4 before M3c resolution.

---

## G. Claim Discipline

| Statement | Classification |
|:----------|:--------------|
| Five seed blocks defined | SUPPORTED |
| V5.2 Ω CV = 0.097 (live) | SUPPORTED |
| M3b Ω CV = 0.097 (live) | SUPPORTED |
| Ω CV ≤ 0.05 across most blocks | HYPOTHESIS |
| H9/H10/H11/H12 confirmed | NOT CLAIMED |

---

*Protocol frozen 2026-07-16. Execution deferred.*
