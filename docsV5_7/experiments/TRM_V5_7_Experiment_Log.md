# TRM V5.7 — Experiment Log

**Branch:** `feature/v5.7-recoverfp-reduced-operator-validation`  
**Base:** V5.6 COMPLETE (`0afed21`)  
**Date:** 2026-07-17  
**Cumulative tests at branch start:** 2435 passed, 0 failed

---

## Initialization

V5.7 initialized from V5.6 COMPLETE (`0afed21`).

Base test count: 2435 cumulative.

**Type:** Falsification and robustness branch.

**Central question:** Does the V5.6 reduced operator model generalize outside the discovery regime?

**Status:** INITIALIZED

---

## Experiment History

| Date | Suite | Tests | Outcome |
|:---|:---|:--:|:---|
| 2026-07-17 | — | — | BRANCH INITIALIZED from V5.6 COMPLETE |
| 2026-07-17 | ROCP | — | Protocol designed. Validation axes defined. Failure criteria pre-registered. |
| 2026-07-17 | ROCE | 6 | GATE B (Partially Robust): R1≤B0 at 8/9 N. d→K universal. V9 AMPLIFICATION FALSIFIED at N≥72. F2/F3 triggered. |
| 2026-07-17 | ROCA | 6 | GATE A (Robust suppressor/local amplifier): V9 d-compression FLIPS at N≥72 (ratio 0.18→2.62→10.2). R1 borderline at N=66 is seed-substitution (0 overlap). K→Omega mediation exists only where branch split exists (N≥64). Model A confirmed. |
