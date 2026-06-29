# TRM Unified Field-Action Roadmap

## Purpose

This document defines a claim-safe roadmap for unifying the current TRM effective sectors into one shared action-level framework:

\[
T,\quad \vec A_T,\quad \Theta
\]

Status target of this roadmap: **strongly structured unification path / not theorem-level closure**.

Current guard status:

- UF01–UF05: sector-limit and additive-baseline consistency guards are passing.
- UF06–UF07: bounded, limit-preserving small cross-coupling guards are passing.
- UF08: globally identifiable cross-coupling guard (without per-group refit dependence) is passing.
- UF09: cross-sector preservation guard for MC/FD/TO behavior under small couplings is passing.

---

## 1. Current sector baseline

1. Scalar transport/time sector \(T\): broad tested-effective weak-field baseline.
2. Vector rotational sector \(\vec A_T\): FD01–FD20 hardening with non-fitted derived \(k_T\), weak-field window compatibility, and controlled bias.
3. Theta-observable sector \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\): TO/TQK/LC/TOL chain strongly supported, not theorem-level.

These are currently coherent as neighboring effective sectors, but not yet derived from one shared microscopic action.

---

## 2. Unification objective

Construct one effective action family

\[
S_{\mathrm{eff}}[T,\vec A_T,\Theta]
=
S_T[T]
+S_A[\vec A_T]
+S_\Theta[\Theta]
+S_{\mathrm{int}}[T,\vec A_T,\Theta]
\]

such that existing guarded behaviors are recovered as limits, not re-fitted add-ons.

---

## 3. Minimal structural ansatz (roadmap level)

\[
S_T = \int d^4x\;\Bigl(\alpha_T (\partial T)^2 + V_T(T)\Bigr)
\]

\[
S_A = \int d^4x\;\Bigl(\alpha_A |\nabla\times\vec A_T|^2 + \beta_A |\partial_t \vec A_T|^2\Bigr)
\]

\[
S_\Theta = \int d^4x\;\Bigl(\alpha_\Theta |\nabla\Theta|^2 + V_\Theta(\Theta)\Bigr)
\]

\[
S_{\mathrm{int}}
=
\int d^4x\;\Bigl(
\gamma_{T\Theta}\, \mathcal I_{T\Theta}
\gamma_{A\Theta}\, \mathcal I_{A\Theta}
\gamma_{TA}\, \mathcal I_{TA}
\Bigr)
\]

Roadmap requirement: interaction terms must preserve already-tested limits:

1. Spin-zero \(\Rightarrow \vec A_T\)-sector collapse.
2. Weak-field vector scaling \(\Omega \sim J/r^3\) retained.
3. Theta-chain guards retained with anti-proxy discipline.

---

## 4. Recovery constraints from existing hardening blocks

The unified action path is acceptable only if it reproduces:

1. Gap 1: \(\phi^2|\dot\mu|\) memory-channel path (MC09–MC12) as admissible low-order effective channel.
2. Gap 3: \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\) derivation chain behavior under TO/TQK/LC/TOL gates.
3. Gap 4: FD16–FD20 behavior for derived \(k_T\), weak-field LT-window compatibility, SI scaling compatibility, and controlled bias.

No sector may be improved by silently violating another sector's existing guards.

---

## 5. Claim boundary

At this stage, the unification is a **derive-or-falsify roadmap**, not a finished derivation.

Claim-safe statement:

> TRM currently has strongly hardened effective sector paths for scalar transport, vector frame-dragging, and theta-response channels. A unified action-level framework is now structurally specified, but not yet closed as theorem-level first-principles derivation.
>
> UF01–UF09 support a unified effective action roadmap whose sector limits, additive baseline, bounded cross-terms, globally identifiable cross-couplings, and MC/FD/TO guard preservation are test-guarded. This remains a candidate action-level structure, not theorem-level unification.

---

## 6. Falsification criteria

The unification roadmap fails if any of the following occurs:

1. A shared interaction term breaks FD16–FD20 weak-field/vector guard behavior.
2. Unification requires per-dataset hidden re-normalization that violates non-fit discipline.
3. Theta-chain holdout/anti-proxy guarantees are degraded.
4. Scalar transport baseline limits are not recovered in zero-coupling reductions.

---

## 7. UF-series progression

Completed:

1. `UF01_UnifiedAction_Should_Reduce_To_ScalarSector_When_VectorAndThetaDisabled`
2. `UF02_UnifiedAction_Should_Reduce_To_VectorSector_When_ScalarThetaCouplings_Off`
3. `UF03_UnifiedAction_Should_Reduce_To_ThetaO5Sector_When_ScalarVectorCouplings_Off`
4. `UF04_UnifiedAction_CrossTerms_Should_Vanish_When_CouplingsZero`
5. `UF05_UnifiedAction_Should_Preserve_AllKnownLimits`
6. `UF06_UnifiedAction_CrossTerms_Should_Remain_Bounded_For_SmallCouplings`
7. `UF07_UnifiedAction_CrossTerms_Should_Not_Break_KnownSectorLimits`
8. `UF08_AllowedCrossCouplings_Should_Be_Identifiable_Without_Refit`
9. `UF09_UnifiedAction_Should_Not_Break_MC_FD_TO_Guards`
10. `UF10_MemoryInteraction_Should_Yield_A2Kappa_As_LeadingTerm`
11. `UF11_LinearAInteraction_Should_Be_Rejected_By_HierarchyOrSymmetry`
12. `UF12_HigherOrderMemoryTerms_Should_Remain_Subleading_InWeakField`

Next:

1. `UF13_MemoryTerm_Should_Follow_From_Variation_Of_MinimalEffectiveAction`
2. `UF14_A2Kappa_Should_Be_StationaryLeadingInteraction_Under_WeakFieldExpansion`
3. `UF15_LinearAInteraction_Should_Vanish_Under_SymmetryAveraging`

All UF tests are guard-style derivation gates; no theorem-level overclaiming is allowed.
