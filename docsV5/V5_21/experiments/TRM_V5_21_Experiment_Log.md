# V5.21 Experiment Log | **Created:** 2026-07-18
| Suite | Status | Tests | Results |
|-------|--------|-------|---------|
| LRP | COMPLETE | 3 tests (protocol only) | 3/3 passed |
| LRE | COMPLETE | 5 tests | 5/5 passed, 741 intervention results, ~3 min |
| LRI | COMPLETE | 5 tests | 5/5 passed, 864 transferred interventions, ~6.5 min |
| LRS | PENDING | — | Recommended next — final synthesis |

## LRI Summary (2026-07-18)
- **Hypothesis tested:** Is low-N immunity a missing-reference artifact?
- **Hypothesis FALSIFIED:** Transferred references do NOT enable low-N rescue
- **864 interventions:** 0% induction, 0% strict persistence across all refs
- **Failure shifts:** Off-vector (100%→near-zero) → Insufficient displacement (50%) + Ref too far (35%)
- **d-space gap:** Low-N candidates at d≈0.3-0.45, references at d≈0.6-1.0 — too large to bridge
- **N=64 example:** dPre +50%, omT2 +0.1% — direction changes, regime doesn't
- **Gates reached:** C (Low-N remains immune)
- **Verdict:** Low-N inaccessibility is true non-inducibility, NOT a missing-reference artifact

## LRE Summary (2026-07-18)
- **Baseline:** N=50-64 CONFIRMED rescue-immune, N=65 CONFIRMED onset
- **Interventions:** 13 operator variants tested across 10 N values
- **Gates reached:** C (Remains immune), H (Unsafe/invalid at N<64)
- **Gates not reached:** A, B, D, E, F, G
- **Key finding:** Off-vector response dominates N=50-63 failure (96-100%)
- **Verdict:** Low-N boundary NOT breakable under tested operator classes

## LRA Summary (2026-07-18)
- **Mechanism:** Three-phase lower boundary
- **Phase 1 (N=50-63):** Natural High branch empty → Hi centroid nonexistent → projHiVec = 0 → exactly orthogonal
- **Phase 2 (N=64):** Hi centroid first appears, but C3 omega shift = 0.009 (negligible)
- **Phase 3 (N=65):** C3 omega shift jumps 15.8x → first rescue
- **Rescued seeds have negative alignPre** — they need correction, not just proximity
- **Gates reached:** A (Off-vector barrier), C (Onset mechanism), E (Response-direction dominates)
- **Boundary model: B — Response-direction barrier / C3-effectiveness threshold**
