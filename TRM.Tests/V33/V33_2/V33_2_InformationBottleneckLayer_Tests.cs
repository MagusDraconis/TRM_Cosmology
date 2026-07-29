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

namespace TRM.Tests.V33_2;

[Trait("Category", "V33_2")]
[Trait("Category", "LongRunning")]
public class V33_2_InformationBottleneckLayer_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_2_InformationBottleneckLayer_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void IBL_01_InformationBottleneckLayerAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== IBL_01: Information Bottleneck Layer Audit ===");
        sb.AppendLine("=== Which transition creates new structure? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var transitionResults = new List<TransitionResult>();

        // ================================================================
        // OSCILLATOR LEVEL METRICS (baseline)
        // ================================================================
        double oscDirPct = 0, oscAmpR = 0, oscCurvR = 0;
        {
            int match = 0, total = 0;
            var pairs = new List<(double gm, double av, double lap)>();
            foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
            {
                const int nG = 15;
                double pMin = 0.5, pMax = 4.5, daG = (aMax - aMin) / (nG - 1), dpG = (pMax - pMin) / (nG - 1);
                var gV1 = new double[nG, nG];
                Parallel.For(0, nG, ai => {
                    double alpha = aMin + daG * ai;
                    var v = new VariantSpec($"{fam}_ibl", fam, 1.0, 1.0, alpha, 0.70, 0.70);
                    for (int pi = 0; pi < nG; pi++)
                    { var cci = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, pMin + dpG * pi, v); gV1[ai, pi] = cci.VarI1; }
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
                        total++;
                        if ((gDir == +1 && trend > 0) || (gDir == -1 && trend < 0)) match++;
                        pairs.Add((gMag, gV1[ai, pi], lap));
                    }
            }
            oscDirPct = total > 0 ? 100.0 * match / total : 0;
            double[] gmA = pairs.Select(p => Math.Log10(Math.Max(1e-15, p.gm))).ToArray();
            double[] avA = pairs.Select(p => Math.Log10(Math.Max(1e-15, Math.Abs(p.av)))).ToArray();
            double[] laA = pairs.Select(p => Math.Log10(Math.Max(1e-15, Math.Abs(p.lap)))).ToArray();
            oscAmpR = PearsonCorr(gmA, avA);
            oscCurvR = PearsonCorr(gmA, laA);
        }

        // ================================================================
        // TRANSITION 1: Oscillator → Parametric
        // ================================================================
        {
            double pDirPct = 0, pAmpR = 0, pCurvR = 0;
            int match = 0, total = 0;
            var pairs = new List<(double gm, double av, double lap)>();
            foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
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
                        total++;
                        if ((gDir == +1 && gS[bi, gi] > 0) || (gDir == -1 && gS[bi, gi] < 0)) match++;
                        pairs.Add((gMag, gM[bi, gi], lap));
                    }
            }
            pDirPct = total > 0 ? 100.0 * match / total : 0;
            double[] gmA = pairs.Select(p => Math.Log10(Math.Max(1e-15, p.gm))).ToArray();
            double[] avA = pairs.Select(p => Math.Log10(Math.Max(1e-15, p.av))).ToArray();
            double[] laA = pairs.Select(p => Math.Log10(Math.Max(1e-15, Math.Abs(p.lap)))).ToArray();
            pAmpR = PearsonCorr(gmA, avA);
            pCurvR = PearsonCorr(gmA, laA);

            double dDir = Math.Abs(pDirPct - oscDirPct);
            double dAmp = Math.Abs(pAmpR - oscAmpR);
            double dCurv = Math.Abs(pCurvR - oscCurvR);
            double totalLoss = dDir / 100.0 + dAmp + dCurv;

            string note = "α-projection: (α,p)VarI1 → (β,γ)|m|. |m| is slope of V1 vs VT across α";
            transitionResults.Add(new TransitionResult("Oscillator→Parametric", note,
                oscDirPct, pDirPct, dDir, oscAmpR, pAmpR, dAmp, oscCurvR, pCurvR, dCurv, totalLoss));
        }

        // ================================================================
        // TRANSITION 2: Parametric → Boundary (PAIRED: same cells, before/after boundary)
        // ================================================================
        {
            foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
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

                // Identify boundary cells (sign-change neighbors)
                var bdrySet = new HashSet<(int, int)>();
                for (int bi = 1; bi < nG - 1; bi++)
                    for (int gi = 1; gi < nG - 1; gi++)
                        if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                            gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                            bdrySet.Add((bi, gi));

                // PAIRED: all cells vs boundary-only cells
                var allCells = new List<(double gm, double av, double lap)>();
                var bdryCells = new List<(double gm, double av, double lap)>();
                int allMatch = 0, allTot = 0, bdryMatch = 0, bdryTot = 0;

                for (int bi = 1; bi < nG - 1; bi++)
                    for (int gi = 1; gi < nG - 1; gi++)
                    {
                        double dB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                        double dG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                        double gMag = Math.Sqrt(dB * dB + dG * dG);
                        int gDir = (dB + dG) > 1e-15 ? +1 : (dB + dG) < -1e-15 ? -1 : 0;
                        double lap = (gM[bi + 1, gi] + gM[bi - 1, gi] + gM[bi, gi + 1] + gM[bi, gi - 1] - 4 * gM[bi, gi]) / (db * db);
                        var tup = (gMag, gM[bi, gi], lap);
                        allCells.Add(tup); allTot++;
                        if ((gDir == +1 && gS[bi, gi] > 0) || (gDir == -1 && gS[bi, gi] < 0)) allMatch++;
                        if (bdrySet.Contains((bi, gi)))
                        {
                            bdryCells.Add(tup); bdryTot++;
                            if ((gDir == +1 && gS[bi, gi] > 0) || (gDir == -1 && gS[bi, gi] < 0)) bdryMatch++;
                        }
                    }

                if (allTot < 20 || bdryTot < 20) continue;

                double allDir = 100.0 * allMatch / allTot;
                double bdryDir = 100.0 * bdryMatch / bdryTot;
                double[] allGm = allCells.Select(c => Math.Log10(Math.Max(1e-15, c.gm))).ToArray();
                double[] allAv = allCells.Select(c => Math.Log10(Math.Max(1e-15, c.av))).ToArray();
                double[] allLa = allCells.Select(c => Math.Log10(Math.Max(1e-15, Math.Abs(c.lap)))).ToArray();
                double[] bGm = bdryCells.Select(c => Math.Log10(Math.Max(1e-15, c.gm))).ToArray();
                double[] bAv = bdryCells.Select(c => Math.Log10(Math.Max(1e-15, c.av))).ToArray();
                double[] bLa = bdryCells.Select(c => Math.Log10(Math.Max(1e-15, Math.Abs(c.lap)))).ToArray();

                double allAmp = PearsonCorr(allGm, allAv), bdryAmp = PearsonCorr(bGm, bAv);
                double allCurv = PearsonCorr(allGm, allLa), bdryCurv = PearsonCorr(bGm, bLa);
                double dDir = Math.Abs(bdryDir - allDir);
                double dAmp = Math.Abs(bdryAmp - allAmp);
                double dCurv = Math.Abs(bdryCurv - allCurv);
                double retention = (double)bdryTot / allTot;
                double totalLoss = dDir / 100.0 + dAmp + dCurv;

                transitionResults.Add(new TransitionResult($"Parametric→Boundary {arch}", $"Boundary extraction: {bdryTot}/{allTot} cells ({retention*100:F0}%)",
                    allDir, bdryDir, dDir, allAmp, bdryAmp, dAmp, allCurv, bdryCurv, dCurv, totalLoss));
            }
        }

        // ================================================================
        // TRANSITION 3: Boundary → Graph (PAIRED: same boundary nodes, local→neighbor metrics)
        // ================================================================
        {
            foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
            {
                const int nGrid = 13;
                var adj = Build3DGraphLocal(nGrid, fam, distances, sortedD, xiBase, k0Base, nA, aMin, daD, out int N, out int E);
                if (N < 50) continue;
                var deg = adj.Select(a => (double)a.Count).ToArray();

                // Boundary level: local degree metrics
                // Graph level: neighbor-gradient degree metrics
                var pairs = new List<(double bGrad, double gGrad, double bDeg, double gDeg)>();
                var rng = new Random(42);
                int nSamp = Math.Min(120, N);
                foreach (int i in Enumerable.Range(0, N).OrderBy(_ => rng.Next()).Take(nSamp))
                {
                    if (adj[i].Count < 2) continue;
                    double d = deg[i];
                    // Boundary metric: local degree (same as graph metric for this node)
                    double bGrad = d;
                    // Graph metric: neighbor-gradient
                    double ng = adj[i].Average(n => deg[n]);
                    double gGrad = Math.Abs(d - ng) / Math.Max(1e-15, d);
                    pairs.Add((bGrad, gGrad, d, d)); // same degree value
                }

                if (pairs.Count < 20) continue;

                // How does gradient→degree correlation differ between local and neighbor views?
                double[] bG = pairs.Select(p => Math.Log10(Math.Max(1, p.bGrad))).ToArray();
                double[] gG = pairs.Select(p => Math.Log10(Math.Max(1, p.gGrad))).ToArray();
                double[] degA = pairs.Select(p => Math.Log10(Math.Max(1, p.bDeg))).ToArray();
                double bAmpR = PearsonCorr(bG, degA); // local: degree vs degree = 1.0
                double gAmpR = PearsonCorr(gG, degA); // neighbor-gradient vs degree

                double dAmp = Math.Abs(gAmpR - bAmpR);

                string note = $"Local deg→deg r={bAmpR:F3} vs neighbor∇deg→deg r={gAmpR:F3}";
                transitionResults.Add(new TransitionResult($"Boundary→Graph {arch}", note,
                    0, 0, 0, bAmpR, gAmpR, dAmp, 0, 0, 0, dAmp));
            }
        }

        // ================================================================
        // TRANSITION 4: Graph → Galactic (cross-domain comparison)
        // ================================================================
        {
            // Graph aggregate metrics
            var graphAmp = transitionResults.Where(t => t.Name.Contains("Boundary→Graph")).ToList();
            double gAmpAvg = graphAmp.Count > 0 ? graphAmp.Average(t => t.AfterAmp) : 0;

            // Galactic metrics from SPARC
            var repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            var massFile = Path.Combine(repoRoot, "TRM.Core", "Data", "MassModels_Lelli2016c.mrt");
            var gd = new Dictionary<string, List<(double r, double vobs, double vb)>>();
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
                if (!gd.ContainsKey(id)) gd[id] = new List<(double, double, double)>();
                gd[id].Add((r, vobs, vb));
            }

            var galPairs = new List<(double gs, double ar)>();
            foreach (var (id, pts) in gd)
            {
                var s = pts.OrderBy(p => p.r).ToList();
                if (s.Count < 10) continue;
                var dD = new List<double>();
                for (int i = 1; i < s.Count - 1; i++)
                { double dr = s[i + 1].r - s[i - 1].r; if (dr < 1e-6) continue; dD.Add((s[i + 1].vb * s[i + 1].vb - s[i - 1].vb * s[i - 1].vb) / dr); }
                if (dD.Count < 5) continue;
                double gS = dD.Average(d => Math.Abs(d));
                double vM = s.Average(p => p.vobs), bM = s.Average(p => p.vb);
                double aR = vM > 0 ? Math.Abs(vM - bM) / vM : 0;
                galPairs.Add((gS, aR));
            }

            double[] gsA = galPairs.Select(p => Math.Log10(Math.Max(1, p.gs))).ToArray();
            double[] arA = galPairs.Select(p => p.ar).ToArray();
            double galAmpR = PearsonCorr(gsA, arA);

            double dAmp = Math.Abs(galAmpR - gAmpAvg);
            string note = $"Graph amp r≈{gAmpAvg:F3} → SPARC amp r={galAmpR:F3}";
            transitionResults.Add(new TransitionResult("Graph→Galactic", note,
                0, 0, 0, gAmpAvg, galAmpR, dAmp, 0, 0, 0, dAmp));
        }

        if (transitionResults.Count < 3) { sb.AppendLine("Insufficient transitions."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // ================================================================
        // TRANSITION LOSS TABLE
        // ================================================================
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Transition Loss Table ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Transition",-28} {"Before%",8} {"After%",8} {"ΔDir%",8} {"BefAmp",8} {"AftAmp",8} {"ΔAmp",8} {"ΔCurv",8} {"TotalΔ",8}");
        sb.AppendLine(new string('-', 100));

        foreach (var t in transitionResults.OrderByDescending(t => t.TotalLoss))
        {
            string bd = t.BeforeDir > 0 ? $"{t.BeforeDir:F1}%" : "---";
            string ad = t.AfterDir > 0 ? $"{t.AfterDir:F1}%" : "---";
            string dd = t.DeltaDir > 0 ? $"{t.DeltaDir:F1}" : "---";
            string ba = t.BeforeAmp != 0 ? $"{t.BeforeAmp:F4}" : "---";
            string aa = t.AfterAmp != 0 ? $"{t.AfterAmp:F4}" : "---";
            string da = t.DeltaAmp > 0 ? $"{t.DeltaAmp:F4}" : "---";
            string dc = t.DeltaCurv > 0 ? $"{t.DeltaCurv:F4}" : "---";
            sb.AppendLine($"{t.Name,-28} {bd,8} {ad,8} {dd,8} {ba,8} {aa,8} {da,8} {dc,8} {t.TotalLoss,8:F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // BOTTLENECK RANKING
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Bottleneck Ranking ===");
        sb.AppendLine("");

        // Aggregate by transition type
        var aggregated = transitionResults
            .GroupBy(t => t.Name.Contains("Oscillator") ? "Osc→Param" :
                          t.Name.Contains("Boundary") ? "Param→Bound" :
                          t.Name.Contains("Graph") && t.Name.Contains("Boundary") ? "Bound→Graph" : "Graph→Gal")
            .Select(g => new { Name = g.Key, AvgLoss = g.Average(t => t.TotalLoss), Note = g.First().Note })
            .OrderByDescending(g => g.AvgLoss)
            .ToList();

        for (int i = 0; i < aggregated.Count; i++)
        {
            var a = aggregated[i];
            sb.AppendLine($"  #{i + 1}: {a.Name}  (Δ = {a.AvgLoss:F4})");
            sb.AppendLine($"      {a.Note}");
        }
        sb.AppendLine("");

        // ================================================================
        // EMERGENCE ANALYSIS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Emergence Analysis ===");
        sb.AppendLine("");

        // Find transitions where metrics IMPROVE (not just degrade)
        var improving = transitionResults.Where(t => t.AfterAmp > t.BeforeAmp + 0.02 && t.BeforeAmp != 0).ToList();
        var degrading = transitionResults.Where(t => t.BeforeAmp - t.AfterAmp > 0.05 && t.BeforeAmp != 0).ToList();

        if (improving.Count > 0)
        {
            sb.AppendLine("  Transitions where gradient law STRENGTHENS:");
            foreach (var t in improving)
                sb.AppendLine($"    {t.Name}: amp r {t.BeforeAmp:F4} → {t.AfterAmp:F4} (+{t.AfterAmp - t.BeforeAmp:F4})");
            sb.AppendLine("");
        }

        if (degrading.Count > 0)
        {
            sb.AppendLine("  Transitions where gradient law WEAKENS:");
            foreach (var t in degrading)
                sb.AppendLine($"    {t.Name}: amp r {t.BeforeAmp:F4} → {t.AfterAmp:F4} (-{t.BeforeAmp - t.AfterAmp:F4})");
            sb.AppendLine("");
        }

        var biggestLoss = aggregated.First();
        bool bottleneckFound = biggestLoss.AvgLoss > 0.15;

        if (bottleneckFound)
            sb.AppendLine($"  PRIMARY BOTTLENECK: {biggestLoss.Name} (Δ={biggestLoss.AvgLoss:F4})");
        else
            sb.AppendLine("  No single dominant bottleneck — loss is distributed.");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool critA = bottleneckFound;
        bool critB = transitionResults.Count >= 4;
        bool critC = improving.Count > 0 || degrading.Count > 0;
        bool critD = aggregated.Count >= 3;

        int critMet = (critA ? 1 : 0) + (critB ? 1 : 0) + (critC ? 1 : 0) + (critD ? 1 : 0);

        string verdict = critMet >= 4 ? "SUPPORTED: a specific transition creates higher-level structure."
            : critMet >= 2 ? "CONDITIONAL: distributed loss."
            : "FALSIFIED: chain remains continuous.";

        sb.AppendLine($"VERDICT: {verdict}  ({critMet}/4 criteria)");
        sb.AppendLine("");
        sb.AppendLine($"  A. Dominant bottleneck found:           {(critA ? "YES" : "NO")} ({biggestLoss.Name}, Δ={biggestLoss.AvgLoss:F4})");
        sb.AppendLine($"  B. ≥4 transitions analyzed:             {(critB ? "YES" : "NO")} ({transitionResults.Count})");
        sb.AppendLine($"  C. Structure change detected:           {(critC ? "YES" : "NO")} (+{improving.Count}/-{degrading.Count})");
        sb.AppendLine($"  D. ≥3 transition types:                 {(critD ? "YES" : "NO")} ({aggregated.Count})");
        sb.AppendLine("");
        sb.AppendLine("Bottleneck Result:");
        foreach (var a in aggregated)
            sb.AppendLine($"  {a.Name}: Δ = {a.AvgLoss:F4}");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== IBL_01 complete. Commit: IBL_01_InformationBottleneckLayerAudit ===");

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

    private record TransitionResult(string Name, string Note,
        double BeforeDir, double AfterDir, double DeltaDir,
        double BeforeAmp, double AfterAmp, double DeltaAmp,
        double BeforeCurv, double AfterCurv, double DeltaCurv,
        double TotalLoss);
}
