# TRM V5.0 — Experiment Log

**Branch:** `feature/v5.0-independent-replication-and-validation`  
**Base tag:** `v4.5-prospective-anchor-prediction-complete`  
**Date:** 2026-07-15

---

## Initialization

V5.0 initialized from `v4.5-prospective-anchor-prediction-complete`.

Base test count: 1943 (V4.1: 1340, V4.2: 294, V4.3: 113, V4.4: 70, V4.5: 126).

**Status:** EXPLORATORY

### Inherited from V4.5

| Suite | Tag | Classification |
|:---|:---|:---|
| Prospective Anchor Prediction Protocol | PAPP | PROTOCOL DEFINED |
| Prospective Anchor Prediction Generation | PAPG | PREDICTION GENERATED |
| Prospective Anchor Prediction Freeze | PAPF | FROZEN |
| Prospective Anchor Prediction Computation | PAPC | COMPUTED |
| Prospective Anchor Prediction Audit | PAPA | AUDITED |
| Prospective Anchor Comparison Protocol | PACP | GOVERNANCE DEFINED |
| Prospective Anchor Prediction Comparison | PAXC | COMPARISON EXECUTED |
| Prospective Anchor Prediction Interpretation | PAXI | INTERPRETATION COMPLETE |
| Prospective Anchor Prediction Branch Synthesis | PABS | COMPLETE |

### V5.0 Goals

1. Independent replication of prospective prediction pipeline
2. Regime sensitivity characterization
3. SI-unit mapping under prospective protocol
4. Governed physical constant comparison
5. Continuum limit extension
6. Error budget validation

### Recommended First Suite

`V5_0_IndependentReplicationProtocol_Tests.cs`

---

## V5_0_IndependentReplicationProtocol_Tests.cs

**Tag:** `IRP`
**Status:** PROTOCOL DEFINED

### Purpose

Define a fully independent replication protocol for the complete prospective prediction pipeline. Specifies allowed inputs, 28 forbidden actions across 5 categories, 6 replication success criteria, 4-phase audit protocol, and 4 replication comparison classes.

### Tests (8, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | ProtocolDefined | 6 replication phases specified |
| 02 | InputsDefined | Allowed: V4.5 artifacts + independent inputs |
| 03 | ForbiddenInputsDefined | 28 forbidden actions across 5 categories |
| 04 | ReplicationCriteriaDefined | 6 criteria (C1-C6) + 4 classification thresholds |
| 05 | AuditCriteriaDefined | 4-phase audit + 8 record fields |
| 06 | ComparisonCriteriaDefined | 4 replication classes (A/B/C/REJECT) |
| 07 | DocumentationGenerated | Theory doc + experiment log |
| 08 | ClaimDisciplineReport | Full SUPPORTED/CONDITIONAL/HYPOTHESIS/NOT_CLAIMED |

### Output

A. Replication protocol — 6 phases defined  
B. Success criteria — 6 criteria (3 primary, 3 secondary)  
C. Allowed inputs — V4.5 artifacts + independent  
D. Forbidden inputs — 5 categories, 28 items  
E. Recommended next suite — V5_0_IndependentReplicationExecution_Tests.cs  

### Classification: **PROTOCOL DEFINED** — no replication executed.

### Recommended Next Suite

`V5_0_IndependentReplicationExecution_Tests.cs`

### Theory Document

`docsV5_0/theory/TRM_V5_0_Independent_Replication_Protocol.md`

---

## V5_0_IndependentReplicationExecution_Tests.cs

**Tag:** `IRE`
**Status:** REPLICATION EXECUTED

### Purpose

Execute an independent replication run of the full prospective prediction workflow. Uses independent seeds (50, 55, 60 ≠ V4.5 seed 45) and graph realizations under the same regime. Generates predictions, freezes them, audits them, and compares against V4.5 frozen outputs under IRP governance.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | ProtocolLoaded | IRP governance loaded |
| 02 | IndependentSeedsGenerated | 50, 55, 60 ≠ 45, all unique |
| 03 | ReplicationExecutionCompleted | IRE vs V4.5 predictions computed |
| 04 | ReplicationFreezeCompleted | SHA-256 hashes + 5 UUIDs |
| 05 | ReplicationAuditCompleted | 3-way reproducibility, audit seeds differ |
| 06 | ReplicationManifestGenerated | 17-field manifest |
| 07 | ReplicationHashesGenerated | Tamper-evident comparison hash |
| 08 | ReplicationUUIDsGenerated | 5 unique prediction IDs |
| 09 | ReplicationComparisonComputed | All 4 metrics vs V4.5 |
| 10 | NoParameterTuningDetected | xi, K0, N, s, law, seed all frozen |
| 11 | NoAnchorReselectionDetected | Same anchor definitions as V4.5 |
| 12 | ReplicationClassification | Per-metric + overall classification |
| 13 | DocumentationGenerated | Theory doc + experiment log |
| 14 | ClaimDisciplineReport | Full SUPPORTED/CONDITIONAL/HYPOTHESIS/NOT_CLAIMED |

### Output

A. Replication summary — IRE vs V4.5 predictions  
B. Replication metrics — all 4 metrics with abs/rel error  
C. Replication classification — per-metric + overall  
D. Divergence analysis — seed sensitivity exposed  
E. Readiness assessment — REPLICATION EXECUTED  
F. Recommended next — V5_0_IndependentReplicationAudit_Tests.cs  

### Classification: **REPLICATION EXECUTED** — interpretation deferred to IRI.

### Recommended Next Suite

`V5_0_IndependentReplicationAudit_Tests.cs`

### Theory Document

`docsV5_0/theory/TRM_V5_0_Independent_Replication_Execution.md`

---

## V5_0_IndependentReplicationAudit_Tests.cs

**Tag:** `IRA`
**Status:** AUDIT-A — COMPLETE

### Purpose

Audit the first independent replication run (IRE). Verify independence from V4.5 artifacts, hash reproducibility (3-way match), manifest integrity, and audit trail completeness. Detect hidden tuning and reselection.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | ReplicationManifestLoaded | 17-field manifest verified |
| 02 | ReplicationHashesLoaded | IRE hashes ≠ V4.5 hashes |
| 03 | ReplicationUUIDsLoaded | 5/5 unique UUIDs |
| 04 | AuditTrailLoaded | 8/8 audit fields present |
| 05 | HashReproducibilityVerified | 3-way SHA-256 match |
| 06 | ManifestReproducibilityVerified | Deterministic manifest |
| 07 | IndependenceVerified | Seeds, preds, hashes all differ |
| 08 | NoTuningDetected | 14/14 deep checks pass |
| 09 | NoReselectionDetected | 11/11 reselection checks pass |
| 10 | AuditCompletenessVerified | A1-A4 all passed |
| 11 | AuditClassification | AUDIT-A — COMPLETE (18/18) |
| 12 | DocumentationGenerated | Theory doc + experiment log |
| 13 | ClaimDisciplineReport | Full discipline report |
| 14 | ReplicationAuditVerified | 13/13 verification checks |

### Output

A. Audit summary — all phases passed  
B. Independence analysis — seeds, predictions, hashes all differ  
C. Reproducibility analysis — 3-way SHA-256 match  
D. Audit classification — AUDIT-A  
E. Readiness — READY FOR COMPARISON  
F. Recommended next — V5_0_IndependentReplicationComparison_Tests.cs  

### Classification: **AUDIT-A — COMPLETE** (18/18).

### Recommended Next Suite

`V5_0_IndependentReplicationComparison_Tests.cs`

### Theory Document

`docsV5_0/theory/TRM_V5_0_Independent_Replication_Audit.md`

---

## V5_0_IndependentReplicationComparison_Tests.cs

**Tag:** `IRC`
**Status:** REPLICATION COMPARISON COMPLETE

### Purpose

Compare the independently replicated prediction chain (IRE, seed=50) against the frozen V4.5 reference chain (seed=45). Compute per-metric comparisons, structural similarity, reproducibility scores, and divergence analysis.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | ReferenceManifestLoaded | V4.5 (seed=45) values loaded |
| 02 | ReplicationManifestLoaded | IRE (seed=50) values loaded |
| 03 | AuditRecordsLoaded | IRA — AUDIT-A verified |
| 04 | OmegaComparisonComputed | ω_IRE vs ω_V4.5 classified |
| 05 | MeanDistComparisonComputed | MD_IRE vs MD_V4.5 classified |
| 06 | CEffComparisonComputed | c_IRE vs c_V4.5 classified |
| 07 | GEffComparisonComputed | G_IRE vs G_V4.5 classified |
| 08 | StructuralSimilarityComputed | Ratio similarity + same structure |
| 09 | ReproducibilityScoreComputed | Weighted score (0-1) |
| 10 | DivergenceAnalysisComputed | Δ/Unc per metric + attribution |
| 11 | ReplicationClassification | Per-metric + overall class |
| 12 | DocumentationGenerated | Theory doc + experiment log |
| 13 | ClaimDisciplineReport | Full discipline report |
| 14 | ComparisonVerified | 10/10 verification checks |

### Output

A. Comparison matrix — all 4 metrics with abs/rel error  
B. Reproducibility score — weighted 0-1 scale  
C. Divergence analysis — per-metric Δ/Unc + attribution  
D. Per-metric classification — REPLICATION-A/B/C  
E. Overall classification — worst per-metric class  
F. Recommended next — V5_0_IndependentReplicationInterpretation_Tests.cs  

### Classification: **REPLICATION COMPARISON COMPLETE.**

### Recommended Next Suite

`V5_0_IndependentReplicationInterpretation_Tests.cs`

### Theory Document

`docsV5_0/theory/TRM_V5_0_Independent_Replication_Comparison.md`

---

## V5_0_IndependentReplicationInterpretation_Tests.cs

**Tag:** `IRI`
**Status:** INTERPRETATION-A — COMPLETE

### Purpose

Interpret the first independent replication campaign (IRC). Classify SUPPORTED, CONDITIONAL, HYPOTHESIS, and NOT CLAIMED findings about TRM robustness, reproducibility, and structural stability.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | ComparisonReportLoaded | IRC results loaded |
| 02 | ReplicationManifestLoaded | IRE manifest verified |
| 03 | AuditReportLoaded | IRA AUDIT-A verified |
| 04 | SupportedFindingsGenerated | 20 SUPPORTED findings |
| 05 | ConditionalFindingsGenerated | 11 CONDITIONAL findings |
| 06 | HypothesesGenerated | 8 formal HYPOTHESES (H1-H8) |
| 07 | NotClaimedGenerated | 17 items NOT CLAIMED |
| 08 | OmegaInterpretationComputed | Per-class interpretation |
| 09 | MeanDistInterpretationComputed | Per-class interpretation |
| 10 | ReproducibilityInterpretationComputed | Tiered reproducibility analysis |
| 11 | InterpretationClassification | INTERPRETATION-A (14/14) |
| 12 | DocumentationGenerated | Theory doc + experiment log |
| 13 | ClaimDisciplineReport | Full discipline report |
| 14 | InterpretationVerified | 11/11 verification checks |

### Output

A. Interpretation summary — all findings synthesized  
B. 20 SUPPORTED findings  
C. 11 CONDITIONAL findings  
D. 8 formal HYPOTHESES  
E. 17 items NOT CLAIMED  
F. Readiness — INTERPRETATION COMPLETE  
G. Recommended next — V5_0_IndependentReplicationBranchSynthesis_Tests.cs  

### Classification: **INTERPRETATION-A — COMPLETE** (14/14).

### Recommended Next Suite

`V5_0_IndependentReplicationBranchSynthesis_Tests.cs`

### Theory Document

`docsV5_0/theory/TRM_V5_0_Independent_Replication_Interpretation.md`

---

## V5_0_IndependentReplicationBranchSynthesis_Tests.cs

**Tag:** `IRBS`
**Status:** COMPLETE

### Purpose

Final synthesis and branch-completion suite for the first independent replication campaign. Aggregates all 5 V5.0 suites. Builds final claim structure. Verifies pipeline integrity. Generates branch completion report.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | ProtocolLoaded | IRP pipeline verified |
| 02 | ExecutionLoaded | IRE predictions verified |
| 03 | AuditLoaded | IRA AUDIT-A verified |
| 04 | ComparisonLoaded | IRC comparison verified |
| 05 | InterpretationLoaded | IRI interpretation verified |
| 06 | SupportedFindingsGenerated | Full pipeline synthesis |
| 07 | ConditionalFindingsGenerated | Regime, scope, structural constraints |
| 08 | HypothesesGenerated | 10 formal hypotheses (H1-H10) |
| 09 | NotClaimedGenerated | 18 items NOT CLAIMED |
| 10 | ReplicationRobustnessGenerated | Per-channel robustness assessment |
| 11 | CompletionClassification | COMPLETE (18/18) |
| 12 | DocumentationGenerated | Completion doc + experiment log |
| 13 | ClaimDisciplineReport | Full branch completion report |
| 14 | SynthesisVerified | 11/11 verification checks |

### V5.0 Pipeline

| Suite | Classification |
|:---|:---|
| IRP | PROTOCOL DEFINED |
| IRE | REPLICATION EXECUTED |
| IRA | AUDIT-A — COMPLETE |
| IRC | REPLICATION COMPARISON COMPLETE |
| IRI | INTERPRETATION-A — COMPLETE |
| **IRBS** | **COMPLETE** |

### Classification: **COMPLETE** (18/18).

### Recommended Next Branch

`feature/v5.1-replication-expansion-and-ensemble-validation`

### Documentation

`docsV5_0/TRM_V5_0_Independent_Replication_Completion.md`

---

## Experiment History

| Date | Suite | Tests | Outcome |
|:---|:---|:--:|:---|
| 2026-07-15 | IRBS | 14 | COMPLETE |
| 2026-07-15 | IRI | 14 | INTERPRETATION-A — COMPLETE |
| 2026-07-15 | IRC | 14 | REPLICATION COMPARISON COMPLETE |
| 2026-07-15 | IRA | 14 | AUDIT-A — COMPLETE |
| 2026-07-15 | IRE | 14 | REPLICATION EXECUTED |
| 2026-07-15 | IRP | 8 | PROTOCOL DEFINED |
| 2026-07-15 | — | — | BRANCH INITIALIZED from v4.5 |
