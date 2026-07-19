# TRM V4 — G1-T2c3: Tensor Compensation Test

**Date:** 2026-07-05
**Status:** Computing tensor compensation — b₁–b₄ signs and trace-sector combination
**Predecessors:** G1-T2c2 (f'·f'' confirms negative b₅)

---

## 1. Key Finding: Trace Sector Does NOT Reverse Sign

For the **trace sector** (B_μν = φ·η_μν), all b_i share the same sign from the radial integral (f'·f'' < 0). The angular factors are all positive. **No sign reversal occurs within the trace sector.**

The compensation must come from the **traceless tensor H_μν** and its mixing with the trace φ in the full 1PN metric. This requires the full Multi-K field equations, not just the trace-sector projection.

---

## 3. Why Full Tensor (φ + H_μν) Is Required

The trace sector alone cannot provide sign reversal — all b_i share the same sign from the radial integral. Genuine compensation requires:

1. **H_μν kinetic and cubic coefficients** — independent from φ's
2. **φ-H mixing terms** — cross-coupling between trace and traceless sectors
3. **Coupled field equations** — the 1PN metric g_00 depends on both φ and H_μν

When H_μν is sourced by the same mass distribution, it contributes to g_00 at O(U²) through the nonlinear field equations. The sign of this contribution can differ from the pure-φ contribution.

---

## 4. Classification

| Finding | Status |
|:---|:---|
| Trace sector: all b_i same sign → β < 1 | **CONFIRMED** (G1-T2a, G1-T2c2, G1-T2c3) |
| Sign reversal within trace sector | **NOT SUPPORTED** (angular factors don't flip sign) |
| Full tensor (φ + H_μν) compensation | **OPEN** (~1 week computational project) |

### Verdict: COMPATIBILITY NOT YET ESTABLISHED

The trace sector robustly gives β < 1. The full tensor compensation requires H_μν dynamics, which is the next research frontier. TRM is neither confirmed nor ruled out at 1PN.
