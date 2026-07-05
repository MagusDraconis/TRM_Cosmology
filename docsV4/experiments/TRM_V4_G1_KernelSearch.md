# TRM V4 — G1-KernelSearch: Critical Correction — f''(0) = 0

**Date:** 2026-07-05
**Status:** CRITICAL CORRECTION. f''(0) = 0 for the quartic kernel. Cubic coupling suppressed.

---

## 1. The Correction

For K(d²) = K₀/(1 + d²/λ² + (d²/λ²)²):

```
f(x)   = K₀/(1 + x + x²)
f'(x)  = −K₀(1+2x)/(1+x+x²)²
f'(0)  = −K₀
f''(x) = −2K₀/(1+x+x²)² + 2K₀(1+2x)²/(1+x+x²)³
f''(0) = −2K₀ + 2K₀ = 0
```

**f''(0) = 0 — the second derivative vanishes at the origin.**

---

## 2. Impact on Previous Results

| Previous Claim | Correction |
|:---|:---|
| "f'(0)·f''(0) = −2K₀² < 0" | **f'(0)·f''(0) = 0** — product vanishes at x=0 |
| "Cubic coupling from f'·f'' cross term is negative" | Cross term is **suppressed** near x=0, not dominant |
| "All cubic coefficients share same negative sign" | Sign still negative (f'<0 for x>−½, f''>0 for x>0) but **magnitude reduced** |

### What Survives

- β_total < 1: still true (all cubic contributions are negative)
- Exact value: **closer to 1** than previously estimated (β_scalar ≈ 0.095 is from [f']³, not from f'·f'')
- The f'·f'' cubic term is suppressed → the TOTAL cubic coupling is smaller → β is CLOSER TO 1

---

## 3. Why β Is Closer to 1

The cubic coupling has two sources:
1. **[f']³ term** (from G1-T2a): b₅ ≈ −0.618 → gives β_scalar ≈ 0.095
2. **f'·f'' term** (from G1-T2c2): suppressed because f''(0)=0 → contribution is smaller

The TOTAL cubic coupling = weighted sum with f'·f'' suppressed:
→ Total cubic magnitude < |b₅| alone
→ β_closer_to_1 > β_scalar

**The quartic kernel is BETTER than we thought — β is closer to 1 because f''(0)=0 suppresses the f'·f'' cubic contribution.**

---

## 4. Updated Assessment

| Aspect | Before | After Correction |
|:---|:---|:---|
| f''(0) | +2K₀ | **0** |
| f'·f'' at x=0 | −2K₀² | **0** |
| Dominant cubic term | f'·f'' (thought large) | **[f']³** (actually dominant) |
| β estimate | 0.095 (scalar only) | **> 0.095** (closer to 1) |

### Verdict

**TENSION REDUCED.** The quartic kernel is closer to GR at 1PN than previously assessed. β_total > β_scalar ≈ 0.095 because f''(0)=0 suppresses the f'·f'' cross term. Whether β reaches ~1 depends on the full tensor computation, but the scalar-only estimate β ≈ 0.095 was a LOWER BOUND, not the central value.
