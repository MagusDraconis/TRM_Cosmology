# 🌌 Temporal Rate Matrix / Temporal Quantum Matrix — V3.0 Review Baseline

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Zenodo DOI](https://img.shields.io/badge/DOI-10.5281%2Fzenodo.21012262-0A7BBB?logo=zenodo&logoColor=white)](https://doi.org/10.5281/zenodo.21012262)

> Claim-safe status: TRM/TQM V3.0 is a tested-effective, hypothesis-supported weak-field transport/synchronization framework. It does not claim theorem-level first-principles closure or equivalence with General Relativity.

This repository contains the numerical implementation and analysis tools for the **Temporal Rate Matrix (TRM)** / **Temporal Quantum Matrix (TQM)** framework.

> *Temporal Rate Matrix / Temporal Quantum Matrix — V3.0 Review Baseline*


## 🔬 Overview

The TRM/TQM framework is organized as a multi-sector weak-field effective model (scalar, vector, nonlocal theta, unified action roadmap) with explicit test guards and claim boundaries.

Instead of claiming a completed curvature-first replacement theory, the model treats observed discrepancies as effective transport/synchronization behavior around a temporal-rate field:

\[
\mathcal{T}(x,t)
\]

The repository implements the numerical models used to evaluate this framework across multiple astrophysical domains.

## ✅ Current Branch Baseline (V3.0 Review Release)

- **Scalar transport + bridge guards:** EL/Fermat executable bridge path remains tested and bounded.
- **Memory-channel path:** `MC09–MC12` hardens the \(\phi^2|\dot{\mu}|\) derivation path up to effective-coupling level.
- **Rational closure path:** `RBF21–RBF23` hardens \(m=3\) via minimal three-constraint model, bounded perturbation stability, and phase-lattice-energy-based action/tick discriminator.
- **Theta observable path:** TO/TQK/LC/TOL guard blocks support \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\) as tested-effective chain.
- **Vector sector path:** `FD01–FD20` hardens weak-field frame-dragging candidate behavior and non-fitted effective \(k_T\) workflow.
- **Unified action path:** `UF01–UF09` guards scalar/vector/theta limit recovery, bounded small cross-terms, and integration preservation.
- **Local gates stabilized:** category-based workflow is active (`CoreRegression`, `Category!=LongRunning`, `Category=LongRunning`).


## 🧭 Reviewer quick start

> TRM/TQM V3.0 is a tested-effective, hypothesis-supported multi-sector framework. It is not a claim to replace General Relativity and does not claim theorem-level first-principles closure.

Key reproducibility commands:

`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category=CoreRegression"`

`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category!=LongRunning"`

## 📚 V3.0 paper set

- `docs/papers/Paper1_TRM_V3_Framework/TRM_V3_0_Framework_Review_Baseline.pdf`
- `docs/papers/Paper2_Memory_ModeLocking/TRM_V3_0_Memory_and_ModeLocking.pdf`
- `docs/papers/Paper3_Theta_Vector_UnifiedAction/TRM_V3_0_Theta_Vector_and_UnifiedAction.pdf`

## 🕰️ Version lineage

- `V3.0`: current review baseline (multi-sector framework with explicit claim boundaries).
- `V2.2` and `V1`: legacy historical baselines.

## 📊 Implemented Domains

This repository includes computational models and analysis scripts for:

- **Galactic Rotation Curves (SPARC)**
  - Non-linear co-fit for the acceleration scale \( a_0 \)
  - Reproduction of flat rotation curves and BTFR

- **Galaxy Clusters (ACCEPT)**
  - Pressure-triggered regime transition
  - Bimodal classification (Newtonian vs TRM-supported)

- **Cosmic Microwave Background (Planck)**
  - k-space acoustic analysis
  - Temporal phase-coherence modeling

- **Cosmological Expansion (Pantheon+)**
  - Luminosity-distance fitting
  - Temporal drift coefficient \( \beta_{\mathcal{T}} \)

---

## 📂 Solution Structure

- `TRM.Core`  
  The theoretical engine. Contains shared analysis models, the RK4 acoustic solver, and grid-sweep optimization algorithms.
- `TRM.CMD`  
  Console entry point to execute selected high-performance analyses and parameter sweeps via an interactive menu.
- `TRM.Tests`  
  xUnit test suite acting as the scientific safeguard. Includes sector hardening blocks (MC09–MC12, RBF21–RBF23, TO/TQK/LC/TOL, FD01–FD20, UF01–UF09), plus domain validations and regression gates.
- `TRM.Python`  
  Python plotting pipeline for visualizing output data (CSV) into publication-ready graphs.

---

## ⚙️ Getting Started

### Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/)
- Visual Studio 2026 (or `dotnet` CLI)
- Python 3.11+ (for `TRM.Python` visualizations)

Install required Python packages for plotting:
`pip install pandas matplotlib seaborn numpy`

### Build the Project
From the solution root directory:
`dotnet build TRM_Cosmology.slnx`

### Run the Analysis (Console App)
`dotnet run --project TRM.CMD/TRM.CMD.csproj`
*Note: The console app reads input catalogs from the `Data` directory. Ensure required files (e.g., SPARC/ACCEPT catalogs and `Pantheon+SH0ES.dat`) are present in your local execution folder.*

### Run the Scientific Validations (Tests)
Execute the rigorous xUnit test suite to verify the exact cosmological constants (a_0, eta_rec, beta_T) against the latest observational bounds:
`dotnet test TRM.Tests/TRM.Tests.csproj`
*Tests use `ITestOutputHelper` for detailed log outputs, visible directly in your Test Explorer or CLI.*

Run only the fast hard regression gate:
`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category=CoreRegression"`

Run the default suite without slow sweeps:
`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category!=LongRunning"`

Run long-running sweeps manually/nightly:
`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category=LongRunning"`

---

## 📄 Key Review/Theory Documents

- `docs/review/TRM_Cover_Letter_And_Abstract.md`
- `docs/review/REVIEW_PACKAGE.md` *(recommended reviewer start point)*
- `docs/review/TRM_Peer_Review_Request.md`
- `docs/review/TRM_Current_Status_For_PeerReview.md`
- `docs/review/TRM_Service_Test_Consolidation.md`
- `docs/review/TRM_TestSuite_Classification.md`
- `docs/review/TRM_Real_Physics_Test_Coverage.md`
- `docs/review/TRM_Code_To_Theory_Audit.md`
- `docs/Theory/TRM_Field_Sector_Map.md`
- `docs/Theory/TRM_First_Principles_Gap_List.md`
- `docs/Theory/TRM_Unified_Field_Action_Roadmap.md`
- `docs/Theory/TRM_Geodesic_Derivation.md`
- `docs/Theory/TRM_Collective_Mode_Locking_BridgeScale.md`
- `docs/Theory/TRM_Finsler_Optical_Action.md`

---

## 📌 Notes & Scientific Contribution
- **Data-Driven:** All analyses depend on external, peer-reviewed observational datasets (SPARC, ACCEPT, Planck, Pantheon+). 
- **Troubleshooting:** If `FileNotFound` errors occur during runtime, verify your working directory and ensure the data catalogs are correctly placed in the `Data` folder and set to "Copy if newer".
- **Contributing:** This is an open-science initiative. Feel free to open issues or submit Pull Requests if you want to optimize the integrators or test the TRM framework against new astrophysical databases.