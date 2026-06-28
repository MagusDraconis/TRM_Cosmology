namespace TRM.Core;

public struct TransportExtractionResult
{
    public double AlphaBase;
    public double AlphaSchwarz;
    public double MissingAlpha;

    public double TransportIntegral;
    public double FEff;

    public double MaxPhi;
    public double AvgPhi;
    public double AvgAbsDmuDt;
    public double WeightedAvgPhi;
}