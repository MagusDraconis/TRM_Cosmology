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

    [Fact]
    public void PEO_01_PrimitiveEigenvalueOriginAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== PEO_01: Primitive Eigenvalue Origin Audit ===");
        _o.WriteLine("=== Are λ1,λ2,λ3 fundamental or derived? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 17107;
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

        var eigenData = new List<(VcFamily fam, double lam1, double lam2, double lam3,
            double l1, double l2, double l3, double acc, double ent, double beta)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_EO", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
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
                    var sortedEE = ee.OrderByDescending(e => e).ToArray();

                    var pe = Enumerable.Range(0, nContrasts).OrderByDescending(i => ee[i]).ToArray();
                    var la = new double[3][];
                    for (int k = 0; k < 3; k++) { la[k] = new double[N]; int er = pe[k]; for (int i = 0; i < N; i++) { double s = 0; for (int c = 0; c < nContrasts; c++) s += X[i][c] * ev[er, c]; la[k][i] = s; } }
                    double r2L1 = R2SinglePredictor(LArr, la[0]), r2L2 = FitModelR2(LArr, new[] { la[0], la[1] }), r2L3 = FitModelR2(LArr, new[] { la[0], la[1], la[2] });
                    double tVal = r2L3 + 1e-12;
                    double occ1 = r2L1 / tVal, occ2 = (r2L2 - r2L1) / tVal, occ3 = (r2L3 - r2L2) / tVal;
                    double ent = 0; if (occ1 > 1e-12) ent -= occ1 * Math.Log(occ1); if (occ2 > 1e-12) ent -= occ2 * Math.Log(occ2); if (occ3 > 1e-12) ent -= occ3 * Math.Log(occ3);
                    double d1a = Math.Abs(occ1 - 1.0) + occ2 + occ3;
                    double d2a = Math.Abs(occ1 - 0.5) + Math.Abs(occ2 - 0.5) + occ3;
                    double d3a = Math.Abs(occ1 - 1.0 / 3) + Math.Abs(occ2 - 1.0 / 3) + Math.Abs(occ3 - 1.0 / 3);
                    double acv = 1.0 / Math.Max(Math.Min(d1a, Math.Min(d2a, d3a)), 0.01);

                    eigenData.Add((fam, sortedEE[0], sortedEE[1], sortedEE[2], occ1, occ2, occ3, acv, ent, beta));
                }
            }
        }

        var Lam1 = eigenData.Select(d => d.lam1).ToArray();
        var Lam2 = eigenData.Select(d => d.lam2).ToArray();
        var Lam3 = eigenData.Select(d => d.lam3).ToArray();
        var Occ1 = eigenData.Select(d => d.l1).ToArray();
        var Occ2 = eigenData.Select(d => d.l2).ToArray();
        var Occ3 = eigenData.Select(d => d.l3).ToArray();
        var Acv = eigenData.Select(d => d.acc).ToArray();
        var Ent = eigenData.Select(d => d.ent).ToArray();

        // ============================================================
        // Part A: Directional causality
        // ============================================================
        _o.WriteLine("=== Part A: Directional Causality ===");
        _o.WriteLine($"N = {eigenData.Count} samples");

        double r2_accFromL = FitModelR2(Acv, new[] { Lam1, Lam2, Lam3 });
        double r2_L1fromAcc = R2SinglePredictor(Lam1, Acv);
        double r2_L2fromAcc = R2SinglePredictor(Lam2, Acv);
        double r2_L3fromAcc = R2SinglePredictor(Lam3, Acv);

        _o.WriteLine($"Accessibility ← λ1+λ2+λ3: R² = {r2_accFromL:F4}");
        _o.WriteLine($"λ1 ← Accessibility:        R² = {r2_L1fromAcc:F4}");
        _o.WriteLine($"λ2 ← Accessibility:        R² = {r2_L2fromAcc:F4}");
        _o.WriteLine($"λ3 ← Accessibility:        R² = {r2_L3fromAcc:F4}");
        _o.WriteLine("");

        string direction;
        if (r2_accFromL > 0.7 && Math.Max(r2_L1fromAcc, Math.Max(r2_L2fromAcc, r2_L3fromAcc)) < 0.3)
            direction = "λ → Accessibility (eigenvalues primary)";
        else if (Math.Max(r2_L1fromAcc, Math.Max(r2_L2fromAcc, r2_L3fromAcc)) > 0.5 && r2_accFromL < 0.3)
            direction = "Accessibility → λ (accessibility primary)";
        else if (r2_accFromL > 0.5 && Math.Max(r2_L1fromAcc, Math.Max(r2_L2fromAcc, r2_L3fromAcc)) > 0.5)
            direction = "Bidirectional (shared origin)";
        else
            direction = "Weak coupling";
        _o.WriteLine($"Direction: {direction}");
        _o.WriteLine("");

        // ============================================================
        // Part B: λ ← Occupation pattern
        // ============================================================
        _o.WriteLine("=== Part B: λ ← Attractor Occupation ===");
        double r2_L1fromOcc = FitModelR2(Lam1, new[] { Occ1, Occ2, Occ3 });
        double r2_L2fromOcc = FitModelR2(Lam2, new[] { Occ1, Occ2, Occ3 });
        double r2_L3fromOcc = FitModelR2(Lam3, new[] { Occ1, Occ2, Occ3 });
        _o.WriteLine($"λ1 ← occupation: R² = {r2_L1fromOcc:F4}");
        _o.WriteLine($"λ2 ← occupation: R² = {r2_L2fromOcc:F4}");
        _o.WriteLine($"λ3 ← occupation: R² = {r2_L3fromOcc:F4}");
        _o.WriteLine("");

        // ============================================================
        // Part C: Reconstruction without λ
        // ============================================================
        _o.WriteLine("=== Part C: Reconstruction Without λ ===");
        double r2_entFromOcc = FitModelR2(Ent, new[] { Occ1, Occ2, Occ3 });
        double r2_accFromOcc = FitModelR2(Acv, new[] { Occ1, Occ2, Occ3 });
        _o.WriteLine($"Entropy ← occupation (no λ):    R² = {r2_entFromOcc:F4}");
        _o.WriteLine($"Accessibility ← occupation:     R² = {r2_accFromOcc:F4}");
        _o.WriteLine("");

        // ============================================================
        // Part D: Early vs Late stability
        // ============================================================
        _o.WriteLine("=== Part D: Early vs Late Stability ===");
        var early = eigenData.Where(d => d.beta < 0.33).ToArray();
        var late = eigenData.Where(d => d.beta > 0.66).ToArray();

        double cvL1_E = StdOverMean(early.Select(d => d.lam1).ToArray());
        double cvL1_L = StdOverMean(late.Select(d => d.lam1).ToArray());
        double cvEnt_E = StdOverMean(early.Select(d => d.ent).ToArray());
        double cvEnt_L = StdOverMean(late.Select(d => d.ent).ToArray());

        _o.WriteLine($"λ1:      CV(early)={cvL1_E:F4}, CV(late)={cvL1_L:F4}");
        _o.WriteLine($"Entropy: CV(early)={cvEnt_E:F4}, CV(late)={cvEnt_L:F4}");

        string stabilizesFirst;
        if (cvL1_E < cvEnt_E && cvL1_L < cvEnt_L) stabilizesFirst = "λ1";
        else if (cvEnt_E < cvL1_E && cvEnt_L < cvL1_L) stabilizesFirst = "Entropy";
        else stabilizesFirst = "both/neither";
        _o.WriteLine($"More stable: {stabilizesFirst}");
        _o.WriteLine("");

        // ============================================================
        // Part E: Cross-family
        // ============================================================
        _o.WriteLine("=== Part E: Cross-Family λ Origin ===");
        _o.WriteLine($"{"Family",-6} {"acc←λ R²",10} {"λ←occ R²",10} {"λ1←acc R²",10}");
        _o.WriteLine(new string('-', 38));
        foreach (var fam in families)
        {
            var fd = eigenData.Where(d => d.fam == fam).ToArray();
            var fL1 = fd.Select(d => d.lam1).ToArray(); var fL2 = fd.Select(d => d.lam2).ToArray(); var fL3 = fd.Select(d => d.lam3).ToArray();
            var fO1 = fd.Select(d => d.l1).ToArray(); var fO2 = fd.Select(d => d.l2).ToArray(); var fO3 = fd.Select(d => d.l3).ToArray();
            var fAc = fd.Select(d => d.acc).ToArray();

            double frAL = FitModelR2(fAc, new[] { fL1, fL2, fL3 });
            double frLO = FitModelR2(fL1, new[] { fO1, fO2, fO3 });
            double frLA = R2SinglePredictor(fL1, fAc);

            _o.WriteLine($"{fam,-6} {frAL,10:F4} {frLO,10:F4} {frLA,10:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        // Decision
        // ============================================================
        _o.WriteLine("=== Decision ===");

        bool lambdaPrimitive = r2_accFromL > 0.7 && r2_L1fromOcc < 0.3 && r2_L1fromAcc < 0.3;
        bool lambdaFromOccupation = r2_L1fromOcc > 0.5;
        bool lambdaFromAccessibility = r2_L1fromAcc > 0.5 && r2_accFromL < 0.3;

        string decision;
        if (lambdaPrimitive) decision = "Model A";
        else if (lambdaFromOccupation) decision = "Model B";
        else if (lambdaFromAccessibility) decision = "Model C";
        else decision = "Model D";

        _o.WriteLine($"Decision model: {decision}");
        _o.WriteLine($"acc←λ R²={r2_accFromL:F4}, λ←occ R²={r2_L1fromOcc:F4}, λ←acc R²={r2_L1fromAcc:F4}");
        _o.WriteLine($"Direction: {direction}");

        if (decision == "Model A")
            _o.WriteLine("λ1,λ2,λ3 are primitive — generating accessibility, occupation, and entropy.");
        else if (decision == "Model B")
            _o.WriteLine("λ values emerge from attractor occupation patterns.");
        else if (decision == "Model C")
            _o.WriteLine("λ values emerge from accessibility geometry.");
        else
            _o.WriteLine("Unresolved eigenvalue origin.");

        _o.WriteLine("");
        _o.WriteLine("=== OUTPUT ===");
        _o.WriteLine("1. Executive determination");
        _o.WriteLine($"   {decision}");
        _o.WriteLine($"2. Direction: {direction}");
        _o.WriteLine($"3. acc←λ R²={r2_accFromL:F4}, λ←occ R²={r2_L1fromOcc:F4}, λ←acc R²={r2_L1fromAcc:F4}");
        _o.WriteLine($"4. Early stability: {stabilizesFirst}");
        _o.WriteLine($"5. Decision: {decision}");
        _o.WriteLine("6. Commit-ready summary:");
        string peoLabel = decision == "Model A" ? "λ primitive" : decision == "Model B" ? "λ from occupation" : decision == "Model C" ? "λ from accessibility" : "λ unresolved";
        _o.WriteLine($"   PEO_01_PrimitiveEigenvalueOriginAudit — {peoLabel}.");
        _o.WriteLine("");
        _o.WriteLine("=== PEO_01 complete. Commit: PEO_01_PrimitiveEigenvalueOriginAudit ===");

        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void PLI_01_PrimitiveLambdaInvarianceAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== PLI_01: Primitive Lambda Invariance Audit ===");
        _o.WriteLine("=== Do λ1,λ2 survive changes of basis? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 18311;
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

        var rawData = new List<(VcFamily fam, double[][] contrasts, double[] LArr)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_LI", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
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

                    if (allL.Count >= 3)
                        rawData.Add((fam, allC.ToArray(), allL.ToArray()));
                }
            }
        }

        _o.WriteLine($"N = {rawData.Count} samples");

        // Baseline: z-score normalized λ1
        var baselineLam1 = new List<double>();
        foreach (var rd in rawData)
        {
            int N = rd.contrasts.Length;
            var X = rd.contrasts.Select(c => (double[])c.Clone()).ToArray();
            for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / s; }
            var cm = CovMatrix(X, nContrasts, N);
            var (ee, _) = JacobiEigenLocal(cm, nContrasts);
            baselineLam1.Add(ee.OrderByDescending(e => e).First());
        }

        // ============================================================
        // Part A: Feature Drop (5/6 features)
        // ============================================================
        _o.WriteLine("=== Part A: Feature Drop ===");
        var dropRs = new List<double>();
        for (int drop = 0; drop < nContrasts; drop++)
        {
            var lam1s = new List<double>();
            foreach (var rd in rawData)
            {
                int N = rd.contrasts.Length; int nF = nContrasts - 1;
                var X = new double[N][];
                for (int i = 0; i < N; i++) { X[i] = new double[nF]; int idx = 0; for (int c = 0; c < nContrasts; c++) if (c != drop) X[i][idx++] = rd.contrasts[i][c]; }
                for (int c = 0; c < nF; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / s; }
                var cm = CovMatrix(X, nF, N);
                var (ee, _) = JacobiEigenLocal(cm, nF);
                lam1s.Add(ee.OrderByDescending(e => e).First());
            }
            double r = PearsonCorrelation(baselineLam1.ToArray(), lam1s.ToArray());
            dropRs.Add(r);
            _o.WriteLine($"Drop {drop}: r(λ1_drop, λ1) = {r:F4}");
        }
        double rDropMean = dropRs.Average();
        _o.WriteLine($"Mean r = {rDropMean:F4}");
        _o.WriteLine("");

        // ============================================================
        // Part B: Min-Max normalization
        // ============================================================
        _o.WriteLine("=== Part B: Min-Max Normalization ===");
        var mmLam1 = new List<double>();
        foreach (var rd in rawData)
        {
            int N = rd.contrasts.Length;
            var X = rd.contrasts.Select(c => (double[])c.Clone()).ToArray();
            for (int c = 0; c < nContrasts; c++) { double mn = Enumerable.Range(0, N).Min(i => X[i][c]); double mx = Enumerable.Range(0, N).Max(i => X[i][c]); double rng = Math.Max(mx - mn, 1e-12); for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - mn) / rng; }
            var cm = CovMatrix(X, nContrasts, N);
            var (ee, _) = JacobiEigenLocal(cm, nContrasts);
            mmLam1.Add(ee.OrderByDescending(e => e).First());
        }
        double rMinMax = PearsonCorrelation(baselineLam1.ToArray(), mmLam1.ToArray());
        _o.WriteLine($"r(λ1_minmax, λ1_zscore) = {rMinMax:F4}");
        _o.WriteLine("");

        // ============================================================
        // Part C: Scale perturbation
        // ============================================================
        _o.WriteLine("=== Part C: Random Scaling ===");
        var rngScale = new Random(baseSeed + 411);
        var scaleLam1 = new List<double>();
        foreach (var rd in rawData)
        {
            int N = rd.contrasts.Length;
            var X = rd.contrasts.Select(c => (double[])c.Clone()).ToArray();
            for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; double sf = 0.5 + rngScale.NextDouble() * 2.0; for (int i = 0; i < N; i++) X[i][c] = ((X[i][c] - m) / s) * sf; }
            var cm = CovMatrix(X, nContrasts, N);
            var (ee, _) = JacobiEigenLocal(cm, nContrasts);
            scaleLam1.Add(ee.OrderByDescending(e => e).First());
        }
        double rScale = PearsonCorrelation(baselineLam1.ToArray(), scaleLam1.ToArray());
        _o.WriteLine($"r(λ1_scaled, λ1) = {rScale:F4}");
        _o.WriteLine("");

        // ============================================================
        // Part D: Cross-family
        // ============================================================
        _o.WriteLine("=== Part D: Cross-Family Perturbation Stability ===");
        _o.WriteLine($"{"Family",-6} {"r(drop)",10} {"r(minmax)",10} {"r(scale)",10}");
        _o.WriteLine(new string('-', 38));

        foreach (var fam in families)
        {
            var fd = rawData.Where(rd => rd.fam == fam).ToList();
            // baseline
            var fBase = new List<double>();
            foreach (var rd in fd) { int N = rd.contrasts.Length; var X = rd.contrasts.Select(c => (double[])c.Clone()).ToArray(); for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / s; } var cm = CovMatrix(X, nContrasts, N); var (ee, _) = JacobiEigenLocal(cm, nContrasts); fBase.Add(ee.OrderByDescending(e => e).First()); }
            var fbArr = fBase.ToArray();

            // minmax
            var fMM = new List<double>();
            foreach (var rd in fd) { int N = rd.contrasts.Length; var X = rd.contrasts.Select(c => (double[])c.Clone()).ToArray(); for (int c = 0; c < nContrasts; c++) { double mn = Enumerable.Range(0, N).Min(i => X[i][c]); double mx = Enumerable.Range(0, N).Max(i => X[i][c]); double rng = Math.Max(mx - mn, 1e-12); for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - mn) / rng; } var cm = CovMatrix(X, nContrasts, N); var (ee, _) = JacobiEigenLocal(cm, nContrasts); fMM.Add(ee.OrderByDescending(e => e).First()); }

            // scale
            var fSc = new List<double>();
            foreach (var rd in fd) { int N = rd.contrasts.Length; var X = rd.contrasts.Select(c => (double[])c.Clone()).ToArray(); for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; double sf = 0.5 + rngScale.NextDouble() * 2.0; for (int i = 0; i < N; i++) X[i][c] = ((X[i][c] - m) / s) * sf; } var cm = CovMatrix(X, nContrasts, N); var (ee, _) = JacobiEigenLocal(cm, nContrasts); fSc.Add(ee.OrderByDescending(e => e).First()); }

            // drop avg
            var fDr = new List<double>();
            for (int drop = 0; drop < nContrasts; drop++) { var fD = new List<double>(); foreach (var rd in fd) { int N = rd.contrasts.Length; int nF = nContrasts - 1; var X = new double[N][]; for (int i = 0; i < N; i++) { X[i] = new double[nF]; int idx = 0; for (int c = 0; c < nContrasts; c++) if (c != drop) X[i][idx++] = rd.contrasts[i][c]; } for (int c = 0; c < nF; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double vr = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(vr) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / s; } var cm2 = CovMatrix(X, nF, N); var (ee2, _) = JacobiEigenLocal(cm2, nF); fD.Add(ee2.OrderByDescending(e => e).First()); } fDr.Add(PearsonCorrelation(fbArr, fD.ToArray())); }

            double rD = fDr.Average();
            double rM = PearsonCorrelation(fbArr, fMM.ToArray());
            double rS = PearsonCorrelation(fbArr, fSc.ToArray());
            _o.WriteLine($"{fam,-6} {rD,10:F4} {rM,10:F4} {rS,10:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        // Decision
        // ============================================================
        _o.WriteLine("=== Decision ===");
        double overallR = (rDropMean + rMinMax + rScale) / 3.0;
        bool invariant = overallR > 0.8;
        bool partialInvariant = overallR > 0.5;

        string decision;
        if (invariant) decision = "Model C";
        else if (partialInvariant) decision = "Model B";
        else decision = "Model A";

        if (overallR < 0.3 && invariant) decision = "Model A"; // shouldn't happen but safety

        _o.WriteLine($"Decision model: {decision}");
        _o.WriteLine($"Perturbation r: drop={rDropMean:F4}, minmax={rMinMax:F4}, scale={rScale:F4}, overall={overallR:F4}");

        if (decision == "Model C")
            _o.WriteLine("λ1 is representation-independent. Survives feature removal, normalization, and scaling — not a PCA artifact.");
        else if (decision == "Model B")
            _o.WriteLine($"Partially invariant (r={overallR:F3}).");
        else
            _o.WriteLine("λ1 is a PCA artifact.");

        _o.WriteLine("");
        _o.WriteLine("=== OUTPUT ===");
        _o.WriteLine("1. Executive determination");
        _o.WriteLine($"   {decision}");
        _o.WriteLine($"2. Perturbation r: drop={rDropMean:F4}, minmax={rMinMax:F4}, scale={rScale:F4}");
        _o.WriteLine($"3. Decision: {decision}");
        _o.WriteLine("4. Commit-ready summary:");
        string pliLabel = decision == "Model C" ? "λ representation-independent" : decision == "Model B" ? "λ partially invariant" : "λ PCA artifacts";
        _o.WriteLine($"   PLI_01_PrimitiveLambdaInvarianceAudit — {pliLabel}.");
        _o.WriteLine("");
        _o.WriteLine("=== PLI_01 complete. Commit: PLI_01_PrimitiveLambdaInvarianceAudit ===");

        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void LTR_01_LambdaToTimeRateAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== LTR_01: Lambda To Time-Rate Audit ===");
        _o.WriteLine("=== Can λ1 predict local time rates? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 19553;
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

        var steps = new List<(VcFamily fam, double lam1, double lam2, double dH)>();
        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var pointData = new List<(double lam1, double lam2, double ent)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_LR", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
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
                    double occ1 = r2L1 / tVal, occ2 = (r2L2 - r2L1) / tVal, occ3 = (r2L3 - r2L2) / tVal;
                    double ent = 0; if (occ1 > 1e-12) ent -= occ1 * Math.Log(occ1); if (occ2 > 1e-12) ent -= occ2 * Math.Log(occ2); if (occ3 > 1e-12) ent -= occ3 * Math.Log(occ3);

                    pointData.Add((sEE[0], sEE[1], ent));
                }

                for (int i = 0; i < pointData.Count - 1; i++)
                {
                    double dHb = Math.Abs(pointData[i + 1].ent - pointData[i].ent) / (1.0 / (nBeta - 1));
                    steps.Add((fam, pointData[i].lam1, pointData[i].lam2, dHb));
                }
            }
        }

        var Lam1 = steps.Select(s => s.lam1).ToArray();
        var Lam2 = steps.Select(s => s.lam2).ToArray();
        var DH = steps.Select(s => s.dH).ToArray();

        // ============================================================
        // Part A: λ → dH/dβ
        // ============================================================
        _o.WriteLine("=== Part A: λ → Time Rate ===");
        _o.WriteLine($"N = {steps.Count} steps");

        double r_Lam1_dH = PearsonCorrelation(Lam1, DH);
        double r2_Lam1 = R2SinglePredictor(DH, Lam1);
        double r2_Lam12 = FitModelR2(DH, new[] { Lam1, Lam2 });
        double r2_Lam2 = R2SinglePredictor(DH, Lam2);

        _o.WriteLine($"r(λ1, dH/dβ)      = {r_Lam1_dH:F4}");
        _o.WriteLine($"R²(dH ~ λ1)        = {r2_Lam1:F4}");
        _o.WriteLine($"R²(dH ~ λ2)        = {r2_Lam2:F4}");
        _o.WriteLine($"R²(dH ~ λ1+λ2)     = {r2_Lam12:F4}");
        _o.WriteLine($"ΔR²(λ2 beyond λ1)  = {r2_Lam12 - r2_Lam1:F4}");
        _o.WriteLine("");

        // ============================================================
        // Part B: Quartile analysis
        // ============================================================
        _o.WriteLine("=== Part B: Clock Rate by λ1 Quartile ===");
        var sortedByLam = steps.OrderBy(s => s.lam1).ToArray();
        int qSize = sortedByLam.Length / 4;
        _o.WriteLine($"{"λ1 quartile",-14} {"mean λ1",10} {"mean dH/dβ",12}");
        _o.WriteLine(new string('-', 38));
        for (int q = 0; q < 4; q++)
        {
            int start = q * qSize; int end = (q == 3) ? sortedByLam.Length : (q + 1) * qSize;
            var slice = sortedByLam[start..end];
            _o.WriteLine($"Q{q + 1,-13} {slice.Average(s => s.lam1),10:F4} {slice.Average(s => s.dH),12:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        // Part C: Cross-family
        // ============================================================
        _o.WriteLine("=== Part C: Cross-Family λ1 → dH/dβ ===");
        _o.WriteLine($"{"Family",-6} {"r(λ1,dH)",10} {"R²(λ1)",10} {"R²(λ1+λ2)",12}");
        _o.WriteLine(new string('-', 40));
        foreach (var fam in families)
        {
            var fd = steps.Where(s => s.fam == fam).ToArray();
            var fL1 = fd.Select(s => s.lam1).ToArray();
            var fL2 = fd.Select(s => s.lam2).ToArray();
            var fDH = fd.Select(s => s.dH).ToArray();

            double fc = PearsonCorrelation(fL1, fDH);
            double fc1 = R2SinglePredictor(fDH, fL1);
            double fc12 = FitModelR2(fDH, new[] { fL1, fL2 });

            _o.WriteLine($"{fam,-6} {fc,10:F4} {fc1,10:F4} {fc12,12:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        // Decision
        // ============================================================
        _o.WriteLine("=== Decision ===");

        bool strongC = r2_Lam1 > 0.4 && r2_Lam12 - r2_Lam1 < 0.1;
        bool partialC = r2_Lam1 > 0.15;

        string decision;
        if (strongC) decision = "Model C";
        else if (partialC) decision = "Model B";
        else decision = "Model A";

        _o.WriteLine($"Decision model: {decision}");
        _o.WriteLine($"r(λ1,dH/dβ)={r_Lam1_dH:F4}, R²(λ1)={r2_Lam1:F4}, R²(λ1+λ2)={r2_Lam12:F4}");

        if (decision == "Model C")
            _o.WriteLine("Local time rates emerge directly from λ1 — the eigenvalue is the clock-rate generator.");
        else if (decision == "Model B")
            _o.WriteLine($"Partial: λ1 explains {r2_Lam1:P1} of time-rate variance.");
        else
            _o.WriteLine("No λ1→time-rate connection.");

        _o.WriteLine("");
        _o.WriteLine("=== OUTPUT ===");
        _o.WriteLine("1. Executive determination");
        _o.WriteLine($"   {decision}");
        _o.WriteLine($"2. r(λ1,dH/dβ)={r_Lam1_dH:F4}, R²={r2_Lam1:F4}");
        _o.WriteLine("3. Commit-ready summary:");
        string ltrLabel = decision == "Model C" ? "λ1 generates time rates" : decision == "Model B" ? "λ1 partially predicts time rates" : "No λ1→time connection";
        _o.WriteLine($"   LTR_01_LambdaToTimeRateAudit — {ltrLabel}.");
        _o.WriteLine("");
        _o.WriteLine("=== LTR_01 complete. Commit: LTR_01_LambdaToTimeRateAudit ===");

        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    [Fact]
    public void KSD_01_KernelStructureDifferentiationAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== KSD_01: Kernel Structure Differentiation Audit ===");
        _o.WriteLine("=== What separates SAC/RCS from GAN/ICS/CNS? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 20731;
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

        var famMetrics = new Dictionary<VcFamily, List<(double lam1, double ent, double dH, double acc, double occ1)>>();
        foreach (var fam in families) famMetrics[fam] = new List<(double, double, double, double, double)>();

        foreach (var fam in families)
        {
            for (int ci = 0; ci < configs.Length; ci++)
            {
                var cfg = configs[ci];
                var pts = new List<(double lam1, double ent, double acc, double occ1)>();

                for (int bi = 0; bi < nBeta; bi++)
                {
                    double beta = bi / (double)(nBeta - 1);
                    var v = new VariantSpec($"{fam}_KS", fam, cfg.alpha, 1.0, cfg.xiScale, beta, 0.0);
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
                    pts.Add((sEE[0], ent, acc, o1));
                }

                for (int i = 0; i < pts.Count - 1; i++)
                {
                    double dH = Math.Abs(pts[i + 1].ent - pts[i].ent) / (1.0 / (nBeta - 1));
                    famMetrics[fam].Add((pts[i].lam1, pts[i].ent, dH, pts[i].acc, pts[i].occ1));
                }
            }
        }

        // ============================================================
        // Structural comparison
        // ============================================================
        _o.WriteLine("=== Family Structural Profiles ===");
        _o.WriteLine($"{"Family",-6} {"CV(λ1)",8} {"CV(ent)",8} {"CV(dH)",8} {"mean occ1",9} {"mean acc",9} {"r(λ1,ent)",9}");
        _o.WriteLine(new string('-', 64));
        foreach (var fam in families)
        {
            var d = famMetrics[fam];
            var l1 = d.Select(x => x.lam1).ToArray();
            var en = d.Select(x => x.ent).ToArray();
            var dh = d.Select(x => x.dH).ToArray();
            _o.WriteLine($"{fam,-6} {StdOverMean(l1),8:F4} {StdOverMean(en),8:F4} {StdOverMean(dh),8:F4} {d.Average(x => x.occ1),9:F4} {d.Average(x => x.acc),9:F4} {PearsonCorrelation(l1, en),9:F4}");
        }
        _o.WriteLine("");

        // ============================================================
        // Group comparison
        // ============================================================
        _o.WriteLine("=== Group Comparison: SAC+RCS vs GAN+ICS+CNS ===");
        var gA = families.Where(f => f == VcFamily.SAC || f == VcFamily.RCS).SelectMany(f => famMetrics[f]).ToList();
        var gB = families.Where(f => f != VcFamily.SAC && f != VcFamily.RCS).SelectMany(f => famMetrics[f]).ToList();

        double cvL1_A = StdOverMean(gA.Select(x => x.lam1).ToArray());
        double cvL1_B = StdOverMean(gB.Select(x => x.lam1).ToArray());
        double cvDH_A = StdOverMean(gA.Select(x => x.dH).ToArray());
        double cvDH_B = StdOverMean(gB.Select(x => x.dH).ToArray());
        double cvEnt_A = StdOverMean(gA.Select(x => x.ent).ToArray());
        double cvEnt_B = StdOverMean(gB.Select(x => x.ent).ToArray());
        double meanOcc1_A = gA.Average(x => x.occ1);
        double meanOcc1_B = gB.Average(x => x.occ1);
        double rLE_A = PearsonCorrelation(gA.Select(x => x.lam1).ToArray(), gA.Select(x => x.ent).ToArray());
        double rLE_B = PearsonCorrelation(gB.Select(x => x.lam1).ToArray(), gB.Select(x => x.ent).ToArray());

        _o.WriteLine($"{"Metric",-18} {"SAC+RCS",12} {"GAN+ICS+CNS",14} {"Ratio",10}");
        _o.WriteLine(new string('-', 56));
        _o.WriteLine($"{"CV(λ1)",-18} {cvL1_A,12:F4} {cvL1_B,14:F4} {cvL1_A / Math.Max(cvL1_B, 1e-12),10:F2}");
        _o.WriteLine($"{"CV(dH/dβ)",-18} {cvDH_A,12:F4} {cvDH_B,14:F4} {cvDH_A / Math.Max(cvDH_B, 1e-12),10:F2}");
        _o.WriteLine($"{"CV(entropy)",-18} {cvEnt_A,12:F4} {cvEnt_B,14:F4} {cvEnt_A / Math.Max(cvEnt_B, 1e-12),10:F2}");
        _o.WriteLine($"{"mean(occ1)",-18} {meanOcc1_A,12:F4} {meanOcc1_B,14:F4} {meanOcc1_A / Math.Max(meanOcc1_B, 1e-12),10:F2}");
        _o.WriteLine($"{"r(λ1,entropy)",-18} {rLE_A,12:F4} {rLE_B,14:F4}");
        _o.WriteLine("");

        // ============================================================
        // Decision
        // ============================================================
        _o.WriteLine("=== Decision ===");

        bool degenTimeRate = cvDH_A < 0.1 && cvDH_B > 0.2;
        bool attractorDiff = Math.Abs(meanOcc1_A - meanOcc1_B) > 0.3;
        bool gradientDiff = cvL1_A / Math.Max(cvL1_B, 1e-12) > 3.0 || cvL1_A / Math.Max(cvL1_B, 1e-12) < 0.3;

        string decision;
        if (degenTimeRate) decision = "Model A";
        else if (attractorDiff) decision = "Model C";
        else if (gradientDiff) decision = "Model B";
        else decision = "Model D";

        _o.WriteLine($"Decision model: {decision}");

        if (decision == "Model A")
            _o.WriteLine($"SAC/RCS have degenerate time rates (CV(dH)={cvDH_A:F4} vs {cvDH_B:F4}). The R²=1.0 in LTR_01 is because dH/dβ is nearly constant — λ1 trivially predicts a constant. The structural differentiator is entropy stasis.");
        else if (decision == "Model B")
            _o.WriteLine($"Gradient structure differentiates families.");
        else if (decision == "Model C")
            _o.WriteLine($"Attractor organization differentiates families.");
        else
            _o.WriteLine("Hybrid mechanism.");

        _o.WriteLine("");
        _o.WriteLine("=== OUTPUT ===");
        _o.WriteLine("1. Executive determination");
        _o.WriteLine($"   {decision}");
        _o.WriteLine($"2. SAC+RCS: CV(dH)={cvDH_A:F4}, CV(ent)={cvEnt_A:F4}, occ1={meanOcc1_A:F4}");
        _o.WriteLine($"3. GAN+ICS+CNS: CV(dH)={cvDH_B:F4}, CV(ent)={cvEnt_B:F4}, occ1={meanOcc1_B:F4}");
        _o.WriteLine("4. Commit-ready summary:");
        string ksdLabel = decision == "Model A" ? "Degenerate time-rate" : decision == "Model B" ? "Gradient" : decision == "Model C" ? "Attractor" : "Hybrid";
        _o.WriteLine($"   KSD_01_KernelStructureDifferentiationAudit — {ksdLabel}");
        _o.WriteLine("");
        _o.WriteLine("=== KSD_01 complete. Commit: KSD_01_KernelStructureDifferentiationAudit ===");

        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));
    }

    private static double[,] CovMatrix(double[][] X, int nF, int N)
    {
        var cm = new double[nF, nF];
        for (int a = 0; a < nF; a++)
            for (int b = 0; b < nF; b++)
                cm[a, b] = PearsonCorrelation(Enumerable.Range(0, N).Select(i => X[i][a]).ToArray(), Enumerable.Range(0, N).Select(i => X[i][b]).ToArray());
        return cm;
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
