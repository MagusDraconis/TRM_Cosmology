# TRM V5.54 SRA_01 — Seed Realization Structure Audit

**Suite ID:** SRA_01_SeedRealizationStructureAudit
**Version:** 1.0
**Date:** 2026-07-21
**Branch:** `feature/v5.54-rawiqr-origin-and-profile-structure`
**Status:** COMPLETE

---

## 1. Research Question

**Is rawIQR a stable seed-level trait across N and profile construction, or a pooled sampling artifact?**

RIO_01 established that rawIQR variation is seed-realization-associated. SRA_01 asks: does the same seed produce consistently high (or low) rawIQR across different N values? Or is seed-level rawIQR N-specific?

---

## 2. Key Findings

### Part B+C — Cross-N Seed Consistency

| Pair | Pearson r | Spearman ρ | p-value |
|:-----|----------:|-----------:|--------:|
| N70 vs N72 | 0.9248 | 0.9322 | <0.0001 |
| N70 vs N75 | 0.9032 | 0.9047 | <0.0001 |
| N72 vs N75 | 0.9544 | 0.9361 | <0.0001 |

**Mean cross-N r = 0.9275 → C1: strong shared seed trait.**

### Part D — Variance Decomposition

| Component | SS | % |
|:----------|----:|--:|
| Between-seed | 0.03124 | **93.6%** |
| Within-seed (across-N) | 0.00215 | 6.4% |
| Ratio bet/win | 13.91 | — |

**Between-seed variance dominates by 14×.** Seed identity explains 93.6% of total rawIQR variance.

### Part E — Seed Rank Stability

- **20 seeds** are globally high (top 25% at all N)
- **Top-10 retention**: 9/10 (N70→N72), 8/10 (N70→N75), 8/10 (N72→N75), **8/10 at all three N**
- Leave-one-N rank correlations: ρ = 0.93–1.00

### Part F — Generator Realization Diagnostic

The generator creates `new Random(seed)` at each N — same seed = identical first draws. Common-70 verification confirms r = 1.0000 between dedicated 70-draw IQR and N70 IQR from the seed-level table. **The generator seed-reuse structure fully explains cross-N rawIQR consistency.**

Cross-N correlation is INHERENT in the generator design — not a separate "seed trait."

### Part G — rawIQR vs rawMean Independence

| Pair | r |
|:-----|--:|
| rawIQR vs rawMean | −0.078 |
| rawIQR vs rawMedian | +0.059 |
| rawIQR vs rawStd | 0.000 |

**rawIQR is independent from central tendency (|r| < 0.08).** V5.53 composite interpretability is preserved — rawIQR and rawMean are independent seed-level descriptors.

### Part H — Link to SAC Predicate

| Link | Status |
|:-----|:------|
| Seed → rawIQR | SUPPORTED (RIO_01) |
| rawIQR → SAC | SUPPORTED (V5.53 SCP_01) |
| Seed → SAC | CONDITIONAL (sample too small) |
| SAC → ordering | SUPPORTED (V5.52) |

### Part I — Stop-Low Safety

**CONFIRMED SAFE.** No model changes. No new c3OmgS computation. Zero-damage maintained.

---

## 3. Decision

### Model A: rawIQR is a stable shared seed-level trait across N.

**Evidence hierarchy:**
1. Cross-N Pearson r = 0.90–0.95 (strong)
2. Between-seed variance = 93.6% (dominant)
3. Top-10 retention = 8/10 at all N
4. Common-70 verification r = 1.0000 (generator explains)
5. rawIQR independent from rawMean (|r| < 0.08)

**However:** The strong cross-N correlation is a **generator artifact**, not an independent seed property. Because `new Random(seed)` produces identical first-70 draws at each N, the cross-N correlation is built into the pipeline design. This is important context — rawIQR IS seed-stable across N, but the stability mechanism is generator seed-reuse.

---

## 4. Supported Findings

1. Cross-N rawIQR Pearson r = 0.90–0.95 (C1: strong shared seed trait).
2. Between-seed variance = 93.6% of total; within-seed = 6.4%.
3. 20 seeds (of 100) are globally high (top 25% at all N).
4. Top-10 seed retention = 8/10 across all N.
5. rawIQR is independent from rawMean at seed level (|r| < 0.08).
6. Generator seed-reuse fully explains cross-N consistency (common-70 r = 1.0000).
7. V5.53 composite (rawIQR + rawMean) interpretability preserved.
8. Stop-Low safe. Zero damage.

---

## 5. Conditional Findings

- finite-N (N = 70, 72, 75)
- 100 seeds only
- generator-class limited (System.Random)
- cross-N correlation is generator-artifact, not independent trait
- Seed → SAC link not yet confirmed with adequate sample
- diagnostic not causal

---

## 6. Hypotheses

- Alternative RNG implementations would produce different seed-to-rawIQR mappings.
- Seeds that are globally high-rawIQR may show consistent P1 preference in larger SAC samples.
- The seed-reuse structure may propagate downstream through the SAC predicate.

---

## 7. Not Claimed

- Causal mechanism
- Deterministic rescue
- Physical interpretation
- Physical N-boundary
- Universal control
- V6 readiness
- Modified M3++, Stop-Low, or thresholds

---

## 8. Claim Audit

**AUDIT PASSED.** 0 causal claims. 0 physical claims. 0 V6 claims. 0 model modifications.

---

*Generated 2026-07-21. Cross-reference with `TRM.App/wwwroot/data/trm-v5-54-status.json`.*
