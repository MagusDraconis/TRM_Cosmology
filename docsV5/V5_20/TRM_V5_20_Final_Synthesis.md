# TRM V5.20 Final Synthesis

**Branch:** feature/v5.20-adaptive-boundary-mechanism
**Status:** COMPLETE | **Date:** 2026-07-18 | **Base:** V5.19 (2661 tests)

## 1. Metadata
| Item | Value |
|------|-------|
| Branch | `feature/v5.20-adaptive-boundary-mechanism` |
| Suites | BMP(3), BME(1), BMA(1) (BMS = this doc) |
| Commits | c84abe0, 24fa2c5, 7b199be |
| V5.20 tests | 5 | Cumulative: 2666 | Failed: 0 |

## 2. Question
**Why does M3++ open at N=65, peak at N=72, and saturate at N=80?** Mixed mechanism.

## 3. Suites
**BMP:** Protocol. **BME:** Diagnostic sweep (8 N, distHi/dPre/RebMag). **BMA:** 6-driver ranking.

## 4. Findings
- Boundary is not single-variable
- Low-N: rescue-immune (candidates exist, A0=0%)
- Onset N=65: first inducible seeds (A0 0%→14%, distHi drops)
- Peak N=72: rebMag(0.33) + orthHiVec + closest distHi(0.21)
- Upper: A0=100% at N=75+, no rescue opportunity

## 5. Driver Ranking
1. Inducibility — prerequisite
2. Rescue opportunity — A0 failures C3 can fix
3. Basin proximity — distHi drops at onset
4. rebMagnitude — peaks at N=72
5. orthHiVec — N=72 unique pre-filter
6. Static saturation — A0 ceiling

## 6. Boundary Table
| Boundary | N | Driver | Evidence |
|----------|---|--------|----------|
| Lower | 64→65 | Inducibility | A0 0%→14%, distHi drops |
| Peak | 72 | rebMag+orthHiVec+distHi | Three factors converge |
| Upper | 75→80 | Saturation | A0=100% |

## 7–10. Model, Weakened, Not Claimed, Conclusion
V5.20 converts V5.19 boundary map into mechanism model. Mixed: lower=inducibility, window=rescue+response, upper=saturation. M3++ remains bounded, not universal.

## 11. V5.21: Low-N Rescue Immunity
**Branch:** `feature/v5.21-low-n-rescue-immunity-and-basin-access`
**Question:** Can rescue-immune N<65 candidates be made inducible?
