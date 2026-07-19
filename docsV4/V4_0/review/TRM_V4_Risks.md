# TRM V4 — Risk Register

**Date:** 2026-07-05

---

## Risk 1 — Scale Mismatch (Classical Gravity)

| Attribute | Detail |
|:---|:---|
| **Risk** | The classical gravity mapping φ = −GM/(c²r) produces galactic φ ~ 5×10⁻⁶ — 34,000× too small for the bridge band [0.16, 0.19] |
| **Impact** | C1 candidate fails Check 3 (I2 compatibility) for any non-compact-object system |
| **Mitigation** | Explore alternative φ(x) forms (C2 exponential, C3 perturbation, C4 medium) |
| **Status** | KNOWN — documented in I2_Calibration_or_Axiom.md Route A |

---

## Risk 2 — No External φ Calibration

| Attribute | Detail |
|:---|:---|
| **Risk** | No non-circular external anchor exists for φ = 0.17 (all 5 routes blocked: I2_Calibration_or_Axiom.md) |
| **Impact** | V4 interpretation cannot claim φ is independently measurable |
| **Mitigation** | Accept φ as a domain-specific prior; focus on internal consistency rather than external derivation |
| **Status** | KNOWN — irreducible under current infrastructure |

---

## Risk 3 — Core Modification Creep

| Attribute | Detail |
|:---|:---|
| **Risk** | V4 interpretations inadvertently modify core oscillator dynamics, I1, I2, or E1 interpretation |
| **Impact** | Violates V4 design principle; invalidates the interpretation layer |
| **Mitigation** | Every V4 document must include explicit "Does this modify the core?" self-audit |
| **Status** | MITIGATED — design principle documented in TRM_V4_Interpretation_Core.md |

---

## Risk 4 — Over-interpretation

| Attribute | Detail |
|:---|:---|
| **Risk** | V4 interpretations are presented as "the physical truth" rather than "a candidate mapping" |
| **Impact** | Misleading to readers; conflates interpretation with derivation |
| **Mitigation** | All V4 documents use hypothesis language ("candidate", "proposal", "hypothesis"); never "proven", "derived", "predicted" |
| **Status** | MITIGATED — claim boundaries document enforces language discipline |

---

## Risk 5 — Documentation Drift

| Attribute | Detail |
|:---|:---|
| **Risk** | V4 documents slowly begin referencing or modifying `docs/` (V3.4 territory) |
| **Impact** | Blurs the frozen/active boundary; creates confusion about what is core vs. interpretation |
| **Mitigation** | Strict directory separation: `docs/` = FROZEN, `docsV4/` = ACTIVE. Cross-references must be read-only. |
| **Status** | MITIGATED — enforced by directory structure |
