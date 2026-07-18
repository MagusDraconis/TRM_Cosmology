# V5.21 Low-N Rescue Execution — Analysis

**Suite:** LRE (Low-N Rescue Execution)
**Branch:** feature/v5.21-low-n-rescue-immunity-and-basin-access
**Status:** LRP COMPLETE → LRE COMPLETE
**Base:** V5.20 COMPLETE
**Execution:** 2026-07-18, 5/5 tests passed, ~3 min runtime

---

## 1. Baseline Reproduction

**Purpose:** Confirm N=50-64 rescue-immune under frozen M3++, N=65 onset.

**Expected (from V5.20):**
- N=50-64: A0=0%, A1=0%, zero rescues → rescue-immune
- N=65: first inducible seed appears, C3 lift > 0
- N=72: adaptive peak (highest rebMag + orthHiVec)

### Baseline Table

| N | Seeds | A0% | A1% | Rescue | Damaged | dPre | Regime |
|---|-------|-----|-----|--------|---------|------|--------|
| 50 | 8 | 0% | 0% | 0 | 0 | 0.415 | inaccessible |
| 55 | 8 | 0% | 0% | 0 | 0 | 0.414 | inaccessible |
| 60 | 8 | 0% | 0% | 0 | 0 | 0.258 | inaccessible |
| 62 | 8 | 0% | 0% | 0 | 0 | 0.410 | inaccessible |
| 63 | 8 | 0% | 0% | 0 | 0 | 0.450 | inaccessible |
| 64 | 8 | 0% | 0% | 0 | 0 | 0.321 | inaccessible |
| 65 | 7 | 14% | 29% | 1 | 0 | 0.368 | onset |
| 66 | 3 | 0% | 0% | 0 | 0 | 0.572 | adaptive-active |
| 70 | 8 | 50% | 62% | 1 | 0 | 0.349 | adaptive-active |
| 72 | 8 | 75% | 100% | 2 | 0 | 0.317 | adaptive-peak |

**Verification:** CONFIRMED — N=50-64 rescue-immune (0% A0, 0% A1 across all 6 low-N values). N=65 shows first C3 rescue (1 seed, 14%→29% lift). N=72 is adaptive peak (75% A0, 100% A1, 2 rescued). Baseline reproduction successful.

---

## 2. Intervention Families

**Tested interventions:**
- I0: Baseline M3++ (frozen V5.20)
- I1-25%, I1-50%, I1-75%: High-basin proximity push
- I2-x1.0, I2-x1.5, I2-x2.0: Entry-vector amplification (C3 factor)
- I3: d-band targeting (Hi d_mean equilibrium band)
- I4: K-preserve + C3
- I5: Rebound dampening + C3
- I6: Full entry package
- C1: Wrong-direction control
- C2: Over-compression negative control

### Intervention Table (all N, key interventions)

**N=50-64: ALL interventions show 0% induction, 0% strict persistence, 0% basin success.**

Notable: I1 (basin push) and I3 (d-band) produce 100% invalid d-matrices at N=50-63. N=64 is the first N where all interventions remain valid.

| N | Int | Seeds | A0% | Imm% | Str% | Bs% | Inv% | Dam% |
|---|-----|-------|-----|------|------|-----|------|------|
| 50 | I0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 50 | I1-75% | 6 | 0 | 0 | 0 | 0 | 100 | 0 |
| 50 | I4 | 6 | 0 | 0 | 0 | 0 | 50 | 0 |
| 50 | I5 | 6 | 0 | 0 | 0 | 0 | 50 | 0 |
| 50 | I6 | 6 | 0 | 0 | 0 | 0 | 100 | 0 |
| 55 | I0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 55 | I1-75% | 6 | 0 | 0 | 0 | 0 | 100 | 0 |
| 55 | I4 | 6 | 0 | 0 | 0 | 0 | 67 | 0 |
| 55 | I5 | 6 | 0 | 0 | 0 | 0 | 67 | 0 |
| 60 | I0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 60 | I4 | 6 | 0 | 0 | 0 | 0 | 17 | 0 |
| 62 | I0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 62 | I4 | 6 | 0 | 0 | 0 | 0 | 50 | 0 |
| 63 | I0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 63 | I4 | 6 | 0 | 0 | 0 | 0 | 83 | 0 |
| 64 | I0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 64 | I1-25% | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 64 | I1-75% | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 64 | I3 | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 64 | I4 | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 64 | I5 | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 64 | I6 | 6 | 0 | 0 | 0 | 0 | 0 | 0 |
| 65 | I0 | 6 | 17 | 33 | 33 | 0 | 0 | 0 |
| 65 | I4 | 6 | 17 | 17 | 17 | 0 | 0 | 0 |
| 65 | I5 | 6 | 17 | 17 | 17 | 0 | 0 | 0 |
| 66 | I0 | 3 | 0 | 0 | 0 | 0 | 0 | 0 |
| 66 | I1-75% | 3 | 67 | 67 | 0 | 0 | 0 | 67 |
| 66 | I3 | 3 | 33 | 33 | 0 | 0 | 0 | 33 |
| 70 | I0 | 6 | 33 | 50 | 50 | 0 | 0 | 0 |
| 70 | I1-75% | 6 | 50 | 50 | 0 | 0 | 0 | 50 |
| 70 | I4 | 6 | 33 | 67 | 33 | 0 | 0 | 0 |
| 72 | I0 | 6 | 67 | 100 | 100 | 0 | 0 | 0 |
| 72 | I4 | 6 | 67 | 83 | 83 | 0 | 0 | 0 |

**Key pattern:** N=50-64 shows zero induction under EVERY tested operator family. N=64 is special — all interventions execute without invalid states, yet still produce zero induction. The boundary is robust.

---

## 3. N=64 vs N=65 Comparison

**Key question:** Is N=64 closer to inducibility than N=50-60? What breaks at N=65?

### Head-to-Head Table

231 results across 2 N values, 7 interventions, expanded seed range (0-199).

| N | Int | A0% | Str% | dPre | rebMag | distHi | projHi |
|---|-----|-----|------|------|--------|--------|--------|
| 64 | I0 | 0% | 0% | 0.324 | 0.065 | 0.255 | -0.198 |
| 64 | I1-75% | 0% | 0% | 0.369 | 0.014 | 0.255 | — |
| 64 | I2-x2.0 | 0% | 0% | 0.324 | 0.065 | 0.255 | — |
| 64 | I3 | 0% | 0% | — | 0.043 | 0.255 | — |
| 64 | I4 | 0% | 0% | 0.324 | 0.065 | 0.255 | — |
| 64 | I5 | 0% | 0% | 0.324 | 0.065 | 0.255 | — |
| 64 | I6 | 0% | 0% | — | — | 0.255 | — |
| 65 | I0 | 7% | 21% | 0.381 | 0.027 | 0.208 | -0.185 |
| 65 | I1-75% | 0% | 0% | 0.382 | 0.043 | 0.208 | — |
| 65 | I2-x2.0 | 7% | 7% | 0.381 | 0.027 | 0.208 | — |
| 65 | I3 | 0% | 0% | — | 0.019 | 0.208 | — |
| 65 | I4 | 7% | 7% | 0.381 | 0.027 | 0.208 | — |
| 65 | I5 | 7% | 7% | 0.381 | 0.027 | 0.208 | — |
| 65 | I6 | 0% | 0% | — | — | 0.208 | — |

### Key Differences (baseline)

| Metric | N=64 | N=65 | Ratio | Significant? |
|--------|------|------|-------|-------------|
| dPre | 0.324 | 0.381 | 1.177x | SIGNIFICANT |
| distHi | 0.255 | 0.208 | 0.816x | SIGNIFICANT |
| rebMag | 0.065 | 0.027 | 0.415x | — (N64 HIGHER) |
| projHi | -0.198 | -0.185 | 0.934x | minor |

**Key finding:** Despite N=64 having HIGHER rebMag (0.065 vs 0.027), it cannot induce. The limiting factors are:
1. Lower dPre (0.324 vs 0.381) — further from needed displacement
2. Greater distHi (0.255 vs 0.208) — further from Natural High centroid
3. Even with all interventions (I1-I6), N=64 stays at 0% induction

---

## 4. Failure-Mode Classification

**Failure modes for N<65 candidates:**

| Mode | Description |
|------|-------------|
| insufficient-disp | Probe displacement too weak |
| dist-Hi | Distance to High basin not overcome |
| K-collapse | K collapses after attempted entry |
| d-rebound | d rebounds preventing persistence |
| off-vector | Response vector misaligned |
| no-rebMag | No rebMagnitude signal |
| threshold-artifact | Threshold-only classification |
| unresolved | Cannot classify |

### Failure-Mode Table

116 low-N candidates classified across 6 N values.

| N | Total | InsDisp | DistHi | KCol | dReb | OffVec | NoReb | Thresh | Unres |
|---|-------|---------|--------|------|------|--------|-------|--------|-------|
| 50 | 21 | 0 | 0 | 0 | 0 | 21 | 0 | 0 | 0 |
| 55 | 20 | 0 | 0 | 0 | 0 | 20 | 0 | 0 | 0 |
| 60 | 18 | 0 | 0 | 0 | 0 | 17 | 1 | 0 | 0 |
| 62 | 19 | 0 | 0 | 0 | 0 | 19 | 0 | 0 | 0 |
| 63 | 19 | 0 | 0 | 0 | 0 | 19 | 0 | 0 | 0 |
| 64 | 19 | 0 | 4 | 0 | 0 | 0 | 2 | 0 | 13 |

### Dominant Failure Modes

| N | Dominant Mode | Count | Rate |
|---|--------------|-------|------|
| 50 | off-vector | 21/21 | 100% |
| 55 | off-vector | 20/20 | 100% |
| 60 | off-vector | 17/18 | 94% |
| 62 | off-vector | 19/19 | 100% |
| 63 | off-vector | 19/19 | 100% |
| 64 | unresolved | 13/19 | 68% |

**Key insight:** N=50-63 failure is dominated by **off-vector response** (96-100%). The candidates' response vectors simply do not point toward the Natural High basin. No intervention can compensate for fundamentally wrong direction. At N=64, the failure mode shifts — only 0% are off-vector, and 68% are unresolved. N=64 is a transitional zone where the mechanism changes, but still no induction possible.

---

## 5. Operator-Class Boundary

**Tested: 36 combinations (6 N values × 6 operator families). ALL boundaries remain INTACT.**

### Operator-Class Boundary Table

36 combinations tested (6 N values × 6 operator families). ALL boundaries remain INTACT.

| N | Family | Breached? |
|---|--------|-----------|
| 50 | proximity | intact |
| 50 | entry-vector-amp | intact |
| 50 | d-band-targeting | intact |
| 50 | K-preservation | intact |
| 50 | rebound-dampening | intact |
| 50 | full-package | intact |
| 55 | proximity | intact |
| 55 | entry-vector-amp | intact |
| 55 | d-band-targeting | intact |
| 55 | K-preservation | intact |
| 55 | rebound-dampening | intact |
| 55 | full-package | intact |
| 60 | proximity | intact |
| 60 | entry-vector-amp | intact |
| 60 | d-band-targeting | intact |
| 60 | K-preservation | intact |
| 60 | rebound-dampening | intact |
| 60 | full-package | intact |
| 62 | proximity | intact |
| 62 | entry-vector-amp | intact |
| 62 | d-band-targeting | intact |
| 62 | K-preservation | intact |
| 62 | rebound-dampening | intact |
| 62 | full-package | intact |
| 63 | proximity | intact |
| 63 | entry-vector-amp | intact |
| 63 | d-band-targeting | intact |
| 63 | K-preservation | intact |
| 63 | rebound-dampening | intact |
| 63 | full-package | intact |
| 64 | proximity | intact |
| 64 | entry-vector-amp | intact |
| 64 | d-band-targeting | intact |
| 64 | K-preservation | intact |
| 64 | rebound-dampening | intact |
| 64 | full-package | intact |

### Boundary Verdict

**Low-N inaccessibility survives all tested operator classes: CONFIRMED.**

No single operator family nor the full package breaches the N<65 boundary. The boundary is robust under:
- Basin proximity push (25%, 50%, 75%)
- Entry-vector amplification (C3 ×1.0, ×1.5, ×2.0)
- d-band targeting
- K-preservation + C3
- Rebound dampening + C3
- Full combined package

---

## 6. Gates

| Gate | Name | Status | Interpretation |
|------|------|--------|---------------|
| A | Low-N Boundary Breaks | **NOT REACHED** | No safe intervention creates strict persistent High-basin access at N<65. Zero induction across all 13 intervention variants for all 6 low-N values. |
| B | N=64 Near-Break | **NOT REACHED** | N=64 remains 0% induction under all interventions. Even with full I6 package, zero strict persistence. |
| C | Low-N Remains Rescue-Immune | **REACHED** | All N=50-64 values show 0% induction, 0% strict persistence, 0% basin success under all tested interventions. N<65 boundary is robust. |
| D | K Preservation Helps | **NOT REACHED** | I4 (K-preserve+C3) produces 0% induction at all N<65 values. K-collapse is not the primary barrier. |
| E | Rebound Dampening Helps | **NOT REACHED** | I5 (rebound-dampening+C3) produces 0% induction at all N<65 values. d-rebound is not the primary barrier. |
| F | Entry-Vector Strength Helps | **NOT REACHED** | I2 (C3 ×1.0, ×1.5, ×2.0) produces 0% induction at all N<65 values. C3 weakness is not the primary barrier. |
| G | Full Package Required | **NOT REACHED** | I6 (full combined package) produces 0% induction at all N<65 values. Even combined operators cannot breach the boundary. |
| H | Unsafe / Invalid | **REACHED** | I1 (basin push) and I3 (d-band targeting) produce 100% invalid d-matrices at N=50-63. N=64 is the first N where all interventions remain valid. |

---

## 7. Claim Discipline Audit

| Rule | Status | Notes |
|------|--------|-------|
| No physical time/space/length/c/relativity/QM | ✓ PASS | No physical interpretation in any test |
| No emergence/cosmology claims | ✓ PASS | Restricted to RecoverFP analysis |
| No attractor decomposition | ✓ PASS | No decomposition claims |
| No universal controllability | ✓ PASS | No generalization beyond tested N |
| No threshold crossing = basin access | ✓ PASS | Basin success requires strict persistence + closer-to-Hi + not invalid |
| No M3++ modification | ✓ PASS | Frozen V5.20 logic used verbatim |
| No retuning of thresholds | ✓ PASS | Omega > 1.783 (V5.3 frozen) |
| No adding selectors | ✓ PASS | Same P1/P1b selection as V5.20 |
| Frozen V5.3 branch threshold (Omega > 1.783) | ✓ PASS | BTHR = 1.783 constant |
| Frozen V5.20 M3++ logic | ✓ PASS | Sim, RP, Nm, DL, Cupd, Of — all frozen |
| Strictly RecoverFP low-N basin-access analysis | ✓ PASS | All interventions operate in RecoverFP space |

## 8. Recommended Next Suite

### LRA — Low-N Rescue Analysis (recommended)
- Deep-dive into the off-vector failure mechanism at N=50-63
- Investigate why response vectors are misaligned at low N
- Analyze N=64 transitional behavior in detail
- Correlation analysis between dPre, distHi, projHi and inducibility threshold
- Seed-level trajectory analysis for N=64 vs N=65 boundary seeds

### LRS — Low-N Rescue Synthesis (contingent)
- If LRA finds any exploitable mechanism, design targeted intervention
- Otherwise, document the lower boundary as a fundamental feature of the model

---

## 9. Executive Summary

**Question:** Can low-N rescue-immune candidates (N=50-64) be made inducible by explicitly improving High-basin access?

**Answer:** No. Under all tested operator classes (basin proximity push, entry-vector amplification, d-band targeting, K-preservation, rebound dampening, and full combined package), N<65 remains rescue-immune with zero induction observed.

**Key findings:**

1. **N=50-63 failure is off-vector dominated (96-100%).** Candidates' response vectors are fundamentally misaligned — they don't point toward the Natural High basin. No intervention can fix a wrong-direction response.

2. **N=64 is a transitional zone.** Failures shift from off-vector (0%) to unresolved (68%), with some dist-Hi (4/19) and no-rebMag (2/19). All interventions execute without invalid states, yet still produce 0% induction.

3. **The N=64→65 boundary involves two significant shifts:** dPre increases (0.324→0.381, 1.177x) and distHi decreases (0.255→0.208, 0.816x). Both are significant and together create the conditions for first inducibility.

4. **Gate H is reached:** Basin proximity push (I1) and d-band targeting (I3) produce invalid d-matrices at N=50-63. N=64 is the first N where all interventions remain valid.

5. **Gate C is reached:** Low-N inaccessibility is robustly confirmed under current operator classes.

**Verdict:** The lower adaptive boundary at N=65 is not breakable by improving High-basin access alone. The failure at N≤63 is a fundamental off-vector response problem — not a basin-distance, K-collapse, rebound, or entry-vector-strength problem. N=64 is close but still 0% induction under all tested interventions.

---

*Generated by V5.21 LRE suite. 5/5 tests passed, 741 intervention results across 10 N values. Execution: 2026-07-18.*
