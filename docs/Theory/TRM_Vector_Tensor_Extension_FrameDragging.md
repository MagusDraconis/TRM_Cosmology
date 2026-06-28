# TRM Vector/Tensor Extension for Frame-Dragging (Candidate Sector)

## Scope

This document defines a new **candidate rotational sector** for TRM, added next to the current scalar sector.

Current scalar TRM is explicitly not sufficient for frame-dragging/Lense-Thirring effects.

---

## 1. Core hypothesis

Extend
\[
T(x,t)
\]
to
\[
T(x,t)\ \rightarrow\ \bigl(T,\ \vec A_T,\ Q_{ij}\ (\text{optional})\bigr).
\]

Interpretation:

- \(T\): scalar time-rate field (existing sector),
- \(\vec A_T\): rotational/time-flow vector potential (new sector),
- \(Q_{ij}\): optional shear/tensor correction (future extension, not required for minimal tests).

Target mechanism:

\[
\text{mass rotation / angular momentum}
\ \rightarrow\
\text{rotational transport field}
\ \rightarrow\
\text{precession and light-path asymmetry}.
\]

---

## 2. Minimal vector ansatz (first implementation target)

Define rotating source current density:
\[
\vec J_M(\vec r)=\rho(\vec r)\,\vec v_{\mathrm{rot}}(\vec r).
\]

Define TRM rotational vector potential:
\[
\vec A_T(\vec r)\ \sim\ \int \frac{\vec J_M(\vec r')}{\lvert \vec r-\vec r'\rvert}\,d^3r'.
\]

Define rotational synchronization field:
\[
\vec B_T=\nabla\times \vec A_T.
\]

Working interpretation:

> \(\vec B_T\) is the TRM frame-dragging / rotational synchronization field candidate.

---

## 3. First test block (FD01–FD05)

### Minimal conventions for FD01–FD05

Use normalized units first:

\[
G=c=1,\qquad k_T=1.
\]

Source:
\[
\vec J_M(\vec r)=\rho(\vec r)\,\vec v_{\mathrm{rot}}(\vec r).
\]

Vector potential:
\[
\vec A_T(\vec r)=k_T\int \frac{\vec J_M(\vec r')}{\lvert \vec r-\vec r'\rvert}\,d^3r'.
\]

Frame-dragging candidate field:
\[
\vec B_T=\nabla\times \vec A_T.
\]

Precession proxy:
\[
\Omega_{\mathrm{FD}}\propto \lvert \vec B_T\rvert.
\]

The first block is structural and weak-field-shape oriented, not a GR-equivalence claim.

1. **FD01_RotatingSource_Should_Generate_Nonzero_VectorPotential**  
   Rotating mass source must produce nonzero \(\vec A_T\).

2. **FD02_NonRotatingSource_Should_Generate_Zero_FrameDraggingField**  
   Static/non-rotating source must produce zero (or numerically negligible) \(\vec B_T\).

3. **FD03_FrameDraggingField_Should_FlipSign_When_SpinReverses**  
   Reversing source spin direction must reverse the sign/orientation of \(\vec B_T\).

4. **FD04_FrameDraggingField_Should_Decay_With_Radius**  
   Field magnitude must decay with radius in the weak-field far zone.

5. **FD05_GyroPrecession_Should_Scale_With_SourceAngularMomentum**  
   Gyroscope-like precession proxy must increase with source angular momentum magnitude and track spin orientation.

Structural sequence to verify first:

\[
\text{rotation}
\rightarrow
\vec A_T
\rightarrow
\nabla\times\vec A_T
\rightarrow
\text{sign/decay/precession-proxy behavior}.
\]

---

## 4. GR reference window (weak-field shape target)

Immediate goal is not “TRM replaces GR.”

Immediate target is:

> The TRM vector sector should reproduce the **weak-field Lense-Thirring scaling shape** qualitatively (and then quantitatively in later stages).

Review-safe benchmark direction:

- rotational source \(\Rightarrow\) nonzero frame-dragging-like field,
- no rotation \(\Rightarrow\) no frame-dragging-like field,
- spin reversal \(\Rightarrow\) sign reversal,
- far-field decay consistent with weak-field rotational coupling behavior.

---

## 5. Claim boundary (mandatory)

Use the following boundary wording until FD tests and later quantitative windows are passed:

> Scalar TRM does not cover frame-dragging.  
> The vector extension is a new candidate sector.  
> No GR-equivalence claim is allowed until weak-field Lense-Thirring tests pass.

---

## 6. Current status and next derivation priority

If FD01–FD05 pass, the next block is a dimensional/coupling guard layer:

1. **FD06_VectorPotential_Should_Be_Dimensionally_Consistent**  
   Check scale consistency of \(\vec A_T\) under current-density rescaling.

2. **FD07_FrameDraggingField_Should_Scale_With_CouplingConstant**  
   Require linear \(\vec B_T\) scaling with coupling \(k_T\).

3. **FD08_PrecessionProxy_Should_Be_Zero_For_RadialSpinlessMotion**  
   Enforce zero precession proxy in spinless/non-rotating radial baseline.

4. **FD09_FarField_Should_Approximate_AngularMomentum_DipoleShape**  
   Require far-field dipole-shape behavior for the rotational sector.

Current status:

- FD01–FD05: structural rotational-sector guards are passing.
- FD06–FD09: dimensional/coupling and far-field dipole-shape guards are passing.
- FD10: weak-field Lense-Thirring scaling shape guard is passing.
- FD11: single global effective \(k_T\) normalization against weak-field reference window is passing.
- FD12: holdout normalization guard for \(k_T\) generalization is passing.
- FD13: source-discretization robustness guard is passing.
- FD14: spin-zero fallback guard is passing (vector sector collapses to scalar-limit compatibility).
- FD15: prograde/retrograde light-path asymmetry guard is passing.

Only after FD01–FD09 should the project enter the first weak-field GR-shape comparison window:

5. **FD10_LenseThirring_WeakFieldScaling_Should_Match_GRShape**  
   Compare structural scaling shape against weak-field Lense-Thirring expectations without over-claiming equivalence.

FD10 status (current):

- Implemented as shape-only guard and passing:
  - \(\Omega_{\mathrm{FD}}\) is linear in angular momentum \(J\),
  - \(\Omega_{\mathrm{FD}}\sim r^{-3}\) in far field,
  - spin reversal flips sign,
  - no rotation gives null field.
- This is still a structural weak-field shape match, not a quantitative GR-amplitude match.

Claim-safe update after FD01–FD15:

> The TRM vector extension is now a weak-field frame-dragging candidate sector with structural Lense-Thirring scaling, stable effective coupling, scalar-limit preservation at zero spin, and prograde/retrograde light-path asymmetry.

Still open:

1. first-principles derivation of \(k_T\),
2. quantitative GR-amplitude equivalence window,
3. real Lense-Thirring benchmark window,
4. strong-field rotating compact-source regime behavior.

Until then, this remains a **candidate extension path**, not a closed first-principles sector.
