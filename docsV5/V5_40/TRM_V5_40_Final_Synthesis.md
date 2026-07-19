# TRM V5.40 Final Synthesis — Causal Closure and Attractor Topology

**Version:** 1.0 | **Date:** 2026-07-19
**Branch:** `feature/v5.40-causal-closure-and-attractor-topology`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.40-causal-closure-and-attractor-topology` |
| Base | V5.39 COMPLETE |
| Suites | CTP, CTE, CTA |
| V5.40 tests | 5 (3 CTP + 1 CTE + 1 CTA) |
| Cumulative tests | 2838 passed, 0 failed |
| Commits | `c3f5741` (init), `61514ae` (CTE), `8e1a955` (CTA) |

---

## 2. Research Question

**If the attractor resists direct state perturbations, can causal closure be tested
by perturbing along attractor-compatible directions instead of fighting restoration?**

Answer: **No.** Attractor-aligned perturbations do not survive better than mismatch
or direct K-scaling. All perturbation families are absorbed 87–99% before c3OmegaShift.
Weak directional signals collapse under stratification and are best explained as
baseline-state / perturbation-pattern artifacts.

---

## 3. Suite Summaries

### CTP — Protocol (3 tests)
Frozen constraints: no M3++ modification, no Stop-Low modification, no new variables,
no V6 derivations, no physical interpretation. "Attractor topology" restricted to
internal response-state structure.

### CTE — Execution (1 test, 1m 46s)
8 perturbation families (P0–P6 + low/high) across N=[65,66,67,70,72,75], 100 seeds.
V5.39 absorption reproduced (lambda1 delta = 0.0231). Model A — early K-state absorption.
All families absorbed 87–99% before c3OmgS. Aligned perturbations do NOT survive
better (Gate D FAILED). Weak c3OmgS directional signals found (55%+ consistency,
csDelta 0.001–0.035). Stop-Low safe.

### CTA — Analysis (1 test, 1m 58s)
728 profiles, stratified by N, c3OmgS band, lambda1 band, absorption rate. Weak signals
collapse: overall correct rate = 52.3% (near chance). Effect-to-noise ratio = 0.886
(< 1.0). Baseline-state artifact discovered: negative c3OmgS = 63.8% correct, high
c3OmgS = 36.1% correct. Absorption is direction-invariant (Model D). Weak signals
classified as Model C — perturbation-pattern artifact. Causal closure remains BLOCKED.

---

## 4. Supported Findings

1. **Direct K/lambda1 perturbation absorption is reproduced** (V5.39 → V5.40).
2. **Perturbation absorption occurs early** in K-state / lambda1 response (Model A).
3. **All tested perturbation families are strongly absorbed** before c3OmegaShift (87–99%).
4. **Attractor-aligned perturbations do not outperform mismatched perturbations.**
   Gate D FAILED — aligned is not superior.
5. **Weak c3OmegaShift directionality is not robust under stratification.**
   Collapses from 55%+ to 52.3% overall.
6. **Residual directionality is a baseline-state / perturbation-pattern artifact.**
   Negative c3OmgS = 63.8%, high c3OmgS = 36.1% (reversal).
7. **Absorption is direction-invariant** (Model D). Correct and wrong profiles
   have identical absorption rates (~0.058).
8. **Stop-Low remains safe and unaffected.**
9. **Causal closure remains blocked.** Gate F NOT REACHED, Gate G REACHED.
10. **V6 remains NOT READY.**

---

## 5. Absorption Topology Result

**Final classification: Model D — Perturbation-invariant restoration**
(with early K-state absorption, Model A).

The attractor absorbs direct, aligned, combined, and mismatched perturbations
with similar efficiency. There is no "weak spot" or "aligned advantage" in the
absorption topology. This explains why strong diagnostic variables (lambda1,
rebMagnitude, omDist) resist direct causal manipulation: the attractor
protects its internal state coordinates uniformly against all perturbation
patterns tested.

---

## 6. Weak Signal Downgrade

| Stage | Finding |
|:------|:--------|
| CTE | Weak directional effects observed (55%+ consistency across 7 families) |
| CTA | Effects collapse under stratification (52.3% overall, near chance) |
| CTA | Explained as baseline-state / perturbation-pattern artifact |
| **Final** | **Weak directional signal is NOT accepted as causal evidence** |

The trajectory from CTE to CTA demonstrates the importance of stratification
analysis: apparent directional consistency can be an artifact of baseline-state
interaction with perturbation sign.

---

## 7. Predictive / Diagnostic / Causal Distinction

**Predictive:**
- c3OmegaShift > 0.1 — strongest operational predictor
- Stop-Low policy: c3OmgS ≤ 0.1 → stop, c3OmgS > 0.1 → continue

**Diagnostic hierarchy (V5.38):**
- lambda1 (|corr| ≈ 0.618) > rebMag (0.528) > omDist (0.518)

**Causal closure:**
- NOT established
- Attractor absorption explains why diagnostic markers resist direct manipulation
- All perturbation families absorbed before c3OmgS
- No perturbation pattern provides causal leverage

---

## 8. Weakened Findings

- Attractor-aligned perturbations as superior causal test (Gate D FAILED)
- Weak c3OmegaShift directional signal as causal evidence (downgraded to artifact)
- Simple perturbation route to causal closure (all routes blocked)
- lambda1 perturbation causality (not established)
- Omega-proximity causal scaling (downgraded V5.37)
- Full causal closure (blocked)
- V6 readiness (not ready)

---

## 9. Not Claimed

V5.40 does NOT claim:
- Deterministic rescue
- Full causal closure
- Universal adaptive control
- Physical interpretation
- Physical topology
- Geometric topology
- Length, space, velocity, or c derivation
- V6 readiness
- c3OmegaShift causal sufficiency
- lambda1 causal control
- Attractor topology as physical structure

---

## 10. Final V5.40 Conclusion

V5.40 shows that the RecoverFP attractor broadly absorbs perturbations before
they can produce stable causal control of c3OmegaShift.

Attractor-compatible perturbations did not survive better than mismatched or
direct perturbations. The weak directional c3OmegaShift signals found during
execution collapse under stratified analysis and are best interpreted as
baseline-state / perturbation-pattern artifacts.

Therefore V5.40 strengthens the conclusion that the present response-state
hierarchy is diagnostically useful but not causally closed. The attractor
protects its internal state coordinates uniformly, and no simple perturbation
pattern can penetrate this protection.

Stop-Low remains operationally valid. V6 remains NOT READY.

---

## 11. Recommended V5.41

**Branch:** `feature/v5.41-causal-test-design-under-attractor-absorption`

**Central question:** If direct and attractor-aligned perturbations are absorbed,
what causal test designs remain valid under strong attractor restoration?

**Purpose:** Move from perturbation attempts to causal-test methodology design.

**Planned suites:** TDP (Protocol), TDE (Execution), TDA (Analysis), TDI (Audit), TDS (Synthesis).

**Core questions:**
1. Which causal tests are invalidated by attractor absorption?
2. Can causal influence be tested using natural variation instead of perturbation?
3. Can causal closure be approached through invariance, mediation, or counterfactual trace?
4. What evidence would establish causality under strong restoration?
5. Are current variables sufficient for causal testing?
6. Does Stop-Low require causal closure, or only predictive stability?
7. Does V6 remain NOT READY?

**Expected caution:** V5.41 is still NOT V6. Do not investigate length, space, velocity, or c.

---

## 12. Suite Reference

| Suite | Tests | Status |
|:------|------:|:------:|
| CTP | 3 | COMPLETE |
| CTE | 1 | COMPLETE |
| CTA | 1 | COMPLETE |
| CTI | — | SKIPPED (CTA subsumes) |
| CTS | — | THIS DOCUMENT |
| **Total** | **5** | **0 failed** |

---

*Generated 2026-07-19. Authoritative V5.40 final synthesis.*
