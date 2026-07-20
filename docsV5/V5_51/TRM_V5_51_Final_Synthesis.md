# TRM V5.51 Final Synthesis — Spread-Order Origin and Kernel-Assignment Mechanism

**Version:** 1.0 | **Date:** 2026-07-20
**Branch:** `feature/v5.51-spread-order-origin-and-kernel-assignment-mechanism`
**Status:** COMPLETE

---

## 1. Branch Metadata

| Property | Value |
|:---------|:------|
| Branch | `feature/v5.51-spread-order-origin-and-kernel-assignment-mechanism` |
| Base | V5.50 COMPLETE |
| Suites | SOO, PWO, FEG, PSO, ERO, RFO, TSS |
| V5.51 tests | 7 |
| Cumulative tests | 2890 passed, 0 failed |

---

## 2. Research Question

**Why does the pre-transition spread ordering K1 > K3 > K2 exist? Where does it come from?**

Answer: **The ordering originates as a stable between-profile raw-frequency ensemble ordering, present before any simulation.** It is amplified by Sim(om), disrupted by DL(d), restored by Cupd(km), strengthened at w0→w1, and destroyed by the w1→w2 regression-to-mean kernel. The deepest instrumented origin is raw-frequency ensemble means — no pre-init instrumentation exists.

---

## 3. Suite Summaries

### SOO_01 — Spread Order Origin Audit
**Model A: Ordering K1>K3>K2 inherited from w0.**
- Present at w0 (0.057 > 0.049 > 0.024), strengthened at w1, destroyed at w2

### PWO_01 — Pre-W0 Instrumentation Audit
**Model B: Ordering generated at init→w0.**
- init(K): NO ordering (0.0035, 0.0041, 0.0034 — uniform random graph)
- w0: YES ordering — first epoch creates it

### FEG_01 — First Epoch Spread Generation Audit
**Model A: Ordering first appears at Sim(om).**
- Substage trace: init→Sim(om)=YES, DL(d)=NO, Cupd(km)=YES
- Create-Disrupt-Restore cycle identified

### PSO_01 — Phase/Omega Ordering Origin Audit
**Model G: Ordering is ensemble-level, not within-profile.**
- Raw freq IQR, phase spread, within-profile omega: all NO ordering
- Only between-profile mean omega shows ordering

### ERO_01 — Ensemble Response Ordering Audit
**Model C: Raw frequency ensemble means already K1>K3>K2.**
- Raw freq mean IQR: K1=0.0109 > K3=0.0079 > K2=0.0071
- Sim amplifies K1 (1.1×), compresses K3/K2 (0.9×)
- Metric identity resolved: between-profile = ordering, within-profile = none

### RFO_01 — Raw Frequency Ordering Stability Audit
**Model A: Ordering is STABLE.**
- Jackknife: 59/59 (100%)
- Random splits: 8/10 (80%)
- Null test: 16/100 (16% by chance)
- Deepest instrumented origin confirmed

---

## 4. Final Decision Model

### Model A+G: Ensemble-Origin Spread Ordering

**Kernel-class spread ordering originates as a stable between-profile raw-frequency ensemble ordering.**

The full lifecycle:

```
init(K): NO ordering (random graph, uniform K=0.5)
  ↓ profile selection + raw frequency sampling
raw freq means: K1>K3>K2 (STABLE, jackknife-verified)
  ↓ Sim amplifies K1
Sim(om): K1>K3>K2 (amplified)
  ↓ DL disrupts
DL(d): NO ordering
  ↓ Cupd restores
Cupd(km): K1>K3>K2 (restored)
  ↓ w0→w1 strengthens
w1: K1>K3>K2 (pre-transition → kernel-class assignment)
  ↓ w1→w2 regression-to-mean kernel
w2: NO ordering (destroyed/reordered)
```

**This is a diagnostic/observational model. Not causal closure. Not a control policy.**

---

## 5. Supported Findings

1. K1>K3>K2 ordering is absent at init(K) (random graph, uniform coupling).
2. K1>K3>K2 ordering is present in raw-frequency ensemble means.
3. Raw-frequency ordering is stable (jackknife 100%, splits 80%, null 16%).
4. Sim(om) amplifies the raw-frequency ensemble ordering.
5. Ordering is between-profile, not within-profile.
6. Phase spread does not show the ordering.
7. Within-profile omega spread does not show the ordering.
8. DL(d) disrupts the ordering (K3 becomes largest).
9. Cupd(km) restores the ordering (differential K-update compression).
10. w0→w1 strengthens the ordering.
11. w1→w2 kernel destroys the preordering (regression-to-mean).
12. Stop-Low remains safe: 42 stopped, 0 rescues.
13. V6 NOT READY. Causal closure remains blocked.

---

## 6. Conditional Findings

- finite-N limits (12-22 profiles per N)
- seed/profile sampling limits
- tested operator classes only (P1/P1b)
- instrumentation limits (no pre-init state beyond K matrix)
- raw-frequency ordering may partially reflect profile-count structure
- null test not equivalent to causal proof
- no physical meaning of N or kernel class
- no causal closure
- V6 NOT READY

---

## 7. Hypotheses

- Raw-frequency ensemble sampling may seed kernel-class assignment.
- Sim may amplify seed/profile-level ensemble differences.
- The rank-inverting kernel may transform ensemble preordering into compression/stable/expansion classes.
- Hidden sampling or profile-selection factors may remain.

---

## 8. Not Claimed

- causal mechanism for ordering or kernel-class assignment
- deterministic rescue
- physical N-boundary or physical interpretation of N
- universal adaptive control
- V6 readiness
- modified M3++
- modified Stop-Low policy
- retuned c3OmegaShift threshold
- new model variables or correction classes
- physical theory interpretation
- physical length, c, GR, spacetime, or cosmology derivation

---

## 9. Claim Audit

| Forbidden Claim | Status |
|:----------------|:------|
| Causality | NOT CLAIMED |
| Physical interpretation | NOT CLAIMED |
| V6 readiness | NOT CLAIMED |
| Deterministic rescue | NOT CLAIMED |
| Threshold retuning | NOT CLAIMED |
| New variables | NOT CLAIMED |
| M3++ modification | NOT CLAIMED |
| Stop-Low modification | NOT CLAIMED |

**AUDIT PASSED.**

---

## 10. Suite Reference

| Suite | Tests | Status |
|:------|------:|:------:|
| SOO | 1 | COMPLETE |
| PWO | 1 | COMPLETE |
| FEG | 1 | COMPLETE |
| PSO | 1 | COMPLETE |
| ERO | 1 | COMPLETE |
| RFO | 1 | COMPLETE |
| TSS | — | THIS DOCUMENT |
| **Total** | **7** | **0 failed** |

Cumulative: 2890 tests, 0 failed.
