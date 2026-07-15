# TRM V5.0 — Independent Replication Protocol

**Suite:** V5_0_IndependentReplicationProtocol_Tests.cs  
**Tag:** IRP  
**Branch:** feature/v5.0-independent-replication-and-validation  
**Base:** v4.5-prospective-anchor-prediction-complete  
**Date:** 2026-07-15  
**Status:** PROTOCOL DEFINED

---

## Overview

The Independent Replication Protocol (IRP) defines a fully independent replication of the complete prospective prediction pipeline established in V4.5. An independent replication run must reproduce the TRM prediction workflow **without access** to historical tuning decisions, intermediate calibration values, ad-hoc proxy selections, or post-hoc parameter adjustments.

## Core Principle

The only shared artifact between V4.5 and V5.0 is the **regime definition**: xi=1.80, K0=1.15, N=100, exponential coupling law, dt=0.05, steps=400. Everything else — seeds, graph realizations, prediction values — must be independently generated.

## Replication Phases

| Phase | Suite | Purpose |
|-------|-------|---------|
| 1 | IRP | Protocol definition (this suite) |
| 2 | IRE | Independent prediction generation |
| 3 | IRF | Independent prediction freeze |
| 4 | IRA | Independent prediction audit |
| 5 | IRC | Replication comparison (V5.0 vs V4.5) |
| 6 | IRI | Replication interpretation |

## Allowed Inputs

### Frozen V4.5 Artifacts (read-only)
- V4.5 regime definition: xi=1.80, K0=1.15, N=100, exponential law
- V4.5 frozen prediction hashes (SHA-256, verification only)
- V4.5 comparison governance rules (PACP)
- V4.5 claim discipline categories
- V4.5 pipeline structure (phase ordering)

### Independent Inputs
- Independent random seeds (different from V4.5 seed=45)
- Independent graph realizations (same KS algorithm, different seeds)
- Same simulation parameters: dt=0.05, steps=400, hd=4
- Same computational primitives and proxy definitions

### Allowed Actions
- Read V4.5 frozen predictions (comparison only)
- Read V4.5 governance rules
- Generate independent predictions under V4.5 regime
- Freeze, audit, compare, interpret replication results

## Forbidden Inputs

| Category | Count | Examples |
|----------|:-----:|----------|
| V4.5 Mutation | 6 | Modify frozen predictions, audit hashes, manifests |
| Parameter Tuning | 7 | Adjust xi, K0, N, coupling law, dt, proxies, seed |
| Anchor Reselection | 4 | Reselect Omega, MeanDist, Source anchors |
| Post-hoc Optimization | 6 | Tune to match V4.5, select favorable seed, trim ensemble |
| External Data | 5 | Use physical c/G, astrophysical data, SI mapping |

**Total: 28 forbidden actions.**

## Replication Success Criteria

| ID | Criterion | Type |
|----|-----------|------|
| C1 | Protocol integrity — follow V4.5 phase ordering | Primary |
| C2 | Prediction generation — independent, no V4.5 intermediates | Primary |
| C3 | Freeze integrity — freeze before comparison | Primary |
| C4 | Audit reproducibility — 3-way hash match | Secondary |
| C5 | Comparison execution — all 4 metrics vs V4.5 | Secondary |
| C6 | Anti-feedback — all 14 pathways locked | Secondary |

## Audit Protocol

### Phases
- **A1 — Pre-execution:** Verify V4.5 artifacts intact, seeds differ
- **A2 — Post-generation:** Verify determinism, generate hashes
- **A3 — Post-freeze:** Verify manifest structure, timestamp ordering
- **A4 — Replication:** Verify predictions NOT identical to V4.5

### Record Fields
1. independent_seed
2. generation_timestamp
3. freeze_timestamp
4. prediction_values
5. prediction_hashes (SHA-256)
6. combined_hash
7. v4_5_prediction_hash (reference)
8. audit_classification

## Comparison Protocol (adapted from PACP)

| Class | Condition | Interpretation |
|-------|-----------|----------------|
| REPLICATION-A | rel_err ≤ 1.0 × unc | Pipeline is seed-stable, structurally robust |
| REPLICATION-B | rel_err ≤ 10.0 × unc | Compatible, some seed sensitivity |
| REPLICATION-C | rel_err > 10.0 × unc | Genuine seed/realization sensitivity |
| REPLICATION-REJECT | Protocol violated | Audit failed, V4.5 artifacts modified |

## Claim Discipline

**SUPPORTED:** Protocol fully defined. All criteria, phases, and governance specified.

**CONDITIONAL:** Protocol assumes V4.5 regime. Success depends on seed stability. Non-replication is valid.

**HYPOTHESIS:** H1-H4 concerning seed stability, structural robustness, sensitivity exposure.

**NOT CLAIMED:** Replication executed, replication successful, physical claims, seed-independence, TRM validated.

## Recommended Next Suite

`V5_0_IndependentReplicationExecution_Tests.cs`
