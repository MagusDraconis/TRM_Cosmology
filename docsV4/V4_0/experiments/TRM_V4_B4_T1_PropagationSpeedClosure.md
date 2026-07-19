# TRM V4 — B4-T1: Propagation Speed Closure

**Date:** 2026-07-05
**Status:** Evaluating whether c_K can be expressed in TRM-native terms
**Predecessors:** B4 (wave equation selected), B3C-T3 (I3 = cesium)

---

## 1. The Question

B4 selected the massless wave equation □K = 0 as the dynamic extension. This introduces one new parameter: c_K, the coupling-field propagation speed.

Can c_K be expressed in terms of existing TRM parameters (K₀, f_ref, coupling topology) or does it require a new irreducible input (I4)?

---

## 2. Candidate Relations

### Candidate A — Identify with Speed of Light

```
c_K = c = 2.99792458×10⁸ m/s
```

**TRM expression:** None. This is an identification with a known physical constant.

**What it gives:** Immediate GR-consistency. Gravitational waves in TRM propagate at c — matching LIGO observations.

**What it costs:** One assumption (c_K = c). Not derived from TRM.

**Classification: ASSUMED.** Matches all observations but not derived from oscillator dynamics.

---

### Candidate B — Oscillator Coupling Timescale

```
c_K = K₀(CML) · Δx · f_ref
```

Where:
- K₀(CML) = 0.10 (dimensionless coupling strength)
- Δx = oscillator spacing (meters)
- f_ref = 9.192631770×10⁹ Hz (I3, cesium)

**Derivation:**

In the discrete oscillator network, a coupling perturbation at site i propagates to neighbor i+1 on the coupling timescale:
```
τ_coupling = 1 / (K₀ · f_ref)     (physical seconds)
```
The propagation speed is:
```
c_K = Δx / τ_coupling = K₀ · Δx · f_ref
```

**TRM expression:** c_K is expressed in terms of K₀ (TRM-native), f_ref (I3), and Δx (spatial embedding).

**What it gives:** A relation connecting c_K to Δx. If Δx is independently known, c_K is predicted. If c_K is identified with c, Δx is predicted (≈ 33 cm).

**What it costs:** Introduces Δx as a new spatial parameter (I4 candidate).

**Numerical check:**

| Assuming c_K = c | Assuming Δx = 1 m |
|:---|:---|
| Δx = c/(K₀·f_ref) ≈ 0.33 m | c_K = 0.10·1·9.19×10⁹ ≈ 9.2×10⁸ m/s ≈ 3.1c |

**Classification: CALIBRATED.** Requires either Δx (spatial embedding) or c_K identification. Two parameters related by one equation → one degree of freedom remains.

---

### Candidate C — Direct CML Front Velocity Measurement

```
c_K(CML) = measured front propagation speed in CML simulation
```

**Approach:** Introduce a coupling defect, measure how fast the phase perturbation propagates through the ring.

**Expected CML value:** c_K(CML) ≈ K₀ = 0.10 (in sites per CML time unit)

This is because the Kuramoto coupling term K₀·sin(Δθ) transfers phase information between neighbors at rate ~K₀.

**Physical expression:**
```
c_K = c_K(CML) · Δx · f_ref
```

**TRM expression:** c_K(CML) is directly measurable from CML simulation. No additional assumptions beyond the oscillator model.

**What it gives:** The dimensionless propagation speed c_K(CML) is a TRM-native prediction.

**What it costs:** Still requires Δx and f_ref for physical units.

**Classification: DERIVABLE (dimensionless c_K). CALIBRATED (physical c_K requires Δx).**

---

### Candidate D — Topology-Dependent Velocity

```
c_K = f(topology) · K₀ · Δx · f_ref
```

Where f(topology) depends on the coupling graph structure:
- Ring (1D): f = 1
- Square lattice (2D): f = √2
- Cubic lattice (3D): f = √3
- All-to-all: f = 0 (instantaneous — infinite speed)

**Problem:** f(topology) is a geometric factor, not a new physical parameter. But the topology itself is model input.

**Classification: DERIVABLE (from topology).** Same Δx dependence as Candidate B.

---

## 3. The Δx Problem

All TRM-native expressions for c_K contain Δx — the oscillator spacing. This is the spatial embedding parameter: how far apart are the oscillators in physical space?

| Question | Answer |
|:---|:---|
| Is Δx a TRM-native quantity? | **No.** The CML ring has no intrinsic spatial scale. The coupling K_ij encodes adjacency but not physical distance. |
| Can Δx be derived from TRM? | **No.** Spatial distance is not defined in the oscillator model. |
| Is Δx a new irreducible input? | **Yes.** It is the spatial analogue of f_ref (I3). |

---

## 4. The I4 Proposal

If c_K cannot be expressed purely in TRM terms without Δx, and Δx cannot be derived from TRM, then:

> **I4: One spatial length scale Δx is required to anchor the dimensionless oscillator spacing to physical distance.**

Together with I3 (f_ref), this gives:
```
Time scale:  f_ref (I3, cesium)
Space scale:  Δx    (I4, oscillator spacing)
```

From I3 + I4 + K₀, c_K is DERIVED:
```
c_K = K₀ · Δx · f_ref
```

This is a clean result: **2 empirical anchors (time + space) → propagation speed derived.**

---

## 5. Alternative: c_K = c (No I4)

If we identify c_K = c (Candidate A), then Δx is predicted:
```
Δx = c / (K₀ · f_ref) ≈ 0.33 m
```

This eliminates I4 at the cost of one assumption (c_K = c). The assumption is empirically well-supported (LIGO: gravitational waves propagate at c to ~10⁻¹⁵) but is not derived from TRM.

| Approach | Irreducible inputs | Derivation status |
|:---|:---|:---|
| **c_K = c (assumed)** | I1, I2, I3 (f_ref), D1 | c_K assumed, Δx derived |
| **I4 = Δx (spatial anchor)** | I1, I2, I3 (f_ref), I4 (Δx), D1 | c_K derived from I3+I4+K₀ |

---

## 6. Recommendation

### Primary: c_K = c (Candidate A)

**Justification:**
1. Empirically supported — gravitational waves propagate at c
2. Matches GR in the weak-field limit
3. Eliminates I4 (Δx is derived, not assumed)
4. One fewer irreducible input

**Cost:** One assumption (c_K = c). Not derived from oscillator dynamics.

### Fallback: I4 = Δx (Candidate B)

If c_K ≠ c is ever measured (deviation from GR), the I4 framework absorbs this naturally:
- c_K predicted from I3+I4+K₀
- c_K ≠ c implies Δx ≠ c/(K₀·f_ref)
- Testable: measure c_K from gravitational wave speed, predict Δx

---

## 7. Updated B4 Status

```
B4 DYNAMIC STATUS:

  DERIVED:
    ✅ Wave equation is the unique causal hyperbolic extension
    ✅ Static limit → ∇²K = 0
    ✅ c_K(CML) = K₀ (dimensionless, from coupling timescale)

  ASSUMED:
    ⬜ c_K = c (matches GR and LIGO, not derived from TRM)

  ALTERNATIVE (if c_K ≠ c):
    ⬜ I4 = Δx (spatial embedding parameter)
    → c_K = K₀ · Δx · f_ref (derived from I3+I4+K₀)

  CLASSIFICATION:
    With c_K = c:  4 inputs (I1, I2, I3, D1) + 1 assumption (c_K=c)
    With I4 = Δx:   5 inputs (I1, I2, I3, I4, D1), c_K derived
```

---

## 8. Cross-Reference

| Document | Role |
|:---|:---|
| `TRM_V4_B4_DynamicCouplingField.md` | Wave equation selection |
| `TRM_V4_B3C_T3_AnchorSelection.md` | I3 = cesium |
| `TRM_V4_Final_Status.md` | Static closure summary |
| This document | c_K closure analysis |
