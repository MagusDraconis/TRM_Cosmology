# TRM LPC01A — Topological Closure Framework

## Scope

This document isolates the Level-5 gap identified in LPC01.2 and asks a single question:

> Can the missing step — "periodic lattice + single-valued collective phase + closed loop → integer winding number $p$ → $q\Omega = p$" — be formulated as a purely mathematical/topological statement using only ingredients already present in the repository?

The answer is **yes**. This document provides the formulation, classifies every ingredient, identifies missing mathematical assumptions (none of which are physics assumptions), and lists counterexample classes. No proof is attempted. No new physics is introduced.

---

## 1. Existing Ingredients From the Repository

| Ingredient | Source Document | Exact Form in Repository |
|:---|:---|:---|
| Periodic lattice with $q$ sites | FP01 (`M3ExactQCoreProof.cs`) | Integer $q \in \mathbb{Z}_{>0}$ indexes the lattice scale. $q$ is a period — the lattice has $q$ sites per fundamental cycle. |
| Lattice-site phase variables | `TRM_Memory_Channel_Microscopic_Derivation.md` §1 | $\theta_a$ are real-valued phase variables on lattice sites $a$. |
| Collective phase / order parameter | `TRM_Memory_Channel_Microscopic_Derivation.md` §2 | $Z(x) = \frac{1}{|\mathcal{W}(x)|}\sum_{a \in \mathcal{W}(x)} e^{i\theta_a}$, with amplitude $A(x) = |Z(x)|$. |
| Rational closure relation | FP01, `M3ExactDefinitions.cs` | $\Omega_m(q) = \frac{q + m}{q}$, $q\Omega - p = 0$, $p = q + m$. |
| Integer winding label | `M3ExactDefinitions.cs` line 21 | Comment: "Target winding number: $p = q + \text{targetShift}$." The word is present; the argument is not. |

### What is NOT present

| Missing Ingredient | Why It Matters |
|:---|:---|
| Explicit statement that the lattice has periodic boundary conditions | Without periodicity, there is no closed loop. |
| Explicit statement that the collective phase is single-valued | Without single-valuedness, the winding number is not constrained to be an integer. |
| The argument linking phase continuity around a closed loop to integer winding | This **is** the Level-5 gap. |

---

## 2. Candidate Mathematical Statement

The weakest possible theorem candidate that would close the Level-5 gap, using only repository ingredients:

> **Theorem Candidate (LPC01A):**
>
> Let a TQM lattice consist of $q \in \mathbb{Z}_{>0}$ sites with periodic boundary conditions.
> Let $\theta_a \in \mathbb{R}$ be the collective phase at site $a$, and let $\Delta\theta$ be the
> phase advance when moving one lattice step. Define $\Omega = \Delta\theta / (2\pi)$ as the
> fractional phase advance per step.
>
> If the collective phase is single-valued (i.e., $\theta_{a+q} \equiv \theta_a \pmod{2\pi}$),
> then the total phase accumulated around a closed loop satisfies:
>
> $$\sum_{k=1}^{q} \Delta\theta = q \cdot \Delta\theta = 2\pi \cdot p$$
>
> for some integer $p \in \mathbb{Z}$. Equivalently:
>
> $$q \cdot \Omega = p$$
>
> This is the **rational compatibility condition** that defines the admissible closure families.

### What this statement does NOT assert

- It does **not** assert that $p = q + m$ — that is a separate assumption (A3 in the LPC discharge plan).
- It does **not** assert that $\Omega$ takes any specific numeric value — that depends on the bridge-band constraint (LPC02).
- It does **not** assert that $m = 3$ is the unique admissible mode — that depends on all three constraints (LPC03).
- It does **not** assert that the lattice dynamics **produce** a single-valued phase — it only states the **consequence if** the phase is single-valued.

---

## 3. Required Mathematical Assumptions

### Already Present in the Repository

| # | Assumption | Classification | Source |
|:---:|:---|:---|:---|
| M1 | The lattice has $q$ sites | **DEFINED** | FP01; $q$ is an integer parameter |
| M2 | Each site has a phase variable $\theta_a \in \mathbb{R}$ | **DEFINED** | Memory Channel doc §1 |
| M3 | There exists a collective phase advance $\Delta\theta$ per lattice step | **DEFINED** | $\Omega$ in FP01 is the rational proxy for this |
| M4 | $q$ steps return to the starting site (periodic lattice) | **DEFINED** | Implicit in FP01's use of $q$ as a period |

### Missing (but purely mathematical — not physics)

| # | Assumption | Classification | Justification |
|:---:|:---|:---|:---|
| M5 | The collective phase is single-valued: $\theta_{a+q} \equiv \theta_a \pmod{2\pi}$ | **OPEN** | Standard condition for any physical phase on a closed manifold. Not stated in any TRM/TQM document. |
| M6 | The per-step phase advance is uniform: $\Delta\theta$ is independent of $a$ | **ASSUMED** | Follows from collective synchronization (all oscillators lock to the same frequency). Supported diagnostically by DS20 ($R \to 1$ in locked state). |
| M7 | Phase differences are measured in cycles (units of $2\pi$) | **DEFINED** | Standard; $\Omega$ is already a dimensionless ratio. |

### Stronger Than Repository Assumptions?

**No.** None of M5–M7 introduce new physics. They are mathematical formalizations of concepts already used operationally in the repository:

- M5 (single-valuedness) is implicitly assumed whenever one writes $\theta_a$ as a well-defined variable on a lattice — a multi-valued phase would not be a function.
- M6 (uniform advance) is the definition of collective synchronization — the locked state in DS20/DS33 where all oscillators share a common frequency.
- M7 (cycles) is a unit convention.

---

## 4. Dependency Graph

```
LEVEL 0:  Periodic lattice with q sites          [DEFINED — M1, M4]
          (FP01: q ∈ ℤ_{>0}, implicit period)

                    │
                    ▼

LEVEL 1:  Phase variables θ_a at each site       [DEFINED — M2]
          (Memory Channel doc §1)

                    │
                    ▼

LEVEL 2:  Closed loop: q steps return to start   [DEFINED — M1, M4]
          (Algebraic: index a+q ≡ a)

                    │
                    ▼

LEVEL 3:  Collective phase advance Δθ per step   [DEFINED — M3, M6]
          (Uniform if synchronized; DS20 diagnostic support)

                    │
                    ▼

LEVEL 4:  Phase single-valuedness                [OPEN — M5]
          θ_{a+q} ≡ θ_a (mod 2π)
          ═══════════════════════════════════
          THIS IS THE LEVEL-5 GAP
          ═══════════════════════════════════

                    │
                    ▼

LEVEL 5:  Total phase around loop = 2π·p         [OPEN — consequence of M5]
          q·Δθ = 2π·p  for some p ∈ ℤ
          (First homotopy group of S¹ is ℤ)

                    │
                    ▼

LEVEL 6:  q·Ω = p   (Ω = Δθ/2π)                 [DEFINED — algebra from Level 5]
          Rational compatibility condition

                    │
                    ▼

LEVEL 7:  p = q + m   (winding decomposition)    [ASSUMED — A3 from LPC plan]
          Ω = (q + m)/q
```

### Classification Summary

| Level | Node | Classification |
|:---:|:---|:---|
| 0 | Periodic lattice ($q$ sites) | **DEFINED** |
| 1 | Phase variables $\theta_a$ | **DEFINED** |
| 2 | Closed loop ($q$ steps → start) | **DEFINED** |
| 3 | Uniform phase advance $\Delta\theta$ | **DEFINED** |
| 4 | **Phase single-valuedness** | **OPEN** |
| 5 | Integer winding $p$ | **OPEN** (consequence of Level 4) |
| 6 | $q\Omega = p$ | **DEFINED** (algebra) |
| 7 | $p = q + m$, $\Omega = (q+m)/q$ | **ASSUMED** |

### The Gap Is Exactly One Node

The entire chain from "periodic lattice" to "$q\Omega = p$" has exactly **one OPEN node**: Level 4 (phase single-valuedness → integer winding). All other nodes are DEFINED or ASSUMED from existing repository content. Closing Level 4 would cascade: Level 5 follows immediately from the topology of $S^1$, and Level 6 is algebra.

---

## 5. Counterexamples

The following counterexample classes would falsify or restrict the candidate statement:

| # | Counterexample | Effect on LPC01A | Repository Status |
|:---:|:---|:---|:---|
| CE1 | **Discontinuous phase:** $\theta_a$ has a jump discontinuity between sites $a$ and $a+1$ | Single-valuedness (M5) fails; $p$ is not constrained to be an integer | Not excluded by any existing document |
| CE2 | **Open lattice:** No periodic boundary condition (lattice is a line, not a circle) | No closed loop exists; the winding argument does not apply | Implicitly excluded by FP01's use of $q$ as a period, but not explicitly stated |
| CE3 | **Non-periodic boundary conditions:** $\theta_{a+q} \neq \theta_a$ (e.g., twisted boundary) | $p$ may be non-integer or the condition $q\Omega = p$ may not hold | Not considered in existing documents |
| CE4 | **Multi-valued phase:** $\theta_a$ is defined only up to an additive constant that varies with $a$ | M5 fails; the phase is not a function on the lattice | Not excluded |
| CE5 | **Non-uniform phase advance:** $\Delta\theta$ depends on $a$ | M6 fails; the sum $\sum \Delta\theta_a$ may not simplify to $q\cdot\Delta\theta$ | Diagnostic evidence (DS20) supports uniformity in the locked state, but not proven for all regimes |

---

## 6. Classification

### Node Classification (from Dependency Graph)

| Node | Classification | Can Be Closed By |
|:---|:---|:---|
| Periodic lattice | **DEFINED** | — |
| Phase variables | **DEFINED** | — |
| Closed loop | **DEFINED** | — |
| Uniform phase advance | **DEFINED** | — |
| Phase single-valuedness | **OPEN** | Stating M5 as an explicit topological condition on the lattice |
| Integer winding $p$ | **OPEN** | Proving that M5 + closed loop → $p \in \mathbb{Z}$ (standard topology of $S^1$) |
| $q\Omega = p$ | **DEFINED** | Algebra from Level 5 |
| $p = q + m$ | **ASSUMED** | Separate assumption (A3); not part of LPC01A |

### Mathematical Status

The step from "phase single-valuedness on a periodic lattice" to "$q\Omega \in \mathbb{Z}$" is:

- **Not physics.** It is a purely topological consequence of the first homotopy group $\pi_1(S^1) = \mathbb{Z}$.
- **Not TRM/TQM-specific.** It applies to any system with a $U(1)$ phase on a periodic 1D lattice.
- **Not numerically testable.** It is a structural condition, not a quantitative prediction.
- **Trivial to state, trivial to prove.** The proof is: a continuous map $f: S^1 \to S^1$ has a winding number $p \in \mathbb{Z}$ given by $\frac{1}{2\pi}\oint d\phi$. On a discrete lattice of $q$ sites, $\oint d\phi = q\cdot\Delta\theta$, so $q\cdot\Delta\theta = 2\pi p$, hence $q\Omega = p$.

---

## 7. Claim Boundaries

| Claim | Status |
|:---|:---|
| LPC01A closes the Level-5 gap | **NOT CLAIMED** — this document only formulates the gap as a mathematical statement |
| LPC01A proves $q\Omega = p$ from lattice structure | **NOT CLAIMED** — the proof would require accepting M5 (phase single-valuedness) as an axiom or deriving it from TQM dynamics |
| LPC01A derives $p = q + m$ | **NOT CLAIMED** — that is assumption A3, separate from the topological argument |
| LPC01A proves $m = 3$ | **NOT CLAIMED** — that requires LPC02 + LPC03 |
| LPC01A introduces new physics | **NOT CLAIMED** — all assumptions are mathematical formalizations of concepts already in the repository |
| LPC01A is a theorem | **NOT CLAIMED** — it is a framework document identifying the precise mathematical statement that would close the gap |

**Accurate description:** "LPC01A identifies the Level-5 gap as a purely topological statement (phase single-valuedness on a periodic lattice → integer winding → $q\Omega = p$) that requires one OPEN node (phase single-valuedness) to be stated as an explicit condition. The gap is mathematical, not physical, and is trivial to close once the single-valuedness condition is accepted or derived."
