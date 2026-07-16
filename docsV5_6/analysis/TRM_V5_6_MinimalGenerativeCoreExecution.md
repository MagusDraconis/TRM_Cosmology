# TRM V5.6 MGCE — Minimal Generative Core Execution

## Gate: GATE A (weakened) — Nm normalization is NOT necessary

## Settings

| Parameter | Value |
|-----------|-------|
| N | 67 |
| Seeds | 0-29 |
| Threshold | Omega > 1.783 (V5.3 frozen) |
| Regime | xi=1.75, K0=1.20, s=0.10 |

## Results

| Condition | Omega_mean | CV | High | dMean | dP90 | Kstd | lam1 |
|-----------|--------|-----|------|-------|------|------|------|
| B0 baseline | 1.2933 | 0.2719 | 5/30 | 0.4630 | 0.9981 | 0.1935 | 63.80 |
| R1 skip-Nm | 1.7083 | 0.4549 | 12/30 | 0.0877 | 0.1619 | 0.0387 | 75.59 |
| R2 double-Cupd | 1.1593 | 0.2151 | 1/30 | 0.4084 | 0.8385 | 0.1690 | 64.81 |

## Branch Overlap

| Condition | Low | High | Overlap with B0 | B0 hi lost | B0 lo gained |
|-----------|-----|------|----------------|------------|--------------|
| B0 | 25 | 5 | — | — | — |
| R1 | 18 | 12 | 4/5 | 1 | 8 |
| R2 | 29 | 1 | 0/5 | 5 | 1 |

## Separation

| Condition | dMeanSep | KstdSep | lam1Sep |
|-----------|----------|---------|---------|
| B0 | 2.73 | 2.81 | 2.25 |
| R1 | 1.55 | 5.08 | 1.76 |

## Key Findings

### R1: Skip-Nm ENHANCES branches

- High-branch fraction DOUBLES: 5/30 to 12/30.
- 4/5 baseline high-branch seeds preserved.
- 8 new seeds flip from low to high.
- Omega mean rises (1.29 vs 1.71).
- d_mean drops 81% — distances shrink without normalization.
- K_std drops 80% — coupling becomes more uniform.
- lambda1(K) INCREASES (63.8 vs 75.6) — stronger leading eigenvalue.

**Nm normalization is NOT necessary. It acts as a BRANCH SUPPRESSOR.**

### R2: Double-Cupd DESTROYS branches

- High-branch collapses: 5/30 to 1/30.
- 5/5 baseline high-branch seeds lost.
- Extra d/K iteration within each epoch prevents branch emergence.

## Decision

**The full 5-stage map is partially reducible.** Nm can be removed without destroying branches — in fact, removal enhances them. However, extra d/K iterations destroy branches, suggesting the epoch structure is carefully balanced. The minimal core excludes Nm but retains the epoch count and Sm→RP→DL→Cupd sequence.

## Claim Discipline

| Claim | Status |
|-------|--------|
| Nm is not necessary for branch generation | CONDITIONALLY SUPPORTED |
| Skip-Nm enhances high-branch fraction | CONDITIONALLY SUPPORTED |
| Double-Cupd destroys branch formation | CONDITIONALLY SUPPORTED |
| Mathematical irreducibility | NOT CLAIMED |
| Physical interpretation | NOT CLAIMED |
| Generalization beyond N=67 | NOT CLAIMED |

## Recommended Next Suite

MGCA — test whether RP can also be removed (Sm→DL→Cupd), and whether stage ordering matters.
