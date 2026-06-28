namespace TRM.Core.Shared;

public struct PhotonMemoryState
{
    public double x;
    public double y;
    public double vx;
    public double vy;

    // Transport memory:
    // memory = ∫ phi * |dmu/dt| dt
    public double memory;
}