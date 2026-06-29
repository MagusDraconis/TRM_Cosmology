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

## 3) What UF10–UF12 add

V3.1 also extends the unified-action guard surface with memory-term hierarchy checks:

1. `UF10_MemoryInteraction_Should_Yield_A2Kappa_As_LeadingTerm`  
   Enforces \(A_{\mathrm{dyn}}^2\kappa\) as leading admissible interaction.

2. `UF11_LinearAInteraction_Should_Be_Rejected_By_HierarchyOrSymmetry`  
   Rejects linear \(A_{\mathrm{dyn}}\kappa\) via symmetry cancellation and weak-field hierarchy pressure.

3. `UF12_HigherOrderMemoryTerms_Should_Remain_Subleading_InWeakField`  
   Keeps higher-order memory terms subleading in the weak-field regime.

Net effect: the memory argument is now tied to a unified-action hierarchy discipline, not only standalone MC proxies.

---

## 4) Why \(A_{\mathrm{dyn}} \propto \phi\) is now stronger

The key bridge is now supported across multiple independent guard angles:

- direct lattice clock-bias response guard (MC13),
- Green-function scaling compatibility (MC14),
- non-Newtonian kernel degradation signal (MC15),
- power-counting selection pressure (MC16),
- unified-action hierarchy coherence (UF10–UF12).

This improves the status from "single-path proportionality evidence" to "multi-guard action-based closure candidate."

This candidate provides a consistent weak-field action-based description of the memory term, but it does not yet constitute an independent variational derivation from a single fundamental action functional.

Key V3.1 test metrics:

- MC13: \(A_{\mathrm{dyn}}(\phi)\) linear response with \(R^2=0.999688\).
- MC14: Green-function potential compatibility with \(R^2=0.999663\).
- MC15: non-Newtonian kernel degradation: \(R^2_{\mathrm{Newton}}=0.999553\), \(R^2_{\mathrm{nonNewton}}=0.968889\).
- MC16: \(A_{\mathrm{dyn}}^2\kappa\) vs. \(\phi^2\kappa\) with \(R^2=0.999997\).

---

## 5) What remains open

The following remains explicitly open:

- theorem-level microscopic first-principles proof of \(\phi^2|\dot{\mu}|\),
- fundamental derivation of the effective coupling normalization beyond current guard-level path,
- any claim that TRM replaces GR.

Claim-safe status statement:

> V3.1 strengthens Gap 1 from a strongly supported lattice-proxy derivation path toward an action-based memory-closure candidate. It remains not theorem-level first-principles closure.
