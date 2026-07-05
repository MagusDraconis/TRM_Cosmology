# TRM V4 — B3C-T3: Frequency Anchor Selection

**Date:** 2026-07-05
**Status:** Comparative ranking of f_ref candidates → recommendation
**Predecessors:** B3C-T2 (candidate catalog), B3C-T1 (energy deficit)

---

## 1. Selection Criteria

Each candidate is scored against 6 criteria (0 = fail, 1 = marginal, 2 = good):

| # | Criterion | Question |
|:---|:---|:---|
| **S1** | Independence from G | Is f_ref defined without using G? |
| **S2** | Independence from TRM assumptions | Would using this f_ref be circular for TRM claims? |
| **S3** | Dimensional consistency | Does f_ref have correct dimensions [1/time]? |
| **S4** | Cosmological consistency | Is f_ref universal (same everywhere)? |
| **S5** | Standard definability | Is f_ref precisely measurable and reproducible? |
| **S6** | Interpretive fit | Is f_ref naturally connected to TRM oscillator physics? |

---

## 2. Candidate Evaluation

### A — CMB Thermal Frequency: f_ref = k_B·T_CMB / h ≈ 5.68×10¹⁰ Hz

| Criterion | Score | Rationale |
|:---|:---|:---|
| S1 (G-independent) | 2 | Does not contain G — depends on k_B, h, T_CMB |
| S2 (TRM-circular) | 1 | **Caution:** If TRM claims to derive CMB structure, using CMB T as anchor is circular. Currently TRM uses CMB as calibration target, not derivation target. Acceptable if CMB remains calibration-only. |
| S3 (Dimensional) | 2 | k_B [J/K] × T [K] / h [J·s] = [1/s] ✓ |
| S4 (Universal) | 2 | T_CMB is isotropic to 10⁻⁵ — universal within observable universe |
| S5 (Measurable) | 2 | T_CMB = 2.72548 ± 0.00057 K (COBE/FIRAS) — precisely known |
| S6 (TRM fit) | 1 | Cosmological — connects to φ₀ (background energy density). No direct oscillator connection. |
| **TOTAL** | **10/12** | |

### B — Orbital (Solar): f_ref = 1/T_orbital

| Criterion | Score | Rationale |
|:---|:---|:---|
| S1 (G-independent) | 0 | Orbital period depends on G·M — contains G |
| S2 (TRM-circular) | 0 | Using G-dependent quantity to derive G is maximally circular |
| S3 (Dimensional) | 2 | [1/time] ✓ |
| S4 (Universal) | 0 | Solar-system specific — not universal |
| S5 (Measurable) | 2 | Precisely measured |
| S6 (TRM fit) | 0 | No connection to oscillator physics |
| **TOTAL** | **4/12** | **REJECTED — CIRCULAR** |

### C — Planck Frequency: f_ref = √(c⁵/(ħ·G))

| Criterion | Score | Rationale |
|:---|:---|:---|
| S1 (G-independent) | 0 | **Explicitly contains G in the definition** |
| S2 (TRM-circular) | 0 | f_ref ∝ 1/√G → using it to derive G is algebraic identity |
| S3 (Dimensional) | 2 | [1/time] ✓ |
| S4 (Universal) | 2 | Fundamental constant combination |
| S5 (Measurable) | 2 | Computable from known constants |
| S6 (TRM fit) | 1 | Planck scale is natural for quantum gravity — but TRM is not quantum gravity |
| **TOTAL** | **7/12** | **REJECTED — CIRCULAR (S1=0 is disqualifying)** |

### D — Intrinsic Synchronization

| Criterion | Score | Rationale |
|:---|:---|:---|
| S1 (G-independent) | 2 | No G dependence |
| S2 (TRM-circular) | 2 | Native to TRM |
| S3 (Dimensional) | 0 | **Not independently fixed** — ω_i = 1.0 is a normalization convention, not a measurement |
| S4 (Universal) | 0 | No physical value exists to compare across systems |
| S5 (Measurable) | 0 | Cannot be independently measured — it IS the convention |
| S6 (TRM fit) | 2 | Perfectly native — but trivially so (it IS the model parameter) |
| **TOTAL** | **6/12** | **REJECTED — NOT INDEPENDENTLY FIXED** |

### E — Dark Energy Scale: f_ref = c·√Λ

| Criterion | Score | Rationale |
|:---|:---|:---|
| S1 (G-independent) | 2 | Λ is measured from cosmic expansion, not from G |
| S2 (TRM-circular) | 0 | **If TRM claims Λ via φ₀ (ρ_bg), using Λ as anchor is circular within TRM** |
| S3 (Dimensional) | 2 | [1/time] ✓ |
| S4 (Universal) | 2 | Λ is constant throughout observable universe |
| S5 (Measurable) | 1 | Λ ≈ 1.1×10⁻⁵² m⁻² from Planck+BAO+SNe — known but less precise than CMB T |
| S6 (TRM fit) | 1 | Connects to φ₀ via ρ_bg, but the connection IS the claim being tested |
| **TOTAL** | **8/12** | **REJECTED — TRM-CIRCULAR (S2=0)** |

### F — Cesium Atomic Standard: f_ref = 9.192631770×10⁹ Hz

| Criterion | Score | Rationale |
|:---|:---|:---|
| S1 (G-independent) | 2 | Atomic transitions depend on α, m_e — not G |
| S2 (TRM-circular) | 2 | No TRM claim depends on cesium frequency |
| S3 (Dimensional) | 2 | [1/time] ✓ — defines the SI second |
| S4 (Universal) | 2 | Same everywhere (Lorentz invariance of atomic physics) |
| S5 (Measurable) | 2 | **Defined exactly** — 9,192,631,770 Hz by SI definition. Zero measurement uncertainty. |
| S6 (TRM fit) | 0 | No connection to oscillator network physics. Entirely conventional. |
| **TOTAL** | **10/12** | |

---

## 3. Final Ranking

| Rank | Candidate | Score | Status |
|:---|:---|:---|:---|
| **1** | **F — Cesium (SI second)** | **10/12** | **PREFERRED** ⭐ |
| 2 | A — CMB thermal | 10/12 | ACCEPTABLE |
| 3 | E — Dark energy Λ | 8/12 | REJECTED (TRM-circular) |
| 4 | C — Planck | 7/12 | REJECTED (contains G) |
| 5 | D — Intrinsic sync | 6/12 | REJECTED (not independently fixed) |
| 6 | B — Orbital | 4/12 | REJECTED (contains G, not universal) |

---

## 4. Why Cesium Wins Over CMB

Both score 10/12, but cesium wins on the decisive criterion:

| Aspect | Cesium | CMB |
|:---|:---|:---|
| **TRM circularity risk** | **ZERO** — no TRM claim depends on atomic physics | **NON-ZERO** — TRM uses CMB as calibration target; using it as anchor is a potential circularity |
| **Measurement precision** | **EXACT** (defined, zero uncertainty) | 2×10⁻⁴ relative uncertainty |
| **Future-proof** | No TRM result can change the SI second | If TRM later claims CMB derivation, the anchor becomes circular |

**Cesium is the safe, non-circular, permanently valid choice.** It is the definition of the second itself — no TRM claim can retroactively invalidate it.

### The key insight

The SI second is **defined**, not derived. Using it as f_ref is analogous to using the meter to measure lengths — it's a convention, not a theory claim. TRM does not need to explain why the cesium frequency is 9.19 GHz; it just needs to anchor its dimensionless tick to a physical time unit. The cesium standard is the most defensible choice because:

1. It cannot become circular (TRM will never claim to derive atomic transition frequencies)
2. It is exact (defined, not measured)
3. It is universal (same everywhere)
4. It is the SI standard — the least arbitrary choice possible

---

## 5. Recommendation

### Primary: Cesium Hyperfine Frequency

```
f_ref = 9,192,631,770 Hz    (SI definition of the second)
```

### I3 Statement (Final Form)

> **I3:** One physical frequency scale f_ref is required to anchor the dimensionless CML tick ω_i = 1.0 to physical time units. The cesium hyperfine transition frequency (9,192,631,770 Hz, defining the SI second) is the recommended anchor — it is independent of all TRM claims, non-circular, exactly defined, and universal.

### What This Means

| Aspect | Status |
|:---|:---|
| G can be numerically predicted? | **Yes** — once δK, ρ_ref, and the energy-deficit mapping are fully calibrated |
| G is predicted from nothing? | **No** — f_ref is an empirical anchor, analogous to the SI second definition |
| Is this a weakness? | **No** — all of physics uses empirical unit definitions. The second is defined by cesium; the meter by c; the kilogram by h. TRM requiring one empirical anchor is standard, not exceptional. |

---

## 6. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_B3C_T2_FrequencyScaleAnchor.md` | Full candidate catalog |
| This document | Comparative ranking → selection |
| `TRM_V4_B3C_CoefficientMapping.md` | C4 energy-deficit mapping |
| `TRM_V4_B3B_BoundaryDefectOrigin.md` | 1/r origin |
| `TRM_V4_B3A_LaplaceUniqueness.md` | PDE uniqueness |
