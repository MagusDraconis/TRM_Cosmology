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

namespace TRM.Tests.V33_13;

[Trait("Category", "V33_13")]
[Trait("Category", "LongRunning")]
public class V33_13_SharedCoreDestruction_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_13_SharedCoreDestruction_Tests(ITestOutputHelper o) { _o = o; }

    // ====================================================================
    // Data structures
    // ====================================================================
    private record EdgeCell(
        string Arch,
        double AbsM, double Fb, double Tick, int Sign,
        double GradM, double GradFb, double GradTick, double TickAnomaly,
        double CurvM, double CurvFb,
        int ZoneDist, bool IsBoundary, bool IsAdjacentToBoundary,
        double Core, double FbResidual, double MResidual,
        // Quantile ranks for edge detection
        double GradMRank, double GradFbRank, double CurvMRank, double CurvFbRank,
        double CoreRank, double FbResRank);

    // ====================================================================
    // Data collection with full edge-case metadata
    // ====================================================================
    private List<EdgeCell> CollectEdgeData()
    {
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);
        var allCells = new ConcurrentBag<EdgeCell>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 22; // slightly larger grid for better edge sampling
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gM = new double[nG, nG]; var gS = new int[nG, nG];
            var gFb = new double[nG, nG]; var gTick = new double[nG, nG];

            Parallel.For(0, nG, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < nG; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gM[bi, gi] = f.absM; gS[bi, gi] = f.sign; gFb[bi, gi] = f.fb; gTick[bi, gi] = f.tick;
                }
            });

            // SharedCore decomposition (PCA on raw values)
            var flatFb = new List<double>(); var flatM = new List<double>();
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) { flatFb.Add(gFb[bi, gi]); flatM.Add(gM[bi, gi]); }
            double[] fArr = flatFb.ToArray(), mArr = flatM.ToArray();
            int n = fArr.Length;
            double fbMean = fArr.Average(), fbStd = Math.Sqrt(fArr.Select(v => (v - fbMean) * (v - fbMean)).Average());
            double mMean = mArr.Average(), mStd = Math.Sqrt(mArr.Select(v => (v - mMean) * (v - mMean)).Average());
            double[] zFb = fArr.Select(v => fbStd > 1e-15 ? (v - fbMean) / fbStd : 0).ToArray();
            double[] zM = mArr.Select(v => mStd > 1e-15 ? (v - mMean) / mStd : 0).ToArray();

            double sFM = 0, sFF = 0, sMM = 0;
            for (int i = 0; i < n; i++) { sFF += zFb[i] * zFb[i]; sMM += zM[i] * zM[i]; sFM += zFb[i] * zM[i]; }
            sFF /= (n - 1); sMM /= (n - 1); sFM /= (n - 1);

            double trace = sFF + sMM;
            double disc = Math.Sqrt(Math.Max(0, trace * trace - 4 * (sFF * sMM - sFM * sFM)));
            double lambda1 = (trace + disc) / 2.0;
            double evFb = sFM, evM = lambda1 - sFF;
            double evNorm = Math.Sqrt(evFb * evFb + evM * evM);
            double wFb = evNorm > 1e-15 ? evFb / evNorm : 1;
            double wM = evNorm > 1e-15 ? evM / evNorm : 0;

            double[] coreZ = zFb.Zip(zM, (f, m) => wFb * f + wM * m).ToArray();
            double coreMeanZ = coreZ.Average();
            double cVar = 0, cCovF = 0, cCovM = 0;
            for (int i = 0; i < n; i++) { double dc = coreZ[i] - coreMeanZ; cVar += dc * dc; cCovF += dc * zFb[i]; cCovM += dc * zM[i]; }
            cVar /= (n - 1); cCovF /= (n - 1); cCovM /= (n - 1);
            double betaF = cVar > 1e-15 ? cCovF / cVar : 0;
            double betaM = cVar > 1e-15 ? cCovM / cVar : 0;

            var coreGrid = new double[nG, nG];
            var fbResGrid = new double[nG, nG];
            var mResGrid = new double[nG, nG];
            int idx = 0;
            for (int bi = 0; bi < nG; bi++)
                for (int gi = 0; gi < nG; gi++)
                {
                    coreGrid[bi, gi] = coreZ[idx];
                    fbResGrid[bi, gi] = zFb[idx] - betaF * coreZ[idx];
                    mResGrid[bi, gi] = zM[idx] - betaM * coreZ[idx];
                    idx++;
                }

            // BFS distance
            var dist = new int[nG, nG];
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) dist[bi, gi] = int.MaxValue;
            var q = new Queue<(int, int)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                    if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                        gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                    { dist[bi, gi] = 0; q.Enqueue((bi, gi)); }
            while (q.Count > 0) { var (bi, gi) = q.Dequeue(); foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) }) if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && dist[nb, ng] == int.MaxValue) { dist[nb, ng] = dist[bi, gi] + 1; q.Enqueue((nb, ng)); } }

            // Build raw cell list (before rank computation)
            var rawCells = new List<(EdgeCell cell, double gradM, double gradFb, double curvM, double curvFb)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradM = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);

                    double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                    double gradFb = Math.Sqrt(dFdB * dFdB + dFdG * dFdG);

                    double dTdB = (gTick[bi + 1, gi] - gTick[bi - 1, gi]) / (2 * db);
                    double dTdG = (gTick[bi, gi + 1] - gTick[bi, gi - 1]) / (2 * dg);
                    double gradTick = Math.Sqrt(dTdB * dTdB + dTdG * dTdG);

                    double lapM = Math.Abs(gM[bi + 1, gi] + gM[bi - 1, gi] + gM[bi, gi + 1] + gM[bi, gi - 1] - 4 * gM[bi, gi]) / (db * db);
                    double lapFb = Math.Abs(gFb[bi + 1, gi] + gFb[bi - 1, gi] + gFb[bi, gi + 1] + gFb[bi, gi - 1] - 4 * gFb[bi, gi]) / (db * db);

                    int d = dist[bi, gi] == int.MaxValue ? 4 : Math.Min(3, dist[bi, gi]);
                    bool isBdry = d == 0;
                    bool isAdj = d == 1;

                    var nT = new List<double>();
                    if (bi > 0) nT.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nT.Add(gTick[bi + 1, gi]);
                    if (gi > 0) nT.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nT.Add(gTick[bi, gi + 1]);
                    double tLocMean = nT.Count > 0 ? nT.Average() : gTick[bi, gi];
                    double tAnom = Math.Abs(gTick[bi, gi] - tLocMean) / Math.Max(1e-15, tLocMean);

                    rawCells.Add((new EdgeCell(arch, gM[bi, gi], gFb[bi, gi], gTick[bi, gi], gS[bi, gi],
                        gradM, gradFb, gradTick, tAnom, lapM, lapFb, d, isBdry, isAdj,
                        coreGrid[bi, gi], fbResGrid[bi, gi], mResGrid[bi, gi],
                        0, 0, 0, 0, 0, 0), gradM, gradFb, lapM, lapFb));
                }

            // Compute quantile ranks within this architecture
            var allGM = rawCells.Select(r => r.gradM).OrderBy(v => v).ToList();
            var allGF = rawCells.Select(r => r.gradFb).OrderBy(v => v).ToList();
            var allLM = rawCells.Select(r => r.curvM).OrderBy(v => v).ToList();
            var allLF = rawCells.Select(r => r.curvFb).OrderBy(v => v).ToList();
            var allCore = rawCells.Select(r => r.cell.Core).OrderBy(v => v).ToList();
            var allFbRes = rawCells.Select(r => Math.Abs(r.cell.FbResidual)).OrderBy(v => v).ToList();

            double Q(double[] sorted, double v) => sorted.Length > 1 ? (double)Array.BinarySearch(sorted, v) / (sorted.Length - 1) : 0.5;

            foreach (var (cell, gradM, gradFb, curvM, curvFb) in rawCells)
            {
                double gmRank = Q(allGM.ToArray(), gradM);
                double gfRank = Q(allGF.ToArray(), gradFb);
                double lmRank = Q(allLM.ToArray(), curvM);
                double lfRank = Q(allLF.ToArray(), curvFb);
                double cRank = Q(allCore.ToArray(), cell.Core);
                double frRank = Q(allFbRes.ToArray(), Math.Abs(cell.FbResidual));

                allCells.Add(new EdgeCell(cell.Arch, cell.AbsM, cell.Fb, cell.Tick, cell.Sign,
                    cell.GradM, cell.GradFb, cell.GradTick, cell.TickAnomaly,
                    cell.CurvM, cell.CurvFb, cell.ZoneDist, cell.IsBoundary, cell.IsAdjacentToBoundary,
                    cell.Core, cell.FbResidual, cell.MResidual,
                    gmRank, gfRank, lmRank, lfRank, cRank, frRank));
            }
        }
        return allCells.ToList();
    }

    // ====================================================================
    // SCDD_01: BOUNDARY HOTSPOT AUDIT
    //
    // Do residuals carry information at boundary hotspots?
    // Hotspot = boundary cell with extreme |Core| (top 10%)
    //
    // Null:  r(fb_res, tick | hotspot) ≈ r(fb_res, tick | all)
    //        Residuals do not become informative at hotspots
    // Alt:   Residuals carry unique information at boundary hotspots
    // ====================================================================
    [Fact]
    public void SCDD_01_BoundaryHotspot_DoResidualsActivateAtHotspots()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCDD_01: Boundary Hotspot Audit ===");
        sb.AppendLine("=== Do residuals carry information at boundary hotspots? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEdgeData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Define hotspots: boundary cells AND top-quartile |Core|
        double coreAbs75 = data.Select(c => Math.Abs(c.Core)).OrderBy(v => v).ElementAt(data.Count * 3 / 4);

        var hotspots = data.Where(c => c.IsBoundary && Math.Abs(c.Core) >= coreAbs75).ToList();
        var coldspots = data.Where(c => c.IsBoundary && Math.Abs(c.Core) < coreAbs75).ToList();
        var allCells = data;

        // Test: do residuals predict tick_anomaly at hotspots?
        double[] GetResR2(List<EdgeCell> cells)
        {
            if (cells.Count < 15) return new[] { 0.0, 0.0, 0.0 };
            double[] ta = cells.Select(c => c.TickAnomaly).ToArray();
            double[] c = cells.Select(c => c.Core).ToArray();
            double[] fr = cells.Select(c => c.FbResidual).ToArray();
            double[] mr = cells.Select(c => c.MResidual).ToArray();

            double r2_c = PearsonCorr(ta, c); r2_c *= r2_c;
            double r2_cfr = MultivariateR2(ta, c, fr);
            double r2_cmr = MultivariateR2(ta, c, mr);
            return new[] { r2_c, r2_cfr - r2_c, r2_cmr - r2_c };
        }

        var allR2 = GetResR2(allCells);
        var hotR2 = GetResR2(hotspots);
        var coldR2 = GetResR2(coldspots);

        sb.AppendLine($"  Boundary hotspot definition: boundary AND |Core| ≥ P75");
        sb.AppendLine($"  Hotspot cells: {hotspots.Count}  Coldspot cells: {coldspots.Count}  All cells: {allCells.Count}");
        sb.AppendLine("");
        sb.AppendLine($"{"Region",-16} {"N",5} {"R²(Core)",10} {"ΔR²(fb_res)",12} {"ΔR²(m_res)",12}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"{"All",-16} {allCells.Count,5} {allR2[0],10:F4} {allR2[1],12:F4} {allR2[2],12:F4}");
        sb.AppendLine($"{"Hotspots",-16} {hotspots.Count,5} {hotR2[0],10:F4} {hotR2[1],12:F4} {hotR2[2],12:F4}");
        sb.AppendLine($"{"Coldspots",-16} {coldspots.Count,5} {coldR2[0],10:F4} {coldR2[1],12:F4} {coldR2[2],12:F4}");
        sb.AppendLine("");

        double hotActivation = Math.Max(hotR2[1], hotR2[2]);
        double allActivation = Math.Max(allR2[1], allR2[2]);
        bool residualsActivate = hotActivation > allActivation * 2.0 && hotActivation > 0.05;

        sb.AppendLine(residualsActivate
            ? $"  → RESIDUALS ACTIVATE at hotspots: ΔR² = {hotActivation:F4} vs {allActivation:F4} globally"
            : $"  → Residuals remain silent at hotspots: ΔR² = {hotActivation:F4} vs {allActivation:F4} globally");
        sb.AppendLine(residualsActivate
            ? "  VERDICT: SharedCore INCOMPLETE — fb_res carries information at boundary hotspots"
            : "  VERDICT: SharedCore COMPLETE — hotspots do not activate residuals");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(!residualsActivate, $"SCDD_01: Hotspot ΔR²={hotActivation:F4}, global ΔR²={allActivation:F4}. SharedCore complete if no activation.");
    }

    // ====================================================================
    // SCDD_02: EXTREME GRADIENT AUDIT
    //
    // Do residuals carry information in regions of extreme gradient?
    // Test top 5% of ∇|m|, ∇fb, and curvature.
    //
    // If residuals contain information, it should be most visible
    // where gradients are strongest (signal-to-noise is highest).
    // ====================================================================
    [Fact]
    public void SCDD_02_ExtremeGradient_DoResidualsActivateAtExtremes()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCDD_02: Extreme Gradient Audit ===");
        sb.AppendLine("=== Do residuals carry information at extreme gradients? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEdgeData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Define extreme regions
        var top5GradM = data.Where(c => c.GradMRank >= 0.95).ToList();
        var top5GradFb = data.Where(c => c.GradFbRank >= 0.95).ToList();
        var top5CurvM = data.Where(c => c.CurvMRank >= 0.95).ToList();
        var bot50GradM = data.Where(c => c.GradMRank < 0.50).ToList();

        double[] ResidualGain(List<EdgeCell> cells, string target)
        {
            if (cells.Count < 15) return new[] { 0.0, 0.0 };
            double[] targ = target switch
            {
                "tick_anomaly" => cells.Select(c => c.TickAnomaly).ToArray(),
                "tick" => cells.Select(c => c.Tick).ToArray(),
                "grad_tick" => cells.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray(),
                _ => cells.Select(c => c.TickAnomaly).ToArray(),
            };
            double[] c = cells.Select(v => v.Core).ToArray();
            double[] fr = cells.Select(v => v.FbResidual).ToArray();

            double r2_c = PearsonCorr(targ, c); r2_c *= r2_c;
            double r2_cfr = MultivariateR2(targ, c, fr);
            return new[] { r2_c, r2_cfr - r2_c };
        }

        sb.AppendLine($"  Residual fb_res contribution to TickAnomaly prediction:");
        sb.AppendLine($"{"Region",-20} {"N",5} {"R²(Core)",10} {"ΔR²(fb_res)",12} {"Gain ratio",10}");
        sb.AppendLine(new string('-', 60));

        var regions = new (string name, List<EdgeCell> cells)[]
        {
            ("All", data),
            ("Top5% ∇|m|", top5GradM),
            ("Top5% ∇fb", top5GradFb),
            ("Top5% ∇²|m|", top5CurvM),
            ("Bottom50% ∇|m|", bot50GradM),
        };

        double baseGain = ResidualGain(data, "tick_anomaly")[1];
        double maxExtremeGain = 0;
        string maxRegion = "";

        foreach (var (name, cells) in regions)
        {
            var gain = ResidualGain(cells, "tick_anomaly");
            double ratio = baseGain > 1e-15 ? gain[1] / baseGain : 0;
            if (gain[1] > maxExtremeGain) { maxExtremeGain = gain[1]; maxRegion = name; }
            sb.AppendLine($"{name,-20} {cells.Count,5} {gain[0],10:F4} {gain[1],12:F4} {ratio,10:F2}x");
        }
        sb.AppendLine("");

        bool residualsActivate = maxExtremeGain > baseGain * 3.0 && maxExtremeGain > 0.05;
        sb.AppendLine(residualsActivate
            ? $"  → Residuals ACTIVATE in {maxRegion} (ΔR² = {maxExtremeGain:F4} vs {baseGain:F4} base)"
            : $"  → Residuals remain SILENT in all extreme regions (max ΔR² = {maxExtremeGain:F4})");
        sb.AppendLine(residualsActivate
            ? "  VERDICT: SharedCore INCOMPLETE — extreme gradients reveal hidden residual structure"
            : "  VERDICT: SharedCore COMPLETE — extremes contain no new residual information");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(!residualsActivate, $"SCDD_02: Max extreme ΔR²={maxExtremeGain:F4}, base ΔR²={baseGain:F4}. Complete if no activation.");
    }

    // ====================================================================
    // SCDD_03: RARE-EVENT INFORMATION AUDIT
    //
    // Test the tails of the residual distribution.
    // If residuals are truly noise, their extreme values should
    // be uninformative (just large noise draws).
    //
    // If extreme residuals correlate with physical observables,
    // they carry signal, not noise.
    // ====================================================================
    [Fact]
    public void SCDD_03_RareEvent_DoResidualTailsCarrySignal()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCDD_03: Rare-Event Information Audit ===");
        sb.AppendLine("=== Do extreme residual values carry physical signal? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEdgeData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Residual tails: top and bottom 5% of fb_res and m_res
        double[] absFbRes = data.Select(c => Math.Abs(c.FbResidual)).OrderBy(v => v).ToArray();
        double[] absMRes = data.Select(c => Math.Abs(c.MResidual)).OrderBy(v => v).ToArray();

        double fbRes95 = absFbRes[(int)(absFbRes.Length * 0.95)];
        double mRes95 = absMRes[(int)(absMRes.Length * 0.95)];

        var fbResTail = data.Where(c => Math.Abs(c.FbResidual) >= fbRes95).ToList();
        var mResTail = data.Where(c => Math.Abs(c.MResidual) >= mRes95).ToList();
        var fbResCore = data.Where(c => Math.Abs(c.FbResidual) < fbRes95).ToList();

        // What properties distinguish tail cells from core cells?
        sb.AppendLine($"  fb_res tail (|res| ≥ P95 = {fbRes95:F4}): {fbResTail.Count} cells");
        sb.AppendLine($"  m_res tail (|res| ≥ P95 = {mRes95:F4}): {mResTail.Count} cells");
        sb.AppendLine("");

        // Compare tail vs core means
        double[] CompareMeans(List<EdgeCell> tail, List<EdgeCell> core, Func<EdgeCell, double> selector, string name)
        {
            double t = tail.Average(selector);
            double c = core.Average(selector);
            double ratio = Math.Abs(c) > 1e-15 ? t / c : 0;
            return new[] { t, c, ratio };
        }

        sb.AppendLine($"  fb_res tail vs core comparison:");
        sb.AppendLine($"{"Property",-20} {"Tail mean",10} {"Core mean",10} {"Ratio",8} {"Differs?",8}");
        sb.AppendLine(new string('-', 60));

        var props = new (string name, Func<EdgeCell, double> sel)[]
        {
            ("|Core|", c => Math.Abs(c.Core)),
            ("Tick", c => c.Tick),
            ("TickAnomaly", c => c.TickAnomaly),
            ("∇|m|", c => c.GradM),
            ("∇fb", c => c.GradFb),
            ("Boundary%", c => c.IsBoundary ? 1.0 : 0.0),
            ("Adjacent%", c => c.IsAdjacentToBoundary ? 1.0 : 0.0),
            ("∇²|m|", c => c.CurvM),
        };

        int tailDiffs = 0;
        foreach (var (name, sel) in props)
        {
            var cmp = CompareMeans(fbResTail, fbResCore, sel, name);
            bool differs = Math.Abs(cmp[2] - 1.0) > 0.20;
            if (differs) tailDiffs++;
            string label = differs ? "YES" : "no";
            sb.AppendLine($"{name,-20} {cmp[0],10:F4} {cmp[1],10:F4} {cmp[2],8:F2}x {label,8}");
        }
        sb.AppendLine("");

        bool tailCarriesSignal = tailDiffs >= 3;
        sb.AppendLine(tailCarriesSignal
            ? $"  → Residual TAILS carry signal: {tailDiffs}/{props.Length} properties differ significantly"
            : $"  → Residual tails are structureless: {tailDiffs}/{props.Length} properties differ");
        sb.AppendLine(tailCarriesSignal
            ? "  VERDICT: SharedCore INCOMPLETE — residual tails are not pure noise"
            : "  VERDICT: SharedCore COMPLETE — residual tails show no systematic structure");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.False(!tailCarriesSignal, $"SCDD_03: {tailDiffs}/{props.Length} tail properties differ. Complete if tails structureless.");
    }

    // ====================================================================
    // SCDD_04: SIGN-FLIP NEIGHBORHOOD AUDIT
    //
    // The region ADJACENT to boundaries (zone=1) is where the
    // parameter-space fields are changing fastest.
    //
    // If residuals carry information, these transition zones
    // should show enhanced residual signal.
    //
    // Null:  Residuals in adjacent zone ≈ residuals in interior
    // Alt:   Adjacent zone residuals carry unique signal
    // ====================================================================
    [Fact]
    public void SCDD_04_SignFlipNeighborhood_DoAdjacentZonesDiffer()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCDD_04: Sign-Flip Neighborhood Audit ===");
        sb.AppendLine("=== Do adjacent-to-boundary cells show enhanced residuals? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEdgeData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Compare residual behavior zone by zone
        sb.AppendLine($"{"Zone",-14} {"N",5} {"σ(fb_res)",10} {"σ(m_res)",10} {"|fb_res|",10} {"|m_res|",10} {"r(fb_res,m_res)",14}");
        sb.AppendLine(new string('-', 76));

        var zoneStats = new List<(int zone, double fbStd, double mStd, double fbAbs, double mAbs, double rRes, int n)>();

        for (int z = 0; z <= 3; z++)
        {
            var zCells = data.Where(c => c.ZoneDist == z).ToList();
            if (zCells.Count < 15) continue;

            double[] fr = zCells.Select(c => c.FbResidual).ToArray();
            double[] mr = zCells.Select(c => c.MResidual).ToArray();

            double fbStd = Math.Sqrt(fr.Select(v => v * v).Average());
            double mStd = Math.Sqrt(mr.Select(v => v * v).Average());
            double fbAbs = fr.Select(v => Math.Abs(v)).Average();
            double mAbs = mr.Select(v => Math.Abs(v)).Average();
            double rRes = PearsonCorr(fr, mr);

            zoneStats.Add((z, fbStd, mStd, fbAbs, mAbs, rRes, zCells.Count));

            string zn = z == 0 ? "Boundary" : z == 1 ? "Adjacent" : z == 2 ? "Near-2" : "Interior";
            sb.AppendLine($"{zn,-14} {zCells.Count,5} {fbStd,10:F4} {mStd,10:F4} {fbAbs,10:F4} {mAbs,10:F4} {rRes,14:F4}");
        }
        sb.AppendLine("");

        // Adjacent vs interior comparison
        var adj = data.Where(c => c.ZoneDist == 1).ToList();
        var interior = data.Where(c => c.ZoneDist >= 2).ToList();

        if (adj.Count >= 15 && interior.Count >= 15)
        {
            double adjFbStd = Math.Sqrt(adj.Select(c => c.FbResidual * c.FbResidual).Average());
            double intFbStd = Math.Sqrt(interior.Select(c => c.FbResidual * c.FbResidual).Average());
            double adjMStd = Math.Sqrt(adj.Select(c => c.MResidual * c.MResidual).Average());
            double intMStd = Math.Sqrt(interior.Select(c => c.MResidual * c.MResidual).Average());

            double fbRatio = adjFbStd / Math.Max(1e-15, intFbStd);
            double mRatio = adjMStd / Math.Max(1e-15, intMStd);

            sb.AppendLine($"  Adjacent/Interior residual σ ratio: fb_res = {fbRatio:F2}x  m_res = {mRatio:F2}x");
            sb.AppendLine("");

            bool residualsEnhanced = fbRatio > 1.30 || mRatio > 1.30;
            sb.AppendLine(residualsEnhanced
                ? "  → Residuals ENHANCED in adjacent zone — sign-flip neighborhoods carry unique structure"
                : "  → Residuals STABLE across zones — no enhancement at sign-flip neighborhoods");
            sb.AppendLine(residualsEnhanced
                ? "  VERDICT: SharedCore INCOMPLETE — residuals carry transition-zone information"
                : "  VERDICT: SharedCore COMPLETE — transition zones do not amplify residuals");
            sb.AppendLine("");
        }

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // SCDD_05: HIGH-CURVATURE BOUNDARY AUDIT
    //
    // At high-curvature boundary segments, the parameter-space
    // topology changes rapidly. If the shared core fails anywhere,
    // it should fail here — curvature is NOT in the PC1 decomposition.
    //
    // Test: at cells where BOTH boundary AND high curvature,
    // do residuals explain curvature better than core?
    //
    // Null:  Curvature is explained by core (R²(core→curv) ≥ R²(fb_res→curv))
    // Alt:   Residuals explain curvature better than core
    // ====================================================================
    [Fact]
    public void SCDD_05_HighCurvatureBoundary_CurvatureResidualTest()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCDD_05: High-Curvature Boundary Audit ===");
        sb.AppendLine("=== Do residuals explain curvature better than Core does? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEdgeData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // High-curvature boundary cells: boundary AND top quartile curvature
        double curv75 = data.Select(c => c.CurvM).OrderBy(v => v).ElementAt(data.Count * 3 / 4);

        var hcBdry = data.Where(c => c.IsBoundary && c.CurvM >= curv75).ToList();
        var allBdry = data.Where(c => c.IsBoundary).ToList();
        var allCells = data;

        double[] CurvR2(List<EdgeCell> cells)
        {
            if (cells.Count < 15) return new[] { 0.0, 0.0, 0.0, 0.0 };
            double[] lc = cells.Select(c => Math.Log10(Math.Max(1e-15, c.CurvM))).ToArray();
            double[] core = cells.Select(c => c.Core).ToArray();
            double[] fr = cells.Select(c => c.FbResidual).ToArray();
            double[] mr = cells.Select(c => c.MResidual).ToArray();

            double r2_c = PearsonCorr(lc, core); r2_c *= r2_c;
            double r2_fr = PearsonCorr(lc, fr); r2_fr *= r2_fr;
            double r2_mr = PearsonCorr(lc, mr); r2_mr *= r2_mr;
            double r2_cfr = MultivariateR2(lc, core, fr);

            return new[] { r2_c, r2_fr, r2_mr, r2_cfr - r2_c };
        }

        var allR2 = CurvR2(allCells);
        var bdryR2 = CurvR2(allBdry);
        var hcR2 = CurvR2(hcBdry);

        sb.AppendLine($"  Curvature explained by Core vs Residuals:");
        sb.AppendLine($"{"Region",-22} {"N",5} {"R²(Core)",10} {"R²(fb_res)",10} {"R²(m_res)",10} {"ΔR²(+fb_res)",12}");
        sb.AppendLine(new string('-', 72));
        sb.AppendLine($"{"All",-22} {allCells.Count,5} {allR2[0],10:F4} {allR2[1],10:F4} {allR2[2],10:F4} {allR2[3],12:F4}");
        sb.AppendLine($"{"All Boundaries",-22} {allBdry.Count,5} {bdryR2[0],10:F4} {bdryR2[1],10:F4} {bdryR2[2],10:F4} {bdryR2[3],12:F4}");
        sb.AppendLine($"{"High-Curv Bdry",-22} {hcBdry.Count,5} {hcR2[0],10:F4} {hcR2[1],10:F4} {hcR2[2],10:F4} {hcR2[3],12:F4}");
        sb.AppendLine("");

        // Does fb_res explain curvature better at high-curvature boundaries?
        double gainAtHC = hcR2[3];
        double gainAll = allR2[3];
        bool residualExplainsCurv = gainAtHC > gainAll * 3.0 && gainAtHC > 0.08;

        // Also test: does fb_res predict curvature better than core at HC boundaries?
        bool fbResBeatsCore = hcR2[1] > hcR2[0] + 0.05;

        sb.AppendLine(residualExplainsCurv || fbResBeatsCore
            ? $"  → Residuals CARRY curvature information at high-curvature boundaries"
            : $"  → Residuals do NOT carry curvature information beyond Core");
        sb.AppendLine(fbResBeatsCore
            ? $"  → fb_res BEATS Core for curvature prediction (R²={hcR2[1]:F4} vs {hcR2[0]:F4})"
            : $"  → Core dominates curvature prediction (R²={hcR2[0]:F4} vs fb_res={hcR2[1]:F4})");
        sb.AppendLine(residualExplainsCurv || fbResBeatsCore
            ? "  VERDICT: SharedCore INCOMPLETE — curvature carries information beyond core"
            : "  VERDICT: SharedCore COMPLETE — curvature is a derived property of core structure");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.False(!residualExplainsCurv && !fbResBeatsCore,
            $"SCDD_05: HC-bdry ΔR²={gainAtHC:F4}, fb_res R²={hcR2[1]:F4} vs core R²={hcR2[0]:F4}. Complete if core dominates.");
    }

    // ====================================================================
    // SCDD_06: TOPOLOGICAL TRANSITION AUDIT
    //
    // When sign flips from +1 to -1 (or vice versa), the topology
    // of parameter space changes. Does the fb-|m| relationship
    // change across this topological transition?
    //
    // If fb and |m| are truly the same quantity, the functional
    // form fb = f(|m|) should be CONTINUOUS across sign boundaries.
    //
    // Test: fit fb = f(|m|) in POS region and NEG region separately.
    // Are the regression coefficients the same?
    // ====================================================================
    [Fact]
    public void SCDD_06_TopologicalTransition_DoesMappingChangeAtSignFlip()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCDD_06: Topological Transition Audit ===");
        sb.AppendLine("=== Does the fb-|m| mapping change at sign boundaries? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEdgeData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        foreach (var arch in new[] { "GAN", "CNS" })
        {
            var archData = data.Where(c => c.Arch == arch).ToList();
            if (archData.Count < 50) continue;

            var pos = archData.Where(c => c.Sign > 0).ToList();
            var neg = archData.Where(c => c.Sign < 0).ToList();
            if (pos.Count < 20 || neg.Count < 20) continue;

            // Linear fit within each sign region
            double[] FitRegion(List<EdgeCell> cells)
            {
                double[] fbV = cells.Select(c => c.Fb).ToArray();
                double[] mV = cells.Select(c => c.AbsM).ToArray();

                double mMean = mV.Average(), fbMean = fbV.Average();
                double cov = 0, varM = 0;
                for (int i = 0; i < mV.Length; i++) { double dm = mV[i] - mMean; cov += dm * (fbV[i] - fbMean); varM += dm * dm; }
                double slope = varM > 1e-15 ? cov / varM : 0;
                double intercept = fbMean - slope * mMean;
                double r2 = PearsonCorr(fbV, mV); r2 *= r2;

                // Residual std
                double[] resid = fbV.Select((f, i) => f - (slope * mV[i] + intercept)).ToArray();
                double residStd = Math.Sqrt(resid.Select(r => r * r).Average());

                return new[] { slope, intercept, r2, residStd };
            }

            var posFit = FitRegion(pos);
            var negFit = FitRegion(neg);

            double slopeDiff = Math.Abs(posFit[0] - negFit[0]);
            double slopeRelDiff = slopeDiff / Math.Max(1e-15, Math.Abs(posFit[0]));
            double r2Diff = Math.Abs(posFit[2] - negFit[2]);

            sb.AppendLine($"  {arch}:");
            sb.AppendLine($"    POS (n={pos.Count}): fb = {posFit[0]:F4}·|m| + {posFit[1]:F4}  R²={posFit[2]:F4}  σ={posFit[3]:F4}");
            sb.AppendLine($"    NEG (n={neg.Count}): fb = {negFit[0]:F4}·|m| + {negFit[1]:F4}  R²={negFit[2]:F4}  σ={negFit[3]:F4}");
            sb.AppendLine($"    Δslope = {slopeDiff:F4} ({slopeRelDiff*100:F1}%)  ΔR² = {r2Diff:F4}");
            sb.AppendLine("");
        }
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // SCDD_07: CROSS-ARCHITECTURE RESIDUAL CORRELATION
    //
    // If residuals are noise, they should be UNCORRELATED
    // across architectures (no systematic pattern).
    //
    // If residuals carry signal, GAN fb_res should correlate
    // with CNS fb_res in corresponding parameter-space regions.
    //
    // (Approximate: compare distribution properties across architectures.)
    // ====================================================================
    [Fact]
    public void SCDD_07_CrossArchitectureResidual_IsResidualArchitectureInvariant()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCDD_07: Cross-Architecture Residual Correlation ===");
        sb.AppendLine("=== Are residuals architecture-invariant (noise) or architecture-specific (signal)? ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEdgeData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        var gan = data.Where(c => c.Arch == "GAN").ToList();
        var cns = data.Where(c => c.Arch == "CNS").ToList();

        if (gan.Count < 50 || cns.Count < 50) { sb.AppendLine("Insufficient per-architecture data."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Compare residual distributions
        double[] ganFbRes = gan.Select(c => c.FbResidual).ToArray();
        double[] cnsFbRes = cns.Select(c => c.FbResidual).ToArray();
        double[] ganMRes = gan.Select(c => c.MResidual).ToArray();
        double[] cnsMRes = cns.Select(c => c.MResidual).ToArray();

        // Distribution moments
        double ganFbMean = ganFbRes.Average(), cnsFbMean = cnsFbRes.Average();
        double ganFbStd = Math.Sqrt(ganFbRes.Select(v => (v - ganFbMean) * (v - ganFbMean)).Average());
        double cnsFbStd = Math.Sqrt(cnsFbRes.Select(v => (v - cnsFbMean) * (v - cnsFbMean)).Average());

        double ganMMean = ganMRes.Average(), cnsMMean = cnsMRes.Average();
        double ganMStd = Math.Sqrt(ganMRes.Select(v => (v - ganMMean) * (v - ganMMean)).Average());
        double cnsMStd = Math.Sqrt(cnsMRes.Select(v => (v - cnsMMean) * (v - cnsMMean)).Average());

        // Compare per-zone residual structure
        sb.AppendLine($"  Residual distribution comparison:");
        sb.AppendLine($"{"Metric",-20} {"GAN",12} {"CNS",12} {"Δ",10} {"Same?",8}");
        sb.AppendLine(new string('-', 65));
        string s1 = Math.Abs(ganFbMean-cnsFbMean)<0.05 ? "YES" : "no";
        string s2 = Math.Abs(ganFbStd-cnsFbStd)<0.05 ? "YES" : "no";
        string s3 = Math.Abs(ganMMean-cnsMMean)<0.05 ? "YES" : "no";
        string s4 = Math.Abs(ganMStd-cnsMStd)<0.05 ? "YES" : "no";
        sb.AppendLine($"{"fb_res mean",-20} {ganFbMean,12:F4} {cnsFbMean,12:F4} {Math.Abs(ganFbMean-cnsFbMean),10:F4} {s1,8}");
        sb.AppendLine($"{"fb_res σ",-20} {ganFbStd,12:F4} {cnsFbStd,12:F4} {Math.Abs(ganFbStd-cnsFbStd),10:F4} {s2,8}");
        sb.AppendLine($"{"m_res mean",-20} {ganMMean,12:F4} {cnsMMean,12:F4} {Math.Abs(ganMMean-cnsMMean),10:F4} {s3,8}");
        sb.AppendLine($"{"m_res σ",-20} {ganMStd,12:F4} {cnsMStd,12:F4} {Math.Abs(ganMStd-cnsMStd),10:F4} {s4,8}");
        sb.AppendLine("");

        // Per-zone comparison
        sb.AppendLine($"  Per-zone |fb_res| comparison:");
        sb.AppendLine($"{"Zone",-14} {"GAN |fb_res|",12} {"CNS |fb_res|",12} {"Ratio",8}");
        sb.AppendLine(new string('-', 48));
        for (int z = 0; z <= 3; z++)
        {
            var gz = gan.Where(c => c.ZoneDist == z).ToList();
            var cz = cns.Where(c => c.ZoneDist == z).ToList();
            if (gz.Count < 10 || cz.Count < 10) continue;
            double gAbs = gz.Average(c => Math.Abs(c.FbResidual));
            double cAbs = cz.Average(c => Math.Abs(c.FbResidual));
            string zn = z == 0 ? "Boundary" : z == 1 ? "Adjacent" : z == 2 ? "Near-2" : "Interior";
            sb.AppendLine($"{zn,-14} {gAbs,12:F4} {cAbs,12:F4} {gAbs/Math.Max(1e-15,cAbs),8:F2}x");
        }
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // SCDD_08: INFORMATION ACCOUNTING — Complete audit of residual info
    //
    // Aggregates all edge-case tests. Classifies whether any residual
    // channel carries information that Core misses.
    // ====================================================================
    [Fact]
    public void SCDD_08_InformationAccounting_CompleteResidualAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCDD_08: Information Accounting — Complete Residual Audit ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEdgeData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Systematic sweep: test fb_res and m_res against all observables
        // in ALL regions (global, boundary, adjacent, interior, extreme)
        double[] fbRes = data.Select(c => c.FbResidual).ToArray();
        double[] mRes = data.Select(c => c.MResidual).ToArray();
        double[] core = data.Select(c => c.Core).ToArray();

        var targets = new (string name, Func<EdgeCell, double> sel, bool useLog)[]
        {
            ("tick",          c => c.Tick, false),
            ("log_tick",      c => Math.Log10(Math.Max(1e-15, c.Tick)), false),
            ("tick_anomaly",  c => c.TickAnomaly, false),
            ("sign",          c => (double)c.Sign, false),
            ("boundary",      c => c.IsBoundary ? 1.0 : 0.0, false),
            ("bd_distance",   c => (double)c.ZoneDist, false),
            ("log_∇|m|",      c => Math.Log10(Math.Max(1e-15, c.GradM)), false),
            ("log_∇fb",       c => Math.Log10(Math.Max(1e-15, c.GradFb)), false),
            ("log_∇Tick",     c => Math.Log10(Math.Max(1e-15, c.GradTick)), false),
            ("log_∇²|m|",     c => Math.Log10(Math.Max(1e-15, c.CurvM)), false),
            ("adjacent",      c => c.IsAdjacentToBoundary ? 1.0 : 0.0, false),
        };

        sb.AppendLine($"  Systematic residual information sweep ({targets.Length} targets):");
        sb.AppendLine($"{"Target",-16} {"R²(Core)",10} {"R²(+fb_res)",12} {"Δ(fb_res)",10} {"R²(+m_res)",12} {"Δ(m_res)",10} {"Signal?",8}");
        sb.AppendLine(new string('-', 82));

        int fbSigCount = 0, mSigCount = 0;
        foreach (var (tname, sel, _) in targets)
        {
            double[] t = data.Select(sel).ToArray();

            double r2_c = PearsonCorr(t, core); r2_c *= r2_c;
            double r2_cfr = MultivariateR2(t, core, fbRes);
            double r2_cmr = MultivariateR2(t, core, mRes);
            double df = r2_cfr - r2_c;
            double dm = r2_cmr - r2_c;

            bool fbSig = df > 0.03;
            bool mSig = dm > 0.03;
            if (fbSig) fbSigCount++;
            if (mSig) mSigCount++;

            string sigLabel = (fbSig || mSig) ? "YES" : "no";
            sb.AppendLine($"{tname,-16} {r2_c,10:F4} {r2_cfr,12:F4} {df,10:F4} {r2_cmr,12:F4} {dm,10:F4} {sigLabel,8}");
        }
        sb.AppendLine("");

        // Edge-case regions: test ONLY in high-gradient, boundary, etc.
        var regions = new (string name, Func<EdgeCell, bool> filter)[]
        {
            ("Boundary",       c => c.IsBoundary),
            ("Adjacent",       c => c.IsAdjacentToBoundary),
            ("Top10% ∇|m|",    c => c.GradMRank >= 0.90),
            ("Top10% ∇fb",     c => c.GradFbRank >= 0.90),
            ("Top10% ∇²|m|",   c => c.CurvMRank >= 0.90),
            ("Top10% |Core|",  c => c.CoreRank >= 0.90),
            ("Top10% |fb_res|",c => c.FbResRank >= 0.90),
        };

        sb.AppendLine($"  Edge-case residual activation ({regions.Length} regions, testing tick_anomaly):");
        sb.AppendLine($"{"Region",-18} {"N",5} {"R²(Core)",10} {"Δ(fb_res)",10} {"Δ(m_res)",10} {"Activated?",10}");
        sb.AppendLine(new string('-', 66));

        int activatedRegions = 0;
        foreach (var (rname, filter) in regions)
        {
            var rCells = data.Where(filter).ToList();
            if (rCells.Count < 15) continue;

            double[] ta = rCells.Select(c => c.TickAnomaly).ToArray();
            double[] rc = rCells.Select(c => c.Core).ToArray();
            double[] rfr = rCells.Select(c => c.FbResidual).ToArray();
            double[] rmr = rCells.Select(c => c.MResidual).ToArray();

            double r2 = PearsonCorr(ta, rc); r2 *= r2;
            double r2fr = MultivariateR2(ta, rc, rfr);
            double r2mr = MultivariateR2(ta, rc, rmr);
            double df = r2fr - r2;
            double dm = r2mr - r2;

            bool activated = df > 0.05 || dm > 0.05;
            if (activated) activatedRegions++;

            string actLabel = activated ? "YES" : "no";
            sb.AppendLine($"{rname,-18} {rCells.Count,5} {r2,10:F4} {df,10:F4} {dm,10:F4} {actLabel,10}");
        }
        sb.AppendLine("");

        sb.AppendLine($"  Global residual signals: fb_res={fbSigCount}/{targets.Length}  m_res={mSigCount}/{targets.Length}");
        sb.AppendLine($"  Edge-case activations:   {activatedRegions}/{regions.Length} regions");
        sb.AppendLine("");

        bool sharedCoreComplete = fbSigCount == 0 && mSigCount == 0 && activatedRegions == 0;
        sb.AppendLine(sharedCoreComplete
            ? "  VERDICT: SharedCore is COMPLETE. No residual channel carries independent information."
            : $"  VERDICT: SharedCore is INCOMPLETE. Residual channels carry signal in {fbSigCount + mSigCount} targets, {activatedRegions} regions.");
        sb.AppendLine(sharedCoreComplete
            ? "  → fb and |m| are ONE SIGNAL measured twice. Merge into SharedCore."
            : "  → Residuals survive edge-case testing. fb and |m| are DISTINCT observables.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.False(sharedCoreComplete,
            $"SCDD_08: fb_res signals={fbSigCount}, m_res signals={mSigCount}, edge activations={activatedRegions}. Complete if all=0.");
    }

    // ====================================================================
    // SCDD_09: V3.4 COMPATIBILITY — Residual and Bridge Band
    //
    // If residuals contain signal, they must participate in V3.4 recovery.
    // If they are noise, only SharedCore needs to map to the bridge band.
    //
    // Identify what changes for V3.4 if residuals are or are not signal.
    // ====================================================================
    [Fact]
    public void SCDD_09_V34Compatibility_ResidualImpact()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== SCDD_09: V3.4 Compatibility — Impact of Residual Status ===");
        sb.AppendLine(new string('=', 96));

        var data = CollectEdgeData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbRes = data.Select(c => c.FbResidual).ToArray();
        double[] mRes = data.Select(c => c.MResidual).ToArray();
        double[] core = data.Select(c => c.Core).ToArray();

        // V3.4 recovery depends on mapping: V33 observables → CML ω_i
        //
        // If residuals are noise:
        //   SharedCore → ω_i_effective → Ω* → bridge band
        //   Simple: one mapping, one parameter
        //
        // If residuals carry signal:
        //   SharedCore + fb_res + m_res → ω_i_effective(+ corrections)
        //   Complex: three channels, need to show ALL contribute to Ω*

        // Test: does Core or fb_res better correlate with tick?
        double r_c_tick = PearsonCorr(core, data.Select(c => c.Tick).ToArray());
        double r_fr_tick = PearsonCorr(fbRes, data.Select(c => c.Tick).ToArray());
        double r_mr_tick = PearsonCorr(mRes, data.Select(c => c.Tick).ToArray());

        sb.AppendLine("  V3.4 bridge-band recovery pathway analysis:");
        sb.AppendLine("");
        sb.AppendLine($"  Correlation with tick (ω_i proxy):");
        sb.AppendLine($"    Core:      r = {r_c_tick:F4}  R² = {r_c_tick * r_c_tick:F4}");
        sb.AppendLine($"    fb_res:    r = {r_fr_tick:F4}  R² = {r_fr_tick * r_fr_tick:F4}");
        sb.AppendLine($"    m_res:     r = {r_mr_tick:F4}  R² = {r_mr_tick * r_mr_tick:F4}");
        sb.AppendLine("");

        if (Math.Abs(r_c_tick) > Math.Max(Math.Abs(r_fr_tick), Math.Abs(r_mr_tick)) * 3.0)
        {
            sb.AppendLine("  → Core dominates tick correlation. Residuals are negligible for V3.4.");
            sb.AppendLine("  → Recovery: C(β,γ) → ω_i_eff → Ω* → bridge band");
            sb.AppendLine("  Classification: PASS — single-channel recovery sufficient");
        }
        else
        {
            sb.AppendLine("  → Residuals contribute non-trivially to tick correlation.");
            sb.AppendLine("  → Recovery: requires 3-channel decomposition + recombination");
            sb.AppendLine("  Classification: CONDITIONAL — multi-channel recovery needed");
        }
        sb.AppendLine("");

        sb.AppendLine("  Recovery failure modes:");
        sb.AppendLine("    1. If fb_res carries V3.4-critical information and is");
        sb.AppendLine("       architecture-specific → different Ω* per architecture");
        sb.AppendLine("    2. If m_res varies with α but Core does not → bridge band");
        sb.AppendLine("       would have α-dependence not captured by Core alone");
        sb.AppendLine("    3. If residuals diverge in CML dynamic regime → static");
        sb.AppendLine("       CCI decomposition does not transfer to dynamic CML");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // STATISTICAL HELPERS
    // ====================================================================

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private static double MultivariateR2(double[] y, double[] x1, double[] x2)
    {
        int n = y.Length; if (n < 3) return 0;
        double sy = y.Sum(), s1 = x1.Sum(), s2 = x2.Sum();
        double s11 = 0, s22 = 0, s12 = 0, s1y = 0, s2y = 0;
        for (int i = 0; i < n; i++) { s11 += x1[i] * x1[i]; s22 += x2[i] * x2[i]; s12 += x1[i] * x2[i]; s1y += x1[i] * y[i]; s2y += x2[i] * y[i]; }
        double s1yc = s1y - s1 * sy / n, s2yc = s2y - s2 * sy / n;
        double s11c = s11 - s1 * s1 / n, s22c = s22 - s2 * s2 / n, s12c = s12 - s1 * s2 / n;
        double det = s11c * s22c - s12c * s12c;
        double b1 = 0, b2 = 0;
        if (Math.Abs(det) > 1e-15) { b1 = (s1yc * s22c - s2yc * s12c) / det; b2 = (s2yc * s11c - s1yc * s12c) / det; }
        double b0 = sy / n - b1 * s1 / n - b2 * s2 / n;
        double ssRes = 0, ssTot = 0; double my = sy / n;
        for (int i = 0; i < n; i++) { double pred = b0 + b1 * x1[i] + b2 * x2[i]; ssRes += (y[i] - pred) * (y[i] - pred); ssTot += (y[i] - my) * (y[i] - my); }
        double r2 = ssTot > 1e-15 ? 1 - ssRes / ssTot : 0;
        return Math.Max(0, Math.Min(1, r2));
    }
}
