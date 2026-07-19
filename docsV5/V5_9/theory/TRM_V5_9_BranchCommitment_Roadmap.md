# TRM V5.9 — Branch Commitment Roadmap

**Date:** 2026-07-17  
**Base:** V5.8 (predictability characterized)  
**Branch:** `feature/v5.9-branch-commitment-and-irreversibility`

---

## 1. Core Question

**When does branch identity become effectively irreversible?**

V5.8: branches become predictable at epoch 5. V5.9: **when do they become committed?**

---

## 2. Key Distinction

| Concept | Definition |
|---------|------------|
| **Predictability** (V5.8) | Can we forecast the final outcome from current state? |
| **Commitment** (V5.9) | Can intervention at the current epoch change the final outcome? |

A branch may be committed (irreversible) before it becomes predictable.

---

## 3. Intervention Methodology

- Apply state-conditioned d_mean suppressor at each epoch (1–4)
- Track whether final branch label changes vs no-intervention baseline
- If intervention at epoch K cannot change outcome → branch is committed by epoch K
- Test at N=67, 69, 72 (class-stable N from V5.8)

---

## 4. Commitment Metrics

| Metric | Definition |
|--------|------------|
| Flip rate | Fraction of seeds that change branch label under intervention |
| Commitment epoch | Earliest epoch where flip rate < 5% |
| Irreversibility onset | Earliest epoch where flip rate = 0% |
| Intervention dose | Strength of d_mean suppressor applied |

---

## 5. Decision Gates

| Gate | Condition | Interpretation |
|------|-----------|----------------|
| A | Commitment before predictability | Branch locks before epoch 5 |
| B | Commitment at predictability epoch | Branch locks at epoch 5 |
| C | Commitment after predictability | Branch is predictable before committed |
| D | No clear commitment | Branch remains reversible throughout |
