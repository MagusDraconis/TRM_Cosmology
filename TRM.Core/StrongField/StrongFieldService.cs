namespace TRM.Core.StrongField;

public sealed class StrongFieldService : IStrongFieldService
{
    private const double G = 6.67430e-11;
    private const double C = 299_792_458.0;
    private const double SolarMass = 1.98847e30;

    public StrongFieldResult ComputeRegularSchwarzschild(StrongFieldInput input)
    {
        var mass = Math.Max(0.1, input.MassSolarUnits) * SolarMass;
        var rs = 2.0 * G * mass / (C * C);
        var rc = Math.Max(0.02, input.CoreScaleFactor) * rs;
        var samples = Math.Clamp(input.Samples, 50, 400);
        var maxR = Math.Max(2.0, input.MaxRadiusInSchwarzschildUnits) * rs;
        var rMin = Math.Max(rs * 1e-4, 1.0);
        var dr = (maxR - rMin) / (samples - 1);

        var profile = new List<StrongFieldPoint>(samples);
        double? inner = null;
        double? outer = null;
        double? prevG00 = null;
        var prevR = rMin;

        for (var i = 0; i < samples; i++)
        {
            var r = rMin + (i * dr);
            var regularMassFactor = 1.0 - Math.Exp(-(r * r * r) / (rc * rc * rc));
            var g00 = 1.0 - (rs / r) * regularMassFactor;
            var grr = 1.0 / Math.Max(1e-12, g00);
            var schwarzschildG00 = 1.0 - rs / r;
            var schwarzschildGrr = 1.0 / Math.Max(1e-12, schwarzschildG00);

            if (prevG00.HasValue && Math.Sign(prevG00.Value) != Math.Sign(g00))
            {
                var h = InterpolateZero(prevR, r, prevG00.Value, g00);
                if (!inner.HasValue)
                {
                    inner = h;
                }
                else
                {
                    outer = h;
                }
            }

            profile.Add(new StrongFieldPoint(r, r / rs, g00, grr, schwarzschildG00, schwarzschildGrr));
            prevG00 = g00;
            prevR = r;
        }

        var centerG00 = 1.0; // regularized core limit for this model

        return new StrongFieldResult(
            Profile: profile,
            SchwarzschildRadiusMeters: rs,
            CoreRadiusMeters: rc,
            InnerHorizonMeters: inner,
            OuterHorizonMeters: outer,
            CenterG00: centerG00,
            HasFiniteCore: !double.IsNaN(centerG00) && !double.IsInfinity(centerG00));
    }

    private static double InterpolateZero(double r1, double r2, double f1, double f2)
    {
        var t = f1 / (f1 - f2);
        return r1 + (t * (r2 - r1));
    }
}
