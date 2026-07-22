# TRM V6 — Theory Document

**Version:** V6.1 | **Date:** 2026-07-22 | **Status:** DOCUMENTATION

---

## 1. Executive Summary

V5.60–V6.0 established that the SAC (Self-Adaptive Coupling) dynamics admit a 2D invariant
manifold with an asymptotically flat Euclidean metric. This replaces the original V4/V5
internal geometry based on c_eff = Ω·MD, which was falsified.

The V6 geometry is defined by:
- Two invariants: I₁, I₂ (cross-seed validated: CV=0.0025, 0.0121)
- A metric: ds² = g₂₂·dI₂² → dI₂² at N → ∞
- A time coordinate: arc length s(t) (strictly monotonic)
- A cyclic driver: Omega (non-monotonic, period-2)

---

## 2. The Problem: Why V4/V5 Geometry Failed

### 2.1 V4 Internal Geometry

V4 proposed c_eff = Omega × MeanDist as an invariant "internal propagation speed."
Additionally, Omega and MeanDist were assumed to be orthogonal proxies for "time" and "space."

### 2.2 Falsifications (V5.60 EMG_01, EMG_02)

| Hypothesis | Evidence | Verdict |
|:-----------|:---------|:-------|
| c_eff invariant | CV(seed)=0.80, CV(N)=1.08 | ❌ FALSIFIED |
| Ω ⟂ MD | r=0.81 overall, r=0.95 at N=80 | ❌ FALSIFIED |
| SAC → fixed point | Period-2 limit cycle | ❌ FALSIFIED |

### 2.3 Root Cause

The exponential Cupd map K = K₀·exp(-d/ξ) does not admit c_eff as an invariant.
Instead, it admits I₁ = 0.70·km + 0.30·d_mean, derived from the linearization
K + (K₀/ξ)·d ≈ K₀.

---

## 3. The Discovery: V6 Geometry

### 3.1 SAC is a Limit Cycle (KEM_01–KEM_04, LCM_01)

- Period = 2 epochs (peaks at 1,3,5,7,9; troughs at 2,4,6,8)
- Half-life = 52–66 epochs (λ = 0.0105–0.0132)
- Mechanism: RP→DL scrambles d-matrix, Cupd maps alternating d → alternating K
- |λ_max| ≈ 0.97 → stable two-cycle attractor

### 3.2 Two Invariants Discovered (LCM_02–LCM_03)

| Invariant | Definition | CV (cross-seed) | N-dependence |
|:----------|:-----------|:---------------:|:-------------|
| I₁ | 0.70·km + 0.30·d_mean | 0.0025 | N-constant |
| I₂ | 0.90·km + 0.10·Omega | 0.0121 | 1/N scaling |

**I₁ linearization origin:**
```
Cupd: K = K₀·exp(-d/ξ)
For small d: K ≈ K₀·(1 - d/ξ)
→ km + (K₀/ξ)·d_mean ≈ K₀
→ 0.70·km + 0.30·d_mean ≈ constant
```

### 3.3 Metric g₂₂ → 1.0 (V5.61_g₂₂, V5.62)

| N | g₂₂ mean | g₂₂ median | CV |
|:-:|:--------:|:----------:|:--:|
| 60 | 2.73 | — | 2.73 |
| 67 | 1.03 | 1.01 | 0.06 |
| 80 | 2.08 | 1.06 | 1.72 |
| 90 | **1.01** | 1.01 | **0.017** |
| 100 | **1.00** | 1.00 | **0.003** |

g₂₂ → 1.0 for N ≥ 67 (median-based). Convergence is faster than power-law.

---

## 4. Mathematical Foundation

### 4.1 Cupd Linearization

The exponential coupling update provides the invariant:
```
K = K₀·exp(-d/ξ)           [Cupd definition]
log(K/K₀) = -d/ξ           [log form]
K ≈ K₀·(1 - d/ξ)           [linearization for small d]
K + (K₀/ξ)·d ≈ K₀          [rearranged — conserved]
km + (K₀/ξ)·d_mean ≈ K₀    [mean-field approximation]
```

### 4.2 CLT Derivation of g₂₂ → 1

```
g₂₂ = 1 + (dI₁/dI₂)²       [definition]

dI₁ ∝ std(km)/√N           [CLT: variance of mean ~ 1/N]
dI₂ ∝ std(Omega) ≈ const   [Omega is self-averaging at large N]

→ dI₁/dI₂ ~ N^{-1/2} or faster
→ g₂₂ - 1 ~ N^{-1} or faster
→ g₂₂ → 1 as N → ∞          [Euclidean limit]
```

The observed convergence is FASTER than N^{-1}, suggesting exponential or threshold-like
behavior near N ≈ 65.

### 4.3 Full Metric

```
ds² = g₁₁·dI₁² + 2·g₁₂·dI₁·dI₂ + g₂₂·dI₂²

g₁₁ = 0    (I₁ conserved: dI₁ ≈ 0, CV = 0.0025)
g₁₂ = 0    (I₁ ⟂ I₂ approximately: r ≈ 0)
g₂₂ = g(N) → 1 as N → ∞

Effective metric:
ds² = g₂₂(N)·dI₂² → dI₂²  (N → ∞)
```

---

## 5. Validation Evidence

### 5.1 Test Summary

| Phase | Tests | Result |
|:------|:-----:|:------:|
| KEM (kernel emergence) | 4 | SAC = limit cycle |
| KSP (phase slips) | 1 | Slips = 0 |
| EMG (emergence) | 2 | c_eff, Ω/MD falsified |
| LCM (limit cycle) | 4 | Invariants, geometry |
| V6_Validation | 3 | Cross-seed (10 seeds) |
| V5.61_g₂₂ | 1 | g₂₂ dynamics |
| V5.62 | 1 | Euclidean limit |
| V6.0 Implementation | 10 | All pass |
| **Total** | **26** | **26/26 passed** |

### 5.2 Cross-Seed Validation

All V6 properties validated across 10 independent seeds:
- I₁: CV = 0.0025 (universal)
- I₂: CV = 0.0121 (universal)
- ε: CV = 0.011 (consistent)
- s(t): 10/10 monotonic
- Ω: 0/10 monotonic (confirms it's a driver, not time)

---

## 6. Comparison to General Relativity (Analogy)

| Property | V6 Geometry | General Relativity |
|:---------|:------------|:-------------------|
| Signatures | Euclidean (+,+) | Lorentzian (-,+,+,+) |
| Dimensions | 1+1D | 3+1D |
| Invariants | I₁, I₂ | Stress-energy tensor |
| Metric | ds² = g₂₂·dI₂² | ds² = g_μν·dx^μ·dx^ν |
| Flat limit | g₂₂ → 1 at N→∞ | η_μν in vacuum |
| Path dependence | g₂₂ varies with seed | g_μν varies with mass-energy |
| Time | Arc length s(t) | Proper time τ |

**Caveat:** This is a STRUCTURAL ANALOGY, not a physical claim. V6 is a mathematical
description of SAC dynamics, not a physical spacetime theory.

---

## 7. Open Questions and Future Work

| # | Question | Status |
|:-:|:---------|:-------|
| Q1 | Can g₂₂ → 1 be proven analytically? | V5.62: empirical only |
| Q2 | What is the exact N threshold for Euclidean convergence? | ~N=65 suggested |
| Q3 | Does g₂₂ → 1 hold for all seeds at large N? | Needs ≥100 seeds at N=90+ |
| Q4 | Can I₂ be tightened? | CV=0.0121, could be <0.01 |
| Q5 | Is the metric Euclidean for N=200+? | Needs large-scale simulation |
| Q6 | Can Ω/MeanDist correlation be explained? | r=0.81 origin unknown |

---

*Generated 2026-07-22. No physical claims are made.*
