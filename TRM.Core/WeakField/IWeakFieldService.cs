namespace TRM.Core.WeakField;

public interface IWeakFieldService
{
    PpnResult ComputePpn(PpnInput input);
    IReadOnlyList<RedshiftPoint> ComputeRedshiftSeries(WeakFieldBodyPreset body, double maxRadiusMultiplier, int samples, double kappa = 0.3, double b = 1.248);
    LightDeflectionResult ComputeLightDeflection(WeakFieldBodyPreset body, double impactParameterMultiplier, double kappa = 0.3, double b = 1.248);
    PerihelionResult ComputePerihelion(PerihelionPlanetPreset planet, double kappa = 0.3, double b = 1.248);
}
