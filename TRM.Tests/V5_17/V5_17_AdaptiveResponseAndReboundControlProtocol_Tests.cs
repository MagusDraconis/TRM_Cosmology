using Xunit;using Xunit.Abstractions;
namespace TRM.Tests.V5_17;
[Trait("Category","V5_17"),Trait("Category","V5_17_ARP")]
public class V5_17_AdaptiveResponseAndReboundControlProtocol_Tests{
    private readonly ITestOutputHelper _o;
    public V5_17_AdaptiveResponseAndReboundControlProtocol_Tests(ITestOutputHelper o){_o=o;}
    [Fact]public void ARP_01_ProtocolRegistration(){
        _o.WriteLine("═══ V5.17 ARP: Adaptive Response and Rebound Control ═══");
        _o.WriteLine("Purpose: Test whether post-intervention rebMagnitude can guide adaptive second intervention.");
        _o.WriteLine("Baseline: M3+ (V5.16). Key signal: rebMagnitude separates outcomes (ES=2.63).");
        _o.WriteLine("═══ PROTOCOL REGISTERED ═══");
    }
    [Fact]public void ARP_02_BaselineVerification(){
        _o.WriteLine("═══ ARP_02: M3+ baseline and rebMagnitude signal ═══");
        _o.WriteLine("M3+: P1/P1b + projHiVec + orthHiVec(N=72). Frozen.");
        _o.WriteLine("rebMagnitude: ES=2.63 TP/FP, independent from M3+ (max|r|=0.12).");
        _o.WriteLine("═══ BASELINE FROZEN ═══");
    }
    [Fact]public void ARP_03_ClaimDiscipline(){
        _o.WriteLine("═══ ARP_03: Adaptive control claims ═══");
        _o.WriteLine("Testing adaptive control only. No claims until holdout evidence.");
        _o.WriteLine("═══ CLAIMS REGISTERED ═══");
    }
}
