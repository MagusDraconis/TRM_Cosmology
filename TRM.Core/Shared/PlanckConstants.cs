namespace TRM.Core.Shared;

/// <summary>
/// Container for Planck base constants used by TRM quantum/uncertainty experiments.
/// Status: derived (standard SI-to-Planck relations), tested (PlanckConsistencyTests), limitation (no uncertainty-propagation model here).
/// </summary>
public class PlanckConstants
{
    public double lP { get; }
    public double tP { get; }
    public double mP { get; }

    public PlanckConstants(double lp, double tp, double mp)
    {
        lP = lp;
        tP = tp;
        mP = mp;
    }

    public static PlanckConstants FromPhysicalConstants()
    {
        double c = PhysicalConstantsSI.c;
        double hbar = PhysicalConstantsSI.hbar;
        double G = PhysicalConstantsSI.G;

        double lP = Math.Sqrt(hbar * G / Math.Pow(c, 3));
        double tP = lP / c;
        double mP = Math.Sqrt(hbar * c / G);

        return new PlanckConstants(lP, tP, mP);
    }

    public static PlanckConstants FromSimulation(
        double latticeSpacing,
        double timeTick,
        double energyScale)
    {
        if (latticeSpacing <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(latticeSpacing), "Lattice spacing must be positive.");
        if (timeTick <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(timeTick), "Time tick must be positive.");
        if (energyScale <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(energyScale), "Energy scale must be positive.");

        double cSim = latticeSpacing / timeTick;
        double mPSim = energyScale / (cSim * cSim);

        return new PlanckConstants(latticeSpacing, timeTick, mPSim);
    }
}
