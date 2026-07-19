# TRM V5.42 Final Synthesis — Counterfactual Trace and Natural Variation Causal Rejection

**Version:** 1.0 | **Date:** 2026-07-19
**Branch:** `feature/v5.42-counterfactual-trace-and-natural-variation-causal-rejection`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.42-counterfactual-trace-and-natural-variation-causal-rejection` |
| Base | V5.41 COMPLETE |
| Suites | CRP, CRE, CRA |
| V5.42 tests | 5 (3 CRP + 1 CRE + 1 CRA) |
| Cumulative tests | 2848 passed, 0 failed |
| Commits | `8a62c82` (init), `75b78e3` (CRE), `82de647` (CRA) |

---

## 2. Research Question

**Given that perturbation-based causal testing is invalidated, which causal
explanations can be rejected through matched-profile comparison and natural variation?**

Answer: **7 sufficiency claims REJECTED.** Near-identical measured profiles diverge
in c3OmgS (7/11 pairs, 63.6%). No single measured response-state variable is
sufficient for c3OmgS or rescue. Causal closure narrowed (Model B) but NOT achieved.
Stop-Low remains operationally valid without causal closure.

---

## 3. Suite Summaries

### CRP — Protocol (3 tests)
Frozen constraints. Success measured by rejection evidence, not positive causal
identification. Causal rejection methodology defined: counterfactual trace matching
+ natural variation stratification.

### CRE — Execution (1 test, 33s)
6 matched-profile families (M1–M6), 61 profiles. 5/11 near-identical pairs diverge.
All 4 tested single-variable sufficiency candidates fail (lambda1, rebMag, omDist,
c3OmgS for rescue). Stop-Low unchanged. All 8 gates reached.

### CRA — Analysis (1 test, 50s)
73 profiles, 100 seeds. Near-identical divergence strengthened to 7/11 (63.6%).
7 sufficiency claims consolidated as REJECTED. Necessity NOT ESTABLISHED for any
variable except c3OmgS>0.1 (operational). Causal closure = Model B (narrowed, not
achieved). 7 surviving explanations inventoried.

---

## 4. Supported Findings

1. **Counterfactual trace can reject sufficiency claims.**
2. **Near-identical measured profiles can diverge in c3OmegaShift** (7/11, 63.6%).
3. **Current measured variables do not uniquely determine c3OmgS or rescue.**
4. **No single response-state variable is sufficient for high c3OmgS.**
5. **c3OmgS is not sufficient for rescue** (18 high-c3 profiles not rescued).
6. **omegaPerK is not sufficient for rescue.**
7. **Full measured gain chain is not sufficient for rescue.**
8. **Causal search space is narrowed but not closed** (Model B).
9. **Stop-Low remains operationally valid.**
10. **V6 remains NOT READY.**

---

## 5. Causal Rejection Inventory

| # | Claim | Status | Evidence |
|--:|:------|:------:|:---------|
| 1 | lambda1 sufficient for c3OmgS | **REJECTED** | Same/near-same lam → divergent c3 |
| 2 | rebMagnitude sufficient for c3OmgS | **REJECTED** | Same/near-same reb → divergent c3 |
| 3 | omDist sufficient for c3OmgS | **REJECTED** | Same/near-same omDist → divergent c3 |
| 4 | c3OmgS sufficient for rescue | **REJECTED** | High-c3 profiles can fail rescue |
| 5 | omegaPerK sufficient for rescue | **REJECTED** | oPK does not uniquely determine rescue |
| 6 | Full gain chain sufficient for rescue | **REJECTED** | Chain variables don't uniquely determine rescue |
| 7 | Stop-Low A rescue impossible | NOT REJECTED | 0 observed A rescues (operational) |

---

## 6. Necessity Caution

V5.42 rejects **sufficiency** claims. It does NOT broadly reject **necessity.**

Necessity remains mostly unestablished. The only operational necessity supported:
all observed rescues occur in c3OmgS > 0.1 stratum. This is an operational finding,
not a universal proof.

| Variable | Necessity Status |
|:---------|:-----------------|
| lambda1 | NOT ESTABLISHED |
| rebMagnitude | NOT ESTABLISHED |
| omDist | NOT ESTABLISHED |
| c3OmgS (>0.1) | NOT REJECTED (operational) |
| omegaPerK | NOT ESTABLISHED |
| d_tail | NOT ESTABLISHED |

---

## 7. Remaining Explanation Space

Surviving possibilities (none asserted):

1. Hidden response-state variable not in current measurement set
2. Higher-order interaction among measured variables
3. Temporal ordering not captured by current matched variables
4. Attractor path dependence
5. Phase/path history not captured in current profile state
6. Deterministic sensitivity to unmeasured profile features
7. Measurement granularity limits

All single-variable sufficiency paths are CLOSED. Remaining explanations require
multi-variable, temporal, or unmeasured-state approaches — not testable with
current variables.

---

## 8. Predictive / Operational / Causal Distinction

| Layer | Status | Detail |
|:------|:------:|:-------|
| **Predictive** | VALID | c3OmgS > 0.1 remains strongest predictor |
| **Operational** | VALID | Stop-Low: c3OmgS≤0.1→stop, >0.1→continue |
| **Causal** | NOT CLOSED | Model B — narrowed but not achieved |
| **Rejection** | EXTENSIVE | 7 sufficiency claims REJECTED |

---

## 9. Weakened Findings

- lambda1 as sufficient cause — REJECTED
- rebMagnitude as sufficient cause — REJECTED
- Omega proximity as sufficient cause — REJECTED
- c3OmgS as sufficient for rescue — REJECTED
- omegaPerK as sufficient for rescue — REJECTED
- Measured gain chain as sufficient causal explanation — REJECTED
- Any claim of causal closure from current variables — NOT ACHIEVED
- V6 readiness — NOT READY

---

## 10. Not Claimed

V5.42 does NOT claim:
- Causal closure
- Deterministic rescue
- Universal adaptive control
- Universal Stop-Low validity
- Physical interpretation
- Length, space, velocity, or c derivation
- V6 readiness
- Impossibility of low-stratum rescue under all future operators
- Necessity proof for lambda1, rebMagnitude, omDist, omegaPerK, or full gain chain

---

## 11. Final V5.42 Conclusion

V5.42 uses counterfactual trace and natural variation to reject unsupported
causal sufficiency claims. Near-identical profiles can diverge strongly in
c3OmegaShift (7/11 pairs, 63.6%), showing that the currently measured
response-state variables do not uniquely determine c3OmegaShift or rescue.

The result narrows the causal search space but does not identify a positive
causal mechanism. All single-variable sufficiency paths are CLOSED. The
remaining explanations require multi-variable, temporal, or unmeasured-state
approaches.

Stop-Low remains operationally valid. Causal closure remains unresolved.
V6 remains NOT READY.

---

## 12. Recommended V5.43

**Branch:** `feature/v5.43-hidden-response-state-and-temporal-trace-discovery`

**Central question:** If measured same-state profiles diverge in c3OmegaShift,
what unmeasured or temporal trace factor separates them?

**Purpose:** Move from causal rejection to hidden-response-state / temporal-trace
discovery. Use existing artifacts to investigate what differs between divergent
near-identical profiles.

**Planned suites:** HTP (Protocol), HTE (Execution), HTA (Analysis), HTI (Audit), HTS (Synthesis).

**Core questions:**
1. What differs between near-identical profiles that diverge in c3OmgS?
2. Is divergence explained by temporal ordering not captured in current variables?
3. Is there a prior-state trace before the matched profile snapshot?
4. Are path-history variables needed?
5. Can divergence be reduced by adding temporal trace information?
6. Does this improve causal closure?
7. Does V6 remain NOT READY?

**Caution:** V5.43 is still NOT V6. Do not investigate length, space, velocity, or c.
Use only already available traces or derived diagnostic trace variables.

---

## 13. Suite Reference

| Suite | Tests | Status |
|:------|------:|:------:|
| CRP | 3 | COMPLETE |
| CRE | 1 | COMPLETE |
| CRA | 1 | COMPLETE |
| CRI | — | SKIPPED (CRA subsumes) |
| CRS | — | THIS DOCUMENT |
| **Total** | **5** | **0 failed** |

---

*Generated 2026-07-19. Authoritative V5.42 final synthesis.*
