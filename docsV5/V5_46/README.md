# V5.46 — Entry-State Distribution Shape and N-Window Origin

**Version:** INITIALIZED | **Date:** 2026-07-19
**Branch:** `feature/v5.46-entry-state-distribution-shape-and-n-window-origin`
**Base:** V5.45 COMPLETE

## Purpose

V5.45 found that entry Omega distribution shape (IQR) controls the C3 bridge.
V5.46 investigates WHY some N windows produce broad bulk distributions while
others produce compressed ones.

## Central Question

**Why do some N windows produce broad c3EntryOm bulk distributions while others**
**produce compressed distributions?**

## Planned Suites

EDP, EDE, EDA, EDI, EDS.

## Core Questions

1. What determines c3EntryOm IQR / bulk spread?
2. Why N=72,75 broad while N=67,70 compressed?
3. Is distribution shape controlled by pre-C3 Omega trajectory, d/K state, lambda1, or N-window?
4. Does entry distribution shape explain rescue-active windows?
5. Does this improve causal closure?
6. Does V6 remain NOT READY?

## Frozen Policy

M3++, Stop-Low, c3OmgS threshold frozen. Diagnostic trace only. V6 NOT READY.
