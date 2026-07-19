# TRM V5.40 Causal Topology Execution — Analysis

**Version:** 1.0 | **Date:** 2026-07-19
**Suite:** CTE
**Status:** COMPLETE

---

## 1. Summary

CTE_01 tested 8 perturbation families (P0–P6 + low/high variants) across N=[65,66,67,70,72,75]
with 100 seeds each. The goal: determine whether attractor-compatible perturbations survive
better than direct K-scaling, and whether any perturbation direction exposes causal structure.

**Key finding:** All perturbation families produce directional c3OmegaShift effects
(>55% profile consistency within each family). However, effect sizes are small
(csDelta 0.001–0.035) and aligned perturbations do NOT survive better than mismatch.
V5.39 absorption is reproduced (lambda1 convergence). Stop-Low remains safe.

---

## 2. Baseline Reproduction

| Metric | P0 (Base) | P1_low | P1_high | MaxDelta |
|:-------|----------:|-------:|--------:|---------:|
| lambda1 T1 | 0.1541 | 0.1309 | 0.1772 | 0.0231 |
| c3OmgS | -0.0100 | -0.0183 | 0.0205 | 0.0306 |

**V5.39 absorption REPRODUCED.** lambda1 delta = 0.0231 (V5.39 had 0.0274).
K-scaling perturbation is absorbed at lambda1 stage. Gate A REACHED.

---

## 3. Perturbation Survival Analysis

All perturbation families produce large initial T1 lambda1 deviations (0.23–0.28
from P0 mean) but c3OmegaShift deviations are small (0.001–0.035):

| Perturbation | lamDelta T1 | csDelta T4 | Absorption Rate |
|:-------------|------------:|-----------:|----------------:|
| P1_lowK | 0.2346 | 0.0083 | 96.5% |
| P1_highK | 0.2808 | 0.0306 | 89.1% |
| P2_OmegaAlign | 0.2577 | 0.0014 | 99.5% |
| P3_ReboundAlign | 0.2593 | 0.0352 | 86.4% |
| P4_DTailAlign | 0.2729 | 0.0110 | 96.0% |
| P5_Combined | 0.2585 | 0.0315 | 87.8% |
| P6_Mismatch | 0.2577 | 0.0085 | 96.7% |

**All perturbations are heavily absorbed before reaching c3OmegaShift.**
The attractor filters out 87–99% of initial lambda1 perturbation by the time
c3OmgS is measured.

---

## 4. Absorption Topology Map

Absorption stage dominance (lambda1 convergence):

| Perturbation | T1→T2 | T2→T3 | T3→T4 | Dominant Stage |
|:-------------|------:|------:|------:|:---------------|
| P1_lowK | 0.0214 | 0.0208 | ~0 | T1→T2 |
| P1_highK | 0.0249 | 0.0186 | ~0 | T1→T2 |
| P2_OmegaAlign | 0.0137 | 0.0208 | ~0 | T2→T3 |
| P3_ReboundAlign | 0.0135 | 0.0224 | ~0 | T2→T3 |
| P4_DTailAlign | 0.0195 | 0.0183 | ~0 | T1→T2 |
| P5_Combined | 0.0141 | 0.0203 | ~0 | T2→T3 |
| P6_Mismatch | 0.0139 | 0.0242 | ~0 | T2→T3 |

**Classification: Model A — Early K-state absorption.**
Direct K-scaling (P1) absorbs fastest at T1→T2. Node-weighted perturbations
(P2, P3, P5, P6) shift absorption to T2→T3. T3→T4 convergence is near-zero
for all families — by T3 the attractor has fully restored lambda1.

Gates B and C REACHED.

---

## 5. Aligned vs Opposed Comparison

| Family | Mean lambda1 T4 survival delta |
|:-------|-------------------------------:|
| Aligned (P2/P3/P5) | 0.0166 |
| Mismatch (P6) | 0.0174 |
| Direct K-scale (P1) | 0.0231 |

**Unexpected result: Mismatch perturbations survive similarly to aligned.**
Aligned perturbations do NOT survive better — in fact, mismatch (P6) shows
slightly higher survival. Direct K-scaling (P1) produces the largest
persistent delta.

**Gate D: FAILED.** Attractor-aligned perturbation does not confer survival
advantage. The attractor absorbs all perturbation patterns with similar efficiency.

---

## 6. c3OmegaShift Directional Effect

All 7 perturbation families show directional consistency (>55% of profiles
shift in the same direction):

| Perturbation | c3OmgS mean | delta vs P0 | Directional |
|:-------------|------------:|------------:|:-----------:|
| P0 (baseline) | -0.0100 | — | — |
| P1_lowK | -0.0183 | -0.0083 | Yes |
| P1_highK | 0.0205 | +0.0306 | Yes |
| P2_OmegaAlign | -0.0087 | +0.0014 | Yes |
| P3_ReboundAlign | 0.0251 | +0.0352 | Yes |
| P4_DTailAlign | 0.0010 | +0.0110 | Yes |
| P5_Combined | 0.0215 | +0.0315 | Yes |
| P6_Mismatch | -0.0185 | -0.0085 | Yes |

**Gate E: REACHED.** All perturbation families produce directional c3OmegaShift
effects. However, effect sizes are small (0.001–0.035) and the directional
consistency threshold was only 55%.

**Critical caveat:** Directional consistency does not equal causal control.
The 55% threshold means 45% of profiles move in the opposite direction.
This is a weak directional signal, not a strong causal lever.

---

## 7. Stop-Low Safety Audit

| Metric | Count |
|:-------|------:|
| Low-stratum rescues | 0 |
| Missed rescues | 0 |

**Gate F: REACHED.** Stop-Low policy unaffected by all perturbation families.
No perturbation creates false rescues or suppresses genuine rescues.

---

## 8. Causal Closure Assessment

**Gate G: CONDITIONALLY REACHED.** A directional c3OmegaShift effect is found
across all perturbation families. This is an advance over V5.38 RII (which found
no directional signal) and V5.39 AAE (which only tested uniform K-scaling).

However, the effect is:
- Small (csDelta < 0.035)
- Weakly directional (55% threshold)
- Not stronger for aligned perturbations than mismatch
- Not practically useful for control

**Gate H: PARTIALLY.** Causal closure is still substantially blocked. The
directional signals are real but do not constitute a causal lever.

---

## 9. Gate Summary

| Gate | Description | Status |
|:-----|:------------|:------:|
| A | V5.39 absorption reproduced | REACHED |
| B | Absorption stage identified | REACHED |
| C | Absorption topology classified | REACHED (Model A) |
| D | Aligned survives better | FAILED |
| E | c3OmegaShift directional effect | REACHED |
| F | Stop-Low safety preserved | REACHED |
| G | Causal closure improved | CONDITIONAL |
| H | Causal closure still blocked | PARTIALLY |
| I | V6 still not ready | REACHED |

---

## 10. Supported Findings

1. **V5.39 absorption reproduced.** K-perturbation converges near lambda1 (Model A).
2. **All perturbation patterns are absorbed before c3OmgS.** 87–99% absorption rate.
3. **c3OmegaShift shows weak directional sensitivity to K-perturbation pattern.**
   Effect sizes are small (0.001–0.035) but directionally consistent (>55%).
4. **Attractor-aligned perturbations do NOT survive better than mismatch.**
   The attractor absorbs all perturbation patterns with similar efficiency.
5. **Stop-Low remains safe** under all tested perturbation families.
6. **V6 remains NOT READY.**

## 11. Conditional Findings

- c3OmegaShift directional effect is conditional on perturbation family and
  may not persist under broader N/cohort sweeps.
- The 55% directional threshold is minimal — results may be consistent with
  noise at stricter thresholds.

## 12. Not Claimed

- Causal closure
- Causal control of c3OmegaShift
- V6 readiness
- Physical interpretation
- Deterministic perturbation leverage

## 13. Interpretation

The CTE results paint a nuanced picture:

**The attractor absorbs ALL perturbation patterns.** Whether aligned with Omega
restoration, rebound, d-compression, or opposed — the attractor restores lambda1
by T3. There is no "weak spot" in the absorption topology.

**c3OmegaShift is weakly sensitive to perturbation direction.** While lambda1
converges, the residual perturbation that survives to c3OmgS is directionally
consistent within each perturbation family. This suggests c3OmgS aggregates
small residual effects that lambda1 convergence does not fully erase.

**This is NOT causal control.** The directional effects are small, weakly
directional, and not stronger for aligned perturbations. The attractor
remains the dominant force.

## 14. Recommendation

Proceed to CTA (Causal Topology Analysis) for detailed interpretation,
or directly to CTS (Final Synthesis) if CTE provides sufficient evidence
for V5.40 closure.

The key question for CTA: are the weak directional signals in c3OmgS
genuine causal residuals, or artifacts of perturbation pattern that
would not survive hostile audit?

---

*Generated 2026-07-19. V5.40 CTE analysis document.*
