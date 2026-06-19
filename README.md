# 🌌 TRM Cosmology (Clockwork Cosmology V2.2)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**TRM Cosmology** is a high-performance .NET 10 analytical framework designed to empirically validate the **Clockwork Cosmology (V2.2)** hypothesis. 

This repository provides the computational engine to demonstrate that the universe can be quantitatively described **without invoking non-baryonic Cold Dark Matter (CDM)**. By redefining gravity as a kinematic drift driven by macroscopic gradients within a ubiquitous **Temporal Rate Matrix (TRM)**, this solution seamlessly unifies astrophysical phenomena across three fundamentally distinct scales.

## 🚀 Key Breakthroughs & Domains

1. **Galactic Scales (SPARC Database):** Natively resolves the Baryonic Tully-Fisher Relation (BTFR) and flat rotation curves using a pure algebraic TRM boundary equation (g_obs = g_bar + sqrt(g_bar * a_0)). 
   * *Result:* Matches the SPARC catalog with a residual scatter of **sigma ≈ 0.1412 dex**, operating exactly at the observational noise floor without ad-hoc empirical interpolation functions (like MOND).
2. **Galaxy Cluster Scales (ACCEPT Database):** Solves the "missing mass" anomaly in galaxy clusters through a parameter-free bimodal phase transition, moderated by morphological ellipticity (beta). 
   * *Result:* Achieves an a priori predictive accuracy of **63.7%** across the ACCEPT X-ray catalog, dynamically preventing unphysical over-corrections in post-merger systems (e.g., the Bullet Cluster and Abell 2744).
3. **Cosmological Scales (CMB / Planck):** Replaces dark matter potential wells with primordial phase-synchronizations. Uses a high-speed Runge-Kutta 4 (RK4) Fourier-space solver to model the photon-baryon fluid.
   * *Result:* Accurately reproduces the primary acoustic peaks of the Cosmic Microwave Background at l ≈ 220 and l ≈ 540 at a derived absolute Euclidean distance of **D_A ≈ 26.2 Gpc**.

---

## 📂 Solution Structure

- `TRM.Core`  
  The theoretical engine. Contains shared analysis models, the RK4 acoustic solver, and grid-sweep optimization algorithms.
- `TRM.CMD`  
  Console entry point to execute selected high-performance analyses and parameter sweeps.
- `TRM.Tests`  
  xUnit test suite acting as the scientific safeguard. Contains dataset-driven validations ensuring the framework strictly adheres to established astrophysical benchmarks.
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
*Note: The console app reads cluster input files from its output/runtime directory via relative paths. Ensure required SPARC/ACCEPT data files are present in the execution directory.*

### Run the Scientific Validations (Tests)
Execute the rigorous xUnit test suite to verify the exact cosmological constants (a_0, eta_rec, D_A) against the latest observational bounds:
`dotnet test TRM.Tests/TRM.Tests.csproj`
*Tests use `ITestOutputHelper` for detailed log outputs, visible directly in your Test Explorer or CLI.*

---

## 📊 Data Visualization

The framework generates data files (e.g., `results.csv`) containing cluster redshifts, pressure gradients, and theoretical improvements. The script `TRM.Python/TRM.Python.py` parses these outputs to create visualizations.

**Expected CSV Columns:**
- `Cluster`
- `z`
- `MaxGradP`
- `Improvement`

**Generate Plots:**
`python TRM.Python/TRM.Python.py`

**Output Image:** `Clockwork_Threshold_Plot.png`

---

## 📌 Notes & Scientific Contribution
- **Data-Driven:** Most analyses depend on external observational datasets. Relevant sample files are included under `TRM.Tests`.
- **Troubleshooting:** If `FileNotFound` errors occur during runtime, verify your working directory and the relative file paths to the data catalogs.
- **Contributing:** This is an open-science initiative. Feel free to open issues or submit Pull Requests if you want to optimize the integrators or test the TRM framework against new astrophysical databases.