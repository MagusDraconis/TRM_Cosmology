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
| green | **tested effective behavior:** MEM01–MEM02, TRM84–TRM87, MC01–MC08, HOA01 support current channel role and constraints. |
| yellow | **candidate derivation:** coarse-graining path via coherence amplitude and transport rotation is formalized in theory docs, but still hypothesis-supported. |
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
| green | **tested effective behavior:** RBF01–RBF15 show robust \(m=3\) selection under full constraints and collapse/non-uniqueness when action/tick is removed. |
| yellow | **candidate derivation:** closure family \(q\Omega-p=0,\; p=q+m\) and three-constraint minimality logic are formalized as derive-or-falsify framework. |
| red | **still open / not derived:** theorem-level microscopic uniqueness of \(m=3\) independent of operational threshold families. |

Current classification:
- tested: yes
- derived: operational formalization only
- hypothesis-supported: yes
- not derived yet: theorem-level closure origin
- limitation: threshold dependence remains explicit

---

## 3. \(\Theta(r)\rightarrow g_{\mathrm{obs}}(r)\)

| Light | Status |
|---|---|
| green | **tested effective behavior:** SPARC/RAR + orbit/full-model suites, plus TO01–TO28, TQK01–TQK04, and LC01–LC08, support a stable effective theta-observable selection workflow with holdout, leakage, solver-ablation, operator-structure, energy-gradient, lattice-consistency, and lambda-discipline guards. |
| yellow | **candidate derivation:** observable-family plan (\(\lvert\partial_r\Theta\rvert\), gradient+level, curvature, orbit-integrated observable) is structured and now test-gated. |
| red | **still open / not derived:** unique physically grounded observable map from \(\Theta\) to \(g_{\mathrm{obs}}\), with identifiability against local reparameterizations. |

Current classification:
- tested: yes (effective derivation-gate level, including TO01–TO28, TQK01–TQK04, and LC01–LC08)
- calibrated: yes (solver/couplings)
- hypothesis-supported: yes
- not derived yet: yes
- limitation: proxy-level observable layer

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
- Claim boundary: O5-W6-InvDistance is now supported as a coarse-grained finite-coherence synchronization-tension operator derived from a small-phase TQM lattice-energy approximation. \(\lambda_\Theta\) is currently a disciplined effective response coefficient, still hypothesis-supported and not theorem-level fundamental.

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
