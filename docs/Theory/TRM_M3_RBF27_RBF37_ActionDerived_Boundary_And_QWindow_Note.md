# TRM M3 RBF27–RBF37 Action-Derived Boundary and Q-Window Note

## Scope

This note summarizes the current m=3 status after RBF27–RBF37 with emphasis on boundary behavior and q-window dependence.

It is reviewer-safe by construction and stays at diagnostic/candidate level.

---

## RBF27–RBF31 summary

- RBF27: derived action/tick and operational action/tick show strong structural agreement.
- RBF28: with shared derived full-stack rules, baseline selection remains `m=3`.
- RBF29: boundary mapping shows bounded admissibility and stress-sensitive transitions.
- RBF30: under shared derived rules, baseline admissibility excludes competing modes.
- RBF31: `m=2` fallback appears as a phase/action boundary tradeoff, not random instability.

Interpretation:

> m=3 remains supported as a bounded candidate under a shared phase+bridge+derived-action rule.

---

## RBF32–RBF34 summary

- RBF32: admissibility shows local continuity near baseline while remaining globally multi-component.
- RBF33: stronger shared action-margin gating can tighten fallback behavior, but clean exclusion of all fallback cases risks threshold-tuning sensitivity.
- RBF34: under tested solver-step variants, boundary behavior remains stable in bounded ranges, while q-window shifts can induce meaningful region drift.

Interpretation:

> boundary structure is bounded and non-trivial; stability is local/bounded rather than universal.

---

## RBF35–RBF37 summary

- RBF35: q-window dependence is largely explainable by bridge-band occupancy geometry.
- RBF36: q-window shifts do not force per-family retuning artifacts in the tested shared-rule setup.
- RBF37: m=3 support is strongest when q-window contains bridge-core support, consistent with a bridge-scale-linked window rationale rather than purely post-hoc choice.

Interpretation:

> the dominant remaining sensitivity is now the structural q-window/bridge-core relation.

---

## Updated status statement

Current reviewer-safe status:

> action-derived bounded three-constraint bridge-mode candidate.

This is stronger than purely operational selection, but still below theorem-level closure.

---

## Main remaining gap: derive bridge-core q-window structurally

The main open gap is no longer primarily action/tick construction quality; it is the structural derivation of the q-window from bridge-scale geometry and lattice/synchronization assumptions.

Target gap question:

```text
Why this q-window?
```

Required upgrade direction:

```text
bridge-scale + lattice assumptions
→ bridge-core support set
→ q-window admissibility rule (shared, non-post-hoc)
```

---

## Claim boundaries

- diagnostic/candidate/tested-effective only
- not theorem-level proof
- not first-principles closure
- not universal `m=3` selection
- not GR replacement
- no numerology claim

---

## Next tests: RBF38+

1. **RBF38**: derive bridge-core q-support from an explicit bridge-scale structural rule and compare to current q-window heuristics.
2. **RBF39**: cross-window transfer with one frozen derived-action rule and one frozen bridge-core rule (no per-family retuning).
3. **RBF40**: boundary manifold persistence under denser q-resolution and phase-stress grids with explicit failure-cause accounting.
4. **RBF41**: structural penalty test for alternative q-window choices that match fit quality only via post-hoc threshold shifts.
5. **RBF42**: consolidated bounded-domain report linking selection region, fallback region, and bridge-core geometry in one shared diagnostic artifact.
