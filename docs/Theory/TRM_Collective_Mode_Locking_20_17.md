# TRM Collective Mode Locking: 20:17 Cadence Hypothesis

## Scope

This document summarizes what is currently established and what remains open for the collective cadence hypothesis:

\[
\Omega_{\text{collective}} = \frac{20}{17}
\quad\Longleftrightarrow\quad
\gamma \approx \frac{17}{20} \approx 0.85.
\]

The key goal is not to re-check whether `0.85` can be made to work, but to ask:

\[
\boxed{\text{Why does a }20:17\text{ mode-lock emerge (if it does)?}}
\]

---

## What is currently validated

### EL bridge status (EL01-EL17)

- EL/Fermat bridge path is executable and bounded in weak field.
- Bridge scale is no longer introduced through direct photon-fit-only tuning.
- Synchronization-to-EL bridge path is deterministic and test-backed.
- Robustness and ablation blocks are in place, including cadence-prior boundary behavior.

### Boundary result from EL17

EL17 exposes the current emergence boundary:

- when cadence prior support is reduced/removed,
- `20/17` loses competitiveness against neighboring rational cadence candidates.

Therefore:

**`EulerBridgeScale ≈ 0.85` is currently validated as a prior-assisted collective bridge scale, not yet as first-principles emergent.**

---

## Core open question

\[
\boxed{\text{Why does a }20:17\text{ mode-lock appear at all?}}
\]

This is now the main theoretical question, replacing the older question "does 0.85 work numerically?".

---

## Hypothesis tracks to test

### A) Rational mode-locking hypothesis

\[
\Omega_{\text{collective}}:\Omega_{\text{local}} = 20:17
\Rightarrow
\gamma_{\text{bridge}}=\frac{17}{20}\approx 0.85.
\]

Test if this ratio remains preferred against nearby rational alternatives under identical constraints.

### B) Discrete cell-coupling hypothesis

Check whether `20:17` is linked to discrete cell count, neighborhood topology, or closure structure.

### C) Action/tick bridge hypothesis

Check whether collective cadence selection is consistent with:

- the `gamma = 1.0` tick baseline structure, and
- the observed collective peak near `gamma ≈ 0.85`.

---

## Non-circular testing requirement

To avoid circularity:

- collective mode-lock tests must be executed in the TQM/phase system,
- without direct dependence on `PhotonTransportModel`.

This isolation is required before feeding cadence conclusions back into EL bridge interpretation.

---

## Current status label

Use this wording consistently in review-facing documents:

> `EulerBridgeScale ≈ 0.85`: validated as prior-assisted collective bridge scale; not yet first-principles emergent.
