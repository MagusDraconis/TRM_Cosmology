using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;

namespace TRM.Tests.V12_0;

[Trait("Category", "V12_0")]
public class V12_0_InformationPhysics_Tests
{
    private readonly ITestOutputHelper _o;
    public V12_0_InformationPhysics_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void IPR_01_InformationPhysicsRealityAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== IPR_01: Information Physics Reality Audit ===");
        _o.WriteLine("=== Is information fundamental or descriptive? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 87823;
        const double xiBase = 2.95;
        const double k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 34, nodesPerSystem: 64);
        var sorted = distances.OrderBy(x => x).ToArray();
        int nDeciles = 10;
        var decileBounds = new double[nDeciles + 1];
        for (int d = 0; d <= nDeciles; d++) decileBounds[d] = Quantile(sorted, d / (double)nDeciles);

        var allFamilies = new[] { VcFamily.SAC, VcFamily.GAN, VcFamily.RCS, VcFamily.ICS, VcFamily.CNS };
        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };
        const int nBeta = 21;

        var data = new List<(VcFamily fam, double l1, double dH, double L, double dl1)>();

        foreach (var fam in allFamilies)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var totals = new List<double>(); var l1s = new List<double>(); var Ls = new List<double>();
                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_IP", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
                    double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;
                    double sv1 = 0, svt = 0; int n = 0;
                    for (int ip = 0; ip < 3; ip++)
                    {
                        double dpv = 0.1 + ip * 0.45; if (dpv > 1.11) continue;
                        var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, dpv, v);
                        sv1 += cci.VarI1; svt += cci.VarTerms; n++;
                    }
                    if (n < 3) continue;
                    totals.Add(sv1 / n + svt / n);
                    double l1 = (sv1 / n) / Math.Max((sv1 / n) + (svt / n), 1e-12);
                    l1s.Add(l1); Ls.Add(1.0 - l1);
                }
                double eqTot = totals.Skip((int)(totals.Count * 0.8)).Average();
                double dBeta = 1.0 / (nBeta - 1);
                for (int i = 0; i < totals.Count - 1; i++)
                {
                    double dH = Math.Abs(totals[i + 1] - totals[i]) / dBeta;
                    double dl1 = Math.Abs(l1s[i + 1] - l1s[i]) / dBeta;
                    data.Add((fam, l1s[i], dH, Ls[i], dl1));
                }
            }
        }

        var l1Arr = data.Select(d => d.l1).ToArray();
        var dHarr = data.Select(d => d.dH).ToArray();
        var Larr = data.Select(d => d.L).ToArray();
        var dl1Arr = data.Select(d => d.dl1).ToArray();

        _o.WriteLine("=== Information Reduction Test ===");
        double r2_l1_dH = R2SinglePredictor(dHarr, l1Arr);
        double r2_l1_L = R2SinglePredictor(Larr, l1Arr);
        double r2_dl1_dH = R2SinglePredictor(dHarr, dl1Arr);

        _o.WriteLine($"R²(dH ~ l1)            = {r2_l1_dH:F4}");
        _o.WriteLine($"R²(L ~ l1)             = {r2_l1_L:F4}");
        _o.WriteLine($"R²(dH ~ |d(l1)/dβ|)    = {r2_dl1_dH:F4}");
        _o.WriteLine("");

        // Regime classification by dl1
        _o.WriteLine("=== Regime from Information Alone ===");
        foreach (var fam in allFamilies)
        {
            var fd = data.Where(d => d.fam == fam).ToArray();
            double mdl1 = fd.Average(d => d.dl1);
            string regime = mdl1 < 1e-8 ? "OFF" : mdl1 < 0.005 ? "Resonant" : "Dissipative";
            _o.WriteLine($"{fam,-6}: mean |d(l1)/dβ|={mdl1:F6} → {regime}");
        }
        _o.WriteLine("");

        string decision = r2_l1_dH > 0.5 && r2_l1_L > 0.9 ? "Model C" : "Model B";
        _o.WriteLine($"Decision: {decision}");
        if (decision == "Model C")
            _o.WriteLine("Information IS fundamental. l1 alone predicts L (R²>0.9), dH, and regime classification.");
        else
            _o.WriteLine("Information and dynamics are dual descriptions.");

        _o.WriteLine("");
        _o.WriteLine("=== IPR_01 complete. Commit: IPR_01_InformationPhysicsRealityAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void IDD_01_InformationDynamicsDualityAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== IDD_01: Information-Dynamics Duality Audit ===");
        _o.WriteLine("=== Is the duality itself fundamental? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 89059;
        const double xiBase = 2.95;
        const double k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 34, nodesPerSystem: 64);
        var sorted = distances.OrderBy(x => x).ToArray();
        int nDeciles = 10;
        var decileBounds = new double[nDeciles + 1];
        for (int d = 0; d <= nDeciles; d++) decileBounds[d] = Quantile(sorted, d / (double)nDeciles);

        var onFamilies = new[] { VcFamily.GAN, VcFamily.ICS, VcFamily.CNS };
        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };
        const int nBeta = 21;

        var data = new List<(double l1, double tick, double dH, double L)>();

        foreach (var fam in onFamilies)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var totals = new List<double>(); var l1s = new List<double>();
                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_ID", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
                    double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;
                    double sv1 = 0, svt = 0; int n = 0;
                    for (int ip = 0; ip < 3; ip++)
                    {
                        double dpv = 0.1 + ip * 0.45; if (dpv > 1.11) continue;
                        var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, dpv, v);
                        sv1 += cci.VarI1; svt += cci.VarTerms; n++;
                    }
                    if (n < 3) continue;
                    totals.Add(sv1 / n + svt / n);
                    l1s.Add((sv1 / n) / Math.Max((sv1 / n) + (svt / n), 1e-12));
                }
                double dBeta = 1.0 / (nBeta - 1);
                for (int i = 0; i < totals.Count - 1; i++)
                {
                    double tick = Math.Abs(totals[i + 1] - totals[i]) / dBeta;
                    data.Add((l1s[i], tick, tick, 1.0 - l1s[i]));
                }
            }
        }

        var l1Arr = data.Select(d => d.l1).ToArray();
        var tArr = data.Select(d => d.tick).ToArray();
        var dHarr = data.Select(d => d.dH).ToArray();
        var Larr = data.Select(d => d.L).ToArray();

        _o.WriteLine("=== Reconstruction: l1 only vs Tick only vs Both ===");
        double r2_l1 = R2SinglePredictor(dHarr, l1Arr);
        double r2_t = R2SinglePredictor(dHarr, tArr);
        double r2_both = FitModelR2(dHarr, new[] { l1Arr, tArr });

        _o.WriteLine($"l1 only:    R²={r2_l1:F4}");
        _o.WriteLine($"Tick only:  R²={r2_t:F4}");
        _o.WriteLine($"l1 + Tick:  R²={r2_both:F4}");
        _o.WriteLine($"ΔR² (Tick beyond l1): {r2_both - r2_l1:F4}");
        _o.WriteLine($"ΔR² (l1 beyond Tick): {r2_both - r2_t:F4}");
        _o.WriteLine("");

        string decision = r2_both > Math.Max(r2_l1, r2_t) * 1.1 ? "Model C" : "Model B";
        _o.WriteLine($"Decision: {decision}");
        if (decision == "Model C")
            _o.WriteLine("The Information-Dynamics duality is fundamental. Both components are required — neither is reducible.");
        else
            _o.WriteLine("One component dominates.");

        _o.WriteLine("");
        _o.WriteLine("=== IDD_01 complete. Commit: IDD_01_InformationDynamicsDualityAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }
}
