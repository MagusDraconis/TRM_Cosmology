# V5.21 Final Synthesis — Low-N Rescue Immunity and Basin Access

**Branch:** feature/v5.21-low-n-rescue-immunity-and-basin-access
**Date:** 2026-07-18
**Status:** COMPLETE
**Base:** V5.20 COMPLETE

---

## 1. Branch Metadata

| Item | Value |
|------|-------|
| Branch | `feature/v5.21-low-n-rescue-immunity-and-basin-access` |
| Suites | LRP, LRE, LRA, LRI |
| V5.21 Tests | 19 (3 LRP + 5 LRE + 6 LRA + 5 LRI) |
| Cumulative Tests | 2688 |
| Failed | 0 |
| M3++ Model | Frozen (V5.20 logic, no modification) |
| Branch Threshold | Omega > 1.783 (V5.3 frozen) |

---

## 2. Research Question

**Can rescue-immune N<65 candidates be made inducible by improving High-basin access, or is the lower boundary robust under current operator classes?**

---

## 3. Suite Summaries

### LRP — Low-N Rescue Immunity Protocol
**Status:** COMPLETE (3 tests)
Protocol definition establishing claim discipline, intervention families, and gate definitions.

### LRE — Low-N Rescue Execution
**Status:** COMPLETE (5 tests, 741 intervention results)

**Findings:**
- N=50-64 rescue immunity CONFIRMED under frozen M3++
- N=65 onset CONFIRMED (14% A0, 29% A1, 1 rescue)
- 13 intervention variants tested across 10 N values
- Zero low-N strict persistent induction
- 36/36 operator-class boundaries intact
- Gate C (Low-N immune) REACHED
- Gate H (Unsafe/invalid at N<64) REACHED

### LRA — Low-N Rescue Analysis
**Status:** COMPLETE (6 tests)

**Findings — Three-Phase Lower Boundary Mechanism:**

**Phase 1 — Structural Absence (N=50-63):**
- Natural High branch is empty (no seeds with Omega > 1.783)
- PCent fails to compute Hi centroid → distHi = NaN → projHiVec = 0
- All candidates are exactly orthogonal (offVecAngle = π/2)
- Off-vector dominates 96-100% of failures
- No intervention can fix: target basin doesn't exist

**Phase 2 — Centroid Present, C3 Weak (N=64):**
- Natural High centroid first appears (distHi finite)
- projHiVec becomes nonzero (-0.200), offVecAngle drops to 18°
- But C3 omega shift = 0.009 (negligible correction effect)
- Candidates can see the target but can't reach it
- 0% induction under all interventions

**Phase 3 — C3 Effective (N=65+):**
- C3 omega shift jumps 15.8x (0.009 → 0.134)
- deltaAlign jumps 2.17x (0.007 → 0.016)
- First rescued seeds appear (14%)
- Rescued seeds have negative alignPre (-0.55) — they need active correction

### LRI — Transferred Entry Reference Audit
**Status:** COMPLETE (5 tests, 864 transferred interventions)

**Hypothesis tested:** Is low-N immunity caused by missing local High reference?
**Result:** **FALSIFIED**

**Findings:**
- Transferred references from R65, R70, R72, R4-avg: 0% induction across all
- Reference profiles successfully redirect response (off-vector drops to near-zero)
- But direction change does not translate to Omega increase
- N=64: dPre +50%, but omT2 +0.1%
- Failure shifts from "off-vector" to "insufficient displacement" (50%)
- Structural d-space gap: low-N at d≈0.3-0.45, references at d≈0.6-1.0
- Gate C (Low-N immune under transferred refs) REACHED

---

## 4. Supported Findings

| Finding | Evidence |
|---------|----------|
| N<65 remains rescue-immune under all tested operator classes | LRE 741 results, LRI 864 results |
| Low-N failure is NOT candidate scarcity | Candidates exist and are selected by M3++ |
| N=50-63 lack a local Natural High target | Hi centroid = NaN, projHiVec = 0 |
| N=64 first shows local High reference but remains non-inducible | distHi finite, C3 omega shift = 0.009 |
| N=65 is the first inducible onset | 14% A0, 29% A1, first C3 rescue |
| Transferred High references do not open N<65 | LRI falsification of missing-reference hypothesis |
| Direction correction alone is insufficient | dPre +50% but omT2 +0.1% at N=64 |
| Current M3++ lower boundary remains robust | 36/36 operator-class boundaries intact |

---

## 5. Weakened Findings

| Previous hypothesis | Status after V5.21 |
|---------------------|-------------------|
| Missing local High reference as primary explanation | FALSIFIED by LRI |
| Direction correction as sufficient for rescue | FALSIFIED by LRI |
| Transferable High-entry reference as sufficient | FALSIFIED by LRI |
| N=64 as near-break | NOT REACHED (Gate B) |
| Low-N boundary as merely operator-strength limited | FALSIFIED — structural gap |

---

## 6. Not Claimed

V5.21 does NOT claim:
- Low-N induction is impossible under all future operators
- Physical criticality of the N=65 boundary
- Universal adaptive control
- Proof of basin topology
- Validity outside tested N (50-72) and intervention families
- Permanent impossibility of opening N<65

---

## 7. Final V5.21 Conclusion

V5.21 tested whether the low-N adaptive boundary could be opened by improving High-basin access.

**It could not.**

Local interventions (I1 basin push, I2 entry-vector amplification, I3 d-band targeting, I4 K-preserve, I5 rebound dampening, I6 full package), and transferred High-entry references (R65, R70, R72, R4-avg) all failed to produce strict persistent High-basin access below N=65.

The lower boundary mechanism has three phases:
1. **N=50-63:** Structural absence of the Natural High branch — the target basin doesn't exist
2. **N=64:** High centroid appears but C3 correction is too weak to bridge the gap
3. **N=65+:** C3 correction becomes effective, enabling first rescue

**V5.21 supports the conclusion that N<65 remains rescue-immune under current operator classes, and that the N=65 onset is a robust inducibility boundary in the tested RecoverFP regime.**

---

## 8. Recommended V5.22

**Branch:** `feature/v5.22-inducibility-onset-and-c3-effectiveness`

**Central question:** What changes at N=65 that makes C3 effective?

**Planned suites:**
- **IOP:** Inducibility Onset Protocol
- **IOE:** Onset Execution
- **IOA:** C3 Effectiveness Analysis
- **IOI:** Onset Intervention Audit
- **IOS:** Final Synthesis

**Core questions:**
1. What changes between N=64 and N=65?
2. Why does C3 omega shift jump 15.8x at N=65?
3. Is onset controlled by d/K response compatibility, C3 alignment gain, rebMagnitude activation, or another variable?
4. Can the N=65 onset be predicted from local response metrics?
5. Is the onset sharp or continuous with finer N scanning (61-66)?

---

*V5.21 — 19 tests, 0 failed. Strict claim discipline maintained throughout.*
