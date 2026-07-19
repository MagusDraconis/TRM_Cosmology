# TRM V5.7 — Reduced Operator Validation Roadmap

**Date:** 2026-07-17  
**Base:** V5.6 COMPLETE (2435 tests, 0 failed)  
**Branch:** `feature/v5.7-recoverfp-reduced-operator-validation`

---

## 1. Core Question

Does the V5.6 reduced operator model generalize outside the discovery regime (N=67,69,72; seeds 0–29)?

---

## 2. V5.6 Mechanism Under Test

The following V5.6 mechanism is treated as a **hypothesis under falsification test** in V5.7:

### Suppression Pathway

```
d' = d + 0.5 * d_mean_current  (state-conditioned operator)
→ Cupd: K = K0 * exp(-d'/xi)
→ K_mean decreases
→ branch suppression
```

### Amplification Pathway

```
Skip-Nm + Double-Cupd
→ d compression before Cupd2
→ Cupd2: K = K0 * exp(-d/xi)
→ K_mean increases
→ branch activation
```

### Central Prediction

The d-state entering Cupd controls the branch outcome through the deterministic exponential mapping, independent of N (within tested range).

---

## 3. Validation Axes

### Axis A: N Generalization

| N | Purpose |
|---|---------|
| 60 | Below V5.6 discovery range |
| 62 | Below V5.6 discovery range |
| 64 | Below V5.6 discovery range |
| 66 | Below V5.6 discovery range |
| 67 | V5.6 baseline (calibration) |
| 69 | V5.6 baseline |
| 72 | V5.6 baseline |
| 75 | Above V5.6 discovery range |
| 80 | Above V5.6 discovery range |

**Test:** For each N, run B0 (with Nm), V1 (skip-Nm), and V1+state-conditioned operator. Compare high-branch fractions.

### Axis B: Seed Generalization

| Block | Seeds | Purpose |
|-------|-------|---------|
| Block 0 | 0–29 | Original V5.6 block (calibration) |
| Block 1 | 30–59 | Independent validation block |
| Block 2 | 60–99 | Independent validation block |

**Test:** Check whether operator performance is stable across blocks.

### Axis C: Mechanism Stability

| Mechanism | Test |
|-----------|------|
| Nm suppression | Compare B0 (with Nm) vs V1 (skip-Nm) across all N |
| Cupd2 amplification | Compare V1 vs V9 across N=67,72,80 |
| d_mean → K | Check d_mean before Cupd predicts K_mean at all N |

### Axis D: Operator Robustness

| Operator | Description |
|----------|-------------|
| Full Nm | Standard RecoverFP Nm normalization |
| State-conditioned | d' = d + 0.5 * d_mean_current |
| Fixed 25% | Fixed 25% of N=67 MGCB deltas (expected to fail at some N) |
| Fixed 40% | Fixed 40% of N=67 MGCB deltas |

### Axis E: Threshold Robustness

Check whether the d_mean dose-response relationship remains monotonic at N outside V5.6 range.

---

## 4. Failure Criteria

| ID | Failure Condition | Consequence |
|----|-------------------|-------------|
| F1 | Reduced operator only works at N=67 | Mechanism is N-local, not general |
| F2 | Reduced operator only works on seeds 0–29 | Mechanism is seed-block-overfit |
| F3 | Cupd2 amplification not reproducible | V9 mechanism is regime-dependent |
| F4 | d_mean no longer predicts K at some N | d→K mapping is N-conditioned |
| F5 | State-conditioned operator produces invalid d | Operator is numerically unstable outside discovery range |

---

## 5. Decision Gates

| Gate | Condition | Interpretation |
|------|-----------|----------------|
| **A** | Operator works across all N and seed blocks | Mechanism robust — supports V5.6 model |
| **B** | Operator works at most N, but thresholds vary | Partially robust — requires N-conditioned refinement |
| **C** | Operator works only at N=67–72 | Weak generalization — highly regime-dependent |
| **D** | Operator fails at most N or seed blocks | Mechanism does NOT generalize — V5.6 was overfit |

---

## 6. Suites

| Suite | Tests | Purpose |
|-------|-------|---------|
| ROCP | 8 | Protocol: pre-register axes, criteria, gates |
| ROCE | ~15 | Execution: cross-N, cross-seed, operator comparison |
| ROCA | ~8 | Analysis: robustness evaluation, failure check, gate classification |
| ROCS | ~0 | Synthesis: branch completion report |

---

## 7. V5.7 Non-Goals

V5.7 does NOT:
- Discover new mechanisms
- Test new operators beyond V5.6 variants
- Explore regimes outside xi=1.75, K0=1.20, s=0.10
- Expand to N > 80
- Introduce physical interpretation
- Claim universality
