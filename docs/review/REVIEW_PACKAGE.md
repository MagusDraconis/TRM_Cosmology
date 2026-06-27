# REVIEW PACKAGE

## 1. What is claimed

- TRM photon transport is treated as a **weak-field effective model** with executable validation tracks.
- The EL/Fermat bridge is validated as an executable bridge path, but not yet a full first-principles closure.
- `gamma ≈ 0.85` is treated as a **band-supported bridge scale** inside an inverse rational collective mode-locking window, not as a uniquely fundamental constant.
- Claim boundary: TRM photon transport admits a **Finsler-like or higher-order optical-action** formulation on a fixed Euclidean base space.

## 2. What is tested

- EL bridge tests: `EL01–EL17` (`PhotonTransportModel_GeodesicSolverTests`).
- Collective mode-locking tests: `CML01–CML08` (`CollectiveModeLockingTests`).
- Memory ablation: `MEM01_MemoryChannel_Zero_Should_Show_DeflectionDeficit`.
- Core fixation and invariants: `TRM78–TRM83` (`PhotonTransportModel_FixationTests`) plus existing CoreRegression suites.

## 3. What is calibrated

- Cosmology core parameters remain calibrated working values: `HT`, `BetaEta`, `Alpha`.
- Parts of photon/cluster effective coefficients remain calibrated or hypothesis-supported.

## 4. What is still open

- First-principles origin and unique selection of the rational collective locking band `Omega ≈ 1.16..1.19`.
- Full first-principles EL/Fermat production closure.
- Frame-dragging / Lense-Thirring coverage in the current scalar path.
- Broader first-principles closure for calibrated cosmology and regime parameters.

## 5. How to run the tests

```bash
# fast hard gate
dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category=CoreRegression"

# default local validation without slow sweeps
dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category!=LongRunning"

# long sweeps, manual/nightly
dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category=LongRunning"
```

## 6. Main documents to read

- `docs/review/TRM_Peer_Review_Request.md`
- `docs/review/TRM_Current_Status_For_PeerReview.md`
- `docs/review/TRM_TestSuite_Classification.md`
- `docs/review/TRM_Real_Physics_Test_Coverage.md`
- `docs/Theory/TRM_Geodesic_Derivation.md`
- `docs/Theory/TRM_Collective_Mode_Locking_BridgeScale.md`
- `docs/Theory/TRM_Finsler_Optical_Action.md`
