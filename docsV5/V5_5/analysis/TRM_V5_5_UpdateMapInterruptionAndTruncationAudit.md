# V5.5 UMI — Update-Map Interruption and Truncation Audit

## Gate Reached: GATE D — FULL MAP REQUIRED

## Settings

| Parameter | Value |
|-----------|-------|
| N | 67 |
| Seeds | 0–29 |
| Threshold | Ω > 1.783 (V5.3 frozen, N=65 baseline) |
| Epochs | 1–5 |
| Freeze-K epochs | 1–4 |

## Baseline (Epoch 5)

| Metric | Value |
|--------|-------|
| Omega_mean | 1.2933 |
| CV | 0.2719 |
| High-branch | 5/30 |

## I1: Epoch Truncation

| Epochs | Omega_mean | CV | High | hi-to-lo | lo-to-hi |
|--------|--------|-----|------|-------|-------|
| E1 | 1.1070 | 0.0407 | 0/30 | 5 | 0 |
| E1-E2 | 1.1715 | 0.1854 | 1/30 | 5 | 1 |
| E1-E3 | 1.1382 | 0.1471 | 1/30 | 4 | 0 |
| E1-E4 | 1.1781 | 0.2086 | 1/30 | 5 | 1 |
| E1-E5 | 1.2933 | 0.2719 | 5/30 | 0 | 0 |

**Result:** Epoch 1 alone eliminates ALL 5 high-branch seeds. Epochs 2-4 partially recover (1/30). Full recovery requires Epoch 5. **Early commitment is NOT supported.** Late amplification is required.

## I2: Freeze-K

| Condition | Omega_mean | CV | High | hi-to-lo |
|-----------|--------|-----|------|-------|
| FreezeK at E1 | 1.1133 | 0.0736 | 0/30 | 5 |
| FreezeK at E2 | 1.1563 | 0.1570 | 1/30 | 5 |
| FreezeK at E3 | 1.1273 | 0.1178 | 1/30 | 4 |
| FreezeK at E4 | 1.1728 | 0.2328 | 1/30 | 5 |

**Result:** Freezing K at any epoch eliminates 4-5 of 5 high-branch seeds. **The d-to-K update loop is NECESSARY.** Without continued coupling evolution, the high branch collapses.

## Decision

**GATE D — FULL MAP REQUIRED**

- Branch generation requires all 5 epochs of the RecoverFP update cycle.
- Neither truncation at E1-E4 nor freeze-K preserves high-branch outcomes.
- The first epoch is insufficient, the last epoch is essential, and the coupling matrix must evolve at every epoch.
- The distributed multi-stage mechanism (UMA Gate D) is confirmed by intervention.

## Claim Discipline Audit

| Claim | Status |
|-------|--------|
| Truncation results at N=67 | SUPPORTED |
| Freeze-K results at N=67 | SUPPORTED |
| Epoch 5 is necessary for branch generation | CONDITIONAL (N=67, 30 seeds) |
| The d-to-K update loop is necessary | CONDITIONAL (freeze-K only) |
| Branch generation involves early commitment | FALSIFIED |
| Physical interpretation | NOT CLAIMED |
| Causation | NOT CLAIMED |
| Generalization beyond N=67 | NOT CLAIMED |

## Recommended Next Step

V5.5 finalization. The finding that the full multi-epoch map is required, combined with UMA's finding of synchronized d-to-K amplification, completes the V5.5 mechanism characterization.
