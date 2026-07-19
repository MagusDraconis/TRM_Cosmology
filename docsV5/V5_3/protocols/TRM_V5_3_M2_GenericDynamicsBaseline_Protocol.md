# TRM V5.3 — M2 Generic Dynamics Baseline Protocol

**Suite:** V5_3_GenericDynamicsBaselineProtocol_Tests.cs
**Tag:** GDBP
**Date:** 2026-07-16
**Status:** PROTOCOL DEFINED — AWAITING EXECUTION
**Prerequisites:** N1 (COMPLETE), M1 (EXECUTED — Gate A, ΔΩ_fixed = 1.629)

---

## A. Purpose

Determine whether the M1 residual Omega-xi sensitivity is **specific to TRM dynamics** (multi-epoch RecoverFP) or **generic to any Kuramoto system** with distance-dependent coupling.

**Core question:** Does the iterative fixed-point recovery (RecoverFP) produce Ω-ξ behavior that differs from simple Kuramoto dynamics with fixed graph-distance coupling?

---

## B. Generic Baseline Definition

### B.1 TRM Pipeline (M1)

```
KS(N, seed) → RecoverFP(E epochs) → Sm(K_fp) → OmegaField(h)
                 │
                 └── for e = 0..E-1:
                       h = Sm(K, ...)
                       d = DL(Nm(RP(h)))    ← emergent distances
                       K = Cupd(d, K0, xi)
```

### B.2 Generic Baseline (M2)

```
KS(N, seed) → BFS distances → Cupd(d_graph, K0, xi) → Sm(K) → OmegaField(h)
```

### B.3 Differences

| Aspect | TRM (M1) | Generic (M2) |
|:-------|:---------|:-------------|
| Epochs | E = 5 (RecoverFP) | 1 (no recovery) |
| Distance source | Phase-correlation emergent | Fixed graph topology |
| Coupling update | Iterative (Sm → DL → Cupd) | Single (BFS → Cupd) |
| Simulations per run | E+1 = 6 | 1 |

### B.4 Identical

| Aspect | Both |
|:-------|:-----|
| Graph generation | KS(N, seed) |
| Natural frequencies | w[i] = 1.0 + s * (U-0.5) * 2 |
| Coupling kernel | K_ij = K₀ * exp(-d_ij / xi) |
| Kuramoto dynamics | dθ_i/dt = ω_i + Σ K_ij * sin(θ_j - θ_i) |
| Omega computation | OmegaField(h) |
| Frozen cluster masks | M1 baseline masks (SHA-256 frozen) |

---

## C. Measurement Pipeline

For each (seed=s, xi) in {520..524} × {1.50, 1.65, 1.80, 1.95, 2.10}:

1. Generate graph: `adj = KS(N=100, seed=s)` (identical to M1's graph)
2. Compute BFS shortest-path distances on `adj`
3. Build coupling: `K[i,j] = K₀ * exp(-d_ij / xi)` (K[i,i] = 0)
4. Run simulation: `h = Sm(K, N, s, seed=s)`
5. Compute per-node Omega: `om = OmegaField(h)`
6. Load M1 frozen cluster mask for seed `s`
7. Compute `Ω_fixed = mean(om[i] for i where mask[i])`
8. Compute `Ω_free = mean(om)` (all nodes)

---

## D. Comparison

| Metric | Source | Value |
|:-------|:-------|:-----|
| ΔΩ_fixed_trm | M1 execution | 1.629 |
| ΔΩ_fixed_gen | M2 execution | TBD |

```
ratio_gen = ΔΩ_fixed_gen / ΔΩ_fixed_trm
residual = ΔΩ_fixed_trm − ΔΩ_fixed_gen
normalized_residual = residual / ΔΩ_fixed_trm
```

---

## E. Decision Gates

| Gate | Condition | Interpretation | H9 Impact | Next |
|:-----|:----------|:---------------|:----------|:-----|
| **A: Generic Dominant** | ratio ≥ 0.70 | Ω-ξ is generic Kuramoto behavior, not TRM-specific | CONDITIONAL, less distinctive | M3 |
| **B: TRM Residual** | ratio ≤ 0.30 | Ω-ξ is TRM-specific; RecoverFP matters | STRONGER CONDITIONAL | M3 |
| **C: Mixed** | 0.30 < ratio < 0.70 | Both generic and TRM-specific components | CONDITIONAL with caveats | Characterize residual fraction |
| **D: No Effect** | ΔΩ_fixed_trm ≤ 0.005 | Contradicts M1 — measurement error | Re-examine M1 | — |

---

## F. Run Plan

| Phase | Description | Runs |
|:------|:------------|:----:|
| 1 | Xi sweep (5 xi × 5 seeds, single Sm each) | 25 |
| **Total** | | **25** |

Expected runtime: ~2–3 seconds (1 Sm per run vs 6 in M1).

---

## G. Forbidden Actions

1. Do NOT change generic baseline definition after execution.
2. Do NOT change decision thresholds (0.70/0.30).
3. Do NOT reselect seeds.
4. Do NOT recompute M1 cluster masks.
5. Do NOT add RecoverFP epochs to generic baseline post-hoc.
6. Do NOT claim H9 confirmed regardless of outcome.
7. Do NOT claim attractor decomposition.

---

## H. Claim Discipline

| Statement | Classification |
|:----------|:--------------|
| Generic baseline is structurally defined | SUPPORTED |
| Generic baseline differs from TRM in RecoverFP only | SUPPORTED |
| Decision thresholds are pre-registered | SUPPORTED |
| Results depend on specific generic baseline choice | CONDITIONAL |
| Results depend on M1 cluster masks | CONDITIONAL |
| Generic dynamics may explain the M1 residual | HYPOTHESIS |
| H9 confirmed | NOT CLAIMED |
| Attractor decomposition | NOT CLAIMED |

---

## I. Relationship to Experiment Pipeline

```
N1 ──► M1 ──► M2 ← THIS PROTOCOL
                 │
                 ├── GATE A (generic) → M3 (MeanDist saturation)
                 ├── GATE B (TRM-specific) → M3
                 └── GATE C (mixed) → Characterize residual

Future: M3 (saturation boundary) — tests H10, H11
        M4 (latent variable) — tests H9-H11 jointly
```

---

*Protocol frozen 2026-07-16. M1 reference value (ΔΩ_fixed_trm = 1.629) is immutable. Thresholds pre-registered. Execution requires implementing V5_3_GenericDynamicsBaselineExecution_Tests.cs.*
