# docsV4 — V4 Interpretation Layer Workspace

**Created:** 2026-07-05
**Branch:** `feature/v4-interpretation-layer`
**Status:** COMPLETE. B1–B6 + G1 + G2 + G3 + G4 closed. 184 xUnit tests passing.

---

## ⚠️ Critical Rule

```
/docs   → V3.4 FINAL (FROZEN) — do not modify
/docsV4 → V4 interpretation layer — active workspace
```

**These directories must NEVER be mixed.** V3.4 core is immutable.

---

## Directory Structure

```
docsV4/
    README.md                          ← this file
    TRM_V4_GR_Replacement_Roadmap.md   ← G1–G6 gap analysis
    theory/
        TRM_V4_Interpretation_Core.md  ← central interpretation framework
        TRM_V4_TimeField_Mapping.md    ← T(x) and φ(x) — C5 breakthrough formulation
        TRM_V4_Gravity_Model.md        ← C5 gravity: a(x) = c²/ρ_ref · ∇(δρ)
        TRM_V4_CouplingFieldEquation.md← B3 candidate catalog (A-D)
        TRM_V4_B3_ApproachDirections.md← B3 deep analysis
        TRM_V4_B3A_LaplaceUniqueness.md← Laplace = unique PDE ✓
        TRM_V4_B3B_BoundaryDefectOrigin.md ← 1/r from graph Laplacian
        TRM_V4_B3C_CoefficientMapping.md   ← α↔M, f_ref anchor
        TRM_V4_B4_DynamicCouplingField.md  ← □K = 0 dynamic extension
        TRM_V4_B5_ObservableDictionary.md  ← O1–O10 observables
        TRM_V4_Final_Status.md        ← CANONICAL V4 closure (updated)
        TRM_V4_G1_NonlinearFieldEquation.md ← G1: 1PN framework
        TRM_V4_G1T_MultiKTensorDynamics.md ← G1: Multi-K tensor
        TRM_V4_G1T2_NonlinearMultiKTensorDynamics.md ← G1: nonlinear Multi-K
        TRM_V4_G2_TensorBridge.md     ← G2: tensor bridge architecture
        TRM_V4_G2A_MetricExtraction.md← G2A: g_μν from K
        TRM_V4_G2B_LinearizedPolarizations.md ← G2B: GW polarizations
        TRM_V4_G2C_LorentzianKernelAndDispersion.md ← G2C: ω=ck
        TRM_V4_G2D_FullLorentzianClosure.md  ← G2D: quartic kernel
        TRM_V4_G3_StrongFieldTestbed.md ← G3: strong-field framework
    review/
        TRM_V4_Claim_Boundaries.md     ← what V4 claims and does not claim
        TRM_V4_Risks.md                ← known risks and mitigations
        TRM_V4_GravityTestAudit.md     ← full test classification audit
        TRM_V4_B1_CriticalReview.md    ← B1 critical review
        TRM_V4_B3C_T3_AnchorSelection.md ← I3 anchor selection
        TRM_V4_B6_BenchmarkProgram.md  ← B6 benchmarks (15/15)
        TRM_V4_B6_ResultMatrix.md      ← B6 result matrix
        TRM_V4_Reviewer_Attack_Surface.md ← reviewer attack surface
    papers/
        TRM_V4_Abstract.md             ← paper abstract
    experiments/
        TRM_V4_MappingTests.md         ← C5 evaluation + test plans
        TRM_V4_B1_Results.md           ← B1 coupling modulation matrix
        TRM_V4_B3C_T2_FrequencyScaleAnchor.md ← frequency scale anchor
        TRM_V4_B4_T1_PropagationSpeedClosure.md ← c_K = c
        TRM_V4_G1_KernelOptimization.md ← G1 kernel optimization → b≈1.25
        TRM_V4_G1_KernelSearch.md       ← G1 kernel search
        TRM_V4_G1T2a_BilocalCoefficientComputation.md ← a=3.237
        TRM_V4_G1T2b_FullTensorPPNBeta.md  ← PPN β extraction
        TRM_V4_G1T2c_FullTensorCubicCoefficients.md ← cubic coefficients
        TRM_V4_G1T2c1_TensorCoefficientExecution.md  ← coefficient execution
        TRM_V4_G1T2c2_PhysicalLagrangianExtraction.md ← Lagrangian
        TRM_V4_G1T2c3_TensorCompensationTest.md ← tensor compensation
        TRM_V4_G1T2d_FullTensor1PNClosure.md  ← G1 definitive closure
        TRM_V4_G1T2d_FinalBetaComputation.md  ← final β computation
        TRM_V4_deltaRho_CriticalValidation.md ← δρ validation
```

---

## C5 Breakthrough Summary

The central insight: **φ is not geometric — it is energetic.**

```
φ(x) = ρ_E(x) / ρ_ref           energy density → time-rate offset
φ₀   = ρ_bg / ρ_ref             global background → naturally ~0.17
δφ(x)= δρ(x) / ρ_ref            local perturbation → gravity via ∇
```

This resolves the C1 scale mismatch (34,000×) because φ₀ is cosmological, not geometric. The original Time-Aether intuition — "Zeitfluss hängt vom energetischen Zustand des Systems ab" — is now mathematically precise.

---

## Naming Conventions

| Layer | Term | Example |
|:---|:---|:---|
| **V3.4** | "core", "theory", "scaffold" | "the core oscillator model" |
| **V4** | "interpretation", "mapping", "candidate" | "candidate C5: energy density mapping" |

### Forbidden Terms in V4 Documents

- "final theory"
- "replacement of GR"
- "derived" (use "hypothesized" or "mapped")
- "predicted" (use "expected under this interpretation")
- "proven" (use "consistent with")

---

## V4 Design Principle

> **V4 ist nicht mehr Theorieentwicklung — sondern Bedeutungsentwicklung.**
> V4 is no longer theory development — it is meaning development.

```
"Does this modify the core?"
→ YES → forbidden
→ NO  → allowed
```
