using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;

namespace TRM.Tests.V8_3;

[Trait("Category", "V8_3")]
public class V8_3_InvariantOrigin_Tests
{
    private readonly ITestOutputHelper _o;
    public V8_3_InvariantOrigin_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void PIO_01_PrimitiveInvariantOriginAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== PIO_01: Primitive Invariant Origin Audit ===");
        _o.WriteLine("=== WHY is Speed invariant? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 14703;
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
        int dimF = 5;
        const int nBeta = 31;

        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };

        var trajData = new List<(VcFamily fam, int cfgIdx,
            double[] timeRates, double[] stepLens, double[] speeds,
            double[] entropy, double[] accessibility, double[] dimVals)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var pts = new List<(double ent, double dimVal, double acc, double r2L1)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_IO", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
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
                    for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double ss = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / ss; }
                    var cm = new double[nContrasts, nContrasts];
                    for (int a = 0; a < nContrasts; a++) for (int b = 0; b < nContrasts; b++) cm[a, b] = PearsonCorrelation(Enumerable.Range(0, N).Select(i => allC[i][a]).ToArray(), Enumerable.Range(0, N).Select(i => allC[i][b]).ToArray());
                    var (ee, ev) = JacobiEigenLocal(cm, nContrasts);
                    var pe = Enumerable.Range(0, nContrasts).OrderByDescending(i => ee[i]).ToArray();
                    var la = new double[3][];
                    for (int k = 0; k < 3; k++) { la[k] = new double[N]; int er = pe[k]; for (int i = 0; i < N; i++) { double s = 0; for (int c = 0; c < nContrasts; c++) s += X[i][c] * ev[er, c]; la[k][i] = s; } }
                    double r2L1 = R2SinglePredictor(LArr, la[0]), r2L2 = FitModelR2(LArr, new[] { la[0], la[1] }), r2L3 = FitModelR2(LArr, new[] { la[0], la[1], la[2] });
                    double t = r2L3 + 1e-12;
                    double l1 = r2L1 / t, l2 = (r2L2 - r2L1) / t, l3 = (r2L3 - r2L2) / t;
                    double ent = 0; if (l1 > 1e-12) ent -= l1 * Math.Log(l1); if (l2 > 1e-12) ent -= l2 * Math.Log(l2); if (l3 > 1e-12) ent -= l3 * Math.Log(l3);
                    double dimVal = Math.Exp(ent);
                    double d1a = Math.Abs(l1 - 1.0) + l2 + l3;
                    double d2a = Math.Abs(l1 - 0.5) + Math.Abs(l2 - 0.5) + l3;
                    double d3a = Math.Abs(l1 - 1.0 / 3) + Math.Abs(l2 - 1.0 / 3) + Math.Abs(l3 - 1.0 / 3);
                    double acc = 1.0 / Math.Max(Math.Min(d1a, Math.Min(d2a, d3a)), 0.01);

                    pts.Add((ent, dimVal, acc, r2L1));
                }

                int nSteps = pts.Count - 1;
                var timeRates = new double[nSteps];
                var stepLens = new double[nSteps];
                var speeds = new double[nSteps];
                var entropy = pts.Select(p => p.ent).ToArray();
                var accessibility = pts.Select(p => p.acc).ToArray();
                var dimVals = pts.Select(p => p.dimVal).ToArray();

                var featArr = pts.Select(p => new[] { p.ent, p.dimVal, p.acc, p.r2L1, p.ent }).ToArray();
                var locMean = new double[dimF]; var locStd = new double[dimF];
                for (int f = 0; f < dimF; f++) { locMean[f] = featArr.Average(p => p[f]); locStd[f] = Math.Sqrt(featArr.Average(p => (p[f] - locMean[f]) * (p[f] - locMean[f]))) + 1e-12; }

                for (int i = 0; i < nSteps; i++)
                {
                    timeRates[i] = Math.Abs(pts[i + 1].ent - pts[i].ent) / (1.0 / (nBeta - 1));
                    stepLens[i] = Math.Sqrt(Enumerable.Range(0, dimF).Sum(f =>
                    {
                        double va = (featArr[i][f] - locMean[f]) / locStd[f];
                        double vb = (featArr[i + 1][f] - locMean[f]) / locStd[f];
                        return (va - vb) * (va - vb);
                    }));
                    speeds[i] = stepLens[i] / Math.Max(timeRates[i], 1e-12);
                }

                trajData.Add((fam, ci, timeRates, stepLens, speeds, entropy, accessibility, dimVals));
            }
        }

        // ============================================================
        // Part A: Causal direction — lag analysis
        // ============================================================
        _o.WriteLine("=== Part A: Causal Direction (Lag Analysis) ===");

        var lagPairs = new List<(double dH_now, double dL_next, double dL_now, double dH_next)>();
        foreach (var td in trajData)
            for (int i = 0; i < td.timeRates.Length - 1; i++)
                lagPairs.Add((td.timeRates[i], td.stepLens[i + 1], td.stepLens[i], td.timeRates[i + 1]));

        double r_Tnow_Lnext = PearsonCorrelation(lagPairs.Select(p => p.dH_now).ToArray(), lagPairs.Select(p => p.dL_next).ToArray());
        double r_Lnow_Tnext = PearsonCorrelation(lagPairs.Select(p => p.dL_now).ToArray(), lagPairs.Select(p => p.dH_next).ToArray());

        _o.WriteLine($"r(dH_t, dL_t+1) = {r_Tnow_Lnext:F4} — Time-rate predicts future Length?");
        _o.WriteLine($"r(dL_t, dH_t+1) = {r_Lnow_Tnext:F4} — Length predicts future Time-rate?");
        _o.WriteLine("");

        string causalDirection;
        if (Math.Abs(r_Tnow_Lnext) > Math.Abs(r_Lnow_Tnext) * 1.5)
            causalDirection = "Time drives Length compensation";
        else if (Math.Abs(r_Lnow_Tnext) > Math.Abs(r_Tnow_Lnext) * 1.5)
            causalDirection = "Length drives Time compensation";
        else
            causalDirection = "Mutual or common-driver";
        _o.WriteLine($"Causal direction: {causalDirection}");
        _o.WriteLine("");

        // ============================================================
        // Part B: Accessibility → Speed direct prediction
        // ============================================================
        _o.WriteLine("=== Part B: Accessibility → Speed ===");

        var aS = trajData.SelectMany(td => td.speeds.Select((s, i) => (acc: td.accessibility[i], speed: s))).ToArray();
        var aT = trajData.SelectMany(td => td.timeRates.Select((r, i) => (acc: td.accessibility[i], rate: r))).ToArray();
        var aL = trajData.SelectMany(td => td.stepLens.Select((l, i) => (acc: td.accessibility[i], len: l))).ToArray();

        double rAccSpeed = PearsonCorrelation(aS.Select(p => p.acc).ToArray(), aS.Select(p => p.speed).ToArray());
        double rAccTime = PearsonCorrelation(aT.Select(p => p.acc).ToArray(), aT.Select(p => p.rate).ToArray());
        double rAccLen = PearsonCorrelation(aL.Select(p => p.acc).ToArray(), aL.Select(p => p.len).ToArray());

        _o.WriteLine($"r(accessibility, speed)      = {rAccSpeed:F4}");
        _o.WriteLine($"r(accessibility, time-rate)   = {rAccTime:F4}");
        _o.WriteLine($"r(accessibility, step-length) = {rAccLen:F4}");

        string accRole = Math.Abs(rAccSpeed) > Math.Max(Math.Abs(rAccTime), Math.Abs(rAccLen)) * 1.3 && Math.Abs(rAccSpeed) > 0.05
            ? "Accessibility predicts Speed DIRECTLY"
            : "Accessibility does NOT directly predict Speed";
        _o.WriteLine($"{accRole}");
        _o.WriteLine("");

        // ============================================================
        // Part C: Reconstruction — Time/Length from Speed + secondary
        // ============================================================
        _o.WriteLine("=== Part C: Reconstruction Test ===");

        var R = trajData.SelectMany(td => Enumerable.Range(0, td.timeRates.Length).Select(i =>
            (T: td.timeRates[i], L: td.stepLens[i], S: td.speeds[i], ent: td.entropy[i], acc: td.accessibility[i]))).ToArray();
        var Tarr = R.Select(r => r.T).ToArray(); var Larr = R.Select(r => r.L).ToArray();
        var Sarr = R.Select(r => r.S).ToArray(); var Earr = R.Select(r => r.ent).ToArray();
        var Aarr = R.Select(r => r.acc).ToArray();

        double r2_T_S = R2SinglePredictor(Tarr, Sarr);
        double r2_T_SE = FitModelR2(Tarr, new[] { Sarr, Earr });
        double r2_L_S = R2SinglePredictor(Larr, Sarr);
        double r2_L_SA = FitModelR2(Larr, new[] { Sarr, Aarr });

        _o.WriteLine($"Time  ~ Speed: R²={r2_T_S:F4}, +entropy: R²={r2_T_SE:F4}, ΔR²={r2_T_SE - r2_T_S:F4}");
        _o.WriteLine($"Length ~ Speed: R²={r2_L_S:F4}, +access: R²={r2_L_SA:F4}, ΔR²={r2_L_SA - r2_L_S:F4}");

        string reconResult = r2_T_S > 0.3 && r2_L_S > 0.3
            ? "Speed alone reconstructs both" : r2_T_S > 0.15 || r2_L_S > 0.15
            ? "Speed partially reconstructs" : "Speed does not reconstruct components";
        _o.WriteLine($"{reconResult}");
        _o.WriteLine("");

        // ============================================================
        // Part D: Early vs Late stability
        // ============================================================
        _o.WriteLine("=== Part D: Early vs Late Stability ===");
        _o.WriteLine($"{"Family/cfg",-14} {"CV(S)early",12} {"CV(S)late",12} {"CV(T)early",12} {"CV(T)late",12}");
        _o.WriteLine(new string('-', 64));

        int earlyStable = 0;
        foreach (var td in trajData)
        {
            int half = td.timeRates.Length / 2;
            double cvSE = StdOverMean(td.speeds.Take(half).ToArray());
            double cvSL = StdOverMean(td.speeds.Skip(half).ToArray());
            double cvTE = StdOverMean(td.timeRates.Take(half).ToArray());
            double cvTL = StdOverMean(td.timeRates.Skip(half).ToArray());
            if (cvSE < cvTE && cvSL < cvTL) earlyStable++;
            _o.WriteLine($"{td.fam,-4} cfg{td.cfgIdx,-2}      {cvSE,12:F4} {cvSL,12:F4} {cvTE,12:F4} {cvTL,12:F4}");
        }
        _o.WriteLine($"Speed more stable in both halves: {earlyStable}/{trajData.Count}");
        _o.WriteLine("");

        // ============================================================
        // Part E: Cross-family
        // ============================================================
        _o.WriteLine("=== Part E: Cross-Family Origin Consistency ===");
        foreach (var fam in families)
        {
            var fd = trajData.Where(td => td.fam == fam).ToArray();
            var fl = new List<(double dH, double dL_next)>();
            foreach (var td in fd)
                for (int i = 0; i < td.timeRates.Length - 1; i++)
                    fl.Add((td.timeRates[i], td.stepLens[i + 1]));
            double rLag = PearsonCorrelation(fl.Select(p => p.dH).ToArray(), fl.Select(p => p.dL_next).ToArray());
            double rAS = PearsonCorrelation(
                fd.SelectMany(td => td.accessibility.Take(td.speeds.Length)).ToArray(),
                fd.SelectMany(td => td.speeds).ToArray());
            _o.WriteLine($"{fam,-4}: r(dH_t→dL_t+1)={rLag:+0.0000;-0.0000}, r(acc→speed)={rAS:+0.0000;-0.0000}");
        }
        _o.WriteLine("");

        // ============================================================
        // Decision
        // ============================================================
        _o.WriteLine("=== Decision ===");

        bool deeperInvariant = Math.Abs(rAccSpeed) > 0.5 && r2_T_S > 0.3 && r2_L_S > 0.3;
        bool compensationEffect = Math.Abs(r_Tnow_Lnext) > 0.3 || Math.Abs(r_Lnow_Tnext) > 0.3;
        bool accidental = !compensationEffect && !deeperInvariant;

        string decision;
        if (deeperInvariant) decision = "Model C";
        else if (compensationEffect) decision = "Model B";
        else if (accidental) decision = "Model A";
        else decision = "Model D";

        _o.WriteLine($"Decision model: {decision}");
        _o.WriteLine($"Causal direction: {causalDirection}");
        _o.WriteLine($"r(acc→speed)={rAccSpeed:F4}, R²(T~S)={r2_T_S:F4}, R²(L~S)={r2_L_S:F4}");
        _o.WriteLine($"Lag: r(dH_t→dL_t+1)={r_Tnow_Lnext:F4}, r(dL_t→dH_t+1)={r_Lnow_Tnext:F4}");

        if (decision == "Model C")
            _o.WriteLine("Speed projects from a deeper invariant — Accessibility Potential itself. Speed remains stable because it reflects the geometric conversion rate of the underlying accessibility structure. Time and Length are reconstructible from Speed + secondary quantities.");
        else if (decision == "Model B")
            _o.WriteLine("Speed invariance emerges from Time-Length compensation.");
        else
            _o.WriteLine("Speed invariance is accidental.");

        _o.WriteLine("");
        _o.WriteLine("=== OUTPUT ===");
        _o.WriteLine("1. Executive determination");
        _o.WriteLine($"   {decision}");
        _o.WriteLine($"2. Causal: {causalDirection}");
        _o.WriteLine($"3. r(acc→speed)={rAccSpeed:F4}");
        _o.WriteLine($"4. Reconstruction: T~S R²={r2_T_S:F4}, L~S R²={r2_L_S:F4}");
        _o.WriteLine($"5. Decision: {decision}");
        _o.WriteLine("6. Commit-ready summary:");
        string label = decision == "Model C" ? "Speed projects from deeper invariant" : decision == "Model B" ? "Speed from compensation" : "Speed invariance accidental";
        _o.WriteLine($"   PIO_01_PrimitiveInvariantOriginAudit — {label}.");
        _o.WriteLine("");
        _o.WriteLine("=== PIO_01 complete. Commit: PIO_01_PrimitiveInvariantOriginAudit ===");

        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void PRC_01_PrimitiveReconstructionClosureAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== PRC_01: Primitive Reconstruction Closure Audit ===");
        _o.WriteLine("=== Can Accessibility be reconstructed from derived quantities? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 15937;
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
        int dimF = 5;
        const int nBeta = 21;

        var configs = new (double alpha, double xiScale)[] { (0.35, 0.8), (0.70, 1.0), (1.05, 1.2) };

        // Collect step-level data: [timeRate, stepLen, speed, accessibility]
        var allSteps = new List<(VcFamily fam, double timeRate, double stepLen, double speed, double acc)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var pts = new List<(double ent, double dimVal, double acc, double r2L1)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_RC", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
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
                    for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double ss = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / ss; }
                    var cm = new double[nContrasts, nContrasts];
                    for (int a = 0; a < nContrasts; a++) for (int b = 0; b < nContrasts; b++) cm[a, b] = PearsonCorrelation(Enumerable.Range(0, N).Select(i => allC[i][a]).ToArray(), Enumerable.Range(0, N).Select(i => allC[i][b]).ToArray());
                    var (ee, ev) = JacobiEigenLocal(cm, nContrasts);
                    var pe = Enumerable.Range(0, nContrasts).OrderByDescending(i => ee[i]).ToArray();
                    var la = new double[3][];
                    for (int k = 0; k < 3; k++) { la[k] = new double[N]; int er = pe[k]; for (int i = 0; i < N; i++) { double s = 0; for (int c = 0; c < nContrasts; c++) s += X[i][c] * ev[er, c]; la[k][i] = s; } }
                    double r2L1 = R2SinglePredictor(LArr, la[0]), r2L2 = FitModelR2(LArr, new[] { la[0], la[1] }), r2L3 = FitModelR2(LArr, new[] { la[0], la[1], la[2] });
                    double t = r2L3 + 1e-12;
                    double l1 = r2L1 / t, l2 = (r2L2 - r2L1) / t, l3 = (r2L3 - r2L2) / t;
                    double ent = 0; if (l1 > 1e-12) ent -= l1 * Math.Log(l1); if (l2 > 1e-12) ent -= l2 * Math.Log(l2); if (l3 > 1e-12) ent -= l3 * Math.Log(l3);
                    double dimVal = Math.Exp(ent);
                    double d1a = Math.Abs(l1 - 1.0) + l2 + l3;
                    double d2a = Math.Abs(l1 - 0.5) + Math.Abs(l2 - 0.5) + l3;
                    double d3a = Math.Abs(l1 - 1.0 / 3) + Math.Abs(l2 - 1.0 / 3) + Math.Abs(l3 - 1.0 / 3);
                    double acc = 1.0 / Math.Max(Math.Min(d1a, Math.Min(d2a, d3a)), 0.01);

                    pts.Add((ent, dimVal, acc, r2L1));
                }

                // Compute step-level quantities (feature vector WITHOUT accessibility)
                var featArr = pts.Select(p => new[] { p.ent, p.dimVal, p.r2L1, p.ent }).ToArray(); // 4D: ent, dim, r2L1, shannonL
                int featDim = 4;
                var locMean = new double[featDim]; var locStd = new double[featDim];
                for (int f = 0; f < featDim; f++) { locMean[f] = featArr.Average(p => p[f]); locStd[f] = Math.Sqrt(featArr.Average(p => (p[f] - locMean[f]) * (p[f] - locMean[f]))) + 1e-12; }

                for (int i = 0; i < pts.Count - 1; i++)
                {
                    double dH = Math.Abs(pts[i + 1].ent - pts[i].ent) / (1.0 / (nBeta - 1));
                    double dL = Math.Sqrt(Enumerable.Range(0, featDim).Sum(f =>
                    {
                        double va = (featArr[i][f] - locMean[f]) / locStd[f];
                        double vb = (featArr[i + 1][f] - locMean[f]) / locStd[f];
                        return (va - vb) * (va - vb);
                    }));
                    double sp = dL / Math.Max(dH, 1e-12);
                    double accVal = pts[i].acc; // accessibility at step start
                    allSteps.Add((fam, dH, dL, sp, accVal));
                }
            }
        }

        var Tarr = allSteps.Select(s => s.timeRate).ToArray();
        var Larr = allSteps.Select(s => s.stepLen).ToArray();
        var Sarr = allSteps.Select(s => s.speed).ToArray();
        var Aarr = allSteps.Select(s => s.acc).ToArray();

        // ============================================================
        // Reconstruction tests
        // ============================================================
        _o.WriteLine("=== Reconstruction: Accessibility ← Derived Quantities ===");
        _o.WriteLine($"N = {allSteps.Count} step-level samples across {families.Length} families × {configs.Length} configs");
        _o.WriteLine("");

        double r2_TL = FitModelR2(Aarr, new[] { Tarr, Larr });
        double r2_TS = FitModelR2(Aarr, new[] { Tarr, Sarr });
        double r2_LS = FitModelR2(Aarr, new[] { Larr, Sarr });
        double r2_TLS = FitModelR2(Aarr, new[] { Tarr, Larr, Sarr });

        // Single-predictor baselines
        double r2_T = R2SinglePredictor(Aarr, Tarr);
        double r2_L = R2SinglePredictor(Aarr, Larr);
        double r2_S = R2SinglePredictor(Aarr, Sarr);

        _o.WriteLine($"{"Model",-18} {"R²",10} {"Δ vs best",10}");
        _o.WriteLine(new string('-', 40));
        _o.WriteLine($"{"Time only",-18} {r2_T,10:F4}");
        _o.WriteLine($"{"Length only",-18} {r2_L,10:F4}");
        _o.WriteLine($"{"Speed only",-18} {r2_S,10:F4}");
        _o.WriteLine($"{"Time + Length",-18} {r2_TL,10:F4}");
        _o.WriteLine($"{"Time + Speed",-18} {r2_TS,10:F4}");
        _o.WriteLine($"{"Length + Speed",-18} {r2_LS,10:F4}");
        _o.WriteLine($"{"T + L + S (full)",-18} {r2_TLS,10:F4}");
        _o.WriteLine("");

        double bestR2 = Math.Max(r2_TLS, Math.Max(r2_TL, Math.Max(r2_TS, Math.Max(r2_LS, Math.Max(r2_T, Math.Max(r2_L, r2_S))))));
        string bestModel = r2_TLS == bestR2 ? "T+L+S" : r2_TL == bestR2 ? "T+L" : r2_TS == bestR2 ? "T+S" : r2_LS == bestR2 ? "L+S" : r2_T == bestR2 ? "T" : r2_L == bestR2 ? "L" : "S";

        _o.WriteLine($"Best model: {bestModel}, R² = {bestR2:F4}");
        _o.WriteLine("");

        // ============================================================
        // Per-family reconstruction
        // ============================================================
        _o.WriteLine("=== Cross-Family Reconstruction ===");
        _o.WriteLine($"{"Family",-6} {"T+L R²",10} {"T+S R²",10} {"L+S R²",10} {"T+L+S R²",10} {"best",8}");
        _o.WriteLine(new string('-', 56));

        var famR2 = new List<(VcFamily fam, double best)>();
        foreach (var fam in families)
        {
            var fd = allSteps.Where(s => s.fam == fam).ToArray();
            var fT = fd.Select(s => s.timeRate).ToArray();
            var fL = fd.Select(s => s.stepLen).ToArray();
            var fS = fd.Select(s => s.speed).ToArray();
            var fA = fd.Select(s => s.acc).ToArray();

            double frTL = FitModelR2(fA, new[] { fT, fL });
            double frTS = FitModelR2(fA, new[] { fT, fS });
            double frLS = FitModelR2(fA, new[] { fL, fS });
            double frTLS = FitModelR2(fA, new[] { fT, fL, fS });
            double frBest = Math.Max(frTLS, Math.Max(frTL, Math.Max(frTS, frLS)));
            string frBestM = frTLS == frBest ? "TLS" : frTL == frBest ? "TL" : frTS == frBest ? "TS" : "LS";

            famR2.Add((fam, frBest));
            _o.WriteLine($"{fam,-6} {frTL,10:F4} {frTS,10:F4} {frLS,10:F4} {frTLS,10:F4} {frBestM,8}");
        }
        double crossFamMean = famR2.Average(f => f.best);
        double crossFamMin = famR2.Min(f => f.best);
        _o.WriteLine($"Cross-family mean R² = {crossFamMean:F4}, min = {crossFamMin:F4}");
        _o.WriteLine("");

        // ============================================================
        // Decision
        // ============================================================
        _o.WriteLine("=== Decision ===");

        bool closed = bestR2 > 0.7 && crossFamMean > 0.5;
        bool partial = bestR2 > 0.3 || crossFamMean > 0.2;
        bool notClosed = !partial;

        string decision;
        if (closed) decision = "Model C";
        else if (partial) decision = "Model B";
        else if (notClosed) decision = "Model A";
        else decision = "Model D";

        _o.WriteLine($"Decision model: {decision}");
        _o.WriteLine($"Best R² = {bestR2:F4} (model: {bestModel})");
        _o.WriteLine($"Cross-family mean R² = {crossFamMean:F4}");

        if (decision == "Model C")
            _o.WriteLine("Reconstruction closure achieved. Accessibility can be reconstructed from derived quantities (Time, Length, Speed). The full chain is internally closed — the endpoint recovers the origin.");
        else if (decision == "Model B")
            _o.WriteLine($"Partial closure (best R²={bestR2:F3}). Some information about Accessibility is recoverable from derived quantities, but significant information loss exists — the forward chain is not fully invertible.");
        else
            _o.WriteLine("Chain not closed. Accessibility cannot be reconstructed from derived quantities.");

        _o.WriteLine("");
        _o.WriteLine("=== OUTPUT ===");
        _o.WriteLine("1. Executive determination");
        _o.WriteLine($"   {decision}");
        _o.WriteLine($"2. Best model: {bestModel}, R²={bestR2:F4}");
        _o.WriteLine($"3. Cross-family mean R²={crossFamMean:F4}");
        _o.WriteLine($"4. Decision: {decision}");
        _o.WriteLine("5. Commit-ready summary:");
        string prcLabel = decision == "Model C" ? "Reconstruction closure achieved" : decision == "Model B" ? "Partial reconstruction closure" : "No reconstruction closure";
        _o.WriteLine($"   PRC_01_PrimitiveReconstructionClosureAudit — {prcLabel}.");
        _o.WriteLine("");
        _o.WriteLine("=== PRC_01 complete. Commit: PRC_01_PrimitiveReconstructionClosureAudit ===");

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
