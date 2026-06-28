# TRM Memory Channel: First-Principles Derivation Track

## Scope

This document defines the first-principles derivation program for the TRM memory channel term
\[
\phi^2|\dot\mu|
\]
used in the photon transport index.

It separates:
1. what is already strongly tested,
2. what is still unproven,
3. which candidate invariants are admissible,
4. and what falsification criteria must be passed.

---

## 1. Current tested evidence

The following tests provide the current empirical/structural baseline:

1. `MEM01_MemoryChannel_Zero_Should_Show_DeflectionDeficit`
   - Removing memory (`LambdaSpace = 0`) degrades bridge behavior while not acting like a pure Shapiro-time driver.

2. `MEM02_MemoryChannel_Should_Improve_ELBridge_In_LowerWeakField_And_Show_UpperBoundary`
   - Memory improves EL/Schwarzschild weak-field bridge quality in the lower weak-field window and defines an explicit upper-boundary documentation point.

3. `TRM84_LocalMemoryTerm_Should_Scale_Quadratically_With_Phi`
   - Enforces quadratic field scaling guard.

4. `TRM85_LocalMemoryTerm_Should_Scale_Linearly_With_DirectionalRotation`
   - Enforces linear directional-rotation dependence guard.

5. `TRM86_WeakField_MemoryContribution_Should_Remain_Subleading_To_TimeChannel`
   - Enforces weak-field subleading behavior relative to the time channel.

6. `TRM87_MemoryChannel_Should_Depict_DirectionalTransport_Not_PureTimeShift`
   - Enforces separation from pure time-channel behavior.

7. `HOA01_TransportIndex_Should_Depend_On_Position_Direction_And_DirectionalChange`
   - Supports higher-order optical-action interpretation (\(x, v, \dot v\)-sensitive transport index).

8. `MC01_MemoryInvariant_Should_Vanish_Without_Field_Or_Turning`
   - Enforces field-off and no-turning vanishing behavior for the memory invariant baseline.

9. `MC02_PhiLinearMemoryCandidate_Should_Fail_WeakFieldSubleadingConstraint`
   - Confirms that the \(\phi|\dot\mu|\) candidate violates weak-field subleading hierarchy, while \(\phi^2|\dot\mu|\) remains subleading in the tested weak-field window.

10. `MC03_PhiSquaredMemoryCandidate_Should_Satisfy_TimeChannelSeparation`
   - Confirms that \(\phi^2|\dot\mu|\) produces no extra time-channel shift at \(|\dot\mu|=0\), but activates under turning.

11. `MC04_AlternativeMemoryPowers_Should_NotImprove_ELBridgeWithoutPenalty`
   - Compares \(\phi|\dot\mu|\), \(\phi^2|\dot\mu|\), \(\phi^2|\dot\mu|^2\), \(\phi^3|\dot\mu|\) against EL/Schwarzschild error with structural penalties (weak-field hierarchy, time-channel separation, bridge relevance).

MC04 reviewer-safe interpretation:

> Among nearby tested candidate invariants, \(\phi^2|\dot\mu|\) is the only form that simultaneously satisfies weak-field hierarchy, time-channel separation, and EL-bridge relevance without penalty.

---

## 2. What this does NOT prove yet

The current tests do **not** yet derive \(\phi^2|\dot\mu|\) from microscopic TQM dynamics.

Open derivation questions:

1. Why exactly quadratic in \(\phi\)?
2. Why exactly first power in \(|\dot\mu|\)?
3. Why absolute value at effective level (instead of signed or squared directional rate)?
4. Why the current coupling structure as the leading admissible transport-memory invariant?

So the term is currently:
- strongly constrained and test-stabilized,
- but not yet theorem-level first-principles-derived.

---

## 3. Candidate invariant family (derivation search space)

Use candidate transport-memory terms of the form:
\[
I_{a,b}(\phi,\dot\mu)=\phi^a |\dot\mu|^b
\]
and nearby alternatives (including signed variants where physically admissible).

Primary candidates to compare:

1. \(\phi|\dot\mu|\)
2. \(\phi^2|\dot\mu|\)  (current baseline)
3. \(\phi^2|\dot\mu|^2\)
4. \(\phi^3|\dot\mu|\)
5. mixed alternatives with explicit sign dependence (if not symmetry-forbidden)

---

## 4. Admissibility constraints for a minimal leading term

A candidate should satisfy all of the following to remain viable:

1. **Field-off vanishing:** term vanishes as \(\phi\to 0\).
2. **No-turning vanishing:** term vanishes when directional change vanishes.
3. **Directional transport behavior:** not reducible to pure time-channel renormalization.
4. **Weak-field hierarchy:** remains subleading to linear time channel in weak-field regime.
5. **Bridge relevance:** contributes to EL/Fermat bridge quality in the validated weak-field window.
6. **Sign/stability consistency:** no pathological sign-flip behavior in effective transport index.

Current test baseline already enforces parts of (1)–(4) for the implemented form.

---

## 5. Why \(\phi^2|\dot\mu|\) is currently the leading candidate

At present, \(\phi^2|\dot\mu|\) is the best-supported candidate because it simultaneously matches:

1. directional-memory channel behavior (non-time-only),
2. weak-field subleading hierarchy,
3. local scaling guards from TRM84–TRM87,
4. executable bridge relevance from MEM01–MEM02 and EL track diagnostics.

\(\phi^2|\dot\mu|\) is currently the best-supported effective candidate, not a final uniqueness proof.

---

## 6. Falsification criteria

The current candidate \(\phi^2|\dot\mu|\) should be demoted if any of the following is shown:

1. A competing invariant satisfies the same admissibility constraints and yields strictly better bridge consistency without added penalties.
2. The weak-field hierarchy constraint fails under robust ablation.
3. The term can be absorbed into a pure local time-channel reparameterization without loss.
4. A microscopic TQM derivation yields a different leading invariant under the same symmetry/closure assumptions.

---

## 7. Completed invariant-selection test block

The initial memory-candidate selection block is now completed:

1. `MC01_MemoryInvariant_Should_Vanish_Without_Field_Or_Turning`
2. `MC02_PhiLinearMemoryCandidate_Should_Fail_WeakFieldSubleadingConstraint`
3. `MC03_PhiSquaredMemoryCandidate_Should_Satisfy_TimeChannelSeparation`
4. `MC04_AlternativeMemoryPowers_Should_NotImprove_ELBridgeWithoutPenalty`

Next test additions should target tighter microscopic constraints and explicit TQM-derived acceptance thresholds.

## 8. Claim boundary (review-safe)

Use:

> \(\phi^2|\dot\mu|\) is currently the best-supported minimal effective memory invariant, not yet a microscopic theorem.

Avoid:

> The memory invariant is already microscopically derived from first principles.
