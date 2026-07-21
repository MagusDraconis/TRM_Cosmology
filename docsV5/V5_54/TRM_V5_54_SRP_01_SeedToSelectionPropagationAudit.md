# TRM V5.54 SRP_01 — Seed-to-Selection Propagation Audit

**Suite ID:** SRP_01_SeedToSelectionPropagationAudit
**Version:** 1.0
**Date:** 2026-07-21
**Branch:** `feature/v5.54-rawiqr-origin-and-profile-structure`
**Status:** COMPLETE

---

## 1. Research Question

**Does seed-level rawIQR propagate into IsHi, SAC, P1/P1b assignment and downstream ordering?**

RIO_01 and SRA_01 established that rawIQR is a seed-level trait. SRP_01 asks whether this trait propagates through the selection pipeline.

---

## 2. Key Finding

### Seed-to-SAC propagation is UNRESOLVED due to sparse per-seed retention.

Across 300 profiles (100 seeds × 3 N), only **7 profiles** are SAC-retained, from only **5 seeds**. At the per-seed level, SAC retention is too sparse to assess propagation.

| Stratum | Profiles | IsHi% | SAC% | P1 | P1b |
|:--------|---------:|------:|-----:|---:|----:|
| Low rawIQR | 99 | 48.5% | 4.0% | 2 | 2 |
| Mid rawIQR | 102 | 45.1% | 2.0% | 2 | 0 |
| High rawIQR | 99 | 43.4% | 1.0% | 1 | 0 |

**IsHi pass rates are similar across strata (~43-48%).** Seed rawIQR does not gate IsHi.

**SAC retention is uniformly low** (1-4%) across all strata.

**P1/P1b counts are too sparse for strata comparison.**

### Composite Propagation

Higher composite seeds show 100% P1 among their few retained profiles (2P1/0P1b) vs 60% for lower composite (3P1/2P1b), but n=2-5 makes this unreliable.

### Link Chain

| Link | Status |
|:-----|:------|
| Seed → rawIQR | SUPPORTED (RIO_01, SRA_01) |
| rawIQR → IsHi | WEAK — IsHi gates on Omega, not rawIQR |
| IsHi → SAC | SUPPORTED (prerequisite) |
| SAC → P1 | SUPPORTED (V5.53) |
| Seed → P1 | CONDITIONAL — r = 0.007, too few profiles |
| P1 → ordering | SUPPORTED (V5.52) |

---

## 3. Decision

### Model F: Seed-level rawIQR propagation unresolved due to sparse seed profiles.

Only 7 SAC-retained profiles across 300, from 5 seeds. The rawIQR→P1 discrimination (V5.53 SCP_01) operates at the pooled population level, not at the per-seed level. Individual seeds have too few retained profiles for seed-level propagation analysis.

---

## 4. Supported Findings

1. IsHi pass rates are similar across rawIQR strata (43-48%).
2. SAC retention is uniformly low (1-4%) across all strata.
3. Only 7 SAC-retained profiles from 5 seeds across 300 runs.
4. Seed→P1 correlation r = 0.007 (essentially zero).
5. Stop-Low safe.

---

## 5. Conditional Findings

- finite-N, finite-seeds
- Per-seed SAC retention insufficient for propagation analysis
- Pooled analysis (V5.53) remains valid at population level

---

## 6. Not Claimed

- Causal mechanism
- Deterministic rescue
- Physical interpretation
- V6 readiness

---

## 7. Claim Audit

**AUDIT PASSED.** 0 causal claims. 0 physical claims.

---

*Generated 2026-07-21.*
