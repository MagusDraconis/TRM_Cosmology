# TRM V5.17 Final Synthesis

**Branch:** feature/v5.17-adaptive-response-and-rebound-control
**Status:** COMPLETE | **Date:** 2026-07-18
**Base:** V5.16 COMPLETE (2644 tests)

## 1. Branch Metadata

| Item | Value |
|------|-------|
| Branch | `feature/v5.17-adaptive-response-and-rebound-control` |
| Suites | ARP, ARE, ARA, ARI, ARV, ARS |
| Commits | 250f2f3, b19dd0a, 3f31a6c, 1cf5d49, f756e73, 09b929f |
| V5.17 tests | 7 (ARP:3, ARE:1, ARI:1, ARV:1, ARS:1) |
| Cumulative | 2651 (all passed, 0 failed) |

## 2. Research Question

**Can post-intervention response dynamics be used for adaptive second-stage control?**

Answer: **YES.** Entry-vector re-alignment (C3) provides +13pp holdout improvement. Static ceiling breached. Adaptive control is validated, conditional, and N-dependent.

## 3. Suite Summaries

### ARP — Protocol (3 tests)
Registered adaptive-response framework, frozen M3+ baseline, rebMagnitude probe threshold.

### ARE — Adaptive Response Execution (1 test, ~70s)
Tested additional compression as second-stage correction. Probe works perfectly (ES=2.07). Compression rescue fails (+2.4%). Universal compression catastrophic (-51%).

### ARA — Response Analysis (analysis only)
Classified ARE failure as correction-type mismatch, not absolute ceiling. Compression destroys high-reb successes, can't fix low-reb failures.

### ARI — Alternative Correction Audit (1 test, ~70s)
Tested 5 alternative corrections. **Entry-vector re-alignment rescues 6/9 (67%).** K-preserve rescues 4/9 (44%). Static ceiling breached.

### ARV — Adaptive Re-Alignment Validation (1 test, ~90s)
Validated on holdout. **C3: +13pp over A0 (60%→73%), 4 rescues, 0 damage.** C1 matches at 73%.

### ARS — Adaptive Control Synthesis Audit (1 test, ~80s)
C3 (83%) preferred over C1 (78%). 38% rescue overlap — complementary mechanisms. C3 has broader coverage (7 vs 4 rescues).

## 4. Supported Findings

| # | Finding | Evidence |
|---|---------|----------|
| 1 | rebMagnitude is near-perfect post-intervention classifier | ES=2.17 holdout |
| 2 | Additional compression is the wrong correction | ARE: -51% universal |
| 3 | Adaptive control is possible | ARI: 67% rescue rate |
| 4 | Entry-vector re-alignment is strongest correction | ARV: +13pp holdout |
| 5 | K-preserve also improves outcomes | ARV: +13pp, 73% |
| 6 | Adaptive control surpasses static M3+ | 60%→73% holdout |
| 7 | Static ceiling ≠ adaptive ceiling | Ceiling breached |
| 8 | N=67 inaccessible, N=80 saturated | Confirmed |

## 5. Key Adaptive Result

| Model | Holdout | Δ |
|-------|---------|---|
| A0 (M3+ static) | 60% | — |
| **C3 (adaptive)** | **73%** | **+13pp** |

4 rescues, 0 damage.

## 6. Final Model — M3++

**P1/P1b + projHiVec + orthHiVec(N=72) + rebMagnitude probe + C3 correction**

| Stage | Action |
|-------|--------|
| 1. Static select | M3+: P1/P1b + projHiVec + orthHiVec(N=72) |
| 2. First intervention | Matched strong compression (50%) |
| 3. Probe | Measure rebMagnitude at T1→T2 |
| 4. Gate | If rebMag < 0.01 → apply C3 correction |
| 5. C3 correction | Nudge d 20% toward Hi-centroid |
| 6. Continue | Run +2 epochs, verify persistence |

## 7. Weakened / Rejected
Additional compression rescue, universal second correction, static ceiling as absolute, compression-only adaptive model.

## 8. Not Claimed
Universal adaptive control, complete persistence explanation, physical interpretation, optimality, universal scaling.

## 9. Final Conclusion

V5.17 converts rebMagnitude from explanatory variable into practical adaptive-control signal. Entry-vector re-alignment provides validated second-stage correction exceeding M3+. The V5.15 ceiling is static-only. Adaptive control is conditional, N-dependent, and limited to tested corrections.

## 10. Recommended V5.18

**Branch:** `feature/v5.18-adaptive-control-generalization`

**Question:** Does adaptive control generalize to larger cohorts, additional N windows, and independent validation?

**Suites:** AGP → AGE → AGA → AGI → AGS
