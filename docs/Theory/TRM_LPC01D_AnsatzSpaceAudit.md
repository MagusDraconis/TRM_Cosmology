# TRM LPC01D — Ansatz Space Audit

## Scope

LPC01A isolated the topological closure path: periodic lattice + phase on $S^1$ + closed loop + single-valuedness → integer winding $p$ → $q\Omega = p$. LPC01C established that $p = q + m$ was introduced as a closure ansatz with empirical motivation — not as a derivation.

This document asks: **is $p = q + m$ mathematically distinguished, or is it merely the simplest parametrization?** The analysis uses only algebra and topology. No physics, no numerics, no bridge-band fitting.

---

## 1. Current Closure Form

```
(1)  qΩ = p                              [topological: p ∈ ℤ from LPC01A]
(2)  p = q + m                            [ansatz: LPC01C]
(3)  Ω = (q + m) / q = 1 + m/q           [algebra from (1)+(2)]
```

Key observation: equation (1) constrains $p$ to be an **integer**. It does **not** constrain $p$ to have any specific relationship to $q$ or $m$. The integer $p$ is the winding number — its value is determined by the collective phase dynamics, not by topology.

---

## 2. Alternative Ansatz Space

### 2.1 $p = q + f(m)$

| Property | Value |
|:---|:---|
| **Form** | $p = q + f(m)$ where $f: \mathbb{Z} \to \mathbb{Z}$ is any integer-valued function |
| **Ω** | $\Omega = 1 + f(m)/q$ |
| **Classification** | **Genuine generalization** of the current ansatz ($f(m) = m$) |
| **Stronger/weaker?** | **Weaker** — introduces an arbitrary function $f$ with no constraints |
| **Excluded by topology?** | **No** — $p$ remains an integer for any integer-valued $f$ |
| **Excluded by algebra?** | **No** — $q\Omega = q + f(m) \in \mathbb{Z}$ ✓ |

**Examples:**
- $f(m) = m$ (current ansatz)
- $f(m) = 2m$ → $\Omega = 1 + 2m/q$
- $f(m) = m^2$ → $\Omega = 1 + m^2/q$
- $f(m) = \text{constant}$ → $\Omega = 1 + c/q$, no $m$-dependence

**Implication:** If $f(m)$ is unconstrained, the closure family index $m$ can be arbitrarily rescaled or remapped without violating topology. The choice $f(m) = m$ is not forced.

---

### 2.2 $p = kq + m$

| Property | Value |
|:---|:---|
| **Form** | $p = kq + m$ where $k \in \mathbb{Z}$ is a base winding multiplier |
| **Ω** | $\Omega = k + m/q$ |
| **Classification** | **Genuine generalization** — adds a free integer parameter $k$ |
| **Stronger/weaker?** | **Stronger** than current ansatz ($k = 1$) — allows $k \neq 1$ |
| **Excluded by topology?** | **No** — $p$ remains integer |
| **Excluded by algebra?** | **No** |

**Physical interpretation:** $k = 1$ means "one full phase cycle per $q$ lattice steps at baseline." $k = 2$ would mean two full cycles. The current ansatz implicitly sets $k = 1$ — this is a physical assumption about the baseline winding rate, not a topological necessity.

**Implication:** If $k = 1$ is not forced, then $\Omega = 1 + m/q$ is not unique — $\Omega = 2 + m/q$, $\Omega = 3 + m/q$, etc. are equally valid closure families from a topological perspective.

---

### 2.3 $p = q + a \cdot m + b$

| Property | Value |
|:---|:---|
| **Form** | $p = q + am + b$ where $a \in \mathbb{Z}_{>0}$, $b \in \mathbb{Z}$ |
| **Ω** | $\Omega = 1 + (am + b)/q$ |
| **Classification** | **Equivalent reparameterization** of $p = q + f(m)$ with $f(m) = am + b$ |
| **Stronger/weaker?** | **Neither** — affine shift and scale of $m$ just relabels the mode index |
| **Excluded by topology?** | **No** |
| **Excluded by algebra?** | **No** |

**Implication:** The specific values $a = 1$, $b = 0$ are notation choices. Any affine transformation $m \to am + b$ produces the same set of rational $\Omega$ values (just with different mode labels). The choice $a = 1$ sets the "unit" of $m$; $b = 0$ centers the mode index at the baseline.

---

### 2.4 $p = q + m^n$

| Property | Value |
|:---|:---|
| **Form** | $p = q + m^n$ where $n \in \mathbb{Z}_{>0}$ |
| **Ω** | $\Omega = 1 + m^n/q$ |
| **Classification** | **Genuine generalization** — nonlinear in $m$ |
| **Stronger/weaker?** | **Weaker** — introduces nonlinearity with no structural motivation |
| **Excluded by topology?** | **No** |
| **Excluded by algebra?** | **No** |

**Implication:** For $n > 1$, the mode spacing becomes nonlinear ($m = 2$ and $m = 3$ produce non-uniform $\Omega$ increments). No topological principle forbids this. The preference for $n = 1$ (linear spacing) is a simplicity assumption, not a mathematical necessity.

---

### 2.5 $p = g(q, m)$

| Property | Value |
|:---|:---|
| **Form** | $p = g(q, m)$ where $g: \mathbb{Z}^2 \to \mathbb{Z}$ is any integer-valued function of two variables |
| **Ω** | $\Omega = g(q, m)/q$ |
| **Classification** | **Fully general** — encompasses all integer-valued winding functions |
| **Stronger/weaker?** | **Weaker** than all above — places no constraints on the $q$-dependence |
| **Excluded by topology?** | **No** — as long as $g$ is integer-valued |
| **Excluded by algebra?** | **No** |

**Implication:** Topology alone places **zero constraints** on the functional form of $p(q, m)$ beyond $p \in \mathbb{Z}$. Any integer-valued function $g(q, m)$ produces a valid rational closure family $\Omega = g/q$. The current ansatz $g(q, m) = q + m$ is one point in an infinite-dimensional space of possibilities.

---

## 3. Structural Invariants

Which properties survive across the ansatz space?

| Property | $p = q + m$ | $p = q + f(m)$ | $p = kq + m$ | $p = g(q,m)$ |
|:---|:---:|:---:|:---:|:---:|
| Integer winding $p \in \mathbb{Z}$ | ✓ | ✓ | ✓ | ✓ (by construction) |
| Rational $\Omega = p/q$ | ✓ | ✓ | ✓ | ✓ |
| $m$ indexes closure families | ✓ | ✓ | ✓ | Depends on $g$ |
| $\Omega \to 1$ as $q \to \infty$ | ✓ | ✓ (if $f$ bounded) | ✗ ($\Omega \to k$) | Depends on $g$ |
| Linear mode spacing | ✓ | Only if $f$ is linear | ✓ | No |
| Unique $m = 3$ selection* | ✓ | Depends on $f$ | Depends on $k$ | Depends on $g$ |

\* Under the full three-constraint stack with bridge-band $\Omega \approx 1.16..1.19$ and $q_{\text{Core}} = \{16, 17, 18\}$.

**Key observation:** The only **topologically invariant** property is $p \in \mathbb{Z}$. All other properties (asymptotic behavior, mode spacing, $m = 3$ selection) depend on the specific choice of $g(q, m)$ and on the bridge-band constraint (LPC02) — they are not consequences of topology alone.

---

## 4. Minimality Analysis

Is $p = q + m$ uniquely selected, structurally preferred, merely simplest, or arbitrary notation?

| Criterion | Assessment |
|:---|:---|
| **Uniquely selected by topology?** | **No.** Topology only requires $p \in \mathbb{Z}$. |
| **Uniquely selected by algebra?** | **No.** Any $p = g(q, m)$ with integer $g$ is algebraically valid. |
| **Structurally preferred?** | **Yes, under one criterion.** $p = q + m$ is the **unique** form that satisfies both: (a) $p - q$ is independent of $q$, and (b) $p - q$ is exactly $m$ (not a function of $m$). Criterion (a) means the excess winding depends only on the mode, not on the lattice scale — a natural separability condition. Criterion (b) is notation. |
| **Merely simplest?** | **Yes.** Among all forms $p = q + f(m)$, the choice $f(m) = m$ is the simplest (identity function). Among forms $p = kq + m$, the choice $k = 1$ is the simplest (single baseline cycle). |
| **Arbitrary notation?** | **Partially.** $m$ could be replaced by any bijection $\tilde{m} = h(m)$ without changing the set of $\Omega$ values. The specific labeling $m = 1, 2, 3, \dots$ is arbitrary. |

### The "Separability" Argument

The current ansatz $p = q + m$ satisfies:

$$\frac{\partial}{\partial q}(p - q) = 0$$

The excess winding $p - q = m$ is **independent of $q$**. This means the mode index $m$ has the same meaning at every lattice scale — it is a property of the mode, not of the scale at which it is measured.

This is the **only structural property that distinguishes** $p = q + m$ from more general forms like $p = g(q, m)$ where the excess winding depends on both $q$ and $m$. However, it does **not** distinguish $p = q + m$ from $p = q + f(m)$ — any $q$-independent excess winding satisfies separability.

**Conclusion:** $p = q + m$ is **minimal** in the sense that it is the simplest member of the class of separable forms ($p - q$ independent of $q$). It is not uniquely selected by topology or algebra.

---

## 5. Counterexamples — Alternative Closure Families Compatible with $q\Omega = p$

The following families are **not excluded by topology alone**:

| # | Family | $\Omega$ | $q_{\text{Core}}$ for $m = 3$, $\Omega \in [1.16, 1.19]$ | Topologically Valid? |
|:---:|:---|:---|:---|:---:|
| CE1 | $p = q + 2m$ | $1 + 2m/q$ | $q \in [32, 38]$ (different $q$-range) | ✓ |
| CE2 | $p = 2q + m$ | $2 + m/q$ | $\Omega \notin [1.16, 1.19]$ for any $q$ | ✓ (but fails bridge-band) |
| CE3 | $p = q + m^2$ | $1 + m^2/q$ | $q \in [53, 63]$ for $m = 3$ | ✓ |
| CE4 | $p = q + c$ (constant) | $1 + c/q$ | No $m$-dependence; single family | ✓ |
| CE5 | $p = q + m$ (current) | $1 + m/q$ | $q \in [16, 19]$ for $m = 3$ | ✓ |

**Key observation:** CE1 and CE3 produce $q_{\text{Core}}$ values **different** from $\{16, 17, 18\}$ for $m = 3$. The specific $q_{\text{Core}} = \{16, 17, 18\}$ is an artifact of the **joint choice** of $f(m) = m$ (ansatz) and the bridge band $\Omega \approx 1.16..1.19$ (empirical). Changing $f(m)$ shifts $q_{\text{Core}}$ without changing the topology.

CE2 produces $\Omega$ values outside the bridge band entirely — it is excluded by the bridge-band constraint (LPC02), not by topology.

---

## 6. Classification

| Component | Classification | Justification |
|:---|:---|:---|
| $q\Omega = p$ with $p \in \mathbb{Z}$ | **DEFINED** | Topological consequence of LPC01A (phase on $S^1$ + closed loop) |
| $p = q + m$ (specific form) | **ASSUMED** | Closure ansatz; not derived from topology or algebra |
| $m \in \mathbb{Z}$ indexes closure families | **DEFINED** | Follows from $p \in \mathbb{Z}$ and $q \in \mathbb{Z}$ |
| $p - q$ independent of $q$ (separability) | **ASSUMED** | Structural simplicity condition; not topologically forced |
| $f(m) = m$ (identity, not general $f$) | **ASSUMED** | Simplest parametrization; alternatives exist |
| $k = 1$ (single baseline cycle) | **ASSUMED** | Physical assumption about baseline winding rate |
| $\Omega = (q + m)/q$ | **DEFINED** | Algebra from $q\Omega = p$ and $p = q + m$ |

---

## 7. Final Question

**Based only on algebra and topology:**

**Can topology distinguish $p = q + m$ from $p = q + f(m)$ ?**

### Answer: **NO**

### Justification

1. Topology (LPC01A) constrains $p$ to be an integer: $p \in \mathbb{Z}$. This is the **only** constraint from the closed-loop + single-valuedness argument.

2. The integer $p$ can be decomposed as $p = q + f(m)$ for **any** integer-valued function $f$, because for any integer $p$ and any integer $q$, the difference $p - q$ is an integer that can be expressed as $f(m)$ for some function $f$ and some mode label $m$.

3. Conversely, for any integer-valued $f$ and any integers $q, m$, the sum $q + f(m)$ is an integer. So **every** member of the ansatz space $p = q + f(m)$ satisfies the topological constraint $p \in \mathbb{Z}$.

4. The topological argument does not constrain the **value** of $p$ — it only constrains its **type** (integer). The specific integer is determined by the collective phase dynamics (how many cycles the phase advances per lattice traversal), which is a physical question, not a topological one.

5. The choice $f(m) = m$ is the simplest instance of a general class. It is mathematically legitimate but not mathematically forced. Topology is silent on whether $m = 3$ or $\tilde{m} = h(3)$ labels the bridge-band mode — the label is notation.

**In short:** Topology tells us that $p$ is an integer. It does not tell us **which** integer.
