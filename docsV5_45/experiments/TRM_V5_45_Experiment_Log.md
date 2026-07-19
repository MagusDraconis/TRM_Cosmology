# V5.45 Experiment Log | **Created:** 2026-07-19
| Suite | Status | Tests | Outcome |
|-------|--------|-------|---------|
| CAP | PLANNED | — | — |
| CAE | COMPLETE | 1 | Model E — Mixed N+state autonomy. Hi-bridge [72,75], Lo [67,70]. All gates. |
| CAA | COMPLETE | 1 | Model E confirmed. N=67 range=0.049. Bridge~entOm_std=0.593. Gate C not reached. |
| CAI | COMPLETE | 1 | N=70 IQR=0.023 solved. Model D. All 10 gates. |
| CAS | COMPLETE | — | Final synthesis — Model D, distribution shape, IQR key. |

## V5.45 Outcome

**Bridge bimodality explained.** Entry variance necessary (N=67), not sufficient (N=70).
Distribution shape (IQR) controls bridge. Model D — entry-variance + N-window interaction.
Stop-Low safe. Causal closure partially improved. V6 NOT READY.
