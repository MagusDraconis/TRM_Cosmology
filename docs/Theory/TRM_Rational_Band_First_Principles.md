# TRM Rational Band: First-Principles Origin

## Core Question

\[
\boxed{
\text{Why does the rational band } \Omega \approx 1.16..1.19 \text{ emerge?}
}
\]

This is now the primary physics question.
The older question ("does \(\gamma \approx 0.85\) work numerically?") is already covered by the EL/CML validation chain.

---

## Current empirical status (already established)

- EL bridge track (`EL01–EL17`) is executable and weak-field bounded.
- Isolated mode-locking track (`CML01–CML08`) supports a rational locking band.
- Band mapping relation:

\[
\Omega \approx 1.16..1.19
\quad\Rightarrow\quad
\gamma = \frac{1}{\Omega} \approx 0.84..0.86
\]

- `20/17` is currently a representative candidate inside the band, not a uniquely derived fundamental constant.

Additional RBF test status:

- RBF01 supports phase closure: intact closure selects/keeps the band strongly; broken closure degrades the score.
- RBF02 shows action consistency alone does not uniquely select the bridge window, but the window remains competitive.
- RBF03 supports lattice scaling: best-omega remains stable across tested cell counts.
- RBF04 confirms the boundary: without structural/prior support, the band is not automatically selected.
- RBF05 provides an explicit analytical closure candidate: \(\Omega=(q+3)/q\) for \(q=16..19\) reproduces the observed band.
- RBF06 compares closure families (\(m=1..5\), \(q=12..24\)): \(m=3\) is structurally competitive and yields stronger bridge-band occupancy than distant neighboring modes.
- RBF07 confirms the compromise interpretation explicitly: with a combined occupancy-vs-cost score, \(m=3\) emerges as a balance mode rather than a strict global cost minimizer.
- RBF08 checks q-window robustness of that balance mode: across shifted windows (\(12..24\), \(14..26\), \(16..28\)), \(m=3\) remains the leading occupancy-cost compromise in the tested q-window shifts (stable rank/gap behavior).
- RBF09 probes neighboring-mode selection (\(m=2,3,4\)) with a structural compromise score (band occupancy + in-band closure quality vs action cost) and selects \(m=3\) as the leading compromise mode.
- RBF10 stress-tests that RBF09 selection under score-weight ablation and finds \(m=3\) robustly retained as a top-ranked compromise mode (with majority wins across tested weight sets).
- RBF11 proposes \(m=3\) as the minimal mode satisfying three operational closure constraints: phase closure, bridge-band occupancy, and action/tick consistency. The result is currently threshold-based and should be treated as a candidate formalization, not yet a uniqueness theorem.
- RBF12 tests threshold robustness of that RBF11 formalization (phase threshold, action-occupancy minimum, and action/tick cost threshold scaling) and keeps \(m=3\) as the minimal closure mode in resolved ablation cases.
- RBF13 identifies action/tick as the key discriminator: without it, minimal mode collapses to \(m=2\).
- RBF14 confirms lock-vs-EL tradeoff against competing bands, not blanket single-band dominance.
- RBF15 shows unique \(m=3\) with the full constraint stack and non-uniqueness without action/tick.
- RBF16 shows unique \(m=3\) persists over a connected threshold region including the baseline point.
- RBF17 derives the action/tick discriminator from a coarse-grained microscopic action proxy.
- RBF18 keeps derived action/tick-based unique \(m=3\) selection across tested solver-step families.
- RBF19 establishes failure-by-family exclusion reasons for neighboring modes.
- RBF20 audits operational artifacts (candidate range, q-window, normalization, occupancy gates, threshold formation, tie-breaking) and keeps \(m=3\) in all resolved scenarios.

Current Gap-2 status:

> strongly constrained theorem path / not theorem-level proof.

---

## What "first-principles origin" means here

To claim first-principles origin, the band must follow from model structure and constraints, not from a tuned cadence prior.

Minimum requirement:

1. A derivation path from base TRM/TQM assumptions to a bounded rational locking interval.
2. Stability of that interval across independent solver parameterizations.
3. No dependence on a narrow hand-shaped cadence prior for the band to appear.

---

## Candidate derivation paths

### A) Phase-closure and resonance topology

Hypothesis:
The band is selected by discrete phase-closure constraints in coupled oscillators.

Target:
Derive admissible rational windows from closure conditions and coupling symmetry.

### B) Variational/action consistency

Hypothesis:
Only cadence windows that minimize a collective optical/action functional remain stable.

Target:
Show that a bounded \(\Omega\)-interval is energetically/action-preferred under TRM transport terms.

### C) Coarse-grained lattice/cell scaling

Hypothesis:
Finite cell-count and coupling neighborhood produce robust rational bands in the continuum limit.

Target:
Demonstrate convergence of the same band under resolution changes, not drift to arbitrary ratios.

---

## Falsification criteria

The first-principles claim fails if any of the following occurs:

1. The band disappears when cadence prior shaping is removed.
2. Preferred interval shifts strongly under modest solver reparameterization.
3. Competing rational windows explain the same bridge behavior equally well without structural penalty.

---

## Next implementation-oriented tasks

1. Derive explicit phase-closure constraints that predict admissible \(\Omega\)-intervals.
2. Build a prior-minimized solver variant and compare predicted interval vs current band.
3. Quantify interval stability under cell-count/coupling/step-size scaling.
4. Define a strict acceptance rule for upgrading from "band-supported" to "first-principles-derived."

Formal closure note:

- The \(m=3\) derive-or-falsify closure formalization track is documented in `docs/Theory/TRM_M3_Closure_First_Principles.md`.

---

## Claim boundary for reviewer-facing text

Use:

> The EL bridge scale is currently constrained by an inverse rational collective locking band. The first-principles origin and unique selection mechanism of that band remain open.

Avoid:

> The band is already derived from first principles.
