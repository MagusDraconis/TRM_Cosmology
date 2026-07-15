# TRM V5.0 — Independent Replication Execution

**Suite:** V5_0_IndependentReplicationExecution_Tests.cs  
**Tag:** IRE  
**Branch:** feature/v5.0-independent-replication-and-validation  
**Base:** v4.5-prospective-anchor-prediction-complete  
**Date:** 2026-07-15  
**Status:** REPLICATION EXECUTED

---

## Overview

The Independent Replication Execution (IRE) suite performs the first independent replication run of the full prospective prediction workflow established in V4.5. It uses independent seeds and graph realizations under the same regime, generates predictions, freezes them, audits them, and compares against V4.5 frozen outputs under IRP governance.

## Regime (shared with V4.5)

| Parameter | Value |
|-----------|-------|
| xi | 1.80 |
| K0 | 1.15 |
| N | 100 |
| Coupling law | Exponential |
| dt | 0.05 |
| Steps | 400 |
| Frequency spread (s) | 0.08 |

## Independent Seeds

| Role | Seed |
|------|------|
| V4.5 (reference) | 45 |
| IRE (primary) | 50 |
| IRE audit 1 | 55 |
| IRE audit 2 | 60 |

All seeds verified as different from V4.5 and from each other.

## Replication Pipeline

| Phase | Suite | Status |
|-------|-------|--------|
| 1 — Protocol | IRP | PROTOCOL DEFINED |
| 2 — Execute | IRE | REPLICATION EXECUTED ← this suite |
| 3 — Freeze | IRE | FROZEN (within this suite) |
| 4 — Audit | IRE | AUDITED (within this suite) |
| 5 — Compare | IRE | COMPARED (within this suite) |
| 6 — Interpret | IRI | PENDING |

## Metrics (IRE vs V4.5)

| Metric | V4.5 (seed=45) | IRE (seed=50) | Classification |
|--------|:-------------:|:-------------:|:--------------:|
| c_eff | Computed | Computed | Classified |
| G_eff | Computed | Computed | Classified |
| omega_anchor | Computed | Computed | Classified |
| meanDist_anchor | Computed | Computed | Classified |

## Replication Classes

| Class | Condition | Interpretation |
|-------|-----------|----------------|
| REPLICATION-A | rel_err ≤ 1.0 × V4.5 uncertainty | Seed-stable, structurally robust |
| REPLICATION-B | rel_err ≤ 10.0 × V4.5 uncertainty | Compatible, some seed sensitivity |
| REPLICATION-C | rel_err > 10.0 × V4.5 uncertainty | Genuine seed/realization sensitivity |
| REPLICATION-REJECT | Protocol violated | Audit failed |

## Integrity Verification

| Check | Result |
|-------|--------|
| Seeds differ from V4.5 | VERIFIED (50, 55, 60 ≠ 45) |
| 3-way audit reproducibility | VERIFIED |
| Audit seeds produce different values | VERIFIED |
| No parameter tuning | VERIFIED |
| No anchor reselection | VERIFIED |
| No V4.5 artifact mutation | VERIFIED |
| 28 forbidden actions | NOT EXECUTED |
| SHA-256 hashes generated | YES |
| Replication UUIDs | 5 unique |

## Claim Discipline

**SUPPORTED:** Independent replication executed under IRP. Predictions frozen and audited. Comparison computed.

**CONDITIONAL:** Same regime as V4.5. Same primitives. Different seeds → different graphs → expected variation.

**HYPOTHESIS:** H1-H3 concerning structural robustness, seed sensitivity, regime compatibility.

**NOT CLAIMED:** Physical correctness, validation, derivation of constants.

## Recommended Next Suite

`V5_0_IndependentReplicationAudit_Tests.cs`
