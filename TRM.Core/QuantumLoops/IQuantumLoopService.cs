namespace TRM.Core.QuantumLoops;

public interface IQuantumLoopService
{
    IReadOnlyList<UvLoopPoint> ComputeUvLoopSeries(UvLoopInput input);
}
