using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace TRM.FormalProofs;

/// <summary>
/// Result of the exact qCore proof execution.
/// </summary>
public record QCoreProofResult(
    bool Passed,
    List<BigInteger> DerivedQCore,
    string Details
);

/// <summary>
/// FP01: Derives the qCore interval exactly using rational arithmetic.
/// </summary>
public static class M3ExactQCoreProof
{
    /// <summary>
    /// Executes the exact rational derivation of the qCore interval for m=3.
    /// </summary>
    public static QCoreProofResult ProveQCore(BigInteger qMax)
    {
        var derived = new List<BigInteger>();
        var details = new StringBuilder();

        var omegaMin = new Rational(116, 100);
        var omegaMax = new Rational(119, 100);
        var gammaMin = new Rational(84, 100);
        var gammaMax = new Rational(86, 100);

        details.AppendLine("--- FP01 EXACT QCORE DERIVATION PROOF ---");
        details.AppendLine($"Using exact rational bounds for m=3:");
        details.AppendLine($"  Omega band: [{omegaMin}, {omegaMax}]");
        details.AppendLine($"  Gamma band: [{gammaMin}, {gammaMax}]");
        details.AppendLine($"  Domain search limit: q <= {qMax}");

        for (BigInteger q = 2; q <= qMax; q++)
        {
            var omega = M3ExactDefinitions.Omega(q, 3);
            var gamma = M3ExactDefinitions.Gamma(q, 3);

            bool inOmega = omega >= omegaMin && omega <= omegaMax;
            bool inGamma = gamma >= gammaMin && gamma <= gammaMax;

            if (inOmega && inGamma)
            {
                derived.Add(q);
                details.AppendLine($"  Admissible q = {q} | Omega = {omega} (~{omega.ToDouble():F6}) | Gamma = {gamma} (~{gamma.ToDouble():F6})");
            }
        }

        var expected = new List<BigInteger> { 16, 17, 18 };
        bool matched = derived.Count == expected.Count;
        if (matched)
        {
            for (int i = 0; i < derived.Count; i++)
            {
                if (derived[i] != expected[i])
                {
                    matched = false;
                    break;
                }
            }
        }

        bool passed = matched;
        details.AppendLine();
        details.AppendLine($"Expected qCore: [{string.Join(", ", expected)}]");
        details.AppendLine($"Actual qCore:   [{string.Join(", ", derived)}]");
        details.AppendLine($"Proof Match:    {(passed ? "PASS" : "FAIL")}");

        return new QCoreProofResult(passed, derived, details.ToString());
    }
}
