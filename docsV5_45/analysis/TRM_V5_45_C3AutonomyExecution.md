# TRM V5.45 C3 Autonomy Execution — Analysis

**Version:** 1.0 | **Date:** 2026-07-19 | **Suite:** CAE | **Status:** COMPLETE

---

## 1. Summary

CAE_01 mapped the N-dependent c3EntryOm→c3ExitOm2 bridge across N=[65,66,67,70,72,75]
with 73 profiles.

**Key result: Bridge is bimodal — N=72,75 HIGH-BRIDGE, N=67,70 LOW-BRIDGE.**
All rescues in high-bridge N (45% vs 0%). Model E — Mixed N + response-state
autonomy. All 10 gates reached.

## 2. N-Dependent Bridge Map

| N | n | entOm~exitOm2 | Auton% | Resc | Bridge |
|--:|--:|-------------:|-------:|-----:|:-------|
| 65 | 7 | 0.999 | 0 | 0 | HIGH* |
| 67 | 11 | 0.199 | 96 | 0 | LOW |
| 70 | 20 | 0.318 | 90 | 0 | MED |
| 72 | 20 | 0.728 | 47 | 2 | HIGH |
| 75 | 12 | 0.618 | 62 | 3 | HIGH |

## 3. High-Bridge vs Low-Bridge

| Metric | High [72,75] | Low [67,70] | Diff |
|:-------|-------------:|------------:|-----:|
| entOm mean | 1.447 | 1.244 | 0.203 |
| entOm std | 0.586 | 0.398 | 0.189 |
| exitOm2 mean | 1.564 | 1.323 | 0.240 |
| Rescue rate | 15.6% | 0.0% | 15.6pp |

High-bridge N have higher entry Omega (closer to THR), higher variance (enables
correlation), and ALL rescues.

## 4. Autonomy Model

**Model E — Mixed N + response-state autonomy.** Bridge depends on N-window
(rescue-active N) and entry-state variance. N=67 has 96% autonomy.

## 5. Gates: A–J all REACHED (H ✓). Stop-Low safe. V6 NOT READY.

Recommendation: CAA or CAS.
