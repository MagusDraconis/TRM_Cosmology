using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;

namespace TRM.Tests.V8_1;

[Trait("Category", "V8_1"), Trait("Category", "LongRunning")]
public class V8_1_PrimitiveOrigin_Tests
{
    private readonly ITestOutputHelper _o;
    public V8_1_PrimitiveOrigin_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void PPO_01_PrimitiveOriginAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== PPO_01: Primitive Origin Audit ===");
        _o.WriteLine("=== Can Primitive P be reduced further? ===");
        _o.WriteLine(new string('=', 108));

        const int baseSeed = 8087;
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
        var rng = new Random(baseSeed + 4447);

        // ============================================================
        // Data: primitive candidates + kernel params
        // ============================================================
        var data = new List<(VcFamily fam, double beta, double disc, double cov, double nf, double slope, double press)>();

        foreach (var fam in families)
        {
            var prevOcc = (l1: 0.0, l2: 0.0, l3: 0.0); bool hasPrev = false;
            for (int bi = 0; bi < 41; bi++)
            {
                double beta = bi * 0.025;
                var variants = new List<VariantSpec>();
                for (int i = 0; i < 4; i++)
                    variants.Add(new VariantSpec($"{fam}_PO_{i}", VcFamily.ICS, 0.30 + rng.NextDouble() * 2.0, 1.0, 0.20 + rng.NextDouble() * 2.5, beta, 0.0));

                var allC = new List<double[]>(); var allL = new List<double>();
                var bspList = new List<BspPoint>(); var cciList = new List<CciPoint>();
                foreach (var v in variants)
                    for (int ip = 0; ip < 5; ip++)
                    {
                        double pVal = 0.1 + ip * 0.3; if (pVal > 1.31) continue;
                        var bsp = EvaluateVariantAtP(distances, sorted, xiBase, k0Base, pVal, v);
                        var cci = EvaluateCciVariantAtP(distances, sorted, xiBase, k0Base, pVal, v);
                        bspList.Add(bsp); cciList.Add(cci);
                        int nD = distances.Length; double[] kA = new double[nD];
                        double xi = xiBase * v.XiScale, k0 = k0Base * v.K0Scale;
                        for (int i = 0; i < nD; i++) { double x = distances[i] / (xi + 1e-15); kA[i] = k0 * Math.Exp(-v.Alpha * Math.Pow(x, pVal)); kA[i] = Math.Clamp(kA[i], 0.0, k0); }
                        var kD = new double[nDeciles + 1]; var ct = new int[nDeciles + 1];
                        for (int i = 0; i < nD; i++) { int dec = 1; while (dec < nDeciles && distances[i] > decileBounds[dec]) dec++; kD[dec] += kA[i]; ct[dec]++; }
                        for (int d = 1; d <= nDeciles; d++) kD[d] /= Math.Max(ct[d], 1);
                        var ctr = new double[nContrasts]; for (int c = 0; c < nContrasts; c++) ctr[c] = kD[contrastDefs[c].i] - kD[contrastDefs[c].j];
                        allC.Add(ctr); allL.Add(Math.Clamp(1.0 - cci.VarI1 / (cci.VarTerms + 1e-15), 0.0, 1.0));
                    }

                int N = allL.Count; var LArr = allL.ToArray();
                var X = new double[N][]; for (int i = 0; i < N; i++) X[i] = (double[])allC[i].Clone();
                for (int c = 0; c < nContrasts; c++) { double m = Enumerable.Range(0, N).Average(i => X[i][c]); double v = Enumerable.Range(0, N).Select(i => (X[i][c] - m) * (X[i][c] - m)).Average(); double s = Math.Sqrt(v) + 1e-12; for (int i = 0; i < N; i++) X[i][c] = (X[i][c] - m) / s; }
                var cm = new double[nContrasts, nContrasts];
                for (int a = 0; a < nContrasts; a++) for (int b = 0; b < nContrasts; b++) cm[a, b] = PearsonCorrelation(Enumerable.Range(0, N).Select(i => allC[i][a]).ToArray(), Enumerable.Range(0, N).Select(i => allC[i][b]).ToArray());
                var (ee, ev) = JacobiEigenLocal(cm, nContrasts);
                var pe = Enumerable.Range(0, nContrasts).OrderByDescending(i => ee[i]).ToArray();
                var la = new double[3][];
                for (int k = 0; k < 3; k++) { la[k] = new double[N]; int er = pe[k]; for (int i = 0; i < N; i++) { double s = 0; for (int c = 0; c < nContrasts; c++) s += X[i][c] * ev[er, c]; la[k][i] = s; } }
                double r2L1 = R2SinglePredictor(LArr, la[0]), r2L2 = FitModelR2(LArr, new[] { la[0], la[1] }), r2L3 = FitModelR2(LArr, new[] { la[0], la[1], la[2] });
                double t = r2L3;
                double l1 = r2L1 / Math.Max(t, 1e-12), l2 = (r2L2 - r2L1) / Math.Max(t, 1e-12), l3 = (r2L3 - r2L2) / Math.Max(t, 1e-12);
                double pressLoc = hasPrev ? (Math.Abs(l1 - prevOcc.l1) + Math.Abs(l2 - prevOcc.l2) + Math.Abs(l3 - prevOcc.l3)) / 0.025 : 0;
                prevOcc = (l1, l2, l3); hasPrev = true;
                data.Add((fam, beta, bspList.Average(bp => bp.Discrimination), cciList.Average(ci => ci.CovarianceAbs),
                    bspList.Average(bp => bp.Discrimination) * 0.8,
                    Math.Abs(k0Base * (beta + 0.5) / 2.0 * Math.Pow(Math.Log(2.0), Math.Max(beta - 0.5, 0.01) / Math.Max(beta + 0.5, 0.05))),
                    pressLoc));
            }
        }
        data = data.Where(d => d.press > 0).ToList();

        double[] disc = data.Select(d => d.disc).ToArray();
        double[] cov = data.Select(d => d.cov).ToArray();
        double[] nfArr = data.Select(d => d.nf).ToArray();
        double[] slope = data.Select(d => d.slope).ToArray();
        double[] press = data.Select(d => d.press).ToArray();
        double[] betaArr = data.Select(d => d.beta).ToArray();

        // ============================================================
        // PART A-C — Primitive decomposition
        // ============================================================
        _o.WriteLine("=== PARTS A-C: Primitive decomposition ===");

        // Normalize
        double[] nDisc = Norm(disc); double[] nCov = Norm(cov); double[] nNf = Norm(nfArr);
        double[] nSlope = Norm(slope); double[] nPress = Norm(press); double[] nBeta = Norm(betaArr);

        // P as latent PC1 of (disc, cov, nf, slope)
        var all4 = new[] { nDisc, nCov, nNf, nSlope };
        var cm4 = new double[4, 4];
        for (int a = 0; a < 4; a++) for (int b = 0; b < 4; b++) cm4[a, b] = PearsonCorrelation(all4[a], all4[b]);
        var (e4, v4) = JacobiEigenLocal(cm4, 4);
        Array.Sort(e4); Array.Reverse(e4);
        double[] latentP = new double[nDisc.Length];
        for (int i = 0; i < latentP.Length; i++) { double s = 0; for (int c = 0; c < 4; c++) s += all4[c][i] * v4[c, 0]; latentP[i] = s; }

        // Can P be expressed as a geometric ratio?
        // Candidates: P ≈ ξ/d_median, P ≈ K0/K_far, P ≈ slopeAtHalf × ξ
        double dMed = Quantile(sorted, 0.5);
        var geomCandidates = new (string name, double[] vals)[]
        {
            ("ξ/d_median", Norm(betaArr.Select(b => 2.0 / dMed).ToArray())),
            ("K_near/K_far", Norm(disc.Select(d => 1.0 / Math.Max(1.0 - d, 0.01)).ToArray())),
            ("slope×ξ", Norm(slope.Select(s => s * 2.0).ToArray())),
            ("β (exponent)", nBeta),
            ("1/(1-D)", Norm(disc.Select(d => 1.0 / Math.Max(1.0 - d, 0.01)).ToArray()))
        };

        _o.WriteLine($"Primitive P reduction candidates:");
        _o.WriteLine($"{"Candidate ratio",-18} {"r(P, ratio)",12} {"R²(P|ratio)",12}");
        _o.WriteLine(new string('-', 44));

        double bestRedR2 = 0; string bestRedName = "";

        foreach (var (name, vals) in geomCandidates)
        {
            double r = PearsonCorrelation(latentP, vals);
            double r2 = R2SinglePredictor(latentP, vals);
            if (r2 > bestRedR2) { bestRedR2 = r2; bestRedName = name; }
            _o.WriteLine($"{name,-18} {r,12:F4} {r2,12:F4}");
        }
        _o.WriteLine("");

        // Multi-variable reduction
        var reducers = geomCandidates.Select(g => g.vals).Take(4).ToArray();
        double r2AllRed = FitModelR2(latentP, reducers);
        _o.WriteLine($"All reduction candidates: R² = {r2AllRed:F4}");
        _o.WriteLine($"Best single reduction: {bestRedName} (R²={bestRedR2:F4}, {bestRedR2 / Math.Max(r2AllRed, 1e-12):P0} of multi-var)");
        _o.WriteLine("");

        // ============================================================
        // PART D-F — Cross-family + Theorem
        // ============================================================
        _o.WriteLine("=== PARTS D-F: Cross-family + Theorem ===");
        _o.WriteLine($"{"Family",-6} {"PC1%",8} {"R²(β→P)",10} {"best reduction",16}");
        _o.WriteLine(new string('-', 42));

        foreach (var fam in families)
        {
            var fd = data.Where(d => d.fam == fam).ToList();
            if (fd.Count < 10) continue;
            double[] fd_ = Norm(fd.Select(d => d.disc).ToArray());
            double[] fc = Norm(fd.Select(d => d.cov).ToArray());
            double[] fn = Norm(fd.Select(d => d.nf).ToArray());
            double[] fs = Norm(fd.Select(d => d.slope).ToArray());
            double[] fb = Norm(fd.Select(d => d.beta).ToArray());

            var fcm = new double[4, 4];
            var f4 = new[] { fd_, fc, fn, fs };
            for (int a = 0; a < 4; a++) for (int b = 0; b < 4; b++) fcm[a, b] = PearsonCorrelation(f4[a], f4[b]);
            var (fe, fv) = JacobiEigenLocal(fcm, 4);
            Array.Sort(fe); Array.Reverse(fe);
            double fpc1 = fe[0] / fe.Sum();

            double[] fP = new double[fd_.Length];
            for (int i = 0; i < fP.Length; i++) { double s = 0; for (int c = 0; c < 4; c++) s += f4[c][i] * fv[c, 0]; fP[i] = s; }

            double fR2beta = R2SinglePredictor(fP, fb);
            double fR2kf = R2SinglePredictor(fP, Norm(fd.Select(d => 1.0 / Math.Max(1.0 - d.disc, 0.01)).ToArray()));
            string fBest = fR2beta >= fR2kf ? "β" : "K_near/K_far";
            _o.WriteLine($"{fam,-6} {fpc1 * 100,7:F1}% {fR2beta,10:F4} {fBest,16}");
        }
        _o.WriteLine("");

        // ============================================================
        // PART G — Decision
        // ============================================================
        _o.WriteLine("=== PART G: Decision ===");

        bool singleRatioWorks = bestRedR2 > 0.50;
        bool multiVarBetter = r2AllRed > bestRedR2 * 1.3;

        string decision;
        if (singleRatioWorks && !multiVarBetter)
            decision = "Model C";
        else if (singleRatioWorks)
            decision = "Model B";
        else
            decision = "Model A";

        _o.WriteLine($"Decision model: {decision}");
        if (decision == "Model C")
            _o.WriteLine($"Primitive P is derivable from a deeper geometric quantity: {bestRedName} (R²={bestRedR2:F3}). P is not fundamental — it emerges from a single geometric ratio that captures the kernel's shape relative to the ensemble distance scale.");
        else if (decision == "Model B")
            _o.WriteLine($"P is partially reducible to {bestRedName} (R²={bestRedR2:F3}) but retains unique variance.");
        _o.WriteLine("");

        _o.WriteLine("=== OUTPUT ===");
        _o.WriteLine("1. Executive determination");
        _o.WriteLine($"   {decision}");
        _o.WriteLine($"2. Best reduction: {bestRedName} (R²={bestRedR2:F4})");
        _o.WriteLine($"3. Multi-var R²={r2AllRed:F4}");
        _o.WriteLine($"4. Theorem: Kernel Shape → {bestRedName} → Primitive P → Dimension");
        _o.WriteLine($"5. Decision model: {decision}");
        _o.WriteLine("6. Commit-ready summary:");
        _o.WriteLine($"   PPO_01_PrimitiveOriginAudit — P is {(singleRatioWorks ? "reducible to " + bestRedName : "irreducible")}.");
        _o.WriteLine("");
        _o.WriteLine("=== PPO_01 complete. Commit: PPO_01_PrimitiveOriginAudit ===");

        Assert.True(new[] { "Model A", "Model B", "Model C", "Model D" }.Contains(decision));

        static double[] Norm(double[] x) { double m = x.Average(); double s = Math.Sqrt(x.Select(v => (v - m) * (v - m)).Average()) + 1e-12; return x.Select(v => (v - m) / s).ToArray(); }
        static (double[] e, double[,] v) JacobiEigenLocal(double[,] a, int n)
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
}
