# BB19 — Model Implications of a Perfectly Global Lapse B(t)

---

## Context

BB17 established that only non-global components of B(t) are
experimentally constrained. BB18 showed that a perfectly global
B(t) is operationally equivalent to a reparameterization of time.
SPARC analysis found no universal galaxy-scale physical effect
requiring B(t).

---

## Goal

Formalize the role of B(t) after CML, empirical tests, and
interpretation.

---

## Key Inputs

| Source | Result |
|--------|--------|
| BB17   | Only non-global parts are constrained |
| BB18   | Perfectly global B(t) is equivalent to time reparameterization |
| SPARC  | No universal physical effect requires B(t) |

---

## Core Statement

A perfectly global B(t):

- introduces **no observable degrees of freedom**
- is **not required** by data
- is **removable** by time redefinition

---

## Implication

The term

```
    dθ/dt = ω + B(t) + coupling
```

is equivalent to

```
    dθ/dt' = ω + coupling
```

under the time reparameterization `t' = t + ∫ B(t) dt`.

---

## Decision

**B(t) should not be treated as a physical variable in TRM/TQM.**

---

## Remaining Physics

Only non-global terms

```
    B_i(t) ≠ B_j(t)
```

would be physical.

But:

- constrained by PHARAO (|slopeDifferenceFraction| < 1.0×10⁻⁴ at 5σ)
- not seen in SPARC (no universal offset, lambda ≈ 1)
- not seen in UTCr (no robust common-mode drift)

---

## Final Conclusion

**B(t) is a redundant global parameter and not part of the
observable physics of the system.**

---

## Status

| Property          | Value      |
|-------------------|------------|
| Mechanism         | CLOSED     |
| Empirical necessity | NONE    |
| Physical role     | REDUNDANT  |

---

## Next Step

**BB20 — Reduced model without B(t)**

Strip B(t) from the core dynamics and verify that all CML
predictions are preserved.
