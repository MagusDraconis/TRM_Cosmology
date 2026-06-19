# TRM Cosmology

TRM Cosmology is a .NET 10 solution for astrophysical data analysis focused on:

- galaxy-cluster profile analysis (Coma/Bullet style datasets)
- SPARC-based Radial Acceleration Relation (RAR) analysis
- Baryonic Tully-Fisher relation checks
- CMB acoustic spectrum sampling

## Solution Structure

- `TRM.Core`  
  Shared analysis models and algorithms.
- `TRM.CMD`  
  Console entry point to run selected analyses.
- `TRM.Tests`  
  xUnit test suite with dataset-driven validation.
- `TRM.Python`  
  Python plotting script for visualizing CSV results.

## Prerequisites

- .NET SDK 10
- Visual Studio 2026 (or `dotnet` CLI)
- Python 3.11+ (for `TRM.Python`)

Python packages:

```powershell
pip install pandas matplotlib seaborn numpy
```

## Build

From solution root:

```powershell
dotnet build TRM_Cosmology.slnx
```

## Run the Console App

```powershell
dotnet run --project TRM.CMD/TRM.CMD.csproj
```

The console app reads cluster input files from its output/runtime directory via relative paths. Ensure required data files are present where the app is executed.

## Run Tests

```powershell
dotnet test TRM.Tests/TRM.Tests.csproj
```

Tests use `ITestOutputHelper` for log output, visible in Test Explorer test details.

## Python Visualization

The script `TRM.Python/TRM.Python.py` expects a `results.csv` file with columns:

- `Cluster`
- `z`
- `MaxGradP`
- `Improvement`

Run:

```powershell
python TRM.Python/TRM.Python.py
```

Output image:

- `Clockwork_Threshold_Plot.png`

## Notes

- Most analyses are data-driven; dataset files are included under `TRM.Tests`.
- If file-not-found errors occur, verify working directory and relative file paths.
