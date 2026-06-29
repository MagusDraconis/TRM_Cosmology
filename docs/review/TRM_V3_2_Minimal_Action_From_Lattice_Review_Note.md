# TRM V3.2 Minimal Action from Lattice — Review Note

## Scope

This note is intentionally narrow and reviews only the V3.2 minimal-action block introduced by UA16–UA23.

Core review question:

> Do UA16–UA23 make the minimal effective action itself plausibly lattice-derived, or is it still an effective action ansatz?

---

## 1) V3.2 update summary

V3.2 starts from the V3.1 Gap-1 status and targets a stronger foundation for the minimal effective action through TQM lattice/synchronization structure.

Implemented guard block:

1. `UA16_LatticeEnergy_Should_Reduce_To_MinimalScalarAction`
2. `UA17_CoarseGrainedAction_Should_Preserve_A2Kappa_Interaction`
3. `UA18_MinimalAction_Should_Reproduce_UF13_To_UF15_Without_Retuning`
4. `UA19_NonMinimalActionTerms_Should_Be_Penalized_Or_Subleading`
5. `UA20_MinimalAction_Should_Preserve_MC_FD_TO_Limits`
6. `UA21_AlternativeLatticeEnergyForms_Should_Degrade_MinimalActionClosure`
7. `UA22_MinimalAction_Should_Be_Robust_Under_LatticePerturbations`
8. `UA23_NonMinimalLatticeActions_Should_Not_Outperform_WithoutPenalty`

Current test status for this block: 8/8 passing.

## Relation to V3.1

V3.1 established the memory term as an action-derived memory-closure candidate:
\[
A_{\mathrm{dyn}}\propto\phi
\rightarrow
A_{\mathrm{dyn}}^2|\dot{\mu}|
\rightarrow
\phi^2|\dot{\mu}|.
\]

V3.2 moves one level deeper by testing whether the minimal effective action itself can be motivated from TQM lattice/synchronization structure.

---

## 2) What each UA guard adds

- **UA16**: lattice nearest-neighbor energy proxy reduces to minimal scalar gradient action in weak field.
- **UA17**: coarse-grained interaction preserves \(A_{\mathrm{dyn}}^2\kappa\) structure and transport-form compatibility.
- **UA18**: UF13–UF15 behavior remains reproducible without retuning.
- **UA19**: nonminimal terms remain penalized/subleading under weak-field hierarchy.
- **UA20**: MC/FD/TO limit behavior is preserved under the minimal-action candidate.

Net effect: the minimal action is better motivated as lattice-compatible and cross-guard coherent.

### Key V3.2 guard metrics

| Guard | Key metric | Interpretation |
|---|---:|---|
| UA16 | exact lattice-to-scalar action reduction | discrete lattice energy reduces to minimal scalar action |
| UA17 | \(R^2 = 1.0\) | \(A_{\mathrm{dyn}}^2\kappa\) preserved under coarse-graining |
| UA18 | UF13–UF15 reproduced without retuning | V3.1 memory-action result survives V3.2 action layer |
| UA19 | non-minimal terms penalized/subleading | minimal action remains preferred |
| UA20 | MC/FD/TO limits preserved | no cross-sector breakage |
| UA21 | alternative lattice forms degrade closure | discriminates against arbitrary lattice energies |
| UA22 | robust under lattice perturbations | stability of minimal-action path |
| UA23 | non-minimal actions do not outperform without penalty | prevents complexity inflation |

---

## 3) Claim-safe interpretation

V3.2 supports a candidate derivation of the minimal effective action from TQM lattice/synchronization structure.  
Under comparative guard tests (UA21–UA23), the minimal effective action is now plausibly lattice-derived.  
It remains not theorem-level first-principles closure.

No statement here implies TRM replaces GR.

Claim-safe summary:

- UA16–UA23 support a candidate derivation of the minimal effective action from TQM lattice/synchronization principles.
- Under comparative guards, the minimal effective action is plausibly lattice-derived.
- This remains not theorem-level first-principles closure.
- No GR replacement claim.

---

## 4) What remains open

1. Independent microscopic derivation of the minimal action form from first principles (beyond candidate-level consistency guards).
2. Theorem-level uniqueness/necessity of the selected minimal interaction structure.
3. Full theorem-level closure across remaining first-principles gaps.

---

## 5) External review focus

Recommended external benchmark prompt:

> Evaluate whether UA16–UA23 are sufficient to treat the minimal effective action as plausibly lattice-derived, or whether the framework still behaves as a calibrated effective action ansatz.

Additional focused review question:

> Do UA16–UA23 justify treating the minimal effective action as plausibly lattice-derived, or does the framework remain primarily a calibrated effective model with improved microscopic motivation?
