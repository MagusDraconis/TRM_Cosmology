# TRM/TQM First-Principles Roadmap

## Purpose

This roadmap consolidates the three core derivation paths needed to move from a tested effective framework toward a first-principles theory:

1. \( \mathrm{TQM} \rightarrow \phi^2\lvert d\mu/dt\rvert \)  
2. \( \mathrm{TQM} \rightarrow m=3 \) closure  
3. \( \Theta(r) \rightarrow g_{\mathrm{obs}}(r) \)

An additional extension track is now active:

4. scalar \(T\) + vector rotational sector \(\vec A_T\) (frame-dragging candidate path)

Traffic-light meanings:

- **green** = tested effective behavior  
- **yellow** = candidate derivation path  
- **red** = still open / not derived

---

## 1. TQM \(\rightarrow \phi^2\lvert d\mu/dt\rvert\)

| Light | Status |
|---|---|
| green | **tested effective behavior:** MEM01–MEM02, TRM84–TRM87, MC01–MC12, HOA01 support current channel role and constraints, including derived-invariant bridge substitution in MC12. |
| yellow | **strongly supported derivation chain:** MC09–MC12 support \(A_{\mathrm{dyn}}\propto\phi \rightarrow A_{\mathrm{dyn}}^2|\dot{\mu}| \rightarrow \phi^2|\dot{\mu}|\) with high-form match and bridge retention up to an effective coupling scale (\(R^2_{\mathrm{MC11}}=0.999799\), \(\text{bridgeRetention}_{\mathrm{MC12}}=0.957676\), \(\text{meanGap}_{\mathrm{MC12}}=1.64\times 10^{-4}\)). |
| red | **still open / not derived:** microscopic uniqueness proof for exponent pair \((a,b)=(2,1)\) and absolute-rate form \(\lvert d\mu/dt\rvert\). |

Current classification:
- tested: yes
- calibrated: yes
- hypothesis-supported: yes
- not derived yet: yes
- limitation: no microscopic theorem yet

---

## 2. TQM \(\rightarrow m=3\) Closure

| Light | Status |
|---|---|
| green | **tested effective behavior:** RBF01–RBF20 support robust \(m=3\) selection, connected threshold-region stability, derived action/tick discriminator behavior, solver-family robustness, and artifact-audit hardening. |
| yellow | **strongly constrained theorem path:** closure family \(q\Omega-p=0,\; p=q+m\) plus RBF16–RBF20 now provide a strongly constrained derivation path with explicit failure-by-family exclusion logic. |
| red | **still open / not derived:** theorem-level microscopic uniqueness of \(m=3\) independent of operational threshold families. |

Current classification:
- tested: yes
- derived: strongly constrained operational theorem path
- hypothesis-supported: yes
- not derived yet: theorem-level closure origin
- limitation: no microscopic theorem proving full solver-family independence

---

## 3. \(\Theta(r)\rightarrow g_{\mathrm{obs}}(r)\)

| Light | Status |
|---|---|
| green | **tested effective behavior:** SPARC/RAR + orbit/full-model suites, plus TO01–TO28, TQK01–TQK04, LC01–LC08, and TOL01–TOL04, support a stable theta-observable path with holdout, leakage, solver-ablation, operator-structure, energy-gradient, lattice-consistency, lambda-discipline, and chain-level response checks. |
| yellow | **strongly supported derivation chain:** TOL01–TOL04 support a coherent lattice-energy-backed path \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\), while microscopic closure is still open. |
| red | **still open / not derived:** unique physically grounded observable map from \(\Theta\) to \(g_{\mathrm{obs}}\), with identifiability against local reparameterizations. |

Current classification:
- tested: yes (effective derivation-gate level, including TO01–TO28, TQK01–TQK04, LC01–LC08, and TOL01–TOL04)
- calibrated: yes (solver/couplings)
- hypothesis-supported: yes
- not derived yet: yes
- limitation: not theorem-level microscopic closure

Current synthesis:
- TO01–TO06: bounded + structurally interesting theta-observable behavior.
- TO07–TO08: current O4/theta does not beat local reparameterization across tested regimes.
- TO09–TO14: independence/residual-space, non-local O5 gating, class stability, ablation robustness, and plausibility-aware kernel selection are active.
- TO15–TO18: galaxy holdout and stratified holdout show stable positive O5-W6-InvDistance improvement.
- TO19–TO20: observed-velocity leakage and solver-parameter ablation guards pass.
- TO21: O5-W6 shows finite-coherence synchronization-tension operator behavior on synthetic Theta profiles.
- TO22–TO24: W-window derivation guards indicate W6 as the smallest stable medium-coherence balance window, with nearby windows still competitive.
- TO25–TO28: O5 aligns with the negative gradient of a discrete finite-coherence energy and shows energy-descent, zero-mode, and bounded smooth-profile behavior.
- TQK01–TQK04: phase-lattice small-angle reduction and gradient consistency checks are positive; inverse-distance kernel remains competitive; W6 stays in a plausible finite-coherence correlation-length band.
- LC01–LC08: lambda-response discipline block is complete. \(\lambda_\Theta\) is constrained by dimensional/holdout/ablation/regularization guards and explicit anti-proxy tests, supporting a regularized regime-conditioned effective coefficient (not global-only and not per-galaxy identity fitting).
- TOL01–TOL04: support chain coherence from phase-lattice energy reduction to O5 energy-gradient behavior, response-scale mapping for \(\lambda_\Theta\), and holdout-stable chain response.
- Claim boundary: the \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\) chain is now strongly supported as a lattice-energy-backed derivation path, but it remains not theorem-level first-principles closure.

---

## 4. Scalar \(T\) + Vector \(\vec A_T\) Frame-Dragging Candidate Sector

| Light | Status |
|---|---|
| green | **tested effective behavior:** FD01–FD15 support a structurally robust weak-field frame-dragging candidate sector (rotation-generated \(A_T/B_T\), spin-sign reversal, \(r^{-3}\) shape, effective \(k_T\) holdout/discretization stability, spin-zero scalar-limit preservation, prograde/retrograde light-path asymmetry). |
| yellow | **candidate derivation:** vector/tensor extension path \(T\rightarrow(T,\vec A_T,Q_{ij})\) is formulated as a test-gated weak-field candidate sector. |
| red | **still open / not derived:** first-principles derivation of \(k_T\), quantitative GR-amplitude equivalence windows, real LT benchmark windows, and strong-field rotating-source regime closure. |

Current classification:
- tested: yes (effective weak-field candidate level, FD01–FD15)
- calibrated: yes (effective \(k_T\) normalization)
- hypothesis-supported: yes
- not derived yet: yes
- limitation: not yet GR-equivalent and not first-principles-derived

---

## Priority order and immediate gate

1. **Memory-term derivation gate:** move from candidate selection to microscopic admissibility proof tests.  
2. **m=3 closure gate:** move from operational minimality to microscopic uniqueness/non-uniqueness theorem statement.  
3. **Theta-observable gate:** establish falsifiable observable selection with non-local identifiability.  
4. **Vector frame-dragging gate:** move from weak-field structural/normalization compatibility to first-principles \(k_T\) derivation and quantitative GR-window benchmarks.

No claim should be upgraded to "derived theorem" before its red items are explicitly closed.
