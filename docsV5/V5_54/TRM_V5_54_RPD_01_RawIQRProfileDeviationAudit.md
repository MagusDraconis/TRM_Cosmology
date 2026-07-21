# TRM V5.54 RPD_01 — RawIQR Profile Deviation Audit

**Suite ID:** RPD_01_RawIQRProfileDeviationAudit
**Version:** 1.0
**Date:** 2026-07-21
**Branch:** `feature/v5.54-rawiqr-origin-and-profile-structure`
**Status:** COMPLETE

---

## 1. Research Question

**What creates profile-local rawIQR deviations within a fixed seed?**

RLP_01 showed residual rawIQR (after seed-centering) survives as the dominant P1/P1b discriminator. RPD_01 asks: where do these within-seed deviations come from?

---

## 2. Residual Construction

For each seed, compute seed-mean rawIQR across N. Profile residual = rawIQR − seed mean.

| Metric | Value |
|:-------|------:|
| Residual mean | 0.000000 |
| Residual std | 0.002515 |
| Between-seed std | 0.010880 |
| **Ratio bet/win** | **4.2×** |

**Between-seed variation is 4.2× larger than within-seed residual.** Seed identity dominates; profile-level deviations are small relative to seed differences.

---

## 3. Residual Ranking

| Predictor | \|r\| with residual |
|:----------|-------------------:|
| **N (sample size)** | **0.1005** |
| rawMean residual | 0.0000 |
| rawStd residual | 0.0025 |

**N is the primary driver.** For a fixed seed, the same Random(seed) sequence produces different IQR depending on how many draws are used (N=70, 72, or 75).

---

## 4. P1 Residual Separation

| Group | Residual mean |
|:------|--------------:|
| P1 | −0.000153 |
| P1b | +0.000901 |
| **Delta** | **−0.001054** |

**Residual separates P1/P1b: YES.** Within-seed rawIQR deviation carries discriminatory information.

---

## 5. Topology

Topology varies **between seeds**, not within seeds. Within-seed residual is N-driven — topology structurally cannot explain it. Association: none expected.

---

## 6. Decision

### Model A: rawIQR residual is N-driven (sample-size variation within a seed).

Within-seed deviation comes from using different N values with the same Random(seed) sequence. More draws = slightly different IQR. The residual magnitude (std=0.0025) is small relative to between-seed variation (std=0.0109, 4.2× larger), but carries enough signal to separate P1 from P1b.

---

## 7. Supported Findings

1. Between-seed rawIQR std = 0.0109; within-seed residual std = 0.0025 (4.2× ratio).
2. N explains residual (|r| = 0.101) — sample-size variation.
3. Residual separates P1/P1b (delta = −0.00105).
4. Topology cannot explain within-seed residual.

---

*Generated 2026-07-21.*
