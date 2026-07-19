# TRM V5.12 Final Synthesis: High-Basin Entry Conditions

**Branch:** feature/v5.12-high-basin-entry-conditions
**Status:** COMPLETE
**Date:** 2026-07-17

---

## 1. Branch Metadata

| Field | Value |
|-------|-------|
| Branch | feature/v5.12-high-basin-entry-conditions |
| Base | V5.11 COMPLETE |
| Suites | 13 (HBP, HBE, HBA, HBI, HBC, HBD, HBEQ, HBF, HBG, HBH, HBJ, HBK, HBL) |
| Tests Added | 28 (3 protocol + 25 analysis) |
| V5.12 Total | 2565 passed / 28 V5.12-specific |
| Cumulative | **2565 tests passed, 0 failed** |
| Key Commits | ba9dcd2, 81deba5, c05d477, 492ad73, c6995bd, f9b8053, ed25a58, 139eff0, 8f22ffc |

---

## 2. Research Question

**What conditions are necessary for genuine Natural High-basin entry?**

Do NOT ask: Can Omega be pushed above threshold? Can transient High states be created?

Ask: What distinguishes Natural High-basin states from threshold-crossing induced states?

---

## 3. Suite Summaries

### HBP — High-Basin Entry Protocol
Pre-registered the experimental protocol, state classes, metrics, and decision gates for V5.12.

### HBE — High-Basin Entry Execution
Natural High and Natural Low states have a **separable entry signature** based on normalized d_mean, K_mean, and K_std. Cohen's d = 1.055 at N=71 (moderate-to-strong separation).

### HBA — High-Basin Entry Analysis
**Transient induced high states are NOT Natural High-basin states.** They are geometrically farther from the High basin than Natural Low. Suppressed High becomes Low-like, confirming High→Low basin access.

### HBI — High-Basin Boundary and Entry Vector Audit
**Natural High entry vector identified**: lower d_mean (-85% contribution), higher K_mean (+45%), narrower K_std (-26%). **K-graft is anti-aligned** with the true High-entry vector — it pushes states AWAY from the Hi basin.

### HBC — High Entry Vector Intervention Audit
Before-Cupd d-compression is the best geometric entry direction. 25% d-compression moves 23/30 states closer to Hi centroid. But **strict persistence remains 0/30**. Coordinated d+K produces immediate Hi but anti-aligned projection (Omega crossing inverse to basin proximity).

### HBD — High-Basin Persistence Barrier Audit
**d-Rebound mechanism discovered**: Hi-proximal induced states are over-compressed (d=0.28 vs NatHi d=0.62). During continuation, d rebounds +0.29, K collapses -0.13. Natural Hi states do the OPPOSITE (d decreases -0.23, K increases +0.10). Entry score is insufficient.

### HBEQ — High-Basin Equilibrium Band Audit
Natural Hi d_mean band at CP5: p25-p75 = [0.49, 0.84]. **Rebound reversal point at dT1≈0.49**: below → rebound, above → Natural Hi drift. Band targeting (p50-p90) produces first strict persistence from before-Cupd (3/30), beating centroid targeting (0/30).

### HBF — High-Basin Band Refinement Audit
Fine dT1 sweep (0.32-0.66): no universal target band found. Strict persistence is sparse (max 1/target) and seed-specific. Cross-N: N=71/72 work, N=67 fails. All strict persistent seeds land at dT1≈0.23-0.28 regardless of target — the **intervention cannot guide seeds to the intended band.**

### HBG — Seed-Intrinsic Persistence Audit
**Counter-intuitive finding**: the single strict persistent seed (36) has the most EXTREME Lo-like baseline (d=0.70 vs NatHi=0.30). The most Hi-like Lo seed (39) fails strict persistence. Relaxation direction does NOT predict persistence — failed seeds relax NatHi-like while persistent seeds relax anti-Hi.

### HBH — Compression Room and Relative Displacement Audit
**Compression room is the strongest persistence predictor**: strict persistent seeds have higher d0 (0.62 vs 0.54), lower dT1 (0.26 vs 0.65), larger Δd_abs (-0.36 vs +0.11), larger Δd_rel (-0.51 vs +0.52). 4/5 persistent seeds have d0>0.50 and ΔdRel<-0.49.

### HBJ — Relative Compression Control Audit
**Two candidate persistence pathways discovered:**
- **Pathway 1 (Compression-Room)**: high d0 seeds, 50% compression, ΔdRel<-0.30 threshold, 14% rate in G1
- **Pathway 2 (Crypto-Hi Mild)**: seed 57 at 10% compression, ΔdRel=-0.06 — anomalous second pathway suggested
- Crypto-Hi seeds produce ZERO strict persistence under strong compression

### HBK — Persistence Pathway Validation Audit
**Both pathways validated with multiple seeds:**
- Pathway 1: 3 strict from 21 G1 seeds at 50%
- **Pathway 2: 2 strict from 22 G3 seeds at 10-15% (seeds 29 AND 57)** — Seed 57 is NOT an outlier
- Wrong protocol kills persistence (strong on G3 → 0 strict)
- Cross-N: both pathways work at N=71/72, fail at N=67

### HBL — Pathway Sufficiency and Selection Audit
**PROSPECTIVE validation of two-pathway model:**

| Protocol | Strict Rate (N=71) |
|----------|-------------------|
| **MATCHED** (P1→50%, P2→10%) | **10.0%** |
| UNIV Strong (all→50%) | 6.0% |
| UNIV Mild (all→10%) | 2.0% |
| MISMATCHED (P1→10%, P2→50%) | **0.0%** |

Cross-N: N=71 (9%), N=72 (12%), N=67 (0%).

---

## 4. Supported Findings

1. **Threshold crossing (Omega > 1.783) is NOT High-basin entry.** (HBA, HBD)
2. **K-graft is anti-aligned** with the true High-entry vector. (HBI)
3. Natural High states are **lower-d, higher-K, narrower-K_std** relative to Natural Low. (HBI, HBEQ)
4. **Before-Cupd d-compression** is the correct geometric entry direction. (HBC)
5. **Over-compression causes d-rebound and K-collapse.** (HBD)
6. **Natural High equilibrium band** matters — centroid targeting over-compresses. (HBEQ, HBF)
7. Baseline High proximity alone is NOT sufficient for persistence. (HBG)
8. **Compression room** (baseline d0) is the strongest persistence predictor found. (HBH, HBJ)
9. **Two candidate persistence pathways exist and are prospectively validated:**
   - Pathway 1 (Compression-Room): high-room seeds + 50% compression
   - Pathway 2 (Crypto-Hi Mild): crypto-Hi seeds + 10-15% compression (seeds 29, 57)
10. **Pathway matching matters** — mismatched protocol = 0% strict. (HBK, HBL)
11. **N=67 remains inaccessible** in tested pathway model. (HBF, HBK, HBL)
12. **N=71 and N=72 show pathway activity** (transition window). (HBJ, HBK, HBL)

---

## 5. The Two-Pathway Model

### Pathway 1: Compression-Room (Primary)
- **Seed profile**: d0 > 0.50 (high baseline d_mean)
- **Intervention**: 50% relative d-compression before Cupd
- **Mechanism**: Large compression creates enough room to survive one epoch of d-rebound
- **Rate**: ~14% in high-room seeds at N=71
- **Cross-N**: Works at N=71 and N=72

### Pathway 2: Crypto-Hi Mild (Secondary)
- **Seed profile**: d0 ≤ 0.40, km0 > 0.98, moderate distHi (0.15-0.18)
- **Intervention**: 10-15% relative d-compression before Cupd
- **Mechanism**: Mild perturbation accesses seed's intrinsic Hi capacity
- **Rate**: ~9% in crypto-Hi seeds at N=71 (seeds 29, 57)
- **Cross-N**: Works at N=71 and N=72

### Prospective Classification Rules

| Class | Criteria | Pathway | Intervention |
|-------|----------|---------|-------------|
| P1b | d0 > 0.65 | 1 (Strong) | 50% d-compression |
| P1 | d0 > 0.50 | 1 (Standard) | 50% d-compression |
| P2 | d0 ≤ 0.40, km0 > 0.98, K-dist < 0.15 | 2 (Mild) | 10% d-compression |
| P3 | d0 in (0.40, 0.50] | Ambiguous | Universal or skip |
| P4 | otherwise | Low-probability | Skip |

---

## 6. Weakened or Falsified Hypotheses

- High proximity alone as persistence predictor → **falsified** (HBG: extreme Lo seeds persist)
- Fixed dT1 band as sufficient → **falsified** (HBF: no universal band)
- K-graft as valid induction → **falsified** (HBI: anti-aligned, 0 strict persistence)
- Centroid targeting as reliable entry → **falsified** (HBEQ: over-compresses)
- Relaxation direction as persistence predictor → **weakened** (HBG: anti-correlated)
- Universal Low→High controllability → **not supported** (max ~12% at N=72)

---

## 7. Not Claimed

V5.12 does NOT claim:
- Universal Low→High controllability
- High-basin entry solved or complete
- Sufficient entry conditions proven
- Physical interpretation of basin structure
- Attractor decomposition
- Universal criticality
- Physical phase transition
- Mathematical proof of basin topology
- Pathways work at all N or all seed blocks

---

## 8. Final V5.12 Conclusion

V5.12 identifies candidate High-basin entry conditions and validates **two weak but reproducible persistence pathways** using prospective pathway classification.

High-basin entry requires more than:
- Threshold crossing (Omega > 1.783)
- Endpoint K manipulation (K-graft)
- High proximity (distHi < distLo)

The strongest current model is **pathway-dependent**:
- High-room seeds respond to strong relative compression (Pathway 1)
- Crypto-Hi seeds respond to mild compression (Pathway 2)

**Matched pathway intervention improves strict persistence** relative to universal or mismatched intervention (10% vs 6% vs 0%), but overall persistence remains sparse (~10%) and N-windowed (N=67 inaccessible).

Therefore V5.12 supports **conditional High-basin entry pathways**, not general High-basin controllability.

---

## 9. Recommended V5.13

**Branch:** feature/v5.13-high-basin-pathway-validation-and-scaling

**Central question:** Do the V5.12 entry pathways generalize across larger seed sets, N ranges, and regimes?

**Planned suites:**
- HVP: Protocol
- HVE: Pathway Validation Execution
- HVA: Scaling Analysis
- HVI: Failure Mode Audit
- HVS: Final Synthesis

**Core questions:**
1. Do Compression-Room and Crypto-Hi pathways survive larger seed blocks (100-199)?
2. Do pathway rules transfer to out-of-sample seeds?
3. Are pathway success rates stable across N?
4. Can pathway classification be improved without overfitting?
5. Does N=67 remain inaccessible?
6. Is High-basin entry fundamentally sparse?

---

## 10. Test Summary

| Category | Count |
|----------|-------|
| V5.12-specific tests | 28 |
| Protocol tests | 3 |
| Analysis/execution tests | 25 |
| Cumulative total | 2565 |
| Failed | 0 |
| LongRunning | 26 (all analysis tests) |
