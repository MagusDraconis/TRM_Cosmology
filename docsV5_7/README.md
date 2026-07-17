# TRM V5.7 — Reduced Operator Validation

**Branch:** `feature/v5.7-recoverfp-reduced-operator-validation`  
**Date:** 2026-07-17  
**Base:** V5.6 COMPLETE (0afed21)  
**Status:** INITIALIZED  
**Type:** Falsification and robustness branch — NOT a discovery branch

---

## Purpose

V5.7 tests whether the V5.6 reduced operator model survives outside the regime in which it was discovered.

V5.6 identified:

1. Nm is suppressive, not generative
2. d_mean before Cupd is the minimal suppressive coordinate
3. State-conditioned operator `d' = d + 0.5 * d_mean_current` works across N=67,69,72
4. Cupd exponential `K = K0 * exp(-d/xi)` is the central mechanism
5. V9 amplification and Nm suppression are symmetric inverse pathways

V5.7 asks: **does this mechanism generalize, or was it overfit to the V5.6 discovery regime?**

---

## Directory Structure

```
docsV5_7/
├── README.md                          ← this file
├── experiments/TRM_V5_7_Experiment_Log.md
├── theory/TRM_V5_7_ReducedOperatorValidation_Roadmap.md
├── protocols/TRM_V5_7_ReducedOperatorValidation_Protocol.md
└── analysis/                          ← per-suite analysis (created as suites execute)

TRM.Tests/V5_7/
└── V5_7_ReducedOperatorValidationProtocol_Tests.cs
```

---

## Suites

| Suite | Type | Purpose |
|-------|------|---------|
| ROCP | Protocol | Pre-register validation axes, failure criteria, decision gates |
| ROCE | Execution | Cross-N, cross-seed validation of reduced operator |
| ROCA | Analysis | Evaluate robustness, failure criteria, gate classification |
| ROCS | Synthesis | Branch completion and V5.7 findings |

---

## Validation Axes

| Axis | What | Range |
|------|------|-------|
| A | N generalization | N=60,62,64,66,67,69,72,75,80 |
| B | Seed generalization | 0–99 with blocked validation (0–29, 30–59, 60–99) |
| C | Mechanism stability | Nm suppression + Cupd2 amplification independently |
| D | Operator robustness | Full Nm vs state-conditioned operator |
| E | Threshold robustness | d_mean → K → branch stability across regimes |

---

## Central Question

**Does the reduced operator model generalize outside the V5.6 discovery regime?**

---

## Falsification Stance

V5.7 attempts to **falsify**, not confirm, V5.6 findings. The preferred outcome is discovering that the mechanism does NOT generalize — this would prevent over-claiming and guide refinement.

---

## Quick Reference

- **V5.6 Baseline Regime:** N=67,69,72; xi=1.75, K0=1.20, s=0.10; seeds 0–29/0–99
- **V5.7 Test Space:** N=60–80; same regime params; multiple seed blocks
- **Branch Threshold:** Omega > 1.783 (V5.3 frozen, NOT redefined)
- **Reduced Operator:** `d' = d + 0.5 * d_mean_current` before Cupd
