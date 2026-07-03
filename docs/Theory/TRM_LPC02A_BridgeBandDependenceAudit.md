# TRM LPC02A — Bridge-Band Dependence Audit

## Scope

LPC01D established that topology only gives $q\Omega = p$ and does not distinguish $p = q + m$ from $p = q + f(m)$. This document traces, through existing repository content only, how strongly the bridge-band result $\Omega \approx 1.16..1.19$ depends on the specific choice $f(m) = m$. No new theory, no numerics, no derivations.

---

## 1. Which Repository Results Use $p = q + m$ Directly?

Every result from RBF05 onward uses $p = q + m$ (or the algebraically equivalent $\Omega = (q+m)/q$). The ansatz is baked into:

| Result | File | How $p = q + m$ Is Used |
|:---|:---|:---|
| RBF05 | `CollectiveModeLockingTests.cs:488` | Tests whether $\Omega = (q+3)/q$ reproduces the observed bridge band |
| RBF06–RBF23 | `CollectiveModeLockingTests.cs` | All mode-locking diagnostics compute $\Omega = (q+m)/q$ for $m = 1..5$ |
| RBF38 | `CollectiveModeLockingTests.cs` | Derives $q_{\text{Core}} = \{16, 17, 18\}$ from $\Omega = (q+3)/q$ with $\Omega \in [1.16, 1.19]$ |
| FP01 | `M3ExactQCoreProof.cs` | Exact-rational derivation of $q_{\text{Core}}$ using $\Omega = (q+m)/q$ |
| FP02–FP31 | `TRM.FormalProofs/` | All phase-defect and energy-margin proofs use $\Omega = (q+m)/q$ |
| FP22–FP31 | `M3ContinuousDomainProofs.cs` | Continuous asymptotic proofs use $\Omega = (q+m)/q$ |
| DS14, DS30, DS34 | `DoubleSlitPhaseCoherenceTests.cs` | Map $m = 3$ to DS visibility via $\delta = |m-3|/3$ — depends on $f(m) = m$ giving zero defect at $m = 3$ |

### Results that do NOT use $p = q + m$

| Result | File | What It Uses Instead |
|:---|:---|:---|
| CML01–CML08 | `CollectiveModeLockingTests.cs` | Rational band $\Omega \approx 1.16..1.19$ from collective mode-locking — independent of the ansatz |
| EL01–EL17 | Euler-Lagrange tests | Effective low-acceleration boundary $g_{\text{eff}} = \bar{g} + \sqrt{\bar{g} \cdot a_0}$ — purely V2.2 physics |
| DS01–DS41 (except DS14/30/34) | `DoubleSlitPhaseCoherenceTests.cs` | Phase-coherence diagnostics — independent of $q_{\text{Core}}$ and the ansatz |
| LPC01A | `TRM_LPC01A_TopologicalClosureFramework.md` | Pure topology: $q\Omega = p$, $p \in \mathbb{Z}$ — independent of $f(m)$ |

---

## 2. Which Results Use Only $q\Omega = p$ and Would Survive a Different $f(m)$?

The topological closure $q\Omega = p$ is independent of the ansatz. The following survive unchanged:

| Result | Survival Under $f(m) \neq m$ | Reason |
|:---|:---:|:---|
| $q\Omega = p$ with $p \in \mathbb{Z}$ | **Unchanged** | Pure topology (LPC01A) |
| $\Omega = p/q$ rational | **Unchanged** | Algebra from $q\Omega = p$ |
| Bridge band $\Omega \approx 1.16..1.19$ | **Unchanged** | From V2.2/CML, independent of the ansatz |
| $m$ indexes closure families | **Unchanged** | $m$ is just a label; any bijection $\tilde{m} = h(m)$ relabels the same families |
| The existence of a $q_{\text{Core}}$ set | **Unchanged** | Solving $\Omega = 1 + f(m)/q \in [1.16, 1.19]$ always produces some $q$-range |
| DS01–DS41 (non-bridge) | **Unchanged** | Phase-coherence diagnostics don't depend on $q_{\text{Core}}$ |

---

## 3. Would FP01–FP31 Remain Valid Under $p = q + f(m)$?

### Substitution analysis

Replace every occurrence of $\Omega = (q+m)/q$ with $\Omega = 1 + f(m)/q$:

| FP Block | Impact | Classification | Reason |
|:---|:---|:---|:---|
| **FP01** ($q_{\text{Core}}$ derivation) | **Modified** | $q_{\text{Core}}$ values change | Solving $1 + f(3)/q \in [1.16, 1.19]$ gives $q \in [f(3)/0.19, f(3)/0.16]$. For $f(3) = 3$: $\{16, 17, 18\}$. For $f(3) = 6$: $\{32, \dots, 37\}$. The derivation method is unchanged; the output values shift. |
| **FP02** (phase defect minimization) | **Modified** | Defect formula changes | $\delta = |q\Omega - p|/3 = |q(1+f(m)/q) - (q+f(m))|/3 = |f(m) - f(m)|/3 = 0$. Wait — the defect is identically zero for ALL $m$, not just $m = 3$! If $p = q + f(m)$ and $\Omega = 1 + f(m)/q$, then $q\Omega - p = q + f(m) - q - f(m) = 0$ for every $m$. The phase-closure defect becomes **trivially zero** for all modes. FP02 would be **invalidated** — it no longer selects a unique mode. |
| **FP03** (finite domain uniqueness) | **Invalidated** | All modes are admissible | If phase defect is zero for all $m$, the phase constraint loses all discriminatory power. Mode selection would depend entirely on bridge-band occupancy and action/tick. |
| **FP04–FP06** (energy margin) | **Invalidated** | Energy margins collapse | With zero phase defect for all modes, the energy-margin computation changes fundamentally. |
| **FP07–FP09** (symbolic export) | **Invalidated** | Inequalities no longer hold | $E_3 < E_2$ depends on $m = 3$ having the unique zero defect. |
| **FP10–FP21** (Lean proofs) | **Invalidated** | Lemma statements change | `lemma_phase_defect_m3_zero` becomes trivial for ALL $m$, not a distinctive property of $m = 3$. |
| **FP22–FP31** (continuous proofs) | **Invalidated** | Asymptotic arguments change | $\varepsilon$-phase bounds depend on $|m-3|/3$; under general $f$, this becomes $|f(m) - f(3)|/3$. |
| **DS14/DS30/DS34** ($q_{\text{Core}}$ bridge) | **Modified** | $q_{\text{Core}}$ and visibility mapping change | $\delta = |m-3|/3$ depends on $f(m) = m$. Under general $f$, the defect would be $|f(m) - f(3)|/3$, which may not be zero for the bridge-band mode. |

### Critical finding

The substitution $p = q + f(m)$ **invalidates** the entire FP01–FP31 scaffold unless $f$ satisfies:

$$f(m) - f(3) = m - 3 \quad \text{(up to scaling)}$$

This means $f(m)$ must be **affine**: $f(m) = m + c$ (or $f(m) = a \cdot m + b$ with $a = 1$). Only affine transformations with unit slope preserve the phase-defect structure on which the entire FP scaffold is built.

However, $f(m) = m + c$ is just a **relabeling** of modes: $m = 3$ under $f(m) = m$ becomes $\tilde{m} = 3 + c$ under $f(m) = m + c$. The unique admissible mode shifts from $3$ to $3 + c$, but the structural selection is identical — only the label changes.

---

## 4. Earliest Point Where $f(m) = m$ Becomes Essential

Tracing the dependency chain:

```
V2.2 / CML01–CML08
  →   Bridge band Ω ≈ 1.16..1.19  observed           [independent of f(m)]
  →   "Why this band?"                                [LPC02, OPEN]

RBF05 (earliest ansatz appearance)
  →   Ω = (q+3)/q  proposed as closure candidate      [f(m)=m is convenient]
  →   q ∈ {16, 17, 18, 19} reproduces the band        [empirical match]

RBF06–RBF23
  →   Mode-locking diagnostics use Ω = (q+m)/q        [f(m)=m is assumed]

FP01
  →   qCore = {16, 17, 18} derived from Ω = (q+3)/q   [f(m)=m is baked in]

FP02
  →   Phase defect δ = |m-3|/3 uses f(m)=m            [f(m)=m is ESSENTIAL]
  →   THIS IS THE POINT OF NO RETURN
```

**The point where $f(m) = m$ becomes essential rather than convenient is FP02.**

At RBF05, $f(m) = m$ is **convenient** — it produces a clean integer $m = 3$ mapping to the bridge band. A different $f$ would produce a different $m$ label for the same $\Omega$ values. The physics (which rational $\Omega$ values are in the band) is unchanged; only the labeling changes.

At FP02, $f(m) = m$ becomes **structurally baked in** — the phase defect formula $\delta = |m-3|/3$ relies on $f(m) = m$ to give zero defect at $m = 3$ and nonzero defect at $m \neq 3$. Changing $f$ would require redefining the defect formula to $\delta = |f(m) - f(3)|/3$, which preserves the zero-at-bridge-mode property but changes the numerical values for competitor modes.

---

## 5. Dependency Map

```
                    ┌─────────────────────────────┐
                    │  V2.2: effective low-a       │
                    │  boundary → γ ≈ 0.85         │
                    │  [INDEPENDENT of f(m)]        │
                    └─────────────┬───────────────┘
                                  │
                    ┌─────────────▼───────────────┐
                    │  CML01–CML08: rational       │
                    │  locking band Ω ≈ 1.16..1.19 │
                    │  [INDEPENDENT of f(m)]        │
                    └─────────────┬───────────────┘
                                  │
                    ┌─────────────▼───────────────┐
                    │  RBF05: Ω = (q+3)/q          │
                    │  reproduces the band         │
                    │  [f(m)=m is CONVENIENT]       │
                    └─────────────┬───────────────┘
                                  │
                    ┌─────────────▼───────────────┐
                    │  FP01: qCore = {16,17,18}    │
                    │  from Ω = (q+3)/q            │
                    │  [f(m)=m is ASSUMED]          │
                    └─────────────┬───────────────┘
                                  │
                    ┌─────────────▼───────────────┐
                    │  FP02: δ = |m-3|/3           │
                    │  zero defect at m=3          │
                    │  [f(m)=m is ESSENTIAL]        │
                    ════════════════════════════════
                    │  POINT OF NO RETURN           │
                    ════════════════════════════════
                    └─────────────┬───────────────┘
                                  │
                    ┌─────────────▼───────────────┐
                    │  FP03–FP31: entire scaffold   │
                    │  depends on δ = |m-3|/3      │
                    │  [f(m)=m is NON-NEGOTIABLE]   │
                    └─────────────────────────────┘
```

### Legend

| Classification | Meaning |
|:---|:---|
| **INDEPENDENT** | Result does not use $f(m)$ at all |
| **CONVENIENT** | $f(m) = m$ produces clean labels; any $f$ works with relabeling |
| **ASSUMED** | $f(m) = m$ is used but the result's structure survives under affine $f$ |
| **ESSENTIAL** | The result's logical structure depends on $f(m) = m$ |
| **NON-NEGOTIABLE** | Built on the essential dependency |

---

## 6. Final Answer

**$f(m) = m$ is:**

| Property | Verdict | Evidence |
|:---|:---:|:---|
| Structurally necessary? | **No** — up to FP01 | Any affine $f(m) = m + c$ produces the same structural selection with relabeled modes. |
| Structurally necessary? | **Yes** — from FP02 onward | The phase defect $\delta = |m-3|/3$ hard-codes $f(m) = m$. |
| Historically contingent? | **Yes** | RBF05 chose $f(m) = m$ because it gives $m = 3$ as a clean integer mapping to the bridge band. |
| Empirically motivated? | **Yes** | The bridge band was observed first (CML, V2.2); the ansatz was chosen to fit it. |
| The simplest parametrization? | **Yes** | LPC01D confirmed $f(m) = m$ is the simplest member of the separable class. |
| Replaceable without destroying the framework? | **Partially** | Affine $f(m) = m + c$ works (just relabels modes). Non-affine $f$ invalidates FP02–FP31. |

**In short:** $f(m) = m$ is the historically convenient ansatz that happened to fit the bridge-band data. It became structurally essential at FP02, where it was baked into the phase-defect formula. It could be replaced by any affine $f(m) = m + c$ (just relabeling modes), but not by a genuinely nonlinear $f$, which would destroy the entire formal proof scaffold.
