# TRM Service/Test Consolidation (Theory-Aligned)

Stand: repository consolidation aligned to current theory/review docs and the current real-physics coverage baseline.

- Theory/review references used:
  - `docs/theory/TRM_Geodesic_Derivation.md`
  - `docs/review/TRM_Peer_Review_Request.md`
  - `docs/review/TRM_Code_To_Theory_Audit.md`
  - `docs/review/TRM_Real_Physics_Test_Coverage.md`
- Integration note:
  - `TRM_Real_Physics_Test_Coverage.md` is now available and used as the cross-topic coverage baseline for this consolidation.

---

## 1) Consolidation Goal

This document maps active TRM/TQM services, models, and tests to:

1. practical code purpose,
2. implemented equations/model structures,
3. relation to current theory docs,
4. parameter status (derived vs calibrated/placeholder),
5. test coverage quality,
6. unresolved gaps,
7. priority class:
   - **A** = directly publish-ready,
   - **B** = numerically good, theory still incomplete,
   - **C** = exploratory/diagnostic,
   - **D** = uncovered limitation.

---

## 2) Priority Legend

- **A (publish-ready):** strong tests + consistent equations + no central derivation blocker for stated claim scope.
- **B (numerically good, theory gap):** robust numerical behavior, but key terms/parameters still calibrated or only structurally derived.
- **C (exploratory):** scans/diagnostics/hypothesis tests, often soft assertions.
- **D (limitation):** missing direct implementation and/or missing dedicated tests for a claimed effect.

---

## 3) Service/Model/Test Consolidation Matrix

## 3.1 Photon transport / geodesic channel

### `TRM.Core/Shared/PhotonTransportModel.cs`
- **Purpose:** consolidated RK4 photon transport model for reality tests.
- **Implemented structures:**
  - `phi = GM/(c^2 r)`
  - `kBase = 2 + 2a*phi + 3b*phi^2`
  - local channel: `phi^2 * |dmu/dt|`
  - unified index in derivatives: `nEff = 2 + LambdaTime*phi + LambdaSpace*(phi^2*|dmu/dt|)`
  - geometric Shapiro accumulation via `shapiroAccum += phi * ds`
  - speed constraint: post-step renormalization `|v| = c`
- **Theory relation:** directly aligned with `TRM_Geodesic_Derivation.md` structure (`n_eff = 2 + lambda_t phi + lambda_s phi^2 |dot(mu)|`, orthogonal direction change concept).
- **Parameter status:** mixed; several coefficients are calibration-history values (`A`, `B`, `Lambda`) and not first-principles-derived in code.
- **Coverage:**
  - `TRM.Tests/RealityTests/PhotonTransportModel_FixationTests.cs` (TRM78–TRM83)
  - `TRM.Tests/RealityTests/TRM_Realtiy_Tests.cs` (TRM49+, TRM67–TRM77 context)
- **Bridge-derivation validation:**
  - `TRM.Tests/RealityTests/PhotonTransportModel_GeodesicSolverTests.cs` (EL01–EL17)
  - explicit Euler-Lagrange/Fermat branch is executable and bounded against transport + Schwarzschild reference.
- **Gaps:** fully closed formal production pipeline is still missing.
- **Class:** **B**

### `TRM.Tests/RealityTests/PhotonTransportModel_FixationTests.cs`
- **Purpose:** lock model invariants and prevent drift.
- **Coverage specifics:**
  - positivity/finite `nEff`,
  - RK4 speed preservation,
  - nonnegative local memory channel,
  - deflection decreases with impact parameter,
  - Shapiro scale-stable diagnostic under current proportional integration domain,
  - clean time/space channel separation.
- **Theory relation:** strongly consistent with current transport-index formulation.
- **Gaps:** validates invariants, not full uniqueness/physical completeness proof.
- **Class:** **A** (for regression/invariant scope)

### `TRM.Tests/RealityTests/TRM_Realtiy_Tests.cs`
- **Purpose:** historical broad reality suite (Mercury, photon deflection, Schwarzschild-reference comparisons, Shapiro diagnostics).
- **Coverage specifics:**
  - Mercury perihelion series (TRM19+),
  - photon/Schwarzschild comparison blocks (TRM49+),
  - Shapiro test blocks (TRM67+).
- **Theory relation:** useful empirical/numerical bridge to peer-review questions.
- **Gaps:** mixed rigor; includes diagnostic/exploratory style sections.
- **Class:** **C** (valuable but heterogeneous and partly diagnostic)

---

## 3.2 Planck/quantum consistency channel

### `TRM.QuantumCore/Planck/PlanckConstants.cs`
- **Purpose:** Planck constants from SI constants.
- **Equations:**
  - `lP = sqrt(hbar*G/c^3)`
  - `tP = lP/c`
  - `mP = sqrt(hbar*c/G)`
- **Theory relation:** baseline constants layer for uncertainty/tick/action analyses.
- **Coverage:** `PlanckConsistencyTests`, uncertainty suites.
- **Gaps:** no uncertainty propagation/error budget in implementation.
- **Class:** **A**

### `TRM.QuantumCore/Planck/DerivedConstants.cs`
- **Purpose:** derive `c`, `hbar`, `G` from Planck tuple.
- **Equations:**
  - `c = lP/tP`,
  - `hbar = mP*lP^2/tP`,
  - `G = lP^3/(mP*tP^2)`.
- **Coverage:** `PlanckConsistencyTests` confirms reconstruction behavior.
- **Gaps:** no major structural gap for current scope.
- **Class:** **A**

### `TRM.Tests/QuantumTests/PlanckConsistencyTests.cs`
- **Purpose:** consistency, sensitivity, and multiscale scan output.
- **Coverage quality:** strong for internal numeric consistency; includes scan export utility path.
- **Gaps:** scan significance is exploratory unless tied to strict acceptance metrics.
- **Class:** **B**

### `TRM.Tests/QuantumTests/UncertaintyTests.cs` and `UncertaintyTests1.cs`
- **Purpose:** tick/action/phase/synchronization and uncertainty behavior scans.
- **Coverage specifics:** includes gamma scans, preferred temporal tick checks, tick-score minima near gamma≈1 patterns, resonance diagnostics.
- **Theory relation:** relevant to TQM tick/action bridge in peer-review framing.
- **Gaps:** many results are stability/diagnostic-centric; derivation-to-observable chain remains incomplete.
- **Class:** **C**

### `TRM.Tests/QuantumTests/CollectiveModeLockingTests.cs`
- **Purpose:** isolated collective cadence mode-lock validation (`20:17`) without PhotonTransportModel dependency.
- **Coverage specifics:** reproduces 20:17 candidate behavior, coupling dependence, closure-break degradation, and \(\gamma \approx 17/20\) approach from cadence scan.
- **Theory relation:** direct non-circular support for the collective cadence hypothesis block.
- **Gaps:** still prior-assisted; not yet first-principles-derived cadence closure.
- **Class:** **B**

### `TRM.Tests/QuantumTests/TRM_Micro_Makro.cs`
- **Purpose:** early micro/macro phase-coupling and emergent-gravity exploration.
- **Coverage quality:** exploratory with hypothesis-mapping character.
- **Gaps:** not hardened as publication-grade validation pipeline.
- **Class:** **C**

---

## 3.3 Galactic rotation / SPARC-RAR channel

### `TRM.Core/Domains/Domain1.GalacticRotation/OrbitalIntegrationService.cs`
- **Purpose:** orbit-integrated effective acceleration estimate.
- **Implemented structures:**
  - base: `gBase = gBar + sqrt(gBar*a0)`
  - drift-derived global state `phi` with `phiEff = tanh(phi)`
  - post-integration correction: `gFinal = gOrbit * (1 + beta*phiEff)` (`beta=0.4`).
- **Theory relation:** pragmatic extension of RAR-like baseline with global drift state.
- **Coverage:** `OrbitalIntegratedTests`.
- **Gaps:** correction term is heuristic/calibrated rather than first-principles-derived.
- **Class:** **B**

### `TRM.Core/Domains/Domain1.GalacticRotation/TrmFieldSolver.cs`
- **Purpose:** theta-field relaxation solver with source, damping, synchronization, and radial terms.
- **Implemented structures:** local source `log10(1 + gBar/1e-12)`, sync proxy `exp(-contrast)`, radial laplacian/gradient relaxation with clamps.
- **Theory relation:** captures structured field dynamics but currently semi-phenomenological.
- **Coverage:** indirectly through orbital/full/regime core tests.
- **Gaps:** synchronization and clamp strategy are pragmatic/heuristic.
- **Class:** **B**

### `TRM.Tests/CoreTests/RarRelationTests.cs`
- **Purpose:** SPARC/RAR validation across baryon modes and residual statistics.
- **Coverage quality:** broad data-pipeline checks, radius-bin residual behavior, mass-model consistency checks.
- **Gaps:** some expectations are empirical-range based; analytic derivation depth still limited.
- **Class:** **A** (for pipeline validation scope)

### `TRM.Tests/CoreTests/OrbitalIntegratedTests.cs`
- **Purpose:** compare local/orbit/full/regime/dual/adaptive behaviors, RMS and radius-bin diagnostics.
- **Coverage quality:** extensive and operationally strong for model comparisons.
- **Gaps:** model hierarchy remains partly fit-driven rather than fully derived.
- **Class:** **B**

---

## 3.4 Cluster channel

### `TRM.Core/Domains/Domain2.GalaxyClusters/BulletClusterAnalysis2.cs`
- **Purpose:** hydrostatic cluster mass analysis with dynamic mixing and bimodal evaluation path.
- **Implemented structures:**
  - hydrostatic mass from pressure gradient and density,
  - effective gravity weighting via turbulence proxy and redshift coupling,
  - ellipticity registry and damping-style factors.
- **Theory relation:** useful for cluster stress-testing, but largely phenomenological.
- **Coverage:** dedicated deterministic test suite added:
  - `TRM.Tests/CoreTests/BulletClusterAnalysis2Tests.cs`
  - hydrostatic mass finite/positive
  - pressure-gradient response
  - turbulence/damping behavior
  - Newton/TRM/mixed regime bounds
  - no NaN / no infinity checks
- **Gaps:** calibrated factors still dominate interpretation; broader observational hardening still required.
- **Class:** **B** (baseline hardening in place, theory still incomplete)

---

## 3.5 Cosmology scale channel (CMB + Pantheon)

### `TRM.Core/Domains/Domain3.Cmb/CmbAcousticSolver.cs`
- **Purpose:** acoustic oscillator sweep, peak extraction, and TRM scale prediction.
- **Implemented structures:**
  - ODE-based acoustic amplitude integration,
  - peak search in normalized k-space,
  - optimization over drive frequency and Doppler weight,
  - scale prediction via recombination eta/redshift and angular-diameter distance estimate.
- **Theory relation:** operational bridge from TRM parameters to CMB-like scale observables.
- **Coverage:** `ClockworkCosmologyTests`.
- **Gaps:** optimized parameters are fitted numerically; theoretical closure remains incomplete.
- **Class:** **B**

### `TRM.Core/Domains/Domain4.Supernovae/PantheonTrmScaleSolver.cs`
- **Purpose:** residual metrics against observed distance modulus data.
- **Implemented structures:** RMS, mean residual, mean absolute residual, max absolute residual, centered RMS.
- **Theory relation:** direct observational consistency check path.
- **Coverage:** `ClockworkCosmologyTests` (Pantheon current and HT sensitivity).
- **Gaps:** quality depends on calibrated cosmology parameter set.
- **Class:** **B**

### `TRM.Core/Shared/TrmCosmologyParameter.cs`
- **Purpose:** central parameter provider (`BetaEta`, `Alpha`, `HT`).
- **Parameter status:** calibrated working values with explicit traceability and algebraic inversion tests (`CMB` drift relation and `D_base` anchor inversion).
- **Coverage:** consumed by CMB/Pantheon mapper tests and dedicated deterministic `TrmCosmologyParameterTraceTests`.
- **Gaps:** first-principles closure is still open; trace path is executable but anchor selection remains calibrated.
- **Class:** **B** (upgraded from former placeholder-only status; traceability hardened, theory closure still incomplete)

### `TRM.Tests/CoreTests/ClockworkCosmologyTests.cs`
- **Purpose:** integrated CMB ratio/scale checks, distance mapper relations, Pantheon fit checks, HT sensitivity.
- **Coverage quality:** robust operational tests with finite/range assertions.
- **Gaps:** constrained by calibrated cosmology parameters.
- **Class:** **B**

---

## 3.6 Simulation optics channel

### `TRM.Tests/SimulationTests/WaveOpticsTests.cs`
- **Purpose:** wavefront/spatial-curvature-like numerical behavior checks.
- **Coverage specifics:** no-mass behavior, near-mass deflection, step/integration-range convergence, M and 1/b scaling, symmetry checks, baseline pattern checks.
- **Theory relation:** supports numerical consistency of optics-related behavior.
- **Gaps:** approximation-model interpretation must remain explicit.
- **Class:** **B**

---

## 4) Cross-Cutting Gaps Identified

1. **Lense-Thirring / frame-dragging remains not covered by scalar TRM** (no direct implementation/tests in current `TRM.Core` / `TRM.Tests` inventory); this stays an explicit KnownLimitation.
2. **No fully closed production Euler-Lagrange geodesic solver pipeline** despite structural derivation document (**validated bridge track exists** via EL01–EL17).
3. **Cluster service coverage is now baseline-hardened** (dedicated deterministic tests exist), but broader fit-robust and dataset-level hardening is still required.
4. **Central cosmology scalars are now traceable but still calibrated** (`TrmCosmologyParameters` exposes executable derivation-trace equations; first-principles closure remains open).
5. **Historical reality suite is mixed rigor** (hard tests + diagnostic/exploratory blocks).

---

## 5) Final Priority List (A/B/C/D)

## A) Directly publish-ready (within stated scope)
- `TRM.QuantumCore/Planck/PlanckConstants.cs`
- `TRM.QuantumCore/Planck/DerivedConstants.cs`
- `TRM.Tests/RealityTests/PhotonTransportModel_FixationTests.cs` (regression/invariant scope)
- `TRM.Tests/CoreTests/RarRelationTests.cs` (pipeline validation scope)

## B) Numerically good, but theory still incomplete
- `TRM.Core/Shared/PhotonTransportModel.cs`
- `TRM.Core/Domains/Domain1.GalacticRotation/OrbitalIntegrationService.cs`
- `TRM.Core/Domains/Domain1.GalacticRotation/TrmFieldSolver.cs`
- `TRM.Core/Domains/Domain3.Cmb/CmbAcousticSolver.cs`
- `TRM.Core/Domains/Domain4.Supernovae/PantheonTrmScaleSolver.cs`
- `TRM.Core/Shared/TrmCosmologyParameter.cs`
- `TRM.Core/Domains/Domain2.GalaxyClusters/BulletClusterAnalysis2.cs`
- `TRM.Tests/CoreTests/OrbitalIntegratedTests.cs`
- `TRM.Tests/CoreTests/ClockworkCosmologyTests.cs`
- `TRM.Tests/CoreTests/BulletClusterAnalysis2Tests.cs`
- `TRM.Tests/CoreTests/TrmCosmologyParameterTraceTests.cs`
- `TRM.Tests/SimulationTests/WaveOpticsTests.cs`
- `TRM.Tests/QuantumTests/PlanckConsistencyTests.cs`

## C) Exploratory / diagnostic
- `TRM.Tests/RealityTests/TRM_Realtiy_Tests.cs` (heterogeneous historical suite)
- `TRM.Tests/QuantumTests/UncertaintyTests.cs`
- `TRM.Tests/QuantumTests/UncertaintyTests1.cs`
- `TRM.Tests/QuantumTests/TRM_Micro_Makro.cs`

## D) Currently uncovered / limitation
- Missing direct frame-dragging/Lense-Thirring code path and tests

---

## 6) Minimal Next Steps for Hardening

1. Expand `BulletClusterAnalysis2` deterministic baseline tests to stronger dataset-level acceptance thresholds.
2. Add explicit frame-dragging/Lense-Thirring target tests (or explicitly scope out as not covered).
3. Introduce a strict geodesic-derivation-to-code traceability test set for photon transport terms.
4. Separate exploratory/diagnostic reality tests from strict CI-grade acceptance tests.
5. Replace calibrated `BetaEta`/`Alpha`/`HT` anchors with first-principles closure while keeping the new derivation-trace API stable.

---

## 7) Alignment with `TRM_Real_Physics_Test_Coverage.md`

This consolidation is now synchronized with the real-physics coverage document:

- Topic-level coverage status (Newton, redshift/time dilation, Mercury, photon, Schwarzschild comparison, Shapiro, Planck, tick/action, phase lock gamma≈0.85, SPARC/RAR/orbit/theta, cluster, CMB, Pantheon/HT, Euler-Lagrange, frame-dragging limitation) is tracked in:
  - `docs/review/TRM_Real_Physics_Test_Coverage.md`
- Service-level and code-structure classification (A/B/C/D priorities, per-file purpose/equation/gap mapping) remains tracked here in:
  - `docs/review/TRM_Service_Test_Consolidation.md`

Together, the two documents provide:
- one cross-topic physics test status view,
- and one implementation-centric service/model/test consolidation view.
