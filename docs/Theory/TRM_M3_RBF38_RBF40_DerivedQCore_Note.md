# TRM M3 RBF38–RBF40 Derived Q-Core Note

## Scope

This note summarizes the RBF38–RBF40 diagnostics on whether the bridge-core q-window can be structurally derived rather than chosen operationally.

---

## RBF38: structural qCore derivation from rational-band geometry

RBF38 derives the m=3 bridge-core support directly from:

```text
Omega = (q + m) / q,  with m = 3
```

under the bridge-band constraints:

```text
Omega in [1.16, 1.19]
gamma = 1 / Omega in [0.84, 0.86]
```

Result:

```text
qCore(m=3) = [16, 17, 18]
```

Interpretation:

> In the tested domain, bridge-core q-support is obtained from a structural band rule, not from post-hoc q-window tuning.

---

## RBF39: derived qCore selection behavior

RBF39 evaluates m=1..5 using only the derived qCore support (no manual q-window choice) under one shared derived three-constraint rule.

Result:

> The derived qCore configuration selects m=3 in the baseline diagnostic case.

Interpretation:

> This supports the path that m=3 admissibility can be recovered from derived bridge-core support without manual q-window selection.

---

## RBF40: fallback regime separation

RBF40 compares core, low-core, and no-core windows and classifies m=2 fallback causes.

Result:

> m=2 fallback separates into:
> 1. phase/action-boundary stress regimes, and
> 2. bridge-core-geometry weakened/missing support regimes.

Interpretation:

> Fallback behavior is diagnosable by structural regime class, not treated as a single undifferentiated failure mode.

---

## Updated status

Current reviewer-safe status:

> action-derived bounded three-constraint bridge-mode candidate with structurally derived bridge-core q-window.

---

## Claim boundaries

- diagnostic/candidate only
- no theorem-level proof
- no universal m=3 selection
- no first-principles closure claim
- no GR replacement claim

---

## Continuation

Follow-up bridge-independence diagnostics after this note are documented in:

`docs/Theory/TRM_M3_RBF44_RBF46_BridgeConstraint_Independence_Note.md`
