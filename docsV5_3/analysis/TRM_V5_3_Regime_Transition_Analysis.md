# TRM V5.3 — Regime Transition Analysis

**Status:** ANALYSIS
**Date:** 2026-07-16
**Scope:** V4.1→V5.3 regime transition
**Basis:** M3d verification + N1/M1/M2/M3 results

---

## 1. Current Supported State

| Finding | Evidence | Confidence |
|:--------|:---------|:----------:|
| Ω is HIGHLY STABLE at V4.1 regime (CV=0.013) | M3d: 20 seeds, N=60, ξ=1.75, K0=1.2, s=0.10 | HIGH |
| Ω is VARIABLE at V5.3 regime (CV=0.077) | M3d: 20 seeds, N=100, ξ=1.80, K0=1.15, s=0.08 | HIGH |
| MD is VARIABLE at V4.1 regime (CV=0.330) | M3d | HIGH |
| MD is STABLE at V5.3 regime (CV=0.068) | M3d + M3b + M3c | HIGH |
| Ω and MD stability CROSS OVER between regimes | M3d | HIGH |
| Ω xi-sensitivity is RecoverFP-specific | M1+M2 | MODERATE |
| The transition is NOT a seed-block artifact | Same seeds (0–19) used in both M3d regimes | HIGH |

---

## 2. Regime Comparison

| Parameter | V4.1 | V5.3 | Δ | Δ% |
|:----------|:----:|:----:|:--|:--:|
| N | 60 | 100 | +40 | +67% |
| ξ | 1.75 | 1.80 | +0.05 | +2.9% |
| K₀ | 1.20 | 1.15 | −0.05 | −4.2% |
| s | 0.10 | 0.08 | −0.02 | −20% |
| K₀·ξ (coupling product) | 2.10 | 2.07 | −0.03 | −1.4% |

| Metric | V4.1 | V5.3 | Ratio |
|:-------|:----:|:----:|:-----:|
| Ω mean | 1.09 | 8.78 | 8.0× |
| Ω std | 0.014 | 0.67 | 48× |
| Ω CV | 0.013 | 0.077 | 6.0× |
| MD CV | 0.330 | 0.068 | 0.21× |

---

## 3. Candidate Stability Drivers — Ranked

| Rank | Param | Δ% | Ω CV Effect | MD CV Effect | Rationale |
|:----:|:------|:--:|:-----------|:------------|:----------|
| 1 | N | +67% | HIGH | MEDIUM | Largest change; changes graph sparsity and RecoverFP convergence |
| 2 | s | −20% | MEDIUM | HIGH | Controls frequency diversity → cluster membership variability → MD CV |
| 3 | K₀ | −4.2% | LOW | LOW | Small change; could interact with N |
| 4 | ξ | +2.9% | LOW | LOW | Negligible; K₀·ξ nearly constant |
| 5–6 | St, REps | — | NEGLIGIBLE | NEGLIGIBLE | Technical parameters |

**Hypothesized decomposition:**
- Ω CV increase: ~70% N, ~20% s, ~10% K₀
- MD CV decrease: ~60% s, ~25% N, ~15% K₀

These are working hypotheses, not claims.

---

## 4. Transition Matrix

| Change (V4.1→V5.3) | Ω CV Prediction | MD CV Prediction | Mechanism |
|:--------------------|:--------------:|:---------------:|:----------|
| N: 60→100 | INCREASE (strong) | MODEST | N changes graph → different RecoverFP attractor → Ω amplification |
| s: 0.10→0.08 | MODEST | DECREASE (strong) | Narrower spread → less cluster diversity → lower MD CV |
| K₀: 1.20→1.15 | SMALL INCREASE | SMALL | Weaker coupling closer to threshold |
| ξ: 1.75→1.80 | NEGLIGIBLE | NEGLIGIBLE | 2.9% change |

---

## 5. Boundary Hypotheses

**BH1 — Single-Driver (N-dominant):** Ω transition driven by N alone; MD is secondary.

**BH2 — Two-Driver:** Ω CV driven by N; MD CV independently driven by s. First test of parameter-class independence (H11).

**BH3 — Interaction-Dominated:** Neither N nor s alone reproduces full transition; interaction required.

---

## 6. Minimal Experiment Set (M4)

### M4a — N-Sweep at V4.1 Regime

| Point | N | ξ | K₀ | s | Seeds | Runs |
|:------|:--|:---|:---|:---|:------|:----:|
| Baseline | 60 | 1.75 | 1.2 | 0.10 | 0–4 | 5 |
| Intermediate | 80 | 1.75 | 1.2 | 0.10 | 0–4 | 5 |
| Target | 100 | 1.75 | 1.2 | 0.10 | 0–4 | 5 |

### M4b — s-Sweep at V4.1 Regime

| Point | N | ξ | K₀ | s | Seeds | Runs |
|:------|:--|:---|:---|:---|:------|:----:|
| Baseline | 60 | 1.75 | 1.2 | 0.10 | 0–4 | 5 |
| Intermediate | 60 | 1.75 | 1.2 | 0.09 | 0–4 | 5 |
| Target | 60 | 1.75 | 1.2 | 0.08 | 0–4 | 5 |

### M4c — Two-Parameter Cross (if needed)

| N | s | Seeds | Runs |
|:--|:---|:------|:----:|
| 60 | 0.10 | 0–4 | 5 |
| 60 | 0.08 | 0–4 | 5 |
| 100 | 0.10 | 0–4 | 5 |
| 100 | 0.08 | 0–4 | 5 |

**Total: 30 runs (M4a+M4b) or 50 (with M4c). ~6–10s.**

### Decision tree

```
M4a (N sweep) → N=100 at V4.1 regime → Ω CV ≈ 0.08?
  YES → N is primary Ω driver → proceed to M4b
  NO  → N insufficient → M4c (interaction)

M4b (s sweep) → s=0.08 at V4.1 regime → MD CV ≈ 0.07?
  YES → s is primary MD driver → BH2 (two-driver) → H11 evidence
  NO  → M4c
```

---

## 7. Recommended M4 Suite

`V5_3_RegimeTransitionDriver_Tests.cs` (RTD) — 12 tests, 30 runs.

| Tests | Purpose |
|:------|:--------|
| RTD-01..03 | N sweep execution (60, 80, 100) |
| RTD-04..05 | Ω CV and MD CV across N sweep |
| RTD-06..08 | s sweep execution (0.10, 0.09, 0.08) |
| RTD-09..10 | Ω CV and MD CV across s sweep |
| RTD-11 | Driver attribution |
| RTD-12 | Claim discipline audit |

---

## 8. Claim Discipline

| Statement | Classification |
|:----------|:--------------|
| Stability inverts between V4.1 and V5.3 | SUPPORTED |
| Transition is not a seed artifact | SUPPORTED |
| N is primary Ω CV driver | HYPOTHESIS |
| s is primary MD CV driver | HYPOTHESIS |
| N and s effects are independent (H11) | HYPOTHESIS |
| H10/H11/H12 confirmed | NOT CLAIMED |

---

*Analysis 2026-07-16. All driver attributions are working hypotheses. M4 is the minimal experiment to distinguish single-driver, two-driver, and interaction-dominated explanations.*
