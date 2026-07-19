# TRM V5.3 — M1 Fixed Cluster Omega Protocol

**Suite:** V5_3_FixedClusterOmegaProtocol_Tests.cs
**Tag:** FCOP
**Date:** 2026-07-16
**Status:** PROTOCOL DEFINED — AWAITING EXECUTION
**Phase:** V5.3 — Stability Mechanism and Control Parameters
**Branch:** `feature/v5.3-stability-mechanism-and-control-parameters`
**Prerequisite:** N1 NullModelBaseline (COMPLETE — 2/4 signatures reproduced)

---

## A. Purpose

Disambiguate the mechanism behind Omega xi-sensitivity — the signature that N1 showed the null model cannot explain.

**Core question:** Is Omega xi-sensitivity caused by:

| Mechanism | Description | Implication |
|:----------|:------------|:------------|
| **A. Membership-mediated** | Different ξ → different nodes synchronize → different mean natural frequency → Ω shifts | ξ acts through cluster membership, not direct dynamics. H9 weakened. |
| **B. Dynamics-driven** | Same nodes stay synchronized across ξ, but coupling dynamics shift the collective frequency | ξ directly affects synchronization dynamics. H9 conditionally supported. |

M1 disambiguates by computing Omega TWICE per (seed, ξ) pair:
- **Ω_free**: standard TRM Omega over all N nodes
- **Ω_fixed**: Omega computed over ONLY nodes synchronized at baseline ξ=1.80

If Ω_fixed retains the xi-sensitivity → dynamics drive the effect (Mechanism B).
If Ω_fixed loses the xi-sensitivity → membership drives the effect (Mechanism A).

---

## B. N1 Context

N1 established:
- The simplest null model (frequency-threshold selection, no dynamics) reproduces Ω seed-stability and MD ξ-robustness but FAILS to reproduce Ω ξ-sensitivity and MD seed-variability.
- Ω ξ-sensitivity is a candidate attractor-specific residual.
- M1 investigates whether this residual is genuinely dynamics-driven or a membership effect within the TRM attractor itself.

---

## C. Experimental Design

### C.1 Frozen Parameters

| Parameter | Value | Justification |
|:----------|:------|:--------------|
| N | 100 | Matches V5.2 primary regime |
| K₀ | 1.15 | Matches V5.2 primary regime |
| s | 0.08 | Matches V5.2 natural frequency spread |
| ξ baseline | 1.80 | Matches V5.2 primary ξ |
| ξ sweep | {1.50, 1.65, 1.80, 1.95, 2.10} | Matches V5.2 Phase 1 |
| Coupling law | Exponential | Matches primary regime |
| Dt, St, Hd, REps | 0.05, 400, 4, 1e-8 | Matches V5.0/V5.2 simulation config |
| Baseline seeds | 20 (indices 500–519) | Sufficient for reliable cluster identification |
| Sweep seeds per ξ | 5 (indices 520–524) | Matches V5.2 design |
| Cluster threshold | r_mean > 0.70 | Conservative: above noise, below full sync |

### C.2 Cluster Identification

```
1. Run TRM simulation at baseline ξ=1.80.
2. Compute phase correlation matrix R = RP(h).
3. Per-node mean correlation: r_mean[i] = (1/N) · Σ_j R[i,j].
4. Cluster membership[i] = (r_mean[i] > 0.70).
5. Edge case: if < 2 nodes selected → fall back to top 50% by r_mean.
6. SHA-256 hash the membership mask → freeze.
```

### C.3 Omega Computation

| Variant | Formula | Notes |
|:--------|:--------|:------|
| Ω_free(ξ) | mean(Ω_i) over ALL N nodes | Standard TRM Omega |
| Ω_fixed(ξ) | mean(Ω_i) over nodes in baseline cluster | Same node IDs across all ξ |

Both use `OmegaField(h)` — per-node phase rotation rate from simulation history.

### C.4 Sensitivity Comparison

```
ΔΩ_free  = |Ω_free(ξ_max) − Ω_free(ξ_min)|
ΔΩ_fixed = |Ω_fixed(ξ_max) − Ω_fixed(ξ_min)|

ratio = ΔΩ_fixed / max(ΔΩ_free, 0.005)

Ensemble: median ratio across seeds, bootstrap 95% CI.
```

---

## D. Decision Gates

| Gate | Condition | Interpretation | H9 Impact | Next Step |
|:-----|:----------|:---------------|:----------|:----------|
| **A: Dynamics Dominant** | ratio_median > 0.50 AND ΔΩ_free > 0.005 | Membership not the driver. Dynamics dominate. | CONDITIONALLY SUPPORTED | M2 (null-model quantitative comparison) |
| **B: Membership Dominant** | ratio_median < 0.30 AND ΔΩ_free > 0.005 | Membership is the primary driver. | WEAKENED | Revise parameter classification |
| **C: Ambiguous** | 0.30 ≤ ratio ≤ 0.50 OR ΔΩ_free ≤ 0.005 OR wide CI | Cannot cleanly disambiguate. | NOT DISAMBIGUATED | Increase seeds, widen ξ range |

### Gate logic diagram

```
M1 execution
  │
  ├── ΔΩ_free ≤ 0.005?
  │     └── YES → GATE C (no detectable effect — re-examine V5.2)
  │
  ├── ratio_median > 0.50?
  │     └── YES → GATE A (dynamics dominant)
  │
  ├── ratio_median < 0.30?
  │     └── YES → GATE B (membership dominant)
  │
  └── else → GATE C (ambiguous)
```

---

## E. Run Plan

| Phase | Description | Runs |
|:------|:------------|:----:|
| 1 | Baseline cluster identification (20 seeds × ξ=1.80) | 20 |
| 2 | Xi sweep (5 ξ × 5 seeds) | 25 |
| **Total** | | **45** |

Expected runtime: ~5–15 minutes.

---

## F. Forbidden Actions

1. Do NOT change cluster threshold (0.70) after seeing results.
2. Do NOT change baseline ξ (1.80) after membership is frozen.
3. Do NOT reselect seeds.
4. Do NOT recompute baseline membership after xi sweep.
5. Do NOT exclude seeds from ensemble (unless numerical failure).
6. Do NOT change decision thresholds (0.50 / 0.30).
7. Do NOT change computation methods after execution.
8. Do NOT add post-hoc interaction axes.
9. Do NOT claim H9 is confirmed if Gate A triggers.
10. Do NOT claim attractor decomposition based on M1 alone.
11. Do NOT proceed to 210-run matrix before M2/M3/M4.
12. All membership masks must be SHA-256 hashed before xi sweep.

---

## G. Claim Discipline

| Statement | Classification |
|:----------|:--------------|
| The protocol defines computation methods | SUPPORTED |
| The protocol defines decision gates and thresholds | SUPPORTED |
| N1 established 2/4 null-model reproduction | SUPPORTED |
| Results depend on cluster threshold (0.70) | CONDITIONAL |
| Results depend on finite seed ensembles | CONDITIONAL |
| Results are specific to N=100, K₀=1.15, s=0.08, exp coupling | CONDITIONAL |
| Ω xi-sensitivity survives fixed cluster membership | HYPOTHESIS (tested by M1 execution) |
| H9 is confirmed | NOT CLAIMED |
| Attractor decomposition exists | NOT CLAIMED |
| Parameter classes are real | NOT CLAIMED |
| Physical constants are derived | NOT CLAIMED |

---

## H. How to Run

```powershell
# Run the M1 protocol suite (definition only)
dotnet test --filter "FullyQualifiedName~V5_3_FCOP" --no-build -v normal

# Run a specific protocol test
dotnet test --filter "FullyQualifiedName~V5_3_FCOP_07_DecisionGatesDefined" --no-build -v normal
```

**Note:** This is a protocol-definition suite only. No TRM simulations are executed. Execution suite: `V5_3_FixedClusterOmegaExecution_Tests.cs` (FCOE) — not yet implemented.

---

## I. Relationship to V5.3 Experiment Pipeline

```
N1 (COMPLETE)
  │
  │  2/4 signatures reproduced by null model.
  │  2 signatures are attractor-specific residual candidates.
  │
  ├── M1 ← THIS PROTOCOL
  │     │  Targets: Omega xi-sensitivity residual
  │     │  Method:  Fixed-cluster vs free-cluster Omega
  │     │
  │     ├── GATE A (dynamics dominant)
  │     │     └── M2: Null-model quantitative comparison
  │     │           Tests whether TRM Ω(ξ) differs from null Ω(ξ)
  │     │           when cluster sizes are matched.
  │     │
  │     ├── GATE B (membership dominant)
  │     │     └── Revise parameter classification.
  │     │         ξ is a membership-control parameter, not sync-control.
  │     │
  │     └── GATE C (ambiguous)
  │           └── Increase statistical power.
  │               More seeds, wider ξ range.
  │
  └── Future: M3 (saturation boundary)
        Targets: MeanDist seed-variability residual
```

---

*Protocol frozen 2026-07-16. All thresholds and methods pre-registered. This protocol defines the M1 experimental design. Execution requires implementing V5_3_FixedClusterOmegaExecution_Tests.cs with the full TRM simulation pipeline (Sm, RecoverFP, RP, OmegaField from V5.0/V5.2).*
