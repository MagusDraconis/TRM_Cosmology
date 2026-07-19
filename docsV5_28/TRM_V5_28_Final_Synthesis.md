# TRM V5.28 Final Synthesis — Rescue Risk Stratum and Control Policy

**Version:** 1.0
**Date:** 2026-07-19
**Branch:** `feature/v5.28-rescue-risk-stratum-and-control-policy`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.28-rescue-risk-stratum-and-control-policy` |
| Base | V5.27 COMPLETE |
| Suites | RSP, RSE, RSA, RSI |
| V5.28 tests | 6 (3 RSP + 1 RSE + 1 RSA + 1 RSI) |
| Cumulative tests | 2768 passed, 0 failed |
| Commits | `6a42486`, `892a36a`, `0728674` |

---

## 2. V5.28 Research Question

**Can the validated c3OmegaShift > 0.1 risk stratum define a safe adaptive control policy?**

Answer: **Yes.** Stop-Low is a safe post-C3 continuation policy that preserves all
observed rescues, maintains zero damage, and reduces continuation workload by ~75%.

---

## 3. Suite Summaries

### RSP — Protocol

3 tests. Protocol freeze: M3++ unchanged, c3OmegaShift threshold 0.1 frozen,
no new variables, no correction classes, policy gating only.

### RSE — Execution

1 test (LongRunning). 364 profiles across 10 N values.

**Key finding:** All 21 rescues are in Stratum B (c3OmgS > 0.1). Stratum A has
zero rescues. Stop-Low preserves 21/21 rescues with zero damage while saving
273 continuations (75% reduction). Preserve-High loses 2 rescues.

### RSA — Policy Analysis

1 test (LongRunning). Comprehensive analysis.

**Key findings:**
- N-stable: 10/10 N values SAFE
- Cohort-stable: 4/4 cohorts SAFE
- Near-miss audit: max c3OmgS in low stratum = 0.070, safe margin confirmed
- P3 failure explained: 2 seeds need continuation
- Policy elevated from Model B to Model A

### RSI — Intervention Audit

1 test (LongRunning). Formal policy audit.

**Key findings:**
- Baseline reproduced (21 rescues, 0 damage)
- P2 preserves all rescues (21/21), zero missed, zero damage
- P4 Audit-All: zero delayed low-stratum rescues
- All 7 gates reached. Stop-Low confirmed as final V5.28 policy.

---

## 4. Supported Findings

1. c3OmegaShift > 0.1 risk stratum remains validated from V5.27.
2. Stop-Low is a safe post-C3 continuation policy.
3. All observed rescues occur in Stratum B (c3OmgS > 0.1).
4. Zero rescues observed in Stratum A (c3OmgS ≤ 0.1).
5. Stop-Low preserves all 21 observed rescues. Zero missed.
6. Stop-Low preserves zero damage.
7. Stop-Low reduces continuation workload by approximately 75% (273/364).
8. Stop-Low is stable across 10 tested N values and 4 cohorts.
9. Rescue efficiency improves from 5.8% (baseline) to 23.1% (Stop-Low) — a 4× gain.

---

## 5. Final Policy Definition

**Stop-Low — Post-C3 Continuation Policy**

```
After C3 measurement:

IF c3OmegaShift <= 0.1:
    STOP continuation. Classify as low rescue probability.

IF c3OmegaShift > 0.1:
    CONTINUE standard M3++ persistence validation.
```

**Critical caveat:** c3OmegaShift is measured AFTER C3. Stop-Low is a post-C3
continuation policy, not a pre-C3 selection policy. It does not reduce first-stage
intervention cost. It eliminates wasted continuation work for seeds that C3 has
already demonstrated will not rescue.

---

## 6. Preserve-High Rejection

Preserve-High (skip continuation for high-stratum seeds) was tested and rejected.

- 20/21 high-stratum rescues persist without continuation
- 2 rescues (N=75 s=230, N=76 s=290) require continuation despite c3OmgS > 0.5
- Therefore: high-stratum seeds should NOT be automatically preserved
- Stop-Low (stop the low, continue the high) is the preferred policy

---

## 7. Weakened or Rejected

- Preserve-High policy (2 missed rescues)
- High-stratum automatic success assumption
- Deterministic rescue
- Pre-C3 risk selection
- c3OmegaShift causal sufficiency claim

---

## 8. Not Claimed Boundaries

V5.28 does NOT claim:

- Deterministic rescue prediction
- Causal proof
- Pre-intervention prediction
- Universal adaptive control
- Universal policy validity
- Applicability outside tested N, cohorts, operators
- That Stratum A can never rescue under future conditions
- Physical interpretation
- Modified M3++

---

## 9. Final V5.28 Conclusion

V5.28 converts the validated c3OmegaShift risk stratum into a practical post-C3
continuation policy. The Stop-Low rule preserves all observed rescues and zero
damage while reducing continuation workload by approximately 75%. The policy
operates as an efficiency and resource-allocation rule, not as a deterministic
rescue predictor.

---

## 10. Recommended V5.29

**Branch:** `feature/v5.29-risk-stratum-boundary-and-threshold-robustness`

**Central question:** How robust is the Stop-Low policy around the c3OmegaShift = 0.1
boundary?

**Purpose:** Stress-test threshold robustness without retuning.

**Planned suites:** RBP, RBE, RBA, RBI, RBS

**Core questions:**
1. Are there hidden rescues near the threshold?
2. Is there a safety margin around 0.1?
3. Are low-stratum and high-stratum clearly separated?
4. Does a boundary ambiguity zone exist?
5. How sensitive is the policy to measurement noise?
6. Is the threshold stable across N and cohorts?

---

## 11. Suite Reference

| Suite | File | Tests | Status |
|:------|:-----|------:|:------:|
| RSP | `V5_28_RescueRiskStratumPolicyProtocol_Tests.cs` | 3 | COMPLETE |
| RSE | `V5_28_RiskStratumExecution_Tests.cs` | 1 | COMPLETE |
| RSA | `V5_28_RiskStratumPolicyAnalysis_Tests.cs` | 1 | COMPLETE |
| RSI | `V5_28_RiskStratumInterventionAudit_Tests.cs` | 1 | COMPLETE |
| **Total** | | **6** | **0 failed** |

---

*Generated 2026-07-19. This is the authoritative V5.28 final synthesis.*
