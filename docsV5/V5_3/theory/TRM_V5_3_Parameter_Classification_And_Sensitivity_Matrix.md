# TRM V5.3 — Parameter Classification and Sensitivity Matrix

**Status:** ANALYSIS — RESEARCH PREPARATION
**Date:** 2026-07-16
**Scope:** V5.3 stability-mechanism investigation
**Base:** V5.2 corrected stability picture (23 regime points, 69 runs)
**Tests referenced:** 2189 (V4.1–V5.2), 0 failed

---

## SECTION 1: Current Supported State

### 1.1 What is established (SUPPORTED)

These statements are backed by completed pipeline evidence. No mechanism is claimed.

| # | Finding | Evidence |
|:--|:--------|:---------|
| S1 | Omega is seed-stable (CV ~0.01 within a regime) | V5.1 10-seed ensemble, V5.2 69-run confirmation |
| S2 | Omega is xi-regime-sensitive (shifts when ξ changes) | V5.2 Phase 1, 5-point ξ sweep, 15 runs |
| S3 | MeanDist is seed-variable (CV ~0.30 within a regime) | V5.1 10-seed ensemble, V5.2 confirmation |
| S4 | MeanDist is xi-regime-robust (persists across ξ changes) | V5.2 Phase 1, 5-point ξ sweep, 15 runs |
| S5 | Seed stability and regime stability are distinct dimensions | V5.2 central finding |
| S6 | c_eff is moderately stable across both dimensions | V5.2 both Phases 1–5 |
| S7 | G_eff is structurally variable (seed) and moderately stable (regime) | V5.2 Phases 1–5 |
| S8 | The coupling law is exponential in the primary regime | V4.1 convergence state |
| S9 | The 5-axis parameter space (ξ, K₀, s, N, law) has been partially explored | V5.2 Phases 1–5 |
| S10 | Omega is statistically independent of MeanDist within a regime (ρ ~0) | V4.1 OGG suite, 14 tests |

### 1.2 What is conditional (CONDITIONAL)

These hold under specified conditions and may not generalize.

| # | Statement | Conditions |
|:--|:----------|:-----------|
| C1 | All regime sensitivity results are from 3-seed ensembles per point | 3 seeds may under-sample the distribution |
| C2 | The corrected V5.2 stability picture is primarily from the ξ sweep | K₀, s, N, law sweeps used different reference points |
| C3 | Omega and MeanDist are proxy definitions | Alternative proxies may yield different classifications |
| C4 | All results are at finite N (80–800) | Continuum limit not characterized |
| C5 | Exponential coupling dominates the tested regimes | Gaussian and power-law only at 3 points each |

### 1.3 What is hypothesized (HYPOTHESIS)

These are the V5.3 mechanism hypotheses from the project roadmap.

| ID | Hypothesis | Status |
|:---|:-----------|:-------|
| H9 | Omega regime-sensitivity is driven by synchronization-control parameters (ξ, K₀), not geometry-control parameters | UNTESTED |
| H10 | MeanDist regime-robustness is driven by geometry-control parameters invariant under ξ changes | UNTESTED |
| H11 | Synchronization-control and geometry-control parameters form distinct, partially independent parameter subsets | UNTESTED |
| H12 | The observed stability split reflects a fundamental attractor decomposition | UNTESTED |

### 1.4 What is NOT CLAIMED

- No mechanism for Omega ξ-sensitivity is claimed.
- No mechanism for MeanDist ξ-robustness is claimed.
- No causal relationship between any parameter and any metric is claimed.
- No physical interpretation is claimed.
- No derivation from first principles is claimed.
- The parameter classification below is a **working classification for experimental design**, not a theoretical claim.

---

## SECTION 2: Candidate Control Parameters

### 2.1 Parameter Inventory

The TRM synchronization model is:

```
dθ_i/dt = ω_i + Σ_j K_ij · sin(θ_j − θ_i)
```

where K_ij = K₀ · f(d_ij / ξ) defines the coupling kernel, ω_i ~ N(ω̄, s²) are natural frequencies, and d_ij are graph distances on the underlying lattice (fixed topology, N nodes).

The following parameters are exposed to experimental control:

| Parameter | Symbol | Type | Domain | Primary regime |
|:----------|:------:|:-----|:-------|:--------------:|
| Coupling length scale | ξ | Continuous | [0.5, 5.0] | 1.80 |
| Base coupling strength | K₀ | Continuous | [0.1, 3.0] | 1.15 |
| Natural frequency spread | s | Continuous | [0.0, 0.50] | 0.08 |
| Node count | N | Discrete | {10, 20, …, 1000} | 100 |
| Coupling kernel law | law | Categorical | {Exp, Gauss, Pow} | Exp |
| Random seed | seed | Discrete | ℤ⁺ | per-run |
| Natural frequency mean | ω̄ | Continuous | Implicit (normally 0) | 0 |
| Integration time step | Δt | Continuous | Technical | fixed |
| Total integration steps | T | Discrete | Technical | fixed |

**Excluded from experimental design:** Δt and T are numerical parameters, not physical control parameters. ω̄ is normally fixed at 0 (symmetric distribution) and is held constant.

### 2.2 How Omega is Computed

Omega = mean(|θ_i(t+Δt) − θ_i(t)|) / Δt, averaged over the synchronized oscillator population and over the steady-state window. It is the collective phase-rotation rate.

**Structural dependencies (from Kuramoto theory, observed in V4.1/V5.2):**

- Omega ≈ ω̄ + bias_correction(K_ij, {ω_i})
- The bias correction depends on the asymmetry of the natural frequencies of synchronized oscillators weighted by coupling strengths.
- In a symmetric distribution (ω̄ = 0), the bias is zero only if the coupling is fully symmetric. Distance-dependent coupling breaks symmetry for finite graphs.
- The set of synchronized oscillators depends on ξ, K₀, s, N, and law.

**This implies:** Omega is a function of both the synchronization state (who syncs) AND the coupling structure (how strongly they couple).

### 2.3 How MeanDist is Computed

MeanDist = mean(d_ij) over pairs (i, j) where both oscillators belong to the synchronized cluster, where d_ij is the graph distance on the fixed lattice.

**Structural dependencies:**

- MeanDist depends on WHICH oscillators synchronize (their positions on the lattice).
- The underlying lattice distances d_ij are FIXED — they do not change with ξ, K₀, s, or law.
- Changing ξ changes coupling strengths but NOT the underlying distances.

**This implies:** MeanDist is a function of synchronization-cluster MEMBERSHIP (who syncs) but NOT of coupling STRENGTHS (how strongly). The underlying geometry (lattice structure, N) is fixed. This is a candidate explanation for the ξ-robustness of MeanDist.

### 2.4 Per-Parameter Analysis

#### ξ — Coupling Length Scale

| Property | Assessment |
|:---------|:-----------|
| Affects coupling range | YES — exponential falloff scale |
| Affects synchronization membership | YES — wider coupling → more oscillators within range |
| Affects collective frequency bias | YES — changes which ω_i contribute to the effective mean |
| Affects underlying graph distances | NO — graph topology is fixed |
| V5.2 evidence for Omega | REGIME SENSITIVE |
| V5.2 evidence for MeanDist | HIGHLY STABLE (robust) |

**Why Omega is ξ-sensitive:** As ξ increases, the effective coupling radius expands. More oscillators participate in the synchronized cluster. The mean natural frequency of the synchronized population shifts (unless the natural frequencies are perfectly uniform in space, which they are not due to random assignment). This shifts Omega.

**Why MeanDist is ξ-robust:** ξ changes coupling strengths but NOT graph distances. If the synchronized cluster membership is only weakly dependent on ξ (because synchronization is driven more by natural-frequency similarity than by coupling strength, once above a threshold), then MeanDist barely changes. The robustness suggests that the spatial extent of the synchronized cluster is determined by the underlying lattice topology and the natural frequency spatial distribution, not by ξ. ξ mainly determines HOW STRONGLY already-connected oscillators couple, not WHICH oscillators couple.

**Classification: MIXED — primarily synchronization-control, weakly geometry-control through cluster membership.**

---

#### K₀ — Base Coupling Strength

| Property | Assessment |
|:---------|:-----------|
| Affects coupling magnitude | YES — global multiplicative factor on K_ij |
| Affects synchronization threshold | YES — below K_c, no synchronization |
| Affects collective frequency bias | YES — above threshold, bias scales with coupling asymmetry |
| Affects synchronization membership | YES — in partially synchronized regimes |
| V5.2 evidence for Omega | Moderately sensitive (Phase 2) |
| V5.2 evidence for MeanDist | Not directly characterized in ξ-independent terms |

**Why K₀ may affect Omega:** Stronger coupling pulls the collective frequency toward a weighted mean of natural frequencies. The bias correction term scales with K₀ (for fixed topology).

**Why K₀ may affect MeanDist:** Stronger coupling may bring more weakly-coupled oscillator pairs into the synchronized cluster, potentially changing MeanDist. However, this effect is mediated through cluster membership, not through the underlying geometry.

**Classification: MIXED — primarily synchronization-control. Affects geometry only through cluster membership changes.**

---

#### s — Natural Frequency Spread

| Property | Assessment |
|:---------|:-----------|
| Affects synchronization possibility | YES — above critical spread, synchronization breaks |
| Affects collective frequency | YES — changes the variance of ω_i, affecting bias correction |
| Affects synchronization membership | YES — wider spread may exclude outliers from the cluster |
| Affects underlying graph distances | NO |
| V5.2 evidence for Omega | Moderately sensitive (Phase 3) |
| V5.2 evidence for MeanDist | Moderately to highly sensitive (Phase 3) |

**Why s may affect Omega:** Changes the distribution of ω_i among synchronized oscillators. This changes the weighted-mean correction that determines Ω*.

**Why s may affect MeanDist:** If oscillators with extreme natural frequencies are excluded from the synchronized cluster (because they are too far from the mean to be entrained), the spatial extent of the synchronized cluster shrinks. This changes MeanDist.

**Classification: MIXED — affects synchronization directly, affects geometry through cluster membership filtering.**

---

#### N — Node Count

| Property | Assessment |
|:---------|:-----------|
| Affects graph topology | YES — determines the underlying distance structure |
| Affects finite-size corrections | YES — larger N reduces finite-size fluctuations |
| Affects Omega | Indirectly — through finite-size effects on synchronization |
| Affects MeanDist | YES — directly changes the available distance range |
| V5.2 evidence for Omega | Moderately sensitive (Phase 4) |
| V5.2 evidence for MeanDist | Moderately to highly sensitive (Phase 4) |

**Why N may affect Omega:** Finite-size fluctuations in the synchronization order parameter. As N → ∞, these fluctuations vanish. The effective mean natural frequency of the synchronized cluster converges.

**Why N may affect MeanDist:** Directly. Larger N means larger possible graph distances. MeanDist is bounded above by the graph diameter, which scales with N. This is a pure geometric effect independent of synchronization.

**Classification: MIXED — primarily geometry-control (affects the available distance range). Weakly synchronization-control through finite-size effects.**

---

#### Coupling Law

| Property | Assessment |
|:---------|:-----------|
| Affects coupling kernel shape | YES — determines K_ij(d_ij) functional form |
| Affects synchronization membership | YES — different kernels define different effective neighborhoods |
| Affects Omega | YES — changes the effective coupling network |
| Affects MeanDist | YES — changes which oscillator pairs are in the cluster |
| V5.2 evidence for Omega | Moderately sensitive (Phase 5) |
| V5.2 evidence for MeanDist | Moderately to highly sensitive (Phase 5) |

**Why law may affect Omega:** Different kernels produce different effective coupling topologies. A power-law kernel has longer tails than an exponential one, coupling more distant oscillators. This changes the synchronization structure and the collective frequency.

**Why law may affect MeanDist:** The kernel shape determines which oscillators at which distances contribute to synchronization. Longer-range kernels (power-law) may produce clusters with larger MeanDist than short-range kernels (Gaussian).

**Classification: MIXED — affects both synchronization structure and geometric cluster extent.**

---

#### Seed — Random Seed

| Property | Assessment |
|:---------|:-----------|
| Affects natural frequency assignment | YES — determines which ω_i goes to which node |
| Affects Omega | NO — seed-stable (CV ~0.01) |
| Affects MeanDist | YES — seed-variable (CV ~0.30) |
| Affects underlying graph distances | NO — topology is fixed |

**Why seed does NOT affect Omega:** The collective frequency is determined by the ensemble of natural frequencies that synchronize, which is a robust population-level statistic. Different seeds reshuffle natural frequencies but preserve the distribution, so the mean of the synchronized subset is stable.

**Why seed DOES affect MeanDist:** The synchronized cluster membership is sensitive to the spatial arrangement of natural frequencies. Different seeds place different ω_i at different positions, changing which specific oscillators synchronize. This changes the spatial extent of the synchronized cluster and therefore MeanDist.

**Classification: GEOMETRY-CONTROL — affects cluster membership and therefore geometry, but not the collective frequency. Seed is the canonical example of a pure geometry-control parameter.**

---

### 2.5 Classification Summary

| Parameter | Primary Classification | Omega Sensitivity | MeanDist Sensitivity | Confidence |
|:----------|:----------------------|:-----------------:|:--------------------:|:----------:|
| ξ | SYNCHRONIZATION-CONTROL | SENSITIVE | ROBUST | HIGH (V5.2 data) |
| K₀ | SYNCHRONIZATION-CONTROL (primary) | MODERATE | INDIRECT (through membership) | MODERATE |
| s | MIXED | MODERATE | MODERATE–HIGH | MODERATE |
| N | GEOMETRY-CONTROL (primary) | MODERATE (finite-size) | HIGH (direct) | HIGH |
| law | MIXED | MODERATE | MODERATE–HIGH | MODERATE |
| seed | GEOMETRY-CONTROL (pure) | NONE | HIGH | HIGH (V5.2 data) |

**Key structural insight:** The classification is based on the MECHANISM of influence:

- **Synchronization-control parameters** affect Omega by changing the synchronization dynamics (coupling structure, frequency distribution) — they act on the `Σ_j K_ij · sin(θ_j − θ_i)` term and the `ω_i` distribution.
- **Geometry-control parameters** affect MeanDist by changing either the underlying graph distances (N) or which nodes participate in the synchronized cluster (seed, s, law) — they act on the `d_ij` structure or cluster membership.
- ξ is classified as synchronization-control despite affecting cluster membership because its primary V5.2 signal is on Omega (regime-sensitive).

---

## SECTION 3: Sensitivity Matrix Design

### 3.1 Design Principles

1. **One-axis sweeps** — vary one parameter at a time, hold others at primary regime.
2. **Multi-seed ensembles per point** — minimum 5 seeds (up from 3 in V5.2) to better characterize seed-variance.
3. **Symmetric sampling** — equal number of points above and below primary value.
4. **Reference anchoring** — primary regime (ξ=1.80, K₀=1.15, s=0.08, N=100, exp) serves as the comparison baseline.
5. **Two-metric output per run** — Ω and MeanDist computed from the same simulation, plus derived metrics (c_eff, G_eff) for completeness.

### 3.2 Frozen Grid — V5.3 Sensitivity Matrix

#### Phase SM1: ξ Sweep (7 points × 5 seeds = 35 runs)

| Parameter | Values | Held Constant |
|:----------|:-------|:--------------|
| ξ | {1.40, 1.55, 1.70, 1.80, 1.90, 2.05, 2.20} | K₀=1.15, s=0.08, N=100, exp |
| seeds | {300, 301, 302, 303, 304} per point | — |

**Rationale:** Finer sampling than V5.2 (7 vs 5 points) to better characterize the Omega-ξ functional relationship and detect any threshold behavior. Expanded range to probe the SAFE-BOUNDARY edge.

#### Phase SM2: K₀ Sweep (7 points × 5 seeds = 35 runs)

| Parameter | Values | Held Constant |
|:----------|:-------|:--------------|
| K₀ | {0.80, 0.95, 1.05, 1.15, 1.25, 1.35, 1.50} | ξ=1.80, s=0.08, N=100, exp |
| seeds | {310, 311, 312, 313, 314} per point | — |

#### Phase SM3: s Sweep (7 points × 5 seeds = 35 runs)

| Parameter | Values | Held Constant |
|:----------|:-------|:--------------|
| s | {0.02, 0.05, 0.08, 0.11, 0.14, 0.18, 0.22} | ξ=1.80, K₀=1.15, N=100, exp |
| seeds | {320, 321, 322, 323, 324} per point | — |

#### Phase SM4: N Sweep (6 points × 5 seeds = 30 runs)

| Parameter | Values | Held Constant |
|:----------|:-------|:--------------|
| N | {50, 80, 100, 200, 500, 1000} | ξ=1.80, K₀=1.15, s=0.08, exp |
| seeds | {330, 331, 332, 333, 334} per point | — |

**Rationale:** Added N=50 and N=1000 to extend the V5.2 range.

#### Phase SM5: Law Sweep (3 points × 5 seeds = 15 runs)

| Parameter | Values | Held Constant |
|:----------|:-------|:--------------|
| law | {Exponential, Gaussian, Power-law} | ξ=1.80, K₀=1.15, s=0.08, N=100 |
| seeds | {340, 341, 342, 343, 344} per point | — |

#### Phase SM6: Seed-Only Baseline (20 seeds = 20 runs)

| Parameter | Values | Held Constant |
|:----------|:-------|:--------------|
| seed | {350, 351, …, 369} | ξ=1.80, K₀=1.15, s=0.08, N=100, exp |

**Rationale:** Larger seed ensemble to precisely characterize the baseline seed-CV for both Ω and MeanDist. This is the null hypothesis: what does "no regime change" look like?

#### Phase SM7: Two-Axis Interaction (8 points × 5 seeds = 40 runs)

| Parameter | Values | Held Constant |
|:----------|:-------|:--------------|
| (ξ, K₀) | {1.70, 1.90} × {1.05, 1.25} (4 points) | s=0.08, N=100, exp |
| (ξ, s) | {1.70, 1.90} × {0.05, 0.11} (4 points) | K₀=1.15, N=100, exp |
| seeds | {370, 371, 372, 373, 374} per point | — |

**Rationale:** The minimal two-axis interaction test. If ξ and K₀ effects on Ω are independent (additive), H11 is strengthened. If they interact, the parameter subsets are not cleanly separable.

### 3.3 Total Grid

| Phase | Points | Seeds/Point | Runs |
|:------|------:|:-----------:|:----:|
| SM1 (ξ) | 7 | 5 | 35 |
| SM2 (K₀) | 7 | 5 | 35 |
| SM3 (s) | 7 | 5 | 35 |
| SM4 (N) | 6 | 5 | 30 |
| SM5 (law) | 3 | 5 | 15 |
| SM6 (seed) | 1 | 20 | 20 |
| SM7 (2-axis) | 8 | 5 | 40 |
| **Total** | **39** | — | **210** |

### 3.4 Per-Run Metrics

Every run computes:

| Metric | Symbol | Definition |
|:-------|:------:|:-----------|
| Collective frequency | Ω | mean(│Δθ_i│)/Δt over steady state |
| Mean distance | MD | mean(d_ij) over synchronized pairs |
| Derived speed | c_eff | Ω · MD · scaling_factor |
| Derived coupling | G_eff | α_TRM · MD³ / (scaling_factor²) |
| Synchronization fraction | r_sync | fraction of oscillators in synchronized cluster |
| Cluster diameter | D_max | max(d_ij) over synchronized pairs |

### 3.5 Derived Sensitivity Metrics

For each parameter sweep, compute:

| Metric | Definition | Purpose |
|:-------|:-----------|:--------|
| ΔΩ/Δparam | Finite-difference sensitivity | Quantify Omega response to parameter |
| ΔMD/Δparam | Finite-difference sensitivity | Quantify MeanDist response to parameter |
| CV_Ω(param) | Coefficient of variation across sweep points | Regime sensitivity of Omega |
| CV_MD(param) | Coefficient of variation across sweep points | Regime sensitivity of MeanDist |
| CV_Ω(seed) | Baseline seed-CV at primary regime | Seed stability of Omega |
| CV_MD(seed) | Baseline seed-CV at primary regime | Seed stability of MeanDist |
| ρ(Ω, MD) | Correlation across sweep points | Detect coupled vs independent responses |

---

## SECTION 4: Hypothesis Tests

### 4.1 H9: Omega sensitivity is primarily controlled by synchronization parameters

**Formal statement:** ΔΩ/Δ(sync_param) > ΔΩ/Δ(geom_param) for all synchronization parameters vs all geometry parameters, measured as normalized sensitivity.

**Operationalization:**

```
H9_SUPPORTED if:
   mean(|∂Ω/∂ξ|_norm, |∂Ω/∂K₀|_norm, |∂Ω/∂s|_norm) >
   mean(|∂Ω/∂N|_norm, |∂Ω/∂seed|_norm)
   AND this inequality holds at p < 0.05 (Wilcoxon rank-sum on bootstrap samples)

H9_FALSIFIED if:
   |∂Ω/∂N|_norm > |∂Ω/∂ξ|_norm  (a geometry parameter drives Omega more than a sync parameter)
   OR |∂Ω/∂seed|_norm > |∂Ω/∂K₀|_norm  (seed drives Omega more than coupling strength)
   OR any single geometry parameter has |∂Ω/∂param| > max(|∂Ω/∂sync_param|)

H9_AMBIGUOUS if:
   Neither criterion met (mixed signals, borderline p-values)
```

**Normalization:** ∂Ω/∂param is normalized by the parameter range span and the baseline Ω value:

```
|∂Ω/∂param|_norm = |ΔΩ / (Ω_baseline)| / |Δparam / param_range|
```

This allows cross-parameter comparison on a common scale.

### 4.2 H10: MeanDist robustness is primarily controlled by geometry parameters

**Formal statement:** MeanDist is more sensitive to geometry-control parameters (N, seed, law) than to synchronization-control parameters (ξ, K₀, s), measured as cross-parameter CV ratio.

**Operationalization:**

```
H10_SUPPORTED if:
   CV_MD(seed) / CV_MD(ξ) > 3.0   (seed variation >> xi variation)
   AND CV_MD(N) / CV_MD(ξ) > 3.0   (N variation >> xi variation)
   AND CV_MD(ξ) < 0.05             (xi variation is genuinely small)

H10_FALSIFIED if:
   CV_MD(ξ) > 0.10                 (xi drives substantial MeanDist variation)
   OR CV_MD(ξ) > CV_MD(seed)       (xi variation exceeds seed variation — contradicts V5.2)
   OR |∂MD/∂ξ|_norm > |∂MD/∂N|_norm  (xi sensitivity exceeds N sensitivity)

H10_AMBIGUOUS if:
   CV_MD(ξ) is between 0.05 and 0.10 with borderline significance
```

### 4.3 H11: Synchronization-control and geometry-control parameters form partially independent classes

**Formal statement:** The parameter-to-metric sensitivity matrix has a block-diagonal structure where synchronization parameters show stronger coupling to Omega and geometry parameters show stronger coupling to MeanDist, with weak cross-talk.

**Operationalization:**

The sensitivity matrix S has entries S[i, j] = normalized sensitivity of metric j to parameter i.

```
H11_SUPPORTED if:
   1. S[ξ, Ω] >> S[ξ, MD]    (xi affects Omega much more than MeanDist)
   2. S[N, MD] >> S[N, Ω]    (N affects MeanDist much more than Omega)
   3. S[seed, MD] >> S[seed, Ω]  (seed affects MeanDist much more than Omega)
   4. The off-diagonal dominance ratio > 2.0 for at least 3 of 4 tested pairs

   The off-diagonal dominance ratio for parameter i is:
   ratio_i = |S[i, primary_target]| / |S[i, secondary_target]|

H11_FALSIFIED if:
   S[ξ, MD] > S[ξ, Ω]       (xi affects geometry more than sync — contradicts the sync-control classification)
   OR S[N, Ω] > S[N, MD]    (N affects sync more than geometry)
   OR S[K₀, MD] > S[K₀, Ω]  (K₀ affects geometry more than sync — contradicts sync-control classification)

H11_AMBIGUOUS if:
   Ratios are between 1.0 and 2.0 for most pairs (weak separation, no clear block structure)
```

### 4.4 H12: The observed stability split reflects attractor decomposition

**Formal statement:** The orthogonal stability dimensions (seed-stable/regime-sensitive for Omega; seed-variable/regime-robust for MeanDist) persist across all tested coupling laws and N values — they are not artifacts of the exponential coupling or N=100.

**Operationalization:**

```
H12_SUPPORTED if:
   1. Omega is seed-stable (CV_seed < 0.05) for ALL tested laws and ALL N ≥ 80
   2. Omega is regime-sensitive to ξ (CV_xi > 3× CV_seed) for ALL tested laws and ALL N ≥ 80
   3. MeanDist is seed-variable (CV_seed > 0.15) for ALL tested laws and ALL N ≥ 80
   4. MeanDist is regime-robust to ξ (CV_xi < CV_seed) for ALL tested laws and ALL N ≥ 80
   5. The (seed, regime) stability quadrant pattern is preserved across at least 2 of 3 laws

H12_FALSIFIED if:
   Any law reverses the stability pattern (Omega becomes seed-variable OR MeanDist becomes ξ-sensitive)
   OR any N < 200 reverses the stability pattern
   OR the pattern breaks at N > 500 (continuum-limit divergence)

H12_AMBIGUOUS if:
   The pattern holds for exponential coupling only (law-specific, not generic)
   OR the pattern degrades but does not reverse at extreme N or s
```

---

## SECTION 5: Falsification Criteria

### 5.1 Minimum Falsification Set for H9

| Test | What would falsify H9 |
|:-----|:----------------------|
| H9-F1 | ∂Ω/∂N > ∂Ω/∂ξ — a geometry parameter outranks a sync parameter in Omega sensitivity |
| H9-F2 | ∂Ω/∂seed (CV across 20 seeds) > ∂Ω/∂K₀ (CV across K₀ sweep) — seed variation exceeds coupling-strength variation |
| H9-F3 | Ω shows no statistically significant ξ-dependence (p > 0.05 for ξ sweep slope) — contradicts V5.2 baseline |
| H9-F4 | In the two-axis interaction grid (SM7), the (ξ, K₀) interaction term is insignificant while the (ξ, N) interaction is significant |

### 5.2 Minimum Falsification Set for H10

| Test | What would falsify H10 |
|:-----|:----------------------|
| H10-F1 | CV_MD(ξ) > 0.10 — MeanDist is substantially sensitive to ξ (contradicts V5.2 "highly stable" classification) |
| H10-F2 | CV_MD(ξ) > CV_MD(seed) — the ξ response exceeds seed variability |
| H10-F3 | |∂MD/∂ξ| > |∂MD/∂s| — ξ affects MeanDist more than frequency spread does |
| H10-F4 | MD shows a monotonic trend with ξ (slope significantly different from zero at p < 0.05) |

### 5.3 Minimum Falsification Set for H11

| Test | What would falsify H11 |
|:-----|:----------------------|
| H11-F1 | S[ξ, MD] > S[ξ, Ω] — xi is more geometry-controlling than sync-controlling |
| H11-F2 | S[N, Ω] > S[N, MD] — N is more sync-controlling than geometry-controlling |
| H11-F3 | The normalized sensitivity matrix has no detectable block structure (clustering analysis: parameters do not group into two classes) |

### 5.4 Minimum Falsification Set for H12

| Test | What would falsify H12 |
|:-----|:----------------------|
| H12-F1 | The stability pattern reverses for Gaussian or power-law coupling |
| H12-F2 | At N=1000, Omega becomes seed-variable (CV > 0.05) |
| H12-F3 | At N=50, MeanDist becomes ξ-sensitive (CV > 0.10) |
| H12-F4 | Any regime point where Ω and MD are positively correlated (ρ > 0.3) — they should be independent per V4.1 OGG |

---

## SECTION 6: Recommended First V5.3 Suite

### Suite Name

`V5_3_StabilityMechanismProtocol_Tests.cs` (SMP)

### Suite Purpose

Define the frozen protocol for V5.3. No execution. Output-only definition of the sensitivity matrix, hypothesis tests, falsification criteria, and forbidden actions.

### Test Structure (8 tests)

| Test ID | Name | Purpose |
|:--------|:-----|:--------|
| SMP-01 | V52BaselineLoaded | Assert V5.2 corrected stability picture as immutable baseline |
| SMP-02 | ParameterClassificationDefined | Define sync-control vs geometry-control parameter classes |
| SMP-03 | FrozenGridDefined | Freeze the 210-run sensitivity matrix (SM1–SM7) |
| SMP-04 | ForbiddenActionsDefined | 10+ forbidden actions (no post-hoc parameter removal, no seed reselection, no anchor adjustment) |
| SMP-05 | HypothesisTestsDefined | Formalize H9–H12 operationalizations |
| SMP-06 | FalsificationCriteriaDefined | Define minimum falsification tests for each hypothesis |
| SMP-07 | DecisionCriteriaDefined | Define SUPPORTED/FALSIFIED/AMBIGUOUS boundaries |
| SMP-08 | SeedManifestFrozen | Freeze all 65 seed values with SHA-256 hash |

### Forbidden Actions (Protocol Gates)

```
1. Do NOT remove any regime point after seeing results.
2. Do NOT reselect seeds after seeing results.
3. Do NOT adjust the primary regime reference values (ξ=1.80, K₀=1.15, s=0.08, N=100, exp).
4. Do NOT tune parameter ranges to achieve desired sensitivity patterns.
5. Do NOT change the definition of Omega or MeanDist between runs.
6. Do NOT exclude outlier runs from ensemble statistics.
7. Do NOT reclassify a parameter from sync-control to geometry-control post-hoc based on results.
8. Do NOT add new interaction axes after the grid is frozen.
9. Do NOT merge seed ensembles from different regimes.
10. Do NOT compute p-values after seeing the direction of effects.
11. Do NOT use Ω-MD correlation as evidence of parameter coupling without pre-registered thresholds.
12. All hypothesis test thresholds are frozen BEFORE execution.
```

---

## SECTION 7: Risks and Confounders

### 7.1 Internal Risks

| Risk | Severity | Mitigation |
|:-----|:--------:|:-----------|
| 3→5 seeds still under-samples seed variance | MEDIUM | Phase SM6 provides 20-seed baseline for CV calibration |
| ξ-Ω relationship may be nonlinear (threshold effects) | MEDIUM | 7-point sweep with finer spacing than V5.2 |
| ω̄=0 symmetry may produce degenerate signatures | LOW | The bias correction exists for finite N even with symmetric ω_i distribution |
| c_eff and G_eff are derived from Ω and MD — they carry no independent information about the decomposition | LOW | They are included for completeness and cross-validation only; hypothesis tests use Ω and MD directly |
| The classification of K₀ as "primarily sync-control" may be wrong | MEDIUM | The two-axis phase SM7 tests the (ξ, K₀) interaction explicitly |
| MeanDist normalization issue: larger N produces larger MeanDist by construction | HIGH | Must normalize MeanDist by graph diameter or expected distance before comparing across N |

### 7.2 External Confounders (acknowledged, not controlled)

| Confounder | Effect |
|:-----------|:-------|
| The Kuramoto model itself may have symmetries that force the observed stability pattern | If true, H12 is SUPPORTED for structural reasons, not mechanistic ones |
| Exponential coupling may be a special case with clean parameter separation | If true, H12 may fail for Gaussian/power-law — this is a test, not a confounder to eliminate |
| Finite simulation time may introduce transient artifacts | Standard: discard first 50% of steps as transient |
| The synchronized-cluster definition threshold is a free parameter | Keep threshold frozen at the V4.1/V5.2 value (typically r_sync > 0.8) |

### 7.3 Interpretive Risks

| Risk | Mitigation |
|:-----|:-----------|
| Finding the expected result (confirmation bias) | Falsification criteria registered BEFORE execution |
| Over-interpreting small differences | Pre-registered thresholds |
| Conflating "not falsified" with "proven" | Claim discipline: results classified as SUPPORTED or FALSIFIED only, never PROVEN |
| Treating parameter classification as theoretical truth | Classification is a working hypothesis for experiment design only |

---

## SECTION 8: Claim Discipline Review

### 8.1 What This Document Claims

- **CLAIMED:** A parameter classification framework for experimental design (6 parameters classified).
- **CLAIMED:** A 210-run frozen sensitivity matrix design.
- **CLAIMED:** Formal operationalizations for H9–H12 with pre-registered decision thresholds.
- **CLAIMED:** A minimum falsification set for each hypothesis.
- **CLAIMED:** A protocol structure for the V5.3 SMP suite.

### 8.2 What This Document Does NOT Claim

- **NOT CLAIMED:** That the parameter classification reflects true causal mechanisms.
- **NOT CLAIMED:** That V5.3 will confirm H9–H12.
- **NOT CLAIMED:** That the attractor decomposition is a fundamental property.
- **NOT CLAIMED:** That the classification generalizes beyond the tested parameter ranges.
- **NOT CLAIMED:** That synchronization-control and geometry-control are the only parameter classes.
- **NOT CLAIMED:** Any physical interpretation of Ω or MeanDist.
- **NOT CLAIMED:** That Ω is "time" or MeanDist is "space" in any physical sense.

### 8.3 Classification of This Document

This document is classified as **RESEARCH PREPARATION — PROTOCOL DRAFT**. It contains:

- **SUPPORTED:** 10 statements derived from completed V5.2 pipeline evidence (Section 1.1).
- **CONDITIONAL:** 5 statements valid under specified conditions (Section 1.2).
- **HYPOTHESIS:** 4 mechanism hypotheses explicitly marked as untested (Section 1.3).
- **WORKING CLASSIFICATION:** 6 parameter classifications for experimental design (Section 2.5).
- **NOT CLAIMED:** 7 items explicitly excluded (Section 8.2).

---

## SECTION 9: Summary

| Item | Value |
|:-----|:------|
| Parameters classified | 6 (ξ, K₀, s, N, law, seed) |
| Classification types | 3 (sync-control, geometry-control, mixed) |
| Frozen grid points | 39 |
| Total runs | 210 |
| Seeds required | 65 unique seeds |
| Hypothesis tests | 4 (H9–H12) |
| Falsification gates | 16 (4 per hypothesis) |
| Forbidden actions | 12 |
| Recommended first suite | SMP (8 protocol tests) |
| Risk level | MEDIUM (normalization across N, seed-count adequacy) |

### Next Step

If this protocol is approved, the next step is implementing `V5_3_StabilityMechanismProtocol_Tests.cs` as an 8-test protocol suite that freezes all definitions, the sensitivity grid, seed manifest, hypothesis operationalizations, falsification criteria, and forbidden actions. No simulation execution occurs in the protocol suite.

Subsequent suites:
1. **SMP** — Protocol (this document, frozen)
2. **SME** — Execution (210 runs, 7 phases)
3. **SMEA** — Execution Audit (hash reproducibility, seed integrity)
4. **SMSC** — Sensitivity Comparison (compute sensitivity matrix)
5. **SMSI** — Sensitivity Interpretation (test H9–H12)
6. **SMBS** — Branch Synthesis (completion report)

---

*Prepared 2026-07-16 under V5.3 EXPLORATORY status. All parameter classifications are working hypotheses for experiment design. No mechanism is claimed. Falsification criteria are pre-registered to prevent confirmation bias.*
