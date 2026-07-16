# TRM V5.3 — Critical Review of Parameter Classification

**Status:** REVIEW — SKEPTICAL AUDIT
**Date:** 2026-07-16
**Scope:** V5.3 working classification framework
**Reviewer stance:** Assume classification is wrong until evidence forces it to survive.
**Tests referenced:** 2189 (V4.1–V5.2), 0 failed

---

## 1. SUPPORTED FACTS

These are the ONLY statements that survive skeptical scrutiny from completed V5.2 evidence. Everything below this section is interpretation, not fact.

| # | Fact | Evidence Class | Caveat |
|:--|:-----|:--------------|:-------|
| F1 | In V5.2 Phase 1, Ω shifted across the ξ sweep {1.50, 1.65, 1.80, 1.95, 2.10} | DIRECT OBSERVATION | 3 seeds/point; direction and magnitude of shift not characterized beyond "sensitive" |
| F2 | In V5.2 Phase 1, MD did not shift substantially across the same ξ sweep | DIRECT OBSERVATION | "Not substantially" is qualitative — no quantitative threshold was pre-registered |
| F3 | In V5.1, Ω showed CV ~0.01 across 10 seeds at primary regime | DIRECT OBSERVATION | Single regime only |
| F4 | In V5.1, MD showed CV ~0.30 across 10 seeds at primary regime | DIRECT OBSERVATION | Single regime only |
| F5 | Seed stability and regime stability produce different rankings for Ω and MD | COMPARATIVE OBSERVATION | "Different" is not the same as "orthogonal" or "independent" |
| F6 | The V4.1 OGG suite found ρ(Ω, MD) ~0 within a single regime | DIRECT OBSERVATION | Within-regime only; cross-regime correlation not tested |
| F7 | The coupling kernel is K_ij = K₀ · exp(-d_ij/ξ) in the primary regime | MODEL DEFINITION | Not a finding — it's the model specification |
| F8 | d_ij are graph distances on a fixed lattice | MODEL DEFINITION | d_ij do not depend on ξ, K₀, s, seed, or law |
| F9 | 2189 tests pass, 0 fail | VERIFICATION FACT | Passing tests confirm internal consistency, not physical truth |
| F10 | The V5.2 corrected stability picture uses qualitative tiers (HIGHLY STABLE, MODERATELY STABLE, etc.) | CLASSIFICATION SCHEME | Tiers are human judgments applied to CV values, not statistically tested classifications |

**Critical observation:** Facts F2 and F5 are the weakest links. F2 lacks a quantitative threshold. F5 conflates "different rankings" with "orthogonal dimensions." Neither has been statistically tested.

---

## 2. CONDITIONAL FINDINGS

These hold only under assumptions that have not been independently verified.

| # | Finding | Required Assumption | Risk if Assumption False |
|:--|:--------|:--------------------|:-------------------------|
| C1 | ξ primarily controls synchronization | ξ affects Ω through coupling structure, not through cluster membership sampling | If ξ affects Ω only by changing which oscillators sync, ξ is a geometry-control parameter, not sync-control |
| C2 | N primarily controls geometry | MeanDist(D_max) is a valid normalization; N does not affect Ω beyond trivial finite-size scaling | If Ω's N-dependence is more than ~1/√N, N is a mixed parameter |
| C3 | Seed is a pure geometry-control parameter | Ω's seed-CV of ~0.01 is genuinely small, not an artifact of 3-seed under-sampling | If Ω seed-CV is actually 0.03–0.05 with more seeds, the "highly stable" classification weakens |
| C4 | The 5-axis parameter space decomposes into two classes | No third class exists; no parameter belongs to both classes | If s or law resist clean classification, the two-class model is insufficient |
| C5 | V5.2 3-seed CV estimates are reliable | 3 seeds adequately sample the seed distribution | Small-sample CV estimates are biased and have high variance |

---

## 3. ALTERNATIVE EXPLANATIONS

For each stability observation, I present the classification-implied mechanism and at least one alternative that does NOT require distinct parameter classes.

### 3.1 Omega Seed Stability

**Classification-implied:** Omega is determined by synchronization dynamics, which are seed-invariant because the distribution of ω_i (not the assignment) determines the collective frequency.

**Alternative A — Statistical inevitability:**
Ω is the mean phase-rotation rate averaged over N oscillators in the synchronized cluster. By the Central Limit Theorem, the standard error of any mean scales as σ/√N_eff where N_eff is the effective number of independent oscillators. At N=100, even with moderate correlation, N_eff ≥ 20–30, giving SE(Ω)/Ω ~ CV/√N_eff ~ 0.05–0.07 before any synchronization effect. The observed CV of 0.01 may simply be the expected statistical precision of a mean, not evidence of a synchronization mechanism.

**Test:** Compute Ω as the mean of individual oscillator frequencies for N independently generated ω_i (no coupling, no synchronization). If the null model produces CV ~0.01, then seed stability is a generic property of averaging, not a synchronization property.

**Alternative B — Insufficient seed sampling:**
V5.1 used 10 seeds, V5.2 used 3 seeds per point. A 3-seed sample CV is a biased estimator of the population CV. The true seed-CV of Ω might be 0.03–0.05, which would change the classification from "HIGHLY STABLE" to "MODERATELY STABLE."

**Test:** Run 50+ seeds at primary regime. Compute CV with bootstrap confidence intervals.

### 3.2 Omega Regime Sensitivity

**Classification-implied:** ξ changes the effective coupling structure → changes the collective frequency through synchronization dynamics.

**Alternative C — Cluster membership sampling (the "who syncs" hypothesis):**
ξ changes the effective coupling radius → different oscillators are within coupling range → the synchronized cluster has different membership → the mean natural frequency of the synchronized population changes → Ω shifts. In this interpretation, ξ is acting as a **sampling parameter**, not a synchronization-control parameter. The effect on Ω is mediated entirely through cluster membership, the same mechanism by which seed affects MeanDist.

**Critical implication:** If Alternative C is correct, then ξ and seed operate through the SAME mechanism (cluster membership), just on different inputs (ξ changes the membership rule; seed changes the spatial arrangement). The distinction between "sync-control" and "geometry-control" collapses — both are "membership-control."

**Test:** Compute Ω for each ξ value using only oscillators that are in the synchronized cluster at ALL ξ values (intersection cluster). If Ω still shifts with ξ for the fixed cluster, the effect is direct (sync-control). If Ω becomes stable, the effect is entirely through membership changes (Alternative C).

**Alternative D — Finite-N bias correction:**
The Ω shift with ξ is a finite-N effect in the bias correction term that vanishes as N → ∞. This would mean the "regime sensitivity" is not a fundamental property but a finite-size artifact.

**Test:** Repeat the ξ sweep at N=500 or N=1000. If the Ω-ξ slope decreases with N, it's a finite-size effect.

### 3.3 MeanDist Seed Variability

**Classification-implied:** Different seeds place different ω_i at different positions → different oscillators synchronize → different spatial extent of the synchronized cluster.

**Alternative E — This is the null hypothesis:**
MeanDist measures which oscillators happen to synchronize. Since natural frequency assignment is random (seed), cluster membership is random. Any metric that depends on cluster membership will be seed-variable. This is not a finding about geometry-control — it's a finding that MeanDist is sensitive to something that is random by design.

**Implication:** Calling seed a "geometry-control parameter" implies it actively controls geometry. A more accurate description: seed randomizes cluster membership, and MeanDist happens to be sensitive to cluster membership. This is a measurement property, not a control property.

**Alternative F — MeanDist CV is inflated by cluster definition sensitivity:**
The synchronized cluster is defined by a threshold (r > r_c or frequency correlation > threshold). Small changes in the threshold could produce large changes in cluster membership near the boundary. The observed CV of 0.30 might be inflated by borderline oscillators whose membership status is ambiguous.

**Test:** Vary the cluster definition threshold and recompute MeanDist. If CV is stable across thresholds, the variability is robust. If CV changes substantially, the threshold choice is a hidden confounder.

### 3.4 MeanDist Regime Robustness

**Classification-implied:** MeanDist measures graph distances which do not depend on ξ → MeanDist is naturally ξ-robust.

**Alternative G — Tautological robustness (the "d_ij doesn't change" argument):**
MeanDist = mean(d_ij for synchronized pairs). d_ij are fixed by the lattice. ξ changes K_ij = K₀ · exp(-d_ij/ξ) but does NOT change d_ij. Therefore MeanDist is ξ-robust by construction unless cluster membership changes substantially with ξ. The observed robustness is not evidence of a "geometry-control parameter class" — it's a consequence of defining MeanDist on a fixed lattice.

**Implication:** If MeanDist were defined differently (e.g., as the effective coupling-weighted distance Σ K_ij · d_ij / Σ K_ij), it WOULD be ξ-sensitive. The robustness is a definitional choice, not a discovery.

**Alternative H — Cluster membership is ξ-insensitive above a threshold:**
If synchronization is primarily determined by natural frequency similarity (not coupling strength), then once K₀ is above the synchronization threshold, further changes in ξ only fine-tune coupling strengths without changing cluster membership. The robustness of MeanDist to ξ is evidence that the synchronization threshold has been saturated, not evidence for a distinct parameter class.

**Test:** Compute the Jaccard similarity of synchronized cluster membership across ξ values. If similarity > 0.90, membership barely changes with ξ (Alternative H). If similarity < 0.70, membership changes substantially but MeanDist is still robust (requires a different explanation).

---

## 4. CONFOUNDERS

Hidden variables that could produce the observed patterns without distinct parameter classes.

### 4.1 Confounder: Mean-of-means stability (C1)

**Mechanism:** Ω is a mean. Means are inherently more stable than individual measurements. MeanDist is also a mean, but of a different quantity (distances, not frequencies). The different CVs (0.01 vs 0.30) might reflect different effective sample sizes, not different control mechanisms.

**Test:** Compute the effective sample size (ESS) for Ω and MeanDist accounting for correlations. If ESS_Ω >> ESS_MD, the CV difference is a sample-size artifact.

### 4.2 Confounder: Threshold dependence (C2)

**Mechanism:** Both Ω and MeanDist are computed over the "synchronized cluster." The cluster definition threshold affects both. If the threshold is chosen post-hoc, it could create or suppress apparent sensitivities.

**Test:** Pre-register the threshold. Vary it ±20% and check whether H9–H12 classifications are stable.

### 4.3 Confounder: Coupling kernel self-similarity (C3)

**Mechanism:** For exponential coupling f(d/ξ) = exp(-d/ξ), changing ξ is mathematically equivalent to rescaling the distance axis. In the continuum limit, the system at (ξ₁, d) is isomorphic to the system at (ξ₂, d · ξ₁/ξ₂). The ratio MeanDist/ξ should be approximately constant. The "robustness" of MeanDist to ξ may simply reflect that the cluster extent doesn't scale with ξ because the CLUSTER MEMBERSHIP threshold depends on frequency similarity, not distance.

**This is subtle:** It means that in one sense MeanDist SHOULD scale with ξ (if coupling range determines cluster extent) and in another sense it SHOULD NOT (if frequency similarity determines cluster extent). The V5.2 data says it does NOT scale. This tells us something about the synchronization mechanism — cluster membership is frequency-driven, not distance-driven — but it does NOT tell us that ξ and N belong to different parameter classes.

### 4.4 Confounder: Finite-size scaling (C4)

**Mechanism:** All V5.2 conclusions are at N=100 (except Phase 4 which went to N=800). At N=100, finite-size effects may dominate. The apparent ξ-robustness of MeanDist might be a finite-N saturation effect: at N=100, the cluster already spans the full graph for most seeds, so MeanDist is bounded by the graph diameter regardless of ξ.

**Test:** Check whether the synchronized cluster fills >80% of the graph at primary regime. If yes, MeanDist is bounded by D_max(graph), which is fixed → "robustness" is a ceiling effect.

### 4.5 Confounder: The "no parameter affects d_ij" tautology (C5)

**Mechanism:** None of the 5 swept parameters (ξ, K₀, s, N, law) change the underlying graph distances d_ij. Only N changes the set of available distances (by changing the graph size). Seed changes which distances are sampled (by changing cluster membership). All other parameters only affect coupling strengths. Therefore:

- Parameters that affect d_ij: N (structural), seed (sampling)
- Parameters that do NOT affect d_ij: ξ, K₀, s, law

The classification into "geometry-control" (N, seed) and "sync-control" (ξ, K₀) might be entirely explained by this structural fact: some parameters affect the underlying distances, others don't. This is not a discovery about attractor decomposition — it's a consequence of which quantities the metrics are computed from.

---

## 5. HYPOTHESIS VULNERABILITY RANKING

Ranked from most vulnerable to least vulnerable. For each: strongest supporting observation, strongest counter-explanation, and exact falsification condition.

### 5.1 H9 — MOST VULNERABLE

> Omega regime-sensitivity is driven by synchronization-control parameters.

**Strongest supporting observation:**
Ω shifted across the V5.2 ξ sweep while seed-CV was low. This is a single correlation in one phase of one experiment.

**Strongest counter-explanation (Alternative C):**
Ω shifts with ξ because ξ changes cluster membership, which changes the mean natural frequency of the synchronized population. This is a sampling effect, not a synchronization-dynamics effect. If true, ξ and seed operate through the same mechanism (cluster membership), and classifying them in different parameter classes is wrong.

**Exact falsification condition:**
```
FALSIFIED if: recomputing Ω using only oscillators in the INTERSECTION
of synchronized clusters across all ξ values shows |ΔΩ| < 2 × CV_seed(Ω).
(This means the Ω shift vanishes when cluster membership is held constant.)
```

**Why this is the most vulnerable:**
H9 makes a causal claim (synchronization dynamics → Omega) based on correlational evidence (Omega shifts with ξ). The counter-explanation (cluster membership → Omega) is simpler and requires no new mechanism. The test is straightforward and could falsify H9 in a single experiment.

### 5.2 H11 — HIGHLY VULNERABLE

> Synchronization-control and geometry-control parameters form partially independent classes.

**Strongest supporting observation:**
ξ affects Ω more than MD; N affects MD more than Ω. This is the entire evidence base.

**Strongest counter-explanation (Confounder C5):**
ξ and K₀ don't affect d_ij — they only affect coupling strengths. N affects d_ij by changing the graph. Seed affects which d_ij are sampled. The "independence" of the two classes is a structural consequence of which quantities the metrics are computed from (frequencies vs distances), not a property of the attractor.

**Exact falsification condition:**
```
FALSIFIED if: the sensitivity matrix S[i, j] can be explained by a single
latent variable (cluster membership fraction) that mediates all parameter
effects on both Ω and MD. In regression terms: the partial correlation
ρ(ξ, Ω | membership) ≈ 0 and ρ(N, MD | membership) ≈ 0.
```

### 5.3 H10 — MODERATELY VULNERABLE

> MeanDist robustness is primarily controlled by geometry parameters.

**Strongest supporting observation:**
MeanDist doesn't change much with ξ (V5.2 Phase 1).

**Strongest counter-explanation (Alternatives G + H):**
MeanDist is robust to ξ by definition (d_ij doesn't depend on ξ) and/or because cluster membership is saturated (most oscillators are synchronized). Neither explanation requires "geometry parameters" as a distinct class.

**Exact falsification condition:**
```
FALSIFIED if: at a regime where cluster membership is NOT saturated
(e.g., s high enough that <50% of oscillators sync), MeanDist becomes
ξ-sensitive (|ΔMD/Δξ| > 2 × CV_seed(MD)).
```

### 5.4 H12 — LEAST VULNERABLE

> The observed stability split reflects attractor decomposition.

**Strongest supporting observation:**
The stability pattern (Ω: seed-stable + ξ-sensitive; MD: seed-variable + ξ-robust) is a robust empirical pattern across V5.1 and V5.2.

**Strongest counter-explanation (Confounders C4 + C5):**
The pattern may be a generic property of any finite coupled-oscillator system where one metric is computed from frequencies (a mean of scalar values) and another is computed from distances (a mean of fixed topological quantities). If null-model simulations without synchronization show the same pattern, the "attractor decomposition" is a generic statistical property, not a TRM-specific finding.

**Exact falsification condition:**
```
FALSIFIED if: running the same metrics on a NULL MODEL (random frequency
assignments, NO coupling, NO synchronization dynamics — just computing Ω
as the mean of ω_i and MD as mean pairwise distance of ALL nodes) produces
the same qualitative stability pattern. This would mean the pattern is
a property of the measurement definitions, not the attractor.
```

**Why this is the least vulnerable:**
Even if the mechanism is wrong, the pattern itself is an empirical observation. Falsifying H12 requires showing that the pattern is not specific to the TRM attractor, which requires external comparisons not yet performed.

---

## 6. MINIMAL FALSIFICATION PROGRAM

### Design Principles

1. **Falsify the strongest claims first.** If H9 falls, the classification collapses.
2. **One decisive experiment per hypothesis.** No multi-phase campaigns for initial falsification.
3. **Reuse existing V5.2 infrastructure.** No new simulation code unless necessary.
4. **Statistical power over grid coverage.** More seeds at fewer points > more points at 3 seeds.
5. **Null-model comparisons.** Every observation must be compared against a no-synchronization baseline.

### Program: 4 Experiments, ~95 runs

#### Experiment M1: Fixed-Cluster Omega (Falsifies H9)

**Question:** Does Ω shift with ξ when cluster membership is held constant?

**Design:**
1. Run primary regime (ξ=1.80, K₀=1.15, s=0.08, N=100, exp) with 20 seeds.
2. Identify the synchronized cluster for each seed.
3. Run ξ ∈ {1.50, 1.80, 2.10} with the SAME seeds.
4. For each seed: identify the INTERSECTION of synchronized oscillators across all 3 ξ values.
5. Compute Ω using ONLY the intersection cluster.

**Runs:** 3 ξ × 20 seeds = 60 runs (reuses primary regime from step 1).

**Decision:**
```
If |Ω(ξ=2.10) − Ω(ξ=1.50)|_intersection < 2 × CV_seed(Ω):
    H9 is FALSIFIED. The Ω shift is a cluster-membership effect.
    ξ is NOT a synchronization-control parameter.

If |Ω(ξ=2.10) − Ω(ξ=1.50)|_intersection ≈ |Ω(ξ=2.10) − Ω(ξ=1.50)|_full:
    H9 SURVIVES this test. The Ω shift persists even with fixed cluster membership.
```

**Information gain:** Very high. Distinguishes between the two leading interpretations of the ξ-Ω relationship.

**Risk:** Cluster membership intersection may be small or empty for some seeds at extreme ξ values. Mitigation: use ξ ∈ {1.70, 1.80, 1.90} instead (narrower range, larger intersection).

### Experiment M2: Null-Model Baseline (Falsifies H12)

**Question:** Does a non-synchronizing system with the same measurement definitions produce the same stability pattern?

**Design:**
1. Generate 20 random natural frequency assignments (seeds) on the same N=100 lattice.
2. With NO coupling (K₀=0), NO dynamics: Ω_null = mean(ω_i), MD_null = mean(d_ij over all pairs).
3. Compute CV_seed(Ω_null) and CV_seed(MD_null).
4. Compare to TRM values: CV_seed(Ω) ~0.01, CV_seed(MD) ~0.30.

**Runs:** 20 seeds, no dynamics needed — pure computation. ~20 runs.

**Decision:**
```
If CV_seed(Ω_null) ≈ CV_seed(Ω_TRM) AND CV_seed(MD_null) ≈ CV_seed(MD_TRM):
    H12 is FALSIFIED. The stability pattern is a property of the measurement
    definitions, not the attractor.

If CV_seed(Ω_null) >> CV_seed(Ω_TRM) (null model is much less stable):
    H12 SURVIVES this test. Synchronization genuinely stabilizes Omega.
```

**Information gain:** Extremely high. Tests whether synchronization dynamics contribute ANYTHING to the stability pattern beyond what the metric definitions alone produce.

### Experiment M3: Saturation Boundary (Falsifies H10 + H11)

**Question:** Does MeanDist become ξ-sensitive when the synchronized cluster is NOT saturated?

**Design:**
1. Increase s from 0.08 to s ∈ {0.15, 0.20, 0.25} to reduce the synchronized fraction.
2. At each s, run ξ ∈ {1.60, 1.80, 2.00} with 5 seeds.
3. Compute MD sensitivity to ξ at each s.

**Runs:** 3 s × 3 ξ × 5 seeds = 45 runs (but s=0.08 can reuse V5.2 data).

**Decision:**
```
If |ΔMD/Δξ| increases with s (more sensitive at lower sync fraction):
    H10 is FALSIFIED. MeanDist robustness is a saturation artifact.
    H11 is WEAKENED. The parameter classes are not independent — they depend
    on the operating point.

If |ΔMD/Δξ| remains small at all s:
    H10 and H11 SURVIVE this test.
```

### Experiment M4: Fixed-Cluster MeanDist (Tests Confounders C2 + C5)

**Question:** Are both Ω and MD driven by a single latent variable (cluster membership)?

**Design:**
Using data from Experiment M1 (already collected):
1. Compute the synchronized fraction r_sync for each (seed, ξ) combination.
2. Regress Ω ~ r_sync + ξ and MD ~ r_sync + ξ.
3. Test whether ξ has significant partial effect after controlling for r_sync.

**Runs:** 0 additional — uses M1 data.

**Decision:**
```
If partial ρ(ξ, Ω | r_sync) ≈ 0 AND partial ρ(ξ, MD | r_sync) ≈ 0:
    A single latent variable (cluster membership) explains ALL parameter effects.
    H9, H10, H11 are JOINTLY FALSIFIED. There are no distinct parameter classes.

If partial ρ(ξ, Ω | r_sync) > 0 but partial ρ(ξ, MD | r_sync) ≈ 0:
    ξ genuinely affects Ω through synchronization dynamics (not just membership).
    But ξ does NOT affect MD. H9 survives, H10 survives partially, H11 survives.
```

### Total Minimal Program

| Experiment | Runs | Primary Target | Secondary |
|:-----------|:----:|:---------------|:----------|
| M1: Fixed-Cluster Ω | 60 | H9 | H11, Confounder C5 |
| M2: Null-Model Baseline | 20 | H12 | H9, Confounders C1, C5 |
| M3: Saturation Boundary | 30* | H10, H11 | Confounders C4, H |
| M4: Latent Variable | 0 | H9–H11 jointly | Confounder C2 |
| **Total** | **~95** | | |

*30 new runs if s=0.08 reuses V5.2. Otherwise 45.

---

## 7. NORMALIZATION REVIEW

### 7.1 The MeanDist Normalization Problem

**The issue:** MeanDist is computed as mean(d_ij) over synchronized pairs on a fixed lattice. At N=100, d_ij ∈ [1, D_max] where D_max ~ O(N^(1/d)) for a d-dimensional lattice. If the synchronized cluster grows with N, MeanDist grows with N by construction.

**Is normalization by graph diameter sufficient?**

No, for three reasons:

1. **D_max is a poor normalization for partial clusters.** If the synchronized cluster covers 60% of the graph, MeanDist/D_max ≠ MeanDist_full/D_max. The normalization depends on the cluster shape, which depends on the seed AND the regime parameters. This creates a circular dependency: you need to know the cluster shape to normalize, but the cluster shape is what you're trying to study.

2. **D_max is sensitive to outliers.** A single pair of oscillators at maximum distance inflates D_max without affecting the mean. Use D_90 (90th percentile distance) or D_eff instead.

3. **The scaling exponent matters.** On a d-dimensional lattice, mean pairwise distance scales as N^(1/d) for the full graph but may scale differently for a localized synchronized cluster (e.g., N^0 if the cluster is a fixed-size neighborhood). Without knowing the scaling exponent, you can't normalize properly.

### 7.2 Recommended Normalization Strategy

| Normalization | Formula | Use Case | Limitation |
|:--------------|:--------|:---------|:-----------|
| None (raw) | MD_raw | Baseline comparability with V5.2 | Not comparable across N |
| Graph diameter | MD / D_max | Cross-N comparison (upper bound) | D_max is outlier-sensitive |
| Graph effective diameter | MD / D_90 | Cross-N comparison (robust) | Still depends on cluster shape |
| Expected distance (null) | MD / E[d_ij] | Comparison against random cluster | E[d_ij] depends on the null model |
| Cluster diameter | MD / D_cluster | Within-cluster density | D_cluster is circular (depends on cluster) |
| Log-N scaling | MD / N^(1/d) | Continuum-limit extrapolation | Requires knowing d; assumes uniform cluster |

### 7.3 Impact on Current Conclusions

| If using this normalization… | Then the current conclusion that MD is ξ-robust… |
|:-----------------------------|:------------------------------------------------|
| MD / D_max | …is STRENGTHENED (D_max is fixed at given N) |
| MD / D_90 | …is UNCHANGED (same reason) |
| MD / D_cluster | …is WEAKENED if D_cluster changes with ξ |
| E[d_ij] (null model) | …is UNCHANGED (null model d_ij are also fixed) |

**The normalization issue primarily affects cross-N comparisons, not the ξ-robustness classification.** The ξ sweep in V5.2 Phase 1 holds N constant, so normalization does not affect the Ω-vs-MD comparison within that phase. However, the classification of N as "geometry-control" depends on the cross-N comparison (Phase 4), where normalization IS critical.

### 7.4 Recommendation

For V5.3:
1. **Report MD_raw as primary metric** (comparability with V5.2).
2. **Report MD/D_90 as a secondary metric** for all cross-N analyses.
3. **Report the synchronized fraction r_sync** alongside every Ω and MD value — this is the single most important confounder variable.
4. **Pre-register the normalization choice** before seeing V5.3 results.

---

## 8. RESEARCH FRONTIER

### 8.1 What Is Genuinely Supported

After skeptical review, the following statements survive:

1. **Ω and MD respond differently to ξ changes.** F1 + F2 establish this as an empirical fact at N=100, exp coupling, 3 seeds/point.
2. **Ω and MD have different seed-CVs at the primary regime.** F3 + F4 establish this at N=100, exp coupling, 10 seeds.
3. **The model's internal consistency is verified.** F9 (2189 tests, 0 failures) confirms the pipeline works as designed.

**That's it.** Three empirical observations. Everything beyond these is interpretation.

### 8.2 What Is Merely Suggestive

1. **"Seed stability and regime stability are distinct dimensions."** The data show different rankings. "Distinct dimensions" implies orthogonality, which requires showing that the seed-CV and regime-CV are uncorrelated across metrics. This has not been tested.

2. **"ξ is a synchronization-control parameter."** The data show ξ affects Ω. This could be through synchronization dynamics OR through cluster membership. The mechanism is unidentified.

3. **"N is a geometry-control parameter."** N affects MD by construction (larger graph → larger possible distances). This is structural, not discovered.

4. **"The attractor decomposes into sync and geometry components."** This is the strongest interpretive claim with the weakest direct evidence. No decomposition has been demonstrated — only a pattern of differential sensitivity.

### 8.3 What Remains Entirely Unverified

1. Whether ξ affects Ω directly (synchronization dynamics) or indirectly (cluster membership).
2. Whether the stability pattern persists in the continuum limit (N → ∞).
3. Whether the stability pattern is specific to TRM or generic to any finite oscillator system.
4. Whether the parameter classes (sync-control, geometry-control) have any reality beyond the measurement definitions.
5. Whether any of H9–H12 survive falsification testing.

---

## 9. RECOMMENDED FIRST EXECUTABLE V5.3 SUITE

### Suite: `V5_3_MinimalFalsification_Tests.cs` (SMF)

**Strategy:** Execute the 4 minimal experiments (M1–M4) in a single suite. This is the smallest experiment set capable of falsifying H9–H12.

**Test structure (12 tests):**

| Test ID | Name | Experiment | Purpose |
|:--------|:-----|:-----------|:--------|
| SMF-01 | Baseline20SeedEnsemble | M1/M2 prep | 20-seed primary regime ensemble for CV calibration and cluster identification |
| SMF-02 | IntersectionClusterIdentification | M1 | Identify synchronized cluster intersection across ξ values for each seed |
| SMF-03 | FixedClusterOmega_XiSweep | M1 | Compute Ω on intersection cluster at ξ ∈ {1.70, 1.80, 1.90} |
| SMF-04 | H9_FalsificationTest | M1 | Test whether Ω shift survives fixed cluster membership |
| SMF-05 | NullModelBaseline | M2 | Compute Ω_null and MD_null with zero coupling |
| SMF-06 | H12_FalsificationTest | M2 | Compare TRM stability pattern to null model |
| SMF-07 | SaturationBoundary_Sweep | M3 | Vary s to reduce sync fraction, sweep ξ at each s |
| SMF-08 | H10_FalsificationTest | M3 | Test whether MD becomes ξ-sensitive below saturation |
| SMF-09 | LatentVariableRegression | M4 | Regress Ω and MD on r_sync and ξ simultaneously |
| SMF-10 | H11_FalsificationTest | M4 | Test whether a single latent variable explains all effects |
| SMF-11 | NormalizationSensitivity | Cross-cut | Recompute all tests with MD/D_90 and check robustness |
| SMF-12 | ClaimDisciplineAudit | Meta | Assert all conclusions use pre-registered thresholds |

**Runs:** ~95 (60 for M1, 20 for M2, 15–45 for M3, 0 additional for M4).

### Pre-Registered Thresholds

| Threshold | Value | Used In |
|:----------|:------|:--------|
| CV_seed(Ω)_threshold | 0.02 (computed from 20-seed baseline) | H9-F1 |
| CV_seed(MD)_threshold | 0.30 (computed from 20-seed baseline) | H10-F1, H10-F2 |
| Significance level | p < 0.05 (bootstrap, 10,000 samples) | All hypothesis tests |
| Cluster membership similarity | Jaccard > 0.70 = "substantial overlap" | M1 interpretation |
| Sync fraction threshold | r_sync < 0.50 = "unsaturated" | M3 interpretation |
| Partial correlation threshold | |ρ_partial| < 0.10 = "no independent effect" | M4 interpretation |
| Normalization stability | Classification unchanged with MD/D_90 | SMF-11 |

---

## 10. CLAIM DISCIPLINE AUDIT

### 10.1 Violations Found in the Working Classification

| Claim | Problem | Correction |
|:------|:--------|:-----------|
| "ξ is a synchronization-control parameter" | Assumes mechanism from correlation | Replace with: "ξ affects Ω (observed). The mechanism (direct sync-dynamics vs cluster-membership mediation) is UNTESTED." |
| "N is a geometry-control parameter" | Tautological — N changes graph size, MD depends on graph size | Replace with: "MD depends on N by construction. Whether this reflects 'geometry control' or 'measurement definition' is UNTESTED." |
| "Seed is a pure geometry-control parameter" | "Pure" implies exclusivity | Replace with: "Seed affects MD more than Ω (observed). Whether seed affects Ω at all beyond statistical expectation is UNTESTED." |
| "Synchronization-control and geometry-control parameters form partially independent classes" | No independence test performed | Replace with: "Parameters show differential effects on Ω and MD (observed). Whether this reflects distinct classes or a single latent variable is UNTESTED." |

### 10.2 Classification of This Review

This review is classified as **SKEPTICAL AUDIT — INTERPRETATION-CRITICAL.** It contains:

- **SUPPORTED (3):** The three empirical observations in Section 8.1.
- **SUPPORTED (10):** The ten model-definition and verification facts in Section 1.
- **CONDITIONAL (5):** The five assumption-dependent findings in Section 2.
- **HYPOTHESIS (0):** This review introduces no new hypotheses.
- **ALTERNATIVE EXPLANATIONS (8):** Eight alternative mechanisms (A–H) in Section 3.
- **CONFOUNDERS (5):** Five hidden-variable explanations (C1–C5) in Section 4.
- **NOT CLAIMED:** All alternative explanations are presented as possibilities to test, not as claims.

### 10.3 Final Assessment

```
CONFIDENCE IN CURRENT CLASSIFICATION: LOW

The classification of parameters into sync-control and geometry-control classes
rests on a single experiment (V5.2 Phase 1, 3 seeds/point) showing differential
ξ-sensitivity of Ω and MD. This observation has at least three alternative
explanations that do not require distinct parameter classes:

  1. Cluster membership sampling (Alternative C)
  2. Tautological robustness of d_ij-based metrics (Alternative G)
  3. Saturation of cluster membership at primary regime (Alternative H)

None of these alternatives has been ruled out. The minimal falsification program
(4 experiments, ~95 runs) can rule them out or confirm them.

RECOMMENDATION: Do NOT proceed with the 210-run sensitivity matrix until
the minimal falsification program has been executed. If H9 is falsified by M1,
the entire classification framework must be rebuilt from scratch.
```

---

*This review was conducted under a skeptical stance: assume the working classification is wrong until evidence forces it to survive. All alternative explanations are presented as testable hypotheses, not as counter-claims. The minimal falsification program is designed to maximize information gain per run — 4 experiments, ~95 runs, capable of jointly falsifying H9–H12.*
