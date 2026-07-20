# V5.50 Experiment Log | **Created:** 2026-07-20
| Suite | Status | Tests | Outcome |
|-------|--------|-------|---------|
| KCA | COMPLETE | 1 (KCA_01) | Model D — Pre-spread preorders kernel classes. Regression-to-mean signature. |
| KAS | COMPLETE | 1 (KAS_01) | Model A — Assignment STABLE. Amp constraints jackknife-robust. Synthesis-ready. |

## KCA_01 — Kernel Class Assignment Origin Audit (2026-07-20)
- **Passed.** 47s execution. **Decision: Model D — Pre-transition spread preorders kernel classes.**
- **K1 (N=72): LARGEST pre-IQR (0.154) → compresses inward (0.57×)**
- **K2 (N=75): SMALLEST pre-IQR (0.086) → expands outward (1.50×)**
- **K3 (N=70): INTERMEDIATE pre-IQR (0.126) → stays stable (1.04×)**
- Regression-to-mean pattern: wide profiles compress, narrow profiles expand
- Mean state nearly identical across classes (ratio ~1.00) — fails to separate
- Movement balance (I/O) correlates but doesn't cleanly preorder
- Jackknife: K1 20/20 stable, K2 11/12, K3 15/20
- Pre-spread ordering is transition-specific (w1→w2 only, not w2→T0)
- Amp ratio remains the definitive post-transition class separator
- Stop-Low: 35 stop, 0 rescues → SAFE
