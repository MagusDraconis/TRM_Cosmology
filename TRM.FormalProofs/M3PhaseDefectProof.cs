using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace TRM.FormalProofs;

/// <summary>
/// Result of the phase defect minimization proof.
/// </summary>
public record PhaseDefectProofResult(
    bool Passed,
    Dictionary<int, Rational> ModeDefects,
    List<string> Counterexamples,
    string Details
);

/// <summary>
/// FP02: Verifies that the closure defect for m=3 is exactly zero and strictly minimized over qCore.
/// </summary>
public static class M3PhaseDefectProof
{
    /// <summary>
    /// Executes the phase defect minimization proof on the derived qCore domain [16, 17, 18].
    /// </summary>
    public static PhaseDefectProofResult ProvePhaseDefectMinimization(List<BigInteger> qCore, int targetShift = 3)
    {
        var modeDefects = new Dictionary<int, Rational>();
        var counterexamples = new List<string>();
        var details = new StringBuilder();

        details.AppendLine("--- FP02 PHASE CLOSURE DEFECT MINIMIZATION PROOF ---");
        details.AppendLine($"Using qCore domain: [{string.Join(", ", qCore)}]");
        details.AppendLine($"Target shift (winding cycles): {targetShift}");
        details.AppendLine("Evaluating exact defects over qCore for m = 1..5:");

        // We evaluate modes 1..5
        for (int m = 1; m <= 5; m++)
        {
            var defectSum = Rational.Zero;
            var detailsForMode = new StringBuilder();

            foreach (var q in qCore)
            {
                var omega = M3ExactDefinitions.Omega(q, m);
                var pCompatible = M3ExactDefinitions.PCompatible(q, targetShift);
                var defect = M3ExactDefinitions.PhaseDefect(q, m, pCompatible);
                var normDefect = defect / new Rational(targetShift);

                defectSum += normDefect;
                detailsForMode.Append($"    q={q}: defect={defect}, normalized={normDefect};");
            }

            var avgDefect = defectSum / new Rational(qCore.Count);
            modeDefects[m] = avgDefect;

            details.AppendLine($"  Mode m = {m} | Average Normalized Defect: {avgDefect} (~{avgDefect.ToDouble():F6})");
            details.AppendLine(detailsForMode.ToString());
        }

        var m3Defect = modeDefects[3];
        details.AppendLine();
        details.AppendLine($"m=3 defect is: {m3Defect}");

        // Verify m=3 minimizes defect and is unique
        bool isUniqueMin = true;
        foreach (var kvp in modeDefects)
        {
            int m = kvp.Key;
            var defect = kvp.Value;

            if (m != 3)
            {
                if (defect <= m3Defect)
                {
                    isUniqueMin = false;
                    counterexamples.Add($"Mode m={m} has defect {defect} which is <= m=3 defect {m3Defect}");
                }
            }
        }

        bool passed = m3Defect == Rational.Zero && isUniqueMin && counterexamples.Count == 0;
        details.AppendLine($"Unique Minimization: {(passed ? "PASS" : "FAIL")}");
        
        if (counterexamples.Count > 0)
        {
            details.AppendLine("Counterexamples found:");
            foreach (var cx in counterexamples)
                details.AppendLine($"  - {cx}");
        }
        else
        {
            details.AppendLine("No phase defect counterexamples exist inside the qCore domain.");
        }

        return new PhaseDefectProofResult(passed, modeDefects, counterexamples, details.ToString());
    }
}
