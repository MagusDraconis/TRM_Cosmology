using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_14;

[Trait("Category","V5_14"),Trait("Category","V5_14_RGP")]
public class V5_14_ResidualGeometryAndControlLimitsProtocol_Tests
{
    private readonly ITestOutputHelper _o;

    public V5_14_ResidualGeometryAndControlLimitsProtocol_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void RGP_01_ProtocolRegistration(){
        _o.WriteLine("═══ V5.14 RGP: Residual Geometry Protocol ═══");
        _o.WriteLine("Purpose: Test whether remaining failures after M3 contain additional explainable geometry.");
        _o.WriteLine("Baseline: M3 = N-conditioned P1/P1b + projHiVec > -0.3281 from V5.13.");
        _o.WriteLine("Question: Can remaining unexplained persistence variance be reduced?");
        _o.WriteLine("Or: Has model reached practical explanatory/control limit?");
        _o.WriteLine("═══ PROTOCOL REGISTERED ═══");
    }

    [Fact]public void RGP_02_BaselineModelVerification(){
        _o.WriteLine("═══ RGP_02: Frozen M3 baseline verification ═══");
        _o.WriteLine("M3: N-conditioned P1/P1b + projHiVec > -0.3281");
        _o.WriteLine("");
        _o.WriteLine("N=67: INACCESSIBLE — no pathway claim, negative control");
        _o.WriteLine("N=71: SELECTOR-USEFUL — apply projHiVec selector (M3: 62.5% holdout)");
        _o.WriteLine("N=72: SELECTOR-USEFUL — apply projHiVec selector (M3: 63.6% holdout)");
        _o.WriteLine("N=75: STRONG PATHWAY — selector bypass (M3: 75.0% holdout)");
        _o.WriteLine("N=80: SATURATED — universal protocols preferred");
        _o.WriteLine("");
        _o.WriteLine("Frozen thresholds:");
        _o.WriteLine("  Omega branch: > 1.783 (V5.3)");
        _o.WriteLine("  projHiVec: > -0.3281 (V5.13 HVI/HVS)");
        _o.WriteLine("  P1 d0: > 0.50 (V5.12)");
        _o.WriteLine("  P1b d0: > 0.65 (V5.12)");
        _o.WriteLine("  P2: DEMOTED to exploratory (V5.13)");
        _o.WriteLine("═══ BASELINE FROZEN ═══");
    }

    [Fact]public void RGP_03_ResidualFeatureFamilies(){
        _o.WriteLine("═══ RGP_03: Residual feature families ═══");
        _o.WriteLine("");
        _o.WriteLine("F1 — Geometry Residuals:");
        _o.WriteLine("  orthHiVec, distToHi, distToLo, entryScoreResid, projHiVecAngle");
        _o.WriteLine("");
        _o.WriteLine("F2 — d-Distribution Residuals:");
        _o.WriteLine("  d_std, d_p90, d_p95, d_max, d_tail_width, d_p90/p50, d_p95/p50");
        _o.WriteLine("");
        _o.WriteLine("F3 — K-Distribution Residuals:");
        _o.WriteLine("  K_std, top1%/5%/10% edge share, maxNodeKMean, maxNodeKStd");
        _o.WriteLine("");
        _o.WriteLine("F4 — Spectral Residuals:");
        _o.WriteLine("  lambda1, lambda2, spectralGap, K_Frob");
        _o.WriteLine("");
        _o.WriteLine("F5 — Trajectory Residuals:");
        _o.WriteLine("  d_velocity, K_velocity, d_accel, K_accel, curvature, reboundProxy");
        _o.WriteLine("");
        _o.WriteLine("F6 — Intervention Response [post]:");
        _o.WriteLine("  delta_dRel_achieved, displacement, over_compress, post_rebound, post_K_collapse");
        _o.WriteLine("");
        _o.WriteLine("All features measured PRE-intervention unless marked [post].");
        _o.WriteLine("═══ FEATURE FAMILIES REGISTERED ═══");
    }

    [Fact]public void RGP_04_NSpecificRoles(){
        _o.WriteLine("═══ RGP_04: N-specific roles ═══");
        _o.WriteLine("");
        _o.WriteLine("N=67: INACCESSIBLE");
        _o.WriteLine("  Negative control — no pathway claim regardless of residual features.");
        _o.WriteLine("");
        _o.WriteLine("N=71: SELECTOR-USEFUL");
        _o.WriteLine("  Primary validation window for residual selectors.");
        _o.WriteLine("");
        _o.WriteLine("N=72: SELECTOR-USEFUL");
        _o.WriteLine("  Secondary validation window for residual selectors.");
        _o.WriteLine("");
        _o.WriteLine("N=75: SELECTOR-NEUTRAL");
        _o.WriteLine("  Strong pathway regime — residual selector must not harm.");
        _o.WriteLine("");
        _o.WriteLine("N=80: SATURATED");
        _o.WriteLine("  Universal protocols — selector unnecessary.");
        _o.WriteLine("");
        _o.WriteLine("Optional: N=70,73,74,76 for N-specific analysis.");
        _o.WriteLine("═══ N ROLES FROZEN ═══");
    }

    [Fact]public void RGP_05_TrainHoldoutSeparation(){
        _o.WriteLine("═══ RGP_05: Train/holdout separation ═══");
        _o.WriteLine("");
        _o.WriteLine("Reference cohort: seeds 0-99 — train thresholds/coefficients.");
        _o.WriteLine("Holdout cohort: seeds 100-199 — validate thresholds/coefficients.");
        _o.WriteLine("Second holdout (optional): seeds 200-299 — additional validation.");
        _o.WriteLine("");
        _o.WriteLine("Rules:");
        _o.WriteLine("  - No threshold tuning on holdout.");
        _o.WriteLine("  - No feature selection based on holdout results.");
        _o.WriteLine("  - No post-hoc optimization.");
        _o.WriteLine("  - All thresholds frozen before holdout evaluation.");
        _o.WriteLine("═══ SEPARATION FROZEN ═══");
    }

    [Fact]public void RGP_06_AllowedModels(){
        _o.WriteLine("═══ RGP_06: Allowed model types ═══");
        _o.WriteLine("");
        _o.WriteLine("Only transparent methods allowed:");
        _o.WriteLine("  - Single-feature threshold rule (e.g., feature > value)");
        _o.WriteLine("  - Logistic regression (linear only)");
        _o.WriteLine("  - Linear Discriminant Analysis (LDA)");
        _o.WriteLine("  - Shallow decision tree (max depth <= 2)");
        _o.WriteLine("");
        _o.WriteLine("Prohibited:");
        _o.WriteLine("  - Random forests, gradient boosting, neural networks");
        _o.WriteLine("  - Any model requiring >2 decision boundaries");
        _o.WriteLine("  - Any model trained on holdout data");
        _o.WriteLine("═══ MODEL CONSTRAINTS FROZEN ═══");
    }

    [Fact]public void RGP_07_DecisionGates(){
        _o.WriteLine("═══ RGP_07: Decision gates ═══");
        _o.WriteLine("");
        _o.WriteLine("Gate A — Residual Geometry Found:");
        _o.WriteLine("  >=1 feature improves holdout vs M3 by >=5% at N=71/72, without overfit.");
        _o.WriteLine("  → Additional explainable geometry exists.");
        _o.WriteLine("");
        _o.WriteLine("Gate B — Residual Selector Validated:");
        _o.WriteLine("  Simple selector improves holdout N=71/72, neutral at N=75.");
        _o.WriteLine("  → M3 can be refined.");
        _o.WriteLine("");
        _o.WriteLine("Gate C — N-Specific Residuals:");
        _o.WriteLine("  Useful residual features differ by N.");
        _o.WriteLine("  → Model must remain N-conditioned.");
        _o.WriteLine("");
        _o.WriteLine("Gate D — Control Ceiling Reached:");
        _o.WriteLine("  No feature improves holdout vs M3.");
        _o.WriteLine("  → Practical explanatory limit reached.");
        _o.WriteLine("");
        _o.WriteLine("Gate E — Saturation Confirmed:");
        _o.WriteLine("  N=80 broadly inducible regardless of selector.");
        _o.WriteLine("  → Saturation regime confirmed.");
        _o.WriteLine("");
        _o.WriteLine("Gate F — Inaccessibility Confirmed:");
        _o.WriteLine("  N=67 remains inaccessible.");
        _o.WriteLine("  → N=67 outside control domain.");
        _o.WriteLine("");
        _o.WriteLine("Gate G — Overfit Warning:");
        _o.WriteLine("  Feature improves train but fails holdout.");
        _o.WriteLine("  → Reject feature; preserve M3.");
        _o.WriteLine("═══ GATES REGISTERED ═══");
    }

    [Fact]public void RGP_08_ClaimDiscipline(){
        _o.WriteLine("═══ RGP_08: Claim discipline ═══");
        _o.WriteLine("");
        _o.WriteLine("NOT CLAIMED:");
        _o.WriteLine("  - Physical interpretation of residual features");
        _o.WriteLine("  - Universal Low→High control");
        _o.WriteLine("  - Complete hidden-variable discovery");
        _o.WriteLine("  - Attractor topology proof");
        _o.WriteLine("  - Generalization beyond N=67-80, seeds 0-199");
        _o.WriteLine("  - P2 pathway validity");
        _o.WriteLine("  - Residual features as sufficient conditions");
        _o.WriteLine("");
        _o.WriteLine("CLAIMED (contingent on evidence):");
        _o.WriteLine("  - Residual geometry exists → requires Gate A holdout evidence");
        _o.WriteLine("  - Residual selector improves M3 → requires Gate B holdout evidence");
        _o.WriteLine("  - Model at control ceiling → requires Gate D evidence");
        _o.WriteLine("");
        _o.WriteLine("This is a RecoverFP residual geometry analysis only.");
        _o.WriteLine("═══ CLAIMS REGISTERED ═══");
    }
}
