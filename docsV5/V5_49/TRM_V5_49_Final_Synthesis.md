# TRM V5.49 Final Synthesis — Spread Generation and Distribution Origin

**Version:** 1.0 | **Date:** 2026-07-20
**Branch:** `feature/v5.49-spread-generation-and-distribution-origin`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.49-spread-generation-and-distribution-origin` |
| Base | V5.48 COMPLETE |
| Suites | DSG, DGT, RTK, KBD, TSS |
| V5.49 tests | 5 |
| Cumulative tests | 2882 passed, 0 failed |
| Commits | `9587af0` (DSG), `bb201e5` (DGT), `6070465` (RTK), `ad56a5f` (KBD), `TSS` (this doc) |

---

## 2. Research Question

**Why do km/lambda spread distributions differ before the handoff? Where does the spread come from?**

Answer: **Spread differences are generated during the w1→w2 warmup transition, not inherited from w0.** N=75 starts NARROWER than N=72 at w0. The spread hierarchy emerges because N=72 collapses (0.57×) while N=75 grows (1.50×) during w1→w2. Rank inversion is a generic transition-kernel feature across all tested transitions. Three stable kernel classes (K1 compression, K2 expansion, K3 stable) are defined by amplification ratio and remain consistent across stages.

---

## 3. Suite Summaries

### DSG_01 — Spread Generation Audit
**Model C: Spread generated in w1→w2 transition.**
- IQR ratio 75/72 FLIPS: w0=0.42× → w1=0.56× → w2=1.48×
- N=75 starts NARROWER than N=72 at w0 (km IQR 0.024 vs 0.057)
- N=72 w1→w2: collapses 0.154→0.087 (0.57×)
- N=75 w1→w2: grows 0.086→0.129 (1.50×)

### DGT_01 — w1→w2 Growth/Collapse Transition Audit
**Model C: w1→w2 is rank-inverting. N=72 compresses inward, N=75 expands outward.**
- All N show negative Spearman w1→w2 (-0.54 to -0.66)
- N=72: 14 inward vs 6 outward → INWARD COMPRESSION
- N=75: 7 outward vs 5 inward → OUTWARD EXPANSION
- delta_d ~ delta_km: N=72=-0.991, N=75=-0.994 (near-perfect)

### RTK_01 — Rank Inversion Transition Kernel Audit
**Model A+C: Rank inversion is a GENERIC transition-kernel feature.**
- 6/6 transitions rank-inverting (3 N × 2 transitions)
- Kernel class preserved across stages for every N:
  - N=72: K1 compression (both transitions)
  - N=75: K2 expansion (both transitions)
  - N=70: K3 stable (both transitions)
- d/K diagnostics mirror across domains (signs flip by variable type)

### KBD_01 — Kernel Boundary Discriminator Audit
**Model D: Amp ratio + spread combine to separate K1/K2/K3.**
- Amp ratio alone cleanly separates all 3 classes (jackknife-robust, no overlap)
- Pre-transition km/lam IQR further strengthens separation
- Inward/outward balance diagnostically associated
- Delta mean sign does NOT cleanly separate

---

## 4. Final Decision Model

### Model A+D: Stable Rank-Inverting Transition Kernel

**N-window formation proceeds through rank-inverting transition kernels.**

1. **Rank inversion is generic** — observed in 6/6 tested N-transition pairs across w1→w2 and w2→T0.
2. **Three stable kernel classes** defined by amplification ratio:
   - **K1 Compression** (N=72): amp < 1 — profiles compress toward median
   - **K2 Expansion** (N=75): amp > 1 — profiles expand outward from median
   - **K3 Stable** (N=70): amp ≈ 1 — spread preserved
3. **Kernel class is an N-window property** — preserved across both transitions for every N.
4. **Spread generation occurs at w1→w2** — shape/spread, not mean state, determines outcome.
5. **Amplification ratio** is the primary jackknife-robust kernel-class discriminator.

**Diagnostic/observational only. Not causal closure. Not a control policy.**

---

## 5. Supported Findings

1. Spread differences are generated during w1→w2, not inherited from w0.
2. N=75 starts narrower than N=72 at w0 (km IQR 0.024 vs 0.057, ratio=0.42×).
3. IQR ratio 75/72 flips from 0.42× (w0) to 1.48× (w2) during warmup.
4. N=72 collapses during w1→w2 (0.57×); N=75 grows (1.50×).
5. Rank inversion occurs in 6/6 tested N-transition pairs (generic kernel feature).
6. Three stable kernel classes: K1 compression, K2 expansion, K3 stable.
7. Kernel class is an N-window property — preserved across both transitions.
8. Distribution shape/spread dominates mean state for N-window classification.
9. Amplification ratio alone cleanly separates K1/K2/K3 (jackknife-robust, no overlap).
10. d/K diagnostics near-perfectly track transition deltas (|r| > 0.92 across all transitions).
11. Stop-Low remains safe: 35 stopped, 0 rescues.
12. V6 NOT READY. Causal closure remains blocked.

---

## 6. Conditional Findings

- finite-N limitation (12-20 profiles per N)
- tested N-windows only (N=70, 72, 75)
- tested operator classes only (P1/P1b)
- 2 transitions tested (w1→w2, w2→T0)
- 3 warmup epochs available
- diagnostic only — no causal closure
- no physical meaning of N or kernel class
- hidden transition operator may remain
- V6 NOT READY

---

## 7. Hypotheses

- N-window formation may proceed through stable rank-inverting transition kernels.
- Amplification ratio may be the primary kernel-class diagnostic descriptor.
- Spread generation may precede and determine handoff formation outcome.
- Kernel class (compression/expansion/stable) may be an intrinsic N-window property.
- A hidden spread-generation operator may remain uninstrumented.

---

## 8. Not Claimed

- causal mechanism for kernel class assignment
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
| Physical interpretation of N | NOT CLAIMED |
| V6 readiness | NOT CLAIMED |
| Deterministic rescue | NOT CLAIMED |
| Threshold retuning | NOT CLAIMED |
| New variables | NOT CLAIMED |
| M3++ modification | NOT CLAIMED |
| Stop-Low modification | NOT CLAIMED |
| Physical constants or GR | NOT CLAIMED |

**AUDIT PASSED.** All forbidden claims absent.

---

## 10. Suite Reference

| Suite | Tests | Status |
|:------|------:|:------:|
| DSG | 1 | COMPLETE |
| DGT | 1 | COMPLETE |
| RTK | 1 | COMPLETE |
| KBD | 1 | COMPLETE |
| TSS | — | THIS DOCUMENT |
| **Total** | **5** | **0 failed** |

Cumulative: 2882 tests, 0 failed.
