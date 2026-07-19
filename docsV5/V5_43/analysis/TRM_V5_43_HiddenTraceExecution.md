# TRM V5.43 Hidden Trace Execution — Analysis

**Version:** 1.0 | **Date:** 2026-07-19
**Suite:** HTE
**Status:** COMPLETE

---

## 1. Summary

HTE_01 collected temporal trace data at 5 checkpoints (T0–T4) for 73 profiles
across N=[65,66,67,70,72,75]. 15 near-identical pairs were matched and their
trajectories compared stage-by-stage.

**Key result: 62.5% of divergence appears only at T4 (c3OmgS measurement).**
The temporal trace does NOT explain the majority of near-identical divergence.
Hidden factor: Model G — Unresolved hidden state.

---

## 2. Trace Availability Audit

| Checkpoint | Omega | lambda1 | d_tail | omDist | reb | c3OmgS |
|:-----------|:-----:|:-------:|:------:|:------:|:---:|:------:|
| T0 (post-warmup) | YES | YES | — | — | — | — |
| T1 (post-compress) | YES | YES | YES | — | — | — |
| T2 (Omega T1) | YES | YES | — | YES | — | — |
| T3 (Omega T2) | YES | YES | — | — | YES | — |
| T4 (c3OmgS) | — | YES | — | — | — | YES |

**Full temporal analysis possible.** All 5 checkpoints available with Omega
and lambda1 coverage throughout.

---

## 3. Near-Identical Pair Trajectories (15 pairs)

| Pair | c3A | c3B | Diverge? | T0ΔOm | T1ΔOm | T2ΔOm | T3ΔOm | FirstDiv |
|-----:|----:|----:|:--------:|------:|------:|------:|------:|:--------:|
| 1 | -0.698 | -0.702 | no | 0.051 | 0.215 | 0.005 | 0.238 | T0 |
| 2 | 0.342 | 0.025 | **YES** | 0.010 | 0.137 | 0.031 | 0.003 | T1 |
| 3 | -0.705 | -0.705 | no | 0.040 | 0.302 | 0.013 | 0.039 | T1 |
| 4 | -0.663 | 0.325 | **YES** | 0.025 | 0.489 | 0.040 | 0.017 | T1 |
| 5 | 1.758 | 1.543 | no | 0.013 | 0.087 | 0.061 | 0.013 | T4 |
| 6 | -0.683 | -0.686 | no | 0.036 | 0.725 | 0.025 | 0.001 | T1 |
| 7 | 1.360 | 0.021 | **YES** | 0.050 | 0.035 | 0.033 | 0.030 | T4 |
| 8 | 0.369 | -0.038 | **YES** | 0.016 | 0.029 | 0.015 | 0.065 | T4 |
| 9 | 1.285 | -0.038 | **YES** | 0.007 | 0.107 | 0.013 | 0.044 | T1 |
| 10 | 0.152 | -0.077 | **YES** | 0.009 | 0.045 | 0.031 | 0.040 | T4 |
| 11 | 0.152 | -0.018 | **YES** | 0.009 | 0.028 | 0.003 | 0.036 | T4 |
| 12 | -0.077 | -0.018 | no | 0.018 | 0.017 | 0.034 | 0.004 | T4 |
| 13 | 0.131 | -0.030 | **YES** | 0.006 | 0.018 | 0.008 | 0.004 | T4 |
| 14 | 0.213 | 0.283 | no | 0.030 | 0.011 | 0.005 | 0.038 | T4 |
| 15 | -0.019 | 0.050 | no | 0.052 | 0.004 | 0.027 | 0.063 | T0 |

**8/15 divergent (53.3%).** First divergence distribution:
- T0 (post-warmup): **0** pairs
- T1 (post-compression): **3** pairs (37.5%)
- T2 (Omega T1): **0** pairs
- T3 (Omega T2): **0** pairs
- T4 (c3OmgS only): **5** pairs (62.5%)

---

## 4. Divergent vs Non-Divergent Trace Comparison

| Metric | Divergent | Convergent | Ratio |
|:-------|----------:|-----------:|------:|
| T0 Omega delta | 0.0165 | 0.0343 | **0.48** |
| T3 Omega delta | 0.0298 | 0.0566 | **0.53** |
| T0 lambda1 delta | 0.0116 | 0.0090 | 1.28 |
| T3 Reb delta | 0.0286 | 0.0729 | **0.39** |

**Counterintuitive finding:** Divergent pairs have SMALLER early Omega differences
(ratio 0.48) and smaller T3 Omega differences (ratio 0.53) than convergent pairs.
Divergent profiles are actually MORE similar at intermediate stages — they diverge
only at the final c3OmgS computation.

---

## 5. Stage-Wise Divergence Map

| Stage | Mean Ω delta | % Div Here | Interpretation |
|:------|------------:|-----------:|:---------------|
| T0 (post-warmup) | 0.025 | 0.0% | No pre-intervention divergence |
| T1 (post-compress) | 0.150 | 37.5% | Compression response divergence |
| T2 (Omega T1) | 0.023 | 0.0% | No post-C3 Omega split at T1 |
| T3 (Omega T2) | 0.042 | 0.0% | No Omega restoration divergence |
| T4 (c3OmgS only) | — | **62.5%** | **Outcome-level only** |

The majority of divergence appears only at the c3OmgS measurement — after all
intermediate trajectory stages have been recorded. The intermediate trajectory
does NOT separate divergent from convergent pairs.

---

## 6. Hidden Response-State Model

**Classification: Model G — Unresolved hidden state.**

The factor that separates near-identical profiles into divergent c3OmgS outcomes
is not visible in the T0–T3 trajectory. It manifests only at T4. This suggests:

- The divergence is in the **C3 correction response itself** (how the profile
  responds to the C3 d-perturbation), not in the pre-C3 state.
- Or the divergence is in the **a0 threshold interaction** (whether Omega T2 > THR),
  which is a binary gate applied at c3OmgS computation.
- Or the divergence is driven by an **unmeasured intermediate** between T3 and T4
  (the C3 application and post-C3 simulation stages).

---

## 7. Causal Closure Update

**Trace explanation PARTIAL.** The temporal trace adds diagnostic depth (identifies
that divergence is late-stage) but does not explain WHY divergence occurs. Causal
closure remains NOT ACHIEVED.

The trace STRENGTHENS the snapshot sufficiency rejection: even with intermediate
trajectory data, near-identical profiles produce divergent outcomes. The hidden factor
is not in the pre-C3 trajectory — it's in the C3 response or threshold interaction.

---

## 8. Gate Summary

| Gate | Description | Status |
|:-----|:------------|:------:|
| A | Trace availability confirmed | REACHED |
| B | First divergence stage identified | REACHED |
| C | Divergence explains c3 split | REACHED (partial, 37.5%) |
| D | Hidden response-state model selected | NOT REACHED — Model G (unresolved) |
| E | Snapshot sufficiency rejection strengthened | REACHED |
| F | Stop-Low preserved | REACHED |
| G | Causal closure improved | REACHED (partial) |
| H | Hidden state still unresolved | NOT REACHED (trace partially explains) |
| I | V6 still not ready | REACHED |

---

## 9. Supported Findings

1. **Temporal trace is available** at 5 checkpoints (T0–T4).
2. **62.5% of divergence appears at T4 only** — after all intermediate stages.
3. **Divergent pairs have SMALLER early Omega differences** than convergent pairs
   (ratio 0.48 at T0, 0.53 at T3). Divergence is not in trajectory drift.
4. **Hidden factor = Model G — Unresolved.** Likely in C3 response or threshold interaction.
5. **Snapshot sufficiency rejection STRENGTHENED** by trace evidence.
6. **Stop-Low operational validity unchanged.**
7. **V6 remains NOT READY.**

## 10. Not Claimed

- Causal closure
- V6 readiness
- Physical interpretation
- Hidden factor identification

## 11. Recommendation

Proceed to **HTA (Hidden Trace Analysis)** for deeper investigation of the C3
response stage and a0 threshold interaction, or to **HTS (Final Synthesis)** if
HTE provides sufficient evidence for V5.43 closure.

---

*Generated 2026-07-19. V5.43 HTE analysis document.*
