namespace TRM.Core.QuantumLoops;

public sealed record UvLoopInput(
    double Kappa,
    double B,
    double LambdaSquared,
    double PMin,
    double PMax,
    int Samples);
