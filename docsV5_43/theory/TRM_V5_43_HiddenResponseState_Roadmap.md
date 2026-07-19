# V5.43 Hidden Response State and Temporal Trace Discovery — Roadmap

**Version:** 1.0 | **Date:** 2026-07-19
**Status:** DRAFT

---

## 1. Background

V5.42 established that 7/11 near-identical profiles (matched on lambda1, omDist,
rebMagnitude) diverge on c3OmgS. The measured variables do not uniquely determine
the outcome. This creates a clear research target: what separates divergent profiles?

## 2. Candidate Trace Factors

| Factor | Description | Accessibility |
|:-------|:------------|:-------------|
| Pre-C3 Omega trajectory | Omega evolution before C3 measurement | Available from pipeline epochs |
| d/K evolution path | How d and K evolve through RecoverFP stages | Available from intermediate states |
| Compression response | How profile responds to d-compression stage | Available from compression fraction |
| Omega T1→T2 transition shape | Shape of Omega change between T1 and T2 | Requires intermediate measurement |
| Seed-initialization sensitivity | How initial random seed affects trajectory | Requires re-initialization comparison |
| Node-level heterogeneity | Variance across nodes within a single profile | Available from current measurements |

## 3. Methodology

For each divergent near-identical pair:
- Collect intermediate pipeline states (epoch 0–5 d/K/Omega)
- Compare trajectory shapes between pair members
- Identify the first epoch where trajectories diverge
- Measure which intermediate variable best predicts outcome divergence

## 4. Success Criteria

- Identification of the earliest divergence point in matched pairs
- Characterization of which intermediate variable separates outcomes
- Reduction in unexplained divergence (even partial)
- Stop-Low operational validity preserved

## 5. Conservative Expectation

V5.43 may identify trajectory-level differences without fully closing the causal
gap. Even identifying *when* divergence occurs (pre-C3, during compression, post-C3)
is methodological progress. V6 remains NOT READY.

---

*Generated 2026-07-19. V5.43 theory roadmap.*
