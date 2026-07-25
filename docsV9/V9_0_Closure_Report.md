# V9.0/V9.1 Closure Report

**Branch:** `v9.0-clockwork-physics`
**Closure date:** 2026-07-25
**Status:** Closed — 20 audits, prediction gap resolved

## 1. Scope

V9.0 Clockwork Physics synthesized the V7.4→V8.4 chain into emergent physical
structure. V9.1 resolved the prediction gap by identifying L as the primary
clockwork observable.

## 2. Audit Outcomes

### Core Clockwork (V9.0)

| Audit | Focus | Decision | Key |
|:------|:------|:---------|:----|
| CPF_01 | ON/OFF physics | C | ON families uniquely support physics |
| CGA_01 | Clockwork gate | D | Non-conservative budget gate |
| VBT_01 | Budget necessity | C | Budget loss necessary+sufficient |
| BAO_01 | Budget origin | A | Budget loss fundamental, immediate |
| BOC_02 | Oscillation test | A | Relaxation, not oscillation |
| WPO_01 | Wave phase | B | Single loss→gain cycle |
| BDC_01 | Budget dynamics | C | Time follows dynamics magnitude |
| TDO_01 | Tick origin | B | Tick necessary, not sufficient |
| TED_01 | Equilibrium distance | B | Disequilibrium contributes |
| CTA_01 | Activation | C | Activation = Tick×D_eq, 0 counterexamples |
| CAI_01 | Activation irreducible | D | Two irreducible components |
| DEO_01 | Disequilibrium origin | C | D_eq deepest dynamic quantity |
| DGO_01 | Genesis | A | Family difference pre-exists at β=0 |
| VRC_01 | V1 recovery | C | 6/6 V1 concepts recovered |
| PPA_01 | Physical prediction | A | Conceptual, not yet predictive |
| CMP_01 | Missing layer | D | Multiple missing layers |
| MSO_01 | Missing state | D | Multiple hidden variables |
| SSC_01 | State space | A | State space sufficient (>80%) |

### Prediction Gap Resolution (V9.1)

| Audit | Focus | Decision | Key |
|:------|:------|:---------|:----|
| OMA_01 | Observable mapping | A | L alone CV=0.15, ratios amplify variance |
| LOC_01 | L as observable | C | L is primary clockwork observable |

## 3. Complete Clockwork Hierarchy

```
STATIC BRANCH (all families):           DYNAMIC BRANCH (ON only):
Family axiom                            Family axiom
  → K(d)                                  → CCI evaluation (β-sensitive)
  → λ1,λ2 (primitive, invariant)          → VarI1(β), VarTerms(β)
  → Accessibility                         → D_eq = |total - total_eq|
  → Occupation                            → Tick = |d(total)/dβ|
  → Resonance                             → Activation = Tick × D_eq
                                           → dH/dβ (time flow)
PRIMARY OBSERVABLE:
  L = 1 - VarI1/VarTerms  (CV=0.15)
```

## 4. Key Quantitative Results

| Result | Value |
|:-------|------:|
| L CV | 0.15 (stable across GAN/ICS/CNS) |
| State space completeness | >80% variance explained |
| V1 concepts recovered | 6/6 |
| Budget loss necessary+sufficient | 0 counterexamples |
| Activation = Tick×D_eq | r=0.83, 0 counterexamples |

## 5. What Is Established

- L = 1 - VarI1/VarTerms is the primary dimensionless clockwork observable
- The clockwork has two orthogonal branches: static (λ, accessibility, occupation)
  and dynamic (VarI1, VarTerms, D_eq, Tick, Activation, dH)
- ON families (GAN, ICS, CNS) are non-conservative; OFF families (SAC, RCS)
  are conservative with zero amplitude
- All 6 original V1 concepts are recovered from the formal hierarchy
- The state space is sufficient for >80% of dH variance

## 6. Test Summary

| Metric | Count |
|:-------|------:|
| V9.0/V9.1 audits | 20 |
| Total tests | ~3393 |
| Failed | 0 |
