# TRM V5.52 Final Synthesis — Raw-Frequency Ensemble Sampling Origin

**Version:** 1.0 | **Date:** 2026-07-20
**Branch:** `feature/v5.52-raw-frequency-ensemble-sampling-origin`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.52-raw-frequency-ensemble-sampling-origin` |
| Base | V5.51 COMPLETE |
| Suites | RSO, PSA, SCD, SACBR, P1C, P1S, TSS |
| V5.52 tests | 7 |
| Cumulative tests | 2896 passed, 0 failed |

---

## 2. Research Question

**Why do raw-frequency ensemble means already show K1>K3>K2? Is it sampling, selection, or hidden structure?**

Answer: **The ordering is NOT present in the full pre-selection population. It is CREATED by the SelectAndClassify retention pipeline.** P1 preferentially retains higher-mean profiles; K2 enters P1 more frequently; branch composition creates the direction flip; the resulting selected ensemble produces K1>K3>K2.

---

## 3. Suite Summaries

### RSO_01 — Raw Sampling Origin Audit
**Model C: Selection creates ordering.**
- Pre-selection (100 seeds/N): NO ordering
- Post-selection (21/22/16): YES ordering
- Equal-count null: ordering persists even with balanced counts

### PSA_01 — Profile Selection Mechanism Audit
**Model B: SAC is the ordering trigger. IsHi does not create ordering.**
- IsHi creates retention imbalance (K2=31% vs K1=68%) but NO ordering
- SAC creates the final ordering

### SCD_01 — SelectAndClassify Discriminator Audit
**Model A: SAC discriminates by raw-frequency mean. Direction flips for K2.**
- K1/K3: SAC retains LOWER mean profiles
- K2: SAC retains HIGHER mean profiles
- Direction flip is N-dependent

### SACBR_01 — Branch Resolution Audit
**Model B: Same dominant branch (P1). N composition creates the flip.**
- All P1/P1b = retained; all else = rejected
- K2 has highest P1 share (75%) vs K1 (55%) vs K3 (50%)

### P1C_01 — P1 Composition Audit
**Model C: Mean + spread drive P1 assignment. P1 selects higher-mean profiles.**
- P1 mean=0.9995 > P1b mean=0.9968
- K2 highest P1% explained by K2 profiles having higher raw-frequency means

### P1S_01 — P1 Composition Stability Audit
**Model A: Findings STABLE and synthesis-ready.**
- Branch composition jackknife-stable
- P1 vs P1b mean difference sign-stable
- Direction flip sign-stable

---

## 4. Final Decision Model

### Model B+C: Selection-Origin Ensemble Ordering

**The K1>K3>K2 ordering is NOT inherent — it is CREATED by the SelectAndClassify retention pipeline.**

The full origin chain:

```
All 100 seeds/N: NO ordering (K3 has largest IQR)
  ↓ IsHi: retention imbalance, still NO ordering
IsHi pass: NO ordering
  ↓ SelectAndClassify: P1 vs P1b classification
  ↓ P1 selects higher-mean profiles
  ↓ K2 enters P1 at 75% (highest rate)
SAC final: K1>K3>K2 ordering CREATED
  ↓ Sim amplifies
  ↓ DL disrupts
  ↓ Cupd restores
  ↓ w0→w1 strengthens
  ↓ w1→w2 kernel destroys into K1/K2/K3 classes
```

**This is a diagnostic selection-origin model. Not causal closure. Not a control policy.**

---

## 5. Supported Findings

1. No K1>K3>K2 ordering in the full pre-selection population.
2. Ordering appears only after SelectAndClassify.
3. IsHi creates retention imbalance but NOT the ordering.
4. SAC discriminates by raw-frequency mean.
5. K2 exhibits a direction flip: SAC retains higher means for K2, lower for K1/K3.
6. P1 preferentially retains higher-mean profiles.
7. K2 has highest P1 share (75% vs 55% K1 vs 50% K3).
8. Branch composition, not branch identity, explains the ordering.
9. Mean + spread jointly support P1 assignment.
10. All findings survive stability audit (jackknife, splits).
11. Stop-Low remains safe.
12. V6 NOT READY. Causal closure blocked.

---

## 6. Conditional Findings

- finite-N limits (12-22 profiles per N post-selection)
- selection-pipeline limits (IsHi + SAC specific)
- seed/profile sampling limits
- instrumentation limits (no pre-init state beyond K matrix)
- diagnostic only — no causal closure
- hidden SAC discriminator may remain
- no physical meaning of N or kernel class
- V6 NOT READY

---

## 7. Hypotheses

- Kernel-class assignment may originate from selective ensemble construction.
- P1 composition may seed subsequent ordering propagation through the pipeline.
- Branch composition may matter more than branch identity.
- Hidden SAC discriminator may remain uninstrumented.

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
| RSO | 1 | COMPLETE |
| PSA | 1 | COMPLETE |
| SCD | 1 | COMPLETE |
| SACBR | 1 | COMPLETE |
| P1C | 1 | COMPLETE |
| P1S | 1 | COMPLETE |
| TSS | — | THIS DOCUMENT |
| **Total** | **7** | **0 failed** |

Cumulative: 2896 tests, 0 failed.
