# Project Structure

Solution: `TRM_Cosmology.slnx`
Root: `D:\Coding\Test\Physics`

## Projects

- `TRM.Core/TRM.Core.csproj`
  - `Domains/`
	- `Domain1.GalacticRotation/`
	- `Domain2.GalaxyClusters/`
	- `Domain3.Cmb/`
	- `Domain4.Supernovae/`
  - `Infrastructure/`
  - `Shared/`
  - `Data/` (scientific source datasets)

- `TRM.CMD/TRM.CMD.csproj`
  - `Program.cs`

- `TRM.QuantumCore/TRM.QuantumCore.csproj`
  - `Fields/`
  - `Fluctuations/`
  - `Planck/`
  - `Statistics/`

- `TRM.Simulations/TRM.Simulations.csproj`
  - `Experiments/`
  - `Pipelines/`

- `TRM.Tests/TRM.Tests.csproj`
  - `CoreTests/`
  - `QuantumTests/`
  - `RealityTests/`
  - `tmpzip/` (large test data files)

- `TRM.Python/TRM.Python.pyproj`
  - `TRM.Python.py`
  - CSV inputs and generated plot/image artifacts

## Notes

- `TRM.Tests/tmpzip` contains many `.dat` files used as test fixtures.
- `TRM.Python` currently contains both source script(s) and generated output files.
