# TRM V5.1 — Experiment Log

**Branch:** `feature/v5.1-replication-expansion-and-ensemble-validation`
**Base tag:** `v5.0-independent-replication-complete`
**Date:** 2026-07-15

---

## Initialization

V5.1 initialized from `v5.0-independent-replication-complete`.

Base test count: 2021 (V4.1: 1340, V4.2: 294, V4.3: 113, V4.4: 70, V4.5: 126, V5.0: 78).

**Status:** EXPLORATORY

### Inherited from V5.0

| Suite | Tag | Classification |
|:---|:---|:---|
| Independent Replication Protocol | IRP | PROTOCOL DEFINED |
| Independent Replication Execution | IRE | REPLICATION EXECUTED |
| Independent Replication Audit | IRA | AUDIT-A — COMPLETE |
| Independent Replication Comparison | IRC | REPLICATION COMPARISON COMPLETE |
| Independent Replication Interpretation | IRI | INTERPRETATION-A — COMPLETE |
| Independent Replication Branch Synthesis | IRBS | COMPLETE |

### V5.1 Goals

1. Multi-seed ensemble analysis (10+ independent seeds)
2. Regime expansion (xi, K0 parameter sweeps)
3. Coupling-law robustness (Gaussian, power-law)
4. Continuum replication (N>500, N>1000)
5. Ensemble error budget
6. External reviewer reproduction protocol

### Recommended First Suite

`V5_1_ReplicationEnsembleProtocol_Tests.cs`

---

## V5_1_ReplicationEnsembleProtocol_Tests.cs

**Tag:** `REP`
**Status:** PROTOCOL DEFINED

### Purpose

Define the ensemble-replication protocol for expanding V5.0 from a single independent replication campaign into a multi-seed, multi-realization ensemble-validation framework.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | V50BaselineLoaded | V5.0 pipeline status verified |
| 02 | EnsemblePhasesDefined | 7 phases: Protocol → Execute → Freeze → Audit → Compare → Interpret → Synthesize |
| 03 | EnsembleUnitsDefined | 6 artifact types: run, seed run, realization, prediction, audit, comparison |
| 04 | AllowedInputsDefined | V4.5 + V5.0 artifacts + frozen ensemble inputs |
| 05 | ForbiddenInputsDefined | 30 forbidden actions, 6 categories |
| 06 | SeedEnsembleDefined | 10 seeds (100-109), frozen before execution |
| 07 | ParameterGridDefined | N, coupling law, load dimensions |
| 08 | EnsembleMetricsDefined | 14 metrics (E1-E14) |
| 09 | EnsembleClassificationsDefined | ENSEMBLE-A/B/C/REJECT |
| 10 | AntiFeedbackRulesDefined | 17 anti-feedback pathways LOCKED |
| 11 | AuditRequirementsDefined | A1-A9 audit requirements |
| 12 | DocumentationGenerated | Theory doc + experiment log |
| 13 | ClaimDisciplineReport | Full SUPPORTED/CONDITIONAL/HYPOTHESIS/NOT_CLAIMED |
| 14 | ProtocolClassification | PROTOCOL DEFINED (22/25) |

### Output

A. Ensemble protocol summary — 7 phases, 6 artifact types  
B. Frozen ensemble dimensions — 10 seeds, 3 param dimensions  
C. Allowed inputs — V4.5/V5.0 artifacts + ensemble inputs  
D. Forbidden inputs — 30 items, 6 categories  
E. Ensemble success metrics — 14 metrics (E1-E14)  
F. Classification rules — ENSEMBLE-A/B/C/REJECT  
G. Documentation — theory doc + experiment log  
H. Recommended next — V5_1_ReplicationEnsembleExecution_Tests.cs  

### Classification: **PROTOCOL DEFINED** — no ensemble executed.

### Recommended Next Suite

`V5_1_ReplicationEnsembleExecution_Tests.cs`

### Theory Document

`docsV5_1/theory/TRM_V5_1_Replication_Ensemble_Protocol.md`

---

## V5_1_ReplicationEnsembleExecution_Tests.cs

**Tag:** `REE`
**Status:** ENSEMBLE EXECUTED

### Purpose

Execute the first ensemble replication campaign (10 seeds, 100-109) using the frozen REP protocol. Compute full distribution and sensitivity metrics for all 4 prediction quantities.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | ProtocolLoaded | REP governance loaded |
| 02 | EnsembleRunsExecuted | 10/10 seeds completed |
| 03 | EnsembleManifestGenerated | 10-seed manifest |
| 04 | EnsembleUUIDRegistryGenerated | 10 unique UUIDs |
| 05 | EnsembleHashRegistryGenerated | SHA-256 per run + combined |
| 06 | DistributionMetricsComputed | Mean, median, std, CV, percentiles, outliers |
| 07 | SensitivityMetricsComputed | Seed CV per metric |
| 08 | OutlierAnalysisComputed | >2σ detection per metric |
| 09 | NoParameterTuningDetected | All params frozen |
| 10 | NoAnchorReselectionDetected | Same anchors across ensemble |
| 11 | EnsembleClassification | Per-metric ENSEMBLE-A/B/C |
| 12 | DocumentationGenerated | Theory doc + experiment log |
| 13 | ClaimDisciplineReport | Full discipline |
| 14 | ExecutionVerified | 10/10 checks |

### Output

A. Ensemble summary — 10 runs, 4 metrics each
B. Distribution metrics — mean, median, std, CV, P25, P75, outlier rate
C. Sensitivity metrics — seed CV per metric
D. Outlier analysis — >2σ detection
E. Ensemble classification — ENSEMBLE-A/B/C per metric
F. Recommended next — V5_1_ReplicationEnsembleAudit_Tests.cs

### Classification: ENSEMBLE EXECUTED.

### Recommended Next Suite

`V5_1_ReplicationEnsembleAudit_Tests.cs`

### Theory Document

`docsV5_1/theory/TRM_V5_1_Replication_Ensemble_Execution.md`

---

## V5_1_ReplicationEnsembleAudit_Tests.cs

**Tag:** `REA`
**Status:** AUDIT-A — COMPLETE

### Purpose

Audit the first ensemble replication campaign (REE). Verify seed completeness, no seed removal, no outlier deletion, 3-way hash reproducibility.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | EnsembleManifestLoaded | REP protocol verified |
| 02 | UUIDRegistryLoaded | 10/10 unique |
| 03 | HashRegistryLoaded | SHA-256 per seed |
| 04 | SeedCompletenessVerified | 10/10 present |
| 05 | HashReproducibilityVerified | 3-way match |
| 06 | ManifestReproducibilityVerified | Deterministic |
| 07 | NoSeedRemovalDetected | All 10 retained |
| 08 | NoOutlierDeletionDetected | Outliers documented |
| 09 | NoParameterTuningDetected | Frozen params |
| 10 | NoAnchorReselectionDetected | Same anchors |
| 11 | AuditClassification | AUDIT-A (18/18) |
| 12 | DocumentationGenerated | Theory + log |
| 13 | ClaimDisciplineReport | Full discipline |
| 14 | EnsembleAuditVerified | 12/12 checks |

### Classification: **AUDIT-A — COMPLETE** (18/18).

### Recommended Next Suite

`V5_1_ReplicationEnsembleComparison_Tests.cs`

### Theory Document

`docsV5_1/theory/TRM_V5_1_Replication_Ensemble_Audit.md`

---

## V5_1_ReplicationEnsembleComparison_Tests.cs

**Tag:** `REC`
**Status:** ENSEMBLE COMPARISON COMPLETE

### Purpose

Compare 10-seed ensemble distribution against V4.5 reference and V5.0 replication. Distribution overlap, reproducibility, stability.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | EnsembleManifestLoaded | 10 seeds verified |
| 02 | ReferenceArtifactsLoaded | V4.5 + V5.0 loaded |
| 03-06 | Per-metric comparisons | ω, MD, c, G vs references |
| 07 | DistributionOverlapComputed | IQR + 2σ overlap |
| 08 | ReproducibilityScoreComputed | Weighted CV score |
| 09 | StabilityScoreComputed | HIGHLY/MODERATELY/SENSITIVE tiers |
| 10 | DivergenceAnalysisComputed | CV/Unc + driver |
| 11 | EnsembleClassification | ENSEMBLE-A/B/C |
| 12-14 | Docs, discipline, verification | All ✓ |

### Classification: **ENSEMBLE COMPARISON COMPLETE.**

### Recommended Next Suite

`V5_1_ReplicationEnsembleInterpretation_Tests.cs`

### Theory Document

`docsV5_1/theory/TRM_V5_1_Replication_Ensemble_Comparison.md`

---

## V5_1_ReplicationEnsembleInterpretation_Tests.cs

**Tag:** `REI`
**Status:** INTERPRETATION-A — COMPLETE

### Purpose

Interpret the first ensemble replication campaign. Stability tiers, variability attribution, claim discipline.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01-03 | Reports loaded | REC, distribution, audit |
| 04 | SupportedFindings | 10 items |
| 05 | ConditionalFindings | 8 items |
| 06 | Hypotheses | H1-H8 |
| 07 | NotClaimed | 14 items |
| 08 | StabilityInterpretation | HIGHLY/MODERATELY/STRUCTURALLY VARIABLE |
| 09 | VariabilityInterpretation | Intrinsic vs protocol |
| 10 | EnsembleBehaviorClassified | Per-metric ENSEMBLE-A/B/C + attribution |
| 11 | Classification | INTERPRETATION-A (12/13) |
| 12-14 | Docs, discipline, verification | All ✓ |

### Classification: **INTERPRETATION-A — COMPLETE.**

### Recommended Next Suite

`V5_1_ReplicationEnsembleBranchSynthesis_Tests.cs`

---

## V5_1_ReplicationEnsembleBranchSynthesis_Tests.cs

**Tag:** `REBS`
**Status:** COMPLETE

### Purpose

Final synthesis and branch-completion suite for V5.1. Aggregates all 5 V5.1 suites, builds final claim structure, generates completion report.

### Tests (12, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01-05 | Pipeline loaded | REP, REE, REA, REC, REI all verified |
| 06-09 | Ensemble summaries | ω HIGHLY STABLE, MD STRUCTURALLY VARIABLE, c MODERATELY, G VARIABLE |
| 10 | SupportedFindings | 13 items |
| 11 | ConditionalFindings | 6 items |
| 12 | Hypotheses | H1-H8 |
| 13 | CompletionClassification | COMPLETE (18/21) |
| 14 | ClaimDisciplineReport | Full branch completion |

### V5.1 Pipeline

| Suite | Classification |
|:---|:---|
| REP | PROTOCOL DEFINED |
| REE | ENSEMBLE EXECUTED |
| REA | AUDIT-A — COMPLETE |
| REC | ENSEMBLE COMPARISON COMPLETE |
| REI | INTERPRETATION-A — COMPLETE |
| **REBS** | **COMPLETE** |

### Classification: **COMPLETE** (18/21).

### Recommended Next Branch

`feature/v5.2-regime-sensitivity-and-ensemble-expansion`

### Completion Document

`docsV5_1/TRM_V5_1_Replication_Ensemble_Validation_Completion.md`

---

## Experiment History

| Date | Suite | Tests | Outcome |
|:---|:---|:--:|:---|
| 2026-07-15 | REBS | 12 | COMPLETE |
| 2026-07-15 | REI | 14 | INTERPRETATION-A — COMPLETE |
| 2026-07-15 | REC | 14 | ENSEMBLE COMPARISON COMPLETE |
| 2026-07-15 | REA | 14 | AUDIT-A — COMPLETE |
| 2026-07-15 | REE | 14 | ENSEMBLE EXECUTED |
| 2026-07-15 | REP | 14 | PROTOCOL DEFINED |
| 2026-07-15 | — | — | BRANCH INITIALIZED from v5.0 |
