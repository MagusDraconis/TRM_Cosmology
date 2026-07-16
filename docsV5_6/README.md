# TRM V5.6 — Minimal Generative Core

**Branch:** `feature/v5.6-recoverfp-minimal-generative-core`
**Status:** INITIALIZED
**Base:** V5.5 COMPLETE (2362 tests, 0 failed)
**Date:** 2026-07-16

## Purpose

Determine the minimal RecoverFP update-map conditions required to reproduce the finite-N branch split.

## Inherited Findings

| Version | Key Finding |
|---------|------------|
| V5.3 | Branch split exists at N≈66. Branch mixing explains Omega seed variability. |
| V5.4 | State space is moderate-dimensional, PC1-dominated, distance-driven. d_mean is strongest endpoint separator. |
| V5.5 | Branch generation requires full 5-epoch cycle. d/K amplify synchronously. Omega is downstream. Freeze-K collapses high branch. |

## Core Question

Is the full RecoverFP 5-stage map irreducible, or can it be reduced to a minimal d↔K amplification core?

## Minimal-Core Hypotheses

| ID | Hypothesis |
|----|-----------|
| MGC1 | Full RecoverFP map is irreducible |
| MGC2 | Reduced d↔K submap is sufficient |
| MGC3 | Epoch ordering is necessary |
| MGC4 | Five epochs necessary, internal stage detail simplifiable |
| MGC5 | Only synchronized d/K amplification must be preserved |

## Planned Suites

| Suite | Name | Purpose |
|-------|------|---------|
| MGCP | Protocol | Define tests, metrics, decision gates |
| MGCE | Reduced Map Execution | Test d↔K submap sufficiency |
| MGCA | Reorder/Simplify Audit | Test stage ordering and simplification |
| MGCS | Surrogate Amplifier Test | Test simplified amplifier surrogates |

## Decision Gates

| Gate | Condition | Interpretation |
|------|-----------|---------------|
| A | No reduced map reproduces branch split | Full map irreducible |
| B | Reduced d/K submap reproduces branches | d/K submap sufficient |
| C | Reordering destroys branches | Epoch ordering necessary |
| D | Amplification profile sufficient | Profile over detail |
| E | Surrogate reproduces branches | Mechanism reducible |
| F | Protocol unsafe or baseline fails | Stop, audit implementation |

## Claim Discipline

- No physical interpretation. No time/space/length/c claims.
- No attractor decomposition. No universal criticality.
- "Conditionally associated" only. No causation without intervention.
- Do not generalize beyond tested N and seeds.
