using System;
using System.Collections.Generic;
using System.Text;
using TRM.QuantumCore.Fields;

namespace TRM.Simulations.Experiments;

public class HeisenbergTest
{
    public static double Run(int samples, double deltaT, QuantumTemporalField field)
    {
        var data = field.SampleRegion(samples, deltaT);

        double mean = data.Average();
        double variance = data.Select(v => (v - mean) * (v - mean)).Average();

        return variance * deltaT;
    }
}
