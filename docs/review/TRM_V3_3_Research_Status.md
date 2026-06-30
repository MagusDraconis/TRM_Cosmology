# TRM V3.3 Research Status (Exploratory)

## Scope

This note summarizes current **V3.3 exploratory** status for:

- **E2E03–E2E09** (quantum-to-macro / emergent-gravity bridge)
- **RAR17–RAR22** (SPARC residual diagnostics)

All statements below are review-safe: **diagnostic**, **candidate**, or **tested-effective** in bounded regimes, not theorem-level claims.

---

## Executive status

- **Improved / strengthened:** no-refit and holdout discipline is now consistent across E2E and SPARC diagnostics; several candidate signals remain stable under frozen-parameter evaluation.
- **Mixed / failed globally:** not all residual corrections improve global RMS; several effects are regime-dependent and can worsen subsets.
- **Candidate-only areas:** distributed/multi-center geometry and tick-field-driven dynamic corrections remain exploratory and are not main-path physics claims.

---

## E2E03–E2E09 (quantum-to-macro bridge)

| Test | Status | Main outcome | Claim boundary |
|---|---|---|---|
| **E2E03** normalization alignment | tested-effective (diagnostic) | Energy-path and phase-path effective gravity can be aligned via normalization in the tested setup. | Internal consistency check; not a gravity theorem. |
| **E2E04** scaling/resolution sweep | diagnostic | Bounded spread under size/resolution sweeps supports practical scaling stability. | Finite numerical convergence window only. |
| **E2E05** frozen-k transfer | candidate | Single frozen normalization shows useful transfer across nearby configurations. | Pre-holdout transfer; not broad generalization proof. |
| **E2E06** derived corrected-k no-refit | tested-effective candidate | Baseline-derived corrected-k form generalizes in bounded error ranges without per-case refit. | Empirical fitted relation; not first-principles closure. |
| **E2E07** stronger holdout stress | candidate | Some corrected-k families and geometry variants reduce hard holdout errors. | Model-family comparison is exploratory and non-unique. |
| **E2E08** tick fluctuation probe | diagnostic | Frozen models remain bounded under tested fluctuation levels; reveals nonlinear sensitivity structure. | Robustness envelope only, not microscopic fluctuation law. |
| **E2E09** conserved tick matrix dynamics | candidate | Dynamic/local/nonlocal emergent-k variants can improve selected hard cases vs static frozen-k. | Effective-model evidence only; no theorem-level derivation. |

### What improved (E2E)

- Stronger **fit/freeze/holdout** separation.
- Better visibility of where dynamic/tick and geometry terms help versus where they do not.
- Clearer distinction between stable transfer behavior and local overfit risk.

### What failed or remained limited (E2E)

- No single corrected-k/dynamic form dominates all holdout regimes.
- Gains are often concentrated in specific stress cases rather than uniform.
- First-principles closure is still open.

### E2E10–E2E15 tick-phase micro compatibility checks

- E2E10–E2E12 showed **raw tick-phase compatibility** in synthetic setups.
- E2E13 detected a **possible target-adjacency risk** in direct proxy construction.
- E2E14 independent-target guard removed the **zero-error artifact**.
- E2E15 showed tickPhase does **not** improve beyond `radiusOnly` for the independent target channel.
- Interpretation: **diagnostic compatibility only**, not a micro-derivation.

Claim boundaries:
- no theorem-level derivation
- no emergent gravity closure claim
- no baseline activation
- SPARC `PhaseProxy` remains residual diagnostic only

---

## RAR17–RAR22 (SPARC residual diagnostics)

| Test | Status | Main outcome | Claim boundary |
|---|---|---|---|
| **RAR17** gradient-regime gate comparison | diagnostic + candidate | Gradient-based soft gates can outperform ungated/HSB-only in subsets. | Optional residual diagnostic only. |
| **RAR18** disk-edge/surface coupling | diagnostic | Edge/surface proxies correlate with residual behavior, but effect is mixed. | Correlation signal, not causal proof. |
| **RAR19** forced outer/inner contrast gate | tested-effective candidate | Direct outer/inner acceleration-ratio gate is strongest in this forced comparison set. | Diagnostic gate candidate, not baseline model replacement. |
| **RAR20** outer-inner takt synchronization | diagnostic + candidate | Synchronization proxy family shows non-trivial residual structure signal. | Proxy-level evidence only. |
| **RAR21** global disk-coherence scan | diagnostic | Coherence/shear proxies capture part of residual variation, with mixed net impact. | Exploratory proxy scan. |
| **RAR22** worst-galaxy geometry variation | candidate (exploratory) | Distributed/multi-center variants can help specific worst galaxies; smooth distributed field is now train-width-fitted and frozen for evaluation. | Exploratory geometry diagnostics; not core TRM path. |

### RAR32–RAR43 PhaseProxy residual diagnostics

- `rawPhase = omega * radiusKpc` organizes SPARC residuals more effectively than `radiusOnly` and tested normalized phase controls.
- Strongest summary guard (RAR43):
  - `rawPhase delta = 0.017804`
  - `radiusOnly delta = 0.013962`
  - `best normalized phase delta = 0.015825`
  - `improved transfers = 20/20`
  - `train-transfer gap = -0.000143`
- Interpretation: **diagnostic candidate** for a **radial-orbital synchronization** residual structure.
- **Baseline TRM-RAR law remains unchanged:**  
  `g_pred = g_bar + sqrt(g_bar * a0)`

Claim boundaries:
- diagnostic candidate only
- no time-wave proof
- no theorem-level derivation
- no GR replacement
- no production activation

### What improved (SPARC)

- Service-layer diagnostics now consistently enforce **fixed a0** and **no per-mode/per-galaxy refit**.
- Outer/inner acceleration contrast emerged as a stronger regime indicator than simple transition-radius gating.
- Worst-galaxy analysis now includes a smoother distributed-field variant with **train-only kernel-width selection**.

### What failed or remained mixed (SPARC)

- Turning-memory style corrections do **not** uniformly improve global RMS in every mode.
- Some galaxies improve strongly while others worsen.
- Smooth distributed field currently improves over toy multi-center in many cases but is not uniformly better than single-center baseline.

---

## Candidate-only items (not main model path)

- Turning-memory residual correction modes (`BinBased`, `Interpolated`, and gated variants).
- Disk-edge/surface and global coherence proxy gates.
- Outer-inner takt synchronization proxies.
- Worst-galaxy distributed/multi-center/smooth takt-field geometry variants.

These remain optional diagnostics and are intentionally outside the core TRM-RAR baseline path.

---

## Explicit claim boundaries

1. **No theorem-level claim** is made for emergent gravity closure from E2E03–E2E09.
2. **No theorem-level claim** is made for SPARC residual corrections in RAR17–RAR22.
3. Current status is: **diagnostic evidence**, **candidate mechanisms**, and selected **tested-effective** behaviors in bounded test regimes.
4. Baseline TRM model path and fitted baseline parameters remain distinct from exploratory correction layers.

---

## Recommended next tests

1. **Nested split robustness for RAR19/RAR22:** multiple deterministic train/holdout partitions for gate/kernel stability maps.
2. **Ablation matrix for RAR22 smooth field:** kernel family, width grid density, blend-strength sensitivity, and regularization penalties.
3. **Failure-cluster analysis:** characterize galaxies that consistently worsen (outer/inner ratio, gas dominance, span, sampling density, profile shape).
4. **Cross-diagnostic transfer test:** train gate/kernel in one split family, evaluate on disjoint split family without retuning.
5. **Uncertainty-aware reporting:** bootstrap confidence bands for delta-RMS and proxy correlations.
6. **Model-selection guardrails:** require consistent gain across splits/subgroups before any candidate graduates from exploratory to tested-effective.
