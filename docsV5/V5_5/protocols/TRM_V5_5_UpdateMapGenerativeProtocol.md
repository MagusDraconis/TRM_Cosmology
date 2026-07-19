# TRM V5.5 — Update-Map Generative Protocol

**Suite:** V5_5_UpdateMapGenerativeProtocol_Tests.cs
**Tag:** UMP
**Date:** 2026-07-16
**Status:** PROTOCOL DEFINED — AWAITING EXECUTION
**Base:** V5.4 COMPLETE (31 V5.4 tests, cumulative 2343, 0 failed)

---

## A. Purpose

Determine which operation in the RecoverFP update cycle generates the distance-driven branch axis identified in V5.4.

---

## B. Update-Map Decomposition

| Stage | Operation | Input → Output | Key Observable |
|:-----:|:----------|:---------------|:---------------|
| A | Sm (Simulation) | K → h, Ω | Ω_mean, Ω_std |
| B | RP (Phase Correlation) | h → R | mean(R), std(R) |
| C | Nm (Normalization) | R → Rn | — |
| **D** | **DL (Distance)** | Rn → d | d_mean, d_std, d_p90 |
| E | Cupd (Coupling Update) | d → K | K_mean, K_std, λ₁(K) |

---

## C. Candidate Generative Mechanisms

| ID | Name | Mechanism | Falsified if |
|:---|:-----|:----------|:-------------|
| GM1 | Distance Amplification | DL log-transform amplifies correlation differences | d_p90 at epoch 1 does not separate branches |
| GM2 | Distance Concentration | Cupd exponential mapping concentrates coupling | d→K produces no branch-separating structure |
| GM3 | Coupling Response | Sm amplifies K differences into Ω | K does not predict next-epoch d |
| GM4 | d↔K Feedback | Iterative cycle amplifies initial differences | Single epoch reproduces branch separation |
| GM5 | Multi-Stage Interaction | Full nonlinear map required | Any single stage fully explains separation |

---

## D. Decision Gates

| Gate | Condition | Interpretation |
|:-----|:----------|:---------------|
| A | Single stage dominates | Branch split is primarily one operation |
| B | Feedback required | Iterative d→K→Sm→R→d loop is necessary |
| C | Multi-stage interaction | Distributed across map; no single dominant stage |
| D | Unresolved | No mechanism survives falsification |

---

## E. Execution Plan

| Phase | Description | Est. Time |
|:------|:------------|:----------|
| 1 | Per-epoch state extraction (150 seeds × 6 Sm) | ~3s |
| 2 | Stage-level separation analysis | ~1s |
| 3 | State-transition trajectory analysis | ~1s |
| 4 | Falsification tests (GM1–GM5) | ~1s |
| 5 | Decision gate application | <1s |

---

## F. Claim Discipline

| Statement | Classification |
|:----------|:--------------|
| Update-map and mechanisms defined | SUPPORTED |
| Falsification criteria pre-registered | SUPPORTED |
| Results depend on decomposition and observables | CONDITIONAL |
| Specific mechanism generates branch axis | HYPOTHESIS |
| Physical interpretation, H9–H12 | NOT CLAIMED |

---

*Protocol frozen 2026-07-16. Execution deferred.*
