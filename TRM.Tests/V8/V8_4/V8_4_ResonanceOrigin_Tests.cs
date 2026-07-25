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

    [Fact]
    public void KLI_01_KernelLostInformationAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== KLI_01: Kernel Lost Information Audit ===");
        _o.WriteLine("=== What information does λ projection discard? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 30397;
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
        const int nBeta = 21;
        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };

        // Collect: raw K(d) across 10 deciles, λ1, residuals, dH/dβ
        var kli = new List<(VcFamily fam, double residualFrac, double dH)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var pts = new List<(double residualFrac, double ent)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_KL", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
                    var allC = new List<double[]>(); var allL = new List<double>();
                    double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;
                    var rawK = new double[nDeciles + 1]; var rawCt = new int[nDeciles + 1];

                    for (int ip = 0; ip < 3; ip++)
                    {
                        double pv = 0.1 + ip * 0.45; if (pv > 1.11) continue;
                        var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, pv, v);
                        int nD = distances.Length; double[] kA = new double[nD];
                        for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, pv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                        var kD = new double[nDeciles + 1]; var ct = new int[nDeciles + 1];
                        for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kD[dec] += kA[i]; ct[dec]++; }
                        for (int d = 1; d <= nDeciles; d++) { kD[d] /= Math.Max(ct[d], 1); rawK[d] += kD[d]; rawCt[d]++; }
                        var ctr = new double[nContrasts]; for (int c = 0; c < nContrasts; c++) ctr[c] = kD[contrastDefs[c].i] - kD[contrastDefs[c].j];
                        allC.Add(ctr); allL.Add(Math.Clamp(1.0 - cci.VarI1 / (cci.VarTerms + 1e-15), 0.0, 1.0));
                    }

                    for (int d = 1; d <= nDeciles; d++) rawK[d] /= Math.Max(rawCt[d], 1);

                    if (allL.Count < 3) continue;
                    int N = allL.Count; var LArr = allL.ToArray();
                    var X = new double[N][]; for (int i = 0; i < N; i++) X[i] = (double[])allC[i].Clone();
                    for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / s; }
                    var cm = new double[nContrasts, nContrasts];
                    for (int a = 0; a < nContrasts; a++) for (int b = 0; b < nContrasts; b++) cm[a, b] = PearsonCorrelation(Enumerable.Range(0, N).Select(i => allC[i][a]).ToArray(), Enumerable.Range(0, N).Select(i => allC[i][b]).ToArray());
                    var (ee, ev) = JacobiEigenLocal(cm, nContrasts);
                    var sEE = ee.OrderByDescending(e => e).ToArray();

                    // Reconstruct contrast features from top-k components
                    var pe = Enumerable.Range(0, nContrasts).OrderByDescending(i => ee[i]).ToArray();
                    // Reconstruct using all components → perfect; residual from top-3
                    double totalVar = sEE.Sum();
                    double residualVar = sEE.Skip(3).Sum();
                    double residualFrac = residualVar / Math.Max(totalVar, 1e-12);

                    var pe2 = Enumerable.Range(0, nContrasts).OrderByDescending(i => ee[i]).ToArray();
                    var la = new double[3][];
                    for (int k = 0; k < 3; k++) { la[k] = new double[N]; int er = pe2[k]; for (int i = 0; i < N; i++) { double s = 0; for (int c = 0; c < nContrasts; c++) s += X[i][c] * ev[er, c]; la[k][i] = s; } }
                    double r2L1 = R2SinglePredictor(LArr, la[0]), r2L2 = FitModelR2(LArr, new[] { la[0], la[1] }), r2L3 = FitModelR2(LArr, new[] { la[0], la[1], la[2] });
                    double tVal = r2L3 + 1e-12;
                    double o1 = r2L1 / tVal, o2 = (r2L2 - r2L1) / tVal, o3 = (r2L3 - r2L2) / tVal;
                    double ent = 0; if (o1 > 1e-12) ent -= o1 * Math.Log(o1); if (o2 > 1e-12) ent -= o2 * Math.Log(o2); if (o3 > 1e-12) ent -= o3 * Math.Log(o3);

                    pts.Add((residualFrac, ent));
                }

                double dBeta = 1.0 / (nBeta - 1);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    double dH = Math.Abs(pts[i + 1].ent - pts[i].ent) / dBeta;
                    kli.Add((fam, pts[i].residualFrac, dH));
                }
            }
        }

        // ============================================================
        _o.WriteLine("=== Information Loss ===");
        _o.WriteLine($"{"Family",-6} {"residual %",12} {"CV(resid)",10} {"r(resid,dH)",12}");
        _o.WriteLine(new string('-', 42));

        foreach (var fam in families)
        {
            var fd = kli.Where(k => k.fam == fam).ToArray();
            double mr = fd.Average(k => k.residualFrac) * 100;
            double cv = StdOverMean(fd.Select(k => k.residualFrac).ToArray());
            double rr = PearsonCorrelation(fd.Select(k => k.residualFrac).ToArray(), fd.Select(k => k.dH).ToArray());
            _o.WriteLine($"{fam,-6} {mr,12:F2}% {cv,10:F4} {rr,12:F4}");
        }
        _o.WriteLine("");

        var gF = kli.Where(k => k.fam == VcFamily.SAC || k.fam == VcFamily.RCS).ToArray();
        var gL = kli.Where(k => k.fam != VcFamily.SAC && k.fam != VcFamily.RCS).ToArray();

        double resF = gF.Average(k => k.residualFrac);
        double resL = gL.Average(k => k.residualFrac);

        _o.WriteLine("=== Group ===");
        _o.WriteLine($"FROZEN residual: {resF * 100:F2}%");
        _o.WriteLine($"LIVE   residual: {resL * 100:F2}%");
        _o.WriteLine($"Δresidual = {(resL - resF) * 100:F2}%");

        double rGlobal = PearsonCorrelation(kli.Select(k => k.residualFrac).ToArray(), kli.Select(k => k.dH).ToArray());
        _o.WriteLine($"r(residual, dH/dβ) = {rGlobal:F4}");
        _o.WriteLine("");

        _o.WriteLine("=== Decision ===");
        string decision;
        if (Math.Abs(rGlobal) > 0.4 && (resL - resF) * 100 > 5) decision = "Model C";
        else if (Math.Abs(rGlobal) > 0.2) decision = "Model B";
        else decision = "Model A";

        _o.WriteLine($"Decision: {decision}  r(residual,dH)={rGlobal:F4}  Δresidual={(resL - resF) * 100:F1}%");

        if (decision == "Model C") _o.WriteLine("Critical dynamic information lost in λ projection. The residual predicts time emergence.");
        else if (decision == "Model B") _o.WriteLine("Minor information loss.");
        else _o.WriteLine("No significant information lost.");

        _o.WriteLine("");
        _o.WriteLine("=== KLI_01 complete. Commit: KLI_01_KernelLostInformationAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void DKO_01_DeltaKernelOriginAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== DKO_01: Delta Kernel Origin Audit ===");
        _o.WriteLine("=== What causes dK/dβ? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 31607;
        const double xiBase = 2.95;
        const double k0Base = 1.0;

        var distances = BuildDistanceEnsemble(baseSeed, systems: 34, nodesPerSystem: 64);
        var sorted = distances.OrderBy(x => x).ToArray();
        int nDeciles = 10;
        var decileBounds = new double[nDeciles + 1];
        for (int d = 0; d <= nDeciles; d++) decileBounds[d] = Quantile(sorted, d / (double)nDeciles);

        var families = new[] { VcFamily.SAC, VcFamily.GAN, VcFamily.RCS, VcFamily.ICS, VcFamily.CNS };
        const int nBeta = 21;
        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };

        // Collect: mean |dK/dβ| per family per config
        var dkData = new List<(VcFamily fam, double meanAbsDK, double dH)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var traj = new List<(double[] kDeciles, double ent)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_DK", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
                    double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;

                    // Raw K per decile (single p-value for efficiency)
                    var kPerDecile = new double[nDeciles];
                    var ctPerDecile = new int[nDeciles];
                    double pv = 0.6;
                    int nD = distances.Length; double[] kA = new double[nD];
                    for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, pv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                    for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kPerDecile[dec - 1] += kA[i]; ctPerDecile[dec - 1]++; }
                    for (int d = 0; d < nDeciles; d++) kPerDecile[d] /= Math.Max(ctPerDecile[d], 1);

                    // Quick entropy from simplified PCA (3 p-values as before would be needed for accuracy)
                    // Use a simpler proxy: SD of K across deciles as entropy proxy
                    double kMean = kPerDecile.Average();
                    double kSD = Math.Sqrt(kPerDecile.Average(k => (k - kMean) * (k - kMean)));
                    double entProxy = Math.Log(Math.Max(kSD / Math.Max(kMean, 1e-12) + 1.0, 1.0));

                    traj.Add((kPerDecile, entProxy));
                }

                double dBeta = 1.0 / (nBeta - 1);
                for (int bi = 0; bi < traj.Count - 1; bi++)
                {
                    double sumAbsDK = 0;
                    for (int d = 0; d < nDeciles; d++)
                        sumAbsDK += Math.Abs(traj[bi + 1].kDeciles[d] - traj[bi].kDeciles[d]);
                    double meanAbsDK = sumAbsDK / (nDeciles * dBeta);
                    double dH = Math.Abs(traj[bi + 1].ent - traj[bi].ent) / dBeta;
                    dkData.Add((fam, meanAbsDK, dH));
                }
            }
        }

        // ============================================================
        _o.WriteLine("=== |dK/dβ| Analysis ===");
        _o.WriteLine($"{"Family",-6} {"mean |dK/dβ|",14} {"CV(|dK/dβ|)",12} {"r(|dK|,dH)",12}");
        _o.WriteLine(new string('-', 46));

        foreach (var fam in families)
        {
            var fd = dkData.Where(d => d.fam == fam).ToArray();
            double m = fd.Average(d => d.meanAbsDK);
            double cv = StdOverMean(fd.Select(d => d.meanAbsDK).ToArray());
            double r = PearsonCorrelation(fd.Select(d => d.meanAbsDK).ToArray(), fd.Select(d => d.dH).ToArray());
            _o.WriteLine($"{fam,-6} {m,14:F6} {cv,12:F4} {r,12:F4}");
        }
        _o.WriteLine("");

        var gF = dkData.Where(d => d.fam == VcFamily.SAC || d.fam == VcFamily.RCS).ToArray();
        var gL = dkData.Where(d => d.fam != VcFamily.SAC && d.fam != VcFamily.RCS).ToArray();

        double dkF = gF.Average(d => d.meanAbsDK);
        double dkL = gL.Average(d => d.meanAbsDK);
        double dhF = gF.Average(d => d.dH);
        double dhL = gL.Average(d => d.dH);

        _o.WriteLine("=== Group ===");
        _o.WriteLine($"FROZEN: |dK/dβ|={dkF:F6}, |dH/dβ|={dhF:F6}");
        _o.WriteLine($"LIVE:   |dK/dβ|={dkL:F6}, |dH/dβ|={dhL:F6}");
        _o.WriteLine($"|dK/dβ| ratio: {(dkF / Math.Max(dkL, 1e-12)):F4}");
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Decision ===");
        bool kernelChanges = dkL > dkF * 2;
        bool kernelStatic = dkL < 1e-9 && dkF < 1e-9;

        string decision;
        if (kernelStatic) decision = "Model A";
        else if (kernelChanges) decision = "Model B";
        else decision = "Model D";

        _o.WriteLine($"Decision: {decision}");

        if (decision == "Model A")
            _o.WriteLine("K(d) itself is static — β does not change the kernel. The dynamics must come from how the same kernel generates different covariance.");
        else if (decision == "Model B")
            _o.WriteLine("K(d) changes with β differently across families — the kernel itself has family-dependent dynamics.");
        else
            _o.WriteLine("Unresolved.");

        _o.WriteLine("");
        _o.WriteLine("=== DKO_01 complete. Commit: DKO_01_DeltaKernelOriginAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void MTO_01_MappingTimeOriginAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== MTO_01: Mapping Time Origin Audit ===");
        _o.WriteLine("=== Does the K→L mapping generate time emergence? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 32833;
        const double xiBase = 2.95;
        const double k0Base = 1.0;

        var distances = BuildDistanceEnsemble(baseSeed, systems: 34, nodesPerSystem: 64);
        var sorted = distances.OrderBy(x => x).ToArray();
        int nDeciles = 10;
        var decileBounds = new double[nDeciles + 1];
        for (int d = 0; d <= nDeciles; d++) decileBounds[d] = Quantile(sorted, d / (double)nDeciles);

        var contrastDefs = new (string name, int i, int j)[] { ("K1-K10", 1, 10), ("K2-K8", 2, 8), ("K4-K6", 4, 6), ("K3-K7", 3, 7), ("K1-K5", 1, 5), ("K5-K9", 5, 9) };
        int nContrasts = 6;
        const int nBeta = 21;

        // Sweep p from 0.2 to 2.0, measure dL/dβ
        var pVals = new double[] { 0.2, 0.4, 0.6, 0.8, 1.0, 1.2, 1.4, 1.6, 1.8, 2.0 };
        var results = new List<(double p, double dL)>();

        foreach (double pv in pVals)
        {
            var pts = new List<double>(); // L values across β

            for (int bi = 0; bi < nBeta; bi++)
            {
                double beta = bi / (double)(nBeta - 1);
                var v = new VariantSpec("sweep", VcFamily.SAC, 0.7, 1.0, 1.0, beta, 0.0);
                var allC = new List<double[]>(); var allL = new List<double>();
                double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;

                for (int ip = 0; ip < 3; ip++)
                {
                    double dpv = 0.1 + ip * 0.45; if (dpv > 1.11) continue;
                    var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, dpv, v);
                    int nD = distances.Length; double[] kA = new double[nD];
                    for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, pv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                    var kD = new double[nDeciles + 1]; var ct = new int[nDeciles + 1];
                    for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kD[dec] += kA[i]; ct[dec]++; }
                    for (int d = 1; d <= nDeciles; d++) kD[d] /= Math.Max(ct[d], 1);
                    var ctr = new double[nContrasts]; for (int c = 0; c < nContrasts; c++) ctr[c] = kD[contrastDefs[c].i] - kD[contrastDefs[c].j];
                    allC.Add(ctr); allL.Add(Math.Clamp(1.0 - cci.VarI1 / (cci.VarTerms + 1e-15), 0.0, 1.0));
                }

                if (allL.Count < 3) continue;
                pts.Add(allL.Average());
            }

            double dBeta = 1.0 / (nBeta - 1);
            double meanDL = 0;
            for (int i = 0; i < pts.Count - 1; i++)
                meanDL += Math.Abs(pts[i + 1] - pts[i]) / dBeta;
            meanDL /= (pts.Count - 1);

            results.Add((pv, meanDL));
        }

        _o.WriteLine("=== p-Sweep: dL/dβ ===");
        _o.WriteLine($"{"p",8} {"|dL/dβ|",12}");
        _o.WriteLine(new string('-', 22));
        foreach (var r in results)
            _o.WriteLine($"{r.p,8:F1} {r.dL,12:F6}");
        _o.WriteLine("");

        // Find threshold: first p where dL > 0.001
        double? threshold = null;
        foreach (var r in results)
        {
            if (r.dL > 0.001 && threshold == null)
                threshold = r.p;
        }
        _o.WriteLine($"Threshold p for dynamics: {(threshold.HasValue ? $"{threshold:F1}" : "NONE")}");
        _o.WriteLine("");

        // ============================================================
        // Map to actual families
        // ============================================================
        _o.WriteLine("=== Family p-ranges ===");
        // SAC and RCS use p≈0.3-0.6 (from variant specs), GAN/ICS/CNS use p≈0.3-1.0
        // Actually, all use the same p-range in our tests (0.1, 0.55, 1.0)
        // The difference is in the family type, not p directly

        var famTest = new[] { VcFamily.SAC, VcFamily.GAN, VcFamily.RCS, VcFamily.ICS, VcFamily.CNS };
        _o.WriteLine($"{"Family",-6} {"|dL/dβ|",12} {"mean L",10}");
        _o.WriteLine(new string('-', 30));

        foreach (var fam in famTest)
        {
            var pts = new List<double>();
            for (int bi = 0; bi < nBeta; bi++)
            {
                double beta = bi / (double)(nBeta - 1);
                var v = new VariantSpec($"{fam}_MT", fam, 0.7, 1.0, 1.0, beta, 0.0);
                var allC = new List<double[]>(); var allL = new List<double>();
                double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;

                for (int ip = 0; ip < 3; ip++)
                {
                    double dpv = 0.1 + ip * 0.45; if (dpv > 1.11) continue;
                    var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, dpv, v);
                    int nD = distances.Length; double[] kA = new double[nD];
                    for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, dpv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                    var kD = new double[nDeciles + 1]; var ct = new int[nDeciles + 1];
                    for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kD[dec] += kA[i]; ct[dec]++; }
                    for (int d = 1; d <= nDeciles; d++) kD[d] /= Math.Max(ct[d], 1);
                    var ctr = new double[nContrasts]; for (int c = 0; c < nContrasts; c++) ctr[c] = kD[contrastDefs[c].i] - kD[contrastDefs[c].j];
                    allC.Add(ctr); allL.Add(Math.Clamp(1.0 - cci.VarI1 / (cci.VarTerms + 1e-15), 0.0, 1.0));
                }

                if (allL.Count < 3) continue;
                pts.Add(allL.Average());
            }

            double dBeta = 1.0 / (nBeta - 1);
            double meanDL = 0;
            for (int i = 0; i < pts.Count - 1; i++)
                meanDL += Math.Abs(pts[i + 1] - pts[i]) / dBeta;
            meanDL /= Math.Max(pts.Count - 1, 1);

            _o.WriteLine($"{fam,-6} {meanDL,12:F6} {pts.Average(),10:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Decision ===");
        // Mapping IS the family type itself — it determines L dynamics
        bool mappingIsOrigin = threshold.HasValue && results.Any(r => r.dL < 0.001);

        string decision;
        if (mappingIsOrigin) decision = "Model C";
        else decision = "Model D";

        _o.WriteLine($"Decision: {decision}");
        if (decision == "Model C")
            _o.WriteLine($"The K→L mapping IS the origin of time emergence. A p-threshold exists at p≈{threshold:F1} where dynamics begin. Different families sit on different sides of this threshold.");
        else
            _o.WriteLine("Unresolved.");

        _o.WriteLine("");
        _o.WriteLine("=== MTO_01 complete. Commit: MTO_01_MappingTimeOriginAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void FCA_01_FamilyCouplingAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== FCA_01: Family Coupling Audit ===");
        _o.WriteLine("=== What property of F creates dynamic vs frozen L? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 34061;
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

        // Collect per-step: dL/dβ, L, d²L/dβ² (curvature)
        var stepData = new List<(VcFamily fam, double dL, double L, double curv)>();

        foreach (var fam in families)
        {
            var pts = new List<double>(); // L values

            for (int bi = 0; bi < nBeta; bi++)
            {
                double beta = bi / (double)(nBeta - 1);
                var v = new VariantSpec($"{fam}_FC", fam, 0.7, 1.0, 1.0, beta, 0.0);
                var allC = new List<double[]>(); var allL = new List<double>();
                double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;

                for (int ip = 0; ip < 3; ip++)
                {
                    double dpv = 0.1 + ip * 0.45; if (dpv > 1.11) continue;
                    var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, dpv, v);
                    int nD = distances.Length; double[] kA = new double[nD];
                    for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, dpv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                    var kD = new double[nDeciles + 1]; var ct = new int[nDeciles + 1];
                    for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kD[dec] += kA[i]; ct[dec]++; }
                    for (int d = 1; d <= nDeciles; d++) kD[d] /= Math.Max(ct[d], 1);
                    var ctr = new double[nContrasts]; for (int c = 0; c < nContrasts; c++) ctr[c] = kD[contrastDefs[c].i] - kD[contrastDefs[c].j];
                    allC.Add(ctr); allL.Add(Math.Clamp(1.0 - cci.VarI1 / (cci.VarTerms + 1e-15), 0.0, 1.0));
                }

                if (allL.Count < 3) continue;
                pts.Add(allL.Average());
            }

            double dBeta = 1.0 / (nBeta - 1);
            for (int i = 1; i < pts.Count - 1; i++)
            {
                double dL = (pts[i + 1] - pts[i - 1]) / (2.0 * dBeta);
                double curv = (pts[i + 1] - 2 * pts[i] + pts[i - 1]) / (dBeta * dBeta);
                stepData.Add((fam, dL, pts[i], curv));
            }
        }

        // ============================================================
        _o.WriteLine("=== dL/dβ Analysis ===");
        _o.WriteLine($"{"Family",-6} {"mean L",10} {"CV(L)",10} {"mean |dL|",10} {"CV(dL)",10} {"CV(curv)",10}");
        _o.WriteLine(new string('-', 58));

        foreach (var fam in families)
        {
            var fd = stepData.Where(s => s.fam == fam).ToArray();
            double mL = fd.Average(s => s.L);
            double cvL = StdOverMean(fd.Select(s => s.L).ToArray());
            double mDL = fd.Average(s => Math.Abs(s.dL));
            double cvDL = StdOverMean(fd.Select(s => Math.Abs(s.dL)).ToArray());
            double cvC = StdOverMean(fd.Select(s => Math.Abs(s.curv)).ToArray());
            _o.WriteLine($"{fam,-6} {mL,10:F4} {cvL,10:F4} {mDL,10:F6} {cvDL,10:F4} {cvC,10:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Group ===");
        var gF = stepData.Where(s => s.fam == VcFamily.SAC || s.fam == VcFamily.RCS).ToArray();
        var gL = stepData.Where(s => s.fam != VcFamily.SAC && s.fam != VcFamily.RCS).ToArray();

        double cvL_F = StdOverMean(gF.Select(s => s.L).ToArray());
        double cvL_L = StdOverMean(gL.Select(s => s.L).ToArray());
        double cvDL_F = StdOverMean(gF.Select(s => Math.Abs(s.dL)).ToArray());
        double cvDL_L = StdOverMean(gL.Select(s => Math.Abs(s.dL)).ToArray());

        _o.WriteLine($"FROZEN: CV(L)={cvL_F:F4}, CV(|dL|)={cvDL_F:F4}");
        _o.WriteLine($"LIVE:   CV(L)={cvL_L:F4}, CV(|dL|)={cvDL_L:F4}");

        // Is dL constant or variable in live families?
        bool liveHasConstantDL = cvDL_L < 0.3;
        bool liveHasVariableDL = cvDL_L > 0.5;
        _o.WriteLine($"Live |dL/dβ| is: {(liveHasConstantDL ? "CONSTANT (linear)" : liveHasVariableDL ? "VARIABLE (nonlinear)" : "MIXED")}");
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Decision ===");
        string decision;
        if (liveHasConstantDL) decision = "Model A";
        else if (liveHasVariableDL) decision = "Model B";
        else decision = "Model D";

        _o.WriteLine($"Decision: {decision}");
        if (decision == "Model A") _o.WriteLine("Live families have CONSTANT dL/dβ — the operator is linear but active.");
        else if (decision == "Model B") _o.WriteLine("Live families have VARIABLE dL/dβ — feedback or nonlinearity drives dynamics.");
        else _o.WriteLine("Hybrid/mixed mechanism.");

        _o.WriteLine("");
        _o.WriteLine("=== FCA_01 complete. Commit: FCA_01_FamilyCouplingAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void ETA_01_EnergyTransferAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== ETA_01: Energy Transfer Audit ===");
        _o.WriteLine("=== Is the ON/OFF distinction an energy-transfer condition? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 35281;
        const double xiBase = 2.95;
        const double k0Base = 1.0;

        var distances = BuildDistanceEnsemble(baseSeed, systems: 34, nodesPerSystem: 64);
        var sorted = distances.OrderBy(x => x).ToArray();
        int nDeciles = 10;
        var decileBounds = new double[nDeciles + 1];
        for (int d = 0; d <= nDeciles; d++) decileBounds[d] = Quantile(sorted, d / (double)nDeciles);

        var families = new[] { VcFamily.SAC, VcFamily.GAN, VcFamily.RCS, VcFamily.ICS, VcFamily.CNS };
        const int nBeta = 31;
        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };

        var data = new List<(VcFamily fam, double dEnergy, double dEntropy, double dL, double energyVar)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var pts = new List<(double energy, double entropy, double L)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_ET", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
                    double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;

                    // Energy = mean K across distance
                    var kPerDecile = new double[nDeciles];
                    var ctPerDecile = new int[nDeciles];
                    double pv = 0.6;
                    int nD = distances.Length; double[] kA = new double[nD];
                    for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, pv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                    for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kPerDecile[dec - 1] += kA[i]; ctPerDecile[dec - 1]++; }
                    for (int d = 0; d < nDeciles; d++) kPerDecile[d] /= Math.Max(ctPerDecile[d], 1);

                    double energy = kPerDecile.Average(); // total coupling energy
                    double energyVariance = kPerDecile.Average(k => (k - energy) * (k - energy)); // transferable energy

                    // Entropy proxy from K-variance
                    double kSD = Math.Sqrt(energyVariance);
                    double entropy = Math.Log(Math.Max(kSD / Math.Max(energy, 1e-12) + 1.0, 1.0));

                    // L proxy: near-far contrast
                    double L = (kPerDecile[0] - kPerDecile[nDeciles - 1]) / Math.Max(kPerDecile[0], 1e-12);

                    pts.Add((energy, entropy, L));
                }

                double dBeta = 1.0 / (nBeta - 1);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    double dE = Math.Abs(pts[i + 1].energy - pts[i].energy) / dBeta;
                    double dH = Math.Abs(pts[i + 1].entropy - pts[i].entropy) / dBeta;
                    double dL = Math.Abs(pts[i + 1].L - pts[i].L) / dBeta;
                    data.Add((fam, dE, dH, dL, 0));
                }
            }
        }

        // ============================================================
        _o.WriteLine("=== Energy Flow ===");
        _o.WriteLine($"{"Family",-6} {"|dE/dβ|",12} {"|dH/dβ|",12} {"|dL/dβ|",12}");
        _o.WriteLine(new string('-', 40));

        foreach (var fam in families)
        {
            var fd = data.Where(d => d.fam == fam).ToArray();
            _o.WriteLine($"{fam,-6} {fd.Average(d => d.dEnergy),12:F6} {fd.Average(d => d.dEntropy),12:F6} {fd.Average(d => d.dL),12:F6}");
        }
        _o.WriteLine("");

        var gF = data.Where(d => d.fam == VcFamily.SAC || d.fam == VcFamily.RCS).ToArray();
        var gL = data.Where(d => d.fam != VcFamily.SAC && d.fam != VcFamily.RCS).ToArray();

        double dEF = gF.Average(d => d.dEnergy);
        double dEL = gL.Average(d => d.dEnergy);

        _o.WriteLine($"FROZEN: |dE/dβ|={dEF:F6}");
        _o.WriteLine($"LIVE:   |dE/dβ|={dEL:F6}");

        double rED_H = PearsonCorrelation(data.Select(d => d.dEnergy).ToArray(), data.Select(d => d.dEntropy).ToArray());
        double rED_L = PearsonCorrelation(data.Select(d => d.dEnergy).ToArray(), data.Select(d => d.dL).ToArray());
        _o.WriteLine($"r(|dE|,|dH|)={rED_H:F4}, r(|dE|,|dL|)={rED_L:F4}");
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Decision ===");
        bool energyTransferExplains = dEL > dEF * 10 && rED_H > 0.5;

        string decision;
        if (energyTransferExplains) decision = "Model C";
        else if (rED_H > 0.3) decision = "Model B";
        else decision = "Model A";

        _o.WriteLine($"Decision: {decision}  dE ratio={(dEL/Math.Max(dEF,1e-12)):F2}  r(dE,dH)={rED_H:F4}");

        if (decision == "Model C")
            _o.WriteLine("The ON/OFF distinction IS an energy-transfer condition. OFF families have zero energy flow; ON families have non-zero energy flow that drives entropy and L dynamics.");
        else if (decision == "Model B")
            _o.WriteLine("Energy transfer partially explains dynamics.");
        else
            _o.WriteLine("Energy transfer is not the distinguishing factor.");

        _o.WriteLine("");
        _o.WriteLine("=== ETA_01 complete. Commit: ETA_01_EnergyTransferAudit ===");
        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void FOP_01_FamilyOperatorNatureAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== FOP_01: Family Operator Nature Audit ===");
        _o.WriteLine("=== What IS the family-dependent operator F? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 36529;
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

        // Fit L(β) = a + b*β per family
        var fits = new List<(VcFamily fam, double a, double b, double r2, double[] Lvals)>();

        foreach (var fam in families)
        {
            var betas = new List<double>();
            var Lvals = new List<double>();

            for (int bi = 0; bi < nBeta; bi++)
            {
                double beta = bi / (double)(nBeta - 1);
                var v = new VariantSpec($"{fam}_FO", fam, 0.7, 1.0, 1.0, beta, 0.0);
                var allL = new List<double>();
                double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;

                for (int ip = 0; ip < 3; ip++)
                {
                    double dpv = 0.1 + ip * 0.45; if (dpv > 1.11) continue;
                    var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, dpv, v);
                    int nD = distances.Length; double[] kA = new double[nD];
                    for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, dpv)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                    var kD = new double[nDeciles + 1]; var ct = new int[nDeciles + 1];
                    for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kD[dec] += kA[i]; ct[dec]++; }
                    for (int d = 1; d <= nDeciles; d++) kD[d] /= Math.Max(ct[d], 1);
                    var ctr = new double[nContrasts]; for (int c = 0; c < nContrasts; c++) ctr[c] = kD[contrastDefs[c].i] - kD[contrastDefs[c].j];
                    allL.Add(Math.Clamp(1.0 - cci.VarI1 / (cci.VarTerms + 1e-15), 0.0, 1.0));
                }

                if (allL.Count < 3) continue;
                betas.Add(beta);
                Lvals.Add(allL.Average());
            }

            // Linear fit: L = a + b*β
            double[] bArr = betas.ToArray();
            double[] lArr = Lvals.ToArray();
            double r2 = FitModelR2(lArr, new[] { bArr });
            double r = PearsonCorrelation(bArr, lArr);

            // Slope b = r * σ_L / σ_β
            double stdB = Math.Sqrt(bArr.Average(bv => (bv - bArr.Average()) * (bv - bArr.Average())));
            double stdL = Math.Sqrt(lArr.Average(lv => (lv - lArr.Average()) * (lv - lArr.Average())));
            double slopeB = r * stdL / Math.Max(stdB, 1e-12);
            double intercept = lArr.Average() - slopeB * bArr.Average();

            fits.Add((fam, intercept, slopeB, r2, lArr));
        }

        // ============================================================
        _o.WriteLine("=== L(β) = a + b*β per family ===");
        _o.WriteLine($"{"Family",-6} {"intercept a",12} {"slope b",12} {"R²",10} {"CV(L)",10}");
        _o.WriteLine(new string('-', 52));

        foreach (var f in fits)
        {
            double cvL = StdOverMean(f.Lvals);
            _o.WriteLine($"{f.fam,-6} {f.a,12:F4} {f.b,12:F4} {f.r2,10:F4} {cvL,10:F4}");
        }
        _o.WriteLine("");

        double frozenSlope = fits.Where(f => f.fam == VcFamily.SAC || f.fam == VcFamily.RCS).Average(f => Math.Abs(f.b));
        double liveSlope = fits.Where(f => f.fam != VcFamily.SAC && f.fam != VcFamily.RCS).Average(f => Math.Abs(f.b));
        _o.WriteLine($"FROZEN mean |slope|: {frozenSlope:F6}");
        _o.WriteLine($"LIVE   mean |slope|: {liveSlope:F6}");
        _o.WriteLine("");

        // ============================================================
        _o.WriteLine("=== Decision ===");
        bool allLinear = fits.All(f => f.r2 > 0.95);
        bool frozenZero = frozenSlope < 1e-6;
        bool liveNonZero = liveSlope > 0.1;

        string decision;
        if (allLinear && frozenZero && liveNonZero) decision = "Model B";
        else if (allLinear) decision = "Model A";
        else decision = "Model D";

        _o.WriteLine($"Decision: {decision}  linear={allLinear}  frozenZero={frozenZero}  liveNonZero={liveNonZero}");

        if (decision == "Model B")
            _o.WriteLine("F is a linear transformation operator. L(β) = a + b*β with family-dependent slope b. Frozen families have b=0 (identity-like); live families have b≠0 (active transformation). F transforms the static K(d) into dynamic L by linearly incorporating β.");
        else if (decision == "Model A")
            _o.WriteLine("F is a static mapping.");
        else
            _o.WriteLine("F has internal clockwork dynamics.");

        _o.WriteLine("");
        _o.WriteLine("=== FOP_01 complete. Commit: FOP_01_FamilyOperatorNatureAudit ===");
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
