namespace TRM.Core.Shared;

public class QuantumTemporalField
{
    private readonly TemporalFluctuation _fluctuation;

    public QuantumTemporalField(TemporalFluctuation fluctuation)
    {
        _fluctuation = fluctuation;
    }

    public List<double> SampleRegion(int samples, double deltaT)
    {
        var data = new List<double>();

        for (int i = 0; i < samples; i++)
            data.Add(_fluctuation.Sample(deltaT));

        return data;
    }
}
