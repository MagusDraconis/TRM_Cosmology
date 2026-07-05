# TRM V4 — Mapping Test Plan

**Date:** 2026-07-05
**Status:** Test plan — no implementations yet

---

## 1. Test Framework

Each V4 interpretation candidate must be evaluated against 3 consistency checks:

```
Check 1 (E1):  Ω* globally constant — no q-dependence introduced
Check 2 (I1):  Ω* = 1 + m/q rational consistency holds
Check 3 (I2):  φ(x) ∈ [0.16, 0.19] for the target system
```

---

## 2. Candidate C1 — Classical Gravity

### Test C1.1 — Scale Verification

| Parameter | Value |
|:---|:---|
| Mapping | φ(r) = −G·M/(c²·r) |
| Test case: Solar mass at r = 3r_s | φ ≈ 0.167 → PASS (in bridge band) |
| Test case: Solar mass at r = 1 AU | φ ≈ 10⁻⁸ → FAIL (not in bridge band) |
| Test case: Galactic mass at r = 10 kpc | φ ≈ 5×10⁻⁶ → FAIL (not in bridge band) |

### Test C1.2 — Gradient Check

```
a(r) = c² · ∇φ(r) = G·M/r²   (Newtonian)
```

Newtonian scaling reproduced exactly — but only in the compact-object regime where φ is physically valid.

### Test C1.3 — Consistency

| Check | Result |
|:---|:---|
| Check 1 (E1) | PASS — Ω* set by φ(r), constant per spatial point |
| Check 2 (I1) | PASS — rational representation holds |
| Check 3 (I2) | **FAIL at all non-compact scales** |

**Classification: PARTIAL** — works formally but domain-limited to compact objects (r ~ 3r_s, not galactic).

---

## 3. Candidate C2 — Exponential Decay

### Test Plan

| Parameter | Value |
|:---|:---|
| Mapping | φ(r) = φ₀·exp(−r/r₀) |
| Free parameters | φ₀ (amplitude), r₀ (decay length) |
| Target | Reproduce φ ∈ [0.16, 0.19] at some physical scale |

### Consistency

| Check | Status |
|:---|:---|
| Check 1 (E1) | TBD |
| Check 2 (I1) | TBD |
| Check 3 (I2) | TBD |

**Status: NOT EVALUATED**

---

## 4. Candidate C3 — Constant + Perturbation

### Test Plan

| Parameter | Value |
|:---|:---|
| Mapping | φ(x) = φ₀ + δφ(x) with δφ ≪ φ₀ |
| Idea | φ₀ = 0.17 provides the bridge-band baseline; δφ(x) encodes local structure |
| Gradient | a(x) = c²·∇(δφ(x)) → local gravity from perturbation only |

### Consistency

| Check | Status |
|:---|:---|
| Check 1 (E1) | TBD |
| Check 2 (I1) | TBD |
| Check 3 (I2) | TBD |

**Status: NOT EVALUATED**

---

## 5. Candidate C4 — Medium/Buoyancy (BB11)

### Test Plan

| Parameter | Value |
|:---|:---|
| Mapping | φ as intrinsic collective medium state of the oscillator system |
| Idea | φ = 0.17 is the system's "density" or "memory integral" — no external field needed |

### Consistency

| Check | Status |
|:---|:---|
| Check 1 (E1) | TBD |
| Check 2 (I1) | TBD |
| Check 3 (I2) | TBD |

**Status: NOT EVALUATED** — reinterprets I2 rather than calibrates it.

---

## 6. Summary

| Candidate | Check 1 (E1) | Check 2 (I1) | Check 3 (I2) | Classification |
|:---|:---|:---|:---|:---|
| C1 — Classical gravity | PASS | PASS | FAIL (non-compact) | **PARTIAL** |
| C2 — Exponential decay | TBD | TBD | TBD | NOT EVALUATED |
| C3 — Constant + perturbation | TBD | TBD | TBD | NOT EVALUATED |
| C4 — Medium/buoyancy | TBD | TBD | TBD | NOT EVALUATED |
