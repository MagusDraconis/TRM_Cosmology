# TRM Current Status for Peer Review

## Snapshot

TRM/TQM is currently a **tested, falsifiable effective theory framework** with strong numerical support.  
It is **not yet** a closed first-principles fundamental theory.

## What is green/tested now

- **Photon transport + EL/Fermat bridge:** `PhotonTransportModel_GeodesicSolverTests` (`EL01–EL17`, `MEM01–MEM02`) is passing with weak-field bounded EL/Schwarzschild behavior.
- **Memory-channel structural guards:** `PhotonTransportModel_FixationTests` (`TRM84–TRM87`, `MC01–MC04`) and `HOA01` are passing.
  - Quadratic \(\phi\)-scaling, linear \(|\dot\mu|\)-scaling,
  - weak-field subleading behavior,
  - separation from pure time-channel behavior,
  - higher-order dependency trace \((x, v, \dot v)\).
- **Claim-boundary guard:** `CLAIM01` is passing (frame-dragging/Lense-Thirring remains explicitly out of scope for scalar TRM).
- **Collective locking / closure track:** `CML01–CML08` and `RBF05–RBF15` are passing in the current repository baseline.
- **Cluster deterministic baseline hardening:** `BulletClusterAnalysis2Tests` is green.
- **Cosmology traceability hardening:** `TrmCosmologyParameterTraceTests` is green.

## Current interpretation of the RBF track

- \(m=3\) is strongly supported as the current robust closure/balance candidate.
- `RBF13` shows non-uniform constraint roles: removing action/tick collapses minimal mode to \(m=2\), while phase/direction removal alone does not.
- `RBF14` shows a **locking-vs-EL tradeoff** across neighboring rational bands (not unique single-band victory).
- `RBF15` operationalizes derive-or-falsify boundary logic:
  - with all three constraints: unique \(m=3\),
  - without action/tick: non-unique set with minimal collapse to \(m=2\).

## What remains calibrated

- `TrmCosmologyParameters` (`HT`, `BetaEta`, `Alpha`) are traceable and regression-guarded, but still calibration-backed.
- Parts of effective coefficients in photon/cluster/regime layers remain calibrated working parameters.

## What remains limitation/open

- **Memory-term first-principles origin:** \(\phi^2|\dot\mu|\) is strongly test-guarded but not yet microscopically derived from TQM.
- **Closure uniqueness theorem:** \(m=3\) is strongly supported but not yet theorem-level unique.
- **Theta observable closure:** \(\Theta(r)\rightarrow g_{\mathrm{obs}}(r)\) is still open.
- **Frame-dragging / Lense-Thirring:** not covered in current scalar TRM.
- **Full formal EL production closure:** executable bridge exists, full formal closure remains open.

## Peer-review package readiness

Current package is coherent and review-ready as a technical baseline:

1. Theory documents (`TRM_Geodesic_Derivation.md`, `TRM_Finsler_Optical_Action.md`, `THEORY_STATUS.md`, `TRM_Memory_Channel_First_Principles.md`, `TRM_M3_Closure_First_Principles.md`, `TRM_Theta_Observable_First_Principles.md`)
2. Code-to-theory audit
3. Service/test consolidation
4. Test-suite classification
5. Explicit known limitations and claim boundaries

## Next physics block (active priority order)

1. **Derive-or-falsify \(m=3\)** from microscopic closure constraints (operationalized test boundary is already in place via `RBF15`).  
   - Formal closure track is now opened in `docs/Theory/TRM_M3_Closure_First_Principles.md`.
2. **Derive \(\phi^2|\dot\mu|\)** from TQM phase/lattice transport dynamics (beyond effective-form fit).  
   - Derivation track is now opened in `docs/Theory/TRM_Memory_Channel_First_Principles.md`.
3. **Close \(\Theta(r)\rightarrow g_{\mathrm{obs}}(r)\)** with explicit acceptance/falsification criteria.
   - Formal closure track is now opened in `docs/Theory/TRM_Theta_Observable_First_Principles.md`.
