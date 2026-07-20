# TRM V5.53 Final Synthesis — SelectAndClassify Predicate Origin

**Version:** 1.0 | **Date:** 2026-07-20
**Branch:** `feature/v5.53-selectandclassify-predicate-origin`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.53-selectandclassify-predicate-origin` |
| Base | V5.52 COMPLETE |
| Suites | SCP, RIG, RIS, RIC, RCS, TSS |
| V5.53 tests | 6 |
| Cumulative tests | 2901 passed, 0 failed |

---

## 2. Research Question

**What predicate drives SelectAndClassify P1 vs P1b assignment, creating the K1>K3>K2 ordering?**

Answer: **P1/P1b assignment is primarily rawIQR-driven (spread), with rawMean providing independent complementary stabilization.** rawIQR is the dominant normalized discriminator; rawMean adds per-N stability. Together they form a rank-based composite that yields consistent P1>P1b direction across all tested N.

---

## 3. Suite Summaries

### SCP_01 — SelectAndClassify Predicate Audit
**rawIQR is the strongest P1/P1b discriminator (10× normalized dominance over rawMean).**

### RIG_01 — RawIQR Genesis Audit
**P1 selects pre-existing high-rawIQR profiles from the IsHi-pass pool.** rawIQR differences exist before SAC.

### RIS_01 — RawIQR Selection Stability Audit
**Global rawIQR dominance confirmed.** Per-N stability is profile-sensitive (jackknife 1/3 N).

### RIC_01 — RawIQR Complement Audit
**rawMean adds independent complementary information.** Residual mean separation (0.00392) exceeds either descriptor alone.

### RCS_01 — RawIQR+Mean Composite Stability Audit
**Rank-based composite improves per-N stability (2/3 N jackknife-stable vs 1/3 for rawIQR alone).** Consistent P1>P1b direction across all N.

---

## 4. Final Decision Model

### Model B+: Spread-Primary, Mean-Complement Predicate

**SelectAndClassify P1/P1b assignment is best described by a rawIQR + rawMean rank composite.**

| Component | Role | Strength |
|-----------|------|----------|
| **rawIQR** | Primary spread discriminator | 10× normalized dominance |
| **rawMean** | Independent complementary stabilizer | Improves per-N jackknife stability |
| **Composite** | Combined diagnostic descriptor | Consistent P1>P1b across all N |

**This refines V5.52:** SelectAndClassify creates ordering through a spread-dominant profile predicate rather than pure mean selection. RawIQR is the dominant normalized signal; rawMean stabilizes per-N ambiguity.

**Diagnostic/observational only. Not causal closure. Not a control policy.**

---

## 5. Supported Findings

1. rawIQR is the strongest P1/P1b discriminator (normalized dominance ~10× over rawMean).
2. P1 selects higher rawIQR profiles in the pooled IsHi-pass population.
3. rawIQR alone is not uniformly stable per-N (jackknife 1/3 N).
4. rawMean adds independent complementary information (residual > either alone).
5. Rank-based rawIQR + rawMean composite improves per-N stability (2/3 N jackknife-stable).
6. Composite yields consistent P1>P1b direction across all 3 tested N.
7. Stop-Low remains safe.
8. Causal closure remains blocked.
9. V6 NOT READY.

---

## 6. Conditional Findings

- finite-N limits (10-11 P1, 3-10 P1b per N)
- small per-N profile counts limit statistical power
- per-N rawIQR instability under jackknife
- seed/profile sampling limits
- selection-pipeline limits (IsHi + SAC specific)
- instrumentation limits
- diagnostic only — no causal closure
- hidden SAC/P1 discriminator may remain
- no physical meaning of N
- V6 NOT READY

---

## 7. Hypotheses

- rawIQR may encode the dominant SAC/P1 spread discriminator.
- rawMean may stabilize rawIQR ambiguity across N.
- P1/P1b assignment may depend on a composite profile-shape signal.
- A hidden SAC predicate may still remain.

---

## 8. Not Claimed

- causal mechanism for SAC discrimination
- deterministic rescue
- physical N-boundary or physical interpretation of N
- universal adaptive control
- V6 readiness
- modified M3++
- modified Stop-Low policy
- retuned c3OmegaShift threshold
- new model variables or correction classes
- physical theory interpretation
- physical length, c, GR, spacetime, or cosmology derivation

---

## 9. Claim Audit

| Forbidden Claim | Status |
|:----------------|:------|
| Causality | NOT CLAIMED |
| Physical interpretation | NOT CLAIMED |
| V6 readiness | NOT CLAIMED |
| Deterministic rescue | NOT CLAIMED |
| Threshold retuning | NOT CLAIMED |
| New variables | NOT CLAIMED |
| M3++ modification | NOT CLAIMED |
| Stop-Low modification | NOT CLAIMED |

**AUDIT PASSED.**

---

## 10. Suite Reference

| Suite | Tests | Status |
|:------|------:|:------:|
| SCP | 1 | COMPLETE |
| RIG | 1 | COMPLETE |
| RIS | 1 | COMPLETE |
| RIC | 1 | COMPLETE |
| RCS | 1 | COMPLETE |
| TSS | — | THIS DOCUMENT |
| **Total** | **6** | **0 failed** |

Cumulative: 2901 tests, 0 failed.
