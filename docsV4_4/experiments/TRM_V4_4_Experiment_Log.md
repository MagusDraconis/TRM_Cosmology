# TRM V4.4 — Experiment Log

**Branch:** `feature/v4.4-prospective-length-anchor-validation`
**Base tag:** `v4.3-geometric-scale-interpretation-complete`
**Date:** 2026-07-15

---

## Initialization

V4.4 initialized from `v4.3-geometric-scale-interpretation-complete`.

Base test count: 1748 (1340 V4.1 + 294 V4.2 + 113 V4.3 + 1 V4 root).

---

## V4_4_ProspectiveLengthAnchorValidation_Tests.cs

**Tag:** `PLAV`
**Status:** VALIDATION COMPLETE
**Classification:** VALIDATED / PROMISING / WEAK / REJECT

### Purpose

Perform the first prospective validation of MeanDist as a future TRM length anchor. Defines acceptance criteria BEFORE evaluation, freezes MeanDist parameters, then verifies robustness across seeds, N, laws, nulls, and hierarchy.

### Tests

| # | Test Name | Status |
|:--:|:---|:---|
| 01 | MeanDistBaselineFrozen | PASS |
| 02 | ProspectiveAcceptanceCriteriaDefined | PASS |
| 03 | SeedRobustnessVerified | PASS |
| 04 | NScalingRobustnessVerified | PASS |
| 05 | LawRobustnessVerified | PASS |
| 06 | NullSeparationVerified | PASS |
| 07 | HierarchyPersistenceVerified | PASS |
| 08 | NoRetrospectiveOptimization | PASS |
| 09 | ValidationMatrixComputed | PASS |
| 10 | ValidationClassification | PASS |
| 11 | RemainingRisksDocumented | PASS |
| 12 | DocumentationGenerated | PASS |
| 13 | NoPhysicalComparisonUsed | PASS |
| 14 | ClaimDisciplineReport | PASS |

### Key Results

- Acceptance criteria frozen before evaluation ✓
- MeanDist passes all 8 prospective criteria
- No physical c/G comparison used
- No retrospective optimization

### Recommended Next Suite

`V4_4_LengthAnchorStressTest_Tests.cs`

### Theory Document

`docsV4_4/theory/TRM_V4_4_Prospective_Length_Anchor_Validation.md`

---

## V4_4_LengthAnchorStressTest_Tests.cs

**Tag:** `LAST`
**Status:** STRESS TEST COMPLETE
**Classification:** SAFE / DEGRADED / FAILURE regions mapped

### Purpose

Determine the operating limits of MeanDist as a geometric length anchor. Applies 6 stress axes (seed, load, coupling, geometry, null, synchronization) and maps SAFE, DEGRADED, and FAILURE regions.

### Tests

| # | Test Name | Status |
|:--:|:---|:---|
| 01 | FrozenInputsVerified | PASS |
| 02 | SeedStressApplied | PASS |
| 03 | LoadStressApplied | PASS |
| 04 | CouplingStressApplied | PASS |
| 05 | GeometryStressApplied | PASS |
| 06 | NullStressApplied | PASS |
| 07 | SynchronizationStressApplied | PASS |
| 08 | MeanDistDriftMeasured | PASS |
| 09 | HierarchyPersistenceMeasured | PASS |
| 10 | ScaleRolePersistenceMeasured | PASS |
| 11 | SafeRegionComputed | PASS |
| 12 | FailureRegionComputed | PASS |
| 13 | DocumentationGenerated | PASS |
| 14 | ClaimDisciplineReport | PASS |

### Key Finding

MeanDist is **SAFE** across the entire V4.2/V4.3 explored parameter space. The primary regime lies well within the SAFE region. Degradation begins only at extreme parameter values.

### Recommended Next Suite

`V4_4_LengthAnchorOperationalEnvelope_Tests.cs`

### Theory Document

`docsV4_4/theory/TRM_V4_4_Length_Anchor_Stress_Test.md`

---

## V4_4_LengthAnchorOperationalEnvelope_Tests.cs

**Tag:** `LAOE`
**Status:** ENVELOPE COMPLETE

### Purpose

Map the complete operational envelope of MeanDist by scanning parameter space (xi, K0, load) and classifying each point. 3 × 2D slice scans with grid-based SAFE/DEGRADED/FAILURE classification.

### Tests (14, all passed)

| # | Test | Purpose |
|:--:|:---|:---|
| 01 | FrozenInputsVerified | PLAV + LAST as frozen inputs |
| 02 | EnvelopeGridGenerated | Grid definition (74 points, 4 seeds each) |
| 03 | StabilityContoursComputed | Slice A: xi × K0 at load=0.1 |
| 04 | DegradationContoursComputed | Slice B: xi × load at K0=1.2 |
| 05 | FailureContoursComputed | Slice C: K0 × load at xi=1.75 |
| 06 | MeanDistCVMapped | CV aggregate across all slices |
| 07 | HierarchyPersistenceMapped | Root-level check at 5 key points |
| 08 | RolePersistenceMapped | PRIMARY/SECONDARY/UNDEFINED |
| 09 | TransitionRegionsDetected | SAFE→DEGRADED, DEGRADED→FAILURE boundaries |
| 10 | SafeRegionComputed | xi∈[1.0,3.5], K0∈[0.5,2.5], s∈[0.01,0.30] |
| 11 | FailureRegionComputed | K0→0, s→0.50+ |
| 12 | DocumentationGenerated | Output summary |
| 13 | NoPhysicalComparisonUsed | Verification gate |
| 14 | ClaimDisciplineReport | Full discipline |

### Key Finding

Primary regime (xi=1.75, K0=1.2, s=0.1) is **well-centered** in the SAFE region with significant safety margins. DEGRADED onset at K0≈0.2 or s≈0.35. FAILURE at K0→0.

### Recommended Next Suite

`V4_4_LengthAnchorBranchSynthesis_Tests.cs`

### Theory Document

`docsV4_4/theory/TRM_V4_4_Length_Anchor_Operational_Envelope.md`

---

## V4_4_LengthAnchorSafetyMargin_Tests.cs

**Tag:** `LASM`
**Status:** MARGIN ANALYSIS COMPLETE
**Classification:** HIGH MARGIN

### Purpose

Quantify the safety margin between the primary regime and nearest degradation/failure boundaries. Computes normalized distances, robustness score, and operating-center score.

### Tests (14, all passed, <1s)

| # | Test | Key Result |
|:--:|:---|:---|
| 03 | XiMarginComputed | Min margin 0.43 — HIGH |
| 04 | K0MarginComputed | Min margin 0.58 — HIGH |
| 05 | LoadMarginComputed | Min margin 0.90 — HIGH |
| 08 | RobustnessScoreComputed | Score 0.43 (xi-limited) |
| 09 | OperatingCenterScoreComputed | Well-centered in SAFE region |
| 10 | NearestBoundaryComputed | Not adjacent to degradation |
| 11 | SafetyMarginClassification | **HIGH MARGIN** |

### Key Finding

Primary regime has **HIGH MARGIN** — robust with significant safety distance from all degradation and failure boundaries. The limiting parameter is xi (margin 0.43), well above the 0.1 caution threshold.

### Recommended Next Suite

`V4_4_LengthAnchorBranchSynthesis_Tests.cs`

### Theory Document

`docsV4_4/theory/TRM_V4_4_Length_Anchor_Safety_Margin.md`

---

## V4_4_LengthAnchorBranchSynthesis_Tests.cs

**Tag:** `LABS`
**Status:** SYNTHESIS COMPLETE

### Purpose

Final synthesis and branch-completion suite. Loads frozen outputs from PLAV, LAST, LAOE, LASM. Produces final claim structure, completion classification, and recommended next branch.

### Tests (14, all passed, <1s)

Loads each prior suite, generates validation/stress/envelope/margin summaries, produces final supported/conditional/hypothesis/not-claimed structure.

### Key Result

**MeanDist qualifies as a VALIDATED future length-anchor candidate. V4.4 COMPLETE.**

### Recommended Next Branch

`feature/v4.5-prospective-anchor-prediction-branch`

### Completion Document

`docsV4_4/TRM_V4_4_Prospective_Length_Anchor_Validation_Completion.md`

---

## V4.4 — Final Summary

| Suite | Tag | Tests | Question | Status |
|:---|:---|:--:|:---|:---|
| Prospective Validation | PLAV | 14 | Can MeanDist survive? | ✅ |
| Length Anchor Stress Test | LAST | 14 | Where are the limits? | ✅ |
| Operational Envelope | LAOE | 14 | What is the SAFE region? | ✅ |
| Length Anchor Safety Margin | LASM | 14 | How much margin? | ✅ |
| Branch Synthesis | LABS | 14 | What does it all mean? | ✅ |
| **V4.4 TOTAL** | | **70** | **Prospective validation COMPLETE** | ✅ |

---

## Experiment History

| Date | Suite | Tests | Outcome |
|:---|:---|:--:|:---|
| 2026-07-15 | LABS | 14 | SYNTHESIS COMPLETE |
| 2026-07-15 | LASM | 14 | MARGIN ANALYSIS COMPLETE |
| 2026-07-15 | LAOE | 14 | ENVELOPE COMPLETE |
| 2026-07-15 | LAST | 14 | STRESS TEST COMPLETE |
| 2026-07-15 | PLAV | 14 | VALIDATION COMPLETE |
| 2026-07-15 | — | — | BRANCH INITIALIZED |
