# 🌌 Temporal Rate Matrix / Temporal Quantum Matrix — V4 Interpretation Layer (active)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Zenodo DOI](https://img.shields.io/badge/DOI-10.5281%2Fzenodo.20681734-0A7BBB?logo=zenodo&logoColor=white)](https://doi.org/10.5281/zenodo.20681734)
[![Live Demo](https://img.shields.io/badge/Live%20Demo-trm.runasp.net-2ea44f?logo=blazor)](https://trm.runasp.net/)

> **V3.4 Core:** Frozen — two irreducible structural inputs (I1, I2). **V4 Interpretation Layer:** Weak-field 1PN GR-compatible, full Lorentzian tensor bridge, strong-field mapped. 184/184 xUnit tests passing.

This repository contains the numerical implementation and analysis tools for the **Temporal Rate Matrix (TRM)** / **Temporal Quantum Matrix (TQM)** framework. The V3.4 core theory is frozen; the V4 interpretation layer bridges oscillator quantities to gravitational observables.

## 🌐 Live App

The TRM web interface is deployed at **[trm.runasp.net](https://trm.runasp.net/)** — explore the PPN validator, quantum loop finiteness, propagator scaling comparison, and cross-scale GR validation dashboards interactively.


## 🔬 Overview

TRM/TQM describes collective frequency emergence in finite coupled phase-oscillator lattices:

\[
\frac{d\theta_i}{dt} = \omega_i + \sum_j K_{ij} \cdot f(\theta_i - \theta_j)
\]

The framework is classified as a **constrained effective theory** — not zero-parameter, not purely phenomenological, but fully closed under exactly two irreducible structural inputs. The oscillator dynamics provide the synchronization mechanism; the constraints provide the mode selection.

### The Two Irreducible Inputs

| Input | Statement | Status |
|:---|:---|:---|
| **I1** | Closure-family ansatz \(p = q + m\) with integer \(m \ge 0\) | Irreducible structural input |
| **I2** | Bridge-band prior \(\Omega \in [1.16, 1.19]\) | Irreducible axiom / domain-specific empirical prior |

Given I1 + I2, the admissible core \(q\text{Core} = \{16, 17, 18\}\) and the closure mode \(m = 3\) follow deductively — no further free parameters are introduced. The formal proof scaffold FP01–FP31 closes without gaps. The bridge band is **imposed** as a structural input (BD1–BD6), not dynamically selected by the oscillator equations. The rational ladder observed in simulation (E1) is an **induced consistency structure**: the collective frequency \(\Omega^*\) is set by system parameters, and integer mode identification follows from rounding, not from dynamical quantization.

### Claim Boundaries

| Classification | Scope |
|:---|:---|
| **PROVEN** | Topological constraint \(q\Omega \in \mathbb{Z}\); FP01–FP31 scaffold closure; \(m = 3\) uniqueness under I1+I2; asymptotic limits |
| **ASSUMED (irreducible)** | I1 (closure-family ansatz); I2 (bridge-band prior) |
| **EMPIRICAL** | E1 rational consistency (induced ladder, not dynamic quantization); bridge-band range from CML grid scan |
| **OPEN** | First-principles derivation of I1; first-principles derivation of I2; physical frequency scale for \(\omega_i\); unique action functional |

### Falsification Conditions

TRM/TQM is falsified if any of the following are observed:
- No stable bridge band in any oscillator system with \(N \in [10, 25]\) (E1 simulation + real-world laser/EEG)
- Different bridge bands in structurally identical systems (parameter rescan)
- \(m \neq 3\) consistently dominates under I1+I2 constraints (BD4 mode competition)
- \(\Omega^*\) distribution incompatible with \([1.16, 1.19]\) across varied \(\omega_i\) baselines
- External calibration of \(\varphi\) yields value outside \([0.16, 0.19]\) and no \(m = 3\) exists

### What Is Explicitly Not Claimed

- \(\Omega\) is dynamically selected by the lattice equations (BD1–BD5: false — imposed)
- \(\gamma = 0.85\) is a fundamental physical constant (algebraic reciprocal of 20/17 only)
- The bridge band emerges from oscillator dynamics (CLASS D — purely imposed)
- I2 is derivable from gravitational potential, MOND \(a_0\), or transport parameters (all routes blocked)
- The rational ladder is a dynamical quantization effect (E1: induced consistency, not dynamic selection)
- TRM/TQM is a zero-parameter theory (it has exactly 2 irreducible inputs)

---

## 🚀 V4 Interpretation Layer (active on `feature/v4-interpretation-layer`)

The V4 interpretation layer maps TRM internal quantities (Ω*, φ, K_ij) to physical observables without modifying the frozen V3.4 core. It is **Bedeutungsentwicklung** (meaning development), not **Theorieentwicklung** (theory development).

### V4 Achievements

| Module | Status | Result |
|:---|:---|:---|
| **B1–B2** | STABLE | K1×F1 coupling mechanism; SPARC phenomenology |
| **B3A–B3B** | DERIVED | ∇²K=0 uniqueness; 1/r from graph Laplacian coupling defect |
| **B3C** | CALIBRATED | α↔M via energy deficit; f_ref = 9.192631770×10⁹ Hz (cesium SI second) |
| **B4** | EFFECTIVE | □K=0 wave dynamics; c_K = c |
| **B5–B6** | COMPLETE | 10-observable dictionary; 15/15 benchmarks pass |
| **G1** | COMPATIBLE | 1PN β ≈ 1 via optimized kernel K₀/(1+x+1.25x²+x⁴) |
| **G2** | SUPPORTED | Full Lorentzian tensor bridge: metric extraction, 2+1 GW polarizations, ω=ck |
| **G3** | SOLVED (scalar) | r_H ≈ 2.275GM (+13.8%); within EHT bounds; falsifiable |
| **G4** | STRUCTURALLY PREFERRED | b=1 is natural fixed point (f''=0, ε minimum, IR attractor) |

### Kernel Family

The central V4 object is the bilocal coupling kernel:

\[
K(x) = \frac{K_0}{1 + x + b x^2 + x^4}, \quad x = d^2/\lambda^2
\]

- **b = 1** (quartic baseline): f''(0) = 0, maximal flatness, natural fixed point
- **b ≈ 1.25** (optimized): β ≈ 1 at 1PN, GR-compatible weak-field
- **b is continuous**: β(b) crosses 1 — no fine-tuning required
- **Analogy**: b is TRM's Brans-Dicke ω; b=1 is the GR-identical limit

### Strong-Field Predictions (scalar approximation)

| Observable | GR | TRM (b=1.25) | Deviation |
|:---|:---|:---|:---|
| Horizon r_H | 2.00 GM | 2.275 GM | +13.8% |
| Photon sphere | 3.00 GM | 3.413 GM | +13.8% |
| Shadow radius | 5.20 GM | 5.91 GM | +13.8% |
| QNM ω·GM | 0.374 | 0.329 | −12.1% |

→ Currently within EHT observational uncertainty (M87* ~17%, Sgr A* ~12%). Falsifiable with next-generation instruments (ngEHT, LISA, 3G GW detectors).

### V4 Documentation

All V4 documentation lives in `docsV4/` — strictly separated from the frozen V3.4 core in `docs/`.

- `docsV4/README.md` — V4 workspace overview and directory structure
- `docsV4/theory/TRM_V4_Final_Status.md` — **Canonical V4 closure document**
- `docsV4/theory/TRM_V4_Interpretation_Core.md` — C5 energy density interpretation
- `docsV4/theory/TRM_V4_G4_OriginOfB.md` — Structural origin of kernel parameter b
- `docsV4/experiments/TRM_V4_G1T2d_FullTensor1PNClosure.md` — G1 1PN definitive closure
- `docsV4/experiments/TRM_V4_G1_KernelOptimization.md` — Kernel optimization → b≈1.25
- `docsV4/theory/TRM_V4_G2D_FullLorentzianClosure.md` — Quartic kernel as full Lorentzian solution
- `docsV4/theory/TRM_V4_G3_StrongFieldTestbed.md` — G3 strong-field framework
- `docsV4/TRM_V4_GR_Replacement_Roadmap.md` — G1–G6 gap analysis toward GR replacement
- `docsV4/review/TRM_V4_GravityTestAudit.md` — Full gravity test audit and classification

### V4 Cardinal Rule

> **"Does this modify the core?" → YES → forbidden. NO → allowed.**

```
/docs   → V3.4 FINAL (FROZEN) — do not modify
/docsV4 → V4 interpretation layer — active workspace
```

---

## ✅ Current Branch Baseline (V3.4 Core Theorie — frozen)

- **FP scaffold closed:** FP01–FP31 complete with zero pending-proof items. FP27 (positivity), FP28 (\(\varepsilon = 0 \iff m = 3\)), FP29 (\(q\text{CoreSupport}(q) = 1 - 3/q\)), FP31 (asymptotic limits) all proven.
- **Bridge band classified:** BD1–BD6 establish CLASS D — the bridge band is purely imposed, not dynamically emergent. R(φ) is flat (σ = 0.0000), single attractor at all φ, no stability peak at φ = 0.17.
- **E1 reinterpreted:** Rational ladder is induced classification (Ω* constant across N, m_eff = round(N·(Ω*−1))), not dynamical quantization.
- **γ/φ origin audited:** γ = 0.85 is the algebraic reciprocal of 20/17; φ = 0.17 is a representative mid-band value, not a physical constant.
- **I2 calibration audit:** Five external anchor routes (gravitational potential, galactic φ, MOND a₀, transport coupling, phenomenological) all blocked or circular.
- **Assumption closure:** Only I1 and I2 remain as irreducible inputs; D1 (shared normalization) is methodological discipline.
- **Scalar transport:** EL/Fermat executable bridge path tested and bounded.
- **Memory-channel path:** MC09–MC12 hardens \(\varphi^2|\dot{\mu}|\) derivation to effective-coupling level.
- **Theta observable path:** TO/TQK/LC/TOL guard blocks support \(\Theta \to O_5 \to \lambda_\Theta \to g_{\text{obs}}\) as tested-effective chain.
- **Vector sector path:** FD01–FD20 hardens weak-field frame-dragging candidate behavior and non-fitted effective \(k_T\) workflow.
- **Unified action path:** UF01–UF09 guards scalar/vector/theta limit recovery, bounded small cross-terms, and integration preservation.
- **External validation:** Predictions P1–P6 defined with explicit falsification conditions and dependency labels ([I1], [I2], [I1+I2]).


## 🧭 Reviewer quick start

> TRM/TQM V3.4 is a constrained effective theory with exactly 2 irreducible structural inputs. It is not a claim to replace General Relativity and does not claim theorem-level first-principles closure.

Key reproducibility commands:

`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category=CoreRegression"`

`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category!=LongRunning"`

## 📚 Paper set

**V3.0 Framework papers:**
- `docs/papers/Paper1_TRM_V3_Framework/TRM_V3_0_Framework_Review_Baseline.pdf`
- `docs/papers/Paper2_Memory_ModeLocking/TRM_V3_0_Memory_and_ModeLocking.pdf`
- `docs/papers/Paper3_Theta_Vector_UnifiedAction/TRM_V3_0_Theta_Vector_and_UnifiedAction.pdf`

**V3.4 Bridge-band finalization paper:**
- `docs/papers/V3_4/main.pdf` — Bridge-band dynamical origin and theory lock

**V3.1/V3.2 addenda:**
- `docs/papers/TRM_V3_1_V3_2_Addendum/TRM_V3_1_V3_2_Addendum_Memory_Action_Lattice.pdf`

## 🕰️ Version lineage

- `V5.1 (Ensemble Validation)`: **COMPLETE** — 10-seed ensemble replication campaign. Omega: HIGHLY STABLE, c_eff: MODERATELY STABLE, MeanDist/G_eff: STRUCTURALLY VARIABLE. 6 suites, 84 tests. Branch: `feature/v5.1-replication-expansion-and-ensemble-validation`. Tag: `v5.1-replication-ensemble-validation-complete`.
- `V5.0 (Independent Replication)`: **COMPLETE** — First independent replication campaign of the V4.5 prospective prediction pipeline. 6 suites, 78 tests. Protocol → Execute → Audit → Compare → Interpret → Synthesize. Independent seeds, hashes, UUIDs. No V4.5 mutation. Branch: `feature/v5.0-independent-replication-and-validation`. Tag: `v5.0-independent-replication-complete`.
- `V4.5 (Prospective Prediction)`: **COMPLETE** — First fully prospective prediction pipeline: Protocol → Generate → Freeze → Compute → Audit → Govern → Compare → Interpret → Synthesize. 9 suites, 126 tests. No anchor reselection, no parameter tuning, no post-hoc optimization. Branch: `feature/v4.5-prospective-anchor-prediction-branch`. Tag: `v4.5-prospective-anchor-prediction-complete`.
- `V4.4 (Length Anchor Validation)`: **COMPLETE** — MeanDist survives 6-axis stress testing, operational envelope mapped, HIGH-MARGIN safety classification. 5 suites, 70 tests. Tag: `v4.4-prospective-length-anchor-validation-complete`.
- `V4.3 (Geometric Scale Interpretation)`: **COMPLETE** — Scale candidates classified, hierarchy mapped, selection governed. 8 suites, 113 tests. Tag: `v4.3-geometric-scale-interpretation-complete`.
- `V4.2 (Physical Calibration)`: **COMPLETE** — SI-unit mapping via Kr-86 length path, CODATA comparison protocol, 10 calibration/prediction chains. 21 suites, 294 tests. Tag: `v4.2-physical-calibration-complete`.
- `V4.1 (Calibration Framework)`: **COMPLETE** — Internal causal geometry synthesis, convergence state, branch completion. 121 suites, 1340 tests. Tag: `v4.1-calibration-framework-complete`.
- `V4.0 (Interpretation Layer)`: **Active** — 1PN GR-compatible bilocal kernel, full Lorentzian tensor bridge, strong-field scalar ODE solver, 184 xUnit tests. Built on frozen V3.4 core. Branch: `feature/v4-interpretation-layer`.
- `V3.4`: **Core Theorie (frozen)** — bridge-band classified as imposed structural input, FP scaffold closed, E1 reinterpreted, canonical statement finalized. No further structural modifications.
- `V3.1–V3.3`: Intermediate research milestones (memory-channel action closure, minimal action from TQM lattice, m=3 closure scaffold).
- `V3.0`: Review baseline (multi-sector framework with explicit claim boundaries).
- `V2.2` and `V1`: Legacy historical baselines.

---

## 🚀 V4.5 — Prospective Prediction Branch (COMPLETE)

**Status:** COMPLETE  
**Branch:** `feature/v4.5-prospective-anchor-prediction-branch`  
**Tag:** `v4.5-prospective-anchor-prediction-complete`  
**Tests:** 126 V4.5 tests (1943 total project tests)

### Key Outcome

The first fully prospective prediction pipeline has been designed, executed, audited, compared, interpreted, and synthesized:

```
Protocol (PAPP) → Generation (PAPG) → Freeze (PAPF)
    → Computation (PAPC) → Audit (PAPA) → Governance (PACP)
    → Comparison (PAXC) → Interpretation (PAXI) → Branch Synthesis (PABS)
```

### Governance Guarantees

| Guarantee | Status |
|:---|:---|
| No anchor reselection | VERIFIED |
| No parameter tuning | VERIFIED |
| No post-hoc optimization | VERIFIED |
| No freeze reset | VERIFIED |
| Audit reproducibility (SHA-256) | VERIFIED |
| Comparison reproducibility | VERIFIED |
| Anti-feedback pathways locked | 14/14 |
| Claim discipline enforced | ALL PHASES |

### Current Completed Phases

| Version | Phase | Tests |
|:---|:---|:--:|
| V4.1 | Calibration Framework | 1340 |
| V4.2 | Physical Calibration | 294 |
| V4.3 | Geometric Scale Interpretation | 113 |
| V4.4 | Length Anchor Validation | 70 |
| V4.5 | Prospective Prediction | 126 |

### Next Phase

**V5.2 — Regime Sensitivity and Ensemble Expansion**

`feature/v5.2-regime-sensitivity-and-ensemble-expansion`

Objectives:
- xi / K0 regime sensitivity mapping
- Load sensitivity characterization
- Coupling-law ensemble robustness (Gaussian, power-law)
- N-scaling ensemble expansion (N>500, N>1000)
- Safe-boundary condition testing
- External reviewer reproduction protocol

---

## 🚀 V5.1 — Replication Expansion and Ensemble Validation (COMPLETE)

**Status:** COMPLETE
**Branch:** `feature/v5.1-replication-expansion-and-ensemble-validation`
**Tag:** `v5.1-replication-ensemble-validation-complete`
**Tests:** 84 V5.1 tests (2105 total project tests)

### Key Outcome

V5.1 expanded V5.0 from a single independent replication campaign to a 10-seed ensemble replication campaign:

```
Protocol (REP) → Execution (REE) → Audit (REA)
    → Comparison (REC) → Interpretation (REI) → Branch Synthesis (REBS)
```

### Ensemble Findings

| Metric | Stability | Driver |
|--------|:---------:|--------|
| Omega | HIGHLY STABLE | Simple frequency proxy |
| c_eff | MODERATELY STABLE | Omega-dominated |
| MeanDist | STRUCTURALLY VARIABLE | Topology-dependent |
| G_eff | STRUCTURALLY VARIABLE | MD³ amplified |

### Governance Guarantees

| Guarantee | Status |
|:---|:---|
| All 10 seeds retained | VERIFIED |
| No seed removal | VERIFIED |
| No outlier deletion | VERIFIED |
| No parameter tuning | VERIFIED |
| No anchor reselection | VERIFIED |
| 3-way hash reproducibility | VERIFIED |
| AUDIT-A classification | VERIFIED |

### Current Completed Phases

| Version | Phase | Tests |
|:---|:---|:--:|
| V4.1 | Calibration Framework | 1340 |
| V4.2 | Physical Calibration | 294 |
| V4.3 | Geometric Scale Interpretation | 113 |
| V4.4 | Length Anchor Validation | 70 |
| V4.5 | Prospective Prediction | 126 |
| V5.0 | Independent Replication | 78 |
| V5.1 | Ensemble Validation | 84 |

---

## 🚀 V5.0 — Independent Replication and Validation (COMPLETE)

**Status:** COMPLETE
**Branch:** `feature/v5.0-independent-replication-and-validation`
**Tag:** `v5.0-independent-replication-complete`
**Tests:** 78 V5.0 tests (2021 total project tests)

### Key Outcome

The first independent replication campaign of the V4.5 prospective prediction pipeline has been completed:

```
Protocol (IRP) → Execution (IRE) → Audit (IRA)
    → Comparison (IRC) → Interpretation (IRI) → Branch Synthesis (IRBS)
```

### Replication Guarantees

| Guarantee | Status |
|:---|:---|
| Independent seeds (50, 55, 60 ≠ V4.5 seed 45) | VERIFIED |
| Independent SHA-256 prediction hashes | VERIFIED |
| Independent replication UUIDs | VERIFIED |
| No V4.5 artifact mutation | VERIFIED |
| No parameter tuning (14/14 deep checks) | VERIFIED |
| No anchor reselection (11/11 checks) | VERIFIED |
| No external calibration | VERIFIED |
| 3-way hash reproducibility | VERIFIED |
| Audit classification | AUDIT-A — COMPLETE |
| Claim discipline preserved | ALL PHASES |

### Current Completed Phases

| Version | Phase | Tests |
|:---|:---|:--:|
| V4.1 | Calibration Framework | 1340 |
| V4.2 | Physical Calibration | 294 |
| V4.3 | Geometric Scale Interpretation | 113 |
| V4.4 | Length Anchor Validation | 70 |
| V4.5 | Prospective Prediction | 126 |
| V5.0 | Independent Replication | 78 |

## 📊 Implemented Domains

This repository includes computational models and analysis scripts for:

- **Galactic Rotation Curves (SPARC)**
  - Non-linear co-fit for the acceleration scale \( a_0 \)
  - Reproduction of flat rotation curves and BTFR

- **Galaxy Clusters (ACCEPT)**
  - Pressure-triggered regime transition
  - Bimodal classification (Newtonian vs TRM-supported)

- **Cosmic Microwave Background (Planck)**
  - k-space acoustic analysis
  - Temporal phase-coherence modeling

- **Cosmological Expansion (Pantheon+)**
  - Luminosity-distance fitting
  - Temporal drift coefficient \( \beta_{\mathcal{T}} \)

---

## 📂 Solution Structure

- `TRM.Core`  
  The theoretical engine. Contains shared analysis models, the RK4 acoustic solver, and grid-sweep optimization algorithms.
- `TRM.FormalProofs`  
  Formal proof infrastructure — m=3 Lean proofs, rational arithmetic.
- `TRM.Simulations`  
  Simulation pipeline — Planck scanning, uncertainty experiments, wave optics.
- `TRM.CMD`  
  Console entry point to execute selected high-performance analyses and parameter sweeps via an interactive menu.
- `TRM.Tests`  
  xUnit test suite acting as the scientific safeguard. Includes sector hardening blocks (CML01–CML23, RBF01–RBF79, TO01–TO28, FD01–FD20, EL01–EL17, BD1–BD6, E1 rational ladder), plus domain validations and regression gates.
- `TRM.Python`  
  Python plotting pipeline for visualizing output data (CSV) into publication-ready graphs.

---

## ⚙️ Getting Started

### Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/)
- Visual Studio 2026 (or `dotnet` CLI)
- Python 3.11+ (for `TRM.Python` visualizations)

Install required Python packages for plotting:
`pip install pandas matplotlib seaborn numpy`

### Build the Project
From the solution root directory:
`dotnet build TRM_Cosmology.slnx`

### Run the Analysis (Console App)
`dotnet run --project TRM.CMD/TRM.CMD.csproj`
*Note: The console app reads input catalogs from the `Data` directory. Ensure required files (e.g., SPARC/ACCEPT catalogs and `Pantheon+SH0ES.dat`) are present in your local execution folder.*

### Run the Scientific Validations (Tests)
Execute the rigorous xUnit test suite to verify the exact cosmological constants (a_0, eta_rec, beta_T) against the latest observational bounds:
`dotnet test TRM.Tests/TRM.Tests.csproj`
*Tests use `ITestOutputHelper` for detailed log outputs, visible directly in your Test Explorer or CLI.*

Run only the fast hard regression gate:
`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category=CoreRegression"`

Run the V4 interpretation layer tests (184 tests, B1–G4):
`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category=V4"`

Run only G1–G4 (1PN closure through origin of b):
`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category=G1|Category=G2|Category=G3|Category=G4"`

Run the default suite without slow sweeps:
`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category!=LongRunning"`

Run long-running sweeps manually/nightly:
`dotnet test TRM.Tests/TRM.Tests.csproj --filter "Category=LongRunning"`

---

## 📄 Key V3.4 Final Documents (Core Theorie)

- `docs/Final/V3_4/TRM_Canonical_Statement.md` — **Canonical theory statement** (supersedes all prior formulations)
- `docs/Final/V3_4/TRM_Final_Formulation.md` — Formal I1/I2/D1 definitions and dependency structure
- `docs/Final/V3_4/TRM_Status_Audit.md` — Complete audit of validated results, FP status, open questions
- `docs/Final/V3_4/AssumptionClosureAnalysis.md` — Derivation and classification of all assumptions
- `docs/Final/V3_4/BridgeBand_Dynamical_Origin.md` — BD1–BD6 results proving bridge band is imposed (CLASS D)
- `docs/Final/V3_4/E1_Interpretation_Update.md` — E1 rational ladder reinterpretation (induced, not dynamic)
- `docs/Final/V3_4/Gamma_Origin_Analysis.md` — γ = 0.85 origin analysis (algebraic, not physical)
- `docs/Final/V3_4/I2_Calibration_or_Axiom.md` — External anchor audit (all 5 routes blocked or circular)
- `docs/Final/V3_4/TRM_External_Validation.md` — Predictions, experiments, falsification conditions
- `docs/Final/V3_4/TRM_Paper_Claims.md` — Publication-ready claim statements with strength and boundaries
- `docs/Final/V3_4/RealWorldRationalTest.md` — EEG/laser rational ladder detection analysis
- `docs/Final/V3_4/TRM_Repository_Patch_Audit.md` — Repository wording consistency audit

## 📄 Key Review/Theory Documents

- `docs/review/TRM_Cover_Letter_And_Abstract.md`
- `docs/review/REVIEW_PACKAGE.md` *(recommended reviewer start point)*
- `docs/review/TRM_Peer_Review_Request.md`
- `docs/review/TRM_Current_Status_For_PeerReview.md`
- `docs/review/TRM_V3_4_Interim_Status.md`
- `docs/review/TRM_V3_4_Interim_Summary_OnePage.md`
- `docs/review/TRM_Service_Test_Consolidation.md`
- `docs/review/TRM_TestSuite_Classification.md`
- `docs/review/TRM_Real_Physics_Test_Coverage.md`
- `docs/review/TRM_Code_To_Theory_Audit.md`
- `docs/Theory/TRM_Field_Sector_Map.md`
- `docs/Theory/TRM_First_Principles_Gap_List.md`
- `docs/Theory/TRM_Unified_Field_Action_Roadmap.md`
- `docs/Theory/TRM_Geodesic_Derivation.md`
- `docs/Theory/TRM_Collective_Mode_Locking_BridgeScale.md`
- `docs/Theory/TRM_LPC_Final_Rebase.md`
- `docs/Theory/TRM_M3_Formal_Proof_Obligations.md`

---

## 📌 Notes & Scientific Contribution
- **Data-Driven:** All analyses depend on external, peer-reviewed observational datasets (SPARC, ACCEPT, Planck, Pantheon+). 
- **Troubleshooting:** If `FileNotFound` errors occur during runtime, verify your working directory and ensure the data catalogs are correctly placed in the `Data` folder and set to "Copy if newer".
- **Contributing:** This is an open-science initiative. Feel free to open issues or submit Pull Requests if you want to optimize the integrators or test the TRM framework against new astrophysical databases.
- **Theory freeze:** The core theory (I1, I2, D1, FP scaffold, claim boundaries) is frozen as of V3.4. No further structural modifications without opening a new major version (V4.0+).