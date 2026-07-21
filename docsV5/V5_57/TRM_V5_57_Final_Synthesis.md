# TRM V5.57 Final Synthesis — Pipeline Origin Audit

**Suite ID:** TSS_01_V5_57_FinalSynthesis
**Version:** 1.0
**Date:** 2026-07-21
**Branch:** `feature/v5.57-artifact-audit`
**Status:** COMPLETE

---

## 1. Executive Determination

**V5.57 asked:** Is the rawIQR/rank preference a genuine SAC discriminator or a pipeline artifact?

**V5.57 answered:** It is a pipeline artifact. The SAC discriminator is **d0** — the mean graph distance after 3 epochs of Sim→RP→Nm→DL→Cupd evolution. rawIQR and within-seed rank are shadow signals that correlate with d0 through the multi-epoch chain but are not direct inputs to SAC classification.

**The d0 variable directly determines P1/P1b** via classifier thresholds: P1 when d0 ∈ [0.50, 0.65], P1b when d0 > 0.65. The rawIQR→d0 correlation is r = −0.03 — essentially zero. The rawIQR-P1 correlation tracked since V5.53 is an emergent epiphenomenon.

---

## 2. Historical Re-evaluation

### V5.53 — SelectAndClassify Predicate Origin

**Original:** rawIQR is the dominant P1/P1b discriminator (~10× normalized dominance).

**V5.57 revision:** rawIQR is a **shadow signal**. The actual discriminator is d0 (SAC internal distance). The rawIQR-P1 correlation is emergent — not direct. The V5.53 finding was **observationally correct** (rawIQR does correlate with P1) but **mechanistically incomplete** (the causal variable is d0, not rawIQR).

### V5.55 — Residual Selection Preference

**Original:** Within-seed rank is the dominant discriminator (1.27σ). Rank-residual decoupling explains SAC selection.

**V5.57 revision:** Rank preference survives permutation (ART_01: 18/20 survivals) — it is a **pipeline artifact**, not a genuine discriminator. The rank signal is a shadow of rawIQR ordering within seeds, which in turn is a shadow of d0. V5.55 findings were **artifact-driven**.

### V5.56 — Relative Gate Preference

**Original:** SAC prefers lowest-rank profiles. Geometry, centrality, winner-take-all rejected.

**V5.57 revision:** The rank preference was real but misinterpreted. It is a downstream consequence of d0 classification, not an independent gate. RGP_01, GEO_01, NBR_01 remain valid as descriptive audits; their interpretation shifts from "gate mechanism" to "artifact characterization."

---

## 3. Evidence Chain

```
Profile generation (rawIQR, rawMean, ...)
        ↓
IsHi filter (Omega-based, rawIQR-NEUTRAL)
        ↓  [delta = 0.00057 — confirmed neutral by PIPE_01]
SAC pipeline:
  Sim → RP → Nm → DL → Cupd (×3 epochs)
        ↓
  RP+DL creates 54% of d-separation (D2_01)
        ↓
  d0 = Dm(final DL output)  [1.74σ structural variable, SV_01]
        ↓
Classifier: d0 ∈ [0.50, 0.65] → P1; d0 > 0.65 → P1b
        ↓
P1/P1b assignment → rawIQR correlation (emergent shadow)
```

**Key finding:** The rawIQR signal is **generated** by this chain, not consumed by it. The 6-operation SAC pipeline (Sim→RP→Nm→DL→Cupd→Dm) takes profile generation inputs and produces d0 values that happen to correlate with rawIQR ordering at the output.

---

## 4. Eliminated Explanations

| Hypothesis | Version | Rejection Evidence |
|:-----------|:--------|:-------------------|
| rawIQR direct mechanism | V5.53 | rawIQR-d0 r=−0.03 (SAC_01) |
| Within-seed rank gate | V5.55 | Permutation survives 18/20 (ART_01) |
| Rank-residual decoupling | V5.55 | d0 effect 1.74σ >> rank 0.41σ (SV_01) |
| Geometry selection | V5.56 | Retained are less isolated (GEO_01) |
| Centrality selection | V5.56 | Retained are NOT centers (NBR_01) |
| Winner-take-all | V5.56 | Retention is distributed (RGS_01) |
| Binary Rank0 gate | V5.56 | Continuous preference, Rank0=39% (RG0_01) |
| d2 as structural variable | V5.57 | d2=0.03σ << d0=1.74σ (SV_01) |

---

## 5. Structural Variable Hierarchy

| Rank | Variable | Effect (σ) | Classification |
|:----:|:---------|-----------:|:---------------|
| 1 | **d0** | **1.74** | **Structural — drives classifier** |
| 2 | residual rawIQR | 0.33 | Shadow signal |
| 3 | d2 (epoch 2) | 0.03–0.85* | Derived signal |
| 4 | rawIQR | 0.11 | Shadow signal |
| 5 | rank | 0.02 | Artifact (permutation test) |

*Note: d2 effect varies by context — 0.85σ on full population (D0_01) but 0.03σ on P1-vs-P1b specifically (SV_01).

---

## 6. Supported Findings

1. d0 is the dominant structural variable (1.74σ). It directly determines P1/P1b via classifier thresholds.
2. rawIQR and d0 are decoupled (r = −0.03). The rawIQR-P1 correlation is emergent, not direct.
3. The SAC pipeline (Sim→RP→Nm→DL→Cupd→Dm) creates the d0 signal progressively.
4. RP+DL creates 54% of d-separation; Cupd contributes 0% within the same epoch (D2_01).
5. Rank preference is a pipeline artifact — survives permutation 18/20 (ART_01).
6. IsHi is rawIQR-neutral (delta = 0.00057). The discriminator operates at SAC, not IsHi (PIPE_01).
7. d0 separation peaks at epoch 2 (d2=0.85σ) but d0 is the final structural variable (SV_01).
8. Geometry, centrality, winner-take-all, Rank0 gate all rejected (GEO_01, NBR_01, RGS_01, RG0_01).
9. Stop-Low remains safe. Zero damage.
10. V6 NOT READY.

---

## 7. Conditional Findings

- Finite-N limits (N=70, 72, 75; 200–300 seeds)
- Classifier dependence (P1/P1b defined by d0 thresholds 0.50 and 0.65)
- Pipeline dependence (SAC operations are fixed; alternative pipelines not tested)
- Sparse SAC retention limits statistical power
- Generator-design dependence (System.Random, uniform weights)
- Diagnostic, not causal

---

## 8. Hypotheses

- d0 may be a proxy for the SAC attractor's internal distance structure.
- Alternative classifier thresholds might reveal different structural variables.
- The RP operation (phase coherence) may be the ultimate source of the d0 signal.
- rawIQR may serve as a pre-filter that shapes the IsHi-pass pool entering SAC.
- Testing with different S, N ranges, or coupling laws may reveal whether d0 dominance generalizes.

---

## 9. Not Claimed

- Causal mechanism for SAC's d0 sensitivity
- Deterministic rescue from d0 signals
- Physical interpretation of d0 (mean graph distance)
- Physical N-boundary
- Universal adaptive control
- Space emergence or distance emergence
- V6 readiness
- Modified M3++
- Modified Stop-Low policy
- Retuned c3OmegaShift threshold
- Physical c, G, GR, spacetime, or cosmology derivation

---

## 10. Recommended Next Frontier

1. **V5.58 — RP Operation Audit:** Is the phase coherence computation (RP) the ultimate source of the d0 discrimination signal? Trace d0 back to its earliest measurable origin in the SAC chain.

2. **V5.58 — Classifier Sensitivity Audit:** How sensitive is the d0→P1/P1b mapping to the d0=0.50/0.65 thresholds? Would alternative thresholds produce different structural variables?

3. **V5.58 — Cross-Regime Validation:** Does d0 remain the dominant structural variable at different N, different S, or different coupling laws?

---

## 11. Commit-Ready Summary

```
TSS_01_V5_57_FinalSynthesis

V5.57 COMPLETE — Pipeline Origin Audit.

The SAC discriminator is d0 (mean graph distance), not rawIQR.
rawIQR and within-seed rank are shadow signals — pipeline artifacts
that emerge from the multi-epoch Sim→RP→Nm→DL→Cupd→Dm chain.

d0 is the structural variable (1.74σ). It directly determines P1/P1b
via classifier thresholds at 0.50 and 0.65.

rawIQR-d0 correlation: r=-0.03 (decoupled).
Rank preference: pipeline artifact (permutation survival 18/20).
RP+DL creates 54% of d-separation; Cupd contributes 0%.

This re-evaluates V5.53 (rawIQR is shadow, not mechanism),
V5.55 (rank is artifact, not discriminator),
and V5.56 (gate is d0 classifier, not rank preference).

Stop-Low safe. Causal closure blocked. V6 NOT READY.

Suites: ART, PIPE, SAC, D0, D2, SV, TSS.
Cumulative: 2928 tests, 0 failed.
```

---

*Generated 2026-07-21. Authoritative V5.57 final synthesis.*
