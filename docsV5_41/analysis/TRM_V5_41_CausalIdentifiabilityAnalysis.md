# TRM V5.41 Causal Identifiability Analysis

**Version:** 1.0 | **Date:** 2026-07-19
**Suite:** TDA
**Status:** COMPLETE

---

## 1. Summary

TDA_01 classified the identifiability of causal claims under attractor absorption
using natural-variation grounding data (50 profiles, 6 N values).

**Key result:** Natural variation provides stable diagnostic stratification but not
causal identification. 8 causal claims are REJECTED by prior evidence. Stop-Low is
outcome-validated without requiring causal closure. All 9 gates reached.

---

## 2. A1 — Natural Variation Identifiability

**Evidence (50 profiles, no perturbation):**
- lambda1 ~ c3OmgS: +0.226
- rebMagnitude ~ c3OmgS: -0.630
- omDist ~ c3OmgS: -0.356
- omegaPerK ~ c3OmgS: -0.524

**lambda1 tercile separation:**
- c3OmgS delta: 0.2718
- Rescue rate delta: 16.7pp

**Classification: STABLE DIAGNOSTIC RELATION.**

Can identify diagnostic separation of c3OmgS by lambda1 tercile. Cannot identify
causal direction or mechanism. Correlation is not causation — the relationship
may be driven by a common cause (attractor state) rather than lambda1 → c3OmgS.

---

## 3. A2 — Invariance Identifiability

| N | n | lam~cs corr |
|--:|--:|------------:|
| 67 | 7 | +0.615 |
| 70 | 14 | +0.013 |
| 72 | 14 | +0.449 |
| 75 | 9 | +0.260 |

Mean corr: +0.334, std: 0.224. No sign reversal across N (consistent direction).

**Classification: WEAKLY INVARIANT — consistent sign, variable magnitude.**

N=70 is a notable outlier (corr = +0.013, essentially zero). The relationship
varies substantially in strength across N, preventing invariant-law claims.
Can identify N-dependent diagnostic patterns; cannot identify invariant causal law.

---

## 4. A3 — Mediation Identifiability

| Path | Correlation | Classification |
|:-----|------------:|:---------------|
| lambda1 → c3OmgS → Stop-Low | +0.226 | DIAGNOSTIC ONLY |
| rebMag → c3OmgS → persistence | -0.630 | DIAGNOSTIC ONLY |
| omDist → c3OmgS → persistence | -0.356 | DIAGNOSTIC ONLY |
| c3OmgS → Omega T2 → persistence | structural | DEFINITIONAL |

**Classification: TEMPORALLY AMBIGUOUS.**

All variables are measured at the same pipeline epoch. True mediation analysis
requires staged measurement (T1 → T2 → T3 temporal separation), which is not
currently instrumented. Cross-sectional correlations are informative but do
not establish causal ordering.

---

## 5. A4 — Counterfactual Trace Identifiability

**Quartile comparison (low vs high c3OmgS):**

| Variable | Low Quartile | High Quartile | Diff |
|:---------|-------------:|--------------:|-----:|
| lambda1 | — | — | +0.045 |
| rebMagnitude | — | — | **-1.936** |
| omDist | — | — | -0.104 |

**Classification: OBSERVATIONAL TRACE — rebMag strongly separates.**

Can identify which variables differ between outcome groups. Cannot identify
causal direction — these are observational differences, not interventional
evidence.

---

## 6. A5 — Causal Rejection Inventory

**8 claims REJECTED by prior evidence:**

1. Direct K/lambda1 perturbation sufficiency — V5.38 RII, V5.39 AAE
2. Omega-proximity robust dose-response — V5.37 OPE (downgraded)
3. Attractor-aligned perturbation superiority — V5.40 CTE Gate D FAILED
4. Full gain-chain causal closure — V5.35 MCA (falsified)
5. c3OmegaShift causal sufficiency — V5.35, V5.40 CTA (artifact)
6. V6 readiness — all versions V5.34–V5.41
7. lambda1 as causal driver — V5.38 RII, V5.39 AAE
8. rebMagnitude as causal driver — V5.38 RIA (diagnostic companion)

**Falsification evidence:** 30/50 (60%) wrong-direction profiles.
Causal sufficiency is NOT established.

---

## 7. A6 — Operational Sufficiency

| Stratum | Profiles | Rescues |
|:--------|---------:|--------:|
| A (c3OmgS ≤ 0.1) | 35 | **0** |
| B (c3OmgS > 0.1) | 15 | **4** |

**Stop-Low validity: OUTCOME-VALIDATED.**

- Predictive validity: YES (stratified rescue rates)
- Safety: YES (zero damage in stratum A)
- Reproducibility: YES (V5.32 — execution-order invariant)
- Efficiency: YES (V5.33 — 72% workload reduction)
- **Causal closure required: NO**

Policy is validated by OUTCOME, not by causal mechanism. This is the central
methodological deliverable of V5.41.

---

## 8. Identifiability Matrix

| Claim | Method | Identifiable? | Level |
|:------|:-------|:------------:|:------|
| lambda1 diagnostic value | Natural var. | YES | DIAGNOSTIC |
| lambda1 → c3OmgS causation | Natural var. | NO | CORRELATION ONLY |
| rebMag diagnostic value | Natural var. | YES | DIAGNOSTIC |
| rebMag → c3OmgS causation | Natural var. | NO | CORRELATION ONLY |
| lambda1~cs N-invariance | Invariance | NO | N-DEPENDENT |
| Diagnostic hierarchy stability | Invariance | PARTIAL | WEAKLY INVARIANT |
| Temporal causal ordering | Mediation | NO | TEMPORALLY AMBIGUOUS |
| Variable separation by outcome | Counterfact. | YES | OBSERVATIONAL TRACE |
| Causal sufficiency of any var | Rejection | YES (rejected) | REJECTED |
| Stop-Low operational validity | Operational | YES | OUTCOME-VALIDATED |
| V6 readiness | All methods | NO | NOT READY |

---

## 9. Method Ranking

| Rank | Method | Usefulness |
|-----:|:-------|:-----------|
| 1 | Causal rejection | MOST USEFUL — prevents overclaiming |
| 2 | Counterfactual trace | USEFUL — identifies outcome-associated variables |
| 3 | Natural variation | USEFUL — diagnostic stratification |
| 4 | Operational sufficiency | ESSENTIAL — validates policy without causality |
| 5 | Invariance testing | LIMITED — N-dependent |
| 6 | Mediation analysis | LIMITED — no temporal ordering |
| 7 | Direct perturbation | INVALIDATED |

---

## 10. Currently Unidentifiable

1. Causal direction of lambda1–c3OmgS relationship
2. Causal mechanism of c3OmegaShift formation
3. Why Stop-Low threshold 0.1 works (mechanism unknown)
4. Why the attractor absorbs perturbations (structure unknown)
5. V6 geometry (length, space, velocity, c)
6. Physical interpretation of any TRM observable

---

## 11. Gate Summary

| Gate | Description | Status |
|:-----|:------------|:------:|
| A | Identifiability matrix built | REACHED |
| B | Natural variation classified | REACHED — STABLE DIAGNOSTIC |
| C | Invariance limits identified | REACHED — WEAKLY INVARIANT |
| D | Mediation limits identified | REACHED — temporally ambiguous |
| E | Counterfactual trace classified | REACHED — OBSERVATIONAL TRACE |
| F | Causal rejections consolidated | REACHED — 8 claims rejected |
| G | Operational sufficiency preserved | REACHED |
| H | V6 still not ready | REACHED |
| I | Ready for TDS | REACHED |

---

## 12. Supported Findings

1. Natural variation is diagnostically useful but not causally identifying.
2. lambda1–c3OmgS relationship is STABLE DIAGNOSTIC, not causal.
3. Relationship is WEAKLY INVARIANT across N (consistent sign, variable magnitude).
4. Mediation is TEMPORALLY AMBIGUOUS without staged measurement.
5. Counterfactual trace identifies outcome-associated variables (observational, not causal).
6. 8 causal claims are REJECTED by prior evidence.
7. Stop-Low is OUTCOME-VALIDATED — does not require causal closure.
8. V6 remains NOT READY.

## 13. Not Claimed

- Causal closure
- Causal direction of any diagnostic relationship
- V6 readiness
- Physical interpretation

## 14. Recommendation

Proceed to **TDS (Final Synthesis)** for V5.41 closure. TDI (Methodology Audit)
can be skipped — TDA subsumes the audit function by systematically classifying
every claim's identifiability status.

---

*Generated 2026-07-19. V5.41 TDA analysis document.*
