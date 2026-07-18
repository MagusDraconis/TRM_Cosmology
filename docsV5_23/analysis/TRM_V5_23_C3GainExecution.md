# V5.23 C3 Gain Execution — Analysis

**Suite:** CGE (C3 Gain Execution)
**Branch:** feature/v5.23-c3-gain-source-and-response-amplification
**Status:** CGE COMPLETE (6/6 tests, ~6 min)
**Base:** V5.22 COMPLETE

---

## 1. Gain Source Ranking

| Rank | Driver | Rescued | Failed | Ratio |
|------|--------|---------|--------|-------|
| **1** | **C3 omega shift** | 1.389 | 0.021 | **67.0x** |
| 2 | C3 K-shift | 0.011 | 0.001 | 20.1x |
| 3 | rebMagnitude | 0.205 | 0.021 | 9.5x |
| 4 | Pre-C3 align | 0.129 | 0.014 | 9.2x |
| 5 | C3 deltaAlign | 0.033 | 0.005 | 7.3x |
| 6 | C3 d-shift | 0.034 | 0.005 | 6.9x |
| 7 | Omega per alignment | 137.3 | 37.9 | 3.6x |
| 8 | Omega per K-shift | 321.9 | 89.9 | 3.6x |
| 9 | Movement norm | 0.040 | 0.027 | 1.5x |
| 10 | Probe dT1 | 0.579 | 0.393 | 1.5x |

**Gate A REACHED:** c3OmegaShift is the dominant gain source (67.0x).

---

## 2. N=64 Gain Cap Analysis

### Gain Decomposition: N=64 vs N=65 Rescued

| Gain Metric | N=64 | N=65 Rescued | N=65 Failed | Ratio (64→65R) |
|------------|------|-------------|-------------|----------------|
| c3OmegaShift | -0.014 | **0.952** | -0.010 | **66.5x** |
| deltaD | -0.005 | -0.075 | 0.003 | 13.9x |
| deltaK | 0.002 | 0.019 | -0.003 | 13.0x |
| deltaAlign | 0.005 | 0.070 | -0.004 | 13.0x |
| movementNorm | 0.027 | 0.087 | 0.027 | **3.3x** |
| kSensitivity | 0.009 | 0.012 | -0.214 | 1.3x |

### The Key Finding

**N=64 is movement-limited, not sensitivity-limited:**

| Aspect | Value | Interpretation |
|--------|-------|---------------|
| Movement gap | **3.27x** | N=64 C3 produces 3.3x less movement |
| Omega sensitivity gap | **0.31x** | N=64 is MORE sensitive per unit movement |

**N=64 omegaPerMovement = 112.7 vs N=65 rescued = 35.2.** N=64 would produce MORE Omega per unit of movement — but it produces so little movement that overall c3OmegaShift stays near zero. The C3 correction at N=64 barely nudges the state.

**Gate B REACHED:** N=64 is C3-movement-limited.

---

## 3. N=65 Activation Analysis

### Per-N Gain Metrics

| N | c3OmegaShift | deltaD | deltaK | deltaAlign | movementNorm |
|---|-------------|--------|--------|------------|-------------|
| 63 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 |
| 64 | -0.013 | -0.003 | 0.001 | 0.003 | 0.023 |
| 65 | 0.069 | -0.005 | -0.001 | 0.004 | 0.040 |
| 66 | -0.044 | -0.009 | 0.001 | 0.009 | 0.034 |

### Omega Sensitivity Per N

| N | omegaPerMovement | omegaPerD | omegaPerK | omegaPerAlign |
|---|-----------------|-----------|-----------|---------------|
| 63 | 0.0 | 0.0 | 0.0 | 0.0 |
| 64 | **140.8** | 160.1 | 349.7 | 146.5 |
| 65 | 5.6 | 6.4 | 14.4 | 6.3 |
| 66 | 1.5 | 2.0 | 3.0 | 1.7 |

**N=64 has 25x higher omega sensitivity than N=65!** The "sensitivity" metrics (omega per unit movement/d/K/alignment) are dominated by near-zero denominators at N=64. They're not meaningful as sensitivity metrics — they're artifacts of dividing small omega shifts by tiny movements.

### First-Changer: N=64→65

| Rank | Metric | Factor |
|------|--------|--------|
| 1 | kSensitivity | ×26.0 |
| 2 | c3OmegaShift | ×5.4 |
| 3 | movementNorm | ×1.8 |
| 4 | deltaK | ×1.7 |
| 5 | deltaD | ×1.5 |

**Gate C REACHED.**

---

## 4. Cross-N Gain Stability

### Rescued Seed Gain Profiles

| N | n | c3OmegaShift | omegaPerD | omegaPerK | movementNorm |
|---|----|-------------|-----------|-----------|-------------|
| 65 | 3 | 0.952 | 40.3 | 89.3 | 0.087 |
| 66 | 1 | 1.194 | 24.5 | 61.6 | 0.054 |
| 70 | 8 | 1.460 | 119.9 | 266.7 | 0.036 |
| 72 | 11 | 1.475 | 201.4 | 449.1 | 0.029 |

### Stability (Coefficient of Variation)

| Metric | Mean | CV |
|--------|------|-----|
| c3OmegaShift | 1.27 | **0.17** — stable |
| movementNorm | 0.051 | 0.44 — variable |
| omegaPerD | 96.5 | 0.73 — highly variable |
| omegaPerK | 216.7 | 0.72 — highly variable |

**c3OmegaShift is the most stable gain metric across N (CV=0.17).** The omega-per-unit metrics are highly variable (CV=0.72-0.73) — they depend on N-dependent movement norms, not just gain.

---

## 5. Pre-C3 Predictability

### Pre-C3 Predictors (Hi-Gain vs Lo-Gain)

| Predictor | Direction | Lo-Gain | Hi-Gain |
|-----------|-----------|---------|---------|
| alignPre | ↓ lower | 0.183 | 0.043 |
| dT1 | ↑ higher | 0.265 | 0.459 |
| omT1 | ↑ higher | 1.137 | 1.349 |
| kT1 | ↓ lower | 1.047 | 0.958 |
| kStdT1 | ↑ higher | 0.122 | 0.188 |

### Simple Threshold Prediction

| Threshold | Accuracy |
|-----------|----------|
| omT1 > 1.2 | **86%** |
| dT1 > 0.5 | 84% |
| alignPre < -0.1 | 83% |
| dT1>0.5 && alignPre<-0.1 | 83% |
| kT1 < 0.95 | 80% |

**Gate D:** omT1 > 1.2 is the best pre-C3 predictor (86%). But accuracy is modest — C3 gain cannot be fully predicted from pre-C3 state alone.

---

## 6. Gain Decomposition Verdict

### Model Classification

| Model | Description | Gap | Status |
|-------|-------------|-----|--------|
| A | Movement-limited | 1.31x | Not supported (vs all N≥65) |
| B | K response limited | 22.88x | **SUPPORTED** |
| C | Omega sensitivity | 0.05x | Not supported |
| D | Alignment-gain | 0.60x | Not supported |
| **E** | **Hidden N-dependent gain** | **20.7x vs 6.2x avg** | **SUPPORTED** |

### Synthesis

Two models are supported, and they're compatible:

1. **Model B (K response):** The K sensitivity (how much K changes per unit d change) differs dramatically between N=64 and N≥65 (22.88x gap). At N=64, the C3 correction changes d_mean but barely touches K_mean. At N≥65, the same correction produces coupled d+K movement.

2. **Model E (Hidden N-gain):** The c3OmegaShift ratio (20.7x) exceeds the average component ratio (6.2x) by 3.3x, indicating a residual N-dependent gain factor beyond what movement and K response explain.

Together they suggest: **C3 gain is primarily about the d→K coupling response, which activates at N=65. But even accounting for K response, there's a residual N-dependent amplification.**

---

## 7. The C3 Gain Mechanism

### What Controls c3OmegaShift?

c3OmegaShift ≈ movement × sensitivity

| Component | N=64 | N=65+ | Gap |
|-----------|------|-------|-----|
| Movement (how much C3 moves the state) | 0.027 | 0.040+ | 1.5-3.3x |
| K coupling (how K responds to d change) | 0.009 | varies | 22.9x |
| Omega response (how Omega responds to state change) | high | moderate | 0.05x |

**The dominant gain limiter is K coupling sensitivity.** N=64 has a very rigid K response to d changes. At N=65, the K response becomes much more flexible, allowing C3 movement to produce coupled d+K shifts that drive Omega changes.

### Why N=64 Is Capped

N=64's C3 correction changes d_mean but K barely budges. Without K movement, Omega doesn't shift. The d→K coupling is suppressed at N=64 and activates at N=65.

---

## 8. Gates

| Gate | Status | Evidence |
|------|--------|----------|
| **A — Gain Source Identified** | **REACHED** | c3OmegaShift 67.0x |
| **B — N=64 Cap Explained** | **REACHED** | Movement-limited (3.27x); K coupling suppressed |
| **C — N=65 Activation** | **REACHED** | kSensitivity ×26.0 at N=64→65 |
| D — Pre-C3 Predictor | PARTIAL | omT1>1.2 has 86% acc |
| E — Omega Sensitivity | NOT REACHED | N=64 MORE sensitive, not less |
| **F — K Response** | **REACHED** | kSens gap 22.88x |
| **G — Hidden N-Gain** | **REACHED** | Residual factor 3.3x beyond components |
| H — Unresolved | NOT REACHED | Mechanism identified |

---

## 9. Recommended Next Suite

**CGA — Gain Source Analysis**
- Probe the d→K coupling mechanism directly
- Test whether K response can be amplified at N=64 to match N=65
- Map the kSensitivity(N) curve across N=62-72
- Determine whether K coupling is a smooth function of N or a sharp transition

---

*CGE execution: 2026-07-18, 6/6 tests passed, ~6 min runtime.*
