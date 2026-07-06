namespace TRM.Core.Shared;

public class TemporalFluctuation
{
    private readonly double _tP;
    private readonly Random _rng = new();

    public TemporalFluctuation(double planckTime)
    {
        _tP = planckTime;
    }

    public double Sample(double deltaT)
    {
        double baseVal = _tP / deltaT;

        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        double randStdNormal =
            Math.Sqrt(-2.0 * Math.Log(u1)) *
            Math.Sin(2.0 * Math.PI * u2);

        return baseVal * (1 + 0.1 * randStdNormal);
    }
}
