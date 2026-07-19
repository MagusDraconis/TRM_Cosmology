# TRM V5.41 Causal Test Design Execution — Analysis

**Version:** 1.0 | **Date:** 2026-07-19
**Suite:** TDE
**Status:** COMPLETE

---

## 1. Summary

TDE_01 evaluated 7 causal test design families (D1–D7) under attractor absorption
constraints. Lightweight natural-variation simulations (N=[65,70,72,75], 80 seeds)
provided feasibility data for D2–D7.

**Key result:** Perturbation-based testing is INVALIDATED (D1). Natural variation,
counterfactual trace, and causal rejection tests are FEASIBLE (D2, D5, D6).
Stop-Low is operationally sufficient without causal closure (D7). All 8 gates reached.

---

## 2. D1 — Perturbation Invalidation

| Strategy | Evidence | Classification |
|:---------|:---------|:---------------|
| Omega-proximity perturbation | Non-monotonic (V5.37) | INVALID |
| lambda1/K-state perturbation | Absorbed (V5.38 RII) | DIAGNOSTIC ONLY |
| Attractor-aligned perturbation | Absorbed 87–99% (V5.40) | INVALID — no survival advantage |
| Mismatch perturbation | Absorbed similarly (V5.40) | INVALID — direction-invariant |
| Combined aligned perturbation | Absorbed (V5.40) | INVALID — no synergy |

**Conclusion:** All perturbation-based causal testing strategies are invalidated by
uniform, direction-invariant attractor absorption (Model D, V5.40).

---

## 3. D2 — Natural Variation Feasibility

**Approach:** Use naturally occurring profile-to-profile variance without perturbation.

**Results (52 profiles):**

| lambda1 Tercile | Mean c3OmgS | Rescue % |
|:----------------|------------:|---------:|
| Low | -0.1608 | 0.0% |
| Mid | -0.0413 | 5.9% |
| High | +0.1476 | 22.2% |

**Natural correlations (no perturbation):**
- lambda1 ~ c3OmgS: +0.192
- omDist ~ c3OmgS: -0.203
- rebMagnitude ~ c3OmgS: -0.687
- omegaPerK ~ c3OmgS: -0.547

**Classification: FEASIBLE.** Natural variation cleanly separates c3OmgS across
lambda1 terciles, with monotonic rescue rate increase. Does not require perturbation.
However, correlation magnitudes are modest (0.19–0.69) and relationships are
observational, not causal.

---

## 4. D3 — Invariance Testing Feasibility

**Approach:** Test whether lambda1–c3OmgS relationship is stable across N.

| N | Profiles | lam~cs corr | Mean c3OmgS | Rescue % |
|--:|---------:|------------:|------------:|---------:|
| 65 | 5 | +0.395 | +0.1605 | 0.0% |
| 70 | 19 | -0.081 | +0.0562 | 0.0% |
| 72 | 17 | +0.445 | +0.0959 | 11.8% |
| 75 | 11 | +0.370 | -0.3890 | 27.3% |

**Correlation stability:** mean = +0.283, std = 0.212.

**Classification: VARIABLE — N-dependent.** The lambda1–c3OmgS correlation is
not invariant across N. N=70 is an outlier (negative correlation). This limits
invariance-based causal claims — relationships that vary across N are unlikely
to reflect stable causal structure.

---

## 5. D4 — Mediation Analysis Feasibility

**Approach:** Test whether omDist mediates lambda1 → c3OmgS.

**Results:**
- lambda1 ~ omDist: -0.053 (weak)
- omDist ~ c3OmgS: -0.203 (modest)

**Classification: PARTIAL.** Cross-sectional correlations exist but temporal
ordering is not established in the current pipeline. All variables are measured
at the same epoch. True mediation analysis would require staged measurement
(T1 → T2 → T3 separation), which is not currently instrumented.

---

## 6. D5 — Counterfactual Trace Feasibility

**Approach:** Match profiles with similar baseline but different c3OmgS.

**Results (quartile comparison):**

| Metric | Low-c3OmgS Quartile | High-c3OmgS Quartile |
|:-------|--------------------:|----------------------:|
| lambda1 | 0.935 | 0.969 |
| rebMagnitude | 2.013 | -0.444 |
| n | 13 | 13 |

**Classification: FEASIBLE.** Matched comparison is possible and informative.
rebMagnitude shows strong separation (2.013 vs -0.444). However, without perturbation,
these are observational differences — they describe what IS, not what CAUSES what.

---

## 7. D6 — Causal Sufficiency Rejection

**Evidence that falsifies causal sufficiency:**
1. Correlation vanishes when controlling for attractor state
2. High-c3OmgS profiles exist with "wrong" lambda1 direction
3. Natural variation shows opposite direction in different N regimes
4. Stop-Low works without lambda1 knowledge

**Wrong-direction count:** 28/52 profiles (53.8%) — majority of profiles do NOT
follow the predicted lambda1→c3OmgS direction.

**Classification: RECOMMENDED.** Causal sufficiency is NOT established.
This test design is the most robust: it actively seeks falsification rather
than confirmation.

---

## 8. D7 — Operational Sufficiency

**Question:** Does Stop-Low require causal closure?

**Results:**
- Stratum A (c3OmgS ≤ 0.1): 36 profiles, **0 rescues**
- Stratum B (c3OmgS > 0.1): 16 profiles, **5 rescues**

**Classification: ACCEPTED.** Stop-Low is operationally sufficient without
causal closure. The policy is validated by outcome (rescue preservation,
damage = 0). Causal understanding would explain WHY the policy works but
is not required for the policy to be USED.

**This is the central methodological insight of V5.41: predictive validity
and causal closure are distinct. Stop-Low has the former without the latter.**

---

## 9. Design Comparison Summary

| Design | Absorption-OK? | Feasible? | Recommendation |
|:-------|:--------------:|:---------:|:---------------|
| D1 — Direct perturbation | NO | NO | **REJECT** |
| D2 — Natural variation | YES | YES | CONSIDER |
| D3 — Invariance testing | YES | WEAK | CONSIDER |
| D4 — Mediation analysis | YES | PARTIAL | CONSIDER |
| D5 — Counterfactual trace | YES | YES | CONSIDER |
| D6 — Causal rejection | YES | YES | **RECOMMEND** |
| D7 — Operational sufficiency | YES | YES | **ACCEPT** |

---

## 10. Gate Summary

| Gate | Description | Status |
|:-----|:------------|:------:|
| A | Invalid perturbation methods identified | REACHED |
| B | Viable non-perturbation tests identified | REACHED |
| C | Natural variation feasibility assessed | REACHED — FEASIBLE |
| D | Invariance testing feasibility assessed | REACHED — VARIABLE |
| E | Mediation/trace feasibility assessed | REACHED |
| F | Operational sufficiency clarified | REACHED |
| G | Causal closure still not established | REACHED |
| H | V6 still not ready | REACHED |

---

## 11. Supported Findings

1. **Perturbation-based causal testing is INVALIDATED** under attractor absorption (D1).
2. **Natural variation is FEASIBLE** — lambda1 terciles cleanly separate c3OmgS (D2).
3. **Invariance is VARIABLE across N** — correlation std = 0.212 (D3).
4. **Mediation analysis is PARTIALLY feasible** — cross-sectional correlations exist
   but temporal ordering is missing (D4).
5. **Counterfactual trace is FEASIBLE** — matched quartile comparison reveals strong
   rebMagnitude separation (D5).
6. **Causal sufficiency is NOT established** — 53.8% wrong-direction profiles (D6).
7. **Stop-Low is operationally sufficient without causal closure** — 0 rescues in
   stratum A, all rescues in stratum B (D7).
8. **V6 remains NOT READY.**

## 12. Not Claimed

- Causal closure
- V6 readiness
- Physical interpretation
- Deterministic rescue
- lambda1 causal control

## 13. Recommendation

Proceed to **TDA (Causal Test Design Analysis)** or directly to **TDS (Final Synthesis)**
if TDE provides sufficient evidence for V5.41 closure.

The central deliverable of V5.41 is the methodological framework: perturbation-based
testing is invalidated; natural variation, counterfactual trace, and causal rejection
are viable alternatives; and Stop-Low is operationally sufficient without causal closure.

---

*Generated 2026-07-19. V5.41 TDE analysis document.*
