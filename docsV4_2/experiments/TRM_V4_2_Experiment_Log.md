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
