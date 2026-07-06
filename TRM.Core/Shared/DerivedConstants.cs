namespace TRM.Core.Shared;

/// <summary>
/// Derived physical constants reconstructed from a PlanckConstants tuple.
/// </summary>
public class DerivedConstants
{
    private readonly PlanckConstants _planck;

    public DerivedConstants(PlanckConstants planck)
    {
        _planck = planck;
    }

    public double SpeedOfLight => _planck.lP / _planck.tP;

    public double ReducedPlanck => _planck.mP * _planck.lP * _planck.lP / _planck.tP;

    public double G => (_planck.lP * _planck.lP * _planck.lP) / (_planck.mP * _planck.tP * _planck.tP);
}
