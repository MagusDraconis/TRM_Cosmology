# TRM.Tests/V4_1 — V4.1 xUnit Validation Suite

**Branch:** `feature/v4.1-emergent-space`
**Status:** ACTIVE — test-driven derivation workflow

---

## Testing Convention

### Workflow (mandatory for all V4.1 additions)

1. **Add / extend theory document** in `docsV4_1/theory/`
2. **Implement minimal C# support code** in `TRM.Core/V4_1/`
3. **Add xUnit tests** that validate:
   - exact identities if structural (e.g., symmetry, triangle inequality)
   - numerical convergence if continuum (e.g., Δ_G → ∇²)
   - falsification criteria if hypothesis-driven
4. **Only then** summarise conclusions in docs

### Category traits

Every V4.1 test must carry `[Trait("Category", "V4.1")]` plus a specific sub-category:

| Sub-category trait | Maps to |
|:---|:---|
| `V4_1_GraphMetric` | §3.6 Metric Properties |
| `V4_1_CausalPropagation` | §3.5 Causal Bound Argument |
| `V4_1_LaplacianContinuum` | §4.2–4.3, §3.7 |
| `V4_1_FunctionalF` | §5.7 Dimensional Selection Functional |

Run all V4.1 tests:
```
dotnet test --filter "Category=V4.1"
```

### Naming standard

```
V4_1_{NN}_{Component}_{Assertion}
```

Examples:
- `V4_1_01_GraphDistance_IsSymmetric`
- `V4_1_04_GraphLaplacian_EqualsDegreeMinusAdjacency`
- `V4_1_07_DimensionFunctional_ComputesExpectedTerms`

### Claim classification in tests

Tests do **not** claim physical truth. Tests validate:
- Mathematical consistency (structural claims)
- Numerical convergence (continuum diagnostics)
- Correct computation (functional evaluation)

All physical claims remain in `docsV4_1/theory/` with explicit classification.

### Test maturity ladder

| Claim type | Required test layer |
|:---|:---|
| Structural (exact identities) | Exact equality / property tests |
| Continuum (convergence) | Resolution-scan convergence tests |
| Dimensional selection (hypothesis) | Benchmark + falsification tests on synthetic inputs |
| Functional F(D) (framework) | Weight robustness + counterexample tests |

No theory note is considered mature unless there is a matching xUnit test layer.

### V4.1 Test Maturity Ladder

| Level | What | Tests |
|:---|:---|:---|
| 1 | Exact graph-structure (adjacency, degree, undirected, connected) | `V4_1_GraphGenerator_Tests` |
| 2 | Continuum-trend (shell growth N(r) ~ r^D) | `V4_1_DimensionalStructure_Tests` |
| 3 | Spectral diagnostics (λ₂, λ_max, S₁ computable) | `V4_1_SpectralDiagnostics_Tests` |
| 4 | Functional/selection pipeline (graph → F(D)) | `V4_1_FunctionalF_Tests`, `V4_1_DimensionSelection_Benchmark_Tests` |
| 5 | Real graph-family comparison across D | `V4_1_SpectralDiagnostics_Tests` (pipeline integration) |
| 6 | CML synchronisation and bridge-band experiments | *Future — requires simulation infrastructure* |
| 7 | Deterministic sync dynamics on real graph families | `V4_1_SynchronizationDynamics_Tests`, `V4_1_SynchronizationDimensionComparison_Tests` |
| 8 | Dense spectral numerics + regression hardening | `V4_1_NumericsRegression_Tests` — Jacobi solver, determinism |
| 9 | Deterministic parameter scans + regression envelopes | `V4_1_SyncParameterScan_Tests`, `V4_1_SyncRegressionEnvelope_Tests` |
| 10 | Deterministic reporting and figure-ready exports | `V4_1_ReportingExport_Tests`, `V4_1_ReportingSummary_Tests`, `V4_1_ReportingMatrix_Tests` |
| 11 | ScottPlot figure generation | `V4_1_PlotGeneration_Tests` |
| 12 | Paper-ready figure bundles | `V4_1_FigureBundle_Tests` |
| 13 | Analysis and interpretation layer | `V4_1_Analysis_Tests` |
| 14 | Publication-ready paper generation | `V4_1_Publication_Tests` |
| 15 | Real CML dimension audit | `V4_1_CmlDimensionAudit_Tests` |
| 16 | Dispersion and Lorentz audit | `V4_1_DispersionAudit_Tests` |
| 17 | Effective causal-cone / horizon audit | `V4_1_CausalConeAudit_Tests` |
| 18 | Localized defect / mass-source audit | `V4_1_DefectResponseAudit_Tests` |
| 19 | Multi-defect superposition / weak-field composition | `V4_1_MultiDefectSuperposition_Tests` |
| 20 | Proto-1PN / nonlinear correction audit | `V4_1_Proto1PnAudit_Tests` — eta fit, near/far-field window |

Parameter scans must be exportable to stable CSV/JSON. Summary tables and matrix outputs
support later plotting and paper figures. Reporting layers do not add new claims —
they only expose validated diagnostics.
