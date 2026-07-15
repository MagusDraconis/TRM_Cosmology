# TRM V5.0 — Independent Replication Roadmap

**Status:** EXPLORATORY  
**Date:** 2026-07-15

---

## A. Motivation

V4.5 established a fully prospective prediction pipeline with 9 suites and 126 tests. The pipeline demonstrated that prospective anchor predictions can be generated, frozen, audited, compared, and interpreted under strict governance. However, the V4.5 pipeline was executed by a single team in a single regime. Independent replication is required to:

1. Distinguish structural robustness from implementation-specific outcomes
2. Verify that anti-feedback governance holds under independent execution
3. Characterize the sensitivity of results to regime parameters
4. Execute SI-unit mapping to enable physical comparison

## B. Reproducibility Goals

| Goal | Criterion |
|------|-----------|
| SHA-256 audit hashes | Reproducible across independent runs |
| Anti-feedback gates | All 14 pathways remain locked |
| Prediction values | Identical within machine epsilon |
| Classification stability | Same A/B/C/D/REJECT across independent runs |
| Pipeline order | Freeze → Audit → Govern → Compare → Interpret |

## C. Independent Replication Protocol

Define governance for independent replication:
- Allowed and forbidden actions during replication
- Anti-feedback gates for replication phase
- Verification criteria for independent execution
- Documentation requirements

## D. Regime Sensitivity

Characterize sensitivity to:

| Parameter | V4.5 Value | Test Range |
|-----------|-----------|------------|
| xi | 1.80 | 1.50, 1.65, 1.80, 1.95, 2.10 |
| K0 | 1.15 | 0.90, 1.05, 1.15, 1.25, 1.40 |
| N | 100 | 80, 100, 200, 500, 800, 1000 |
| Coupling law | Exponential | Exp, Gaussian, Power-law |
| Seed | 45 | Independent random seeds |

## E. Independent Prediction Runs

Generate predictions under independent conditions:
- Different random seeds
- Potentially different initial graph realizations
- Same frozen regime (xi=1.80, K0=1.15, N=100, exponential)
- Compare with V4.5 frozen predictions

## F. Prospective SI Mapping

Execute SI-unit mapping under prospective protocol:
- **Time:** Cs-133 hyperfine transition (9,192,631,770 Hz)
- **Length:** Kr-86 wavelength (pre-1983, independent of c)
- **Source:** SI kilogram (2019, h-based, independent of G)

Verify anti-circularity:
- SI meter depends on c → circular for c_eff comparison
- Kr-86 avoids this circularity

## G. Governed Physical Comparison

Compare SI-mapped predictions to CODATA 2018 values:
- c = 299,792,458 m/s (exact)
- G = 6.67430 × 10⁻¹¹ m³/(kg·s²)

Apply PACP governance:
- A/B/C/D/REJECT classifications
- SUPPORTED/CONDITIONAL/HYPOTHESIS/NOT_CLAIMED interpretation
- Tamper-evident comparison hashes

## H. Continuum Extension

Extend continuum limit characterization:
- N = 500, 800, 1000
- Determine if classification is N-stable
- Identify finite-N bias magnitude

## I. Error Budget Validation

Full uncertainty decomposition:
- Seed variance (CV_seed)
- N-scaling systematic (CV_N)
- Load sensitivity
- Law sensitivity
- Proxy sensitivity

## J. Claim Discipline

Same as V4.5:
- SUPPORTED: Pipeline integrity, audit, comparison execution
- CONDITIONAL: Regime-specific, finite-N, dimensionless
- HYPOTHESIS: Structural analogy, stability, generality
- NOT CLAIMED: Physical c/G/gravity/GR/spacetime/SI units derived

**Recommended first suite:** `V5_0_IndependentReplicationProtocol_Tests.cs`
