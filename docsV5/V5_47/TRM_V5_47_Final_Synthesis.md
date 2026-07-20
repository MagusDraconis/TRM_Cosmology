# TRM V5.47 Final Synthesis — Post-Warmup T0 Spread Origin and N-Window Formation

**Version:** 1.0 | **Date:** 2026-07-20
**Branch:** `feature/v5.47-post-warmup-t0-spread-origin-and-n-window-formation`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.47-post-warmup-t0-spread-origin-and-n-window-formation` |
| Base | V5.46 COMPLETE |
| Suites | TSP, TSE, THD, TSA, TSS |
| V5.47 tests | 5 (1 TSP + 1 TSE + 1 THD + 1 TSA + 1 TSS) |
| Cumulative tests | 2877 passed, 0 failed |
| Commits | `2c0a587` (init), `f7b2e05` (TSP), `a9c5906` (TSE), `1082d92` (THD), `7c12810` (TSA), `TSS` (this doc) |

---

## 2. Research Question

**Why does T0 post-warmup spread differ by N, especially why is N=75 uniquely broad before the later entry bridge stage?**

Answer: **The post-warmup handoff w2→T0 is the key N-window formation transform.** N=75 amplifies through this handoff (1.98×) while N=72 collapses (0.11×). The handoff is rank-inverting. d/K at w2 near-perfectly diagnoses profile-level delta_omega (|r| > 0.92). N-window outcome — amplification vs collapse — is determined by w2-state distribution **spread** (km/lambda IQR), not by mean d_w2. N=75 achieves bulk-wide amplification; N=72 achieves bulk collapse; N=70 remains stable compressed.

---

## 3. Suite Summaries

### TSP_01 — T0 Spread Origin Protocol
**Model A: T0 inherited spread dominates. N=75 genuinely broad at origin.**
- T0→entry IQR corr = 1.000 (confirms V5.46)
- N=75 T0 IQR = 1.200 (25× next highest), IQR/range = 0.93 → bulk-wide
- Warmup trace: N=75 NOT broad at epoch-0 (IQR=0.004), broadness appears post-warmup
- d/K pre-state: diagnostic only, not causal
- Model A robust without N=75 (T0→entry = 0.740)
- 63 profiles across N={67,70,72,75}

### TSE_01 — Spread Evolution Post-Warmup Handoff Audit
**Model A: w2→T0 handoff amplifies N=75 bulk spread.**
- N=75: w2 IQR=0.604 (IQR/range=0.20, NOT bulk-wide) → T0 IQR=1.200 (amp=1.98×)
- N=72: w2 IQR=0.433 (IQR/range=0.31, bulk-wide!) → T0 IQR=0.047 (amp=0.11×, collapse)
- N=72 paradox resolved: bulk-wide at w2, collapses at T0 handoff
- w2→T0 amp ~ T0 IQR corr = 0.823
- Full d/K/lambda per-stage instrumentation across 7 checkpoints

### THD_01 — Handoff Discriminator Audit
**Model E: d/K at w2 near-perfectly diagnostic. Handoff is rank-INVERTING.**
- N=75 Spearman = -0.671, N=72 Spearman = -0.483 — both rank-INVERTING
- **Contrary to hypothesis:** ranks are NOT preserved; handoff inverts/scrambles rank order
- d_w2 ~ delta_om: N=72=-0.923, N=75=-0.964 (near-perfect profile-level diagnostic)
- km_w2 ~ delta_om: N=72=0.956, N=75=0.964
- d/K strongly diagnostic within each N, but does NOT explain amp vs collapse between N
- Shape transforms: N=75=S1 Bulk-wide amp, N=72=S2 Bulk collapse, N=70=S3 Stable compressed

### TSA_01 — Handoff Discriminator Stability Audit
**SUPPORTED stability — all 8 claims survive robustness.**
- 10 random splits per N: all d/K signs stable, rank inversion sign-stable
- Leave-one-profile jackknife: no single profile flips any conclusion
- N=75 amp jackknife mean=9.26, min=1.59 — direction always positive
- Cross-N: mean d_w2 nearly identical (N=72=0.457, N=75=0.458)
- km/lam IQR: N=75/N=72 = 1.48× — w2-state spread distinguishes outcomes
- Stop-Low: 41 stop, 0 rescues — zero damage across all cuts

---

## 4. Final Decision Model

### Model A+E: Handoff Transform N-Window Formation

**The post-warmup handoff w2→T0 is the key N-window formation transform.**

1. The transform is **rank-inverting** — ranks are scrambled/inverted during handoff for all N.
2. **d/K at w2 strongly diagnoses** profile-level delta_omega within each N (|r| > 0.92).
3. **Inter-N outcome** (amplification vs collapse) is determined by w2-state **distribution shape/spread**, especially km/lambda IQR, not by mean d_w2.
4. **N=75** = bulk-wide amplification (S1).
5. **N=72** = bulk collapse (S2).
6. **N=70** = stable compressed (S3).

**This is a diagnostic/observational model. Not causal closure. Not a control policy.** Stop-Low remains outcome-validated and unchanged.

---

## 5. Supported Findings

1. T0 inherited spread dominates entry IQR (corr = 1.000, confirms V5.46).
2. N=75 is uniquely broad at T0 (IQR=1.200, 25× next highest).
3. N=75 bulk-wide broadness emerges between w2 and T0 (w2 IQR/range=0.20 → T0 IQR/range=0.93).
4. w2→T0 handoff amplifies N=75 (1.98×) and collapses N=72 (0.11×).
5. The handoff is rank-inverting (Spearman N=75=-0.671, N=72=-0.483).
6. d_w2 anti-correlates with delta_omega: N=72=-0.923, N=75=-0.964 (sign-stable).
7. km_w2 correlates with delta_omega: N=72=0.956, N=75=0.964 (sign-stable).
8. N=75 shape class = S1 bulk-wide amplification (stable).
9. N=72 shape class = S2 bulk collapse (stable).
10. N=70 shape class = S3 stable compressed (stable).
11. w2-state spread (km/lam IQR, 1.48× ratio), not mean d_w2 (1.00× ratio), distinguishes N=72 vs N=75.
12. Stop-Low remains safe: c3OmgS ≤ 0.1 has 0 rescues across all N and cuts.
13. No single profile controls the results (jackknife verified).
14. All 8 stability claims survive random splits and leave-one-profile jackknife.

---

## 6. Conditional Findings

- finite-N limitation (12-20 profiles per N for N=70/72/75)
- tested operator classes only (P1/P1b profiles)
- available instrumentation only (warmup epochs w0-w2, T0-T3, C3)
- N=72 vs N=75 comparison is sensitive to profile-count limits
- diagnostic only — no causal closure claimed
- no physical meaning of N
- hidden pre-warmup factor may remain
- hidden handoff operator may remain
- V6 NOT READY

---

## 7. Hypotheses

- w2→T0 handoff transform may be a stable N-window formation mechanism.
- Distribution spread/IQR may determine whether handoff produces amplification or collapse.
- km/lambda IQR may be the key diagnostic signature distinguishing N=75 from N=72.
- A hidden handoff operator may remain uninstrumented.
- Future instrumentation may need to capture the exact w2→T0 operation mechanism.

---

## 8. Not Claimed

- causal mechanism for handoff amplification vs collapse
- deterministic rescue
- physical N-boundary or physical interpretation of N
- universal adaptive control
- V6 readiness
- modified M3++
- modified Stop-Low policy
- retuned c3OmegaShift threshold
- new model variables or correction classes
- physical theory interpretation
- physical length, c, GR, spacetime, or cosmology derivation

---

## 9. Claim Audit

| Forbidden Claim | Status |
|:----------------|:------|
| Causality | NOT CLAIMED |
| Physical interpretation of N | NOT CLAIMED |
| V6 readiness | NOT CLAIMED |
| Deterministic rescue | NOT CLAIMED |
| Threshold retuning | NOT CLAIMED |
| New variables | NOT CLAIMED |
| M3++ modification | NOT CLAIMED |
| Stop-Low modification | NOT CLAIMED |
| Physical constants or GR comparison | NOT CLAIMED |

**AUDIT PASSED.** All forbidden claims absent from final synthesis.

---

## 10. V5.47 Resolution

**V5.47 asked:** Why does T0 post-warmup spread differ by N, especially why is N=75 uniquely broad?

**V5.47 answered:** The w2→T0 handoff is the key N-window formation transform. The transform is rank-inverting. d/K at w2 near-perfectly diagnoses profile-level delta_omega. N-window outcome — amplification vs collapse — is determined by w2-state distribution shape/spread (km/lambda IQR), not by mean d_w2. N=75 amplifies to bulk-wide T0 spread; N=72 collapses; N=70 remains stable compressed.

**What remains unresolved:**
- Why does the handoff amplify N=75 but collapse N=72? km/lambda IQR differs (1.48×) but the mechanism is not causally closed.
- Pre-warmup instrumentation still missing — origin before w0 remains unknown.
- The exact w2→T0 operation is not directly measured — observed only through its transform output.

**V6 remains NOT READY.** Causal closure remains blocked. Stop-Low remains outcome-validated.

---

## 11. Next Steps / V5.48 Recommendations

- **TSI (Instrumentation):** Add pre-warmup instrumentation to capture state before w0.
- **W2→T0 mechanism:** Measure the exact handoff operation rather than inferring from transform output.
- **Cross-N N-window formation:** Extend analysis beyond 4 N values for broader validation.
- V6 derivation, causal closure, and physical interpretation remain NOT READY.

---

## 12. Suite Reference

| Suite | Tests | Status |
|:------|------:|:------:|
| TSP | 1 | COMPLETE |
| TSE | 1 | COMPLETE |
| THD | 1 | COMPLETE |
| TSA | 1 | COMPLETE |
| TSS | — | THIS DOCUMENT |
| **Total** | **5** | **0 failed** |

Cumulative: 2877 tests, 0 failed.
