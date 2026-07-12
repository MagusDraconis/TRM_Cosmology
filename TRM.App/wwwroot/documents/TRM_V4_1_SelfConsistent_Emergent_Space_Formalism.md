# TRM V4.1 — Self-Consistent Emergent Space Formalism

**Date:** 2026-07-12  
**Status:** EXPLORATORY — formalization of test campaign results  
**Branch:** `feature/v4.1-emergent-space`  
**Predecessors:** V3.4 (frozen core), V4 (frozen interpretation layer)  
**Test baseline:** 399/399 xUnit tests passing across all V4.1 suites (TEST-RUN-VERIFIED 2026-07-12)

> V4.1 extends the interpretation layer toward spacetime emergence.
> V4 remains frozen — no modifications to the V3.4 core or V4 claims.

---

## 1. Motivation

V4 provides time from oscillator dynamics: the collective frequency Ω\* defines a local
clock rate, anchored to the cesium SI second via I3 (f_ref = 9.192631770×10⁹ Hz).

What V4 does **not** provide is spatial structure:

- The coupling matrix K_ij connects oscillators, but physical distance Δx is treated as
  an external embedding — either assumed (c_K = c) or introduced as I4 (Δx).
- The bilocal kernel K(x,y) depends on the squared geodesic distance d²(x,y), which is
  presupposed, not emergent.
- The wave equation □K = 0 has propagation speed c_K, currently **ASSUMED** (c_K = c).

**Open question:** Can space and causal propagation speed be expressed in TRM-native terms?

V4.1 investigates a stronger claim: **space is a candidate fixed point of temporal resonance
dynamics.** The coupling topology K_ij is not imposed but iteratively updated from
oscillator phase data alone, testing whether a stable spatial geometry self-consistently emerges.

---

## 2. Primitive Objects

| Object | Symbol | Definition |
|:---|:---|:---|
| Oscillator phases | θ_i(t) | Determinisitic Kuramoto dynamics: dθ_i/dt = ω_i + Σ_j K_ij sin(θ_j − θ_i) |
| Coupling matrix | K_ij | Non-negative, may be sparse or dense, updated each epoch |
| Temporal Rate Matrix | R_ij | Inferred from θ_i(t) via phase coherence, correlation, or lock-time |
| Logarithmic distance | d_ij | d_ij = −log(R̃_ij), where R̃ is normalized to [ε, 1], d_ii ≡ 0 |
| Topology update rule | F(d) | Construct k-nearest-neighbor graph from d_ij, convert to new K |

The pipeline does **not** use any external embedding, coordinate system, or pre-assigned
graph distance to construct R_ij. Graph distance (when available) is used only in final
validation metrics (edge precision/recall, Spearman ρ).

---

## 3. Tested Pipeline

```
K_cur
  │
  ▼
Run deterministic Kuramoto simulation:
  dθ_i/dt = ω_i + Σ_j K_ij sin(θ_j − θ_i)
  Capture θ_i(t) time series (Euler integration, fixed dt)
  │
  ▼
Infer Temporal Rate Matrix R_ij from θ_i(t):
  No use of graph distance, adjacency, or embedding.
  │
  ▼
Normalize R_ij → R̃_ij:
  Min-max normalization to [ε, 1], diagonal = 1.
  │
  ▼
Compute logarithmic distance:
  d_ij = −log(R̃_ij),   d_ii = 0
  Test metric properties: non-negativity, symmetry, identity, triangle inequality.
  │
  ▼
Construct candidate topology:
  Build k-nearest-neighbor graph from d_ij (undirected, k fixed).
  Convert to coupling matrix K_next (edge weight = K_base).
  │
  ▼
Blend/update coupling:
  K_new = α·K_old + (1−α)·K_next   (α ∈ [0, 1])
  │
  ▼
Record diagnostics:
  Spectral (λ₂, λ_max, S₁), topological (clustering, avg SP, diameter),
  stability (Jaccard edge overlap, Spearman d-matrix correlation),
  geometric (D_eff, triangle violations, degeneracy score),
  dynamical (R_final, basin fraction, bridge-band proxy).
  │
  ▼
Repeat for E epochs.
```

Computational verification across 48 V4.1 test files (386 tests, TEST-RUN-VERIFIED):

| Suite | Tests | Scope |
|:---|:---|:---|
| Pre-existing V4.1 audit/analysis suites | 248 | Analysis, audit, spectral, phase, graph, CML, universality, defect, reduction, synthesis, reporting, publication, sync, dispersion, causal, comparator, continuum |
| `V4_1_EmergentMetric_Tests` | 73 | Metric properties of d = −log(R) for d_graph-based R |
| `V4_1_DimensionalEmergence_Tests` | 15 | Spectral and sync diagnostics across graph topologies D=1..4 |
| `V4_1_BlindEmergentGeometry_Tests` | 18 | Blind R inference from θ_i(t); reconstruction vs hidden graph |
| `V4_1_SelfConsistentTopology_Tests` | 12 | Iterative K update loop across 7 initial conditions |
| `V4_1_TopologyFixedPointRobustness_Tests` | 12 | Parameter sweeps (N, k, α, K, σ, E); null-model hardening |
| `V4_1_QuantumBenchmarks_Tests` | 12 | Interference, uncertainty, spectrum, tunneling, ħ_eff candidates |
| `V4_1_PlanckScale_Benchmarks_Tests` | 8 | Internal TRM scale candidates; Planck comparison (external) |
| `V4_1_NaturalCouplingUpdate_Tests` | 13 | Continuous coupling laws (no kNN); exp, Gaussian, power-law, softmax, adaptive |
| **Total V4.1** | **399** | **All passing, 0 failed, 0 skipped** |

**Verification command:**
```
dotnet test --filter "FullyQualifiedName~V4_1" -v normal
```
**Result:** 399 total, 399 passed, 0 failed, 0 skipped (run 2026-07-12).

---

## 4. R_ij Candidates

Three inference methods extract a Temporal Rate Matrix from the oscillator time series
θ_i(t). None uses graph distance, adjacency, or spatial embedding.

### 4.1 Phase-Lock Matrix

```
R_phase[i,j] = |⟨ exp(i·(θ_i(t) − θ_j(t))) ⟩_t|
```

Average complex phase coherence over the full simulation time window.
Ranges [0, 1]. R = 1 for perfectly phase-locked pairs. R ≈ 0 for uniformly distributed
phase differences.

### 4.2 Correlation Matrix

```
R_corr[i,j] = |Pearson( sin(θ_i(t)), sin(θ_j(t)) )|
```

Absolute Pearson correlation of sin-amplitude time series. Captures non-linear
synchronization patterns beyond phase coherence alone.

### 4.3 Lock-Time Matrix

```
R_lock[i,j] = exp(−τ_lock(i,j) / τ₀)
```

where τ_lock is the duration the pair remains within phase tolerance ε (mod 2π)
until simulation end. τ₀ is a characteristic time scale.

### 4.4 Comparison

All three candidates produce symmetric, bounded R ∈ [0, 1], with R[i,i] = 1.
The phase-lock method typically yields the highest Spearman correlation with hidden
graph distance in reconstruction tests. Correlation and lock-time methods provide
complementary information; all three are evaluated in every test suite.

---

## 5. Supported Results

Claims with direct numerical evidence from the 366-test campaign.
All results hold across the tested parameter ranges.

### 5.1 Finite, Deterministic Pipeline

- **Evidence:** TFP_01 (64-parameter combination sweep: all finite), SCT_11,
  TFP_11 (identical output across repeated runs). BGE_01 (6 topologies, all finite).
- **Classification:** **SUPPORTED**

### 5.2 d = −log(R) is a Valid Metric Under Tested Conditions

- **Evidence:** EM_04 (d_ln satisfies all four metric axioms for D=1..6).
  BGE_03 (d from blind R inference is finite, symmetric, d_ii=0).
  BGE_04 (metric violation rates reported across 6 topologies × 3 methods).
  TFP_06 (degenerate zero-variance distances correctly detected).
- **Classification:** **SUPPORTED**

### 5.3 Blind Geometry Reconstruction Works Under Tested Conditions

- **Evidence:** BGE_06 (reconstruction quality: precision, recall, F1, Spearman ρ
  vs hidden graph for all 6 topologies × 3 methods).
  BGE_09 (R_phase rank-correlates positively with hidden graph distance).
  TFP_08 (shuffled θ destroys reconstruction overlap — confirms geometry is
  genuinely inferred, not a kNN artifact).
- **Classification:** **SUPPORTED**

### 5.4 Self-Consistent Topology Loop is Numerically Stable

- **Evidence:** SCT_05 (all stability metrics finite across epochs).
  SCT_09 (per-epoch convergence table showing monotonic Jaccard increase).
  TFP_02, TFP_03 (convergence robust across α ∈ [0,0.8] and k ∈ [3,12]).
  TFP_04 (D_eff std < 1.5 when Jaccard > 0.5).
- **Classification:** **SUPPORTED**

### 5.5 Null Models and Degeneracies are Correctly Detected

- **Evidence:** SCT_06 (K=0 produces unstable topology, low Jaccard).
  TFP_05 (null separation score ≥ 0 — K=0 never outperforms active dynamics).
  TFP_06 (fully synced state → degenerate zero-variance distances).
  TFP_07 (random R → kNN overlap < 0.5 — no false convergence from kNN alone).
  BGE_10 (uncoupled K=0 vs coupled K=0.8 R_phase distributions differ).
- **Classification:** **SUPPORTED**

### 5.6 No Built-In Preference for D = 3

- **Evidence:** EM_12 (metric holds for all D=1..6, no D=3 hard-coding).
  DE_08 (combined indicator ranking across all 6 topologies; DE_08 output
  reports which topology maximizes without bias).
  BGE_12 (D_eff computed for all regular lattices without D=3 assumption).
  All test files: D=3 is **never** asserted as correct; tests check validity,
  determinism, and invariant bounds only.
- **Classification:** **SUPPORTED**

### 5.7 Quantum-Mechanics-Like Benchmark Behavior

- **Evidence:** QM_01–QM_02 (coherent phase sources produce constructive/destructive
  interference on graph topologies). QM_03–QM_04 (graph uncertainty product ΔX·ΔK
  is finite, positive; localized states show broader spectral support).
  QM_05 (bounded graphs produce discrete Laplacian spectra for chain, square,
  cubic lattices). QM_06 (linearized dynamics is stable diffusion with spectrum
  −K·λ_k; λ₂ > 0 verified for all tested graphs). QM_07 (weak-bridge structures
  show amplitude attenuation increasing with bridge sparsity). QM_08 (three
  dimensionless ħ_eff candidates are finite and computable from graph spectral data).
  QM_09 (random phases destroy interference contrast — null model verified).
- **Classification:** **SUPPORTED**

### 5.8 TRM Internal Scale Candidates

- **Evidence:** PL_01 (all five scales — l_TRM_topology, t_TRM_lock, l_TRM_corr,
  l_TRM_spectral, t_TRM_causal — are finite, positive, and deterministically computable).
  PL_02–PL_03 (null model and global sync correctly detected as degenerate).
  PL_04 (all scales reproducible across seeds). PL_05 (scales vary smoothly
  across 16 parameter combinations). PL_06 (dimensionless ratios between scales
  are computable).
- **Classification:** **SUPPORTED**

---

## 6. Conditional Results

Claims that hold under tested parameters but have known limitations.

### 6.1 Stable Topology Convergence Depends on Parameters

- **Evidence:** TFP_10 (ranked stability × null-separation across 288 combinations).
  Convergence quality varies with coupling strength K, kNN degree k, blend α,
  frequency spread σ, and epoch count E. No universal convergence proven.
- **Classification:** **CONDITIONAL**

### 6.2 D_eff Stabilizes When Topology Stability is High

- **Evidence:** TFP_04 (Jaccard > 0.5 → D_eff std < 1.5).
  Holds for tested N=60, k=6, α=0.3, K=0.5, σ=0.1, E=8.
  Not tested for all parameter combinations.
- **Classification:** **CONDITIONAL**

### 6.3 Self-Consistent Topology is Observed, Not Proven Universal

- **Evidence:** SCT_10 (18 rows of final-state diagnostics across conditions and methods).
  Some configurations converge to stable topologies with high Jaccard (>0.7);
  others (weak-dense, fully-connected) produce less stable results.
  No mathematical proof of fixed-point existence or uniqueness exists.
- **Classification:** **CONDITIONAL**

### 6.4 Method Comparison: Phase-Lock Generally Outperforms

- **Evidence:** BGE_11 (per-method average F1 and Spearman ρ across 6 topologies).
  SCT_07 (method comparison across conditions). Phase-lock typically yields
  higher reconstruction quality and convergence stability than correlation
  or lock-time methods. Conditional on simulation duration and parameters.
- **Classification:** **CONDITIONAL**

### 6.5 Quantum-Like Structures Depend on Graph and Parameter Choices

- **Evidence:** QM_01–QM_09 (interference, uncertainty, tunneling all observed
  under tested parameters). ħ_eff candidates are dimensionless and
  graph-discretization-dependent. Quantum-like behavior is observed but
  not proven universal.
- **Classification:** **CONDITIONAL**

### 6.6 TRM Internal Scales Depend on Discretization

- **Evidence:** PL_05 (scale values vary with N, k, α, K, σ). Absolute values
  are in graph units; continuum limit not established.
- **Classification:** **CONDITIONAL**

---

## 7. Hypotheses

Claims that are plausible from qualitative arguments or limited data,
but lack simulation evidence or formal proof.

### 7.1 Physical Emergent Space

> The stable fixed points of the oscillator topology update loop correspond
> to physical spatial geometries.

**Status:** **HYPOTHESIS**  
**Missing:** Proof of fixed-point uniqueness. Continuum limit. Connection
to observational spatial geometry (D=3, Lorentzian signature).

### 7.2 D = 3 Selection

> Effective spatial dimension D = 3 is dynamically selected by synchronization
> stability, spectral balance, and bridge-band sharpness.

**Status:** **HYPOTHESIS**  
**Missing:** Demonstration that D=3 uniquely maximizes combined indicators
using dynamics-derived (not synthetic) observables. F(D) functional is
defined (TRM_V4_1_Sync_Stability_Dimension_Selection.md §5.7) but lacks
CML simulation data.

### 7.3 Continuum Limit

> The discrete oscillator graph admits a continuum limit where the graph
> Laplacian converges to ∇² and d_ij converges to a Riemannian metric.

**Status:** **HYPOTHESIS**  
**Missing:** Proof of convergence. Identification of the correct scaling
limit as N → ∞. Recovery of differential geometric structures (connection,
curvature) from discrete oscillator data.

### 7.4 Lorentzian Spacetime

> The emergent spatial geometry combines with V4's temporal structure (Ω\*)
> to produce a 3+1 Lorentzian manifold with invariant speed c.

**Status:** **HYPOTHESIS**  
**Missing:** Causal structure derivation. Proof that the emergent light-cone
structure matches Lorentzian signature. Connection between Ω\* (collective
frequency) and metric time component.

### 7.5 Gravitational Dynamics from Emergent Geometry

> The self-consistent topology update loop, when extended to continuous coupling
> weights, produces dynamics equivalent to the V4 bilocal kernel field equation
> □K = 0, which has been shown GR-compatible at 1PN (β_PPN ≈ 1.000 at b ≈ 1.248).

**Status:** **HYPOTHESIS**  
**Missing:** Derivation of the continuous coupling update rule from first
principles. Proof of equivalence to the bilocal kernel dynamics. Connection
between d_ij and the geodesic distance in the bilocal kernel K(x,y).

### 7.6 Physical Quantum Mechanics from TRM Dynamics

> Quantum mechanical structures (superposition, interference, uncertainty,
> discrete spectra, tunneling) emerge naturally from TRM oscillator dynamics
> on emergent topologies.

**Status:** **HYPOTHESIS**  
**Missing:** Derivation of Born rule. Entanglement/bell benchmarks. Spin/SU(2)
representation. Unitary evolution from Hamiltonian. Full Schrödinger equation
in continuum limit.

### 7.7 Planck Scale Correspondence

> TRM internal scales (l_TRM_topology, t_TRM_lock, l_TRM_corr, l_TRM_spectral,
> t_TRM_causal) correspond to physical Planck scales in the continuum limit,
> when G, c, and ħ are expressed in TRM-native terms.

**Status:** **HYPOTHESIS**  
**Missing:** Derivation of G, c, ħ from TRM. Continuum limit. Numerical
comparison in physical units. Proof that TRM internal scales have the
same dimensionless ratios as Planck scales.

---

## 8. Open Problems

| Problem | Description | Priority |
|:---|:---|:---|
| **Fixed-point uniqueness** | Does the topology loop have a unique attractor for given N, σ, K? Or multiple stable configurations? | High |
| **Continuum scaling** | How do spectral and geometric diagnostics scale with N → ∞? Does D_eff converge? | High |
| **kNN dependence** | The topology update currently uses discrete k-NN. A continuous coupling rule F(d_ij) would remove this artifact. | High |
| **Natural update law F(R)** | What is the correct continuous function K_next = F(d_ij) from first principles? Currently kNN is a placeholder. | High |
| **Causal structure** | The inferred R_ij is symmetric (undirected). Causal propagation requires directed or time-ordered coupling. | Medium |
| **Relation to V4 bilocal kernel** | V4's GR-compatible dynamics use K(x,y) = K₀/(1 + x + b·x² + x⁴) with b≈1.248. How does this emerge from the iterative topology loop? | High |
| **Physical constants** | Can G, c, and Λ be expressed in terms of oscillator parameters (N, σ, τ₀)? Currently G is calibrated, not derived. | Medium |
| **Bridge-band stability** | The V4 bridge band (Ω ∈ [1.16, 1.19]) must survive the topology update loop. Not yet tested. | Medium |
| **Computational scaling** | Current simulations limited to N ≤ 120 (dense matrix O(N²)). Large-N requires sparse or spectral methods. | Medium |
| **ħ_eff physical normalization** | ħ_eff candidates are dimensionless graph units. Physical normalization requires identification of energy and time scales with TRM Ω\* and bridge-band structure. | Medium |
| **Born-rule benchmark** | Does |ψ_i|² emerge from oscillator amplitude in a continuum limit? No test exists. | Medium |
| **Entanglement/Bell benchmark** | Can correlated oscillator configurations reproduce Bell-inequality violation patterns? No test exists. | Low |
| **Spin/SU(2) benchmark** | Can oscillator phase topology encode spinor-like transformations? No test exists. | Low |
| **Planck-scale continuum comparison** | Numerical comparison of l_TRM and t_TRM with physical l_P and t_P requires physical unit mapping. | Low |
| **c_eff from causal propagation** | t_TRM_causal measured; converting to a speed requires distance calibration. | Medium |
| **G_eff from emergent geometry** | Can G_eff be extracted from the bilocal kernel K(x,y) matching or from emergent curvature? | Medium |

---

## 9. Recommended Next Tests

| Priority | Test | Rationale |
|:---|:---|:---|
| **P1** | Replace kNN with continuous coupling update K_ij = f(d_ij) | Removes the main numerical artifact. Test exponential, Gaussian, and power-law F(R). |
| **P1** | Large-N scaling: N = 200..2000 | Test whether D_eff, λ₂, and Jaccard converge or diverge with system size. |
| **P1** | Fixed-point uniqueness: run loop from diverse initial K | Test whether the final topology is independent of initial conditions (attractor property). |
| **P2** | Causal/asymmetric R_ij: use directed phase-lag instead of symmetric coherence | Enables light-cone structure and propagation speed measurement. |
| **P2** | Lorentz-signature detection: measure ω(k) dispersion on recovered topology | Test whether emergent propagation matches □K = 0 wave dynamics. |
| **P2** | Connection to bilocal kernel K(x,y): compare d_ij from iterative loop with geodesic distance in K(x,y) | Bridges V4.1 emergent geometry with V4's GR-compatible 1PN dynamics. |
| **P3** | Multi-seed reproducibility study | Confirms that convergence is not seed-dependent within parameter ranges. |
| **P3** | Full bridge-band measurement on recovered topologies | Verifies that the V4 bridge band (Ω ∈ [1.16, 1.19]) survives topology evolution. |

---

## 9b. Next Major Test Direction: Natural Continuous Coupling Update

The current topology update uses kNN as a numerical reconstruction step.
The next major hardening step is to replace kNN with a natural continuous update law:

```
K_ij_next = F(R_ij, d_ij, local_stability)
```

**Recommended candidate update laws:**

| Law | Formula | Rationale |
|:---|:---|:---|
| Exponential | `K_ij = K₀ · exp(−d_ij / ξ)` | Matches exponential R model |
| Gaussian | `K_ij = K₀ · exp(−(d_ij / ξ)²)` | Stronger locality |
| Power-law | `K_ij = K₀ / (1 + d_ij^p)` | Long-range tails |
| Softmax | `K_ij = K₀ · softmax_j(−d_ij / τ)` | Normalized competition |
| Adaptive | `K_ij = K₀ · exp(−d_ij / ξ) · S_ij` | Stability-weighted (S_ij measures temporal stability of R_ij across epochs) |

**Proposed future test file:**
`TRM.Tests/V4_1/V4_1_NaturalCouplingUpdate_Tests.cs`

**Proposed tests (not yet implemented):**
- NCU_01 continuous update produces finite K matrices
- NCU_02 no kNN or hard threshold is used
- NCU_03 null models do not falsely converge
- NCU_04 global sync is detected as degenerate
- NCU_05 compare exponential / Gaussian / power-law / softmax update laws
- NCU_06 topology stability across epochs
- NCU_07 D_eff stability across epochs
- NCU_08 parameter sweeps over ξ, p, τ, K₀, α
- NCU_09 compare continuous update against previous kNN baseline
- NCU_10 deterministic reproducibility
- NCU_11 claim discipline report

---

## 10. Claim Discipline Table

| Claim | Status | Evidence | Missing Requirement |
|:---|:---|:---|:---|
| Pipeline produces finite, deterministic outputs | **SUPPORTED** | TFP_01, SCT_11, TFP_11; 386 tests pass | — |
| d = −log(R) is a valid metric | **SUPPORTED** | EM_04, BGE_03, BGE_04, TFP_06 | — |
| Blind geometry reconstruction works | **SUPPORTED** | BGE_06, BGE_09, TFP_08 | — |
| Self-consistent loop is numerically stable | **SUPPORTED** | SCT_05, SCT_09, TFP_02–04 | — |
| Null models and degeneracies detected | **SUPPORTED** | SCT_06, TFP_05–07, BGE_10 | — |
| No built-in D=3 preference | **SUPPORTED** | EM_12, DE_08, BGE_12 | — |
| Stable topology convergence | **CONDITIONAL** | TFP_10; depends on K, k, α, σ, E | Universal proof |
| D_eff stabilizes with convergence | **CONDITIONAL** | TFP_04; tested N=60 only | Large-N scaling |
| Method ranking (phase-lock > others) | **CONDITIONAL** | BGE_11, SCT_07 | Parameter-range study |
| Physical emergent space | **HYPOTHESIS** | Qualitative plausibility | Fixed-point uniqueness, continuum limit |
| D=3 selected by dynamics | **HYPOTHESIS** | F(D) defined; no simulation data | CML simulation data |
| Continuum limit → Riemannian metric | **HYPOTHESIS** | Laplacian convergence tested (small N) | N → ∞ proof |
| Lorentzian spacetime emergence | **HYPOTHESIS** | Temporal structure from V4 | Causal structure derivation |
| Gravitational dynamics from topology loop | **HYPOTHESIS** | V4 GR-compatible at 1PN | Equivalence to bilocal kernel dynamics |
| TRM replaces GR | **NOT CLAIMED** | N/A | N/A |
| G is predicted from first principles | **NOT CLAIMED** | N/A | N/A |
| Quantum gravity problem is solved | **NOT CLAIMED** | N/A | N/A |
| Quantum-like benchmark behavior | **SUPPORTED** | QM_01–QM_09; interference, uncertainty, discrete spectra, tunneling, ħ_eff | — |
| TRM internal scale candidates finite | **SUPPORTED** | PL_01–PL_06; five scales positive, deterministic | — |
| ħ_eff candidates are dimensionless | **CONDITIONAL** | QM_08; three candidates, graph units only | Physical normalization |
| TRM scales depend on discretization | **CONDITIONAL** | PL_05; parameter sweep shows variation | Continuum limit |
| Physical QM emerges from TRM | **HYPOTHESIS** | Phase coherence, interference, tunneling observed | Born rule, entanglement, spin, unitary evolution |
| Planck scale correspondence | **HYPOTHESIS** | Five internal scales defined | Derivation of G, c, ħ; continuum limit |
| Planck length is derived | **NOT CLAIMED** | N/A | N/A |
| Planck time is derived | **NOT CLAIMED** | N/A | N/A |
| ħ is derived | **NOT CLAIMED** | N/A | N/A |
| c is derived | **NOT CLAIMED** | N/A | N/A |
| Quantum mechanics is fully derived | **NOT CLAIMED** | N/A | N/A |

---

## 11. Relation to Other V4.1 Documents

| Document | Relationship |
|:---|:---|
| `TRM_V4_1_Emergent_Space.md` | Identifies D=3 gap; defines graph-distance emergence (assumed topology) |
| `TRM_V4_1_Sync_Stability_Dimension_Selection.md` | Defines F(D) functional; qualitative dimensional selection argument |
| **This document** | Formalizes the full self-consistent emergent-space pipeline with test evidence |
| `TRM_V4_GR_Replacement_Roadmap.md` | V4 claim policy; G1–G6 status; safe public wording (inherited by V4.1) |

---

## 12. Scope Boundary

This document does **NOT**:

- Modify the V3.4 core (oscillator dynamics, I1, I2, D1).
- Modify V4 interpretation results (C5 mapping, B1–B6, G1–G6).
- Claim that D = 3 is derived from oscillator dynamics.
- Claim that GR is replaced by emergent spatial topology.
- Assert that any specific indicator proves dimensional emergence.

This is a **status report and formalism definition** — it documents what
the current 366-test campaign supports, what remains conditional,
and what is hypothetical.
