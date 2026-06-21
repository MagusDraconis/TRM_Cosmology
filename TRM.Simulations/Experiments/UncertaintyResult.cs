using System;
using System.Collections.Generic;
using System.Linq;

namespace TRM.Simulations.Experiments;

public class UncertaintyResult
{
    public double DeltaT { get; set; }
    public double MeanTemporalFluctuation { get; set; }
    public double StdTemporalFluctuation { get; set; }
    public double DeltaE { get; set; }
    public double Product { get; set; }
}


