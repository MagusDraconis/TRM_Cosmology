# THEORY STATUS (TRM/TQM)

## Current one-line status

TRM/TQM is currently a **tested, falsifiable effective theory framework** with strong numerical support, but it is **not yet a closed first-principles fundamental theory**.

---

## 1. What is already strong (derived/structured + tested)

1. **Photon transport / geodesic layer**
   - Effective transport index is implemented and tested:
   \[
   n_{\mathrm{eff}} = 2 + \lambda_t \phi + \lambda_s \phi^2 |\dot{\mu}|
   \]
   - EL/Fermat bridge path is executable and bounded in weak-field validation windows.
   - Memory-channel guard tests (TRM84–TRM87), bridge ablations (MEM01–MEM02), and higher-order dependency trace (HOA01) are in place.

2. **Collective mode-locking bridge scale**
   - Rational locking band support:
   \[
   \Omega \approx 1.16..1.19 \Rightarrow \gamma \approx 0.84..0.86
   \]
   - Not treated as an isolated single-fit constant.

3. **Closure-family status**
   - \(m=3\) is supported as a robust closure/balance candidate across the current RBF chain.
   - Constraint-removal and derive-or-falsify boundary tests show non-uniform constraint roles (action/tick is key for \(m=3\) minimal selection and uniqueness).

4. **Geometric interpretation boundary**
   - Reviewer-safe statement:
   > TRM photon transport admits a Finsler-like or higher-order optical-action formulation on a fixed Euclidean base space.
   - Stronger claims (full Finsler closure) are explicitly avoided.

5. **Galaxy dynamics direction**
   - Current theory track supports an orbit-integrated, synchronization-driven interpretation over a purely local one.

---

## 2. What is calibrated (not first-principles closed yet)

1. Photon transport coefficients (including practical \(\lambda\)-channel settings).
2. Cosmology parameters (\(H_T\), \(\beta_\eta\), \(\alpha\)) are traceable and test-guarded, but still calibration-backed.
3. Multiple regime/weight parameters in galaxy and cluster layers remain effective-model parameters.

---

## 3. Open first-principles gaps

1. **Memory-term origin**
   - Full microscopic derivation of \(\phi^2|\dot{\mu}|\): why quadratic in \(\phi\), why \(|\dot{\mu}|\), why current coupling structure.

2. **Closure uniqueness**
   - Why exactly \(m=3\) is selected fundamentally (not only as a robust tested candidate).

3. **Theta-field observable map**
   - Formal mapping from \(\Theta(r)\) to \(g_{\mathrm{obs}}(r)\) remains open.

4. **Rotating-mass sector**
   - Frame-dragging / Lense-Thirring is not covered by current scalar TRM.

5. **Full formal closure**
   - The complete variational first-principles closure chain is still incomplete.

---

## 4. Priority next steps

1. **Derive-or-falsify \(m=3\) from microscopic closure constraints** (theorem-level or explicit non-uniqueness outcome).
2. **Derive the memory channel \(\phi^2|\dot{\mu}|\) from TQM phase/lattice transport dynamics.**
3. **Close the \(\Theta(r) \rightarrow g_{\mathrm{obs}}(r)\) observable mapping** with explicit acceptance/falsification criteria.

Progress note:
- Step 1 is now operationalized in the test suite with an explicit derive-or-falsify boundary check (RBF15).
- Step 1 formal closure track is now documented in `docs/Theory/TRM_M3_Closure_First_Principles.md`.
- Step 3 formal closure track is now documented in `docs/Theory/TRM_Theta_Observable_First_Principles.md`.

---

## 5. Claim boundary (recommended wording)

Use:

> TRM/TQM is a developed effective theory framework with strong numerical validation and explicit falsification tests, but with open first-principles derivation gaps.

Avoid:

> TRM/TQM is already a fully derived fundamental replacement theory.
