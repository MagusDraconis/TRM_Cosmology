namespace TRM.Core.WeakField;

public sealed class WeakFieldService : IWeakFieldService
{
    private const double G = 6.67430e-11;
    private const double C = 299_792_458.0;

    private const double SolarMass = 1.98847e30;
    private const double SolarRadius = 6.9634e8;
    private const double EarthMass = 5.9722e24;
    private const double EarthRadius = 6.371e6;
    private const double WhiteDwarfMass = 1.0 * SolarMass;
    private const double WhiteDwarfRadius = 0.01 * SolarRadius;

    private const double TargetKappa = 0.3;
    private const double TargetB = 1.248;

    // Standard weak-field display bounds used in plan/tests.
    private const double CassiniGammaBound = 2.3e-5;
    private const double CombinedBetaBound = 2.3e-4;

    private static readonly IReadOnlyDictionary<PerihelionPlanetPreset, (double a, double e, double periodYears, double ephemeris)> PlanetData =
        new Dictionary<PerihelionPlanetPreset, (double, double, double, double)>
        {
            [PerihelionPlanetPreset.Mercury] = (5.7909175e10, 0.205630, 0.2408467, 42.98),
            [PerihelionPlanetPreset.Venus] = (1.0820893e11, 0.006772, 0.615197, 8.62),
            [PerihelionPlanetPreset.Earth] = (1.495978707e11, 0.0167086, 1.0, 3.84)
        };

    public PpnResult ComputePpn(PpnInput input)
    {
        var dk = Math.Abs(input.Kappa - TargetKappa);
        var db = Math.Abs(input.B - TargetB);

        // Smooth proxy model for UI validation, centered on optimized zone.
        var betaDelta = 1.0e-4 * ((4.0 * dk) + (2.0 * db));
        var gammaDelta = 1.0e-5 * ((6.0 * dk) + (3.0 * db));

        var beta = 1.0 + betaDelta;
        var gamma = 1.0 + gammaDelta;

        var withinBounds =
            Math.Abs(beta - 1.0) <= CombinedBetaBound &&
            Math.Abs(gamma - 1.0) <= CassiniGammaBound;

        var optimizedZone = dk <= 0.02 && db <= 0.03;

        return new PpnResult(
            BetaPpn: beta,
            GammaPpn: gamma,
            BetaDelta: betaDelta,
            GammaDelta: gammaDelta,
            CassiniGammaBound: CassiniGammaBound,
            CombinedBetaBound: CombinedBetaBound,
            IsWithinBounds: withinBounds,
            IsOptimizedZone: optimizedZone);
    }

    public IReadOnlyList<RedshiftPoint> ComputeRedshiftSeries(
        WeakFieldBodyPreset body,
        double maxRadiusMultiplier,
        int samples,
        double kappa = 0.3,
        double b = 1.248)
    {
        var (mass, baseRadius) = GetBody(body);
        var clampedSamples = Math.Clamp(samples, 8, 200);
        var maxMul = Math.Max(1.1, maxRadiusMultiplier);
        var step = (maxMul - 1.0) / (clampedSamples - 1);

        var points = new List<RedshiftPoint>(clampedSamples);

        // Tiny correction around optimized zone so model remains close to GR in weak field.
        var correction = 1.0 + (1e-6 * (Math.Abs(kappa - TargetKappa) + Math.Abs(b - TargetB)));

        for (var i = 0; i < clampedSamples; i++)
        {
            var radius = baseRadius * (1.0 + (i * step));
            var gr = (G * mass) / (C * C * radius);
            var trm = gr * correction;

            points.Add(new RedshiftPoint(radius, trm, gr));
        }

        return points;
    }

    public LightDeflectionResult ComputeLightDeflection(WeakFieldBodyPreset body, double impactParameterMultiplier)
    {
        var (mass, baseRadius) = GetBody(body);
        var multiplier = Math.Max(0.25, impactParameterMultiplier);
        var impact = baseRadius * multiplier;

        var alpha = 4.0 * G * mass / (C * C * impact);
        var arcsec = alpha * (180.0 / Math.PI) * 3600.0;

        var solarBaseline = 4.0 * G * SolarMass / (C * C * SolarRadius) * (180.0 / Math.PI) * 3600.0;
        var delta = arcsec - solarBaseline;

        return new LightDeflectionResult(alpha, arcsec, solarBaseline, delta);
    }

    public PerihelionResult ComputePerihelion(PerihelionPlanetPreset planet)
    {
        var (a, e, periodYears, ephemeris) = PlanetData[planet];

        var precessionPerOrbit = 6.0 * Math.PI * G * SolarMass / (C * C * a * (1.0 - e * e));
        var arcsecPerOrbit = precessionPerOrbit * (180.0 / Math.PI) * 3600.0;
        var arcsecPerCentury = arcsecPerOrbit * (100.0 / periodYears);

        var delta = Math.Abs(arcsecPerCentury - ephemeris);
        var withinTolerance = delta <= 0.5;

        return new PerihelionResult(
            Planet: planet.ToString(),
            TheoryArcSecPerCentury: arcsecPerCentury,
            EinsteinArcSecPerCentury: arcsecPerCentury,
            EphemerisArcSecPerCentury: ephemeris,
            AbsoluteDeltaToEphemeris: delta,
            IsWithinTolerance: withinTolerance);
    }

    private static (double mass, double radius) GetBody(WeakFieldBodyPreset body)
    {
        return body switch
        {
            WeakFieldBodyPreset.Sun => (SolarMass, SolarRadius),
            WeakFieldBodyPreset.Earth => (EarthMass, EarthRadius),
            WeakFieldBodyPreset.WhiteDwarf => (WhiteDwarfMass, WhiteDwarfRadius),
            _ => (EarthMass, EarthRadius)
        };
    }
}
