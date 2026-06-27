
using System;

namespace TRM.Core;

public static class TrmDerivedParameters
{
    // --------------------------------------------------
    // Fundamental acceleration scale
    // --------------------------------------------------
    public static double GetA0_Ms2()
        => TrmModelParameters.DefaultA0_Ms2;

    public static double GetA0_Cgs()
        => TrmModelParameters.DefaultA0_Cgs;

    // --------------------------------------------------
    // Phase / synchronization coupling
    // --------------------------------------------------
    public static double GetPhiBeta()
        => TrmModelParameters.DefaultPhiBeta;

    // Optional future overload:
    public static double GetPhiBeta(double coherence, double stability = 1.0)
    {
        double beta = TrmModelParameters.DefaultPhiBeta;

        // aktuell noch konservativ: nur sanfte Modulation
        double factor = 1.0 + 0.0 * coherence + 0.0 * stability;

        return beta * factor;
    }

    // --------------------------------------------------
    // Radial regime correction
    // --------------------------------------------------
    public static double GetRegimeGamma()
        => TrmModelParameters.DefaultRegimeGamma;

    public static double GetRegimeGamma(double radialContrast, double coherence = 1.0)
    {
        double gamma = TrmModelParameters.DefaultRegimeGamma;

        // Platzhalter für spätere Herleitung
        double factor = 1.0 + 0.0 * radialContrast + 0.0 * coherence;

        return gamma * factor;
    }

    // --------------------------------------------------
    // Bulge treatment
    // --------------------------------------------------
    public static double GetBulgeSofteningKpc()
        => TrmModelParameters.DefaultBulgeSofteningKpc;

    public static double GetBulgeSofteningKpc(double diskScaleLengthKpc)
    {
        if (diskScaleLengthKpc <= 0)
            return TrmModelParameters.DefaultBulgeSofteningKpc;

        // konservative abgeleitete Variante:
        // nicht kleiner als Default, aber diskabhängig skalierbar
        double derived = 0.20 * diskScaleLengthKpc;

        return Math.Max(
            TrmModelParameters.DefaultBulgeSofteningKpc,
            derived
        );
    }

    // --------------------------------------------------
    // Radial transition regime
    // --------------------------------------------------
    public static double GetInnerRegimeStart_Rd()
        => TrmModelParameters.InnerRegimeStart_Rd;

    public static double GetInnerRegimeEnd_Rd()
        => TrmModelParameters.InnerRegimeEnd_Rd;

    public static (double StartRd, double EndRd) GetInnerRegimeBounds()
        => (
            TrmModelParameters.InnerRegimeStart_Rd,
            TrmModelParameters.InnerRegimeEnd_Rd
        );

    // spätere adaptive Variante
    public static (double StartRd, double EndRd) GetInnerRegimeBounds(double coherenceProxy)
    {
        double start = TrmModelParameters.InnerRegimeStart_Rd;
        double end = TrmModelParameters.InnerRegimeEnd_Rd;

        // aktuell noch fix, aber struktur vorbereitet
        double adjustedStart = start;
        double adjustedEnd = end;

        return (adjustedStart, adjustedEnd);
    }

    // --------------------------------------------------
    // Numerical stability / guards
    // --------------------------------------------------
    public static double GetMinimumIntegrationWeight()
        => TrmModelParameters.MinimumIntegrationWeight;

    public static double GetMinCorrectionFactor()
        => TrmModelParameters.MinCorrectionFactor;

    public static double GetMaxCorrectionFactor()
        => TrmModelParameters.MaxCorrectionFactor;

    public static double GetMinLocalToFullRatio()
        => TrmModelParameters.MinLocalToFullRatio;

    public static double GetMaxLocalToFullRatio()
        => TrmModelParameters.MaxLocalToFullRatio;

    // --------------------------------------------------
    // Convenience helpers
    // --------------------------------------------------
    public static double ClampCorrectionFactor(double value)
    {
        return Math.Clamp(
            value,
            GetMinCorrectionFactor(),
            GetMaxCorrectionFactor()
        );
    }

    public static double ClampLocalToFullRatio(double value)
    {
        return Math.Clamp(
            value,
            GetMinLocalToFullRatio(),
            GetMaxLocalToFullRatio()
        );
    }

    public static double ComputeInnerWeight(double xOverRd)
    {
        double start = GetInnerRegimeStart_Rd();
        double end = GetInnerRegimeEnd_Rd();

        if (xOverRd <= start)
            return 1.0;

        if (xOverRd >= end)
            return 0.0;

        double t = (xOverRd - start) / (end - start);

        // quadratischer Abfall
        double w = 1.0 - t * t;

        return Math.Clamp(w, 0.0, 1.0);
    }
}