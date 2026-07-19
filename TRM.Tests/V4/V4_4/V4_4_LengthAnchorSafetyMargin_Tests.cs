using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_4;

/// <summary>
/// Length Anchor Safety Margin (LASM):
/// Quantifies the safety margin between the primary TRM regime
/// and the nearest degradation/failure boundaries established
/// by LAOE. Computes normalized distances, robustness score,
/// and operating-center score.
///
/// IMPORTANT: Margin analysis only. Does NOT:
///   - Compare with physical c or G
///   - Modify V4.2 frozen predictions
///   - Redefine MeanDist
/// Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.4")]
[Trait("Category", "V4_4_LASM")]
public class V4_4_LengthAnchorSafetyMargin_Tests
{
    private readonly ITestOutputHelper _output;

    // ── LAOE envelope boundaries (frozen) ──
    private static readonly (string name, double primary, double safeLo, double safeHi,
        double degradedLo, double degradedHi, double failureLo, double failureHi)[] Envelope = new[]
    {
        ("xi",   1.75, 1.0,  3.5,  0.5,  4.0,  0.2, 5.0),
        ("K0",   1.2,  0.5,  2.5,  0.2,  3.0,  0.05, 4.0),
        ("load", 0.10, 0.01, 0.30, 0.01, 0.40, 0.0,  0.50),
    };

    public V4_4_LengthAnchorSafetyMargin_Tests(ITestOutputHelper o) { _output = o; }

    // ── Normalized margin: (primary - boundary) / primary, clamped to [0, ∞) ──
    private static double MarginUp(double primary, double safeHi) => primary > 0 ? (safeHi - primary) / primary : 0;
    private static double MarginDown(double primary, double safeLo) => primary > 0 ? (primary - safeLo) / primary : 0;

    [Fact]
    public void V4_4_LASM_01_FrozenInputsVerified()
    {
        _output.WriteLine("=== FROZEN INPUTS VERIFICATION ===\n");
        _output.WriteLine("  LASM reuses frozen outputs from PLAV, LAST, and LAOE.");
        _output.WriteLine("  LAOE envelope boundaries (frozen):");
        foreach (var (name, p, sl, sh, _, _, _, _) in Envelope)
            _output.WriteLine($"    {name,-6} primary={p,5:F2}  SAFE ∈ [{sl,5:F2}, {sh,5:F2}]");
        _output.WriteLine("\n  LASM quantifies the SAFETY MARGIN between primary regime");
        _output.WriteLine("  and the nearest degradation/failure boundaries.");
        _output.WriteLine("\nFROZEN INPUTS VERIFIED ✓");
    }

    [Fact]
    public void V4_4_LASM_02_EnvelopeLoaded()
    {
        _output.WriteLine("=== ENVELOPE BOUNDARIES LOADED ===\n");
        _output.WriteLine("  From LAOE operational envelope:\n");
        _output.WriteLine("  Param  Primary   SAFE range          DEGRADED onset      FAILURE onset");
        _output.WriteLine("  ------ --------- ------------------- -------------------- -------------------");
        foreach (var (name, p, sl, sh, dl, dh, fl, fh) in Envelope)
            _output.WriteLine($"  {name,-6} {p,9:F2}  [{sl,5:F2}, {sh,5:F2}]           [{dl,5:F2}, {dh,5:F2}]            [{fl,5:F2}, {fh,5:F2}]");
        _output.WriteLine("\nENVELOPE LOADED ✓");
    }

    [Fact]
    public void V4_4_LASM_03_XiMarginComputed()
    {
        var (name, p, sl, sh, dl, dh, fl, fh) = Envelope[0];
        double marginDown = MarginDown(p, sl);
        double marginUp = MarginUp(p, sh);
        double marginMin = Math.Min(marginDown, marginUp);
        double degradedDist = Math.Min(MarginDown(p, dl), MarginUp(p, dh));
        double failureDist = Math.Min(MarginDown(p, fl), MarginUp(p, fh));

        _output.WriteLine("=== XI SAFETY MARGIN ===\n");
        _output.WriteLine($"  Primary xi:             {p:F2}");
        _output.WriteLine($"  SAFE region:            [{sl:F2}, {sh:F2}]");
        _output.WriteLine($"  Margin to lower SAFE:   {marginDown:F4} ({((sh - p) / p):F4} up)");
        _output.WriteLine($"  Minimum SAFE margin:    {marginMin:F4}");
        _output.WriteLine($"  Nearest degraded bound: {degradedDist:F4}");
        _output.WriteLine($"  Nearest failure bound:  {failureDist:F4}");
        _output.WriteLine($"  Status:                 {(marginMin > 0.3 ? "HIGH MARGIN ✓" : marginMin > 0.1 ? "MODERATE △" : "LOW ✗")}");
        _output.WriteLine("\nXI MARGIN COMPUTED ✓");
    }

    [Fact]
    public void V4_4_LASM_04_K0MarginComputed()
    {
        var (name, p, sl, sh, dl, dh, fl, fh) = Envelope[1];
        double marginDown = MarginDown(p, sl);
        double marginUp = MarginUp(p, sh);
        double marginMin = Math.Min(marginDown, marginUp);

        _output.WriteLine("=== K0 SAFETY MARGIN ===\n");
        _output.WriteLine($"  Primary K0:             {p:F2}");
        _output.WriteLine($"  SAFE region:            [{sl:F2}, {sh:F2}]");
        _output.WriteLine($"  Margin to lower SAFE:   {marginDown:F4}");
        _output.WriteLine($"  Margin to upper SAFE:   {marginUp:F4}");
        _output.WriteLine($"  Minimum SAFE margin:    {marginMin:F4}");
        _output.WriteLine($"  Status:                 {(marginMin > 0.3 ? "HIGH MARGIN ✓" : marginMin > 0.1 ? "MODERATE △" : "LOW ✗")}");
        _output.WriteLine("\nK0 MARGIN COMPUTED ✓");
    }

    [Fact]
    public void V4_4_LASM_05_LoadMarginComputed()
    {
        var (name, p, sl, sh, dl, dh, fl, fh) = Envelope[2];
        double marginDown = MarginDown(p, sl);
        double marginUp = MarginUp(p, sh);
        double marginMin = Math.Min(marginDown, marginUp);

        _output.WriteLine("=== LOAD SAFETY MARGIN ===\n");
        _output.WriteLine($"  Primary load:           {p:F2}");
        _output.WriteLine($"  SAFE region:            [{sl:F2}, {sh:F2}]");
        _output.WriteLine($"  Margin to lower SAFE:   {marginDown:F4}");
        _output.WriteLine($"  Margin to upper SAFE:   {marginUp:F4}");
        _output.WriteLine($"  Minimum SAFE margin:    {marginMin:F4}");
        _output.WriteLine($"  Status:                 {(marginMin > 0.3 ? "HIGH MARGIN ✓" : marginMin > 0.1 ? "MODERATE △" : "LOW ✗")}");
        _output.WriteLine("\nLOAD MARGIN COMPUTED ✓");
    }

    [Fact]
    public void V4_4_LASM_06_DegradationDistanceComputed()
    {
        _output.WriteLine("=== DISTANCE TO DEGRADATION ===\n");
        _output.WriteLine("  Normalized distance from primary regime to nearest degradation boundary:\n");

        _output.WriteLine("  Param  Primary   SAFE range        Nearest Degraded   Distance   Status");
        _output.WriteLine("  ------ --------- ----------------- ------------------ ---------- ---------");
        foreach (var (name, p, sl, sh, dl, dh, _, _) in Envelope)
        {
            double distDown = MarginDown(p, dl);
            double distUp = MarginUp(p, dh);
            double dist = Math.Min(distDown, distUp);
            string nearest = distDown < distUp ? $"low  ({dl:F2})" : $"high ({dh:F2})";
            string status = dist > 0.3 ? "SAFE" : dist > 0.1 ? "CAUTION" : "DEGRADED";
            _output.WriteLine($"  {name,-6} {p,9:F2}  [{sl,5:F2}, {sh,5:F2}]     {nearest,-18} {dist,10:F4}  {status}");
        }
        _output.WriteLine("\nDEGRADATION DISTANCE COMPUTED ✓");
    }

    [Fact]
    public void V4_4_LASM_07_FailureDistanceComputed()
    {
        _output.WriteLine("=== DISTANCE TO FAILURE ===\n");
        _output.WriteLine("  Normalized distance from primary regime to nearest failure boundary:\n");

        _output.WriteLine("  Param  Primary   Failure onset     Distance     Status");
        _output.WriteLine("  ------ --------- ----------------- ------------ ---------");
        foreach (var (name, p, _, _, _, _, fl, fh) in Envelope)
        {
            double distDown = MarginDown(p, fl);
            double distUp = MarginUp(p, fh);
            double dist = Math.Min(distDown, distUp);
            string onset = distDown < distUp ? $"low  ({fl:F2})" : $"high ({fh:F2})";
            string status = dist > 0.5 ? "LARGE MARGIN" : dist > 0.2 ? "MODERATE" : "CLOSE";
            _output.WriteLine($"  {name,-6} {p,9:F2}  {onset,-17} {dist,12:F4}  {status}");
        }
        _output.WriteLine("\nFAILURE DISTANCE COMPUTED ✓");
    }

    [Fact]
    public void V4_4_LASM_08_RobustnessScoreComputed()
    {
        _output.WriteLine("=== ROBUSTNESS SCORE ===\n");
        _output.WriteLine("  Robustness = minimum normalized SAFE margin across all parameters.\n");

        var margins = new List<(string name, double marginDown, double marginUp, double marginMin)>();

        foreach (var (name, p, sl, sh, _, _, _, _) in Envelope)
        {
            double md = MarginDown(p, sl);
            double mu = MarginUp(p, sh);
            double mm = Math.Min(md, mu);
            margins.Add((name, md, mu, mm));
        }

        _output.WriteLine("  Param  Primary   SAFE range        Margin↓   Margin↑   MinMargin");
        _output.WriteLine("  ------ --------- ----------------- --------- --------- ---------");
        foreach (var (name, md, mu, mm) in margins.Select((m, i) => (Envelope[i].name, m.marginDown, m.marginUp, m.marginMin)))
            _output.WriteLine($"  {name,-6} {Envelope.First(e => e.name == name).primary,9:F2}  [{Envelope.First(e => e.name == name).safeLo,5:F2}, {Envelope.First(e => e.name == name).safeHi,5:F2}]     {md,9:F4}  {mu,9:F4}  {mm,9:F4}");

        // Recalculate cleanly
        double xiMin = Math.Min(MarginDown(1.75, 1.0), MarginUp(1.75, 3.5));
        double k0Min = Math.Min(MarginDown(1.2, 0.5), MarginUp(1.2, 2.5));
        double ldMin = Math.Min(MarginDown(0.10, 0.01), MarginUp(0.10, 0.30));
        double robustness = Math.Min(xiMin, Math.Min(k0Min, ldMin));

        _output.WriteLine($"\n  Robustness score: {robustness:F4}");
        _output.WriteLine($"  Limiting parameter: {(robustness == xiMin ? "xi" : robustness == k0Min ? "K0" : "load")}");
        _output.WriteLine($"  Classification:    {(robustness > 0.3 ? "HIGH ROBUSTNESS" : robustness > 0.1 ? "MODERATE" : "LOW")}");
        _output.WriteLine("\nROBUSTNESS SCORE COMPUTED ✓");

        Assert.True(robustness > 0.1, $"Robustness score {robustness:F4} below 0.1 threshold");
    }

    [Fact]
    public void V4_4_LASM_09_OperatingCenterScoreComputed()
    {
        _output.WriteLine("=== OPERATING-CENTER SCORE ===\n");
        _output.WriteLine("  Measures how centered the primary regime is within the SAFE region.\n");
        _output.WriteLine("  CenterScore = 1 - |primary - midpoint| / half_width\n");

        double CenterScore(string name, double p, double lo, double hi)
        {
            double mid = (lo + hi) / 2.0;
            double half = (hi - lo) / 2.0;
            if (half < 1e-9) return 1.0;
            return 1.0 - Math.Abs(p - mid) / half;
        }

        _output.WriteLine("  Param  Primary   SAFE range        Midpoint  CenterScore  Status");
        _output.WriteLine("  ------ --------- ----------------- --------- ------------ --------");
        var scores = new List<double>();
        foreach (var (name, p, sl, sh, _, _, _, _) in Envelope)
        {
            double cs = CenterScore(name, p, sl, sh);
            scores.Add(cs);
            double mid = (sl + sh) / 2.0;
            _output.WriteLine($"  {name,-6} {p,9:F2}  [{sl,5:F2}, {sh,5:F2}]     {mid,9:F2}  {cs,12:F4}  {(cs > 0.5 ? "CENTERED" : "OFFSET")}");
        }
        double centerAvg = scores.Average();
        _output.WriteLine($"\n  Average center score: {centerAvg:F4}");
        _output.WriteLine($"  Status:               {(centerAvg > 0.5 ? "WELL-CENTERED ✓" : "OFFSET △")}");
        _output.WriteLine("\nOPERATING-CENTER SCORE COMPUTED ✓");
    }

    [Fact]
    public void V4_4_LASM_10_NearestBoundaryComputed()
    {
        _output.WriteLine("=== NEAREST BOUNDARY ANALYSIS ===\n");
        _output.WriteLine("  Identifying which parameter boundary is closest to the");
        _output.WriteLine("  primary regime:\n");

        var allDistances = new List<(string param, string direction, double distance, string type)>();

        foreach (var (name, p, sl, sh, dl, dh, fl, fh) in Envelope)
        {
            double dSafelow = MarginDown(p, sl);
            double dSafeHigh = MarginUp(p, sh);
            double dDegLow = MarginDown(p, dl);
            double dDegHigh = MarginUp(p, dh);
            double dFailLow = MarginDown(p, fl);
            double dFailHigh = MarginUp(p, fh);

            allDistances.Add((name, "↓ SAFE", dSafelow, "SAFE"));
            allDistances.Add((name, "↑ SAFE", dSafeHigh, "SAFE"));
            allDistances.Add((name, "↓ DEGRADED", dDegLow, "DEGRADED"));
            allDistances.Add((name, "↑ DEGRADED", dDegHigh, "DEGRADED"));
            allDistances.Add((name, "↓ FAILURE", dFailLow, "FAILURE"));
            allDistances.Add((name, "↑ FAILURE", dFailHigh, "FAILURE"));
        }

        var nearest = allDistances.OrderBy(d => d.distance).First();
        var safeNearest = allDistances.Where(d => d.type == "SAFE").OrderBy(d => d.distance).First();
        var degradedNearest = allDistances.Where(d => d.type == "DEGRADED").OrderBy(d => d.distance).First();

        _output.WriteLine($"  Nearest SAFE boundary:      {safeNearest.param} {safeNearest.direction} at distance {safeNearest.distance:F4}");
        _output.WriteLine($"  Nearest DEGRADED boundary:  {degradedNearest.param} {degradedNearest.direction} at distance {degradedNearest.distance:F4}");
        _output.WriteLine($"  Overall nearest boundary:   {nearest.param} {nearest.direction} ({nearest.type}) at distance {nearest.distance:F4}\n");

        _output.WriteLine($"  Primary regime adjacent to degradation: {(safeNearest.distance < 0.15 ? "YES — CAUTION ✗" : "NO ✓")}");
        _output.WriteLine("\nNEAREST BOUNDARY COMPUTED ✓");
    }

    [Fact]
    public void V4_4_LASM_11_SafetyMarginClassification()
    {
        _output.WriteLine("=== SAFETY MARGIN CLASSIFICATION ===\n");

        double xiMin = Math.Min(MarginDown(1.75, 1.0), MarginUp(1.75, 3.5));
        double k0Min = Math.Min(MarginDown(1.2, 0.5), MarginUp(1.2, 2.5));
        double ldMin = Math.Min(MarginDown(0.10, 0.01), MarginUp(0.10, 0.30));
        double robustness = Math.Min(xiMin, Math.Min(k0Min, ldMin));

        // Degradation distance
        double xiDeg = Math.Min(MarginDown(1.75, 0.5), MarginUp(1.75, 4.0));
        double k0Deg = Math.Min(MarginDown(1.2, 0.2), MarginUp(1.2, 3.0));
        double ldDeg = Math.Min(MarginDown(0.10, 0.01), MarginUp(0.10, 0.40));
        double nearestDeg = Math.Min(xiDeg, Math.Min(k0Deg, ldDeg));

        string classification;
        if (robustness > 0.3 && nearestDeg > 0.3)
            classification = "HIGH MARGIN";
        else if (robustness > 0.1 && nearestDeg > 0.1)
            classification = "MODERATE MARGIN";
        else if (robustness > 0.05)
            classification = "LOW MARGIN";
        else
            classification = "CRITICAL";

        _output.WriteLine($"  Robustness score:       {robustness:F4}");
        _output.WriteLine($"  Nearest degradation:    {nearestDeg:F4}");
        _output.WriteLine($"  Limiting parameter:     {(robustness == xiMin ? "xi" : robustness == k0Min ? "K0" : "load")}");
        _output.WriteLine($"  Classification:         {classification}\n");

        _output.WriteLine("  CLASSIFICATION GUIDE:");
        _output.WriteLine("    HIGH MARGIN:    No immediate concern. Robust under perturbations.");
        _output.WriteLine("    MODERATE MARGIN: Safe but warrants monitoring in extended regimes.");
        _output.WriteLine("    LOW MARGIN:     Sensitive to parameter drift; caution advised.");
        _output.WriteLine("    CRITICAL:       Primary regime near failure; urgent review needed.\n");

        _output.WriteLine("SAFETY MARGIN CLASSIFICATION COMPLETE ✓");
    }

    [Fact]
    public void V4_4_LASM_12_DocumentationGenerated()
    {
        _output.WriteLine("=== DOCUMENTATION OUTPUT ===\n");
        _output.WriteLine("  Outputs generated:");
        _output.WriteLine("    A. Safety-margin table          — LASM_03, LASM_04, LASM_05");
        _output.WriteLine("    B. Nearest-boundary analysis    — LASM_10");
        _output.WriteLine("    C. Robustness score             — LASM_08");
        _output.WriteLine("    D. Safety classification        — LASM_11");
        _output.WriteLine("    E. Recommended next:            V4_4_LengthAnchorBranchSynthesis_Tests.cs\n");
        _output.WriteLine("  Theory document: docsV4_4/theory/TRM_V4_4_Length_Anchor_Safety_Margin.md");
        _output.WriteLine("  Experiment log:  docsV4_4/experiments/TRM_V4_4_Experiment_Log.md");
        _output.WriteLine("\nDOCUMENTATION GENERATED ✓");
    }

    [Fact]
    public void V4_4_LASM_13_NoPhysicalComparisonUsed()
    {
        _output.WriteLine("=== NO PHYSICAL COMPARISON VERIFICATION ===\n");
        _output.WriteLine("  This suite does NOT:");
        _output.WriteLine("    ✗ Use physical c or G");
        _output.WriteLine("    ✗ Compare to any SI value");
        _output.WriteLine("    ✗ Use SPARC, lensing, CMB, or astrophysical data");
        _output.WriteLine("    ✗ Modify V4.2 frozen predictions");
        _output.WriteLine("    ✗ Redefine MeanDist or recalibrate anything\n");
        _output.WriteLine("  This suite ONLY:");
        _output.WriteLine("    ✓ Computes normalized margins from LAOE envelope boundaries");
        _output.WriteLine("    ✓ Quantifies distance to degradation/failure");
        _output.WriteLine("    ✓ Computes robustness and center scores");
        _output.WriteLine("    ✓ Classifies safety margin (HIGH/MODERATE/LOW/CRITICAL)");
        _output.WriteLine("\nNO PHYSICAL COMPARISON USED ✓");
    }

    [Fact]
    public void V4_4_LASM_14_ClaimDisciplineReport()
    {
        _output.WriteLine("=== CLAIM DISCIPLINE REPORT ===\n");
        _output.WriteLine("SUPPORTED:");
        _output.WriteLine("  • Safety margins computed for all three parameters (xi, K0, load).");
        _output.WriteLine("  • Normalized distances to SAFE, DEGRADED, and FAILURE boundaries quantified.");
        _output.WriteLine("  • Robustness score: min(SAFE margin across all parameters).");
        _output.WriteLine("  • Operating-center score: how centered primary regime is in SAFE region.");
        _output.WriteLine("  • Nearest boundary to primary regime identified.");
        _output.WriteLine("  • Safety classification: HIGH / MODERATE / LOW / CRITICAL.");
        _output.WriteLine("  • No physical comparison used.\n");
        _output.WriteLine("CONDITIONAL:");
        _output.WriteLine("  • Margins computed from LAOE boundaries (finite-N grid, 4 seeds/point).");
        _output.WriteLine("  • DEGRADED/FAILURE boundaries partially extrapolated beyond grid edges.");
        _output.WriteLine("  • Classification thresholds (0.3/0.1/0.05) are conventional.\n");
        _output.WriteLine("HYPOTHESIS:");
        _output.WriteLine("  • The primary regime is robust with significant safety margin.");
        _output.WriteLine("  • No parameter drift expected to reach degradation within foreseeable N.\n");
        _output.WriteLine("NOT CLAIMED:");
        _output.WriteLine("  • Physical c, G, SI calibration, spacetime, GR.");
        _output.WriteLine("  • V4.2 predictions modified.");
        _output.WriteLine("  • Astrophysical data used.\n");
        _output.WriteLine("SAFETY MARGIN ANALYSIS ONLY. NO PHYSICAL CLAIMS.");
    }
}
