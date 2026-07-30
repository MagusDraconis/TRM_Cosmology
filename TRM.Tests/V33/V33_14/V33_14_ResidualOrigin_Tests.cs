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

namespace TRM.Tests.V33_14;

[Trait("Category", "V33_14")]
[Trait("Category", "LongRunning")]
public class V33_14_ResidualOrigin_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_14_ResidualOrigin_Tests(ITestOutputHelper o) { _o = o; }

    private record ResCell(
        string Arch,
        double AbsM, double Fb, double Tick, double V, int Sign,
        double GradM, double GradFb, double GradTick,
        double CurvM, double CurvFb,
        double TickAnomaly,
        int ZoneDist, bool IsBoundary, bool IsAdjacent,
        double Core, double FbResidual, double MResidual,
        double FbMDiff, double VDiv,
        double GradMRank, double CurvMRank, double FbResRank, double MResRank);

    private List<ResCell> CollectData()
    {
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);
        var all = new ConcurrentBag<ResCell>();

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            const int nG = 22;
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
            double wFb = evNorm > 1e-15 ? evFb / evNorm : 1, wM = evNorm > 1e-15 ? evM / evNorm : 0;

            double[] coreZ = zFb.Zip(zM, (f, m) => wFb * f + wM * m).ToArray();
            double coreMeanZ = coreZ.Average();
            double cVar = 0, cCovF = 0, cCovM = 0;
            for (int i = 0; i < n; i++) { double dc = coreZ[i] - coreMeanZ; cVar += dc * dc; cCovF += dc * zFb[i]; cCovM += dc * zM[i]; }
            cVar /= (n - 1); cCovF /= (n - 1); cCovM /= (n - 1);
            double betaF = cVar > 1e-15 ? cCovF / cVar : 0, betaM = cVar > 1e-15 ? cCovM / cVar : 0;

            var coreGrid = new double[nG, nG]; var fbResGrid = new double[nG, nG]; var mResGrid = new double[nG, nG];
            int idx = 0;
            for (int bi = 0; bi < nG; bi++)
                for (int gi = 0; gi < nG; gi++)
                { coreGrid[bi, gi] = coreZ[idx]; fbResGrid[bi, gi] = zFb[idx] - betaF * coreZ[idx]; mResGrid[bi, gi] = zM[idx] - betaM * coreZ[idx]; idx++; }

            var distGrid = new int[nG, nG];
            for (int bi = 0; bi < nG; bi++) for (int gi = 0; gi < nG; gi++) distGrid[bi, gi] = int.MaxValue;
            var q = new Queue<(int, int)>();
            for (int bi = 1; bi < nG - 1; bi++)
                for (int gi = 1; gi < nG - 1; gi++)
                    if (gS[bi, gi] != gS[bi + 1, gi] || gS[bi, gi] != gS[bi - 1, gi] ||
                        gS[bi, gi] != gS[bi, gi + 1] || gS[bi, gi] != gS[bi, gi - 1])
                    { distGrid[bi, gi] = 0; q.Enqueue((bi, gi)); }
            while (q.Count > 0) { var (bi, gi) = q.Dequeue(); foreach (var (nb, ng) in new[] { (bi + 1, gi), (bi - 1, gi), (bi, gi + 1), (bi, gi - 1) }) if (nb >= 0 && nb < nG && ng >= 0 && ng < nG && distGrid[nb, ng] == int.MaxValue) { distGrid[nb, ng] = distGrid[bi, gi] + 1; q.Enqueue((nb, ng)); } }

            var raw = new List<(ResCell c, double gm, double cm, double abfr, double abmr)>();
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
                    int d = distGrid[bi, gi] == int.MaxValue ? 4 : Math.Min(3, distGrid[bi, gi]);
                    var nT = new List<double>();
                    if (bi > 0) nT.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nT.Add(gTick[bi + 1, gi]);
                    if (gi > 0) nT.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nT.Add(gTick[bi, gi + 1]);
                    double tLocMean = nT.Count > 0 ? nT.Average() : gTick[bi, gi];
                    double tAnom = Math.Abs(gTick[bi, gi] - tLocMean) / Math.Max(1e-15, tLocMean);

                    raw.Add((new ResCell(arch, gM[bi, gi], gFb[bi, gi], gTick[bi, gi], gV[bi, gi], gS[bi, gi],
                        gradM, gradFb, gradTick, lapM, lapFb, tAnom, d, d == 0, d == 1,
                        coreGrid[bi, gi], fbResGrid[bi, gi], mResGrid[bi, gi],
                        Math.Abs(gFb[bi, gi] - gM[bi, gi]), Math.Abs(gV[bi, gi] - 1.0),
                        0, 0, 0, 0), gradM, lapM, Math.Abs(fbResGrid[bi, gi]), Math.Abs(mResGrid[bi, gi])));
                }

            var allGM = raw.Select(r => r.gm).OrderBy(v => v).ToArray();
            var allCM = raw.Select(r => r.cm).OrderBy(v => v).ToArray();
            var allAFR = raw.Select(r => r.abfr).OrderBy(v => v).ToArray();
            var allAMR = raw.Select(r => r.abmr).OrderBy(v => v).ToArray();
            double Q(double[] s, double v) { int i = Array.BinarySearch(s, v); if (i < 0) i = ~i; return s.Length > 1 ? (double)i / (s.Length - 1) : 0.5; }

            foreach (var (c, gm, cm, abfr, abmr) in raw)
            {
                all.Add(new ResCell(c.Arch, c.AbsM, c.Fb, c.Tick, c.V, c.Sign, c.GradM, c.GradFb, c.GradTick,
                    c.CurvM, c.CurvFb, c.TickAnomaly, c.ZoneDist, c.IsBoundary, c.IsAdjacent,
                    c.Core, c.FbResidual, c.MResidual, c.FbMDiff, c.VDiv,
                    Q(allGM, gm), Q(allCM, cm), Q(allAFR, abfr), Q(allAMR, abmr)));
            }
        }
        return all.ToList();
    }

    // ====================================================================
    // RGO_01: BOUNDARY LOCALIZATION
    // ====================================================================
    [Fact]
    public void RGO_01_BoundaryLocalization_DoResidualsCollapseAwayFromBoundaries()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== RGO_01: Boundary Localization ===");

        var data = CollectData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        sb.AppendLine($"{"Zone",-12} {"N",5} {"|fb_res|",10} {"σ(fb_res)",10} {"|m_res|",10} {"σ(m_res)",10} {"|Core|",10} {"∇|m|",10}");
        sb.AppendLine(new string('-', 80));

        var zoneRes = new List<(int z, double fbAbs, double mAbs, double core, double gm)>();

        for (int z = 0; z <= 3; z++)
        {
            var cells = data.Where(c => c.ZoneDist == z).ToList();
            if (cells.Count < 15) continue;
            double fbAbs = cells.Average(c => Math.Abs(c.FbResidual));
            double fbStd = Math.Sqrt(cells.Select(c => c.FbResidual * c.FbResidual).Average());
            double mAbs = cells.Average(c => Math.Abs(c.MResidual));
            double mStd = Math.Sqrt(cells.Select(c => c.MResidual * c.MResidual).Average());
            double core = cells.Average(c => Math.Abs(c.Core));
            double gm = cells.Average(c => c.GradM);
            zoneRes.Add((z, fbAbs, mAbs, core, gm));
            string zn = z == 0 ? "Boundary" : z == 1 ? "Adjacent" : z == 2 ? "Near-2" : "Interior";
            sb.AppendLine($"{zn,-12} {cells.Count,5} {fbAbs,10:F4} {fbStd,10:F4} {mAbs,10:F4} {mStd,10:F4} {core,10:F4} {gm,10:F4}");
        }
        sb.AppendLine("");

        if (zoneRes.Count >= 2)
        {
            var bdry = zoneRes.First(z => z.z == 0);
            var interior = zoneRes.First(z => z.z >= 2);
            double fbRatio = bdry.fbAbs / Math.Max(1e-15, interior.fbAbs);
            double mRatio = bdry.mAbs / Math.Max(1e-15, interior.mAbs);
            double coreRatio = bdry.core / Math.Max(1e-15, interior.core);

            sb.AppendLine($"  Bdry/Interior: Core={coreRatio:F2}x  |fb_res|={fbRatio:F2}x  |m_res|={mRatio:F2}x");
            sb.AppendLine("");

            bool residualsCollapse = fbRatio > coreRatio * 1.3 || mRatio > coreRatio * 1.3;
            bool residualsPersist = fbRatio < coreRatio * 0.7 || mRatio < coreRatio * 0.7;

            if (residualsCollapse)
                sb.AppendLine("  → Residuals COLLAPSE at boundaries — boundary artifacts");
            else if (residualsPersist)
                sb.AppendLine("  → Residuals PERSIST in interior — NOT boundary artifacts");
            else
                sb.AppendLine("  → Residuals track core concentration — neutral");
        }
        sb.AppendLine("");
        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // RGO_02: CURVATURE LOCALIZATION
    // ====================================================================
    [Fact]
    public void RGO_02_CurvatureLocalization_AreResidualsCurvatureLinked()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== RGO_02: Curvature Localization ===");

        var data = CollectData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        var byCurv = data.OrderBy(c => c.CurvM).ToList();
        int qN = byCurv.Count / 4;

        sb.AppendLine($"{"Curv Q",-10} {"N",5} {"|fb_res|",10} {"|m_res|",10} {"r(fb_res,∇²)",14} {"r(m_res,∇²)",14}");
        sb.AppendLine(new string('-', 66));

        for (int qi = 0; qi < 4; qi++)
        {
            var q = byCurv.Skip(qi * qN).Take(qi == 3 ? byCurv.Count - 3 * qN : qN).ToList();
            if (q.Count < 15) continue;
            double fab = q.Average(c => Math.Abs(c.FbResidual));
            double mab = q.Average(c => Math.Abs(c.MResidual));
            double[] lcm = q.Select(c => Math.Log10(Math.Max(1e-15, c.CurvM))).ToArray();
            double rFb = PearsonCorr(q.Select(c => c.FbResidual).ToArray(), lcm);
            double rM = PearsonCorr(q.Select(c => c.MResidual).ToArray(), lcm);
            sb.AppendLine($"{"Q" + (qi + 1),-10} {q.Count,5} {fab,10:F4} {mab,10:F4} {rFb,14:F4} {rM,14:F4}");
        }
        sb.AppendLine("");

        double[] allLcm = data.Select(c => Math.Log10(Math.Max(1e-15, c.CurvM))).ToArray();
        double[] allAFR = data.Select(c => Math.Abs(c.FbResidual)).ToArray();
        double[] allAMR = data.Select(c => Math.Abs(c.MResidual)).ToArray();
        double[] allAC = data.Select(c => Math.Abs(c.Core)).ToArray();

        double r_fb_curv = PearsonCorr(allAFR, allLcm);
        double r_m_curv = PearsonCorr(allAMR, allLcm);
        double r_core_curv = PearsonCorr(allAC, allLcm);

        sb.AppendLine($"  Global: |Core|↔∇² r={r_core_curv:F4}  |fb_res|↔∇² r={r_fb_curv:F4}  |m_res|↔∇² r={r_m_curv:F4}");
        sb.AppendLine("");

        bool residualsCurvLinked = Math.Abs(r_fb_curv) > 0.20 || Math.Abs(r_m_curv) > 0.20;
        sb.AppendLine(residualsCurvLinked ? "  → Residuals ARE curvature-linked" : "  → Residuals curvature-independent");
        sb.AppendLine("");
        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // RGO_03: TOPOLOGICAL EVENT AUDIT
    // ====================================================================
    [Fact]
    public void RGO_03_TopologicalEventAudit_DoResidualsSpikeAtTopologyEvents()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== RGO_03: Topological Event Audit ===");

        var data = CollectData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        var bdry = data.Where(c => c.IsBoundary).ToList();
        var interior = data.Where(c => c.ZoneDist >= 2).ToList();

        double curv75 = data.Select(c => c.CurvM).OrderBy(v => v).ElementAt(data.Count * 3 / 4);
        var bifurcations = bdry.Where(c => c.CurvM >= curv75).ToList();

        string Rpt(string name, List<ResCell> cells) =>
            cells.Count < 10 ? $"{name,-22}: insufficient" :
            $"{name,-22} n={cells.Count,5} |fb_res|={cells.Average(c => Math.Abs(c.FbResidual)):F4} |m_res|={cells.Average(c => Math.Abs(c.MResidual)):F4}";

        sb.AppendLine(Rpt("All", data));
        sb.AppendLine(Rpt("Boundary", bdry));
        sb.AppendLine(Rpt("Interior", interior));
        sb.AppendLine(Rpt("Bifurcations", bifurcations));
        sb.AppendLine("");

        double intFb = interior.Average(c => Math.Abs(c.FbResidual));
        double bdrySpike = bdry.Average(c => Math.Abs(c.FbResidual)) / Math.Max(1e-15, intFb);
        double bifSpike = bifurcations.Count >= 10 ? bifurcations.Average(c => Math.Abs(c.FbResidual)) / Math.Max(1e-15, intFb) : 1;

        sb.AppendLine($"  Spike ratios vs interior: Boundary={bdrySpike:F2}x  Bifurcations={bifSpike:F2}x");
        sb.AppendLine(bdrySpike > 1.5 || bifSpike > 1.5 ? "  → Residuals SPIKE at topology events" : "  → Residuals topology-stable");
        sb.AppendLine("");
        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // RGO_04: ALPHA-P COUPLING
    // ====================================================================
    [Fact]
    public void RGO_04_AlphaPCoupling_DoResidualsEncodeCoupling()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== RGO_04: Alpha-P Coupling ===");

        var data = CollectData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] afr = data.Select(c => Math.Abs(c.FbResidual)).ToArray();
        double[] amr = data.Select(c => Math.Abs(c.MResidual)).ToArray();
        double[] coreAbs = data.Select(c => Math.Abs(c.Core)).ToArray();
        double[] fmd = data.Select(c => c.FbMDiff).ToArray();
        double[] bBin = data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray();

        double r_fr_fmd = PearsonCorr(afr, fmd);
        double r_fr_bd = PearsonCorr(afr, bBin);
        double r_fr_core = PearsonCorr(afr, coreAbs);

        double r2_fr_core = r_fr_core * r_fr_core;
        double r2_fr_joint = MultivariateR2(afr, coreAbs, fmd);
        double deltaAlphaP = r2_fr_joint - r2_fr_core;

        sb.AppendLine($"  |fb_res| ↔ |fb-|m||:  r = {r_fr_fmd:F4}");
        sb.AppendLine($"  |fb_res| ↔ boundary:  r = {r_fr_bd:F4}");
        sb.AppendLine($"  |fb_res| ↔ |Core|:    r = {r_fr_core:F4}");
        sb.AppendLine($"  α-p coupling ΔR²:     {deltaAlphaP:F4}");
        sb.AppendLine("");

        bool encodes = deltaAlphaP > 0.05 || (Math.Abs(r_fr_fmd) > 0.25 && Math.Abs(r_fr_fmd) > Math.Abs(r_fr_core));
        sb.AppendLine(encodes ? "  → Residuals ENCODE α-p coupling" : "  → Residuals do NOT encode α-p coupling");
        sb.AppendLine("");
        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // RGO_05: INFORMATION DECOMPOSITION
    // ====================================================================
    [Fact]
    public void RGO_05_InformationDecomposition_CompleteModelComparison()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== RGO_05: Information Decomposition ===");

        var data = CollectData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] core = data.Select(c => c.Core).ToArray();
        double[] fbRes = data.Select(c => c.FbResidual).ToArray();
        double[] mRes = data.Select(c => c.MResidual).ToArray();

        var targets = new (string name, double[] values)[]
        {
            ("boundary",     data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray()),
            ("bd_distance",  data.Select(c => (double)c.ZoneDist).ToArray()),
            ("tick_anomaly", data.Select(c => c.TickAnomaly).ToArray()),
            ("tick",         data.Select(c => c.Tick).ToArray()),
            ("log_∇|m|",     data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray()),
            ("log_∇fb",      data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray()),
            ("log_∇Tick",    data.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray()),
            ("log_∇²|m|",    data.Select(c => Math.Log10(Math.Max(1e-15, c.CurvM))).ToArray()),
            ("sign",         data.Select(c => (double)c.Sign).ToArray()),
        };

        sb.AppendLine($"{"Target",-16} {"R²(Core)",10} {"+fb_res Δ",10} {"+m_res Δ",10} {"+both Δ",10} {"Best",10} {"Sig",6}");
        sb.AppendLine(new string('-', 74));

        int sigCount = 0;
        foreach (var (tname, tval) in targets)
        {
            double r2_c = PearsonCorr(tval, core); r2_c *= r2_c;
            double r2_cfr = MultivariateR2(tval, core, fbRes);
            double r2_cmr = MultivariateR2(tval, core, mRes);
            double r2_all = MultivariateR3(tval, core, fbRes, mRes);
            double dFb = r2_cfr - r2_c, dM = r2_cmr - r2_c, dBoth = r2_all - r2_c;
            double best = Math.Max(dFb, Math.Max(dM, dBoth));
            bool sig = best > 0.03;
            if (sig) sigCount++;
            string sigLabel = sig ? "YES" : "no";
            sb.AppendLine($"{tname,-16} {r2_c,10:F4} {dFb,10:F4} {dM,10:F4} {dBoth,10:F4} {best,10:F4} {sigLabel,6}");
        }
        sb.AppendLine("");
        sb.AppendLine($"  Residuals add info on {sigCount}/{targets.Length} targets");
        sb.AppendLine(sigCount >= 2 ? "  → GENUINE INFORMATION CHANNELS" : "  → NOISE-DOMINATED");
        sb.AppendLine("");
        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // RGO_06: BOTTLENECK ANALYSIS
    // ====================================================================
    [Fact]
    public void RGO_06_BottleneckAnalysis_WhereDoResidualsEmerge()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== RGO_06: Bottleneck Analysis ===");

        var data = CollectData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] core = data.Select(c => c.Core).ToArray();
        double[] fbRes = data.Select(c => c.FbResidual).ToArray();
        double[] mRes = data.Select(c => c.MResidual).ToArray();

        var layers = new (string name, double[] values)[]
        {
            ("OscMismatch",  data.Select(c => c.FbMDiff).ToArray()),
            ("fb",           data.Select(c => c.Fb).ToArray()),
            ("|m|",          data.Select(c => c.AbsM).ToArray()),
            ("∇fb",          data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray()),
            ("∇|m|",         data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray()),
            ("TickAnomaly",  data.Select(c => c.TickAnomaly).ToArray()),
            ("Boundary",     data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray()),
            ("Curvature",    data.Select(c => Math.Log10(Math.Max(1e-15, c.CurvM))).ToArray()),
        };

        sb.AppendLine($"{"Layer",-14} {"R²(Core)",10} {"Δ(fb_res)",10} {"Δ(m_res)",10} {"Δ(both)",10} {"Emerged",8}");
        sb.AppendLine(new string('-', 64));

        int emerged = 0;
        string first = "none";
        foreach (var (lname, lval) in layers)
        {
            double r2_c = PearsonCorr(lval, core); r2_c *= r2_c;
            double r2_cfr = MultivariateR2(lval, core, fbRes);
            double r2_cmr = MultivariateR2(lval, core, mRes);
            double r2_all = MultivariateR3(lval, core, fbRes, mRes);
            double dFb = r2_cfr - r2_c, dM = r2_cmr - r2_c, dBoth = r2_all - r2_c;
            bool em = dFb > 0.03 || dM > 0.03;
            if (em) { emerged++; if (first == "none") first = lname; }
            string emLabel = em ? "YES" : "no";
            sb.AppendLine($"{lname,-14} {r2_c,10:F4} {dFb,10:F4} {dM,10:F4} {dBoth,10:F4} {emLabel,8}");
        }
        sb.AppendLine("");
        sb.AppendLine($"  Emergence: {emerged}/{layers.Length} layers, first at: {first}");
        sb.AppendLine(emerged >= 3 ? "  → PERVASIVE" : emerged >= 1 ? $"  → At layer: {first}" : "  → SILENT");
        sb.AppendLine("");
        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // RGO_07: SYMMETRY AUDIT
    // ====================================================================
    [Fact]
    public void RGO_07_SymmetryAudit_AreFbResAndMResSymmetric()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== RGO_07: Symmetry Audit ===");

        var data = CollectData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] fbRes = data.Select(c => c.FbResidual).ToArray();
        double[] mRes = data.Select(c => c.MResidual).ToArray();
        double[] afr = data.Select(c => Math.Abs(c.FbResidual)).ToArray();
        double[] amr = data.Select(c => Math.Abs(c.MResidual)).ToArray();
        double[] signV = data.Select(c => (double)c.Sign).ToArray();
        double[] bdV = data.Select(c => (double)c.ZoneDist).ToArray();
        double[] lcm = data.Select(c => Math.Log10(Math.Max(1e-15, c.CurvM))).ToArray();

        double r_cross = PearsonCorr(fbRes, mRes);

        sb.AppendLine($"  Cross-correlation r(fb_res, m_res) = {r_cross:F4}");
        sb.AppendLine("");
        sb.AppendLine($"{"Metric",-20} {"fb_res",10} {"m_res",10} {"Δ",8} {"Sym",6}");
        sb.AppendLine(new string('-', 56));

        var tests = new (string name, double fVal, double mVal)[]
        {
            ("r(sign)", PearsonCorr(fbRes, signV), PearsonCorr(mRes, signV)),
            ("r(bd_dist)", PearsonCorr(afr, bdV), PearsonCorr(amr, bdV)),
            ("r(curvature)", PearsonCorr(afr, lcm), PearsonCorr(amr, lcm)),
        };

        int symCount = 0;
        foreach (var (name, fVal, mVal) in tests)
        {
            double delta = Math.Abs(fVal - mVal);
            bool sym = delta < 0.10;
            if (sym) symCount++;
            string symLabel = sym ? "YES" : "no";
            sb.AppendLine($"{name,-20} {fVal,10:F4} {mVal,10:F4} {delta,8:F4} {symLabel,6}");
        }
        sb.AppendLine("");
        sb.AppendLine(symCount >= 2 && Math.Abs(r_cross) < 0.30 ? $"  → SYMMETRIC ({symCount}/3) — single channel" : $"  → ASYMMETRIC ({symCount}/3) — two channels");
        sb.AppendLine("");
        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // RGO_08: ELIMINATION TEST
    // ====================================================================
    [Fact]
    public void RGO_08_EliminationTest_CanResidualsBeReduced()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== RGO_08: Elimination Test ===");

        var data = CollectData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] afr = data.Select(c => Math.Abs(c.FbResidual)).ToArray();
        double[] amr = data.Select(c => Math.Abs(c.MResidual)).ToArray();

        var candidates = new (string name, double[] values)[]
        {
            ("Boundary",          data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray()),
            ("Bd_distance",       data.Select(c => (double)c.ZoneDist).ToArray()),
            ("Curvature",         data.Select(c => Math.Log10(Math.Max(1e-15, c.CurvM))).ToArray()),
            ("Topology(adj)",     data.Select(c => c.IsAdjacent ? 1.0 : 0.0).ToArray()),
            ("|fb-|m||",          data.Select(c => c.FbMDiff).ToArray()),
            ("|V-1|",             data.Select(c => c.VDiv).ToArray()),
            ("∇|m|",              data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray()),
            ("∇fb",               data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray()),
        };

        string Cls(double r2) => r2 > 0.50 ? "SUPPORTED" : r2 > 0.25 ? "CONDITIONAL" : r2 > 0.10 ? "HYPOTHESIS" : "FAIL";

        sb.AppendLine($"{"Candidate",-20} {"R²(fb_res)",12} {"Class",10} {"R²(m_res)",12} {"Class",10}");
        sb.AppendLine(new string('-', 66));

        string bestFb = "FAIL", bestM = "FAIL";
        string bestFbCand = "", bestMCand = "";
        double bestFbR2 = 0, bestMR2 = 0;

        foreach (var (cname, cval) in candidates)
        {
            double r2_f = PearsonCorr(afr, cval); r2_f *= r2_f;
            double r2_m = PearsonCorr(amr, cval); r2_m *= r2_m;
            string cf = Cls(r2_f), cm = Cls(r2_m);
            if (r2_f > bestFbR2) { bestFbR2 = r2_f; bestFb = cf; bestFbCand = cname; }
            if (r2_m > bestMR2) { bestMR2 = r2_m; bestM = cm; bestMCand = cname; }
            sb.AppendLine($"{cname,-20} {r2_f,12:F4} {cf,10} {r2_m,12:F4} {cm,10}");
        }
        sb.AppendLine("");
        sb.AppendLine($"  fb_res best: {bestFbCand} → {bestFb} (R²={bestFbR2:F4})");
        sb.AppendLine($"  m_res best:  {bestMCand} → {bestM} (R²={bestMR2:F4})");
        sb.AppendLine(bestFb == "SUPPORTED" && bestM == "SUPPORTED" ? "  → ELIMINATED" : bestFb == "SUPPORTED" || bestM == "SUPPORTED" ? "  → PARTIAL" : bestFb == "CONDITIONAL" || bestM == "CONDITIONAL" ? "  → CONDITIONAL" : "  → SURVIVE");
        sb.AppendLine("");
        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // RGO_09: V3.4 COMPATIBILITY
    // ====================================================================
    [Fact]
    public void RGO_09_V34Compatibility_ResidualOriginImpact()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== RGO_09: V3.4 Compatibility ===");

        var data = CollectData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] core = data.Select(c => c.Core).ToArray();
        double[] fbRes = data.Select(c => c.FbResidual).ToArray();
        double[] mRes = data.Select(c => c.MResidual).ToArray();
        double[] tickV = data.Select(c => c.Tick).ToArray();
        double[] gmLog = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();

        double r2_c_tick = PearsonCorr(tickV, core); r2_c_tick *= r2_c_tick;
        double r2_all_tick = MultivariateR3(tickV, core, fbRes, mRes);
        double deltaTick = r2_all_tick - r2_c_tick;

        double r2_c_gm = PearsonCorr(gmLog, core); r2_c_gm *= r2_c_gm;
        double r2_all_gm = MultivariateR3(gmLog, core, fbRes, mRes);
        double deltaGm = r2_all_gm - r2_c_gm;

        sb.AppendLine($"  Core → tick:  R²={r2_c_tick:F4}  +Res: R²={r2_all_tick:F4}  Δ={deltaTick:F4}");
        sb.AppendLine($"  Core → ∇|m|:  R²={r2_c_gm:F4}  +Res: R²={r2_all_gm:F4}  Δ={deltaGm:F4}");
        sb.AppendLine("");

        if (deltaTick < 0.03 && deltaGm < 0.03)
        { sb.AppendLine("  → Negligible. PASS — Core-alone recovery."); }
        else if (deltaTick < 0.08 && deltaGm < 0.08)
        { sb.AppendLine("  → Weak. CONDITIONAL."); }
        else
        { sb.AppendLine("  → Significant. UNKNOWN — multi-channel needed."); }
        sb.AppendLine("");
        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // RGO_10: COMPREHENSIVE VERDICT
    // ====================================================================
    [Fact]
    public void RGO_10_ComprehensiveVerdict_ResidualOrigin()
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 96));
        sb.AppendLine("=== RGO_10: Comprehensive Verdict — Residual Origin ===");

        var data = CollectData();
        if (data.Count < 200) { sb.AppendLine($"Insufficient: {data.Count}"); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] afr = data.Select(c => Math.Abs(c.FbResidual)).ToArray();
        double[] amr = data.Select(c => Math.Abs(c.MResidual)).ToArray();
        double[] core = data.Select(c => c.Core).ToArray();
        double[] bBin = data.Select(c => c.IsBoundary ? 1.0 : 0.0).ToArray();
        double[] lcm = data.Select(c => Math.Log10(Math.Max(1e-15, c.CurvM))).ToArray();
        double[] fmd = data.Select(c => c.FbMDiff).ToArray();
        double[] adjV = data.Select(c => c.IsAdjacent ? 1.0 : 0.0).ToArray();

        int Score(double r2, double weak, double strong) => r2 > strong ? 2 : r2 > weak ? 1 : 0;

        double r2_bd_fr = PearsonCorr(afr, bBin); r2_bd_fr *= r2_bd_fr;
        double r2_bd_mr = PearsonCorr(amr, bBin); r2_bd_mr *= r2_bd_mr;
        int e1 = Score(Math.Max(r2_bd_fr, r2_bd_mr), 0.05, 0.15);

        double r2_cv_fr = PearsonCorr(afr, lcm); r2_cv_fr *= r2_cv_fr;
        double r2_cv_mr = PearsonCorr(amr, lcm); r2_cv_mr *= r2_cv_mr;
        int e2 = Score(Math.Max(r2_cv_fr, r2_cv_mr), 0.05, 0.15);

        double r2_adj = PearsonCorr(afr, adjV); r2_adj *= r2_adj;
        int e3 = Score(r2_adj, 0.03, 0.10);

        double r2_ap = PearsonCorr(afr, fmd); r2_ap *= r2_ap;
        int e4 = Score(r2_ap, 0.03, 0.10);

        double[] taV = data.Select(c => c.TickAnomaly).ToArray();
        double r2_cta = PearsonCorr(taV, core); r2_cta *= r2_cta;
        double r2_ata = MultivariateR2(taV, core, afr);
        int e5 = Score(r2_ata - r2_cta, 0.02, 0.05);

        int total = e1 + e2 + e3 + e4 + e5;

        sb.AppendLine($"  E1 Boundary:  {e1}/2  E2 Curvature: {e2}/2  E3 Topology: {e3}/2");
        sb.AppendLine($"  E4 Alpha-P:   {e4}/2  E5 Predictive: {e5}/2");
        sb.AppendLine($"  Total: {total}/10");
        sb.AppendLine("");

        string origin;
        if (total <= 1) origin = "NOISE";
        else if (e1 >= e2 && e1 >= e3 && e1 >= e4) origin = "BOUNDARY ARTIFACT";
        else if (e2 >= e1 && e2 >= e3) origin = "CURVATURE ARTIFACT";
        else if (e3 >= e1 && e3 >= e2) origin = "TOPOLOGY ARTIFACT";
        else if (e4 >= Math.Max(e1, Math.Max(e2, e3))) origin = "GENUINE DYNAMICAL (α-p coupling)";
        else origin = "MIXED";

        sb.AppendLine($"  ORIGIN: {origin}");
        sb.AppendLine(origin.Contains("GENUINE") ? "  → SharedCore INSUFFICIENT. Retain fb + |m|." :
                      origin == "NOISE" ? "  → SharedCore COMPLETE. Collapse fb + |m|." :
                      "  → Artifacts — may be eliminable.");
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

    private static double MultivariateR3(double[] y, double[] x1, double[] x2, double[] x3)
    {
        int n = y.Length; if (n < 4) return 0;
        double sy = y.Sum();
        double s1 = x1.Sum(), s2 = x2.Sum(), s3 = x3.Sum();
        double[] s = { s1, s2, s3 };
        double[,] S = new double[3, 3];
        double[] Sy = new double[3];
        for (int i = 0; i < n; i++)
        {
            double[] xv = { x1[i], x2[i], x3[i] };
            for (int p = 0; p < 3; p++)
            {
                Sy[p] += xv[p] * y[i];
                for (int q = 0; q < 3; q++) S[p, q] += xv[p] * xv[q];
            }
        }
        double[,] Sc = new double[3, 3];
        double[] Syc = new double[3];
        for (int p = 0; p < 3; p++)
        {
            Syc[p] = Sy[p] - s[p] * sy / n;
            for (int q = 0; q < 3; q++) Sc[p, q] = S[p, q] - s[p] * s[q] / n;
        }
        double det = Sc[0, 0] * (Sc[1, 1] * Sc[2, 2] - Sc[1, 2] * Sc[2, 1])
                   - Sc[0, 1] * (Sc[1, 0] * Sc[2, 2] - Sc[1, 2] * Sc[2, 0])
                   + Sc[0, 2] * (Sc[1, 0] * Sc[2, 1] - Sc[1, 1] * Sc[2, 0]);
        double[] beta = { 0, 0, 0 };
        if (Math.Abs(det) > 1e-15)
        {
            for (int p = 0; p < 3; p++)
            {
                double[,] D = (double[,])Sc.Clone();
                for (int r = 0; r < 3; r++) D[r, p] = Syc[r];
                double detP = D[0, 0] * (D[1, 1] * D[2, 2] - D[1, 2] * D[2, 1])
                            - D[0, 1] * (D[1, 0] * D[2, 2] - D[1, 2] * D[2, 0])
                            + D[0, 2] * (D[1, 0] * D[2, 1] - D[1, 1] * D[2, 0]);
                beta[p] = detP / det;
            }
        }
        double b0 = sy / n - beta[0] * s1 / n - beta[1] * s2 / n - beta[2] * s3 / n;
        double ssRes = 0, ssTot = 0, my = sy / n;
        for (int i = 0; i < n; i++)
        {
            double pred = b0 + beta[0] * x1[i] + beta[1] * x2[i] + beta[2] * x3[i];
            ssRes += (y[i] - pred) * (y[i] - pred);
            ssTot += (y[i] - my) * (y[i] - my);
        }
        return ssTot > 1e-15 ? Math.Max(0, Math.Min(1, 1 - ssRes / ssTot)) : 0;
    }
}
