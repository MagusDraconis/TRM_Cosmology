# TRM V5.9 Branch Commitment Protocol (BCP)

**Date:** 2026-07-17  
**Suite:** BCP  
**Status:** DESIGN COMPLETE

---

## Purpose

Pre-register the V5.9 commitment protocol: intervention methodology, commitment metrics, irreversibility criteria, decision gates.

---

## 1. Protocol Checks

### BCP.1 — Commitment Problem Defined

Target: identify when branch identity becomes irreversible under RecoverFP dynamics. Distinguish commitment (cannot flip) from predictability (can forecast).

### BCP.2 — Intervention Methodology Pre-Registered

Apply state-conditioned d_mean suppressor at epochs 1–4. Track branch flips vs baseline. No parameter tuning.

### BCP.3 — Commitment Metrics Defined

Flip rate, commitment epoch (flip < 5%), irreversibility onset (flip = 0%).

### BCP.4 — Branch Threshold Frozen

Omega > 1.783 (V5.3 frozen). No redefinition.

### BCP.5 — Decision Gates Pre-Registered

Gate A (commitment before predictability), Gate B (at predictability), Gate C (after predictability), Gate D (no clear commitment).

### BCP.6 — No Physical Interpretation

Stay within RecoverFP operator intervention analysis.

---

## 2. Next Suite

**BCE: Branch Commitment Execution** — apply epoch-level interventions at class-stable N.
