# TRM V3.3 PhaseProxy Residual Diagnostics

**Status:** diagnostic candidate only  
**Scope:** SPARC residual diagnostics (RAR32–RAR47), strict no-refit train->transfer checks  
**Baseline model path:** unchanged

---

## Summary

RAR32-RAR47 indicate a robust residual structure organized by a **raw orbital phase-scale proxy** (`rawPhase = omega * radiusKpc`).

The strongest current guard result (RAR43) is:

- rawPhase delta: `0.017804`
- radiusOnly delta: `0.013962`
- best normalized phase delta: `0.015825`
- rawPhase extra over radius: `0.003843`
- rawPhase extra over normalized best: `0.001979`
- improved transfers (rawPhase): `20/20`
- train-transfer gap (rawPhase): `-0.000143`

---

## What this supports

- Residual structure is better organized by a raw phase-scale proxy than by radius-only bins.
- The raw phase proxy also outperforms tested normalized phase variants in this diagnostic series.
- Transfer behavior remains stable under the current cross-split discipline.
- RAR46 indicates the gain is structurally clustered (success/failure regimes differ in acceleration level, gas dominance, outer/inner ratio, and span).
- RAR47 argues against promoting a hard cluster gate for activation (diagnostic interpretation yes, hard activation no).

---

## Claim boundaries (review-safe)

- This is a **diagnostic candidate for radial-orbital synchronization structure**.
- This is **not** a theorem-level derivation and **not** a replacement of the baseline TRM-RAR law.
- No production activation is implied by these tests.
- RAR49–RAR53 indicate that global baryonic normalization must be audited before interpreting residual structures as new TRM physics.

Baseline TRM-RAR prediction path remains:

```
g_pred = g_bar + sqrt(g_bar * a0)
```

---

## Recommended next checks

1. Extend split robustness with additional deterministic partition schemes.
2. Repeat summary guard under controlled perturbations of binning and proxy preprocessing.
3. Keep all evaluations in strict train->transfer no-refit mode.
