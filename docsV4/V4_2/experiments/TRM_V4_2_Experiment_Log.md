# V4.2 — Experiment Log

**Branch:** feature/v4.2-physical-calibration-and-prediction

---

## 2026-07-14 — ETCE — External Time Calibration Execution

- Suite: V4_2_ExternalTimeCalibrationExecution_Tests.cs
- Tests: 14/14 passed (13s)
- Classification: A READY
- Omega_ref: finite, positive, CV ~0.01
- T_scale: reproducible, stochastic uncertainty < 2%
- N-scaling bias: systematic, reported separately
- Anti-circularity: 7/7 gates pass
- Total project: 1464 tests (1450 V4.1 + 14 V4.2)

---
## 2026-07-14 — ELCE — External Length Calibration Execution

- Suite: V4_2_ExternalLengthCalibrationExecution_Tests.cs
- Tests: 14/14 passed (13s)
- Classification: A READY
- MeanDist_ref: finite, positive, CV ~0.30
- L_scale: reproducible, stochastic uncertainty bounded
- Independent of time, c_eff, G_eff, source calibration
- Anti-circularity: all gates pass
- Total project: 1478 tests

---
## 2026-07-14 — ESCE — External Source Calibration Execution

- Suite: V4_2_ExternalSourceCalibrationExecution_Tests.cs
- Tests: 14/14 passed
- Classification: A READY
- OmegaSource_ref: finite, positive, CV ~0.01
- M_scale: reproducible, independent of T_scale, L_scale, c_eff, G_eff
- All three anchors now calibrated
- Total project: 1492 tests
- Next: V4_2_BlindCEffPrediction_Tests.cs

---
## 2026-07-14 — BCEP — Blind c_eff Prediction

- Suite: V4_2_BlindCEffPrediction_Tests.cs
- Tests: 14/14 passed (44s)
- Classification: PREDICTION READY
- c_eff_predicted computed from frozen T_scale + L_scale
- Deterministic, seed-stable, anti-circularity enforced
- No comparison to physical c executed
- Total project: 1506 tests
- Next: V4_2_BlindGEffPrediction_Tests.cs

---
## 2026-07-14 — BGEP — Blind G_eff Prediction

- Suite: V4_2_BlindGEffPrediction_Tests.cs
- Tests: 14/14 passed (39s)
- Classification: PREDICTION READY
- G_eff_predicted = G_eff_internal × L³/(T²·M)
- Dimensional consistency verified
- Deterministic, anti-circularity enforced
- No comparison to physical G executed
- Total project: 1520 tests
- All 5 calibration+prediction suites complete

---
## 2026-07-14 — BPCP — Blind Prediction Comparison Protocol

- Suite: V4_2_BlindPredictionComparisonProtocol_Tests.cs
- Tests: 14/14 passed
- Classification: COMPARISON READY
- Governance protocol, audit trail, hashing, anti-feedback defined
- No comparison to physical c or G executed
- Total project: 1534 tests
- All 6 V4.2 suites complete

---
## 2026-07-14 — PFA — Prediction Freeze and Audit

- Suite: V4_2_PredictionFreezeAndAudit_Tests.cs
- Tests: 14/14 passed
- Classification: AUDIT READY
- SHA-256 hashes, manifest, audit trail, tamper evidence defined
- No comparison to physical c or G executed
- Total project: 1548 tests
- All 7 V4.2 suites complete — pre-comparison freeze done

---
## 2026-07-14 — BPCC — Blind Physical Constant Comparison

- Suite: V4_2_BlindPhysicalConstantComparison_Tests.cs
- Tests: 14/14 passed
- Classification: COMPARISON EXECUTED — PROTOCOL COMPLETE
- Audit verified, predictions unchanged, no parameter modification
- DIMENSIONAL UNIT GAP ACKNOWLEDGED: predictions dimensionless, physical constants in SI
- Numerical comparison is protocol-level only — not physically interpretable
- Total project: 1562 tests
- All 8 V4.2 suites complete

---
## 2026-07-14 — SIUMP — SI Unit Mapping Policy

- Suite: V4_2_SIUnitMappingPolicy_Tests.cs
- Tests: 14/14 passed
- Classification: MAPPING FRAMEWORK READY
- Admissible/forbidden anchors, anti-circularity, audit framework defined
- Critical finding: SI meter depends on c — independent length standard required (Option A)
- No SI mapping executed
- Total project: 1576 tests
- All 9 V4.2 suites complete

---
## 2026-07-14 — SITMD — SI Time Mapping Design

- Suite: V4_2_SI_Time_Mapping_Design_Tests.cs
- Tests: 14/14 passed
- Classification: A DESIGN READY
- Cs-133 hyperfine reference: admissible, independent of c, G, length, source
- SI_T_scale formula defined
- No circularity, no astrophysical data
- Total project: 1590 tests

---
## 2026-07-14 — SILMD — SI Length Mapping Design

- Suite: V4_2_SI_Length_Mapping_Design_Tests.cs
- Tests: 14/14 passed
- Classification: A DESIGN READY
- Option A: Kr-86 wavelength standard (pre-1983, independent of c)
- Option B: Modern SI meter (CONDITIONAL, disclosed c-circularity)
- All forbidden references rejected
- Total project: 1604 tests

---
## 2026-07-14 — SISMD — SI Source Mapping Design

- Suite: V4_2_SI_Source_Mapping_Design_Tests.cs
- Tests: 14/14 passed
- Classification: A DESIGN READY
- SI kilogram (2019, h-based) admissible — independent of G and c
- All three SI mapping designs complete
- Total project: 1618 tests
- Next: V4_2_SI_Calibrated_Predictions_Tests.cs

---
## 2026-07-14 — SICP — SI Calibrated Predictions

- Suite: V4_2_SI_Calibrated_Predictions_Tests.cs
- Tests: 14/14 passed
- Classification: A SI PREDICTION READY
- c_eff_SI computed via Kr-86 primary path
- G_eff_SI computed via L^3/(T^2*M) dimensional form
- Modern SI meter CONDITIONAL only (c-circularity)
- No physical comparison executed
- Total project: 1632 tests
- Next: V4_2_SI_Physical_Comparison_Tests.cs

---
## 2026-07-14 — SIPC — SI Physical Comparison

- Suite: V4_2_SI_Physical_Comparison_Tests.cs
- Tests: 14/14 passed (7s)
- Classification: A COMPARISON COMPLETE
- Kr-86 primary path, no modern-meter circularity
- c_eff_SI and G_eff_SI compared to CODATA 2018 values
- Error metrics computed, predictions unchanged
- Honest reporting enforced
- Total project: 1646 tests
- V4.2 SI comparison chain COMPLETE

---
## 2026-07-14 — PCIT — Post-Comparison Interpretation

- Suite: V4_2_PostComparisonInterpretation_Tests.cs
- Tests: 14/14 passed
- Classification: INTERPRETATION COMPLETE
- SUPPORTED/CONDITIONAL/HYPOTHESIS/NOT CLAIMED matrix enforced
- Non-tuning mismatch diagnostics defined
- Recommended next experiments listed
- Total project: 1660 tests
- V4.2 chain: 15 suites, 210 tests COMPLETE

---
## 2026-07-14 — SIEBS — SI Error Budget Sensitivity

- Suite: V4_2_SI_ErrorBudgetSensitivity_Tests.cs
- Tests: 14/14 passed (41s)
- Classification: A — DOMINANT ERROR SOURCES IDENTIFIED
- KEY FINDING: c_eff_SI MeanDist CANCELS — c_eff_SI depends ONLY on Omega (CV~0.01)
- KEY FINDING: G_eff_SI depends on (Omega/MeanDist)^3 — MeanDist (3x~0.30) dominates
- Total project: 1674 tests
- 16 V4.2 suites complete

---
## 2026-07-14 — CBN500 — Continuum Beyond N=500

- Suite: V4_2_ContinuumBeyondN500_Tests.cs
- Tests: 14/14 passed (~12min including N=800,1000)
- Classification: A — CONTINUUM CHARACTERIZED BEYOND N=500
- Omega ultra-stable at all N (CV ~0.01)
- c_eff_SI cancellation verified at N=500,800,1000
- MeanDist CV persists (~0.30) — proxy limitation or structural
- G_eff_SI remains MD-dominated at all N
- Total project: 1688 tests
- Next: V4_2_MeanDistAnchorRefinement_Tests.cs

---
## 2026-07-14 — MDAR — MeanDist Anchor Refinement

- Suite: V4_2_MeanDistAnchorRefinement_Tests.cs
- Tests: 14/14 passed (39s)
- Classification: A — PROXY ANALYSIS COMPLETE
- 7 proxies compared: MeanDist, Median, TrimmedMean, GeodesicMean, LocalShell, P50, P75
- c_eff cancellation holds for all multiplicative proxies
- MeanDist CV ~0.30 — proxies show similar CV
- MeanDist retained as baseline
- Total project: 1702 tests

---
## 2026-07-14 — ATR — AlphaTRM Refinement

- Suite: V4_2_AlphaTRMRefinement_Tests.cs
- Tests: 14/14 passed
- Classification: A — ALPHA ANALYSIS COMPLETE
- 6 alpha candidates: Baseline, LocalWeighted, GeodesicWeighted, ShellAvg, Median, Trimmed
- All CV ~0.30 — similar to baseline
- Alpha refinement less impactful than length refinement (weight 1 vs 3)
- Baseline retained
- Total project: 1716 tests

---
## 2026-07-14 — EBR — Error Budget Reconciliation

- Suite: V4_2_ErrorBudgetReconciliation_Tests.cs
- Tests: 14/14 passed
- Classification: A RECONCILED
- c_eff_SI: Omega-dominated (CV ~0.01). MeanDist cancels by construction.
- G_eff_SI: MeanDist-dominated (cubic, CV ~0.90). Structural, not noise.
- No retroactive proxy substitution. Predictions unchanged.
- Total project: 1730 tests
- V4.2: 20 suites, 280 tests complete

---
## 2026-07-14 — PCBS — Physical Calibration Branch Synthesis

- Suite: V4_2_PhysicalCalibrationBranchSynthesis_Tests.cs
- Tests: 14/14 passed
- Classification: BRANCH READY FOR COMPLETION
- All 10 chains complete. 1744 total tests.

---
## 2026-07-14 — FINAL — V4.2 Branch Ready

- 21 suites, 294 tests added to V4.1 baseline (1450)
- Total: 1744 tests
- All 10 chains complete
- Homepage JSON fully updated with V4.2 claims, hypotheses, open problems
- Branch ready for tag: v4.2-physical-calibration-complete
- Recommended next: feature/v4.3-geometric-scale-interpretation
