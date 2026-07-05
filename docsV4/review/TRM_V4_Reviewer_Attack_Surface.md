# TRM V4 — Reviewer Attack Surface

**Date:** 2026-07-05
**Purpose:** Preemptive analysis of likely reviewer criticisms and TRM V4 responses

---

## Attack 1: "G is not predicted"

**Criticism:** The gravitational constant G is not derived from TRM parameters. The relation k = G·K₀/c² is post-hoc calibration — you set k to whatever makes the answer match Newton.

**Response:** Correct. G is calibrated, not predicted. This is the status of G in Newtonian gravity and in GR (where G or the Planck mass is an empirical input). TRM advances the derivation depth by one level: it explains the 1/r **form** from network topology (B3B), while acknowledging that the **scale** remains empirical. This is explicitly documented as PARTIAL / CALIBRATED, not claimed as derived.

**Severity:** MEDIUM. Honest acknowledgment defuses the attack. The gap is structural — the coupling field governing equation is not derived from oscillator dynamics.

---

## Attack 2: "c_K = c is assumed, not derived"

**Criticism:** The wave propagation speed c_K is identified with the speed of light by assumption. This is not derived from TRM oscillator dynamics.

**Response:** Correct. B4-T1 evaluates four candidate relations for c_K. The recommended approach (c_K = c) is an empirically-supported assumption (LIGO: c_gw = c ± 10⁻¹⁵), not a TRM derivation. The alternative — introducing a spatial anchor I4 = Δx and deriving c_K = K₀·Δx·f_ref — is available if c_K ≠ c is ever measured. The assumption is explicitly documented as ASSUMED, not DERIVED.

**Severity:** LOW. The assumption is empirically well-supported and explicitly acknowledged.

---

## Attack 3: "Weak-field only — where is strong-field GR?"

**Criticism:** TRM V4 reproduces weak-field GR observables (redshift, deflection, precession at O(φ)). It does not cover strong-field phenomena: black holes, gravitational collapse, cosmological singularities, or full nonlinear GR.

**Response:** Correct. TRM V4 is a weak-field framework. The wave equation □K = 0 (B4) is the linearized limit — analogous to linearized GR. The full nonlinear theory (analogous to the Einstein equations) is not developed. This is a limitation of the current framework, not a contradiction. The scope is explicitly: weak-field, static + dynamic (linear wave), phenomenological.

**Severity:** HIGH for "replaces GR" claims. MITIGATED because TRM V4 does not claim to replace GR — it claims to be a structurally novel weak-field framework.

---

## Attack 4: "No cosmological constant / dark energy derivation"

**Criticism:** φ₀ = ρ_bg/ρ_ref ≈ 0.17 is identified with the background energy density, but ρ_bg is not independently measured. The connection to Λ (cosmological constant) is not derived.

**Response:** Correct. φ₀ is calibrated via the bridge-band prior I2. The identification φ₀ ↔ ρ_bg is a C5 interpretation, not a derivation. ρ_bg and ρ_ref are not independently determined. This is documented as CALIBRATED.

**Severity:** MEDIUM. The gap is acknowledged. The bridge band I2 constrains Ω ∈ [1.16, 1.19], but the physical origin of this band (why this range?) remains open (BD1-BD6: CLASS D, imposed).

---

## Attack 5: "Dark matter not explained — SPARC is just fitting"

**Criticism:** SPARC galaxy rotation curves are fitted with calibrated a₀, not derived from first principles. This is curve-fitting, not a dark matter replacement.

**Response:** Correct. SPARC is CALIBRATED, not DERIVED. TRM V4 does not claim to explain dark matter — it claims that baryonic models with TRM phenomenology are competitive with MOND. The honest status is: "competitive phenomenological fit, derivation open."

**Severity:** HIGH for "solves dark matter" claims. MITIGATED because TRM V4 does not make this claim. The B2 tests are explicitly phenomenological consistency tests.

---

## Attack 6: "Oscillator network is a toy model — not physical"

**Criticism:** The CML (N = 20 oscillators on a ring) is a toy model. No physical system of coupled oscillators with these specific parameters is known to exist.

**Response:** The oscillator model is the **mathematical core**, not a claim about physical ontology. The claim is: "IF a network of coupled phase oscillators with I1, I2, D1 exists, THEN the gravitational 1/r form follows from the graph Laplacian." The physical interpretation (C5: energy density) provides the bridge to observables. The model is falsifiable: coupling defect tests (B3B-T1) predict measurable 1/r phase perturbations in any synchronized oscillator system.

**Severity:** MEDIUM. The gap between mathematical model and physical ontology is explicitly acknowledged. Falsifiability is provided.

---

## Attack 7: "Why δK ~ M/r? You just chose it."

**Criticism:** K1 (δK ~ M/r) was selected from 16 candidates because it works. This is post-hoc selection, not derivation.

**Response:** B3B addresses this: δK ~ 1/r is the Green's function of the discrete graph Laplacian with a localized coupling defect. The form is **not arbitrary** — it's the unique isotropic response of a 3D elliptic operator to a point perturbation. The question "WHY does a mass produce a coupling defect?" is B3C (coefficient mapping), which remains calibrated. But the 1/r **form** is explained (B3B), not chosen.

**Severity:** LOW. The form is structurally explained; only the coefficient remains calibrated.

---

## Attack 8: "81 xUnit tests prove nothing about physical reality"

**Criticism:** Passing xUnit tests proves internal consistency, not physical correctness. The tests validate the mathematical framework against itself, not against nature.

**Response:** The xUnit suite serves three purposes: (1) regression — prevents accidental breakage, (2) classification — distinguishes DERIVED from CALIBRATED from ASSUMED, (3) benchmark — verifies numerical agreement with known physical values (solar g, deflection angle, redshift). The tests do not claim to prove physical correctness — they claim to demonstrate internal consistency and numerical benchmark matching. Physical validation comes from comparison with observational data (SPARC, LIGO, Eddington).

**Severity:** LOW. The role of xUnit tests is correctly scoped.

---

## Summary

| Attack | Severity | Mitigation |
|:---|:---|:---|
| G not predicted | MEDIUM | Explicitly CALIBRATED; form explained, scale empirical |
| c_K = c assumed | LOW | Empirically supported (LIGO); alternative available |
| Weak-field only | HIGH | Scope explicitly limited; not claimed as GR replacement |
| No Λ derivation | MEDIUM | CALIBRATED; bridge band origin open (CLASS D) |
| SPARC = fitting | HIGH | Explicitly CALIBRATED; not claimed as DM solution |
| Toy model | MEDIUM | Falsifiable predictions; mathematical core, not ontology claim |
| δK chosen arbitrarily | LOW | Form structurally explained (B3B); only coefficient calibrated |
| xUnit ≠ physical proof | LOW | Correctly scoped as consistency + benchmark, not physical validation |

### Strongest defenses

1. **Honest classification** — every claim is labeled DERIVED/EFFECTIVE/CALIBRATED/ASSUMED
2. **No over-claims** — "replaces GR", "solves DM", "predicts G" are all explicitly denied
3. **Falsifiability** — B3B-T1 coupling defect test, O(φ²) time dilation deviation, c_K ≠ c would falsify B4-T1
4. **Unique mechanism** — K1×F1 is the only one among 16 candidates that works
