# TRM Current Status for Peer Review

## Paper core positioning

TRM should currently be presented as a **weak-field effective transport/synchronization framework**, not as a claim that TRM replaces GR.

---

## Executive status snapshot

- **tested:** Broad numerical test coverage exists for weak-field gravity/redshift baselines, Mercury/perihelion blocks, photon-transport scaling/invariants, Schwarzschild reference comparisons, SPARC/RAR pipelines, CMB and Pantheon pipelines, and deterministic cluster baseline tests.

- **tested-effective:** Three candidate sectors are now tested-effective at guard level.
  1. Theta/O5 path (**TO01–TO28, TQK01–TQK04, LC01–LC08**) supports O5-W6-InvDistance plus regularized \(\lambda_\Theta\), and Gap 3 is strengthened by **TOL01–TOL04** into a lattice-energy-supported derivation chain \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\).
  2. Vector frame-dragging path (**FD01–FD20**) supports weak-field Lense-Thirring scaling shape, stable effective \(k_T\), scalar-limit preservation at zero spin, and prograde/retrograde light-path asymmetry; FD16 adds a non-fitted microscopic-response normalization proxy for \(k_T\), FD17 shows derived-\(k_T\) stability under source-discretization, probe-geometry, and spin-axis ablations, FD18 validates a weak-field LT compatibility window with frozen derived \(k_T\) (no refit), FD19 keeps this compatibility under SI/dimension-aware unit scaling, and FD20 adds explicit systematic-bias control auditing with a small controlled high-side bias.
  3. Memory microscopic path (**MC09–MC12**, plus MEM/TRM/MC baseline block) strongly supports \(A_{\mathrm{dyn}}\propto\phi \rightarrow A_{\mathrm{dyn}}^2|\dot{\mu}| \rightarrow \phi^2|\dot{\mu}|\) up to an effective coupling scale, with \(R^2_{\mathrm{MC11}}=0.999799\) and EL/Fermat bridge retention in MC12 (\(\text{bridgeRetention}=0.957676\), \(\text{meanGap}=1.64\times10^{-4}\)).
  4. Gap-2 \(m=3\) closure has a strongly constrained theorem path via **RBF16–RBF20** (continuous-threshold region, derived action/tick discriminator, solver-family robustness, failure-by-family exclusion, and artifact audit).

- **calibrated:** Core response/cosmology parameters are still calibration-backed in production usage (including parts of `HT/BetaEta/Alpha`, transport coefficients, and regime-related coefficients).

- **not derived yet:** Theorem-level first-principles closure remains open for (a) the \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\) chain (now strongly supported as a derivation chain, still not theorem-level), (b) the memory channel \(\phi^2|\dot{\mu}|\), and (c) microscopic theorem closure of \(m=3\).

---

## Claim-safe one-liner

> O5-W6-InvDistance with regularized regime-conditioned \(\lambda_\Theta\) and TOL01–TOL04 now support a strongly supported \(\Theta \rightarrow O_5 \rightarrow \lambda_\Theta \rightarrow g_{\mathrm{obs}}\) derivation chain; together with the FD01–FD20 vector extension path, the MC09–MC12 memory path, and the RBF16–RBF20 \(m=3\) theorem-path hardening, these are strong weak-field evidence blocks that remain below theorem-level first-principles proof.  
> For Gap 1 specifically: MC09–MC12 strongly support a lattice-proxy derivation path for \(\phi^2|\dot{\mu}|\) up to an effective coupling scale, but this is not yet theorem-level microscopic closure.

---

## Next theory block (highest priority)

Build a unified effective action roadmap for:

\[
T,\ \vec A_T,\ \Theta
\]

that keeps current claim-safe boundaries and shows how scalar transport, vector frame-dragging, and theta-observable sectors connect in one derivation framework.

Current unification hardening:
- UF01–UF08 are passing and currently support limit consistency, additive zero-coupling decomposition, bounded small cross-couplings, and globally identifiable cross-couplings without per-group refit dependence.
- Cross-sector interaction structure remains candidate-level and below theorem-level unification.
