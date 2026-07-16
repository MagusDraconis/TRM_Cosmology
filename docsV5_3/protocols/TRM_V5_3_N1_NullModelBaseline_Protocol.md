# TRM V5.3 — N1 Null Model Baseline Protocol

**Suite:** V5_3_NullModelBaseline_Tests.cs
**Tag:** NMB
**Date:** 2026-07-16
**Status:** PROTOCOL DEFINED — READY FOR EXECUTION
**Phase:** V5.3 — Stability Mechanism and Control Parameters
**Branch:** `feature/v5.3-stability-mechanism-and-control-parameters`

---

## A. Purpose

Determine whether the observed Omega/MeanDist stability split (V5.2 central finding) requires an attractor at all. The null model removes ALL synchronization dynamics — no coupling, no phase evolution, no attractor — and tests whether the qualitative stability pattern persists.

If the null model reproduces the pattern, then:
- H9, H10, H11, H12 are jointly weakened.
- V5.3 must pivot toward identifying quantitative attractor-specific residuals.
- The stability pattern is a generic property, not evidence for attractor decomposition.

If the null model fails:
- The failed signature identifies genuinely attractor-specific behavior.
- Proceed to M1–M4 minimal falsification experiments.
- This is the first positive evidence that the TRM attractor produces distinctive behavior.

---

## B. Null Model Specification

### B.1 What is removed

| TRM Component | Null Model Replacement |
|:--------------|:-----------------------|
| Coupling kernel K_ij = K₀ · exp(-d_ij/ξ) | Removed — no coupling matrix constructed |
| Phase evolution dθ_i/dt | Removed — no dynamics |
| Phase correlation matrix RP(h) | Removed — no simulation history h |
| Emergent distance DL(Nm(RP(h))) | Replaced by fixed graph shortest-path distances |
| Attractor fixed-point recovery (RecoverFP) | Removed — no epochs, no fixed-point search |
| Synchronized cluster identification | Replaced by frequency-threshold selection rule |

### B.2 What is preserved

| Component | Preservation |
|:----------|:------------|
| Graph generation | KS(N, seed) — identical Erdős–Rényi + connectivity enforcement |
| Graph topology | Same adjacency structure per seed |
| Natural frequency distribution | ω_i = 1.0 + s · (U(0,1) − 0.5) · 2.0 — identical sampling |
| Metric definitions | Ω = mean(frequencies of selected nodes); MD = mean(pairwise distances of selected pairs) |
| Seed values | Fresh seed range (400–419) — independent of V5.0/V5.1/V5.2 |

### B.3 Selection Rule

```
include node i if |ω_i − 1.0| < δ(ξ)
  where δ(ξ) = s · (ξ / ξ_primary)
        s = 0.08 (natural frequency spread)
        ξ_primary = 1.80 (V5.2 primary regime)
```

This is the minimal model of frequency entrainment without dynamics:
- Larger ξ → larger δ → more nodes selected (mimics wider coupling range)
- Selected nodes have frequencies near the population mean (mimics frequency entrainment)
- Selection is purely frequency-based — no spatial structure

---

## C. Test Structure (10 tests)

| Test ID | Name | Purpose |
|:--------|:-----|:--------|
| NMB-01 | NullModelIsCouplingFree | Assert null model has no coupling, no dynamics, no attractor |
| NMB-02 | NullModelPreservesGraphStructure | Assert identical graph generation to TRM |
| NMB-03 | NullModelPreservesFrequencySampling | Assert identical frequency sampling to TRM |
| NMB-04 | SelectionRuleIsXiDependent | Assert δ(ξ) is monotonically increasing with ξ |
| NMB-05 | OmegaCvSeedStable | Compute Ω_null seed-CV at primary ξ |
| NMB-06 | MeanDistCvSeedVariable | Compute MD_null seed-CV at primary ξ |
| NMB-07 | OmegaXiSensitivity | Compute Ω_null across ξ sweep {1.50, 1.65, 1.80, 1.95, 2.10} |
| NMB-08 | MeanDistXiRobustness | Compute MD_null across ξ sweep |
| NMB-09 | PatternReproductionSummary | Aggregate: do all 4 qualitative signatures match? |
| NMB-10 | ClaimDisciplineAudit | Self-audit: verify no prohibited claims made |

---

## D. Frozen Parameters

| Parameter | Value | Justification |
|:----------|:------|:--------------|
| N | 100 | Matches V5.2 primary regime |
| s | 0.08 | Matches V5.2 natural frequency spread |
| ξ_primary | 1.80 | Matches V5.2 primary ξ |
| ξ sweep | {1.50, 1.65, 1.80, 1.95, 2.10} | Matches V5.2 Phase 1 for direct comparability |
| Seeds for CV | 20 (indices 400–419) | Up from V5.2's 3/point for reliable CV estimation |
| Seeds for ξ sweep | 5 per point (indices 400–404) | Matches V5.2 design |
| δ(ξ) form | δ = s · (ξ / ξ_primary) | Simplest monotonic choice |

---

## E. Pre-Registered Decision Thresholds

| Threshold | Value | Used In | Rationale |
|:----------|:------|:--------|:----------|
| Omega seed-stable | CV < 0.05 | NMB-05 | Errs on side of calling Ω unstable (conservative) |
| MeanDist seed-variable | CV > 0.15 | NMB-06 | Errs on side of calling MD stable (conservative) |
| Omega ξ-sensitive | ΔΩ/Ω > 0.02 | NMB-07 | Small but detectable shift across 5 ξ points |
| MeanDist ξ-robust | ΔMD/MD < 0.05 | NMB-08 | Errs on side of calling MD sensitive (conservative) |

**Design note:** All thresholds are conservative — they make it HARDER for the null model to match the TRM pattern. If the null model passes despite conservative thresholds, the case against H9–H12 is strengthened.

---

## F. Decision Gates

### Gate 1: Pattern Reproduction (NMB-09)

```
If 4/4 signatures reproduced:
  → H9, H10, H11 WEAKENED
  → H12 STRONGLY WEAKENED
  → PIVOT: V5.3 goal shifts from "characterize parameter classes"
    to "identify quantitative attractor-specific residuals"
  → Next: N2 (membership-matched Omega comparison)

If 3/4 signatures reproduced:
  → Partial null-model success
  → Failed signature is candidate for attractor-specific behavior
  → Next: Focus M1-M4 experiments on the failed signature

If ≤2/4 signatures reproduced:
  → Null model FAILS
  → This is the first positive evidence for attractor-specific behavior
  → Next: M1-M4 minimal falsification program
  → Do NOT claim attractor decomposition — only that null model fails
```

### Gate 2: Recommendation After Pattern Reproduction

```
If null model reproduces pattern:
  RECOMMENDATION: Do NOT execute the 210-run sensitivity matrix.
  The matrix assumes H9-H12 are viable working hypotheses.
  If H9-H12 are weakened by N1, the matrix design is invalid.
  Instead: design experiments to find TRM-specific quantitative
  residuals (N2: membership-matched Omega comparison).
```

---

## G. Forbidden Actions

1. Do NOT change δ(ξ) functional form after seeing results.
2. Do NOT adjust thresholds after seeing results.
3. Do NOT reselect seeds.
4. Do NOT change the graph generation algorithm.
5. Do NOT change the frequency sampling distribution.
6. Do NOT add coupling or dynamics to the null model after seeing results.
7. Do NOT reinterpret a null-model pattern match as "the null model proves TRM wrong."
8. Do NOT reinterpret a null-model failure as "H9-H12 are confirmed."
9. Do NOT compare null-model CVs quantitatively to TRM CVs — this is a QUALITATIVE pattern match test.
10. Do NOT claim the null model is physically correct.

---

## H. Expected Interpretation of Each Possible Outcome

### Outcome A: Full Pattern Match (4/4)

```
The null model reproduces the ENTIRE qualitative stability pattern
without coupling, dynamics, or attractor.

This means:
  - Omega seed stability is predicted by elementary statistics
    (SE of mean of many samples).
  - Omega xi-sensitivity is predicted by cluster-membership
    sampling (different delta → different subset → different mean).
  - MeanDist seed variability is predicted by random spatial
    selection (random subsets have variable mean pairwise distance).
  - MeanDist xi-robustness is predicted by graph-distance fixity
    (d_ij don't depend on xi) plus large-subset near-constancy.

The burden of proof shifts to TRM: demonstrate that the
QUANTITATIVE pattern (magnitudes, not just directions) differs
from the null model in a way that requires synchronization dynamics.

H9-H12 are not falsified, but they are severely weakened as
explanations for the qualitative pattern. The pattern itself
is no longer evidence for attractor decomposition.
```

### Outcome B: Partial Match (3/4)

```
One signature fails. The identity of the failed signature
determines the next step:

If Omega seed-stable FAILS:
  → Omega in the null model is MORE variable than TRM.
  → TRM synchronization genuinely stabilizes Omega.
  → This is evidence FOR H9 (sync-control).

If Omega xi-sensitive FAILS:
  → Null-model Omega doesn't shift with xi.
  → The TRM Omega-xi relationship is not a sampling effect.
  → Requires investigation: why does cluster membership change
    Omega in TRM but not in the null model?

If MeanDist seed-variable FAILS:
  → Null-model MD is MORE stable than TRM MD.
  → TRM synchronization genuinely adds variability to MD.
  → This is evidence FOR geometry-sensitivity from dynamics.

If MeanDist xi-robust FAILS:
  → Null-model MD shifts with xi.
  → TRM MD is MORE robust than null expectation.
  → This is evidence FOR H10 (geometry-control).
```

### Outcome C: Null Model Fails (≤2/4)

```
The null model CANNOT reproduce the majority of the pattern.
This is the first positive evidence that the TRM attractor
produces distinctive behavior.

Proceed to M1-M4 minimal falsification.
Do NOT claim H9-H12 are confirmed — only that the simplest
null model has been rejected.
```

---

## I. Relationship to Other V5.3 Experiments

```
N1 (this suite)
  │
  ├── NULL MODEL REPRODUCES PATTERN (4/4)
  │     │
  │     └── N2: Membership-matched Omega comparison
  │           Tests whether TRM Omega differs from null Omega
  │           when cluster sizes are matched.
  │
  └── NULL MODEL FAILS (≤3/4)
        │
        └── M1–M4: Minimal falsification program
              M1: Fixed-cluster Omega (tests H9)
              M2: TRM vs null model quantitative (tests H12)
              M3: Saturation boundary (tests H10, H11)
              M4: Latent variable regression (tests H9-H11 jointly)
```

---

## J. How to Run

```powershell
# Run the N1 suite only
dotnet test --filter "FullyQualifiedName~V5_3_NMB" -v normal

# Run with detailed output
dotnet test --filter "FullyQualifiedName~V5_3_NMB" -v detailed

# Run a specific test
dotnet test --filter "FullyQualifiedName~V5_3_NMB_09_PatternReproductionSummary" -v normal
```

**Expected runtime:** < 5 seconds (pure computation, no simulation dynamics).

---

## K. Claim Discipline

| Statement | Classification |
|:----------|:--------------|
| The null model is structurally defined | SUPPORTED |
| The null model contains no coupling, dynamics, or attractor | SUPPORTED |
| The null model preserves graph structure and frequency sampling | SUPPORTED |
| The selection rule is xi-dependent | SUPPORTED |
| The computed CVs and sensitivities are as reported | SUPPORTED |
| The null model may reproduce the V5.2 stability pattern | HYPOTHESIS (tested by this suite) |
| Results depend on KS graph generation | CONDITIONAL |
| Results depend on delta(xi) functional form | CONDITIONAL |
| Graph distances may differ from emergent distances | CONDITIONAL |
| TRM attractor decomposition is confirmed | NOT CLAIMED |
| H9-H12 are falsified | NOT CLAIMED |
| Physical constants are derived | NOT CLAIMED |
| Quantum mechanics is involved | NOT CLAIMED |
| Spacetime emerges from TRM | NOT CLAIMED |

---

*Protocol frozen 2026-07-16. Thresholds pre-registered. No post-hoc adjustment permitted. This suite is the first executable V5.3 test — it must pass or fail on its own merits before any larger campaign is attempted.*
