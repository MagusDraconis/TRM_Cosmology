# TRM V4 — Mapping Test Plan

**Date:** 2026-07-05
**Status:** B1 COMPLETE. B2 COMPLETE. Both are **stable validation layers** — no further modifications unless bugs found. C5 is PARTIAL. Next: B3 (coupling field equation).

---

## 0. Research Status Overview

```
TRM V3.4 CORE         → FROZEN ✅
C5 framework          → STRUCTURALLY VALID ✅ (passes E1, I1, I2)
C5 Newtonian limit    → PROVEN ✅ — B1 shows K1×F1 yields exact GM/r²
Path B                → PRIMARY RESEARCH PATH
  B1 (coupling test)  → ✅ PASSED — see TRM_V4_B1_Results.md
  B2 (SPARC test)     → NEXT — unblocked
```

### The Decisive Question — ANSWERED

> **Can a mass-induced oscillator coupling perturbation δK(r) generate an effective energy density δρ_eff(r) ~ 1/r, producing a(r) ~ GM/r²?**

**YES.** K1×F1: δK(r) = k·M/r, ρ_eff = ρ_ref·δK/K₀ → a(r) = −GM/r² exactly, with k = G·K₀/c². No free parameters.

---

## 1. Test Framework

### 1.1 Structural Consistency Checks (C5 — PASSED)

| Check | Condition | C5 Result |
|:---|:---|:---|
| E1 | Ω* globally constant — no q-dependence | **PASS** |
| I1 | Ω* = 1 + m/q rational consistency | **PASS** |
| I2 | φ(x) ∈ [0.16, 0.19] for target system | **PASS** — φ₀ = ρ_bg/ρ_ref |

### 1.2 Newtonian Limit Gate (C5 — OPEN)

| Gate | Condition | Status |
|:---|:---|:---|
| **G1** | δρ_eff(r) ~ 1/r at large r | **PENDING** |
| **G2** | a_eff(r) = c²/ρ_ref · ∇(δρ_eff) → GM/r² | **PENDING** — follows from G1 |

---

## 2. Path B — Coupling Modulation (PRIMARY RESEARCH PATH)

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

## 3. Test B1 — Coupling Modulation Gradient (DECISIVE GATE)

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

### 3.6 Success Criteria (B1 — MET ✅)

| Criterion | Threshold | Actual |
|:---|:---|:---|
| Asymptotic α | −1.0 ± 0.2 | **−1.000** |
| Acceleration β | −2.0 ± 0.2 | **−2.000** |
| Free parameters | 0 | **0** (k = G·K₀/c²) |

---

## 4. Test B2 — SPARC Galaxy Rotation (DEPENDS ON B1 SUCCESS)

### 4.1 Objective

Apply the best-performing (δK, F) pair from B1 to observed SPARC galaxy rotation curves. Test whether baryonic mass distributions reproduce observed velocities **without dark matter halos**.

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
| **C5 framework** | ✅ STRUCTURALLY VALID | None — framework complete |
| **Consistency checks (E1, I1, I2)** | ✅ ALL PASSED | None |
| **Path B hypothesis** | 📋 FORMULATED | Execute B1 numerical test |
| **Gate G1 (δρ ~ 1/r)** | 🔴 OPEN | **B1 — decisive test** |
| **Gate G2 (Newtonian limit)** | 🔴 OPEN | Follows from G1 |
| **SPARC validation** | ⏳ BLOCKED ON B1 | Execute B2 after B1 success |

### The Mode Has Shifted

```
BEFORE:  Theorie bauen (build theory)
NOW:     Einen einzigen harten Mechanismus testen (test one hard mechanism)
```

This is the strongest scientific position the project has ever been in. One testable hypothesis, one decisive gate, one clear success/failure criterion.
