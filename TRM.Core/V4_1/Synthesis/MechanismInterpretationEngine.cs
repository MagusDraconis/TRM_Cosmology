namespace TRM.Core.V4_1.Synthesis;

public static class MechanismInterpretationEngine
{
    public static List<MechanismInterpretation> Interpret(MechanismExtractionResult result)
    {
        var interps = new List<MechanismInterpretation>();

        var top3 = result.Contributions.Take(3).ToList();
        interps.Add(new MechanismInterpretation(
            $"The current D=3 advantage is driven primarily by {top3[0].MechanismName}, " +
            $"{top3[1].MechanismName}, and {top3[2].MechanismName} " +
            $"(combined contribution: {result.D3AdvantageFromTop3:F2}).",
            result.D3AdvantageFromTop3 > 0.6 ? "SUPPORTED" : "CONDITIONAL",
            new() { ["top3_score"] = result.D3AdvantageFromTop3 }));

        interps.Add(new MechanismInterpretation(
            result.WinStyle == "compromise-optimum"
                ? "D=3 behaves as a compromise optimum across multiple mechanisms rather than a single dominant channel."
                : "D=3 wins through a small set of dominant mechanisms.",
            result.WinStyle == "compromise-optimum" ? "CONDITIONAL" : "SUPPORTED",
            new() { ["win_style"] = result.WinStyle == "compromise-optimum" ? 1 : 0 }));

        int fragileCount = result.FragileMechanisms.Count;
        interps.Add(new MechanismInterpretation(
            $"{fragileCount}/{result.Contributions.Count} mechanisms are parameter-sensitive — " +
            (fragileCount > 4
                ? "D=3 advantage is fragile to parameter variation."
                : "most mechanisms remain robust under parameter variation."),
            fragileCount <= 3 ? "SUPPORTED" : fragileCount <= 5 ? "CONDITIONAL" : "INCONCLUSIVE",
            new() { ["fragile_count"] = fragileCount }));

        var (d3Over2, d3Over4) = MechanismExtractionEngine.PairwiseMargins();
        interps.Add(new MechanismInterpretation(
            $"D=3 beats D=2 primarily via spectral/sync/isotropy mechanisms (score: {d3Over2:F2}); " +
            $"D=3 beats D=4 primarily via radial/defect mechanisms (score: {d3Over4:F2}).",
            "CONDITIONAL",
            new() { ["d3_over_2"] = d3Over2, ["d3_over_4"] = d3Over4 }));

        return interps;
    }
}
