# TRM V5.55 Final Synthesis — Residual Selection Preference

**Suite ID:** TSS_01_V5_55_FinalSynthesis
**Version:** 1.0
**Date:** 2026-07-21
**Branch:** `feature/v5.55-residual-selection-preference`
**Status:** COMPLETE

---

## Part A — Executive Determination

### 1. What V5.55 Asked

**Why is SAC sensitive to the 6.4% residual component rather than the dominant 93.6% seed component?**

V5.54 established a two-level rawIQR structure: between-seed (93.6% variance, SAC-irrelevant) and within-seed residual (6.4%, SAC-relevant). V5.55 asked: what makes the residual component SAC-relevant, and is it absolute or relative?

### 2. How the Frontier Evolved

V5.55 traced the discriminator from absolute spread → residual spread → within-seed rank → rank-residual decoupling. Each suite narrowed the signal:
- **RSP**: seed+residual both contribute; residual is 1.8× more efficient
- **RSR**: sign reversal confirmed — pooled P1>P1b, residual P1<P1b
- **RRA**: within-seed rank dominates (1.27σ vs 0.81σ residual vs 0.32σ absolute)
- **RRC**: rank and residual survive mutual controls — both carry independent information
- **RCC**: composite adds no value; rank is 95× stronger in raw units
- **RRD**: SAC selects where rank-residual correlation breaks (Pearson 0.70→0.00)
- **RDC**: most-coupled profiles 0% P1; most-decoupled 83% P1
- **RGM**: rank 0.41σ, residual 0.43σ; gate operates at SAC, not IsHi

### 3. Why Absolute rawIQR Became Insufficient

Absolute rawIQR captures mostly seed-level variance (93.6%), which does not propagate to P1 (seed→P1 r=0.044). The SAC-relevant information is concentrated in profile-local deviations from seed means.

### 4. Why Residual rawIQR Mattered

Residual rawIQR (6.4% variance) carries the P1/P1b signal (delta=0.00105). It survives seed matching and remains the dominant profile-local discriminator.

### 5. Why Rank Became Dominant

Within each seed with 3 profiles, rawIQR values are ordered deterministically by N (same Random sequence, different length). Within-seed rank (0, 0.5, 1.0) captures the relative position. Rank effect = 1.27σ (strongest single descriptor across all suites).

### 6. Why Decoupling Was Tested

RRD_01 found that population Pearson(rank,residual)=0.70 but SAC-retained Pearson=0.00. Decoupling — the deviation from expected residual given rank — was tested as a candidate gate mechanism. It works (0% P1 in most-coupled, 83% in most-decoupled) but is weaker than rank alone (0.21σ vs 0.41σ).

### 7. Why SAC-Local Gating Matters

RGM_01 localized the gate: Pre-selection decoupling≈0, IsHi is decoupling-neutral, SAC creates the separation. The discriminator is not a property of the profile generation — it emerges at the SAC gate itself.

### 8. Why V6 Remains NOT READY

No causal mechanism for SAC's rank sensitivity. No physical interpretation. No deterministic rescue. V6 requires causal closure, which remains blocked.

---

## Part B — Final Model

### Model A+: Rank-Primary, Residual-Secondary SAC Discriminator

```
Seed realization → profile pool
        ↓
Within-seed relative rank (primary: 0.41σ)
        ↓
Residual rawIQR adjustment (secondary: 0.43σ)
        ↓
SAC gate (rank-residual decoupling emerges)
        ↓
P1/P1b assignment
        ↓
Ordering propagation (K1>K3>K2)
```

| Layer | Descriptor | Effect (σ) | Role |
|:------|:-----------|-----------:|:-----|
| Primary | Within-seed rank | 0.41 | Dominant discriminator |
| Secondary | Residual rawIQR | 0.43 | Independent complement |
| Composite | Rank + residual | no gain | Rank saturation |
| Gate | Decoupling score | 0.21 | Emerges at SAC |

**Key insight:** SAC discriminates on *relative* position within a seed, not absolute spread. The strongest signal is "where does this profile rank among its seed-mates?"

---

## Part C — Supported Findings

1. Absolute rawIQR (0.32σ) is weaker than relative rank (0.41σ) and residual (0.43σ).
2. Residual rawIQR survives seed matching (RLP_01).
3. Within-seed rank is the strongest observed discriminator (RRA_01: 1.27σ on retained subset).
4. Rank and residual survive mutual controls — both carry independent information (RRC_01).
5. Composite adds no measurable improvement — rank saturation (RCC_01).
6. Population rank-residual coupling exists: Pearson=0.70 (RRD_01).
7. SAC-retained profiles break that coupling: Pearson=0.00 (RRD_01).
8. Most-coupled profiles: 0% P1; most-decoupled: 83% P1 (RDC_01).
9. SAC gate is localized — IsHi is decoupling-neutral (RGM_01).
10. P1 concentrated in lowest rank quartile (RRA_01: 0-25% rank = 100% P1).
11. Stop-Low remains safe. V6 NOT READY.

---

## Part D — Conditional Findings

- finite-N limits (N=70,72,75)
- sparse SAC retention (15 P1, 3 P1b)
- profile-count limits (one profile per seed×N cell)
- seed-reuse structure creates inherent cross-N coupling
- diagnostic not causal
- hidden SAC descriptor may remain

---

## Part E — Hypotheses

- SAC may operate primarily on relative position within seed.
- Residual spread may act as secondary refinement to rank.
- Decoupling may be a byproduct of SAC selection, not its mechanism.
- The rank signal may be a proxy for a deeper profile-shape descriptor.
- Additional latent descriptors beyond rank+residual may remain.

---

## Part F — Not Claimed

- Causal mechanism for SAC rank sensitivity
- Deterministic rescue from rank/residual signals
- Physical interpretation of within-seed rank
- Physical N-boundary
- Universal adaptive control
- V6 readiness
- Modified M3++
- Modified Stop-Low policy
- Retuned c3OmegaShift threshold
- New model variables or correction classes
- Physical c, G, GR, spacetime, cosmology, or length derivation

---

## Part G — V5.55 Lineage Entry

### V5.55 — Residual Selection Preference

**Status:** COMPLETE.

**Summary:** The strongest SAC discriminator is not absolute spread but relative position within a seed. Within-seed rank is the dominant signal (0.41σ in SAC-retained, 1.27σ in pooled). Residual rawIQR contributes independent secondary information (0.43σ). SAC-retained profiles break the normal rank-residual coupling (Pearson 0.70→0.00). The gate operates at SAC, not IsHi. Composite adds no improvement due to rank saturation within 3-profile seeds.

**Refinement over V5.54:** The SAC discriminator is fundamentally *relative* — it compares profiles to their seed-mates, not to absolute thresholds.

**Suites:** RSP_01, RSR_01, RRA_01, RRC_01, RCC_01, RRD_01, RDC_01, RGM_01, TSS_01
**Tests:** 9
**Cumulative:** 2916 passed, 0 failed

---

## Part H — Claim Audit

| Check | Status |
|:------|:------|
| Causal mechanism | NOT CLAIMED |
| Physical interpretation | NOT CLAIMED |
| V6 readiness | NOT CLAIMED |
| Deterministic rescue | NOT CLAIMED |
| Threshold retuning | NOT CLAIMED |
| M3++ modification | NOT CLAIMED |
| Stop-Low modification | NOT CLAIMED |

**AUDIT PASSED.**

---

## Part J — Commit-Ready Summary

```
TSS_01_V5_55_FinalSynthesis

V5.55 COMPLETE — Residual Selection Preference.

Final Model: A+ — Rank-primary, residual-secondary SAC discriminator.

SAC selects on relative position within a seed, not absolute spread.
Within-seed rank is the dominant signal, residual provides complement.
SAC-retained profiles break normal rank-residual coupling.
Gate operates at SAC, not IsHi. Composite adds no improvement.

Refines V5.54: discriminator is fundamentally RELATIVE.

Stop-Low safe. Causal closure blocked. V6 NOT READY.

Suites: RSP, RSR, RRA, RRC, RCC, RRD, RDC, RGM, TSS.
Cumulative: 2916 tests, 0 failed.
```

---

*Generated 2026-07-21. Authoritative V5.55 final synthesis.*
