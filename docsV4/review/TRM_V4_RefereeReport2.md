================================================================================
REFEREE REPORT #2
================================================================================
Manuscript: "Bilocal Coupling Gravity: Covariant Action, Emergent EFT, and
            Nonlocal UV Completion from a Frozen Oscillator-Network Core"
Journal:    Phys. Rev. D (target)
Date:       2026-07-05
================================================================================

A. SUMMARY
──────────────────────────────────────────────────────────────────────────────

The paper presents a bilocal effective action S[K,g] for gravity, constructed
on top of a frozen discrete oscillator-network core (the TRM/TQM framework,
V3). The bilocal kernel K(x,y) = K₀/(1+x+bx²+x⁴) with x = d²/λ² is the
central object. From the action, the authors derive the effective field
equations (□K=0, ∇²K=−4παρ) in the flat-background limit, extract a metric
via the coincidence-limit Hessian, and match the resulting local EFT to a
higher-derivative gravity theory with coefficients determined by kernel
moments. A single free parameter b controls deviations from GR. The authors
claim: (1) a structurally derived G_eff formula, (2) a ghost-free nonlocal UV
completion (the spin-2 ghost of the local EFT truncation is an artifact),
(3) weak-field 1PN compatibility at b≈1.25, and (4) strong-field horizon
estimates at r_H≈2.275 GM within EHT bounds.

B. STRENGTHS
──────────────────────────────────────────────────────────────────────────────

1. The bilocal action construction (Section 4) is a legitimate theoretical
   advance over the earlier operational-PDE formulation. The kinetic term is
   the natural covariant bilocal generalization of (∂φ)², and the
   distributional computation of the coincidence-limit back-reaction
   (Phase 1B, cited) is clean work.

2. The identification of the spin-2 ghost as a local truncation artifact
   (Section 7) is credible. The Källén-Lehmann spectral positivity check
   (ρ(m²)≥0) is the correct diagnostic, and the argument that the ghost
   pole appears at k²~1/λ² — outside the EFT's validity domain — is
   standard EFT reasoning.

3. The paper is unusually honest about its limitations. The Scope &
   Limitations paragraph, the three-tier Scope of Derivation (Section 12.1),
   and the explicit classification of each result (POSTULATED/FORM INFERRED/
   APPROXIMATED/DERIVED) are model practices that more theory papers should
   adopt.

4. The parameter-economy argument (2 empirical anchors + 1 free parameter)
   is correctly counted and appropriately modest. The paper does not claim
   to predict G from first principles, which would be a fatal overclaim.

C. WEAKNESSES
──────────────────────────────────────────────────────────────────────────────

C.1 [CRITICAL] The V3 core-gravity connection remains a logical gap.

The paper states repeatedly that the V3 core "does not contain gravity"
and that V4 "does not modify the core." Yet the bilocal kernel K(x,y) is
introduced as the continuum limit of the discrete coupling matrix K_ij —
which IS in the core equations (dθ_i/dt = ω_i + Σ K_ij sin(θ_j−θ_i)).
The transition from "K_ij is a set of coupling constants in an oscillator
network" to "K(x,y) is a dynamical bilocal field with a covariant action"
is NOT derived — it is postulated. This is the central missing link. The
paper acknowledges that the kinetic term S_kin is POSTULATED, but it
does not explain why the oscillator network should produce precisely this
kinetic term in the continuum limit. A reviewer will ask: "Why does the
oscillator model imply a bilocal field theory rather than, say, a
hydrodynamic description?"

RECOMMENDATION: Add a paragraph in Section 2.2 or a new Section 2.5
explicitly stating: "The continuum limit K_ij → K(x,y) and the choice
of the covariant kinetic term (9) are postulates of the V4 program. They
are not derived from the oscillator dynamics. The justification is
aesthetic (simplest covariant bilocal form) and empirical (the resulting
EFT matches GR at 1PN)." This transforms a hidden gap into an explicit
assumption — which is how physics papers should handle such transitions.

C.2 [MAJOR] The β_PPN computation uses a scalar proxy.

The paper is honest that the full tensor mixing coefficients b₁–b₄ are
pending. However, the strong claim "weak-field 1PN compatibility is
achievable" appears in the Abstract and Section 8 without the qualifier
"within the scalar proxy" at every occurrence. The reader could
reasonably conclude that β_PPN has been computed, when in fact only a
scalar estimate has been obtained. The difference matters: the angular
mixing factors could shift b* by O(0.1) or change the sign structure.

RECOMMENDATION: In the Abstract, replace "achieves β_PPN ≈ 1" with
"achieves β_PPN ≈ 1 in the scalar proxy computation." In Section 8,
add a sentence: "The scalar proxy captures the sign and approximate
magnitude of the cubic coupling; the full tensor result may shift b*
but is not expected to eliminate the crossing, because all b_i share
the same sign from the radial integral."

C.3 [MAJOR] The strong-field horizon claim is over-positioned.

The scalar nonlinear ODE φ''+(2/r)φ' = β_ode(φ')² is a phenomenological
model. The paper acknowledges this but then states in multiple places
that it "predicts" or "yields" r_H ≈ 2.275 GM. A nonlinear scalar ODE
in one variable is NOT a strong-field solution — it is an illustrative
toy model. The connection to the bilocal EFT coefficients (Section 10,
"EFT connection" paragraph) helps, but the paper should stop calling
this a "prediction" and call it what it is: an "illustrative estimate."

RECOMMENDATION: Replace all instances of "predicts" or "yields" for the
strong-field results with "suggests, within the scalar approximation."
Already partially done in Section 10 — extend to Abstract and Conclusion.

C.4 [MAJOR] The Padé kernel form has an unjustified coefficient (a₃=0).

Equation (1): K(x) = K₀/(1+x+bx²+x⁴). The paper states a₃=0 is "assumed
(minimal choice)." A referee will note that there are four denominator
coefficients. Two are fixed by requirements (a₁=1 by normalization,
a₄=1 by stability). One is free (a₂=b). The fourth (a₃) is set to zero
by fiat. This is not "minimal" — it's an arbitrary choice that defines
the entire kernel family. If a₃ ≠ 0, the β(b,a₃) crossing surface shifts.
The paper should either: (a) derive a₃=0 from a symmetry, (b) show that
a₃ ≠ 0 does not qualitatively change results, or (c) treat a₃ as a
second free parameter and show that it is observationally constrained.

RECOMMENDATION: Add a paragraph in Section 2.2 demonstrating that
including a₃ ≠ 0 shifts b* continuously but does not remove the β=1
crossing (because β(b,a₃) is a continuous function on a connected
2-parameter domain). A one-paragraph argument plus a reference to the
numerical scan in the DeepCompletion documents would satisfy this.

C.5 [MINOR] Equation numbering is non-sequential.

Equations are numbered (1), (6), (7), (5), (6), (7), (9–15). This
occurred because sections were inserted and renumbered without
renumbering equations. While not a scientific issue, it will annoy
copy editors and suggests the manuscript was hastily assembled.

RECOMMENDATION: Renumber equations sequentially (1)–(15) throughout.

C.6 [MINOR] The a₃=0 justification is self-contradictory in Section 2.2.

The text says a₃=0 is "assumed (minimal choice; a₃ ≠ 0 is mathematically
viable but introduces no new physics)." This is contradictory: if a₃≠0
introduces no new physics, then a₃≠0 and a₃=0 are physically equivalent,
in which case the choice is not merely "minimal" but irrelevant. If a₃≠0
DOES affect physics (e.g., shifts b*), then it DOES introduce new physics.
The authors should pick one position.

RECOMMENDATION: Replace with: "a₃ = 0 is set to zero for simplicity;
a non-zero a₃ shifts the location of the β_PPN=1 crossing but does not
eliminate it. The a₃=0 choice is falsifiable: if future data require
a₃ ≠ 0, the framework accommodates it."

D. REQUIRED REVISIONS (by priority)
──────────────────────────────────────────────────────────────────────────────

1. [CRITICAL] Add explicit statement that K_ij → K(x,y) continuum limit
   and the choice of S_kin are postulates (Section 2).

2. [MAJOR] Qualify all β_PPN claims with "scalar proxy" in Abstract and
   Section 8.

3. [MAJOR] Replace "predicts/yields" for strong-field with "suggests within
   scalar approximation" in Abstract and Conclusion.

4. [MAJOR] Add justification for a₃=0 (show a₃≠0 does not break the
   framework).

5. [MINOR] Renumber equations sequentially.

6. [MINOR] Fix a₃=0 justification text (remove contradiction).

E. ADDITIONAL COMMENTS
──────────────────────────────────────────────────────────────────────────────

The paper would benefit from a diagram showing the K → B → g chain with
labels indicating the classification of each step (POSTULATED / DERIVED /
APPROXIMATED). This would make the derivation status visually clear.

The version lineage table and the repeated emphasis on "V3.4 is an internal
refinement" feels defensive. One clear sentence would suffice. The table
can stay but could be moved to an appendix.

The reviewer Q&A (Section 12.5) is an unusual but effective device. It
anticipates criticism without being argumentative. I recommend keeping it.

F. OVERALL EVALUATION
──────────────────────────────────────────────────────────────────────────────

The paper reports genuine theoretical progress: a covariant bilocal action
construction, a clean ghost-resolution argument, and an honest parameter
inventory. The weaknesses are primarily matters of presentation and
qualification — the scientific content is sound within its stated scope.

The critical issue (C.1) requires a one-paragraph clarification, not new
calculations. The major issues (C.2–C.4) require wording changes, not new
physics. The minor issues are copy-editing.

The paper does not overclaim its results. The Scope & Limitations section
is exemplary. The honesty about what is derived vs. assumed vs. approximated
is a strength that distinguishes this manuscript from many theory papers.

RECOMMENDATION: MINOR REVISION

The paper should be accepted after the authors address items C.1 through
C.6. No new calculations are required. The revised manuscript will be
suitable for publication in Phys. Rev. D.

================================================================================
END OF REPORT
================================================================================
