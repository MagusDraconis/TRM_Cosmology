# TRM Theta-O5-Lambda Theorem Path (Gap 3)

## Scope

This document defines the theorem-path structure for Gap 3:
\[
\text{TQM lattice energy}
\rightarrow
\Theta
\rightarrow
O_5
\rightarrow
\lambda_\Theta
\rightarrow
g_{\mathrm{obs}}.
\]

Current claim-safe status:

> strongly constrained theorem path / not theorem-level proof.

---

## 1. Current tested evidence

From `ThetaObservableDerivationTests.cs`:

- **TO01–TO28:** tested-effective operator-selection and stability path for theta-observable candidates, including holdout, leakage guards, solver ablations, finite-coherence behavior, and energy-operator checks.
- **TQK01–TQK04:** tested consistency between small-phase lattice reduction and theta-coherence energy/operator interpretation.
- **LC01–LC08:** tested lambda-discipline path (dimensional consistency, holdout, regularized regime conditioning, strata-safe guards, anti-proxy checks).

Evidence classification:

- tested: yes
- hypothesis-supported: yes
- first-principles microscopic closure: not yet

---

## 2. The target closure chain

The required first-principles chain is:
\[
E_{\mathrm{TQM}}[\theta]
\Rightarrow
E_\Theta[\Theta]
\Rightarrow
O_5 \sim -\frac{\partial E_\Theta}{\partial \Theta}
\Rightarrow
\lambda_\Theta\ \text{from response theory}
\Rightarrow
g_{\mathrm{obs}} = g_{\mathrm{base}} + \lambda_\Theta O_5.
\]

This is the exact closure target for Gap 3.

---

## 3. TQM lattice energy \(\rightarrow\) \(\Theta\) field

Working path:

1. Start from phase-lattice coupling energy in \(\theta_a\).
2. Apply small-phase/coarse-grained reduction to a radial/coherence-scale field \(\Theta(r)\).
3. Identify the effective theta-energy \(E_\Theta\) and its valid regime.

Current support:

- TQK01 supports the reduction direction.
- The full microscopic uniqueness of \(E_\Theta\) is not yet proven.

---

## 4. \(\Theta\) field \(\rightarrow\) \(O_5\) finite-coherence operator

Working path:

1. Define finite-coherence interaction window \(W\) and coupling kernel class.
2. Require operator behavior consistent with synchronization tension.
3. Enforce energy-gradient consistency:
\[
O_5 \approx -\frac{\partial E_\Theta}{\partial\Theta}.
\]

Current support:

- TO21–TO28 and TQK02–TQK04 support this as a tested-effective/hypothesis-supported operator path.
- \(W6\) is currently the smallest stable medium-coherence window in the tested setup, not a proven universal constant.

---

## 5. \(O_5\) \(\rightarrow\) \(\lambda_\Theta\) response coefficient

Working path:

1. Treat \(\lambda_\Theta\) as a response coefficient linking \(O_5\) to observable acceleration correction.
2. Constrain \(\lambda_\Theta\) by dimensionality, holdout behavior, regularization, and anti-proxy guards.
3. Reject both extremes: global-only rigidity and per-galaxy identity fitting.

Current support:

- LC01–LC08 strongly constrain operational misuse.
- \(\lambda_\Theta\) is still an effective disciplined coefficient, not yet derived from microscopic response theory.

---

## 6. \(\lambda_\Theta\) \(\rightarrow\) \(g_{\mathrm{obs}}\) correction

Current effective map:
\[
g_{\mathrm{obs}} = g_{\mathrm{base}} + \lambda_\Theta O_5.
\]

Interpretation:

- operationally stable and tested-effective in current guard structure,
- still not a theorem-level unique observable map from first principles.

---

## 7. What is still missing for theorem closure

1. A microscopic derivation of \(E_\Theta\) with explicit assumptions and validity bounds.
2. A non-ambiguous derivation of the \(O_5\) kernel/window class from lattice physics (not only selection by tests).
3. A response-theory derivation for \(\lambda_\Theta\), replacing operational regularization as primary justification.
4. A uniqueness/identifiability result for the map to \(g_{\mathrm{obs}}\) against nearby reparameterizations.

---

## 8. Falsification criteria

The theorem path fails if any of the following occurs:

1. no stable small-phase reduction from \(E_{\mathrm{TQM}}[\theta]\) to \(E_\Theta[\Theta]\),
2. \(O_5\) cannot be maintained as an energy-gradient finite-coherence operator under independent checks,
3. \(\lambda_\Theta\) requires per-galaxy proxy behavior to stay competitive,
4. \(g_{\mathrm{obs}}\) correction is equally explained by local reparameterization without structural penalty.

---

## 9. Proposed next tests for Gap 3 closure

1. **TO29_ThetaEnergyFunctional_Should_Emerge_From_PhaseLatticeCoarseGraining**  
   Derive and test explicit parameter mapping from lattice-phase energy to \(E_\Theta\).
2. **TO30_O5KernelWindow_Should_Follow_From_DerivedCorrelationScale_Not_Search**  
   Replace window search logic with derived coherence-length prediction.
3. **LC09_LambdaTheta_Should_Be_Derived_From_LinearResponse_Around_ThetaEnergyMinimum**  
   Derive \(\lambda_\Theta\) from response around coarse-grained equilibrium.
4. **TO31_ThetaToGobs_Map_Should_Remain_Identifiable_Against_LocalReparameterizations**  
   Strengthen non-local identifiability with explicit competing-map exclusion.

These tests should target microscopic closure, not additional unconstrained score tuning.

---

## Claim boundary

Use:

> The Theta-O5-lambda chain is now strongly constrained at tested-effective level and structured as a theorem path, but it is not yet a theorem-level microscopic proof.

Avoid:

> O5, \(\lambda_\Theta\), and \(g_{\mathrm{obs}}\) mapping are already first-principles-derived.
