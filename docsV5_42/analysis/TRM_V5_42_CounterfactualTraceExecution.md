# TRM V5.42 Counterfactual Trace Execution — Analysis

**Version:** 1.0 | **Date:** 2026-07-19
**Suite:** CRE
**Status:** COMPLETE

---

## 1. Summary

CRE_01 constructed 6 matched-profile comparison families (M1–M6) using 61 natural-variation
profiles across N=[65,66,67,70,72,75]. Tested 4 sufficiency claims and built a rejection
inventory of 7 claims.

**Key finding: All four single-variable sufficiency claims are REJECTED.** Near-identical
profiles (matched on lambda1, omDist, rebMagnitude) produce divergent c3OmgS outcomes
in 5/11 cases. No single variable — including c3OmgS itself — is sufficient to determine
rescue. All 8 gates reached.

---

## 2. Counterfactual Divergence by Matched Family

### M1 — Match on N + Cohort
- 19 matched pairs across 30 N×cohort cells
- 5/30 cells show divergent c3 outcomes
- 2/30 cells show divergent rescue outcomes

### M2 — Match on lambda1 Band

| lambda1 Band | n | hiC3 | loC3 | c3Div |
|:-------------|--:|-----:|-----:|------:|
| [0.90, 0.94) | 9 | 3 | 6 | +1.05 |
| [0.94, 0.97) | 8 | 2 | 6 | +2.15 |
| [0.97, 1.00) | 6 | 4 | 2 | +0.64 |
| [1.00, 1.10) | 16 | 6 | 10 | +1.23 |

**All lambda1 bands contain both high and low c3OmgS outcomes.**
Same lambda1 → different c3OmgS. Sufficiency REJECTED.

### M3 — Match on c3OmgS Band — Rescued vs Non-Rescued

| c3OmgS Band | n | Rescued | Not Rescued | Key Diff |
|:------------|--:|--------:|------------:|:---------|
| Neg | 25 | 0 | 25 | — |
| NearZero | 8 | 0 | 8 | — |
| LowPos | 8 | 0 | 8 | — |
| MidPos | 3 | 1 | 2 | lamR=0.983, lamN=0.973 |
| High | 9 | 4 | 5 | rebR=+0.80, rebN=-0.17 |

**5/9 high-c3 profiles are NOT rescued.** c3OmgS sufficiency for rescue REJECTED.

### M4 — Match on Omega Proximity

| omDist Band | n | hiC3 | loC3 |
|:------------|--:|-----:|-----:|
| [0.0, 0.3) | 3 | 2 | 1 |
| [0.3, 0.6) | 3 | 1 | 2 |
| [0.6, 1.0) | 55 | 16 | 39 |

All bands have both outcomes. sufficiency REJECTED.

### M5 — Match on rebMagnitude Band

| reb Band | n | hiC3 | loC3 |
|:---------|--:|-----:|-----:|
| [-5.0, -0.5) | 8 | 5 | 3 |
| [-0.5, 0.0) | 13 | 9 | 4 |
| [0.0, 0.5) | 13 | 3 | 10 |
| [0.5, 5.0) | 27 | 2 | 25 |

All bands have both outcomes. sufficiency REJECTED.

### M6 — Near-Identical Profiles
- **11 pairs** matched on N, lamDelta<0.02, omDistDelta<0.1, rebDelta<0.3
- **5/11 pairs diverge** on c3OmgS outcome (>0.1 vs ≤0.1)
- **45.5% divergence rate** among near-identical starting conditions

**This is the strongest counterfactual evidence:** profiles with virtually identical
lambda1, omDist, and rebMagnitude produce opposite c3OmgS outcomes. The variables
that should predict c3OmgS do not uniquely determine it.

---

## 3. Sufficiency Rejection

| Claim | Result | Evidence |
|:------|:------:|:---------|
| lambda1 sufficient for c3OmgS | **REJECTED** | Same lam → different c3 (all bands) |
| rebMagnitude sufficient for c3OmgS | **REJECTED** | Same reb → different c3 (all bands) |
| omDist sufficient for c3OmgS | **REJECTED** | Same omDist → different c3 (all bands) |
| c3OmgS sufficient for rescue | **REJECTED** | High c3 → 14/19 non-rescued |

**No single response-state variable is sufficient to determine c3OmgS or rescue.**

---

## 4. Natural Variation Stratification

| Tercile | Mean c3OmgS | Rescue % |
|:--------|------------:|---------:|
| Low lambda1 | -0.158 | 0.0 |
| High lambda1 | +0.149 | 19.0 |
| Low rebMagnitude | +0.631 | 10.0 |
| High rebMagnitude | -0.479 | 9.5 |
| Low omDist | +0.143 | 15.0 |
| High omDist | -0.056 | 4.8 |

Natural variation confirms the diagnostic hierarchy (lambda1 separates c3OmgS and rescue
rates) but does not establish sufficiency. Even in the strongest tercile (high lambda1),
only 19% of profiles are rescued.

---

## 5. Rejection Inventory

| # | Claim | Status |
|--:|:------|:------:|
| 1 | lambda1 sufficient for c3OmgS | **REJECTED** |
| 2 | rebMagnitude sufficient for c3OmgS | **REJECTED** |
| 3 | omDist sufficient for c3OmgS | **REJECTED** |
| 4 | c3OmgS sufficient for rescue | **REJECTED** |
| 5 | Full gain chain causal closure | **REJECTED** (V5.35) |
| 6 | Perturbation-based causality | **REJECTED** (V5.38–V5.40) |
| 7 | Single-variable causal driver | **REJECTED** (all fail sufficiency) |

---

## 6. Operational Consistency

| Stratum | Profiles | Rescues |
|:--------|---------:|--------:|
| A (≤0.1) | 42 | **0** |
| B (>0.1) | 19 | **5** |

**Stop-Low: UNCHANGED.** Operational validity preserved.

---

## 7. Gate Summary

| Gate | Description | Status |
|:-----|:------------|:------:|
| A | Matched traces built | REACHED |
| B | Counterfactual divergences observed | REACHED (5/11 near-identical diverge) |
| C | Sufficiency claim rejected | REACHED (4 claims) |
| D | Natural variation replicated | REACHED |
| E | Rejection inventory created | REACHED (7 claims) |
| F | Stop-Low unchanged | REACHED |
| G | Causal closure still blocked | REACHED |
| H | V6 still not ready | REACHED |

---

## 8. Supported Findings

1. **Near-identical profiles produce divergent c3OmgS outcomes** (5/11 pairs, 45.5%).
2. **No single response-state variable is sufficient** for c3OmgS or rescue.
3. **lambda1, rebMagnitude, omDist, and c3OmgS all fail sufficiency tests.**
4. **Diagnostic hierarchy is confirmed** (natural variation stratification)
   but does not imply causal sufficiency.
5. **Stop-Low operational validity is unchanged.**
6. **V6 remains NOT READY.**

## 9. Not Claimed

- Causal closure
- V6 readiness
- Physical interpretation
- Positive causal identification
- Necessary condition identification

## 10. Recommendation

Proceed to **CRA (Causal Rejection Analysis)** for detailed interpretation of
rejection evidence, or directly to **CRS (Final Synthesis)** if CRE provides
sufficient evidence for V5.42 closure.

---

*Generated 2026-07-19. V5.42 CRE analysis document.*
