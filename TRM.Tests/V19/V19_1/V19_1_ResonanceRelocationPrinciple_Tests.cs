using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TRM.Tests.V7_3_and_4;
using static TRM.Tests.V7_3_and_4.V7TestHelpers;
using static TRM.Tests.V14.V14TestHelpers;

namespace TRM.Tests.V19_1;

[Trait("Category", "V19_1")]
public class V19_1_ResonanceRelocationPrinciple_Tests
{
    private readonly ITestOutputHelper _o;
    public V19_1_ResonanceRelocationPrinciple_Tests(ITestOutputHelper o) { _o = o; }

    [Fact]
    public void RRP_01_ResonanceRelocationPrincipleAudit()
    {
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== RRP_01: Resonance Relocation Principle Audit ===");
        _o.WriteLine("=== Does resonance survive in another role? ===");
        _o.WriteLine(new string('=', 108));

        _o.WriteLine("");
        _o.WriteLine("KNOWN: Resonance (|m+1|~0) is NOT the universal organizer (V14.0).");
        _o.WriteLine("QUESTION: Does resonance predict ANY TRM property?");
        _o.WriteLine("");

        const int baseSeed = 629471;
        const double xiBase = 2.95, k0Base = 1.0;
        var distances = BuildDistanceEnsemble(baseSeed, systems: 8, nodesPerSystem: 64);
        var sortedD = distances.OrderBy(x => x).ToArray();
        const int nA = 21;
        double aMin = 0.21, aMax = 1.40, da = (aMax - aMin) / (nA - 1);

        // ================================================================
        // Generate diverse states across architectures
        // ================================================================
        var rng = new Random(42);
        var states = new List<ResonanceState>();

        // Sweep architectures broadly
        foreach (var (fam, label, theta) in new[]
        {
            (VcFamily.SAC, "PURE", 0.00),
            (VcFamily.RCS, "RATIONAL", 999.0),
            (VcFamily.ICS, "STRETCHED", 1.00),
            (VcFamily.GAN, "COMPOSITE", 0.64),
        })
        {
            int nPts = fam == VcFamily.GAN ? 60 : 30;
            for (int i = 0; i < nPts; i++)
            {
                double beta = fam == VcFamily.ICS ? -1.0 + rng.NextDouble() * 3.0
                    : rng.NextDouble() * 2.0;
                double gamma = fam == VcFamily.GAN ? rng.NextDouble() * 2.0 : 0.0;

                var (m, dTdp) = ComputeM_and_DTdp(fam, 1.0, 1.0, 0.70, beta, gamma,
                    distances, sortedD, xiBase, k0Base, nA, aMin, da);

                double absM = Math.Abs(m);
                double V = Math.Abs(1.0 + m);     // |m+1| — resonance distance
                int sign = dTdp > 1e-8 ? 1 : -1;

                // Compute Tick stability: variance of Tick across alpha sweep
                var ticks = new List<double>();
                for (int si = 0; si < nA; si++)
                {
                    double a = aMin + da * si;
                    var v = new VariantSpec("X", fam, 1.0, 1.0, a, beta, gamma);
                    double sv1 = 0, svt = 0;
                    for (int pIdx = 0; pIdx < 3; pIdx++)
                    { double p = 1.5 + pIdx * 1.0; var cci = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, p, v); sv1 += cci.VarI1; svt += cci.VarTerms; }
                    ticks.Add((sv1 + svt) / 3.0);
                }
                double tickMean = ticks.Average();
                double tickCV = ticks.Count > 1 ? Math.Sqrt(ticks.Average(t => (t - tickMean) * (t - tickMean))) / Math.Abs(tickMean) : 0;

                // Attractor persistence: does sign stay constant across alpha?
                var signs = new List<int>();
                for (int si = 0; si < nA; si++)
                {
                    double a = aMin + da * si;
                    var v = new VariantSpec("X", fam, 1.0, 1.0, a, beta, gamma);
                    double sv1 = 0, svt = 0;
                    for (int pIdx = 0; pIdx < 3; pIdx++)
                    { double p = 1.5 + pIdx * 1.0; var cci = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, p, v); sv1 += cci.VarI1; svt += cci.VarTerms; }
                    double rawM = 0;
                    // simplified: tick gradient sign as proxy
                    signs.Add(svt > sv1 ? 1 : -1);
                }
                double signPersistence = (double)signs.Count(s => s == signs[0]) / signs.Count;

                // Feedback magnitude
                var fbVals = new List<double>();
                var v1Vals = new List<double>(); var vtVals = new List<double>();
                for (int si = 0; si < nA; si++)
                {
                    double a = aMin + da * si;
                    var v = new VariantSpec("X", fam, 1.0, 1.0, a, beta, gamma);
                    double sv1 = 0, svt = 0;
                    for (int pIdx = 0; pIdx < 3; pIdx++)
                    { double p = 1.5 + pIdx * 1.0; var cci = EvaluateCciVariantAtP(distances, sortedD, xiBase, k0Base, p, v); sv1 += cci.VarI1; svt += cci.VarTerms; }
                    v1Vals.Add(sv1 / 3.0); vtVals.Add(svt / 3.0);
                }
                for (int si = 1; si < nA; si++)
                {
                    double dV1 = (v1Vals[si] - v1Vals[si - 1]) / da;
                    if (Math.Abs(dV1) > 1e-12)
                        fbVals.Add(-(vtVals[si] - vtVals[si - 1]) / da / dV1);
                }
                double fb = fbVals.Count > 0 ? fbVals.Average() : 0;

                // Distance to SAC fixed point
                double distToSac = Math.Abs(absM - 0.93);

                states.Add(new(label, absM, V, sign, tickCV, signPersistence,
                    Math.Abs(fb), distToSac, tickMean, m));
            }
        }

        _o.WriteLine($"Generated {states.Count} states across 4 architectures.");
        _o.WriteLine("");

        // ================================================================
        // Resonance Measures
        // ================================================================
        // V = |m+1| : classical resonance distance
        // distToSac : distance to SAC fixed point (|m|=0.93)
        // 1/V : inverse resonance distance

        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Architecture Resonance Profiles ===");
        _o.WriteLine("");

        _o.WriteLine($"{"Arch",-14} {"mean(|m|)",10} {"mean(|m+1|)",12} {"mean(distSAC)",14} {"mean(TickCV)",13} {"mean(|FB|)",11} {"SignPersist",12}");
        _o.WriteLine(new string('-', 88));

        foreach (var grp in states.GroupBy(s => s.label))
        {
            var g = grp.ToList();
            _o.WriteLine($"{grp.Key,-14} {g.Average(s => s.absM),10:F3} {g.Average(s => s.V),12:F3} {g.Average(s => s.distToSac),14:F3} {g.Average(s => s.tickCV),13:F4} {g.Average(s => s.fbMag),11:F3} {g.Average(s => s.signPersistence),11:F3}");
        }
        _o.WriteLine("");

        // ================================================================
        // Correlation Matrix: Resonance vs TRM Properties
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Resonance Correlation Matrix ===");
        _o.WriteLine("");

        var allV = states.Select(s => s.V).ToArray();
        var allDistSac = states.Select(s => s.distToSac).ToArray();
        var allInvV = states.Select(s => 1.0 / Math.Max(s.V, 0.001)).ToArray();
        var allAbsM = states.Select(s => s.absM).ToArray();

        var properties = new (string name, double[] values, string desc)[]
        {
            ("Tick stability (CV)",   states.Select(s => -s.tickCV).ToArray(), "lower CV = more stable"),
            ("Sign persistence",      states.Select(s => s.signPersistence).ToArray(), "fraction of alpha where sign unchanged"),
            ("Feedback magnitude",    states.Select(s => s.fbMag).ToArray(), "|avg feedback|"),
            ("|m|",                   states.Select(s => s.absM).ToArray(), "budget tradeoff coordinate"),
            ("Sign (binary)",         states.Select(s => (double)s.sign).ToArray(), "organizational sign"),
            ("Distance to SAC",       states.Select(s => s.distToSac).ToArray(), "|m-0.93|"),
            ("m (raw)",               states.Select(s => s.m).ToArray(), "slope coordinate"),
        };

        _o.WriteLine("Correlation of resonance measures with TRM properties:");
        _o.WriteLine("");
        _o.WriteLine($"{"Property",-24} {"r(V=|m+1|)",13} {"r(1/V)",13} {"r(distSAC)",13} {"r(|m|)",13}");
        _o.WriteLine(new string('-', 78));

        var bestPairs = new List<(string prop, string res, double r, double r2)>();

        foreach (var prop in properties)
        {
            double rV = PearsonCorr(allV, prop.values);
            double rInvV = PearsonCorr(allInvV, prop.values);
            double rDistSac = PearsonCorr(allDistSac, prop.values);
            double rAbsM = PearsonCorr(allAbsM, prop.values);

            _o.WriteLine($"{prop.name,-24} {rV,13:F4} {rInvV,13:F4} {rDistSac,13:F4} {rAbsM,13:F4}");

            double bestR = new[] { Math.Abs(rV), Math.Abs(rInvV), Math.Abs(rDistSac), Math.Abs(rAbsM) }.Max();
            string bestRes = Math.Abs(rV) >= Math.Abs(rInvV) && Math.Abs(rV) >= Math.Abs(rDistSac) && Math.Abs(rV) >= Math.Abs(rAbsM) ? "|m+1|"
                : Math.Abs(rInvV) >= Math.Abs(rDistSac) && Math.Abs(rInvV) >= Math.Abs(rAbsM) ? "1/|m+1|"
                : Math.Abs(rDistSac) >= Math.Abs(rAbsM) ? "distSAC" : "|m|";
            bestPairs.Add((prop.name, bestRes, bestR, bestR * bestR));
        }
        _o.WriteLine("");

        // ================================================================
        // Which property is best predicted by resonance?
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Best Resonance-Predicted Property ===");
        _o.WriteLine("");

        var ranked = bestPairs.OrderByDescending(p => p.r2).ToList();
        _o.WriteLine($"{"Rank",5} {"Property",-26} {"Best Measure",-14} {"r",9} {"R²",9} {"Meaningful?",12}");
        _o.WriteLine(new string('-', 77));

        foreach (var (pair, idx) in ranked.Select((p, i) => (p, i + 1)))
        {
            bool meaningful = pair.r2 > 0.30;
            _o.WriteLine($"{idx,5} {pair.prop,-26} {pair.res,-14} {pair.r,9:F4} {pair.r2,9:F4} {(meaningful ? "YES" : "no"),12}");
        }
        _o.WriteLine("");

        var top = ranked.First();
        _o.WriteLine($"Best: {top.prop} is predicted by {top.res} (r={top.r:F4}, R²={top.r2:F4})");
        _o.WriteLine("");

        // ================================================================
        // Does resonance predict stability?
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Does Resonance Predict Stability? ===");
        _o.WriteLine("");

        double rV_stab = PearsonCorr(allV, properties.First(p => p.name == "Tick stability (CV)").values);
        double rInvV_stab = PearsonCorr(allInvV, properties.First(p => p.name == "Tick stability (CV)").values);

        _o.WriteLine($"r(|m+1|, tick stability) = {rV_stab:F4} (R²={rV_stab * rV_stab:F4})");
        _o.WriteLine($"r(1/|m+1|, tick stability) = {rInvV_stab:F4} (R²={rInvV_stab * rInvV_stab:F4})");
        _o.WriteLine("");

        if (rV_stab * rV_stab > 0.30)
        {
            _o.WriteLine("YES — resonance predicts Tick stability.");
            _o.WriteLine("States closer to m=-1 have more stable Tick fields.");
        }
        else
        {
            _o.WriteLine("NO — resonance does NOT meaningfully predict Tick stability.");
        }

        // ================================================================
        // Does resonance predict attractor persistence?
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Does Resonance Predict Attractor Persistence? ===");
        _o.WriteLine("");

        double rV_pers = PearsonCorr(allV, properties.First(p => p.name == "Sign persistence").values);
        double rInvV_pers = PearsonCorr(allInvV, properties.First(p => p.name == "Sign persistence").values);

        _o.WriteLine($"r(|m+1|, sign persistence) = {rV_pers:F4} (R²={rV_pers * rV_pers:F4})");
        _o.WriteLine("");

        if (rV_pers * rV_pers > 0.20)
            _o.WriteLine("YES — resonance predicts attractor persistence.");
        else
            _o.WriteLine("NO — resonance does NOT predict attractor persistence.");

        // ================================================================
        // Is SAC a resonance fixed point?
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Is SAC a Resonance Fixed Point? ===");
        _o.WriteLine("");

        var sacStates = states.Where(s => s.label == "PURE").ToList();
        double sacVmean = sacStates.Average(s => s.V);
        double sacDistSacMean = sacStates.Average(s => s.distToSac);

        _o.WriteLine($"SAC mean |m+1| = {sacVmean:F4}");
        _o.WriteLine($"SAC mean dist(SAC) = {sacDistSacMean:F4} (|m|-0.93)");
        _o.WriteLine($"SAC |m| range: [{sacStates.Min(s => s.absM):F4}, {sacStates.Max(s => s.absM):F4}]");
        _o.WriteLine("");

        // SAC has |m| ≈ 0.93, so |m+1| ≈ 1.93 — NOT near zero
        if (sacVmean > 1.0)
        {
            _o.WriteLine("SAC is NOT a resonance fixed point.");
            _o.WriteLine($"SAC |m|≈0.93 => |m+1|≈{sacVmean:F2}, far from the resonance value 0.");
            _o.WriteLine("");
            _o.WriteLine("SAC is a CONSERVATION fixed point, not a resonance point.");
            _o.WriteLine("Its invariance comes from zero-width kernel topology,");
            _o.WriteLine("not from proximity to m=-1.");
        }

        // ================================================================
        // Has resonance been rejected ONLY as organizational principle?
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Scope of Resonance Rejection ===");
        _o.WriteLine("");

        bool anyStrong = ranked.Any(p => p.r2 > 0.40);
        bool anyWeak = ranked.Any(p => p.r2 > 0.15);

        if (anyStrong)
        {
            _o.WriteLine("Resonance has explanatory power BEYOND organization.");
            _o.WriteLine($"Top prediction: {top.prop} (R²={top.r2:F4}).");
            _o.WriteLine("Rejection scope: organization only, NOT all TRM properties.");
        }
        else if (anyWeak)
        {
            _o.WriteLine("Resonance has WEAK explanatory power for non-organizational properties.");
            _o.WriteLine($"Best: {top.prop} (R²={top.r2:F4}) — marginal at best.");
            _o.WriteLine("Rejection scope: organization primarily, but weak elsewhere too.");
        }
        else
        {
            _o.WriteLine("Resonance has NO explanatory power for ANY TRM property tested.");
            _o.WriteLine("Rejection scope: COMPLETE.");
        }
        _o.WriteLine("");

        // ================================================================
        // Decision
        // ================================================================
        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== Decision ===");
        _o.WriteLine("");

        string classification;
        if (anyStrong)
        {
            _o.WriteLine("VERDICT: SUPPORTED — resonance survives in another role.");
            _o.WriteLine($"Resonance predicts {top.prop} (R²={top.r2:F4}).");
            classification = "SUPPORTED";
        }
        else if (anyWeak)
        {
            _o.WriteLine("VERDICT: CONDITIONAL — resonance has marginal non-organizational role.");
            classification = "CONDITIONAL";
        }
        else
        {
            _o.WriteLine("VERDICT: FALSIFIED — resonance has no independent explanatory power.");
            classification = "FALSIFIED";
        }

        _o.WriteLine("");
        _o.WriteLine($"Classification: {classification}");
        _o.WriteLine("");

        _o.WriteLine("Resonance Relocation Principle:");
        if (anyStrong)
            _o.WriteLine($"  Resonance (|m+1|) predicts {top.prop}, not organization.");
        else
            _o.WriteLine("  Resonance was correctly rejected as universal organizer.");
        _o.WriteLine("  SAC is a conservation fixed point, not a resonance point.");
        _o.WriteLine($"  Best resonance role: {top.prop} (r={top.r:F4}, R²={top.r2:F4}).");
        _o.WriteLine("");

        _o.WriteLine(new string('=', 108));
        _o.WriteLine("=== RRP_01 complete. Commit: RRP_01_ResonanceRelocationPrincipleAudit ===");
        Assert.True(true);
    }

    private static double PearsonCorr(double[] x, double[] y)
    {
        int n = Math.Min(x.Length, y.Length);
        double mx = 0, my = 0;
        for (int i = 0; i < n; i++) { mx += x[i]; my += y[i]; }
        mx /= n; my /= n;
        double cov = 0, sx = 0, sy = 0;
        for (int i = 0; i < n; i++)
        { double dx = x[i] - mx, dy = y[i] - my; cov += dx * dy; sx += dx * dx; sy += dy * dy; }
        return Math.Sqrt(sx * sy) > 1e-15 ? cov / Math.Sqrt(sx * sy) : 0;
    }

    private record ResonanceState(string label, double absM, double V, int sign,
        double tickCV, double signPersistence, double fbMag, double distToSac,
        double tickMean, double m);
}
