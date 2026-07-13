namespace TRM.Core.Analysis;

/// <summary>
/// SPARC residual analysis placeholder.
/// Provides method stubs for future TRM-SPARC comparison.
/// All methods return explicit "NotRun" results — no synthetic data is fabricated.
/// </summary>
public static class SparcResidualAnalysis
{
    public static SparcResult ComputeNewtonianResidualPreview()
        => new() { Status = "NotRun", Message = "Newtonian residual analysis not yet implemented." };

    public static SparcResult ComputeTrmResponseKernelProjection()
        => new() { Status = "NotRun", Message = "TRM response kernel projection not yet implemented." };

    public static SparcResult ComputeLogPeriodicResidualDiagnostic()
        => new() { Status = "NotRun", Message = "Log-periodic residual diagnostic not yet implemented." };

    public static SparcResult ComputeFractalResidualDiagnostic()
        => new() { Status = "NotRun", Message = "Fractal residual diagnostic not yet implemented." };

    public static SparcResult CompareToEnergyLoadKernel()
        => new() { Status = "NotRun", Message = "Energy-load kernel comparison not yet implemented." };
}

public class SparcResult
{
    public string Status { get; set; } = "NotRun";
    public string Message { get; set; } = "";
}
