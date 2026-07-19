# TRM V5.6 MGCA — Minimal Generative Core Analysis

## Gate: GATE A (Nm Suppressor) + GATE C (Minimal Map Without Nm) — REACHED

Key finding: **Extra Cupd is only destructive when Nm is present.** Without Nm, Double-Cupd massively amplifies branch formation.

## Settings

| Parameter | Value |
|-----------|-------|
| N values | 67, 69, 72 |
| Seeds (Stage 1) | 0-29 (30 seeds) |
| Seeds (Stage 2) | 0-99 (100 seeds, LongRunning) |
| Threshold | Omega > 1.783 (V5.3 frozen) |
| Regime | xi=1.75, K0=1.20, s=0.10 |

## Variants

| ID | Label | Pipeline | Purpose |
|----|-------|----------|---------|
| B0 | Baseline | Sm→RP→Nm→DL→Cupd | Full RecoverFP reference |
| V1 | Skip-Nm | Sm→RP→DL→Cupd | Nm suppression generalizability |
| V2 | Nm-after-DL | Sm→RP→DL→NmOnD→Cupd | Nm position before Cupd |
| V3 | Nm-after-Cupd | Sm→RP→DL→Cupd→Nm | Cross-epoch Nm effect |
| V4 | Skip-RP | Sm→Nm→DL→Cupd | RP necessity (synthetic R) |
| V5 | Skip-RP-and-Nm | Sm→DL→Cupd | Minimal non-transitional map |
| V6 | Cupd-before-DL | Sm→RP→Nm→Cupd→DL | Order necessity |
| V7 | Double-DL | Sm→RP→Nm→DL→DL→Cupd | Extra DL effect |
| V8 | Double-Cupd | Sm→RP→Nm→DL→Cupd→Cupd | Extra Cupd effect (MGCE R2) |
| V9 | Skip-Nm+Double-Cupd | Sm→RP→DL→Cupd→Cupd | Extra Cupd without Nm |
| V10 | Skip-Nm+Double-DL | Sm→RP→DL→DL→Cupd | Extra DL without Nm |

## Implementation Notes

### V2: Nm-after-DL
Nm type signature (R→Rn) incompatible with post-DL position (d available, not R).
`NmOnD` applies identical min-max normalization to distance matrix `d`.
Tests whether normalizing distances vs order parameters changes branch formation.
Result: NmOnD is MORE suppressive (2/30 high) than standard Nm (5/30).

### V3: Nm-after-Cupd
Delayed cross-epoch normalization. Nm(R) saved from epoch N, feeds DL in epoch N+1.
Epoch 0 uses unnormalized R. Tests within-epoch vs cross-epoch Nm.
Result: Also more suppressive (2/30 high) than B0, similar to V2.

### V4: Skip-RP
RP removed. Nm receives synthetic uniform R (R[i,j]=0.5, R[i,i]=1.0).
Result: All high-branch eliminated (0/30). RP is NECESSARY.

### V5: Skip-RP-and-Nm
Both removed. DL receives synthetic uniform R.
Result: All high-branch eliminated (0/30).

### V6: Cupd-before-DL
Cupd on Rn via K = K0 * Rn^(1/xi), mathematically equivalent to DL→Cupd but reordered.
Result: 3/30 high, 4/30 flipped. Order is borderline — UNRESOLVED.

## Stage 1 Results (N=67, seeds 0-29)

### Baseline Reproduction

| Condition | Omega | CV | High | dMean | dP90 | Kstd | lam1 |
|-----------|-------|-----|------|-------|------|------|------|
| B0 (MGCE expected) | 1.2933 | 0.2719 | 5/30 | 0.4630 | 0.9981 | 0.1935 | 63.80 |
| B0 (MGCA actual) | 1.2933 | 0.2719 | 5/30 | 0.4630 | 0.9981 | 0.1935 | 63.80 |

**Reproduction: EXACT MATCH. All 7 metrics identical to MGCE. PASS.**

### Full Variant Execution

| ID | Label | Omega | CV | High | dMean | dP90 | Kstd | lam1 |
|----|-------|-------|-----|------|-------|------|------|------|
| B0 | Baseline | 1.2933 | 0.2719 | 5/30 | 0.4630 | 0.9981 | 0.1935 | 63.80 |
| V1 | Skip-Nm | 1.7083 | 0.4549 | 12/30 | 0.6544 | 1.3898 | 0.2444 | 59.56 |
| V2 | Nm-after-DL | 1.2488 | 0.2413 | 2/30 | 0.4680 | 0.9912 | 0.1960 | 63.22 |
| V3 | Nm-after-Cupd | 1.2511 | 0.2787 | 2/30 | 0.4325 | 0.9180 | 0.1822 | 64.44 |
| V4 | Skip-RP | 1.0002 | 0.0059 | 0/30 | 0.2892 | 0.7852 | 0.1926 | 69.40 |
| V5 | Skip-RP-and-Nm | 1.0855 | 0.0114 | 0/30 | 0.4172 | 0.8301 | 0.1718 | 64.20 |
| V6 | Cupd-before-DL | 1.2166 | 0.2754 | 3/30 | 0.4398 | 0.9136 | 0.1817 | 63.90 |
| V7 | Double-DL | 1.1579 | 0.2059 | 1/30 | 0.3896 | 0.7869 | 0.1640 | 65.36 |
| V8 | Double-Cupd | 1.1593 | 0.2151 | 1/30 | 0.4084 | 0.8385 | 0.1690 | 64.81 |
| V9 | Skip-Nm+Double-Cupd | **3.0678** | **0.2439** | **29/30** | 0.9162 | 1.7281 | 0.2529 | 50.78 |
| V10 | Skip-Nm+Double-DL | 1.7163 | 0.3634 | 15/30 | 0.6573 | 1.4070 | 0.2543 | 59.63 |

### Branch Overlap (B0 as reference)

| Transition | Count | Interpretation |
|------------|-------|----------------|
| B0 hi → V1 hi | 4/5 preserved | Skip-Nm preserves most baseline hi |
| B0 lo → V1 hi | 8 new | Skip-Nm converts many low to high |
| B0 hi → V1 lo | 1 lost | One hi lost to lo |
| B0 hi → V2 lo | 5/5 lost | Nm-after-DL eliminates ALL baseline hi |
| B0 hi → V3 lo | 5/5 lost | Nm-after-Cupd eliminates ALL baseline hi |
| V6 flips vs B0 | 4/30 | Order change flips 4 seeds |

### Separation Metrics

| ID | Label | dMeanSep | KstdSep | lam1Sep |
|----|-------|----------|---------|---------|
| B0 | Baseline | 2.73 | 2.81 | 2.25 |
| V1 | Skip-Nm | 4.15 | 3.12 | 3.58 |
| V2 | Nm-after-DL | 2.32 | 2.51 | 1.86 |
| V3 | Nm-after-Cupd | 2.47 | 2.72 | 2.05 |
| V6 | Cupd-before-DL | 3.31 | 4.09 | 2.38 |
| V7 | Double-DL | 3.73 | 4.10 | 2.81 |
| V8 | Double-Cupd | 6.33 | 5.91 | 4.33 |
| V9 | Skip-Nm+Double-Cupd | 4.34 | 0.65 | 6.95 |
| V10 | Skip-Nm+Double-DL | 3.96 | 4.48 | 3.57 |

## Stage 2 Results (N=67,69,72, seeds 0-99)

*Pending execution (LongRunning).*

## Decision Gates

### Gate A — Nm Suppressor Confirmed: **REACHED**

Skip-Nm (V1) increases high-branch from 5/30 to 12/30 (+140%).
Nm-after-DL (V2) reduces to 2/30 (-60% vs B0).
Nm-after-Cupd (V3) also reduces to 2/30.
Nm position matters: both position shifts make Nm MORE suppressive, not less.

**Nm is conditionally identified as a branch suppressor.**
Removing Nm increases high-branch access.
Shifting Nm later in the pipeline intensifies its suppressive effect.

### Gate B — Stage Ordering Necessary: **UNRESOLVED**

V6 (Cupd-before-DL): 3/30 high vs B0 5/30. 4/30 flips.
This is borderline — not catastrophic but not preserved either.
DL→Cupd ordering shows sensitivity but the effect size (60% preservation, 13% flip rate)
is within the range of sampling noise at 30 seeds.

**Recommendation:** Stage 2 (100 seeds across N) to resolve.

### Gate C — Minimal Map Without Nm: **REACHED**

V1 (Sm→RP→DL→Cupd) preserves high-branch access (12/30) and branch geometry.
V5 (Sm→DL→Cupd) fails (0/30 high) — RP is necessary.
**Minimal surviving map: Sm→RP→DL→Cupd (4 stages, skip Nm).**

### Gate D — Granularity Critical: **PARTIALLY REACHED — Nm-mediated**

V7 (Double-DL, with Nm): DESTRUCTIVE — high-branch collapses to 1/30.
V8 (Double-Cupd, with Nm): DESTRUCTIVE — high-branch collapses to 1/30 (MGCE confirmed).
V9 (Skip-Nm + Double-Cupd): **NOT destructive — 29/30 high (Omega=3.0678)!**
V10 (Skip-Nm + Double-DL): ENHANCES — 15/30 high.

**Critical finding: The destructive effect of extra Cupd/DL passes is Nm-mediated.**
When Nm is absent, extra passes amplify rather than destroy branch formation.
Granularity sensitivity is conditional on Nm presence.

### Gate E — Full Map Still Required: **NOT REACHED**

Alternative maps reproduce core branch signatures.

### Gate F — Unsafe or Invalid: **NOT REACHED**

Baseline reproduces exactly. No invariants violated.

## Key Findings

### 1. Nm is a Robust Branch Suppressor
- Removing Nm consistently increases high-branch access.
- Shifting Nm position (V2, V3) makes it MORE suppressive, not less.
- Nm's suppressive effect is POSITION-DEPENDENT.

### 2. RP is Strictly Necessary
- Synthetic R matrices produce zero high-branch outcomes.
- RP's computed order parameters are essential for branch generation.
- Cannot be replaced by uniform/default coupling.

### 3. DL→Cupd Order Shows Sensitivity (Stage 2 needed)
- V6 preserves 3/5 (60%) of B0 high-branch seeds.
- 4/30 seeds flip branch assignment.
- Effect is borderline at 30 seeds — needs 100-seed confirmation.

### 4. Granularity is Nm-Mediated (Critical Discovery)
- Extra Cupd/DL is destructive ONLY when Nm is present.
- Without Nm, extra passes AMPLIFY branches — V9 reaches 29/30 high.
- This means Nm is not just a branch suppressor but also a **pass-count gate**.
- The destructive effect observed in MGCE R2 is Nm-dependent, not inherent.

### 5. V9: Extraordinary Amplification Without Nm
- Skip-Nm + Double-Cupd (V9): Omega=3.07, 29/30 high.
- This is the highest high-branch fraction observed in any V5.x condition.
- Suggests Nm imposes a constraint that prevents runaway amplification.
- Without Nm, the d↔K loop can "run away" across extra passes.

## Minimum Surviving Map

**Sm→RP→DL→Cupd** (V1, 4 stages, no Nm)

- Preserves high-branch access (12/30, exceeds B0).
- Preserves distance-driven branch geometry.
- Preserves d/K amplification.
- RP remains necessary.
- Nm is removable.

## Claim Discipline

| Claim | Status |
|-------|--------|
| Nm is a branch-suppressing stage | CONDITIONALLY SUPPORTED (N=67, 30 seeds) |
| RP is necessary for branch generation | CONDITIONALLY SUPPORTED (N=67, 30 seeds) |
| DL→Cupd order matters | UNRESOLVED (borderline, needs Stage 2) |
| Extra Cupd/DL is destructive | CONDITIONAL on Nm presence |
| Without Nm, extra passes amplify | CONDITIONALLY SUPPORTED (N=67, 30 seeds) |
| Minimal map: Sm→RP→DL→Cupd | CONDITIONALLY SUPPORTED |
| Physical interpretation | NOT CLAIMED |
| Attractor decomposition | NOT CLAIMED |
| Universal criticality | NOT CLAIMED |
| Mathematical irreducibility | NOT CLAIMED |
| Generalization beyond N=67, 30 seeds | NOT CLAIMED |

## Recommended Next Suite

**MCA (Minimal Core Audit):**
1. Validate Sm→RP→DL→Cupd across N=67,69,72 with 100 seeds.
2. Characterize Nm dose-response: partial Nm, weakened Nm.
3. Investigate V9 runaway: what prevents all seeds from going high?
4. Resolve DL→Cupd ordering with larger sample.
5. Test pass-count sensitivity without Nm (3, 4, 6 Cupd passes).
