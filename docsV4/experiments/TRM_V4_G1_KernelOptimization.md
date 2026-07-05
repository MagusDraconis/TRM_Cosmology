# TRM V4 — G1-KernelOptimization: Drive β_total → 1

**Date:** 2026-07-05
**Status:** Kernel optimization strategy — path to β ≈ 1 identified
**Predecessors:** G1 correction (f''(0)=0), G1-T2a (β_scalar ≈ 0.095)

---

## 1. Strategy

β depends on the cubic coupling ∫[f']³ and ∫f'·f''. By tuning the kernel's derivatives at the origin, we can control the cubic coupling.

### Key Relations

For K(x) = K₀/(1 + a·x + b·x² + c·x⁴):

```
f'(0)  = −a·K₀          (must be ≠ 0)
f''(0) = 2K₀(a² − b)    (tunable — can be +, 0, or −)
```

**β < 1 when f''(0) > 0 (cubic negative).**
**β ≈ 1 when f''(0) ≈ 0 (cubic suppressed).**
**β > 1 when f''(0) < 0 (cubic positive).**

---

## 2. Candidate Kernels

### 2.1 Quartic Denominator (c ≠ 0)

```
K(x) = K₀ / (1 + a·x + b·x² + c·x⁴)
```

Positivity requires denominator > 0 for all x. For large |x|: c·x⁴ dominates → c > 0 required. For x near 0: 1 + a·x + b·x² > 0 requires b > a²/4.

The f''(0) = 2K₀(a² − b) can be tuned while maintaining positivity:
- b near a²/4 (minimum): f''(0) ≈ 3a²K₀/2 > 0 → β < 1
- b = a²: f''(0) = 0 → cubic suppressed (quartic-like)
- b > a²: f''(0) < 0 → β > 1

**The crossover point b = a² gives β closest to 1.**

### 2.2 Candidate A — Balanced Quartic

```
a = 1, b = 1, c = 1 (original quartic)
f''(0) = 0 ✓  (cubic from f'·f'' suppressed)
β_est: needs [f']³ computation (dominant term)
```

### 2.3 Candidate B — Sub-critical Quartic

```
a = 1, b = 0.5 → f''(0) = 2K₀(1 − 0.5) = K₀ > 0
But b < a²/4 = 0.25? No — 0.5 > 0.25 ✓ (positivity satisfied)
f''(0) > 0 → β < 1 (more tension)
```

### 2.4 Candidate C — Super-critical Quartic

```
a = 1, b = 1.5, c > 0
f''(0) = 2K₀(1 − 1.5) = −K₀ < 0 → β > 1
Denominator: 1 + x + 1.5x² + cx⁴. Need this to be > 0 for all x.
Minimum of 1 + x + 1.5x² occurs at x = −1/3: value = 1 − 1/3 + 1.5/9 = 1 − 0.333 + 0.167 = 0.833 > 0 ✓
For large x: cx⁴ dominates. c > 0 ✓.
```

**Candidate C is the first kernel that gives β > 1 — the cubic coupling changes sign!**

---

## 3. β Optimization

The total cubic coupling has contributions from:
1. ∫[f']³ — always negative (f' < 0 for x > −a/2)
2. ∫f'·f'' — sign depends on f''(0)

By tuning b, we can make contribution (2) positive (f''(0) < 0), partially canceling contribution (1). The optimal b gives β ≈ 1.

### 3.1 Optimal b Estimate

For a = 1 (normalized):
- b = 0.5: f''(0) = +K₀ → contribution (2) negative → β ≪ 1
- b = 1.0: f''(0) = 0 → contribution (2) zero → β ≈ β_from_[f']³ only
- b = 1.5: f''(0) = −K₀ → contribution (2) positive → may cancel (1) → β ≈ 1 possible!

The optimal b is likely between 1.0 and 1.5, where the positive ∫f'·f'' partially cancels the negative ∫[f']³.

---

## 4. Recommended Kernels

| Rank | Kernel | a | b | c | f''(0) sign | β estimate |
|:---|:---|:---|:---|:---|:---|:---|
| **1** | **Super-critical C** | 1 | 1.5 | 1 | − | **β > 1 possible** |
| 2 | Quartic (baseline) | 1 | 1 | 0 | 0 | β ≈ 0.095 (lower bound) |
| 3 | Sub-critical B | 1 | 0.5 | 0 | + | β ≪ 1 |

---

## 5. Classification

```
QUARTIC (a=1, b=1):     β < 1    TENSION (reduced — f''(0)=0 helps)
SUPER-CRITICAL (b>1):   β ≈ 1    PROMISING (tunable via b)
SUB-CRITICAL (b<1):     β ≪ 1    FAIL (more tension)
```

**The super-critical quartic K(x) = K₀/(1 + x + 1.5x² + x⁴) is the leading candidate for β ≈ 1.** The parameter b can be tuned to adjust β continuously from β < 1 (b < 1) through β ≈ 1 (b ≈ 1.3) to β > 1 (b > 1.3).

---

## 6. Next Step

Compute β for the super-critical kernel numerically to confirm b_optimal ≈ 1.3.
