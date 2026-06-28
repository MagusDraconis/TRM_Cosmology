# TRM Field Sector Map

## Purpose

This one-page map summarizes the current TRM/TQM field-sector architecture for review:

1. scalar transport sector,
2. vector rotational sector,
3. nonlocal Theta-observable sector.

It separates what is tested-effective today from what remains open.

---

## 1) Scalar transport sector

**Core objects**

- \(T\), \(\phi\), memory transport channel (\(\phi^2|\dot{\mu}|\)),
- EL/Fermat executable bridge path.

**Current role**

- weak-field effective transport backbone for photon-path-related behavior.

**Test status (high level)**

- EL/Fermat bridge and transport guards are active (`EL01–EL17`, `MEM01–MEM02`, `TRM84–TRM87`, `MC01–MC08`, `HOA01`, related fixation/core guards).

**Current claim boundary**

- tested-effective and hypothesis-supported in weak-field domain,
- not yet a fully closed first-principles EL production theorem.

**Open items**

- full first-principles closure of the scalar transport chain,
- closure of remaining calibrated coefficients at microscopic level.

---

## 2) Vector rotational sector (frame-dragging candidate)

**Core objects**

- \(\vec A_T\): rotational/time-flow vector potential,
- \(\vec B_T=\nabla\times\vec A_T\): frame-dragging candidate field,
- effective coupling \(k_T\) (currently effective/calibrated).

**Current role**

- candidate sector for weak-field frame-dragging-like behavior.

**Test status (high level)**

- `FD01–FD15` in `TRM.Tests/CoreTests/FrameDraggingVectorExtensionTests.cs`:
  - rotation \(\rightarrow\) nonzero \(A_T/B_T\),
  - spin sign flip,
  - weak-field \(r^{-3}\) shape compatibility,
  - single global effective \(k_T\) normalization with holdout/discretization stability,
  - scalar-limit preservation at zero spin,
  - prograde/retrograde light-path asymmetry.

**Current claim boundary**

- weak-field Lense–Thirring-shape-compatible candidate sector,
- not yet first-principles-derived,
- not yet quantitatively GR-equivalent.

**Open items**

- first-principles derivation of \(k_T\),
- quantitative GR amplitude/reference-window closure,
- strong-field rotating compact-source regime.

---

## 3) Nonlocal Theta-observable sector

**Core objects**

- \(\Theta(r)\),
- nonlocal candidate \(O_5\),
- response coefficient \(\lambda_\Theta\) (regularized regime-conditioned effective form).

**Current role**

- candidate mapping path for \(\Theta \rightarrow g_{\mathrm{obs}}\) under nonlocal identifiability guards.

**Test status (high level)**

- `TO01–TO28`, `TQK01–TQK04`, `LC01–LC08` in `TRM.Tests/CoreTests/ThetaObservableDerivationTests.cs`:
  - structural/operator/energy-gradient/lattice-consistency guards,
  - holdout/leakage/ablation discipline,
  - anti-proxy lambda discipline.

**Current claim boundary**

- tested-effective and hypothesis-supported candidate path,
- not theorem-level fundamental closure of \(\Theta \rightarrow g_{\mathrm{obs}}\).

**Open items**

- first-principles closure of \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\),
- uniqueness/identifiability beyond current effective candidate level.

---

## Cross-sector review-safe summary

> TRM currently consists of a tested-effective scalar backbone, a tested-effective weak-field vector frame-dragging candidate sector, and a tested-effective nonlocal Theta-observable candidate sector.  
> All three remain hypothesis-supported at current closure depth; theorem-level first-principles unification is still open.
