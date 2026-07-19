# V5.31 Policy Limit Mapping and Failure Discovery

**Status:** INITIALIZED | **Date:** 2026-07-19
**Branch:** `feature/v5.31-policy-limit-mapping-and-failure-discovery`
**Base:** V5.30 COMPLETE

## Purpose

Actively search for rare failure regimes of Stop-Low. Where does it fail?

## Core Questions

1. Can a low-stratum rescue be forced?
2. What is the smallest positive safety gap observed?
3. Which N values are closest to failure?
4. Does the margin continue to shrink under scale?
5. Is Stop-Low robust because of mechanism or because failures are rare?

## Planned Suites

| Suite | Name | Purpose |
|:------|:-----|:--------|
| PLP | Policy Limit Protocol | Define failure search rules |
| PLE | Limit Execution | Stress-test at limits |
| PLA | Failure Analysis | Analyze failures |
| PLI | Independent Failure Audit | Verify findings |
| PLS | Final Synthesis | Conclude V5.31 |

## Constraints

M3++ frozen. Threshold 0.1 frozen. Stop-Low frozen.
Search for failures. Do not create them artificially.
