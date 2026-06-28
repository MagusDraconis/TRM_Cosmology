using System;
using System.Collections.Generic;
using System.Text;
using TRM.QuantumCore.Planck;

namespace TRM.Tests.QuantumTests;


public class GammaSummary
{
    public double Gamma { get; set; }

    public double MeanScaleError { get; set; }
    public double StdScaleError { get; set; }

    public double MeanSpreadError { get; set; }
    public double StdSpreadError { get; set; }

    public double MeanTickScore { get; set; }
    public double StdTickScore { get; set; }
}

public class ActionGammaSummary
{
    public double Gamma { get; set; }

    public double MeanActionError { get; set; }
    public double StdActionError { get; set; }

    public double MeanActionSpread { get; set; }
    public double StdActionSpread { get; set; }

    public double MeanActionScore { get; set; }
    public double StdActionScore { get; set; }
}
public class ResonanceSummary
{
    public double GammaOmega { get; set; }
    public double MeanResponse { get; set; }
    public double StdResponse { get; set; }
    public double MeanInternalSpread { get; set; }
}
public class ResonanceMultiSummary
{
    public double GammaOmega { get; set; }

    public double MeanTemporalResponse { get; set; }
    public double StdTemporalResponse { get; set; }
    public double MeanDeltaEResponse { get; set; }
    public double MeanProductResponse { get; set; }

    public double SeedSpreadTemporalMean { get; set; }
    public double SeedSpreadTemporalStd { get; set; }
    public double SeedSpreadDeltaE { get; set; }
    public double SeedSpreadProduct { get; set; }
}
public class PhaseLockSummary
{
    public double Gamma { get; set; }
    public double MeanCos { get; set; }
    public double MeanSin { get; set; }
    public double LockStrength { get; set; }
}

public class PhaseModeSummary
{
    public double Gamma { get; set; }
    public double MeanCos { get; set; }
    public double MeanSin { get; set; }
    public double LockStrength { get; set; }
    public int LockOrder { get; set; }
    public double ModeScore { get; set; }
}

public class PhaseTickSummary
{
    public double Gamma { get; set; }
    public double MeanDeltaT { get; set; }
    public double StdDeltaT { get; set; }
    public double MeanError { get; set; }
    public double StdError { get; set; }
    public double PhaseScore { get; set; }
}


public class PhaseSyncSummary
{
    public double Gamma { get; set; }
    public double MeanOrder { get; set; }
    public double StdOrder { get; set; }

}