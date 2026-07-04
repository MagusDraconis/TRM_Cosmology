# BB03C — Emergent Omega Prototype

**Status:** Research prototype design — no implementation, no derivation claim.
**Parent program:** V3.4 Bridge-Band Origin (BB01–BB04).
**Predecessors:** BB03A (directionality audit — Ω always input), BB03B (reconstruction audit — infrastructure partially exists).

---

## Scope

This document defines a **prototype-level research test**, not a derivation.

Explicit scope boundaries:

- **Prototype only.** The goal is to test directional reversibility: can Ω be extracted from dynamics instead of imposed externally?
- **No derivation claim.** Even if the prototype succeeds, it does not constitute a first-principles derivation of the bridge band.
- **No new theory.** Only existing repository components are used or minimally reconfigured.
- **Objective:** Determine whether the existing codebase contains the raw material for emergent Ω, by running a self-organizing variant of `SimulateModeLock` and attempting to extract Ω* from the resulting phase trajectories.

---

## Existing Infrastructure

The following components, already present in the repository, make this prototype possible without introducing new machinery:

| Component | Location | Role in prototype |
|:---|:---|:---|
| `SimulateModeLock` | `CollectiveModeLockingTests.cs:7930` | Full Kuramoto dynamics with per-oscillator phase tracking. Contains intrinsic frequencies, coupling, and a tunable external drive weight. |
| Intrinsic frequency array `omegas[i]` | `SimulateModeLock` lines 7935–7939 | `omegas[i] = 1.0 + 0.05·sin(angle) + 0.03·cos(2·angle)`. Provides the natural frequency distribution from which an emergent Ω* would arise in self-organizing mode. |
| `CollectiveWeight` parameter | `ModeLockConfig` record, line 8998 | Setting `CollectiveWeight = 0` removes the external drive term `W·sin(Ωt − φ_i)`, converting the model from externally driven to self-organizing. |
| Order parameter computation | `ComputeOrderParameter`, line 8057 | Computes Kuramoto R = |⟨e^{iφ}⟩|. Used to detect whether locking has occurred and when the system has settled. |
| `ModeLockResult` record | Line 9021 | Return type from `SimulateModeLock`. Currently holds `CollectiveOmega` (input), `MeanOrder`, `ClosureResidual`, `ModeLockScore`. Would need extension for emergent Ω*. |
| `BuildNoCadencePriorConfig` | Line 7922 | Already provides a config with `CadenceScoreWeight = 0.0`, removing the 20/17 cadence prior. Demonstrates the pattern for modifying `ModeLockConfig` to remove priors. |
| Energy proxy formula | `BuildModeFamilyFromLatticeProxy`, line 8072 | `E = (0.50·δ_o² + 0.35·δ_c² + 0.15·δ_t²) / Ω`. Algebraically compatible with any Ω value — no structural barrier to accepting an emergent Ω*. |

### Pre-existing evidence that locking can occur without external drive

- **DS20** (`DoubleSlitPhaseCoherenceTests.cs:1160`): A standard Kuramoto model without external drive demonstrates synchronized lock (R ≥ 0.85) under strong coupling (K = 5.0, σ = 0.0). This confirms that self-organizing synchronization is physically achievable in the repository's numerical framework.
- **Limitation:** DS20 has no intrinsic frequencies (all ω_i = 0) and does not extract the locked frequency. It provides a **proof of concept** for self-organizing dynamics, not an Ω-extraction pipeline.

---

## Minimal Prototype Design

The prototype consists of four steps, each using only existing machinery or minimal reconfiguration:

### Step 1: Run SimulateModeLock in self-organizing mode

```
config = BuildNoCadencePriorConfig() with { CollectiveWeight = 0.0 }
```

- The external drive term `W·sin(Ωt − φ_i)` is zeroed.
- Only intrinsic frequencies and Kuramoto coupling remain: `dφ_i/dt = ω_i + K·Σ sin(φ_j − φ_i)`.
- Since the external drive is removed, `collectiveOmega` is **irrelevant to the dynamics** — it only affects the (now-absent) drive term. It can be set to any value (e.g., 1.0) or the function signature can be bypassed.

**Open question:** Should `SimulateModeLock` be called at all, or should a new helper be extracted? The function currently requires a `collectiveOmega` parameter. The simplest path is to pass a dummy value and set `CollectiveWeight = 0`, since the dummy value has no dynamical effect when the drive is disabled.

### Step 2: Estimate emergent Ω* from phase evolution

After transient settling (e.g., `SettleSteps = 600` as in the default config), extract the average phase velocity:

```
Ω* = (1/N) · Σ_i (φ_i(t_final) − φ_i(t_settle)) / (t_final − t_settle)
```

This is the oscillator-averaged unwrapped phase slope over the post-settling window.

**Alternative candidate methods** — see Candidate Extraction Methods below.

### Step 3: Use lock criteria to detect valid synchronized states

- `MeanOrder` (computed from `ComputeOrderParameter`) must exceed a threshold (e.g., R ≥ 0.85 for synchronized lock, following DS20 convention).
- If `MeanOrder` is below threshold, the system did not lock — Ω* is not meaningful.
- Optional: check stability of Ω* across multiple time windows within the post-settling period. A stable Ω* (low variance) indicates a genuine locked state.

### Step 4: Compare extracted Ω* to reference values

| Reference | Expected value | Meaning |
|:---|:---|:---|
| Intrinsic frequency mean | ≈ 1.0 (mean of `omegas[i]`) | Standard Kuramoto theory predicts Ω* → mean(ω_i) when locked. This is the null hypothesis. |
| Bridge band | [1.16, 1.19] | The target. If Ω* lands here, the prototype provides positive evidence for emergent band selection. |
| CML optimum (externally driven) | ≈ 1.176 (20/17) | The current argmax over `BuildOmegaGrid` with external drive. Useful for comparison. |

**Critical note:** If Ω* ≈ 1.0 (the intrinsic mean), this is NOT a failure of the extraction method — it is the expected result from Kuramoto theory for the current ω_i distribution. It would mean that the bridge band cannot emerge from dynamics alone without re-centering the intrinsic frequency distribution.

---

## Candidate Extraction Methods

Four methods for extracting Ω* from phase trajectories. None are implemented yet. They differ in robustness to noise and compatibility with existing code.

### Method A: Average phase velocity ⟨dφ_i/dt⟩

```
Ω* = mean_i [ (φ_i(t_final) − φ_i(t_settle)) / Δt ]
```

| Property | Assessment |
|:---|:---|
| Simplest | **Yes.** Direct computation from existing `phases[]` array. |
| Most robust to noise | **Moderate.** Noise at individual time steps averages out over the integration window. Phase-wrapping issues are avoided by using the unwrapped difference, not instantaneous derivative. |
| Compatible with existing code | **Yes.** Requires only storing `phases[]` at the settle boundary and at the final step. The difference is a single arithmetic operation. |
| Risk | If oscillators are not fully locked, individual dφ_i/dt may differ — the mean is still well-defined but the interpretation as "common frequency" weakens. |

### Method B: Mean unwrapped phase slope

```
Ω* = linear_regression_slope( φ_i(t) for t ∈ [t_settle, t_final] )
```

| Property | Assessment |
|:---|:---|
| Simplest | No — requires linear regression over the post-settling window. |
| Most robust to noise | **Yes.** Regression over many time points suppresses noise better than endpoint differences. |
| Compatible with existing code | **Requires modification.** Phases must be stored at each post-settling step (or the slope accumulated online). Currently, phases are overwritten each step. |
| Risk | Over-engineering for a prototype. Method A provides similar information with less code. |

### Method C: Order parameter phase angle derivative

```
Φ(t) = arg( Σ_i e^{iφ_i(t)} )
Ω* = dΦ/dt over post-settling window
```

| Property | Assessment |
|:---|:---|
| Simplest | No — requires computing the collective phase angle Φ(t) and its derivative. |
| Most robust to noise | **Moderate.** The order parameter phase filters individual oscillator noise through the ensemble average. |
| Compatible with existing code | **Partially.** `ComputeOrderParameter` already computes Σ e^{iφ_i}. The phase angle of the complex order parameter would need to be extracted and tracked. |
| Risk | Phase wrapping of Φ(t) requires unwrapping. Adds complexity without clear benefit over Method A for a prototype. |

### Method D: Oscillator-average frequency after settling

```
Ω* = mean_i [ mean_{t > t_settle} ( dφ_i/dt ) ]
```

| Property | Assessment |
|:---|:---|
| Simplest | No — requires computing instantaneous derivatives at each step. |
| Most robust to noise | **Lowest.** Instantaneous derivatives amplify step-to-step noise. |
| Compatible with existing code | **Requires modification.** `dφ_i/dt` must be stored at each post-settling step. |
| Risk | Noise amplification makes this the least reliable method. |

### Recommendation

**Method A** (average phase velocity) for the initial prototype. It requires the fewest code changes, is least sensitive to noise, and is directly interpretable as the mean locked frequency. If Method A succeeds, Method B or C can be added for cross-validation.

---

## Required Code Touchpoints

### `SimulateModeLock` (line 7930)

**What changes:** Record `phases[]` at `step = SettleSteps` and at `step = Steps − 1`. Store both snapshots.

**Why:** Method A requires φ_i at two time points. Currently, `phases[]` is overwritten each step and not preserved.

**Risk:** Minimal. Storing two extra arrays of length `CellCount` (~20 doubles) has negligible memory cost.

### `ModeLockResult` record (line 9021)

**What changes:** Add fields:

```csharp
double? EmergentOmega,          // extracted Ω* (null if not applicable)
double EmergentOmegaStability   // variance or stability metric
```

**Why:** The prototype test needs to inspect the extracted Ω* alongside existing sync outputs.

**Risk:** Adding nullable fields to a record is backward-compatible. All existing code that constructs `ModeLockResult` will need the new fields, but they can default to `null` / `0.0`.

### New helper: `ExtractEmergentOmega` (new, near line 8060)

**What:** A static method that takes phase snapshots and time interval, returns Ω* and a stability metric.

**Why:** Isolates the extraction logic for testability and clarity.

**Risk:** New code, but small (~15 lines) and purely computational.

### New prototype test (new, near existing CML/RBF tests)

**What:** A `[Fact]` test following the CML naming convention, e.g.:

```
CML09_SelfOrganizingModeLock_Should_Extract_EmergentOmega
```

or an RBF-series test:

```
RBF83_EmergentOmega_Should_Be_Extractable_From_SelfOrganizingDynamics
```

**Why:** The prototype needs a single, focused test to exercise the extraction pipeline.

**Risk:** Standard test addition — no modification to existing tests.

### Configuration

| Config parameter | Value | Reason |
|:---|:---|:---|
| `CollectiveWeight` | 0.0 | Removes external drive |
| `CouplingKappa` | 0.10 (default) or higher | Stronger coupling may be needed for self-organized lock |
| `SettleSteps` | 600 (default) | Conservative settling period |
| `Steps` | 1200+ | Need sufficient post-settling data for stable Ω* |
| `CadenceScoreWeight` | 0.0 | Remove 20/17 cadence prior |
| `OrderScoreWeight` | 0.55 | From `BuildNoCadencePriorConfig` |
| `AlignmentScoreWeight` | 0.45 | From `BuildNoCadencePriorConfig` |

---

## Success Criteria

The prototype provides **useful evidence** (not proof) if ALL of the following hold:

1. **Ω* can be extracted reproducibly.** Running the same configuration multiple times (with deterministic seeding if applicable) yields the same Ω* within a small tolerance.

2. **Ω* stabilizes after settling.** The extracted value does not drift when the post-settling window is varied (e.g., using the second half vs. the last quarter of steps).

3. **Ω* is not the imposed input.** Since `CollectiveWeight = 0`, the dummy `collectiveOmega` parameter has no dynamical effect. Ω* should be independent of the dummy value passed to `SimulateModeLock`. This must be verified explicitly: run with dummy Ω = 1.0, 1.5, 2.0 and confirm Ω* is unchanged.

4. **Ω* can be fed into the existing energy proxy.** The extracted Ω* can be passed to a variant of `BuildModeFamilyFromLatticeProxy` (or an equivalent computation) without structural changes to the energy formula.

### What success does NOT mean

- Success does **not** mean Ω* lands in the bridge band [1.16, 1.19].
- Success does **not** mean the bridge band is derived from first principles.
- Success means only that the directionality arrow `dynamics → Ω*` is operationally realizable within the existing codebase — a necessary precondition for any future derivational path.

---

## Failure Criteria

The following outcomes are **explicitly allowed** and do not invalidate the broader research program:

### Failure Mode 1: No stable Ω* extractable

**Symptom:** Ω* varies widely across runs or across post-settling windows, or `MeanOrder` never exceeds the lock threshold.

**Interpretation:** The self-organizing dynamics do not produce a well-defined locked state under the tested parameters. The coupling strength K or the intrinsic frequency spread may need tuning.

**Impact on BB program:** Low. The prototype tests one specific configuration. Failure does not rule out emergent Ω under different parameter regimes.

### Failure Mode 2: Ω* collapses to intrinsic mean ≈ 1.0

**Symptom:** Ω* ≈ 1.0 consistently, with high lock quality (R ≥ 0.85).

**Interpretation:** The dynamics are working correctly — Kuramoto theory predicts Ω* → mean(ω_i) for a symmetric intrinsic frequency distribution. The bridge band [1.16, 1.19] cannot emerge from dynamics with the current ω_i centered at 1.0.

**Impact on BB program:** **Significant.** This would mean the intrinsic frequency distribution must be re-centered near 1.175 for the bridge band to emerge from dynamics. This is a structural constraint, not a failure of the method. It would shift the research question from "can Ω emerge?" to "why are intrinsic frequencies centered at ~1.175?" — a different (and potentially deeper) question.

### Failure Mode 3: Ω* is contaminated by residual drive artifacts

**Symptom:** Ω* depends on the dummy `collectiveOmega` parameter even though `CollectiveWeight = 0`.

**Interpretation:** A bug or unexpected coupling path. The prototype code must be audited for unintended Ω dependencies.

**Impact on BB program:** Low. This is a code-quality issue, not a conceptual one.

---

## Dependency Map

```
SimulateModeLock(CollectiveWeight=0)
    │
    ├── phases[] tracked at settle + final         [NEW: save snapshots]
    │
    ├── ExtractEmergentOmega(phases_settle, phases_final, Δt)  [NEW: helper]
    │       │
    │       └── Ω* (emergent frequency)
    │
    ├── ComputeOrderParameter(phases)              [EXISTS]
    │       │
    │       └── MeanOrder → lock detection
    │
    └── ModeLockResult                             [MODIFIED: add EmergentOmega]
            │
            └── prototype test asserts:
                    Ω* reproducible
                    Ω* ≠ dummy input Ω
                    Ω* stable across windows
                    Ω* compatible with energy proxy
```

---

## Relationship to Prior Audits

| Audit | Finding | Relevance to BB03C prototype |
|:---|:---|:---|
| BB01A | Synchronization provides descriptive, not derivational, path to bridge band | The prototype does not attempt to derive the band — only to test whether Ω can be an output. |
| BB02A | Energy minimization selects m=3, not Ω directly | Even if Ω* emerges, the energy proxy still discriminates modes (m). The prototype addresses the orthogonal question of Ω origin. |
| BB01B/BB02B | Sync + energy do not jointly derive Ω — both treat Ω as input | The prototype closes this specific gap by making Ω an output of the sync dynamics. |
| BB03A | Ω is universally input — no directionality reversal exists | The prototype is the first deliberate test of directionality reversal. |
| BB03B | Infrastructure for emergent Ω partially exists | The prototype activates the dormant infrastructure identified in BB03B. |

---

## Non-Goals

- **Do not** re-center intrinsic frequencies to 1.175. The prototype must work with the existing `omegas[i]` distribution. If that produces Ω* ≈ 1.0, this is an allowed failure outcome.
- **Do not** add a feedback loop from energy proxy back to Ω. The prototype tests one-directional extraction: dynamics → Ω*. Feedback (Ω* → energy → adjusted dynamics) is a separate and more complex question.
- **Do not** claim the bridge band is derived if Ω* ≈ 1.175 happens to emerge. That would require an explanation of WHY the intrinsic frequencies are centered there — a question outside this prototype's scope.
- **Do not** modify existing tests. The prototype is a new, standalone test.

---

## Summary

| Question | Answer |
|:---|:---|
| Can the directionality be reversed using existing code? | **To be tested.** The prototype sets `CollectiveWeight = 0` and extracts Ω* from the resulting phase trajectories. |
| What is the simplest extraction method? | **Method A:** Average phase velocity between settle and final steps. |
| What is the expected null result? | Ω* ≈ 1.0 (the intrinsic frequency mean), since standard Kuramoto theory predicts locking at the mean of ω_i. |
| What would count as positive evidence? | Ω* is reproducible, stable, independent of dummy input Ω, and compatible with the energy proxy — regardless of its numerical value. |
| Does success imply the bridge band is derived? | **No.** The prototype only tests whether Ω can be an output. Deriving the specific band [1.16, 1.19] would require additional steps beyond this prototype. |
