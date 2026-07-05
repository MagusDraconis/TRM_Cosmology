# docsV4 — V4 Interpretation Layer Workspace

**Created:** 2026-07-05
**Branch:** `feature/v4-interpretation-layer`

---

## ⚠️ Critical Rule

```
/docs   → V3.4 FINAL (FROZEN) — do not modify
/docsV4 → V4 interpretation layer — active workspace
```

**These directories must NEVER be mixed.** V3.4 core is immutable.

---

## Directory Structure

```
docsV4/
    README.md                          ← this file
    theory/
        TRM_V4_Interpretation_Core.md  ← central interpretation framework
        TRM_V4_TimeField_Mapping.md    ← T(x) = Ω*(x) detailed mapping (TBD)
        TRM_V4_Gravity_Model.md        ← effective gravity from ∇T(x) (TBD)
    review/
        TRM_V4_Claim_Boundaries.md     ← what V4 claims and does not claim
        TRM_V4_Risks.md                ← known risks and mitigations
    papers/
        TRM_V4_Abstract.md             ← paper abstract (TBD)
        TRM_V4_Draft_Paper.md          ← full paper draft (TBD)
    experiments/
        TRM_V4_MappingTests.md         ← test plan for interpretation candidates
```

---

## Naming Conventions

| Layer | Term | Example |
|:---|:---|:---|
| **V3.4** | "core", "theory", "scaffold" | "the core oscillator model" |
| **V4** | "interpretation", "mapping", "candidate" | "candidate mapping C1: classical gravity" |

### Forbidden Terms in V4 Documents

- "final theory"
- "replacement of GR"
- "derived" (use "hypothesized" or "mapped")
- "predicted" (use "expected under this interpretation")
- "proven" (use "consistent with")

---

## V4 Design Principle

> **V4 is nicht mehr Theorieentwicklung — sondern Bedeutungsentwicklung.**
> V4 is no longer theory development — it is meaning development.

```
"Does this modify the core?"
→ YES → forbidden
→ NO  → allowed
```
