# TRM/TQM Lattice Phase Closure — Assumption Discharge Plan (LPC Track)

## Scope

This document turns the remaining explicit assumption **"TQM lattice phase closure"** into a structured set of reviewable proof obligations. It does not claim a proof. It does not claim that $\Omega = (q + m)/q$ has been derived from first principles. It provides the most conservative possible formal definition candidate, identifies the weakest necessary assumptions, and defines three proof obligations (LPC01–LPC03) that — if discharged — would elevate the assumption to derived status.

The document is reviewer-safe: all claim boundaries are explicit, and no new physics assumptions are introduced beyond what is already documented in the V1 → V2.2 → V3.0 → V3.3 lineage.

---

## Historical Origin of the Assumption

### V1: Variable Time-Rate Origin

The original Clockwork Cosmology formulation introduced the central intuition that gravitational-like behavior can be organized around a variable local time-rate factor. V1 treated timing structure as the core organizing variable.

> V1 is a conceptual origin and hypothesis direction, not a closed proof framework.

### V2.2: TRM Scalar-Field Formalization

V2.2 reformulated the early language into the Temporal Rate Matrix framework, centered on a scalar time-rate field $T(x, t)$. Gravitational and cosmological behavior were interpreted as kinematic drifts induced by gradients of the scalar temporal-rate field.

> V2.2 provides a field-style effective framework, but not a theorem-level derivation.

### V3.0: Memory + Mode-Locking Baseline

V3.0 separated the theory into memory-channel admissibility and rational mode-locking / $m = 3$ closure path. The $m = 3$ path was framed as a strongly constrained closure-order candidate, explicitly leaving theorem-level microscopic closure open.

### V3.1–V3.2: Action-Derived Memory and Minimal Lattice Action

V3.1 linked the memory term to action-derived guards. V3.2 introduced the UA16–UA23 guard block, testing whether the minimal effective action can be motivated from TQM lattice/synchronization structure. The operational action-tolerance was linked to the minimal lattice Euler stationarity proxy.

### V3.3: Bounded $m = 3$ Bridge-Mode Candidate

RBF24–RBF82 established that $m = 3$ is selected only when three constraints are jointly active:

```text
1. phase closure:        qΩ - p = 0,  p = q + m,  Ω = (q + m) / q
2. bridge-band occupancy: Ω ≈ 1.16..1.19,  γ ≈ 0.84..0.86
3. action/tick consistency: δE_action > 0 against admissible competitors
```

The three-constraint structure was consolidated through RBF24–RBF82 and formalized in the FP01–FP31 Lean proof scaffold. However, the phase-closure constraint itself — the rational compatibility condition $\Omega = (q + m)/q$ — remains an assumption, not a derivation.

---

## Current Formal Status

### Proven (FP01–FP31)

The following are proven within the Lean proof scaffold (0 `sorry` remaining under explicit assumptions):

1. $q_{\text{Core}} = \{16, 17, 18\}$ is derived exactly from the rational bounds $\Omega \in [1.16, 1.19]$, $\gamma \in [0.84, 0.86]$ (FP01).
2. For $m = 3$ over $q_{\text{Core}}$, the normalized phase defect $|q\Omega - p| / 3 = 0$ (FP02).
3. $m = 3$ is the unique admissible mode for finite $m_{\text{Max}} = 5$, $q \leq 10000$ (FP03).
4. $\forall m \neq 3$, $\text{EpsilonPhaseExact}(m) = |m - 3|/3 > 0$ over all $\mathbb{Z}$ (FP27, trichotomy).
5. $\text{EpsilonPhaseExact}(m) = 0 \leftrightarrow m = 3$ (FP28).
6. $\text{qCoreSupport}(q) = 1 - 3/q$ is an exact Rational model function; both $q_{\text{Core}}$ limit and $\varepsilon$-phase asymptotic bound are proven via the ceil inequality lemma (FP29–FP31).
7. All 7 DEFINED lemmas/theorems have complete Lean proof structures.

### Assumed

The following 4 model hypotheses are declared explicitly in every relevant document:

| # | Assumption | Role in the Scaffold |
|:---:|:---|:---|
| 1 | **TQM lattice phase closure** | Defines the admissible rational families $\Omega = (q + m)/q$ |
| 2 | Minimal lattice action | Provides the action/tick consistency discriminator |
| 3 | Shared/global normalization | Prevents per-mode free-parameter tuning |
| 4 | Bounded admissible domain | Constrains $(m, q)$ to a valid regime |

Assumption 1 is the **most upstream gap** — all other assumptions operate on structures that depend on the phase-closure family definition.

### Diagnostic Evidence (DS01–DS41)

The DS diagnostics provide numerical evidence for phase-coherence compatibility but are not formal proofs:

- DS01–DS08: Double-slit interference and coherence mapping (analytical verification).
- DS14, DS30, DS34: $q_{\text{Core}} = \{16, 17, 18\}$ / $m = 3$ bridge mapping confirmed across multiple diagnostic configurations.
- DS25: Sorkin $I_3 = -3.85 \times 10^{-15}$ confirms Born-rule pairwise interference structure.
- DS35–DS41: Audit track confirms falsifiability, absence of over-fitting, and reproducibility.

### Not Proven

- $\Omega = (q + m)/q$ has **not** been derived from TQM lattice dynamics or first principles.
- The rational band $\Omega \approx 1.16..1.19$ has **not** been derived from structure; its origin remains an open question.
- The phase-closure condition $q\Omega - p = 0$ is currently an **operational formalization**, not a theorem-level microscopic derivation.

---

## Formal Definition Candidate

The most conservative possible definition of "TQM lattice phase closure" that is consistent with all existing documents:

> **Definition (Lattice Phase Closure, operational form):**
>
> Let a TQM lattice be characterized by a discrete set of collective phase modes indexed by integer $m \in \mathbb{Z}_{>0}$. For a given mode $m$ and lattice-scale parameter $q \in \mathbb{Z}_{>0}$, the rational phase-closure family is:
>
> $$\Omega_m(q) = \frac{q + m}{q}, \quad \gamma_m(q) = \frac{1}{\Omega_m(q)} = \frac{q}{q + m}$$
>
> The phase-closure condition is:
>
> $$q \cdot \Omega_m(q) - (q + m) = 0$$
>
> The normalized phase defect for mode $m$ over a $q$-support set $Q$ is:
>
> $$\delta_{\text{phase}}(m, Q) = \frac{1}{|Q|} \sum_{q \in Q} \frac{|q \cdot \Omega_m(q) - (q + m)|}{3}$$
>
> Mode $m$ satisfies **lattice phase closure** over $Q$ if $\delta_{\text{phase}}(m, Q) = 0$.

**What this definition does NOT assert:**
- It does **not** derive $\Omega = (q + m)/q$ from lattice dynamics.
- It does **not** explain why this specific rational family is selected.
- It does **not** claim that the phase-closure condition is the only possible form.
- It does **not** assert that the $q_{\text{Core}} = \{16, 17, 18\}$ values are unique — they are unique **given** the independently motivated bridge-band $\Omega \approx 1.16..1.19$.

---

## Minimal Assumption Set

The weakest set of assumptions that appears necessary to maintain the existing FP01–FP31 scaffold:

| # | Assumption | Justification from Existing Documents |
|:---:|:---|:---|
| A1 | The TQM lattice admits a discrete set of collective phase modes indexed by integer $m$ | V3.0 mode-locking framework; DS20/DS33 Kuramoto synchronization diagnostics |
| A2 | Collective phase locking imposes a rational compatibility condition of the form $q\Omega - p = 0$ with $p \in \mathbb{Z}$ | V3.3 three-constraint stack (RBF24–RBF82); FP02 phase defect minimization |
| A3 | The integer relation $p = q + m$ is the structurally simplest admissible form | FP01 exact $q_{\text{Core}}$ derivation depends on this form; alternatives not explored |
| A4 | The bridge-relevant $\Omega$-band $\approx 1.16..1.19$ is independently motivated | V2.2 effective low-acceleration boundary; V3.0 bridge-band occupancy constraint |

**Weakening notes:**
- If A3 is relaxed, the $q_{\text{Core}}$ derivation may produce different $q$-sets for different $p(m, q)$ relations.
- If A4 is relaxed, the $q_{\text{Core}} = \{16, 17, 18\}$ specific values may shift, but the structure of the FP scaffold (exact-rational + Lean) would still apply with updated $q$-values.

---

## Proof Obligation Tree

### LPC01: Rational Form of Phase-Closure Families

- **Goal:** Prove that collective phase locking on a TQM lattice imposes rational compatibility families of the form $\Omega = (q + m)/q$ with integer $m$.
- **Status:** **OPEN** — this is the core upstream gap. Currently assumed (Assumption A2–A3).
- **Dependencies:**
  - Assumption A1 (discrete collective phase modes exist).
  - A structural argument linking TQM lattice synchronization to rational phase relations.
- **Required upgrade path:**
  ```text
  TQM lattice synchronization structure
  → discrete phase-locking condition
  → rational compatibility: qΩ - p = 0
  → integer relation: p = q + m
  → Ω = (q + m)/q
  ```
- **Claim boundaries:**
  - The proof would show that the rational form follows from lattice structure, not from enumeration or post-hoc selection.
  - It would **not** claim that $m = 3$ is the only admissible mode — that depends on the bridge-band occupancy constraint (Constraint 2) and action/tick consistency (Constraint 3).
- **Falsification risk:** The obligation fails if a different closure family (e.g., $\Omega = (q + f(m))/q$ with $f(m) \neq m$) emerges from the same TQM lattice assumptions and is not structurally penalized.

### LPC02: Bridge-Band Origin from Structure

- **Goal:** Derive the bridge-relevant rational band $\Omega \approx 1.16..1.19$ from TQM lattice or effective-action structure, not from cadence-prior shaping or post-hoc bridge targeting.
- **Status:** **OPEN** — currently an independently motivated operational band (Assumption A4). The FP scaffold uses it to derive $q_{\text{Core}} = \{16, 17, 18\}$.
- **Dependencies:**
  - LPC01 (rational family form must be established first).
  - V2.2 effective low-acceleration boundary relation $g_{\text{eff}} = \bar{g} + \sqrt{\bar{g} \cdot a_0}$.
  - V3.2 minimal lattice action / action-stationarity structure.
- **Required upgrade path:**
  ```text
  TQM lattice + effective action structure
  → bounded rational locking interval
  → Ω ≈ 1.16..1.19
  ```
- **Claim boundaries:**
  - The proof would show that the band emerges from structure, not from post-hoc fitting.
  - The specific numeric values $1.16..1.19$ are derived, not chosen.
- **Falsification risk:** The band-origin path weakens if the band disappears when cadence-prior shaping is removed, or if the preferred interval shifts strongly under solver reparameterization.

### LPC03: Uniqueness of $m = 3$ Under Full Constraint Stack

- **Goal:** Prove that $m = 3$ is the unique mode satisfying all three constraints (phase closure + bridge-band occupancy + action/tick consistency) under the derived lattice structure.
- **Status:** **Proven under assumptions** — FP01–FP31 prove this **given** the 4 explicit assumptions. LPC03 would become a **derived theorem** once LPC01 and LPC02 discharge their respective assumptions.
- **Dependencies:**
  - LPC01 (rational family form derived).
  - LPC02 (bridge band derived from structure).
  - Existing FP01–FP31 scaffold (exact-rational uniqueness + continuous asymptotic bounds).
- **Required upgrade path:**
  ```text
  LPC01 (rational families)
  + LPC02 (bridge band)
  + existing FP scaffold (exact-rational + Lean)
  → m = 3 is the unique admissible mode
  ```
- **Claim boundaries:**
  - Even with LPC01–LPC02 discharged, this is a **model theorem** — it proves uniqueness within the TQM lattice phase-closure framework, not a universal physics theorem.
  - The proof covers the admissible $(m, q)$ domain defined by the constraints; modes outside that domain are excluded by definition, not by proof.
- **Falsification risk:** $m = 4$ or another neighboring family reproduces phase closure, bridge occupancy, and action/tick consistency equally well under the derived constraints.

---

## Possible Counterexamples

The following counterexample classes are reviewer-relevant and should be addressed by any attempted proof of LPC01–LPC03:

1. **Alternative rational families:** A family of the form $\Omega = (q + f(m))/q$ with $f(m) \neq m$ that satisfies the same lattice synchronization conditions with comparable or lower structural penalty.
2. **Non-rational closure:** A continuous (non-discrete) phase relation that satisfies the TQM lattice dynamics without producing rational $\Omega$ families.
3. **Band-shift under reparameterization:** The preferred $\Omega$-band shifts significantly (e.g., $1.16..1.19 \to 1.20..1.23$) under a physically motivated change in the effective-action or lattice-scale parameterization.
4. **Competitor mode at different $q_{\text{Core}}$:** A different mode (e.g., $m = 2$ or $m = 4$) becomes the unique admissible mode under a $q_{\text{Core}}$ slice that is equally justified by the bridge-band structure.
5. **Constraint collapse:** Removing any one of the three constraints (phase closure, bridge-band occupancy, action/tick consistency) allows competitor modes to become admissible — this is already confirmed by RBF25 and RBF81, but a stronger structural argument should show **why** all three are jointly necessary.

---

## Claim Boundaries

The following claims are **explicitly excluded** for the LPC track:

| Claim | Status |
|:---|:---|
| Formal proof scaffold only | **YES** — LPC01–LPC03 are proof obligations, not completed proofs |
| Full first-principles closure | **NOT CLAIMED** — LPC01 and LPC02 are open |
| Universal physics theorem | **NOT CLAIMED** — the framework applies to the TQM lattice model |
| QM replacement | **NOT CLAIMED** |
| GR replacement | **NOT CLAIMED** |
| Numerology | **NOT CLAIMED** — all definitions are exact Rational |
| $\Omega = (q + m)/q$ derived from first principles | **NOT CLAIMED** — this is LPC01, currently OPEN |
| Bridge band $\Omega \approx 1.16..1.19$ derived from structure | **NOT CLAIMED** — this is LPC02, currently OPEN |

**Accurate current description:** "LPC track: structured proof obligations for the TQM lattice phase closure assumption. LPC03 (uniqueness of $m = 3$) is proven under assumptions. LPC01 (rational family form) and LPC02 (bridge-band origin) are open proof obligations."

---

## Relationship to Existing Tracks

| Track | Status | Relationship to LPC |
|:---|:---|:---|
| DS01–DS41 | **Complete** (41/41 PASS) | Numerical evidence for phase-coherence compatibility; not formal proofs |
| FP01–FP31 | **Complete** (0 PENDING-PROOF, 0 BLOCKED) | Proves $m = 3$ uniqueness **given** the 4 assumptions |
| **LPC01–LPC03** | **LPC01 OPEN, LPC02 OPEN, LPC03 proven under assumptions** | Discharges the most upstream assumption (TQM lattice phase closure) |

If LPC01 and LPC02 are discharged, the remaining 3 assumptions (minimal lattice action, shared normalization, bounded domain) could be addressed in separate tracks, and the full FP scaffold would become a derived theorem with only model-definition assumptions remaining.
