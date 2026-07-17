# TRM V5.10 — Branch Control Asymmetry Roadmap

**Date:** 2026-07-17  
**Base:** V5.9 (directional commitment discovered)  
**Branch:** `feature/v5.10-branch-control-asymmetry-and-induction`

---

## 1. Core Question

**Why can high branches be suppressed at CP5, but low branches cannot be induced high?**

V5.9 BCI: Hi→Lo = 14–31%, Lo→Hi = 0% at all N at epoch 5.

---

## 2. Intervention Classes

| Class | Direction | Examples |
|-------|-----------|----------|
| I-A: d_mean increase | Hi→Lo | d += 0.5×d_mean (tested, works) |
| I-B: d_mean decrease | Lo→Hi | d −= α×d_mean (to be tested) |
| I-C: KMean boost | Lo→Hi | Rescale K upward post-Cupd |
| I-D: Combined | Both | d-decrease + K-boost |
| I-E: Stage re-entry | Lo→Hi | Apply intervention + extra epoch |

---

## 3. Decision Gates

| Gate | Condition | Interpretation |
|------|-----------|----------------|
| A | Lo→Hi possible with d_mean decrease | Induction requires opposite operator |
| B | Lo→Hi possible with K boost | Induction is K-mediated |
| C | Lo→Hi possible with combined intervention | Induction requires multi-coordinate |
| D | Lo→Hi possible with stage re-entry | Induction requires earlier checkpoint |
| E | Lo→Hi impossible under all tested | Genuine structural basin asymmetry |
