# V5.45 — C3 Response Autonomy and N-Dependent Bridge

**Version:** INITIALIZED | **Date:** 2026-07-19
**Branch:** `feature/v5.45-c3-response-autonomy-and-n-dependent-bridge`
**Base:** V5.44 COMPLETE

## Purpose

V5.44 identified c3ExitOm2 as the C3 microstate separator but found the entry→exit
bridge is N-dependent (std=0.287) and 67% of C3 response is autonomous. V5.45
investigates what controls this autonomy and N-dependence.

## Central Question

**What controls the autonomous 67% of C3 Omega response, and why is the**
**entry→exit bridge N-dependent?**

## Planned Suites

CAP, CAE, CAA, CAI, CAS.

## Core Questions

1. Why does c3EntryOm explain c3ExitOm2 strongly in some N but weakly in others?
2. What distinguishes high-bridge N from low-bridge N?
3. What controls the autonomous 67% of C3 response?
4. Can C3 autonomy be reduced by additional diagnostic instrumentation?
5. Does this improve causal closure?
6. Does V6 remain NOT READY?

## Frozen Policy

M3++, Stop-Low, c3OmgS threshold frozen. No new controls/selectors/corrections.
All new quantities = diagnostic trace only. V6 NOT READY.
