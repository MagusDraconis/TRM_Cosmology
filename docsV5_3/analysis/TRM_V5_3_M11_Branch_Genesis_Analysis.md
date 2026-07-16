# TRM V5.3 — M11: RecoverFP Branch Genesis Analysis

**Status:** RESEARCH DESIGN
**Date:** 2026-07-16
**Scope:** V5.3 branch-formation mechanism
**Basis:** N1–M10 results (98 tests, 0 failed)

---

## 1. Supported Findings (from N1–M10)

| # | Finding | Source | Confidence |
|:--|:--------|:-------|:----------:|
| F1 | Two RecoverFP ω outcome families exist: low (≈1.1) and high (≈2.0–2.7) | M7 | HIGH |
| F2 | Finite-N threshold at N≈66: below N≤65, >95% low-branch; at N≥66, high branch accessible | M5, M6 | HIGH |
| F3 | Branch gap is ≈1.0 — discrete separation, not continuous spread | M7 | HIGH |
| F4 | Branch split is NOT explained by cluster membership (M1), generic dynamics (M2), or pre-RecoverFP predictors (M8) | M1, M2, M8 | HIGH |
| F5 | K_std separates future branches at Epoch 1 (|Δ|/σ=1.24) while ω barely separates (0.21) | M9 | MODERATE |
| F6 | Post-Epoch-1 K_std intervention does NOT change branch assignment | M10 | MODERATE |
| F7 | Most seeds change branch across N (27/50 at N=65→67→72) — branch is not a fixed seed property | M8 | HIGH |
| F8 | N is the primary driver of branch accessibility, not s, K₀, or ξ individually | M4 | HIGH |

---

## 2. What Remains Unknown

| Question | Status | Why |
|:---------|:------|:----|
| What CAUSES the branch split? | UNKNOWN | M10 showed K_std is marker, not driver |
| Is the split continuous or discrete? | UNKNOWN | M9 tracked epochs but not per-node trajectories |
| Does a subset of nodes bifurcate first? | UNKNOWN | M9 used global matrix statistics only |
| Is the split deterministic or stochastic? | UNKNOWN | M8 showed weak pre-RecoverFP prediction |
| What property of the distance→coupling update creates two basins? | UNKNOWN | Core mechanism question |

---

## 3. Candidate Bifurcating Variables — Ranked

### 3.1 Spectral candidates

| Rank | Variable | Rationale | Testable? |
|:----:|:---------|:----------|:---------:|
| 1 | **Distance matrix leading eigenvalue (λ₁)** | Spectral radius of d captures global distance structure. Different convergence basins may have different spectral signatures detectable before scalar means diverge. | Yes — compute eig(d) per epoch |
| 2 | **Coupling matrix spectral gap (λ₁−λ₂)** | The gap between leading eigenvalues of K indicates how strongly the coupling is dominated by a single mode. Branch-dependent gap would emerge from different coupling topologies. | Yes — compute eig(K) per epoch |
| 3 | **Graph Laplacian Fiedler value** | The second eigenvalue of the graph Laplacian measures algebraic connectivity. If the RecoverFP update reshapes effective connectivity, the Fiedler value may bifurcate. | Yes — but requires reconstructing effective graph from K |
| 4 | **Distance matrix trace / mean** | Simpler than full spectrum. Already partially captured by d_mean in M9, but trace may be more sensitive. | Yes |

### 3.2 Structural candidates

| Rank | Variable | Rationale | Testable? |
|:----:|:---------|:----------|:---------:|
| 5 | **Per-node distance sum (d_i = Σ_j d_ij)** | Identifies outlier nodes whose distances from the rest of the graph differ between branches. A single "bridge node" could seed branch divergence. | Yes — compute per-node d_i per epoch |
| 6 | **Coupling degree distribution (K_i = Σ_j K_ij)** | Effective per-node coupling strength. If some nodes receive systematically different coupling in future-high vs future-low branches, this precedes global separation. | Yes — compute per-node K_i per epoch |
| 7 | **OmegaField spatial variance** | Within-seed spatial variation of ω across nodes. If the field develops spatial structure (clusters, gradients) that differs between branches, this would appear before the global mean diverges. | Yes — already partially captured in M6/M9 |

### 3.3 Dynamical candidates

| Rank | Variable | Rationale | Testable? |
|:----:|:---------|:----------|:---------:|
| 8 | **Epoch-to-epoch K change magnitude (||K_{e+1} − K_e||)** | The Frobenius norm of the coupling update. If future-high branches experience larger or differently-structured updates, this would be detectable. | Yes |
| 9 | **Distance→coupling update nonlinearity** | The Cupd function: K_ij = K₀·exp(−d_ij/ξ). The nonlinear mapping from d to K may amplify small distance differences differently in different regimes. | Yes — analyze d→K sensitivity per epoch |

---

## 4. Branch Genesis Model Candidates

### Model 1 — Distance Spectral Bifurcation
The distance matrix develops two distinct spectral structures during RecoverFP iteration. Future-high-branch seeds produce distance matrices whose leading eigenvalues grow more rapidly, creating stronger coupling gradients that amplify ω.

**Test:** Track λ₁(d) per epoch. If λ₁(d) separates before d_mean or K_std, spectral structure is the earliest signal.

### Model 2 — Coupling Percolation Threshold
The effective coupling graph crosses a percolation threshold at a critical epoch. Future-high-branch seeds cross this threshold earlier (or at a different epoch) than future-low-branch seeds, producing different K topology.

**Test:** Track effective graph connectivity metrics (largest component size, mean path length) per epoch. Compare epoch of threshold crossing between branches.

### Model 3 — Node-Level Bifurcation Cascade
A small subset of nodes (1–5) develops anomalous d_i or K_i values at an early epoch. These nodes seed a cascade that propagates through subsequent RecoverFP iterations, eventually pulling ω into the high branch.

**Test:** Identify top-k outlier nodes per epoch by per-node d_i or K_i. Test whether future-high-branch seeds have a consistent set of outlier nodes at Epoch 1.

### Model 4 — Initial-Condition Sensitivity
The branch split is determined by the interaction between the random graph (KS) and the random frequency assignment (Sm). This interaction is encoded in the first-epoch distance matrix but is too complex to predict from simple scalar statistics (M8). The split is effectively stochastic at the seed level.

**Test:** Run the same graph with different frequency assignments (or vice versa). If outcomes change, the interaction is causal.

---

## 5. M11 Experimental Design

### 5.1 Primary experiment: Per-epoch spectral + structural audit

**Setup:** N=67 (where branches first clearly separate), 50 seeds, RecoverFP with per-epoch diagnostics.

**Per-epoch diagnostics (expanded from M9):**
- Distance matrix: mean, std, trace, leading eigenvalue λ₁, spectral gap (λ₁−λ₂)
- Coupling matrix: mean, std, trace, leading eigenvalue, spectral gap
- Per-node: d_i = Σ_j d_ij (distance sum), K_i = Σ_j K_ij (coupling sum) — top 5 and bottom 5 nodes
- OmegaField: mean, std, spatial autocorrelation (Moran's I if available)
- K_update: Frobenius norm ||K_e − K_{e−1}||

**Analysis:**
1. For each epoch, compute |Δ|/σ between future-low and future-high seeds for ALL diagnostics.
2. Rank diagnostics by earliest epoch where |Δ|/σ > 0.8.
3. Identify the single diagnostic that separates FIRST.
4. For per-node diagnostics, identify whether specific node IDs consistently separate.

**Runs:** 50 seeds × 6 Sm calls = 300 Sm calls, ~60 seconds.

### 5.2 Secondary experiment: Controlled perturbation

If a specific diagnostic D (e.g., λ₁(d)) separates first, test whether perturbing D changes outcomes:

**Setup:** N=67, 30 seeds. After Epoch 1, perturb D toward the low-branch mean and continue RecoverFP.

**Compare:** Branch outcomes with perturbation vs baseline.

### 5.3 Tertiary experiment: Initial-condition swap

If Models 3 or 4 are viable, test the graph×frequency interaction:

**Setup:** N=67, 10 seeds. For each seed, generate the graph KS and frequency assignment independently. Run 4 combinations:
- Graph A + Frequencies A (same seed)
- Graph A + Frequencies B (shuffled)
- Graph B + Frequencies A
- Graph B + Frequencies B

**Compare:** Branch outcomes across combinations. If ω outcome follows the graph, the graph drives the split. If it follows frequencies, the frequency assignment drives it. If neither consistently, the interaction is complex.

---

## 6. Falsification Criteria

| Model | Falsified if |
|:------|:------------|
| M1 (distance spectral) | λ₁(d) does NOT separate before d_mean or K_std |
| M2 (coupling percolation) | Effective graph connectivity does NOT differ between branches at any epoch |
| M3 (node cascade) | No consistent set of outlier nodes exists across future-high seeds at Epoch 1 |
| M4 (initial-condition sensitivity) | Graph×frequency swap produces random (50/50) branch assignment |

---

## 7. Risks and Confounders

| Risk | Mitigation |
|:-----|:-----------|
| Spectral computation is expensive for N=67–72 | Use power iteration for leading eigenvalues only |
| Per-node diagnostics have high dimensionality | Pre-register: track top-5 and bottom-5 nodes by d_i and K_i |
| 50 seeds may not capture rare branch transitions | Accept that M11 is exploratory; expand if signal detected |
| Epoch 1 separation (M9) may be amplified by subsequent epochs | Compare Epoch 1 to random baseline (shuffled branch labels) |

---

## 8. Recommended M11 Suite

`V5_3_BranchGenesisSpectralAudit_Tests.cs` — 6 tests:

| Test | Purpose |
|:-----|:--------|
| BGSA-01 | Protocol loaded: diagnostics, thresholds, branch rule |
| BGSA-02 | Baseline run: 50 seeds at N=67, all per-epoch diagnostics |
| BGSA-03 | Spectral separation: λ₁(d), λ₁(K), spectral gaps by epoch |
| BGSA-04 | Per-node analysis: top/bottom nodes by d_i, K_i at Epoch 1 |
| BGSA-05 | K_update dynamics: ||K_{e+1}−K_e|| by epoch and branch |
| BGSA-06 | Earliest separator ranking + model support/falsification |

**Estimated runtime:** ~60 seconds.

---

## 9. Claim Discipline Review

| Statement | Classification |
|:----------|:--------------|
| Two ω branches exist in RecoverFP | SUPPORTED (M7) |
| Finite-N threshold at N≈66 | SUPPORTED (M5, M6) |
| K_std separates at Epoch 1 | SUPPORTED (M9) |
| K_std is a marker, not a driver | CONDITIONAL (M10, pending M10r fix) |
| Distance spectrum bifurcates first | HYPOTHESIS (not yet tested) |
| Node-level cascade drives branch split | HYPOTHESIS (not yet tested) |
| Attractor decomposition exists | NOT CLAIMED |
| H11, H12 confirmed | NOT CLAIMED |

---

*Analysis prepared 2026-07-16. All branch genesis models are working hypotheses. M11 is designed to falsify, not confirm. The goal is to identify the first quantity that genuinely bifurcates between low-ω and high-ω RecoverFP trajectories.*
