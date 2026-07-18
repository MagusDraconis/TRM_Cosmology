# TRM V5.14 RGP: Residual Geometry Protocol

**Suite:** RGP_ResidualGeometryProtocol
**Status:** INITIALIZED
**Date:** 2026-07-18

---

## 1. Purpose

Define how V5.14 will test whether remaining failures after the V5.13 M3 selector contain additional explainable geometry or represent the practical control limit of the current model.

---

## 2. Frozen Baseline — M3

### Model Specification

**Pathway:** P1/P1b Compression-Room only (P2 demoted to exploratory)

**Selector:** projHiVec > -0.3281

**N-Conditioning:**

| N | Regime | Action |
|---|--------|--------|
| 67 | Inaccessible | No pathway claim; negative control |
| 71 | Selector-useful | Apply projHiVec selector |
| 72 | Selector-useful | Apply projHiVec selector |
| 75 | Strong pathway | Selector bypass (use all P1/P1b) |
| 80 | Saturated | Universal protocols; pathway selection unnecessary |

**Intervention:** Matched strong relative compression (50% d reduction for P1/P1b)

### Frozen Thresholds

| Parameter | Value | Frozen From |
|-----------|-------|-------------|
| Omega branch | > 1.783 | V5.3 — do not redefine |
| projHiVec | > -0.3281 | V5.13 HVI/HVS |
| P1/P1b d0 | > 0.50 | V5.12 — do not redefine |
| P1b d0 | > 0.65 | V5.12 — do not redefine |
| P2 d0 | <= 0.40, K > 0.98, K-dist < 0.15 | V5.12 — demoted |

---

## 3. Residual Feature Families

All features are measured **before intervention** unless marked `[post]`.

### F1 — Geometry Residuals

| Feature | Description |
|---------|-------------|
| orthHiVec | Orthogonal distance from Hi-entry vector |
| distToHi | Distance to Natural High centroid in (d,K,Ks) space |
| distToLo | Distance to Natural Low centroid |
| entryScoreResid | Residual of entry score after projHiVec projection |
| projHiVecAngle | Angular deviation from Hi-entry direction |

### F2 — d-Distribution Residuals

| Feature | Description |
|---------|-------------|
| d_std | Standard deviation of d values |
| d_p90 | 90th percentile of d distribution |
| d_p95 | 95th percentile of d distribution |
| d_max | Maximum d value |
| d_tail_width | d_p95 - d_p75 |
| d_p90_over_p50 | Tail-heaviness ratio |
| d_p95_over_p50 | Extreme tail ratio |

### F3 — K-Distribution Residuals

| Feature | Description |
|---------|-------------|
| K_std | Standard deviation of K values |
| top1EdgeShare | Fraction of total K in top 1% edges |
| top5EdgeShare | Fraction of total K in top 5% edges |
| top10EdgeShare | Fraction of total K in top 10% edges |
| maxNodeKMean | Maximum per-node mean K |
| maxNodeKStd | Maximum per-node K std |

### F4 — Spectral Residuals

| Feature | Description |
|---------|-------------|
| lambda1 | Largest eigenvalue of K matrix |
| lambda2 | Second largest eigenvalue |
| spectralGap | lambda1 - lambda2 |
| K_Frob | Frobenius norm of K matrix |

### F5 — Trajectory Residuals

| Feature | Description |
|---------|-------------|
| d_velocity | d_mean change from previous epoch |
| K_velocity | K_mean change from previous epoch |
| d_acceleration | Change in d_velocity |
| K_acceleration | Change in K_velocity |
| trajectoryCurvature | Rate of direction change in (d,K) space |
| reboundProxy | Tendency to reverse direction |

### F6 — Intervention Response Residuals [post]

| Feature | Description |
|---------|-------------|
| delta_dRel_achieved | Actual relative d reduction achieved |
| displacement_along_HiVec | Displacement along Hi-entry direction |
| insufficient_disp_indicator | Binary: did d reach target? |
| over_compress_indicator | Binary: d_mean > 0.85 after intervention |
| post_rebound | d recovery after initial compression |
| post_K_collapse | K drops below threshold after intervention |

---

## 4. Allowed Models

Only transparent methods:

| Method | Max Complexity |
|--------|---------------|
| Threshold rule | Single-feature split |
| Logistic regression | Linear only |
| LDA | Linear discriminant |
| Shallow decision tree | Max depth = 2 |

**No black-box models.**

---

## 5. Validation Design

### Train/Holdout Split

| Cohort | Seeds | Use |
|--------|-------|-----|
| Reference | 0-99 | Train threshold/coefficients |
| Holdout | 100-199 | Test threshold/coefficients |
| Second holdout (optional) | 200-299 | Additional validation |

### Success Criterion

A residual feature is **validated** if:
- Trained on 0-99, tested on 100-199
- Improves holdout persistence rate over M3 baseline by ≥ 5% at N=71 or N=72
- Does not harm N=75 (degradation ≤ 2%)
- Feature and threshold/coefficient are frozen before testing

---

## 6. Decision Gates

| Gate | Condition | Interpretation |
|------|-----------|----------------|
| **A** — Residual Geometry Found | ≥1 feature improves holdout vs M3 by ≥5% without overfit | Additional explainable geometry exists |
| **B** — Residual Selector Validated | Simple selector improves holdout at N=71/72, neutral at N=75 | M3 can be refined |
| **C** — N-Specific Residuals | Useful features differ by N | Model must remain N-conditioned |
| **D** — Control Ceiling Reached | No feature improves holdout vs M3 | Practical explanatory limit reached |
| **E** — Saturation Confirmed | N=80 broadly inducible regardless of selector | Saturation regime confirmed |
| **F** — Inaccessibility Confirmed | N=67 remains inaccessible under valid interventions | N=67 outside control domain |
| **G** — Overfit Warning | Feature improves train but fails holdout | Reject feature; preserve M3 |

---

## 7. Failure Criteria

A residual feature **fails** validation if any of:
- Holdout lift < 5% at N=71 and N=72
- Holdout degradation > 2% at N=75
- Feature was trained or thresholds tuned on holdout data
- Black-box model used
- Feature selected post-hoc after seeing holdout results

---

## 8. Claim Discipline

| Claim | Status |
|-------|--------|
| Physical interpretation of residual features | NOT CLAIMED |
| Universal Low→High control | NOT CLAIMED |
| Complete hidden-variable discovery | NOT CLAIMED |
| Attractor topology proof | NOT CLAIMED |
| Generalization beyond tested N/seeds | NOT CLAIMED |
| P2 pathway validity | NOT CLAIMED (demoted) |
| Residual features as sufficient conditions | NOT CLAIMED |

---

## 9. How to Run

```bash
# Run RGP protocol tests (fast, < 1s)
dotnet test --filter "Category=V5_14_RGP"

# Run full V5.14 suite with long-running tests
dotnet test --filter "Category=V5_14" --settings TRM.Tests/long-running.runsettings
```

---

## 10. Recommended Next Suite

**RGE — Residual Geometry Execution**

Collect residual features across all candidate seeds, apply M3 baseline, extract F1-F6 features, and prepare data for RGA analysis.
