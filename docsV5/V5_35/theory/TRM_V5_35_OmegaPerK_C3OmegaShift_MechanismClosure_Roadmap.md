# V5.35 omegaPerK and c3OmegaShift Mechanism Closure — Roadmap

**Version:** 1.0 | **Date:** 2026-07-19

## Background

V5.31-V5.33 established that omegaPerK separates rescued from stopped profiles
by a factor of 58-76x, and that c3OmegaShift > 0.1 is the minimal robust
predictive summary. But WHY these separations exist is unknown.

## Research Plan

1. Decompose omegaPerK into its constituent factors
2. Identify which upstream variables predict omegaPerK
3. Test whether c3OmegaShift is a sufficient summary of upstream effects
4. Determine whether the chain is causal or merely predictive

## Expected Outcome

V5.35 should either close the explanatory gap or classify which layers
are diagnostic rather than causal.

## Constraints

M3++ frozen. Stop-Low frozen. No new variables. No V6 derivations.
