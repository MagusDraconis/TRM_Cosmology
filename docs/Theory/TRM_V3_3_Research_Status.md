# TRM V3.3 Research Status (Theory-facing)

This theory-facing status note mirrors the current reviewer-safe V3.3 posture and highlights the m=3 boundary diagnostics track.

## RBF27–RBF37 m=3 action-derived boundary diagnostics

- RBF27: derived action/tick reproduces the operational discriminator structure.
- RBF28 and RBF30: baseline derived full-stack selects `m=3` as the only admissible mode.
- RBF29 and RBF31: `m=3` shows a phase/action boundary and `m=2` fallback under stress.
- RBF32: `m=3` shows local baseline continuity with multiple admissibility components.
- RBF33: stronger shared action-margin gating does not cleanly remove `m=2` fallback without threshold-tuning risk.
- RBF34: solver-step variants remain stable in the tested setup; q-window shifts can still move admissibility strongly.
- RBF35–RBF37: bridge-core q-window geometry explains much of `m=3` selection; support is strongest when q-window contains bridge-core support.

Status:

> action-derived bounded three-constraint bridge-mode candidate.

Claim boundaries:

- diagnostic/candidate only
- not theorem-level proof
- not first-principles closure
- not universal `m=3` selection
- not GR replacement
- no numerology

For the broader exploratory status matrix and cross-sector context, see:

- `docs/review/TRM_V3_3_Research_Status.md`
