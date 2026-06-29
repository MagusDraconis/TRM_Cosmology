# TRM V3.1 Memory Action-Closure Review Note

## Scope

This note is intentionally narrow: it reviews only **Gap 1 / memory-term hardening** in V3.1.

Target question:

> Is the action-based path
> \[
> A_{\mathrm{dyn}} \propto \phi
> \rightarrow
> A_{\mathrm{dyn}}^2|\dot{\mu}|
> \rightarrow
> \phi^2|\dot{\mu}|
> \]
> now more convincing, or still too phenomenological?

---

## 1) What V3.0 already established

V3.0 provided a strongly supported lattice-proxy path for the memory channel:

- MC09 supported weak-field \(A_{\mathrm{dyn}}(\phi)\) response.
- MC10 supported \(A_{\mathrm{dyn}}^2|\dot{\mu}|\) as first admissible memory order in the tested window.
- MC11 supported near-linear correspondence between \(A_{\mathrm{dyn}}^2|\dot{\mu}|\) and \(\phi^2|\dot{\mu}|\).
- MC12 supported EL/Fermat bridge retention under substitution.

Interpretation at V3.0: strong tested-effective derivation path, but not theorem-level first-principles closure.

---

## 2) What MC13–MC16 add

V3.1 introduces a second hardening layer around the same chain:

1. `MC13_LatticeClockBias_Should_Produce_LinearPhiResponse`  
   Adds an explicit lattice clock-bias linear-response guard for \(A_{\mathrm{dyn}}(\phi)\).

2. `MC14_CoherenceAmplitude_Should_Follow_GreenFunctionPotential`  
   Tests consistency with point-source Green-function potential behavior.

3. `MC15_AphiScaling_Should_Break_When_ResponseKernelIsNonNewtonian`  
   Adds a degradation/falsification-style check: closure quality should weaken under non-Newtonian response kernel.

4. `MC16_MemoryTermPowerCounting_Should_Select_PhiSquaredKappa`  
   Strengthens weak-field power counting: rejects linear term, keeps quadratic term admissible/relevant, keeps higher order subleading.

Net effect: not just proportionality matching, but also kernel-sensitivity and hierarchy selection guards.

---

## 3) What UF10–UF15 add

V3.1 also extends the unified-action guard surface with memory-term hierarchy checks:

1. `UF10_MemoryInteraction_Should_Yield_A2Kappa_As_LeadingTerm`  
   Enforces \(A_{\mathrm{dyn}}^2\kappa\) as leading admissible interaction.

2. `UF11_LinearAInteraction_Should_Be_Rejected_By_HierarchyOrSymmetry`  
   Rejects linear \(A_{\mathrm{dyn}}\kappa\) via symmetry cancellation and weak-field hierarchy pressure.

3. `UF12_HigherOrderMemoryTerms_Should_Remain_Subleading_InWeakField`  
   Keeps higher-order memory terms subleading in the weak-field regime.

4. `UF13_MemoryTerm_Should_Follow_From_Variation_Of_MinimalEffectiveAction`  
   Adds a minimal-action stationarity guard that maps the varied memory interaction to the \(\phi^2\kappa\) transport form.

5. `UF14_A2Kappa_Should_Be_StationaryLeadingInteraction_Under_WeakFieldExpansion`  
   Enforces \(A_{\mathrm{dyn}}^2\kappa\) as stationary leading interaction under weak-field expansion.

6. `UF15_LinearAInteraction_Should_Vanish_Under_SymmetryAveraging`  
   Enforces odd linear-term cancellation under sign-symmetric averaging.

Net effect: the memory argument is now tied to both unified-action hierarchy discipline and a variation-compatible minimal-action path.

---

## 4) Why \(A_{\mathrm{dyn}} \propto \phi\) is now stronger

The key bridge is now supported across multiple independent guard angles:

- direct lattice clock-bias response guard (MC13),
- Green-function scaling compatibility (MC14),
- non-Newtonian kernel degradation signal (MC15),
- power-counting selection pressure (MC16),
- unified-action hierarchy coherence (UF10–UF12),
- variation-compatible stationary leading-interaction guards (UF13–UF15).

This improves the status from "single-path proportionality evidence" to "multi-guard action-based closure candidate."

V3.1 now supports the memory channel as a variation-compatible leading interaction candidate from a minimal effective action. It is stronger than V3.0, but still not theorem-level first-principles closure.

Key V3.1 test metrics:

- MC13: \(A_{\mathrm{dyn}}(\phi)\) linear response with \(R^2=0.999688\).
- MC14: Green-function potential compatibility with \(R^2=0.999663\).
- MC15: non-Newtonian kernel degradation: \(R^2_{\mathrm{Newton}}=0.999553\), \(R^2_{\mathrm{nonNewton}}=0.968889\).
- MC16: \(A_{\mathrm{dyn}}^2\kappa\) vs. \(\phi^2\kappa\) with \(R^2=0.999997\).
- UF13: minimal-action variation compatibility with \(R^2=0.999922\), stationarity residual \(\approx 4.2\times10^{-15}\).
- UF14: \(A_{\mathrm{dyn}}^2\kappa\) remains stationary leading interaction; higher-order terms remain subleading in weak field.
- UF15: symmetry averaging cancels linear \(A_{\mathrm{dyn}}\kappa\) contribution (\(\mathrm{meanLinear}=0\)).

---

## 5) V3.1 status

Current V3.1 status:

- Gap 1: action-derived candidate level
- not theorem-level closure

---

## 6) What remains open

The following remains explicitly open:

- theorem-level microscopic first-principles proof of \(\phi^2|\dot{\mu}|\),
- fundamental derivation of the effective coupling normalization beyond current guard-level path,
- any claim that TRM replaces GR.

Claim-safe status statement:

> V3.1 strengthens Gap 1 to an action-derived memory-closure candidate. The \(\phi^2|\dot{\mu}|\) term is now variationally consistent with a minimal effective action and supported by multiple independent guards. It remains short of theorem-level first-principles microscopic derivation.
