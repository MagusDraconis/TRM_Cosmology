# TRM Zenodo Release Notes (V3.0 Review Baseline)

## Scope

This release packages the TRM/TQM V3.0 review baseline as a tested-effective weak-field transport/synchronization framework across scalar, vector, theta, and unified-action sectors.

## What is tested-effective

- Scalar photon-transport path with executable EL/Fermat bridge guards.
- Memory-channel derivation path for \(\phi^2|\dot{\mu}|\) with MC09-MC12 hardening.
- Rational mode-locking \(m=3\) closure path with RBF21-RBF23 hardening.
- Nonlocal theta observable chain \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\) with TO/TOL/TQK/LC guard blocks.
- Vector frame-dragging candidate path with FD01-FD20 weak-field structural hardening.
- Unified effective-action roadmap with UF01-UF09 limit and cross-sector guards.

## What remains not derived yet

- Theorem-level microscopic closure for the full \(m=3\) path.
- Theorem-level microscopic closure for memory-channel and theta-observable chains.
- Full GR equivalence is not established.
- First-principles closure for all calibrated cosmology/regime parameters remains open.

## Main documentation entry points

- `docs/review/TRM_Cover_Letter_And_Abstract.md`
- `docs/review/REVIEW_PACKAGE.md`
- `docs/review/TRM_Current_Status_For_PeerReview.md`
- `docs/Theory/TRM_Field_Sector_Map.md`
- `docs/Theory/TRM_First_Principles_Gap_List.md`
- `docs/Theory/TRM_Unified_Field_Action_Roadmap.md`

## Main test categories

- `Category=CoreRegression` (fast hard gate)
- `Category!=LongRunning` (default full local validation)
- `Category=LongRunning` (extended sweeps)

## Claim boundary

TRM V3.0 does not claim to replace General Relativity and does not claim theorem-level first-principles completion. This release is a claim-safe, test-driven, reproducible review baseline.
