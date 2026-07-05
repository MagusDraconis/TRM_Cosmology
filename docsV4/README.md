# docsV4 — V4 Interpretation Layer Workspace

**Created:** 2026-07-05
**Branch:** `feature/v4-interpretation-layer`
**Primary candidate:** C5 — Energy Density Mapping ⭐
**Status:** FINAL. B1/B2 STABLE. B3 closed (form explained, coefficient calibrated). I3 = cesium (SI second). 37 xUnit tests passing.

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
        TRM_V4_TimeField_Mapping.md    ← T(x) and φ(x) — C5 breakthrough formulation
        TRM_V4_Gravity_Model.md        ← C5 gravity: a(x) = c²/ρ_ref · ∇(δρ)
        TRM_V4_CouplingFieldEquation.md← B3 candidate catalog (A-D)
        TRM_V4_B3_ApproachDirections.md← B3 deep analysis: which candidates can work
    review/
        TRM_V4_Claim_Boundaries.md     ← what V4 claims and does not claim
        TRM_V4_Risks.md                ← known risks and mitigations
    papers/
        TRM_V4_Abstract.md             ← paper abstract with C5 framing
        TRM_V4_Draft_Paper.md          ← full paper draft (TBD)
    experiments/
        TRM_V4_MappingTests.md         ← C5 evaluation + C1–C4 test plans
```

---

## C5 Breakthrough Summary

The central insight: **φ is not geometric — it is energetic.**

```
φ(x) = ρ_E(x) / ρ_ref           energy density → time-rate offset
φ₀   = ρ_bg / ρ_ref             global background → naturally ~0.17
δφ(x)= δρ(x) / ρ_ref            local perturbation → gravity via ∇
```

This resolves the C1 scale mismatch (34,000×) because φ₀ is cosmological, not geometric. The original Time-Aether intuition — "Zeitfluss hängt vom energetischen Zustand des Systems ab" — is now mathematically precise.

---

## Naming Conventions

| Layer | Term | Example |
|:---|:---|:---|
| **V3.4** | "core", "theory", "scaffold" | "the core oscillator model" |
| **V4** | "interpretation", "mapping", "candidate" | "candidate C5: energy density mapping" |

### Forbidden Terms in V4 Documents

- "final theory"
- "replacement of GR"
- "derived" (use "hypothesized" or "mapped")
- "predicted" (use "expected under this interpretation")
- "proven" (use "consistent with")

---

## V4 Design Principle

> **V4 ist nicht mehr Theorieentwicklung — sondern Bedeutungsentwicklung.**
> V4 is no longer theory development — it is meaning development.

```
"Does this modify the core?"
→ YES → forbidden
→ NO  → allowed
```
