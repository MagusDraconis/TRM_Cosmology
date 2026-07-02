# TRM Theta Observable: First-Principles Formalization Track

## Scope

This document defines the formal derive-or-falsify track for the open galaxy-dynamics bridge:
\[
\Theta(r)\;\rightarrow\;g_{\mathrm{obs}}(r).
\]

It formalizes what is already implemented and tested, and what still needs to be derived.
It does **not** claim a completed first-principles closure yet.

---

## 1. Operational starting point in the codebase

The current Domain1 stack already contains:

1. a radial theta-field relaxation solver (`TrmFieldSolver.SolveField`),
2. a local observable proxy (`TrmFieldSolver.ComputeEffectiveAcceleration`),
3. orbit-integrated and full-model comparisons (`OrbitalIntegrationService`, `TrmFullModel`),
4. SPARC/RAR evaluation pipelines (`RarRelationTests`, `OrbitalIntegratedTests`).

Current solver-level structure (effective, not yet microscopic theorem):
\[
\Theta_{n+1}(r_i)
\leftarrow
\Theta_n(r_i)
\;+\;
\epsilon\bigl[S(r_i)-D\,\Theta_n(r_i)+\Sigma(r_i)-\mathcal R[\Theta_n](r_i)\bigr],
\]
where \(S\) is a baryonic source proxy, \(D\) a damping term, \(\Sigma\) a synchronization proxy, and \(\mathcal R\) radial regularization.

---

## 2. Candidate observable family

The implemented proxy currently mixes gradient and level terms:
\[
g_{\Theta}(r)\sim
\alpha\,|\partial_r\Theta(r)|
\;+\;
\beta\,\frac{\max(\Theta(r),0)}{r}.
\]

For first-principles closure, treat this as one member of a candidate family:
\[
\mathcal O_{\Theta}^{(k)}(r),
\]
with explicit admissibility constraints, instead of assuming this specific form is final.

---

## 3. Bridge ansatz to observables

Define the effective prediction layer:
\[
g_{\mathrm{pred}}(r)
=
g_{\mathrm{base}}(r)
+
\lambda_{\Theta}\,\mathcal O_{\Theta}(r),
\]
where \(g_{\mathrm{base}}(r)\) is the already tested baryonic + orbit-integrated baseline.

The first-principles task is to justify:

1. why \(\mathcal O_{\Theta}\) has this structural form,
2. why \(\lambda_{\Theta}\) is positive and regime-bounded,
3. why this bridge cannot be absorbed into a purely local reparameterization.

---

## 4. Admissibility constraints for \(\mathcal O_{\Theta}\)

A candidate observable is admissible only if all hold:

1. **Positivity/physicality:** \(g_{\mathrm{pred}}(r)\ge 0\) in tested SPARC windows.
2. **Outer-regime boundedness:** no unphysical growth in low-\(g_{\mathrm{bar}}\), large-\(r\) tails.
3. **Inner-regime compatibility:** no destructive degradation in high-\(g_{\mathrm{bar}}\), small-\(r\) windows.
4. **Non-local contribution:** improvement must persist against purely local baselines.
5. **Cross-galaxy stability:** improvements are distributed, not concentrated in a few outliers.

---

## 5. Acceptance and falsification criteria

Upgrade level from "open bridge" to "best-supported effective bridge" only if:

1. the same \(\mathcal O_{\Theta}\)-family remains competitive across justified binning/weighting variants,
2. full-model residual behavior remains at least as good as orbit-only within documented tolerances,
3. no regime-specific pathology appears (inner or outer bins).

Downgrade/falsify if:

1. a purely local model reproduces the same improvements without non-local structure,
2. improvements disappear under modest robustness perturbations,
3. another observable family dominates while satisfying all constraints with fewer penalties.

---

## 6. Current claim boundary (review-safe)

Use:

> TO01–TO06 show bounded and structurally interesting theta-observable behavior. TO07–TO08 show that the current O4/theta candidate does not beat local reparameterization in tested regimes. TO09–TO20 establish non-local O5 gating, robustness, holdout behavior, leakage guards, and solver-ablation stability. TO21–TO24 provide operator-level finite-coherence interpretation and window-balance constraints for W6. W6 is currently the best finite-coherence balance window at the tested medium resolution; nearby windows remain competitive, so W6 should be treated as an effective discretized coherence scale, not a universal constant.

Avoid:

> The \(\Theta\)-observable map is already uniquely derived from first principles.

---

## 7. Next concrete derivation tasks

1. Derive the leading admissible \(\mathcal O_{\Theta}\) terms from the radial TRM/TQM transport structure.
2. Prove (or falsify) identifiability against local reparameterizations.
3. Add dedicated theta-observable selection tests:
   - `TO01_ThetaObservable_Should_Respect_InnerOuterRegimeBounds`
   - `TO02_ThetaObservable_Should_Improve_Over_Local_Without_OuterTailPenalty`
   - `TO03_AlternativeThetaObservables_Should_Not_Outperform_WithoutPenalty`
   - `TO04_ThetaObservable_Should_Not_Be_Reducible_To_Pure_Local_Reparameterization`
   - `TO05_ThetaObservable_Should_Show_CrossGalaxy_Stability_Not_OutlierDependence`
   - `TO06_ThetaObservable_Should_Improve_ResidualStructure_Beyond_LocalScaling`
   - `TO07_ThetaObservable_Should_Identify_RegimeWhere_NonLocalSignalDominates`
   - `TO08_ThetaObservable_Should_Be_Classified_As_Diagnostic_When_LocalWinsAllRegimes`
   - `TO09_ThetaObservable_Should_Show_Independence_From_LocalGbar`
   - `TO10_ThetaObservable_Should_Predict_LocalResidual_NotFullSignal`
   - `TO11_NonLocalThetaKernel_Should_Outperform_LocalScaling_In_ResidualSpace`
4. `TO12_O5_Should_Remain_Stable_Across_GalaxyClasses_Not_OutlierDriven`
5. `TO13_O5Kernel_Should_Be_Robust_Under_KernelAblation`
6. `TO14_O5Kernel_Should_Have_PhysicalSelectionCriterion`
7. `TO15_O5W6InvDistance_Should_Not_Be_Overfit_To_CurrentSample`
8. `TO16_O5Kernel_Should_Select_By_HoldoutStability_NotTrainingScore`
9. `TO17_O5HoldoutWeakFold_Should_Explain_GeneralizationInstability`
10. `TO18_O5_Should_Remain_Stable_Under_StratifiedGalaxyHoldout`
11. `TO19_O5_Should_Not_Use_ObservedVelocityLeakage`
12. `TO20_O5_Should_Remain_Stable_Under_Reasonable_ThetaSolverParameter_Ablation`
13. `TO21_O5W6_Should_Behave_Like_FiniteCoherenceSynchronizationTension`
14. `TO22_W6_Should_Be_Minimal_Window_With_Stable_CoherenceResponse`
15. `TO23_W6_Should_Balance_Locality_And_Nonlocality`
16. `TO24_W6_Should_Remain_Stable_Under_ProfileResolutionScaling`
17. Keep the claim level at tested-effective / hypothesis-supported until microscopic derivation and identifiability closure are completed.
