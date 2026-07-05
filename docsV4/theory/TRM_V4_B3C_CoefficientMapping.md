# TRM V4 — B3C: Coefficient Mapping (α ↔ M)

**Date:** 2026-07-05
**Status:** The remaining open problem in B3
**Predecessors:** B3A (Laplace uniqueness), B3B (1/r origin from coupling defect)
**Key question:** How does physical mass M map to coupling defect strength α, without using G as input?

---

## 1. The Remaining Gap

B3A closed the PDE class. B3B explained the 1/r form. The last open piece:

```
α = ?(M, K₀, ω_i, N, coupling topology)
```

Currently: α = (G·K₀/c²)·M/(4π) — this uses G, which is the thing we're trying to explain.

**The question:** Can α be expressed in terms of TRM-native quantities without referencing G?

---

## 2. What "Predicting G" Would Require

The Newtonian gravitational constant G appears in:

```
a(r) = G·M/r²
```

In the TRM chain:

```
a(r) = (c²/ρ_ref) · ∇(δρ)
     = (c²/ρ_ref) · (ρ_ref/K₀) · ∇(δK)
     = (c²/K₀) · ∇(α/r)
     = (c²·α)/(K₀·r²)
```

For this to equal G·M/r²:

```
α = (G·K₀·M) / c²
```

To "predict G from TRM", we need:

```
G = (c²·α) / (K₀·M)
```

where α and M are independently expressed in TRM terms. This requires:

| Requirement | Meaning |
|:---|:---|
| **α in TRM terms** | Defect strength expressed via K₀, coupling topology, defect size |
| **M in TRM terms** | Physical mass expressed via oscillator energy, frequency, or coupling |
| **Physical frequency scale** | ω_i = 1.0 (dimensionless) must be anchored to a physical Hz value to give K₀ SI units |

---

## 3. Candidate Coefficient Mappings

### 3.1 Candidate C1 — Defect Magnitude Proportional to Mass

**Mapping:**
```
α = κ · M          where κ is a new fundamental constant
```

**TRM expression:** None. κ is a new constant — effectively a renamed G.

**What it achieves:** Nothing beyond current status. Replaces G with κ.

**Classification: NOT SUPPORTED.** This is just renaming G, not deriving it.

---

### 3.2 Candidate C2 — Defect Strength from Modified Intrinsic Frequency

**Mapping:**
```
α ∝ Δω_defect          where Δω_defect = ω_defect − ω_baseline
```

A "mass site" has its intrinsic frequency ω_i shifted from the baseline ω_i = 1.0. The frequency deficit (or excess) determines the coupling perturbation strength.

**Why this might work:**
- ω_i is a native TRM quantity
- A frequency shift is physically interpretable as "mass/energy at a site"
- The CML already has ω_i = 1.0 + 0.05·sin(2πi/N) + 0.03·cos(4πi/N) — frequency variations exist

**Problem:** In the CML, Ω* = mean(ω_i) + α·φ. A frequency shift at one site changes mean(ω_i) by Δω/N, which vanishes as N → ∞. The local effect on K is not obvious — frequency shift affects phase dynamics, not coupling directly.

**Classification: CALIBRATED (could be fitted, not predicted).** Δω → α mapping is not defined by the oscillator model.

---

### 3.3 Candidate C3 — Defect Strength from Cluster Size

**Mapping:**
```
α ∝ N_defect · δK_site          where N_defect = number of defect sites
```

A "mass concentration" corresponds to a cluster of N_defect sites with perturbed coupling. The total defect strength scales with cluster size.

**Why this might work:**
- N_defect is a discrete TRM quantity
- Larger mass → larger defect cluster — physically intuitive
- The total perturbation strength α ∝ N_defect · δK_site

**Problem:** This relates α to cluster size, but does not relate cluster size to physical mass M. The mapping N_defect ↔ M is still open.

**Classification: PARTIALLY DERIVED (structure explained, scale open).** The proportionality α ∝ N_defect is clean, but N_defect ↔ M requires an additional relation.

---

### 3.4 Candidate C4 — Defect Strength from Energy Deficit

**Mapping:**
```
α ∝ E_defect          where E_defect = energy stored in the coupling perturbation
E_defect ∝ Σ_{i,j in defect} (K_ij − K₀) · cos(θ_i − θ_j)
```

In the synchronized state, cos(Δθ) ≈ 1:

```
E_defect ∝ N_defect · δK
α ∝ N_defect · δK
```

**Why this might work:**
- Energy is a native physical quantity
- E = mc² connects energy to mass: M = E_defect / c²
- This gives: α ∝ M·c² → α ∝ M (with c² as conversion factor)

**The key relation:**

```
M = E_defect / c² = (N_defect · δK · R²) / c²      (in appropriate units)
```

If this holds, then:

```
α ∝ N_defect · δK ∝ M·c²/R² ∝ M                    (R is constant in sync)
```

The proportionality constant would be expressible in terms of c² and the synchronization amplitude R².

**This is the most promising candidate — it connects mass to coupling energy via E = mc².**

**Classification: PROMISING (requires verification).** If the coupling perturbation energy can be independently identified with mass-energy, the coefficient mapping closes naturally.

---

### 3.5 Candidate C5 — α as an Empirical Constant (I3 Fallback)

**Mapping:**
```
α = const · M          where const is an irreducible empirical constant
```

This is the Newtonian approach: G is measured, not predicted. TRM would have an analogous constant (call it γ_TRM) that maps mass to coupling defect strength.

**What it achieves:** Formal closure at the cost of one new irreducible constant. Honest — does not claim false derivation.

**Classification: CALIBRATED (honest fallback).** If B3C cannot be derived, I3 = "α ∝ M with empirical proportionality constant" is the fallback. This adds one irreducible input to I1, I2, D1.

---

## 4. The Most Promising Path: C4 (Energy Deficit)

### 4.1 The Chain

```
Mass M
    ↓  E = mc²
Energy E_defect stored in coupling perturbation
    ↓  E_defect = Σ (K_ij − K₀) · cos(Δθ) ≈ N_defect · δK · R²
Coupling perturbation strength δK
    ↓  α ∝ N_defect · δK
Defect strength α
    ↓  Green's function of graph Laplacian
δK(r) = α/r
    ↓  continuum limit
a(r) = G·M/r²     (with G emerging from c², K₀, R²)
```

### 4.2 What This Would Predict

If the energy-deficit mapping holds:

```
G ∝ c² / (K₀ · R² · ρ_ref)     (in appropriate units)
```

G would be expressed in terms of:
- c (speed of light — known)
- K₀ (baseline coupling — requires physical frequency scale)
- R (order parameter ≈ 0.889 from CML)
- ρ_ref (reference energy density — requires cosmological calibration)

**This does not "predict G from nothing" — but it expresses G in terms of TRM quantities plus one physical frequency scale.** That's one level of derivation better than "G is an independent constant."

### 4.3 What's Still Missing

1. **K₀ in physical units:** ω_i = 1.0 is dimensionless. To give K₀ SI units ([1/time]), we need a physical frequency f_ref such that ω_i(physical) = f_ref · ω_i(CML). Then K₀(physical) = f_ref · K₀(CML).
2. **ρ_ref calibration:** φ₀ = ρ_bg/ρ_ref ≈ 0.17 from I2. ρ_bg (cosmic background energy density) must be independently estimated.
3. **R² in the energy relation:** The coupling energy E_defect ∝ δK · R². R is measured (≈ 0.889) but its exact role in the energy needs verification.

---

## 5. Classification Summary

| Candidate | Mechanism | Derivation Depth | Classification |
|:---|:---|:---|:---|
| C1 — Renamed constant | α = κ·M | None — renames G | **NOT SUPPORTED** |
| C2 — Frequency shift | α ∝ Δω | Calibrated — no Δω→α law | **CALIBRATED** |
| C3 — Cluster size | α ∝ N_defect | Partial — structure explained | **PARTIALLY DERIVED** |
| **C4 — Energy deficit** | **α ∝ E_defect/c² ∝ M** | **Promising — uses E=mc² + coupling energy** | **PROMISING** ⭐ |
| C5 — Empirical constant | α = const·M | Honest — one new irreducible input (I3) | **CALIBRATED (fallback)** |

---

## 6. Impact on Overall TRM Status

### If C4 succeeds (energy deficit → α → G):

```
Total irreducible inputs:  I1, I2, D1, + 1 physical frequency scale (f_ref)
G expressed in TRM terms:  G = f(c, K₀, R², ρ_ref)
Classification:            PARTIAL → EFFECTIVE (one calibration remaining: f_ref)
```

### If C4 fails (I3 fallback):

```
Total irreducible inputs:  I1, I2, D1, I3 (α ∝ M with empirical constant)
G remains empirical:       G is measured, not predicted
Classification:            Remains PARTIAL with explicit I3
```

### Current status

```
B3 = FORM EXPLAINED (B3A+B3B), COEFFICIENT CALIBRATED (B3C open)
```

---

## 7. Next Steps

1. **Formalize C4** — write the energy-deficit mapping explicitly in TRM terms
2. **Determine f_ref** — anchor the CML tick to a physical frequency (CMB? Planck? Orbital?)
3. **Verify R² energy relation** — check whether coupling energy ∝ δK·R² holds in the CML
4. **If C4 fails** — accept I3 as honest fallback, document the 3-input theory
