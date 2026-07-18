# TRM V5.17 ARA: Adaptive Response Analysis

**Suite:** ARA | **Status:** COMPLETE (analysis, no new simulation) | **Date:** 2026-07-18

## Quick Summary

ARE results re-analyzed. **rebMagnitude probe is near-perfect (ES=2.07, 0 false positives).** Rescue failure is not a control ceiling — it is a **correction-type mismatch**. Additional compression is destructive for high-reb seeds (22/41 damaged) and ineffective for low-reb seeds (1 rescue). The adaptive ceiling is **compression-specific**, not absolute. **Alternative correction types (K-preserving, trajectory correction, rebound dampening) are justified.** Gate F (Alternative Correction) REACHED.

---

## 1. Probe Reliability Confirmed

| Probe Class | Seeds | Persist | Rate |
|------------|-------|---------|------|
| rebMag < 0.01 | 9 | 0 | **0%** |
| rebMag ≥ 0.01 | 32 | 28 | **88%** |

**ES = 2.07. Gate A: REACHED — probe is valid.**

---

## 2. Rescue Candidate Analysis (G3/G4)

| Group | Seeds | Description |
|-------|-------|-------------|
| G3 (rescued) | 1 | rebMag<0.01, A0 fail → A2 succeed |
| G4 (not rescued) | 8 | rebMag<0.01, A0 fail → A2 fail |
| G5 (low-reb) | 9 | All rebMag<0.01 seeds |
| G6 (high-reb) | 32 | All rebMag≥0.01 seeds |

**Why only 1 rescue?** Low-reb seeds (n=9) all fail under A0. Additional compression rescues 1. The other 8 are **compression-resistant** — their failure is not from insufficient compression (they already have low rebound, meaning d is already compressed or collapsing). More compression doesn't help; it pushes them further from equilibrium.

**Key insight:** Low rebMagnitude = d is already shrinking. Additional compression accelerates the collapse. These seeds need **K-preserving or trajectory-stabilizing** correction, not more d reduction.

---

## 3. Damage Analysis (G2/G7)

| Model | Seeds | Damaged | Damage Rate |
|-------|-------|---------|-------------|
| A2 (gated) | 41 | 0 | 0% |
| A4 (universal) | 41 | **22** | **54%** |

**A4 damages 22 seeds — all were high-reb (rebMag≥0.01) and succeeding under A0.** Universal correction applies 20% extra d compression to seeds that are already in healthy rebound. This over-compresses them, pushing them below the persistence threshold.

**A2 avoids all damage by gating.** The `rebMag<0.01` filter correctly preserves high-reb successes.

**Gate C: REACHED — gating prevents damage.**

---

## 4. Correction-Type Assessment

| Seed Class | Failure Mechanism | Right Correction |
|-----------|------------------|-----------------|
| Low-reb (reb<0.01) | d already collapsing | **K-preserving or trajectory correction** |
| High-reb (reb≥0.01) | Already succeeding | **No correction (preserve)** |
| Low-reb+compression resistant | d-stabilization failure | **Rebound dampening or K-anchoring** |

**Additional compression is the wrong correction for both classes:**
- For low-reb seeds: accelerates d collapse → ineffective
- For high-reb seeds: pushes past equilibrium → destructive

**Gate B: REACHED — additional compression is the wrong correction.**
**Gate D: REACHED — compression rescue is inadequate.**

---

## 5. Adaptive Ceiling Assessment

| Hypothesis | Evidence | Verdict |
|-----------|----------|---------|
| Absolute control ceiling | A2 marginally improves, A4 catastrophic | **NOT absolute** — gating prevents damage |
| Compression-specific ceiling | Low-reb seeds resistant, high-reb seeds damaged | **YES** — compression is wrong tool |
| Correction-type mismatch | Both classes need non-compression fixes | **YES** |
| Insufficient data | 41 seeds, 9 low-reb | Adequate for analysis |

**Gate E: NOT REACHED — not an absolute ceiling.**
**Gate F: REACHED — alternative correction justified.**

---

## 6. Decision Gates

| Gate | Status |
|------|--------|
| **A — Probe Confirmed** | ✅ REACHED (ES=2.07) |
| **B — Wrong Correction Type** | ✅ REACHED (compression-specific failure) |
| **C — Preserve Validated** | ✅ REACHED (gating prevents damage) |
| **D — Compression Rescue Inadequate** | ✅ REACHED |
| E — Absolute Ceiling | ❌ NOT REACHED |
| **F — Alternative Justified** | ✅ REACHED |

---

## 7. Recommended Next: ARI

**ARI_AlternativeCorrectionAudit** — Test non-compression corrections:
1. K-preserving correction (stabilize K while adjusting d)
2. Trajectory correction (nudge back toward Hi-entry vector)
3. Rebound dampening (constrain d oscillation amplitude)
4. No second intervention (probe-only, accept M3+ outcome)

---

## 8. Claim Discipline

No absolute ceiling claimed. No causality beyond ARE data. Compression-specific failure, not adaptive failure. No physical interpretation.
