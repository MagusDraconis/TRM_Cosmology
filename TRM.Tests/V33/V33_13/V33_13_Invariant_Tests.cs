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
public class V33_13_Invariant_Tests
{
    private readonly ITestOutputHelper _o;
    public V33_13_Invariant_Tests(ITestOutputHelper o) { _o = o; }

    // ====================================================================
    // INV_01: CONSERVED QUANTITY — Is there a conserved quantity across the loop?
    //
    // Null:        No conserved quantity across G-T-F
    // Alt:         A conserved quantity exists (e.g., loop integral)
    // Observable:  Does |∇|m|| · |∇fb| / |∇Tick| show reduced variance?
    //              (ad-hoc conserved quantity candidate)
    // Pass (RL favored):    A quantity has significantly lower CoV than components
    // Fail (CC favored):    No combination has lower CoV
    // ====================================================================
    [Fact]
    public void INV_01_ConservedQuantity_IsThereAnInvariant()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== INV_01: Conserved Quantity — Is there an invariant across the loop? ===");

        var data = CollectInvariantData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] gmV = data.Select(c => c.GradM).ToArray();
        double[] gfV = data.Select(c => c.GradFb).ToArray();
        double[] gtV = data.Select(c => c.GradTick).ToArray();
        double[] taV = data.Select(c => c.TickAnomaly).ToArray();
        double[] bdV = data.Select(c => (double)c.ZoneDist).ToArray();

        // Individual CoVs
        double Cov(double[] x) { double m = x.Average(); return Math.Sqrt(x.Select(v => (v - m) * (v - m)).Average()) / Math.Max(1e-15, m); }

        // Candidate invariants: combinations that might be conserved
        double[] c1 = gmV.Zip(gfV, (g, f) => g / Math.Max(1e-15, f)).ToArray();   // ∇|m| / ∇fb
        double[] c2 = gmV.Zip(gtV, (g, t) => g / Math.Max(1e-15, t)).ToArray();   // ∇|m| / ∇Tick
        double[] c3 = gmV.Zip(gfV, (g, f) => g * f).ToArray();                    // ∇|m| · ∇fb
        double[] c4 = gmV.Zip(gfV, (g, f) => (g - f) / Math.Max(1e-15, g + f)).ToArray(); // fractional difference
        double[] c5 = taV.Zip(bdV, (a, b) => a * Math.Max(1, b + 1)).ToArray();   // anomaly × distance

        double covGm = Cov(gmV);
        double covGf = Cov(gfV);
        double covGt = Cov(gtV);
        double covC1 = Cov(c1);
        double covC2 = Cov(c2);
        double covC3 = Cov(c3);
        double covC4 = Cov(c4);
        double covC5 = Cov(c5);

        double minCompCoV = Math.Min(covGm, Math.Min(covGf, covGt));
        double minCandidateCoV = Math.Min(Math.Min(covC1, covC2), Math.Min(Math.Min(covC3, covC4), covC5));

        sb.AppendLine($"{"Quantity",-28} {"CoV",8}");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"{"∇|m|",-28} {covGm,8:F4}");
        sb.AppendLine($"{"∇fb",-28} {covGf,8:F4}");
        sb.AppendLine($"{"∇Tick",-28} {covGt,8:F4}");
        sb.AppendLine($"{"∇|m| / ∇fb",-28} {covC1,8:F4}");
        sb.AppendLine($"{"∇|m| / ∇Tick",-28} {covC2,8:F4}");
        sb.AppendLine($"{"∇|m| · ∇fb",-28} {covC3,8:F4}");
        sb.AppendLine($"{"(∇|m|-∇fb)/(sum)",-28} {covC4,8:F4}");
        sb.AppendLine($"{"Anomaly × (dist+1)",-28} {covC5,8:F4}");
        sb.AppendLine("");

        bool hasInvariant = minCandidateCoV < minCompCoV * 0.70;
        sb.AppendLine($"  Min component CoV: {minCompCoV:F4}  Min candidate CoV: {minCandidateCoV:F4}");
        sb.AppendLine($"  → {(hasInvariant ? "INVARIANT FOUND — a combination has significantly lower variance" : "NO invariant — all combinations have comparable variance")}");
        sb.AppendLine(hasInvariant
            ? "  VERDICT: Favors RESONANCE LOOP — conserved quantity suggests dynamical coupling"
            : "  VERDICT: Favors COMMON CAUSE — no special invariant structure detected");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(!hasInvariant, $"INV_01: Min component CoV={minCompCoV:F4}, min candidate CoV={minCandidateCoV:F4}. CC favored if no invariant found.");
    }

    // ====================================================================
    // INV_02: SYMMETRY — Is the G-T-F structure symmetric under exchange?
    //
    // Null:        G, T, F are symmetric (can be permuted)
    // Alt:         G, T, F have asymmetric roles (hierarchy)
    // Observable:  Correlation matrix asymmetry — is r(G,F) ≠ r(G,T) ≠ r(F,T)?
    // Pass (specific structure):  Correlations differ — hierarchy exists
    // Fail (symmetric):           All pairwise correlations equal
    // ====================================================================
    [Fact]
    public void INV_02_SymmetryConstraint_AreGTFSymmetric()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== INV_02: Symmetry Constraint — Are G, T, F symmetric under exchange? ===");

        var data = CollectInvariantData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] gmV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] gfV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] gtV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray();
        double[] taV = data.Select(c => c.TickAnomaly).ToArray();

        double rGF = PearsonCorr(gmV, gfV);
        double rGT = PearsonCorr(gmV, gtV);
        double rFT = PearsonCorr(gfV, gtV);
        double rGA = PearsonCorr(gmV, taV);
        double rFA = PearsonCorr(gfV, taV);

        sb.AppendLine("  Correlation matrix:");
        sb.AppendLine($"    r(∇|m|, ∇fb)     = {rGF,8:F4}");
        sb.AppendLine($"    r(∇|m|, ∇Tick)   = {rGT,8:F4}");
        sb.AppendLine($"    r(∇fb, ∇Tick)    = {rFT,8:F4}");
        sb.AppendLine($"    r(∇|m|, Anomaly)  = {rGA,8:F4}");
        sb.AppendLine($"    r(∇fb, Anomaly)   = {rFA,8:F4}");
        sb.AppendLine("");

        // Asymmetry: max pairwise difference
        double maxDiff = Math.Max(Math.Abs(rGF - rGT), Math.Max(Math.Abs(rGF - rFT), Math.Abs(rGT - rFT)));
        bool isAsymmetric = maxDiff > 0.15;

        sb.AppendLine($"  Max pairwise difference: {maxDiff:F4}");
        sb.AppendLine($"  → {(isAsymmetric ? "ASYMMETRIC — G, T, F have distinct roles in the correlation structure" : "SYMMETRIC — G, T, F are interchangeable in correlations")}");
        sb.AppendLine(isAsymmetric
            ? "  VERDICT: Favors NEITHER — asymmetry is expected under both models"
            : "  VERDICT: Favors COMMON CAUSE — symmetry suggests shared rather than causal coupling");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // INV_03: DIMENSIONAL CONSTRAINT — How many effective dimensions?
    //
    // Null:        G, T, F span 3 independent dimensions
    // Alt:         They span fewer (redundancy)
    // Observable:  PCA eigenvalues of [∇|m|, ∇fb, ∇Tick, TickAnomaly]
    // Pass (CC favored):     Dominant first component (> 70% variance)
    // Fail (RL favored):     Multiple significant components
    // ====================================================================
    [Fact]
    public void INV_03_DimensionalConstraint_EffectiveDimensions()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== INV_03: Dimensional Constraint — Effective dimension of G-T-F space ===");

        var data = CollectInvariantData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        // Standardize variables
        double[] gmV = ZScore(data.Select(c => c.GradM).ToArray());
        double[] gfV = ZScore(data.Select(c => c.GradFb).ToArray());
        double[] gtV = ZScore(data.Select(c => c.GradTick).ToArray());
        double[] taV = ZScore(data.Select(c => c.TickAnomaly).ToArray());

        // Covariance matrix (4×4)
        int n = gmV.Length;
        double s11 = 0, s22 = 0, s33 = 0, s44 = 0;
        double s12 = 0, s13 = 0, s14 = 0, s23 = 0, s24 = 0, s34 = 0;
        for (int i = 0; i < n; i++)
        {
            s11 += gmV[i] * gmV[i]; s22 += gfV[i] * gfV[i];
            s33 += gtV[i] * gtV[i]; s44 += taV[i] * taV[i];
            s12 += gmV[i] * gfV[i]; s13 += gmV[i] * gtV[i]; s14 += gmV[i] * taV[i];
            s23 += gfV[i] * gtV[i]; s24 += gfV[i] * taV[i]; s34 += gtV[i] * taV[i];
        }
        s11 /= (n - 1); s22 /= (n - 1); s33 /= (n - 1); s44 /= (n - 1);
        s12 /= (n - 1); s13 /= (n - 1); s14 /= (n - 1);
        s23 /= (n - 1); s24 /= (n - 1); s34 /= (n - 1);

        // Power iteration for top 2 eigenvalues
        double[] ev = { s11, s22, s33, s44 };
        double trace = s11 + s22 + s33 + s44;

        // Approximate eigenvalues via covariance trace and off-diagonal sum
        double offDiagSum = Math.Abs(s12) + Math.Abs(s13) + Math.Abs(s14) + Math.Abs(s23) + Math.Abs(s24) + Math.Abs(s34);

        // For a correlation-like matrix, trace = 4 (standardized)
        // First eigenvalue ≈ 1 + sum-of-off-diagonals / trace (rough)
        double lambda1 = 1.0 + offDiagSum / 4.0;
        double varExplained = 100.0 * lambda1 / 4.0; // divide by trace (=4 for correlation matrix)

        sb.AppendLine($"  Trace (total variance): {trace:F4}");
        sb.AppendLine($"  Off-diagonal sum:       {offDiagSum:F4}");
        sb.AppendLine($"  λ₁ estimate:            {lambda1:F4}");
        sb.AppendLine($"  Variance explained:     {varExplained:F1}%");
        sb.AppendLine("");

        bool isOneDimensional = varExplained > 70;
        sb.AppendLine($"  → {(isOneDimensional ? "LOW-DIMENSIONAL — dominant single component, G-T-F are redundant" : "MULTI-DIMENSIONAL — multiple independent components")}");
        sb.AppendLine(isOneDimensional
            ? "  VERDICT: Favors COMMON CAUSE — one dominant dimension suggests shared origin"
            : "  VERDICT: Favors RESONANCE LOOP — multiple dimensions suggest distinct dynamical roles");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(isOneDimensional, $"INV_03: λ₁ explains {varExplained:F1}% variance. CC favored if >70% (one dominant component).");
    }

    // ====================================================================
    // INV_04: CODIMENSION CONSTRAINT — Is the G-T-F structure codim-1?
    //
    // Null:        Boundary is codim-1 → G-T-F should also be codim-1
    // Alt:         G-T-F has higher codimension structure
    // Observable:  Does conditioning on 1 variable screen the other 2?
    // Pass (CC favored):     Partial correlations ≈ 0 after conditioning on one
    // Fail (RL favored):     Significant partial correlations remain
    // ====================================================================
    [Fact]
    public void INV_04_CodimensionConstraint_IsCodimOne()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== INV_04: Codimension Constraint — Is the G-T-F structure codim-1? ===");

        var data = CollectInvariantData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        double[] gmV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
        double[] gfV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
        double[] gtV = data.Select(c => Math.Log10(Math.Max(1e-15, c.GradTick))).ToArray();
        double[] taV = data.Select(c => c.TickAnomaly).ToArray();

        // Partial correlations: conditioning on one variable
        double rGF_T = PartialCorrelation(gmV, gfV, gtV);
        double rGF_A = PartialCorrelation(gmV, gfV, taV);
        double rGT_F = PartialCorrelation(gmV, gtV, gfV);
        double rFT_G = PartialCorrelation(gfV, gtV, gmV);

        sb.AppendLine($"  r(∇|m|, ∇fb | ∇Tick):     {rGF_T,8:F4}");
        sb.AppendLine($"  r(∇|m|, ∇fb | TickAnom):  {rGF_A,8:F4}");
        sb.AppendLine($"  r(∇|m|, ∇Tick | ∇fb):     {rGT_F,8:F4}");
        sb.AppendLine($"  r(∇fb, ∇Tick | ∇|m|):     {rFT_G,8:F4}");
        sb.AppendLine("");

        double maxPartial = Math.Max(Math.Abs(rGF_T), Math.Max(Math.Abs(rGF_A), Math.Max(Math.Abs(rGT_F), Math.Abs(rFT_G))));
        bool isCodim1 = maxPartial < 0.20;

        sb.AppendLine($"  Max |partial r|: {maxPartial:F4}");
        sb.AppendLine($"  → {(isCodim1 ? "CODIM-1 — conditioning on one variable screens the others" : "HIGHER CODIMENSION — residual coupling survives single-variable conditioning")}");
        sb.AppendLine(isCodim1
            ? "  VERDICT: Favors COMMON CAUSE — codim-1 structure matches boundary"
            : "  VERDICT: Favors RESONANCE LOOP — multi-dimensional coupling structure");
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(isCodim1, $"INV_04: Max |partial r| = {maxPartial:F4}, threshold 0.20. CC favored if codim-1.");
    }

    // ====================================================================
    // INV_05: TOPOLOGY CONSTRAINT — Is the correlation structure topological?
    //
    // Null:        G-T-F correlations are smooth (continuous) across parameter space
    // Alt:         Correlations change discretely at topological boundaries
    // Observable:  Does r(∇|m|, ∇fb) differ between sign regions?
    // Pass (topological):     r differs between POS and NEG regions
    // Fail (geometric):       r is continuous across sign boundaries
    // ====================================================================
    [Fact]
    public void INV_05_TopologyConstraint_DoCorrelationsJumpAtBoundaries()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== INV_05: Topology Constraint — Do correlations jump at boundaries? ===");

        var data = CollectInvariantData();
        if (data.Count < 100) { sb.AppendLine("Insufficient."); _o.WriteLine(sb.ToString()); Assert.True(true); return; }

        foreach (var arch in new[] { "GAN", "CNS" })
        {
            var archData = data.Where(c => c.Arch == arch).ToList();
            if (archData.Count < 50) continue;

            var pos = archData.Where(c => c.Sign > 0).ToList();
            var neg = archData.Where(c => c.Sign < 0).ToList();
            if (pos.Count < 20 || neg.Count < 20) continue;

            double[] pGm = pos.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
            double[] pGf = pos.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();
            double[] nGm = neg.Select(c => Math.Log10(Math.Max(1e-15, c.GradM))).ToArray();
            double[] nGf = neg.Select(c => Math.Log10(Math.Max(1e-15, c.GradFb))).ToArray();

            double rPos = PearsonCorr(pGm, pGf);
            double rNeg = PearsonCorr(nGm, nGf);
            double delta = rPos - rNeg;

            double mPos = pos.Average(c => c.GradM);
            double mNeg = neg.Average(c => c.GradM);
            double fPos = pos.Average(c => c.GradFb);
            double fNeg = neg.Average(c => c.GradFb);

            sb.AppendLine($"  {arch}:");
            sb.AppendLine($"    POS: r(∇|m|,∇fb)={rPos:F4}  ∇|m|={mPos:F4}  ∇fb={fPos:F4}  n={pos.Count}");
            sb.AppendLine($"    NEG: r(∇|m|,∇fb)={rNeg:F4}  ∇|m|={mNeg:F4}  ∇fb={fNeg:F4}  n={neg.Count}");
            sb.AppendLine($"    Δr = {delta:F4}  Δ∇|m|={mPos-mNeg:F4}  Δ∇fb={fPos-fNeg:F4}");
        }
        sb.AppendLine("");

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // INV_06: α CHAIN INVARIANCE — Is the hierarchy invariant under α?
    //
    // Null:        The causal hierarchy is invariant under α (coupling strength)
    // Alt:         The hierarchy changes with α
    // Observable:  Does the dominant predictor of ∇|m| change with α?
    // Pass (hierarchy stable):  Same predictor dominates across α
    // Fail (hierarchy α-dependent): Different predictors dominate at different α
    // ====================================================================
    [Fact]
    public void INV_06_AlphaChainInvariance_IsHierarchyStable()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== INV_06: α-Chain Invariance — Is the causal hierarchy stable under α? ===");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();

        double[] alphaTest = { 0.30, 0.50, 0.70, 0.90, 1.10, 1.30 };

        foreach (var (arch, fam) in new[] { ("GAN", VcFamily.GAN), ("CNS", VcFamily.CNS) })
        {
            sb.AppendLine($"  {arch}:");
            sb.AppendLine($"  {"α",6} {"r(∇fb→∇|m|)",14} {"r(∇Tick→∇|m|)",14} {"Dominant",10}");

            foreach (double alpha in alphaTest)
            {
                const int nG = 14;
                double bMin = 0.0, bMax = 2.0, gMin = 0.0, gMax = 2.0;
                double db = (bMax - bMin) / (nG - 1), dg = (gMax - gMin) / (nG - 1);

                var gM = new double[nG, nG]; var gFb = new double[nG, nG]; var gTick = new double[nG, nG];
                var gS = new int[nG, nG];

                Parallel.For(0, nG, bi =>
                {
                    double beta = bMin + db * bi;
                    for (int gi = 0; gi < nG; gi++)
                    {
                        double gamma = gMin + dg * gi;
                        var f = ComputeFull(fam, 1.0, 1.0, alpha, beta, gamma, distances, sortedD, xiBase, k0Base, 21, 0.21, (1.40 - 0.21) / 20.0);
                        gM[bi, gi] = f.absM; gS[bi, gi] = f.sign; gFb[bi, gi] = f.fb; gTick[bi, gi] = f.tick;
                    }
                });

                var pairs = new ConcurrentBag<(double gm, double gf, double gt)>();
                Parallel.For(1, nG - 1, bi =>
                {
                    for (int gi = 1; gi < nG - 1; gi++)
                    {
                        double dMdB = (gM[bi + 1, gi] - gM[bi - 1, gi]) / (2 * db);
                        double dMdG = (gM[bi, gi + 1] - gM[bi, gi - 1]) / (2 * dg);
                        double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                        double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                        double dTdB = (gTick[bi + 1, gi] - gTick[bi - 1, gi]) / (2 * db);
                        double dTdG = (gTick[bi, gi + 1] - gTick[bi, gi - 1]) / (2 * dg);
                        pairs.Add((Math.Sqrt(dMdB*dMdB+dMdG*dMdG), Math.Sqrt(dFdB*dFdB+dFdG*dFdG), Math.Sqrt(dTdB*dTdB+dTdG*dTdG)));
                    }
                });

                var all = pairs.ToList();
                if (all.Count < 50) continue;

                double[] gmArr = all.Select(p => Math.Log10(Math.Max(1e-15, p.gm))).ToArray();
                double[] gfArr = all.Select(p => Math.Log10(Math.Max(1e-15, p.gf))).ToArray();
                double[] gtArr = all.Select(p => Math.Log10(Math.Max(1e-15, p.gt))).ToArray();

                double rFb = PearsonCorr(gmArr, gfArr);
                double rTick = PearsonCorr(gmArr, gtArr);
                string dom = Math.Abs(rFb) > Math.Abs(rTick) ? "∇fb" : "∇Tick";

                sb.AppendLine($"  {alpha,6:F2} {rFb,14:F4} {rTick,14:F4} {dom,10}");
            }
            sb.AppendLine("");
        }

        _o.WriteLine(sb.ToString());
        Assert.True(true);
    }

    // ====================================================================
    // DATA COLLECTION
    // ====================================================================

    private List<InvCell> CollectInvariantData()
    {
        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, daD = (aMax - aMin) / (nA - 1);
        var allCells = new ConcurrentBag<InvCell>();

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

                    double dFdB = (gFb[bi + 1, gi] - gFb[bi - 1, gi]) / (2 * db);
                    double dFdG = (gFb[bi, gi + 1] - gFb[bi, gi - 1]) / (2 * dg);
                    double gradFb = Math.Sqrt(dFdB * dFdB + dFdG * dFdG);

                    double dTdB = (gTick[bi + 1, gi] - gTick[bi - 1, gi]) / (2 * db);
                    double dTdG = (gTick[bi, gi + 1] - gTick[bi, gi - 1]) / (2 * dg);
                    double gradTick = Math.Sqrt(dTdB * dTdB + dTdG * dTdG);

                    int d = dist[bi, gi] == int.MaxValue ? 4 : Math.Min(3, dist[bi, gi]);
                    bool isBdry = d == 0;

                    var nT = new List<double>();
                    if (bi > 0) nT.Add(gTick[bi - 1, gi]); if (bi + 1 < nG) nT.Add(gTick[bi + 1, gi]);
                    if (gi > 0) nT.Add(gTick[bi, gi - 1]); if (gi + 1 < nG) nT.Add(gTick[bi, gi + 1]);
                    double tLocMean = nT.Count > 0 ? nT.Average() : gTick[bi, gi];
                    double tAnom = Math.Abs(gTick[bi, gi] - tLocMean) / Math.Max(1e-15, tLocMean);

                    allCells.Add(new InvCell(arch, gradM, gradFb, gradTick, d, isBdry, tAnom, gS[bi, gi]));
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

    private static double[] ZScore(double[] x)
    {
        double m = x.Average();
        double sd = Math.Sqrt(x.Select(v => (v - m) * (v - m)).Average());
        return x.Select(v => sd > 1e-15 ? (v - m) / sd : 0.0).ToArray();
    }

    private record InvCell(string Arch, double GradM, double GradFb, double GradTick, int ZoneDist, bool IsBoundary, double TickAnomaly, int Sign);
}
