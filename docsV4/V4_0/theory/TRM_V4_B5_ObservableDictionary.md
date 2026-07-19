# TRM V4 — B5: Observable Dictionary

**Date:** 2026-07-05
**Status:** Mapping TRM V4 field structure to measurable physical quantities
**Predecessors:** B1–B4 (full mechanism chain), B3C-T3 (I3 = cesium), B4-T1 (c_K = c)

---

## 1. The Dictionary

| # | Observable | TRM Expression | Derivation Depth | Status |
|:---|:---|:---|:---|:---|
| **O1** | Local time rate | T(x) = 1 + φ(x) = 1 + ρ_E(x)/ρ_ref | DEFINED (C5) | Framework |
| **O2** | Acceleration | a(x) = c²·∇T(x) | DERIVED (B1+B3B) | FORM EXPLAINED |
| **O3** | Gravitational potential | φ(x) = ρ_E(x)/ρ_ref | DEFINED (C5) | Framework |
| **O4** | Redshift | z = Δφ/(1+φ) ≈ Δφ | DERIVED (from O1+O2) | EFFECTIVE |
| **O5** | Time dilation | dt/dτ = T(x) | DERIVED (from O1) | EFFECTIVE |
| **O6** | Light deflection | α = (2/c²) ∫ ∇φ·dl | DERIVED (from O2, Fermat) | EFFECTIVE |
| **O7** | Shapiro delay | Δt = −(2/c³) ∫ φ·dl | DERIVED (from O1, propagation) | EFFECTIVE |
| **O8** | Rotation curve | v(r) = √(r·a(r)) | DERIVED (from O2) | CALIBRATED |
| **O9** | Gravitational wave speed | c_K = c | ASSUMED (B4-T1) | ASSUMED |
| **O10** | Gravitational constant | G = (c²·δK·R²)/(K₀_phys·4π) | DERIVED in form, CALIBRATED in value | FORM EXPLAINED |

---

## 2. Observable Derivations

### O1 — Local Time Rate

```
T(x) = 1 + φ(x)
φ(x) = ρ_E(x) / ρ_ref
```

**Source:** C5 energy density interpretation.
**Status:** DEFINED. T(x) is the interpretation of the emergent collective frequency Ω* as a local field.

### O2 — Acceleration

```
a(x) = c² · ∇T(x) = c² · ∇φ(x)
```

**Source:** Gradient of time-rate field.
**Static point mass:** a(r) = −GM/r² (Newtonian, with k = G·K₀/c² calibration).
**Status:** FORM EXPLAINED (B3A+B3B). Coefficient calibrated (B3C, k = G·K₀/c²).

### O3 — Gravitational Potential

```
φ(x) = ρ_E(x) / ρ_ref
φ(r) = α/r = (G·K₀·M)/(c²·r)     (static point mass)
```

**Source:** Energy density → time-rate offset.
**Status:** DEFINED. φ is the dimensionless clock-bias parameter interpreted as normalized energy density.

### O4 — Redshift

**Derivation:**

For light emitted at x₁ (potential φ₁) and observed at x₂ (potential φ₂):

```
ν_obs / ν_em = T(x₂) / T(x₁) = (1 + φ₂) / (1 + φ₁)

z = (ν_em − ν_obs) / ν_obs = (1 + φ₁) / (1 + φ₂) − 1
```

For weak fields (φ ≪ 1):
```
z ≈ φ₁ − φ₂ = Δφ
```

For a static point mass:
```
φ(r) = −G·M/(c²·r)     →     z = G·M/(c²·r₁) − G·M/(c²·r₂)
```

This matches the GR weak-field prediction:
```
z_GR = G·M/(c²·r₁) − G·M/(c²·r₂)     ✓
```

**Status:** EFFECTIVE. Matches GR in the weak-field limit. φ = GM/(c²r) is used — the same calibration path as Newtonian gravity.

### O5 — Time Dilation

**Derivation:**

Proper time dτ vs coordinate time dt:
```
dτ/dt = T(x) = 1 + φ(x)
```

For a static point mass:
```
dτ/dt = 1 − G·M/(c²·r)     (φ is negative for attractive gravity)
```

This matches the GR weak-field prediction:
```
dτ/dt_GR = √(1 − 2G·M/(c²·r)) ≈ 1 − G·M/(c²·r)     ✓
```

**Status:** EFFECTIVE. Matches GR to first order in φ. Second-order deviation is testable (e.g., GPS satellite clocks).

### O6 — Light Deflection

**Derivation:**

Light propagates in the time-rate field. The effective refractive index:
```
n(x) = 1 / T(x) ≈ 1 − φ(x)     (for φ ≪ 1)
```

Fermat's principle: light path minimizes ∫ n·dl. The deflection angle:

```
α = (2/c²) ∫ ∇φ · dl_perp
```

For a static point mass with impact parameter b:
```
α = 4G·M/(c²·b)
```

**Comparison:**
- Newton (corpuscular): α = 2G·M/(c²·b)
- GR: α = 4G·M/(c²·b)
- TRM V4: α = 4G·M/(c²·b)     ✓

**Status:** EFFECTIVE. The factor of 2 relative to Newton arises from the spatial + temporal contributions to the light path — same as in GR. Requires φ = GM/(c²r) calibration.

### O7 — Shapiro Delay

**Derivation:**

Light propagation time in the time-rate field:
```
Δt = (1/c) ∫ n·dl ≈ (1/c) ∫ (1 − φ)·dl = t_0 − (1/c) ∫ φ·dl
```

The excess delay relative to flat spacetime:
```
δt = −(2/c³) ∫ φ·dl
```

For a radar signal grazing the Sun (impact parameter b):
```
δt ≈ (4G·M/c³) · ln(4·r_Earth·r_target / b²)
```

**Status:** EFFECTIVE. Matches GR weak-field prediction. Existing TRM tests (TRM67–TRM77) reproduce Shapiro delay numerically.

### O8 — Rotation Curve

```
v(r) = √(r · a(r)) = √(r · c² · dφ/dr)
```

For a point mass: v(r) = √(G·M/r) (Keplerian).
For a distributed mass M(r): v(r) = √(G·M(r)/r).

**Status:** CALIBRATED. SPARC data fitted with TRM baryonic models. B2 phenomenological consistency tests pass. Dark matter replacement not yet derived.

### O9 — Gravitational Wave Speed

```
c_K = c     (B4-T1 assumption)
```

**Status:** ASSUMED. Supported by LIGO (c_gw = c ± 10⁻¹⁵). Not derived from TRM oscillator dynamics.

### O10 — Gravitational Constant

```
G = (c² · δK · R²) / (K₀ · f_ref · 4π · ρ_ref · L_char³)
```

Where all quantities are TRM-native except f_ref (I3) and a characteristic length L_char.

**Status:** FORM EXPLAINED (expression exists). Value CALIBRATED (requires f_ref + density calibration).

---

## 3. Summary Matrix

| Observable | TRM Chain | Depth | Matches GR? | Calibration needed? |
|:---|:---|:---|:---|:---|
| **Time rate T(x)** | φ = ρ_E/ρ_ref → T = 1+φ | DEFINED | N/A (framework) | ρ_ref |
| **Acceleration a(x)** | a = c²·∇T → GM/r² | FORM EXPLAINED | ✓ (weak field) | k = G·K₀/c² |
| **Redshift z** | z = ΔT/T ≈ Δφ | EFFECTIVE | ✓ (weak field) | φ = GM/(c²r) |
| **Time dilation** | dτ/dt = T(x) | EFFECTIVE | ✓ (1st order) | Same as redshift |
| **Light deflection** | α = (2/c²)∫∇φ·dl | EFFECTIVE | ✓ (factor 4) | φ = GM/(c²r) |
| **Shapiro delay** | δt = −(2/c³)∫φ·dl | EFFECTIVE | ✓ | φ = GM/(c²r) |
| **Rotation curve** | v = √(r·a) | CALIBRATED | Competitive (SPARC) | a₀ from data |
| **GW speed** | c_K = c | ASSUMED | ✓ (by assumption) | LIGO validation |
| **G constant** | Composite expression | FORM EXPLAINED | ✓ (via calibration) | f_ref, ρ_ref |

---

## 4. What Each Observable Requires

| Observable | What is derived | What is assumed/calibrated |
|:---|:---|:---|
| T(x) | φ↔ρ_E mapping (C5) | ρ_ref reference density |
| a(x) | ∇T → acceleration, 1/r² scaling (B3A+B3B) | k = G·K₀/c² (Newton calibration) |
| z | ΔT/T redshift formula | φ = GM/(c²r) potential form |
| dτ/dt | T(x) time dilation | Same as redshift |
| Light deflection | Fermat + n = 1/T, factor 4 | φ = GM/(c²r) |
| Shapiro delay | Propagation time integral | φ = GM/(c²r) |
| Rotation curve | v = √(r·a) kinematics | SPARC a₀, baryonic mass models |
| c_K | Wave equation PDE class | c_K = c identification |
| G | Composite TRM expression | f_ref (I3), ρ_ref, δK calibration |

---

## 5. Cross-Reference

| Observable | Test files |
|:---|:---|
| O4 (Redshift) | `TRM.Tests/RealityTests/TRM_Realtiy_Tests.cs` (TRM redshift tests) |
| O5 (Time dilation) | `TRM.Tests/RealityTests/TRM_Realtiy_Tests.cs` |
| O6 (Deflection) | `TRM.Tests/RealityTests/PhotonTransportModel_*.cs` (EL01–EL17) |
| O7 (Shapiro) | `TRM.Tests/RealityTests/TRM_Realtiy_Tests.cs` (TRM67–TRM77) |
| O8 (Rotation) | `TRM.Tests/CoreTests/RarRelationTests.cs` |
| O2 (Acceleration) | `TRM.Tests/V4/B1_*.cs`, `TRM.Tests/V4/B3B_*.cs` |
