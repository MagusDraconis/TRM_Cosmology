# V5.21 Low-N Rescue Analysis — Results

**Suite:** LRA (Low-N Rescue Analysis)
**Branch:** feature/v5.21-low-n-rescue-immunity-and-basin-access
**Status:** LRA COMPLETE (6/6 tests passed, ~4.5 min)
**Base:** V5.20 COMPLETE, LRE COMPLETE

---

## 1. Off-Vector Mechanism Analysis

**Question:** Why are N=50-63 candidates off-vector?

### Response Vector Direction

| N | n | projHiVec | orthHiVec | offVecAng | respProjHi | respOrthHi |
|---|----|-----------|-----------|-----------|------------|------------|
| 50 | 20 | 0.0000 | 0.1820 | 1.5708 | 0.0000 | 0.0953 |
| 55 | 20 | 0.0000 | 0.1808 | 1.5708 | 0.0000 | 0.1021 |
| 60 | 17 | 0.0000 | 0.2486 | 1.5708 | 0.0000 | 0.1286 |
| 62 | 18 | 0.0000 | 0.2180 | 1.5708 | 0.0000 | 0.1191 |
| 63 | 19 | 0.0000 | 0.2278 | 1.5708 | 0.0000 | 0.1266 |
| 65 | 14 | -0.1846 | 0.0355 | 0.1922 | 0.0073 | 0.0336 |

### Key Finding

**projHiVec = 0.0000 universally across N=50-63.** All candidates have exactly zero projection onto the High-entry vector. The off-vector angle is exactly π/2 (1.5708 rad = 90°) — perfectly orthogonal. This is not a statistical trend; it is an exact structural property.

**Why:** The Natural High centroid does not exist at N=50-63. `PCent(n, true)` finds zero seeds with Omega > 1.783, producing a NaN centroid. Distance to Hi (distHi) is universally NaN. The entry vector from Lo to NaN-Hi is itself NaN, making projHiVec = 0.

**Implication:** N<64 candidates cannot be rescued because there is no High basin to point toward. The "off-vector" failure is not a response problem — it's a structural absence of the target basin.

### Severity Classification
- Severely off-vector (> 0.15 rad): 94/94 (100%)
- Mildly off-vector (≤ 0.15 rad): 0/94 (0%)

### Gate A: Off-Vector Barrier Confirmed — **REACHED**

---

## 2. N=64 Transitional Analysis

**Question:** Why does N=64 shift from off-vector to unresolved but remain non-inducible?

### Metric Comparison: N=63 → 64 → 65

| Metric | N=63 | N=64 | N=65 | 63→64 | 64→65 |
|--------|------|------|------|-------|-------|
| distHi | NaN | 0.2573 | 0.2084 | — | -0.049 |
| projHiVec | 0.0000 | -0.2000 | -0.1846 | -0.200 | +0.015 |
| offVecAngle | 1.5708 | 0.3140 | 0.1922 | -1.257 | -0.122 |
| dT1 | 0.3933 | 0.3225 | 0.3808 | -0.071 | +0.058 |
| rebMag | -0.0357 | 0.0745 | 0.0270 | +0.110 | -0.048 |
| respProjHi | 0.0000 | 0.0452 | 0.0073 | +0.045 | -0.038 |
| deltaAlign | 0.0000 | 0.0072 | 0.0157 | +0.007 | +0.009 |

### Key Finding

**N=64 is closer to N=65 than N=63** (normalized distance: N=64→65 = 0.366, N=63→64 = NaN because N=63 has NaN distHi).

The critical transition happens at N=64 itself:
- **projHiVec becomes nonzero** (0 → -0.200): the Natural High centroid exists for the first time
- **offVecAngle drops from 90° to 18°** (1.571 → 0.314): candidates can now "see" the High basin
- **rebMag becomes positive** (-0.036 → +0.075): the probe response becomes directional
- **respProjHi becomes nonzero** (0 → 0.045): response has a component toward Hi

**But N=64 still fails** because:
- C3 omega shift is only 0.0085 (negligible correction effect)
- deltaAlign is only 0.007 (C3 barely changes alignment)

### Failure-Mode Distribution
| N | off-vector | other |
|---|-----------|-------|
| 63 | 19 (100%) | 0 |
| 64 | 18 (100%) | 0 |

Note: The LRA_02 test classifies N=64 as "off-vector" because offVecAngle (0.314) > 0.12 threshold. This differs from LRE_04 which classified N=64 as "unresolved" using a different classification logic.

### Gate B: N=64 Transitional — **NOT REACHED** (N=64 still classified as off-vector by LRA; 0% induction)

---

## 3. N=64→65 Onset Comparison

**Question:** What changes at N=65 that makes rescue possible?

### N=65 Rescued vs Failed

| Metric | Rescued (n=2) | Failed (n=12) | Ratio |
|--------|--------------|---------------|-------|
| distHi | 0.1947 | 0.2107 | 0.92x |
| offVecAngle | 0.1829 | 0.1938 | 0.94x |
| dT1 | 0.9557 | 0.2850 | **3.35x** |
| dT2 | 0.1969 | 0.4429 | 0.44x |
| rebMag | -0.7588 | 0.1579 | -4.81x |
| deltaAlign | 0.1106 | -0.0001 | — |
| respProjHi | -0.6290 | 0.1134 | -5.55x |

**Critical observation:** Rescued seeds at N=65 have:
1. **Much higher dT1** (0.956 vs 0.285, 3.35x): the probe creates much more displacement
2. **Massive negative rebMag** (-0.759): the system springs back strongly
3. **Large deltaAlign** (0.111): C3 correction is very effective at realigning

### N=64 vs N=65 Rescued

| Metric | N=64 (fail) | N=65 Rescued | Ratio |
|--------|------------|-------------|-------|
| distHi | 0.2573 | 0.1947 | 0.76x |
| offVecAngle | 0.3140 | 0.1829 | 0.58x |
| dT1 | 0.3225 | 0.9557 | **2.96x** |
| rebMag | 0.0745 | -0.7588 | -10.2x |
| deltaAlign | 0.0072 | 0.1106 | **15.3x** |
| kCollapse | -0.0343 | 0.2896 | — |

### C3 Correction Effect

| N | c3DShift | c3KShift | c3OmegaShift |
|---|----------|----------|-------------|
| 64 | -0.0068 | 0.0027 | 0.0085 |
| 65 | -0.0168 | 0.0043 | **0.1344** |

**The C3 omega shift jumps 15.8x from N=64 to N=65** (0.0085 → 0.1344). This is the single largest change. The C3 correction goes from producing a negligible 0.009 Omega increase at N=64 to a substantial 0.134 increase at N=65.

### Gate C: Onset Mechanism Identified — **REACHED**

The N=65 onset is driven by:
1. Natural High centroid becomes reachable (N=64)
2. **C3 correction effectiveness jumps 15.8x** (N=64→65)
3. Entry-vector alignment improvement (deltaAlign 15.3x)
4. Probe displacement capability (dT1 2.96x)

---

## 4. Rescue-Immunity Driver Ranking

### Barrier Ranking (by metric contrast between low-N and adaptive)

| Rank | Barrier | Contrast | Notes |
|------|---------|----------|-------|
| **1** | **Off-vector response** | 6.66x | projHiVec = 0 at N<64, becomes nonzero at N=64 |
| 2 | Weak C3 alignment | 83.77x* | *Artifact of near-zero denominator at N<64 |
| 3 | Insufficient displacement | 0.97x | dT1 similar across boundary |
| 4 | K collapse | 0.19x | Not a primary barrier |
| 5 | Basin distance | NaN | Hi centroid doesn't exist at N<64 |

**Dominant barrier:** Off-vector response. This is not a continuous metric — it's a structural binary: at N<64 the Natural High centroid does not exist, making all candidates exactly orthogonal to a nonexistent target.

### Key Metrics by N

| N | offAng | distHi | dT1 | rebMag | deltaAlign | rescue% |
|---|--------|--------|-----|--------|-----------|---------|
| 50 | 1.571 | NaN | 0.423 | -0.036 | 0.000 | 0% |
| 55 | 1.571 | NaN | 0.421 | -0.073 | 0.000 | 0% |
| 60 | 1.571 | NaN | 0.285 | 0.063 | 0.000 | 0% |
| 62 | 1.571 | NaN | 0.410 | -0.091 | 0.000 | 0% |
| 63 | 1.571 | NaN | 0.450 | -0.066 | 0.000 | 0% |
| 64 | 0.295 | 0.269 | 0.321 | 0.017 | 0.001 | 0% |
| 65 | 0.207 | 0.213 | 0.368 | 0.133 | **0.016** | **14%** |
| 66 | 0.114 | 0.340 | 0.572 | -0.081 | 0.047 | 0% |
| 70 | 0.135 | 0.271 | 0.402 | 0.068 | 0.017 | 20% |
| 72 | 0.297 | 0.238 | 0.312 | 0.233 | 0.006 | 20% |

### Gates D/E/F
- Gate D (Distance dominates): NOT REACHED
- Gate E (Response-direction dominates): REACHED (off-vector is primary barrier at N<64)
- Gate F (Mixed barrier): NOT REACHED (single dominant barrier)

---

## 5. Boundary Mechanism Classification

### Model Evidence

| Model | Description | Status | Evidence |
|-------|-------------|--------|----------|
| A | Distance barrier | Not supported | distHi = NaN at N<64 (Hi centroid absent) |
| **B** | **Response-direction barrier** | **SUPPORTED** | offAng = π/2 at N<64, drops to 0.31 at N=64 |
| C | Invalid geometry | SUPPORTED (N<64) | Basin push/d-band produce invalid d-matrices |
| D | d/K response barrier | Supported | rebMag changes -0.04 → 0.07 across boundary |

### Per-N Classification

| N | Model | offAng | distHi | rebMag |
|---|-------|--------|--------|--------|
| 50 | B: response-direction | 1.571 | NaN | -0.043 |
| 55 | B: response-direction | 1.571 | NaN | -0.052 |
| 60 | B: response-direction | 1.571 | NaN | 0.071 |
| 62 | B: response-direction | 1.571 | NaN | -0.082 |
| 63 | B: response-direction | 1.571 | NaN | -0.036 |
| 64 | B: response-direction | 0.314 | 0.257 | 0.074 |
| 65 | B: response-direction | 0.192 | 0.208 | 0.027 |
| 66 | A: distance barrier | 0.064 | 0.262 | 0.100 |
| 70 | B: response-direction | 0.146 | 0.281 | 0.080 |
| 72 | B: response-direction | 0.285 | 0.225 | 0.244 |

### Final Classification: **Model B — Response-Direction Barrier**

The lower adaptive boundary is a response-direction barrier. At N<64, the Natural High centroid does not exist, making every candidate exactly orthogonal to the target. At N=64, the centroid appears but C3 correction is too weak (omega shift = 0.009). At N=65, C3 effectiveness jumps 15.8x, enabling first rescue.

### Gate G: Hidden Variable — NOT REACHED (mechanism explained)

---

## 6. Response-Vector Trajectory Analysis

### C3 Entry-Vector Alignment

| N | alignPre | alignPost | deltaAlign | c3OmegaShift |
|---|----------|-----------|------------|-------------|
| 50 | 0.0000 | 0.0000 | 0.0000 | NaN |
| 55 | 0.0000 | 0.0000 | 0.0000 | NaN |
| 60 | 0.0000 | 0.0000 | 0.0000 | NaN |
| 62 | 0.0000 | 0.0000 | 0.0000 | NaN |
| 63 | 0.0000 | 0.0000 | 0.0000 | NaN |
| 64 | 0.0460 | 0.0532 | 0.0072 | 0.0085 |
| 65 | 0.0055 | 0.0212 | 0.0157 | **0.1344** |
| 66 | 0.0039 | 0.0247 | 0.0208 | 0.0804 |
| 70 | 0.0510 | 0.0678 | 0.0168 | 0.2791 |
| 72 | 0.1620 | 0.1674 | 0.0054 | 0.2604 |

### Boundary Alignment Shift (N=64→65)
| Metric | N=64 | N=65 | Change |
|--------|------|------|--------|
| alignPre | 0.0460 | 0.0055 | 0.12x |
| alignPost | 0.0532 | 0.0212 | 0.40x |
| deltaAlign | 0.0072 | 0.0157 | **2.17x** |
| c3OmegaShift | 0.0085 | 0.1344 | **15.84x** |

### Rescued vs Failed Alignment

| N | Rescued alignPre | Failed alignPre | Rescued deltaAlign | Failed deltaAlign |
|---|-----------------|-----------------|-------------------|-------------------|
| 65 | **-0.5531** | 0.0986 | **0.1106** | -0.0001 |
| 66 | -0.2163 | 0.0196 | 0.0507 | 0.0187 |
| 70 | -0.1533 | 0.0882 | 0.0384 | 0.0129 |
| 72 | -0.1271 | 0.2198 | 0.0342 | -0.0004 |

**Critical finding:** Rescued seeds have **negative alignPre** — their natural alignment is away from the Hi basin. The C3 correction actively reverses their direction, producing large positive deltaAlign. Failed seeds have positive alignPre but tiny or negative deltaAlign. **Rescue is not about being close — it's about being correctable.**

---

## 7. Synthesis: The Lower Boundary Mechanism

### Three-phase transition:

**Phase 1 — N=50-63: Structural Absence**
- The Natural High branch is empty (no seeds have Omega > 1.783)
- PCent fails to compute a Hi centroid → distHi = NaN → projHiVec = 0
- All candidates are exactly orthogonal to a nonexistent target
- No intervention can fix: you can't point toward something that doesn't exist

**Phase 2 — N=64: Centroid Exists, C3 Too Weak**
- Natural High centroid first appears (distHi becomes finite)
- projHiVec becomes nonzero (-0.200), offVecAngle drops to 18°
- But C3 correction effect is negligible (omega shift = 0.0085)
- Candidates can see the target but can't reach it

**Phase 3 — N=65: C3 Becomes Effective**
- C3 omega shift jumps 15.8x (0.0085 → 0.1344)
- deltaAlign jumps 2.17x (0.007 → 0.016)
- First rescued seeds appear (2/14, 14%)
- Rescued seeds have negative alignPre — they need correction, not just proximity

### The boundary at N=65 is a C3-correction-effectiveness threshold, not a distance or response-direction threshold.

---

## 8. Claim Discipline Audit

| Rule | Status |
|------|--------|
| No physical time/space/length/c/relativity/QM | ✓ |
| No emergence/cosmology claims | ✓ |
| No attractor decomposition | ✓ |
| No universal controllability | ✓ |
| No threshold crossing = basin access | ✓ |
| No M3++ modification | ✓ |
| No retuning of thresholds | ✓ |
| Frozen V5.3 branch threshold | ✓ |
| Strictly RecoverFP low-N analysis | ✓ |

---

## 9. Recommended Next Suite

**LRS — Low-N Rescue Synthesis**
- Document the three-phase boundary mechanism
- Formalize: (1) structural absence, (2) centroid-present-but-weak-C3, (3) effective-C3
- Quantify the C3-effectiveness curve across N=64-72
- Produce final boundary model

---

*LRA execution: 2026-07-18, 6/6 tests passed, ~4.5 min runtime.*
