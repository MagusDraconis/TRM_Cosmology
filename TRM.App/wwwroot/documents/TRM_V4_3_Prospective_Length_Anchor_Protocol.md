# TRM V4.3 — Prospective Length Anchor Protocol

**Status:** PROTOCOL DEFINED
**Suite:** `V4_3_ProspectiveLengthAnchorProtocol_Tests.cs`
**Tag:** `V4_3_PLAP`
**Branch:** `feature/v4.3-geometric-scale-interpretation`
**Base:** `v4.2-physical-calibration-complete` (1744 tests)
**Date:** 2026-07-15

---

## 1. Motivation

The V4.3 evidence chain (GSCS → GSC → GVLS → GSH → GSI → GSS) has:
- Surveyed 12 geometric scale candidates
- Classified them into stable geometric classes
- Analyzed global vs. local vs. causal structure
- Mapped the scale hierarchy DAG
- Assigned geometric roles (PRIMARY/SECONDARY/DERIVED/BRIDGE/REDUNDANT)
- Ranked candidates by composite SelectionScore

PLAP is the penultimate protocol suite: **Can the selected candidate serve as a future length anchor?** It evaluates compatibility with the V4.2 calibration framework, verifies anti-circularity, and defines a freeze protocol — all without modifying any frozen V4.2 result.

---

## 2. Candidate Assessment

The candidate is loaded from the GSS selection ranking. Key properties:

| Property | Requirement | Verified |
|:---|:---|:---|
| Multiplicative length proxy | c_eff cancellation must hold | ✓ (all candidates are multiplicative) |
| Deterministic | Fully determined by seed and N | ✓ |
| No external physical inputs | Computed from TRM distance matrix only | ✓ |
| GSI role | PRIMARY or SECONDARY | Evaluated from frozen GSI output |
| Stability | CV < 0.50 | Verified (PLAP_03) |

---

## 3. Calibration Compatibility

### 3.1 Time Calibration (ETCE)

```
T_scale = ExternalTimeRef / MeanOmega
```

Omega (mean angular velocity) is independent of the length proxy. T_scale is **unaffected** by length anchor choice. ✓

### 3.2 Length Calibration (ELCE)

```
L_scale = ExternalLengthRef / proxy_ref
```

Any multiplicative length proxy maintains the same calibration form. The L_scale value changes with the proxy, but the framework is compatible. ✓

### 3.3 Source Calibration (ESCE)

```
M_scale = ExternalSourceRef / MeanOmega
```

M_scale is **unaffected** by length anchor choice. ✓

### 3.4 Cross-Calibration

```
c_eff_SI = L_scale / T_scale ∝ Omega / proxy × proxy = Kr86/Cs133 × Omega
```

The proxy cancels in c_eff_SI — this holds for **any** multiplicative length proxy. ✓

```
G_eff_SI = alpha_TRM × L³ / (T² × M)
```

G_eff_SI is **proxy-sensitive** (cubic dependence on L_scale, which depends on 1/proxy). A proxy change changes the G_eff prediction. This is the primary motivation for proxy refinement. △

---

## 4. Error Budget Impact

| Quantity | Current (MeanDist) | With Lower-CV Proxy |
|:---|:---|:---|
| c_eff_SI uncertainty | Omega-dominated (CV ~0.01) | Unchanged (~0.01) |
| G_eff_SI uncertainty | L³-dominated (CV ~0.90) | 3 × CV_proxy (first order) |
| G_eff effective CV | sqrt((3×0.30)² + 0.15²) ≈ 0.91 | sqrt((3×CV_new)² + 0.15²) |

Reducing proxy CV from 0.30 to 0.20 reduces G_eff CV from ~0.90 to ~0.60. ✓

---

## 5. Anti-Circularity

All 10 circularity gates pass:

1. Candidate not defined using physical c ✓
2. Candidate not defined using physical G ✓
3. Candidate not ranked using physical c agreement ✓
4. Candidate not ranked using physical G agreement ✓
5. Selection weights not tuned to physical constants ✓
6. SI comparison outcomes not used in selection ✓
7. Astrophysical data not used in selection ✓
8. Candidate not selected post-comparison ✓
9. Candidate not fitted to minimize G_eff residual ✓
10. V4.2 predictions not modified during selection ✓

---

## 6. Null Controls

From frozen GSCS/GVLS/GSH outputs:
- Random distances → no hierarchy ✓
- K=0 → no geometric structure ✓
- Random distances → no class structure ✓

The discovered geometric structure is **not** a distance-metric artifact. ✓

---

## 7. Future Freeze Protocol

### Phase 1 — Pre-freeze (V4.3, this suite)
1. Candidate selected via geometric criteria (GSS)
2. Anti-circularity verified (PLAP_08)
3. Calibration compatibility verified (PLAP_05)
4. Error budget compatibility verified (PLAP_07)

### Phase 2 — Freeze (future branch)
1. Compute L_scale = ExternalLengthRef / proxy_ref
2. Recompute c_eff_SI (should be unchanged)
3. Recompute G_eff_SI with new L_scale
4. Generate SHA-256 manifest
5. Record branch, timestamp, test count, regime
6. Lock anti-feedback gates

### Phase 3 — Audit (before comparison)
1. Verify manifest hash reproducibility
2. Verify no mutable dependencies
3. Verify anti-feedback gates intact
4. Document uncertainty budget

### Phase 4 — Comparison (gated)
1. Blind comparison only after freeze
2. No re-freeze after comparison
3. No weight/sample selection using comparison outcomes

---

## 8. Future-Use Classification

| Classification | Criteria |
|:---|:---|
| **READY FOR FUTURE USE** | PRIMARY/SECONDARY role, CV < 0.50, not redundant, anti-circular, calibration-compatible |
| **PROMISING** | SECONDARY/RESERVE role or moderate CV; merits further investigation |
| **EXPERIMENTAL** | DERIVED role or C-candidate; requires definition refinement |
| **REJECT** | REDUNDANT or CV > 0.50 or requires physical inputs |

---

## 9. Risk Factors

| Risk | Severity | Mitigation |
|:---|:---|:---|
| G_eff cubic sensitivity | High | Full re-freeze and re-audit required |
| Proxy change shifts G_eff value | High | Blind comparison protocol |
| Observer-frame dependence | Medium | Verify frame-independence for primary |
| Coupling-law dependence | Low | Verified in GSH/GVLS |

---

## 10. Recommended Next Suite

`V4_3_GeometricScaleBranchSynthesis_Tests.cs` — synthesize the complete V4.3 geometric-scale interpretation into a branch-ready summary, including the recommended primary scale, protocol checklist, and transition plan.

---

## 11. Claim Discipline

### SUPPORTED

- Candidate loaded from GSS selection ranking.
- Stability, hierarchy, calibration, SI prediction, and error budget compatibility verified.
- Anti-circularity: all 10 gates pass.
- Null controls: structure destroyed under null conditions.
- Future freeze protocol defined (4 phases).
- Future-use classification: READY / PROMISING / EXPERIMENTAL / REJECT.

### CONDITIONAL

- This suite evaluates prospective suitability — no candidate is adopted.
- Any actual anchor change requires a new branch with full re-freeze.
- G_eff_SI is proxy-sensitive (cubic); a new anchor changes the prediction.
- SI-unit mapping uses dimensionless placeholders (1.0).

### HYPOTHESIS

- A geometrically-selected anchor may reduce G_eff_SI uncertainty.
- The freeze protocol is sufficient to prevent circularity.
- c_eff_SI is structurally robust to multiplicative proxy choice.

### NOT CLAIMED

- Any candidate adopted as length anchor.
- Physical c or G derived, compared, or used.
- SI calibration with actual SI values.
- V4.2 MeanDist baseline replaced.
- Spacetime, Lorentz, SR, GR, Einstein equations derived.
- Astrophysical data used.
