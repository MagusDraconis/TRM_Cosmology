# V5.27 Experiment Log | **Created:** 2026-07-19 | **Updated:** 2026-07-19

| Suite | Status | Tests | Key Finding |
|-------|--------|-------|-------------|
| FCP | COMPLETE | 3 | Protocol defined: frozen M3++, validated chain variables only |
| FCE_01 | COMPLETE | 1 | No information accumulation; c3OmgS strongest predictor |
| FCE_02 | COMPLETE | 1 | c3OmgS>0.1 best frozen model; continuous calibration fails |
| FCE_03 | COMPLETE | 1 | c3OmgS>0.1 replicates across splits/cohorts |
| FCE_04 | COMPLETE | 1 | Two-stratum table calibrated: P_B=17.1%, enrichment 8.3x |
| FCE_05 | COMPLETE | 1 | 13/13 stress splits, null p<0.01%, zero inversions |
| FCA | COMPLETE | 5 | Meta-analysis: c3OmgS>0.1 is preferred model |
| FCI | COMPLETE | 1 | Independent validation: P_A=0.0%, P_B=9.6%, CI separated |
| **Total** | | **14** | **All passed. 0 failed.** |

### Final Calibrated Table (FCI pooled, n=1377)

| Stratum | P(rescue) | 95% CI |
|---------|----------:|--------|
| c3OmgS <= 0.1 | 0.0% | [0.0%, 0.3%] |
| c3OmgS > 0.1 | 9.6% | [6.6%, 13.6%] |

### Cumulative: 2762 tests, 0 failed
