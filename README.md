# 🌌 TRM Cosmology (Clockwork Cosmology V2.2)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

This repository contains the numerical implementation and analysis tools for the **Temporal Rate Matrix (TRM)** framework, as presented in the paper:

> *Clockwork Cosmology: A Temporal Rate Matrix Framework for Unified Gravitational and Cosmological Dynamics*


## 🔬 Overview

The TRM framework explores an alternative scalar-field approach to gravitational and cosmological phenomena.  
Instead of treating dark matter and dark energy as independent components, the model interprets observed discrepancies as manifestations of a single **temporal rate field**:

\[
\mathcal{T}(x,t)
\]

The repository implements the numerical models used to evaluate this framework across multiple astrophysical domains.

---
## <img src="https://cdn.simpleicons.org/zenodo/0A7BBB" alt="Zenodo" width="18" /> Zenodo link

[![Zenodo DOI](https://img.shields.io/badge/DOI-10.5281%2Fzenodo.20772292-0A7BBB?logo=zenodo&logoColor=white)](https://doi.org/10.5281/zenodo.20772292)

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
*Note: The console app reads input catalogs from the `Data` directory. Ensure required files (e.g., SPARC/ACCEPT catalogs and `Pantheon+SH0ES.dat`) are present in your local execution folder.*

### Run the Scientific Validations (Tests)
Execute the rigorous xUnit test suite to verify the exact cosmological constants (a_0, eta_rec, beta_T) against the latest observational bounds:
`dotnet test TRM.Tests/TRM.Tests.csproj`
*Tests use `ITestOutputHelper` for detailed log outputs, visible directly in your Test Explorer or CLI.*

---

## 📌 Notes & Scientific Contribution
- **Data-Driven:** All analyses depend on external, peer-reviewed observational datasets (SPARC, ACCEPT, Planck, Pantheon+). 
- **Troubleshooting:** If `FileNotFound` errors occur during runtime, verify your working directory and ensure the data catalogs are correctly placed in the `Data` folder and set to "Copy if newer".
- **Contributing:** This is an open-science initiative. Feel free to open issues or submit Pull Requests if you want to optimize the integrators or test the TRM framework against new astrophysical databases.