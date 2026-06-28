namespace TRM.Simulations.Experiments;

public class ScanResult
{
    public double epsL;
    public double epsT;
    public double epsM;

    public double c;
    public double hbar;
    public double G;

    public double Stability { get; set; }
    public double HbarError { get; set; } // optional sauber statt Tag

}
