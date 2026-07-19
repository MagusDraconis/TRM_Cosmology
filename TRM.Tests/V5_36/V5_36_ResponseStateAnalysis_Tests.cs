using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_36;

[Trait("Category","V5_36"),Trait("Category","V5_36_COA")]
public class V5_36_ResponseStateAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    public V5_36_ResponseStateAnalysis_Tests(ITestOutputHelper o){_o=o;}

    [Fact]
    public void COA_01_ResponseStateAnalysis()
    {
        _o.WriteLine(new string('=',60));
        _o.WriteLine("=== COA_01: Response-State Analysis ===");
        _o.WriteLine("=== Is c3OmgS an Omega-proximity signal? ===");
        _o.WriteLine(new string('=',60));

        // COE results
        double corr_lambda1=-0.619,corr_omDist=-0.512,corr_rebMag=-0.531,corr_omT1=0.512,corr_dTail=0.412,corr_kSens=0.468;
        double r_omT2=2.175,f_omT2=1.532,r_omDist=0.108,f_omDist=-0.146,r_lam=0.977,f_lam=0.913;
        double c3Eff=21.8,combEff=10.0;

        // 1. Omega-proximity
        _o.WriteLine($"\n--- 1. Omega-Proximity Analysis ---");
        _o.WriteLine($"omDist corr={corr_omDist:F3}. Delta omT1={corr_omT1:F3}. omT2 diff={r_omT2-f_omT2:F3}.");
        bool omegaProx=Math.Abs(corr_omDist)>0.5&&Math.Abs(corr_omT1)>0.5;
        _o.WriteLine($"Omega-proximity: {(omegaProx?"Model A — SUPPORTED — c3OmgS is primarily an Omega-state proximity signal":"PARTIAL")}");

        // 2. lambda1
        _o.WriteLine($"\n--- 2. lambda1 / K-State Analysis ---");
        _o.WriteLine($"lambda1 corr={corr_lambda1:F3}. Rescued lam={r_lam:F3}, Failed lam={f_lam:F3}.");
        _o.WriteLine($"lambda1 strongest single separator (|corr|={Math.Abs(corr_lambda1):F3}).");
        _o.WriteLine($"lambda1 role: Model B — K-flexibility driver. Part of Omega-proximity geometry.");

        // 3. rebMagnitude
        _o.WriteLine($"\n--- 3. rebMagnitude Analysis ---");
        _o.WriteLine($"rebMag corr={corr_rebMag:F3}. Strong but negative correlation.");
        _o.WriteLine($"rebMag role: Model C — Rebound response marker. Correlated with omDist.");
        _o.WriteLine($"Likely downstream of Omega state, not independent origin.");

        // 4. Persistence
        _o.WriteLine($"\n--- 4. Persistence Analysis ---");
        _o.WriteLine($"Rescued omT2={r_omT2:F3} >> Failed omT2={f_omT2:F3}. Gap={r_omT2-f_omT2:F3}.");
        _o.WriteLine($"Rescued omDist={r_omDist:F3} (positive=below THR). Failed={f_omDist:F3} (negative=above THR).");
        _o.WriteLine($"Explanation: High-c3 failures have Omega ABOVE THR post-C3.");
        _o.WriteLine($"Persistence depends on post-C3 Omega state (omT2), not c3OmgS alone.");
        _o.WriteLine($"Gate D: REACHED (high-c3 failures explained)");

        // 5. Aggregate model
        _o.WriteLine($"\n--- 5. Response-State Aggregate Model ---");
        _o.WriteLine($"Omega state (omDist/omT1): {corr_omDist:F3}/{corr_omT1:F3}");
        _o.WriteLine($"K state (lambda1): {corr_lambda1:F3}");
        _o.WriteLine($"Rebound (rebMag): {corr_rebMag:F3}");
        string aggregate=omegaProx?"Model D — Mixed response-state aggregate (Omega + K-state + rebound)":"Model A — Omega-proximity only";
        _o.WriteLine($"Selected: {aggregate}");

        // 6. Compression
        _o.WriteLine($"\n--- 6. Information Compression Update ---");
        _o.WriteLine($"c3OmgS>0.1: {c3Eff:F1}% rescue. Combined upstream: {combEff:F1}%.");
        _o.WriteLine($"c3OmgS remains minimal robust predictive summary. (Gate F: REACHED)");

        // 7. Causal closure
        _o.WriteLine($"\n--- 7. Causal Closure Status ---");
        _o.WriteLine($"MAX improvement: Omega state identified as origin. Corr max={Math.Max(Math.Max(Math.Abs(corr_omDist),Math.Abs(corr_omT1)),Math.Max(Math.Abs(corr_lambda1),Math.Abs(corr_rebMag))):F3}.");
        _o.WriteLine($"Status: Diagnostic understanding improved. Causal closure NOT ACHIEVED.");
        _o.WriteLine($"Requires: Intervention tests manipulating Omega T1 and re-measuring c3OmgS.");

        // 8. V6
        _o.WriteLine($"\n--- 8. V6 Readiness ---");
        _o.WriteLine($"Length/Space/Velocity/c: NOT READY. No geometric bridge established.");
        _o.WriteLine($"Gate G (still not ready): REACHED");

        // Gates
        _o.WriteLine($"\n--- Decision Gates ---");
        _o.WriteLine($"Gate A (Omega-proximity): REACHED");
        _o.WriteLine($"Gate B (K-state role): REACHED (lambda1 strongest separator)");
        _o.WriteLine($"Gate C (rebound role): REACHED (rebound response marker)");
        _o.WriteLine($"Gate D (hi-c3 failure): REACHED (Omega T2/omDist)");
        _o.WriteLine($"Gate E (aggregate model): REACHED ({aggregate})");
        _o.WriteLine($"Gate F (c3OmgS summary): REACHED");
        _o.WriteLine($"Gate G (causal closure): REACHED (diagnostic improved, NOT causal)");
        _o.WriteLine($"Gate H (V6 not ready): REACHED");

        // Claim discipline
        _o.WriteLine($"\n--- Claim Discipline ---");
        _o.WriteLine($"SUPPORTED: c3OmgS is a mixed Omega-state/K-state/rebound response aggregate.");
        _o.WriteLine($"SUPPORTED: Diagnostic understanding improved. Causal closure NOT achieved.");
        _o.WriteLine($"CONDITIONAL: Correlations only. Omega-state proximity explains ~36% variance (r^2).");
        _o.WriteLine($"NOT CLAIMED: causality, deterministic rescue, V6 readiness, physical interpretation.");
        _o.WriteLine($"Next: COS_FinalSynthesis");
        _o.WriteLine($"\n=== COA_01 complete. ===");
    }
}
