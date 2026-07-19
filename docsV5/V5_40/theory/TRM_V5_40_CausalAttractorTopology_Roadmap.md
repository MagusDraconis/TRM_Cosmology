# V5.40 Causal Closure and Attractor Topology — Roadmap

**Version:** 1.0 | **Date:** 2026-07-19
**Status:** DRAFT

---

## 1. Background

V5.39 characterized attractor absorption: K-state perturbation (±15%) produces
only ~0.027 delta in lambda1. The attractor protects its internal state coordinates
against external perturbation. This explains why V5.38 RII's diagnostic-to-causal
conversion attempt failed.

The central open question: if direct state manipulation is absorbed, how can
causal closure be tested?

## 2. Key Insight from V5.39

The perturbation is not fully eliminated — it propagates downstream through
Omega T1 → Omega T2. While lambda1 converges (partial absorption), c3OmgS
shifts substantially (delta 0.1468). This suggests:

- **Early-stage absorption is partial, not total.**
- **Downstream amplification may enable indirect causal probing.**
- **Working with attractor flow direction may succeed where counter-flow fails.**

## 3. Candidate Approaches

| Approach | Description | Risk |
|:---------|:------------|:-----|
| Multi-stage perturbation | Perturb d_tail + K simultaneously | Attractor may absorb both |
| Directional alignment | Perturb aligned with natural attractor gradient | May be observationally indistinguishable |
| Topology probing | Map attractor basin structure via systematic perturbation grid | Exploratory, may not yield causal leverage |
| Parametric leverage | Identify parameter regimes where attractor resistance weakens | Risk of overfitting / narrow domain |
| Response timing | Time perturbation relative to C3 application | May interact with Omega restoration |

## 4. Decision Criteria

For any candidate approach:
1. Does it produce directional c3OmegaShift shift beyond baseline?
2. Is the effect distinguishable from diagnostic correlation?
3. Does it survive hostile audit?
4. Does it preserve Stop-Low safety?

## 5. Conservative Expectation

The most likely outcome is partial or negative. V5.35-V5.39 have consistently
found that diagnostic power does not convert to causal control. The attractor
may be fundamentally resistant to causal manipulation via current observable
channels. V6 remains NOT READY.

---

*Generated 2026-07-19. V5.40 theory roadmap.*
