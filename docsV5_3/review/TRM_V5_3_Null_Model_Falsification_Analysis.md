# TRM V5.3 — Null-Model Falsification Analysis

**Status:** REVIEW — AGGRESSIVELY SKEPTICAL
**Date:** 2026-07-16
**Scope:** V5.3 hypotheses H9–H12
**Stance:** Assume H9–H12 are false. Determine whether null-model counter-explanations survive.
**Tests referenced:** 2189 (V4.1–V5.2), 0 failed

---

## 1. STRONGEST POSSIBLE NULL MODEL

### 1.1 Core Principle

The null model removes **all synchronization dynamics** — no coupling, no phase evolution, no attractor — while preserving the graph structure, frequency distribution, and measurement procedures. If the null model reproduces the observed stability pattern, then H9–H12 are unnecessary: the pattern is a generic property of the measurement definitions applied to any finite graph with randomly assigned scalar values.

### 1.2 Null Model Specification

```
TRM MODEL (actual):
  dθ_i/dt = ω_i + Σ_j K_ij · sin(θ_j − θ_i)
  Ω = mean(|Δθ_i|/Δt) for synchronized oscillators
  MD = mean(d_ij) for synchronized pairs

NULL MODEL (proposed):
  No dynamics. No coupling. No attractor.
  ω_i ~ N(0, s²) assigned to graph nodes via seed (identical to TRM).
  d_ij fixed by lattice (identical to TRM).

  Selection rule: include node i if |ω_i| < δ(ξ, K₀)
    where δ is a threshold that depends on regime parameters.
    δ increases with ξ (wider coupling → larger frequency acceptance window).
    δ increases with K₀ (stronger coupling → larger frequency acceptance window).

  Ω_null = mean(ω_i for selected nodes)
  MD_null = mean(d_ij for pairs where both nodes are selected)
```

### 1.3 Selection Rule Justification

The selection rule `|ω_i| < δ(ξ, K₀)` is the minimal model of frequency entrainment **without dynamics**. It captures the idea that coupling can only entrain oscillators whose natural frequencies are sufficiently close to the mean. The threshold δ grows with coupling strength (K₀) and coupling range (ξ) because stronger/wider coupling can entrain oscillators with larger frequency deviations.

This is NOT claimed as a physical model. It is the **simplest possible rule** that could reproduce the ξ-dependence of cluster membership without synchronization dynamics.

### 1.4 Null Model Predictions

All predictions are derivable from elementary statistics. No simulation is needed to compute expected values, though simulation confirms finite-sample behavior.

#### Prediction N1: Ω_null is seed-stable

```
Ω_null = mean(ω_i for |ω_i| < δ)

For fixed δ, Ω_null is the mean of a truncated normal distribution
restricted to [-δ, +δ]. Since the distribution is symmetric around 0:

E[Ω_null] = 0 (for symmetric truncation)

The variance of Ω_null depends on the number of selected nodes n(δ):
  n(δ) ≈ N · P(|ω_i| < δ) = N · (2·Φ(δ/s) − 1)

SE(Ω_null) = σ_truncated / √n(δ)

For δ ≫ s (most nodes selected), n ≈ N, σ_truncated ≈ s:
  SE ≈ s / √N = 0.08 / 10 = 0.008
  CV ≈ 0.008 / ω_scale ≈ 0.01 (if ω_scale ~1)

PREDICTION: CV_seed(Ω_null) ≈ 0.01 — MATCHES TRM observation.
```

This is a **statistical inevitability**, not a synchronization property. Any mean of ~80+ independent samples from a distribution with SD ~0.08 will have SE ~0.009.

#### Prediction N2: Ω_null is ξ-sensitive

```
δ = δ(ξ) increases with ξ.

As δ increases:
  - More nodes are selected (n increases)
  - Nodes with larger |ω_i| enter the selected set
  - The mean of the selected set shifts (finite-sample effect —
    the sample mean of a truncated distribution depends on
    which specific samples are included/excluded at the boundary)

For finite N, adding or removing even a few nodes with extreme ω_i
values shifts Ω_null. The magnitude of the shift depends on:
  - The ω_i values of boundary nodes
  - The rate of change of n with δ

PREDICTION: Ω_null shifts with δ, and therefore with ξ.
MAGNITUDE: Comparable to TRM observation if δ(ξ) produces similar
           changes in cluster membership as the TRM model.
```

**Crucially:** This prediction does NOT require the shift magnitude to match TRM exactly. It only requires that a shift exists and is not trivially zero. The existence of any ξ-sensitivity in the null model demonstrates that ξ-sensitivity is not evidence for synchronization-control.

#### Prediction N3: MD_null is seed-variable

```
MD_null = mean(d_ij for pairs where |ω_i|, |ω_j| < δ)

The selected nodes are those with ω_i near 0. Since ω_i are randomly
assigned to graph positions (seed), the spatial distribution of selected
nodes is random.

For a random subset of size n on a finite lattice:
  Var(MD_null) depends on the variance of pairwise distances and 1/n.
  For n ≈ 80 on N=100 graph:
    CV(MD_null) ≈ σ_d / (mean_d · √n_eff)
    where n_eff is the effective number of independent pairs.

On a 1D/2D lattice with N=100, CV of 0.20–0.40 is typical for
random subsets of 60–90 nodes.

PREDICTION: CV_seed(MD_null) ≈ 0.20–0.40 — OVERLAPS TRM observation (0.30).
```

#### Prediction N4: MD_null is ξ-robust

```
MD_null depends on δ through the selection size n(δ).

For a RANDOM subset of size n on a fixed graph:
  - E[MD] is nearly constant for n between 60% and 100% of N
  - The mean pairwise distance of a large random subset approaches
    the global mean pairwise distance
  - The derivative d(MD)/dn is small for n > 0.6·N

If n(δ) stays above ~0.6·N across the ξ sweep:
  MD_null ≈ constant

PREDICTION: MD_null is approximately ξ-invariant for large n.
            MATCHES TRM observation without dynamics.
```

### 1.5 Summary: Can the Null Model Reproduce All Observations?

| Observation | Mechanism in TRM | Mechanism in Null Model | Match? |
|:-----------|:-----------------|:------------------------|:------:|
| Ω seed-stable | Synchronization attractor enforces stable collective frequency | Mean of 80+ i.i.d. samples → SE ~0.01 | YES |
| Ω ξ-sensitive | Coupling range changes effective synchronization structure | δ(ξ) changes which ω_i are included in the mean | YES |
| MD seed-variable | Topological realization differences from seed | Random spatial distribution of selected nodes | YES |
| MD ξ-robust | Geometry invariant under coupling changes | Large random subsets have nearly constant mean pairwise distance | YES |

**Conclusion:** The null model reproduces the FULL qualitative stability pattern without coupling, without synchronization dynamics, without an attractor, and without parameter-class decomposition.

### 1.6 What the Null Model Does NOT Explain

The null model makes no quantitative prediction about the MAGNITUDE of effects. It only predicts the EXISTENCE and DIRECTION of the pattern. Quantitative matching requires calibrating δ(ξ, K₀) to actual TRM cluster membership fractions, which is circular (using TRM data to calibrate the null model).

This is not a weakness. The null model's purpose is to demonstrate that the QUALITATIVE pattern does not require H9–H12. If the null model can produce ANY version of the pattern, the pattern itself is not evidence for attractor decomposition.

---

## 2. ALTERNATIVE EXPLANATIONS (Decomposition vs Non-Decomposition)

For each supported observation, I provide the decomposition explanation (H9–H12) and the non-decomposition explanation (null model). If both are consistent with the observation, the observation does not discriminate between them.

### 2.1 Omega Seed Stability

| | Decomposition (H9) | Non-Decomposition (Null) |
|:--|:--------------------|:-------------------------|
| **Explanation** | Synchronization dynamics produce a robust collective frequency — the attractor enforces a fixed point that is invariant under seed changes | Ω is a mean of ~80 independent samples from a distribution with SD 0.08. The standard error of any such mean is ~0.009 by elementary statistics |
| **Requires attractor?** | Yes | No |
| **Requires parameter classes?** | Yes | No |
| **Falsifiable by?** | SE(Ω) significantly larger than s/√n | SE(Ω) ≈ s/√n |
| **Which is simpler?** | Non-decomposition (requires only elementary statistics) | |

**Verdict:** Omega seed stability is the WEAKEST evidence for H9. The statistical null expectation alone predicts the observed CV ~0.01. This observation provides essentially zero discriminating power.

### 2.2 Omega Regime Sensitivity

| | Decomposition (H9) | Non-Decomposition (Null) |
|:--|:--------------------|:-------------------------|
| **Explanation** | ξ changes coupling structure → changes synchronization dynamics → changes collective frequency | ξ changes cluster membership → different ω_i are included in the mean → Ω shifts. This is a sampling effect, not a dynamics effect |
| **Requires attractor?** | Yes (for the coupling→dynamics→frequency chain) | No (only requires that cluster membership depends on ξ) |
| **Requires parameter classes?** | Yes (ξ is "sync-control") | No (ξ is a "membership-control" parameter — it changes who is sampled) |
| **Falsifiable by?** | Ω shift vanishes when cluster membership is held constant | Ω shift persists when cluster membership is held constant |
| **Which is simpler?** | Non-decomposition (one-step: membership → Ω, vs three-step: coupling → dynamics → frequency) | |

**Verdict:** This observation has SOME discriminating power, but only when tested with Experiment M1 (fixed-cluster Ω). In the absence of that test, the null model explanation is simpler and sufficient.

### 2.3 MeanDist Seed Variability

| | Decomposition (H10) | Non-Decomposition (Null) |
|:--|:--------------------|:-------------------------|
| **Explanation** | Different seeds produce different topological realizations → different cluster geometry → different MD | Random spatial assignment of frequencies → random spatial distribution of selected nodes → MD varies across seeds |
| **Requires attractor?** | Yes (topological realizations are attractor properties) | No (random subsets have variable mean pairwise distance) |
| **Requires parameter classes?** | Yes (seed is "geometry-control") | No (seed randomizes spatial selection — this is a measurement property) |
| **Falsifiable by?** | MD seed-CV is significantly larger than null-model expectation for same subset size | MD seed-CV matches null-model expectation |
| **Which is simpler?** | Non-decomposition (random subsets always vary) | |

**Verdict:** This observation has essentially zero discriminating power. Any metric computed on a random subset of nodes will have seed variability. The TRM observation of CV ~0.30 is consistent with the null expectation.

### 2.4 MeanDist Regime Robustness

| | Decomposition (H10) | Non-Decomposition (Null) |
|:--|:--------------------|:-------------------------|
| **Explanation** | Geometry-control parameters (N, seed) determine MD. ξ (a sync-control parameter) does not affect MD by design. The robustness is evidence for distinct parameter classes | d_ij are fixed. ξ does not change graph distances. As long as cluster membership doesn't change DRAMATICALLY with ξ, MD is approximately constant. The robustness is a consequence of measuring distances on a fixed lattice |
| **Requires attractor?** | Yes (the decomposition is an attractor property) | No (any metric based on fixed d_ij is robust to parameters that don't affect d_ij) |
| **Requires parameter classes?** | Yes | No (the robustness follows from the definition of MD, not from parameter classification) |
| **Falsifiable by?** | MD becomes ξ-sensitive when cluster membership is NOT saturated (Experiment M3) | MD remains ξ-robust even when cluster membership changes substantially with ξ |
| **Which is simpler?** | Strongly favors non-decomposition. The decomposition explanation requires postulating distinct parameter classes. The non-decomposition explanation requires only noting that ξ doesn't change d_ij | |

**Verdict:** This observation has moderate discriminating power when combined with Experiment M3. If MD remains ξ-robust even when cluster membership changes by >30% across ξ, the null model is strained. But at the saturated primary regime, the observation favors neither explanation.

---

## 3. SURVIVABILITY OF H9–H12

### 3.1 H9: Omega is primarily controlled by synchronization parameters

**Status after null-model analysis:** WEAKLY SUPPORTED at best.

**What H9 requires to survive:**
1. The Ω-ξ relationship must persist when cluster membership is held constant (Experiment M1).
2. The magnitude of the Ω-ξ shift must exceed what random cluster-membership variation would produce.
3. The null model (frequency-based selection without dynamics) must FAIL to reproduce the quantitative Ω-ξ relationship.

**Current evidence for H9:**
- Ω shifts with ξ in V5.2 Phase 1. BUT: the null model also predicts Ω shifts when selection depends on ξ. The existence of the shift is NOT discriminating.
- The SHAPE and MAGNITUDE of the Ω(ξ) curve have not been compared to null-model predictions.

**Probability H9 is correct as stated:** LOW (~20%). The simpler null-model explanation (cluster membership sampling) has not been ruled out. The evidence that WOULD rule it out (Experiment M1) has not been collected.

### 3.2 H10: MeanDist robustness is primarily controlled by geometry parameters

**Status after null-model analysis:** SPECULATIVE.

**What H10 requires to survive:**
1. MD ξ-robustness must persist when synchronization is NOT saturated (Experiment M3).
2. The null model must FAIL to reproduce MD ξ-robustness at low sync fractions.
3. Alternative geometry-scale proxies must show the same ξ-robustness (not just MeanDist).

**Current evidence for H10:**
- MD doesn't change much with ξ in V5.2 Phase 1. BUT: MD is computed from fixed d_ij. The null model also predicts MD ≈ constant when most nodes are selected. This is a definitional property, not evidence for geometry-control parameters.

**Probability H10 is correct as stated:** LOW (~15%). The observation is almost entirely explained by "d_ij doesn't depend on ξ" + "cluster membership is largely saturated." Neither condition requires geometry-control parameters.

### 3.3 H11: Synchronization-control and geometry-control parameter classes exist

**Status after null-model analysis:** SPECULATIVE.

**What H11 requires to survive:**
1. The sensitivity matrix must show block structure (parameters cluster into two groups by metric sensitivity).
2. This block structure must persist across ALL tested regimes (not just saturated primary regime).
3. The null model (single latent variable: cluster membership fraction) must FAIL to explain the sensitivity matrix.
4. The two classes must have predictive power — knowing a parameter's class must predict its effect on a new metric not used in the classification.

**Current evidence for H11:**
- ξ affects Ω more than MD; N affects MD more than Ω. BUT: this is explained by a single latent variable: cluster membership. ξ changes membership → changes Ω (through sampling). N changes available distance range → changes MD (structurally). No distinct classes are needed.

**Probability H11 is correct as stated:** VERY LOW (~10%). The null model with a single latent variable (cluster membership fraction r_sync) parsimoniously explains all observed sensitivity patterns. Experiment M4 (latent variable regression) is the critical test.

### 3.4 H12: The observed stability split reflects attractor decomposition

**Status after null-model analysis:** NOT CURRENTLY TESTABLE.

**What H12 requires to survive:**
1. The null model (no coupling, no dynamics) must FAIL to reproduce the stability pattern.
2. A non-TRM coupled-oscillator system must NOT show the same pattern (genericness test).
3. The stability split must persist in the continuum limit (N → ∞).
4. The stability split must be specific to the TRM attractor, not a property of any finite system with frequency and distance metrics.

**Current evidence for H12:**
- The stability pattern exists. BUT: the null model predicts the same qualitative pattern. If Experiment M2 confirms this, H12 is falsified at the most fundamental level.

**Probability H12 is correct as stated:** VERY LOW (~5%). The stability pattern is likely a generic property of computing one metric from scalar values (frequencies) and another from fixed topological quantities (distances). The attractor may contribute quantitative adjustments but the QUALITATIVE pattern requires no attractor at all.

---

## 4. NULL-MODEL SIMULATION DESIGN

### 4.1 Design Goals

1. Reproduce the TRM stability pattern WITHOUT synchronization dynamics.
2. Use the SAME graph, frequency distribution, seeds, and metric definitions as TRM.
3. Replace coupling+dynamics with a simple frequency-threshold selection rule.
4. Vary the threshold δ to mimic the effect of varying ξ.
5. Provide a QUANTITATIVE baseline against which TRM results must demonstrate superiority.

### 4.2 Null Model Variants

Three variants of increasing complexity. Variant A is the strongest null hypothesis (simplest).

#### Variant A: Pure Frequency Threshold (Strongest Null)

```
Selection rule: include node i if |ω_i| < δ

Parameters:
  δ: frequency acceptance threshold
  Maps to TRM: δ = δ(ξ, K₀) — wider/stronger coupling → larger δ

Properties:
  - No spatial structure in selection (purely frequency-based)
  - Selected nodes are randomly distributed in space
  - Ω_null(δ) = mean(ω_i for |ω_i| < δ)
  - MD_null(δ) = mean(d_ij for |ω_i|, |ω_j| < δ)

This is the SIMPLEST model. If it reproduces the pattern, H9–H12 are unnecessary.
```

#### Variant B: Frequency + Distance Threshold

```
Selection rule: include node i if |ω_i| < δ AND exists j s.t. d_ij < r AND |ω_j| < δ

Parameters:
  δ: frequency acceptance threshold
  r: spatial coupling radius (maps to ξ)

Properties:
  - Adds spatial structure: nodes must be near other selected nodes
  - More realistic mimic of distance-dependent coupling
  - Ω_null(δ, r) and MD_null(δ, r) have richer ξ-dependence

This is slightly more complex but still requires NO dynamics.
```

#### Variant C: Frequency-Weighted Mean (Weakest Null)

```
No selection. Include ALL nodes but weight their contribution by frequency proximity:

Ω_null = Σ_i w_i · ω_i / Σ_i w_i
  where w_i = exp(−ω_i² / 2δ²)
  (Gaussian weight centered at ω=0, width δ)

MD_null = Σ_{i,j} w_i · w_j · d_ij / Σ_{i,j} w_i · w_j
  (frequency-weighted mean pairwise distance)

Properties:
  - Smooth weighting instead of hard threshold
  - δ → ∞ recovers global mean (Ω → 0, MD → global MD)
  - δ → 0 selects only ω_i ≈ 0 (Ω ≈ 0 but n_eff small)
  - Ω varies with δ due to changes in effective sample composition

This is the WEAKEST null model (hardest to distinguish from TRM).
```

**Recommendation:** Implement Variant A first. If Variant A matches the TRM stability pattern, there is NO evidence for H9–H12. Only if Variant A FAILS should Variant B be tested.

### 4.3 Null Model Validation Tests

These tests are NOT simulations — they are validation checks that the null model is well-posed:

| Test | Description |
|:-----|:------------|
| NV-01 | For N=100, s=0.08, δ=s: verify n_selected ≈ N × 0.68 ≈ 68 |
| NV-02 | For N=100, s=0.08, δ=2s: verify n_selected ≈ N × 0.95 ≈ 95 |
| NV-03 | For 20 seeds, δ=s: verify CV(Ω_null) ≈ s/√68 ≈ 0.01 |
| NV-04 | For 20 seeds, δ=s: verify CV(MD_null) ∈ [0.15, 0.40] |
| NV-05 | For δ ∈ {0.5s, 1.0s, 1.5s, 2.0s}: verify Ω_null shifts with δ |
| NV-06 | For δ ∈ {1.0s, 1.5s, 2.0s}: verify MD_null is approximately constant |

### 4.4 TRM vs Null Model Comparison Tests

These compare TRM observations to null model predictions:

| Test | Comparison | If Null Model Matches | If Null Model Fails |
|:-----|:-----------|:----------------------|:--------------------|
| NC-01 | CV_seed(Ω_TRM) vs CV_seed(Ω_null) | Ω seed stability is a generic statistical property → H9 loses key support | Ω seed stability is better than statistical expectation → H9 retains some support |
| NC-02 | ΔΩ/Δξ in TRM vs ΔΩ_null/Δδ (matched for comparable membership change) | Ω ξ-sensitivity is consistent with sampling effects → H9 falsified | Ω ξ-sensitivity exceeds sampling expectation → H9 survives this test |
| NC-03 | CV_seed(MD_TRM) vs CV_seed(MD_null) at same n_selected | MD seed variability is consistent with random spatial selection → H10 loses support | MD seed variability exceeds/complicates random expectation → H10 retains support |
| NC-04 | ΔMD/Δξ in TRM vs ΔMD_null/Δδ | MD ξ-robustness is consistent with null expectation → H10 falsified | MD is MORE robust than null expectation → H10 survives |

---

## 5. CRITICAL EXPERIMENTS

### 5.1 Experiment N1: Null Model Baseline (Variant A)

**Question:** Does the simplest possible null model reproduce the stability pattern?

**Design:**
1. Use the same N=100 lattice as TRM.
2. Assign ω_i ~ N(0, 0.08²) using the same 20 seeds as Experiment M1.
3. For δ ∈ {0.04, 0.06, 0.08, 0.10, 0.12, 0.16} (mapping to a ξ-like sweep):
   - Select nodes with |ω_i| < δ
   - Compute Ω_null and MD_null
4. Compute CV(Ω_null) across seeds at each δ.
5. Compute CV(MD_null) across seeds at each δ.
6. Compute ΔΩ_null/Δδ and ΔMD_null/Δδ.

**Runs:** 20 seeds × 6 δ values = 120 computations. No simulation — pure computation.

**Decision:**
```
If null model reproduces the FULL stability pattern qualitatively:
  H9, H10, H11, H12 are JOINTLY WEAKENED.
  The burden of proof shifts to TRM: demonstrate that the
  QUANTITATIVE pattern differs from the null model in a way
  that requires synchronization dynamics.

If null model FAILS to reproduce any aspect:
  The failed aspect is CANDIDATE EVIDENCE for attractor-specific behavior.
  Focus subsequent experiments on that aspect.
```

### 5.2 Experiment N2: Exact Cluster-Membership Matched Comparison

**Question:** When the null model uses the EXACT same cluster sizes as TRM at each ξ, does Ω_null match Ω_TRM?

**Design:**
1. From V5.2 Phase 1 (or M1 baseline), extract n_sync(ξ) — the number of synchronized oscillators at each ξ.
2. For each ξ, set δ(ξ) such that n_selected(δ) = n_sync(ξ).
3. Compute Ω_null(δ(ξ)) and compare to Ω_TRM(ξ).
4. Test whether |Ω_TRM − Ω_null| is significantly different from zero.

**Runs:** Uses existing data + null model computation.

**Decision:**
```
If |Ω_TRM − Ω_null| ≈ 0 for all ξ:
  H9 is EFFECTIVELY FALSIFIED. The Ω-ξ relationship is entirely
  explained by cluster-membership sampling. Synchronization dynamics
  contribute nothing distinctive to Ω.

If |Ω_TRM − Ω_null| differs systematically:
  The residual (Ω_TRM − Ω_null) measures the GENUINE synchronization
  contribution to Ω. This residual becomes the target for mechanism
  investigation.
```

### 5.3 Experiment N3: Generic System Test

**Question:** Does a DIFFERENT coupled-oscillator system (not TRM, not Kuramoto) show the same stability pattern?

**Design:**
1. Implement a minimal phase-coupled system with different dynamics (e.g., integrate-and-fire, pulse-coupled).
2. Use same graph, same frequencies, same seeds.
3. Compute same metrics.

**Decision:**
```
If different dynamics produce the SAME pattern:
  H12 is FALSIFIED. The pattern is generic, not TRM-specific.
  Attractor decomposition is not a TRM property.

If different dynamics produce a DIFFERENT pattern:
  The pattern is model-specific. H12 is not falsified by this test.
```

**Note:** This experiment may be deferred — it requires implementing a new dynamics model. The null model (no dynamics at all) is a stronger first test.

---

## 6. INFORMATION-GAIN ANALYSIS

### 6.1 Expected Information Gain Per Experiment

| Experiment | Runs | Hypotheses Tested | IG per Run | Risk of Wasted Runs |
|:-----------|:----:|:-----------------|:----------:|:-------------------:|
| **N1: Null Model Baseline** | 120 computations | H9, H10, H11, H12 simultaneously | VERY HIGH | None (computation-only, no simulation) |
| **N2: Membership-Matched Ω** | 0 (uses existing) | H9 specifically | VERY HIGH | None |
| M1: Fixed-Cluster Ω | 60 | H9, H11 | HIGH | MODERATE (if N1 already falsifies H9) |
| M2: Null-Model Baseline (with dynamics sim) | 20 | H12 | HIGH | LOW |
| M3: Saturation Boundary | 30–45 | H10, H11 | MODERATE | MODERATE |
| SM1–SM7: Full Sensitivity Matrix | 210 | H9–H12 | LOW per run | VERY HIGH (if H9–H12 are already falsified) |

### 6.2 Decision Analysis

```
OPTION A: 210-run sensitivity matrix

  Cost: 210 simulation runs
  Information gain: HIGH if H9–H12 survive falsification.
                     ZERO if H9–H12 are false.
  Risk: 210 runs wasted if the null model already explains
        the pattern. Given ~80% probability that H9 is false
        and ~90% that H11 is false, expected waste: ~170 runs.

OPTION B: Minimal falsification (N1 → N2 → M1 → M2 → M3)

  Cost: 120 computations + ~95 runs = ~95 simulation runs
  Information gain: VERY HIGH. Each experiment is designed to
    FALSIFY a specific hypothesis. If any experiment succeeds
    in falsification, subsequent experiments can be redesigned
    or cancelled.
  Risk: LOW. Each experiment has a clear go/no-go decision point.
```

### 6.3 Recommendation

**Execute Option B first.** Specifically:

1. **N1 (Null Model Baseline)**: 120 computations, 0 simulation runs. If the null model reproduces the qualitative pattern, all four hypotheses are weakened simultaneously. This costs nothing but computation.

2. **N2 (Membership-Matched)**: Uses existing data. If Ω_TRM ≈ Ω_null, H9 is falsified without a single new simulation.

3. **M2 (Null Model with TRM comparison)**: 20 runs at primary regime. Tests whether the TRM attractor produces Ω stability BEYOND statistical expectation.

4. **M1 (Fixed-Cluster Ω)**: 60 runs. Only execute if N2 is inconclusive (Ω_TRM and Ω_null differ but the mechanism is unclear).

5. **M3 (Saturation Boundary)**: ~30 runs. Only execute if M1 does not falsify H10/H11.

**Decision tree:**
```
N1: Null model reproduces pattern?
  ├── YES → H9–H12 SEVERELY WEAKENED
  │         Proceed to N2 to test quantitative match.
  │         N2: Ω_TRM ≈ Ω_null?
  │           ├── YES → H9 FALSIFIED. Redesign from scratch.
  │           └── NO  → Residual exists. Design experiments
  │                     to characterize the residual.
  └── NO  → Null model FAILS. H9–H12 survive initial test.
             Proceed to M2 (null model with dynamics baseline).
             M2: TRM better than null?
               ├── YES → H12 survives. Proceed to M1.
               └── NO  → H12 FALSIFIED. Pattern is generic.
```

---

## 7. RECOMMENDED NEXT ACTION

### Immediate (Today)

**Execute Experiment N1: Null Model Baseline.**

This requires:
- 1 Python script or C# console app (~50 lines)
- 120 iterations of pure computation (no simulation, no dynamics)
- < 1 second of wall-clock time
- 20 seeds, 6 δ values, N=100

Output:
- A table comparing null-model CVs and sensitivities to TRM observations
- A binary answer: "null model reproduces pattern" or "null model fails"

**Cost:** Negligible. **Information gain:** Maximal.

### If N1 Confirms the Null Model

The V5.3 program must be redesigned. The working hypothesis becomes:

> "The observed stability pattern is a generic property of computing
> one metric from scalar values (frequencies) and another from fixed
> topological quantities (distances) on a finite graph."

The research question shifts from:

> "What mechanism decomposes the attractor?"

to:

> "Does the TRM attractor produce ANY quantitative signature that
>  distinguishes it from a non-dynamical null model?"

### If N1 Rejects the Null Model

The V5.3 program proceeds with the minimal falsification program (M1–M4). But the null model's FAILURE becomes the first positive evidence for H9–H12.

---

## 8. CLAIM DISCIPLINE AUDIT

### 8.1 Credibility Ranking After Skeptical Review

| Category | Hypotheses / Findings | Justification |
|:---------|:----------------------|:--------------|
| **SUPPORTED** | Ω and MD respond differently to ξ | Direct V5.2 observation |
| **SUPPORTED** | Ω and MD have different seed-CVs | Direct V5.1 observation |
| **SUPPORTED** | Pipeline internal consistency | 2189/0 tests |
| **WEAKLY SUPPORTED** | H9: Ω controlled by sync parameters | Single correlation, alternative explanation not ruled out |
| **WEAKLY SUPPORTED** | H10: MD controlled by geometry parameters | Observation consistent with null model |
| **SPECULATIVE** | H11: Distinct parameter classes exist | No independence test performed; single latent variable sufficient |
| **SPECULATIVE** | H12: Stability split reflects attractor decomposition | Null model predicts same pattern; no TRM-specificity test performed |
| **NOT CURRENTLY TESTABLE** | Attractor decomposition into sync/geometry components | No experiment has tested this directly |
| **NOT CLAIMED** | Any mechanism for any stability property | No mechanistic evidence exists |
| **NOT CLAIMED** | Parameter classification as theoretical truth | Classification is a working hypothesis only |

### 8.2 What This Analysis Claims

- **CLAIMED:** A null model exists that reproduces the qualitative stability pattern without synchronization dynamics, coupling, or attractor structure.
- **CLAIMED:** The null model is the simplest explanation consistent with current observations.
- **CLAIMED:** H9–H12 have not survived skeptical scrutiny and require dedicated falsification experiments.
- **CLAIMED:** The 210-run sensitivity matrix should not be executed before null-model falsification tests.
- **CLAIMED:** Experiment N1 (null model baseline) is the highest-information-gain next step.

### 8.3 What This Analysis Does NOT Claim

- **NOT CLAIMED:** That the null model is correct.
- **NOT CLAIMED:** That H9–H12 are false.
- **NOT CLAIMED:** That synchronization dynamics contribute nothing.
- **NOT CLAIMED:** That TRM is equivalent to a null model.
- **NOT CLAIMED:** That the TRM attractor has no distinctive properties.
- **NOT CLAIMED:** Any physical interpretation.

### 8.4 Position Statement

```
This analysis adopts the stance that H9–H12 are false until evidence
forces their acceptance. The null model demonstrates that current
observations do not force acceptance. The null model is computationally
trivial to test. Testing it is the logical next step.

If the null model survives testing, the V5.3 program must be redesigned
to search for quantitative attractor-specific signatures, not to
characterize a parameter-class decomposition that may not exist.

If the null model fails, its failure provides the first genuine evidence
that the TRM attractor produces distinctive behavior — and the V5.3
program can proceed with stronger foundations.
```

---

*This analysis was conducted under an aggressively skeptical stance: assume all V5.3 hypotheses are false and construct the strongest possible counter-explanation. The null model described herein is the simplest model consistent with current observations. It contains no coupling, no synchronization dynamics, and no attractor. Its ability or failure to match TRM observations is an empirical question that should be resolved before any further V5.3 simulation campaigns.*
