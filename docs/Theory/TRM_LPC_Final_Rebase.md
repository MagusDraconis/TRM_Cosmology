# TRM LPC — Final Rebase

## Original Assumption

From the FP01–FP31 formal proof scaffold and the LPC discharge plan, the original assumption was stated as:

> **"TQM lattice phase closure"** — defines the admissible rational families $\Omega = (q + m)/q$ and the phase-closure condition $q\Omega - p = 0$ with $p = q + m$.

This assumption was one of four explicit model hypotheses (alongside minimal lattice action, shared/global normalization, and bounded admissible domain). It was identified as the **most upstream gap** — all other assumptions operate on structures that depend on the phase-closure family definition.

---

## What LPC Has Reduced

### LPC01A — Topological Closure Framework

| Established | Classification |
|:---|:---|
| Periodic lattice + phase on $S^1$ + closed loop → $q\Omega = p$ with $p \in \mathbb{Z}$ | **Topology** |
| The step from "discrete synchronization" to "$q\Omega = p$" is exactly 8 levels deep | **Analysis** |
| The only OPEN node in the topological chain is M5 (phase single-valuedness) | **Finding** |
| The chain from M5 to $q\Omega = p$ is a standard topological consequence of $\pi_1(S^1) = \mathbb{Z}$ | **Topology** |

### LPC01B — Single-Valuedness Audit

| Established | Classification |
|:---|:---|
| M5 (phase single-valuedness modulo $2\pi$) is IMPLICITLY present in 4 repository locations | **Finding** |
| It is not explicit, not derived, and not contradicted | **Finding** |
| It is a mathematical property of the $U(1)$ phase representation, not a physics assumption | **Classification** |
| Stating M5 explicitly would name an assumption already in use, not add a new one | **Conclusion** |

### LPC01C — Origin of $p = q + m$

| Established | Classification |
|:---|:---|
| $p = q + m$ was introduced in RBF05 as a closure-family ansatz | **Historical** |
| It was motivated by empirical bridge-band match, not derived | **Finding** |
| It is labeled "reparameterization" in all subsequent documents | **Definitional** |
| The original form was $p - q = m$ (defining $m$ as the difference) | **Historical** |
| Status: **ASSUMED**, not DERIVED | **Classification** |

### LPC01D — Ansatz Space Audit

| Established | Classification |
|:---|:---|
| Topology CANNOT distinguish $p = q + m$ from $p = q + f(m)$ | **Finding** |
| Five explicit alternative families are compatible with $q\Omega = p$ | **Counterexamples** |
| The only structural property that distinguishes the current ansatz is separability ($p - q$ independent of $q$) | **Finding** |
| $f(m) = m$ is the simplest member of the separable class, not the only member | **Conclusion** |
| Only affine $f(m) = m + c$ preserves the FP01–FP31 scaffold (relabeling) | **Constraint** |

### LPC02A — Bridge-Band Dependence Audit

| Established | Classification |
|:---|:---|
| The bridge band $\Omega \approx 1.16..1.19$ is independent of $f(m)$ | **Finding** |
| $f(m) = m$ is CONVENIENT at RBF05, ASSUMED at FP01, ESSENTIAL at FP02 | **Dependency trace** |
| Non-affine $f$ invalidates FP02–FP31 | **Constraint** |
| The bridge band constrains $\Omega$, not $m$ — $m = 3$ is the label assigned to the band-matching family under $f(m) = m$ | **Finding** |

### LPC02B — Meaning of $m$ Audit

| Established | Classification |
|:---|:---|
| $m$ is an indexing convention, not a fundamental quantity | **Conclusion** |
| Primary meaning: mode index (label) | **Definitional** |
| Secondary meaning: winding offset ($m = p - q$) | **Algebraic** |
| Tertiary meaning: closure-defect proxy ($\delta = \|m-3\|/3$) | **Definitional** |
| $m$ is never claimed to be a physical observable, topological invariant, or derived quantity | **Finding** |

### LPC03A — Selection Principle Audit

| Established | Classification |
|:---|:---|
| No repository component selects $m = 3$ independently of $\delta = \|m-3\|/3$ | **Finding** |
| The root selection is: "$m = 3$ is the smallest integer that, under the ansatz, maps to the bridge band" | **Finding** |
| This is an EMPIRICAL FIT, not a derivation | **Classification** |
| The three-constraint stack formalizes the pre-selection; it does not discover it | **Finding** |
| targetShift = 3 is circular: it defines the phase defect to be zero at the pre-selected mode | **Finding** |

---

## Eliminated Components

The following components of the original assumption have been reduced to simpler categories:

| Original Component | Reduced To | Classification |
|:---|:---|:---|
| "$q\Omega - p = 0$ is the phase-closure condition" | Topological consequence of phase on $S^1$ + closed loop (LPC01A), once M5 is stated | **Topology + IMPLICIT (M5)** |
| "Closure families are rational" | $q\Omega = p$ with $p, q \in \mathbb{Z}$ → $\Omega = p/q$ rational by algebra | **Algebra** |
| "$m$ is a fundamental closure index" | $m$ is an indexing convention; $m = p - q$ is the excess winding | **Convention** |
| "Phase defect selects $m = 3$" | targetShift = 3 is chosen because $m = 3$ was pre-selected by bridge-band fit | **Circular (definitional)** |
| "$m = 3$ is uniquely selected by topology" | Topology cannot distinguish $m = 3$ from any other index | **False (LPC01D, LPC03A)** |

---

## Remaining Independent Components

After reduction, the following statements remain genuinely independent. They are not consequences of topology, algebra, or existing repository infrastructure.

| # | Statement | Classification | Justification |
|:---:|:---|:---|:---|
| R1 | The collective phase on the TQM lattice is single-valued modulo $2\pi$ | **IMPLICIT** → can be made **DEFINED** | Already used in 4 repository locations (LPC01B); stating it explicitly closes the last topological OPEN node |
| R2 | The closure-family ansatz is $p = q + m$ (equivalently $\Omega = (q+m)/q$) | **ASSUMED** | Not derived from topology or lattice dynamics (LPC01C, LPC01D). The structurally simplest separable form. |
| R3 | The bridge-relevant rational band is $\Omega \approx 1.16..1.19$ | **EMPIRICAL** | From V2.2 effective low-acceleration boundary + CML mode-locking diagnostics. Independent of the ansatz (LPC02A). |
| R4 | Under R2 + R3, the bridge-band mode is $m = 3$ (smallest integer producing $\Omega$ in the band) | **EMPIRICAL** | A fit, not a derivation (LPC03A). The value $m = 3$ is contingent on both the ansatz choice and the band bounds. |
| R5 | The FP01–FP31 scaffold is valid given R1–R4 | **DEFINED** | Proven in the Lean proof scaffold (0 PENDING-PROOF, 0 BLOCKED). |

---

## Dependency Graph

```
┌─────────────────────────────────────────┐
│  Periodic lattice with q sites           │  [DEFINED — FP01]
│  Phase variables θ_a on sites            │  [DEFINED — Memory Channel doc]
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│  R1: Phase single-valuedness             │  [IMPLICIT → DEFINED by statement]
│  θ_{a+q} ≡ θ_a (mod 2π)                 │  [LPC01B: used in 4 repo locations]
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│  qΩ = p  with  p ∈ ℤ                     │  [TOPOLOGY: π₁(S¹) = ℤ]
│  Ω = p/q  (rational)                     │  [ALGEBRA]
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│  R2: Ansatz  p = q + m                   │  [ASSUMED — simplest separable form]
│  Ω = (q+m)/q                             │  [LPC01C, LPC01D: convention]
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│  R3: Bridge band  Ω ≈ 1.16..1.19         │  [EMPIRICAL — V2.2 + CML]
│  (Independent of R2)                     │  [LPC02A: constrains Ω, not m]
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│  R4: m = 3 is the bridge-band mode       │  [EMPIRICAL — smallest integer under R2]
│  (Contingent on R2 + R3)                 │  [LPC03A: fit, not derivation]
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│  targetShift = 3, δ = |m-3|/3            │  [DEFINITIONAL — centered on R4]
│  qCore = {16, 17, 18}                    │  [GEOMETRIC — from R2 + R3 + R4]
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│  R5: FP01–FP31 scaffold                  │  [DEFINED — proven under R1–R4]
│  7 DEFINED, 4 ASSUMED, 0 PENDING-PROOF   │
│  0 BLOCKED, no sorry remains             │
└─────────────────────────────────────────┘
```

### Node Classification Summary

| Level | Node | Classification |
|:---:|:---|:---|
| 0 | Periodic lattice + phase variables | DEFINED |
| 1 | R1: Phase single-valuedness | **IMPLICIT → DEFINED** (statement needed) |
| 2 | $q\Omega = p$, $p \in \mathbb{Z}$ | TOPOLOGY |
| 3 | $\Omega = p/q$ (rational) | ALGEBRA |
| 4 | R2: Ansatz $p = q + m$ | **ASSUMED** |
| 5 | R3: Bridge band $\Omega \approx 1.16..1.19$ | **EMPIRICAL** |
| 6 | R4: $m = 3$ is the bridge-band mode | **EMPIRICAL** |
| 7 | targetShift, $\delta$, $q_{\text{Core}}$ | DEFINITIONAL / GEOMETRIC |
| 8 | R5: FP01–FP31 scaffold | DEFINED (proven) |

---

## Residual Assumption Analysis

### 1. Most Conservative Formulation

The smallest statement that still must be assumed if all LPC findings are accepted:

> The closure-family ansatz $p = q + m$ is adopted as the operational definition of the rational phase-closure families. The bridge-relevant rational band $\Omega \approx 1.16..1.19$ is taken as an empirically motivated constraint. Under these conventions, $m = 3$ is the smallest integer mode that maps to the band, and the FP01–FP31 scaffold is valid given these inputs.

This formulation contains exactly two independent assumptions (R2, R3) plus one contingent empirical identification (R4). Everything else follows.

### 2. Strongest Formulation Currently Justified

> Under the closure-family ansatz $p = q + m$ and the independently motivated bridge-band constraint $\Omega \in [1.16, 1.19]$, the mode $m = 3$ is the unique admissible mode in the finite domain $m \leq 5$, $q \leq 10000$ under the three-constraint stack (phase closure, bridge-band occupancy, action/tick consistency). The continuous asymptotic bounds are proven in the Lean scaffold (FP31). Uniqueness over unbounded $m$ and the first-principles origin of the ansatz and the bridge band remain open.

### 3. Reviewer-Facing Status Formulation

> The TRM/TQM $m = 3$ formal proof scaffold (FP01–FP31) is closed under four explicit assumptions: (1) phase single-valuedness on the periodic lattice, (2) the closure-family ansatz $p = q + m$, (3) the empirically motivated bridge band $\Omega \approx 1.16..1.19$, and (4) the minimal lattice action / shared normalization / bounded domain hypotheses. Of these, (1) is an implicit mathematical property of the $U(1)$ phase representation used throughout the repository; (2) is a definitional convention; (3) is an irreducible structural input — the bridge band has zero dynamical origin (BD1–BD6) and no non-circular external calibration exists (I2_Calibration_or_Axiom.md); and (4) are model-level scaffolding assumptions. The selection of $m = 3$ is a consequence of (2) + (3) and is not independently derived from topology or lattice dynamics.

---

## Reviewer-Safe Status

The original assumption "TQM lattice phase closure" has been decomposed by the LPC track into its constituent parts. The condition $q\Omega = p$ follows from phase single-valuedness on a periodic lattice (a topological consequence of representing the collective phase on $S^1$, already implicit in the repository). The specific form $\Omega = (q+m)/q$ is a closure-family ansatz — a definitional convention, not a derivation. The bridge band $\Omega \approx 1.16..1.19$ is an irreducible structural input (I2) from V2.2 and the CML mode-locking track, independent of the ansatz. BD1–BD6 confirm it has zero dynamical origin (CLASS D — Purely Imposed). No non-circular external calibration law exists (I2_Calibration_or_Axiom.md). The identification $m = 3$ as the bridge-band mode is a contingent empirical fit under the chosen ansatz. The FP01–FP31 scaffold is proven correct given these inputs. No first-principles derivation of the ansatz or of the bridge band is claimed. No claim is made that $m = 3$ is uniquely selected by topology, algebra, or lattice dynamics independently of the ansatz and the empirical band.

---

## Final Question

**Has the original assumption "TQM lattice phase closure" been:**

| Option | Verdict |
|:---|:---|
| A) Fully discharged | **No** — R2 (ansatz) and R3 (bridge band) remain independent assumptions |
| B) Partially discharged | **Yes** — the topological component ($q\Omega = p$) has been reduced to phase single-valuedness (already implicit); the rational-family component has been identified as a convention |
| C) Decomposed into smaller assumptions | **Yes** — the original monolithic assumption is now five clearly separated components: R1 (single-valuedness, implicit → definable), R2 (ansatz, assumed), R3 (bridge band, empirical), R4 (m = 3 identification, contingent), R5 (scaffold validity, proven) |
| D) Unchanged | **No** — the LPC track has eliminated the claim that $m = 3$ is topologically selected and identified the precise assumptions on which the scaffold depends |

**Answer: C (decomposed into smaller assumptions), with partial progress toward B.**

The assumption has been reduced from one opaque statement ("TQM lattice phase closure defines the admissible rational families") to five transparent components with clear classifications. The ansatz (R2) and the bridge band (R3) remain independent — they have not been derived. But they are now explicitly named, bounded, and separated from the topological infrastructure that supports the scaffold.
