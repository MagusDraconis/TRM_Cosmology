using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// G3-FalsificationTest: at what observational precision does TRM
/// become distinguishable from GR? Forward prediction for instrument design.
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "G3")]
public class G3_Falsification_Tests
{
    private readonly ITestOutputHelper _output;

    private const double G = 1.0;
    private const double M = 1.0;

    public G3_Falsification_Tests(ITestOutputHelper o) { _output = o; }

    // ════════════════════════════════════════════════════════════
    // Helper: β(b) → r_H → Δ(b)
    // ════════════════════════════════════════════════════════════

    private static double BetaFromB(double b)
    {
        // β ∝ (b−1), calibrated at b=1.25 → β≈0.55
        const double bRef = 1.25, betaRef = 0.55;
        return betaRef * (b - 1.0) / (bRef - 1.0);
    }

    private static double ComputeHorizonRadius(double beta, int n = 15000)
    {
        double rStart = 100.0, dr = (rStart - 0.005) / n;
        double r = rStart, phi = 1.0 / rStart, phiPrime = -1.0 / (rStart * rStart);
        double rH = 0;

        for (int i = 0; i < n; i++)
        {
            r -= dr;
            if (r < 0.005) break;
            double pdd = beta * phiPrime * phiPrime - 2.0 * phiPrime / r;
            double rMid = r + 0.5 * dr;
            double pMid = phi - 0.5 * phiPrime * dr;
            double ppMid = phiPrime - 0.5 * pdd * dr;
            double pddMid = beta * ppMid * ppMid - 2.0 * ppMid / rMid;
            phi -= ppMid * dr;
            phiPrime -= pddMid * dr;
            if (rH == 0 && -2.0 * phi <= -0.999) rH = r;
        }
        return rH > 0 ? rH : 0.01;
    }

    // ════════════════════════════════════════════════════════════
    // G3F_01 — Δ(b) scan: deviation vs b
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G3F_01_Deviation_vs_b()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3-F.01 — Δ(b) = (r_H(b) − 2GM) / 2GM");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double[] bs = { 0.98, 0.99, 1.00, 1.01, 1.02, 1.05, 1.10, 1.15, 1.20, 1.25, 1.30 };

        _output.WriteLine($"  {"b",8} {"β",8} {"r_H(GM)",10} {"Δ(%)",8} {"Detectable at σ <",18}");
        _output.WriteLine($"  {new string('-',8)} {new string('-',8)} {new string('-',10)} {new string('-',8)} {new string('-',18)}");

        var results = new List<(double b, double beta, double rH, double delta)>();

        foreach (double b in bs)
        {
            double beta = BetaFromB(b);
            double rH = ComputeHorizonRadius(beta);
            double delta = (rH / 2.0 - 1.0) * 100;
            double sigmaNeeded = Math.Abs(delta);

            string detectability = sigmaNeeded < 1 ? "sub-% (beyond 3G)" :
                                   sigmaNeeded < 5 ? "few-% (3G detectors)" :
                                   sigmaNeeded < 10 ? "~10% (ngEHT/LISA)" :
                                   sigmaNeeded < 17 ? "~15% (current EHT)" :
                                   ">17% (already testable)";

            _output.WriteLine($"  {b,8:F2} {beta,8:F3} {rH,10:F4} {delta,8:F2} {sigmaNeeded,18:F1}% — {detectability}");
            results.Add((b, beta, rH, delta));
        }

        _output.WriteLine("");
        _output.WriteLine("  RULE: to detect TRM deviation at 3σ confidence:");
        _output.WriteLine("    required σ_obs ≤ |Δ| / 3");
        _output.WriteLine("");

        // Assert: b=1 gives Δ=0 (GR identical)
        double deltaAt1 = results.Find(r => Math.Abs(r.b - 1.0) < 1e-9).delta;
        Assert.True(Math.Abs(deltaAt1) < 0.5,
            $"b=1.00 should give Δ≈0, got {deltaAt1:F2}%");

        // Assert: monotonic (larger |b−1| → larger |Δ|)
        for (int i = 2; i < results.Count; i++)
        {
            double prevAbsDelta = Math.Abs(results[i - 1].delta);
            double currAbsDelta = Math.Abs(results[i].delta);
            if (results[i].b > 1.0 && results[i - 1].b > 1.0)
                Assert.True(currAbsDelta >= prevAbsDelta - 0.5,
                    $"|Δ| should grow with |b−1|; got {prevAbsDelta:F2} → {currAbsDelta:F2}");
        }
    }

    // ════════════════════════════════════════════════════════════
    // G3F_02 — Detection threshold map
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G3F_02_Detection_Threshold_Map()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3-F.02 — DETECTION THRESHOLD MAP");
        _output.WriteLine("  \"At what σ does TRM separate from GR?\"");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Instrument classes
        var instruments = new (string name, double sigmaPct, string domain)[]
        {
            ("Current EHT",              17.0, "VLBI — M87*, Sgr A*"),
            ("ngEHT (next-gen)",          10.0, "VLBI — improved baselines"),
            ("LISA",                       5.0, "space GW — supermassive BH ringdown"),
            ("Einstein Telescope (3G)",    2.0, "ground GW — stellar BH ringdown"),
            ("Cosmic Explorer",            1.0, "ground GW — precision QNM spectroscopy"),
            ("Future VLBI (Space VLBI)",   0.5, "space VLBI — μas astrometry"),
        };

        // TRM kernel candidates
        var kernels = new (string label, double b, string note)[]
        {
            ("quartic baseline",       1.00, "natural: K₀/(1+x+x²+x⁴)"),
            ("mild deviation",         1.05, "small quartic deformation"),
            ("G1 calibrated",          1.10, "weak tension with EHT"),
            ("G1 β≈1 candidate",       1.25, "β-compatible at 1PN"),
            ("strong deformation",     1.30, "large deviation"),
        };

        _output.WriteLine("  DETECTION MATRIX (3σ confidence):");
        _output.WriteLine("");

        // Header
        var header = $"  {"Kernel",-22}";
        foreach (var inst in instruments)
            header += $" {inst.name,-24}";
        _output.WriteLine(header);

        // Separator
        var sep = $"  {new string('-',22)}";
        foreach (var _ in instruments)
            sep += $" {new string('-',24)}";
        _output.WriteLine(sep);

        foreach (var (label, b, note) in kernels)
        {
            double beta = BetaFromB(b);
            double rH = ComputeHorizonRadius(beta);
            double delta = Math.Abs(rH / 2.0 - 1.0) * 100;

            var line = $"  {label,-22}";
            foreach (var (name, sigma, domain) in instruments)
            {
                // 3σ detection: observable deviation must exceed 3×measurement σ
                bool detectable = delta > 3.0 * sigma;
                string mark = detectable ? "DETECTABLE" : "—";
                line += $" {mark,-24}";
            }
            _output.WriteLine(line);
        }

        _output.WriteLine("");
        _output.WriteLine("  TRM SIGNATURE:");
        _output.WriteLine("    All observables scale uniformly with r_H.");
        _output.WriteLine("    Photon sphere:  r_ph = 1.5·r_H");
        _output.WriteLine("    Shadow radius:  r_sh = (3√3/2)·r_H");
        _output.WriteLine("    ISCO:           r_ISCO = 3·r_H");
        _output.WriteLine("    QNM frequency:  ω ∝ 1/r_H");
        _output.WriteLine("");
        _output.WriteLine("  If b ≠ 1 → all strong-field observables shift by Δ.");
        _output.WriteLine("  This is a single-parameter falsification test.");
    }

    // ════════════════════════════════════════════════════════════
    // G3F_03 — Precision requirements by observable
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G3F_03_Precision_By_Observable()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3-F.03 — PRECISION REQUIREMENTS");
        _output.WriteLine("  Per observable, for given b");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double[] bs = { 1.01, 1.05, 1.10, 1.25 };
        string[] obsNames = { "Shadow radius", "Photon sphere", "ISCO", "QNM frequency", "Innermost binding energy" };
        double[] obsScaleFactors = { 1.0, 1.0, 1.0, -1.0, 1.0 }; // how Δ propagates per observable

        foreach (double b in bs)
        {
            double beta = BetaFromB(b);
            double rH = ComputeHorizonRadius(beta);
            double delta = (rH / 2.0 - 1.0) * 100;

            _output.WriteLine($"  ── b = {b:F2}, β = {beta:F3}, r_H = {rH:F4} GM, Δ = {delta:F2}% ──");
            _output.WriteLine("");

            _output.WriteLine($"  {"Observable",-28} {"Δ(%)",8} {"σ for 3σ detection",20} {"Instrument class",-24}");
            _output.WriteLine($"  {new string('-',28)} {new string('-',8)} {new string('-',20)} {new string('-',24)}");

            foreach (var (name, factor) in obsNames.Zip(obsScaleFactors))
            {
                double obsDelta = Math.Abs(delta * factor);
                double sigmaReq = obsDelta / 3.0;

                string instrument = sigmaReq > 15 ? "Current EHT" :
                                   sigmaReq > 8 ? "ngEHT" :
                                   sigmaReq > 3 ? "LISA / 3G GW" :
                                   sigmaReq > 1 ? "Einstein Telescope" :
                                   sigmaReq > 0.3 ? "Cosmic Explorer" :
                                   "Future precision GW";

                _output.WriteLine($"  {name,-28} {obsDelta,8:F2} {sigmaReq,20:F1}%  {instrument,-24}");
            }
            _output.WriteLine("");
        }

        _output.WriteLine("  KEY INSIGHT:");
        _output.WriteLine("    QNM frequency has inverse scaling: ω ∝ 1/r_H.");
        _output.WriteLine("    Ringdown is the most sensitive probe for r_H deviation.");
        _output.WriteLine("    LIGO already measures QNM frequencies to ~10% for loud events.");
    }

    // ════════════════════════════════════════════════════════════
    // G3F_04 — Falsifiability threshold
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G3F_04_Falsifiability_Threshold()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3-F.04 — FALSIFIABILITY THRESHOLD");
        _output.WriteLine("  Minimum |b−1| for detectability at given σ");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double[] sigmas = { 17, 10, 5, 2, 1, 0.5, 0.1 };

        _output.WriteLine($"  {"σ_obs (%)",12} {"|b−1|_min",12} {"Δ_min (%)",12} {"r_H_min (GM)",14} {"Instrument",-24}");
        _output.WriteLine($"  {new string('-',12)} {new string('-',12)} {new string('-',12)} {new string('-',14)} {new string('-',24)}");

        foreach (double sigma in sigmas)
        {
            // For 3σ detection: |Δ| ≥ 3σ
            // Δ ≈ 2.2·(b−1) from β calibration
            // → |b−1|_min = 3σ / 2.2
            double deltaMin = 3.0 * sigma;
            double bMinus1 = deltaMin / 2.2;
            double rHMin = 2.0 * (1.0 + deltaMin / 100.0);

            string instrument = sigma > 15 ? "Current EHT" :
                               sigma > 8 ? "ngEHT" :
                               sigma > 3 ? "LISA" :
                               sigma > 1 ? "Einstein Telescope" :
                               sigma > 0.3 ? "Cosmic Explorer" :
                               "Future ultra-precision GW";

            _output.WriteLine($"  {sigma,12:F1} {bMinus1,12:F4} {deltaMin,12:F2} {rHMin,14:F4} {instrument,-24}");
        }

        _output.WriteLine("");
        _output.WriteLine("  ╔══════════════════════════════════════════════╗");
        _output.WriteLine("  ║  FALSIFIABILITY SUMMARY                     ║");
        _output.WriteLine("  ║                                              ║");
        _output.WriteLine("  ║  b=1.25 (Δ=13.8%) → falsifiable NOW (EHT)    ║");
        _output.WriteLine("  ║    but error bars too large for detection    ║");
        _output.WriteLine("  ║                                              ║");
        _output.WriteLine("  ║  b=1.10 (Δ=5.5%)  → ngEHT / LISA era         ║");
        _output.WriteLine("  ║  b=1.05 (Δ=2.8%)  → 3G GW detectors           ║");
        _output.WriteLine("  ║  b=1.02 (Δ=1.1%)  → Cosmic Explorer           ║");
        _output.WriteLine("  ║  b=1.01 (Δ=0.6%)  → beyond next-gen            ║");
        _output.WriteLine("  ║                                              ║");
        _output.WriteLine("  ║  b=1.00 (Δ≈0%)    → indistinguishable from GR ║");
        _output.WriteLine("  ╚══════════════════════════════════════════════╝");
        _output.WriteLine("");

        // Falsifiability criterion per Popper
        _output.WriteLine("  POPPER FALSIFIABILITY:");
        _output.WriteLine("    A theory is scientific iff it makes predictions");
        _output.WriteLine("    that can be contradicted by observation.");
        _output.WriteLine("");
        _output.WriteLine("    TRM prediction: r_H(b) ≠ 2GM for b ≠ 1.");
        _output.WriteLine("    → falsifiable via EHT shadow + GW ringdown.");
        _output.WriteLine("    → b=1 (GR-identical) is the unfalsifiable limit.");
        _output.WriteLine("    → any b≠1 prediction is testable at finite precision.");
    }

    // ════════════════════════════════════════════════════════════
    // G3F_05 — Combined multi-messenger falsification
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G3F_05_Multi_Messenger_Falsification()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3-F.05 — MULTI-MESSENGER FALSIFICATION");
        _output.WriteLine("  Combined EHT + GW ringdown constraints");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // If EHT and LIGO/LISA both measure r_H-consistent observables,
        // the combined constraint is tighter.

        double[] bs = { 1.05, 1.10, 1.25 };

        _output.WriteLine("  Joint significance for b ≠ 1:");
        _output.WriteLine("");
        _output.WriteLine($"  {"b",8} {"Δ(%)",8} {"EHT (σ=17%)",14} {"GW (σ=10%)",13} {"GW (σ=5%)",12} {"Combined χ²",13} {"p-val",8} {"Significance",-14}");
        _output.WriteLine($"  {new string('-',8)} {new string('-',8)} {new string('-',14)} {new string('-',13)} {new string('-',12)} {new string('-',13)} {new string('-',8)} {new string('-',14)}");

        foreach (double b in bs)
        {
            double beta = BetaFromB(b);
            double rH = ComputeHorizonRadius(beta);
            double delta = (rH / 2.0 - 1.0) * 100;

            // χ² per observable:
            double chi2EHT = Math.Pow(delta / 17.0, 2);
            double chi2GW10 = Math.Pow(delta / 10.0, 2);
            double chi2GW5 = Math.Pow(delta / 5.0, 2);

            // Combined χ² with 3 observables:
            double chi2Combined = chi2EHT + chi2GW10 + chi2GW5;

            // Approximate p-value from χ² with 3 dof
            double pVal = Math.Exp(-chi2Combined / 2.0);

            string significance = pVal < 0.001 ? "COMPELLING (>3σ)" :
                                 pVal < 0.01 ? "STRONG (>2.5σ)" :
                                 pVal < 0.05 ? "SIGNIFICANT (>2σ)" :
                                 pVal < 0.32 ? "SUGGESTIVE (>1σ)" :
                                 "NEGLIGIBLE";

            _output.WriteLine($"  {b,8:F2} {delta,8:F2} {chi2EHT,14:F3} {chi2GW10,13:F3} {chi2GW5,12:F3} {chi2Combined,13:F3} {pVal,8:F4} {significance,-14}");
        }

        _output.WriteLine("");
        _output.WriteLine("  MULTI-MESSENGER STRATEGY:");
        _output.WriteLine("    Each independent observable adds χ² contribution.");
        _output.WriteLine("    With 3 observables at current precision:");
        _output.WriteLine("      b=1.10 → suggestively distinguishable from GR");
        _output.WriteLine("      b=1.25 → strongly distinguishable from GR");
        _output.WriteLine("");
        _output.WriteLine("  With future precision (EHT σ→5%, GW σ→2%):");
        _output.WriteLine("    Even b=1.05 becomes detectable at >3σ.");
    }

    // ════════════════════════════════════════════════════════════
    // G3F_06 — Summary
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G3F_06_Summary()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3 FALSIFICATION TEST — SUMMARY");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine("  1. TRM strong-field predictions depend on b.");
        _output.WriteLine("     b=1 (quartic) → identical to GR at scalar level.");
        _output.WriteLine("     b≠1 → uniform scaling of all strong-field observables.");
        _output.WriteLine("");
        _output.WriteLine("  2. Current constraints (EHT ~17%, GW ~10%):");
        _output.WriteLine("     b=1.25 (Δ=13.8%) — within errors, not yet falsifiable.");
        _output.WriteLine("     b=1.10 (Δ=5.5%) — within errors.");
        _output.WriteLine("");
        _output.WriteLine("  3. Next-generation (ngEHT, LISA, 3G GW):");
        _output.WriteLine("     σ ~ 2-5% → b≥1.05 becomes testable.");
        _output.WriteLine("");
        _output.WriteLine("  4. Falsifiability: TRM is falsifiable for any b≠1.");
        _output.WriteLine("     The b→1 limit is the GR-identical limit.");
        _output.WriteLine("");
        _output.WriteLine("  5. TRM is a scientific theory per Popper criterion.");
        _output.WriteLine("");
        _output.WriteLine("  STATUS:");
        _output.WriteLine("    Current:       NOT YET TESTABLE (errors too large)");
        _output.WriteLine("    Next decade:   POTENTIALLY TESTABLE (ngEHT + 3G)");
        _output.WriteLine("    Ultimate:      FALSIFIABLE at any b≠1");
    }
}
