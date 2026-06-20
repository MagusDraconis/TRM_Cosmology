using System;
using System.Collections.Generic;
using System.Text;
using TRM.QuantumCore.Fluctuations;

namespace TRM.QuantumCore.Fields;

public class QuantumTemporalField
{
    private readonly TemporalFluctuation fluctuation;

    public QuantumTemporalField(TemporalFluctuation fluct)
    {
        fluctuation = fluct;
    }

    public List<double> SampleRegion(int samples, double deltaT)
    {
        var data = new List<double>();

        for (int i = 0; i < samples; i++)
            data.Add(fluctuation.Sample(deltaT));

        return data;
    }
}
