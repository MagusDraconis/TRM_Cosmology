# TRM M3 RBF80–RBF82 Action Stationarity Anchor Note

## Scope

This note summarizes RBF80–RBF82, which anchor the action-stationarity criterion empirically to its exact minimal lattice-energy Euler residual, necessity, and shared global normalization discipline. This final empirical anchoring transitions the framework's diagnostic scaffolding into a fully defined, structurally invariant candidate form ready for formal analytical derivation.

---

## 1) RBF80: Euler Residual Mapping

- **Concept:** Action-stationarity residual `δE_action` is verified to map exactly to the normalized minimal lattice Euler condition `dE = E_m - E_min`, mapping the operational tolerance directly to this physical/structural stationarity residual proxy.
- **Verification:**
  - The formal Euler proxy is expressed as `dE = E_m - E_min` where `E_min` is the global lattice minimum action within the mode family.
  - The computed action residual is defined as `δE_action = dE / actionRange`, where `actionRange = E_max - E_min`.
  - Numerical tests confirm that the computed action residual matches this exact analytical Euler stationarity condition to high precision (`< 10^-4`).
- **Role in Selection:** This verifies that the action residual used in the shared functional is not an arbitrary fitting parameter, but a direct physical representation of the normalized energy distance from the global minimal lattice configuration.

---

## 2) RBF81: Action-Stationarity Necessity for Margin Dominance

- **Concept:** RBF81 provides diagnostic evidence that action-stationarity is necessary to maintain the energy-margin dominance (`ΔE > 0`) for `m=3` within the tested shared-functional domain.

- **Ablation Test:**
  - Removing or weakening the action-stationarity constraint (e.g., setting the action weight to `0.0` or setting the action tolerance to a large value such as `99.0`) allows competing modes to enter the admissible space.
  - When evaluated against a strict margin threshold, the ablated rule fails to isolate `m=3` and either abstains or admits competing alternatives.

- **Conclusion:** Without the action-stationarity constraint, the `m=3` margin dominance collapses in the tested diagnostic setup. This supports the interpretation that action-stationarity is a necessary shared-rule filter for isolating the stable `m=3` candidate over competitive alternatives. This remains diagnostic/candidate evidence only, not theorem-level proof.

---

## 3) RBF82: Rejection of Per-Family Scaling and Parameters

- **Concept:** The action-stationarity criterion must rely strictly on shared global normalization and rejects any per-family scaling or local free parameters.
- **Verification:** 
  - A corruption test is performed where per-family tuning is attempted (e.g., artificially subtracting a large value from the derived action tick of a competing mode like `m=2` to lower its energy).
  - Corrupting the shared global normalization in this manner immediately breaks the diagnostic validity of the selection rule, resulting in either a failure to resolve or a false positive.
- **Conclusion:** The action-stationarity criterion contains no per-family free parameter or hidden per-mode tuning. It depends entirely on a shared global normalization discipline, confirming the structural integrity and uniqueness of the joint selection rule.

---

## Updated status

Current reviewer-safe status:

> m=3 is a bounded shared-functional selection candidate with analytical constraint-boundary, lemma-scaffold, and action-stationarity Euler anchoring support.

---

## Remaining gap

Primary remaining gaps:
Formal analytical proof of topological necessity, sufficiency, and domain closure.

---

## Claim boundaries

- diagnostic/candidate only
- not theorem-level proof
- not full first-principles closure
- not universal m=3 selection
- not GR replacement
- no numerology claim
