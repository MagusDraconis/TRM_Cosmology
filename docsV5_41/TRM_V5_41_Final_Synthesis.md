# TRM V5.41 Final Synthesis — Causal Test Design Under Attractor Absorption

**Version:** 1.0 | **Date:** 2026-07-19
**Branch:** `feature/v5.41-causal-test-design-under-attractor-absorption`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.41-causal-test-design-under-attractor-absorption` |
| Base | V5.40 COMPLETE |
| Suites | TDP, TDE, TDA |
| V5.41 tests | 5 (3 TDP + 1 TDE + 1 TDA) |
| Cumulative tests | 2843 passed, 0 failed |
| Commits | `bc906ad` (init), `2a43f6d` (TDE), `0ae4109` (TDA) |

---

## 2. Research Question

**If perturbation-based causal testing is blocked by attractor absorption, what
causal test designs remain valid?**

Answer: Direct perturbation is INVALIDATED. Natural variation, counterfactual
trace, and causal rejection are viable non-perturbation alternatives — but they
provide diagnostic and observational evidence only, not causal closure.
The strongest methodology is **causal rejection** (identifying what fails).
Stop-Low is operationally sufficient without causal closure.

---

## 3. Suite Summaries

### TDP — Protocol (3 tests)
Frozen constraints: no M3++ modification, no Stop-Low modification, no new
variables, no V6 derivations, no physical interpretation. Causal test taxonomy
defined (Classes A–H).

### TDE — Execution (1 test, 33s)
Evaluated D1–D7 causal test designs with natural-variation grounding data (52 profiles).
D1 (perturbation) REJECTED. D2 (natural variation) FEASIBLE. D3 (invariance) WEAK/N-dependent.
D4 (mediation) PARTIAL. D5 (counterfactual trace) FEASIBLE. D6 (causal rejection) RECOMMENDED.
D7 (operational sufficiency) ACCEPTED. Central insight: predictive validity ≠ causal closure.

### TDA — Analysis (1 test, 42s)
Classified identifiability of causal claims (50 profiles, 6N). Natural variation = STABLE
DIAGNOSTIC RELATION. Invariance = WEAKLY INVARIANT (std=0.224). Mediation = TEMPORALLY
AMBIGUOUS. Counterfactual trace = OBSERVATIONAL TRACE. 8 causal claims REJECTED.
Stop-Low = OUTCOME-VALIDATED.

---

## 4. Supported Findings

1. **Direct perturbation is INVALIDATED** for causal closure under attractor absorption.
2. **Natural variation supports diagnostic stratification** but not causal identification.
   lambda1 terciles separate c3OmgS (delta=0.27) and rescue rates (delta=16.7pp).
3. **Invariance is limited by N-dependence.** Correlation std=0.224; N=70 is outlier.
4. **Mediation is limited** by lack of clean temporal ordering in current pipeline.
5. **Counterfactual trace identifies outcome-associated patterns** (rebMag diff=-1.94)
   but is observational, not causal.
6. **Causal rejection tests are the strongest current methodology.**
   8 causal claims REJECTED by V5.35–V5.40 evidence.
7. **Stop-Low is operationally sufficient without causal closure.**
   0 rescues in stratum A, all rescues in stratum B.
8. **Predictive validity and causal closure must remain separate.**
   Stop-Low has the former without the latter.
9. **V6 remains NOT READY.**

---

## 5. Causal-Test Design Verdict

| Design | Verdict | Note |
|:-------|:--------|:-----|
| D1 — Direct perturbation | **REJECT** | All families absorbed 87–99% |
| D2 — Natural variation | CONSIDER | Diagnostic only, not causal |
| D3 — Invariance | CONSIDER | Weak, N-dependent |
| D4 — Mediation | PARTIAL | Temporally ambiguous |
| D5 — Counterfactual trace | CONSIDER | Observational trace |
| D6 — Causal rejection | **RECOMMEND** | Strongest methodology |
| D7 — Operational sufficiency | **ACCEPT** | Policy valid without causality |

---

## 6. Identifiability Verdict

**Identifiable:**
- Diagnostic value of lambda1, rebMagnitude, omDist
- Outcome-associated variables (rebMag strongly separates quartiles)
- Policy validity without causal closure (Stop-Low)
- Rejected causal claims (8 claims rejected)

**Not identifiable:**
- Causal direction of any current variable
- N-invariant causal law (variable magnitude across N)
- Temporal causal ordering (all vars at same epoch)
- Mechanism of c3OmegaShift formation
- V6 geometry (length, space, velocity, c)
- Physical interpretation of any TRM observable

---

## 7. Predictive vs Causal Separation

**Predictive validity:**
- c3OmegaShift > 0.1 — operational predictor
- Stop-Low: c3OmgS ≤ 0.1 → stop, c3OmgS > 0.1 → continue
- Validated by outcome: zero damage, all rescues preserved
- Reproducible (V5.32), efficient (V5.33), generalized (V5.30)

**Causal closure:**
- NOT ESTABLISHED
- 8 causal claims rejected
- No perturbation pattern succeeds
- Attractor absorbs all tested perturbations uniformly

**Operational conclusion:**
Stop-Low does NOT require causal closure to remain valid as an operational policy.
The policy is validated by OUTCOME, not by mechanism.

**V6 conclusion:**
V6 DOES require mechanism and remains NOT READY.
No geometry bridge has been established.

---

## 8. Rejected / Weakened Causal Claims

1. Direct K/lambda1 perturbation sufficiency — V5.38 RII, V5.39 AAE
2. Robust Omega-proximity dose-response — V5.37 OPE (downgraded)
3. Attractor-aligned perturbation superiority — V5.40 CTE Gate D FAILED
4. Full gain-chain causal closure — V5.35 MCA (falsified)
5. c3OmegaShift causal sufficiency — V5.35, V5.40 CTA (artifact)
6. N-invariant causal law — V5.41 TDA (std=0.224)
7. V6 readiness — all versions V5.34–V5.41
8. Physical interpretation — never claimed

---

## 9. Not Claimed

V5.41 does NOT claim:
- Causal closure
- Deterministic rescue
- Universal adaptive control
- Universal Stop-Low validity
- c3OmegaShift causal sufficiency
- lambda1 causality
- rebMagnitude causality
- Physical interpretation
- Length, space, velocity, or c derivation
- V6 readiness

---

## 10. Final V5.41 Conclusion

V5.41 establishes that under strong RecoverFP attractor absorption, direct
perturbation is not a valid route to causal closure.

The strongest useful methodology is **causal rejection**: identifying which
causal claims fail. Eight claims have been rejected by V5.35–V5.41 evidence.

Natural variation, invariance, mediation, and counterfactual trace can support
diagnostic and observational analysis, but not full causal closure under
current artifacts. The primary limitation is temporal ambiguity — all response-state
variables are measured at the same pipeline epoch.

**The central methodological contribution of V5.41 is the separation of predictive
validity from causal closure.** Stop-Low is operationally valid without causal
understanding. This removes the pressure to achieve causal closure for operational
purposes and allows the project to proceed with diagnostic and predictive tools
while acknowledging causal limits.

V6 remains NOT READY.

---

## 11. Recommended V5.42

**Branch:** `feature/v5.42-counterfactual-trace-and-natural-variation-causal-rejection`

**Central question:** Can counterfactual trace and natural variation identify stronger
causal rejection boundaries without relying on perturbation?

**Purpose:** Move from causal-test methodology to non-perturbative causal rejection
analysis using counterfactual trace and natural variation.

**Planned suites:** CRP (Protocol), CRE (Execution), CRA (Analysis), CRI (Audit), CRS (Synthesis).

**Core questions:**
1. Which causal claims can be rejected using counterfactual trace?
2. Which response-state variables remain diagnostic under matched-profile comparison?
3. Can matched profiles reveal why c3OmgS differs without perturbation?
4. Does natural variation strengthen or weaken the diagnostic hierarchy?
5. Can causal closure be narrowed by eliminating impossible causal paths?
6. Does Stop-Low remain operationally sufficient?
7. Does V6 remain NOT READY?

**Expected caution:** V5.42 is still NOT V6. Do not investigate length, space, velocity, or c.

---

## 12. Suite Reference

| Suite | Tests | Status |
|:------|------:|:------:|
| TDP | 3 | COMPLETE |
| TDE | 1 | COMPLETE |
| TDA | 1 | COMPLETE |
| TDI | — | SKIPPED (TDA subsumes) |
| TDS | — | THIS DOCUMENT |
| **Total** | **5** | **0 failed** |

---

*Generated 2026-07-19. Authoritative V5.41 final synthesis.*
