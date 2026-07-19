# TRM V5.15: Control Ceiling and Unexplained Variance

**Branch:** feature/v5.15-control-ceiling-and-unexplained-variance
**Status:** INITIALIZED
**Date:** 2026-07-18
**Base:** V5.14 COMPLETE (M3+ model, 2615 tests, 0 failed)

## Quick Reference

| Item | Value |
|------|-------|
| Purpose | Quantify remaining unexplained persistence variance after M3+ and determine whether further improvement is possible without overfitting |
| Baseline model | M3+: P1/P1b + projHiVec + orthHiVec (N=72) |
| Primary N | 71, 72, 75 |
| Suites | CVP → CVE → CVA → CVI → CVS |
| Core question | How much variance remains? Is it structured or random? |

## Document Index

| Document | Path |
|----------|------|
| Experiment Log | `docsV5_15/experiments/TRM_V5_15_Experiment_Log.md` |
| Roadmap | `docsV5_15/theory/TRM_V5_15_ControlCeilingAndVariance_Roadmap.md` |
| Protocol | `docsV5_15/protocols/TRM_V5_15_ControlCeilingAndVariance_Protocol.md` |
| Tests | `TRM.Tests/V5_15/V5_15_ControlCeilingAndVarianceProtocol_Tests.cs` |

## Suite Map

| Suite | Phase | Purpose |
|-------|-------|---------|
| CVP | Protocol | Define variance metrics, ceiling criteria |
| CVE | Execution | Collect M3+ outcomes, quantify unexplained variance |
| CVA | Analysis | Test for structure in residual failures |
| CVI | Limit Audit | Determine if ceiling is final |
| CVS | Synthesis | Final ceiling declaration or residual model |
