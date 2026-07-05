using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// G3-Observables: strong-field predictions from r_H.
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "G3")]
public class G3_Observables_Tests
{
    private readonly ITestOutputHelper _output;

    // From G3 ODE solver
    private const double RH = 2.275;     // TRM horizon (GM units)
    private const double RSCH = 2.0;     // Schwarzschild horizon
    private const double G = 1.0;
    private const double M = 1.0;

    public G3_Observables_Tests(ITestOutputHelper o) { _output = o; }

    [Fact]
    public void G3Obs_01_Photon_Sphere()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3-OBS.01 — PHOTON SPHERE");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double rPhTrm = 1.5 * RH;           // r_ph = 3r_H/2
        double rPhGr = 3.0 * RSCH / 2.0;    // r_ph = 3GM (Schwarzschild)
        double deviation = (rPhTrm / rPhGr - 1.0) * 100;

        _output.WriteLine($"  TRM photon sphere:   r_ph = {rPhTrm:F3} GM");
        _output.WriteLine($"  GR photon sphere:    r_ph = {rPhGr:F2} GM");
        _output.WriteLine($"  Deviation:           {deviation:F1}%");
        _output.WriteLine("");
        _output.WriteLine("  Detectability: photon sphere not directly observable.");
        _output.WriteLine("  Affects lensing and shadow size.");
    }

    [Fact]
    public void G3Obs_02_Shadow_Radius()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3-OBS.02 — SHADOW RADIUS (EHT)");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double shadowTrm = 1.5 * Math.Sqrt(3.0) * RH;   // (3√3/2)·r_H
        double shadowGr = 1.5 * Math.Sqrt(3.0) * RSCH;  // (3√3/2)·2GM = 3√3·GM
        double deviation = (shadowTrm / shadowGr - 1.0) * 100;

        _output.WriteLine($"  TRM shadow radius:   r_sh = {shadowTrm:F3} GM");
        _output.WriteLine($"  GR shadow radius:    r_sh = {shadowGr:F2} GM");
        _output.WriteLine($"  Deviation:           {deviation:F1}%");
        _output.WriteLine("");

        double devAs = deviation * 42.0 / 100;  // M87* shadow ~42 μas
        _output.WriteLine($"  EHT M87* shadow:     ~42 μas");
        _output.WriteLine($"  TRM shift:           ~{devAs:F1} μas");
        _output.WriteLine($"  EHT resolution:      ~20 μas");
        _output.WriteLine($"  EHT uncertainty:     ~7-14% (current)");
        _output.WriteLine("");

        string detectability = Math.Abs(deviation) < 7 ? "BELOW CURRENT DETECTION" :
                               Math.Abs(deviation) < 14 ? "MARGINALLY DETECTABLE" :
                               "DETECTABLE (ngEHT)";
        _output.WriteLine($"  Classification: {detectability}");
    }

    [Fact]
    public void G3Obs_03_ISCO()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3-OBS.03 — ISCO");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double iscoTrm = 3.0 * RH;     // r_ISCO = 3·r_H
        double iscoGr = 6.0;           // r_ISCO = 6GM (Schwarzschild)
        double deviation = (iscoTrm / iscoGr - 1.0) * 100;

        _output.WriteLine($"  TRM ISCO:            r_ISCO = {iscoTrm:F3} GM");
        _output.WriteLine($"  GR ISCO:             r_ISCO = {iscoGr:F2} GM");
        _output.WriteLine($"  Deviation:           {deviation:F1}%");
        _output.WriteLine("");
        _output.WriteLine("  Detectability: ISCO measured via accretion disk");
        _output.WriteLine("  spectroscopy (Fe Kα line) and QPO frequencies.");
        _output.WriteLine($"  {Math.Abs(deviation):F0}% shift in ISCO → shift in Fe line");
        _output.WriteLine("  redshift and QPO frequency.");
    }

    [Fact]
    public void G3Obs_04_Ringdown()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3-OBS.04 — RINGDOWN FREQUENCY");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // Fundamental QNM: ω ≈ 0.374/GM for Schwarzschild
        // Scales as 1/r_H
        double omegaTrm = 0.374 * RSCH / RH;
        double omegaGr = 0.374;
        double deviation = (omegaTrm / omegaGr - 1.0) * 100;

        _output.WriteLine($"  TRM ω_QNM:           ω·GM = {omegaTrm:F4}");
        _output.WriteLine($"  GR ω_QNM:            ω·GM = {omegaGr:F3}");
        _output.WriteLine($"  Deviation:           {deviation:F1}%");
        _output.WriteLine("");
        _output.WriteLine("  For M=10 M_sun:  f_GR ≈ 1.2 kHz → f_TRM ≈ {1.2 * omegaTrm / omegaGr:F1} kHz");
        _output.WriteLine("  For M=10⁶ M_sun: f_GR ≈ 12 mHz → f_TRM ≈ {12 * omegaTrm / omegaGr:F0} mHz");
        _output.WriteLine("");
        _output.WriteLine("  LIGO: sensitive to ~50-1000 Hz (stellar-mass)");
        _output.WriteLine("  LISA: sensitive to ~0.1-100 mHz (supermassive)");
        _output.WriteLine($"  {Math.Abs(deviation):F0}% shift — detectable with 3G detectors.");
    }

    [Fact]
    public void G3Obs_05_Summary()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3 OBSERVABLES — SUMMARY");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        double dev = (RH / RSCH - 1.0) * 100;

        _output.WriteLine($"  r_H = {RH:F3} GM (GR: {RSCH:F2} GM, deviation: {dev:F1}%)");
        _output.WriteLine("");
        _output.WriteLine($"  Observable          GR value      TRM value      Deviation");
        _output.WriteLine($"  ──────────          ────────      ─────────      ─────────");
        _output.WriteLine($"  Photon sphere       3.00 GM       {1.5*RH:F2} GM        {dev:F1}%");
        _output.WriteLine($"  Shadow radius       5.20 GM       {1.5*Math.Sqrt(3)*RH:F2} GM        {dev:F1}%");
        _output.WriteLine($"  ISCO                6.00 GM       {3*RH:F2} GM        {dev:F1}%");
        _output.WriteLine($"  QNM (ℓ=2,n=0)      0.374/GM      {0.374*RSCH/RH:F3}/GM      {dev:F1}%");
        _output.WriteLine("");
        _output.WriteLine($"  All deviations: ±{dev:F1}% (scale linearly with r_H).");
        _output.WriteLine("");
        string cls = Math.Abs(dev) < 5 ? "BELOW CURRENT DETECTION" :
                     Math.Abs(dev) < 15 ? "MARGINALLY DETECTABLE (EHT/LIGO)" :
                     "CLEARLY DETECTABLE";
        _output.WriteLine($"  Classification: {cls}");
        _output.WriteLine("");
        _output.WriteLine("  ⚠ These predictions assume scalar approximation.");
        _output.WriteLine("  Full tensor strong-field solution may modify values.");
    }
}
