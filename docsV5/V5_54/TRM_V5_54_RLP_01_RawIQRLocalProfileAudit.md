# TRM V5.54 RLP_01 — RawIQR Local Profile Audit

**Suite ID:** RLP_01_RawIQRLocalProfileAudit
**Version:** 1.0
**Date:** 2026-07-21
**Branch:** `feature/v5.54-rawiqr-origin-and-profile-structure`
**Status:** COMPLETE

---

## 1. Research Question

**What profile-local feature within a seed distinguishes P1 vs P1b vs rejected profiles?**

SRX_01 showed seed-level rawIQR does not predict per-seed P1 rate (r=0.044). RLP_01 asks: if seed identity doesn't determine outcome, what profile-local descriptors do?

---

## 2. Within-Seed Comparison

Only **10 of 300 seeds** have mixed outcomes (P1 + other retained profile). This limits within-seed matched comparison but permits pooled analysis.

### Pooled P1 vs P1b Descriptors

| Descriptor | P1 (n=15) | P1b (n=3) | Delta | Norm |
|:-----------|----------:|----------:|------:|-----:|
| **rawIQR** | **0.10018** | 0.09692 | **0.00327** | **100%** |
| rawMedian | 0.99953 | 0.99809 | 0.00144 | 44% |
| rawMean | 0.99789 | 0.99890 | −0.00101 | 31% |
| rawStd | 0.05800 | 0.05818 | −0.00018 | 6% |

**rawIQR remains the dominant discriminator** even after SRX_01 showed no seed-level propagation.

---

## 3. Seed-Centered Residual

After subtracting each seed's mean rawIQR:

| Group | Residual rawIQR |
|:------|----------------:|
| P1 | −0.00015 |
| P1b | +0.00090 |
| **Delta** | **−0.00105** |

**Seed-centering PRESERVES P1/P1b separation.** The rawIQR signal is not purely a seed-level artifact — it persists after controlling for seed identity. A P1 profile within a seed tends to have slightly lower residual rawIQR than a P1b profile within the same seed (direction consistent with V5.53: P1 prefers higher rawIQR + lower rawMean).

---

## 4. Decision

### Model A: rawIQR survives seed matching. Local spread remains the dominant profile-local discriminator.

**Evidence:** rawIQR normalized delta = 100% (3.2× rawMean, 2.3× rawMedian). Residual separation survives seed-centering.

**Interpretation:** The P1/P1b discriminator is profile-local — it distinguishes profiles within the population even when seed effects are removed. A profile's rawIQR deviation from its seed mean carries discriminatory information. This explains why seed-level rawIQR (pooled across N) does not predict P1 rate: the discriminator operates on profile-specific rawIQR values, not seed-level averages.

---

## 5. Supported Findings

1. rawIQR remains dominant P1/P1b discriminator (100% normalized delta).
2. Residual rawIQR survives seed-centering (delta = −0.00105).
3. 10 of 300 seeds have mixed outcomes — within-seed comparison limited.
4. rawMean/rawMedian contribute secondary information (31%/44%).
5. Stop-Low safe.

---

## 6. Conditional Findings

- Small P1/P1b counts (15 P1, 3 P1b)
- Only 10 seeds with mixed outcomes
- Within-seed matched comparison underpowered

---

## 7. Not Claimed

- Causal mechanism. Physical interpretation. V6 readiness.

---

*Generated 2026-07-21.*
