# TRM DS35–DS37 Audit Track: Negative Controls, Anti-Fit, and Parameter Sensitivity

## Scope
This note documents the audit track (DS35–DS37), which verifies the robustness of the DS01–DS34 diagnostic suite. Rather than extending into new physics domains, these tests probe whether the diagnostics are:
1. **Falsifiable:** Negative controls (broken physics assumptions) must correctly fail or abstain.
2. **Free of over-fitting:** The shared coherence-to-visibility mapping must not rely on per-case parameter tuning.
3. **Sensitive to parameters:** A non-trivial pass/fail boundary must exist across parameter sweeps — not all cases should pass.

---

## 1) DS35: Negative Controls — Broken Physics Must Fail

- **Concept:** Three negative controls deliberately break key physical assumptions:
  - **NC1 (Wrong carrier $k$):** Replace $k = 2\pi/\lambda$ with $k/3$, producing incorrect fringe spacing.
  - **NC2 (Incoherent summation):** Replace coherent amplitude superposition $|\psi_1 + \psi_2|^2$ with particle-like incoherent sum $|\psi_1|^2 + |\psi_2|^2$ — no interference cross terms.
  - **NC3 (Invalid geometry):** Set slit separation $d \to 0$, eliminating path-length difference and thus interference.

- **Results:**
  - **Status:** **PASS**

  | Negative Control | Correct $V$ | Broken $V$ | Verdict |
  |:---|:---:|:---:|:---|
  | NC1: Wrong $k$ ($k/3$) | 1.0000 | 0.4772 | **FAIL** (correctly) |
  | NC2: Incoherent sum | 1.0000 | 0.0000 | **FAIL** (correctly) |
  | NC3: Slit distance $d \to 0$ | 1.0000 | 0.0000 | **FAIL** (correctly) |

  - **Summary:** 3/3 negative controls correctly fail. None pass spuriously.
  - **Interpretation:** All three broken-physics scenarios are detected by the visibility diagnostic. NC1 (wrong $k$) reduces visibility by ~52% due to mismatched fringe spacing. NC2 (incoherent sum) collapses visibility to exactly zero since $|\psi_1|^2 = |\psi_2|^2 = 1/2$ everywhere — no interference pattern exists. NC3 ($d = 0$) eliminates path-length difference, making both paths identical and destroying interference. No negative control passes the diagnostic criteria — the suite is **falsifiable**.
  - **Diagnostic only** — no claim of exhaustive falsifiability proof.

---

## 2) DS36: No Per-Case Tuning — Anti-Fit Audit

- **Concept:** This audit verifies that the coherence-to-visibility mapping does not rely on per-case parameter fitting. A shared coherence value $c = 0.80$ is applied to multi-slit systems with $N = 3, 5, 10$. The resulting visibilities are compared. Then, per-$N$ coherence values are fitted to force all visibilities to match the $N = 3$ target, and the deviation from the shared value is measured.

- **Results:**
  - **Status:** **PASS**

  | $N$ | Shared $V$ ($c = 0.80$) | Fitted $c$ for $V = 0.8317$ | Deviation from shared |
  |:---:|:---:|:---:|:---:|
  | 3 | 0.8317 | 0.8000 | 0.0000 |
  | 5 | 0.9085 | 0.6650 | 0.1350 |
  | 10 | 0.9514 | 0.5021 | 0.2979 |

  - **Shared mapping behavior:** Visibility increases with $N$ for fixed coherence ($0.83 \to 0.91 \to 0.95$). This is physically correct — more slits produce sharper grating peaks (higher visibility) at the same coherence level.
  - **Anti-fit detection:** Forcing identical visibility across $N$ would require substantially different coherence inputs ($0.80$ vs $0.67$ vs $0.50$). The maximum fitted-coherence deviation is $0.298$, far exceeding a plausible noise floor. Per-case tuning is therefore **detectable** — the shared mapping produces genuinely $N$-dependent visibility, not a fitted constant.
  - **Average visibility deviation from target** (shared mapping): $0.066$, confirming the model predicts different visibility for different $N$, as expected from grating physics.
  - **Interpretation:** The diagnostic mapping is not over-fitted. The coherence parameter is a genuine shared input; per-$N$ fitting would produce visibly different coherence values that would be detected in an audit. The model's $N$-dependent visibility output is a physical prediction, not a tuning artifact.
  - **Diagnostic only** — no claim of universal over-fitting immunity.

---

## 3) DS37: Parameter Sensitivity — Pass/Fail Boundary Mapping

- **Concept:** This audit sweeps three key parameter axes to map where diagnostics transition between pass, abstain, and fail regimes:
  1. **Decoherence $\lambda$:** Sweep $\lambda \in [0, 1]$ (multi-slit visibility, $N = 5$)
  2. **Phase noise $\sigma$:** Sweep $\sigma \in [0, 3]$ in Kuramoto model ($M = 5$ oscillators)
  3. **Geometry perturbation:** Scale slit distance by factor $\in [0.5, 5.0]$, measure visibility deviation from baseline

- **Results:**
  - **Status:** **PASS**

  **Sweep 1 — Decoherence $\lambda$:**
  | $\lambda$ | Coherence $c$ | Visibility | Regime |
  |:---:|:---:|:---:|:---|
  | 0.0 | 1.0 | 0.9994 | **PASS** (coherent) |
  | 0.2 | 0.8 | 0.9085 | **PASS** (coherent) |
  | 0.4 | 0.6 | 0.7889 | **PASS** (coherent) |
  | 0.6 | 0.4 | 0.6244 | ABSTAIN (partial) |
  | 0.8 | 0.2 | 0.3842 | ABSTAIN (partial) |
  | 1.0 | 0.0 | 0.0000 | **FAIL** (incoherent) |

  **Sweep 2 — Phase noise $\sigma$:**
  | $\sigma$ | Order $R$ | Regime |
  |:---:|:---:|:---|
  | 0.0 | 0.9998 | **PASS** (locked) |
  | 0.5 | 0.7567 | ABSTAIN (partial) |
  | 1.0 | 0.5371 | ABSTAIN (partial) |
  | 1.5 | 0.4393 | ABSTAIN (partial) |
  | 2.0 | 0.3874 | **FAIL** (chaotic) |
  | 3.0 | 0.3966 | **FAIL** (chaotic) |

  **Sweep 3 — Geometry perturbation:**
  | Scale factor | $d$ | Visibility | $\Delta V$ | Regime |
  |:---:|:---:|:---:|:---:|:---|
  | 0.5 | 1.0 | 0.9463 | 0.0537 | **PASS** (stable) |
  | 0.8 | 1.6 | 1.0000 | 0.0000 | **PASS** (stable) |
  | 1.0 | 2.0 | 1.0000 | 0.0000 | **PASS** (stable) |
  | 1.5 | 3.0 | 0.9999 | 0.0001 | **PASS** (stable) |
  | 2.0 | 4.0 | 0.9996 | 0.0004 | **PASS** (stable) |
  | 5.0 | 10.0 | 0.9991 | 0.0009 | **PASS** (stable) |

  - **Summary:** **PASS: 10 | ABSTAIN: 5 | FAIL: 3** (18 total test points)
  - **Interpretation:** A non-trivial pass/fail boundary exists:
    - **Decoherence** produces a clear transition: coherent ($\lambda \leq 0.4$) → partial ($0.6 \leq \lambda \leq 0.8$) → incoherent ($\lambda = 1.0$).
    - **Phase noise** shows a smooth degradation: locked ($\sigma = 0$) → partial ($0.5 \leq \sigma \leq 1.5$) → chaotic ($\sigma \geq 2.0$).
    - **Geometry perturbation** is highly robust: visibility remains within 5.4% of baseline even for 5× slit-distance scaling. This is expected — double-slit interference geometry is scale-invariant (only the ratio $d / L$ matters for fringe spacing, not absolute distances).
  - Multiple regimes (pass, abstain, fail) are populated across sweeps — the diagnostic suite is **not** trivially passing. Robustness to geometry is a genuine physical property of interference, not a test weakness.
  - **Diagnostic only** — parameter sweeps are finite; boundaries are approximate.

---

## Current Status & Claim Boundaries

The **DS01–DS37** diagnostic and audit suite is now complete and all 37 tests are passing. The framework covers:
- 30 physics diagnostics (DS01–DS30): interference, decoherence, wave-packets, topology, relativity proxies, entanglement proxies, Born-rule consistency, path integrals, action stationarity, TQM bridging
- 4 extended diagnostics (DS31–DS34): spin splitting, Bell correlation, entropic coherence, full qCore sweep
- 3 audit/meta-diagnostics (DS35–DS37): negative controls, anti-fit checks, parameter sensitivity

**Claim Boundaries (unchanged):**
- Diagnostic/candidate only; not a replacement for QM or GR.
- Not a theorem-level proof.
- No claims against standard quantum mechanics.
- No GR replacement.
- No numerology.

---

## Next Direction (DS38+)

Future work may explore:
1. **DS38:** Cross-validation across independent code paths (e.g., re-implement key diagnostics in a different numerical scheme).
2. **DS39:** Statistical power analysis — minimum sample sizes for reliable regime classification.
3. **DS40:** Edge-case stress tests (extreme parameters: $k \to 0$, $d \to \infty$, $\sigma \to \infty$).
4. **DS41:** Reproducibility audit — verify deterministic seeded RNG produces bit-identical results across runs.
