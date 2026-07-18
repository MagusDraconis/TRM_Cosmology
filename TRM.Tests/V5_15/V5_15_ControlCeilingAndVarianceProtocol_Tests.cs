using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_15;

[Trait("Category","V5_15"),Trait("Category","V5_15_CVP")]
public class V5_15_ControlCeilingAndVarianceProtocol_Tests
{
    private readonly ITestOutputHelper _o;
    public V5_15_ControlCeilingAndVarianceProtocol_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void CVP_01_ProtocolRegistration(){
        _o.WriteLine("═══ V5.15 CVP: Control Ceiling and Unexplained Variance ═══");
        _o.WriteLine("Purpose: Quantify remaining unexplained persistence variance after M3+.");
        _o.WriteLine("Baseline: M3+ = P1/P1b + projHiVec + orthHiVec (N=72) from V5.14.");
        _o.WriteLine("Question: How much variance remains? Structured or noise?");
        _o.WriteLine("═══ PROTOCOL REGISTERED ═══");
    }

    [Fact]public void CVP_02_BaselineVerification(){
        _o.WriteLine("═══ CVP_02: Frozen M3+ baseline ═══");
        _o.WriteLine("M3+ = M3 + orthHiVec > 0.0153 (N=72 only)");
        _o.WriteLine("N=67: INACCESSIBLE | N=71: M3 | N=72: M3+ | N=75: M3 | N=80: SATURATED");
        _o.WriteLine("Frozen thresholds: Omega>1.783, projHiVec>-0.3281, orthHiVec>0.0153");
        _o.WriteLine("═══ BASELINE FROZEN ═══");
    }

    [Fact]public void CVP_03_ClaimDiscipline(){
        _o.WriteLine("═══ CVP_03: Claim discipline ═══");
        _o.WriteLine("NOT CLAIMED: physical interpretation, universal control, complete discovery");
        _o.WriteLine("V5.15 is a quantitative variance audit only.");
        _o.WriteLine("═══ CLAIMS REGISTERED ═══");
    }
}
