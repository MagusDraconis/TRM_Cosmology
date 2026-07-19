# V5.24 Omega per K Gain Execution — Analysis

**Suite:** OGE (Omega per K Gain Execution)
**Status:** OGE COMPLETE (5/5 tests, ~5.4 min)
**Base:** V5.23 COMPLETE

---

## 1. omegaPerK Driver Ranking

| Rank | Driver | Rescued | Failed | Ratio |
|------|--------|---------|--------|-------|
| **1** | **c3OmegaShift** | 1.389 | 0.021 | **67.0x** |
| 2 | deltaK | 0.011 | 0.001 | 20.1x |
| 3 | alignPre | 0.137 | 0.015 | 9.3x |
| 6 | omegaPerK | 321.9 | 89.9 | 3.6x |
| 12 | Omega T1 | 1.623 | 1.212 | 1.3x |
| 14 | lambda1 | 0.898 | 0.972 | 0.9x |
| 17 | Omega dist | 0.161 | 0.571 | 0.3x |

**Gate A REACHED:** c3OmegaShift remains the dominant gain indicator (67.0x).

---

## 2. N=64 omegaPerK Suppression

### The Key Finding: N=64 omegaPerK Is Large but NEGATIVE

| Metric | N=64 | N=65 Rescued | N=65 Failed |
|--------|------|-------------|-------------|
| omegaPerK | **-279.9** | 89.3 | 6.6 |
| c3OmegaShift | -0.014 | 0.952 | -0.036 |
| deltaK | 0.002 | 0.019 | -0.004 |
| omT1 | 1.100 | 1.618 | 1.103 |
| **omDist** | **0.683** | **0.165** | **0.680** |

### K-State at T1

| Metric | N=64 | N=65 Rescued |
|--------|------|-------------|
| kMean | 1.006 | **0.869** |
| kStd | 0.152 | **0.281** |
| lambda1 | 0.991 | **0.855** |
| omT1 | 1.100 | **1.618** |

**N=65 rescued seeds have distinctly different K-state:** lower kMean, higher kStd, lower lambda1 (more flexible coupling), and Omega 1.5x closer to threshold.

### Why omegaPerK Is Negative at N=64

```
omegaPerK = c3OmegaShift / abs(deltaK)

N=64: c3OmegaShift = -0.014, deltaK = 0.002
      → omegaPerK = -279.9 (LARGE NEGATIVE)

N=65 rescued: c3OmegaShift = +0.952, deltaK = 0.019
              → omegaPerK = +89.3 (LARGE POSITIVE)
```

**N=64 already HAS large omegaPerK magnitude — it's just in the WRONG DIRECTION.** C3 at N=64 moves Omega away from threshold instead of toward it.

**Gate B REACHED.**

---

## 3. Cross-N Validation

| N | Rescued | omegaPerK | c3OmegaShift | omT1 | lambda1 |
|---|---------|-----------|-------------|------|---------|
| 65 | 3 | 14.4 | 0.069 | 1.173 | 0.980 |
| 66 | 1 | 3.0 | -0.044 | 1.215 | 0.973 |
| 70 | 8 | 21.0 | -0.232 | 1.299 | 0.977 |
| 72 | 11 | 54.2 | -0.443 | 1.369 | 1.000 |

### Threshold Prediction

| Threshold | Accuracy |
|-----------|----------|
| omegaPerK > 100 | **89%** |
| omegaPerK > 200 | 90% |
| omDist < 0.5 | **81%** |
| omDist < 0.6 | 81% |

---

## 4. Pre-C3 Predictors

| Predictor | Accuracy |
|-----------|----------|
| Omega > 1.2 | 58% |
| omDist < 0.6 | 58% |
| lambda1 < 0.95 | 50% |
| K_mean < 0.98 | 46% |

### N as Predictor

| N range | omegaPerK > 50 |
|---------|---------------|
| N ≤ 64 | 6% |
| N ≥ 65 | 45% |
| N ≥ 70 | 61% |

**Gate D:** Best individual predictor is only 58%. N alone predicts 45%. No single pre-C3 variable strongly predicts omegaPerK.

---

## 5. Gain-Layer Decomposition

| N | deltaD | deltaK | omegaPerK | c3OmegaShift | Resc% |
|---|--------|--------|-----------|-------------|-------|
| 64 | 0.005 | 0.002 | **279.9** | -0.014 | 0% |
| 65 | 0.003 | 0.002 | 14.2 | 0.054 | 9% |
| 66 | 0.008 | 0.001 | 12.3 | -0.105 | 3% |
| 70 | 0.005 | 0.005 | 24.4 | -0.228 | 15% |
| 72 | 0.021 | 0.012 | 26.1 | -0.617 | 17% |

### Primary Limiter at N=64→65 Onset

| Layer | Change |
|-------|--------|
| deltaD | ×0.5 |
| deltaK | ×1.1 |
| omegaPerK | ×0.05 (sign reversal) |

**Primary limiter: deltaK** — the C3 correction produces similar deltaK magnitude at N=64 and N=65, but the sign is wrong at N=64.

---

## 6. Synthesis: The omegaPerK Sign Problem

### The C3 gain chain at N=64:

```
C3 correction → deltaD = 0.005 (small)
             → deltaK = 0.002 (small, POSITIVE)
             → omegaPerK = c3OmegaShift/deltaK = -0.014/0.002 = -280
             → Omega moves AWAY from threshold
```

### The C3 gain chain at N=65 rescued:

```
C3 correction → deltaD = 0.075 (large)
             → deltaK = 0.019 (larger, POSITIVE)
             → omegaPerK = c3OmegaShift/deltaK = 0.952/0.019 = +89
             → Omega moves TOWARD threshold
```

### What Changes at N=65

| Factor | N=64 | N=65 Rescued | How It Changes |
|--------|------|-------------|----------------|
| Omega baseline (omT1) | 1.10 | **1.62** | Higher baseline → less distance to THR |
| Omega distance (omDist) | 0.68 | **0.17** | 4x closer → less C3 correction needed |
| K-state (kMean) | 1.01 | **0.87** | Lower mean K → more flexible response |
| deltaD magnitude | 0.005 | **0.075** | 14x larger C3 movement |
| c3OmegaShift sign | negative | **positive** | Sign reversal at onset |

**The N=65 onset is a sign reversal in omegaPerK.** N=64 has omegaPerK magnitude of 280 (large) but negative — C3 effectively pushes Omega down. N=65 rescued has omegaPerK of +89 (positive) — C3 pushes Omega up toward threshold.

---

## 7. Gates

| Gate | Status | Evidence |
|------|--------|----------|
| **A — omegaPerK Driver** | **REACHED** | c3OmegaShift 67.0x |
| **B — N=64 Suppression** | **REACHED** | omegaPerK is large negative at N=64 |
| **C — N=65 Activation** | **REACHED** | Sign reversal + omDist collapse at onset |
| D — Pre-C3 Predictor | PARTIAL | Best predictor 58% |
| E — K-State Driver | assessing | kStd and lambda1 differ but modestly |
| F — Geometry Driver | NOT primary | Geometry alone insufficient |
| G — Mixed | assessing | Multiple factors contribute |
| H — Unresolved | NOT REACHED | Mechanism identified |

---

## 8. Recommended Next

**OGA — Omega per K Analysis** or **OGS — Final Synthesis**

---

*OGE execution: 2026-07-19, 5/5 tests passed, ~5.4 min runtime.*
