namespace TRM.App.Models;

/// <summary>
/// V6 geometry trajectory model for the Blazor UI.
/// Mirrors V6Trajectory from TRM.Core.Geometry.V6 but as a serializable POCO.
/// </summary>
public class V6TrajectoryModel
{
    public int Epochs { get; set; }
    public double[] I1 { get; set; } = [];
    public double[] I2 { get; set; } = [];
    public double[] ArcLength { get; set; } = [];
    public double[] G22 { get; set; } = [];
    public double I1Mean { get; set; }
    public double I1CV { get; set; }
    public double I2Mean { get; set; }
    public double I2CV { get; set; }
    public double TotalArcLength { get; set; }
    public double G22Mean { get; set; }
    public double G22Median { get; set; }
    public double Eccentricity { get; set; }
    public double AxisRatio { get; set; }
    public bool IsArcMonotonic { get; set; }
    public bool IsI1Invariant { get; set; }
    public bool IsI2Invariant { get; set; }
    public bool IsMetricEuclidean { get; set; }
}
