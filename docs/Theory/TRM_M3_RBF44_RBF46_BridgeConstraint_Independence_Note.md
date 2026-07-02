# TRM M3 RBF44–RBF46 Bridge-Constraint Independence Note

## Scope

This note summarizes RBF44–RBF46 diagnostics on bridge-constraint independence after structural qCore derivation.

---

## RBF44: bridge double-counting diagnostic after derived qCore

RBF44 compares:

1. manual broad q-window + bridge constraint,
2. derived qCore + bridge constraint,
3. derived qCore without bridge constraint.

Diagnostic result:

> bridge occupancy may be partly encoded by derived qCore in bounded baseline settings, so bridge gating must be checked for double-counting risk.

Interpretation:

> bridge is not automatically removed; the test only checks whether independence remains visible once qCore is pre-structured.

---

## RBF45: bridge necessity outside qCore

RBF45 expands to wider q-support including non-core values and compares full stack vs no-bridge ablation.

Diagnostic result:

> explicit bridge gating remains necessary outside qCore to suppress structurally irrelevant/non-core candidates and competing fallback modes.

Interpretation:

> bridge remains part of the shared constraint stack in broad-support diagnostics, even if qCore absorbs part of the role near baseline.

---

## RBF46: qCore role classification

RBF46 compares:

1. qCore-focused selection behavior,
2. full-support selection with explicit bridge gate,
3. full-support selection without bridge gate.

Diagnostic result:

> derived qCore should be treated as a structural bridge prior, not as a theorem-level derived domain.

Interpretation:

> qCore strengthens structural organization of the bridge path, but does not establish theorem-level closure.

---

## Updated status

Current reviewer-safe status:

> action-derived bounded three-constraint bridge-mode candidate with structurally derived qCore and bridge-prior interpretation.

---

## Claim boundaries

- diagnostic/candidate only
- no theorem-level proof
- no universal m=3 selection
- no first-principles closure claim
- no GR replacement claim
- no numerology claim

---

## Next prepared tests (RBF47–RBF49, not yet implemented)

1. **RBF47_PhaseConstraint_Should_Follow_From_IntegerClosureDefect**  
   test whether phase thresholding can be replaced by a derived integer closure-defect criterion.

2. **RBF48_ActionTickConstraint_Should_Follow_From_LatticeEnergyStationarity**  
   test whether action/tick admissibility follows from stationarity or minimal lattice-energy structure instead of threshold scaling.

3. **RBF49_ThreeConstraintStack_Should_Map_To_OneMinimalEnergyFunctional**  
   test whether phase, bridge-prior, and action/tick can be represented as projections of one shared lattice/action functional.

Claim boundary for this plan: candidate/diagnostic only; no theorem-level proof, no first-principles closure claim, no GR replacement claim.

