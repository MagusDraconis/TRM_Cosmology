# TRM V4 — Time-Field Mapping

**Date:** 2026-07-05
**Status:** Placeholder — detailed mapping analysis TBD

---

## 1. T(x) Definition

```
T(x) := Ω*(x)
```

The local time-rate field is the interpretation of the emergent collective frequency as a spatially dependent quantity. In V3.4, Ω* is a single global value per simulation run — the interpretation step maps "different runs at different φ" to "different spatial positions x with different T(x)".

---

## 2. φ(x) Candidates (Summary)

| # | Form | Physical Motivation | Domain |
|:---|:---|:---|:---|
| C1 | −GM/(c²r) | Classical gravitational potential | Compact objects (r ~ 3r_s) |
| C2 | φ₀·exp(−r/r₀) | Yukawa-like decay from source | TBD |
| C3 | φ₀ + δφ(x) | Global baseline + local perturbation | Any |
| C4 | Intrinsic medium state | BB11 buoyancy interpretation | System-internal |

Detailed evaluation in `docsV4/experiments/TRM_V4_MappingTests.md`.

---

## 3. Effective Gravity

```
a(x) = c² · ∇T(x) = c² · ∇φ(x)
```

For C1: reproduces Newtonian a = GM/r² exactly (formal identity).
For C2–C4: gradient behavior depends on functional form — to be computed.

---

## 4. Open Questions

- Can φ be measured independently in any physical system?
- Does the time-rate gradient produce testable deviations from Newton/Einstein gravity?
- Is there a regime where C2 or C3 naturally produces φ ∈ [0.16, 0.19] at a physically relevant scale?
- Can the BB22/BB23 gauge-fixing argument be reconciled with the clock-bias interpretation?
