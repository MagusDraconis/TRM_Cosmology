# TRM Memory Channel Derivation Attempt

## Scope

Goal: structure a first-principles derivation path for the transport/memory term

\[
\phi^2|\dot{\mu}|
\]

without overclaiming theorem-level closure.

---

## 1. Current Tested Facts

- **MEM01 — `MEM01_MemoryChannel_Zero_Should_Show_DeflectionDeficit`**  
  **Status:** tested  
  Removing \(\lambda_s\) weakens bridge behavior; memory is not equivalent to a pure Shapiro-time channel.

- **MEM02 — `MEM02_MemoryChannel_Should_Improve_ELBridge_In_LowerWeakField_And_Show_UpperBoundary`**  
  **Status:** tested  
  In the lower weak-field window, memory improves EL-vs-Schwarzschild proximity; upper-boundary behavior is documented, not overclaimed.

- **TRM84–TRM87**  
  **Status:** tested  
  These tests establish:
  - quadratic field scaling guard \(\phi^2\),
  - linear turning-rate scaling guard \(|\dot{\mu}|\),
  - weak-field subleading behavior,
  - separation from pure time-channel behavior.

- **MC01–MC08**  
  **Status:** tested  
  Candidate-selection block across nearby invariants:
  - \(\phi|\dot{\mu}|\),
  - \(\phi^2|\dot{\mu}|\),
  - \(\phi^2|\dot{\mu}|^2\),
  - \(\phi^3|\dot{\mu}|\),
  with structural penalties, algebraic consistency checks, and non-reabsorbability gates.

- **HOA01**  
  **Status:** tested + hypothesis-supported  
  Supports that \(n_{\mathrm{eff}}\) depends on position, direction state, and directional-change rate, consistent with a higher-order optical-action interpretation.

- **`PhotonTransportModel.cs` implementation**  
  **Status:** derived effective form + calibrated + limitation  
  Current production form uses:

\[
n_{\mathrm{eff}}
=
2+
\lambda_t\phi+
\lambda_s\phi^2|\dot{\mu}|
\]

with calibrated channel coefficients and no microscopic closure proof.

---

## 2. Candidate Invariant Family

Define:

\[
I_{a,b}(\phi,\dot{\mu})
=
\phi^a|\dot{\mu}|^b
\]

Primary local candidates near the current baseline:

- \(I_{1,1}=\phi|\dot{\mu}|\)
- \(I_{2,1}=\phi^2|\dot{\mu}|\) — current baseline
- \(I_{2,2}=\phi^2|\dot{\mu}|^2\)
- \(I_{3,1}=\phi^3|\dot{\mu}|\)

**Status:** hypothesis-supported search family; not derived yet.

---

## 3. Required Admissibility Constraints

A leading effective memory invariant should satisfy:

- **Field-off vanishing:** \(I_{a,b}\to0\) for \(\phi\to0\).
- **No-turning vanishing:** \(I_{a,b}\to0\) for \(|\dot{\mu}|\to0\).
- **Time-channel separation:** no extra shift when \(|\dot{\mu}|=0\).
- **Weak-field hierarchy:** memory channel remains subleading to \(\lambda_t\phi\) in validated weak-field ranges.
- **Bridge relevance:** contributes to EL/Fermat bridge quality, not only formal admissibility.
- **Stability/physicality:** no pathological sign/amplification behavior in tested windows.

**Status:** partly tested, partly hypothesis-supported.

---

## 4. Why \(\phi|\dot{\mu}|\) Fails

From **MC02**:

- For \(I_{1,1}\), the memory/time ratio scales approximately as constant in \(\phi\), so it does not naturally suppress in weak field.
- In the MC02 thresholds, \(I_{1,1}\) violates weak-field subleading constraints.

From **MC04**:

- \(I_{1,1}\) also fails structural selection once penalties for weak-field hierarchy, time-channel separation, and bridge relevance are included.

**Status:** tested failure within the current tested windows and thresholds.

---

## 5. Why \(\phi^2|\dot{\mu}|\) Is Currently Preferred

\(I_{2,1}\) is currently preferred because it is the only nearby tested candidate that simultaneously satisfies:

- weak-field hierarchy constraints (**TRM86**, **MC02**),
- time-channel separation under no-turning conditions (**TRM87**, **MC03**),
- field/turning activation logic (**MC01**),
- bridge relevance with no structural penalty in the MC04 selection framework (**MC04**),
- consistency with the current effective transport implementation in `PhotonTransportModel.cs`.

**Status:** tested effective preference + hypothesis-supported derivation path.

---

## 6. What Remains Unproven

- Why microscopic TQM coarse-graining must produce \(a=2\) exactly.
- Why the leading admissible turning exponent is \(b=1\) exactly.
- Why \(|\dot{\mu}|\) is mandatory at leading effective order, rather than a signed or alternative invariant.
- Why \(\lambda_s\) and the coupling structure arise uniquely from first principles.

Overall status of the memory term:

- **tested:** yes — effective behavior and constraints
- **calibrated:** yes — couplings
- **strongly supported derivation path:** yes — lattice-proxy MC09–MC12 support \(A_{\mathrm{dyn}}\propto\phi\), \(A_{\mathrm{dyn}}^2|\dot{\mu}|\), and bridge substitution consistency up to an effective coupling scale
- **not derived yet:** yes — microscopic theorem-level closure
- **limitation:** uniqueness and full TQM closure remain open

---

## 7. Completed Derivation-Gate Tests: MC05–MC08

- **MC05 — `MC05_PhiSquaredMemoryInvariant_Should_Imply_AeffProportionalToPhi`**  
  **Status:** tested  
  Algebraic consistency check: the implemented invariant \(\phi^2|\dot{\mu}|\) implies \(A_{\mathrm{eff}}\propto\phi\). It does not independently derive \(A\propto\phi\) from microscopic TQM dynamics.

- **MC06 — `MC06_LeadingInvariantOrder_Should_Reject_LinearInA_Coupling_Under_WeakFieldHierarchy`**  
  **Status:** tested  
  Confirms that linear-in-\(A\) coupling fails weak-field hierarchy while quadratic-in-\(A\) remains admissible in the tested window.

- **MC07 — `MC07_SignedVsAbsoluteTurningRate_Should_Preserve_DissipativePositivityConstraints`**  
  **Status:** tested  
  Confirms the \(|\dot{\mu}|\)-based channel keeps non-negative dissipative contribution, while the signed-rate variant can produce negative corrections.

- **MC08 — `MC08_MemoryInvariant_Should_Not_Be_Reabsorbed_Into_PureTimeChannel_Reparameterization`**  
  **Status:** tested  
  Confirms that turning dependence cannot be captured by a single pure time-channel reparameterization at fixed \(\phi\).

These remain derivation-gate tests, not fit-generation tests, and do not by themselves close the microscopic theorem.

Reviewer-safe completion statement:

> MC01–MC08 complete the local invariant-selection block for the memory channel.

---

## 8. Gap-1 lattice-proxy derivation extension: MC09–MC12

- **MC09 — `MC09_CoherenceAmplitude_Should_Scale_With_Phi_From_Lattice`**  
  **Status:** tested  
  Supports weak-field lattice-proxy response \(A_{\mathrm{dyn}}\propto\phi\) in the tested window.

- **MC10 — `MC10_QuadraticCoherenceCoupling_Should_Be_First_Admissible_MemoryInvariant`**  
  **Status:** tested  
  Supports that \(A|\dot{\mu}|\) violates weak-field hierarchy while \(A^2|\dot{\mu}|\) is first admissible and bridge-relevant in the tested window.

- **MC11 — `MC11_DerivedMemoryInvariant_Should_Match_PhotonTransport_Form`**  
  **Status:** tested  
  Supports strong linear proportionality between
  \[
  I_{\mathrm{derived}}=A_{\mathrm{dyn}}^2|\dot{\mu}|
  \]
  and
  \[
  I_{\mathrm{transport}}=\phi^2|\dot{\mu}|
  \]
  up to an effective coupling scale.

- **MC12 — `MC12_DerivedMemoryInvariant_Should_Reproduce_ELBridge_When_Substituted`**  
  **Status:** tested  
  Supports dynamic EL/Fermat bridge retention when substituting \(A_{\mathrm{dyn}}^2|\dot{\mu}|\) for \(\phi^2|\dot{\mu}|\) in the bridge path.

Claim-safe summary:

> Gap 1 is strongly supported by lattice-proxy derivation tests MC09–MC12.  
> The memory term \(\phi^2|\dot{\mu}|\) is no longer only an effective invariant selection; it has a tested microscopic derivation path up to an effective coupling scale.  
> It is not yet theorem-level first-principles closure.
