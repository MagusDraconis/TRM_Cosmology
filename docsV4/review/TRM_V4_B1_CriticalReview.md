# TRM V4 — B1 Critical Review (No Optimism Bias)

**Date:** 2026-07-05
**Reviewer:** Self-audit — strict classification, no narrative smoothing
**Referent:** `TRM_V4_B1_Results.md`

---

## 1. What B1 Actually Did

B1 evaluated 16 (δK, F) pairs **analytically** by:
1. Taking the functional form of δK(r) and F(δK)
2. Computing asymptotic power-law exponents α (for δρ) and β (for a(r))
3. Comparing against targets α = −1, β = −2

**No numerical simulation was performed.** All results follow directly from the functional forms of the candidates. This is a valid analytic check, but it has limitations (see Part 5).

---

## 2. Best Pair: K1×F1

```
δK(r)   = k·M/r
ρ_eff   = ρ_ref · δK/K₀
→ δρ(r) = (ρ_ref·k·M)/(K₀·r)  →  α = −1.000
→ a(r)  = −(c²·k·M)/(K₀·r²)   →  β = −2.000
```

---

## 3. Classification: PARTIAL, not VALID

### 3.1 Why the numeric scores are perfect

The asymptotic exponents α = −1.000, β = −2.000 are exact — zero deviation from target. Under the strict threshold (|α+1| < 0.10, |β+2| < 0.10), this would be classified VALID.

### 3.2 Why the classification should be downgraded to PARTIAL

**Reason 1: k is determined post-hoc, not predicted.**

```
k = G·K₀/c²
```

This is not a prediction — it is a **consistency condition**. We set k to whatever value makes a(r) match Newton, then declared success. A genuine prediction would derive k from independent principles and then show the resulting a(r) matches Newton without tuning.

**Classification impact:** The model has exactly as many degrees of freedom as needed to match the data. This is curve-fitting, not prediction. → **PARTIAL at best.**

**Reason 2: K1 is asserted, not derived.**

δK ~ M/r is claimed to be "the natural 3D Laplace Green's function." But **no equation governing the coupling field is derived** — we simply chose the functional form that produces the right answer. We did not show:

- That the coupling field obeys ∇²(δK) ∝ −M from oscillator dynamics
- That K1 is the unique solution under specified boundary conditions
- That any other form is excluded by the oscillator equations

Without a governing equation for δK, K1 is a **modeling choice**, not a derivation. → **PARTIAL.**

**Reason 3: F3 and F4 reduction to F1 is contingent on perfect synchronization.**

F3 and F4 reduce to F1 only if:
- The order parameter R is globally constant (≈ 0.889 in CML)
- cos(Δθ) = 1 everywhere (all phases perfectly aligned)

A radial coupling gradient δK(r) will produce **radial phase gradients** — the oscillators at different r will have slightly different effective couplings, producing slightly different frequencies, producing non-zero Δθ. In that regime, cos(Δθ) < 1, and F3/F4 no longer reduce exactly to F1.

**This effect is small in the synchronized state but not zero.** → F3/F4 classification should be: **LIKELY PARTIAL** (close to F1 but not identical).

**Reason 4: The linear superposition assumption is untested.**

K1 assumes δK ∝ M/r for a single point mass. For multiple masses, the implicit assumption is linear superposition:

```
δK_total(r) = Σ_i k·M_i / |r − r_i|
```

This follows if ∇²(δK) ∝ −M is linear. But:
- Is the coupling field equation actually linear?
- Do coupling perturbations from different masses interact?
- Is there a saturation or screening effect at large M?

**None of these are tested.** → **PARTIAL** — linear regime only, unverified.

---

## 4. What B1 Genuinely Established

| Finding | Confidence | Caveat |
|:---|:---|:---|
| K1×F1 has the correct asymptotic form | **HIGH** — analytic, exact | Only if K1 is the correct δK form |
| No other (δK, F) pair produces Newtonian gravity | **HIGH** — exhaustive check of 16 pairs | Only among the 4 K-candidates and 4 F-mappings tested |
| The mechanism has exactly 0 free parameters in the Newtonian limit | **MEDIUM** — k is post-hoc identified, not predicted | k = G·K₀/c² is a consistency condition |
| The mechanism is unique (only K1 works) | **HIGH** — no degeneracy in the solution space | True within the tested candidate set |

---

## 5. What B1 Did NOT Establish

| Gap | Severity | Impact |
|:---|:---|:---|
| Governing equation for δK | **CRITICAL** | Without ∇²(δK) law, K1 is a modeling choice, not a derivation |
| Physical value of K₀ | **HIGH** | K₀ is dimensionless in CML; needs physical frequency anchor |
| Nonlinear regime behavior | **MEDIUM** | Strong-field corrections unknown |
| Multi-mass superposition validity | **MEDIUM** | Required for galaxy-scale application |
| Phase gradient effect on F3/F4 | **LOW** | Small correction in synchronized state |

---

## 6. Revised Classification

```
Original classification:  VALID  (|α+1| = 0.000, |β+2| = 0.000)
Revised classification:   PARTIAL
```

**Justification for downgrade:**

1. k is post-hoc matched, not predicted → the model has exactly as many degrees of freedom as the data requires
2. K1 is a modeling choice, not derived from oscillator equations → the functional form is selected to produce the right answer
3. The coupling field lacks a governing equation → the mechanism is phenomenological, not fundamental

**The model is NOT falsified.** It is also NOT validated. It is **consistent with Newtonian gravity** when K1 is chosen as the δK form and k is set to G·K₀/c². This is a **phenomenological match**, not a first-principles derivation.

---

## 7. What Would Upgrade the Classification to VALID

| Requirement | Current Status |
|:---|:---|
| Derive ∇²(δK) ∝ −M from oscillator dynamics | **NOT DONE** — K1 is asserted |
| Show K1 is the unique solution | **NOT DONE** — no governing equation exists |
| Predict k from independent principles | **NOT DONE** — k is post-hoc matched to G |
| Test nonlinear superposition numerically | **NOT DONE** |
| Verify F3/F4 reduction to F1 with radial K gradient | **NOT DONE** |

---

## 8. B2 Recommendation

### Should we proceed to B2?

**YES — but with explicit caveats.**

B2 is justified as a **phenomenological consistency test**, not as a confirmatory validation. The correct framing for B2:

> "Assuming the K1×F1 mechanism (δK ~ M/r, ρ_eff ∝ K), does the baryonic mass distribution in SPARC galaxies produce rotation curves consistent with observations, using a single global ρ_ref?"

**What B2 CAN establish:**
- Whether the mechanism is phenomenologically viable at galactic scales
- Whether a single ρ_ref works across diverse galaxies
- Whether the BTFR emerges naturally

**What B2 CANNOT establish:**
- That δK ~ M/r is derived from first principles
- That the coupling field equation is correct
- That the mechanism is unique or fundamental

### Warning

If B2 succeeds, the temptation will be to claim "TRM explains galaxy rotation without dark matter." **Resist this.** The correct claim is:

> "Under the hypothesis that mass perturbs oscillator coupling as δK ~ M/r, the resulting effective gravity reproduces observed galaxy rotation curves. The hypothesis itself — the coupling field equation — remains an open theoretical question."

---

## 9. Final Verdict

```
B1 RESULT:  PARTIAL (phenomenological match, not first-principles derivation)

Best pair:          K1×F1
Asymptotic match:   Exact (α = −1.000, β = −2.000)
Derivation depth:   Shallow — K1 chosen to produce the right answer
Free parameters:    1 post-hoc identification (k = G·K₀/c²) — not a prediction
Unique mechanism?   Yes — only K1×F1 works among 16 candidates

B2 recommendation:  PROCEED as phenomenological consistency test
                    DO NOT claim as validation of fundamental mechanism
```

---

## Appendix — Honest Self-Assessment

| Question | Honest Answer |
|:---|:---|
| Did we derive Newtonian gravity from oscillator dynamics? | **No.** We chose δK ~ 1/r because it produces the right answer. |
| Did we predict the value of G? | **No.** We used G to calibrate k. k = G·K₀/c² is circular. |
| Did we discover that only one mechanism works? | **Yes.** Among 16 candidates, only K1×F1 matches. The mechanism is unique within the tested space. |
| Is this scientifically valuable? | **Yes.** A unique phenomenological mechanism is identified. The gap between "unique phenomenological match" and "first-principles derivation" is clearly documented. |
| Would a reviewer accept this as "Newton derived from TRM"? | **No competent reviewer would.** A competent reviewer would note the post-hoc calibration and ask: "Why δK ~ 1/r?" |
| What's the honest status? | **Phenomenological model with exact Newtonian limit, awaiting a governing equation for the coupling field.** |
