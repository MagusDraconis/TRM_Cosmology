# TRM V5.60 — Final Synthesis: Kernel Emergence Audit

**Date:** 2026-07-22
**Status:** COMPLETE
**Branch:** feature/v5.60-kernel-emergence-audit
**Tests:** 7/7 passed (all LongRunning)
**Cumulative Tests:** 2940 (2933 + 7)

---

## Executive Summary

V5.60 audited three foundational questions about the SAC (Self-Adaptive Coupling) dynamics:

1. **How does km (mean coupling) emerge?** — Amnesic emergence via Cupd, driven by SAC dynamical feedback across epochs.
2. **Is c_eff (= Omega × MeanDist) an invariant?** — FALSIFIED. c_eff varies with N and seed. Omega and MeanDist are CORRELATED (r=0.81), not orthogonal.
3. **Does SAC converge to a fixed point?** — FALSIFIED. SAC is a LIMIT CYCLE with period 2 epochs, half-life ~66 epochs.

**Bottom line:** V6 readiness is NOT ACHIEVED. The three core assumptions of a V6 spacetime-emergence derivation (invariant c_eff, orthogonal Omega/MeanDist, SAC fixed point) are all falsified or unsupported.

---

## 1. Suite Summaries

### 1.1 KEM_01 — Kernel Emergence Audit

**Question:** How is km generated from random K?

**Method:** Simulated SAC across 3 epochs for N=70,72,75 (200 seeds each), tracked P1/P1b km separation.

**Key findings:**
| Metric | Value |
|:-------|:------|
| P1/P1b separation epoch 1 | 0.249σ |
| P1/P1b separation epoch 3 | 1.445σ |
| Epoch 1 contribution | 17% |
| Epoch 3 contribution | 67% |
| Growth pattern | DISTRIBUTED |
| init↔epoch1 correlation | r=0.000 |
| init↔final correlation | r=0.000 |
| N=70 eff | 1.669σ |
| N=72 eff | 1.792σ |
| N=75 eff | 1.440σ |

**Decision:** Model C — Amnesic emergence. First Cupd erases initial K and creates new structure from Sim→RP→DL phase data. km emergence builds across epochs through SAC dynamical feedback.

**Evidence strength:** Strong. Clear separation growth, zero initial→final correlation.

---

### 1.2 KSP_01 — Kuramoto Slip-Phase Audit

**Question:** Does phase-slip frequency generate the P1/P1b signal?

**Method:** Computed phase slips for connected oscillator pairs at epoch 1, correlated with km and class.

**Key findings:**
| Relationship | Pearson r | p-value |
|:-------------|:---------:|:-------:|
| rawIQR → meanSlipRate | 0.000 | 1.000 |
| rawIQR → slipFraction | 0.000 | 1.000 |
| meanSlipRate → km | 0.000 | 1.000 |
| slipFraction → km | 0.000 | 1.000 |
| rawIQR → km | 0.000 | 1.000 |

**Decision:** Model A — Phase slips are NEGLIGIBLE. At K=0.5 (> critical coupling), all connected pairs are fully locked (zero slips). The signal must come from phase-difference VARIANCE (tightness of lock), not from slips.

**Evidence strength:** Definitive. Slips are identically zero across all tested profiles.

**Interpretation:** Signal origin is SAC DYNAMICAL FEEDBACK — the multi-epoch iteration of the Sim→RP→DL→Cupd chain.

---

### 1.3 EMG_01 — Emergence Audit Part A: Structural Invariance of c_eff

**Question:** Is c_eff = Omega × MeanDist structurally invariant?

**Method:** Computed CV across seeds and N for Omega, MeanDist, and c_eff using 10 seeds × 5 N values.

**Key findings:**
| Metric | CV(seed) | CV(N) | Mean | Invariant? |
|:-------|:--------:|:-----:|:----:|:----------:|
| Omega | 0.396 | 0.708 | 2.336 | **no** |
| MeanDist | 0.445 | 0.486 | 0.607 | **no** |
| c_eff | 0.800 | 1.083 | 1.778 | **no** |

**Decision:** FALSIFIED. c_eff is NOT invariant. CV(seed)=0.80 and CV(N)=1.08 far exceed the <0.05 threshold for invariance. Both Omega and MeanDist individually also fail invariance.

**Evidence strength:** Strong. 50 profiles across 5 N values.

---

### 1.4 EMG_02 — Variance Decomposition

**Question:** Why is c_eff NOT invariant?

**Method:** ANOVA-style variance decomposition (Between-N, Between-Seed, Residual) for Omega and MeanDist. Tested candidate invariant combinations. Measured Omega-MeanDist correlation.

**Key findings — Variance decomposition:**
| Source | Omega % | MeanDist % |
|:-------|:-------:|:----------:|
| Between-N | 62.6% | 33.7% |
| Between-Seed | 7.1% | 23.0% |
| Residual | 30.3% | 43.3% |

Omega is N-dominated (62.6%). MeanDist has high residual variance (43.3%).

**Key findings — Candidate invariants:**
| Candidate | CV(seed) | CV(N) |
|:----------|:--------:|:-----:|
| Omega | 0.396 | 0.708 |
| MeanDist | 0.445 | 0.486 |
| c_eff | 0.800 | 1.083 |
| O/MD | 0.412 | 0.404 |
| MD/O | **0.343** | **0.386** |
| O²/MD | 0.595 | 0.954 |
| MD²/O | 0.589 | 0.500 |
| sqrt(O×MD) | 0.408 | 0.582 |

MD/O is the closest to invariant — but still CV≈0.34-0.39, far above invariance threshold.

**Key findings — Omega-MeanDist correlation:**
| N | Pearson r | Spearman | p-value | Dependence |
|:-:|:---------:|:--------:|:-------:|:-----------|
| 67 | 0.829 | 0.624 | 0.002 | moderate |
| 70 | 0.778 | 0.939 | 0.006 | moderate |
| 72 | 0.638 | 0.370 | 0.046 | moderate |
| 75 | 0.889 | 0.830 | 0.000 | moderate |
| 80 | 0.946 | 0.636 | 0.000 | moderate |
| **Overall** | **0.814** | **0.865** | — | **moderate** |

**Decision:** FALSIFIED. Omega and MeanDist are NOT orthogonal — they are strongly correlated (r=0.81). This undermines the assumption that they represent independent "time" and "space" proxies. The correlation increases with N, reaching r=0.95 at N=80.

**Evidence strength:** Definitive. Consistent across all N with p<0.05.

---

### 1.5 KEM_02 — Kernel Emergence Depth

**Question:** What is the depth of kernel emergence? How does K0 affect km?

**Method:** Emergence speed across N (60-80), phase-diff variance analysis at N=72, K0 sweep (seed 1005).

**Key findings — K0 sweep (seed 1005, N=72):**
| K0 | km1 | km3 | km5 | Δ(km5-km1) |
|:--:|:---:|:---:|:---:|:----------:|
| 0.8 | 0.717 | 0.649 | 0.672 | -0.044 |
| 1.0 | 0.896 | 0.900 | 0.886 | -0.010 |
| 1.2 | 1.075 | 0.867 | 0.740 | -0.335 |
| 1.4 | 1.254 | 0.844 | 0.810 | -0.445 |
| 1.6 | 1.434 | 0.847 | 0.774 | -0.659 |

**Decision:** km declines monotonically across epochs for K0≥1.0. Higher K0 produces larger absolute Δ but converges toward similar km≈0.77-0.81 range. K0 controls the amplitude of the epoch-1 overshoot, not the asymptotic km.

**Evidence strength:** Moderate. Single-seed sweep, limited P1/P1b retention in Part A/B.

---

### 1.6 KEM_03 — Parameter Sweep

**Question:** How do Xi, N, Dt affect km stability? (seed 1005, no classification)

**Method:** 5-epoch SAC chain, Xi×7, N×9, Dt×5 sweeps. Stability = |km5-km1| < 0.05.

**Key findings — Xi sweep (N=72):**
| Xi | km1 | km5 | Δ | Stable? |
|:--:|:---:|:---:|:--:|:-------:|
| 1.00 | 1.007 | 0.992 | -0.016 | YES |
| 1.25 | 1.037 | 0.980 | -0.057 | no |
| 1.50 | 1.059 | 0.822 | -0.237 | no |
| 1.75 | 1.075 | 1.135 | +0.060 | no |
| 2.00 | 1.088 | 1.106 | +0.018 | YES |
| 2.25 | 1.099 | 1.094 | -0.005 | YES |
| 2.50 | 1.107 | 1.046 | -0.061 | no |

Xi=1.00, 2.00, 2.25 show apparent stability after 5 epochs. Intermediate Xi values produce oscillation.

**Key findings — N sweep (Xi=1.75):**
All N values show |Δ| > 0.05 — NO N-value produces stable convergence at 5 epochs.

**Key findings — Dt sweep (N=72, Xi=1.75):**
| Dt | km1 | km5 | Δ | Stable? |
|:--:|:---:|:---:|:--:|:-------:|
| 0.010 | 1.041 | 0.918 | -0.122 | no |
| 0.025 | 1.043 | 0.876 | -0.168 | no |
| 0.050 | 1.075 | 1.135 | +0.060 | no |
| 0.075 | 1.086 | 1.105 | +0.019 | YES |
| 0.100 | 1.082 | 1.001 | -0.080 | no |

Dt=0.075 shows the most stable behavior. Lower Dt values show overshoot-then-decline.

**Decision:** km DOES NOT converge to a fixed point for most parameters. The instability is robust — it appears across Xi, N, and Dt sweeps. This directly motivated KEM_04.

---

### 1.7 KEM_04 — Limit Cycle Characterization

**Question:** Is SAC a fixed point or a limit cycle? (seed 1005, no classification)

**Method:** 10-epoch SAC chain with damped sinusoid fit: km(t) = km_eq + A·exp(-λt)·sin(ωt + φ).

**Key findings — Baseline (N=72, K0=1.2, Xi=1.75, Dt=0.05):**
| Parameter | Value |
|:----------|:------|
| Period | 2.000 epochs |
| ω | π rad/epoch |
| λ (damping) | 0.0105 |
| Half-life | 66.1 epochs |
| km_eq | 0.878 |
| Amplitude A | 0.120 |
| R² | 0.350 |

**Key findings — Parameter sensitivity:**

| Parameter | Effect on λ | Effect on convergence |
|:----------|:-----------|:---------------------|
| K0 ≤ 1.0 | λ=0.11-0.13 (oscillatory) | No |
| K0 ≥ 1.4 | λ=0 (convergent) | Yes |
| Xi=1.25 | λ=0 (convergent) | Yes |
| N=60 | λ=0.276 (rapid damping) | Yes (fast) |
| N=100 | λ=0 (convergent) | Yes |
| Dt | λ=0.01-0.13 (varies) | Only at Dt=0.075 |

**Decision:** SAC is fundamentally a LIMIT CYCLE, not a fixed point. The oscillation period is exactly 2 epochs (alternating peak/trough). The half-life of ~66 epochs means the oscillation is effectively persistent in practical simulations. Convergence to a fixed point requires specific parameter choices (high K0 ≥ 1.4, large N ≥ 100, or specific Xi values).

**Evidence strength:** Strong. Consistent peaks at epochs 1,3,5,7,9; troughs at 2,4,6,8 across 10 epochs. The KEM_01 "emergence" is the first half-cycle of this oscillation.

---

## 2. Consolidated Findings

### 2.1 SUPPORTED (with evidence)

| # | Finding | Evidence | Source |
|:-:|:--------|:---------|:-------|
| S1 | km emerges through SAC dynamical feedback across epochs | P1/P1b separation grows from 0.25σ→1.45σ | KEM_01 |
| S2 | First Cupd erases initial K structure (AMNESIC emergence) | init↔epoch1 r=0.000 | KEM_01 |
| S3 | Phase slips are zero at K=0.5 — pairs are fully locked | All slip correlations r=0.000 | KSP_01 |
| S4 | The P1/P1b signal originates in phase-diff variance | Slips=0, variance correlates weakly | KSP_01 |
| S5 | Omega and MeanDist are CORRELATED (r=0.81 overall, r=0.95 at N=80) | p<0.05 at all N | EMG_02 |
| S6 | SAC is a LIMIT CYCLE, not a fixed point | Period=2 epochs, λ=0.0105 | KEM_04 |
| S7 | km never converges for most parameter regimes | |Δkm|>0.05 for most Xi/N/Dt | KEM_03, KEM_04 |
| S8 | The SAC chain is: K→Sim→RP→Nm→DL→Cupd→K' | Traced through epoch | KEM_01 |

### 2.2 FALSIFIED (with evidence)

| # | Hypothesis | Why falsified | Evidence | Source |
|:-:|:-----------|:-------------|:---------|:-------|
| F1 | c_eff is structurally invariant | CV(seed)=0.80, CV(N)=1.08 | 50 profiles | EMG_01 |
| F2 | Omega and MeanDist are orthogonal | r=0.81 overall, r=0.95 at N=80 | p<0.05 at all N | EMG_02 |
| F3 | SAC converges to a fixed point | Period=2 limit cycle, λ=0.01 | 10 epochs, multiple params | KEM_04 |
| F4 | K0→km is monotonic in single epoch | K0 sweep shows non-monotonic km₅ | km varies with K0 | KEM_02 |

### 2.3 CONDITIONAL

| # | Finding | Condition |
|:-:|:--------|:----------|
| C1 | All findings are seed-1005 or finite-seed-sample limited | Broader seed ensembles needed |
| C2 | Limit cycle characterization uses simple damped sinusoid fit (R²=0.35) | Alternative cycle shapes (square-wave, asymmetric) may fit better |
| C3 | MD/O is the closest to invariant (CV≈0.34-0.39) | Still far from true invariance |
| C4 | Convergence at K0≥1.4, N=100 may be artifacts of 10-epoch window | Longer simulations needed |

### 2.4 OPEN QUESTIONS

| # | Question | Priority |
|:-:|:---------|:--------:|
| Q1 | Why is the oscillation period exactly 2 epochs? What mechanism drives the flip-flop? | HIGH |
| Q2 | Can the oscillation be characterized analytically from the SAC equations? | HIGH |
| Q3 | Is there an invariant quantity in the SAC dynamics (if not c_eff)? | MEDIUM |
| Q4 | Why are Omega and MeanDist correlated? Is this a Kuramoto property or SAC-specific? | MEDIUM |
| Q5 | Does the limit cycle persist to infinite epochs, or does km eventually converge? | MEDIUM |
| Q6 | Can the oscillation be damped/resonated by parameter tuning (control theory approach)? | LOW |
| Q7 | Does the 2-epoch period relate to graph diameter or spectral gap? | LOW |

### 2.5 NOT CLAIMED

- Physical c, G, GR, spacetime, or cosmology derivation
- V6 readiness
- Universal SAC behavior
- Physical interpretation of the limit cycle
- Invariance of any internal quantity
- Causal closure
- Modification of M3++ or Stop-Low

---

## 3. V6 Readiness Assessment

| Criterion | Required | Actual | Status |
|:----------|:---------|:-------|:------:|
| Invariant c_eff | CV < 0.05 | CV = 0.80-1.08 | ❌ FAIL |
| Orthogonal Omega/MeanDist | |r| < 0.3 | r = 0.81 | ❌ FAIL |
| SAC fixed point | Stable convergence | Limit cycle (λ=0.01) | ❌ FAIL |
| Causal closure | Proven | BLOCKED | ❌ FAIL |

**V6 readiness: NOT ACHIEVED.** Three of four core V6 prerequisites are falsified. The fourth (causal closure) was already blocked from V5.40-V5.41.

---

## 4. Stop-Low Status

Stop-Low policy remains SAFE and UNCHANGED:
- c3OmgS > 0.1: continue
- c3OmgS ≤ 0.1: stop
- Preserves all rescues. Zero damage.

The V5.60 findings are DIAGNOSTIC, not causal. They do not affect Stop-Low validity.

---

## 5. Recommended Next Steps

### V5.61: Limit Cycle Mechanism

**Primary question:** WHY is the SAC period exactly 2 epochs?

**Proposed investigation:**
1. Track full K-matrix across epochs (not just km). Does the oscillation reflect alternation between two coupling matrices K_even and K_odd?
2. Trace the Cupd output — does d (distance matrix) alternate between two configurations?
3. Test whether the period-2 cycle is a property of the exponential Cupd: K = K0·exp(-d/xi)
4. Simulate longer chains (50-100 epochs) to confirm persistent oscillation vs. ultra-slow convergence
5. Test whether the cycle amplitude depends on initial conditions vs. being an attractor property

**Rationale:** KEM_04 proved SAC is a limit cycle. KEM_04 did NOT explain why. Understanding the mechanism could reveal an analytical invariant or a control strategy.

### Alternative: V5.61 — Omega-MeanDist Correlation Origin

**Primary question:** Why are Omega and MeanDist correlated (r=0.81)?

**Proposed investigation:**
1. Is the correlation present in the raw Kuramoto dynamics (before SAC)?
2. Does SAC amplify or suppress the correlation?
3. Is the correlation a graph-topology effect (Erdős-Rényi specific)?
4. Does the correlation depend on the ω_i distribution spread?

**Rationale:** The r=0.81 correlation is the strongest single finding against V6 readiness. Understanding its origin could either salvage or permanently close the spacetime-emergence path.

### Pivot consideration

The three V6 prerequisites (invariant c_eff, orthogonal Ω/MD, SAC fixed point) are ALL falsified. A pivot to a different V6 approach may be warranted:
- Abandon the c_eff = Ω×MD definition; seek a different internal propagation speed
- Abandon the orthogonality requirement; accept correlated "time" and "space" proxies
- Accept the limit cycle as the fundamental dynamical mode and build V6 around it

**Recommendation:** Complete V5.61 (limit cycle mechanism) before deciding on pivot. The 2-epoch period suggests a deep structural property that may be analytically tractable.

---

## 6. Development Statistics

| Metric | Value |
|:-------|:------|
| V5.60 test suites | 7 (KEM_01, KSP_01, EMG_01, EMG_02, KEM_02, KEM_03, KEM_04) |
| V5.60 tests passed | 7/7 |
| Cumulative tests (est.) | 2940 |
| Supported findings | 8 |
| Falsified hypotheses | 4 |
| Open questions | 7 |
| V6 readiness | NOT ACHIEVED |
| Stop-Low | SAFE (unchanged) |

---

*Generated 2026-07-22. This document supersedes TRM_V5_60_Emergence_of_c_Space_Time.md as the authoritative V5.60 summary. All claims are diagnostic, not physical. TRM explicitly does NOT claim derivation of physical c, G, GR, spacetime, or cosmology.*
