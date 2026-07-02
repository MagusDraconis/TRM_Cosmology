# TRM Theta to Observable Derivation Plan

## Scope

Goal: structure a theory and test path for
\[
\Theta(r)\rightarrow g_{\mathrm{obs}}(r)
\]
without introducing new fit-driven claims.

---

## 1. Current role of \(\Theta(r)\)

From `TrmFieldSolver.cs`, \(\Theta(r)\) is currently a relaxed radial field driven by:

1. baryonic source proxy (`ComputeLocalSource`),  
2. damping term,  
3. synchronization proxy (`ComputeSyncTerm`),  
4. radial regularization (Laplacian-like + radial-gradient terms).

Current classification:

- **tested:** indirectly via SPARC/RAR core suites  
- **calibrated:** solver strengths/relaxation parameters  
- **hypothesis-supported:** synchronization-field interpretation  
- **not derived yet:** microscopic observable map  
- **limitation:** current observable extraction is still a proxy layer

---

## 2. Existing observable candidates

### 2.1 Gradient candidate
\[
\mathcal O_1(r)\sim|\partial_r\Theta|
\]
**Status:** hypothesis-supported (documented candidate class).

### 2.2 Gradient + level term
\[
\mathcal O_2(r)\sim
\alpha|\partial_r\Theta|+\beta\frac{\max(\Theta,0)}{r}
\]
This is closest to current `ComputeEffectiveAcceleration`.
**Status:** calibrated + diagnostic; partially tested indirectly.

### 2.3 Curvature candidate
\[
\mathcal O_3(r)\sim|\partial_r^2\Theta+\frac{1}{r}\partial_r\Theta|
\]
Motivated by solver radial operators.
**Status:** hypothesis-supported; not derived yet as observable.

### 2.4 Orbit-integrated observable
\[
\mathcal O_4(r)\sim \operatorname{OrbitKernel}\!\left(\Theta, g_{\mathrm{bar}}, r\right)
\]
Consistent with non-local orbit-integration logic in `OrbitalIntegrationService`.
**Status:** hypothesis-supported; not yet explicit as a standalone theta-observable formula.

---

## 3. Why local RAR is likely insufficient

Current evidence chain:

1. `OrbitalIntegratedTests` compares local vs orbit-only vs full model and reports improved behavior for non-local/full structures over purely local baseline.  
2. `TRM_TQM_Theorie_Statement.md` explicitly treats local-only \(g_{\mathrm{obs}}=f(g_{\mathrm{bar}})\) as likely incomplete.

Classification:

- **tested:** comparative non-local/full improvements are present in current test suite  
- **hypothesis-supported:** physical interpretation as orbit-integrated synchronization dynamics  
- **limitation:** still model-dependent and not a unique first-principles observable proof

---

## 4. Candidate mapping from \(\Theta\) to \(g_{\mathrm{obs}}\)

Working derivation ansatz:
\[
g_{\mathrm{pred}}(r)=g_{\mathrm{base}}(r)+\lambda_{\Theta}\,\mathcal O_{\Theta}(r),
\]
with
\[
\mathcal O_{\Theta}\in\{\mathcal O_1,\mathcal O_2,\mathcal O_3,\mathcal O_4\}.
\]

Where:

1. \(g_{\mathrm{base}}\): existing baryonic + orbit-integrated baseline,  
2. \(\lambda_\Theta\): effective coupling (currently calibrated),  
3. \(\mathcal O_\Theta\): candidate observable family to be selected by falsification tests.

Classification:

- **derived:** effective mapping template only  
- **calibrated:** coupling choices remain calibrated  
- **not derived yet:** microscopic uniqueness and identifiability

---

## 5. Gap-3 synthesis status

### Completed gates

- **implemented and passing:** TO01–TO28, TQK01–TQK04, LC01–LC08, and TOL01–TOL04 in `TRM.Tests/CoreTests/ThetaObservableDerivationTests.cs`.
- **status:** strongly supported derivation chain / not theorem-level.

### Current synthesis

- **Theta reduction:** TQK01 and TOL01 support small-phase lattice-energy reduction to a quadratic \(\Theta\)-coherence form.
- **O5 derivation path:** TO25–TO28, TQK02, and TOL02 support \(O_5\) as an energy-gradient finite-coherence operator under bounded-response and relaxation checks.
- **\(\lambda_\Theta\) role:** LC01–LC08 and TOL03 support \(\lambda_\Theta\) as a regularized regime-conditioned effective response scale, not a per-galaxy free proxy.
- **observable link:** TOL04 supports holdout-stable positive-share behavior for the chain \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\).
- **claim boundary:** the chain is lattice-energy-supported and review-safe as strongly supported, but it is still below theorem-level microscopic closure.

### Remaining theorem gap

- Derive \(\lambda_\Theta\) from a microscopic coarse-grained action with no calibration degree of freedom.
- Prove identifiability and uniqueness of the \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta\) map under admissible kernel/solver families.
- Show stability and closure in analytic form beyond finite tested classes.

The detailed gate inventory remains in the test source and related theorem-path documents; no free-fit expansion is justified without a new first-principles constraint.

---

## 6. Minimum criteria for claiming a physically meaningful observable

Claim upgrade from "candidate observable" to "physically meaningful effective observable" only if all hold:

1. **Cross-regime stability:** inner, mid, and outer bins remain bounded and interpretable.  
2. **Non-local necessity:** outperforms local-only formulations without hidden equivalent reparameterization.  
3. **Cross-galaxy robustness:** improvement is not dominated by a small subset.  
4. **Parameter discipline:** no uncontrolled parameter inflation.  
5. **Theory compatibility:** candidate remains consistent with TRM/TQM synchronization interpretation.

Even if passed, this still means:

- **tested effective observable:** yes  
- **first-principles derived observable:** not yet, unless microscopic derivation is completed.
