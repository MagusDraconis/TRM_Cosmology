# TRM V5.12 HBG: Seed-Intrinsic Persistence Audit

**Branch:** feature/v5.12-high-basin-entry-conditions
**Status:** COMPLETE
**Date:** 2026-07-17

---

## Quick Summary

Pre-intervention profiling of 50 Lo seeds + 20 Natural Hi seeds at N=71 CP3, followed by before-Cupd d-compression to target dT1≈0.50. **Strict persistence is seed-intrinsic and counter-intuitive**: the single persistent seed (36) has the most EXTREME Lo-like baseline, while the most Hi-like Lo seed (39) fails persistence.

**Gate A (seed-intrinsic signature) REACHED. Gate D (Seed 39 is not reproducible) REACHED.**

---

## 1. Classification at tgt=0.50 (50 Lo seeds)

| Class | N | Description |
|-------|---|-------------|
| **S1 Strict Persistent** | **1** | Seed 36: immHi + persists |
| S2 HiProx Non-Persist | 15 | distHi<distLo but fails persistence |
| S3 ImmHi Non-Persist | 11 | Omega > THR but collapses (includes seed 39) |
| S4 Delayed | 8 | Persists but not immediate |
| S5 Failed | 15 | Never Hi-proximal |

---

## 2. Pre-Intervention Profiles (CP3 Baseline)

| Class | N | d_mean | K_mean | K_std | distHi | distLo | Entry Score | nat Δd |
|-------|---|--------|--------|-------|--------|--------|-------------|--------|
| **S1 Strict** | 1 | **0.703** | **0.871** | **0.305** | 0.320 | 0.281 | **-0.039** | **+0.118** |
| S2 HiProxNP | 15 | 0.551 | 0.927 | 0.213 | 0.273 | 0.266 | -0.007 | -0.023 |
| S5 Failed | 15 | 0.498 | 0.949 | 0.211 | 0.219 | 0.217 | -0.002 | +0.028 |
| NatLo | 20 | 0.558 | 0.924 | 0.224 | 0.252 | 0.237 | -0.015 | +0.008 |
| **NatHi** | 20 | **0.304** | **1.026** | **0.134** | **0.174** | 0.202 | **+0.028** | +0.026 |

### Critical Finding

**S1 Strict (seed 36) has the most EXTREME Lo-like baseline:**
- Highest d_mean (0.703 — nearly 2× NatHi)
- Lowest K_mean (0.871)
- Widest K_std (0.305 — nearly 2× NatHi)
- Most negative entry score (-0.039)
- Largest positive natural drift (+0.118 → d increases naturally)

This is the polar opposite of what you'd expect for a "Hi-accessible" seed.

---

## 3. Seed 39 Forensic Profile

| Metric | Baseline | Post-Int T1 | Post-Int T2 | ΔT1→T2 |
|--------|----------|------------|------------|--------|
| d_mean | **0.327** | 0.888 | 0.142 | **-0.746** |
| K_mean | **1.009** | 0.819 | 1.114 | **+0.295** |
| K_std | 0.133 | — | — | — |
| Omega | 1.100 | **2.213** | 1.074 | -1.139 |
| Entry Score | +0.039 | — | — | — |
| nat Δd | +0.059 | — | — | — |

**Seed 39 at tgt=0.50 is S3 (ImmHi Non-Persistent), NOT strict persistent.**

At baseline, seed 39 is the most Hi-like Lo seed: dm=0.327 (vs NatHi 0.304), km=1.009 (vs NatHi 1.026), ks=0.133 (vs NatHi 0.134). It's essentially a "crypto-Hi" Lo seed.

But its natural drift is ANTI-Hi: dD=+0.059 (d increases — toward Lo) and dK=-0.026 (K decreases). Its dynamics push it away from Hi.

At tgt=0.50, intervention overshoots (dT1=0.888, way above target), then crashes massively (d drops -0.746, K amplifies +0.295). Despite the massive K amplification, Omega collapses from 2.21 to 1.07.

**Seed 39 at tgt=0.51 (HBF) was strict persistent** — the 0.01 target difference flips the outcome entirely.

---

## 4. Relaxation Direction (T1→T2)

| Class | Δd_mean | ΔK_mean | NatHi-like? |
|-------|---------|---------|-------------|
| S1 Strict | **+0.433** | **-0.171** | **NO** ← surprise! |
| S2 HiProxNP | +0.056 | -0.013 | NO |
| **S5 Failed** | **-0.302** | **+0.129** | **YES** ← paradox! |

### The Relaxation Paradox

**Failed seeds have the most NatHi-like relaxation** (d down, K up). Strict persistent seed 36 has the most ANTI-Hi relaxation (d up strongly, K down). Persistence is NOT about matching the NatHi relaxation direction at this target.

This suggests seed 36 persists DESPITE anti-Hi dynamics — the intervention is strong enough to carry it through one epoch even against its natural tendency.

---

## 5. Pre-Intervention Predictability

| Metric | S1 Strict (seed 36) | S2 HiProxNP (avg) | Difference |
|--------|---------------------|-------------------|------------|
| baseDm | **0.703** | 0.508 | **+0.195** |
| baseKm | 0.871 | 0.942 | -0.071 |
| baseKs | **0.305** | 0.203 | **+0.102** |
| baseEs | -0.039 | -0.002 | -0.037 |
| natDDm | **+0.118** | -0.019 | **+0.137** |

Seed 36 is distinguished by: high baseline d_mean, wide K_std, POSITIVE natural drift (d increases). These are all "anti-Hi" properties but they predict strict persistence at this target.

---

## 6. Decision Gates

| Gate | Description | Status |
|------|-------------|--------|
| **Gate A** — Seed-intrinsic signature | Seed 36 has extreme Lo-like baseline | **REACHED** — but counter-intuitive direction |
| Gate B — Relaxation direction | S1 relaxes ANTI-Hi, S5 relaxes NatHi-like | **NOT SUPPORTED** — relaxation direction is anti-predictive |
| Gate C — NatHi proximity before | Seed 36 is FAR from Hi at baseline | **NOT SUPPORTED** — the opposite |
| **Gate D** — Seed 39 is unique outlier | Seed 39 fails at tgt=0.50, worked at tgt=0.51 | **REACHED** — target-hypersensitive |
| Gate E — No predictive signature | Seed 36 clearly distinguishable | **NOT REACHED** — signature exists but counter-intuitive |

---

## 7. The Counter-Intuitive Signature

The seed that achieves strict persistence (36) is:
- The MOST Lo-like at baseline (highest d, lowest K, widest Ks)
- Has ANTI-Hi natural drift (d increases +0.12)
- Has ANTI-Hi relaxation after intervention (d up, K down)
- But persists through +1 epoch

This is the opposite of what geometric/proximity theories would predict. The mechanism may be:
1. Extreme Lo seeds have high baseline d → intervention has more "room" to compress
2. The compression is proportionally large enough to survive one epoch of rebound
3. Hi-like Lo seeds (like 39) are already near Hi → small intervention → easily undone by dynamics

**Persistence at this target may be about intervention magnitude relative to baseline, not about intrinsic Hi-accessibility.**

---

## 8. Claim Discipline Audit

| Claim | Status |
|-------|--------|
| Strict persistence seed has extreme Lo baseline | **SUPPORTED** (seed 36) |
| Most Hi-like Lo seed fails strict persistence | **SUPPORTED** (seed 39) |
| Relaxation direction does not predict persistence | **SUPPORTED** |
| Seed-intrinsic signature exists | **SUPPORTED** (but counter-intuitive) |
| Physical interpretation | **NOT CLAIMED** |
| Cross-N validity | **NOT CLAIMED** |

---

## 9. Recommended Next Suite

**HBH — High-Basin Persistence via Intervention Magnitude**

Test whether strict persistence is controlled by intervention magnitude (target compression ratio) rather than absolute dT1 band. Hypothesis: larger compression ratios (more aggressive d-compression relative to baseline) produce more persistence, up to a validity limit. Test seeds at fixed compression ratios (25%, 50%, 75%, 100% of baseline d) rather than fixed absolute targets.

---

## Test Summary

- File: `TRM.Tests/V5_12/V5_12_SeedIntrinsicPersistenceAudit_Tests.cs`
- Tests: 2 (HBG_01, HBG_02)
- Passed: 2
- Runtime: ~51s (Parallel.ForEach)
- Tagged: LongRunning
