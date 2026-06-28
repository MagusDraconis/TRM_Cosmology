# TRM First-Principles Gap List

## Purpose

This compact list defines the current first-principles closure gaps that remain after the current review-package hardening.

---

## Gap 1 — Microscopic derivation of \(\phi^2|\dot{\mu}|\)

**Current status**

- strongly supported derivation path via lattice-proxy tests MC09–MC12.
- MC11 shows near-linear form match between \(A_{\mathrm{dyn}}^2|\dot{\mu}|\) and \(\phi^2|\dot{\mu}|\) (\(R^2=0.999799\)).
- MC12 preserves EL/Fermat bridge behavior under substitution (\(\text{bridgeRetention}=0.957676\), \(\text{meanGap}=1.64\times 10^{-4}\)).
- tested microscopic path up to an effective coupling scale.
- not theorem-level first-principles closure yet.

**Gap**

- derive the \(\phi^2|\dot{\mu}|\) memory channel from a microscopic TQM transport/lattice mechanism (not only effective fitting/selection).

---

## Gap 2 — Theorem-level closure for \(m=3\)

**Current status**

- strongly constrained theorem path via RBF16–RBF20.
- \(m=3\) is structurally robust under continuous-threshold, solver-family, exclusion, and artifact-audit checks.
- not theorem-level microscopic proof yet.

**Gap**

- prove (or strictly delimit) theorem-level uniqueness/necessity of \(m=3\), independent of operational threshold families.

---

## Gap 3 — Derive \(O_5,\lambda_\Theta\) from TQM lattice coupling

**Current status**

- strongly supported derivation chain via TO01–TO28, TQK01–TQK04, LC01–LC08, and TOL01–TOL04.
- lattice-energy-supported path for \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\).
- not theorem-level first-principles closure yet.

**Gap**

- close first-principles derivation chain
  \[
  \Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}
  \]
  from TQM lattice/microscopic coupling, beyond effective candidate selection.

---

## Gap 4 — First-principles normalization of vector-sector \(k_T\)

**Current status**

- hardening level: **strongly hardened / not theorem-level**.
- vector frame-dragging candidate sector is structurally and numerically robust (FD01–FD20), with stable effective \(k_T\) normalization.
- FD16 adds a non-fitted microscopic-response normalization proxy for \(k_T\) in the weak-field synthetic lattice setup.
- FD17 shows derived-\(k_T\) stability under source-discretization + geometry + spin-axis ablations (\(k\)-rel-spread \(\approx 0.0597\), holdout mean band \(\approx 0.00546\)).
- FD18 adds a non-refit weak-field GR-Lense-Thirring compatibility window using frozen derived \(k_T\) (\(\text{mean}\approx 1.0068\), \(p10\approx 0.9887\), \(p90\approx 1.0294\)).
- FD19 adds SI/dimension-aware scaling compatibility for frozen derived \(k_T\) across physical unit systems (\(\text{mean}\approx 1.0288\), \(\text{spread}\approx 0.0144\), \(\text{meanBand}\approx 0\)).
- FD20 audits the systematic unit-scaling bias without refit and shows controlled behavior with at least one approximation family reducing absolute bias (small high-side tendency remains within weak-field tolerance).
- still not theorem-level first-principles \(k_T\) closure.

**Gap**

- derive \(k_T\) from first principles and close quantitative weak-field GR benchmark windows without overfitting.

---

## Priority note

These four gaps are the core blockers between:

- **tested-effective, hypothesis-supported multi-sector framework**

and

- **theorem-level first-principles closure claims**.
