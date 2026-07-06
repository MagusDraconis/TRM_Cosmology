namespace TRM.Core.QuantumLoops;

public sealed record UvLoopPoint(
    double MomentumSquared,
    double Kernel,
    double OneLoopIntegrand,
    double TwoLoopIntegrand,
    double GrKernel,
    double GrOneLoopIntegrand,
    double GrTwoLoopIntegrand);
