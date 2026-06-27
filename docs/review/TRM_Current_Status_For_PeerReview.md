# TRM Current Status for Peer Review

## Snapshot

This status snapshot summarizes the current repository state after EL bridge hardening, cluster deterministic hardening, and cosmology parameter traceability hardening.

## What is green/tested now

- **Photon EL/Fermat bridge path:** `PhotonTransportModel_GeodesicSolverTests` (EL01-EL17) is passing and weak-field bounded against Schwarzschild reference.
- **Cluster service baseline hardening:** `BulletClusterAnalysis2Tests` is passing with deterministic fixtures and finite/bounded checks.
- **Cosmology parameter traceability:** `TrmCosmologyParameterTraceTests` is passing with algebraic inversion checks for `BetaEta`, `Alpha`, and `HT`.
- **Isolated cadence validation:** `CollectiveModeLockingTests` validates 20:17 mode-lock behavior without `PhotonTransportModel` dependency.
- **Core regression gate:** core tagged tests (including new trace tests) are green.

## Validated bridge track

- **Euler-Lagrange/Fermat:** now an executable and validated bridge track (EL01-EL17), but still not a fully closed production derivation chain.

EL09 establishes a deterministic synchronization-to-EL bridge path.
The bridge scale is no longer directly fitted in the photon test, but the synchronization solver still contains a collective-mode prior.
Robustness and ablation tests (EL10–EL17) are now in place; first-principles emergence of gamma≈0.85 remains an open physics task.

EL17 exposes the current emergence boundary: when the cadence prior is removed,
20/17 loses competitiveness against neighboring rational cadence candidates.
Therefore the current synchronization-to-EL bridge is robustly constrained but still cadence-prior assisted.

## What remains calibrated

- **`TrmCosmologyParameters` (`HT`, `BetaEta`, `Alpha`):** remain calibrated working values, now with explicit traceability and inversion tests.
- **Parts of cluster and other effective-model coefficients:** remain fit/calibrated rather than first-principles closed.

## What remains limitation

- **Lense-Thirring / frame-dragging:** not covered by the current scalar TRM implementation (no dedicated model/test path yet).
- **Full EL production closure:** bridge is validated, but full formal closure (including boundary-condition-complete production path) remains open.

## Peer-review package readiness

Current package is coherent and review-ready as a technical baseline:

1. Theory documents
2. Code-to-theory audit
3. Service/test consolidation
4. Test-suite classification
5. Explicit known limitations

## Next physics block (after review package)

**Priority topic:** explain `EulerBridgeScale = 0.85` physically, not only numerically.

- Treat connection to collective peak `gamma ≈ 0.85` as a **testable hypothesis**, not as a claim.
- Define a derivation/constraint path to decide whether the scale is emergent, fitted, or replaceable.

## Opened derivation track: why `collectiveOmega = 20/17`?

After EL09-EL17, the critical prior is now `collectiveOmega = 20/17` in the synchronization-to-EL bridge path.

Current status statement:

- `collectiveOmega = 20/17` is currently a deterministic, test-backed synchronization prior.
- It is **not yet** a first-principles-derived constant.
- `CML05` now provides an explicit no-cadence-prior score check (`0.55*meanOrder + 0.45*alignment`) to distinguish competitiveness vs. boundary behavior.
- `CML06` shows that without explicit cadence prior, `20/17` does not need to strictly win but remains competitive inside a nearby rational mode-locking cluster.
- `CML07` extends this to a dense rational sweep in the `1.16..1.19` window and supports a competitive band interpretation instead of a single-point resonance claim.
- `CML08` links this band to EL weak-field behavior: candidates from the competitive mode-locking band map via `EulerBridgeScale = 1/Ω` into the EL/Schwarzschild weak-field window.
- CML05 removes the explicit cadence prior. Under this condition, 20/17 no longer strictly wins, but remains competitive within a nearby rational mode-locking cluster. This suggests that the bridge scale may arise from a collective rational-locking band rather than from a uniquely selected 20:17 resonance.

Planned falsification/derivation checkpoints:

1. **Mode-ratio identifiability:** compare `20/17` against nearby rational alternatives (`19/16`, `6/5`, `21/18`) under the same robustness matrix.
2. **Prior dominance test:** verify whether gamma-peak location remains stable when collective cadence prior strength is reduced.
3. **Cross-regime invariance:** test whether preferred `collectiveOmega` remains stable across `(kappa, collectiveWeight, cellCount)` ablations.

Acceptance threshold for "emergent":

- A value is treated as emergent only if it is preferred across independent solver settings with bounded spread and without requiring tight prior shaping.
