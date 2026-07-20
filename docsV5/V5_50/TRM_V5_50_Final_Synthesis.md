# TRM V5.50 Final Synthesis — Kernel-Class Assignment and Boundary Origin

**Version:** 1.0 | **Date:** 2026-07-20
**Branch:** `feature/v5.50-kernel-class-assignment-and-boundary-origin`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.50-kernel-class-assignment-and-boundary-origin` |
| Base | V5.49 COMPLETE |
| Suites | KCA, KAS, TSS |
| V5.50 tests | 3 |
| Cumulative tests | 2884 passed, 0 failed |
| Commits | `34cd966` (KCA), `7b0c80b` (KAS), `TSS` (this doc) |

---

## 2. Research Question

**What determines kernel-class assignment? Why does N=72 become K1 compression, N=75 become K2 expansion, and N=70 become K3 stable?**

Answer: **Pre-transition km/lambda spread preorders the stable kernel classes.** A regression-to-mean signature is observed: profiles with wide pre-transition spread compress inward (K1), profiles with narrow spread expand outward (K2), and intermediate profiles remain stable (K3). Mean-state explanations fail. This is a diagnostic assignment model, not causal closure.

---

## 3. Suite Summaries

### KCA_01 — Kernel Class Assignment Origin Audit
**Model D: Pre-transition spread preorders kernel classes.**
- K1 (N=72): pre km IQR=0.154 (largest) → compresses (0.57×)
- K3 (N=70): pre km IQR=0.126 (intermediate) → stable (1.04×)
- K2 (N=75): pre km IQR=0.086 (smallest) → expands (1.50×)
- Regression-to-mean signature: wide→compress, narrow→expand
- Mean state nearly identical across classes (ratio~1.00)

### KAS_01 — Kernel Class Assignment Stability Audit
**Model A: Assignment STABLE and synthesis-ready.**
- Jackknife: all amp constraints stable (no single-profile flips)
- Pre-IQR and amp ordering stable at full-N
- km and lam IQR jointly support ordering; d IQR and Spearman do not
- Mean-state failure confirmed
- Amp ordering cross-domain stable (both w1→w2 and w2→T0)

---

## 4. Final Decision Model

### Model D+: Spread-Ordered Kernel-Class Assignment

**Kernel-class assignment is diagnostically associated with pre-transition spread ordering.**

| Class | N | Pre km IQR | Amp | Pattern |
|-------|---|------------|-----|---------|
| **K1 Compression** | 72 | 0.154 (largest) | 0.57× | Wide → compresses |
| **K3 Stable** | 70 | 0.126 (intermediate) | 1.04× | Medium → stable |
| **K2 Expansion** | 75 | 0.086 (smallest) | 1.50× | Narrow → expands |

**Supporting evidence:**
- Pre km IQR orders classes: K1 > K3 > K2 (jackknife-stable at full-N)
- Amp ratio orders classes: K1 < K3 < K2 (cross-domain stable)
- Lambda IQR independently supports the same ordering
- Mean-state explanations fail (all ratios ~1.00)
- No single profile controls the assignment

**This is a diagnostic/observational model. Not causal closure. Not a control policy.**

---

## 5. Supported Findings

1. Kernel classes (K1 compression, K2 expansion, K3 stable) remain stable.
2. Amplification ratio alone separates all three kernel classes (jackknife-robust).
3. Pre-transition km spread preorders classes: K1 > K3 > K2.
4. Lambda spread independently supports the same preordering.
5. Regression-to-mean signature observed: wide compresses, narrow expands.
6. Mean-state explanations fail (ratios ~1.00 across all mean-state descriptors).
7. Amp ordering (K1 < K3 < K2) is cross-domain stable (both w1→w2 and w2→T0).
8. No single profile controls the class assignment (jackknife verified).
9. Stop-Low remains safe: 35 stopped, 0 rescues.
10. V6 NOT READY. Causal closure remains blocked.

---

## 6. Conditional Findings

- finite-N limitation (12-20 profiles per N, 3 N values total)
- tested N-windows only (N=70, 72, 75)
- tested operator classes only (P1/P1b)
- pre-spread ordering is domain-specific (km domain for w1→w2, not omega domain for w2→T0)
- diagnostic only — no causal closure
- no physical meaning of N or kernel class
- hidden transition operator may remain
- V6 NOT READY

---

## 7. Hypotheses

- Kernel-class assignment may emerge from pre-transition spread ordering.
- Regression-to-mean may be a characteristic property of rank-inverting transition kernels.
- km/lambda spread may jointly encode the class-assignment signal.
- A hidden spread-generation operator may remain uninstrumented.

---

## 8. Not Claimed

- causal mechanism for kernel-class assignment
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
| KCA | 1 | COMPLETE |
| KAS | 1 | COMPLETE |
| TSS | — | THIS DOCUMENT |
| **Total** | **3** | **0 failed** |

Cumulative: 2884 tests, 0 failed.
