# TRM V5.10 — Branch Control Asymmetry and Induction

**Branch:** `feature/v5.10-branch-control-asymmetry-and-induction`  
**Date:** 2026-07-17  
**Base:** V5.9 COMPLETE (60d71c2)  
**Status:** INITIALIZED  
**Type:** Control asymmetry investigation

---

## Purpose

V5.9 discovered directional commitment: at epoch 5, Hi→Lo suppression works (14–31%) but Lo→Hi induction fails (0%). V5.10 asks: **why this asymmetry?**

---

## Core Question

**Why can high branches be suppressed at CP5, but low branches cannot be induced high?**

---

## Hypotheses

1. **Basin asymmetry:** High→Low is down-gradient in state space; Low→High is up-gradient
2. **Intervention strength:** Current d_mean increase is too weak for induction
3. **Operator mismatch:** Suppression uses d_mean, induction needs d_mean decrease or K boost
4. **Structural lock-in:** Low-branch K-state is a stable attractor basin
5. **N-conditioning:** Asymmetry varies with N

---

## Planned Suites

| Suite | Purpose |
|-------|---------|
| CAP | Protocol: define control asymmetry problem, intervention classes |
| CAE | Execution: test Lo→Hi induction with multiple intervention types |
| CAA | Analysis: characterize asymmetry mechanism |
| CAI | Induction audit: stronger interventions, stage re-entry |
| CAS | Synthesis |
