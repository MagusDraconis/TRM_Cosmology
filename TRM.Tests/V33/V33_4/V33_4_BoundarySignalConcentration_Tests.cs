using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V33_4;

[Trait("Category", "V33_4")]
[Trait("Category", "LongRunning")]
public class V33_4_BoundarySignalConcentration_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_4_BoundarySignalConcentration_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void BSC_01_BoundarySignalConcentrationAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BSC_01: Boundary Signal Concentration Audit ===");
        sb.AppendLine("=== What mechanism amplifies gradient at boundaries? ===");
        sb.AppendLine(new string('=', 108));

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var allZoneResults = new List<ZoneResult>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 20;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gM = new double[nG, nG];
            var gS = new int[nG, nG];

            Parallel.For(0, nG, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < nG; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gM[bi, gi] = f.absM; gS[bi, gi] = f.sign;
                }
            });

            // Compute boundary distance for each interior cell
            var bdrySet = new HashSet<(int, int)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                    if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                        gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                        bdrySet.Add((bi, gi));

            // BFS distance from boundary set
            var dist = new int[nG, nG];
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) dist[bi, gi] = int.MaxValue;
            var q = new Queue<(int, int)>();
            foreach (var (bi, gi) in bdrySet) { dist[bi, gi] = 0; q.Enqueue((bi, gi)); }
            while (q.Count > 0)
            {
                var (bi, gi) = q.Dequeue();
                foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) })
                    if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && dist[nb, ng] == int.MaxValue)
                    { dist[nb, ng] = dist[bi, gi] + 1; q.Enqueue((nb, ng)); }
            }

            // Collect cells per zone (interior only for gradient computation)
            var zones = new Dictionary<int, List<ZoneCell>>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    if (dist[bi, gi] == int.MaxValue) continue;
                    int z = Math.Min(3, dist[bi, gi]); // 0=boundary, 1=near-1, 2=near-2, 3=interior

                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradMag = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);
                    int gDir = (dMdB + dMdG) > 1e-15 ? +1 : (dMdB + dMdG) < -1e-15 ? -1 : 0;
                    double lap = (gM[bi + 1, gi] + gM[bi - 1, gi] + gM[bi, gi + 1] + gM[bi, gi - 1] - 4 * gM[bi, gi]) / (db * db);
                    bool dirMatch = (gDir == +1 && gS[bi, gi] > 0) || (gDir == -1 && gS[bi, gi] < 0);

                    // Sign gradient at this cell
                    double sgB = (gS[bi + 1, gi] - gS[bi - 1, gi]) / (2.0 * db);
                    double sgG = (gS[bi, gi + 1] - gS[bi, gi - 1]) / (2.0 * dg);
                    double signGrad = Math.Sqrt(sgB * sgB + sgG * sgG);

                    if (!zones.ContainsKey(z)) zones[z] = new List<ZoneCell>();
                    zones[z].Add(new ZoneCell(gM[bi, gi], gS[bi, gi], gradMag, Math.Abs(lap), dirMatch, signGrad));
                }

            // Zone statistics
            string[] zoneNames = { "Boundary", "Near-1", "Near-2", "Interior" };
            for (int z = 0; z <= 3; z++)
            {
                if (!zones.ContainsKey(z) || zones[z].Count < 10) continue;
                var cells = zones[z];
                double gradMean = cells.Average(c => c.GradMag);
                double curvMean = cells.Average(c => c.CurvMag);
                double cohPct = 100.0 * cells.Count(c => c.DirMatch) / cells.Count;
                double signGradMean = cells.Average(c => c.SignGradMag);

                double[] gm = cells.Select(c => Math.Log10(Math.Max(1e-15, c.GradMag))).ToArray();
                double[] am = cells.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();
                double[] la = cells.Select(c => Math.Log10(Math.Max(1e-15, c.CurvMag))).ToArray();
                double ampR = PearsonCorr(gm, am);
                double curvR = PearsonCorr(gm, la);

                allZoneResults.Add(new ZoneResult($"{arch}", zoneNames[z], z, cells.Count,
                    gradMean, curvMean, cohPct, signGradMean, ampR, curvR));
            }

            // ---- RANDOM SELECTION CONTROL ----
            // Random sample of same size as boundary, from all interior cells
            var allInterior = new List<ZoneCell>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    if (dist[bi, gi] == int.MaxValue) continue;
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradMag = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);
                    int gDir = (dMdB + dMdG) > 1e-15 ? +1 : (dMdB + dMdG) < -1e-15 ? -1 : 0;
                    bool dirMatch = (gDir == +1 && gS[bi, gi] > 0) || (gDir == -1 && gS[bi, gi] < 0);
                    double lap = (gM[bi + 1, gi] + gM[bi - 1, gi] + gM[bi, gi + 1] + gM[bi, gi - 1] - 4 * gM[bi, gi]) / (db * db);
                    allInterior.Add(new ZoneCell(gM[bi, gi], gS[bi, gi], gradMag, Math.Abs(lap), dirMatch, 0));
                }

            int bdryN = zones.ContainsKey(0) ? zones[0].Count : 0;
            var rng = new Random(42);
            var randomSample = allInterior.OrderBy(_ => rng.Next()).Take(bdryN).ToList();

            if (randomSample.Count >= 10)
            {
                double rGradMean = randomSample.Average(c => c.GradMag);
                double rCurvMean = randomSample.Average(c => c.CurvMag);
                double rCohPct = 100.0 * randomSample.Count(c => c.DirMatch) / randomSample.Count;
                double[] rgm = randomSample.Select(c => Math.Log10(Math.Max(1e-15, c.GradMag))).ToArray();
                double[] ram = randomSample.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();
                double[] rla = randomSample.Select(c => Math.Log10(Math.Max(1e-15, c.CurvMag))).ToArray();
                double rAmpR = PearsonCorr(rgm, ram);
                double rCurvR = PearsonCorr(rgm, rla);

                allZoneResults.Add(new ZoneResult($"{arch}", "Random Control", -1, randomSample.Count,
                    rGradMean, rCurvMean, rCohPct, 0, rAmpR, rCurvR));
            }
        }

        if (allZoneResults.Count < 6) { sb.AppendLine("Insufficient data."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // ================================================================
        // ZONE GRADIENT TABLE
        // ================================================================
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Zone Comparison: Boundary → Near → Interior ===");
        sb.AppendLine("");
        sb.AppendLine($"{"Arch",-8} {"Zone",-14} {"N",5} {"GradMean",10} {"CurvMean",10} {"Coh%",7} {"Sign∇",8} {"|∇|→|·|r",10} {"∇-∇²r",9}");
        sb.AppendLine(new string('-', 85));

        foreach (var z in allZoneResults.OrderBy(z => z.Arch).ThenBy(z => z.ZoneIdx))
        {
            string sg = z.SignGradMean > 0 ? $"{z.SignGradMean:F3}" : "---";
            sb.AppendLine($"{z.Arch,-8} {z.Zone,-14} {z.Count,5} {z.GradMean,10:F4} {z.CurvMean,10:F4} {z.CohPct,7:F1}% {sg,8} {z.AmpR,10:F4} {z.CurvR,9:F4}");
        }
        sb.AppendLine("");

        // ================================================================
        // MECHANISM TESTS
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Mechanism Tests ===");
        sb.AppendLine("");

        // Test 1: Sign-transition geometry — does |∇sign| predict amplification?
        var bdryZones = allZoneResults.Where(z => z.ZoneIdx == 0).ToList();
        double sgGradR = 0;
        if (bdryZones.Count >= 2)
        {
            double[] sgVals = bdryZones.Select(z => z.SignGradMean).ToArray();
            double[] gradVals = bdryZones.Select(z => z.GradMean).ToArray();
            sgGradR = PearsonCorr(sgVals, gradVals);
            sb.AppendLine($"  Mechanism 1: Sign-transition geometry");
            sb.AppendLine($"    |∇sign| vs gradient mean: r = {sgGradR:F4}");
            sb.AppendLine($"    → {(Math.Abs(sgGradR) > 0.5 ? "Sign gradient DRIVES amplification" : "Sign gradient weakly/non correlated")}");
            sb.AppendLine("");
        }

        // Test 2: Curvature concentration — does curvature predict gradient-law strength?
        var allZ = allZoneResults.Where(z => z.ZoneIdx >= 0).ToList();
        double[] curvVals = allZ.Select(z => z.CurvMean).ToArray();
        double[] ampRVals = allZ.Select(z => z.AmpR).ToArray();
        double curvAmpR = PearsonCorr(curvVals, ampRVals);
        sb.AppendLine($"  Mechanism 2: Curvature concentration");
        sb.AppendLine($"    Curvature mean vs |∇|→|·| r: r = {curvAmpR:F4}");
        sb.AppendLine($"    → {(Math.Abs(curvAmpR) > 0.5 ? "Curvature DRIVES gradient-law strength" : "Curvature weakly/non correlated")}");
        sb.AppendLine("");

        // Test 3: Information compression — boundary vs random selection
        var bdryVsRandom = allZoneResults.Where(z => z.Zone == "Random Control" || z.ZoneIdx == 0)
            .GroupBy(z => z.Arch).ToList();
        sb.AppendLine($"  Mechanism 3: Information compression (boundary vs random)");
        foreach (var g in bdryVsRandom)
        {
            var b = g.FirstOrDefault(z => z.ZoneIdx == 0);
            var r = g.FirstOrDefault(z => z.Zone == "Random Control");
            if (b == null || r == null) continue;
            sb.AppendLine($"    {g.Key}:");
            sb.AppendLine($"      Boundary: grad={b.GradMean:F4}  ampR={b.AmpR:F4}  coh={b.CohPct:F1}%");
            sb.AppendLine($"      Random:   grad={r.GradMean:F4}  ampR={r.AmpR:F4}  coh={r.CohPct:F1}%");
            double gradRatio = b.GradMean / Math.Max(1e-15, r.GradMean);
            double ampRatio = b.AmpR / Math.Max(1e-15, r.AmpR);
            sb.AppendLine($"      B/R:      grad={gradRatio:F2}x  ampR={ampRatio:F2}x  → {(gradRatio > 1.2 || ampRatio > 1.2 ? "BOUNDARY ≠ RANDOM" : "Boundary ≈ random selection")}");
        }
        sb.AppendLine("");

        // Test 4: Topological selection — does amplification decay with distance?
        sb.AppendLine($"  Mechanism 4: Topological selection (distance decay)");
        foreach (var arch in new[] { "GAN", "CNS" })
        {
            var az = allZoneResults.Where(z => z.Arch == arch && z.ZoneIdx >= 0).OrderBy(z => z.ZoneIdx).ToList();
            if (az.Count < 3) continue;
            double[] dists = az.Select(z => (double)z.ZoneIdx).ToArray();
            double[] grads = az.Select(z => z.GradMean).ToArray();
            double[] cohs = az.Select(z => z.CohPct).ToArray();
            double distGradR = PearsonCorr(dists, grads);
            double distCohR = PearsonCorr(dists, cohs);

            sb.AppendLine($"    {arch}: gradient vs distance r={distGradR:F4}, coherence vs distance r={distCohR:F4}");
            sb.AppendLine($"      → {(Math.Abs(distGradR) > 0.7 ? "STRONG distance decay — local mechanism" : Math.Abs(distGradR) > 0.3 ? "MODERATE decay — mixed local/global" : "WEAK decay — global/topological mechanism")}");
        }
        sb.AppendLine("");

        // ================================================================
        // BOUNDARY CONCENTRATION LAW
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Candidate Boundary Concentration Law ===");
        sb.AppendLine("");

        // Fit gradient vs distance
        var allNonRandom = allZoneResults.Where(z => z.ZoneIdx >= 0).ToList();
        double[] allDist = allNonRandom.Select(z => (double)z.ZoneIdx).ToArray();
        double[] allGrad = allNonRandom.Select(z => Math.Log10(Math.Max(1e-15, z.GradMean))).ToArray();
        var (slope, intercept, r2) = LinearRegression(allDist, allGrad);

        sb.AppendLine($"  log(|∇|m||) vs distance from boundary:");
        sb.AppendLine($"    log(|∇|m||) = {intercept:F3} + {slope:F3} · d    (R²={r2:F4})");
        sb.AppendLine($"    → Gradient {(slope < -0.05 ? "DECAYS" : "CONSTANT")} with distance");
        sb.AppendLine("");

        // ================================================================
        // DECISION
        // ================================================================
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== Decision ===");
        sb.AppendLine("");

        bool mech1 = Math.Abs(sgGradR) > 0.5;
        bool mech2 = Math.Abs(curvAmpR) > 0.5;
        bool mech3 = bdryVsRandom.Any(g => {
            var b = g.FirstOrDefault(z => z.ZoneIdx == 0);
            var r = g.FirstOrDefault(z => z.Zone == "Random Control");
            return b != null && r != null && (b.GradMean / Math.Max(1e-15, r.GradMean) > 1.2 || b.AmpR / Math.Max(1e-15, r.AmpR) > 1.2);
        });
        bool mech4 = Math.Abs(slope) > 0.05;

        int mechCount = (mech1 ? 1 : 0) + (mech2 ? 1 : 0) + (mech3 ? 1 : 0) + (mech4 ? 1 : 0);

        string verdict = mechCount >= 3 ? "SUPPORTED: specific mechanism identified."
            : mechCount >= 2 ? "CONDITIONAL: multiple contributors."
            : "FALSIFIED: amplification remains unexplained.";

        sb.AppendLine($"VERDICT: {verdict}  ({mechCount}/4 mechanisms identified)");
        sb.AppendLine("");
        sb.AppendLine($"  M1. Sign-transition geometry:             {(mech1 ? "✓ DRIVES" : "✗ WEAK")} (r={sgGradR:F4})");
        sb.AppendLine($"  M2. Curvature concentration:              {(mech2 ? "✓ DRIVES" : "✗ WEAK")} (r={curvAmpR:F4})");
        sb.AppendLine($"  M3. Boundary ≠ random selection:          {(mech3 ? "✓ DISTINCT" : "✗ SAME")}");
        sb.AppendLine($"  M4. Distance decay:                        {(mech4 ? "✓ DECAYS" : "✗ CONSTANT")} (slope={slope:F3})");
        sb.AppendLine("");
        sb.AppendLine("Boundary Concentration Result:");
        if (mech4 && mech1)
            sb.AppendLine("  PRIMARY MECHANISM: Sign-transition geometry with local distance decay.");
        else if (mech4)
            sb.AppendLine("  PRIMARY MECHANISM: Local gradient decay from boundary (distance-dependent).");
        else if (mech3)
            sb.AppendLine("  PRIMARY MECHANISM: Topological selection — boundary ≠ random filter.");
        else
            sb.AppendLine("  No dominant mechanism identified — amplification is diffuse.");
        sb.AppendLine("");
        sb.AppendLine(new string('=', 108));
        sb.AppendLine("=== BSC_01 complete. Commit: BSC_01_BoundarySignalConcentrationAudit ===");

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

    private static (double slope, double intercept, double r2) LinearRegression(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return (0, ys.Length > 0 ? ys[0] : 0, 0);
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        double slope = sxx > 1e-15 ? sxy / sxx : 0;
        double intercept = my - slope * mx;
        double r2 = syy > 1e-15 ? (slope * sxy) / syy : 0;
        return (slope, intercept, r2);
    }

    private record ZoneCell(double AbsM, int Sign, double GradMag, double CurvMag, bool DirMatch, double SignGradMag);
    private record ZoneResult(string Arch, string Zone, int ZoneIdx, int Count,
        double GradMean, double CurvMean, double CohPct, double SignGradMean, double AmpR, double CurvR);
}
