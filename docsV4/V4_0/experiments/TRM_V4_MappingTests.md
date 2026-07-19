# TRM V4 — Mapping Test Plan

**Date:** 2026-07-05
**Status:** B1 COMPLETE. B2 COMPLETE. Both are **stable validation layers** — no further modifications unless bugs found. C5 is PARTIAL. Next: B3 (coupling field equation).

---

## 0. Research Status Overview

```
TRM V3.4 CORE         → FROZEN ✅
C5 framework          → STRUCTURALLY CONSISTENT ✅ (passes E1, I1, I2)
C5 Newtonian limit    → PHENOMENOLOGICAL MATCH — K1×F1 yields exact GM/r²
                        (post-hoc calibration: k = G·K₀/c², PARTIAL)
Path B                → STABLE VALIDATION LAYER
  B1 (coupling test)  → STABLE ✅ — unique mechanism identified
  B2 (SPARC test)     → STABLE ✅ — phenomenological consistency test
  B3 (field equation) → OPEN 🔴 — the decisive unresolved step
```

### The Decisive Question — REFRAMED

> **Can a mass-induced oscillator coupling perturbation δK(r) generate an effective energy density δρ_eff(r) ~ 1/r, producing a(r) ~ GM/r²?**

**Phenomenologically:** YES — K1×F1 is the unique mechanism among 16 candidates. δK(r) = k·M/r, ρ_eff = ρ_ref·δK/K₀ → a(r) = −GM/r² exactly, with k = G·K₀/c².

**From first principles:** NOT YET — k = G·K₀/c² is post-hoc calibration. WHY δK ~ M/r is assumed (Laplace/Poisson), not derived from oscillator dynamics. This is B3.

**Current classification: PARTIAL.** Unique phenomenological bridge. First-principles derivation of the coupling field equation remains open.

---

## 1. Test Framework

### 1.1 Structural Consistency Checks (C5 — PASSED)

| Check | Condition | C5 Result |
|:---|:---|:---|
| E1 | Ω* globally constant — no q-dependence | **PASS** |
| I1 | Ω* = 1 + m/q rational consistency | **PASS** |
| I2 | φ(x) ∈ [0.16, 0.19] for target system | **PASS** — φ₀ = ρ_bg/ρ_ref |

### 1.2 Newtonian Limit Gate (C5 — PHENOMENOLOGICAL MATCH)

| Gate | Condition | Status |
|:---|:---|:---|
| **G1** | δρ_eff(r) ~ 1/r at large r | **PHENOMENOLOGICAL MATCH** — K1×F1 satisfies this analytically when K1 is chosen as the δK form |
| **G2** | a_eff(r) = c²/ρ_ref · ∇(δρ_eff) → GM/r² | **PHENOMENOLOGICAL MATCH** — follows from G1 with k = G·K₀/c² (post-hoc) |
| **G3** | First-principles derivation of δK ~ M/r | **OPEN** — B3: coupling field equation not yet derived from oscillator dynamics |

---

## 2. Path B — Coupling Modulation (STABLE VALIDATION LAYER)

### 2.1 Core Hypothesis

Mass is not a "thing" placed in space — it is a **modulation of the oscillator coupling K_ij**:

```
M → δK(r) → δρ_eff(r) → a(r)
```

This stays entirely within the TRM framework:
- K_ij already exists in the oscillator model
- Coupling encodes spatial adjacency
- No external ontology needed

### 2.2 Required Chain

```
Step 1: Mass M perturbs coupling       δK(r) = f(M, r)
Step 2: Coupling gradient → density    δρ_eff(r) = F(K, ∇K, sync metrics)
Step 3: Density gradient → gravity     a(r) = c²/ρ_ref · ∇(δρ_eff)
Step 4: Verify                         a(r) ~ GM/r²
```

---

## 3. Test B1 — Coupling Modulation Gradient (STABLE — PHENOMENOLOGICAL MATCH)

### 3.1 Objective

Test whether any physically motivated δK(r) form produces δρ_eff(r) ~ 1/r and a(r) ~ GM/r².

### 3.2 Coupling Perturbation Candidates

| Candidate | Form | Physical Motivation |
|:---|:---|:---|
| δK-A | M/r | Inverse-distance modulation (1D wave equation Green's function) |
| δK-B | M/r² | Inverse-square (3D isotropic decay) |
| δK-C | M·exp(−r/r₀) | Yukawa-like screening |
| δK-D | from Σ_baryon(r) | Direct mapping from baryonic surface density |

### 3.3 Effective Density Mappings

| Mapping | Form | Rationale |
|:---|:---|:---|
| F1 — Direct | ρ_eff(r) ∝ K(r) | Simplest — coupling strength as density |
| F2 — Gradient | ρ_eff(r) ∝ \|∇K(r)\| | Density from coupling change rate |
| F3 — Sync energy | ρ_eff(r) ∝ local sync energy E_sync(r) | Energy stored in maintaining sync against perturbation |
| F4 — Action proxy | ρ_eff(r) ∝ action residual δS(r) | Deviation from minimal action as energy cost |

### 3.4 Numerical Test Protocol

```
For each (δK candidate, F mapping) pair:
  1. Compute δρ_eff(r) profile
  2. Fit asymptotic scaling: δρ_eff(r) ∝ r^α, extract α
  3. Test α ≈ −1 (required for Newtonian match)
  4. Compute a_eff(r) = c²/ρ_ref · ∇(δρ_eff)
  5. Compare with a_N(r) = GM/r²
  6. Classify: VALID (α ≈ −1), PARTIAL (α ∈ [−1.5, −0.5]), INVALID (otherwise)
```

### 3.5 Classification Matrix (B1 RESULTS)

| | K1 (M/r) | K2 (M/r²) | K3 (exp) | K4 (Σ_bar) |
|:---|:---|:---|:---|:---|
| **F1** (K direct) | **✅ VALID** α=−1.000 β=−2.000 | ❌ INVALID α=−2 β=−3 | ❌ INVALID α=undef | ⏳ B2 |
| **F2** (\|∇K\|) | ❌ INVALID α=−2 β=−3 | ❌ INVALID α=−3 β=−4 | ❌ INVALID α=undef | ⏳ B2 |
| **F3** (sync E) | **✅ VALID** ≡ F1 | ❌ INVALID ≡ F1 | ❌ INVALID ≡ F1 | ⏳ B2 |
| **F4** (action) | **✅ VALID** ≡ F1 | ❌ INVALID ≡ F1 | ❌ INVALID ≡ F1 | ⏳ B2 |

**Best pair: K1×F1 — δK = k·M/r, ρ_eff = ρ_ref·δK/K₀, k = G·K₀/c². See `TRM_V4_B1_Results.md` for full derivation.**

### 3.6 B1 Results Summary

| Criterion | Threshold | Actual | Notes |
|:---|:---|:---|:---|
| Asymptotic α | −1.0 ± 0.2 | **−1.000** | Exact analytic match for K1×F1 |
| Acceleration β | −2.0 ± 0.2 | **−2.000** | Exact analytic match |
| Unique mechanism | Only 1 of 16 pairs works | **K1×F1 only** | No degeneracy |
| Coupling constant | Must predict G | **k = G·K₀/c² (post-hoc)** | K₀ not independently determined |
| First-principles status | Derivation from oscillator | **NOT DERIVED** | δK ~ M/r is assumed (Laplace), not derived |

**Result: K1×F1 is the unique phenomenological mechanism. Classification: PARTIAL — post-hoc calibration, B3 unresolved.**

---

## 4. Test B2 — SPARC Galaxy Rotation (PHENOMENOLOGICAL CONSISTENCY TEST)

### 4.1 Objective

Apply the best-performing (δK, F) pair from B1 to observed SPARC galaxy rotation curves. Test whether the K1×F1 mechanism produces phenomenologically consistent rotation curves. **Note: B2 is a consistency test, not a derivation test — k = G·K₀/c² is post-hoc, and the coupling field equation (B3) remains open.**

### 4.2 Mapping Chain

```
Baryons Σ_bar(r) → δK(r) → δρ_eff(r) → a_eff(r) → v_pred(r) = √(r · a_eff)
```

### 4.3 Test Protocol

```
For each SPARC galaxy (or representative subset):
  1. Input: baryonic surface density Σ_bar(r) [gas + stars]
  2. Map Σ_bar → δK(r) using best B1 candidate
  3. Map δK(r) → δρ_eff(r) using best B1 mapping
  4. Compute a_eff(r) = c²/ρ_ref · ∇(δρ_eff)
  5. Compute v_pred(r) = √(r · a_eff(r))
  6. Compare with v_obs(r)
  7. Evaluate: shape, outer flatness, residuals
```

### 4.4 Evaluation Criteria

| Criterion | Description |
|:---|:---|
| Shape agreement | Does v_pred(r) reproduce the characteristic flat rotation curve shape? |
| Outer flatness | Does v_pred(r) asymptote to constant v_flat without DM halo? |
| BTFR consistency | Does the baryonic Tully-Fisher relation emerge naturally? |
| Single ρ_ref | One global ρ_ref for all galaxies (no per-galaxy tuning)? |

### 4.5 Success / Failure Definition

| Outcome | Interpretation |
|:---|:---|
| v_pred matches v_obs across diverse galaxies with single ρ_ref | **C5 phenomenologically consistent** — coupling modulation mechanism matches Newtonian limit (post-hoc calibration, PARTIAL) |
| v_pred matches only with per-galaxy ρ_ref tuning | PARTIAL — correct mechanism, missing scale law |
| v_pred fails to produce flat rotation curves | C5 **phenomenologically insufficient** — coupling modulation alone does not explain observed rotation curves without additional physics |

---

## 5. Deprioritized Candidates (Historical Reference)

| Candidate | Form | Classification | Reason Deprioritized |
|:---|:---|:---|:---|
| C1 — Classical gravity | φ = −GM/(c²r) | PARTIAL | Scale mismatch 34,000× at galactic scales |
| C2 — Exponential decay | φ₀·exp(−r/r₀) | NOT EVALUATED | Free parameters r₀, not TRM-native |
| C3 — Constant + perturbation | φ₀ + δφ(x) | ABSORBED into C5 | Now the φ₀/δφ decomposition in C5 |
| C4 — Medium/buoyancy (BB11) | φ as intrinsic medium | NOT EVALUATED | Reinterprets I2 rather than calibrates it |

---

## 6. Summary

| Layer | Status | Next Action |
|:---|:---|:---|
| **C5 framework** | ✅ STRUCTURALLY CONSISTENT | None — framework complete |
| **Consistency checks (E1, I1, I2)** | ✅ ALL PASSED | None |
| **B1 (coupling mechanism)** | ✅ STABLE — unique phenomenological match | None — frozen unless bug found |
| **B2 (SPARC consistency)** | ✅ STABLE — phenomenological consistency test | None — frozen |
| **B3 (coupling field equation)** | 🔴 OPEN | Derive δK ~ M/r from oscillator dynamics |
| **Overall classification** | **PARTIAL** | Post-hoc calibration; first-principles derivation open |

### The Mode Has Shifted

```
BEFORE:   Theorie bauen (build theory)
B1/B2:   Einen einzigen harten Mechanismus testen (test one hard mechanism)
NOW:     Die Feldgleichung finden (find the field equation) ← B3
```

C5 is a stable phenomenological bridge. K1×F1 is the unique mechanism. The single open question — **why does δK ~ M/r?** — is now precisely isolated in B3.
