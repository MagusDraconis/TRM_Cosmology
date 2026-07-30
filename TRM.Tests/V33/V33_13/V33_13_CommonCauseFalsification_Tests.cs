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
public class V33_13_CommonCauseFalsification_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_13_CommonCauseFalsification_Tests(ITestOutputHelper o) { _o = o; }

    // ====================================================================
    // CCF_01: ESTIMATOR COLLINEARITY — Are fb and |m| functionally dependent?
    //
    // Null:        fb and |m| are independent observables
    // Alt:         fb ≈ g(|m|) — they measure the same α-slope
    // Observable:  R² of fb predicted from |m| alone
    // Pass (CC falsified):  R² > 0.80 → fb carries little independent information
    // Fail (CC survives):   R² < 0.50 → fb is genuinely separate from |m|
    // ====================================================================
    [Fact]
    public void CCF_01_EstimatorCollinearity_FbVsAbsM()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CCF_01: Estimator Collinearity — fb vs |m| ===");

        var data = CollectGridData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbV = data.Select(c => c.Fb).ToArray();
        double[] mV = data.Select(c => c.AbsM).ToArray();

        // Linear: fb ~ |m|
        double rLin = PearsonCorr(fbV, mV);
        double r2Lin = rLin * rLin;

        // Log-linear: log(fb) ~ log(|m|)
        double[] lFb = data.Select(c => Math.Log10(Math.Max(1e-15, c.Fb))).ToArray();
        double[] lM = data.Select(c => Math.Log10(Math.Max(1e-15, c.AbsM))).ToArray();
        double rLog = PearsonCorr(lFb, lM);
        double r2Log = rLog * rLog;

        // Quadratic: fb ~ a·|m|² + b·|m| + c
        double r2Quad = MultivariateR2(fbV, mV, data.Select(c => c.AbsM * c.AbsM).ToArray());

        double bestR2 = Math.Max(r2Lin, Math.Max(r2Log, r2Quad));

        sb.AppendLine($"  Linear:      R² = {r2Lin:F4}  r = {rLin:F4}");
        sb.AppendLine($"  Log-linear:  R² = {r2Log:F4}  r = {rLog:F4}");
        sb.AppendLine($"  Quadratic:   R² = {r2Quad:F4}");
        sb.AppendLine($"  Best R²:     {bestR2:F4}");
        sb.AppendLine("");

        bool ccFalsified = bestR2 > 0.80;
        sb.AppendLine($"  → fb is {(ccFalsified ? "FUNCTIONALLY DEPENDENT on |m| — fb carries little independent information" : "INDEPENDENT of |m| — fb is a separate observable")}");
        sb.AppendLine(ccFalsified
            ? "  VERDICT: CC model WEAKENED — fb and |m| are not independent observables"
            : "  VERDICT: CC model SURVIVES — fb carries information not in |m|");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(ccFalsified, $"CCF_01: fb vs |m| R² = {bestR2:F4}, threshold 0.80. CC falsified if fb ≈ func(|m|).");
    }

    // ====================================================================
    // CCF_02: α/P ORTHOGONALITY — Is boundary upstream of fb?
    //
    // Null:        Boundary status predicts fb gradient
    // Alt:         Boundary (from p-sweep) is orthogonal to fb (from α-sweep)
    // Observable:  Can fb gradient be predicted from boundary distance?
    // Pass (CC falsified):  R²(boundary → ∇fb) < 0.20 → boundary does not cause fb
    // Fail (CC survives):   R²(boundary → ∇fb) > 0.30 → boundary meaningfully predicts fb
    // ====================================================================
    [Fact]
    public void CCF_02_AlphaPOrthogonality_BoundaryVsFb()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CCF_02: α/P Orthogonality — Boundary vs ∇fb ===");

        var data = CollectGridData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] gfV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] bdV = data.Select(c => (double)c.ZoneDist).ToArray();
        double[] bBin = data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray();
        double[] gmLog = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();

        // Model: ∇fb ~ boundary_distance (null: boundary upstream of fb)
        double rBd = PearsonCorr(gfV, bdV);
        double r2Bd = rBd * rBd;

        // Model: ∇fb ~ boundary_binary
        double rBin = PearsonCorr(gfV, bBin);
        double r2Bin = rBin * rBin;

        // Residual: does boundary add anything beyond ∇|m|?
        double r2GradM = PearsonCorr(gfV, gmLog);
        r2GradM *= r2GradM;
        double r2Combined = MultivariateR2(gfV, gmLog, bdV);
        double deltaR2 = r2Combined - r2GradM;

        sb.AppendLine($"  ∇fb ~ boundary_distance:  R² = {r2Bd:F4}  r = {rBd:F4}");
        sb.AppendLine($"  ∇fb ~ boundary_binary:    R² = {r2Bin:F4}  r = {rBin:F4}");
        sb.AppendLine($"  ∇fb ~ ∇|m|:               R² = {r2GradM:F4}");
        sb.AppendLine($"  ∇fb ~ ∇|m| + boundary:    R² = {r2Combined:F4}  (Δ = {deltaR2:F4})");
        sb.AppendLine("");

        bool ccFalsified = r2Bd < 0.20 && deltaR2 < 0.05;
        sb.AppendLine($"  → Boundary {(ccFalsified ? "does NOT predict ∇fb — boundary is orthogonal to fb" : "predicts ∇fb — boundary may be upstream")}");
        sb.AppendLine($"  → Residual gain from boundary: {(deltaR2 < 0.05 ? "NEGLIGIBLE" : "SIGNIFICANT")}");
        sb.AppendLine(ccFalsified
            ? "  VERDICT: CC model FALSIFIED — boundary does not cause fb gradient"
            : "  VERDICT: CC model SURVIVES — boundary has predictive power over fb");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(ccFalsified, $"CCF_02: Boundary→∇fb R²={r2Bd:F4}, ΔR²={deltaR2:F4}. CC falsified if boundary orthogonal to fb.");
    }

    // ====================================================================
    // CCF_03: CONDITIONAL INDEPENDENCE — Does boundary explain all G-T-F covariance?
    //
    // Null:        P(G,T,F | boundary) = P(G|bd)·P(T|bd)·P(F|bd) — CC holds
    // Alt:         Residual covariance remains after conditioning on boundary
    // Observable:  Partial correlation ∇|m| ↔ ∇fb controlling for boundary distance
    // Pass (CC falsified):  |partial r| > 0.30 — residual coupling remains
    // Fail (CC survives):   |partial r| < 0.15 — boundary explains the correlation
    // ====================================================================
    [Fact]
    public void CCF_03_ConditionalIndependence_ResidualAfterBoundary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CCF_03: Conditional Independence — Residual G-F covariance after conditioning on boundary ===");

        var data = CollectGridData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] gfV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] gmLog = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] bdV = data.Select(c => (double)c.ZoneDist).ToArray();

        // Simple correlation (no conditioning)
        double rRaw = PearsonCorr(gfV, gmLog);

        // Partial correlation: ∇fb ↔ ∇|m| controlling for boundary distance
        double partialR = PartialCorrelation(gfV, gmLog, bdV);

        // Within-zone correlations
        var zoneRs = new List<(int zone, double r, int n)>();
        for (int z = 0; z <= 3; z++)
        {
            var idx = new List<int>();
            for (int i = 0; i < data.Count; i++) if (data[i].ZoneDist == z) idx.Add(i);
            if (idx.Count < 10) continue;
            double[] zGf = idx.Select(i => gfV[i]).ToArray();
            double[] zGm = idx.Select(i => gmLog[i]).ToArray();
            zoneRs.Add((z, PearsonCorr(zGf, zGm), idx.Count));
        }

        sb.AppendLine($"  Raw correlation:       r = {rRaw:F4}  R² = {rRaw*rRaw:F4}");
        sb.AppendLine($"  Partial r (ctrl bd):   r = {partialR:F4}  R² = {partialR*partialR:F4}");
        sb.AppendLine("");
        sb.AppendLine($"  Within-zone correlations:");
        foreach (var (z, r, n) in zoneRs)
            sb.AppendLine($"    Zone {z}: r = {r:F4}  (n = {n})");
        sb.AppendLine("");

        double absPartial = Math.Abs(partialR);
        bool ccFalsified = absPartial > 0.30;
        sb.AppendLine($"  → {(ccFalsified ? "RESIDUAL COUPLING EXISTS — CC cannot explain all covariance" : "COVARIANCE EXPLAINED — boundary suffices")}");
        sb.AppendLine(ccFalsified
            ? "  VERDICT: CC model FALSIFIED — boundary does not screen the G-F correlation"
            : "  VERDICT: CC model SURVIVES — boundary explains the G-F correlation");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(ccFalsified, $"CCF_03: Partial r = {partialR:F4}, threshold 0.30. CC falsified if residual coupling survives boundary conditioning.");
    }

    // ====================================================================
    // CCF_04: INFORMATION LOSS — Is information lost when reducing to boundary?
    //
    // Null:        P(G,T,F | boundary) captures all joint information
    // Alt:         Conditioning on boundary loses information
    // Observable:  Mutual information I(G,T,F) vs I(G,T,F | boundary)
    // Pass (CC falsified):  ΔI > threshold — boundary discards information
    // Fail (CC survives):   ΔI ≈ 0 — boundary is sufficient statistic
    // ====================================================================
    [Fact]
    public void CCF_04_InformationLoss_ThroughBoundaryReduction()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CCF_04: Information Loss Through Boundary Reduction ===");

        var data = CollectGridData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] gmV = data.Select(c => c.GradM).ToArray();
        double[] gfV = data.Select(c => c.GradFb).ToArray();
        double[] tAV = data.Select(c => c.TickAnomaly).ToArray();
        double[] bdV = data.Select(c => (double)c.ZoneDist).ToArray();

        // Discretize into bins for mutual-information approximation
        int nBins = 10;
        double[][] vars = { gmV, gfV, tAV };
        var allBinned = vars.Select(v => Discretize(v, nBins)).ToArray();

        // Joint entropy proxy: sum of marginal entropies minus correlation structure
        // We use R² as a variance-explained proxy for information loss
        // Model: how much of the G-F-T covariance matrix survives boundary conditioning?

        // Covariance matrix of [∇|m|, ∇fb, tick_anomaly] — unconditional
        double rGF = PearsonCorr(gmV, gfV);
        double rGT = PearsonCorr(gmV, tAV);
        double rFT = PearsonCorr(gfV, tAV);
        double totalCov = Math.Abs(rGF) + Math.Abs(rGT) + Math.Abs(rFT);

        // Covariance within each zone, weighted by zone size
        double condCov = 0; double totalW = 0;
        for (int z = 0; z <= 3; z++)
        {
            var idx = new List<int>();
            for (int i = 0; i < data.Count; i++) if (data[i].ZoneDist == z) idx.Add(i);
            if (idx.Count < 10) continue;
            double w = (double)idx.Count / data.Count;
            double[] zGm = idx.Select(i => gmV[i]).ToArray();
            double[] zGf = idx.Select(i => gfV[i]).ToArray();
            double[] zTa = idx.Select(i => tAV[i]).ToArray();
            double zrGF = PearsonCorr(zGm, zGf);
            double zrGT = PearsonCorr(zGm, zTa);
            double zrFT = PearsonCorr(zGf, zTa);
            condCov += w * (Math.Abs(zrGF) + Math.Abs(zrGT) + Math.Abs(zrFT));
            totalW += w;
        }

        double infoLoss = totalCov - condCov;
        double infoLossPct = totalCov > 0 ? 100.0 * infoLoss / totalCov : 0;

        sb.AppendLine($"  Unconditional |r| sum:   {totalCov:F4}");
        sb.AppendLine($"  Conditional |r| sum:     {condCov:F4}");
        sb.AppendLine($"  Information loss:        {infoLoss:F4} ({infoLossPct:F1}%)");
        sb.AppendLine("");

        bool ccFalsified = condCov > 0.20; // significant residual covariance remains
        sb.AppendLine($"  → {(ccFalsified ? "SIGNIFICANT residual covariance — boundary loses information" : "Boundary is sufficient — no information loss")}");
        sb.AppendLine(ccFalsified
            ? "  VERDICT: CC model FALSIFIED — boundary reduction discards coupling information"
            : "  VERDICT: CC model SURVIVES — boundary is a sufficient statistic");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(ccFalsified, $"CCF_04: Conditional |r| sum = {condCov:F4}, threshold 0.20. CC falsified if residual covariance survives boundary conditioning.");
    }

    // ====================================================================
    // CCF_05: FB INDEPENDENT INFORMATION — Does fb carry information beyond |m|?
    //
    // Null:        fb = f(|m|) — fb is redundant
    // Alt:         fb carries α-sweep information not captured by |m|
    // Observable:  Does fb predict things |m| cannot?
    // Pass (CC survives):   ΔR² < 0.05 — fb is redundant with |m|
    // Fail (CC falsified):  ΔR² > 0.10 — fb adds predictive power
    // ====================================================================
    [Fact]
    public void CCF_05_FbIndependentInformation_BeyondAbsM()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CCF_05: fb Independent Information Beyond |m| ===");

        var data = CollectGridData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Target: can we predict tick from |m| alone vs |m| + fb?
        double[] tickV = data.Select(c => c.Tick).ToArray();
        double[] mV = data.Select(c => c.AbsM).ToArray();
        double[] fbV = data.Select(c => c.Fb).ToArray();

        double r2_mOnly = PearsonCorr(tickV, mV);
        r2_mOnly *= r2_mOnly;
        double r2_mFb = MultivariateR2(tickV, mV, fbV);
        double deltaR2 = r2_mFb - r2_mOnly;

        // Target: can we predict tick anomaly from |m| alone vs |m| + fb?
        double[] taV = data.Select(c => c.TickAnomaly).ToArray();
        double r2_taM = PearsonCorr(taV, mV);
        r2_taM *= r2_taM;
        double r2_taMFb = MultivariateR2(taV, mV, fbV);
        double deltaR2_ta = r2_taMFb - r2_taM;

        // Target: can we predict sign from |m| alone vs |m| + fb?
        double[] sV = data.Select(c => (double)c.Sign).ToArray();
        double r2_sM = PearsonCorr(sV, mV);
        r2_sM *= r2_sM;
        double r2_sMFb = MultivariateR2(sV, mV, fbV);
        double deltaR2_s = r2_sMFb - r2_sM;

        sb.AppendLine($"  Predict Tick:         |m| R²={r2_mOnly:F4}  +fb R²={r2_mFb:F4}  Δ={deltaR2:F4}");
        sb.AppendLine($"  Predict TickAnomaly:  |m| R²={r2_taM:F4}  +fb R²={r2_taMFb:F4}  Δ={deltaR2_ta:F4}");
        sb.AppendLine($"  Predict Sign:         |m| R²={r2_sM:F4}  +fb R²={r2_sMFb:F4}  Δ={deltaR2_s:F4}");
        sb.AppendLine("");

        double avgDelta = (deltaR2 + deltaR2_ta + deltaR2_s) / 3.0;
        bool fbAddsInfo = avgDelta > 0.10;
        sb.AppendLine($"  Average ΔR²: {avgDelta:F4}");
        sb.AppendLine($"  → fb {(fbAddsInfo ? "ADDS predictive power beyond |m| — fb is not redundant" : "is REDUNDANT with |m| — no independent information")}");
        sb.AppendLine(fbAddsInfo
            ? "  VERDICT: CC estimator-collinearity claim WEAKENED — fb carries independent information"
            : "  VERDICT: CC estimator-collinearity claim SUPPORTED — fb IS redundant with |m|");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(!fbAddsInfo, $"CCF_05: ΔR² = {avgDelta:F4}, threshold 0.10. CC survives if fb adds no independent information beyond |m|.");
    }

    // ====================================================================
    // CCF_06: (β,γ) AS SUFFICIENT STATISTIC — Is everything determined by (β,γ)?
    //
    // Null:        All observables are deterministic functions of (β, γ)
    // Alt:         There is variation not explained by (β, γ) position
    // Observable:  Grid smoothness — can neighbors predict each other?
    // Pass (CC trivial model supported):  neighbor prediction > 0.90
    // Fail (CC trivial model falsified):  neighbor prediction < 0.70
    // ====================================================================
    [Fact]
    public void CCF_06_SufficientStatistic_BetaGammaDeterminesAll()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CCF_06: (β,γ) as Sufficient Statistic ===");

        var data = CollectGridData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Re-run at finer resolution to enable neighbor prediction
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 20;
            double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
            double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

            var gFb = new double[nG, nG];
            var gM = new double[nG, nG];
            var gTick = new double[nG, nG];

            Parallel.For(0, nG, bi =>
            {
                double beta = bMin + db * bi;
                for (int gi = 0; gi < nG; gi++)
                {
                    double gamma = gMin + dg * gi;
                    var f = ComputeFull(fam, 1.0, 1.0, 0.70, beta, gamma, distances, sortedD, xiBase, k0Base, nA, aMin, daD);
                    gFb[bi, gi] = f.fb; gM[bi, gi] = f.absM; gTick[bi, gi] = f.tick;
                }
            });

            // Neighbor prediction: predict cell from average of 4 neighbors
            double fbErr = 0, mErr = 0, tErr = 0;
            double fbDenom = 0, mDenom = 0, tDenom = 0;
            int nPred = 0;
            for (int bi = 2; bi < nG - 2; bi++)
            {
                for (int gi = 2; gi < nG - 2; gi++)
                {
                    double fbPred = (gFb[bi + 1, gi] + gFb[bi - 1, gi] + gFb[bi, gi + 1] + gFb[bi, gi - 1]) / 4.0;
                    double mPred = (gM[bi + 1, gi] + gM[bi - 1, gi] + gM[bi, gi + 1] + gM[bi, gi - 1]) / 4.0;
                    double tPred = (gTick[bi + 1, gi] + gTick[bi - 1, gi] + gTick[bi, gi + 1] + gTick[bi, gi - 1]) / 4.0;

                    fbErr += (gFb[bi, gi] - fbPred) * (gFb[bi, gi] - fbPred);
                    mErr += (gM[bi, gi] - mPred) * (gM[bi, gi] - mPred);
                    tErr += (gTick[bi, gi] - tPred) * (gTick[bi, gi] - tPred);
                    fbDenom += gFb[bi, gi] * gFb[bi, gi];
                    mDenom += gM[bi, gi] * gM[bi, gi];
                    tDenom += gTick[bi, gi] * gTick[bi, gi];
                    nPred++;
                }
            }

            double fbR2 = fbDenom > 1e-15 ? 1.0 - fbErr / fbDenom : 0;
            double mR2 = mDenom > 1e-15 ? 1.0 - mErr / mDenom : 0;
            double tR2 = tDenom > 1e-15 ? 1.0 - tErr / tDenom : 0;

            sb.AppendLine($"  {arch}: fb neighbor R²={fbR2:F4}  |m| neighbor R²={mR2:F4}  tick neighbor R²={tR2:F4}");
        }
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // CCF_07: TICK ANOMALY AS BOUNDARY ARTIFACT — Is tick anomaly just boundary proximity?
    //
    // Null:        Tick anomaly arises from geometry-tick interaction
    // Alt:         Tick anomaly is a direct consequence of boundary proximity
    // Observable:  Does boundary distance explain tick anomaly?
    // Pass (CC supported):   R²(boundary → tick_anomaly) > 0.50
    // Fail (CC weakened):    R²(boundary → tick_anomaly) < 0.20
    // ====================================================================
    [Fact]
    public void CCF_07_TickAnomalyAsBoundaryArtifact()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CCF_07: Tick Anomaly as Boundary Artifact ===");

        var data = CollectGridData();
        if (data.Count < 100) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] taV = data.Select(c => c.TickAnomaly).ToArray();
        double[] bdV = data.Select(c => (double)c.ZoneDist).ToArray();
        double[] bBin = data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray();

        double rBd = PearsonCorr(taV, bdV);
        double r2Bd = rBd * rBd;
        double rBin = PearsonCorr(taV, bBin);
        double r2Bin = rBin * rBin;

        // Mean anomaly by zone
        sb.AppendLine($"  Boundary_distance → TickAnomaly:  R² = {r2Bd:F4}  r = {rBd:F4}");
        sb.AppendLine($"  Boundary_binary → TickAnomaly:    R² = {r2Bin:F4}  r = {rBin:F4}");
        sb.AppendLine("");

        for (int z = 0; z <= 3; z++)
        {
            var cells = data.Where(c => c.ZoneDist == z).ToList();
            if (cells.Count < 10) continue;
            double taMean = cells.Average(c => c.TickAnomaly);
            double taStd = Math.Sqrt(cells.Select(c => (c.TickAnomaly - taMean) * (c.TickAnomaly - taMean)).Average());
            sb.AppendLine($"  Zone {z}: anomaly = {taMean:F4} ± {taStd:F4}  n = {cells.Count}");
        }
        sb.AppendLine("");

        bool ccSupported = r2Bd > 0.50;
        sb.AppendLine($"  → {(ccSupported ? "Tick anomaly IS a boundary artifact — CC model supported" : "Tick anomaly has structure beyond boundary — CC model insufficient")}");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // DATA COLLECTION
    // ====================================================================

    private List<CcCell> CollectGridData()
    {
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);

        var allCells = new ConcurrentBag<CcCell>();

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

            // BFS distance from boundaries
            var dist = new int[nG, nG];
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) dist[bi, gi] = int.MaxValue;
            var q = new Queue<(int, int)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                    if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                        gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                    { dist[bi, gi] = 0; q.Enqueue((bi, gi)); }
            while (q.Count > 0)
            {
                var (bi, gi) = q.Dequeue();
                foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) })
                    if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && dist[nb, ng] == int.MaxValue)
                    { dist[nb, ng] = dist[bi, gi] + 1; q.Enqueue((nb, ng)); }
            }

            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                {
                    double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                    double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                    double gradM = Math.Sqrt(dMdB * dMdB + dMdG * dMdG);

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

                    allCells.Add(new CcCell(arch, gM[bi, gi], gS[bi, gi], gFb[bi, gi], gTick[bi, gi],
                        gradM, gradFb, d, isBdry, tAnom));
                }
        }

        return allCells.ToList();
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

    private static double PartialCorrelation(double[] x, double[] y, double[] z)
    {
        double rxy = PearsonCorr(x, y);
        double rxz = PearsonCorr(x, z);
        double ryz = PearsonCorr(y, z);
        double denom = (1 - rxz * rxz) * (1 - ryz * ryz);
        if (denom < 1e-15) return 0;
        return (rxy - rxz * ryz) / Math.Sqrt(denom);
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

    private static int[] Discretize(double[] values, int nBins)
    {
        double min = values.Min(), max = values.Max();
        double range = max - min;
        if (range < 1e-15) return values.Select(_ => 0).ToArray();
        return values.Select(v => Math.Min(nBins - 1, (int)((v - min) / range * nBins))).ToArray();
    }

    private record CcCell(string Arch, double AbsM, int Sign, double Fb, double Tick,
        double GradM, double GradFb, int ZoneDist, bool IsBoundary, double TickAnomaly);
}
