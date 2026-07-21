# TRM V5.54 SRX_01 — Sparse Retention Expansion Audit

**Suite ID:** SRX_01_SparseRetentionExpansionAudit
**Version:** 1.0
**Date:** 2026-07-21
**Branch:** `feature/v5.54-rawiqr-origin-and-profile-structure`
**Status:** COMPLETE

---

## 1. Research Question

**Does expanding the seed sample resolve the sparse-retention problem and reveal seed-level rawIQR propagation into P1 assignment?**

SRP_01 found only 7 SAC-retained profiles from 100 seeds, preventing seed-level analysis. SRX_01 expands to 300 seeds.

---

## 2. Expansion Design

| Parameter | SRP_01 | SRX_01 |
|:----------|-------:|-------:|
| Seeds | 0–99 | 0–299 |
| Profiles | 300 | 900 |
| Pipeline | M3++ frozen | M3++ frozen |
| IsHi/SAC | unchanged | unchanged |

---

## 3. Key Findings

### Retention

| Metric | SRP_01 | SRX_01 |
|:-------|-------:|-------:|
| Total profiles | 300 | 900 |
| IsHi pass | ~45% | 46.3% |
| SAC retained | 7 | **18** |
| P1 count | ~5 | **15** |
| Seeds with P1 | 5 | 13 |

**Classification: R3 — improved but still underpowered** (need 20+ for robust stratification).

### Seed Stratification

| Stratum | IsHi% | SAC% | P1 | P1b |
|:--------|------:|-----:|---:|----:|
| Low rawIQR | 48.7% | 2.3% | 5 | 2 |
| Mid rawIQR | 45.3% | 2.3% | 6 | 1 |
| High rawIQR | 45.0% | 1.3% | 4 | 0 |

IsHi rates nearly identical across strata — rawIQR does not gate IsHi.

### Seed-to-P1 Correlation

| Pair | Pearson r |
|:-----|----------:|
| seed rawIQR → P1 rate | **0.044** |
| seed rawIQR → SAC rate | 0.021 |
| seed rawMean → P1 rate | −0.046 |
| composite → P1 rate | 0.007 |

**All |r| < 0.05 — no seed-level propagation signal.**

### Pooled vs Seed-Level

| Level | Result |
|:------|:-------|
| Pooled (V5.53) | P1 rawIQR=0.100, P1b=0.097, δ=0.003 — discriminator works |
| Seed-level | r=0.044 — no propagation |

**P2: pooled strong, seed-level weak.**

---

## 4. Decision

### Model B/D: Propagation remains pooled-only. No seed-level propagation detected.

Even with 3× expansion (300 seeds, 900 profiles, 18 SAC-retained), seed-level rawIQR shows essentially zero correlation with per-seed P1 rate (r=0.044). The V5.53 pooled discriminator (rawIQR→P1) works at the population level but does not propagate to individual seed-level prediction.

**Interpretation:** A seed's rawIQR value (determined by its weight draw) does not predict how many of its N-specific profiles will be classified as P1. The P1/P1b assignment operates on profile-local features within the pooled distribution, not on seed-level traits.

---

## 5. Supported Findings

1. 3× expansion (100→300 seeds) increases SAC-retained from 7 to 18.
2. IsHi pass rates are rawIQR-independent (45–49% across strata).
3. Seed→P1 r = 0.044 — no seed-level propagation (even with 300 seeds).
4. All seed→outcome correlations |r| < 0.05.
5. Pooled discriminator (V5.53) works; seed-level does not.
6. P1 rawIQR (0.100) > P1b rawIQR (0.097) — pooled direction consistent.
7. Stop-Low safe.

---

## 6. Not Claimed

- Causal mechanism. Physical interpretation. V6 readiness.

---

*Generated 2026-07-21.*
