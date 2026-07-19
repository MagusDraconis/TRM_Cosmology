# TRM V5.0 — Independent Replication Audit

**Suite:** V5_0_IndependentReplicationAudit_Tests.cs  
**Tag:** IRA  
**Branch:** feature/v5.0-independent-replication-and-validation  
**Base:** v4.5-prospective-anchor-prediction-complete  
**Date:** 2026-07-15  
**Status:** AUDIT-A — COMPLETE

---

## Overview

The Independent Replication Audit (IRA) audits the first independent replication run (IRE). It verifies that the replication chain is independent, reproducible, and free from contamination by historical decisions.

## Audit Phases

| Phase | Description | Result |
|-------|-------------|--------|
| A1 | Pre-execution — V4.5 artifacts intact, seeds differ | PASS |
| A2 | Post-generation — determinism, hashes, primitives | PASS |
| A3 | Post-freeze — manifest structure, timestamp ordering, 3-way hash reproducibility | PASS |
| A4 | Replication — predictions differ from V4.5, no seed reuse | PASS |

## Independence Verification

| Criterion | Result |
|-----------|--------|
| Seeds differ from V4.5 | 50, 55, 60 ≠ 45 ✓ |
| Predictions differ from V4.5 | IRE values ≠ V4.5 values ✓ |
| Hashes differ from V4.5 | IRE SHA-256 ≠ V4.5 SHA-256 ✓ |
| Audit seeds differ from primary | aud1 ≠ primary ✓ |
| Same regime | xi=1.80, K0=1.15, N=100, exponential ✓ |
| Same primitives | Sm, RP, Nm, DL, ExpUpd ✓ |
| Same proxies | OmegaField, MeanDistProxy ✓ |

## Deep Parameter Audit (14 checks)

All parameters verified as matching V4.5 frozen values:
- xi=1.80, K0=1.15, N=100, s=0.08, exponential law
- dt=0.05, steps=400, hd=4
- KS, Sm, RP, Nm, DL, ExpUpd, RecoverFP primitives
- No seed reuse, no SI mapping, no external calibration

## Reselection Detection (11 checks)

All anchor definitions verified as identical to V4.5:
- OmegaField(), MeanDistProxy() functions
- T_scale, L_scale, M_scale definitions
- c_eff, G_eff formulas
- No proxy substitution, no weighting changes

## Hash Reproducibility

3-way SHA-256 match confirmed across independent recomputation runs.

## Audit Trail

8 audit record fields present and verified:
1. independent_seed
2. generation_timestamp
3. freeze_timestamp
4. prediction_values
5. prediction_hashes
6. combined_hash
7. v4_5_prediction_hash
8. audit_classification

## Classification

**AUDIT-A — COMPLETE** (18/18 scoring criteria)

## Claim Discipline

**SUPPORTED:** All audit phases passed. Independence, reproducibility, and protocol compliance verified.

**CONDITIONAL:** Same regime and primitives as V4.5. Audit verifies protocol, not physics.

**HYPOTHESIS:** Replication audit confirms protocol integrity and structural independence.

**NOT CLAIMED:** Physical correctness, validation, derivation of constants.

## Recommended Next Suite

`V5_0_IndependentReplicationComparison_Tests.cs`
