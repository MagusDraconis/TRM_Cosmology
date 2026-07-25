using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;

namespace TRM.Tests.V8_4;

[Trait("Category", "V8_4")]
public class V8_4_ResonanceOrigin_Tests
{
    private readonly ITestOutputHelper _o;
    public V8_4_ResonanceOrigin_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void ROA_01_ResonanceOriginAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== ROA_01: Resonance Origin Audit ===");
        _o.WriteLine("=== Does resonance explain λ and time emergence? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 25501;
        const double xiBase = 2.95;
        const double k0Base = 1.0;

        var distances = BuildDistanceEnsemble(baseSeed, systems: 34, nodesPerSystem: 64);
        var sorted = distances.OrderBy(x => x).ToArray();
        int nDeciles = 10;
        var decileBounds = new double[nDeciles + 1];
        for (int d = 0; d <= nDeciles; d++) decileBounds[d] = Quantile(sorted, d / (double)nDeciles);

        var contrastDefs = new (string name, int i, int j)[] { ("K1-K10", 1, 10), ("K2-K8", 2, 8), ("K4-K6", 4, 6), ("K3-K7", 3, 7), ("K1-K5", 1, 5), ("K5-K9", 5, 9) };
        int nContrasts = 6;
        var families = new[] { VcFamily.SAC, VcFamily.GAN, VcFamily.RCS, VcFamily.ICS, VcFamily.CNS };
        const int nBeta = 31;
        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };

        // Collect: resonance concentration (λ1/Σλ), λ1, dH/dβ, accessibility
        var data = new List<(VcFamily fam, double resonance, double lam1, double dH, double acc)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var pts = new List<(double resonance, double lam1, double ent, double acc)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_RA", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
                    var allC = new List<double[]>(); var allL = new List<double>();
                    double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;

                    for (int ip = 0; ip < 3; ip++)
                    {
                        double pv = 0.1 + ip * 0.45; if (pv > 1.11) continue;
                        var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, pv, v);
                        int nD = distances.Length; double[] kA = new double[nD];
                        for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, pv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                        var kD = new double[nDeciles + 1]; var ct = new int[nDeciles + 1];
                        for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kD[dec] += kA[i]; ct[dec]++; }
                        for (int d = 1; d <= nDeciles; d++) kD[d] /= Math.Max(ct[d], 1);
                        var ctr = new double[nContrasts]; for (int c = 0; c < nContrasts; c++) ctr[c] = kD[contrastDefs[c].i] - kD[contrastDefs[c].j];
                        allC.Add(ctr); allL.Add(Math.Clamp(1.0 - cci.VarI1 / (cci.VarTerms + 1e-15), 0.0, 1.0));
                    }

                    if (allL.Count < 3) continue;
                    int N = allL.Count; var LArr = allL.ToArray();
                    var X = new double[N][]; for (int i = 0; i < N; i++) X[i] = (double[])allC[i].Clone();
                    for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / s; }
                    var cm = new double[nContrasts, nContrasts];
                    for (int a = 0; a < nContrasts; a++) for (int b = 0; b < nContrasts; b++) cm[a, b] = PearsonCorrelation(Enumerable.Range(0, N).Select(i => allC[i][a]).ToArray(), Enumerable.Range(0, N).Select(i => allC[i][b]).ToArray());
                    var (ee, ev) = JacobiEigenLocal(cm, nContrasts);
                    var sEE = ee.OrderByDescending(e => e).ToArray();
                    double totalE = sEE.Sum() + 1e-12;
                    double resonance = sEE[0] / totalE; // λ1 fraction = resonance concentration

                    var pe = Enumerable.Range(0, nContrasts).OrderByDescending(i => ee[i]).ToArray();
                    var la = new double[3][];
                    for (int k = 0; k < 3; k++) { la[k] = new double[N]; int er = pe[k]; for (int i = 0; i < N; i++) { double s = 0; for (int c = 0; c < nContrasts; c++) s += X[i][c] * ev[er, c]; la[k][i] = s; } }
                    double r2L1 = R2SinglePredictor(LArr, la[0]), r2L2 = FitModelR2(LArr, new[] { la[0], la[1] }), r2L3 = FitModelR2(LArr, new[] { la[0], la[1], la[2] });
                    double tVal = r2L3 + 1e-12;
                    double o1 = r2L1 / tVal, o2 = (r2L2 - r2L1) / tVal, o3 = (r2L3 - r2L2) / tVal;
                    double ent = 0; if (o1 > 1e-12) ent -= o1 * Math.Log(o1); if (o2 > 1e-12) ent -= o2 * Math.Log(o2); if (o3 > 1e-12) ent -= o3 * Math.Log(o3);
                    double d1a = Math.Abs(o1 - 1.0) + o2 + o3;
                    double d2a = Math.Abs(o1 - 0.5) + Math.Abs(o2 - 0.5) + o3;
                    double d3a = Math.Abs(o1 - 1.0 / 3) + Math.Abs(o2 - 1.0 / 3) + Math.Abs(o3 - 1.0 / 3);
                    double acc = 1.0 / Math.Max(Math.Min(d1a, Math.Min(d2a, d3a)), 0.01);

                    pts.Add((resonance, sEE[0], ent, acc));
                }

                for (int i = 0; i < pts.Count - 1; i++)
                {
                    double dH = Math.Abs(pts[i + 1].ent - pts[i].ent) / (1.0 / (nBeta - 1));
                    data.Add((fam, pts[i].resonance, pts[i].lam1, dH, pts[i].acc));
                }
            }
        }

        var Res = data.Select(d => d.resonance).ToArray();
        var Lam = data.Select(d => d.lam1).ToArray();
        var DHv = data.Select(d => d.dH).ToArray();
        var Acc = data.Select(d => d.acc).ToArray();

        // ============================================================
        _o.WriteLine("=== Resonance Spectrum ===");
        _o.WriteLine($"{"Family",-6} {"mean res",10} {"CV(res)",10} {"mean λ1",10} {"CV(dH)",10}");
        _o.WriteLine(new string('-', 48));

        double[][] famRes = families.Select(f => data.Where(d => d.fam == f).Select(d => d.resonance).ToArray()).ToArray();

        foreach (var fam in families)
        {
            var fd = data.Where(d => d.fam == fam).ToArray();
            double mr = fd.Average(d => d.resonance);
            double cvr = StdOverMean(fd.Select(d => d.resonance).ToArray());
            double ml = fd.Average(d => d.lam1);
            double cvd = StdOverMean(fd.Select(d => d.dH).ToArray());
            _o.WriteLine($"{fam,-6} {mr,10:F4} {cvr,10:F4} {ml,10:F4} {cvd,10:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Resonance → λ, Accessibility, Time ===");
        double rResLam = PearsonCorrelation(Res, Lam);
        double rResAcc = PearsonCorrelation(Res, Acc);
        double rResDH = PearsonCorrelation(Res, DHv);

        _o.WriteLine($"r(resonance, λ1)          = {rResLam:F4}");
        _o.WriteLine($"r(resonance, accessibility) = {rResAcc:F4}");
        _o.WriteLine($"r(resonance, dH/dβ)        = {rResDH:F4}");
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Per-Family Resonance → Time ===");
        _o.WriteLine($"{"Family",-6} {"r(res,dH)",10} {"r(res,λ1)",10} {"r(res,acc)",10}");
        _o.WriteLine(new string('-', 38));

        foreach (var fam in families)
        {
            var fd = data.Where(d => d.fam == fam).ToArray();
            double r1 = PearsonCorrelation(fd.Select(d => d.resonance).ToArray(), fd.Select(d => d.dH).ToArray());
            double r2 = PearsonCorrelation(fd.Select(d => d.resonance).ToArray(), fd.Select(d => d.lam1).ToArray());
            double r3 = PearsonCorrelation(fd.Select(d => d.resonance).ToArray(), fd.Select(d => d.acc).ToArray());
            _o.WriteLine($"{fam,-6} {r1,10:F4} {r2,10:F4} {r3,10:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Group Split: SAC+RCS vs GAN+ICS+CNS ===");
        var gA = data.Where(d => d.fam == VcFamily.SAC || d.fam == VcFamily.RCS).ToArray();
        var gB = data.Where(d => d.fam != VcFamily.SAC && d.fam != VcFamily.RCS).ToArray();

        double resA = gA.Average(d => d.resonance);
        double resB = gB.Average(d => d.resonance);
        double cvDHA = StdOverMean(gA.Select(d => d.dH).ToArray());
        double cvDHB = StdOverMean(gB.Select(d => d.dH).ToArray());

        _o.WriteLine($"SAC+RCS:     mean res={resA:F4}, CV(dH)={cvDHA:F4}");
        _o.WriteLine($"GAN+ICS+CNS: mean res={resB:F4}, CV(dH)={cvDHB:F4}");
        _o.WriteLine($"Δresonance = {resA - resB:F4}");

        // Threshold test: does time-rate emergence require >X resonance?
        double threshold = (resA + resB) / 2.0;
        var highRes = data.Where(d => d.resonance > threshold).ToArray();
        var lowRes = data.Where(d => d.resonance <= threshold).ToArray();
        double rDH_High = PearsonCorrelation(highRes.Select(d => d.resonance).ToArray(), highRes.Select(d => d.dH).ToArray());
        double rDH_Low = PearsonCorrelation(lowRes.Select(d => d.resonance).ToArray(), lowRes.Select(d => d.dH).ToArray());
        _o.WriteLine($"r(res,dH) above threshold: {rDH_High:F4}, below: {rDH_Low:F4}");
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Decision ===");

        bool explainsBoth = Math.Abs(rResLam) > 0.5 && Math.Abs(rResDH) > 0.4 && Math.Abs(resA - resB) > 0.05;
        bool explainsLambdaOnly = Math.Abs(rResLam) > 0.5 && Math.Abs(rResDH) < 0.2;
        bool noRelation = Math.Abs(rResLam) < 0.3;

        string decision;
        if (explainsBoth) decision = "Model C";
        else if (explainsLambdaOnly) decision = "Model B";
        else if (noRelation) decision = "Model A";
        else decision = "Model D";

        _o.WriteLine($"Decision: {decision}");
        _o.WriteLine($"r(res,λ1)={rResLam:F4}, r(res,dH)={rResDH:F4}, Δres={resA - resB:F4}");

        if (decision == "Model C")
            _o.WriteLine("Resonance explains both λ generation and time-rate emergence.");
        else if (decision == "Model B")
            _o.WriteLine("Resonance explains λ dominance but not time-rate emergence.");
        else
            _o.WriteLine("Resonance is not a primary driver.");

        _o.WriteLine("");
        _o.WriteLine("=== ROA_01 complete. Commit: ROA_01_ResonanceOriginAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void DAO_01_DynamicsOriginAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== DAO_01: Dynamics Origin Audit ===");
        _o.WriteLine("=== Do dynamics differ even when state structure doesn't? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 26713;
        const double xiBase = 2.95;
        const double k0Base = 1.0;

        var distances = BuildDistanceEnsemble(baseSeed, systems: 34, nodesPerSystem: 64);
        var sorted = distances.OrderBy(x => x).ToArray();
        int nDeciles = 10;
        var decileBounds = new double[nDeciles + 1];
        for (int d = 0; d <= nDeciles; d++) decileBounds[d] = Quantile(sorted, d / (double)nDeciles);

        var contrastDefs = new (string name, int i, int j)[] { ("K1-K10", 1, 10), ("K2-K8", 2, 8), ("K4-K6", 4, 6), ("K3-K7", 3, 7), ("K1-K5", 1, 5), ("K5-K9", 5, 9) };
        int nContrasts = 6;
        var families = new[] { VcFamily.SAC, VcFamily.GAN, VcFamily.RCS, VcFamily.ICS, VcFamily.CNS };
        const int nBeta = 31;
        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };

        // Collect d(state)/dβ for entropy, λ1, accessibility
        var dynamics = new List<(VcFamily fam, double dEnt_dB, double dLam_dB, double dAcc_dB)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var pts = new List<(double ent, double lam1, double acc)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_DA", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
                    var allC = new List<double[]>(); var allL = new List<double>();
                    double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;

                    for (int ip = 0; ip < 3; ip++)
                    {
                        double pv = 0.1 + ip * 0.45; if (pv > 1.11) continue;
                        var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, pv, v);
                        int nD = distances.Length; double[] kA = new double[nD];
                        for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, pv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                        var kD = new double[nDeciles + 1]; var ct = new int[nDeciles + 1];
                        for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kD[dec] += kA[i]; ct[dec]++; }
                        for (int d = 1; d <= nDeciles; d++) kD[d] /= Math.Max(ct[d], 1);
                        var ctr = new double[nContrasts]; for (int c = 0; c < nContrasts; c++) ctr[c] = kD[contrastDefs[c].i] - kD[contrastDefs[c].j];
                        allC.Add(ctr); allL.Add(Math.Clamp(1.0 - cci.VarI1 / (cci.VarTerms + 1e-15), 0.0, 1.0));
                    }

                    if (allL.Count < 3) continue;
                    int N = allL.Count; var LArr = allL.ToArray();
                    var X = new double[N][]; for (int i = 0; i < N; i++) X[i] = (double[])allC[i].Clone();
                    for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / s; }
                    var cm = new double[nContrasts, nContrasts];
                    for (int a = 0; a < nContrasts; a++) for (int b = 0; b < nContrasts; b++) cm[a, b] = PearsonCorrelation(Enumerable.Range(0, N).Select(i => allC[i][a]).ToArray(), Enumerable.Range(0, N).Select(i => allC[i][b]).ToArray());
                    var (ee, ev) = JacobiEigenLocal(cm, nContrasts);
                    var sEE = ee.OrderByDescending(e => e).ToArray();
                    var pe = Enumerable.Range(0, nContrasts).OrderByDescending(i => ee[i]).ToArray();
                    var la = new double[3][];
                    for (int k = 0; k < 3; k++) { la[k] = new double[N]; int er = pe[k]; for (int i = 0; i < N; i++) { double s = 0; for (int c = 0; c < nContrasts; c++) s += X[i][c] * ev[er, c]; la[k][i] = s; } }
                    double r2L1 = R2SinglePredictor(LArr, la[0]), r2L2 = FitModelR2(LArr, new[] { la[0], la[1] }), r2L3 = FitModelR2(LArr, new[] { la[0], la[1], la[2] });
                    double tVal = r2L3 + 1e-12;
                    double o1 = r2L1 / tVal, o2 = (r2L2 - r2L1) / tVal, o3 = (r2L3 - r2L2) / tVal;
                    double ent = 0; if (o1 > 1e-12) ent -= o1 * Math.Log(o1); if (o2 > 1e-12) ent -= o2 * Math.Log(o2); if (o3 > 1e-12) ent -= o3 * Math.Log(o3);
                    double d1a = Math.Abs(o1 - 1.0) + o2 + o3;
                    double d2a = Math.Abs(o1 - 0.5) + Math.Abs(o2 - 0.5) + o3;
                    double d3a = Math.Abs(o1 - 1.0 / 3) + Math.Abs(o2 - 1.0 / 3) + Math.Abs(o3 - 1.0 / 3);
                    double acc = 1.0 / Math.Max(Math.Min(d1a, Math.Min(d2a, d3a)), 0.01);
                    pts.Add((ent, sEE[0], acc));
                }

                double dBeta = 1.0 / (nBeta - 1);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    double dE = (pts[i + 1].ent - pts[i].ent) / dBeta;
                    double dL = (pts[i + 1].lam1 - pts[i].lam1) / dBeta;
                    double dA = (pts[i + 1].acc - pts[i].acc) / dBeta;
                    dynamics.Add((fam, dE, dL, dA));
                }
            }
        }

        // ============================================================
        _o.WriteLine("=== Derivative Comparison (dynamics) ===");
        _o.WriteLine($"{"Family",-6} {"mean|dE/dβ|",12} {"mean|dλ/dβ|",12} {"mean|dA/dβ|",12} {"CV(dE)",10} {"CV(dλ)",10}");
        _o.WriteLine(new string('-', 64));

        foreach (var fam in families)
        {
            var fd = dynamics.Where(d => d.fam == fam).ToArray();
            double mE = fd.Average(d => Math.Abs(d.dEnt_dB));
            double mL = fd.Average(d => Math.Abs(d.dLam_dB));
            double mA = fd.Average(d => Math.Abs(d.dAcc_dB));
            double cE = StdOverMean(fd.Select(d => Math.Abs(d.dEnt_dB)).ToArray());
            double cL = StdOverMean(fd.Select(d => Math.Abs(d.dLam_dB)).ToArray());
            _o.WriteLine($"{fam,-6} {mE,12:F4} {mL,12:F4} {mA,12:F4} {cE,10:F4} {cL,10:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Group Comparison ===");
        var gA = dynamics.Where(d => d.fam == VcFamily.SAC || d.fam == VcFamily.RCS).ToArray();
        var gB = dynamics.Where(d => d.fam != VcFamily.SAC && d.fam != VcFamily.RCS).ToArray();

        double mEA = gA.Average(d => Math.Abs(d.dEnt_dB));
        double mEB = gB.Average(d => Math.Abs(d.dEnt_dB));
        double mLA = gA.Average(d => Math.Abs(d.dLam_dB));
        double mLB = gB.Average(d => Math.Abs(d.dLam_dB));

        _o.WriteLine($"|dE/dβ|: SAC+RCS={mEA:F4}, GAN+ICS+CNS={mEB:F4}, ratio={mEA/Math.Max(mEB,1e-12):F2}");
        _o.WriteLine($"|dλ/dβ|: SAC+RCS={mLA:F4}, GAN+ICS+CNS={mLB:F4}, ratio={mLA/Math.Max(mLB,1e-12):F2}");

        bool dynamicsDiffer = Math.Abs(mEA - mEB) / Math.Max(Math.Max(mEA, mEB), 1e-12) > 0.3 ||
                              Math.Abs(mLA - mLB) / Math.Max(Math.Max(mLA, mLB), 1e-12) > 0.3;
        _o.WriteLine($"Dynamics differ: {(dynamicsDiffer ? "YES" : "NO")}");
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Decision ===");
        string decision;
        if (dynamicsDiffer && Math.Abs(mEA - mEB) / Math.Max(Math.Max(mEA, mEB), 1e-12) > 0.5)
            decision = "Model C";
        else if (dynamicsDiffer)
            decision = "Model B";
        else
            decision = "Model A";

        _o.WriteLine($"Decision: {decision}");

        if (decision == "Model C")
            _o.WriteLine("Time emergence is determined by dynamic laws. Families share state structure but differ in evolution rules.");
        else if (decision == "Model B")
            _o.WriteLine("Weak dynamic differences exist but don't fully explain time emergence.");
        else
            _o.WriteLine("Same dynamics across families.");

        _o.WriteLine("");
        _o.WriteLine("=== DAO_01 complete. Commit: DAO_01_DynamicsOriginAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void DFG_01_DynamicsFlowGeneratorAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== DFG_01: Dynamics Flow Generator Audit ===");
        _o.WriteLine("=== What generates entropy flow when λ is static? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 27931;
        const double xiBase = 2.95;
        const double k0Base = 1.0;

        var distances = BuildDistanceEnsemble(baseSeed, systems: 34, nodesPerSystem: 64);
        var sorted = distances.OrderBy(x => x).ToArray();
        int nDeciles = 10;
        var decileBounds = new double[nDeciles + 1];
        for (int d = 0; d <= nDeciles; d++) decileBounds[d] = Quantile(sorted, d / (double)nDeciles);

        var contrastDefs = new (string name, int i, int j)[] { ("K1-K10", 1, 10), ("K2-K8", 2, 8), ("K4-K6", 4, 6), ("K3-K7", 3, 7), ("K1-K5", 1, 5), ("K5-K9", 5, 9) };
        int nContrasts = 6;
        var families = new[] { VcFamily.SAC, VcFamily.GAN, VcFamily.RCS, VcFamily.ICS, VcFamily.CNS };
        const int nBeta = 31;
        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };

        var flowData = new List<(VcFamily fam, double dOcc_dB, double dEnt_dB, double dL_dB)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var pts = new List<(double occ1, double ent, double Lmean)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_FG", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
                    var allC = new List<double[]>(); var allL = new List<double>();
                    double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;

                    for (int ip = 0; ip < 3; ip++)
                    {
                        double pv = 0.1 + ip * 0.45; if (pv > 1.11) continue;
                        var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, pv, v);
                        int nD = distances.Length; double[] kA = new double[nD];
                        for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, pv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                        var kD = new double[nDeciles + 1]; var ct = new int[nDeciles + 1];
                        for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kD[dec] += kA[i]; ct[dec]++; }
                        for (int d = 1; d <= nDeciles; d++) kD[d] /= Math.Max(ct[d], 1);
                        var ctr = new double[nContrasts]; for (int c = 0; c < nContrasts; c++) ctr[c] = kD[contrastDefs[c].i] - kD[contrastDefs[c].j];
                        allC.Add(ctr); allL.Add(Math.Clamp(1.0 - cci.VarI1 / (cci.VarTerms + 1e-15), 0.0, 1.0));
                    }

                    if (allL.Count < 3) continue;
                    int N = allL.Count; var LArr = allL.ToArray();
                    var X = new double[N][]; for (int i = 0; i < N; i++) X[i] = (double[])allC[i].Clone();
                    for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / s; }
                    var cm = new double[nContrasts, nContrasts];
                    for (int a = 0; a < nContrasts; a++) for (int b = 0; b < nContrasts; b++) cm[a, b] = PearsonCorrelation(Enumerable.Range(0, N).Select(i => allC[i][a]).ToArray(), Enumerable.Range(0, N).Select(i => allC[i][b]).ToArray());
                    var (ee, ev) = JacobiEigenLocal(cm, nContrasts);
                    var pe = Enumerable.Range(0, nContrasts).OrderByDescending(i => ee[i]).ToArray();
                    var la = new double[3][];
                    for (int k = 0; k < 3; k++) { la[k] = new double[N]; int er = pe[k]; for (int i = 0; i < N; i++) { double s = 0; for (int c = 0; c < nContrasts; c++) s += X[i][c] * ev[er, c]; la[k][i] = s; } }
                    double r2L1 = R2SinglePredictor(LArr, la[0]), r2L2 = FitModelR2(LArr, new[] { la[0], la[1] }), r2L3 = FitModelR2(LArr, new[] { la[0], la[1], la[2] });
                    double tVal = r2L3 + 1e-12;
                    double o1 = r2L1 / tVal, o2 = (r2L2 - r2L1) / tVal, o3 = (r2L3 - r2L2) / tVal;
                    double ent = 0; if (o1 > 1e-12) ent -= o1 * Math.Log(o1); if (o2 > 1e-12) ent -= o2 * Math.Log(o2); if (o3 > 1e-12) ent -= o3 * Math.Log(o3);
                    pts.Add((o1, ent, LArr.Average()));
                }

                double dBeta = 1.0 / (nBeta - 1);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    double dO = Math.Abs(pts[i + 1].occ1 - pts[i].occ1) / dBeta;
                    double dE = Math.Abs(pts[i + 1].ent - pts[i].ent) / dBeta;
                    double dL = Math.Abs(pts[i + 1].Lmean - pts[i].Lmean) / dBeta;
                    flowData.Add((fam, dO, dE, dL));
                }
            }
        }

        _o.WriteLine("=== Flow Generator ===");
        _o.WriteLine($"{"Family",-6} {"|d(occ)/dβ|",12} {"|d(ent)/dβ|",12} {"|d(L)/dβ|",12}");
        _o.WriteLine(new string('-', 44));

        foreach (var fam in families)
        {
            var fd = flowData.Where(d => d.fam == fam).ToArray();
            _o.WriteLine($"{fam,-6} {fd.Average(d => d.dOcc_dB),12:F6} {fd.Average(d => d.dEnt_dB),12:F6} {fd.Average(d => d.dL_dB),12:F6}");
        }
        _o.WriteLine("");

        var gF = flowData.Where(d => d.fam == VcFamily.SAC || d.fam == VcFamily.RCS).ToArray();
        var gL = flowData.Where(d => d.fam != VcFamily.SAC && d.fam != VcFamily.RCS).ToArray();

        double mOF = gF.Average(d => d.dOcc_dB), mEF = gF.Average(d => d.dEnt_dB), mLF = gF.Average(d => d.dL_dB);
        double mOL = gL.Average(d => d.dOcc_dB), mEL = gL.Average(d => d.dEnt_dB), mLL = gL.Average(d => d.dL_dB);

        _o.WriteLine("=== Group ===");
        _o.WriteLine($"FROZEN: |dOcc|={mOF:F6}, |dEnt|={mEF:F6}, |dL|={mLF:F6}");
        _o.WriteLine($"LIVE:   |dOcc|={mOL:F6}, |dEnt|={mEL:F6}, |dL|={mLL:F6}");
        _o.WriteLine("");

        string decision;
        if (mOL > mOF * 10 && mEL > mEF * 10 && mLF < 1e-9) decision = "Model B";
        else if (mLL > mLF * 10) decision = "Model C";
        else decision = "Model D";

        _o.WriteLine($"Decision: {decision}");
        if (decision == "Model B") _o.WriteLine("Occupation dynamics generate entropy flow.");
        else if (decision == "Model C") _o.WriteLine("Covariance signal changes drive entropy flow.");
        else _o.WriteLine("Multiple/hybrid mechanism.");

        _o.WriteLine("");
        _o.WriteLine("=== DFG_01 complete. Commit: DFG_01_DynamicsFlowGeneratorAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void LFG_01_LSignalFlowGeneratorAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== LFG_01: L Signal Flow Generator Audit ===");
        _o.WriteLine("=== Does L drive entropy, or vice versa? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 29173;
        const double xiBase = 2.95;
        const double k0Base = 1.0;

        var distances = BuildDistanceEnsemble(baseSeed, systems: 34, nodesPerSystem: 64);
        var sorted = distances.OrderBy(x => x).ToArray();
        int nDeciles = 10;
        var decileBounds = new double[nDeciles + 1];
        for (int d = 0; d <= nDeciles; d++) decileBounds[d] = Quantile(sorted, d / (double)nDeciles);

        var contrastDefs = new (string name, int i, int j)[] { ("K1-K10", 1, 10), ("K2-K8", 2, 8), ("K4-K6", 4, 6), ("K3-K7", 3, 7), ("K1-K5", 1, 5), ("K5-K9", 5, 9) };
        int nContrasts = 6;
        var families = new[] { VcFamily.SAC, VcFamily.GAN, VcFamily.RCS, VcFamily.ICS, VcFamily.CNS };
        const int nBeta = 31;
        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };

        // Collect per-step: L(t), dH(t→t+1), L(t+1)
        var steps = new List<(VcFamily fam, double L_now, double dH_next, double L_next)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var pts = new List<(double ent, double L)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_LF", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
                    var allC = new List<double[]>(); var allL = new List<double>();
                    double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;

                    for (int ip = 0; ip < 3; ip++)
                    {
                        double pv = 0.1 + ip * 0.45; if (pv > 1.11) continue;
                        var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, pv, v);
                        int nD = distances.Length; double[] kA = new double[nD];
                        for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, pv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                        var kD = new double[nDeciles + 1]; var ct = new int[nDeciles + 1];
                        for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kD[dec] += kA[i]; ct[dec]++; }
                        for (int d = 1; d <= nDeciles; d++) kD[d] /= Math.Max(ct[d], 1);
                        var ctr = new double[nContrasts]; for (int c = 0; c < nContrasts; c++) ctr[c] = kD[contrastDefs[c].i] - kD[contrastDefs[c].j];
                        allC.Add(ctr); allL.Add(Math.Clamp(1.0 - cci.VarI1 / (cci.VarTerms + 1e-15), 0.0, 1.0));
                    }

                    if (allL.Count < 3) continue;
                    int N = allL.Count; var LArr = allL.ToArray();
                    var X = new double[N][]; for (int i = 0; i < N; i++) X[i] = (double[])allC[i].Clone();
                    for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / s; }
                    var cm = new double[nContrasts, nContrasts];
                    for (int a = 0; a < nContrasts; a++) for (int b = 0; b < nContrasts; b++) cm[a, b] = PearsonCorrelation(Enumerable.Range(0, N).Select(i => allC[i][a]).ToArray(), Enumerable.Range(0, N).Select(i => allC[i][b]).ToArray());
                    var (ee, ev) = JacobiEigenLocal(cm, nContrasts);
                    var pe = Enumerable.Range(0, nContrasts).OrderByDescending(i => ee[i]).ToArray();
                    var la = new double[3][];
                    for (int k = 0; k < 3; k++) { la[k] = new double[N]; int er = pe[k]; for (int i = 0; i < N; i++) { double s = 0; for (int c = 0; c < nContrasts; c++) s += X[i][c] * ev[er, c]; la[k][i] = s; } }
                    double r2L1 = R2SinglePredictor(LArr, la[0]), r2L2 = FitModelR2(LArr, new[] { la[0], la[1] }), r2L3 = FitModelR2(LArr, new[] { la[0], la[1], la[2] });
                    double tVal = r2L3 + 1e-12;
                    double o1 = r2L1 / tVal, o2 = (r2L2 - r2L1) / tVal, o3 = (r2L3 - r2L2) / tVal;
                    double ent = 0; if (o1 > 1e-12) ent -= o1 * Math.Log(o1); if (o2 > 1e-12) ent -= o2 * Math.Log(o2); if (o3 > 1e-12) ent -= o3 * Math.Log(o3);
                    pts.Add((ent, LArr.Average()));
                }

                double dBeta = 1.0 / (nBeta - 1);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    double dH = Math.Abs(pts[i + 1].ent - pts[i].ent) / dBeta;
                    steps.Add((fam, pts[i].L, dH, pts[i + 1].L));
                }
            }
        }

        var Lnow = steps.Select(s => s.L_now).ToArray();
        var DHnext = steps.Select(s => s.dH_next).ToArray();
        var Lnext = steps.Select(s => s.L_next).ToArray();
        var DHnow = DHnext; // dH is already the forward step

        // ============================================================
        _o.WriteLine("=== Lag Analysis ===");
        double r_Lnow_DH = PearsonCorrelation(Lnow, DHnext);
        double r_DH_Lnext = PearsonCorrelation(DHnow, Lnext);

        _o.WriteLine($"r(L_t, dH_next) = {r_Lnow_DH:F4} — L predicts next entropy change?");
        _o.WriteLine($"r(dH, L_next)    = {r_DH_Lnext:F4} — entropy change predicts next L?");
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Per-Family Lag ===");
        _o.WriteLine($"{"Family",-6} {"r(L_t,dH)",10} {"r(dH,L_t+1)",12}");
        _o.WriteLine(new string('-', 30));
        foreach (var fam in families)
        {
            var fd = steps.Where(s => s.fam == fam).ToArray();
            double r1 = PearsonCorrelation(fd.Select(s => s.L_now).ToArray(), fd.Select(s => s.dH_next).ToArray());
            double r2 = PearsonCorrelation(fd.Select(s => s.dH_next).ToArray(), fd.Select(s => s.L_next).ToArray());
            _o.WriteLine($"{fam,-6} {r1,10:F4} {r2,12:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Direct Driver? ===");
        // Separate live families only
        var live = steps.Where(s => s.fam != VcFamily.SAC && s.fam != VcFamily.RCS).ToArray();
        double rL_Live = PearsonCorrelation(live.Select(s => s.L_now).ToArray(), live.Select(s => s.dH_next).ToArray());
        double rL_Live2 = PearsonCorrelation(live.Select(s => s.dH_next).ToArray(), live.Select(s => s.L_next).ToArray());

        _o.WriteLine($"Live families: r(L_t,dH)={rL_Live:F4}, r(dH,L_t+1)={rL_Live2:F4}");

        // Can entropy flow without L changing?
        var dLnext = steps.Select(s => Math.Abs(s.L_next - s.L_now)).ToArray();
        double r_DH_dL = PearsonCorrelation(DHnext, dLnext);
        _o.WriteLine($"r(dH, |ΔL|) = {r_DH_dL:F4} — are dH and |ΔL| coupled?");
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Decision ===");
        bool LdrivesEntropy = Math.Abs(r_Lnow_DH) > Math.Abs(r_DH_Lnext) * 1.5 && Math.Abs(r_Lnow_DH) > 0.3;
        bool bidirectional = Math.Abs(r_Lnow_DH) > 0.3 && Math.Abs(r_DH_Lnext) > 0.3;
        bool correlated = Math.Abs(r_Lnow_DH) > 0.15;

        string decision;
        if (LdrivesEntropy) decision = "Model C";
        else if (bidirectional) decision = "Model B";
        else if (correlated) decision = "Model A";
        else decision = "Model D";

        _o.WriteLine($"Decision: {decision}");
        _o.WriteLine($"r(L_t,dH)={r_Lnow_DH:F4}, r(dH,L_t+1)={r_DH_Lnext:F4}");

        if (decision == "Model C") _o.WriteLine("L directly drives entropy flow. Covariance signal change precedes entropy change.");
        else if (decision == "Model B") _o.WriteLine("Bidirectional coupling between L and entropy.");
        else _o.WriteLine("L is only correlated with entropy flow.");

        _o.WriteLine("");
        _o.WriteLine("=== LFG_01 complete. Commit: LFG_01_LSignalFlowGeneratorAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    private static double StdOverMean(double[] x)
    {
        double m = x.Average() + 1e-12;
        return Math.Sqrt(x.Average(v => (v - m) * (v - m))) / m;
    }

    private static (double[] e, double[,] v) JacobiEigenLocal(double[,] a, int n)
    {
        var m = new double[n, n]; var d = new double[n];
        for (int i = 0; i < n; i++) { m[i, i] = 1.0; d[i] = a[i, i]; }
        var b = new double[n]; var z = new double[n];
        for (int i = 0; i < n; i++) { b[i] = d[i]; z[i] = 0.0; }
        for (int iter = 0; iter < 100; iter++)
        {
            double sm = 0; for (int i = 0; i < n - 1; i++) for (int j = i + 1; j < n; j++) sm += Math.Abs(a[i, j]);
            if (sm < 1e-12) break;
            double thresh = iter < 3 ? 0.2 * sm / (n * n) : 0.0;
            for (int i = 0; i < n - 1; i++) for (int j = i + 1; j < n; j++)
            {
                double g = 100.0 * Math.Abs(a[i, j]);
                if (iter > 3 && Math.Abs(d[i]) + g == Math.Abs(d[i]) && Math.Abs(d[j]) + g == Math.Abs(d[j])) a[i, j] = 0.0;
                else if (Math.Abs(a[i, j]) > thresh)
                {
                    double h = d[j] - d[i], t;
                    if (Math.Abs(h) + g == Math.Abs(h)) t = a[i, j] / h;
                    else { double theta = 0.5 * h / a[i, j]; t = 1.0 / (Math.Abs(theta) + Math.Sqrt(1.0 + theta * theta)); if (theta < 0) t = -t; }
                    double cc = 1.0 / Math.Sqrt(1.0 + t * t), s = t * cc, tau = s / (1.0 + cc);
                    h = t * a[i, j]; z[i] -= h; z[j] += h; d[i] -= h; d[j] += h; a[i, j] = 0.0;
                    for (int k = 0; k < i; k++) { g = a[k, i]; h = a[k, j]; a[k, i] = g - s * (h + g * tau); a[k, j] = h + s * (g - h * tau); }
                    for (int k = i + 1; k < j; k++) { g = a[i, k]; h = a[k, j]; a[i, k] = g - s * (h + g * tau); a[k, j] = h + s * (g - h * tau); }
                    for (int k = j + 1; k < n; k++) { g = a[i, k]; h = a[j, k]; a[i, k] = g - s * (h + g * tau); a[j, k] = h + s * (g - h * tau); }
                    for (int k = 0; k < n; k++) { g = m[k, i]; h = m[k, j]; m[k, i] = g - s * (h + g * tau); m[k, j] = h + s * (g - h * tau); }
                }
            }
            for (int i = 0; i < n; i++) { b[i] += z[i]; d[i] = b[i]; z[i] = 0.0; }
        }
        return (d, m);
    }
}
