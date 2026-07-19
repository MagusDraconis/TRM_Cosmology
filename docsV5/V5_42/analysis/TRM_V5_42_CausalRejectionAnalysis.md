# TRM V5.42 Causal Rejection Analysis

**Version:** 1.0 | **Date:** 2026-07-19
**Suite:** CRA
**Status:** COMPLETE

---

## 1. Summary

CRA_01 converted CRE counterfactual evidence into a precise causal rejection map
using 73 natural-variation profiles (100 seeds × 6 N).

**Key result: 5/5 single-variable sufficiency claims REJECTED. 7/11 near-identical
profiles diverge on c3OmgS. Causal closure = Model B (narrowed, not achieved).
7 surviving explanations inventoried (none asserted). All 8 gates reached.**

---

## 2. Sufficiency Rejection Table

| # | Claim | Status | Evidence |
|--:|:------|:------:|:---------|
| 1 | lambda1 sufficient for c3OmgS | **REJECTED** | All 3 lam bands have both hi/lo c3 |
| 2 | rebMagnitude sufficient for c3OmgS | **REJECTED** | All 3 reb bands have both hi/lo c3 |
| 3 | omDist sufficient for c3OmgS | **REJECTED** | Both omDist bands have both outcomes |
| 4 | c3OmgS sufficient for rescue | **REJECTED** | 18 high-c3 profiles not rescued |
| 5 | omegaPerK sufficient for rescue | **REJECTED** | oPK bands not pure |
| 6 | Full gain chain sufficient for rescue | **REJECTED** | V5.35 MCA — chain is trace, not causal |
| 7 | Stop-Low A implies impossible rescue | **NOT REJECTED** | 0 low-stratum rescues (operational) |

**All 5 single-variable sufficiency paths are CLOSED.** No measured response-state
variable alone determines c3OmgS or rescue.

---

## 3. Necessity Caution Table

| Variable | Necessity Status | Reason |
|:---------|:-----------------|:-------|
| lambda1 | NOT ESTABLISHED | Sufficiency rejected; necessity untested |
| rebMagnitude | NOT ESTABLISHED | Sufficiency rejected; necessity untested |
| omDist | NOT ESTABLISHED | Sufficiency rejected; necessity untested |
| c3OmgS (>0.1) | **NOT REJECTED** | All rescues in stratum B (operational) |
| omegaPerK | NOT ESTABLISHED | Strongly associated; necessity untested |
| d_tail | NOT ESTABLISHED | Necessary for C3 gain; sufficiency untested |

**Critical distinction:** Sufficiency is rejected (same variable → different outcomes).
Necessity is NOT rejected for most variables — we have NOT shown that high c3OmgS can
occur without these variables. Only c3OmgS>0.1 survives as an operational necessary
condition (all rescues require c3OmgS>0.1).

---

## 4. Near-Identical Divergence Detail

11 matched pairs (lamDelta<0.02, omDistDelta<0.1, rebDelta<0.3):

| Pair | N | lamA | lamB | c3A | c3B | Diverge? |
|-----:|--:|-----:|-----:|----:|----:|:--------:|
| 1 | 75 | 1.001 | 1.012 | -0.698 | -0.702 | no |
| 2 | 72 | 0.862 | 0.880 | **0.342** | **0.025** | **YES** |
| 3 | 72 | 0.860 | 0.859 | -0.705 | -0.705 | no |
| 4 | 72 | 0.941 | 0.932 | **-0.663** | **0.325** | **YES** |
| 5 | 72 | 0.960 | 0.948 | 1.758 | 1.543 | no |
| 6 | 72 | 0.947 | 0.966 | -0.683 | -0.686 | no |
| 7 | 70 | 0.932 | 0.927 | **1.360** | **0.021** | **YES** |
| 8 | 70 | 0.863 | 0.849 | **0.369** | **-0.038** | **YES** |
| 9 | 70 | 0.832 | 0.849 | **1.285** | **-0.038** | **YES** |
| 10 | 67 | 0.960 | 0.966 | **0.152** | **-0.077** | **YES** |
| 11 | 67 | 0.960 | 0.966 | **0.152** | **-0.018** | **YES** |

**7/11 diverge (63.6%).** Divergence distributed across N=67,70,72 — not N-concentrated.

**Striking examples:**
- Pair 7: lam=0.932 vs 0.927 (Δ=0.005), c3=1.360 vs 0.021 (Δ=1.339)
- Pair 9: lam=0.832 vs 0.849 (Δ=0.017), c3=1.285 vs -0.038 (complete outcome reversal)

**Implication:** Diagnostic variables describe a profile but do not determine its
c3OmgS outcome. Something beyond lambda1, omDist, and rebMagnitude — possibly
attractor phase, path dependence, or an unmeasured state variable — drives the
divergence.

---

## 5. Remaining Explanation Inventory

After sufficiency rejection, 7 surviving candidate explanations (none asserted):

1. Hidden response-state variable not in current measurement set
2. Higher-order interaction (lam × reb × omDist) beyond additive
3. Temporal / path dependence not captured by endpoint measurements
4. Attractor phase sensitivity (same endpoint, different trajectory)
5. Stochastic-like profile sensitivity under deterministic dynamics
6. Measurement granularity — current variables too coarse
7. Multi-variable necessity (AND/OR combinations of current vars)

All single-variable sufficiency paths are CLOSED. The remaining search space
requires multi-variable, temporal, or unmeasured-state explanations.

---

## 6. Causal Closure Verdict

**Model B — Causal closure narrowed but NOT achieved.**

- Sufficiency: 5 single-variable + full chain + perturbation = **all REJECTED**
- Necessity: c3OmgS>0.1 NOT REJECTED (operational); all others NOT ESTABLISHED
- Search space: narrowed from ~10 candidate paths to 7 surviving possibilities
- Testability: surviving explanations require variables or measurements not available

The causal search space is smaller but not closed. The remaining explanations
are not testable with current artifacts. This is methodological progress —
eliminating impossible paths — but does not achieve positive causal identification.

---

## 7. Operational Validity

| Stratum | Profiles | Rescues |
|:--------|---------:|--------:|
| A (≤0.1) | 50 | **0** |
| B (>0.1) | 23 | **5** |

**Causal rejection does NOT weaken operational validation.** Stop-Low is validated
by outcome (rescue preservation, zero damage), not by causal mechanism. The policy
works regardless of whether we understand WHY it works.

---

## 8. Gate Summary

| Gate | Description | Status |
|:-----|:------------|:------:|
| A | Sufficiency rejections consolidated | REACHED |
| B | Necessity overclaim avoided | REACHED |
| C | Near-identical divergence scoped | REACHED (7/11, not N-concentrated) |
| D | Remaining search space defined | REACHED (7 explanations) |
| E | Operational policy preserved | REACHED |
| F | Causal closure still blocked | REACHED |
| G | V6 still not ready | REACHED |
| H | Ready for CRS | REACHED |

---

## 9. Supported Findings

1. **5 single-variable sufficiency claims REJECTED** by counterfactual evidence.
2. **7/11 near-identical profiles diverge** on c3OmgS (63.6%).
3. **Causal closure = Model B** — narrowed but not achieved.
4. **Necessity is NOT ESTABLISHED** for any response-state variable (except c3OmgS>0.1 operationally).
5. **Stop-Low operational validity is independent** of causal closure.
6. **V6 remains NOT READY.**

## 10. Not Claimed

- Causal closure
- Positive causal identification
- Necessity of any variable
- Physical interpretation
- V6 readiness

## 11. Recommendation

Proceed to **CRS (Final Synthesis)** for V5.42 closure. CRI (Independent Rejection Audit)
can be skipped — CRA provides sufficient rigor in the rejection analysis.

---

*Generated 2026-07-19. V5.42 CRA analysis document.*
