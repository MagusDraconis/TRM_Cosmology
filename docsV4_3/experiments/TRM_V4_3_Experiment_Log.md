# TRM V4.3 — Experiment Log

**Branch:** `feature/v4.3-geometric-scale-interpretation`
**Base:** `v4.2-physical-calibration-complete` (1744 tests)
**Date:** 2026-07-15

---

## V4_3_GeometricScaleCandidateSurvey_Tests.cs

**Tag:** `GSCS`
**Status:** SURVEY COMPLETE
**Classification:** A / B / C / REJECT

### Purpose

Survey all plausible TRM geometric scale candidates (A–L) that may serve as alternatives, refinements, or interpretations of the MeanDist length anchor established in V4.2.

### Tests

| # | Test Name | Status | Notes |
|:--:|:---|:---|:---|
| 01 | `V4_3_GSCS_01_FrozenPredictionManifestVerified` | PASS | V4.2 manifest confirmed intact |
| 02 | `V4_3_GSCS_02_MeanDistBaselineLoaded` | PASS | Baseline CV validated |
| 03 | `V4_3_GSCS_03_CandidateCatalogGenerated` | PASS | 12 candidates catalogued |
| 04 | `V4_3_GSCS_04_StabilityMetricsComputed` | PASS | Seed CV across 15 seeds |
| 05 | `V4_3_GSCS_05_LawRobustnessComputed` | PASS | Exp vs. Gaussian drift |
| 06 | `V4_3_GSCS_06_NullControlsComputed` | PASS | Structured-null separation |
| 07 | `V4_3_GSCS_07_WeakFieldCompatibilityComputed` | PASS | Correlation with MeanDist |
| 08 | `V4_3_GSCS_08_GeodesicCompatibilityComputed` | PASS | Direct vs. geodesic |
| 09 | `V4_3_GSCS_09_ObserverFrameCompatibilityComputed` | PASS | Sub-frame stability |
| 10 | `V4_3_GSCS_10_CandidateRankingComputed` | PASS | Composite ranking |
| 11 | `V4_3_GSCS_11_CandidateClassificationGenerated` | PASS | A/B/C classification |
| 12 | `V4_3_GSCS_12_NoPhysicalComparisonUsed` | PASS | Verification gate |
| 13 | `V4_3_GSCS_13_SurveyClassification` | PASS | Self-assessment |
| 14 | `V4_3_GSCS_14_ClaimDisciplineReport` | PASS | Full claim discipline |

### Candidate Summary

| ID | Candidate | Class | Key Finding |
|:--:|:---|:--:|:---|
| A | MeanDist | **A** | Baseline; persistent CV ~0.30 |
| B | MedianDist | **B** | Robust central tendency |
| C | TrimmedMeanDist | **B** | Outlier-resistant; comparable to MD |
| D | GeodesicMeanDist | **C** | O(N³) cost; captures topology |
| E | LocalShellScale | **B** | Local structure well-defined |
| F | CurvatureRadiusProxy | **C** | Experimental definition |
| G | CausalHorizonScale | **B** | Causal interpretation |
| H | SpectralScale | **C** | Experimental spectral proxy |
| I | PercentileDistanceScale | **B** | P90 tail probe |
| J | MetricProxyScale | **C** | Gram-volume proxy |
| K | CurvatureShellScale | **B** | Shell-banded curvature zone |
| L | ObserverFrameScale | **B** | Omega-anchored observer frame |

### Outputs

1. Candidate catalog — `V4_3_GSCS_03`
2. Stability table — `V4_3_GSCS_04`
3. Compatibility matrix — `V4_3_GSCS_05` through `V4_3_GSCS_09`
4. CV ranking — `V4_3_GSCS_10`
5. N-drift ranking — embedded in `V4_3_GSCS_10`
6. Law-drift ranking — `V4_3_GSCS_05`
7. Null-separation ranking — `V4_3_GSCS_06`
8. Geometric-interpretation notes — `V4_3_GSCS_03`, theory document

### Discipline

- No frozen predictions modified ✓
- No c_eff_SI recalibration ✓
- No G_eff_SI recalibration ✓
- No physical c comparison ✓
- No physical G comparison ✓
- Survey and classification only ✓

### Recommended Next Suite

`V4_3_GeometricScaleClassification_Tests.cs` — deep-dive classification on A/B candidates with expanded statistics (larger N, more seeds, power-law coupling).

### Theory Document

`docsV4_3/theory/TRM_V4_3_Geometric_Scale_Candidate_Survey.md`

---

## V4_3_GeometricScaleClassification_Tests.cs

**Tag:** `GSC`
**Status:** CLASSIFICATION COMPLETE
**Classification:** Geometric class structure confirmed

### Purpose

Determine which geometric scale candidates represent distinct geometric structures and which are merely different estimators of the same underlying scale. Uses pairwise correlations, hierarchical clustering, and stability analysis to discover emergent geometric scale classes from the 8 non-C GSCS candidates.

### Tests

| # | Test Name | Status | Notes |
|:--:|:---|:---|:---|
| 01 | `V4_3_GSC_01_CandidateSetLoaded` | PASS | 8 candidates loaded |
| 02 | `V4_3_GSC_02_PairwisePearsonCorrelationMatrix` | PASS | Linear correlation structure |
| 03 | `V4_3_GSC_03_PairwiseSpearmanCorrelationMatrix` | PASS | Rank correlation structure |
| 04 | `V4_3_GSC_04_DistanceMatrixComputed` | PASS | 1 − \|r\| distance matrix |
| 05 | `V4_3_GSC_05_HierarchicalClusteringPerformed` | PASS | Complete-linkage clustering |
| 06 | `V4_3_GSC_06_ClassDiscoveryPerformed` | PASS | Q50-threshold class assignment |
| 07 | `V4_3_GSC_07_SeedStabilityOfClasses` | PASS | Bootstrap co-clustering (5 iter) |
| 08 | `V4_3_GSC_08_NScalingStabilityOfClasses` | PASS | N=40, 80 persistence |
| 09 | `V4_3_GSC_09_LawRobustnessOfClasses` | PASS | Exp vs. gaussian persistence |
| 10 | `V4_3_GSC_10_NullControlClassSeparation` | PASS | Structure-dependent classes |
| 11 | `V4_3_GSC_11_ClassPurityScores` | PASS | Intra-r vs. inter-r per class |
| 12 | `V4_3_GSC_12_ClassHierarchyGenerated` | PASS | Dendrogram merge order |
| 13 | `V4_3_GSC_13_NoPhysicalComparisonUsed` | PASS | Verification gate |
| 14 | `V4_3_GSC_14_Classification` | PASS | Overall class rating |
| 15 | `V4_3_GSC_15_ClaimDisciplineReport` | PASS | Full claim discipline |

### Outputs

1. Correlation matrix (Pearson + Spearman) — `GSC_02`, `GSC_03`
2. Distance matrix — `GSC_04`
3. Class hierarchy / dendrogram — `GSC_05`, `GSC_12`
4. Class discovery (Q50 threshold) — `GSC_06`
5. Stability analysis (seed, N, law) — `GSC_07`, `GSC_08`, `GSC_09`
6. Class purity scores — `GSC_11`
7. Null-control verification — `GSC_10`

### Discipline

- No physical c/G comparison ✓
- No V4.2 recalibration ✓
- No SI comparison results used ✓
- Geometric classification only ✓

### Recommended Next Suite

`V4_3_GlobalVsLocalScale_Tests.cs` — investigate whether global distance scale and local/shell scale are genuinely distinct.

### Theory Document

`docsV4_3/theory/TRM_V4_3_Geometric_Scale_Classification.md`

---

## V4_3_GlobalVsLocalScale_Tests.cs

**Tag:** `GVLS`
**Status:** ANALYSIS COMPLETE
**Classification:** GLOBAL-DOMINATED / LOCAL-DOMINATED / MULTI-SCALE / UNRESOLVED

### Purpose

Determine whether TRM geometry is better characterized by a global geometric scale or a local geometric scale. Tests three candidate groups: GLOBAL (MeanDist, MedianDist, TrimmedMeanDist, PercentileP90), LOCAL (LocalShellScale, CurvatureShellScale, ObserverFrameScale), and CAUSAL (CausalHorizonScale).

### Tests

| # | Test Name | Status | Notes |
|:--:|:---|:---|:---|
| 01 | `V4_3_GVLS_01_FrozenInputsAvailable` | PASS | GSCS/GSC inputs confirmed |
| 02 | `V4_3_GVLS_02_GlobalScaleMetricsComputed` | PASS | Within-group r, globality score |
| 03 | `V4_3_GVLS_03_LocalScaleMetricsComputed` | PASS | Within-group r, locality score |
| 04 | `V4_3_GVLS_04_HierarchyMetricsComputed` | PASS | Rank ordering, hierarchy score |
| 05 | `V4_3_GVLS_05_CrossScaleCorrelationComputed` | PASS | Full 8×8 correlation matrix |
| 06 | `V4_3_GVLS_06_SeedStabilityConfirmed` | PASS | CV across 12 seeds |
| 07 | `V4_3_GVLS_07_NScalingConfirmed` | PASS | N=40, 80 persistence |
| 08 | `V4_3_GVLS_08_LawRobustnessConfirmed` | PASS | Exp vs. gaussian |
| 09 | `V4_3_GVLS_09_NullControlsDestroyHierarchy` | PASS | Random distances abolish structure |
| 10 | `V4_3_GVLS_10_ScaleIndependenceComputed` | PASS | Global vs. local composite r |
| 11 | `V4_3_GVLS_11_GlobalVsLocalClassification` | PASS | DOMINATED/MULTI/UNRESOLVED |
| 12 | `V4_3_GVLS_12_DocumentationGenerated` | PASS | Output summary |
| 13 | `V4_3_GVLS_13_NoPhysicalComparisonUsed` | PASS | Verification gate |
| 14 | `V4_3_GVLS_14_ClaimDisciplineReport` | PASS | Full claim discipline |

### Outputs

1. Global scale table — `GVLS_02`
2. Local scale table — `GVLS_03`
3. Correlation matrix — `GVLS_05`
4. Hierarchy score — `GVLS_04`
5. Scale independence — `GVLS_10`
6. Classification — `GVLS_11`

### Discipline

- No physical c/G comparison ✓
- No V4.2 recalibration ✓
- No SI comparison results ✓
- Geometric interpretation only ✓

### Recommended Next Suite

`V4_3_GeometricScaleHierarchy_Tests.cs`

### Theory Document

`docsV4_3/theory/TRM_V4_3_Global_Vs_Local_Scale.md`

---

## V4_3_GeometricScaleHierarchy_Tests.cs

**Tag:** `GSH`
**Status:** HIERARCHY ANALYSIS COMPLETE
**Classification:** STRONG HIERARCHY / MODERATE HIERARCHY / WEAK HIERARCHY / NO HIERARCHY

### Purpose

Determine whether TRM geometry contains a genuine hierarchy of geometric scales. Tests whether the identified scales are independent, nested, hierarchical, or redundant, and whether they form a reproducible scale structure with directed parent→child dependencies.

### Tests

| # | Test Name | Status | Notes |
|:--:|:---|:---|:---|
| 01 | `V4_3_GSH_01_FrozenInputsAvailable` | PASS | GSCS/GSC/GVLS frozen inputs confirmed |
| 02 | `V4_3_GSH_02_CandidateClassesLoaded` | PASS | 8 candidates, 3 groups |
| 03 | `V4_3_GSH_03_HierarchyGraphGenerated` | PASS | Directed R² asymmetry edges |
| 04 | `V4_3_GSH_04_ScaleDependencyGraphGenerated` | PASS | Full 8×8 R² matrix |
| 05 | `V4_3_GSH_05_HierarchyStrengthComputed` | PASS | Strength, depth, direction |
| 06 | `V4_3_GSH_06_SeedStabilityVerified` | PASS | Bootstrap (8 iterations) |
| 07 | `V4_3_GSH_07_NScalingVerified` | PASS | N=40, 80 persistence |
| 08 | `V4_3_GSH_08_LawRobustnessVerified` | PASS | Exp vs. gaussian |
| 09 | `V4_3_GSH_09_LoadRobustnessVerified` | PASS | s=0.05, 0.10, 0.20 |
| 10 | `V4_3_GSH_10_NullControlsDestroyHierarchy` | PASS | Random distances, K=0 |
| 11 | `V4_3_GSH_11_HierarchyClassification` | PASS | STRONG/MODERATE/WEAK/NO |
| 12 | `V4_3_GSH_12_DocumentationGenerated` | PASS | Output summary |
| 13 | `V4_3_GSH_13_NoPhysicalComparisonUsed` | PASS | Verification gate |
| 14 | `V4_3_GSH_14_ClaimDisciplineReport` | PASS | Full claim discipline |

### Outputs

1. Hierarchy graph — `GSH_03`
2. Dependency graph (R² matrix) — `GSH_04`
3. Hierarchy strength + depth + direction — `GSH_05`
4. Reproducibility score (bootstrap) — `GSH_06`
5. Hierarchy classification — `GSH_11`

### Discipline

- No physical c/G comparison ✓
- No V4.2 recalibration ✓
- No SI comparison results ✓
- Geometric interpretation only ✓

### Recommended Next Suite

`V4_3_GeometricScaleInterpretation_Tests.cs`

### Theory Document

`docsV4_3/theory/TRM_V4_3_Geometric_Scale_Hierarchy.md`

---

## V4_3_GeometricScaleInterpretation_Tests.cs

**Tag:** `GSI`
**Status:** INTERPRETATION COMPLETE
**Classification:** PRIMARY / SECONDARY / DERIVED / BRIDGE / REDUNDANT

### Purpose

Assign geometric roles to each scale class based on the frozen V4.3 evidence chain (GSCS → GSC → GVLS → GSH). Computes structural role, hierarchy contribution, information flow, and geometric uniqueness scores for all 8 candidates.

### Tests

| # | Test Name | Status | Notes |
|:--:|:---|:---|:---|
| 01 | `V4_3_GSI_01_FrozenInputsVerified` | PASS | GSCS/GSC/GVLS/GSH frozen inputs confirmed |
| 02 | `V4_3_GSI_02_HierarchyLoaded` | PASS | DAG structure loaded |
| 03 | `V4_3_GSI_03_GlobalScaleRoleComputed` | PASS | GLOBAL role + downstream edges |
| 04 | `V4_3_GSI_04_LocalScaleRoleComputed` | PASS | LOCAL role + independence from GLOBAL |
| 05 | `V4_3_GSI_05_CausalScaleRoleComputed` | PASS | CAUSAL role + bridge score |
| 06 | `V4_3_GSI_06_ObserverScaleRoleComputed` | PASS | ObserverFrame uniqueness analysis |
| 07 | `V4_3_GSI_07_StructuralRoleScoresComputed` | PASS | Full role-score matrix |
| 08 | `V4_3_GSI_08_InformationFlowScoresComputed` | PASS | Between-group R² direction |
| 09 | `V4_3_GSI_09_BridgeScaleDetectionComputed` | PASS | Cross-domain bridge candidates |
| 10 | `V4_3_GSI_10_RedundantScaleDetectionComputed` | PASS | R² > 0.95 redundancy check |
| 11 | `V4_3_GSI_11_InterpretationClassificationGenerated` | PASS | PRIMARY/SECONDARY/DERIVED/BRIDGE/REDUNDANT |
| 12 | `V4_3_GSI_12_DocumentationGenerated` | PASS | Output summary |
| 13 | `V4_3_GSI_13_NoPhysicalComparisonUsed` | PASS | Verification gate |
| 14 | `V4_3_GSI_14_ClaimDisciplineReport` | PASS | Full claim discipline |

### Outputs

1. Scale-role matrix — `GSI_07`
2. Hierarchy interpretation — `GSI_02`, `GSI_03`, `GSI_04`
3. Bridge-scale analysis — `GSI_05`, `GSI_09`
4. Uniqueness analysis — `GSI_06`, `GSI_10`
5. Recommended primary scale — `GSI_11`

### Discipline

- No physical c/G comparison ✓
- No V4.2 recalibration ✓
- No SI comparison results ✓
- Geometric interpretation only ✓

### Recommended Next Suite

`V4_3_GeometricScaleSelection_Tests.cs`

### Theory Document

`docsV4_3/theory/TRM_V4_3_Geometric_Scale_Interpretation.md`

---

## V4_3_GeometricScaleSelection_Tests.cs

**Tag:** `GSS`
**Status:** SELECTION COMPLETE
**Classification:** PRIMARY / SECONDARY / RESERVE / REJECT

### Purpose

Select the recommended geometric scale for a future prediction branch based purely on geometric evidence from the full V4.3 chain (GSCS → GSC → GVLS → GSH → GSI). Composite SelectionScore combines stability, hierarchy, uniqueness, null separation, and bridge value. Forward-looking only — no V4.2 modifications.

### Tests

| # | Test Name | Status | Notes |
|:--:|:---|:---|:---|
| 01 | `V4_3_GSS_01_FrozenInputsVerified` | PASS | Full V4.3 chain confirmed |
| 02 | `V4_3_GSS_02_CandidatePoolLoaded` | PASS | 8 candidates, 3 groups |
| 03 | `V4_3_GSS_03_StabilityScoresComputed` | PASS | CV, N-drift, law-drift |
| 04 | `V4_3_GSS_04_HierarchyScoresComputed` | PASS | DAG level + outdegree |
| 05 | `V4_3_GSS_05_UniquenessScoresComputed` | PASS | 1 - max R² |
| 06 | `V4_3_GSS_06_BridgeScoresComputed` | PASS | Cross-group harmonic R² |
| 07 | `V4_3_GSS_07_SelectionScoresComputed` | PASS | Composite weighted ranking |
| 08 | `V4_3_GSS_08_PrimaryCandidateSelected` | PASS | Top non-redundant |
| 09 | `V4_3_GSS_09_SecondaryCandidateSelected` | PASS | Next-best complementary |
| 10 | `V4_3_GSS_10_ReserveCandidateSelected` | PASS | Fallback candidates |
| 11 | `V4_3_GSS_11_NoPhysicalComparisonUsed` | PASS | Verification gate |
| 12 | `V4_3_GSS_12_NoRetrospectiveOptimization` | PASS | Fixed a priori weights |
| 13 | `V4_3_GSS_13_DocumentationGenerated` | PASS | Output summary |
| 14 | `V4_3_GSS_14_ClaimDisciplineReport` | PASS | Full claim discipline |

### Selection Criteria

| Criterion | Weight | Rationale |
|:---|:--:|:---|
| Stability (CV + N-drift + law-drift) | 0.25 | Reproducibility |
| Hierarchy (DAG position + outdegree) | 0.25 | Structural centrality |
| Uniqueness (1 - max R²) | 0.20 | Non-redundant information |
| Null separation | 0.15 | Structure-dependence |
| Bridge value (cross-group R²) | 0.15 | Geometric breadth |

### Discipline

- No physical c/G comparison ✓
- No V4.2 recalibration ✓
- Fixed a priori weights ✓
- Forward-looking only ✓

### Recommended Next Suite

`V4_3_ProspectiveLengthAnchorProtocol_Tests.cs`

### Theory Document

`docsV4_3/theory/TRM_V4_3_Geometric_Scale_Selection.md`

---

## V4_3_ProspectiveLengthAnchorProtocol_Tests.cs

**Tag:** `PLAP`
**Status:** PROTOCOL DEFINED
**Classification:** READY / PROMISING / EXPERIMENTAL / REJECT

### Purpose

Evaluate whether the GSS-selected geometric scale candidate can serve as a future length anchor while preserving anti-circularity, calibration discipline, prediction freezing, SI mapping compatibility, and causal-geometry consistency. References frozen V4.2 manifests without rerunning them.

### Tests

| # | Test Name | Status | Notes |
|:--:|:---|:---|:---|
| 01 | `V4_3_PLAP_01_FrozenInputsVerified` | PASS | Full V4.3 chain + V4.2 manifests |
| 02 | `V4_3_PLAP_02_CandidateLoaded` | PASS | All 8 candidates spot-verified |
| 03 | `V4_3_PLAP_03_StabilityVerified` | PASS | Seed CV, determinism |
| 04 | `V4_3_PLAP_04_HierarchyCompatibilityVerified` | PASS | GSI role compatibility |
| 05 | `V4_3_PLAP_05_CalibrationCompatibilityVerified` | PASS | ETCE/ELCE/ESCE compatible |
| 06 | `V4_3_PLAP_06_SIPredictionCompatibilityVerified` | PASS | c_eff invariant, G_eff sensitive |
| 07 | `V4_3_PLAP_07_ErrorBudgetCompatibilityVerified` | PASS | CV³ propagation framework |
| 08 | `V4_3_PLAP_08_AntiCircularityVerified` | PASS | 10/10 gates pass |
| 09 | `V4_3_PLAP_09_NullControlsVerified` | PASS | Frozen GSCS/GVLS/GSH nulls |
| 10 | `V4_3_PLAP_10_FreezeProtocolDefined` | PASS | 4-phase protocol |
| 11 | `V4_3_PLAP_11_FutureUseClassification` | PASS | READY/PROMISING/EXPERIMENTAL/REJECT |
| 12 | `V4_3_PLAP_12_DocumentationGenerated` | PASS | Output summary |
| 13 | `V4_3_PLAP_13_NoPhysicalComparisonUsed` | PASS | Verification gate |
| 14 | `V4_3_PLAP_14_ClaimDisciplineReport` | PASS | Full claim discipline |

### Key Findings

- c_eff_SI invariant to any multiplicative length proxy ✓
- G_eff_SI cubic-sensitive to proxy CV △
- All 10 anti-circularity gates pass ✓
- 4-phase freeze protocol defined ✓
- No physical comparison used ✓

### Discipline

- No V4.2 prediction modification ✓
- No c_eff_SI/G_eff_SI recalculation ✓
- No physical c/G comparison ✓
- Protocol evaluation only ✓

### Recommended Next Suite

`V4_3_GeometricScaleBranchSynthesis_Tests.cs`

### Theory Document

`docsV4_3/theory/TRM_V4_3_Prospective_Length_Anchor_Protocol.md`

---

## V4_3_GeometricScaleBranchSynthesis_Tests.cs

**Tag:** `GSBS`
**Status:** SYNTHESIS COMPLETE
**Classification:** COMPLETE

### Purpose

Synthesize the complete V4.3 geometric-scale interpretation program into a branch-ready summary. Loads frozen results from all 7 prior suites, generates supported/conditional/hypothesis/not-claimed claim structure, documents 8 open problems, and recommends the next branch.

### Tests

| # | Test Name | Status | Notes |
|:--:|:---|:---|:---|
| 01 | `V4_3_GSBS_01_CandidateDiscoveryLoaded` | PASS | GSCS findings synthesized |
| 02 | `V4_3_GSBS_02_ClassificationLoaded` | PASS | GSC findings synthesized |
| 03 | `V4_3_GSBS_03_GlobalLocalLoaded` | PASS | GVLS findings synthesized |
| 04 | `V4_3_GSBS_04_HierarchyLoaded` | PASS | GSH findings synthesized |
| 05 | `V4_3_GSBS_05_InterpretationLoaded` | PASS | GSI findings synthesized |
| 06 | `V4_3_GSBS_06_SelectionLoaded` | PASS | GSS findings synthesized |
| 07 | `V4_3_GSBS_07_ProspectiveProtocolLoaded` | PASS | PLAP findings synthesized |
| 08 | `V4_3_GSBS_08_SupportedFindingsGenerated` | PASS | Final SUPPORTED claims |
| 09 | `V4_3_GSBS_09_ConditionalFindingsGenerated` | PASS | Final CONDITIONAL claims |
| 10 | `V4_3_GSBS_10_HypothesesGenerated` | PASS | Final HYPOTHESIS claims |
| 11 | `V4_3_GSBS_11_OpenProblemsGenerated` | PASS | 8 open problems |
| 12 | `V4_3_GSBS_12_CompletionClassification` | PASS | Program COMPLETE |
| 13 | `V4_3_GSBS_13_DocumentationGenerated` | PASS | Completion doc path |
| 14 | `V4_3_GSBS_14_ClaimDisciplineReport` | PASS | Final discipline report |

### Key Synthesis

- MeanDist remains the recommended baseline (no clearly superior alternative found)
- c_eff_SI structurally robust to any multiplicative proxy
- G_eff_SI cubic-sensitive — primary motivation for continued refinement
- 8 open problems documented for future investigation
- Recommended next branch: `feature/v4.4-prospective-length-anchor-validation`

### Completion Document

`docsV4_3/TRM_V4_3_Geometric_Scale_Interpretation_Completion.md`

---

## V4.3 — Final Summary

| Suite | Tag | Tests | Question | Status |
|:---|:---|:--:|:---|:---|
| Candidate Survey | GSCS | 14 | What scales exist? | ✅ |
| Classification | GSC | 15 | Do they form classes? | ✅ |
| Global vs. Local | GVLS | 14 | Global or local? | ✅ |
| Hierarchy | GSH | 14 | Hierarchy or flat? | ✅ |
| Interpretation | GSI | 14 | What role does each play? | ✅ |
| Selection | GSS | 14 | Which scale to select? | ✅ |
| Prospective Protocol | PLAP | 14 | Can it be a future anchor? | ✅ |
| Branch Synthesis | GSBS | 14 | What does it all mean? | ✅ |
| **V4.3 TOTAL** | | **113** | **Geometric interpretation COMPLETE** | ✅ |

---

## Experiment History

| Date | Suite | Tests | Outcome |
|:---|:---|:--:|:---|
| 2026-07-15 | GSBS | 14 | SYNTHESIS COMPLETE |
| 2026-07-15 | PLAP | 14 | PROTOCOL DEFINED |
| 2026-07-15 | GSS | 14 | SELECTION COMPLETE |
| 2026-07-15 | GSI | 14 | INTERPRETATION COMPLETE |
| 2026-07-15 | GSH | 14 | HIERARCHY ANALYSIS COMPLETE |
| 2026-07-15 | GVLS | 14 | ANALYSIS COMPLETE |
| 2026-07-15 | GSC | 15 | CLASSIFICATION COMPLETE |
| 2026-07-15 | GSCS | 14 | SURVEY COMPLETE |
