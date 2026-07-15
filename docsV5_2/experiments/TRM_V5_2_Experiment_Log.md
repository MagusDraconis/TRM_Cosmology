# TRM V5.2 — Experiment Log

**Branch:** `feature/v5.2-regime-sensitivity-and-ensemble-expansion`
**Base tag:** `v5.1-replication-ensemble-validation-complete`
**Date:** 2026-07-15

---

## Initialization

V5.2 initialized from `v5.1-replication-ensemble-validation-complete`.

Base test count: 2105.

**Status:** EXPLORATORY

### Recommended First Suite

`V5_2_RegimeSensitivityProtocol_Tests.cs`

---

## V5_2_RegimeSensitivityProtocol_Tests.cs

**Tag:** `RSP`
**Status:** PROTOCOL DEFINED

### Purpose

Define regime-sensitivity protocol: 4 regime classes, 5 variation axes, frozen grid (23 points, 69 runs), 21 metrics.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | V51BaselineLoaded | V5.1 baseline verified |
| 02 | RegimeClassesDefined | PRIMARY/SAFE-BOUNDARY/DEGRADED/FAILURE-PROXIMAL |
| 03 | VariationAxesDefined | xi, K0, s, N, law |
| 04 | AllowedRangesDefined | xi[1.50,2.10], K0[0.90,1.40], s[0.02,0.20], N{80-800} |
| 05 | ForbiddenRangesDefined | 8 forbidden actions |
| 06 | FrozenGridDefined | 23 regime points, 69 total runs |
| 07 | EnsembleArchitectureDefined | 3-seed per point |
| 08 | ComparisonMetricsDefined | 11 metrics (RM1-RM11) |
| 09 | SensitivityMetricsDefined | 10 metrics (SM1-SM10) |
| 10 | ClassificationRulesDefined | REGIME-A/B/C/REJECT |
| 11 | AuditRequirementsDefined | A1-A8 |
| 12-14 | Docs, discipline, classification | PROTOCOL DEFINED (20/21) |

### Classification: **PROTOCOL DEFINED.**

### Recommended Next Suite

`V5_2_RegimeSensitivityExecution_Tests.cs`

---

## V5_2_RegimeSensitivityExecution_Tests.cs

**Tag:** `RSE`
**Status:** REGIME EXECUTED

### Purpose

Execute frozen regime-sensitivity campaign: 23 regime points, 69 runs, 5 phases.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01 | ProtocolLoaded | RSP loaded |
| 02 | RegimeRunsExecuted | 69/69 runs |
| 03-05 | Manifests | Manifest, UUIDs, hashes |
| 06 | RegimeMetricsComputed | Per-regime CV + classification |
| 07 | SensitivityMetricsComputed | Cross-regime trends |
| 08 | RegimeClassificationComputed | REGIME-A/B/C counts |
| 09-11 | Integrity | No tuning, no reselection, no removal |
| 12-14 | Docs, discipline, verification | All ✓ |

### Classification: **REGIME EXECUTED.**

### Recommended Next Suite

`V5_2_RegimeSensitivityAudit_Tests.cs`

---

## V5_2_RegimeSensitivityAudit_Tests.cs

**Tag:** `RSA`
**Status:** AUDIT-A — COMPLETE

### Purpose

Audit regime-sensitivity campaign: 23 points, 69 runs, hash/manifest reproducibility.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01-03 | Manifests loaded | Regime, UUID, hash |
| 04 | RegimePointCompleteness | 23/23 |
| 05 | RunCompleteness | 69/69 |
| 06 | SeedCompleteness | 3/3 per point |
| 07 | HashReproducibility | 3-way verified |
| 08 | ManifestReproducibility | Deterministic |
| 09-11 | No removal/grid change | All verified |
| 12 | AuditClassification | AUDIT-A (14/16) |
| 13-14 | Docs, discipline | All ✓ |

### Classification: **AUDIT-A — COMPLETE.**

### Recommended Next Suite

`V5_2_RegimeSensitivityComparison_Tests.cs`

---

## V5_2_RegimeSensitivityComparison_Tests.cs

**Tag:** `RSC`
**Status:** REGIME COMPARISON COMPLETE

### Purpose

Compare all 23 regime points vs primary reference. Stability scores, degradation, regime map.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01-02 | Manifests loaded | Regime + sensitivity |
| 03-06 | Per-metric comparison | ω, MD, c, G vs ref |
| 07 | StabilityScores | Per-phase tier |
| 08 | DegradationScores | Per-metric max deviation |
| 09 | RegimeMap | REGIME-A/B/C per point |
| 10 | DivergenceAnalysis | Dominant drivers |
| 11 | Classification | Overall REGIME-A/B/C |
| 12-14 | Docs, discipline, verification | All ✓ |

### Classification: **REGIME COMPARISON COMPLETE.**

### Recommended Next Suite

`V5_2_RegimeSensitivityInterpretation_Tests.cs`

---

## V5_2_RegimeSensitivityInterpretation_Tests.cs

**Tag:** `RSI`
**Status:** INTERPRETATION-A — COMPLETE

### Purpose

Interpret regime sensitivity campaign: stability tiers, sensitivity drivers, claim discipline.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01-03 | Reports loaded | RSC, classifications, sensitivity |
| 04 | SupportedFindings | 14 items |
| 05 | ConditionalFindings | 6 items |
| 06 | Hypotheses | H1-H8 |
| 07 | NotClaimed | 11 items |
| 08 | StabilityInterpretation | HIGHLY/MODERATELY/SENSITIVE per metric |
| 09 | SensitivityInterpretation | Intrinsic vs regime vs protocol |
| 10 | DriverRanking | G_eff > MD > c_eff > Omega |
| 11 | Classification | INTERPRETATION-A (13/13) |
| 12-14 | Docs, discipline, verification | All ✓ |

### Classification: **INTERPRETATION-A — COMPLETE.**

### Recommended Next Suite

`V5_2_RegimeSensitivityBranchSynthesis_Tests.cs`

---

## V5_2_RegimeSensitivityBranchSynthesis_Tests.cs

**Tag:** `RSBS`
**Status:** COMPLETE

### Purpose

Final synthesis for V5.2. Central finding: seed stability ≠ regime stability.

### Tests (14, all passed)

| # | Test | Key Result |
|:--:|:---|:---|
| 01-05 | Pipeline loaded | RSP, RSE, RSA, RSC, RSI verified |
| 06 | SeedVsRegimeStability | Omega: seed-stable but regime-sensitive; MD: seed-variable but regime-robust |
| 07-10 | Per-metric summaries | ω, MD, c, G regime results |
| 11 | CorrectedStabilityPicture | Replaces V5.1 simplified hypothesis |
| 12 | SupportedFindings | 13 items |
| 13 | CompletionClassification | COMPLETE (17/18) |
| 14 | ClaimDisciplineReport | Full branch completion |

### Classification: **COMPLETE.**

### Recommended Next Branch

`feature/v5.3-stability-mechanism-and-control-parameters`

---

## Experiment History

| Date | Suite | Tests | Outcome |
|:---|:---|:--:|:---|
| 2026-07-15 | RSBS | 14 | COMPLETE |
| 2026-07-15 | RSI | 14 | INTERPRETATION-A — COMPLETE |
| 2026-07-15 | RSC | 14 | REGIME COMPARISON COMPLETE |
| 2026-07-15 | RSA | 14 | AUDIT-A — COMPLETE |
| 2026-07-15 | RSE | 14 | REGIME EXECUTED |
| 2026-07-15 | RSP | 14 | PROTOCOL DEFINED |
| 2026-07-15 | — | — | BRANCH INITIALIZED from v5.1 |
