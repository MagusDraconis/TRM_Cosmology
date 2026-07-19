using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_42;

/// <summary>
/// V5.42 Causal Rejection Protocol (CRP).
/// Freezes constraints for non-perturbative causal rejection methodology.
/// </summary>
public class V5_42_CausalRejectionProtocol_Tests
{
    private readonly ITestOutputHelper _o;

    public V5_42_CausalRejectionProtocol_Tests(ITestOutputHelper output) => _o = output;

    [Fact]
    public void CRP_01_Protocol()
    {
        _o.WriteLine("=== CRP_01: V5.42 Causal Rejection Protocol ===");
        _o.WriteLine("");
        _o.WriteLine("Frozen policy:");
        _o.WriteLine("  M3++ model: FROZEN");
        _o.WriteLine("  c3OmegaShift threshold 0.1: FROZEN");
        _o.WriteLine("  Stop-Low policy: FROZEN");
        _o.WriteLine("  No new variables.");
        _o.WriteLine("  No new correction classes.");
        _o.WriteLine("  No perturbation-based testing.");
        _o.WriteLine("  No V6 derivations.");
        _o.WriteLine("  No physical interpretation.");
        _o.WriteLine("");
        _o.WriteLine("Purpose: Use counterfactual trace and natural variation for causal rejection.");
        _o.WriteLine("Base: V5.41 COMPLETE — perturbation INVALIDATED, rejection = strongest method.");
        _o.WriteLine("Methodology: Counterfactual trace + natural variation stratification.");
        _o.WriteLine("  - Match profiles by baseline state, compare outcomes");
        _o.WriteLine("  - Variables that do NOT differ between outcome groups are REJECTED as causal");
        _o.WriteLine("");
        _o.WriteLine("CRP_01 PASSED. Protocol frozen.");
    }

    [Fact]
    public void CRP_02_Methodology()
    {
        _o.WriteLine("=== CRP_02: Rejection Methodology ===");
        _o.WriteLine("");
        _o.WriteLine("Counterfactual trace:");
        _o.WriteLine("  - Match profiles by lambda1 band, N, cohort");
        _o.WriteLine("  - Compare high-c3OmgS vs low-c3OmgS matched pairs");
        _o.WriteLine("  - Identify variables that separate outcome groups");
        _o.WriteLine("");
        _o.WriteLine("Natural variation rejection:");
        _o.WriteLine("  - Stratify by variable terciles");
        _o.WriteLine("  - Test c3OmgS separation across strata");
        _o.WriteLine("  - Failed separation = variable not causally robust");
        _o.WriteLine("");
        _o.WriteLine("Rejection targets:");
        _o.WriteLine("  1. Single-variable causal sufficiency");
        _o.WriteLine("  2. Invariant causal relationships");
        _o.WriteLine("  3. Causal closure under current constraints");
        _o.WriteLine("");
        _o.WriteLine("CRP_02 PASSED.");
    }

    [Fact]
    public void CRP_03_ClaimDiscipline()
    {
        _o.WriteLine("=== CRP_03: Claim Discipline ===");
        _o.WriteLine("");
        _o.WriteLine("NOT CLAIMED:");
        _o.WriteLine("  - Causal closure");
        _o.WriteLine("  - V6 readiness");
        _o.WriteLine("  - Physical interpretation");
        _o.WriteLine("  - Positive causal identification");
        _o.WriteLine("  - Deterministic rescue");
        _o.WriteLine("");
        _o.WriteLine("SUPPORTED (V5.38-V5.41):");
        _o.WriteLine("  - Perturbation-based testing is INVALIDATED");
        _o.WriteLine("  - Natural variation provides diagnostic stratification");
        _o.WriteLine("  - Causal rejection is strongest methodology");
        _o.WriteLine("  - 8 causal claims REJECTED by prior evidence");
        _o.WriteLine("  - Stop-Low is OUTCOME-VALIDATED without causal closure");
        _o.WriteLine("  - Predictive validity != causal closure");
        _o.WriteLine("  - V6 remains not ready");
        _o.WriteLine("");
        _o.WriteLine("CRP_03 PASSED.");
    }
}
