# V5.46 Experiment Log | **Created:** 2026-07-19
| Suite | Status | Tests | Outcome |
|-------|--------|-------|---------|
| EDP | PLANNED | — | — |
| EDE | COMPLETE | 1 | Model C — Post-compression controlled. T0→Entry IQR=1.000. All 11 gates. |
| EDA | COMPLETE | 1 | Model A — T0 inherited spread. N=75 retention=1.14, N=72=0.05. All 11 gates. |
| EDI | COMPLETE | 1 | Model A ROBUST. T0->T2=0.807 w/o N=75. N=75 uniquely broad. |
| EDS | COMPLETE | — | Final synthesis — Model A ROBUST, T0 inherited spread. |

## V5.46 Outcome

**T0 inherited spread dominates.** N=75 uniquely broad at T0 (IQR=1.200 vs 0.047). N=70/72 T1-broad collapses at T2. Model A survives leave-one-N-out audit. Entry IQR→rescue=0.926. Stop-Low safe. Causal closure partially improved. V6 NOT READY.
