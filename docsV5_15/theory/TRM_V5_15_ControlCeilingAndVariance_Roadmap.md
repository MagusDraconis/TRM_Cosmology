# TRM V5.15 Control Ceiling and Unexplained Variance — Roadmap

**Version:** 1.0 | **Date:** 2026-07-18 | **Base:** V5.14 M3+

## 1. Research Question

How much unexplained persistence variance remains after M3+, and is it structured or effectively random?

## 2. Motivation

V5.14 found one residual degree of freedom (orthHiVec at N=72). M3+ holdout rates: N=71: 62%, N=72: 78%, N=75: 75%. Remaining unexplained: ~38% at N=71, ~22% at N=72, ~25% at N=75. V5.15 asks whether this residual variance has structure or represents the noise floor.

## 3. Core Questions

1. What percentage of outcomes remain unexplained by M3+?
2. Are remaining failures structured or random?
3. Has the practical ceiling been reached?
4. What is the maximum realistic holdout performance?

## 4. Suite Plan

| Suite | Phase |
|-------|-------|
| CVP | Protocol — variance metrics, ceiling criteria |
| CVE | Execution — M3+ outcome collection |
| CVA | Analysis — structure tests, failure classification |
| CVI | Limit Audit — ceiling determination |
| CVS | Synthesis — final model or ceiling declaration |
