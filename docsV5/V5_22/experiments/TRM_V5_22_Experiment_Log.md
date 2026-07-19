# V5.22 Experiment Log | **Created:** 2026-07-18
| Suite | Status | Tests | Results |
|-------|--------|-------|---------|
| IOP | COMPLETE | 3 tests (protocol) | 3/3 passed |
| IOI | COMPLETE | 6 tests | 6/6 passed, ~4 min |

## IOI Summary (2026-07-18)
- **N=64 baseline:** CONFIRMED immune, max c3OmegaShift=0.049
- **Component audit:** No single boost enables rescue. Best: I1-dT1x2.0 max c3OmgS=0.715
- **Full package:** max c3OmgS=0.636 — STILL below RR min (0.765)
- **0/56 N=64 seeds** ever exceed RR c3OmegaShift threshold across ALL interventions
- **Best N=64 seed** meets all RR preconditions (dT1=0.95, alignPre=-0.61) but c3OmgS=0.72
- **4 seeds (14%)** have preconditions met but c3OmgS<0.1 → Omega gain failure
- **Finding:** N=64 has a fundamental C3 response-gain block
- **Gate G REACHED:** N=64 is genuinely below the C3-effectiveness threshold
- **N=65 onset is a true C3 gain boundary**, not an operator limitation

## IOA Summary (2026-07-18)
- **27 rescued seeds** classified: 10 RR (37%), 7 DA (26%), 10 unclear (37%)
- **C3 omega shift** is dominant driver (61.6x contrast rescued vs failed)
- **RR signature:** alignPre [-0.56,-0.14], dT1 [0.58,0.97], c3OmgS [0.77,2.25]
- **N=64 mystery:** 14% meet RR preconditions but c3OmgS capped at 0.049 (15.6x below RR min)
- **Cross-N validated:** RR exists at N=65, 66, 70, 72 — NOT N=65-specific
- **Best threshold:** c3OmegaShift>0.10 (97% N=65 acc, 91% holdout acc)
- **Gates reached:** A (C3 driver), B (RR defined), E (cross-N generalization)
- **Gate C partial:** N=64 preconditions met but c3OmegaShift fundamentally capped

## IOE Summary (2026-07-18)
- **Fine scan N=62-67:** Sharp onset at N=65 (0%→14% rescues)
- **C3 omega shift:** Dominant onset driver (70.2x contrast pre vs post)
- **Jump at N=64→65:** c3OmegaShift ×15.8, rescued seeds up to 0.882
- **Rescued seed profile:** Negative alignPre (-0.553), high dT1 (0.956), massive c3 effect (0.882)
- **Mechanism:** Resonant reversal — anti-aligned seeds + large probe displacement → C3 correction produces huge Omega shift
- **Gates reached:** A (Sharp onset), C (C3 threshold), D (Single driver)
