using TRM.QuantumCore.Planck;

namespace TRM.Tests.QuantumTests;

public class PhaseCell
{
    public double Phi { get; set; }
    public double Omega { get; set; }
}
public class CoupledPhaseCell
{
    public double Phi { get; set; }
    public double Omega { get; set; }
}

public class PlanckPointInfo
{
    public string Name { get; set; } = "";
    public PlanckConstants Planck { get; set; } = null!;
}