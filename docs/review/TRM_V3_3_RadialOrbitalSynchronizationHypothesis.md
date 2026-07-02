# TRM V3.3 Radial-Orbital Synchronization Hypothesis

## 1. Scope

This note summarizes a **diagnostic candidate** interpretation of SPARC residual tests RAR32–RAR43.  
It does not introduce new production model logic and does not modify the baseline TRM-RAR prediction path.

## 2. Diagnostic motivation

Across RAR32–RAR43, residual organization was repeatedly tested with:

`rawPhase = omega * radiusKpc`

The core question was whether this phase-like proxy captures transfer-stable residual structure beyond simpler controls such as radius-only or normalized phase variants.

## 3. Candidate hypothesis

A **radial-orbital synchronization** diagnostic candidate:  
part of the remaining SPARC residual sector may follow a coupled radial-orbital phase scale that is not fully represented by radius-only bin structure.

## 4. What the tests support

RAR43 summary guard (no-refit transfer discipline) reports:

- rawPhase delta = `0.017804`
- radiusOnly delta = `0.013962`
- best normalized phase delta = `0.015825`
- rawPhase extra over radius = `0.003843`
- rawPhase extra over normalized best = `0.001979`
- improved transfers = `20/20`
- train-transfer gap = `-0.000143`

These diagnostics support a transfer-stable residual pattern where raw phase scale outperforms tested radius-only and normalized phase controls.

## 5. What they do NOT support

- They do **not** prove a time wave.
- They do **not** establish a GR replacement.
- They do **not** provide a theorem-level derivation.
- They do **not** justify baseline activation of new physics terms.

## 6. Claim boundaries

Baseline TRM-RAR law remains unchanged:

```
g_pred = g_bar + sqrt(g_bar * a0)
```

The phase-proxy line is retained strictly as a **diagnostic candidate** for residual structure analysis.

## 7. Recommended next checks

1. Extend robustness checks with additional deterministic and grouped split schemes.
2. Repeat summary guards with fixed pre-registered proxy/binner settings.
3. Add explicit null controls (proxy shuffles and label-preserving perturbations) for effect-size calibration.
4. Keep all evaluations in strict train->transfer no-refit mode.

---

Status: diagnostic candidate only; baseline TRM-RAR law unchanged.
