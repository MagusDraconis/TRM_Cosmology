# TRM LPC02B — Meaning of $m$ Audit

## Scope

LPC01C established that $p = q + m$ originated as a closure-family ansatz. LPC02A established that the bridge band does not derive it. This document asks: **what does $m$ actually represent?** The audit traverses the entire repository, classifying every documented meaning of $m$ and answering six specific questions.

---

## 1. Semantic Classification of $m$ Across the Repository

| Document | Statement | Semantic Classification |
|:---|:---|:---|
| RBF05 (earliest) | `p - q = m` | **Bookkeeping variable** — $m$ is defined as the difference $p - q$, not given independent meaning |
| `TRM_M3_Closure_Derivation_Attempt.md` §3 | "$m$ measures the discrete closure offset/defect between synchronized cycle count $p$ and baseline denominator $q$" | **Winding offset** — $m = p - q$, the excess winding beyond one full cycle |
| `TRM_M3_Closure_Derivation_Attempt.md` §3 | "larger $m$ generally increases closure offset" | **Winding offset** (comparative) |
| `TRM_M3_Closure_Derivation_Attempt.md` §3 | "$m$ is currently an effective closure-order index, not yet a microscopic topological invariant" | **Mode index** — a label, not a fundamental quantity |
| `TRM_M3_Closure_Theorem_Path.md` §3 | "$m$ is a discrete closure-order index (effective defect order)" | **Mode index** — explicitly "effective," not fundamental |
| `TRM_M3_Closure_First_Principles.md` §3 | "$m^\star = \min\{m \mid \exists q: (m,q) \in \mathcal{A}\}$" | **Bookkeeping variable** — used as an index into an admissible set |
| `TRM_M3_First_Principles_Gap_Audit.md` | "$m=3$ as a strongly constrained closure-order candidate" | **Mode index** — a candidate label |
| `M3ExactDefinitions.cs` | `PCompatible(q, targetShift) => q + targetShift` | **Bookkeeping variable** — `targetShift` = 3 = $m$, hard-coded |
| FP02 | $\delta = \|m-3\|/3$ | **Mode index** — $m=3$ is the reference zero; others are deviations |
| `TRM_TQM_LatticePhaseClosure_Assumption_Discharge_Plan.md` | "discrete set of collective phase modes indexed by integer $m$" | **Mode index** — $m$ indexes modes |
| LPC01D | "$p - q = m$ is independent of $q$ — the mode index has the same meaning at every lattice scale" | **Winding offset** — a $q$-independent property of the mode |
| DS14/DS30/DS34 | $\delta(m) = \|m-3\|/3$ | **Mode index** — used as a numerical label in visibility mapping |
| CML/RBF collective mode-locking | $m$ scanned from 1 to 5 | **Mode index** — a scan parameter |

---

## 2. Frequency Count by Semantic Category

| Semantic Category | Count | Description |
|:---|:---:|:---|
| **Mode index** | 10 | $m$ labels discrete collective phase modes; an enumeration parameter |
| **Winding offset** | 4 | $m = p - q$, the excess winding beyond one baseline cycle per $q$ steps |
| **Bookkeeping variable** | 4 | $m$ is defined by a relation ($p - q$ or $p = q + m$) rather than having independent meaning |
| Physical quantity | **0** | $m$ is never claimed to be a physical observable with units |
| Topological invariant | **0** | Explicitly denied: "not yet a microscopic topological invariant" |
| Fitting parameter | **0** | $m$ is never adjusted to fit data; $m=3$ is fixed by the bridge-band match |
| Fundamental constant | **0** | Never claimed |

---

## 3. Answers to the Six Questions

### Q1: What is the earliest meaning assigned to $m$?

**Bookkeeping variable.** In RBF05 (the earliest appearance), $m$ is defined by the relation $p - q = m$. It is not given a physical interpretation — it is simply the integer difference between two other integers. The formula $\Omega = (q+m)/q$ is then tested against the independently observed bridge band, and $m = 3$ is found to reproduce it.

### Q2: Does any document derive $m$ from another quantity?

**No.** Every document either:
- **Defines** $m$ algebraically ($m = p - q$ or $p = q + m$), or
- **Scans** $m$ as a parameter ($m = 1..5$), or
- **Uses** $m$ as a label in a formula ($\delta = |m-3|/3$).

No document derives $m$ from lattice dynamics, synchronization structure, or any more fundamental quantity. The value $m = 3$ is selected by empirical bridge-band match, not by derivation.

### Q3: Is $m$ observable?

**No.** $m$ is not a physical observable. What is observable (through the bridge-band mapping) is the rational ratio $\Omega = (q+m)/q \approx 1.16..1.19$, which constrains the combination $m/q$. The individual values $m = 3$ and $q \in \{16, 17, 18\}$ are jointly constrained, not separately observed. Any affine rescaling $m \to am + b$, $q \to aq$ produces the same $\Omega$ and is observationally indistinguishable.

### Q4: Is $m$ treated as an invariant?

**Partially.** The Closure Derivation Attempt explicitly states $m$ is "not yet a microscopic topological invariant." RBF68 shows that the phase-defect form $|q\Omega - p|/3$ is invariant under simultaneous scaling of $(q, m, \text{targetShift})$, which means the **defect** is invariant but the **label** $m$ is not. The LPC01D separability argument shows $p - q = m$ is $q$-independent — this is a structural property of the parametrization, not of $m$ itself.

### Q5: Is $m$ merely the label of closure families?

**Yes, and this is the most accurate description.** The Closure Derivation Attempt calls it an "effective closure-order index." The Closure First Principles uses it as an index into an admissible set. The LPC discharge plan defines modes as "indexed by integer $m$." The value $m = 3$ labels the family whose members ($\Omega = (q+3)/q$ for various $q$) fall in the bridge band. A different labeling convention would assign a different integer to the same family.

### Q6: Which statements remain true if $m$ is renamed to $k$?

**All of them.** Renaming $m \to k$ changes no mathematical content. The statements "$\Omega = (q+k)/q$," "$\delta = |k-3|/3$," and "$k = 3$ is the closure-order index" are structurally identical. The integer $3$ is special because it labels the bridge-band family, not because the symbol $m$ carries intrinsic meaning.

---

## 4. Semantic Map of $m$

```
┌─────────────────────────────────────────────────────────────┐
│                        WHAT m IS                             │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  PRIMARY: Mode index / bookkeeping variable          │    │
│  │  "m labels one member of a discrete family of        │    │
│  │   collective phase modes indexed by integers."       │    │
│  │  Status: DEFINED (by convention)                     │    │
│  └─────────────────────────────────────────────────────┘    │
│                          │                                  │
│                          ▼                                  │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  SECONDARY: Winding offset                           │    │
│  │  "m = p - q, the excess winding beyond one full      │    │
│  │   phase cycle per q lattice steps."                  │    │
│  │  Status: DEFINED (algebraic identity under ansatz)   │    │
│  └─────────────────────────────────────────────────────┘    │
│                          │                                  │
│                          ▼                                  │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  TERTIARY: Closure-defect proxy                      │    │
│  │  "δ(m) = |m-3|/3 measures deviation from the         │    │
│  │   bridge-band mode m=3."                             │    │
│  │  Status: DEFINED (under f(m)=m convention)           │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                      WHAT m IS NOT                           │
│                                                             │
│  ✗ Physical observable (no units, not directly measurable)  │
│  ✗ Topological invariant (explicitly denied in docs)        │
│  ✗ Derived quantity (always defined, never derived)          │
│  ✗ Fitting parameter (fixed at m=3 by bridge-band match)    │
│  ✗ Fundamental constant (labeling convention, not constant) │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. Classification

| Property of $m$ | Classification | Justification |
|:---|:---|:---|
| $m \in \mathbb{Z}_{>0}$ indexes closure families | **DEFINED** | Convention established at RBF05 |
| $m = p - q$ (winding offset) | **DEFINED** | Algebraic identity under $p = q + m$ |
| $m = 3$ maps to the bridge band | **MOTIVATED** | Empirical match (RBF05); not derived |
| $m$ is a topological invariant | **FALSE** | Explicitly denied in Closure Derivation Attempt §3 |
| $m$ is a physical observable | **FALSE** | Never claimed; not directly measurable |
| $m$ is a fundamental constant | **FALSE** | An indexing convention, not a constant |
| $\delta = \|m-3\|/3$ measures defect | **DEFINED** | Under $f(m) = m$ convention |
| $m = 3$ is the unique zero-defect mode | **DEFINED** | Proven in FP01–FP31 under the ansatz |

---

## 6. Conclusion

**$m$ is an indexing convention, not a fundamental quantity.**

Its three layers of meaning are:
1. **Primary (definitional):** $m$ labels discrete collective phase modes ($m \in \mathbb{Z}_{>0}$).
2. **Secondary (algebraic):** Under the ansatz $p = q + m$, $m$ is the excess winding $p - q$.
3. **Tertiary (diagnostic):** $\delta(m) = |m-3|/3$ quantifies deviation from the bridge-band mode.

All three layers are **conventions**, not derivations. The value $m = 3$ is special only because it labels the family whose rational ratios $\Omega = (q+3)/q$ fall in the independently observed bridge band. Renaming the modes (any bijection on $\mathbb{Z}_{>0}$) would preserve all structural relationships — only the label of the bridge-band family would change.
