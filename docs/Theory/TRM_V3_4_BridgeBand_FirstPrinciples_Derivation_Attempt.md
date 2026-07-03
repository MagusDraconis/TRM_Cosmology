# TRM V3.4 — Bridge Band: First-Principles Derivation Attempt

## Scope

**Status: OPEN.** No first-principles derivation of the bridge band $\Omega \approx 1.16..1.19$ currently exists in the repository.

This document is a **research-program document**, not a proof, not a derivation, and not a claim. It maps what is known, identifies candidate structural sources within existing repository concepts, constructs a dependency tree, defines explicit failure conditions, and outlines a research program (BB01–BB04) to close or bound the gap.

---

## Current Knowledge

### Established Facts

| Fact | Source | Classification |
|:---|:---|:---|
| The bridge band is $\Omega \approx 1.16..1.19$, corresponding to $\gamma = 1/\Omega \approx 0.84..0.86$ | V2.2 effective low-acceleration boundary; CML01–CML08 | **EMPIRICAL** |
| The band is supported by an isolated rational collective mode-locking window | `TRM_Collective_Mode_Locking_BridgeScale.md` | **EMPIRICAL** |
| The band constrains $\Omega$, not $m$; it is independent of the ansatz $p = q + m$ | LPC02A | **ESTABLISHED** |
| Under the ansatz $\Omega = (q+m)/q$, the band-matching mode is $m = 3$ with $q \in \{16, 17, 18, 19\}$ | RBF05 | **EMPIRICAL FIT** |
| The ansatz $p = q + m$ is a definitional convention, not a derivation | LPC01C/D | **ESTABLISHED** |
| Topology ($\pi_1(S^1) = \mathbb{Z}$) gives $q\Omega = p$ with $p \in \mathbb{Z}$ but does not constrain the value of $\Omega$ | LPC01A | **ESTABLISHED** |
| The FP01–FP31 scaffold is valid given the band + ansatz, but does not derive the band | FP01–FP31 | **ESTABLISHED** |
| The DS01–DS41 diagnostics provide numerical phase-coherence evidence but do not constrain $\Omega$ | DS01–DS41 | **ESTABLISHED** |
| The three-constraint stack (phase closure + bridge-band + action/tick) selects $m = 3$; the bridge-band constraint is one of the three | RBF24–RBF26 | **ESTABLISHED** |
| The rational band question is acknowledged as the primary open problem: "Why does the rational band $\Omega \approx 1.16..1.19$ emerge?" | `TRM_Rational_Band_First_Principles.md` | **OPEN (documented)** |

### What Is NOT Known

- Why the band has the specific bounds $[1.16, 1.19]$
- Whether the band is unique or one of several possible bands
- Whether the band follows from TQM lattice structure or is contingent on V2.2 galactic parameters
- Whether the band depends on the ansatz choice $f(m) = m$

---

## Candidate Structural Sources

The following concepts exist in the repository and could, in principle, constrain or produce a rational band. Each is classified by relevance, known connection, and evidence level.

### 1. Lattice Synchronization (Kuramoto / Collective Mode-Locking)

| Field | Assessment |
|:---|:---|
| **Relevance** | **HIGH** — the band was originally identified through collective mode-locking diagnostics (CML01–CML08) |
| **Known connection** | CML diagnostics show that a rational collective locking band can produce bridge-scale values in the weak-field EL/Fermat window. The band is an emergent property of synchronized phase oscillators on a lattice. |
| **Current evidence level** | **MODERATE** — the band is observed in CML diagnostics, but the mechanism that selects the specific band bounds is not identified |
| **Open question** | Can the Kuramoto coupling strength $K$ and the lattice size $q$ produce bounded rational locking intervals, and can the specific interval $[1.16, 1.19]$ be derived from $K$ and $q$? |

### 2. Memory-Channel Structure ($\phi^2|\dot{\mu}|$)

| Field | Assessment |
|:---|:---|
| **Relevance** | **MODERATE** — the memory channel operates at the effective-action level, not directly at the lattice-phase level |
| **Known connection** | The memory term links the scalar time-rate field $\phi$ to transport observables. The bridge scale emerges at the intersection of the memory channel and the EL/Fermat bridge. |
| **Current evidence level** | **WEAK** — the memory channel constrains the effective dynamics but does not directly constrain the rational band |
| **Open question** | Can the memory-channel structure (specifically the $\phi^2$ scaling) produce a preferred $\Omega$ range through variational or energy-minimization principles? |

### 3. Theta-Vector Structure ($\Theta$, $O_5$, $\lambda_\Theta$)

| Field | Assessment |
|:---|:---|
| **Relevance** | **UNKNOWN** — the Theta-vector track operates at the coarse-grained effective-theory level |
| **Known connection** | The Theta chain (TQK01–TQK04, TOL01–TOL04) connects phase-lattice energy reduction to $O_5$ energy-gradient behavior. The response-scale mapping for $\lambda_\Theta$ may indirectly constrain the bridge scale. |
| **Current evidence level** | **UNKNOWN** — the connection between $\Theta$-sector parameters and the rational band has not been explored |
| **Open question** | Does the $\Theta$-sector response scale $\lambda_\Theta$ map to the bridge-band $\Omega$ range? |

### 4. Unified Action Structure

| Field | Assessment |
|:---|:---|
| **Relevance** | **MODERATE** — the minimal effective action from V3.2 may produce stationarity conditions that constrain $\Omega$ |
| **Known connection** | V3.2 (UA16–UA23) tests whether the minimal effective action can be motivated from TQM lattice/synchronization structure. The action/tick discriminator used in the three-constraint stack is linked to lattice-energy stationarity. |
| **Current evidence level** | **WEAK** — the action structure has been used to discriminate modes, not to derive the band |
| **Open question** | Can the Euler-Lagrange stationarity condition $\delta S = 0$ for the unified action produce a preferred $\Omega$ range as a solution, rather than as an input? |

### 5. Tick/Action Relations

| Field | Assessment |
|:---|:---|
| **Relevance** | **MODERATE** — tick/action consistency is the decisive discriminator in the three-constraint stack |
| **Known connection** | RBF13/RBF15 show that without action/tick, $m = 3$ uniqueness collapses. The action/tick discriminator is derived from a microscopic phase-lattice energy proxy (RBF23). |
| **Current evidence level** | **MODERATE** — the discriminator is effective, but its connection to the band bounds is not established |
| **Open question** | Does the phase-lattice energy proxy have a minimum or stationary point that selects a specific $\Omega$ range? |

### 6. Normalization Constraints

| Field | Assessment |
|:---|:---|
| **Relevance** | **LOW** — normalization is a scaffolding assumption, not a dynamical constraint |
| **Known connection** | RBF82 confirms that action-stationarity uses exclusively shared/global normalization. Per-family scaling is structurally rejected. |
| **Current evidence level** | **NONE** — normalization constraints do not select $\Omega$ values |
| **Open question** | Could a normalization condition (e.g., requiring the phase defect and action residual to share a common scale) indirectly constrain the admissible $\Omega$ range? |

---

## Dependency Tree

```
TRM/TQM lattice structure
    │
    ├── Lattice phase variables θ_a on sites             [DEFINED]
    │       │
    │       └── Collective synchronization (Kuramoto)     [EMPIRICAL — DS20, CML]
    │               │
    │               └── Rational locking candidates       [EMPIRICAL — CML06–CML08]
    │                       │
    │                       └── Ω ≈ 1.16..1.19 band      [EMPIRICAL — observed, not derived]
    │                               │
    │                               └── BB01–BB04 target   [OPEN — this research program]
    │
    ├── Memory channel  φ²|μdot|                          [EMPIRICAL — V3.1]
    │       │
    │       └── Effective action stationarity             [ASSUMED — A4]
    │               │
    │               └── Action/tick discriminator          [EMPIRICAL — RBF13, RBF23]
    │
    ├── Unified action / minimal lattice action           [ASSUMED — V3.2]
    │       │
    │       └── Stationarity condition δS = 0             [OPEN — could constrain Ω if derived]
    │
    └── Phase-lattice energy proxy                        [DERIVED — RBF23]
            │
            └── Energy minimization → preferred Ω?        [OPEN — unexplored connection]
```

### Classification Summary

| Level | Node | Classification |
|:---:|:---|:---|
| 0 | Lattice phase variables $\theta_a$ | DEFINED |
| 1 | Collective synchronization | EMPIRICAL (DS20, CML) |
| 2 | Rational locking candidates | EMPIRICAL (CML06–CML08) |
| 3 | Bridge band $\Omega \approx 1.16..1.19$ | **EMPIRICAL — the target of this program** |
| 4 | Memory channel $\phi^2\|\dot{\mu}\|$ | EMPIRICAL (V3.1) |
| 5 | Effective action stationarity | ASSUMED (A4) |
| 6 | Unified action / minimal lattice action | ASSUMED (V3.2) |
| 7 | Phase-lattice energy proxy | DERIVED (RBF23) |
| 8 | Energy minimization → preferred $\Omega$ | **OPEN** |

---

## Failure Conditions

The following are explicit ways the derivation attempt could fail. Any one of these would mean the bridge band remains an empirical input:

1. **The band remains empirical.** No structural mechanism within existing TRM/TQM concepts is found to produce a preferred $\Omega$ range. The band is accepted as a contingent input from V2.2 galactic dynamics.

2. **Multiple bands are equally possible.** The structural mechanism produces not one band but a family of bands (e.g., $\Omega \in [1.16, 1.19]$, $\Omega \in [1.20, 1.23]$, etc.) with no selection principle among them. The specific band observed in CML diagnostics is contingent, not unique.

3. **No unique structural source exists.** The band emerges from the intersection of multiple independent constraints (synchronization + memory + action), none of which individually produce it. The band is an emergent property of the full constraint stack and cannot be reduced to a single mechanism.

4. **Ansatz dependence cannot be removed.** The band bounds $[1.16, 1.19]$ depend on the ansatz choice $f(m) = m$ (LPC02A). Changing the ansatz shifts the band location. The band is a joint product of the empirical CML observation and the ansatz convention, not a structural invariant.

5. **The band is not rational.** The CML diagnostics identify a continuous locking window, not a discrete rational band. The rational interpretation ($20/17$, $7/6$, etc.) is a post-hoc fit, not a structural prediction.

---

## Research Program

### BB01 — Lattice Synchronization → Bounded Rational Band

| Field | Value |
|:---|:---|
| **Goal** | Determine whether Kuramoto-style collective synchronization on a TQM lattice can produce bounded rational locking intervals, and whether the coupling parameters can select the specific interval $[1.16, 1.19]$ |
| **Required evidence** | A mathematical relationship between the Kuramoto coupling strength $K$, the lattice size $q$, and the resulting locking band $\Omega \in [\Omega_{\text{min}}, \Omega_{\text{max}}]$ |
| **Success criterion** | The relationship produces $\Omega_{\text{min}} \approx 1.16$, $\Omega_{\text{max}} \approx 1.19$ from independently motivated $K$ and $q$ values |
| **Failure criterion** | No such relationship exists; the band bounds are determined by the initial conditions or noise level, not by the coupling structure |

### BB02 — Phase-Lattice Energy Minimization

| Field | Value |
|:---|:---|
| **Goal** | Investigate whether the phase-lattice energy proxy (RBF23: weighted order-defect, closure-defect, and transport-defect density) has a minimum or stationary point that selects a preferred $\Omega$ range |
| **Required evidence** | The energy functional $E(\Omega)$ evaluated over the admissible $(m, q)$ domain, showing a minimum in the neighborhood of $\Omega \approx 1.175$ |
| **Success criterion** | $E(\Omega)$ has a unique minimum whose location is determined by lattice parameters, not by the empirical band bounds |
| **Failure criterion** | $E(\Omega)$ is flat or has multiple minima; the bridge band is not an energy minimum |

### BB03 — Unified Action Stationarity

| Field | Value |
|:---|:---|
| **Goal** | Test whether the Euler-Lagrange stationarity condition $\delta S = 0$ for the unified effective action (V3.2) produces a preferred $\Omega$ range as a solution |
| **Required evidence** | The stationarity equation $\delta S/\delta\Omega = 0$ evaluated on the admissible $(m, q)$ domain, with the solution $\Omega^*$ in the neighborhood of the observed band |
| **Success criterion** | $\Omega^*$ is within $[1.16, 1.19]$ and is derived from action parameters, not fitted |
| **Failure criterion** | The stationarity condition does not constrain $\Omega$; $\Omega$ is a free parameter of the action |

### BB04 — Ansatz Independence Audit

| Field | Value |
|:---|:---|
| **Goal** | Determine whether the bridge band bounds $[1.16, 1.19]$ are stable under changes to the closure-family ansatz $f(m)$ |
| **Required evidence** | The band bounds recomputed for a representative set of alternative ansätze $f(m) \neq m$ (e.g., $f(m) = 2m$, $f(m) = m + c$) |
| **Success criterion** | The band bounds are invariant (or nearly invariant) under the tested ansatz changes — i.e., the band is a structural property, not an artifact of $f(m) = m$ |
| **Failure criterion** | The band bounds shift significantly under ansatz changes — i.e., the band is contingent on the ansatz convention |

---

## Claim Boundaries

The following claims are **explicitly excluded** for the V3.4 research program:

| Claim | Status |
|:---|:---|
| First-principles derivation of the bridge band achieved | **NOT CLAIMED** — this is a research program, not a result |
| $m = 3$ independently derived | **NOT CLAIMED** — family-index selection remains contingent on the band + ansatz |
| Bridge band reduced to a single structural mechanism | **NOT CLAIMED** — multiple candidate sources are under investigation |
| Full first-principles closure | **NOT CLAIMED** — assumptions A2–A6 remain |
| QM replacement | **NOT CLAIMED** |
| GR replacement | **NOT CLAIMED** |

**Accurate description:** "V3.4 is a structured research program investigating whether the empirically observed bridge band $\Omega \approx 1.16..1.19$ can be derived from existing TRM/TQM structural concepts. No derivation is claimed."

---

## Success Definition

A **genuine reduction of remaining assumptions** in V3.4 would be:

> At least one of the four research objectives (BB01–BB04) produces a **structural relationship** between independently motivated TRM/TQM parameters and the bridge-band bounds $[1.16, 1.19]$, such that the band is no longer an empirical input but a **derived consequence** of lattice synchronization, phase-lattice energy minimization, unified action stationarity, or ansatz-structural invariance.

If **none** of BB01–BB04 succeed, the bridge band remains an empirical input, and Assumption A3 is **unchanged**. This outcome is explicitly allowed — the research program is designed to investigate, not to guarantee success.

If BB04 succeeds but BB01–BB03 fail, the band is recognized as a **structural invariant** of the closure-family formalism (insensitive to the specific ansatz choice), which would partially reduce its contingency without fully deriving it.

If any of BB01–BB03 succeed, the corresponding assumption (A3) is elevated from EMPIRICAL to DERIVED, and the V3.4 assumption registry is updated accordingly.
