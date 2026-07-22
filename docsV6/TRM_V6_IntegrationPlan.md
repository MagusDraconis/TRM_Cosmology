# TRM V6 — Integration Plan

**Version:** V6.1 | **Date:** 2026-07-22

---

## 1. Overview

This document outlines the plan for integrating the V6 geometry module into the main
TRM project. V6.0 implemented the geometry as a standalone test module. V6.1 will
integrate it into the production codebase.

**Current state:**
- V6Geometry class: TRM.Tests/V6/V6_0/V6_Geometry.cs
- 10 tests: all pass, cross-seed validated
- Branch: feature/v6.0-geometry-implementation → feature/v6.1-documentation-integration

---

## 2. Phase 1: Code Integration (V6.1)

### 2.1 Move to Production Code

```
TRM.Tests/V6/V6_0/V6_Geometry.cs  →  TRM.Core/Geometry/V6Geometry.cs
Namespace: TRM.Tests.V6_0          →  TRM.Core.Geometry
Class name: V6Geometry              →  V6Geometry (unchanged)
```

### 2.2 Add to TRM.Core Project

- Add file to `TRM.Core/Geometry/` directory
- Update namespace to `TRM.Core.Geometry`
- Add `using TRM.Core.Geometry;` where needed
- Update test files to reference new namespace

### 2.3 Build and Verify

- `dotnet build` must succeed
- All 10 V6 tests must pass after migration

---

## 3. Phase 2: Test Integration (V6.2)

### 3.1 Move Tests to Standard Location

```
TRM.Tests/V6/V6_0/  →  TRM.Tests/V6/V6_0/  (keep location)
```

### 3.2 Add to CI Pipeline

- V6_0 tests should run on every PR
- LongRunning tag: V6 tests take ~5 seconds total — acceptable
- Consider adding category for "V6_Smoke" for 4 fast tests

---

## 4. Phase 3: V5 Integration (V6.3)

### 4.1 Connect V6 to V5 Simulation Pipeline

The V5 SAC simulation produces (km, d_mean, Omega) arrays.
V6 computes geometry from these arrays.

```
V5:  K → Sim → RP → Nm → DL → Cupd → K'  (epoch loop)
       ↓
V6:  V6Geometry.ComputeTrajectory(km[], dm[], om[])
```

### 4.2 Add V6 Analysis to Existing Tests

- After each SAC simulation, optionally compute V6 geometry
- Add V6 metrics to simulation result objects
- Enable V6 invariant checks in validation tests

---

## 5. Phase 4: Performance Optimization (V6.4)

### 5.1 Current Performance

- V6 computations are O(N²) per epoch (same as SAC itself)
- No significant overhead beyond existing SAC simulation

### 5.2 Optimization Opportunities

- Cache km and d_mean computations (already computed in SAC)
- Vectorize I₁, I₂ computations using SIMD
- Parallelize cross-seed validation

---

## 6. Phase 5: Production Readiness (V6.5)

### 6.1 Documentation Complete

- [x] User Guide (docsV6/TRM_V6_UserGuide.md)
- [x] Theory Document (docsV6/TRM_V6_Theory.md)
- [x] Integration Plan (this document)
- [ ] API Reference (XML docs)
- [ ] Migration Guide (V5 → V6)

### 6.2 Validation Complete

- [x] 10/10 tests pass
- [x] Cross-seed validated (10 seeds)
- [x] N-scaling validated (N=67-100)
- [ ] Large-N validation (N=200+)
- [ ] Multi-K₀ validation

### 6.3 Performance Acceptable

- [x] < 1 second per seed at N=72
- [ ] < 10 seconds for 100-seed ensemble at N=72

---

## 7. Dependencies

| Component | Dependency | Impact if Changed |
|:----------|:-----------|:------------------|
| Cupd function | K₀, ξ | I₁, I₂ coefficients change |
| Graph topology | Erdős-Rényi | Invariants may change |
| Omega distribution | Uniform [0.9, 1.1] | I₂ may shift |
| Dt | 0.05 | Convergence rate may change |

---

## 8. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|:-----|:----------:|:------:|:-----------|
| I₁, I₂ not invariant for new K₀ | Medium | High | Validate across K₀ sweep |
| g₂₂ not → 1 for seeds > 9 | Low | Medium | Validate 100 seeds |
| Performance issues at N=200 | Low | Low | Optimization in Phase 4 |
| Breaking changes in V5 | Low | High | V5 is frozen (M3++, Stop-Low) |

---

*Generated 2026-07-22.*
