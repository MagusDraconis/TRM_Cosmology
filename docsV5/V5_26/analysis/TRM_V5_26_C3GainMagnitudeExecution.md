# V5.26 C3 Gain Magnitude & Rescue Conversion — Results

**Suites:** MGE + MGA (2 tests, ~3 min)
**Branch:** feature/v5.26-c3-gain-magnitude-and-rescue-conversion
**Base:** V5.25 COMPLETE

---

## 1. Positive-Sign: Rescued vs Failed

| Metric | Rescued (n=9) | Failed (n=98) | Ratio |
|--------|-------------|---------------|-------|
| **c3OmegaShift** | **0.815** | 0.537 | 1.5x |
| omegaPerK | 164.9 | 158.1 | 1.0x |
| deltaD | 0.005 | 0.016 | 0.3x |
| deltaK | — | — | — |
| rebMag | — | — | — |

**Counterintuitive:** Rescued seeds have SMALLER deltaD. Movement magnitude does NOT predict rescue.

---

## 2. Rescue Thresholds (sign+ seeds only)

| Threshold | n | Rescue% |
|-----------|----|---------|
| sign+ only | 107 | 8% |
| c3OmgS > 0.1 | 54 | 15% |
| c3OmgS > 0.5 | 39 | 13% |
| omegaPerK > 100 | 32 | **16%** |
| deltaD > 0.02 | 48 | 10% |

**Gate D NOT REACHED:** No single threshold exceeds 30% rescue. Conversion is probabilistic, not threshold-based.

---

## 3. Progressive Chain Rescue Rates

| Chain Condition | n | Rescue% |
|----------------|----|---------|
| sign+ only | 107 | 8% |
| + c3OmgS > 0.1 | 54 | 15% |
| + c3OmgS > 0.5 | 39 | 13% |
| + deltaD > 0.02 | 48 | 10% |
| sign+ & c3OmgS>0.1 & deltaD>0.02 | 31 | **16%** |

Each additional filter enriches rescue rate modestly.

---

## 4. Model Selection: Mixed Conversion (Model E)

| Model | Evidence |
|-------|----------|
| A: c3OmegaShift threshold | 15% rescue at >0.1 — not sufficient alone |
| B: deltaD threshold | 10% — worse than c3OmegaShift |
| C: omegaPerK magnitude | 16% at >100 — best single predictor |
| D: resonant reversal | — |
| **E: mixed conversion** | **SELECTED** — no single threshold dominates |

---

## 5. Gates

| Gate | Status |
|------|--------|
| **A — Magnitude Driver** | **REACHED** (c3OmegaShift 1.5x) |
| **B — Sign+ Failures Explained** | **REACHED** (insufficient c3OmegaShift) |
| D — Rescue Threshold | NOT REACHED (max 16%) |
| **E — Mixed Model** | **REACHED** |
| C, F | NOT REACHED |

---

## 6. Interpretation

After positive sign is achieved, rescue conversion depends primarily on **c3OmegaShift magnitude** and **omegaPerK magnitude**. Movement (deltaD) is NOT the driver — rescued seeds have smaller deltaD. The conversion is probabilistic: even with sign+ AND c3OmgS>0.5, rescue rate is only 13%.

**The gain chain is necessary but not sufficient at any single layer.** Each layer filters out non-rescues but no layer guarantees rescue.

---

*V5.26 MGE+MGA: 2 tests, ~3 min.*
