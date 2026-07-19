# TRM V5.40 Causal Topology Analysis

**Version:** 1.0 | **Date:** 2026-07-19
**Suite:** CTA
**Status:** COMPLETE

---

## 1. Summary

CTA_01 analyzed 728 profiles across 7 perturbation families (P1–P6 + low/high),
150 seeds × 6 N values, to determine whether the weak directional c3OmegaShift
signals from CTE are robust causal hints or perturbation-pattern artifacts.

**Result: The signals are artifacts.**
- Overall correct rate: **52.3%** (near chance)
- Effect-to-noise ratio: **0.886** (below significance)
- Best perturbation: P3 at 56.7% (still weak)
- Absorption is direction-invariant
- Signals concentrated in negative-baseline c3OmgS profiles (63.8%)
- High-baseline profiles show reverse pattern (36.1%)
- Classification: **Model C — Perturbation-pattern artifact (not causal)**

---

## 2. Directionality Robustness

| Perturbation | N | %Correct | %Wrong | MeanDelta | StdDelta |
|:-------------|--:|---------:|-------:|----------:|---------:|
| P1_LowK | 104 | 52.9 | 44.2 | -0.1009 | 0.9927 |
| P1_HighK | 104 | 47.1 | 51.0 | 0.1300 | 0.9339 |
| P2_Omega | 104 | 48.1 | 49.0 | -0.0578 | 0.8874 |
| P3_Rebound | 104 | **56.7** | 41.3 | 0.1440 | 1.0361 |
| P4_DTail | 104 | 48.1 | 51.0 | -0.0537 | 0.9412 |
| P5_Combined | 104 | 52.9 | 44.2 | 0.0675 | 1.1155 |
| P6_Mismatch | 104 | 51.9 | 45.2 | -0.0592 | 0.9867 |

**Key observation:** Standard deviation (0.89–1.12) is 6–20× larger than mean delta
(0.05–0.14). The signal is dominated by noise. P3 (rebound-aligned) has the highest
correct rate at 56.7% — only 6.7 percentage points above chance.

---

## 3. Stratified Directionality

### By N

| N | Profiles | %Correct | %Wrong | Mean\|Delta\| |
|--:|---------:|---------:|-------:|-------------:|
| 65 | 77 | 52.0 | 48.0 | 0.2819 |
| 66 | 49 | 56.2 | 43.8 | 0.1662 |
| 67 | 91 | 46.0 | 54.0 | 0.3401 |
| 70 | 196 | **58.5** | 41.5 | 0.6686 |
| 72 | 203 | 47.5 | 52.5 | 0.8135 |
| 75 | 112 | 53.6 | 46.4 | 1.0866 |

No consistent N-dependence. N=70 highest (58.5%), N=67 lowest (46.0% — below chance).

### By Baseline c3OmgS Band

| Band | Profiles | %Correct | %Wrong | Mean\|Delta\| |
|:-----|---------:|---------:|-------:|-------------:|
| Neg (< -0.05) | 308 | **63.8** | 36.2 | 0.6214 |
| NearZero (-0.05–0) | 70 | **65.7** | 34.3 | 0.1417 |
| LowPos (0–0.05) | 105 | 49.5 | 50.5 | 0.2605 |
| MidPos (0.05–0.15) | 28 | 35.7 | 64.3 | 0.6513 |
| High (> 0.15) | 133 | 36.1 | 63.9 | 0.7743 |

**Strong baseline-dependence.** Negative-baseline profiles show 63.8% correct rate.
High-baseline profiles show 36.1% (significant reversal). This suggests the directional
signal is a **baseline artifact**: perturbation direction interacts with the sign of
c3OmgS, not with any causal mechanism.

### By lambda1 Band

| lambda1 | Profiles | %Correct | %Wrong |
|:--------|---------:|---------:|-------:|
| [0.90, 0.95) | 113 | 52.8 | 47.2 |
| [0.95, 1.00) | 118 | 55.2 | 44.8 |
| [1.00, 1.05) | 105 | 45.5 | 54.5 |
| [1.05, 1.10) | 89 | 54.0 | 46.0 |

No lambda1-dependent pattern. All bands near 50%.

### By Absorption Rate

| absRate | Profiles | %Correct | %Wrong |
|:--------|---------:|---------:|-------:|
| [0.00, 0.01) | 380 | 53.8 | 46.2 |
| [0.01, 0.02) | 28 | 50.0 | 50.0 |
| [0.05, 1.00) | 312 | 50.3 | 49.7 |

Absorption rate does not predict directionality. All bands near chance.

---

## 4. Correct vs Wrong Profile Comparison

| Metric | Correct | Wrong | Diff | Significant? |
|:-------|--------:|------:|-----:|:------------:|
| lambda1 | 0.9538 | 0.9628 | 0.009 | No |
| **rebMagnitude** | **0.318** | **0.493** | **0.175** | **Yes** |
| omDist | 0.6716 | 0.6737 | 0.002 | No |
| Omega T1 | 1.3145 | 1.3014 | 0.013 | No |
| **d_tail** | **0.540** | **0.465** | **0.075** | **Yes** |
| kSensitivity | 0.1618 | 0.1513 | 0.011 | No |
| absRate | 0.0576 | 0.0612 | 0.004 | No |
| **csBase** | **-0.125** | **+0.208** | **0.333** | **Yes** |

Three metrics distinguish correct from wrong profiles:
1. **csBase** — correct profiles have negative baseline c3OmgS (-0.125 vs +0.208)
2. **rebMagnitude** — correct profiles have lower rebound (0.318 vs 0.493)
3. **d_tail** — correct profiles have slightly higher d_tail (0.540 vs 0.465)

**Interpretation:** Directional "correctness" is driven by baseline state, not by
perturbation mechanism. Profiles with negative c3OmgS tend to shift in the predicted
direction; profiles with positive c3OmgS tend to shift opposite. This is a sign-dependent
artifact of the perturbation pattern, not a causal lever.

---

## 5. Absorption Topology Classification

| Metric | Value |
|:-------|------:|
| Mean absorption rate | 0.0592 ± 0.0659 |
| Correct-direction absorption | 0.0576 |
| Wrong-direction absorption | 0.0612 |

**Classification: Model D — Perturbation-invariant restoration (direction-invariant).**

Absorption rates are nearly identical for correct and wrong profiles (0.058 vs 0.061).
The attractor absorbs all perturbation patterns equally, regardless of whether the
resulting c3OmgS shift aligns with prediction.

---

## 6. Weak-Signal Validity Classification

| Metric | Value |
|:-------|------:|
| Overall correct rate | 52.3% |
| Overall wrong rate | 47.7% |
| Effect-to-noise ratio | 0.886 |
| Mean \|csDelta\| | 0.6575 |

**Classification: Model C — Perturbation-pattern artifact (not causal).**

Criteria met:
- Correct rate near chance (52.3% vs 50% expected)
- Effect-to-noise ratio < 1.0 (0.886)
- Standard deviation >> mean delta for all perturbations
- Strong baseline-dependence (negative profiles = 63.8%, high = 36.1%)
- No perturbation-family advantage (all near 50%)

The weak directional signals from CTE are best explained as baseline-state artifacts:
the sign of baseline c3OmgS interacts with perturbation direction to produce apparent
directional consistency that disappears under stratification.

---

## 7. Gate Summary

| Gate | Description | Status |
|:-----|:------------|:------:|
| A | Directional signal characterized | REACHED |
| B | State conditions identified | REACHED |
| C | Absorption topology classified | REACHED (Model D) |
| D | Weak signal validity classified | REACHED (Model C — artifact) |
| E | Stop-Low safety preserved | REACHED |
| F | Causal closure improved | **NOT REACHED** |
| G | Causal closure still blocked | **REACHED** |
| H | V6 still not ready | REACHED |
| I | Ready for CTS | REACHED |

---

## 8. Supported Findings

1. **Weak directional signals from CTE are perturbation-pattern artifacts.**
   Overall correct rate = 52.3%, near chance. Effect-to-noise ratio < 1.0.

2. **Directionality is baseline-state dependent.**
   Negative-baseline c3OmgS profiles show 63.8% correct; high-baseline show 36.1%.
   This is a sign artifact, not a causal mechanism.

3. **Absorption is direction-invariant.**
   Model D — perturbation-invariant restoration. Correct and wrong profiles
   have identical absorption rates (~0.058).

4. **No perturbation family provides causal leverage.**
   Best family (P3) achieves only 56.7% correct. Standard deviations are
   6–20× larger than mean effects.

5. **Causal closure remains blocked.**
   Gate F NOT REACHED. Gate G REACHED.

6. **Stop-Low remains safe.**
   No policy violation. No threshold artifacts.

7. **V6 remains NOT READY.**

## 9. Conditional Findings

- The baseline-dependence finding (negatives = 63.8%, high = 36.1%) is conditional
  on the perturbation patterns tested and may not generalize to untested patterns.
- The artifact classification assumes the perturbation families tested are
  representative of attractor-compatible directions.

## 10. Not Claimed

- Causal closure
- Causal control of c3OmegaShift
- V6 readiness
- Physical interpretation
- Deterministic perturbation leverage
- Universal absorption model

## 11. Conclusion

CTA resolves the ambiguity from CTE. The weak directional c3OmegaShift signals
observed in CTE (55%+ consistency) collapse to near-chance (52.3%) under detailed
stratification analysis. The signal is a **perturbation-pattern artifact** driven
by baseline c3OmgS sign interaction, not by causal leverage.

**The attractor absorbs all perturbation families equally and the residual c3OmgS
shifts are dominated by baseline state, not perturbation mechanism.**

V5.40 has now established:
- V5.39 absorption reproduced (CTE)
- Aligned perturbations do not survive better (CTE Gate D)
- Weak directional signals are artifacts (CTA Gate D = Model C)
- Causal closure remains blocked (CTA Gate G)

## 12. Recommendation

Proceed to **CTS (Final Synthesis)** for V5.40 closure. The evidence is sufficient:
causal closure cannot be achieved through attractor-compatible perturbation patterns.
The attractor's absorption is robust and direction-invariant.

V5.41 should pivot from perturbation-based causal testing to a different approach —
possibly attractor structure characterization or acceptance of causal opacity as
a fundamental feature of the RecoverFP system.

---

*Generated 2026-07-19. V5.40 CTA analysis document.*
