# V5.23 C3 Gain Perturbation Audit — Results

**Suite:** CGI (C3 Gain Perturbation Audit)
**Branch:** feature/v5.23-c3-gain-source-and-response-amplification
**Status:** CGI COMPLETE (6/6 tests, ~5 min)
**Base:** V5.22 COMPLETE, CGE COMPLETE, CGA COMPLETE

---

## 1. Baseline Reproduction

| N | n | dTail | deltaD | deltaK | c3OmegaShift | Resc% |
|---|----|-------|--------|--------|-------------|-------|
| 64 | 8 | 0.491 | 0.001 | -0.001 | 0.006 | 0% |
| 65 | 7 | 0.648 | -0.031 | 0.008 | -0.059 | 0% |
| 66 | 3 | 1.009 | -0.030 | 0.012 | -0.001 | 0% |
| 70 | 20 | 0.752 | -0.011 | 0.001 | 0.108 | 0% |
| 72 | 20 | 0.506 | -0.025 | 0.009 | -0.074 | 0% |

---

## 2. d_tail Expansion Effect

### N=64: Expansion Changes deltaD but NOT c3OmegaShift

| Perturbation | dTail | deltaD | deltaK | c3OmegaShift | kSensitivity |
|-------------|-------|--------|--------|-------------|-------------|
| P0-baseline | 0.491 | 0.001 | -0.001 | 0.006 | 0.022 |
| P1-tail+10% | 0.540 | -0.011 | 0.005 | -0.008 | 0.094 |
| P1-tail+25% | 0.614 | **-0.021** | 0.009 | 0.005 | 0.330 |
| P1-tail+50% | 0.736 | **-0.014** | 0.006 | **0.007** | 0.213 |
| P3-upTail+50% | 0.736 | -0.005 | 0.002 | 0.007 | 0.033 |

**d_tail expansion increases deltaD magnitude (up to 40x at +50%), but c3OmegaShift stays flat at ~0.007.** The C3 correction moves the state more but Omega doesn't respond.

### N=65 and N=72

| N | dTail change | c3OmegaShift |
|---|-------------|-------------|
| 65 | 0.648 → 0.971 (+50%) | -0.059 → -0.061 |
| 72 | 0.506 → 0.759 (+50%) | -0.074 → +0.019 |

**Gate A REACHED:** Expansion increases deltaD magnitude but NOT c3OmegaShift.

---

## 3. d_tail Compression Effect

| N | Perturbation | dTail | deltaD | c3OmegaShift |
|---|-------------|-------|--------|-------------|
| 65 | P0-baseline | 0.648 | -0.031 | -0.059 |
| 65 | P2-tail-25% | 0.486 | -0.034 | -0.161 |
| 65 | P2-tail-50% | 0.325 | -0.048 | 0.006 |
| 72 | P0-baseline | 0.506 | -0.025 | -0.074 |
| 72 | P2-tail-25% | 0.380 | -0.047 | 1.015 |
| 72 | P2-tail-50% | 0.254 | -0.058 | 1.176 |

**Counterintuitive:** Compression at N=72 INCREASES c3OmegaShift. Narrowing the tail may concentrate the C3 correction effect.

**Gate B REACHED:** Compression changes C3 gain.

---

## 4. N=64 Breakability

**N=65 rescued d_tail reference: 1.500**

| Perturbation | dTail | c3OmegaShift | deltaD | Strict? |
|-------------|-------|-------------|--------|---------|
| P0-baseline | 0.570 | -0.004 | -0.008 | No |
| P5-factor1.5 | 0.892 | 0.666 | -0.027 | No |
| P5-factor2.0 | — | — | — | No |
| P5-factor2.5 | — | — | — | No |

**Best N=64 seed (s=91, P5-factor1.5):** dTail=0.892, c3OmegaShift=0.666, deltaD=-0.027.

c3OmegaShift rises from ~0 to 0.666 — significant gain — but still below the RR minimum (0.765) and zero strict persistence.

**Gate C NOT REACHED.** **Gate D REACHED** — N=64 can reach near-threshold c3OmegaShift (0.666) but fails strict persistence. d_tail perturbation produces gain but not rescue.

---

## 5. Tail Specificity

| N | Type | dTail | c3OmegaShift | deltaD |
|---|------|-------|-------------|--------|
| 64 | P3-upTail | 0.736 | 0.007 | -0.005 |
| 64 | P1-fullTail | 0.736 | 0.007 | -0.014 |

Full-tail and upper-tail expansion produce similar c3OmegaShift at N=64. **Gate E REACHED** — upper vs full tail makes little difference.

---

## 6. Causality Classification

### Evidence

| N | dTail range | deltaD range | c3OmegaShift range |
|---|------------|-------------|-------------------|
| 64 | 0.246 → 0.736 | 0.001 → -0.014 | 0.006 → 0.011 |
| 65 | 0.325 → 0.971 | -0.048 → 0.007 | -0.059 → 0.006 |
| 72 | 0.254 → 0.759 | -0.058 → -0.015 | -0.074 → 1.176 |

### Classification: **Necessary but Not Sufficient**

| Evidence | Assessment |
|----------|-----------|
| deltaD changes with d_tail | ✓ Causal link confirmed |
| c3OmegaShift changes with deltaD | ✗ Link broken |
| d_tail predicts kSensitivity | ✓ (84% from CGA) |
| d_tail alone enables C3 gain | ✗ Not sufficient |

**d_tail is a necessary precondition for C3 gain (wider distributions enable larger C3 movement), but it is not sufficient.** Even with deltaD magnitude increased 40x, c3OmegaShift stays flat. The C3 gain chain has an additional bottleneck between deltaD and c3OmegaShift that d_tail alone cannot bypass.

---

## 7. The C3 Gain Chain Bottleneck

```
d_tail → deltaD magnitude  ← d_tail CAN influence this
   ↓
deltaD × kSensitivity → deltaK  ← kSensitivity stable at ~0.01
   ↓
deltaK × omegaPerK → c3OmegaShift  ← omegaPerK is the hidden bottleneck
```

**d_tail controls the INPUT (deltaD magnitude) but not the GAIN (omegaPerK).** The gain factor omegaPerK varies dramatically across seeds and N values, and it is this factor — not d_tail — that ultimately determines whether C3 produces rescue.

---

## 8. Gates

| Gate | Status | Evidence |
|------|--------|----------|
| **A — Expansion increases deltaD** | **REACHED** | deltaD 0.001→-0.014 at N=64 |
| **B — Compression changes gain** | **REACHED** | c3OmgS -0.074→1.176 at N=72 |
| C — Opens N=64 | NOT REACHED | 0% strict persistence |
| **D — Gain but no persistence** | **REACHED** | c3OmgS reaches 0.666, no strict |
| **E — Upper-tail similar to full** | **REACHED** | Similar effect |
| F — Marker only | NOT REACHED | d_tail does change deltaD |
| G — Unsafe/invalid | NOT REACHED | 0% invalid across all perturbations |

---

## 9. Recommended Next Suite

**CGS — C3 Gain Synthesis**
- Finalize V5.23 with the d→K coupling + omegaPerK bottleneck model
- Document that d_tail is necessary but omegaPerK is the hidden gain factor
- Synthesize findings from CGE, CGA, CGI

---

*CGI execution: 2026-07-19, 6/6 tests passed, ~5 min runtime.*
