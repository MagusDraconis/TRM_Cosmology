# Project Structure

Solution: `TRM_Cosmology.slnx`
Root: `D:\Coding\Test\Physics`

## Repository Root

- `.github/`
  - `copilot-instructions.md`
- `docs/`
  - `Theory/`
  - `review/`
  - `Archive/`
- `TRM.CMD/`
- `TRM.Core/`
- `TRM.Python/`
- `TRM.QuantumCore/`
- `TRM.Simulations/`
- `TRM.Tests/`
- `README.md`
- `CITATION.cff`
- `.zenodo.json`
- `ProjectStructure.md`

## Projects

- `TRM.Core/TRM.Core.csproj`
  - `Baryons/`
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
  - `SimulationTests/`
  - `tmpzip/` (large test data files)

- `TRM.Python/TRM.Python.pyproj`
  - `TRM.Python.py`
  - CSV inputs and generated plot/image artifacts

## Documentation

- `docs/review/`
  - `REVIEW_PACKAGE.md`
  - `TRM_Cover_Letter_And_Abstract.md`
  - `TRM_Current_Status_For_PeerReview.md`
  - `TRM_Zenodo_Release_Notes.md`
- `docs/Theory/`
  - `TRM_Field_Sector_Map.md`
  - `TRM_First_Principles_Gap_List.md`
  - `TRM_First_Principles_Roadmap.md`
  - `TRM_Unified_Field_Action_Roadmap.md`
  - plus detailed theorem/derivation notes

## Notes

- `TRM.Tests/tmpzip` contains many `.dat` files used as test fixtures.
- `TRM.Python` currently contains both source script(s) and generated output files.
