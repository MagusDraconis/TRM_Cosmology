# TRM V5.6 — Experiment Log

**Branch:** `feature/v5.6-recoverfp-minimal-generative-core`
**Base:** V5.5 COMPLETE (19 V5.5 tests, cumulative 2362, 0 failed)
**Date:** 2026-07-16

---

## Initialization

V5.6 initialized from V5.5 COMPLETE (`e44485e`).

Base test count: 2362 cumulative.

**Status:** INITIALIZED

---

## Experiment History

| Date | Suite | Tests | Outcome |
|:---|:---|:--:|:---|
| 2026-07-16 | — | — | BRANCH INITIALIZED from V5.5 |
| 2026-07-16 | MGCP (Protocol) | 10 | DESIGN COMPLETE: MGC1-MGC5 hypotheses, gates A-F defined |
| 2026-07-16 | MGCE (Execution) | 3 | Nm NOT NECESSARY: Skip-Nm doubles high-branch (5->12/30). Double-Cupd destroys branches (5->1/30). Full map partially reducible. |
| 2026-07-17 | MGCA (Analysis) | 10 | GATE A+C REACHED: Nm confirmed branch suppressor. Extra Cupd/DL destructive ONLY with Nm. Without Nm, Double-Cupd amplifies to 29/30 high. Minimal map: Sm→RP→DL→Cupd. RP necessary. DL→Cupd order unresolved. |
| 2026-07-17 | MGCB (Nm Characterization) | 11 | GATE C REACHED: Nm acts exclusively in d-space (K,Omega,KLam1 unchanged). d_max +13.5, d_p90 +0.66, d_std +0.41. Uniform across seeds (CV=0.05). Not branch-selective. |
| 2026-07-17 | MGCD (DSpace Intervention) | 9 | GATES A,B,C,D,E REACHED: d_mean shift eliminates V1 (12→0/30). Full dist match = exact B0. d_max clamp = zero effect. Nm fully reproducible in d-space. Inverse-Nm conditional. |
| 2026-07-17 | MGCF (Nm Dose-Response) | 7 | GATES A,B REACHED: 10% dose halves high (12→6). 25%→B0 (3/30). 40%→zero. Monotonic with sharp threshold. Inverse −50% safely recovers V1 (5→12). |
| 2026-07-17 | MGCG (Minimal Operator) | 6 | GATES C,E REACHED: Fixed shifts fail cross-N. d'=d+0.5×d_mean works N=67,69,72. V9 not suppressible by generic placement. Gate A not reached (N-dependent). |
| 2026-07-17 | MGCH (Deep Pipeline State) | 6 | GATES A,D REACHED: V9 Cupd2 compresses d→amplifies K (ΔK×Ω r=0.957). Op before Cupd2 suppresses V9 (29→10/30). Nm & V9 are symmetric inverse mechanisms through same exp(−d/ξ). |
| 2026-07-17 | MGCI (Cupd2 Dose-Response) | 6 | GATES B,E REACHED: d→K r=−0.998 (deterministic). K→Ω r=0.847. V9 suppresses 29→~9/30 but hits floor. Placement confirmed. Two-component V9 identified. |
| 2026-07-17 | MGCJ (Two-Component Decomp) | 6 | GATES A,B,D REACHED: S0=1, S1=20, S2=9. S2: 3.9× smaller d, 4.3× tighter K, 0% B0/V1-high. KMean threshold gap: S1≤1.115, S2≥1.174. Same mechanism at magnitude extremes. |
| 2026-07-17 | MGCK (Stronger Suppressor) | 6 | GATES A,B,D REACHED: α=3.0× suppresses all S2 (0/9) and all seeds (0/30). dTarget=0.22 suppresses 9/9 S2. KMean=1.080 cap suppresses 8/9. KStd alone = 0 effect. S2 is NOT separate mechanism. |
| 2026-07-17 | **MGCL (Final Synthesis)** | — | **V5.6 COMPLETE.** Full map not fully irreducible. Nm=d-space suppressive regulator. d_mean→Cupd→K controls branch. Two symmetric pathways. 11 suites, 80 tests added, 2435 cumulative, 0 failed. → V5.7 |
