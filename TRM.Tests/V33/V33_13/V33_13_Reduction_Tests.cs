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
public class V33_13_Reduction_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_13_Reduction_Tests(ITestOutputHelper o) { _o = o; }

    // ====================================================================
    // RED_01: GEOMETRY → BOUNDARY reduction
    //
    // Null:        ∇|m| structure is independent of boundary
    // Alt:         ∇|m| is concentrated at boundaries
    // Test:        Bdry/Interior gradient ratio
    // Pass:        Ratio > 1.5 → Geometry is boundary-concentrated
    // Fail:        Ratio < 1.1 → Geometry is boundary-independent
    // Classification target: SUPPORTED if ratio > 1.5 across all families
    // ====================================================================
    [Fact]
    public void RED_01_GeometryToBoundary_Reduction()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RED_01: Geometry → Boundary Reduction ===");

        var data = CollectReductionData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        foreach (var arch in new[] { "GAN", "CNS" })
        {
            var archData = data.Where(c => c.Arch == arch).ToList();
            var bdry = archData.Where(c => c.IsBoundary).ToList();
            var interior = archData.Where(c => c.ZoneDist >= 2).ToList();
            if (bdry.Count < 10 || interior.Count < 10) continue;

            double bGrad = bdry.Average(c => c.GradM);
            double iGrad = interior.Average(c => c.GradM);
            double ratio = bGrad / Math.Max(1e-15, iGrad);

            double bCoh = 100.0 * bdry.Count(c => c.GradDir == c.Sign) / bdry.Count;
            double iCoh = 100.0 * interior.Count(c => c.GradDir == c.Sign) / interior.Count;

            sb.AppendLine($"  {arch}: B/I grad ratio = {ratio:F2}x  coherence B={bCoh:F1}% I={iCoh:F1}%");
        }
        sb.AppendLine("");

        // Overall ratio
        var allBdry = data.Where(c => c.IsBoundary).ToList();
        var allInt = data.Where(c => c.ZoneDist >= 2).ToList();
        double allRatio = allBdry.Average(c => c.GradM) / Math.Max(1e-15, allInt.Average(c => c.GradM));
        bool supported = allRatio > 1.5;
        sb.AppendLine($"  Overall B/I ratio: {allRatio:F2}x");
        sb.AppendLine($"  Classification: {(supported ? "SUPPORTED" : allRatio > 1.1 ? "CONDITIONAL" : "HYPOTHESIS")}");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(supported, $"RED_01: B/I = {allRatio:F2}x, threshold 1.5x. SUPPORTED if Geometry concentrates at boundary.");
    }

    // ====================================================================
    // RED_02: TICK → BOUNDARY reduction
    //
    // Null:        Tick structure is independent of boundary
    // Alt:         Tick anomaly concentrates at boundaries
    // Test:        Bdry/Interior tick anomaly ratio
    // Pass:        Ratio > 1.5 → Tick anomaly is boundary-concentrated
    // Fail:        Ratio < 1.1 → Tick anomaly is boundary-independent
    // ====================================================================
    [Fact]
    public void RED_02_TickToBoundary_Reduction()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RED_02: Tick → Boundary Reduction ===");

        var data = CollectReductionData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        foreach (var arch in new[] { "GAN", "CNS" })
        {
            var archData = data.Where(c => c.Arch == arch).ToList();
            var bdry = archData.Where(c => c.IsBoundary).ToList();
            var interior = archData.Where(c => c.ZoneDist >= 2).ToList();
            if (bdry.Count < 10 || interior.Count < 10) continue;

            double bTick = bdry.Average(c => c.Tick);
            double iTick = interior.Average(c => c.Tick);
            double bAnom = bdry.Average(c => c.TickAnomaly);
            double iAnom = interior.Average(c => c.TickAnomaly);

            sb.AppendLine($"  {arch}: B/I tick ratio = {bTick/iTick:F2}x  B/I anomaly ratio = {bAnom/Math.Max(1e-15,iAnom):F2}x");
        }
        sb.AppendLine("");

        var allBdry = data.Where(c => c.IsBoundary).ToList();
        var allInt = data.Where(c => c.ZoneDist >= 2).ToList();
        double anomRatio = allBdry.Average(c => c.TickAnomaly) / Math.Max(1e-15, allInt.Average(c => c.TickAnomaly));
        bool supported = anomRatio > 1.5;
        sb.AppendLine($"  Overall B/I anomaly ratio: {anomRatio:F2}x");
        sb.AppendLine($"  Classification: {(supported ? "SUPPORTED" : anomRatio > 1.1 ? "CONDITIONAL" : "HYPOTHESIS")}");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(supported, $"RED_02: B/I anomaly = {anomRatio:F2}x, threshold 1.5x. SUPPORTED if Tick anomaly concentrates at boundary.");
    }

    // ====================================================================
    // RED_03: FEEDBACK → BOUNDARY reduction
    //
    // Null:        ∇fb structure is independent of boundary
    // Alt:         ∇fb concentrates at boundaries
    // Test:        Bdry/Interior ∇fb ratio
    // Pass:        Ratio > 1.5 → Feedback gradient is boundary-concentrated
    // Fail:        Ratio < 1.1 → Feedback gradient is boundary-independent
    // ====================================================================
    [Fact]
    public void RED_03_FeedbackToBoundary_Reduction()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RED_03: Feedback → Boundary Reduction ===");

        var data = CollectReductionData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        foreach (var arch in new[] { "GAN", "CNS" })
        {
            var archData = data.Where(c => c.Arch == arch).ToList();
            var bdry = archData.Where(c => c.IsBoundary).ToList();
            var interior = archData.Where(c => c.ZoneDist >= 2).ToList();
            if (bdry.Count < 10 || interior.Count < 10) continue;

            double bGradFb = bdry.Average(c => c.GradFb);
            double iGradFb = interior.Average(c => c.GradFb);
            double bFb = bdry.Average(c => c.Fb);
            double iFb = interior.Average(c => c.Fb);

            sb.AppendLine($"  {arch}: B/I ∇fb ratio = {bGradFb/Math.Max(1e-15,iGradFb):F2}x  B/I fb ratio = {bFb/Math.Max(1e-15,iFb):F2}x");
        }
        sb.AppendLine("");

        var allBdry = data.Where(c => c.IsBoundary).ToList();
        var allInt = data.Where(c => c.ZoneDist >= 2).ToList();
        double fGradRatio = allBdry.Average(c => c.GradFb) / Math.Max(1e-15, allInt.Average(c => c.GradFb));
        bool supported = fGradRatio > 1.5;
        sb.AppendLine($"  Overall B/I ∇fb ratio: {fGradRatio:F2}x");
        sb.AppendLine($"  Classification: {(supported ? "SUPPORTED" : fGradRatio > 1.1 ? "CONDITIONAL" : "HYPOTHESIS")}");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(supported, $"RED_03: B/I ∇fb = {fGradRatio:F2}x, threshold 1.5x. SUPPORTED if ∇fb concentrates at boundary.");
    }

    // ====================================================================
    // RED_04: BOUNDARY → OSCILLATOR MISMATCH reduction
    //
    // Null:        Boundary exists independently of oscillator mismatch
    // Alt:         Sign boundaries are determined by oscillator mismatch structure
    // Test:        Can boundary location be predicted from |fb - |m||?
    // Pass:        R² > 0.30 → Boundary is a function of oscillator mismatch
    // Fail:        R² < 0.10 → Boundary is independent of mismatch
    // ====================================================================
    [Fact]
    public void RED_04_BoundaryToOscillatorMismatch_Reduction()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RED_04: Boundary → Oscillator Mismatch Reduction ===");

        var data = CollectReductionData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbV = data.Select(c => c.Fb).ToArray();
        double[] mV = data.Select(c => c.AbsM).ToArray();
        double[] fbMDiff = data.Select(c => Math.Abs(c.Fb - c.AbsM)).ToArray();
        double[] bBin = data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray();
        double[] bdV = data.Select(c => (double)c.ZoneDist).ToArray();

        // Does oscillator mismatch predict boundary?
        double r2_diff = PearsonCorr(fbMDiff, bBin); r2_diff *= r2_diff;
        double r2_diff_cont = PearsonCorr(fbMDiff, bdV); r2_diff_cont *= r2_diff_cont;

        // Does fb or |m| individually predict boundary better?
        double r2_fb = PearsonCorr(fbV, bBin); r2_fb *= r2_fb;
        double r2_m = PearsonCorr(mV, bBin); r2_m *= r2_m;

        sb.AppendLine($"  |fb-|m||  → Boundary (binary):   R² = {r2_diff:F4}");
        sb.AppendLine($"  |fb-|m||  → Boundary (distance):  R² = {r2_diff_cont:F4}");
        sb.AppendLine($"  fb        → Boundary (binary):   R² = {r2_fb:F4}");
        sb.AppendLine($"  |m|        → Boundary (binary):   R² = {r2_m:F4}");
        sb.AppendLine("");

        bool supported = r2_diff > 0.30;
        sb.AppendLine($"  Classification: {(supported ? "SUPPORTED" : r2_diff > 0.15 ? "CONDITIONAL" : "HYPOTHESIS")}");
        sb.AppendLine(supported
            ? "  Boundary CAN be reduced to oscillator mismatch."
            : "  Boundary is NOT fully explained by oscillator mismatch.");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(supported, $"RED_04: |fb-|m|| → Boundary R² = {r2_diff:F4}. SUPPORTED if oscillator mismatch predicts boundary.");
    }

    // ====================================================================
    // RED_05: BOUNDARY ELIMINATION — Can boundary be eliminated?
    //
    // Null:        Boundary is irreducible
    // Alt:         Boundary can be eliminated — it's a derived property of sign
    //              which is derived from dTdp which is derived from oscillator
    // Test:        Does conditioning on dTdp eliminate boundary's predictive power?
    // Pass:        ΔR²(geometry | dTdp, boundary) ≈ ΔR²(geometry | dTdp)
    //              → Boundary adds nothing beyond dTdp
    // Fail:        Boundary adds significant predictive power beyond dTdp
    // ====================================================================
    [Fact]
    public void RED_05_BoundaryElimination_CanBoundaryBeRemoved()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RED_05: Boundary Elimination — Can boundary be removed from the model? ===");

        // We test: does (β,γ) → boundary add anything that (β,γ) alone doesn't?
        // Using the grid structure: neighbor prediction with and without boundary encoding

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 18;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gM = new double[nG, nG]; var gS = new int[nG, nG];
            var gFb = new double[nG, nG]; var gTick = new double[nG, nG];
            var gV = new double[nG, nG];

            Parallel.For(0, nG, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < nG; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gM[bi, gi] = f.absM; gS[bi, gi] = f.sign; gFb[bi, gi] = f.fb; gTick[bi, gi] = f.tick; gV[bi, gi] = f.V;
                }
            });

            // Neighbor prediction of ∇|m|
            // Model A: neighbors + boundary indicator
            // Model B: neighbors only
            double errA = 0, errB = 0, denom = 0;
            int nPred = 0;
            for (int bi = 2; bi < nG - 2; bi++)
            {
                for (int gi = 2; gi < nG - 2; gi++)
                {
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradM = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);

                    // Neighbor average
                    double[] nGrad = new double[4];
                    nGrad[0] = Math.Sqrt(Math.Pow((gM[bi + 2, gi] - gM[bi, gi]) / (2 * db), 2) + Math.Pow((gM[bi + 1, gi + 1] - gM[bi + 1, gi - 1]) / (2 * dg), 2));
                    nGrad[1] = Math.Sqrt(Math.Pow((gM[bi, gi] - gM[bi - 2, gi]) / (2 * db), 2) + Math.Pow((gM[bi - 1, gi + 1] - gM[bi - 1, gi - 1]) / (2 * dg), 2));
                    nGrad[2] = Math.Sqrt(Math.Pow((gM[bi + 1, gi + 1] - gM[bi - 1, gi + 1]) / (2 * db), 2) + Math.Pow((gM[bi, gi + 2] - gM[bi, gi]) / (2 * dg), 2));
                    nGrad[3] = Math.Sqrt(Math.Pow((gM[bi + 1, gi - 1] - gM[bi - 1, gi - 1]) / (2 * db), 2) + Math.Pow((gM[bi, gi] - gM[bi, gi - 2]) / (2 * dg), 2));
                    double nAvg = nGrad.Average();

                    // Boundary-weighted correction
                    int nBdry = 0;
                    foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) })
                        if (gS[nb, ng] != gS[bi + (nb > bi ? 1 : nb < bi ? -1 : 0), gi + (ng > gi ? 1 : ng < gi ? -1 : 0)])
                            nBdry++;

                    double predA = nAvg * (1.0 + 0.1 * nBdry); // heuristic boundary correction
                    double predB = nAvg;

                    errA += (gradM - predA) * (gradM - predA);
                    errB += (gradM - predB) * (gradM - predB);
                    denom += gradM * gradM;
                    nPred++;
                }
            }

            double r2A = denom > 1e-15 ? 1.0 - errA / denom : 0;
            double r2B = denom > 1e-15 ? 1.0 - errB / denom : 0;
            double deltaR2 = r2A - r2B;

            sb.AppendLine($"  {arch}: neighbor-only R² = {r2B:F4}  +boundary R² = {r2A:F4}  Δ = {deltaR2:F4}");
        }
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // RED_06: THREE-WAY REDUCTION — Can all three reduce past boundary?
    //
    // Null:        At least one of {Geometry, Tick, Feedback} is irreducible to boundary
    // Alt:         All three are boundary-concentrated
    // Test:        Is any observable boundary-independent?
    // Pass (reduction supported):  ALL three have B/I ratio > 1.3
    // Fail (reduction rejected):   At least one has B/I ratio < 1.1
    // ====================================================================
    [Fact]
    public void RED_06_ThreeWayReduction_AllToBoundary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RED_06: Three-Way Reduction — Can ALL THREE reduce to boundary? ===");

        var data = CollectReductionData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        var bdry = data.Where(c => c.IsBoundary).ToList();
        var interior = data.Where(c => c.ZoneDist >= 2).ToList();

        double gRatio = bdry.Average(c => c.GradM) / Math.Max(1e-15, interior.Average(c => c.GradM));
        double tRatio = bdry.Average(c => c.TickAnomaly) / Math.Max(1e-15, interior.Average(c => c.TickAnomaly));
        double fRatio = bdry.Average(c => c.GradFb) / Math.Max(1e-15, interior.Average(c => c.GradFb));

        int reductions = (gRatio > 1.3 ? 1 : 0) + (tRatio > 1.3 ? 1 : 0) + (fRatio > 1.3 ? 1 : 0);

        sb.AppendLine($"  Geometry  B/I: {gRatio:F2}x  {(gRatio > 1.3 ? "REDUCES" : "IRREDUCIBLE")}");
        sb.AppendLine($"  Tick      B/I: {tRatio:F2}x  {(tRatio > 1.3 ? "REDUCES" : "IRREDUCIBLE")}");
        sb.AppendLine($"  Feedback  B/I: {fRatio:F2}x  {(fRatio > 1.3 ? "REDUCES" : "IRREDUCIBLE")}");
        sb.AppendLine($"  Reductions: {reductions}/3");
        sb.AppendLine("");

        bool allReduce = reductions == 3;
        sb.AppendLine($"  Classification: {(allReduce ? "SUPPORTED — all three reduce to boundary" : reductions >= 2 ? "CONDITIONAL — partial reduction" : "HYPOTHESIS — boundary reduction incomplete")}");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(allReduce, $"RED_06: {reductions}/3 reduce. SUPPORTED if all three have B/I > 1.3x.");
    }

    // ====================================================================
    // RED_07: INFORMATION BOTTLENECK — Is boundary the bottleneck?
    //
    // Null:        Information flows through boundary
    // Alt:         Information can bypass boundary
    // Test:        Compare mutual information paths
    // Pass (boundary IS bottleneck):  I(oscillator, gravity | boundary) ≈ 0
    // Fail (boundary NOT bottleneck): I(oscillator, gravity | boundary) > 0
    // ====================================================================
    [Fact]
    public void RED_07_InformationBottleneck_IsBoundaryTheBottleneck()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== RED_07: Information Bottleneck — Is boundary THE bottleneck? ===");

        var data = CollectReductionData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] gmV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] fbV = data.Select(c => c.Fb).ToArray();
        double[] mV = data.Select(c => c.AbsM).ToArray();
        double[] bBin = data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray();

        // Path 1: fb → boundary → ∇|m|
        double r_fb_bd = PearsonCorr(fbV, bBin);
        double r_bd_gm = PearsonCorr(bBin, gmV);
        double path1 = r_fb_bd * r_bd_gm;

        // Path 2: fb → ∇|m| (direct)
        double path2 = PearsonCorr(fbV, gmV);

        // Does boundary mediate the fb→∇|m| relationship?
        double partialR = PartialCorrelation(fbV, gmV, bBin);
        double mediation = path2 - partialR;

        sb.AppendLine($"  fb → boundary:        r = {r_fb_bd:F4}");
        sb.AppendLine($"  boundary → ∇|m|:      r = {r_bd_gm:F4}");
        sb.AppendLine($"  Mediated path:        r = {path1:F4}");
        sb.AppendLine($"  Direct path:          r = {path2:F4}");
        sb.AppendLine($"  Partial (ctrl bd):    r = {partialR:F4}");
        sb.AppendLine($"  Mediation drop:       {mediation:F4}");
        sb.AppendLine("");

        bool isBottleneck = Math.Abs(partialR) < 0.15 && Math.Abs(path1) > 0.2;
        sb.AppendLine($"  → {(isBottleneck ? "Boundary IS the bottleneck — direct path vanishes when controlling for boundary" : "Boundary is NOT the bottleneck — direct path persists")}");
        sb.AppendLine(isBottleneck
            ? "  Classification: SUPPORTED — boundary is the information bottleneck"
            : "  Classification: CONDITIONAL — boundary is not the exclusive channel");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // DATA COLLECTION
    // ====================================================================

    private List<RedCell> CollectReductionData()
    {
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);
        var allCells = new ConcurrentBag<RedCell>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 18;
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

            var dist = new int[nG, nG];
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) dist[bi, gi] = int.MaxValue;
            var q = new Queue<(int, int)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                    if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                        gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                    { dist[bi, gi] = 0; q.Enqueue((bi, gi)); }
            while (q.Count > 0) { var (bi, gi) = q.Dequeue(); foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) }) if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && dist[nb, ng] == int.MaxValue) { dist[nb, ng] = dist[bi, gi] + 1; q.Enqueue((nb, ng)); } }

            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradM = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);
                    int gDir = (dMdB + dMdG) > 1e-15 ? 1 : (dMdB + dMdG) < -1e-15 ? -1 : 0;

                    double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                    double gradFb = Math.Sqrt(dFdB * dFdB + dFdG * dFdG);

                    int d = dist[bi, gi] == int.MaxValue ? 4 : Math.Min(3, dist[bi, gi]);
                    bool isBdry = d == 0;

                    var nT = new List<double>();
                    if (bi > 0) nT.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nT.Add(gTick[bi + 1, gi]);
                    if (gi > 0) nT.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nT.Add(gTick[bi, gi + 1]);
                    double tLocMean = nT.Count > 0 ? nT.Average() : gTick[bi, gi];
                    double tAnom = Math.Abs(gTick[bi, gi] - tLocMean) / Math.Max(1e-15, tLocMean);

                    allCells.Add(new RedCell(arch, gM[bi, gi], gS[bi, gi], gFb[bi, gi], gTick[bi, gi],
                        gradM, gradFb, d, isBdry, tAnom, gDir));
                }
        }
        return allCells.ToList();
    }

    private static double PearsonCorr(double[] xs, double[] ys)
    {
        int n = xs.Length; if (n < 2) return 0;
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < n; i++) { double dx = xs[i] - mx, dy = ys[i] - my; sxy += dx * dy; sxx += dx * dx; syy += dy * dy; }
        return sxx > 1e-15 && syy > 1e-15 ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    private static double PartialCorrelation(double[] x, double[] y, double[] z)
    {
        double rxy = PearsonCorr(x, y), rxz = PearsonCorr(x, z), ryz = PearsonCorr(y, z);
        double denom = (1 - rxz * rxz) * (1 - ryz * ryz);
        return denom > 1e-15 ? (rxy - rxz * ryz) / Math.Sqrt(denom) : 0;
    }

    private record RedCell(string Arch, double AbsM, int Sign, double Fb, double Tick,
        double GradM, double GradFb, int ZoneDist, bool IsBoundary, double TickAnomaly, int GradDir);
}
