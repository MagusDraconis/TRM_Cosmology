using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.Simulations.Experiments;

public class CoarseGrainingTest
{
    public static double CoarseGrain(List<double> fluctuations)
    {
        return fluctuations.Average();
    }
}
