# BB27N — Execution Plan for External Validation Using Neural Oscillations

## Context

BB23 established the minimal TRM/TQM model:

    dθ_i/dt = ω_i + Σ_j K_ij · f(θ_i − θ_j)

BB24 identified the physical core:
- intrinsic frequencies ω_i
- coupling K_ij
- topology

BB26 derived collective behavior as a fixed-point / stability phenomenon.

BB27 begins external validation.
The next target class is neural oscillations.

---

## Goal

Test whether real neural oscillation systems exhibit the same
structural behavior predicted by the minimal TRM/TQM model:

- emergent collective frequency Ω*
- synchronization threshold
- phase locking / coherence
- topology dependence
- stable frequency bands

---

## Why Neural Oscillations

Neural systems provide:
- measurable local oscillator frequencies
- tunable or state-dependent effective coupling
- explicit network topology
- directly observable coherence bands

They are more structurally complex than laser arrays,
but still belong to the same general coupled-oscillator class.

---

## Target Systems

Start with the following neural classes:

1. cortical gamma oscillations
2. hippocampal theta rhythms
3. alpha-band thalamocortical activity
4. large-scale resting-state / default-mode oscillations

---

## Validation Questions

1. Does a collective frequency Ω* emerge?
2. Is Ω* related to the mean / dominant local frequency distribution?
3. Is there a threshold-like transition in coherence as effective coupling increases?
4. Does topology alter stability and synchrony as predicted?
5. Do stable collective frequency bands appear as locking windows?

---

## Data Requirements

Collect or extract from literature:

- local oscillation frequencies (or band centers)
- global/coherent oscillation frequency
- coherence / phase-locking metric
- effective connectivity or coupling proxy
- topology / network structure
- uncertainty / variability window

Possible observables:
- EEG / MEG coherence
- LFP band power
- PLV / phase-locking value
- graph-theoretic connectivity metrics

---

## Phase A — Literature and Dataset Collection

Search for papers / datasets on:

- neural synchronization with controllable coupling
- EEG/MEG phase locking
- cortical or hippocampal oscillation coherence
- topology effects on oscillatory stability

Minimum target:
- at least 3 studies with local frequencies + coherent band
- at least 2 studies with coupling/connectivity manipulation
- at least 1 study with topology/network comparison

---

## Phase B — Structural Mapping

Map experimental concepts to TRM/TQM concepts:

| Neural concept | TRM/TQM concept |
|----------------|-----------------|
| local rhythm / cell assembly frequency | ω_i |
| synaptic / effective functional coupling | K_ij |
| anatomical / functional network graph | topology |
| coherent population rhythm | Ω* |
| phase coherence / PLV / band-locking | synchronization metric |

---

## Phase C — Tests

### Test C1 — Centroid / collective frequency condition
Check whether:

    Ω* ≈ mean(ω_i)
or
    Ω* ≈ weighted mean of dominant local rhythms

within reported uncertainty.

### Test C2 — Threshold condition
Check whether coherence rises rapidly once:

    K_eff / σ_ω

crosses a critical range.

### Test C3 — Topology dependence
Check whether changing connectivity or lesioning the network modifies:
- coherence
- stability
- band width
- locking persistence

### Test C4 — Stable band condition
Check whether neural systems show:
- persistent narrow collective frequency windows
- robustness against perturbation
- topology-dependent band stability

---

## Analysis Outputs

Create tables:

1. neural_mapping.csv
   columns:
   study_id, system_type, mean_omega, sigma_omega, K_proxy, Omega_star, coherence_metric, topology_type

2. centroid_test.csv
   columns:
   study_id, Omega_star, mean_omega, weighted_mean_omega, difference

3. threshold_test.csv
   columns:
   study_id, K_over_sigma, coherence_metric, threshold_supported

4. topology_test.csv
   columns:
   study_id, topology_type, coherence, stability_window, topology_supported

---

## Success Criteria

The neural validation is considered positive if:

1. collective neural frequencies are observed
2. Ω* tracks mean or weighted mean local frequencies
3. coherence rises above a threshold in effective K/σ_ω
4. topology measurably alters stability
5. stable coherent frequency bands are reported

---

## Interpretation

If all criteria pass:

    TRM/TQM captures a real physical oscillator class
    extending beyond optical arrays into neural systems.

If some criteria pass:

    the model is structurally relevant but incomplete;
    amplitude, stochasticity, or adaptive coupling may need to be added.

If most fail:

    the bridge-band / fixed-point mechanism may be restricted
    to narrower physical classes.

---

## Final Question

Do neural oscillations validate:

- the centroid condition
- the threshold condition
- the topology dependence
- the fixed-point interpretation of stable frequency bands?

---

## Next Step

If positive:
    compare neural and laser validation side by side

If negative:
    identify which missing neural physics
    (noise, plasticity, amplitude dynamics, nonstationarity)
    breaks the minimal mapping
