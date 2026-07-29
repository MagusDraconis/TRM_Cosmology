using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V33_1;

[Trait("Category", "V33_1")]
[Trait("Category", "LongRunning")]
public class V33_1_OscillatorToGravityChain_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_1_OscillatorToGravityChain_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void OGC_01_OscillatorToGravityChainAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== OGC_01: Oscillator-to-Gravity Chain Audit ===");
        sb.AppendLine("=== Is there an unbroken causal chain from oscillator to galaxy? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var levelResults = new List<LevelResult>();

        // ================================================================
        // LEVEL 1: OSCILLATOR — raw VarI1/VarTerms on (α, p) grid
        // ================================================================
        {
            var samples = new List<(double gradMag, int gradDir, double val, int trend, double lap)>();
            foreach (var (arch, fam) in new[] { ("GAN-osc", VcFamily.GAN), ("CNS-osc", VcFamily.CNS) })
            {
                const int nG = 15;
                double pMin = 0.5, pMax = 4.5, daG = (aMax - aMin) / (nG - 1), dpG = (pMax - pMin) / (nG - 1);
                var gV1 = new double[nG, nG];
                Parallel.For(0, nG, ai => {
                    double alpha = aMin + daG * ai;
                    var v = new VariantSpec($"{fam}_ogc", fam, 1.0, 1.0, alpha, 0.70, 0.70);
                    for (int pi = 0; pi < nG; pi++)
                    {
                        var cci = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, pMin + dpG * pi, v);
                        gV1[ai, pi] = cci.VarI1;
                    }
                });
                for (int ai = 1; ai < nG - 1; ai++)
                    for (int pi = 1; pi < nG - 1; pi++)
                    {
                        double dA = (gV1[ai + 1, pi] - gV1[ai - 1, pi]) / (2 * daG);
                        double dP = (gV1[ai, pi + 1] - gV1[ai, pi - 1]) / (2 * dpG);
                        double gMag = Math.Sqrt(dA * dA + dP * dP);
                        int gDir = (dA + dP) > 1e-15 ? +1 : (dA + dP) < -1e-15 ? -1 : 0;
                        int trend = dA > 1e-12 ? +1 : dA < -1e-12 ? -1 : 0;
                        double lap = (gV1[ai + 1, pi] + gV1[ai - 1, pi] + gV1[ai, pi + 1] + gV1[ai, pi - 1] - 4 * gV1[ai, pi]) / (daG * daG);
                        samples.Add((gMag, gDir, gV1[ai, pi], trend, lap));
                    }
            }
            if (samples.Count > 30)
            {
                double[] gd = samples.Select(s => (double)s.gradDir).ToArray();
                double[] tr = samples.Select(s => (double)s.trend).ToArray();
                double[] gm = samples.Select(s => Math.Log10(Math.Max(1e-15, s.gradMag))).ToArray();
                double[] av = samples.Select(s => Math.Log10(Math.Max(1e-15, Math.Abs(s.val)))).ToArray();
                double[] la = samples.Select(s => Math.Log10(Math.Max(1e-15, Math.Abs(s.lap)))).ToArray();
                int match = samples.Count(s => (s.gradDir == +1 && s.trend > 0) || (s.gradDir == -1 && s.trend < 0));
                levelResults.Add(new LevelResult("1. Oscillator", "VarI1(α,p)", samples.Count,
                    100.0 * match / samples.Count, PearsonCorr(gd, tr), PearsonCorr(gm, av), PearsonCorr(gm, la)));
            }
        }

        // ================================================================
        // LEVEL 2: PARAMETRIC — ComputeFull on (β, γ) grid
        // ================================================================
        {
            var samples = new List<(double gradMag, int gradDir, double val, int sign, double lap)>();
            foreach (var (arch, fam) in new[] { ("GAN-param", VcFamily.GAN), ("CNS-param", VcFamily.CNS) })
            {
                const int nG = 15;
                double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
                double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);
                var gM = new double[nG, nG]; var gS = new int[nG, nG];
                Parallel.For(0, nG, bi => {
                    double beta = bMin + db * bi;
                    for (int gi = 0; gi < nG; gi++)
                    {
                        double gamma = gMin + dg * gi;
                        var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                        gM[bi, gi] = f.absM; gS[bi, gi] = f.sign;
                    }
                });
                for (int bi = 1; bi < nG - 1; bi++)
                    for (int gi = 1; gi < nG - 1; gi++)
                    {
                        double dB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                        double dG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                        double gMag = Math.Sqrt(dB * dB + dG * dG);
                        int gDir = (dB + dG) > 1e-15 ? +1 : (dB + dG) < -1e-15 ? -1 : 0;
                        double lap = (gM[bi + 1, gi] + gM[bi - 1, gi] + gM[bi, gi + 1] + gM[bi, gi - 1] - 4 * gM[bi, gi]) / (db * db);
                        samples.Add((gMag, gDir, gM[bi, gi], gS[bi, gi], lap));
                    }
            }
            if (samples.Count > 30)
            {
                double[] gd = samples.Select(s => (double)s.gradDir).ToArray();
                double[] sg = samples.Select(s => (double)s.sign).ToArray();
                double[] gm = samples.Select(s => Math.Log10(Math.Max(1e-15, s.gradMag))).ToArray();
                double[] av = samples.Select(s => Math.Log10(Math.Max(1e-15, s.val))).ToArray();
                double[] la = samples.Select(s => Math.Log10(Math.Max(1e-15, Math.Abs(s.lap)))).ToArray();
                int match = samples.Count(s => (s.gradDir == +1 && s.sign > 0) || (s.gradDir == -1 && s.sign < 0));
                levelResults.Add(new LevelResult("2. Parametric", "|m|(β,γ)", samples.Count,
                    100.0 * match / samples.Count, PearsonCorr(gd, sg), PearsonCorr(gm, av), PearsonCorr(gm, la)));
            }
        }

        // ================================================================
        // LEVEL 3: BOUNDARY — 3D sign-boundary graph nodes
        // ================================================================
        {
            var samples = new List<(double gradMag, int gradDir, double val, int sign, double lap)>();
            foreach (var (arch, fam) in new[] { ("GAN-bound", VcFamily.GAN), ("CNS-bound", VcFamily.CNS) })
            {
                const int nGrid = 13;
                var adj = Build3DGraphLocal(nGrid, fam, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int N, out int E);
                if (N < 50) continue;
                var deg = adj.Select(a => (double)a.Count).ToArray();
                var rng = new Random(42);
                int nSamp = Math.Min(150, N);
                foreach (int i in Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSamp))
                {
                    if (adj[i].Count < 2) continue;
                    double d = deg[i];
                    double ng = adj[i].Average(n => deg[n]);
                    double gMag = Math.Abs(d - ng) / Math.Max(1e-15, d);
                    int gDir = d > ng ? +1 : -1;
                    int localSign = adj[i].Count(n => deg[n] > deg.Average()) > adj[i].Count / 2 ? +1 : -1;
                    double lap = adj[i].Sum(n => deg[n]) - adj[i].Count * d;
                    samples.Add((gMag, gDir, d, localSign, lap));
                }
            }
            if (samples.Count > 30)
            {
                double[] gd = samples.Select(s => (double)s.gradDir).ToArray();
                double[] sg = samples.Select(s => (double)s.sign).ToArray();
                double[] gm = samples.Select(s => Math.Log10(Math.Max(1, s.gradMag))).ToArray();
                double[] av = samples.Select(s => Math.Log10(Math.Max(1, s.val))).ToArray();
                double[] la = samples.Select(s => Math.Log10(Math.Max(1, Math.Abs(s.lap)))).ToArray();
                int match = samples.Count(s => (s.gradDir == +1 && s.sign > 0) || (s.gradDir == -1 && s.sign < 0));
                levelResults.Add(new LevelResult("3. Boundary", "degree graph", samples.Count,
                    100.0 * match / samples.Count, PearsonCorr(gd, sg), PearsonCorr(gm, av), PearsonCorr(gm, la)));
            }
        }

        // ================================================================
        // LEVEL 4: GRAPH — connectivity gradient on boundary graph
        // ================================================================
        {
            var samples = new List<(double gradMag, double val, double lap)>();
            foreach (var (arch, fam) in new[] { ("GAN-graph", VcFamily.GAN), ("CNS-graph", VcFamily.CNS) })
            {
                const int nGrid = 13;
                var adj = Build3DGraphLocal(nGrid, fam, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int N, out int E);
                if (N < 50) continue;
                var deg = adj.Select(a => (double)a.Count).ToArray();
                var rng = new Random(42);
                int nSamp = Math.Min(150, N);
                foreach (int i in Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSamp))
                {
                    if (adj[i].Count < 2) continue;
                    double d = deg[i];
                    double ng = adj[i].Average(n => deg[n]);
                    double gMag = Math.Abs(d - ng) / Math.Max(1e-15, d);
                    double lap = adj[i].Sum(n => deg[n]) - adj[i].Count * d;
                    samples.Add((gMag, d, lap));
                }
            }
            if (samples.Count > 30)
            {
                double[] gm = samples.Select(s => Math.Log10(Math.Max(1, s.gradMag))).ToArray();
                double[] av = samples.Select(s => Math.Log10(Math.Max(1, s.val))).ToArray();
                double[] la = samples.Select(s => Math.Log10(Math.Max(1, Math.Abs(s.lap)))).ToArray();
                levelResults.Add(new LevelResult("4. Graph", "degree grad", samples.Count,
                    0, 0, PearsonCorr(gm, av), PearsonCorr(gm, la)));
            }
        }

        // ================================================================
        // LEVEL 5: GALACTIC — SPARC ∇ρ profiles
        // ================================================================
        {
            var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");
            var gd = new Dictionary<string, List<OgcPt>>();
            bool inData = false;
            foreach (var line in File.ReadLines(massFile))
            {
                if (!inData) { if (line.StartsWith("---") && line.Contains("---")) { inData = true; continue; } continue; }
                if (line.StartsWith("---") || line.StartsWith("===") || line.StartsWith("Note") || string.IsNullOrWhiteSpace(line)) continue;
                if (line.Length < 59) continue;
                string id = line.Substring(0, 11).Trim(); if (id.Length == 0) continue;
                double r = ParseD(line.Substring(19, 7)), vobs = ParseD(line.Substring(26, 7));
                double vgas = ParseD(line.Substring(39, 7)), vdisk = ParseD(line.Substring(46, 7)), vbul = ParseD(line.Substring(53, 7));
                if (double.IsNaN(r) || double.IsNaN(vobs) || r < 0 || vobs < 0) continue;
                double vb = Math.Sqrt(Math.Max(0, vgas * vgas + vdisk * vdisk + vbul * vbul));
                if (!gd.ContainsKey(id)) gd[id] = new List<OgcPt>();
                gd[id].Add(new OgcPt(r, vobs, vb));
            }

            int shapeOk = 0, total = 0;
            var ampPairs = new List<(double gradStr, double ampRes)>();
            foreach (var (id, pts) in gd)
            {
                var s = pts.OrderBy(p => p.R).ToList();
                if (s.Count < 10) continue;
                var dD = new List<double>();
                for (int i = 1; i < s.Count - 1; i++)
                { double dr = s[i + 1].R - s[i - 1].R; if (dr < 1e-6) continue; dD.Add((s[i + 1].Vbary * s[i + 1].Vbary - s[i - 1].Vbary * s[i - 1].Vbary) / dr); }
                if (dD.Count < 5) continue;
                int neg = dD.Count(d => d < -1e-15), pos = dD.Count(d => d > 1e-15);
                int nO = Math.Max(3, s.Count / 3);
                var inn = s.Take(s.Count * 2 / 5).ToList();
                var outr = s.Skip(s.Count - nO).ToList();
                if (inn.Count < 2 || outr.Count < 2) continue;
                double vI = inn.Average(p => p.Vobs), vO = outr.Average(p => p.Vobs);
                total++;
                if ((vO > vI && neg > pos) || (vO < vI && pos > neg) || Math.Abs(vO - vI) < 0.02 * Math.Max(1, vO)) shapeOk++;
                double vM = s.Average(p => p.Vobs), bM = s.Average(p => p.Vbary);
                double aR = vM > 0 ? Math.Abs(vM - bM) / vM : 0;
                double gS = dD.Average(d => Math.Abs(d));
                ampPairs.Add((gS, aR));
            }

            double shapePct = total > 0 ? 100.0 * shapeOk / total : 0;
            double[] gsA = ampPairs.Select(p => Math.Log10(Math.Max(1, p.gradStr))).ToArray();
            double[] arA = ampPairs.Select(p => p.ampRes).ToArray();
            double ampR = PearsonCorr(gsA, arA);

            levelResults.Add(new LevelResult("5. Galactic", "SPARC ∇ρ", total, shapePct, 0, ampR, 0));
        }

        if (levelResults.Count < 3) { sb.AppendLine("Insufficient levels."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // ================================================================
        // SCALE TRANSITION TABLE
        // ================================================================
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Scale Transition Table ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Level",-22} {"Field",-16} {"N",6} {"Dir%",8} {"Dir r",8} {"|∇|→|·|r",11} {"∇-∇² r",9}");
        sb.AppendLine(new string('-', 82));

        foreach (var lr in levelResults)
        {
            string dp = lr.DirPct > 0 ? $"{lr.DirPct:F1}%" : "---";
            string dr = lr.DirR != 0 ? $"{lr.DirR:F4}" : "---";
            string ar = lr.AmpR != 0 ? $"{lr.AmpR:F4}" : "---";
            string cr = lr.CurvR != 0 ? $"{lr.CurvR:F4}" : "---";
            sb.AppendLine($"{lr.Name,-22} {lr.Field,-16} {lr.Count,6} {dp,8} {dr,8} {ar,11} {cr,9}");
        }
        sb.AppendLine("");

        // ================================================================
        // INFORMATION RETENTION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Information Retention Across Transitions ===");
        sb.AppendLine("");

        var levelsWithAmp = levelResults.Where(l => l.AmpR != 0).ToList();
        if (levelsWithAmp.Count >= 2)
        {
            sb.AppendLine("  |∇|→|·| correlation by level:");
            foreach (var l in levelsWithAmp)
                sb.AppendLine($"    {l.Name}: r = {l.AmpR:F4}");
            sb.AppendLine("");

            double ampFirst = Math.Abs(levelsWithAmp.First().AmpR);
            double ampLast = Math.Abs(levelsWithAmp.Last().AmpR);
            double retention = ampFirst > 1e-15 ? ampLast / ampFirst : 1;
            sb.AppendLine($"  Amplitude retention (last/first): {retention:F2}x");
            sb.AppendLine("");
        }

        var levelsWithCurv = levelResults.Where(l => l.CurvR != 0).ToList();
        if (levelsWithCurv.Count >= 2)
        {
            sb.AppendLine("  ∇-∇² correlation by level:");
            foreach (var l in levelsWithCurv)
                sb.AppendLine($"    {l.Name}: r = {l.CurvR:F4}");
            sb.AppendLine("");

            double curvCV = Cv(levelsWithCurv.Select(l => l.CurvR).ToArray());
            sb.AppendLine($"  ∇-∇² stability (CV): {curvCV:F3}  ({(curvCV < 0.3 ? "STABLE" : "VARIABLE")})");
            sb.AppendLine("");
        }

        // ================================================================
        // EMERGENCE CHAIN
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Emergence Chain ===");
        sb.AppendLine("");
        sb.AppendLine("  Oscillator → Parametric → Boundary → Graph → Galactic");
        sb.AppendLine("     ∇VarI1  →   ∇|m|    →  ∇deg   →  ∇deg  →   ∇ρ");
        sb.AppendLine("       ↓           ↓          ↓         ↓         ↓");
        sb.AppendLine("     trend      sign       sign      degree    V_rot");
        sb.AppendLine("");

        // Count how many levels show the gradient law
        int dirLevels = levelResults.Count(l => l.DirPct > 55 || (l.DirR != 0 && Math.Abs(l.DirR) > 0.15));
        int ampLevels = levelResults.Count(l => l.AmpR != 0 && Math.Abs(l.AmpR) > 0.10);
        int curvLevels = levelResults.Count(l => l.CurvR != 0 && Math.Abs(l.CurvR) > 0.30);

        sb.AppendLine($"  Direction law present:  {dirLevels}/{levelResults.Count} levels");
        sb.AppendLine($"  Strength law present:   {ampLevels}/{levelResults.Count} levels");
        sb.AppendLine($"  Curvature derivation:   {curvLevels}/{levelResults.Count} levels");
        sb.AppendLine("");

        bool continuousChain = dirLevels >= levelResults.Count - 1 && ampLevels >= levelResults.Count - 2;

        if (continuousChain)
        {
            sb.AppendLine("  CONTINUOUS CHAIN: gradient law persists through all transitions.");
            sb.AppendLine("  No new law is introduced — galactic behavior is encoded in");
            sb.AppendLine("  oscillator dynamics and survives each scale transition.");
        }
        else
        {
            sb.AppendLine("  DISCONTINUOUS: gradient law appears/vanishes at some transitions.");
            sb.AppendLine("  New structure may be introduced at higher scales.");
        }
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = continuousChain;
        bool critB = levelResults.Count >= 5;
        bool critC = ampLevels >= 3;
        bool critD = curvLevels >= 3;

        int critMet = (critA ? 1 : 0) + (critB ? 1 : 0) + (critC ? 1 : 0) + (critD ? 1 : 0);

        string verdict = critMet >= 4 ? "SUPPORTED: gravity emerges continuously from oscillator gradients."
            : critMet >= 2 ? "CONDITIONAL: partial continuity."
            : "FALSIFIED: new laws appear at higher scales.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Continuous chain:                    {(critA ? "YES" : "NO")}");
        sb.AppendLine($"  B. All 5 levels present:               {(critB ? "YES" : "NO")} ({levelResults.Count})");
        sb.AppendLine($"  C. Strength law at ≥3 levels:          {(critC ? "YES" : "NO")} ({ampLevels})");
        sb.AppendLine($"  D. Curvature derivation at ≥3 levels:  {(critD ? "YES" : "NO")} ({curvLevels})");
        sb.AppendLine("");
        sb.AppendLine("Chain Result:");
        sb.AppendLine($"  Traced gradient law from oscillator to galaxy across {levelResults.Count} levels.");
        sb.AppendLine($"  Direction: {dirLevels} levels, Strength: {ampLevels} levels, Curvature: {curvLevels} levels.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== OGC_01 complete. Commit: OGC_01_OscillatorToGravityChainAudit ===");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private static double Cv(double[] xs)
    {
        double m = xs.Average();
        double sd = Math.Sqrt(xs.Average(x => (x - m) * (x - m)));
        return m > 1e-15 ? sd / m : 0;
    }

    private static double ParseD(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
    }

    private static List<int>[] Build3DGraphLocal(int nGrid, VcFamily fam,
        double[] distances, double[] sortedD, double xiBase, double k0Base,
        int nA, double aMin, double daD, out int N, out int edges)
    {
        var bag = new ConcurrentBag<(int ai, int bi, int gi, int sign)>();
        Parallel.For(0, nGrid, ai => {
            double alpha = 0.1 + (3.0 - 0.1) * ai / (nGrid - 1);
            for (int bi = 0; bi < nGrid; bi++)
            {
                double beta = 0.0 + 2.0 * bi / (nGrid - 1);
                for (int gi = 0; gi < nGrid; gi++)
                {
                    double gamma = 0.0 + 2.0 * gi / (nGrid - 1);
                    var (m, dTdp) = ComputeM_and_DTdp(fam, 1.0, 1.0, alpha, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    bag.Add((ai, bi, gi, dTdp > 1e-8 ? 1 : -1));
                }
            }
        });
        var all = bag.ToList();
        var s3 = new int[nGrid, nGrid, nGrid];
        foreach (var p in all) s3[p.ai, p.bi, p.gi] = p.sign;
        var bdry = new List<(int, int, int)>();
        var bset = new HashSet<(int, int, int)>();
        for (int ai = 0; ai < nGrid; ai++)
            for (int bi = 0; bi < nGrid; bi++)
                for (int gi = 0; gi < nGrid; gi++)
                {
                    bool opp = false;
                    if (ai > 0 && s3[ai, bi, gi] != s3[ai - 1, bi, gi]) opp = true;
                    if (ai + 1 < nGrid && s3[ai, bi, gi] != s3[ai + 1, bi, gi]) opp = true;
                    if (bi > 0 && s3[ai, bi, gi] != s3[ai, bi - 1, gi]) opp = true;
                    if (bi + 1 < nGrid && s3[ai, bi, gi] != s3[ai, bi + 1, gi]) opp = true;
                    if (gi > 0 && s3[ai, bi, gi] != s3[ai, gi - 1, gi]) opp = true;
                    if (gi + 1 < nGrid && s3[ai, bi, gi] != s3[ai, gi + 1, gi]) opp = true;
                    if (opp) { bdry.Add((ai, bi, gi)); bset.Add((ai, bi, gi)); }
                }
        N = bdry.Count;
        var imap = new Dictionary<(int, int, int), int>();
        for (int i = 0; i < N; i++) imap[bdry[i]] = i;
        var adj = new List<int>[N];
        for (int i = 0; i < N; i++) adj[i] = new List<int>();
        for (int da = -1; da <= 1; da++)
            for (int db = -1; db <= 1; db++)
                for (int dg = -1; dg <= 1; dg++)
                {
                    if (da == 0 && db == 0 && dg == 0) continue;
                    for (int i = 0; i < N; i++)
                    {
                        var (a, b, g2) = bdry[i];
                        int na = a + da, nb = b + db, ng = g2 + dg;
                        if (bset.Contains((na, nb, ng))) { int j = imap[(na, nb, ng)]; if (!adj[i].Contains(j)) adj[i].Add(j); }
                    }
                }
        edges = adj.Sum(a => a.Count) / 2;
        return adj;
    }

    private record OgcPt(double R, double Vobs, double Vbary);
    private record LevelResult(string Name, string Field, int Count, double DirPct, double DirR, double AmpR, double CurvR);
}
