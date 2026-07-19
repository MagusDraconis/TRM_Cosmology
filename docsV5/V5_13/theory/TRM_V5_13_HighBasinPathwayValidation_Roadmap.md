# TRM V5.13 Roadmap: High-Basin Pathway Validation and Scaling

**Branch:** feature/v5.13-high-basin-pathway-validation-and-scaling
**Base:** V5.12 COMPLETE

---

## V5.12 Legacy

V5.12 established two candidate High-basin entry pathways:
1. **Compression-Room**: high d0 seeds (>0.50) + 50% relative d-compression → ~14% strict
2. **Crypto-Hi Mild**: crypto-Hi seeds (d0≤0.40, km0>0.98) + 10-15% compression → ~9% strict

Prospective pathway model: MATCHED=10%, UNIV Strong=6%, MISMATCHED=0%.
N=71/72 accessible; N=67 inaccessible.

## V5.13 Goals

1. **Scale**: test pathways on seeds 0-199 (double the seed set)
2. **Validate**: verify pathway rules on out-of-sample seeds (100-199)
3. **Characterize**: map pathway success across N=60-80
4. **Diagnose**: understand N=67 failure mechanism
5. **Quantify**: estimate true pathway success rates with larger N

## Open Questions

- Is the ~10% strict rate fundamental or improvable?
- Does pathway classification hold without overfitting?
- Are there more than two pathways?
- Can we predict persistence before intervention with >50% accuracy?
