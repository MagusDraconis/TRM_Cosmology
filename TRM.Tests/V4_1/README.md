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
